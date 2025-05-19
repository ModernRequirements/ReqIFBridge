using Edev.LM;
using Edev.LM.Model;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Licensing;
using Microsoft.VisualStudio.Services.OAuth;
using Newtonsoft.Json;
using ReqIFBridge.Models;
using ReqIFBridge.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using OperationResult = Edev.LM.OperationResult;
using OperationStatusTypes = Edev.LM.OperationStatusTypes;

namespace ReqIFBridge.Controllers
{
    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    public class LicenseController : Controller
    {
        #region Constants

        const string CONST_WEB_SERVICE_URL = "https://modernrequirements.compliance.flexnetoperations.com:443/deviceservices";
        const int CONST_PRODUCT_ID = 14;
        const string CONST_PRODUCT_NAME = "ReqIF4DevOps";
        const string CONST_PRODUCT_CODE = "F50";
        const int CONST_PRODUCT_MAJOR_VERSION = 1;
        const int CONST_PRODUCT_MINOR_VERSION = 0;
        const int CONST_PRODUCT_BUILD_VERSION = 0;
        const string CONST_PRODUCT_ENCRYPTION_KEY = "ReqIF4DevOps4314571";
        const string CONST_PRODUCT_GUID = "{B7ED3A03-8F37-426D-BDD9-58B12BC98C75}";
        const string CONST_PUBLIC_KEY = "A59Jip0lt73Xig==";
        const string CONST_TRIAL_LICENSE_KEY = "EEJX1-70200-G3E8H-18S6Z-3J1VF-SDNF97WY";
        const string CONST_VISUAL_STUDIO_COM = "visualstudio.com";
        const string CONST_DEV_AZURE_DOT_COM = "dev.azure.com";
        const string CONST_EMPTY_LICENSE_KEY = "Enter license key.";
        const string CONST_LICENSE_ACTIVATED_MSG = "Your license has been successfully activated. Please wait for a few seconds.";
        const string CONST_PRODUCT_NOT_DEFINED_MSG = "There is some error occurred in registering license manager. (See logs for error)";
        const string SESSION_ACCOUNT_NAME = "AccountName";
        const string CONST_LICENSE_FEATURE_NOT_ALLOWED_MSG = "Your License code is not valid for this product.";
        const string SESSION_LICENSE_INFORMATION = "LicenseInformation";
        const string CONST_LICENSE_EMPTY_FEATURES = "Your License code does not belong to this product.";
        const string CONST_PRODUCT_NOT_ACTIVATED_MSG = "Your product is not activated.";
        const string CONST_PRODUCT_ACTIVATED_MSG = "Your Product is ACTIVATED.";
        string CONST_LICENSE_FILES_PATH = ConfigurationManager.AppSettings.Get("License.FilesPath");
        string CONST_LICENSE_TRIAL_FILE_PATH = MvcApplication.GetTrialFilePath;
        const string CONST_CLEAR_LICENSE_SUCCESS_MSG = "Your License has been cleared. Please wait for a few seconds.";
        const string CONST_CLEAR_LICENSE_FAILED_MSG = "Your request of license clear has failed.";
        const string CONST_LICENSE_VALIDATION_FAILED_MSG = "Failed to validate account license.";
        const string CONST_LICENSE_FILE_CREATION_FAILED_MSG = "Unable to create file in specified directory.";
        const string CONST_LICENSE_INVALID_RESPONSE_FILE = "The selected response file is invalid.";
        const string CONST_LICENSE_INVALID_RESPONSE_FILE_SIZE = "The selected file size should be greater than 0.";
        const string CONST_LICENSE_INVALID_RESPONSE_FILE_CODE = "[1,7DE,9,0[70000039,2,250293]]";
        const string CONST_LICENSE_NO_REQUEST_FILE_FOUND = "Unable to find 'Generate Request File' request from this machine.";
        const string CONST_LICENSE_EMPTY_ACCOUNT_MSG = "Account name is empty or null.";
        const string CONST_LICENSE_TRIAL_FILE_NOT_FOUND = "Unable to find trial file on specified path.";
        #endregion

        #region Public_Methods
        public JsonResult ValidateLicense(AdoParams model)
        {
            DebugLogger.LogStart("LicenseController", "ValidateLicense");
            JsonResult jsonResult = Json(new { isActivated = false, data = "", message = CONST_LICENSE_VALIDATION_FAILED_MSG }, JsonRequestBehavior.AllowGet); ;
            try
            {
                jsonResult = this.ValidateLicenseCore(model);
                //GetProjectProcessId(model);                
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                
            }
            DebugLogger.LogEnd("LicenseController", "ValidateLicense");
            return jsonResult;
        }

        public static Guid GetProjectProcessId(AdoParams model)
        {
            DebugLogger.LogStart("LicenseController", "GetProjectProcessId");
            Guid currentProjectProcessID = Guid.Empty;
            string keyWithPrefix = model.ProjectId + MvcApplication.CONST_PROJECT_PROCESS_ID;
            long lngTicks = DateTime.Now.Ticks;
            if (RedisHelper.KeyExist(keyWithPrefix))
            {
                try
                {
                    currentProjectProcessID = JsonConvert.DeserializeObject<Guid>(RedisHelper.LoadFromCache(keyWithPrefix));
                    lngTicks = DateTime.Now.Ticks - lngTicks;
                    DebugLogger.LogInfo(CommonUtility.TIME_TAKEN_TO_GET_PROCESS_ID_MSG+" " + new DateTime(lngTicks).Minute + "minute " + new DateTime(lngTicks).Second + "second " + new DateTime(lngTicks).Millisecond + "millisecond.");
                    DebugLogger.LogEnd("LicenseController", "GetProjectProcessId");

                    return currentProjectProcessID;
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(ex);
                }
            }
            DebugLogger.LogInfo("ServerUri:" + model.ServerUri);
            try
            {
                VssCredentials vssCredentials =
    new VssCredentials(new VssOAuthAccessTokenCredential(model.AccessToken));
                WorkItemTrackingProcessHttpClient workItemTrackingProcessHttpClient =
                new WorkItemTrackingProcessHttpClient(new Uri(model.ServerUri), vssCredentials);

                Guid projectGuid = new Guid(model.ProjectId);
                var processList = workItemTrackingProcessHttpClient.GetListOfProcessesAsync(Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models.GetProcessExpandLevel.Projects).Result;
                if (processList != null)
                {
                    foreach (var process in processList)
                    {
                        if (process.Projects != null && process.Projects.Count() > 0)
                        {
                            var guid = process.Projects.FirstOrDefault(x => x.Id == projectGuid);
                            if (guid != null)
                            {
                                currentProjectProcessID = process.TypeId;
                                RedisHelper.SaveInCache(keyWithPrefix, JsonConvert.SerializeObject(currentProjectProcessID), null);

                                break;
                            }
                        }
                        else
                        {
                            DebugLogger.LogError(process.TypeId +": "+CommonUtility.UNABLE_TO_GET_PROJECT_LIST_MSG);
                            
                        }                      
                    }
                }

                lngTicks = DateTime.Now.Ticks - lngTicks;
                DebugLogger.LogInfo(CommonUtility.TIME_TAKEN_TO_GET_PROCESS_ID_MSG + " " + new DateTime(lngTicks).Minute + "minute " + new DateTime(lngTicks).Second + "second " + new DateTime(lngTicks).Millisecond + "millisecond.");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                            
            }

            DebugLogger.LogEnd("LicenseController", "GetProjectProcessId");
            return currentProjectProcessID;
        }
        public JsonResult ActivateProductLicense(LicenseType licenseType, string licKey, string licenseServerURL)
        {
            DebugLogger.LogStart("LicenseController", "ActivateProductLicense");
            JsonResult jsonResult = Json(new { });
            try
            {
                if (string.IsNullOrEmpty(licKey) || licenseType == LicenseType.None)
                {
                    jsonResult = Json(new { validated = false, reason = CONST_EMPTY_LICENSE_KEY, statuscode = "" });
                    DebugLogger.LogError(CONST_EMPTY_LICENSE_KEY);
                    DebugLogger.LogEnd("LicenseController", "ActivateProductLicense");
                    return jsonResult;
                }
                string accountName = this.AccountName.ToLower();
                LicenseInfo licenseInfo = this.ActivateProductLicenseCore(accountName, licenseType, licKey, licenseServerURL);

                if (licenseInfo == null || !licenseInfo.ProductDefined)
                {
                    jsonResult = Json(new { validated = false, reason = CONST_PRODUCT_NOT_DEFINED_MSG, statuscode = "" });

                }
                else
                {
                    if (!licenseInfo.LicenseValidated)
                    {
                        jsonResult = Json(new { validated = false, reason = licenseInfo.LicenseActivationFailedMessage, statuscode = "" });
                    }
                    else if (licenseInfo.AllowedFeatures == null || !licenseInfo.AllowedFeatures.Contains(CONST_PRODUCT_CODE))
                    {
                        jsonResult = Json(new { validated = false, reason = CONST_LICENSE_FEATURE_NOT_ALLOWED_MSG, statuscode = "" });
                    }
                    else
                    {
                        LicenseInformation = licenseInfo;
                        jsonResult = Json(new { validated = true, reason = CONST_LICENSE_ACTIVATED_MSG, statuscode = "" });
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                
            }
            DebugLogger.LogEnd("LicenseController", "ActivateProductLicense");
            return jsonResult;
        }

        public JsonResult ClearLicenseInfo()
        {
            DebugLogger.LogStart("LicenseController", "ClearLicenseInfo");
            JsonResult jsonResult = Json(new { isRemoved = false, message = CONST_CLEAR_LICENSE_FAILED_MSG }, JsonRequestBehavior.AllowGet);
            try
            {
                if (LicenseInformation != null)
                {
                    jsonResult = this.ClearLicenseInfoCore(this.AccountName.ToLower());
                   
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            
            DebugLogger.LogEnd("LicenseController", "ClearLicenseInfo");
            return jsonResult;

        }

        public FileResult GenerateOfflineRequestFile(string licKey)
        {
            DebugLogger.LogStart("LicenseController", "GenerateOfflineRequestFile");

            if (string.IsNullOrEmpty(licKey))
            {
                DebugLogger.LogError(CONST_EMPTY_LICENSE_KEY);
                DebugLogger.LogEnd("LicenseController", "GenerateOfflineRequestFile");
                return null;
            }

            try
            {
                byte[] fileBytes;
                DownloadedFile downloadedfile = new DownloadedFile();
                downloadedfile = this.GenerateOfflineRequestFileCore(licKey);
                using (var streamReader = new System.IO.MemoryStream())
                {
                    downloadedfile.FileData.CopyTo(streamReader);
                    fileBytes = streamReader.ToArray();
                }
                DebugLogger.LogEnd("LicenseController", "GenerateOfflineRequestFileLicense");
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, downloadedfile.FileName);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }           

            throw new Exception(CONST_LICENSE_FILE_CREATION_FAILED_MSG);
        }

        public JsonResult LicenseStatus()
        {
            DebugLogger.LogStart("LicenseController", "LicenseStatus");
            string userLicenseType = "";
            string userLicenseStatus = "";
            bool userProductActivated = false;
            bool userIsTrialLicense = false;
            string userLicenseInfo = CONST_PRODUCT_NOT_ACTIVATED_MSG;

            if (LicenseInformation != null)
            {
                LicenseInfo licenseInfo = LicenseInformation;
                string licenseStatus = string.Empty;
                if (licenseInfo.ProductActivated && licenseInfo.LicenseType != LicenseType.TrialLicense)
                {
                    if (licenseInfo.AllowedFeatures == null || licenseInfo.AllowedFeatures.Count == 0)
                    {
                        userProductActivated = false;
                        userLicenseInfo = CONST_LICENSE_EMPTY_FEATURES;
                    }
                    else
                    {
                        if (licenseInfo.DaysLeft == -1 && licenseInfo.ProductActivated && licenseInfo.LicenseType != LicenseType.None)
                        {
                            licenseStatus = "Permanent";
                        }
                        else if (licenseInfo.DaysLeft == -1 && !licenseInfo.ProductActivated && licenseInfo.LicenseType != LicenseType.None)
                        {
                            licenseStatus = "Expired";
                        }

                        else
                        {
                            if (licenseInfo.LicenseType == LicenseType.None)
                            {
                                licenseStatus = "None";
                            }
                            else
                            {
                                licenseStatus = licenseInfo.DaysLeft.ToString();
                            }
                        }

                        string textForType = licenseInfo.LicenseType.ToString();
                        userProductActivated = true;
                        userLicenseInfo = CONST_PRODUCT_ACTIVATED_MSG;
                        userIsTrialLicense = false;
                        userLicenseType = textForType.Replace("License", "");
                        userLicenseStatus = licenseStatus;
                    }


                }
                else if (licenseInfo.ProductActivated && licenseInfo.LicenseType == LicenseType.TrialLicense)
                {
                    string textForType = licenseInfo.LicenseType.ToString();
                    userProductActivated = true;
                    userLicenseInfo = CONST_PRODUCT_ACTIVATED_MSG;
                    userIsTrialLicense = true;
                    userLicenseType = textForType.Replace("License", "");
                }
                else
                {
                    userLicenseInfo = CONST_PRODUCT_NOT_ACTIVATED_MSG;
                }

            }
            JsonResult jsonResult = Json(new { LicenseType = userLicenseType, LicenseStatus = userLicenseStatus, ProductActivated = userProductActivated, LicenseInfo = userLicenseInfo, IsTrialLicense = userIsTrialLicense });
            DebugLogger.LogEnd("LicenseController", "LicenseStatus");
            return jsonResult;
        }

        [ChildActionOnly]
        public ActionResult LicenseActivationDialog(string callingViewId)
        {
            DebugLogger.LogStart("LicenseController", "PartialView: LicenseActivationDialog");
            ViewBag.CallingViewId = callingViewId;
            ViewBag.AssemblyVersion = CommonUtility.GetAssemblyVersion();
            DebugLogger.LogEnd("LicenseController", "PartialView: LicenseActivationDialog");
            return PartialView();
        }
        public JsonResult ActivateTrial()
        {
            DebugLogger.LogStart("LicenseController", "Activatetrial");
            JsonResult jsonResult = Json(new { validated = false, reason = CONST_PRODUCT_NOT_ACTIVATED_MSG, statuscode = "" });
            try
            {
                string accountName = this.AccountName.ToLower();
                bool activateTrial = this.ActivateTrialCore(accountName);

                if (activateTrial)
                {
                    LicenseInfo licenseInfo = this.ValidateTrial(accountName);

                    if (licenseInfo != null && licenseInfo.ProductDefined && licenseInfo.LicenseType == LicenseType.TrialLicense)
                    {

                        LicenseInformation = licenseInfo;
                        jsonResult = Json(new { validated = true, reason = CONST_LICENSE_ACTIVATED_MSG, statuscode = "" });

                    }
                    else
                    {
                        jsonResult = Json(new { validated = false, reason = CONST_PRODUCT_NOT_DEFINED_MSG, statuscode = "" });

                    }
                }

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("LicenseController", "Activatetrial");
            return jsonResult;
        }

        public ActionResult UploadResponseFile(IEnumerable<HttpPostedFileBase> file)
        {
            DebugLogger.LogStart("LicenseController", "UploadResponseFile");
            LicenseInfo licenseInfo = new LicenseInfo();
            JsonResult jsonResult = Json(new { validated = false, reason = CONST_PRODUCT_NOT_ACTIVATED_MSG, statuscode = "" });
            
            if (file == null || file.Count() == 0 || file.FirstOrDefault() == null)
            {
                DebugLogger.LogError(CONST_LICENSE_INVALID_RESPONSE_FILE);
                jsonResult = Json(new { validated = false, reason = CONST_LICENSE_INVALID_RESPONSE_FILE, statuscode = "" });
            }
            else
            {
                HttpPostedFileBase fileBase = file.FirstOrDefault();
                switch (fileBase.ContentLength)
                {
                    case 0:
                        DebugLogger.LogError(CONST_LICENSE_INVALID_RESPONSE_FILE_SIZE);
                        jsonResult = Json(new { validated = false, reason = CONST_LICENSE_INVALID_RESPONSE_FILE_SIZE, statuscode = "" });

                        break;
                    default:
                        {
                            try
                            {
                                licenseInfo = this.UploadResponseFileCore(fileBase);
                                if (licenseInfo == null || !licenseInfo.ProductDefined)
                                {
                                    string errorMessage = licenseInfo.LicenseActivationFailedMessage;

                                    if (!errorMessage.Equals(CONST_LICENSE_NO_REQUEST_FILE_FOUND))
                                    {
                                        if (licenseInfo.LicenseActivationFailedMessage != string.Empty && licenseInfo.LicenseActivationFailedMessage.Contains(CONST_LICENSE_INVALID_RESPONSE_FILE_CODE))
                                        {
                                            errorMessage = CONST_LICENSE_INVALID_RESPONSE_FILE;
                                        }
                                        else if (licenseInfo.AllowedFeatures == null || !licenseInfo.AllowedFeatures.Contains(CONST_PRODUCT_CODE))
                                        {
                                            errorMessage = CONST_LICENSE_FEATURE_NOT_ALLOWED_MSG;
                                        }
                                    }

                                    jsonResult = Json(new { validated = false, reason = errorMessage, statuscode = "" });

                                }
                                else
                                {
                                    if (!licenseInfo.AllowedFeatures.Contains(CONST_PRODUCT_CODE))
                                    {
                                        jsonResult = Json(new { validated = false, reason = CONST_LICENSE_FEATURE_NOT_ALLOWED_MSG, statuscode = "" });

                                    }
                                    else
                                    {
                                        LicenseInformation = licenseInfo;
                                        jsonResult = Json(new { validated = true, reason = CONST_LICENSE_ACTIVATED_MSG, statuscode = "" });

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError(ex);
                            }

                            break;
                        }
                }
            }

            DebugLogger.LogEnd("LicenseController", "UploadResponseFile");
            return jsonResult;
        }

  
        #endregion

        #region Public_Properties

        public string AccountName
        {
            get
            {
                var item = Session[SESSION_ACCOUNT_NAME];
                return item == null ? string.Empty : item.ToString();
            }

            set { Session[SESSION_ACCOUNT_NAME] = value != null ? value.Trim() : value; }
        }

        public static LicenseInfo LicenseInformation
        {
            get
            {
                Object item = System.Web.HttpContext.Current.Session[SESSION_LICENSE_INFORMATION];
                return item as LicenseInfo;
            }
            set { System.Web.HttpContext.Current.Session[SESSION_LICENSE_INFORMATION] = value; }
        }

        #endregion

        #region Private_Methods
       
        private JsonResult ValidateLicenseCore(AdoParams model)
        {
            DebugLogger.LogStart("LicenseController", "ValidateLicenseCore");

            LicenseInfo licenseInfo = new LicenseInfo();
            
            JsonResult jsonResult = null;

            if (LicenseInformation != null && LicenseInformation.LicenseType != LicenseType.None && LicenseInformation.AllowedFeatures != null && LicenseInformation.AllowedFeatures.Contains(CONST_PRODUCT_CODE))
            {
                jsonResult = Json(new { isActivated = true, data = LicenseInformation, message = "" }, JsonRequestBehavior.AllowGet);
                DebugLogger.LogInfo(CommonUtility.LICENSE_INFO_FOUND_MSG);
                DebugLogger.LogEnd("LicenseController", "ValidateLicenseCore");

                return jsonResult;
            }

            if (model == null || string.IsNullOrEmpty(model.AccountName))
            {
                jsonResult = Json(new { isActivated = false, data = licenseInfo, message = CONST_LICENSE_EMPTY_ACCOUNT_MSG }, JsonRequestBehavior.AllowGet);
                DebugLogger.LogError(CONST_LICENSE_EMPTY_ACCOUNT_MSG);
                DebugLogger.LogEnd("LicenseController", "ValidateLicenseCore");
                return jsonResult;
            }
            try
            {
                string uniqueName = model.AccountName.ToLower();
                if (!IsAzureDevOpsConnected(model.ServerUri))
                {
                    Uri myUri = new Uri(model.ServerUri);
                    uniqueName = myUri.Host;
                }
                if (!string.IsNullOrEmpty(uniqueName))
                {
                    this.AccountName = uniqueName;
                }

                // checking for trial license

                licenseInfo = this.ValidateTrial(uniqueName);

                if (licenseInfo != null && licenseInfo.ProductDefined && licenseInfo.LicenseType == LicenseType.TrialLicense)
                {

                    LicenseInformation = licenseInfo;
                    jsonResult = Json(new { isActivated = true, data = licenseInfo, message = "" }, JsonRequestBehavior.AllowGet);

                    DebugLogger.LogEnd("LicenseController", "ValidateLicenseCore");
                    return jsonResult;
                }


                // checking for flexera license
                ILicense license = LicenseInvoker.License;
                OperationResult defineLicenseResult = DefineLicense(license, uniqueName, "");

                if (defineLicenseResult.OperationStatus == OperationStatusTypes.Success)
                {

                    OperationResult getLicenseStatus = license.GetLicenseStatus();

                    if (getLicenseStatus.OperationStatus == OperationStatusTypes.Success)
                    {

                        //if online key expired
                        if (license.DaysLeftInExpiry == -2)
                        {
                            licenseInfo.ProductActivated = false;
                            licenseInfo.DaysLeft = -1;
                            licenseInfo.LicenseType = (LicenseType)license.LicenseType;
                            licenseInfo.ProductDefined = true;
                            //expire trial also.
                            licenseInfo.IsTrialExpired = true;
                        }
                        else
                        {
                            if (license.IsTrialLicenseExpired || license.LicenseType == LicenseTypes.TrialLicense)
                            {
                                licenseInfo.ProductDefined = true; //bug fixing
                                licenseInfo.ProductActivated = false;
                                licenseInfo.DaysLeft = -1;
                                licenseInfo.LicenseType = LicenseType.None;
                                licenseInfo.IsTrialExpired = true;
                            }
                            else
                            {
                                OperationResult result = license.GetEnabledFeatures();
                                string[] featuresOnLM = null;
                                if (result != null && result.OperationStatus == OperationStatusTypes.Success)
                                {
                                    featuresOnLM = result.Tag as string[];
                                    licenseInfo.AllowedFeatures = featuresOnLM.ToList();
                                }

                                licenseInfo.ProductActivated = true;
                                licenseInfo.DaysLeft = license.DaysLeftInExpiry;
                                licenseInfo.LicenseType = (LicenseType)license.LicenseType;
                                licenseInfo.ProductDefined = true;                              

                            }                           
                        }
                       
                    }

                    else
                    {
                        licenseInfo.ProductDefined = true; //bug fixing
                        if (license.IsTrialLicenseExpired)
                        {
                            licenseInfo.ProductActivated = false;
                            licenseInfo.DaysLeft = -1;
                            licenseInfo.LicenseType = LicenseType.None;
                            licenseInfo.IsTrialExpired = true;
                        }
                    }

                    LicenseInformation = licenseInfo;

                }
                else
                {
                    licenseInfo.ProductActivated = false;
                    licenseInfo.DaysLeft = -1;
                    licenseInfo.LicenseType = LicenseType.None;
                    licenseInfo.ProductDefined = false;
                    licenseInfo.LicenseActivationFailedMessage = defineLicenseResult.Message;

                }

                if (licenseInfo.ProductDefined && !licenseInfo.ProductActivated)
                {
                    jsonResult = Json(new { isActivated = false, data = licenseInfo, message = CONST_PRODUCT_NOT_ACTIVATED_MSG }, JsonRequestBehavior.AllowGet);
                }
                else if (licenseInfo.AllowedFeatures == null || !licenseInfo.AllowedFeatures.Contains(CONST_PRODUCT_CODE))
                {
                    jsonResult = Json(new { isActivated = false, data = licenseInfo, message = CONST_LICENSE_FEATURE_NOT_ALLOWED_MSG, statuscode = "" });
                }
                else if (licenseInfo.ProductDefined && licenseInfo.ProductActivated && licenseInfo.AllowedFeatures.Contains(CONST_PRODUCT_CODE))
                {
                    jsonResult = Json(new { isActivated = true, data = licenseInfo, message = "" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    jsonResult = Json(new { isActivated = false, data = licenseInfo, message = CONST_LICENSE_VALIDATION_FAILED_MSG }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                
            }
            
            DebugLogger.LogEnd("LicenseController", "ValidateLicenseCore");
            return jsonResult;
        }
              
        private LicenseInfo ActivateProductLicenseCore(string accountName, LicenseType licenseType, string licKey, string licenseServerURL)
        {
            DebugLogger.LogStart("LicenseController", "ActivateProductLicenseCore");
            ILicense license = LicenseInvoker.License;
            LicenseInfo licenseInfo = new LicenseInfo();

            try
            {
                OperationResult defineLicenseResult = this.DefineLicense(license, accountName, licenseServerURL);

                if (defineLicenseResult.OperationStatus == OperationStatusTypes.Success)
                {

                    string motherBoardSerial = GetMotherBoardSerial();
                    OperationResult validateLicenseResult = null;
                    validateLicenseResult = license.ValidateLicense(LicenseTypes.OnLineLicense, licKey, accountName, motherBoardSerial);

                    if (validateLicenseResult != null && validateLicenseResult.OperationStatus == OperationStatusTypes.Success)
                    {
                        //if online key expired
                        if (license.DaysLeftInExpiry == -2)
                        {
                            licenseInfo.ProductActivated = false;
                            licenseInfo.LicenseValidated = false;
                            licenseInfo.DaysLeft = licenseInfo.DaysLeft;
                            licenseInfo.LicenseType = (LicenseType)license.LicenseType;
                            licenseInfo.ProductDefined = true;
                            licenseInfo.LicenseActivationFailedMessage = "Your license key has expired.";
                        }
                        else
                        {
                            OperationResult result = license.GetEnabledFeatures();
                            string[] featuresOnLM = null;
                            if (result.OperationStatus == OperationStatusTypes.Success)
                            {
                                featuresOnLM = result.Tag as string[];
                            }
                            licenseInfo.ProductActivated = true;
                            licenseInfo.LicenseValidated = true;
                            licenseInfo.DaysLeft = license.DaysLeftInExpiry;
                            if (licenseType == LicenseType.OnLineLicense)
                            {
                                licenseInfo.LicenseType = LicenseType.OnLineLicense;
                            }

                            licenseInfo.ProductDefined = true;
                            licenseInfo.IsTrialExpired = license.IsTrialLicenseExpired;
                            licenseInfo.AllowedFeatures = featuresOnLM.ToList();
                        }

                    }
                    else
                    {
                        licenseInfo.ProductActivated = false;
                        licenseInfo.LicenseValidated = false;
                        licenseInfo.DaysLeft = licenseInfo.DaysLeft;
                        licenseInfo.LicenseType = (LicenseType)license.LicenseType;
                        licenseInfo.ProductDefined = true;
                        licenseInfo.IsTrialExpired = license.IsTrialLicenseExpired;
                        licenseInfo.LicenseActivationFailedMessage = validateLicenseResult.Message;
                        licenseInfo.IsAllFloatingSeatReserved = license.IsAllFloatingSeatReserved;
                    }

                    LicenseInformation = licenseInfo;

                }

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("LicenseController", "ActivateProductLicenseCore");
            return licenseInfo;
        }
         
        private JsonResult ClearLicenseInfoCore(string accountName)
        {
            DebugLogger.LogStart("LicenseController", "ClearLicenseInfoCore");
            JsonResult jsonResult = Json(new { isRemoved = false, message = CONST_CLEAR_LICENSE_FAILED_MSG }, JsonRequestBehavior.AllowGet);
            OperationResult result = new OperationResult(OperationStatusTypes.Failed);

            try
            {
                switch (LicenseInformation.LicenseType)
                {
                    case LicenseType.TrialLicense:
                        result = ClearTrailLicense(accountName);
                        break;
                    default:
                        {
                            ILicense license = LicenseInvoker.License;
                            OperationResult defineLicenseResult = this.DefineLicense(license, accountName, "");
                            if (defineLicenseResult.OperationStatus == OperationStatusTypes.Success)
                            {
                                result = license.ClearLicense(accountName);
                            }

                            break;
                        }
                }

                if (result.OperationStatus == OperationStatusTypes.Success)
                {
                    LicenseInformation = null;
                    jsonResult = Json(new { isRemoved = true, message = CONST_CLEAR_LICENSE_SUCCESS_MSG }, JsonRequestBehavior.AllowGet);
                    
                }
                
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("LicenseController", "ClearLicenseInfoCore");
            return jsonResult;
        }

        private OperationResult ClearTrailLicense(string accountName)
        {
            DebugLogger.LogStart("LicenseController", "ClearTrailLicense");
            OperationResult result = new OperationResult(OperationStatusTypes.Failed);
            List<string> lstAccountName = new List<string>();

            try
            {
                if (System.IO.File.Exists(CONST_LICENSE_TRIAL_FILE_PATH))
                {
                    var lines = System.IO.File.ReadAllLines(CONST_LICENSE_TRIAL_FILE_PATH);
                    foreach (var singleLine in lines)
                    {
                        if (!string.IsNullOrEmpty(singleLine))
                        {
                            lstAccountName.Add(singleLine);
                        }
                    }
                }

                if (lstAccountName.Contains(accountName))
                {
                    lstAccountName.Remove(accountName);
                }

                using (StreamWriter file = new StreamWriter(CONST_LICENSE_TRIAL_FILE_PATH))
                    foreach (var item in lstAccountName)
                    {
                        file.WriteLine("{0}", item);
                    }
               result = new OperationResult(OperationStatusTypes.Success);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("LicenseController", "ClearTrailLicense");
            return result;
        }
        
        private DownloadedFile GenerateOfflineRequestFileCore(string licenseKey)
        {
            DebugLogger.LogStart("LicenseController", "GenerateOfflineRequestFileCore");
            DownloadedFile downloadedfile = new DownloadedFile();
            Uri uri;
            OperationResult result = null;
            System.IO.Stream stream = null;
            string userName = this.AccountName.ToLower();
            try
            {
                string motherBoardSerial = GetMotherBoardSerial();
                long lngTicks = DateTime.Now.Ticks;
                string downloadFileName = userName + lngTicks + ".bin";
                string fileDownloadpath = System.IO.Path.GetTempPath() + downloadFileName;
                ILicense license = LicenseInvoker.License;
                ProductDefinition productDefinition = this.GetProductDefinition(userName);

                if (Uri.TryCreate(CONST_WEB_SERVICE_URL, UriKind.Absolute, out uri))
                {
                    license.ServerWebServiceUrl = uri;
                }
                // Start Fixing - 24904
                license.DefineProduct(productDefinition);
                // End Fixing - 24904
                result = license.CreateOfflineActivationKey(productDefinition, license.ServerWebServiceUrl, licenseKey, userName, motherBoardSerial, "", fileDownloadpath);

                if (result.OperationStatus == OperationStatusTypes.Success && result != null)
                {

                    stream = new MemoryStream();
                    using (Stream input = System.IO.File.OpenRead(fileDownloadpath))
                    {
                        input.CopyTo(stream);
                    }
                    stream.Position = 0;

                }


                downloadedfile.FileName = AccountName.ToLower() + ".bin";
                downloadedfile.FileData = stream;

            }
            catch (AggregateException e)
            {
                DebugLogger.LogError(e);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex); 
            }
            DebugLogger.LogEnd("LicenseController", "GenerateOfflineRequestFileCore");
            return downloadedfile;
        }             

        private bool ActivateTrialCore(string accountName)
        {
            DebugLogger.LogStart("LicenseController", "ActivateTrialCore");
            bool isTrialActivated = false;
            List<string> lstAccountName = new List<string>();
           
            try
            {
                if (System.IO.File.Exists(CONST_LICENSE_TRIAL_FILE_PATH))
                {
                    var lines = System.IO.File.ReadAllLines(CONST_LICENSE_TRIAL_FILE_PATH);
                    foreach (var singleLine in lines)
                    {
                       if (!string.IsNullOrEmpty(singleLine))
                        {
                            lstAccountName.Add(singleLine);
                        }
                    }
                }
               
                if(!lstAccountName.Contains(accountName))
                {
                    lstAccountName.Add(accountName);
                }

                using (StreamWriter file = new StreamWriter(CONST_LICENSE_TRIAL_FILE_PATH))
                    foreach (var item in lstAccountName)
                        {
                            file.WriteLine("{0}", item);
                        }
                isTrialActivated = true;
                DebugLogger.LogInfo(CONST_PRODUCT_NAME + "-"+CommonUtility.TRAIL_ACTIVATE_MSG);

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("LicenseController", "ActivateTrialCore");
            return isTrialActivated;
        }

        private LicenseInfo ValidateTrial(string accountName)
        {
            DebugLogger.LogStart("LicenseController", "validateTrial");
            LicenseInfo licenseInfo = new LicenseInfo();
                        
            try
            {
                if (System.IO.File.Exists(CONST_LICENSE_TRIAL_FILE_PATH))
                {
                    var fileAccountNames = System.IO.File.ReadAllLines(CONST_LICENSE_TRIAL_FILE_PATH);
                    foreach (var fileaccountname in fileAccountNames)
                    {
                        if (!string.IsNullOrEmpty(fileaccountname) && fileaccountname.Equals(accountName))
                        {
                            licenseInfo.ProductActivated = true;
                            licenseInfo.ProductDefined = true;
                            licenseInfo.IsTrialExpired = false;
                            licenseInfo.LicenseType = LicenseType.TrialLicense;
                            licenseInfo.LicenseValidated = true;
                            DebugLogger.LogInfo(CONST_PRODUCT_NAME + "-" + CommonUtility.TRIAL_LICENSE_FOUND_MSG);
                        }
                    }
                }
                else
                {
                    DebugLogger.LogError(CONST_LICENSE_TRIAL_FILE_NOT_FOUND);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("LicenseController", "ValidateTrial");
            return licenseInfo ;
        }

        private string GetMotherBoardSerial()
        {
            DebugLogger.LogStart("LicenseController", "GetMotherBoardSerial");
            string motherBoardSerial = string.Empty;
           
            try
            {
                ManagementScope scope = new ManagementScope("\\\\" + Environment.MachineName + "\\root\\cimv2");
                scope.Connect();

                ManagementObject wmiClass = new ManagementObject(scope, new ManagementPath("Win32_BaseBoard.Tag=\"Base Board\""), new ObjectGetOptions());


                PropertyDataCollection pdc = wmiClass.Properties;

                PropertyData propertyData = pdc["SerialNumber"];

                DebugLogger.LogInfo("Motherboard serial is: " + Convert.ToString(propertyData.Value));
                
                motherBoardSerial = Convert.ToString(propertyData.Value);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
           DebugLogger.LogEnd("LicenseController", "GetMotherBoardSerial");
           return motherBoardSerial;
        }

        private OperationResult DefineLicense(ILicense license, string userName, string licenseServerURL)
        {
            DebugLogger.LogStart("LicenseController", "DefineLicense");
            
            Uri uri;
            OperationResult result = null;

            if (string.IsNullOrEmpty(userName))
            {
                DebugLogger.LogError(CommonUtility.USERNAME_NULL_OR_EMPTY_MSG);
                result = new OperationResult(OperationStatusTypes.Aborted, CommonUtility.USERNAME_NULL_OR_EMPTY_MSG);
            
                DebugLogger.LogEnd("LicenseController", "DefineLicense");
                return result;
            }

            try
            {
                ProductDefinition productDefinition = GetProductDefinition(userName);

                if (Uri.TryCreate(CONST_WEB_SERVICE_URL, UriKind.Absolute, out uri))
                {
                    license.ServerWebServiceUrl = uri;
                }

                if (!string.IsNullOrEmpty(licenseServerURL))
                {
                    bool uriResult = Uri.TryCreate(licenseServerURL, UriKind.Absolute, out uri) &&
                                     (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
                    if (uriResult)
                    {
                        license.FloatingWebServiceUrl = uri;
                    }
                    else
                    {
                        result = new OperationResult(OperationStatusTypes.Aborted, CommonUtility.FLOATING_SERVER_URL_NOT_VALID_MSG);
                        return result;
                    }
                }
                DebugLogger.LogInfo("productDefinition.UserName: " + productDefinition.HostID);
                result = license.DefineProduct(productDefinition);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("LicenseController", "DefineLicense");
            return result;
        }
              

        private ProductDefinition GetProductDefinition(string userName)
        {
            DebugLogger.LogStart("LicenseController", "GetProductDefinition");
            ProductDefinition productDefinition = new ProductDefinition(true);
            try
            {
                productDefinition.Id = CONST_PRODUCT_ID;
                productDefinition.Name = CONST_PRODUCT_NAME;
                productDefinition.MajorVersion = CONST_PRODUCT_MAJOR_VERSION;
                productDefinition.MinorVersion = CONST_PRODUCT_MINOR_VERSION;
                productDefinition.BuildVersion = CONST_PRODUCT_BUILD_VERSION;
                productDefinition.EncryptionKey = CONST_PRODUCT_ENCRYPTION_KEY;
                productDefinition.GUID = CONST_PRODUCT_GUID;
                productDefinition.PublicKey = CONST_PUBLIC_KEY;
                productDefinition.TrialLicenseKey = CONST_TRIAL_LICENSE_KEY;

                string trailFilePathRelative = "~/bin/App_Data/IGWATrial.bin";
                string trailFilePathAbsolute = string.Empty;

                if (HttpContext != null)
                {
                    trailFilePathAbsolute = HttpContext.Server.MapPath(trailFilePathRelative);
                }                
                productDefinition.LicenseFilePath = trailFilePathAbsolute;
                productDefinition.HostID = userName;

                if (IsProxyDefined)
                {
                    DebugLogger.LogInfo("IsProxyDefined:True");
                    productDefinition.ProxyCredentials = GetProxyCredentials();
                }
                
                IRegistry redisRegistry = new RedisRegistry(CONST_PRODUCT_ID, CONST_PRODUCT_MAJOR_VERSION, CONST_PRODUCT_MINOR_VERSION, CONST_PRODUCT_BUILD_VERSION);
                redisRegistry.BackupFilePath = CONST_LICENSE_FILES_PATH + "\\" + "eLM.bin"; //constant
                productDefinition.RegistryUtility = redisRegistry;
                productDefinition.LoadBalancerTrustedStoragePath = CONST_LICENSE_FILES_PATH;
                
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("LicenseController", "GetProductDefinition");
            return productDefinition;
        }

        private bool IsAzureDevOpsConnected(string tfsServerUrl)
        {
            DebugLogger.LogStart("LicenseController", "IsAzureDevOpsConnected");
            bool IsAzureDevOpsConnected = false;
            try
            {
                
                if (!string.IsNullOrEmpty(tfsServerUrl))
                {
                    Uri tfsServerUri = new Uri(tfsServerUrl);

                    if (tfsServerUri.Scheme == Uri.UriSchemeHttps && (tfsServerUri.Host.ToLower().Contains(CONST_VISUAL_STUDIO_COM) || tfsServerUri.Host.ToLower().Contains(CONST_DEV_AZURE_DOT_COM)))
                    {
                        IsAzureDevOpsConnected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogInfo("IsAzureDevOpsConnected:" + IsAzureDevOpsConnected.ToString());
            DebugLogger.LogEnd("LicenseController", "IsAzureDevOpsConnected");
            return IsAzureDevOpsConnected;
        }

        private LicenseInfo UploadResponseFileCore(HttpPostedFileBase ResponseFile)
        {
            DebugLogger.LogStart("LicenseController", "UploadResponseFileCore");

            LicenseInfo licenseInfo = new LicenseInfo();
            string userName = this.AccountName.ToLower();
            long lngTicks = DateTime.Now.Ticks;
            string uploadedFileName = userName + lngTicks + ".bin";
            string uploadedFilePath = System.IO.Path.GetTempPath() + uploadedFileName;
            OperationResult result = null;
            ILicense license = LicenseInvoker.License;
            try
            {
                OperationResult defineLicenseResult = this.DefineLicense(license, userName, "");
                if (defineLicenseResult.OperationStatus == OperationStatusTypes.Success)
                {
                    OperationResult isUserAllowed = license.IsUserRegistered(userName);
                    if (isUserAllowed.OperationStatus != OperationStatusTypes.Success &&
                        (LicenseType)isUserAllowed.Tag != LicenseType.OfflineLicense)
                    {

                        licenseInfo.ProductActivated = false;
                        licenseInfo.LicenseActivationFailedMessage = CONST_LICENSE_NO_REQUEST_FILE_FOUND;
                        return licenseInfo;
                    }
                    byte[] fileContent = null;
                    using (BinaryReader binaryReader = new BinaryReader(ResponseFile.InputStream))
                    {
                        fileContent = binaryReader.ReadBytes(ResponseFile.ContentLength);
                    }

                    bool byteToFile = ConvertByteArrayToFile(uploadedFilePath, fileContent);
                    if (byteToFile)
                    {

                        result = license.OfflineFileSaveLocation(uploadedFilePath);

                        if (result.OperationStatus == OperationStatusTypes.Success && result != null)
                        {
                            OperationResult getLicenseStatus = license.GetLicenseStatus();

                            if (getLicenseStatus.OperationStatus == OperationStatusTypes.Success && license.LicenseType == LicenseTypes.OfflineLicense)
                            {
                                    OperationResult featureresult = license.GetEnabledFeatures();

                                    if (featureresult.OperationStatus == OperationStatusTypes.Success)
                                    {
                                        string[] featuresOnLM = featureresult.Tag as string[];

                                        if (featuresOnLM != null)
                                        {
                                            if (license.DaysLeftInExpiry == -2)
                                            {
                                                licenseInfo.ProductActivated = false;
                                                licenseInfo.DaysLeft = -1;
                                                licenseInfo.LicenseType = (LicenseType)license.LicenseType;
                                                licenseInfo.ProductDefined = true;
                                                //expire trial also.
                                                licenseInfo.IsTrialExpired = true;
                                            }
                                            else
                                            {
                                                licenseInfo.ProductActivated = true;
                                                licenseInfo.DaysLeft = license.DaysLeftInExpiry;
                                                licenseInfo.LicenseType = (LicenseType)license.LicenseType;
                                                licenseInfo.ProductDefined = true;
                                                licenseInfo.AllowedFeatures = featuresOnLM.ToList();
                                            }
                                        }
                                        else
                                        {
                                            licenseInfo.ProductActivated = false;
                                            licenseInfo.LicenseActivationFailedMessage = "No feature is found in response file.";                                        

                                        }
                                    }                               

                            }
                            else
                            {

                                licenseInfo.ProductActivated = false;
                                licenseInfo.LicenseActivationFailedMessage = getLicenseStatus.Message;

                            }
                        }

                        else
                        {
                            licenseInfo.ProductActivated = false;
                            licenseInfo.LicenseActivationFailedMessage = CONST_LICENSE_FILE_CREATION_FAILED_MSG;
                        }
                    }
                    else
                    {
                        licenseInfo.ProductActivated = false;
                        licenseInfo.LicenseActivationFailedMessage = CONST_LICENSE_FILE_CREATION_FAILED_MSG;
              
                    }

                }
            }

            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("LicenseController", "UploadResponseFileCore");
            return licenseInfo;

        }

        private bool ConvertByteArrayToFile(string _FileName, byte[] _ByteArray)
        {
            DebugLogger.LogStart("LicenseController", "ConvertByteArrayToFile");
            bool operationStatus = false;
            try
            {
                System.IO.FileStream _FileStream =
                   new System.IO.FileStream(_FileName, System.IO.FileMode.Create,
                                            System.IO.FileAccess.Write);
                _FileStream.Write(_ByteArray, 0, _ByteArray.Length);
                _FileStream.Close();
                operationStatus = true;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogInfo("ConvertByteArrayToFile:" + operationStatus.ToString());
            DebugLogger.LogEnd("LicenseController", "ConvertByteArrayToFile");
            return operationStatus;
        }

        private ProxyInformation GetProxyCredentials()
        {
            DebugLogger.LogStart("LicenseController", "GetProxyCredentials");
            ProxyInformation licenseProxyInfo = new ProxyInformation();
            try
            {
                string licenseProxyCredentials = ConfigurationManager.AppSettings["License.LicenseProxyCredentials"];
                DebugLogger.LogInfo("LicenseProxyCredentials:" + licenseProxyCredentials);

                //CommonUtility.LogInfo("LicenseProxyCredentials:" + licenseProxyCredentials);
                string[] proxyCredentialsSplitArray = licenseProxyCredentials.Split(',');
                for (int i = 0; i < proxyCredentialsSplitArray.Length; i++)
                {
                    var proxyCredentialsItem = proxyCredentialsSplitArray[i];
                    var proxyCredentialsValues = proxyCredentialsItem.Split('=');
                    string proxyRequiredField = proxyCredentialsValues[0];
                    if (proxyRequiredField != null)
                    {
                        switch (proxyRequiredField)
                        {
                            case "hostname":
                                licenseProxyInfo.HostName = proxyCredentialsValues[1];
                                break;
                            case "port":
                                licenseProxyInfo.Port = proxyCredentialsValues[1];
                                break;
                            case "username":
                                licenseProxyInfo.UserName = proxyCredentialsValues[1];
                                break;
                            case "password":
                                licenseProxyInfo.Password = proxyCredentialsValues[1];
                                break;
                            case "domain":
                                licenseProxyInfo.Domain = proxyCredentialsValues[1];
                                break;
                            default:
                                DebugLogger.LogError(CommonUtility.INVALID_PROXY_CONFIGURATION_MSG);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("LicenseController", "GetProxyCredentials");
            return licenseProxyInfo;
        }

        #endregion

        #region Private_Properties
        private bool IsProxyDefined
        {
            get
            {
                bool isProxyDefined = false;

                string licenseProxyCredentials = System.Configuration.ConfigurationManager.AppSettings["License.LicenseProxyCredentials"];

                if (!string.IsNullOrEmpty(licenseProxyCredentials))
                {
                    isProxyDefined = true;
                }

                ////DebugLogger.LogDebug("get:IsProxyDefined:get" + isProxyDefined.ToString());
                return isProxyDefined;
            }
        }
        #endregion
    }
}