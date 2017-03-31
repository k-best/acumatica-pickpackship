using Acumatica.DeviceHub.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Acumatica.DeviceHub
{
    public partial class Main : Form
    {
        private List<Task> _tasks;
        private List<IMonitor> _monitors;
        private HashSet<object> _errorTasks;
        private CancellationTokenSource _cancellationTokenSource;

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(Properties.Settings.Default.AcumaticaUrl))
            {
                using (var form = new Configuration())
                {
                    if(form.ShowDialog() != DialogResult.OK)
                    {
                        Application.Exit();
                        return;
                    }
                }
            }
            
            StartMonitors();
        }
        
        private void StartMonitors()
        {
            WriteToLog(Strings.StartMonitoringNotify);
            _cancellationTokenSource = new CancellationTokenSource();
            _tasks = new List<Task>();
            _monitors = new List<IMonitor>();
            _errorTasks = new HashSet<object>();

            var monitorTypes = new Type[] { typeof(PrintJobMonitor), typeof(ScaleMonitor) };
            foreach (Type t in monitorTypes)
            {
                IMonitor monitor = (IMonitor) Activator.CreateInstance(t);
                Task task = monitor.Initialize(new Progress<MonitorMessage>(p => HandleMonitorProgress(monitor, p)), _cancellationTokenSource.Token);
                if(task != null)
                {
                    _tasks.Add(task);
                    WriteToLog(String.Format(Strings.StartMonitoringSuccessNotify, t.Name));
                }
            }
        }

        private void StopMonitors()
        {
            if (_tasks == null) return;

            WriteToLog(Strings.StopMonitoringNotify);
            _cancellationTokenSource.Cancel();
            Task.WaitAll(_tasks.ToArray());
            foreach (var monitor in _monitors.OfType<IDisposable>())
            {
                monitor.Dispose();
            }
            _tasks = null;
            _monitors = null;
            _cancellationTokenSource = null;
            WriteToLog(Strings.StopMonitoringSuccessNotify);
        }

        private void HandleMonitorProgress(object sender, MonitorMessage message)
        {
            WriteToLog(message.Text);

            if (message.State == MonitorMessage.MonitorStates.Error)
            {
                if(!_errorTasks.Contains(sender))
                {
                    _errorTasks.Add(sender);
                }
            }
            else if(message.State == MonitorMessage.MonitorStates.Ok)
            {
                if(_errorTasks.Contains(sender))
                {
                    _errorTasks.Remove(sender);
                }
            }

            if(_errorTasks.Count > 0)
            {
                notifyIcon.Icon = Properties.Resources.AppRed;
            }
            else
            {
                notifyIcon.Icon = Properties.Resources.App;
            }
        }

        private void WriteToLog(string message)
        {
            logListBox.Items.Insert(0, (object)DateTime.Now.ToString() + " - " + message);
            if (logListBox.Items.Count > 100) logListBox.Items.RemoveAt(100);
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
            else
            { 
                StopMonitors();
            }
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            notifyIcon.Visible = false;
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                notifyIcon.Visible = true;

                notifyIcon.ShowBalloonTip(1000, this.Text, Strings.MinimizedToNotificationAreaWarning, ToolTipIcon.Info);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void configureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopMonitors();
            using (var form = new Configuration())
            {
                form.ShowDialog();
            }
            StartMonitors();
        }
    }
}
