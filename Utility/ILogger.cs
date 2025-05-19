using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReqIFBridge.Utility
{
    public interface ILogger
    {
        void Log(Object o);
        void Log(LogLevel level, Object o);
        void LogFatal(Object o);
        void LogError(Object o);
        void LogWarn(Object o);
        void LogInfo(Object o);
        void LogDebug(Object o);

        bool IsDebugEnabled
        {
            get;
        }
    }
    public enum LogLevel
    {
        Off,
        Fatal,
        Error,
        Warn,
        Info,
        Debug,
        All,
    }
}