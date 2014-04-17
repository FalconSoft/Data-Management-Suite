using System;

namespace FalconSoft.ReactiveWorksheets.Common
{
    public interface ILogger
    {
        void Debug(string message);

        void Debug(string message, Exception exception);

        void DebugFormat(string format, params object[] args);

        void Error(string message);

        void Error(string message, Exception exception);

        void ErrorFormat(string format, params object[] args);

        void Info(string message);

        void InfoFormat(string format, params object[] args);

        void Warn(object message);

        void Warn(object message, Exception exception);

        void WarnFormat(string format, params object[] args);
    }
}
