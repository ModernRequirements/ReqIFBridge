using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Xml.Linq;
using ReqIFBridge.Models;
using ReqIFBridge.Utility;

namespace ReqIFBridge
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static Dictionary<string, OperationResult> mOperationsResults = new Dictionary<string, OperationResult>();
        public static int Trial_License_Allowed_Count = 30;
        private const string CONST_PRODUCT_NAME_FOLDER = "ReqIF4DevOps";
        private static string mGetTrialFilePath = string.Empty;
        private static string mGetTraceFilePath = string.Empty;
        public static string CONST_PROJECT_PROCESS_ID = ":ProjectProcessId";
        private static bool mLogInitialize;

        protected void Application_Start()
        {
            //InitializeApplicationLogger();
            this.InitLog();
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            InitializeLicenseTrialFile();
            LogSystemInfo();
            EnsureReqIFTemplatesFolderExists();
        }

        public static Dictionary<string, OperationResult> OperationResults
        {
            get { return mOperationsResults; }
        }

        private void InitializeLicenseTrialFile()
        {
            try
            {
                string trialFolderpath = ConfigurationManager.AppSettings.Get("License.FilesPath") + "\\" + CONST_PRODUCT_NAME_FOLDER;
                System.IO.Directory.CreateDirectory(trialFolderpath);
                string trialFilename = trialFolderpath + "\\" + ConfigurationManager.AppSettings.Get("License.TrialFileName");
                var trialFileStream = File.Exists(trialFilename) ? new FileStream(trialFilename, FileMode.Append) : new FileStream(trialFilename, FileMode.OpenOrCreate);
                mGetTrialFilePath = trialFilename;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }                 
        }

        public static string GetTrialFilePath
        {
            get { return mGetTrialFilePath; }
        }
        private void InitLog()
        {
            if(!mLogInitialize)
            {
                InitializeLogger();
                mLogInitialize = true;
               
            }
        }
        private void InitializeLogger()
        {
            try
            {
                //Type t = typeof(ErrorHandler);

                string loggerGenericPath = CommonUtility.LogConfigurationFile;

                string loggerPath = Server.MapPath(loggerGenericPath);

                DebugLogger.InitLogger(loggerPath);
            }
            catch (Exception ex)
            {
                DebugLogger.LoggerStatus.IsLog4NetInitilized = false;
                DebugLogger.LoggerStatus.Log4NetExceptionMsg = ex.Message;
            }

        }

      
        private void LogSystemInfo()
        {
            try
            {
                OperatingSystem operatingSystem = Environment.OSVersion;
                DebugLogger.LogInfo("Application Started" +"" +":: Test");
                DebugLogger.LogInfo("Application Version:: " + typeof(MvcApplication).Assembly.GetName().Version.ToString());
                DebugLogger.LogInfo("Operating System: " + operatingSystem.VersionString);
                DebugLogger.LogInfo(Environment.Is64BitOperatingSystem ? "64 bit operating system." : "32 bit operating system");
                DebugLogger.LogInfo("OS Language is: " + CultureInfo.InstalledUICulture.Name);
                DebugLogger.LogInfo(".Net Framwork Version: " + Environment.Version);
                DebugLogger.LogInfo("License.TrialFilePath: " + mGetTrialFilePath);

            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
            }
        }

        private void EnsureReqIFTemplatesFolderExists()
        {
            string reqIFTemplatesFolderPath = HttpContext.Current.Server.MapPath("~/App_Data/ReqIF_Templates");
            if (!Directory.Exists(reqIFTemplatesFolderPath))
            {
                Directory.CreateDirectory(reqIFTemplatesFolderPath);
            }
        }
    }
}
