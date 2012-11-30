using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.ServiceProcess;
using OpenAX25Contracts;
using OpenAX25Core;

namespace OpenAX25_Service
{
    [PermissionSetAttribute(SecurityAction.LinkDemand, Name = "FullTrust")]
    public partial class OpenAX25Service : ServiceBase, ILogProvider
    {
        internal const string SERVICE_NAME = "OpenAX25Service";

        private StreamWriter m_logWriter = null;

        public OpenAX25Service()
        {
            InitializeComponent();
            this.ServiceName = SERVICE_NAME;
        }

        protected override void OnStart(string[] args)
        {
            Runtime runtime = new Runtime();
            Runtime.Instance = runtime;
            Assembly assembly = Assembly.GetAssembly(typeof(OpenAX25Service));
            Configuration config =
                ConfigurationManager.OpenExeConfiguration(assembly.Location);
            KeyValueConfigurationCollection appSettings = config.AppSettings.Settings;
            if (appSettings == null)
                throw new Exception("Unable to read application settings");

            KeyValueConfigurationElement _v = appSettings["LogLevel"];
            if (_v == null)
                throw new Exception("Key \"LogLevel\" is not set");
            string _logLevel = _v.Value;
            if ("NONE".Equals(_logLevel))
                runtime.LogLevel = LogLevel.NONE;
            else if ("ERROR".Equals(_logLevel))
                runtime.LogLevel = LogLevel.ERROR;
            else if ("WARNING".Equals(_logLevel))
                runtime.LogLevel = LogLevel.WARNING;
            else if ("INFO".Equals(_logLevel))
                runtime.LogLevel = LogLevel.INFO;
            else if ("DEBUG".Equals(_logLevel))
                runtime.LogLevel = LogLevel.DEBUG;
            else
                throw new Exception(
                    "Invalid LogLevel (NONE|ERROR|WARNING|INFO|DEBUG): \"" +
                        ((_logLevel != null)?_logLevel:"<NULL>") + "\"");
            if (runtime.LogLevel > LogLevel.NONE)
            {
                runtime.LogProvider = this;
                _v = appSettings["LogFile"];
                if (_v == null)
                    throw new Exception("Key \"LogFile\" is not set");
                m_logWriter = File.AppendText(_v.Value);
                runtime.Log(LogLevel.INFO, SERVICE_NAME, "Service start");
            }
            _v = appSettings["ConfigFile"];
            if (_v == null)
                throw new Exception("Key \"ConfigFile\" is not set");
            runtime.LoadConfig(_v.Value);
        }

        protected override void OnStop()
        {
            Runtime runtime = Runtime.Instance;
            runtime.Log(LogLevel.INFO, SERVICE_NAME, "Service start");
            runtime.Shutdown();
            runtime = null;
            if (m_logWriter != null)
            {
                m_logWriter.Flush();
                m_logWriter.Close();
            }
            m_logWriter = null;
        }

        /// <summary>
        /// The logging implementation.
        /// </summary>
        /// <param name="component">Name of the component</param>
        /// <param name="message">Log message</param>
        public void OnLog(string component, string message)
        {
            string text = String.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}",
                DateTime.UtcNow, component, message);
            m_logWriter.WriteLine(text);
            m_logWriter.Flush();
        }

    }
}
