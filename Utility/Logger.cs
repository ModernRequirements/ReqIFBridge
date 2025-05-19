using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace ReqIFBridge.Utility
{
    public class Logger : ILogger
    {
        private ILog mLog;
        public Logger(string uniqueName,string loggerConfigPath)
        {
            mLog = LogManager.GetLogger(uniqueName);
            mLog.Logger.Repository.ConfigurationChanged += new LoggerRepositoryConfigurationChangedEventHandler(OnRepository_ConfigurationChanged);
            XmlConfigurator.ConfigureAndWatch(new FileInfo(loggerConfigPath));
        }

        private void OnRepository_ConfigurationChanged(object sender, EventArgs e)
        {
            SetLogFileName();
        }
        private void SetLogFileName()
        {
            ILoggerRepository repository = mLog.Logger.Repository;



            //get all of the appenders for the repository
            IAppender[] appenders = repository.GetAppenders();

            foreach (IAppender appender in (from iAppender in appenders
                                            where iAppender is RollingFileAppender
                                            select iAppender))
            {
                RollingFileAppender fileAppender = appender as RollingFileAppender;

                string fileName = Path.GetFileNameWithoutExtension(fileAppender.File);
                string logFileName = string.Format("{0}-{1}", Path.GetRandomFileName(), fileName);
                string direc = Path.GetDirectoryName(fileAppender.File);
                string ext = Path.GetExtension(fileAppender.File);
                String name;
               
#if(DEBUG)
                name = logFileName + "-DEBUG" + ext;
#else
                name = fileName + "-" + Process.GetCurrentProcess().Id + "-" + ext;
#endif



                string path = Path.Combine(direc, name);
                fileAppender.File = path;

                string elog = Path.GetTempPath() + "\\ReqIfLogInfo.txt";
                using (FileStream fileStream = new FileStream(elog, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    StreamWriter streamWriter = new StreamWriter(fileStream);
                    streamWriter.Write("filecreate : " + path);
                    streamWriter.Close();
                    fileStream.Close();
                }
                //make sure to call fileAppender.ActivateOptions() to notify the logging
                //sub system that the configuration for this appender has changed. 
                fileAppender.ActivateOptions();

            }
        }


        public bool IsDebugEnabled
        {
            get {
                return mLog.IsDebugEnabled;
}
        }

        public void Log(object o)
        {
            this.LogInfo(o);
        }

        public void Log(LogLevel level, object o)
        {
            Exception e = o as Exception;
            string message = string.Empty;
            if (e != null)
            {
                message = GetExceptionAsString(e, null);
            }
            switch (level)
            {
                case LogLevel.Fatal:
                    if (e == null)
                    {
                        mLog.Fatal(o);
                    }
                    else
                    {
                        mLog.Fatal(message);
                    }
                    break;
                case LogLevel.Error:

                    if (e == null)
                    {
                        mLog.Error(o);
                    }
                    else
                    {
                        mLog.Error(message);
                    }
                    break;
                case LogLevel.Warn:
                    if (e == null)
                    {
                        mLog.Warn(o);
                    }
                    else
                    {
                        mLog.Warn(message);
                    }
                    break;
                case LogLevel.Info:
                    mLog.Info(o);
                    break;
                case LogLevel.Debug:
                    mLog.Debug(o);
                    break;
            }
        }

        internal static string GetExceptionAsString(Exception ex, string info)
        {
            StringBuilder detail = new StringBuilder();
            Exception inner = ex;

            if (!String.IsNullOrEmpty(info))
            {
                detail.AppendFormat(CommonUtility.CONST_LOCALE_SETTINGS, "{0}{1}", info, Environment.NewLine);
            }

            detail.AppendFormat(CommonUtility.CONST_LOCALE_SETTINGS, "{0}{1}", inner.GetType().Name, Environment.NewLine);



            do
            {
                detail.AppendFormat(CommonUtility.CONST_LOCALE_SETTINGS, "{1}{0}{2}{0}{0}", Environment.NewLine, inner.Message, inner.StackTrace);
                inner = inner.InnerException;
            } while (inner != null);

            string typeName, methodName;

            string assemblyName = typeName = methodName = "Unknown";

            if (inner != null && inner.TargetSite != null)
            {
                assemblyName = inner.TargetSite.Module.Assembly.GetName().Name;
                methodName = inner.TargetSite.Name;
                typeName = (inner.TargetSite.DeclaringType == null) ? string.Empty : inner.TargetSite.DeclaringType.Name;
                detail.AppendFormat(CommonUtility.CONST_LOCALE_SETTINGS, "{0}{1}{0}{2}{0}{3}", Environment.NewLine,
                                    assemblyName, methodName, typeName);
            }
            return detail.ToString();
        }

        public void LogDebug(object o)
        {
            this.Log(LogLevel.Debug, o);
        }

        public void LogError(object o)
        {
            this.Log(LogLevel.Error, o);
        }

        public void LogFatal(object o)
        {
            this.Log(LogLevel.Fatal, o);
        }

        public void LogInfo(object o)
        {
            this.Log(LogLevel.Info, o);
        }

        public void LogWarn(object o)
        {
            this.Log(LogLevel.Warn, o);
        }
    }
}