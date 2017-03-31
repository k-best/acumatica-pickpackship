using Acumatica.DeviceHub.Properties;
using Acumatica.DeviceHub.ScreenApi;
using Newtonsoft.Json;
using PdfPrintingNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Acumatica.DeviceHub.DTO;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json.Linq;

namespace Acumatica.DeviceHub
{
    internal class PrintJobMonitor : IMonitor, IDisposable
    {
        private IProgress<MonitorMessage> _progress;

        private const string PrintJobsScreen = "SM206500";
        private const string PrintQueuesScreen = "SM206510";
        private Screen _screen;
        private Dictionary<string, PrintQueue> _queues;
        private ConcurrentDictionary<string,string> _alreadyProcessed;
        private readonly string _basicAuthToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(Properties.Settings.Default.Login + ":" + Settings.ToInsecureString(Settings.DecryptString(Properties.Settings.Default.Password))));
        private string _companyNameSegment;
        private static readonly string Printjobs = "PrintJobs";
        private HubConnection _connection { get; set; }
        
        public Task Initialize(Progress<MonitorMessage> progress, CancellationToken cancellationToken)
        {
            var loginSplit = Properties.Settings.Default.Login.Split('@');
            if (loginSplit.Length > 1 && !string.IsNullOrEmpty(loginSplit[loginSplit.Length - 1]))
            {
                _companyNameSegment = "/" + loginSplit[loginSplit.Length - 1];
            }
           
            _progress = progress;
            _alreadyProcessed = new ConcurrentDictionary<string, string>();
            _queues = JsonConvert.DeserializeObject<IEnumerable<PrintQueue>>(Properties.Settings.Default.Queues).ToDictionary<PrintQueue, string>(q => q.QueueName);
            
            if (_queues.Count == 0)
            {
                _progress.Report(new MonitorMessage(Strings.PrintQueuesConfigurationMissingWarning));
                return null;
            }
            return Start(cancellationToken).ContinueWith(async t =>
            {
                if (t.IsFaulted)
                    await Restart(cancellationToken);
            }, cancellationToken);
        }

        private async Task Restart(CancellationToken cancellationToken)
        {
            if(cancellationToken.IsCancellationRequested)
                return;
            Thread.Sleep(Properties.Settings.Default.ErrorWaitInterval);
            Stop();
            await Start(cancellationToken).ContinueWith(async t =>
            {
                if (t.IsFaulted)
                    await Restart(cancellationToken);
            }, cancellationToken);
        }

        private async Task Start(CancellationToken cancellationToken)
        {
            var jobsRequestUri = FormOdataUri();
            try
            {
                while (!LoginToAcumatica())
                {
                    Thread.Sleep(Properties.Settings.Default.PrinterPollingInterval);
                }
                await SubscribeToPushNotificationsAboutPrintJobs(cancellationToken);
                await PollPrintJobs(jobsRequestUri, cancellationToken);
            }
            catch (Exception ex)
            {
                // Assume the server went offline or our session got lost - new login will be attempted in next iteration
                _progress.Report(new MonitorMessage(string.Format(Strings.PollingQueueUnknownError, ex.Message),
                    MonitorMessage.MonitorStates.Error));
                throw;
            }
        }

        private void Stop()
        {
            LogoutFromAcumatica();
            _screen?.Dispose();
            _connection.Stop();
        }

        private async Task SubscribeToPushNotificationsAboutPrintJobs(CancellationToken cancellationToken)
        {
            _connection = new HubConnection(Properties.Settings.Default.AcumaticaUrl);
            _connection.Headers.Add("Authorization", "Basic " + _basicAuthToken);
            var hub = _connection.CreateHubProxy("PushNotificationsHub");
            await _connection.Start().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _progress.Report(new MonitorMessage($"There was an error opening the connection:{task.Exception?.GetBaseException()}", MonitorMessage.MonitorStates.Error));
                }
                else
                {
                    hub.Invoke<string>("Subscribe", Printjobs).Wait(cancellationToken);
                }
            }, cancellationToken);
            hub.On<NotificationResult>("ReceiveNotification", nr=>ProcessPrintJobNotification(nr, cancellationToken));
            _connection.Closed += () => Restart(cancellationToken).Wait(cancellationToken);
        }

        private void ProcessPrintJobNotification(NotificationResult nr, CancellationToken cancellationToken)
        {
            var inserted = nr.Inserted.GroupBy(c => new PrintJob() {Description = c.Description, JobID = c.JobID, PrintQueue = c.PrintQueue, ReportID = c.ReportID}, c=>new{c.ParameterName, c.ParameterValue});
            var deleted = nr.Deleted.GroupBy(c => new PrintJob() { Description = c.Description, JobID = c.JobID, PrintQueue = c.PrintQueue, ReportID = c.ReportID }, c => new { c.ParameterName, c.ParameterValue }).ToDictionary(c=>c.Key, c=>c.ToArray());
            foreach (var row in inserted)
            {
                PrintQueue queue;
                if (deleted.ContainsKey(row.Key)||!_queues.TryGetValue(row.Key.PrintQueue, out queue)) continue;
                _alreadyProcessed?.TryAdd(row.Key.JobID, row.Key.JobID);
                try
                {
                    ProcessJob(queue, row.Key.JobID, row.Key.ReportID, row.Key.Description,
                        row.AsEnumerable()
                            .Where(c => c.ParameterName != null)
                            .ToDictionary(c => c.ParameterName, c => c.ParameterValue));
                }
                catch (Exception e)
                {
                    _progress.Report(new MonitorMessage(string.Format(Strings.PollingQueueUnknownError, e.Message),
                    MonitorMessage.MonitorStates.Error));
                    Restart(cancellationToken).Wait(cancellationToken);
                }
            }
        }

        private bool LoginToAcumatica()
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.LoginNotify, Properties.Settings.Default.AcumaticaUrl), MonitorMessage.MonitorStates.Undefined));
            _screen = new Screen
            {
                Url = Properties.Settings.Default.AcumaticaUrl + "/Soap/.asmx",
                CookieContainer = new CookieContainer()
            };

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
            if (_screen == null) return;
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

        private async Task PollPrintJobs(string jobsRequestUri, CancellationToken cancellationToken)
        {
            _progress.Report(new MonitorMessage(Strings.PrintJobStartPollingNotify));

            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri(Properties.Settings.Default.AcumaticaUrl + "/odata/"),
                DefaultRequestHeaders = { Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json")}, Authorization = new AuthenticationHeaderValue("Basic", _basicAuthToken) }
            };

            var result = await httpClient.GetAsync(httpClient.BaseAddress+_companyNameSegment + jobsRequestUri, cancellationToken);
            if (result.StatusCode == HttpStatusCode.NotFound)
            {
                result = await httpClient.GetAsync(jobsRequestUri, cancellationToken);
            }
            if (!result.IsSuccessStatusCode)
            {
                var errorContent = await result.Content.ReadAsStringAsync();
                throw new InvalidOperationException(errorContent);
            }
            var content = (await result.Content.ReadAsAsync<JObject>(cancellationToken));
            var jobParameters = content.Value<JArray>("value").Select(c=>c.ToObject<PrintJobParameter>()).ToArray();

            var jobs = jobParameters.GroupBy(c => new PrintJob() { Description = c.Description, JobID = c.JobID, PrintQueue = c.PrintQueue, ReportID = c.ReportID }, c => new { c.ParameterName, c.ParameterValue });
            foreach (var job in jobs)
            {
                if(cancellationToken.IsCancellationRequested)
                    return;
                PrintQueue queue;
                if (job.Key.JobID == null || _alreadyProcessed.ContainsKey(job.Key.JobID) || !_queues.TryGetValue(job.Key.PrintQueue, out queue))
                    continue;
                ProcessJob(queue, job.Key.JobID, job.Key.ReportID, job.Key.Description,
                    job.AsEnumerable().Where(c => c.ParameterName != null).ToDictionary(c => c.ParameterName, c => c.ParameterValue));
            }
            _alreadyProcessed = null;
        }

        private string FormOdataUri()
        {
            var filterValue = string.Join(" or ", _queues.Keys.Select(c => "PrintQueue eq '" + c + "'"));
            return "/"+Printjobs+"?$filter=" + filterValue;
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
                        PrintRaw(queue, jobDescription, data);
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

        private void PrintRaw(PrintQueue queue, string jobDescription, byte[] rawData)
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.PrintRawDataNotify, queue.QueueName, queue.PrinterName)));
            RawPrinterHelper.SendRawBytesToPrinter(queue.PrinterName, jobDescription, rawData);
        }

        private void PrintPdf(string jobDescription, PrintQueue queue, byte[] pdfReport)
        {
            _progress.Report(new MonitorMessage(String.Format(Strings.PrintPdfNotify, queue.QueueName, queue.PrinterName)));
            var pdfPrint = new PdfPrint("Acumatica", "g/4JFMjn6KtGjMEKn2ZY1H6+hSqKUHU+273JkSGKwP0=");
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
            else if (queue.Orientation == PrintQueue.PrinterOrientation.Landscape)
            {
                pdfPrint.IsLandscape = true;
            }
            else if (queue.Orientation == PrintQueue.PrinterOrientation.Portrait)
            {
                pdfPrint.IsLandscape = false;
            }

            pdfPrint.Print(pdfReport, new PdfWatermark(), jobDescription);
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

        public void Dispose()
        {
            Stop();
        }
    }

    public class PrintJob
    {
        public string JobID { get; set; }
        public string ReportID { get; set; }
        public string PrintQueue { get; set; }
        public string Description { get; set; }

        protected bool Equals(PrintJob other)
        {
            return string.Equals(JobID, other.JobID) && string.Equals(PrintQueue, other.PrintQueue);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PrintJob)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((JobID != null ? JobID.GetHashCode() : 0) * 397) ^ (PrintQueue != null ? PrintQueue.GetHashCode() : 0);
            }
        }
    }
}
