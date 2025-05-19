using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReqIFBridge.Utility
{
    public class DebugLogger
    {
        private static ILogger instance;
        private static Log4NetStatus logStatus;
        private DebugLogger()
        {

        }
        public static void InitLogger(string configfile)
        {
            logStatus = new Log4NetStatus();

            try
            {
                instance = new Logger("DebugLogger", configfile);
                logStatus.IsLog4NetInitilized = true;
                logStatus.Log4NetExceptionMsg = string.Empty;
            }
            catch (Exception ex)
            {
                logStatus.IsLog4NetInitilized = false;
                logStatus.Log4NetExceptionMsg = ex.Message;
            }
        }
        private static void Log(LogLevel level, Object o)
        {
            if (instance == null)
            {
                InitLogger("");
            }
            instance.Log(level, o);
        }

        public static void LogError(Object o)
        {
            Log(LogLevel.Error, o);

            Exception exception = o as Exception;

            if (exception != null && exception.InnerException != null)
            {
                LogError(exception.InnerException);
            }
        }

        public static void LogError(Exception ex, String info)
        {
            Log(LogLevel.Error, Logger.GetExceptionAsString(ex, info));

            if (ex != null && ex.InnerException != null)
            {
                LogError(ex.InnerException);
            }
        }

        public static void LogFatal(Object o)
        {
            Log(LogLevel.Fatal, o);
        }

        public static void LogWarn(Object o)
        {
            Log(LogLevel.Warn, o);
        }

        public static void LogInfo(Object o)
        {
            Log(LogLevel.Info, o);
        }

        public static void LogDebug(Object o)
        {
            Log(LogLevel.Debug, o);
        }
        // Use this method only for service public method
        public static void LogStart(string className, string methodName)
        {
            string message = "start:: class: " + className + ", method: " + methodName + "()";

            LogInfo(message);
        }

        // Use this method only for service public method
        public static void LogEnd(string className, string methodName)
        {
            string message = "end:: class: " + className + ", method: " + methodName + "()";

            LogInfo(message);
        }
        public static Log4NetStatus LoggerStatus
        {
            get { return logStatus; }
        }
        public static ILogger GetLogger
        {
            get { return instance; }
        }
        public static bool IsDebugEnabled
        {
            get
            {
                return instance.IsDebugEnabled;
            }
        }
    }
    public class Log4NetStatus
    {
        public bool IsLog4NetInitilized { set; get; }

        public string Log4NetExceptionMsg { set; get; }
    }
    
}