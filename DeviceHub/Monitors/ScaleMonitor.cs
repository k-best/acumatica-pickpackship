using Acumatica.DeviceHub.Properties;
using Acumatica.DeviceHub.ScreenApi;
using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Acumatica.DeviceHub
{
    class ScaleMonitor : IMonitor
    {
        private IProgress<MonitorMessage> _progress;

        private const string ScaleScreen = "SM206530";
        private ScreenApi.Screen _screen;
        private decimal _lastWeightSentToAcumatica = 0;

        #region HID Constants 

        // Ref: http://www.usb.org/developers/hidpage/pos1_02.pdf

        private enum ReportId : byte
        {
            ScaleAttributes = 0x01,
            ScaleControl = 0x02,
            ScaleData = 0x03,
            ScaleStatus = 0x04,
            ScaleWeightLimit = 0x05,
            ScaleStatistics = 0x06,
        }

        private enum ScaleData : int
        {
            ScaleStatus = 0x00,
            WeightUnit = 0x01,
            DataScaling = 0x02,
            DataWeightLSB = 0x03,
            DataWeightMSB = 0x04
        }

        private enum ScaleStatus : byte
        {
            None = 0x00,
            Fault = 0x01,
            StableAtCenterOfZero = 0x02,
            InMotion = 0x03,
            WeightStable = 0x04,
            UnderZero = 0x05,
            OverWeightLimit = 0x06,
            RequiresCalibration = 0x07,
            RequiresReZeroing = 0x08
        }

        private enum WeightUnits : byte
        {
            None = 0x00,
            Milligram = 0x01,
            Gram = 0x02,
            Kilogram = 0x03,
            Carats = 0x04,
            Taels = 0x05,
            Grains = 0x06,
            Pennyweights = 0x07,
            MetricTon = 0x08,
            AvoirTon = 0x09,
            TroyOunce = 0x0A,
            Ounce = 0x0B,
            Pound = 0x0C
        }

        private Dictionary<WeightUnits, string> WeightUnitToStringAbbreviation = new Dictionary<WeightUnits, string>()
        {
            { WeightUnits.None, string.Empty },
            { WeightUnits.Milligram, "mg" },
            { WeightUnits.Gram, "g" },
            { WeightUnits.Kilogram, "kg" },
            { WeightUnits.Carats, "ct" },
            { WeightUnits.Taels, "ozt" },
            { WeightUnits.Grains, "gr" },
            { WeightUnits.Pennyweights, "dwt" },
            { WeightUnits.MetricTon, "mt" },
            { WeightUnits.AvoirTon, "t" },
            { WeightUnits.TroyOunce, "t oz" },
            { WeightUnits.Ounce, "oz" },
            { WeightUnits.Pound, "lb" }
        };

        #endregion

        public Task Initialize(Progress<MonitorMessage> progress, CancellationToken cancellationToken)
        {
            _progress = progress;

            if(String.IsNullOrEmpty(Properties.Settings.Default.ScaleID))
            {
                _progress.Report(new MonitorMessage(Strings.ScaleConfigurationMissingWarning));
                return null;
            }

            return Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            LogoutFromAcumatica();
                            break;
                        }

                        decimal currentWeight = 0;
                        WeightUnits weightUnit;
                        HidDevice hidDevice = HidDevices.Enumerate(Properties.Settings.Default.ScaleDeviceVendorId, Properties.Settings.Default.ScaleDeviceProductId).FirstOrDefault();

                        if (hidDevice != null)
                        {
                            using (hidDevice)
                            {
                                hidDevice.OpenDevice();
                                WaitForConnection(hidDevice);
                                currentWeight = ReadWeight(hidDevice, out weightUnit);
                                hidDevice.CloseDevice();
                            }

                            _progress.Report(new MonitorMessage(String.Format(Strings.ScaleWeightNotify, currentWeight, WeightUnitToStringAbbreviation[weightUnit])));

                            if (_lastWeightSentToAcumatica != currentWeight)
                            {
                                if (_screen != null || LoginToAcumatica())
                                {
                                    UpdateWeight(Properties.Settings.Default.ScaleID, currentWeight);
                                    _lastWeightSentToAcumatica = currentWeight;
                                }
                            }
                        }

                        System.Threading.Thread.Sleep(Properties.Settings.Default.ScaleReadInterval);
                    }
                    catch (Exception ex)
                    {
                        // Assume the server went offline or our session got lost - new login will be attempted in next iteration
                        _progress.Report(new MonitorMessage(String.Format(Strings.ScaleWeightError, ex.Message), MonitorMessage.MonitorStates.Error));
                        _screen = null;
                        System.Threading.Thread.Sleep(Properties.Settings.Default.ErrorWaitInterval);
                    }
                }
            });
        }

        private static decimal ReadWeight(HidDevice hidDevice, out WeightUnits weightUnit)
        {
            const int decimalBase = 10;
            const int readTimeout = 250;
            const byte eightBits = 8;
            decimal weight = 0;
            weightUnit = WeightUnits.None;

            HidReport hidReport = hidDevice.ReadReport(readTimeout);

            if (ValidateReport(hidReport))
            {
                // Ref: http://www.usb.org/developers/hidpage/pos1_02.pdf
                byte[] data = hidReport.Data;

                // Unpack weight word
                int lowOrderbyte = data[(int)ScaleData.DataWeightLSB];
                int highOrderbyte = data[(int)ScaleData.DataWeightMSB] << eightBits;

                // Data scaling uses a signed byte value
                sbyte dataScaling = (sbyte)data[(int)ScaleData.DataScaling];

                // Weight = (High Order Byte + Low Order Byte) * (10 ^ Scale Factor)
                weight = (highOrderbyte + lowOrderbyte) * (decimal)Math.Pow(decimalBase, dataScaling);

                // Assign weight unit
                weightUnit = (WeightUnits)data[(int)ScaleData.WeightUnit];
            }

            return weight;
        }

        private static bool ValidateReport(HidReport hidReport)
        {
            // No data or wrong data type, wait for reading
            if (hidReport == null ||
                hidReport.Data == null ||
                !hidReport.ReadStatus.Equals(HidDeviceData.ReadStatus.Success) ||
                !hidReport.ReportId.Equals((byte)ReportId.ScaleData))
            {
                return false;
            }

            switch ((ScaleStatus)hidReport.Data[(int)ScaleData.ScaleStatus])
            {
                // Scale is empty
                case ScaleStatus.StableAtCenterOfZero:
                    goto case ScaleStatus.WeightStable;

                // Scale has a valid non-zero reading
                case ScaleStatus.WeightStable:
                    return true;

                // Weighting in progress, wait for reading to stabilize
                case ScaleStatus.InMotion:
                    return false;

                // Error weight overflow
                case ScaleStatus.OverWeightLimit:
                    throw new ApplicationException(Strings.ScaleOverweightError);

                // Error requires manual check, calibration and zeroing
                case ScaleStatus.Fault:
                    goto case ScaleStatus.RequiresCalibration;

                case ScaleStatus.UnderZero:
                    goto case ScaleStatus.RequiresCalibration;

                case ScaleStatus.RequiresReZeroing:
                    goto case ScaleStatus.RequiresCalibration;

                case ScaleStatus.RequiresCalibration:
                    throw new ApplicationException(Strings.ScaleCalibrationError);

                default:
                    return false;
            }
        }

        private static void WaitForConnection(HidDevice scale)
        {
            const int retryTimeout = 250;
            const int retryCount = 20;
            int waitTries = 0;

            while (!scale.IsConnected)
            {
                // Sometimes the scale doesn't open immediately, retry a few times.
                Thread.Sleep(retryTimeout);

                waitTries++;
                if (waitTries > retryCount)
                {
                    throw new ApplicationException(Strings.ScaleConnectionError);
                }
            }
        }

        private void UpdateWeight(string scaleID, decimal weight)
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.UpdateScaleWeightNotify, scaleID)));
            var commands = new Command[]
            {
                new Key { ObjectName = "Scale", FieldName = "ScaleID", Value = "=[Scale.ScaleID]" },
                new ScreenApi.Action { FieldName = "Cancel", ObjectName = "Scale" },
                new Value { Value = scaleID, ObjectName = "Scale", FieldName = "ScaleID", Commit = true },
                new Value { Value = weight.ToString(System.Globalization.CultureInfo.InvariantCulture), ObjectName = "Scale", FieldName = "LastWeight" },
                new ScreenApi.Action { FieldName = "Save", ObjectName = "Scale" },
            };
            var result = _screen.Submit(ScaleScreen, commands);
            _progress.Report(new MonitorMessage(String.Format(Strings.UpdateScaleWeightSuccessNotify, scaleID), MonitorMessage.MonitorStates.Ok));
        }

        private bool LoginToAcumatica()
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.LoginNotify, Properties.Settings.Default.AcumaticaUrl)));
            _screen = new ScreenApi.Screen();
            _screen.Url = Properties.Settings.Default.AcumaticaUrl + "/Soap/.asmx";
            _screen.CookieContainer = new System.Net.CookieContainer();

            try
            {
                _screen.Login(Properties.Settings.Default.Login, Settings.ToInsecureString(Settings.DecryptString(Properties.Settings.Default.Password)));
                return true;
            }
            catch
            {
                _screen = null;
                throw;
            }
        }

        private void LogoutFromAcumatica()
        {
            _progress.Report(new MonitorMessage(Strings.LogoutNotify));
            if (_screen != null)
            {
                try
                {
                    _screen.Logout();
                }
                catch
                {
                    //Ignore all errors in logout.
                }
                _screen = null;
            }
        }
    }
}
