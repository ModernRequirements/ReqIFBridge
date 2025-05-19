using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Newtonsoft.Json;
using ReqIFBridge.ReqIF;
using ReqIFBridge.Models;
using ReqIFBridge.ReqIF.ReqIFMapper;
using ReqIFBridge.Utility;
using ReqIFSharp;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi;
using System.IO.Compression;
using System.Configuration;
using System.Web.UI;


namespace ReqIFBridge.Controllers
{
    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    [NoCache]
    public class DefaultController : Controller
    {
        private const string CONT_GET_LICENSE_STATUS_VIEW = "GetWebContextDetail";
        private const string CONST_REQIF_FILE_VALIDATION_FAILED = "Unable to validate specified ReqIF file.";
        private const string CONST_REQIF_MAPPPING_FILE_NOTFOUND = "Mapping file is not found.";
        private const string CONST_REQIF_MAPPPING_FILE_NOTSAVED = " Unable to save/update mapping template!.";
        private const string CONST_WORKITEM_TYPES_FIELDS_DATA = ":WorkItemTypesFieldsData";
        private const string CONST_LINKTYPES_DATA_COLLECTION = ":LinkTypesDataCollection";
        private const string CONST_WORKITEM_TYPES = ":WorkItemTypes";
        private const string CONST_LINKS_TYPES = ":LinkTypes";
        private List<string> mandatoryFieldExceptionList = new List<string> { "System.State", "System.AreaId", "System.IterationId"};
        private const string SESSION_CURRENT_WORKITEMS = "CurrentWorkItems";
        private const string CONST_REQIF_FILE_MAPPING_KEY = ":ConfigureMappingFile";
        private const string CONST_TRIM_SUFFIX = "...";
        private const int CONST_POPUP_MESSAGE_ALLOWED_LENGTH=70;
        private const string CONST_WORKITEM_TITLE_FIELD = "System.Title";
        private const string CONST_WORKITEM_STATE_FIELD = "System.State";
        private const string CONST_WORKITEM_ASSIGNED_TO_FIELD = "System.AssignedTo";
        private const string CONST_WORKITEM_TYPE_FIELD = "System.WorkItemType";
        public ActionResult Index()
        {
            DebugLogger.LogStart("DefaultController", "Index");
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            ViewBag.BuildVersion = version;
            DebugLogger.LogEnd("DefaultController", "Index");
            return View();
        }

        public ActionResult ConfirmationModal(string containerId)
        {
            DebugLogger.LogStart("DefaultController", "ConfirmationModal");
            ViewBag.ControlId = containerId;
            DebugLogger.LogEnd("DefaultController", "ConfirmationModal");
            return PartialView();
        }

        public ActionResult ExportconfirmationModal(string containerId)
        {
            DebugLogger.LogStart("DefaultController", "ExportconfirmationModal");
            ViewBag.ControlId = containerId;
            DebugLogger.LogEnd("DefaultController", "ExportconfirmationModal");
            return PartialView();
        }

        public ActionResult ExportReqIFAction()
        {
            DebugLogger.LogInfo("View:ExportReqIfAction Called");
            return View();
        }

        public ActionResult ExportReqIFDialog()
        {
            DebugLogger.LogInfo("View:ExportReqIFDialog Called");
            return View();
        }

        public ActionResult ImportReqIFAction()
        {
            DebugLogger.LogInfo("View:ImportReqIFAction Called");
            return View();
        }

        public ActionResult ImportReqIFDialog(bool isClose = false, string projectGuid=null)
        {

            DebugLogger.LogStart("DefaultController", "ImportReqIFDialog");
            string redisKey = projectGuid + CONST_REQIF_FILE_MAPPING_KEY;
            if (isClose)
            {
                string ReqIfFilePath = string.Empty;
                if (RedisHelper.KeyExist(redisKey))
                {
                    try
                    {
                        ReqIfFilePath = Convert.ToString(RedisHelper.LoadFromCache(redisKey));
                        ViewBag.FilePath = true;

                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(ex);
                    }
                }
            }
            else
            {
                if(RedisHelper.KeyExist(redisKey))
                    RedisHelper.RemoveFromCache(redisKey);
            }
            DebugLogger.LogEnd("DefaultController", "ImportReqIFDialog");
            return View();
        }


        [HttpPost]
        public ActionResult PerformImport(AdoParams model, string reqIFPath)
        {
            DebugLogger.LogStart("DefaultController", "PerformImport");
            DebugLogger.LogInfo("Import process started by user " + model.UserName);

            OperationResult result = null;

            if (string.IsNullOrEmpty(reqIFPath))
            {
                result = new OperationResult(OperationStatusTypes.Failed, null, new Exception("reqIf path is empty"));
                DebugLogger.LogError(new Exception(CommonUtility.REQIF_PATH_IS_EMPTY));
            }
            try
            {
                if (LicenseController.LicenseInformation == null)
                {
                    DebugLogger.LogInfo("Session has been expired.");
                    LicenseController licenseController = new LicenseController();
                    JsonResult jsonResult = licenseController.ValidateLicense(model);
                }

                LicenseType licenseType = LicenseController.LicenseInformation.LicenseType;

                if (licenseType != LicenseType.TrialLicense
                    && (LicenseController.LicenseInformation.AllowedFeatures == null || LicenseController.LicenseInformation.AllowedFeatures.Count == 0))
                {
                    result = new OperationResult(OperationStatusTypes.Failed, null, "");
                    return new JsonResult()
                    {
                        Data = JsonConvert.SerializeObject(result)
                    };
                }

                string redisKey = model.ProjectId + CONST_REQIF_FILE_MAPPING_KEY;
                RedisHelper.SaveInCache(redisKey, reqIFPath);

                VssCredentials vssCredentials =
                    new VssCredentials(new VssOAuthAccessTokenCredential(model.AccessToken));
                WorkItemTrackingHttpClient workItemTrackingHttpClient =
                    new WorkItemTrackingHttpClient(new Uri(model.ServerUri), vssCredentials);

                ReqIFDeserializer deserializer = new ReqIFDeserializer();
                ReqIFSharp.ReqIF reqif = deserializer.Deserialize(reqIFPath).FirstOrDefault();



                string reqIFMappingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Templates/{model.ProjectId}.xml");
                ReqIFMappingTemplate mappingTemplate = CommonUtility.LoadMapping(reqIFMappingPath);

                string reqIFBindingPath =
                    HttpContext.Server.MapPath($"~/App_Data/ReqIF_Bindings/{model.ProjectId}-binding.json");
                Dictionary<string, int> binding = CommonUtility.GetBinding(reqIFBindingPath);

                //todo if the folder is not exist , create it first
                Guid resultKey = Guid.NewGuid();
                MvcApplication.OperationResults[resultKey.ToString()] = null;
                HostingEnvironment.QueueBackgroundWorkItem(token => PerformImportCore(model, workItemTrackingHttpClient, reqif, mappingTemplate, binding, reqIFBindingPath, resultKey, licenseType));
                DebugLogger.LogInfo("Operation Guid: " + resultKey.ToString());

                result = new OperationResult(OperationStatusTypes.Success, resultKey.ToString(), null);

            }
            catch (Exception e)
            {
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
                DebugLogger.LogError(e);
            }
            DebugLogger.LogEnd("DefaultController", "PerformImport");
            return new JsonResult()
            {
                Data = JsonConvert.SerializeObject(result)
            };
        }


        //[Obsolete("This implementation is now obselete, use with string parameter", true)]
        //[HttpPost]
        //public ActionResult PerformImport(AdoParams model, HttpPostedFileBase reqIFFile, bool fromCache)
        //{
        //    CommonUtility.LogInfo("DefaultController", "PerformImport", "Start");
        //    CommonUtility.LogInfo("Import process started by user " + model.UserName);
        //    OperationResult result = null;
        //    string redisKey = model.ProjectId + CONST_REQIF_FILE_MAPPING_KEY;

        //    try
        //    {
        //        if (LicenseController.LicenseInformation == null)
        //        {
        //            CommonUtility.LogInfo("Session has been expired.");
        //            LicenseController licenseController = new LicenseController();
        //            JsonResult jsonResult = licenseController.ValidateLicense(model);
        //        }

        //        LicenseType licenseType = LicenseController.LicenseInformation.LicenseType;

        //        if (licenseType != LicenseType.TrialLicense
        //            && (LicenseController.LicenseInformation.AllowedFeatures == null || LicenseController.LicenseInformation.AllowedFeatures.Count == 0))
        //        {
        //            result = new OperationResult(OperationStatusTypes.Failed, null, "");
        //            return new JsonResult()
        //            {
        //                Data = JsonConvert.SerializeObject(result)
        //            };
        //        }
        //        string reqIFPath = string.Empty;
        //        if (RedisHelper.KeyExist(redisKey) && fromCache)
        //        {
        //            reqIFPath = Convert.ToString(RedisHelper.LoadFromCache(redisKey));
        //        }
        //        else
        //        {
        //            reqIFPath = Path.Combine(Path.GetTempPath(), $"{DateTime.Now.Ticks}.reqifz");

        //            if (reqIFFile.ContentLength > 0)
        //            {
        //                reqIFFile.SaveAs(reqIFPath);


        //                RedisHelper.SaveInCache(redisKey, reqIFPath, null);
        //            }
        //            else
        //            {
        //                CommonUtility.LogError(CONST_REQIF_FILE_VALIDATION_FAILED);
        //                return new JsonResult()
        //                {

        //                    Data = JsonConvert.SerializeObject(new OperationResult(OperationStatusTypes.Failed,
        //                    CONST_REQIF_FILE_VALIDATION_FAILED))
        //                };
        //            }
        //        }

        //        VssCredentials vssCredentials =
        //            new VssCredentials(new VssOAuthAccessTokenCredential(model.AccessToken));
        //        WorkItemTrackingHttpClient workItemTrackingHttpClient =
        //            new WorkItemTrackingHttpClient(new Uri(model.ServerUri), vssCredentials);

        //        ReqIFDeserializer deserializer = new ReqIFDeserializer();
        //        ReqIFSharp.ReqIF reqif = deserializer.Deserialize(reqIFPath);

        //        string reqIFMappingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Templates/{model.ProjectId}.xml");
        //        ReqIFMappingTemplate mappingTemplate = LoadMapping(reqIFMappingPath);

        //        string reqIFBindingPath =
        //            HttpContext.Server.MapPath($"~/App_Data/ReqIF_Bindings/{model.ProjectId}-binding.json");
        //        Dictionary<string, int> binding = GetBinding(reqIFBindingPath);

        //        Guid resultKey = Guid.NewGuid();
        //        MvcApplication.OperationResults[resultKey.ToString()] = null;
        //        HostingEnvironment.QueueBackgroundWorkItem(token => PerformImportCore(model, workItemTrackingHttpClient, reqif, mappingTemplate, binding, reqIFBindingPath, resultKey, licenseType));


        //        result = new OperationResult(OperationStatusTypes.Success, resultKey.ToString(), null);
        //    }
        //    catch (Exception e)
        //    {
        //        result = new OperationResult(OperationStatusTypes.Failed, null, e);
        //        CommonUtility.LogError(e);
        //    }
        //    CommonUtility.LogInfo("DefaultController", "PerformImport", "End");
        //    return new JsonResult()
        //    {
        //        Data = JsonConvert.SerializeObject(result)
        //    };
        //}

        //[HttpPost]
        //public JsonResult GetReqIFServerPath( HttpPostedFileBase reqIFFile)
        //{
        //    CommonUtility.LogInfo("DefaultController", "GetReqIFServerPath", "Start");
        //    JsonResult jsonResult = null;

        //    try
        //    {
        //        System.IO.FileInfo fileInfo = new FileInfo(reqIFFile.FileName);
        //        string getExtension = fileInfo.Extension;

        //        string reqIFPath = Path.Combine(Path.GetTempPath(), $"{DateTime.Now.Ticks}" + getExtension);


        //        if (reqIFFile.ContentLength > 0)
        //        {

        //            reqIFFile.SaveAs(reqIFPath);


        //            //try
        //            //{
        //            //    var directory = Path.GetDirectoryName(reqIFPath);
        //            //    var filename = Path.GetFileNameWithoutExtension(reqIFPath);
        //            //    string dir = Path.Combine(directory, filename);
        //            //    var zip = ZipFile.OpenRead(reqIFPath);
        //            //}
        //            //catch (Exception ex)
        //            //{

        //            //}


        //            if (getExtension != null && getExtension.Equals(".reqifz"))
        //            {
        //                var directory = Path.GetDirectoryName(reqIFPath);
        //                var filename = Path.GetFileNameWithoutExtension(reqIFPath);
        //                string dir = Path.Combine(directory, filename);
        //                ZipFile.ExtractToDirectory(reqIFPath, dir);
        //            }


        //            jsonResult = Json(new { success = true, path = reqIFPath, message = "" }, JsonRequestBehavior.AllowGet);
        //        }
        //        else
        //        {
        //            CommonUtility.LogError(CONST_REQIF_FILE_VALIDATION_FAILED);
        //            jsonResult = Json(new { success = false, path = "", message = CONST_REQIF_FILE_VALIDATION_FAILED }, JsonRequestBehavior.AllowGet);

        //        }

        //    }
        //    catch (Exception e)
        //    {

        //        CommonUtility.LogError(e);
        //    }
        //    CommonUtility.LogInfo("DefaultController", "GetReqIFServerPath", "End");
        //    return jsonResult;

        //}

        //[HttpPost]
        //public JsonResult GetReqIFServerPath(HttpPostedFileBase reqIFFile)
        //{
        //    CommonUtility.LogInfo("DefaultController", "GetReqIFServerPath", "Start");
        //    JsonResult jsonResult = null;

        //    try
        //    {

        //        if (reqIFFile.ContentLength > 0)
        //        {
        //            System.IO.FileInfo fileInfo = new FileInfo(reqIFFile.FileName);
        //            string getExtension = fileInfo.Extension;

        //            string uniqueFileOrFolderName = DateTime.Now.Ticks.ToString();
        //            string basePath = Path.Combine(Path.GetTempPath(), $"{uniqueFileOrFolderName}");
        //            var baseDirectory = Directory.CreateDirectory(basePath);
        //            string reqIFPath = Path.Combine(basePath, $"{uniqueFileOrFolderName}" + getExtension);
        //            reqIFFile.SaveAs(reqIFPath);

        //            if (getExtension != null && getExtension.Equals(".reqifz"))
        //            {
        //                var directory = Path.GetDirectoryName(reqIFPath);
        //                var filename = Path.GetFileNameWithoutExtension(reqIFPath);
        //                string dir = Path.Combine(directory, filename);
        //                ZipFile.ExtractToDirectory(reqIFPath, dir);
        //            }
        //            else if (getExtension != null && getExtension.Equals(".reqif"))
        //            {
        //                var filesDirectory = Directory.CreateDirectory(Path.Combine(basePath,"files"));
        //                ZipFile.CreateFromDirectory(basePath, basePath + ".reqifz");
        //            }

        //            jsonResult = Json(new { success = true, path = reqIFPath, message = "" }, JsonRequestBehavior.AllowGet);
        //        }
        //        else
        //        {
        //            CommonUtility.LogError(CONST_REQIF_FILE_VALIDATION_FAILED);
        //            jsonResult = Json(new { success = false, path = "", message = CONST_REQIF_FILE_VALIDATION_FAILED }, JsonRequestBehavior.AllowGet);

        //        }

        //    }
        //    catch (Exception e)
        //    {

        //        CommonUtility.LogError(e);
        //    }
        //    CommonUtility.LogInfo("DefaultController", "GetReqIFServerPath", "End");
        //    return jsonResult;

        //}


        [HttpPost]
        public JsonResult GetReqIFServerPath(HttpPostedFileBase reqIFFile)
        {
            DebugLogger.LogStart("DefaultController", "GetReqIFServerPath");
            JsonResult jsonResult = null;
            string validFileResult = "";
            
            try
            {

                if (reqIFFile.ContentLength > 0)
                {
                    System.IO.FileInfo fileInfo = new FileInfo(reqIFFile.FileName);
                    string getExtension = fileInfo.Extension;

                    string reqIFPath = string.Empty;
                    if (getExtension != null && getExtension.Equals(".reqifz"))
                    {
                        reqIFPath = Path.Combine(Path.GetTempPath(), $"{DateTime.Now.Ticks}" + ".reqifz");
                        reqIFFile.SaveAs(reqIFPath);
                        var directory = Path.GetDirectoryName(reqIFPath);
                        var filename = Path.GetFileNameWithoutExtension(reqIFPath);
                        string dir = Path.Combine(directory, filename);
                        ZipFile.ExtractToDirectory(reqIFPath, dir);
                        validFileResult = checkValidFile(reqIFPath);
                        
                    }
                    else if (getExtension != null && getExtension.Equals(".reqif"))
                    {
                        string uniqueFileOrFolderName = DateTime.Now.Ticks.ToString();
                        string basePath = Path.Combine(Path.GetTempPath(), $"{uniqueFileOrFolderName}");
                        var baseDirectory = Directory.CreateDirectory(basePath);
                        reqIFPath = Path.Combine(basePath, $"{uniqueFileOrFolderName}" + ".reqif");
                        reqIFFile.SaveAs(reqIFPath);
                        var filesDirectory = Directory.CreateDirectory(Path.Combine(basePath, "files"));
                        ZipFile.CreateFromDirectory(basePath, basePath + ".reqifz");
                        reqIFPath = basePath + ".reqifz";
                        validFileResult = checkValidFile(reqIFPath);                      
                    }

                    jsonResult = Json(new { success = true, path = reqIFPath, message = validFileResult }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    DebugLogger.LogError(CONST_REQIF_FILE_VALIDATION_FAILED);
                    throw new Exception("Not Found!");
                }

            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                jsonResult = Json(new { success = false, path = "", message = CONST_REQIF_FILE_VALIDATION_FAILED }, JsonRequestBehavior.AllowGet);
            }
            DebugLogger.LogEnd("DefaultController", "GetReqIFServerPath");
            return jsonResult;
        }

        [HttpGet]
        public ActionResult GetResult(string key)
        {
            DebugLogger.LogStart("DefaultController", "GetResult");
            try
            {
                if (MvcApplication.OperationResults.ContainsKey(key))
                {
                    OperationResult result = MvcApplication.OperationResults[key];

                    if (result == null)
                    {
                        result = new OperationResult(OperationStatusTypes.InProgress);
                        DebugLogger.LogInfo("Operation Guid"+ key);
                    }
                    DebugLogger.LogEnd("DefaultController", "GetResult");
                    return new JsonResult()
                    {
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                        Data = JsonConvert.SerializeObject(result)
                    };
                }
                DebugLogger.LogError(CommonUtility.KEY_NOT_FOUND);
                DebugLogger.LogEnd("DefaultController", "GetResult");
            }
            catch (Exception ex)
            {

                DebugLogger.LogError(ex);
            }


            return new HttpNotFoundResult(CommonUtility.KEY_NOT_FOUND);
        }

        private static void PerformImportCore(AdoParams model, WorkItemTrackingHttpClient workItemTrackingHttpClient,
            ReqIFSharp.ReqIF reqif, ReqIFMappingTemplate mappingTemplate, Dictionary<string, int> binding, string reqIFBindingPath, Guid resultKey, LicenseType licenseType)
        {
            DebugLogger.LogStart("DefaultController", "PerformImportCore");
            DebugLogger.LogInfo("ReqIf Import Core by User" + model.UserName);
            OperationResult result = null;

            try
            {
                IDictionary<string, WorkItemRelationType> relationTypes = GetRelationTypes(workItemTrackingHttpClient);

                ReqIFImport reqIfImport = new ReqIFImport(reqif, workItemTrackingHttpClient, new Guid(model.ProjectId),
                    mappingTemplate, binding, relationTypes, licenseType);

                result = reqIfImport.Import();

                if (result.OperationStatus == OperationStatusTypes.Success)
                {
                    string json = JsonConvert.SerializeObject(binding);
                    byte[] bytes = Encoding.UTF8.GetBytes(json);

                    using (MemoryStream memoryStream = new MemoryStream(bytes))
                    {
                        using (FileStream reader = new FileStream(reqIFBindingPath, FileMode.Create))
                        {
                            memoryStream.CopyTo(reader);

                            memoryStream.Flush();
                            reader.Flush();

                            memoryStream.Close();
                            reader.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("DefaultController","PerformImportCore");
            MvcApplication.OperationResults[resultKey.ToString()] = result;
        }


        [HttpPost]
        public JsonResult ShowWorkItemGrid(AdoParams adoParams)
        {
            DebugLogger.LogStart("DefaultController", "ShowWorkItemGrid");
            List<WorkItemInfo> workItemInfos = new List<WorkItemInfo>();
            Dictionary<string, string> iconUrlCache = new Dictionary<string, string>(); // Cache to store iconUrl's
            Dictionary<string, string> stateIconCache = new Dictionary<string, string>(); // State icon cache
            long lngTicks = DateTime.Now.Ticks;

            try
            {
                VssCredentials vssCredentials = new VssCredentials(new VssOAuthAccessTokenCredential(adoParams.AccessToken));
                WorkItemTrackingHttpClient workItemTrackingHttpClient = new WorkItemTrackingHttpClient(new Uri(adoParams.ServerUri), vssCredentials);

                List<WorkItem> workItems = workItemTrackingHttpClient.GetWorkItemsAsyncCustom(adoParams.WorkItemIds);
                CurrentWorkItemsList = workItems;

                foreach (WorkItem workItem in workItems)
                {
                    if (workItem == null)
                    {
                        continue;
                    }

                    string workItemType = workItem.Fields[CONST_WORKITEM_TYPE_FIELD].ToString();
                    string state = workItem.Fields[CONST_WORKITEM_STATE_FIELD].ToString();

                    if (!stateIconCache.TryGetValue(state, out string stateIcon))
                    {
                        stateIcon = workItemTrackingHttpClient.GetWorkItemTypeStatesAsync(adoParams.ProjectId, workItemType)
                            .Result.FirstOrDefault(x => x.Name == state)?.Color;
                        stateIconCache[state] = stateIcon;
                    }

                    if (!iconUrlCache.TryGetValue(workItemType, out string iconUrl))
                    {
                        iconUrl = workItemTrackingHttpClient.GetWorkItemTypeAsync(adoParams.ProjectId, workItemType).Result.Icon.Url;
                        iconUrlCache[workItemType] = iconUrl;
                    }

                    workItemInfos.Add(new WorkItemInfo
                    {
                        Statecolor = stateIcon,
                        IconUrl = iconUrl,
                        ID = workItem.Id.Value,
                        Title = workItem.Fields[CONST_WORKITEM_TITLE_FIELD].ToString(),
                        WorkItemType = workItem.Fields[CONST_WORKITEM_TYPE_FIELD].ToString(),
                        AssignedTo = workItem.Fields.ContainsKey(CONST_WORKITEM_ASSIGNED_TO_FIELD)
                            ? ((Microsoft.VisualStudio.Services.WebApi.IdentityRef)workItem.Fields[CONST_WORKITEM_ASSIGNED_TO_FIELD]).DisplayName
                            : string.Empty,
                        State = state
                    });
                }

                lngTicks = DateTime.Now.Ticks - lngTicks;
                DebugLogger.LogInfo($"Time taken to get selected work items: {new TimeSpan(lngTicks).TotalMilliseconds} ms");

                string keyWithPrefix = adoParams.ProjectId + CONST_WORKITEM_TYPES;
                if (!RedisHelper.KeyExist(keyWithPrefix))
                {
                    GetAllWorkItemTypes(adoParams);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("DefaultController", "ShowWorkItemGrid");

            // Set the MaxJsonLength property
            var jsonResult = new JsonResult
            {
                Data = workItemInfos,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                MaxJsonLength = int.MaxValue
            };
            return jsonResult;
        }

        public List<WorkItem> CurrentWorkItemsList
        {
            get
            {
                Object item = System.Web.HttpContext.Current.Session[SESSION_CURRENT_WORKITEMS];
                return item as List<WorkItem>;
            }
            set { System.Web.HttpContext.Current.Session[SESSION_CURRENT_WORKITEMS] = value; }
        }

        [HttpGet]
        public FileResult DownloadFile(string fileName)
        {
            DebugLogger.LogStart("DefaultController", "DownloadFile");
            byte[] fileBytes = null;
            try
            {
                //need to change.
                //string directory = Path.GetFileNameWithoutExtension(fileName);
                //string absPath = Path.Combine(directory, fileName);
                string path = Path.Combine(Path.GetTempPath(), fileName);

                if (!System.IO.File.Exists(path))
                {
                    return null;
                }

                fileBytes = System.IO.File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("DefaultController", "DownloadFile");
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public FileResult DownloadMappingFile(ReqIFMappingTemplate mappingJson, string projectGuid)
        {
            DebugLogger.LogStart("DefaultController", "DownloadMappingFile");
            byte[] fileBytes = null;

            DownloadedFile downloadedfile = new DownloadedFile();
            downloadedfile.FileName = projectGuid + ".xml";
            try
            {
                OperationResult operationResult = ValidateMappingTemplate(mappingJson,projectGuid);
                if (operationResult.OperationStatus != OperationStatusTypes.Success)
                {
                    fileBytes = Encoding.ASCII.GetBytes(operationResult.Message);
                    DebugLogger.LogEnd("DefaultController", "DownloadMappingFile");
                    return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, downloadedfile.FileName);

                }

                string path = Path.Combine(Path.GetTempPath(), projectGuid + ".xml");
                XmlSerializer serializer = new XmlSerializer(typeof(ReqIFMappingTemplate));
                var fileStream = System.IO.File.Create(path);
                serializer.Serialize(fileStream, mappingJson);
                fileStream.Close();

                System.IO.Stream stream = new MemoryStream();
                using (Stream input = System.IO.File.OpenRead(path))
                {
                    input.CopyTo(stream);
                }
                stream.Position = 0;

                downloadedfile.FileData = stream;

                using (var streamReader = new System.IO.MemoryStream())
                {
                    downloadedfile.FileData.CopyTo(streamReader);
                    fileBytes = streamReader.ToArray();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("DefaultController", "DownloadMappingFile");
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, downloadedfile.FileName);
        }

        [HttpPost]
        public ActionResult PerformExport(AdoParams adoParams,ExportDTO exportDto)
        {
            DebugLogger.LogStart("DefaultController", "PerformExport");
            DebugLogger.LogInfo("Export Process Started by user" + adoParams.UserName);
            OperationResult result = null;

            try
            {
                if (LicenseController.LicenseInformation == null)
                {
                    LicenseController licenseController = new LicenseController();
                    JsonResult jsonResult = licenseController.ValidateLicense(adoParams);
                }

                switch (LicenseController.LicenseInformation.LicenseType)
                {
                    case LicenseType.TrialLicense:
                        {
                            var selectedWorkItems = adoParams.WorkItemIds.ToList().Take(MvcApplication.Trial_License_Allowed_Count);
                            adoParams.WorkItemIds = selectedWorkItems.ToArray();
                            break;
                        }

                    default:
                        if (LicenseController.LicenseInformation.AllowedFeatures == null || LicenseController.LicenseInformation.AllowedFeatures.Count == 0)
                        {
                            result = new OperationResult(OperationStatusTypes.Failed, null, "");
                            return new JsonResult()
                            {
                                Data = JsonConvert.SerializeObject(result)
                            };
                        }

                        break;
                }

                VssCredentials vssCredentials =
                new VssCredentials(new VssOAuthAccessTokenCredential(adoParams.AccessToken));
                WorkItemTrackingHttpClient workItemTrackingHttpClient =
                    new WorkItemTrackingHttpClient(new Uri(adoParams.ServerUri), vssCredentials);


                ReqIFSharp.ReqIF reqif = null;
                string reqIFMappingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Templates/{adoParams.ProjectId}.xml");
                ReqIFMappingTemplate mappingTemplate = CommonUtility.LoadMapping(reqIFMappingPath);

                //todo : need to change this implementation into Mongo
                string reqIFBindingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Bindings/{adoParams.ProjectId}-binding.json");
                Dictionary<string, int> binding = CommonUtility.GetBinding(reqIFBindingPath);
                string bindingDirectory = Path.GetDirectoryName(reqIFBindingPath);
                if (!Directory.Exists(bindingDirectory))
                {
                    Directory.CreateDirectory(bindingDirectory);
                }

                Guid resultKey = Guid.NewGuid();
                MvcApplication.OperationResults[resultKey.ToString()] = null;
                HostingEnvironment.QueueBackgroundWorkItem(token => PerformExportCore(adoParams, workItemTrackingHttpClient, reqif, mappingTemplate, binding, reqIFBindingPath, resultKey, exportDto));

                result = new OperationResult(OperationStatusTypes.Success, resultKey.ToString(), null);
            }
            catch (Exception e)
            {
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
                DebugLogger.LogError(e);
            }
            DebugLogger.LogEnd("DefaultController", "PerformExport");
            return new JsonResult()
            {
                Data = JsonConvert.SerializeObject(result)
            };
        }

        public ActionResult ReqIFImport(string callingViewId, string adoParamModel, string reqIFFilePath, string reqIFFileName)
        {
            DebugLogger.LogStart("DefaultController", "ReqIfImport");
            //ImportReqIFDialog
            var reqIFTempFilePath = JsonConvert.DeserializeObject<string>(reqIFFilePath);
            string redisKey = "";
            AdoParams resultAdoParams = new AdoParams();
            try
            {
                if (adoParamModel != null)
                {

                    resultAdoParams = JsonConvert.DeserializeObject<AdoParams>(adoParamModel);
                    DebugLogger.LogInfo("Import mapping template viewed by user " + resultAdoParams.ProjectId);
                    if (resultAdoParams.ProjectId != null && !string.IsNullOrWhiteSpace(resultAdoParams.ProjectId))
                    {
                        redisKey = resultAdoParams.ProjectId + CONST_REQIF_FILE_MAPPING_KEY;
                    }
                    string reqIFMappingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Templates/{resultAdoParams.ProjectId}.xml");
                    ReqIFMappingTemplate savedMappingTemplate = CommonUtility.LoadMapping(reqIFMappingPath);
                    List<string> savedMappingWorkitemsTypes = new List<string>();

                    if (savedMappingTemplate == null)
                    {
                        savedMappingTemplate = CreateTemplateForImport(reqIFTempFilePath);
                    }
                    else
                    {
                        foreach (var typeMap in savedMappingTemplate.TypeMaps)
                        {
                            savedMappingWorkitemsTypes.Add(typeMap.WITypeName);
                        }

                        OperationResult addRefIFResult = AddReqIFFileIntoMapping(reqIFTempFilePath, ref savedMappingTemplate);
                        if (addRefIFResult.OperationStatus != OperationStatusTypes.Success)
                        {
                            DebugLogger.LogError(addRefIFResult.Message);
                        }
                    }

                    ViewBag.CallingViewId = callingViewId;
                    ViewBag.MappingJSON = savedMappingTemplate;
                    ViewBag.ProjectGuid = resultAdoParams.ProjectId;
                    ViewBag.AdoParams = adoParamModel;
                    ViewBag.WorkItemTypes = GetAllWorkItemTypes(resultAdoParams).Keys.ToList();
                    ViewBag.AllLinksTypes = GetAllRelationTypesDataCollection(resultAdoParams);
                    ViewBag.WorkItemTypeFieldsData = GetWorkItemTypesFieldsData(resultAdoParams, savedMappingWorkitemsTypes);
                    if (reqIFFileName != null)
                    {
                        ViewBag.FileName = "(" + HttpUtility.UrlDecode(JsonConvert.DeserializeObject<string>(reqIFFileName)) + ")";

                    }
                    else
                    {
                        ViewBag.FileName = "";
                    }
                    OperationResult operationResult = ValidateReqIFEnumFields(reqIFTempFilePath);
                    if (operationResult.OperationStatus != OperationStatusTypes.Success)
                    {
                        ViewBag.DuplicateLongName = operationResult.Tag as List<string>;
                    }
                    if (resultAdoParams.ProjectId != null && !string.IsNullOrWhiteSpace(resultAdoParams.ProjectId))
                        RedisHelper.SaveInCache(redisKey, reqIFTempFilePath, null);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("DefaultController", "ReqIfImport");
            return View();
        }

        public JsonResult ReqIFExportData(AdoParams adoParamModel)
        {
            DebugLogger.LogStart("DefaultController", "ReqIfExportData");
            JsonResult jsonResult = Json(new { operation = true, message = "" }, JsonRequestBehavior.AllowGet);
            TempData["model"] = adoParamModel;
            DebugLogger.LogEnd("DefaultController", "ReqIFExportData");
            return jsonResult;
        }
        public ActionResult ReqIFExport()
        {
            DebugLogger.LogStart("DefaultController", "ReqIfExport");
            long lngTicks = DateTime.Now.Ticks;
            long AlllngTicks = DateTime.Now.Ticks;
            ViewBag.CallingViewId = "ExportReqIFDialog";
            AdoParams adoParamModel = null;
            if (TempData.ContainsKey("model"))
            {
                adoParamModel = (AdoParams)TempData["model"];
            }

            try
            {
                if (adoParamModel != null)
                {
                    var resultAdoParams = adoParamModel;// JsonConvert.DeserializeObject<AdoParams>(adoParamModel);
                    string reqIFMappingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Templates/{resultAdoParams.ProjectId}.xml");
                    DebugLogger.LogInfo("Export mapping template viewed by user " + resultAdoParams.UserName);
                    ReqIFMappingTemplate mappingTemplate = CommonUtility.LoadMapping(reqIFMappingPath);
                    List<string> selectedWorkitemsTypes = new List<string>();
                    if (mappingTemplate == null)
                    {

                        mappingTemplate = CreateTemplate(resultAdoParams, ref selectedWorkitemsTypes);
                        lngTicks = DateTime.Now.Ticks - lngTicks;
                        DebugLogger.LogInfo("Time taken CreateTemplate first time " + new DateTime(lngTicks).Minute + "minute " + new DateTime(lngTicks).Second + "second " + new DateTime(lngTicks).Millisecond + "millisecond.");
                    }
                    else
                    {
                        lngTicks = DateTime.Now.Ticks;
                        foreach (var typeMap in mappingTemplate.TypeMaps)
                        {
                            selectedWorkitemsTypes.Add(typeMap.WITypeName);
                        }

                        List<WorkItem> selectedWorkItems = null;
                        IDictionary<string, WorkItemRelationType> adoRelationTypes = null;

                        GetSelectedWorkItemsAndTypes(resultAdoParams, ref selectedWorkItems, ref adoRelationTypes);

                        lngTicks = DateTime.Now.Ticks - lngTicks;
                        DebugLogger.LogInfo("Time taken for this operation is:GetSelectedWorkItemsAndTypes " + new DateTime(lngTicks).Minute + "minute " + new DateTime(lngTicks).Second + "second " + new DateTime(lngTicks).Millisecond + "millisecond.");
                        //CommonUtility.LogInfo("Time taken for this operation is:GetSelectedWorkItemsAndTypes " + new DateTime(lngTicks).Minute + "minute " + new DateTime(lngTicks).Second + "second " + new DateTime(lngTicks).Millisecond + "millisecond.");
                        lngTicks = DateTime.Now.Ticks;
                        foreach (WorkItem workitem in selectedWorkItems)
                        {
                            string workItemType = workitem.Fields["System.WorkItemType"].ToString();
                            if (!selectedWorkitemsTypes.Contains(workItemType))
                            {
                                TypeMap typeMap = new TypeMap();
                                List<EnumFieldMap> lstEnumFieldMaps = new List<EnumFieldMap>();
                                EnumFieldMap enumFieldMap = new EnumFieldMap();


                                enumFieldMap.WIFieldName = "System.Title";
                                enumFieldMap.ReqIFFieldName = "System.Title";
                                enumFieldMap.FieldType = FieldTypes.String;
                                lstEnumFieldMaps.Add(enumFieldMap);

                                typeMap.WITypeName = workItemType;
                                typeMap.ReqIFTypeName = workItemType;
                                typeMap.EnumFieldMaps = lstEnumFieldMaps;
                                mappingTemplate.TypeMaps.Add(typeMap);
                                selectedWorkitemsTypes.Add(workItemType);

                            }
                            if (workitem.Relations != null && adoRelationTypes != null)
                            {
                                foreach (var relationType in workitem.Relations)
                                {
                                    if (relationType.Rel != "AttachedFile")
                                    {
                                        var linkType = adoRelationTypes.FirstOrDefault(x => x.Value.ReferenceName == relationType.Rel).Key;

                                        if (!mappingTemplate.LinkMaps.Any(x => x.WILinkName.Contains(linkType)))
                                        {
                                            LinkMap linkMap = new LinkMap();
                                            linkMap.WILinkName = linkType;
                                            linkMap.ReqIFRelationName = linkType;
                                            mappingTemplate.LinkMaps.Add(linkMap);
                                        }
                                    }

                                }
                            }
                        }
                        lngTicks = DateTime.Now.Ticks - lngTicks;
                        DebugLogger.LogInfo("Time taken for this operation is:Iteration & json generation " + new DateTime(lngTicks).Minute + "minute " + new DateTime(lngTicks).Second + "second " + new DateTime(lngTicks).Millisecond + "millisecond.");
                    }
                    ViewBag.AdoParams = adoParamModel;
                    ViewBag.MappingJSON = mappingTemplate;
                    ViewBag.ProjectGuid = resultAdoParams.ProjectId;
                    ViewBag.WorkItemTypeFieldsData = GetWorkItemTypesFieldsData(resultAdoParams, selectedWorkitemsTypes);
                    string keyWithPrefix = adoParamModel.ProjectId + CONST_WORKITEM_TYPES;

                    if (!RedisHelper.KeyExist(keyWithPrefix))
                    {
                        GetAllWorkItemTypes(adoParamModel);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            AlllngTicks = DateTime.Now.Ticks - AlllngTicks;
            DebugLogger.LogInfo("Time taken for overall operation to load export plugin " + new DateTime(AlllngTicks).Minute + "minute " + new DateTime(AlllngTicks).Second + "second " + new DateTime(AlllngTicks).Millisecond + "millisecond.");
            DebugLogger.LogEnd("DefaultController", "ReqIfExport");
            return View();
        }
        [ValidateInput(false)]
        public JsonResult OnImportMappingFile(string xmlAsString,string reqifSessionObject)
        {
            DebugLogger.LogStart("DefaultController", "OnImportMappingFile");
            JsonResult jsonResult = null;

            try
            {
                long lngTicks = DateTime.Now.Ticks;
                string uploadedFileName = lngTicks + ".xml";
                string uploadedFileSavingPath = System.IO.Path.GetTempPath() + uploadedFileName;

                // Create the XmlDocument.
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlAsString);

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                // Save the document to a file and auto-indent the output.
                XmlWriter writer = XmlWriter.Create(uploadedFileSavingPath, settings);
                doc.Save(writer);
                writer.Close();
                if (System.IO.File.Exists(uploadedFileSavingPath))
                {

                    ReqIFMappingTemplate mappingTemplate = CommonUtility.LoadMapping(uploadedFileSavingPath);
                    if (mappingTemplate != null)
                    {

                        if (!string.IsNullOrEmpty(reqifSessionObject))
                        {

                            const string fromFilePath = "&reqIFFilePath=";
                            const string toFileName = "&reqIFFileName=";

                            int pFrom = reqifSessionObject.IndexOf(fromFilePath) + fromFilePath.Length;
                            int pTo = reqifSessionObject.LastIndexOf(toFileName);

                            string reqifFilePath = JsonConvert.DeserializeObject<string>(reqifSessionObject.Substring(pFrom, pTo - pFrom));
                            OperationResult addRefIFResult = AddReqIFFileIntoMapping(reqifFilePath, ref mappingTemplate);

                            if (addRefIFResult.OperationStatus != OperationStatusTypes.Success)
                            {
                                DebugLogger.LogError(addRefIFResult.Message);
                            }
                        }

                        var json = new JavaScriptSerializer().Serialize(mappingTemplate);
                        jsonResult = Json(new { success = true, mappingjson = json, message = "" }, JsonRequestBehavior.AllowGet);
                        DebugLogger.LogEnd("DefaultController", "OnImportMappingFile");
                        return jsonResult;

                    }
                }


            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            jsonResult = Json(new { success = false, mappingjson = "", message = "" }, JsonRequestBehavior.AllowGet);
            DebugLogger.LogEnd("Defaultcontroller", "OnImportMappingFile");
            return jsonResult;
        }
        public JsonResult SaveMappingFile(ReqIFMappingTemplate mappingJson, string projectGuid)
        {
            DebugLogger.LogStart("DefaultController", "SaveMappingFile");
            JsonResult jsonResult = null;
            string errMsg = CONST_REQIF_MAPPPING_FILE_NOTSAVED;

            OperationResult operationResult = ValidateMappingTemplate(mappingJson, projectGuid);
            if (operationResult.OperationStatus != OperationStatusTypes.Success)
            {
                jsonResult = Json(new { validation = false, message = operationResult.Message }, JsonRequestBehavior.AllowGet);
                DebugLogger.LogEnd("DefaultController", "SaveMappingFile");
                return jsonResult;
            }

            try
            {
                DebugLogger.LogInfo("Mapping template has been updated by a current user agianst the project Guid " + projectGuid);

                string reqIFMappingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Templates/{projectGuid}.xml");

                XmlSerializer serializer = new XmlSerializer(typeof(ReqIFMappingTemplate));
                var fileStream = System.IO.File.Create(reqIFMappingPath);
                serializer.Serialize(fileStream, mappingJson);
                fileStream.Close();

                ReqIFMappingTemplate mappingTemplate = CommonUtility.LoadMapping(reqIFMappingPath);
                if (System.IO.File.Exists(reqIFMappingPath) && mappingTemplate != null)
                {
                    jsonResult = Json(new { validation = true, message = "" }, JsonRequestBehavior.AllowGet);
                    DebugLogger.LogEnd("DefaultController", "SaveMappingFile");
                    return jsonResult;
                }

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                errMsg = ex.Message;
            }

            jsonResult = Json(new { validation = false, message = errMsg }, JsonRequestBehavior.AllowGet);

            DebugLogger.LogEnd("DefaultController", "SaveMappingFile");
            return jsonResult;
        }

        public JsonResult GetWorkItemsIdsByQuery(AdoParams model)
        {
            JsonResult jsonResult = null;
            DebugLogger.LogStart("DefaultController", "GetWorkItemsIdsByQuery");
            try
            {
                jsonResult = Json(new { nonFlat = false, success = false, result = "" }, JsonRequestBehavior.AllowGet);
                List<int> workItemIds = new List<int>();
                VssCredentials vssCredentials =
                 new VssCredentials(new VssOAuthAccessTokenCredential(model.AccessToken));
                WorkItemTrackingHttpClient workItemTrackingHttpClient =
                    new WorkItemTrackingHttpClient(new Uri(model.ServerUri), vssCredentials);

                WorkItemQueryResult workItemQueryResult = null;
                Guid g;
                if (Guid.TryParse(model.Query, out g))
                {
                    workItemQueryResult = workItemTrackingHttpClient.QueryByIdAsync(g).Result;
                }
                else
                {
                    var wiqlQuery = model.Query;

                    if (wiqlQuery.Contains("@project"))
                    {
                        wiqlQuery = wiqlQuery.Replace("@project", "'" + model.ProjectName + "'");
                    }

                    Wiql wiql = new Wiql
                    {
                        Query = wiqlQuery
                    };
                    workItemQueryResult = workItemTrackingHttpClient.QueryByWiqlAsync(wiql).Result;
                }


                if (workItemQueryResult != null)
                {
                    if (workItemQueryResult.WorkItems != null)
                    {
                        workItemIds = workItemQueryResult.WorkItems.Select(x => x.Id).OrderBy(id => id).ToList();
                        jsonResult = Json(new { nonFlat = false, success = true, result = workItemIds }, JsonRequestBehavior.AllowGet);
                    }
                    else if (workItemQueryResult.WorkItems == null && workItemQueryResult.WorkItemRelations != null)
                    {
                        workItemIds = GetWorkItemIdsbyRelation(workItemQueryResult.WorkItemRelations).OrderBy(id => id).ToList();
                        jsonResult = Json(new { nonFlat = true, success = true, result = workItemIds }, JsonRequestBehavior.AllowGet);
                    }

                    //jsonResult = Json(new { success = true, result = workItemIds }, JsonRequestBehavior.AllowGet);
                }

            }
            catch (ArgumentNullException ex)
            {
                DebugLogger.LogError(ex);
            }
            catch (VssOAuthTokenRequestException ex)
            {
                DebugLogger.LogError(ex);
            }
            catch (AggregateException ex)
            {
                DebugLogger.LogError(ex);
            }
            catch (JsonSerializationException ex)
            {
                DebugLogger.LogError(ex);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogStart("DefaultController", "GetWorkItemsIdsByQuery");
            return jsonResult;
        }

        private List<int> GetWorkItemIdsbyRelation(IEnumerable<WorkItemLink> workItemRelations)
        {
            DebugLogger.LogStart("DefaultController", "GetWorkItemIdsbyRelation");
            List<int> workitemIds = new List<int>();
            try
            {

                foreach (var item in workItemRelations)
                {
                    if (item.Source != null && item.Source.Id > 0)
                    {
                        if (!workitemIds.Contains(item.Source.Id))
                            workitemIds.Add(item.Source.Id);
                    }

                    if (item.Target != null && item.Target.Id > 0)
                    {
                        if (!workitemIds.Contains(item.Target.Id))
                            workitemIds.Add(item.Target.Id);
                    }

                }
            }
            catch (Exception ex)
            {

                DebugLogger.LogError(ex);
            }


            DebugLogger.LogEnd("DefaultController", "GetWorkItemIdsbyRelation");
            return workitemIds;
        }

        private OperationResult ValidateMappingTemplate(ReqIFMappingTemplate mappingJson, string projectGuid)
        {
            OperationResult result = new OperationResult(OperationStatusTypes.Success);
            DebugLogger.LogStart("DefaultController", "validateMappingTemplate");


            try
            {
                if (mappingJson.TypeMaps == null && mappingJson.LinkMaps == null)
                {
                    return result;
                }

                if (mappingJson.TypeMaps != null)
                {

                    //Start - Bug Fixing: 29656
                    foreach (var item in mappingJson.TypeMaps)
                    {
                        if (!item.EnumFieldMaps.Any(x => x != null && x.WIFieldName == "System.Title"))
                        {
                            result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.TITLE_FIELD_VALIDATION_MSG);
                            DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                            return result;
                        }
                    }

                    //End - Bug Fixing: 29656

                    if (mappingJson.TypeMaps.Any(x => x.ReqIFTypeName == null) || mappingJson.TypeMaps.Any(x => x.WITypeName == null))
                    {
                        result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.EMPTY_MSG);
                        DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                        return result;

                    }

                    if (mappingJson.TypeMaps.Any(x => x.EnumFieldMaps.Any(y => y.WIFieldName == null)) || mappingJson.TypeMaps.Any(p => p.EnumFieldMaps.Any(y => y.ReqIFFieldName == null)))
                    {
                        result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.EMPTY_MSG);
                        DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                        return result;
                    }

                    var regex = new Regex("[<>&'\"\r\n]");

                    if (mappingJson.TypeMaps.Any(x => regex.IsMatch(x.ReqIFTypeName)) ||
                       mappingJson.TypeMaps.Any(x => x.EnumFieldMaps.Any(y => regex.IsMatch(y.ReqIFFieldName))))
                    {
                        result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.CONST_SPECIAL_CHAR_MSG);
                        DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                        return result;
                    }

                    var duplicateWorkItems = mappingJson.TypeMaps.GroupBy(x => x.WITypeName)
                        .Where(g => g.Count() > 1)
                        .Select(y => y.Key)
                        .ToList();

                    if (duplicateWorkItems.Count > 0)
                    {
                        string duplicateWIStr = GetValidLength((string.Join(",", duplicateWorkItems.Select(x => x.ToString()))));
                        result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.DUPLICATE_MSG + "[" + duplicateWIStr + "]");

                        DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                        return result;
                    }

                    foreach (var item in mappingJson.TypeMaps)
                    {
                        var duplicateDataFields = item.EnumFieldMaps.GroupBy(x => x.WIFieldName)
                        .Where(g => g.Count() > 1)
                        .Select(y => y.Key)
                        .ToList();

                        if (duplicateDataFields.Count > 0)
                        {
                            StringBuilder stringBuilder = new StringBuilder();

                            foreach (string duplicate in duplicateDataFields)
                            {
                                stringBuilder.Append(duplicate).Append(", ");
                            }

                            string duplicateStr = stringBuilder.ToString().Trim();
                            DebugLogger.LogError(CommonUtility.DUPLICATE_FIELDS_MSG + duplicateStr + " for " + item.WITypeName);
                            result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.DUPLICATE_FIELDS_MSG);
                            DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                            return result;
                        }
                    }


                    //Start - Bug Fixing: 29006

                    var duplicateReqIFTypes = mappingJson.TypeMaps.GroupBy(x => x.ReqIFTypeName)
                       .Where(g => g.Count() > 1)
                       .Select(y => y.Key)
                       .ToList();

                    if (duplicateReqIFTypes.Count > 0)
                    {
                        string duplicateWIStr = GetValidLength(string.Join(",", duplicateReqIFTypes.Select(x => x.ToString())));
                        result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.DUPLICATE_MSG_FOR_REQIF + "[" + duplicateWIStr + "]");

                        DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                        return result;
                    }

                    foreach (var item in mappingJson.TypeMaps)
                    {
                        var duplicateDataFields = item.EnumFieldMaps.GroupBy(x => x.ReqIFFieldName)
                        .Where(g => g.Count() > 1)
                        .Select(y => y.Key)
                        .ToList();

                        if (duplicateDataFields.Count > 0)
                        {
                            result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.DUPLICATE_FIELDS_MSG_FOR_REQIF);
                            DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                            return result;
                        }
                    }


                    //End - Bug Fixing: 29006



                    //foreach (var item in mappingJson.TypeMaps)
                    //{
                    //    if (!item.EnumFieldMaps.Any(x => x.WIFieldName == "System.Title"))
                    //    {
                    //        result = new OperationResult(OperationStatusTypes.Failed, titlefieldValidation);
                    //        CommonUtility.LogInfo("DefaultController", "ValidateMappingTemplate", "End");
                    //        return result;
                    //    }                        
                    //}






                }
                if (mappingJson.LinkMaps != null)
                {
                    if (mappingJson.LinkMaps.Any(x => x.ReqIFRelationName == null) || mappingJson.LinkMaps.Any(x => x.WILinkName == null))
                    {
                        result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.EMPTY_MSG);
                        DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                        return result;
                    }

                    //Temporary allowing duplicate link mapping in order to import siemen energy data
                    //var duplicateRelationTypes = mappingJson.LinkMaps.GroupBy(x => x.ReqIFRelationName)
                    //   .Where(g => g.Count() > 1)
                    //   .Select(y => y.Key)
                    //   .ToList();

                    //if (duplicateRelationTypes.Count > 0)
                    //{
                    //    string duplicateWIStr = (string.Join(",", duplicateRelationTypes.Select(x => x.ToString())));
                    //    result = new OperationResult(OperationStatusTypes.Failed, duplicateMsgForLink + "[" + duplicateWIStr + "]");

                    //    CommonUtility.LogInfo("DefaultController", "ValidateMappingTemplate", "End");
                    //    return result;
                    //}

                }

                string keyWithPrefix = projectGuid + CONST_WORKITEM_TYPES;
                Dictionary<string, string> workitemTypeInfo = new Dictionary<string, string>();
                if (RedisHelper.KeyExist(keyWithPrefix))
                {
                    try
                    {
                        workitemTypeInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(RedisHelper.LoadFromCache(keyWithPrefix));
                        if (workitemTypeInfo != null && workitemTypeInfo.Keys.Count > 0)
                        {
                            List<string> lstString = mappingJson.TypeMaps.Select(x => x.WITypeName).ToList();
                            var notFoundList = lstString.Except(workitemTypeInfo.Keys.ToList()).ToList();
                            if (notFoundList.Count > 0)
                            {
                                string strinCSV = GetValidLength(string.Join(",", notFoundList.Select(x => x.ToString())));
                                result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.NOT_FOUND_MSG + " " + strinCSV);
                                DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                                return result;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(ex);
                    }
                }

                string keyWithPrefixWT = projectGuid + CONST_WORKITEM_TYPES_FIELDS_DATA;
                Dictionary<string, List<WorkItemFieldsDataCollection>> dicWorkitemFieldDataCollection = new Dictionary<string, List<WorkItemFieldsDataCollection>>();

                if (RedisHelper.KeyExist(keyWithPrefixWT))
                {
                    try
                    {
                        dicWorkitemFieldDataCollection = JsonConvert.DeserializeObject<Dictionary<string, List<WorkItemFieldsDataCollection>>>(RedisHelper.LoadFromCache(keyWithPrefixWT));
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(ex);
                    }
                }

                foreach (var item in mappingJson.TypeMaps)
                {
                    List<string> lstDataFieldsMT = item.EnumFieldMaps.Select(x => x.WIFieldName).ToList();
                    if (dicWorkitemFieldDataCollection.Keys.Count > 0)
                    {
                        List<string> lstDataFieldsAll = dicWorkitemFieldDataCollection[item.WITypeName].Select(x => x.name).ToList();

                        var notFoundList = lstDataFieldsMT.Except(lstDataFieldsAll).ToList();
                        if (notFoundList.Count > 0)
                        {

                            string notValidFields = (string.Join(",", notFoundList.Select(x => x.ToString())));
                            result = new OperationResult(OperationStatusTypes.Failed, notValidFields + " is/are not the valid field(s) of work item type " + item.WITypeName);
                            DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
                            return result;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                result = new OperationResult(OperationStatusTypes.Failed, ex.Message);
            }
            DebugLogger.LogEnd("DefaultController", "ValidateMappingTemplate");
            return result;
        }

        private string GetValidLength(string popUpStr)
        {
            DebugLogger.LogStart("DefaultController", "GetValidLength");
            if (!string.IsNullOrEmpty(popUpStr) && popUpStr.Length > CONST_POPUP_MESSAGE_ALLOWED_LENGTH)
            {

                popUpStr = popUpStr.Substring(0, CONST_POPUP_MESSAGE_ALLOWED_LENGTH) + CONST_TRIM_SUFFIX;

            }

            DebugLogger.LogEnd("DefaultController", "GetValidLength");
            return popUpStr;
        }

        private ReqIFMappingTemplate CreateTemplate(AdoParams adoParams, ref List<string> selectedWorkItemsTypes)
        {
            DebugLogger.LogStart("DefaultController", "CreateTemplate");
            DebugLogger.LogInfo("Create Template process started by user" + adoParams.UserName);
            ReqIFMappingTemplate reqIFMappingTemplate = new ReqIFMappingTemplate();
            List<TypeMap> typeMaps = new List<TypeMap>();
            List<LinkMap> linkMaps = new List<LinkMap>();

            reqIFMappingTemplate.TemplateName = "basic";
            reqIFMappingTemplate.TemplateVersion = "0.1.0";
            List<WorkItem> selectedWorkItems = null;
            IDictionary<string, WorkItemRelationType> adoRelationTypes = null;
            GetSelectedWorkItemsAndTypes(adoParams, ref selectedWorkItems, ref adoRelationTypes);


            foreach (WorkItem workitem in selectedWorkItems)
            {
                string workItemType = workitem.Fields["System.WorkItemType"].ToString();

                if (!typeMaps.Any(x => x.WITypeName.Contains(workItemType)))
                {
                    TypeMap typeMap = new TypeMap();
                    List<EnumFieldMap> lstEnumFieldMaps = new List<EnumFieldMap>();
                    EnumFieldMap enumFieldMap = new EnumFieldMap();


                    enumFieldMap.WIFieldName = "System.Title";
                    enumFieldMap.ReqIFFieldName = "System.Title";
                    enumFieldMap.FieldType = FieldTypes.String;
                    lstEnumFieldMaps.Add(enumFieldMap);

                    typeMap.WITypeName = workItemType;
                    typeMap.ReqIFTypeName = workItemType;
                    typeMap.EnumFieldMaps = lstEnumFieldMaps;
                    typeMaps.Add(typeMap);
                    selectedWorkItemsTypes.Add(workItemType);

                }


                if (workitem.Relations != null)
                {
                    foreach (var relationType in workitem.Relations)
                    {
                        if (adoRelationTypes != null && relationType.Rel != "AttachedFile")
                        {
                            var linkType = adoRelationTypes.FirstOrDefault(x => x.Value.ReferenceName == relationType.Rel).Key;

                            if (!linkMaps.Any(x => x.WILinkName.Contains(linkType)))
                            {
                                LinkMap linkMap = new LinkMap();
                                linkMap.WILinkName = linkType;
                                linkMap.ReqIFRelationName = linkType;
                                linkMaps.Add(linkMap);
                            }
                        }


                    }
                }

            }
            reqIFMappingTemplate.TypeMaps = typeMaps;
            reqIFMappingTemplate.LinkMaps = linkMaps;
            DebugLogger.LogEnd("DefaultController", "CreateTemplate");
            return reqIFMappingTemplate;
        }

        private bool GetSelectedWorkItemsAndTypes(AdoParams adoParams, ref List<WorkItem> selectedWorkItems, ref IDictionary<string, WorkItemRelationType> adoRelationTypes)
        {
            DebugLogger.LogStart("DefaultController", "GetSelectedWorkItemsAndTypes");
            DebugLogger.LogInfo("GetSelectedWorkItemsAndTypes by User" + adoParams.UserName);
            string keyWithPrefix = adoParams.ProjectId + CONST_LINKS_TYPES;
            VssCredentials vssCredentials =
      new VssCredentials(new VssOAuthAccessTokenCredential(adoParams.AccessToken));
            WorkItemTrackingHttpClient workItemTrackingHttpClient =
                 new WorkItemTrackingHttpClient(new Uri(adoParams.ServerUri), vssCredentials);

            if (RedisHelper.KeyExist(keyWithPrefix))
            {
                try
                {
                    adoRelationTypes = JsonConvert.DeserializeObject<Dictionary<string, WorkItemRelationType>>(RedisHelper.LoadFromCache(keyWithPrefix));
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(ex);
                }
            }


            selectedWorkItems = CurrentWorkItemsList != null
                ? CurrentWorkItemsList
                : workItemTrackingHttpClient.GetWorkItemsAsyncCustom(adoParams.WorkItemIds);

            if (adoRelationTypes == null || adoRelationTypes.Count == 0)
            {
                adoRelationTypes = GetRelationTypes(workItemTrackingHttpClient);
                bool isWriteOnRedis = RedisHelper.SaveInCache(keyWithPrefix, JsonConvert.SerializeObject(adoRelationTypes), null);

            }
            DebugLogger.LogEnd("DefaultController", "GetSelectedWorkItemsAndTypes");
            return true;
        }
        private FieldTypes GetReqIfFieldType(string reqIFname)
        {
            switch (reqIFname)
            {
                case "DatatypeDefinitionXHTML":
                    //case "RichText":
                    //case "xHTML":
                    return FieldTypes.RichText;
                case "DatatypeDefinitionInteger":
                case "DatatypeDefinitionDouble":
                case "DatatypeDefinitionFloat":
                case "DatatypeDefinitionReal":
                    return FieldTypes.Numeric;
                case "DatatypeDefinitionEnumeration":
                    return FieldTypes.Enum;
                case "DatatypeDefinitionDate":
                    return FieldTypes.DateTime;

                default:
                    return FieldTypes.String;

            }

        }

        private ReqIFMappingTemplate CreateTemplateForImport(string reqIFFilePath)
        {
            DebugLogger.LogStart("DefaultController", "CreateTemplateForImport");
            ReqIFMappingTemplate reqIFMappingTemplate = new ReqIFMappingTemplate();
            List<TypeMap> typeMaps = new List<TypeMap>();
            List<LinkMap> linkMaps = new List<LinkMap>();

            reqIFMappingTemplate.TemplateName = "basic";
            reqIFMappingTemplate.TemplateVersion = "0.1.0";

            ReqIFDeserializer deserializer = new ReqIFDeserializer();
            ReqIFSharp.ReqIF reqif = deserializer.Deserialize(reqIFFilePath).FirstOrDefault();
            var content = reqif.CoreContent;

            //Adding ReqIF Object Types in temporary mapping file

            foreach (var item in content.SpecTypes)
            {

                if (item.GetType().Name.ToString() == "SpecObjectType")
                {

                    //if (string.IsNullOrEmpty(item.LongName))
                    //{
                  
                    //    item.LongName = item.Identifier;
                    //}

                    string reqifWorkItemType = Regex.Replace(item.LongName, @"Type", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline).TrimEnd();

                    TypeMap typeMap = new TypeMap();
                    List<EnumFieldMap> lstEnumFieldMaps = new List<EnumFieldMap>();

                    typeMap.WITypeName = "";
                    typeMap.ReqIFTypeName = reqifWorkItemType;

                    foreach (var attributes in item.SpecAttributes)
                    {
                        if (!lstEnumFieldMaps.Any(x => x.ReqIFFieldName == attributes.LongName))
                        {
                            EnumFieldMap enumFieldMap = new EnumFieldMap();
                            enumFieldMap.WIFieldName = "";
                            enumFieldMap.ReqIFFieldName = attributes.LongName;
                            enumFieldMap.FieldType = FieldTypes.String;
                            enumFieldMap.ReqIFFieldType = GetReqIfFieldType(attributes.DatatypeDefinition.GetType().Name);

                            if (attributes.DatatypeDefinition.GetType().Name.Equals("DatatypeDefinitionEnumeration"))
                            {
                                var enumValues = ((ReqIFSharp.AttributeDefinitionEnumeration)attributes).Type.SpecifiedValues.ToList();

                                List<EnumValueMap> LstEnumValueMap = new List<EnumValueMap>();

                                foreach (var enumValue in enumValues)
                                {
                                    EnumValueMap enumValueMap = new EnumValueMap();
                                    enumValueMap.ReqIFEnumFieldValue = enumValue.LongName;
                                    enumValueMap.WIEnumFieldValue = "";
                                    LstEnumValueMap.Add(enumValueMap);
                                }

                                enumFieldMap.EnumValueMaps = LstEnumValueMap;
                            }

                            lstEnumFieldMaps.Add(enumFieldMap);
                        }
                        else
                        {
                            DebugLogger.LogError("Object Type: " + reqifWorkItemType + " has contain duplicate fields: " + attributes.LongName);
                        }

                    }
                    typeMap.EnumFieldMaps = lstEnumFieldMaps;
                    typeMaps.Add(typeMap);

                }

            }

            //Adding ReqIF Relation Types in temporary mapping file
            foreach (var item in content.SpecTypes)
            {
                if (item.GetType().Name.ToString() == "SpecRelationType")
                {
                    string reqifWorkItemType = Regex.Replace(item.LongName, @"Type", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline).TrimEnd();
                    LinkMap newLinkMap = new LinkMap();
                    newLinkMap.ReqIFRelationName = reqifWorkItemType;
                    newLinkMap.WILinkName = "";
                    linkMaps.Add(newLinkMap);
                }
            }

            reqIFMappingTemplate.TypeMaps = typeMaps;
            reqIFMappingTemplate.LinkMaps = linkMaps;
            DebugLogger.LogEnd("DefaultController", "CreateTemplateForImport");

            return reqIFMappingTemplate;
        }

        private OperationResult AddReqIFFileIntoMapping(string reqIFFilePath, ref ReqIFMappingTemplate reqIFMappingTemplate)
        {
            DebugLogger.LogStart("DefaultController", "AddReqIFFileIntoMapping");
            OperationResult result = new OperationResult(OperationStatusTypes.Success);
            try
            {
                ReqIFDeserializer deserializer = new ReqIFDeserializer();
                ReqIFSharp.ReqIF reqif = deserializer.Deserialize(reqIFFilePath).FirstOrDefault();
                var content = reqif.CoreContent;
                List<TypeMap> listTypeMaps = new List<TypeMap>();

                // checking new fields for existing object types into save mapping template
                foreach (var item in content.SpecTypes)
                {
                    if (item.GetType().Name.ToString() == "SpecObjectType")
                    {
                        string reqifWorkItemType = Regex.Replace(item.LongName, @"Type", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline).TrimEnd();
                        var typeMaps = reqIFMappingTemplate.TypeMaps.Where(x => x.ReqIFTypeName == reqifWorkItemType).FirstOrDefault();
                        if (typeMaps != null)
                        {
                            foreach (var attributes in item.SpecAttributes)
                            {
                                if (!typeMaps.EnumFieldMaps.Any(y => y.ReqIFFieldName == attributes.LongName))
                                {
                                    EnumFieldMap enumFieldMap = new EnumFieldMap();
                                    enumFieldMap.WIFieldName = "";
                                    enumFieldMap.ReqIFFieldName = attributes.LongName;
                                    enumFieldMap.FieldType = FieldTypes.String;
                                    enumFieldMap.ReqIFFieldType = GetReqIfFieldType(attributes.DatatypeDefinition.GetType().Name);

                                    if (attributes.DatatypeDefinition.GetType().Name.Equals("DatatypeDefinitionEnumeration"))
                                    {
                                        var enumValues = ((ReqIFSharp.AttributeDefinitionEnumeration)attributes).Type.SpecifiedValues.ToList();

                                        List<EnumValueMap> LstEnumValueMap = new List<EnumValueMap>();

                                        foreach (var enumValue in enumValues)
                                        {
                                            EnumValueMap enumValueMap = new EnumValueMap();
                                            enumValueMap.ReqIFEnumFieldValue = enumValue.LongName;
                                            enumValueMap.WIEnumFieldValue = "";
                                            LstEnumValueMap.Add(enumValueMap);
                                        }

                                        enumFieldMap.EnumValueMaps = LstEnumValueMap;


                                    }

                                    typeMaps.EnumFieldMaps.Add(enumFieldMap);
                                }

                            }

                        }
                        //Adding new objects with all its new fields 
                        else
                        {
                            TypeMap typeMap = new TypeMap();
                            List<EnumFieldMap> lstEnumFieldMaps = new List<EnumFieldMap>();

                            typeMap.WITypeName = "";
                            typeMap.ReqIFTypeName = reqifWorkItemType;

                            foreach (var attributes in item.SpecAttributes)
                            {
                                EnumFieldMap enumFieldMap = new EnumFieldMap();
                                enumFieldMap.WIFieldName = "";
                                enumFieldMap.ReqIFFieldName = attributes.LongName;
                                enumFieldMap.FieldType = FieldTypes.String;
                                enumFieldMap.ReqIFFieldType = GetReqIfFieldType(attributes.DatatypeDefinition.GetType().Name);

                                if (attributes.DatatypeDefinition.GetType().Name.Equals("DatatypeDefinitionEnumeration"))
                                {
                                    var enumValues = ((ReqIFSharp.AttributeDefinitionEnumeration)attributes).Type.SpecifiedValues.ToList();
                                    List<EnumValueMap> LstEnumValueMap = new List<EnumValueMap>();

                                    foreach (var enumValue in enumValues)
                                    {
                                        EnumValueMap enumValueMap = new EnumValueMap();
                                        enumValueMap.ReqIFEnumFieldValue = enumValue.LongName;
                                        enumValueMap.WIEnumFieldValue = "";
                                        LstEnumValueMap.Add(enumValueMap);
                                    }

                                    enumFieldMap.EnumValueMaps = LstEnumValueMap;
                                }
                                lstEnumFieldMaps.Add(enumFieldMap);

                            }
                            typeMap.EnumFieldMaps = lstEnumFieldMaps;

                            reqIFMappingTemplate.TypeMaps.Add(typeMap);
                        }
                    }
                    else if (item.GetType().Name.ToString() == "SpecRelationType")
                    {
                        string reqifWorkItemLinkType = Regex.Replace(item.LongName, @"Type", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline).TrimEnd();
                        bool isExists = reqIFMappingTemplate.LinkMaps.Any(x => x.ReqIFRelationName == reqifWorkItemLinkType);
                        if (!isExists)
                        {
                            LinkMap linkMap = new LinkMap();
                            linkMap.ReqIFRelationName = reqifWorkItemLinkType;
                            linkMap.WILinkName = "";
                            reqIFMappingTemplate.LinkMaps.Add(linkMap);
                        }
                    }
                }



            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                result = new OperationResult(OperationStatusTypes.Failed, ex.Message);
            }
            DebugLogger.LogEnd("DefaultController", "AddReqIFFileIntoMapping");
            return result;
        }

        private WorkItemTypeFieldWithReferences FilterAllowedValuesOfWorkItems(WorkItemTypeFieldWithReferences workItemTypeFieldWithReferences)
        {
            DebugLogger.LogStart("DefaultController", "FilterAllowedValuesOfWorkItems");
            WorkItemTypeFieldWithReferences workItemTypeFieldWith = workItemTypeFieldWithReferences;
            try
            {
                for (int i = 0; i < workItemTypeFieldWith.AllowedValues.Length; i++)
                {
                    var item = workItemTypeFieldWith.AllowedValues[i].ToString();

                    if (item != null && item.Length > 0 && item.StartsWith("<") && item.EndsWith(">"))
                    {
                        workItemTypeFieldWith.AllowedValues[i] = item.Substring(1, item.Length - 2);
                    }
                }
                return workItemTypeFieldWith;
            }
            catch (Exception ex)
            {

                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("DefaultController", "FilterAllowedValuesOfWorkItems");
            return workItemTypeFieldWithReferences;
        }
        private Dictionary<string, List<WorkItemFieldsDataCollection>> GetWorkItemTypesFieldsData(AdoParams adoParams, List<string> selectedWorkitemsTypes)
        {
            DebugLogger.LogStart("DefaultController", "GetWorkItemTypesFieldsData");
            long OverAll_lngTicks = DateTime.Now.Ticks;
            Dictionary<string, List<WorkItemFieldsDataCollection>> dicWorkitemFieldDataCollection = new Dictionary<string, List<WorkItemFieldsDataCollection>>();
            string keyWithPrefix = adoParams.ProjectId + CONST_WORKITEM_TYPES_FIELDS_DATA;
            try
            {

                VssCredentials vssCredentials =
                    new VssCredentials(new VssOAuthAccessTokenCredential(adoParams.AccessToken));

                WorkItemTrackingHttpClient workItemTrackingHttpClient =
                    new WorkItemTrackingHttpClient(new Uri(adoParams.ServerUri), vssCredentials);

                WorkItemTrackingProcessHttpClient workItemTrackingProcessHttpClient =
                   new WorkItemTrackingProcessHttpClient(new Uri(adoParams.ServerUri), vssCredentials);

                bool updateRedisCache = false;
                if (RedisHelper.KeyExist(keyWithPrefix))
                {
                    try
                    {
                        dicWorkitemFieldDataCollection = JsonConvert.DeserializeObject<Dictionary<string, List<WorkItemFieldsDataCollection>>>(RedisHelper.LoadFromCache(keyWithPrefix));
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(ex);
                    }
                }
                var newWorkItemsTypes = selectedWorkitemsTypes.Where(item => !dicWorkitemFieldDataCollection.Keys.ToList().Any(e => item == e));

                List<WorkItemTypeFieldWithReferences> workItemTypeFieldWithReferences = new List<WorkItemTypeFieldWithReferences>();

                foreach (var workItemType in newWorkItemsTypes)
                {
                    //string workitemTypeRefName = dicWorkitemTypes[workItemType];
                    long lngTicks = DateTime.Now.Ticks;
                    try
                    {
                        workItemTypeFieldWithReferences = workItemTrackingHttpClient.GetWorkItemTypeFieldsWithReferencesAsync(adoParams.ProjectId, workItemType, WorkItemTypeFieldsExpandLevel.AllowedValues).Result;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogInfo(CommonUtility.UNABLE_TO_GET_WORKITEM_TYPES + " " + workItemType);
                        DebugLogger.LogError(ex);
                    }

                    try
                    {
                        var fields = workItemTrackingHttpClient.GetFieldsAsync(adoParams.ProjectId).Result;

                        List<WorkItemFieldsDataCollection> lstWorkItemFieldsDataCollection = new List<WorkItemFieldsDataCollection>();

                        string[] hiddenFields = { };
                        var values = ConfigurationManager.AppSettings.Get("ReqIF.HiddenADOFields");
                        if (!string.IsNullOrWhiteSpace(values))
                        {
                            hiddenFields = values.Split(',')
                                                 .Select(x => x.Trim())
                                                 .Where(x => !string.IsNullOrWhiteSpace(x))
                                                 .ToArray();
                        }

                        foreach (var workItemTypeFieldWithReference in workItemTypeFieldWithReferences)
                        {


                            var getType = fields.FirstOrDefault(x => x.ReferenceName == workItemTypeFieldWithReference.ReferenceName);

                            if (getType != null)
                            {
                                WorkItemFieldsDataCollection workItemFieldsDataCollection = new WorkItemFieldsDataCollection();
                                //

                                //WorkItemField workItemField = workItemTrackingHttpClient.GetFieldAsync(workItemTypeFieldWithReference.ReferenceName).Result;
                                // Bug Fixing 29829 - Start

                                if (!hiddenFields.Contains(workItemTypeFieldWithReference.ReferenceName))
                                {
                                    // Bug Fixing 29829 - End
                                    workItemFieldsDataCollection.isVisibleOnUI = true;
                                    workItemFieldsDataCollection.name = workItemTypeFieldWithReference.ReferenceName;
                                    workItemFieldsDataCollection.referenceName = workItemTypeFieldWithReference.ReferenceName;
                                    workItemFieldsDataCollection.type = getType.Type.ToString();

                                    if (workItemTypeFieldWithReference.AllowedValues.Count() > 0)
                                    {
                                        // Bug Fixing 32584 - Start
                                        workItemTypeFieldWithReference.AllowedValues = FilterAllowedValuesOfWorkItems(workItemTypeFieldWithReference).AllowedValues;
                                        // Bug Fixing 32584 - End
                                        workItemFieldsDataCollection.type = "Enum";

                                    }
                                    workItemFieldsDataCollection.allowedValues = workItemTypeFieldWithReference.AllowedValues;

                                    if (workItemTypeFieldWithReference.AlwaysRequired && workItemTypeFieldWithReference.DefaultValue == null && !mandatoryFieldExceptionList.Contains(workItemTypeFieldWithReference.ReferenceName))
                                    {
                                        workItemFieldsDataCollection.alwaysRequired = true;
                                        if (workItemTypeFieldWithReference.AllowedValues.Count() > 0)
                                        {
                                            workItemFieldsDataCollection.defaultValue = workItemTypeFieldWithReference.AllowedValues[0].ToString();
                                        }
                                        else
                                        {
                                            workItemFieldsDataCollection.defaultValue = "No " + workItemTypeFieldWithReference.Name;
                                        }
                                    }




                                    switch (workItemFieldsDataCollection.type)
                                    {
                                        case "Html":
                                        case "RichText":
                                            workItemFieldsDataCollection.type = FieldTypes.RichText.ToString();
                                            break;
                                        case "Integer":
                                        case "Double":
                                            workItemFieldsDataCollection.type = FieldTypes.Numeric.ToString();
                                            break;
                                        case "Enum":
                                            workItemFieldsDataCollection.type = FieldTypes.Enum.ToString();
                                            break;
                                        case "DateTime":
                                            workItemFieldsDataCollection.type = FieldTypes.DateTime.ToString();
                                            break;
                                        default:
                                            workItemFieldsDataCollection.type = FieldTypes.String.ToString();
                                            break;
                                    }

                                    lstWorkItemFieldsDataCollection.Add(workItemFieldsDataCollection);
                                }
                            }

                        }
                        dicWorkitemFieldDataCollection.Add(workItemType, lstWorkItemFieldsDataCollection);
                        updateRedisCache = true;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogInfo(CommonUtility.UNABLE_TO_GET_FIELDS_FOR_WORKITEM_MSG + ": " + workItemType);
                        DebugLogger.LogError(ex);

                    }

                }
                if (updateRedisCache)
                {
                    RedisHelper.SaveInCache(keyWithPrefix, JsonConvert.SerializeObject(dicWorkitemFieldDataCollection), null);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            OverAll_lngTicks = DateTime.Now.Ticks - OverAll_lngTicks;
            DebugLogger.LogInfo("Time taken for the operation GetAllWorkItemTypeFieldsAsync " + new DateTime(OverAll_lngTicks).Minute + "minute " + new DateTime(OverAll_lngTicks).Second + "second " + new DateTime(OverAll_lngTicks).Millisecond + "millisecond.");
            DebugLogger.LogEnd("DefaultController", "GetWorkItemTypesFieldsData");
            return dicWorkitemFieldDataCollection;
        }
        private Dictionary<string, string> GetAllWorkItemTypes(AdoParams adoParams)
        {
            DebugLogger.LogStart("DefaultController", "GetAllWorkItemTypes");
            long lngTicks = DateTime.Now.Ticks;
            Dictionary<string, string> workitemTypeInfo = new Dictionary<string, string>();

            try
            {
                string keyWithPrefix = adoParams.ProjectId + CONST_WORKITEM_TYPES;

                if (RedisHelper.KeyExist(keyWithPrefix))
                {
                    try
                    {
                        workitemTypeInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(RedisHelper.LoadFromCache(keyWithPrefix));
                        if (workitemTypeInfo != null && workitemTypeInfo.Keys.Count > 0)
                        {
                            lngTicks = DateTime.Now.Ticks - lngTicks;
                            DebugLogger.LogInfo(CommonUtility.TIME_TAKEN_TO_GET_ALL_WORKITEMS_MSG + " " + new DateTime(lngTicks).Minute + "minute " + new DateTime(lngTicks).Second + "second " + new DateTime(lngTicks).Millisecond + "millisecond.");
                            DebugLogger.LogEnd("DefaultController", "GetAllWorkItemTypes");
                            return workitemTypeInfo;
                        }

                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(ex);
                    }
                }

                VssCredentials vssCredentials =
new VssCredentials(new VssOAuthAccessTokenCredential(adoParams.AccessToken));
                WorkItemTrackingHttpClient workItemTrackingHttpClient =
                    new WorkItemTrackingHttpClient(new Uri(adoParams.ServerUri), vssCredentials);
                List<WorkItemType> workItemTypes = workItemTrackingHttpClient.GetWorkItemTypesAsync(adoParams.ProjectId).Result;

                //                WorkItemTrackingProcessHttpClient workItemTrackingProcessHttpClient =
                //new WorkItemTrackingProcessHttpClient(new Uri(adoParams.ServerUri), vssCredentials);

                //                var workItemTypes = workItemTrackingProcessHttpClient.GetProcessWorkItemTypesAsync(LicenseController.GetProjectProcessId(adoParams), Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models.GetWorkItemTypeExpand.None).Result;

                string[] hiddenWITypes = { };
                var values = ConfigurationManager.AppSettings.Get("ReqIF.HiddenWorkItemTypes");
                if (!string.IsNullOrWhiteSpace(values))
                {
                    hiddenWITypes = values.Split(',')
                                         .Select(x => x.Trim())
                                         .Where(x => !string.IsNullOrWhiteSpace(x))
                                         .ToArray();
                }

                foreach (var workItemType in workItemTypes)
                {
                    if (!hiddenWITypes.Contains(workItemType.ReferenceName))
                    {
                        workitemTypeInfo.Add(workItemType.Name, workItemType.ReferenceName);
                    }
                }
                lngTicks = DateTime.Now.Ticks - lngTicks;
                DebugLogger.LogInfo(CommonUtility.TIME_TAKEN_TO_GET_ALL_WORKITEMS_MSG + " " + new DateTime(lngTicks).Minute + "minute " + new DateTime(lngTicks).Second + "second " + new DateTime(lngTicks).Millisecond + "millisecond.");

                RedisHelper.SaveInCache(keyWithPrefix, JsonConvert.SerializeObject(workitemTypeInfo), null);

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);

            }

            DebugLogger.LogEnd("DefaultController", "GetAllWorkItemTypes");
            return workitemTypeInfo;
        }

        private List<RelationTypesDataCollection> GetAllRelationTypesDataCollection(AdoParams resultAdoParams)
        {
            DebugLogger.LogStart("DefaultController", "GetAllRelationTypesDataCollection");
            List<RelationTypesDataCollection> listRelationTypesDataCollections = new List<RelationTypesDataCollection>();
            string keyWithPrefix = resultAdoParams.ProjectId + CONST_LINKTYPES_DATA_COLLECTION;
            if (RedisHelper.KeyExist(keyWithPrefix))
            {
                try
                {
                    listRelationTypesDataCollections = JsonConvert.DeserializeObject<List<RelationTypesDataCollection>>(RedisHelper.LoadFromCache(keyWithPrefix));
                    if (listRelationTypesDataCollections.Count > 0)
                    {
                        return listRelationTypesDataCollections;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(ex);

                }
            }

            VssCredentials vssCredentials =
new VssCredentials(new VssOAuthAccessTokenCredential(resultAdoParams.AccessToken));
            WorkItemTrackingHttpClient workItemTrackingHttpClient =
                new WorkItemTrackingHttpClient(new Uri(resultAdoParams.ServerUri), vssCredentials);


            IDictionary<string, WorkItemRelationType> relationTypes = GetRelationTypes(workItemTrackingHttpClient);
            try
            {
                foreach (var item in relationTypes)
                {
                    RelationTypesDataCollection relationTypesDataCollection = new RelationTypesDataCollection();
                    relationTypesDataCollection.Name = item.Key;
                    relationTypesDataCollection.ReferenceName = item.Value.ReferenceName;
                    relationTypesDataCollection.Id = 0;
                    relationTypesDataCollection.LinkType = "";
                    relationTypesDataCollection.IsForwardLink = true;
                    relationTypesDataCollection.OppositeLinkName = "";
                    listRelationTypesDataCollections.Add(relationTypesDataCollection);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);

            }
            RedisHelper.SaveInCache(keyWithPrefix, JsonConvert.SerializeObject(listRelationTypesDataCollections), null);

            DebugLogger.LogEnd("DefaultController", "GetAllRelationTypesDataCollection");
            return listRelationTypesDataCollections;
        }

        public string GetWorkItemTypeFieldData(string adoParams, string selectedWorkitemsType)
        {
            DebugLogger.LogStart("DefaultController", "GetWorkItemTypeFieldData");
            Dictionary<string, List<WorkItemFieldsDataCollection>> result = null;
            try
            {
                if (String.IsNullOrEmpty(selectedWorkitemsType)) return null;
                adoParams = HttpUtility.HtmlDecode(adoParams);
                var resultAdoParams = JsonConvert.DeserializeObject<AdoParams>(adoParams);
                List<string> wiType = selectedWorkitemsType.Split(new char[] { ',' }).ToList();
                result = GetWorkItemTypesFieldsData(resultAdoParams, wiType);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);

            }
            if (result == null)
            {
                DebugLogger.LogError(CommonUtility.RESULT_IS_NULL_MSG);

            }
            DebugLogger.LogEnd("DefaultController", "GetWorkItemTypeFieldData");
            return JsonConvert.SerializeObject(result);
        }

        public JsonResult ValidateMappingFile(AdoParams model)
        {
            DebugLogger.LogStart("DefaultController", "ValidateMappingFile");
            JsonResult jsonResult = null;
            string returnErrMsg = string.Empty;
            string reqIFMappingPath = string.Empty;
            try
            {
                reqIFMappingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Templates/{model.ProjectId}.xml");
                ReqIFMappingTemplate mappingTemplate = CommonUtility.LoadMapping(reqIFMappingPath);
                if (System.IO.File.Exists(reqIFMappingPath) && mappingTemplate != null && mappingTemplate.TypeMaps.Count > 0)
                {
                    jsonResult = Json(new { validation = true, message = "" }, JsonRequestBehavior.AllowGet);
                    DebugLogger.LogEnd("DefaultController", "ValidateMappingFile");
                    return jsonResult;
                }

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            returnErrMsg = CONST_REQIF_MAPPPING_FILE_NOTFOUND + "Path: " + reqIFMappingPath;
            jsonResult = Json(new { validation = false, message = returnErrMsg }, JsonRequestBehavior.AllowGet);
            DebugLogger.LogEnd("DefaultController", "validateMappingFile");
            return jsonResult;
        }

        public JsonResult ValidateTemplateOnDownload(ReqIFMappingTemplate mappingJson, string projectGuid)
        {
            DebugLogger.LogStart("DefaultController", "ValidateTemplateOnDownload");
            JsonResult jsonResult = Json(new { validation = true, message = "" }, JsonRequestBehavior.AllowGet); ;
            OperationResult operationResult = ValidateMappingTemplate(mappingJson, projectGuid);
            if (operationResult.OperationStatus != OperationStatusTypes.Success)
            {
                jsonResult = Json(new { validation = false, message = operationResult.Message }, JsonRequestBehavior.AllowGet);
            }
            DebugLogger.LogEnd("DefaultController", "ValidateTemplateOnDownload");
            return jsonResult;
        }

        public JsonResult ClearCache(AdoParams model)
        {
            DebugLogger.LogStart("DefaultController", "ClearCache");
            string returnMsg = string.Empty;
            JsonResult jsonResult = Json(new { operation = false, message = returnMsg }, JsonRequestBehavior.AllowGet); ;

            try
            {
                string keyWithPrefix = string.Empty;
                keyWithPrefix = model.ProjectId + CONST_WORKITEM_TYPES_FIELDS_DATA;
                RedisHelper.RemoveFromCache(keyWithPrefix);

                keyWithPrefix = model.ProjectId + CONST_LINKTYPES_DATA_COLLECTION;
                RedisHelper.RemoveFromCache(keyWithPrefix);

                keyWithPrefix = model.ProjectId + CONST_WORKITEM_TYPES;
                RedisHelper.RemoveFromCache(keyWithPrefix);

                keyWithPrefix = model.ProjectId + CONST_LINKS_TYPES;
                RedisHelper.RemoveFromCache(keyWithPrefix);
                jsonResult = Json(new { operation = true, message = returnMsg }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                returnMsg = ex.Message;
                DebugLogger.LogError(ex);
            }


            DebugLogger.LogEnd("DefaultController", "ClearCache");
            return jsonResult;
        }
        public ActionResult ConfirmationDialog(string containerId)
        {
            DebugLogger.LogStart("DefaultController", "ConfirmationDialog");
            ViewBag.ControlId = containerId;
            DebugLogger.LogEnd("DefaultController", "ConfirmationDialog");
            return PartialView();
        }


        [HttpPost]
        public ActionResult CompareWorkItems(CompareWorkItemsRequest request)
        {
            try
            {
                if (request == null || request.WorkItemIds == null || request.BindingInfos == null)
                {
                    return Json(new { success = false, message = "Invalid request parameters" });
                }

                var comparison = new WorkItemComparisonResult();
                var bindingValues = request.BindingInfos.Values.ToList();

                // Find items in workItemIds that are not in bindingInfos (new items)
                comparison.MissingInBinding = request.WorkItemIds
                    .Where(id => !bindingValues.Contains(id))
                    .ToList();

                // Find items in bindingInfos that are not in workItemIds (existing items not selected)
                comparison.MissingInWorkItems = bindingValues
                    .Where(id => !request.WorkItemIds.Contains(id))
                    .ToList();

                string mainMessage;
                string detailMessage;

                if (comparison.MissingInBinding.Count == request.WorkItemIds.Length)
                {
                    // Case 3: All selected items are new
                    mainMessage =   "All selected work items are new and do not match any of the work items from the saved ReqIF. Click here for more details.";
                    detailMessage = "All selected work items are new and do not match any of the work items from the saved ReqIF.\n" +
                                    "Following are the new work items that will be added:\n" +
                                    $"{string.Join(", ", comparison.MissingInBinding)}";
                }
                else if (comparison.MissingInBinding.Count > 0)
                {
                    // Case 2: Some selected items are saved, others are new
                    mainMessage = "Some of the selected work items are new, and some already exist in the saved ReqIF. Click here for more details. Do you want to proceed?";
                    detailMessage = "Some of the selected work items are new, and some already exist in the saved ReqIF.\n" +
                                    "Following are the new work items that will be added:\n" +
                                    $"{string.Join(", ", comparison.MissingInBinding)}";

                    // Only add the missing items message if there are actually missing items
                    if (comparison.MissingInWorkItems.Count > 0)
                    {
                        detailMessage += "\nFollowing are the work items from the saved ReqIF that were not included in your selection:\n" +
                                        $"{string.Join(", ", comparison.MissingInWorkItems)}";
                    }
                }

                else if (comparison.MissingInWorkItems.Count > 0)
                {
                    // Case 1: All selected items are part of saved document (subset match)
                    mainMessage = "Some work items from the saved ReqIF were not included in your current selection. Click here for more details. Do you want to proceed?";
                    detailMessage = "Some work items from the saved ReqIF were not included in your current selection.\n" +
                                    "Following are the work items that are missing from your selection:\n" +
                                    $"{string.Join(", ", comparison.MissingInWorkItems)}";
                }


                else
                {
                    // Perfect match - no discrepancies
                    return Json(new
                    {
                        success = true,
                        hasDiscrepancies = false
                    });
                }

                return Json(new
                {
                    success = true,
                    hasDiscrepancies = true,
                    mainMessage = mainMessage,
                    detailMessage = detailMessage,
                    data = new
                    {
                        missingInBinding = comparison.MissingInBinding,
                        missingInWorkItems = comparison.MissingInWorkItems,
                        scenario = comparison.MissingInBinding.Count == request.WorkItemIds.Length ? 3 :
                                  comparison.MissingInBinding.Count > 0 && comparison.MissingInWorkItems.Count > 0 ? 2 : 1
                    }
                });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                return Json(new { success = false, message = "Error comparing work items" });
            }
        }


        private static void PerformExportCore(AdoParams adoParams,
            WorkItemTrackingHttpClient workItemTrackingHttpClient, ReqIFSharp.ReqIF reqif, ReqIFMappingTemplate mappingTemplate,
            Dictionary<string, int> binding, string reqIFBindingPath, Guid resultKey, ExportDTO exportDto)
        {
            DebugLogger.LogStart("DefaultController", "PerformExportCore");
            OperationResult result;
            IDictionary<string, WorkItemRelationType> relationTypes = GetRelationTypes(workItemTrackingHttpClient);



            string dirName = exportDto.ReqIF_FileName;
            string dirPath = Path.Combine(Path.GetTempPath(), $"{dirName}");
            //Create a directory and a files folder in it along with reqif file

            string reqIFPath = string.Empty;
            reqIFPath = Path.Combine(dirPath, $"{dirName}.reqif");

            try
            {
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                }

                var dirInfo = Directory.CreateDirectory(dirPath);
                Directory.CreateDirectory(Path.Combine(dirPath, "files"));
            }
            catch (IOException ex)
            {
                DebugLogger.LogError(ex);
                result = new OperationResult(OperationStatusTypes.Failed, null, null);
                return;

            }
            catch (UnauthorizedAccessException ex)
            {
                DebugLogger.LogError(ex);
                result = new OperationResult(OperationStatusTypes.Failed, null, null);
                return;

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                result = new OperationResult(OperationStatusTypes.Failed, null, null);
                return;
            }

            List<string> selectedWorkitemsTypes = new List<string>();

            foreach (var typeMap in mappingTemplate.TypeMaps)
            {
                selectedWorkitemsTypes.Add(typeMap.WITypeName);
            }
            // Retrieve or generate the work item fields data collection
            Dictionary<string, List<string>> dicEnumFieldsData = GetWorkItemTypesEnumFieldsData(adoParams);

            if (!string.IsNullOrEmpty(exportDto.Exchange_Id))
            {
                try
                {
                    MongoController mongoController = new MongoController();

                    //extract string from exchange id before character '(' 
                    string exchangeId = exportDto.Exchange_Id.Split('(')[0].Trim();

                    ReqIFDocument reqIFDocument = mongoController.GetReqIFByExchangeId(exchangeId);
                    
                    if (reqIFDocument != null && !string.IsNullOrEmpty(reqIFDocument.ReqIfXml))
                    {
                       
                        // Create a temporary file to store the XML
                        string tempFilePath = Path.Combine(Path.GetTempPath(), $"temp_reqif_{DateTime.Now.Ticks}.reqif");
                        System.IO.File.WriteAllText(tempFilePath, reqIFDocument.ReqIfXml);

                        // Use ReqIFDeserializer to convert the XML back to ReqIF object
                        ReqIFDeserializer deserializer = new ReqIFDeserializer();
                        reqif = deserializer.Deserialize(tempFilePath).FirstOrDefault();

                        // Clean up the temporary file
                        if (System.IO.File.Exists(tempFilePath))
                        {
                            System.IO.File.Delete(tempFilePath);
                        }                       


                    }
                    else
                    {
                        DebugLogger.LogError("ReqIF document or XML content is null");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError($"Error deserializing ReqIF XML: {ex.Message}");
                    throw;
                }
            }


            ReqIFExport reqIfExport = new ReqIFExport(reqif, workItemTrackingHttpClient, mappingTemplate, binding,
    adoParams.WorkItemIds, relationTypes, reqIFPath, exportDto, dicEnumFieldsData);

            result = reqIfExport.Export();

            if (result.OperationStatus == OperationStatusTypes.Success)
            {
                ReqIFSerializer serializer = new ReqIFSerializer();

                reqif = result.Tag as ReqIFSharp.ReqIF;
                IEnumerable<ReqIFSharp.ReqIF> reqIFs = new List<ReqIFSharp.ReqIF>() { reqif };
                serializer.Serialize(reqIFs, reqIFPath);

                string json = JsonConvert.SerializeObject(binding);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                using (MemoryStream memoryStream = new MemoryStream(bytes))
                {
                    using (FileStream reader = new FileStream(reqIFBindingPath, FileMode.Create))
                    {
                        memoryStream.CopyTo(reader);

                        memoryStream.Flush();
                        reader.Flush();

                        memoryStream.Close();
                        reader.Close();
                    }
                }

                //create zip path and return. reqifz
                string reqifDir = Path.GetDirectoryName(reqIFPath);
                string reqifzFile = reqifDir + ".reqifz";
                if (System.IO.File.Exists(reqifzFile))
                {
                    System.IO.File.Delete(reqifzFile);
                }
                ZipFile.CreateFromDirectory(reqifDir, reqifzFile);


                if (result != null && result.SecondaryObject != null)
                {
                    result = new OperationResult(OperationStatusTypes.Success, Path.GetFileName(reqifzFile), result.SecondaryObject);
                }
                else
                {
                    result = new OperationResult(OperationStatusTypes.Success, Path.GetFileName(reqifzFile), null);
                }


            }

            MvcApplication.OperationResults[resultKey.ToString()] = result;
            DebugLogger.LogEnd("DefaultController", "PerformExportCore");

        }

        private static Dictionary<string, List<string>> GetWorkItemTypesEnumFieldsData(AdoParams adoParams)
        {
            DebugLogger.LogStart("DefaultController", "GetWorkItemTypesEnumFieldsData");
            long OverAll_lngTicks = DateTime.Now.Ticks;
            Dictionary<string, List<string>> dicEnumFieldsData = new Dictionary<string, List<string>>();
            string keyWithPrefix = adoParams.ProjectId + CONST_WORKITEM_TYPES_FIELDS_DATA;
            try
            {
                if (RedisHelper.KeyExist(keyWithPrefix))
                {
                    try
                    {
                        var dicWorkitemFieldDataCollection = JsonConvert.DeserializeObject<Dictionary<string, List<WorkItemFieldsDataCollection>>>(RedisHelper.LoadFromCache(keyWithPrefix));

                        foreach (var workItemType in dicWorkitemFieldDataCollection)
                        {
                            foreach (var fieldData in workItemType.Value)
                            {
                                if (fieldData.type == FieldTypes.Enum.ToString() && fieldData.allowedValues != null)
                                {
                                    dicEnumFieldsData[fieldData.referenceName] = fieldData.allowedValues.Select(v => v.ToString()).ToList();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(ex);
                    }
                }
                OverAll_lngTicks = DateTime.Now.Ticks - OverAll_lngTicks;
                DebugLogger.LogInfo("Time taken for the operation GetWorkItemTypesEnumFieldsData " + new DateTime(OverAll_lngTicks).Minute + "minute " + new DateTime(OverAll_lngTicks).Second + "second " + new DateTime(OverAll_lngTicks).Millisecond + "millisecond.");

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            finally
            {                
                DebugLogger.LogEnd("DefaultController", "GetWorkItemTypesEnumFieldsData");
            }
         
            return dicEnumFieldsData;
        }


    

        private string checkValidFile(string reqIFFilePath)
        {
            string result = string.Empty;
            try
            {
                DebugLogger.LogStart("DefaultController", "checkSpecificationCount");

                ReqIFDeserializer deserializer = new ReqIFDeserializer();
                ReqIFSharp.ReqIF reqif = deserializer.Deserialize(reqIFFilePath).FirstOrDefault();
                var content = reqif.CoreContent;
                if (content.Specifications.Count == 0)
                {
                    result = CommonUtility.CONST_SPECIFICATION_COUNT_MSG;
                }

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                result = CommonUtility.CONST_CORRUPT_FILE_MSG;
            }

            DebugLogger.LogEnd("DefaultController", "checkSpecificationCount");
            return result;
        }

        private static IDictionary<string, WorkItemRelationType> GetRelationTypes(WorkItemTrackingHttpClient workItemTrackingHttpClient)
        {
            DebugLogger.LogStart("DefaultController", "GetRelationTypes");
            Dictionary<string, WorkItemRelationType> retRelationTypes = new Dictionary<string, WorkItemRelationType>();
            try
            {
                List<WorkItemRelationType> relationTypes = workItemTrackingHttpClient.GetRelationTypesAsync().Result;

                foreach (WorkItemRelationType relationType in relationTypes)
                {
                    retRelationTypes.Add(relationType.Name, relationType);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("DefaultController", "GetRelationTypes");
            return retRelationTypes;
        }


        private OperationResult ValidateReqIFEnumFields(string reqIFFilePath)
        {
            DebugLogger.LogStart("DefaultController", "ValidateReqIFEnumFields");
            try
            {
                ReqIFDeserializer deserializer = new ReqIFDeserializer();
                ReqIFSharp.ReqIF reqif = deserializer.Deserialize(reqIFFilePath).FirstOrDefault();
                var content = reqif.CoreContent;


                // Dictionary to store attributes grouped by LONG-NAME
                var attributeGroups = new Dictionary<string, Dictionary<string, HashSet<string>>>();

                // Process SPEC-TYPES
                foreach (var specType in content.SpecTypes)
                {
                    if (specType is SpecObjectType specObjectType)
                    {
                        foreach (var attributeDefinition in specObjectType.SpecAttributes)
                        {
                            string longName = attributeDefinition.LongName;
                            string datatypeGuid = attributeDefinition.DatatypeDefinition?.Identifier;

                            if (!string.IsNullOrEmpty(longName) && !string.IsNullOrEmpty(datatypeGuid))
                            {
                                if (!attributeGroups.ContainsKey(longName))
                                {
                                    attributeGroups[longName] = new Dictionary<string, HashSet<string>>();
                                }

                                if (!attributeGroups[longName].ContainsKey(specObjectType.Identifier))
                                {
                                    attributeGroups[longName][specObjectType.Identifier] = new HashSet<string>();
                                }

                                attributeGroups[longName][specObjectType.Identifier].Add(datatypeGuid);
                            }
                        }
                    }
                }

                // List to collect inconsistent longNames
                List<string> inconsistentLongNames = new List<string>();

                foreach (KeyValuePair<string, Dictionary<string, HashSet<string>>> group in attributeGroups)
                {
                    string longName = group.Key;
                    Dictionary<string, HashSet<string>> specTypeGuids = group.Value;

                    // Flatten all GUIDs for the current longName
                    List<string> allGuids = specTypeGuids.Values.SelectMany(guidSet => guidSet).ToList();

                    // Get distinct GUIDs
                    List<string> uniqueGuids = allGuids.Distinct().ToList();

                    if (uniqueGuids.Count > 1)
                    {
                        DebugLogger.LogError($"LongName '{longName}' has inconsistent identifier GUIDs: {string.Join(", ", uniqueGuids)}");
                        inconsistentLongNames.Add(longName);
                        // Additional handling can be performed here
                    }
                    else if (uniqueGuids.Count == 0)
                    {
                        DebugLogger.LogInfo($"LongName '{longName}' has no valid identifier GUIDs defined.");
                        // Optionally, handle the absence of identifiers
                    }
                    else
                    {
                        // Identifiers are consistent
                        DebugLogger.LogInfo($"LongName '{longName}' has consistent identifier GUID: {uniqueGuids.First()}");
                    }
                }

                if (inconsistentLongNames.Count > 0)
                {
                    // Handle inconsistencies as needed, e.g., return failure or perform corrective actions
                    string errorMessage = $"Inconsistent identifier GUIDs found for the following longNames: {string.Join(", ", inconsistentLongNames)}";
                    DebugLogger.LogError(errorMessage);
                    return new OperationResult(OperationStatusTypes.Failed, inconsistentLongNames);
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                DebugLogger.LogEnd("DefaultController", "ValidateReqIFEnumFields");
            }       

            return new OperationResult(OperationStatusTypes.Success);
        }

    }
}
