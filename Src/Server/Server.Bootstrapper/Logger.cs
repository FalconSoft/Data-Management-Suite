using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FalconSoft.ReactiveWorksheets.Common;

namespace FalconSoft.ReactiveWorksheets.Server.Bootstrapper
{
    public class Logger : ILogger
    {
        private readonly log4net.ILog _log;

        public Logger()
        {
            log4net.Config.XmlConfigurator.Configure();

            _log = log4net.LogManager.GetLogger("Main Log");
        }

        public void Debug(string message)
        {
            _log.Debug(message);
        }

        public void Debug(string message, Exception exception)
        {
            _log.Debug(message, exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            _log.DebugFormat(format, args);
        }

        public void Error(string message)
        {
            _log.Error(message);
        }

        public void Error(string message, Exception exception)
        {
            _log.Error(message, exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            _log.ErrorFormat(format, args);
        }

        public void Info(string message)
        {
            _log.Info(message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            _log.InfoFormat(format, args);
        }

        public void Warn(object message)
        {
            _log.Warn(message);
        }

        public void Warn(object message, Exception exception)
        {
            _log.Warn(message, exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            _log.WarnFormat(format, args);
        }
    }
}
