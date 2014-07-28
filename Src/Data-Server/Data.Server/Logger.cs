using System;
using System.IO;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using ILogger = FalconSoft.Data.Management.Common.ILogger;

namespace FalconSoft.Data.Server
{
    public class Logger : ILogger
    {
        private readonly ILog _log;

        public Logger()
        {
            Setup();
            _log = LogManager.GetLogger("Main Log");
        }

        public static void Setup()
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            var patternLayout = new PatternLayout
            {
                ConversionPattern = "%date [%thread] %-5level %logger - %message%newline"
            };
            patternLayout.ActivateOptions();

            var roller = new RollingFileAppender
            {
                AppendToFile = true,
                //C:\Users\%UserName%\AppData\Local\Temp\FalconSoft\ReactiveWorksheets\server-log-file.txt
                File = Path.Combine(Path.GetTempPath(), "FalconSoft", "ReactiveWorksheets" + @"\server-log-file.txt"),
                Layout = patternLayout,
                MaxSizeRollBackups = 10,
                MaximumFileSize = "10MB",
                RollingStyle = RollingFileAppender.RollingMode.Size,
                StaticLogFileName = true
            };
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            var memory = new MemoryAppender();
            memory.ActivateOptions();
            hierarchy.Root.AddAppender(memory);

            hierarchy.Root.Level = Level.Info;
            hierarchy.Configured = true;
        }

        public void Debug(string message)
        {
            _log.Debug(message);
            Console.WriteLine(message);
        }

        public void Debug(string message, Exception exception)
        {
            _log.Debug(message, exception);
            Console.WriteLine(message + string.Format("Exception : {0}", exception.Message));
        }

        public void DebugFormat(string format, params object[] args)
        {
            _log.DebugFormat(format, args);
            Console.WriteLine(format,args);
        }

        public void Error(string message)
        {
            _log.Error(message);
            Console.WriteLine(message);
        }

        public void Error(string message, Exception exception)
        {
            _log.Error(message, exception);
            Console.WriteLine(message + string.Format("Exception : {0}", exception.Message));
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _log.ErrorFormat(format, args);
            Console.WriteLine(format, args);
        }

        public void Info(string message)
        {
            _log.Info(message);
            Console.WriteLine(message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            _log.InfoFormat(format, args);
            Console.WriteLine(format,args);
        }

        public void Warn(object message)
        {
            _log.Warn(message);
            Console.WriteLine(message);
        }

        public void Warn(object message, Exception exception)
        {
            _log.Warn(message, exception);
            Console.WriteLine(message + string.Format("Exception : {0}", exception.Message));
        }

        public void WarnFormat(string format, params object[] args)
        {
            _log.WarnFormat(format, args);
            Console.WriteLine(format, args);
        }
    }
}
