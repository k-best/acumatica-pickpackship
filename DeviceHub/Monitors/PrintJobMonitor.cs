using Acumatica.DeviceHub.Properties;
using Acumatica.DeviceHub.ScreenApi;
using Newtonsoft.Json;
using PdfPrintingNet;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Acumatica.DeviceHub
{
    class PrintJobMonitor : IMonitor
    {
        private IProgress<MonitorMessage> _progress;

        private const string PrintJobsScreen = "SM206500";
        private const string PrintQueuesScreen = "SM206510";
        private ScreenApi.Screen _screen;
        private Dictionary<string, PrintQueue> _queues;

        public Task Initialize(Progress<MonitorMessage> progress, CancellationToken cancellationToken)
        {
            _progress = progress;
            _queues = JsonConvert.DeserializeObject<IEnumerable<PrintQueue>>(Properties.Settings.Default.Queues).ToDictionary<PrintQueue, string>(q => q.QueueName);

            if(_queues.Count == 0)
            {
                _progress.Report(new MonitorMessage(Strings.PrintQueuesConfigurationMissingWarning));
                return null;
            }

            return Task.Run(() =>
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogoutFromAcumatica();
                        break;
                    }

                    try
                    { 
                        if(_screen != null || LoginToAcumatica())
                        { 
                            PollPrintJobs();
                        }

                        System.Threading.Thread.Sleep(Properties.Settings.Default.PrinterPollingInterval);
                    }
                    catch(Exception ex)
                    {
                        // Assume the server went offline or our session got lost - new login will be attempted in next iteration
                        _progress.Report(new MonitorMessage(String.Format(Strings.PollingQueueUnknownError, ex.Message), MonitorMessage.MonitorStates.Error));
                        _screen = null;
                        System.Threading.Thread.Sleep(Properties.Settings.Default.ErrorWaitInterval);
                    }
                }
            });
        }
        
        private bool LoginToAcumatica()
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.LoginNotify, Properties.Settings.Default.AcumaticaUrl), MonitorMessage.MonitorStates.Undefined));
            _screen = new ScreenApi.Screen();
            _screen.Url = Properties.Settings.Default.AcumaticaUrl + "/Soap/.asmx";
            _screen.CookieContainer = new System.Net.CookieContainer();

            try
            {
                _screen.Login(Properties.Settings.Default.Login, Settings.ToInsecureString(Settings.DecryptString(Properties.Settings.Default.Password)));
                return VerifyQueues();
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

        private bool VerifyQueues()
        {
            _progress.Report(new MonitorMessage(Strings.PrintQueueInitializeNotify));

            var configuredQueues = GetAvailableQueuesFromAcumatica();
            foreach (var queue in _queues)
            {
                if (configuredQueues.Contains(queue.Key))
                {
                    _progress.Report(new MonitorMessage(String.Format(Strings.PrintQueueInitializeSuccessNotify, queue.Key)));
                }
                else
                {
                    _progress.Report(new MonitorMessage(String.Format(Strings.PrintQueuesConfigurationMissingWarning, queue.Key)));
                    return false;
                }
            }

            return true;
        }

        private HashSet<string> GetAvailableQueuesFromAcumatica()
        {
            var commands = new Command[]
            {
                new Field { FieldName = "PrintQueue", ObjectName = "Queues" }
            };

            var results = _screen.Export(PrintQueuesScreen, commands, null, 0, false, true);
            var queueNames = new HashSet<string>();

            for (int i = 0; i < results.Length; i++)
            {
                queueNames.Add(results[i][0]);
            }

            return queueNames;
        }
        
        private void PollPrintJobs()
        {
            _progress.Report(new MonitorMessage(Strings.PrintJobStartPollingNotify));
            var commands = new Command[]
            {
                new Field { FieldName = "JobID", ObjectName = "Job" },
                new Field { FieldName = "ReportID", ObjectName = "Job" },
                new Field { FieldName = "PrintQueue", ObjectName = "Job" },
                new Field { FieldName = "Description", ObjectName = "Job" },
                new Field { FieldName = "ParameterName", ObjectName = "Parameters" },
                new Field { FieldName = "ParameterValue", ObjectName = "Parameters" },
                new Field { FieldName = "ParameterValue", ObjectName = "Parameters" },
            };

            var filters = new List<Filter>();
            foreach (var queue in _queues)
            {
                filters.Add(new Filter { Field = new Field { FieldName = "PrintQueue", ObjectName = "Job" }, Value = queue.Key, Condition = FilterCondition.Equals, Operator = FilterOperator.Or });
            }

            var results = _screen.Export(PrintJobsScreen, commands, filters.ToArray(), 0, false, true);

            string currentJobID = null;
            string reportID = null;
            string queueName = null;
            string description = null;
            Dictionary<string, string> parameters = null;

            if(results.Length == 0)
            {
                _progress.Report(new MonitorMessage(Strings.EmptyQueueNotify, MonitorMessage.MonitorStates.Ok));
            }
            else
            {
                _progress.Report(new MonitorMessage(Strings.ProcessPrintJobsNotify, MonitorMessage.MonitorStates.Ok));
            }

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i][0] != currentJobID)
                {
                    if (currentJobID != null) ProcessJob(_queues[queueName], currentJobID, reportID, description, parameters);

                    currentJobID = results[i][0];
                    reportID = results[i][1];
                    queueName = results[i][2];
                    description = results[i][3];

                    parameters = new Dictionary<string, string>();
                }

                parameters.Add(results[i][4], results[i][5]);
            }

            if (currentJobID != null) ProcessJob(_queues[queueName], currentJobID, reportID, description, parameters);
        }

        private void ProcessJob(PrintQueue queue, string jobID, string reportID, string jobDescription, Dictionary<string, string> parameters)
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.ProcessPrintJobNotify, queue.QueueName, jobID)));
            const string fileIdKey = "FILEID";
            byte[] data = null;

            if (reportID == String.Empty)
            {
                if (parameters.ContainsKey(fileIdKey))
                {
                    data = GetFileID(parameters[fileIdKey]);
                }
                else
                {
                    _progress.Report(new MonitorMessage(String.Format(Strings.FileIdMissingWarning, queue.QueueName, jobID), MonitorMessage.MonitorStates.Warning));
                }
            }
            else
            {
                data = GetReportPdf(reportID, parameters);
            }

            if (data != null)
            {
                if (queue.RawMode)
                {
                    if (IsPdf(data))
                    {
                        _progress.Report(new MonitorMessage(String.Format(Strings.PdfWrongModeWarning, queue.QueueName, jobID), MonitorMessage.MonitorStates.Warning));
                    }
                    else
                    {
                        PrintRaw(queue, data);
                    }
                }
                else
                {
                    if (IsPdf(data))
                    {
                        PrintPdf(jobDescription, queue, data);
                    }
                    else
                    {
                        _progress.Report(new MonitorMessage(String.Format(Strings.PdfWrongFileFormatWarning, queue.QueueName, jobID), MonitorMessage.MonitorStates.Warning));
                    }
                }
            }

            DeleteJobFromQueue(jobID);
        }

        private static bool IsPdf(byte[] data)
        {
            //%PDF−1.0
            return (data.Length > 4 && data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46);
        }

        private void PrintRaw(PrintQueue queue, byte[] rawData)
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.PrintRawDataNotify, queue.QueueName, queue.PrinterName)));
            RawPrinterHelper.SendRawBytesToPrinter(queue.PrinterName, rawData);
        }

        private void PrintPdf(string jobDescription, PrintQueue queue, byte[] pdfReport)
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.PrintPdfNotify, queue.QueueName, queue.PrinterName)));
            var pdfPrint = new PdfPrint("demoCompany", "demoKey");
            pdfPrint.PrinterName = queue.PrinterName;

            // Retrieve paper size, source and orientation
            var printerSettings = new System.Drawing.Printing.PrinterSettings();
            printerSettings.PrinterName = queue.PrinterName;

            if (queue.PaperSize != PrintQueue.PrinterDefault)
            {
                bool paperSizeSet = false;
                foreach (PaperSize paperSize in printerSettings.PaperSizes)
                {
                    if (paperSize.RawKind == queue.PaperSize)
                    {
                        pdfPrint.PaperSize = paperSize;
                        paperSizeSet = true;
                        break;
                    }
                }

                if (!paperSizeSet)
                {
                    _progress.Report(new MonitorMessage(String.Format(Strings.PaperSizeMissingWarning, queue.PrinterName, queue.PaperSize), MonitorMessage.MonitorStates.Warning));
                }
            }

            if (queue.PaperSource != PrintQueue.PrinterDefault)
            {
                bool paperSourceSet = false;
                foreach (PaperSource paperSource in printerSettings.PaperSources)
                {
                    if (paperSource.RawKind == queue.PaperSource)
                    {
                        pdfPrint.PaperSource = paperSource;
                        paperSourceSet = true;
                        break;
                    }
                }

                if (!paperSourceSet)
                {
                    _progress.Report(new MonitorMessage(String.Format(Strings.PaperSourceMissingWarning, queue.PrinterName, queue.PaperSource), MonitorMessage.MonitorStates.Warning));
                }
            }

            if (queue.Orientation == PrintQueue.PrinterOrientation.Automatic)
            {
                pdfPrint.IsAutoRotate = true;
            }
            else if(queue.Orientation == PrintQueue.PrinterOrientation.Landscape)
            {
                pdfPrint.IsLandscape = true;
            }
            else if(queue.Orientation == PrintQueue.PrinterOrientation.Portrait)
            {
                pdfPrint.IsLandscape = false;
            }

            pdfPrint.Print(pdfReport,
                           new PdfWatermark(),
                           jobDescription);
        }

        private byte[] GetFileID(string fileID)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = _screen.CookieContainer })
            using (var client = new HttpClient(handler))
            using (var result = client.GetAsync(Properties.Settings.Default.AcumaticaUrl + "/entity/Default/6.00.001/files/" + fileID).Result)
            {
                result.EnsureSuccessStatusCode();
                return result.Content.ReadAsByteArrayAsync().Result;
            }
        }

        private byte[] GetReportPdf(string reportID, Dictionary<string, string> parameters)
        {
            var commands = new List<Command>();
            foreach (string parameterName in parameters.Keys)
            {
                commands.Add(new Value { Value = parameters[parameterName], ObjectName = "Parameters", FieldName = parameterName });
            }

            commands.Add(new Field { FieldName = "PdfContent", ObjectName = "ReportResults" });

            var result = _screen.Submit(reportID, commands.ToArray());
            if (result != null && result.Length > 0)
            {
                var field = result[0]
                    .Containers.Where(c => c != null && c.Name == "ReportResults").FirstOrDefault()
                    .Fields.Where(f => f != null && f.FieldName == "PdfContent").FirstOrDefault();

                return Convert.FromBase64String(field.Value);
            }
            else
            {
                throw new ApplicationException(String.Format(Strings.WebServiceReportReturnValueMissingWarning, reportID));
            }
        }

        private void DeleteJobFromQueue(string jobID)
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.DeletePrintJobNotify, jobID)));
            var commands = new Command[]
            {
                new Key { ObjectName = "Job", FieldName = "JobID", Value = "=[Job.JobID]" },
                new ScreenApi.Action { FieldName = "Cancel", ObjectName = "Job" },
                new Value { Value = jobID, ObjectName = "Job", FieldName = "JobID", Commit = true },
                new ScreenApi.Action { FieldName = "Delete", ObjectName = "Job" }
            };

            var result = _screen.Submit(PrintJobsScreen, commands);
        }
    }
}
