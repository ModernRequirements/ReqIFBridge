using inteGREAT.Web.Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using ReqIFBridge.Models;
using ReqIFBridge.ReqIF.ReqIFMapper;
using ReqIFBridge.Utility;
using ReqIFSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ReqIFBridge.ReqIF
{
    public class ReqIFImport
    {
        const string WI_LINK_PARENT = "Parent";
        const string WI_LINK_RELATED = "Related";
        const string CONST_ATTACHMENT_FILE = "AttachedFile";
        private readonly string[] markupIdentifiers = { "xhtml:object", "img" , "object data" };

        private readonly WorkItemTrackingHttpClient mWorkItemTrackingHttpClient = null;
        private readonly ReqIFMappingTemplate mMappingTemplate = null;
        private readonly ReqIFSharp.ReqIF mReqIf = null;
        private readonly IDictionary<string, WorkItemRelationType> mRelationTypes = null;
        private readonly Guid mProjectGuid = Guid.Empty;
        private Dictionary<string, int> mWiToReqIfBindings = null;
        private Dictionary<int, WorkItem> mWorkitemRecord = new Dictionary<int, WorkItem>();
        private readonly LimitedConcurrencyLevelTaskScheduler mImportTasksScheduler = null;
        private string mTargetedReqIFSpec = string.Empty;
        private int mImportRecordsCurrentCounter = 0;
        private LicenseType mLicenseType;
        StringBuilder importSB = new StringBuilder();
        private const string CONST_WORKITEM_TYPES_FIELDS_DATA = ":WorkItemTypesFieldsData";
        private const string CONST_REQIF_FILE_MAPPING_KEY = ":ConfigureMappingFile";
        private const string CONST_FILE_GUID = "FileNameGuid";
        private const string CONST_DELETE_ATTACHMENTS_KEY = "_delAttachments";
        private WorkItemsCountSummary workItemsCountSummary;

        //private Dictionary<string, string> attachmentFiles = new Dictionary<string, string>();
        /// <summary>
        /// Initializes a new instance of the <see cref="ReqIFImport" /> class.
        /// </summary>
        /// <param name="reqIf">The req if.</param>
        /// <param name="wiProject">The wi project.</param>
        /// <param name="mappingTemplate">The mapping template.</param>
        /// <param name="wiToReqIfBindings">The wi to req if bindings.</param>
        /// <param name="targetedReqIFSpec">The targeted req if spec.</param>
        public ReqIFImport(ReqIFSharp.ReqIF reqIf, WorkItemTrackingHttpClient workItemTrackingHttpClient, Guid projectGuid, ReqIFMappingTemplate mappingTemplate, Dictionary<string, int> wiToReqIfBindings, IDictionary<string, WorkItemRelationType> relationTypes, LicenseType mLicenseType, string targetedReqIFSpec = null)
        {
            DebugLogger.LogStart("ReqIFImport", "ReqIFImport()");
            this.mReqIf = reqIf;
            this.mWorkItemTrackingHttpClient = workItemTrackingHttpClient;
            this.mMappingTemplate = mappingTemplate;
            this.mWiToReqIfBindings = wiToReqIfBindings;
            this.mImportTasksScheduler = new LimitedConcurrencyLevelTaskScheduler(10);
            this.mRelationTypes = relationTypes;
            this.mProjectGuid = projectGuid;
            this.mLicenseType = mLicenseType;
            if (!string.IsNullOrEmpty(targetedReqIFSpec))
            {
                this.mTargetedReqIFSpec = targetedReqIFSpec;
            }
            DebugLogger.LogEnd("ReqIFImport", "ReqIFImport()");

        }

        public OperationResult Import()
        {
            return this.ImportCore();
        }

        public OperationResult UploadFileToTfsServer(Stream fileStream, string filename)
        {
            DebugLogger.LogStart("ReqIFImport", "UploadFileToTfsServer");
            OperationResult result = OperationResult.SuccessWithNoMessage;

            try
            {
                fileStream.Position = 0;

                //WorkItemTrackingHttpClient wiTrackingHttpClientParam = new WorkItemTrackingHttpClient(new Uri(this.ServerUri), new VssCredentials(mCredential));
                AttachmentReference attachmentReference = this.mWorkItemTrackingHttpClient.CreateAttachmentAsync(fileStream, fileName: filename).Result;


                result = new OperationResult(OperationStatusTypes.Success, (object)attachmentReference.Url);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                result = new OperationResult(OperationStatusTypes.Failed, null, ex);
            }
            DebugLogger.LogEnd("ReqIFImport", "UploadFileToTfsServer");

            return result;
        }
        private OperationResult ImportCore()
        {
            long lngTicks = DateTime.Now.Ticks;

            DebugLogger.LogStart("ReqIFImport", "ImportCore");
            //var header = this.mReqIf.TheHeader.FirstOrDefault();
            importSB.Clear();
            workItemsCountSummary = new WorkItemsCountSummary();


            var mappingDetails = this.mMappingTemplate.TypeMaps.SelectMany(x =>
            {
                var lst = new List<string>();
                string mappingInfo = $"{ x.WITypeName } : {string.Join(",", x.EnumFieldMaps.Select(y => y.WIFieldName))}";
                lst.Add(mappingInfo);
                return lst;
            });

            DebugLogger.LogInfo($"Template Mapping Detail -> {string.Join(Environment.NewLine, mappingDetails)}");
            OperationResult result = null;
            try
            {

                var content = this.mReqIf.CoreContent;
                int totalWorkItems = content.SpecObjects.Count;
                workItemsCountSummary.TotalWorkItems = totalWorkItems;
                if (this.mLicenseType == LicenseType.TrialLicense && totalWorkItems > MvcApplication.Trial_License_Allowed_Count)
                {

                    result = new OperationResult(OperationStatusTypes.Aborted, CommonUtility.CONST_REQIF_TRIAL_IMPORT_MSG);
                    DebugLogger.LogInfo(CommonUtility.CONST_REQIF_TRIAL_IMPORT_MSG);
                    DebugLogger.LogEnd("ReqIFImport", "ImportCore");
                    return result;

                }

                List<string> traversedHierarchy = new List<string>();

                GetAllMappedWorkItems();

                if (string.IsNullOrEmpty(this.mTargetedReqIFSpec))
                {
                    content.Specifications.ForEach(specification =>
                    {
                        result = ImportSpecificationCore(specification, traversedHierarchy);
                    });
                }
                else
                {
                    Specification retVal = GetSpecification();

                    result = ImportSpecificationCore(retVal, traversedHierarchy);
                }

                //Product Backlog Item 32044: Start
                //As a user, I shall be able to add configurable delay time in ReqIF4DevOps to avoid threshold level of Azure DevOps services.
                int adoDelayInMinutes = Convert.ToInt16(ConfigurationManager.AppSettings.Get("ReqIFImport.Delay"));
                if (adoDelayInMinutes >= 2 && adoDelayInMinutes <= 6)
                {
                    int milliSecond = (int)TimeSpan.FromMinutes(adoDelayInMinutes).TotalMilliseconds;
                    Thread.Sleep(milliSecond);
                }
                //Product Backlog Item 32044: End
                content.SpecRelations.ForEach(relation => { result = ImportRelationCore(relation); });
            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            string exchangeID = CommonUtility.GetConversationExchangeId(this.mReqIf);

            if (importSB.Length > 0)
            {
                try
                {
                    DebugLogger.LogError(importSB.ToString());
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(ex);
                }

                string fileName = $"{DateTime.Now.Ticks}.txt";
                string importLogPath = Path.Combine(Path.GetTempPath(), fileName);

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(importLogPath))
                {
                    file.WriteLine(importSB.ToString()); // 
                }
      

              result = new OperationResult(OperationStatusTypes.Success, exchangeID, fileName);
            }
            else
            {
                result = new OperationResult(OperationStatusTypes.Success, exchangeID);
            }


            //Generate Summary Report Function and calculate all of this.
            workItemsCountSummary.WorkItemsCountReport("Import");
            lngTicks = DateTime.Now.Ticks - lngTicks;
            DebugLogger.LogInfo("Time taken to get selected work items " + new DateTime(lngTicks).Minute + " minute " + new DateTime(lngTicks).Second + " second " + new DateTime(lngTicks).Millisecond + " millisecond.");
            DebugLogger.LogInfo("Import Process End:");
            DebugLogger.LogEnd("ReqIFImport", "ImportCore");

            return result;
        }


        private void GetAllMappedWorkItems()
        {
            DebugLogger.LogStart("ReqIFImport", "GetAllMappedWorkItems");
            this.mWorkitemRecord.Clear();
            int[] wrkItemIds = this.mWiToReqIfBindings.Values.ToList().Distinct().ToArray();

            List<WorkItem> workItems = (List<WorkItem>)this.mWorkItemTrackingHttpClient.GetWorkItemsAsyncCustom(wrkItemIds);

            workItems.ForEach(item =>
            {
                if (item != null)
                {
                    this.mWorkitemRecord.Add(item.Id.Value, item);
                }
            });
            DebugLogger.LogEnd("ReqIFImport", "GetAllMappedWorkItems");
        }

        private OperationResult ImportRelationCore(SpecRelation specRelation)
        {
            DebugLogger.LogStart("ReqIFImport", "ImportRelationCore");
            OperationResult result = null;
            string fromSpecObjectId = specRelation.Source.Identifier;
            string toSpecObjectId = specRelation.Target.Identifier;
            int fromWiId = 0, toWiId = 0;

            try
            {
                if (this.mWiToReqIfBindings.ContainsKey(fromSpecObjectId))
                {
                    fromWiId = this.mWiToReqIfBindings[fromSpecObjectId];
                }

                if (this.mWiToReqIfBindings.ContainsKey(toSpecObjectId))
                {
                    toWiId = this.mWiToReqIfBindings[toSpecObjectId];
                }

                if (!(fromWiId > 0 && toWiId > 0))
                {
                    return new OperationResult(OperationStatusTypes.Aborted);
                }

                LinkMap linkMap = this.mMappingTemplate.LinkMaps.Find(map => map.ReqIFRelationName == specRelation.Type.LongName);

                if (linkMap == null)
                {
                    return new OperationResult(OperationStatusTypes.Aborted, "Mapping not found");
                }

                WorkItem fromWorkItem = this.mWorkitemRecord[fromWiId];

                bool isAlreadyCreated = false;

                if (fromWorkItem.Relations != null)
                {
                    List<WorkItemRelation> relations = (from c in fromWorkItem.Relations
                                                        where c.Rel != "AttachedFile"
                                                        select c).ToList();

                    for (int i = 0; i < relations.Count; i++)
                    {
                        WorkItemRelation relatedLink = relations[i];

                        if (relatedLink != null)
                        {
                            if (relatedLink.RelatedWorkItemId() == toWiId &&
                                relatedLink.GetLinkName() == linkMap.WILinkName)
                            {
                                if (relatedLink.Attributes.ContainsKey("comment") &&
                                    relatedLink.Attributes["comment"] == specRelation.LongName)
                                {
                                    isAlreadyCreated = true;
                                    result = OperationResult.SuccessWithNoMessage;
                                    break;
                                }

                                relatedLink.Attributes["comment"] = specRelation.Identifier;

                                JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();
                                JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();
                                WorkItemRelation workItemRelation = new WorkItemRelation();

                                jsonPatchOperation.Operation = Operation.Add;
                                jsonPatchOperation.Path = "/relations/-";

                                workItemRelation.Attributes = new Dictionary<string, object>();
                                workItemRelation.Attributes.Add("comment", relatedLink.Attributes["comment"]);
                                workItemRelation.Attributes.Add("Name", relatedLink.GetLinkName());
                                workItemRelation.Rel = relatedLink.Rel;
                                workItemRelation.Url = relatedLink.Url;
                                jsonPatchOperation.Value = workItemRelation;

                                jsonPatchDocument.Add(jsonPatchOperation);

                                WorkItem workItem = this.mWorkItemTrackingHttpClient
                                    .UpdateWorkItemAsync(jsonPatchDocument, fromWorkItem.Id.Value).Result;
                                break;
                            }
                        }
                    }
                }

                if (!isAlreadyCreated)
                {
                    result = TfsUtility.AddLinkedWorkItem(this.mWorkItemTrackingHttpClient, fromWorkItem,
                        this.mRelationTypes, toWiId, linkMap.WILinkName, specRelation.Identifier);
                    //result = fromWorkItem.Links.AddLinkedWorkItem(toWiId, linkMap.WILinkName, specRelation.Identifier);
                }

            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                importSB.AppendLine(e.Message);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            DebugLogger.LogEnd("ReqIFImport", "GetAllMappedWorkItems");

            return result;
        }

        private OperationResult ImportSpecificationCore(Specification specification, List<string> traversedHierarchy)
        {
            DebugLogger.LogStart("ReqIFImport", "ImportSpecificationCore");
            OperationResult result = null;

            try
            {
                List<Task<OperationResult>> tasks = new List<Task<OperationResult>>();

                if (this.mLicenseType == LicenseType.TrialLicense && (specification.Children.Count > MvcApplication.Trial_License_Allowed_Count))
                {

                    mImportRecordsCurrentCounter = MvcApplication.Trial_License_Allowed_Count;
                    specification.Children.Take(MvcApplication.Trial_License_Allowed_Count).ForEach(hierarchy =>
                    {
                        Task<OperationResult> task = Task.Factory.StartNew(
                            () => ImportSpecHierarchyCore(hierarchy, traversedHierarchy), CancellationToken.None,
                            TaskCreationOptions.None, this.mImportTasksScheduler);

                        tasks.Add(task);
                    });

                }
                else
                {
                    mImportRecordsCurrentCounter = specification.Children.Count;
                    specification.Children.ForEach(hierarchy =>
                    {
                        //Task<OperationResult> task = Task.Factory.StartNew(
                        //    () => ImportSpecHierarchyCore(hierarchy, traversedHierarchy), CancellationToken.None,
                        //    TaskCreationOptions.None, this.mImportTasksScheduler);

                        //tasks.Add(task);

                        ImportSpecHierarchyCore(hierarchy, traversedHierarchy);
                    });

                }
                //Task.WaitAll(tasks.ToArray());


                result = OperationResult.SuccessWithNoMessage;

            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }

            DebugLogger.LogEnd("ReqIFImport", "ImportSpecificationCore");
            return result;
        }

        private OperationResult ImportSpecHierarchyCore(SpecHierarchy specHierarchy, List<string> traversedHierarchy, WorkItem parentWorkItem = null)
        {
            DebugLogger.LogStart("ReqIFImport", "ImportSpecHierarchyCore");
            SpecObject specObject = specHierarchy.Object;
            OperationResult result = this.ImportSpecObjectCore(specObject, parentWorkItem);
            WorkItem workItem = null;

            try
            {
                if (result.OperationStatus == OperationStatusTypes.Success)
                {
                    workItem = (WorkItem)result.Tag;

                    this.mWorkitemRecord[workItem.Id.Value] = workItem;

                    if (traversedHierarchy.Contains(specHierarchy.Identifier))
                    {
                        return new OperationResult(OperationStatusTypes.Success);
                    }

                    traversedHierarchy.Add(specHierarchy.Identifier);
                    List<Task<OperationResult>> tasks = new List<Task<OperationResult>>();

                    int remaining_Count = MvcApplication.Trial_License_Allowed_Count - mImportRecordsCurrentCounter;
                    if (this.mLicenseType == LicenseType.TrialLicense && (mImportRecordsCurrentCounter < MvcApplication.Trial_License_Allowed_Count) && (specHierarchy.Children.Count > remaining_Count))
                    {
                        specHierarchy.Children.Take(remaining_Count).ForEach(child =>
                            {
                                Task<OperationResult> task = Task.Factory.StartNew(
                                    () => ImportSpecHierarchyCore(child, traversedHierarchy, workItem), CancellationToken.None,
                                    TaskCreationOptions.None, this.mImportTasksScheduler);

                                tasks.Add(task);
                            });
                    }
                    else
                    {
                        specHierarchy.Children.ForEach(child =>
                        {
                            //Task<OperationResult> task = Task.Factory.StartNew(
                            //    () => ImportSpecHierarchyCore(child, traversedHierarchy, workItem), CancellationToken.None,
                            //    TaskCreationOptions.None, this.mImportTasksScheduler);

                            //tasks.Add(task);
                            ImportSpecHierarchyCore(child, traversedHierarchy, workItem);
                        });
                    }

                    //Task.WaitAll(tasks.ToArray());
                    result = OperationResult.SuccessWithNoMessage;
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            DebugLogger.LogEnd("ReqIFImport", "ImportSpecHierarchyCore");

            return result;
        }

        private OperationResult ImportSpecObjectCore(SpecObject specObject, WorkItem parentWorkItem = null)
        {
            DebugLogger.LogStart("ReqIFImport", "ImportSpecObjectCore");
            OperationResult result = null;
            string specType = specObject.Type.LongName;
            bool copyEmbObjectToAttachment = false;
            bool removeAttachmentsConfig = false;



            try
            {

                if (specType.EndsWith(" Type"))
                {
                    specType = specType.Replace(" Type", string.Empty);
                }

                TypeMap typeMap = this.mMappingTemplate.TypeMaps.Find(map => map.ReqIFTypeName == specType);

                if (typeMap == null)
                {
                    workItemsCountSummary.RevertedDueToMapping += 1;
                    importSB.AppendLine(specType + ":ReqIF type not found in mapping template.");
                    return new OperationResult(OperationStatusTypes.Aborted, "ReqIF type not found in mapping template.");
                }

            
                result = this.GetWorkItem(typeMap.WITypeName, specObject.Identifier);

                if (result.OperationStatus != OperationStatusTypes.Success)
                {
                    return result;
                }

                WorkItem workItem = (WorkItem)result.Tag;
                JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();
                string deleteCommentKey = specObject.Identifier + CONST_DELETE_ATTACHMENTS_KEY;


                removeAttachmentsConfig = ConfigurationManager.AppSettings.Get("ReqIFImport.RemoveAttachments") == "true" ? true : false;
                copyEmbObjectToAttachment = ConfigurationManager.AppSettings.Get("ReqIFImport.CopyEmbObjectToAttachment") == "true" ? true : false;

                DebugLogger.LogInfo("ReqIFImport.RemoveAttachments: " + removeAttachmentsConfig);
                DebugLogger.LogInfo("ReqIFImport.CopyEmbObjectToAttachment: " + copyEmbObjectToAttachment);

                if (removeAttachmentsConfig && copyEmbObjectToAttachment)
                {
                    if (workItem.Relations != null && workItem.Relations.Count > 0)
                    {

                        List<JsonPatchOperation> jsonPatchOperations = new List<JsonPatchOperation>();
                        int i = 0;
                        foreach (var item in workItem.Relations)
                        {
                            if (item.Rel.Equals(CONST_ATTACHMENT_FILE) && item.Attributes.ContainsKey("comment") && item.Attributes["comment"].Equals(specObject.Identifier))
                            {
                                JsonPatchOperation jsonPatchOperationForEmbeddedObjs = new JsonPatchOperation();
                                jsonPatchOperationForEmbeddedObjs.Operation = Operation.Replace;
                                jsonPatchOperationForEmbeddedObjs.Path = "/relations/" + i + "/attributes/comment";
                                jsonPatchOperationForEmbeddedObjs.Value = deleteCommentKey;
                                jsonPatchOperations.Add(jsonPatchOperationForEmbeddedObjs);

                            }
                            i++;
                        }

                        jsonPatchDocument.AddRange(jsonPatchOperations);

                    }
                }

                typeMap.EnumFieldMaps.ForEach(map =>
                {
                    AttributeValue attributeValue =
                        specObject.Values.Find(value => value.AttributeDefinition.LongName == map.ReqIFFieldName);

                    if (attributeValue == null || (attributeValue.ObjectValue is string && attributeValue.ObjectValue.ToString() == string.Empty))
                    {
                        if (!string.IsNullOrEmpty(map.ReqIfFieldNullThen))
                        {
                            AttributeValue altAttributeValue =
                                specObject.Values.Find(value => value.AttributeDefinition.LongName == map.ReqIfFieldNullThen);

                            if (altAttributeValue != null)
                            {
                                attributeValue = altAttributeValue;
                            }
                        }
                    }

                    if (attributeValue == null)
                    {
                        return;
                    }

                   
                        OperationResult operationResultForHtml = null;
                        if (!workItem.Fields.ContainsKey(map.WIFieldName) || workItem.Fields[map.WIFieldName].ToString() !=
                            attributeValue.ObjectValue.ToString())
                        {

                            string value = attributeValue.ObjectValue.ToString();
                            ReqIFSharp.AttributeValueEnumeration attributeValue1 = attributeValue as ReqIFSharp.AttributeValueEnumeration;

                            if (attributeValue1 != null && attributeValue1.Values.Count > 0)
                            {
                                List<string> allowedvalues = new List<string>();
                                for (int i = 0; i < attributeValue1.Values.Count; i++)
                                {
                                    allowedvalues.Add(attributeValue1.Values[i].LongName);
                                }
                                value = string.Join(";", allowedvalues.ToArray());
                            }
                            else
                            {
                                if(attributeValue1 is AttributeValueEnumeration)
                                {

                                    return;
                                }
                            }

                           workItem.Fields[map.WIFieldName] = HttpUtility.HtmlDecode(value.StripTagsRegex());

                            ReqIFSharp.AttributeValueXHTML attributeValueXHTML = attributeValue as ReqIFSharp.AttributeValueXHTML;

                            if (attributeValueXHTML != null)
                            {

                                workItem.Fields[map.WIFieldName] = "";                               
                                if (map.WIFieldName == "System.Title")
                                {
                                    workItem.Fields[map.WIFieldName] = HtmlParsingUtility.ConvertHTMLIntoString(value);
                                }
                                else {                                   
                                  
                                    if (value.Contains("&lt;") && value.Contains("&gt;") && value.Contains("<") && value.Contains(">"))
                                    {
                                          value = GetEncodedText(value);
                                    }

                                    if (this.mReqIf.TheHeader.SourceToolId == "Jama Connect")
                                    {
                                        if (!string.IsNullOrEmpty(value))
                                        {

                                      
                                        value = HtmlConverter.ConvertJamaToADO(value);
                                        }                                           
                                    }
                                    workItem.Fields[map.WIFieldName] = HttpUtility.HtmlDecode(value);
                                }  
                                //check if the objectValue contains html markups. 
                                if (map.WIFieldName != "System.Title" && markupIdentifiers.Any(x=> attributeValue.ObjectValue.ToString().Contains(x)) )
                                {
                                    string path = null;
                                    string redisKeyForConfigureMapping = mProjectGuid + CONST_REQIF_FILE_MAPPING_KEY;
                                    if (RedisHelper.KeyExist(redisKeyForConfigureMapping))
                                    {
                                        path = RedisHelper.LoadFromCache(redisKeyForConfigureMapping);
                                        if (!string.IsNullOrEmpty(path) && Path.GetExtension(path).Equals(".reqifz"))
                                        {
                                            var directory = Path.GetDirectoryName(path);
                                            var filename = Path.GetFileNameWithoutExtension(path);
                                            string dir = Path.Combine(directory, filename);

                                            string tempValue = value;

                                            operationResultForHtml = FindAndReplaceXHTMLObjectElement(attributeValueXHTML.ObjectValue.ToString(), dir, workItem.Id.Value);

                                            if (operationResultForHtml != null && operationResultForHtml.OperationStatus == OperationStatusTypes.Success)
                                            {
                                                var htmlObjectResult = (HtmlObjectResult)operationResultForHtml.Tag;
                                                value = htmlObjectResult.Html;
                                            }
                                            else
                                            {

                                                value = tempValue.Replace("<", "&").Replace(">", "&").Replace("&lt;","&");
                                                importSB.Append($"One or more attachment(s) are missing against WorkItem ID ({workItem.Id.Value}) in Field ({map.WIFieldName})");
                                            }
                                            workItem.Fields[map.WIFieldName] = HttpUtility.HtmlDecode(value);
                                        }
                                    }
                                }

                            }
                            
                           

                            ReqIFSharp.AttributeValueDate attributeValueDate = attributeValue as ReqIFSharp.AttributeValueDate;
                            if (attributeValueDate != null)
                            {
                                workItem.Fields[map.WIFieldName] = DateTime.Parse(attributeValueDate.ObjectValue.ToString());
                            }

                            JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();

                            jsonPatchOperation = new JsonPatchOperation()
                            {
                                Operation = Operation.Add,
                                Path = string.Format("/fields/{0}", map.WIFieldName),
                                Value = workItem.Fields[map.WIFieldName] ?? string.Empty
                            };

                            jsonPatchDocument.Add(jsonPatchOperation);

                            if (copyEmbObjectToAttachment && operationResultForHtml != null && operationResultForHtml.OperationStatus == OperationStatusTypes.Success )
                            {
                                var htmlObjectResult = (HtmlObjectResult)operationResultForHtml.Tag;
                                if (htmlObjectResult.attachmentFiles.Count > 0)
                                {
                                    List<JsonPatchOperation> jsonPatchOperations = new List<JsonPatchOperation>();
                                    foreach (KeyValuePair<string, string> kvp in htmlObjectResult.attachmentFiles)
                                    {
                                        JsonPatchOperation jsonPatchOperationForEmbeddedObjs = new JsonPatchOperation();
                                        WorkItemRelation workItemRelation = new WorkItemRelation();

                                        jsonPatchOperationForEmbeddedObjs.Operation = Operation.Add;
                                        jsonPatchOperationForEmbeddedObjs.Path = "/relations/-";

                                        workItemRelation.Attributes = new Dictionary<string, object>();
                                        workItemRelation.Attributes["comment"] = specObject.Identifier;
                                        workItemRelation.Attributes["name"] = kvp.Value;
                                        workItemRelation.Rel = "AttachedFile";
                                        workItemRelation.Url = kvp.Key;

                                        jsonPatchOperationForEmbeddedObjs.Value = workItemRelation;

                                        jsonPatchOperations.Add(jsonPatchOperationForEmbeddedObjs);
                                    }

                                    jsonPatchDocument.AddRange(jsonPatchOperations);
                                }
                            }
                        }
                        else
                        {
                            if (workItem.Fields[map.WIFieldName] != attributeValue.ObjectValue)
                            {

                                string value = attributeValue.ObjectValue.ToString();
                                ReqIFSharp.AttributeValueEnumeration attributeValue1 = attributeValue as ReqIFSharp.AttributeValueEnumeration;
                                if (attributeValue1 != null)
                                {
                                    value = attributeValue1.Values[0].LongName;
                                }
                                workItem.Fields[map.WIFieldName] =
                                    HttpUtility.HtmlDecode(value);

                                JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();

                                jsonPatchOperation = new JsonPatchOperation()
                                {
                                    Operation = Operation.Add,
                                    Path = string.Format("/fields/{0}", map.WIFieldName),
                                    Value = workItem.Fields[map.WIFieldName] ?? string.Empty
                                };

                                jsonPatchDocument.Add(jsonPatchOperation);
                            }
                        }
                    
                });
                bool isUpdateWorkItemSuccessful = true;
                if (jsonPatchDocument.Count > 0)
                {
                    WorkItem updated;
                    try
                    {
                        updated = this.mWorkItemTrackingHttpClient.UpdateWorkItemAsync(jsonPatchDocument, workItem.Id.Value).Result;
                        workItem = updated;
                        workItemsCountSummary.CreatedWorkItems += 1;

                    }
                    catch (Exception ex)
                    {
                        DebugLogger.LogError(ex);
                        isUpdateWorkItemSuccessful = false;

                        string titleText = string.Empty;

                        if (ex.InnerException.Message.Contains("'System.Title'")) {

                            foreach (var item in jsonPatchDocument)
                            {
                                if (item.Path == "/fields/System.Title")
                                {
                                    JsonPatchDocument jsonPatchDocument1 = new JsonPatchDocument();

                                    titleText = string.IsNullOrEmpty(item.Value.ToString()) ? "No Title - " + workItem.Id.Value : item.Value.ToString();
                                    importSB.AppendLine(string.Format("Unable to update work item ID: {0} due to the following error.", workItem.Id.Value));
                                    importSB.AppendLine(ex.InnerException.Message);

                                    if (ex.InnerException.Message.Contains("TF401324: Value for field 'System.Title' is too long."))
                                    {

                                        importSB.AppendLine("Creating title with first 250 characters.");
                                        titleText = item.Value.ToString().Substring(0, 250);
                                    }

                                    jsonPatchDocument1.Add(new JsonPatchOperation()
                                    {
                                        Operation = Operation.Add,
                                        Path = "/fields/System.Title",
                                        Value = titleText
                                    });

                                    updated = this.mWorkItemTrackingHttpClient.UpdateWorkItemAsync(jsonPatchDocument1, workItem.Id.Value).Result;
                                    workItem = updated;
                                    workItemsCountSummary.UpdatedWorkItems += 1;
                                    break;
                                }
                            }
                        }
                        if(string.IsNullOrWhiteSpace(titleText))
                        {
                            importSB.AppendLine(string.Format("Unable to update work item ID: {0} due to the following error.", workItem.Id.Value));
                            importSB.AppendLine(ex.InnerException.Message);
                        }


                    }

                    if (copyEmbObjectToAttachment && removeAttachmentsConfig)
                    {

                        JsonPatchDocument jsonPatchDocumentForRemoveAttachments = new JsonPatchDocument();
                        if (isUpdateWorkItemSuccessful && workItem.Relations != null)
                        {
                            List<JsonPatchOperation> jsonPatchOperationsForRemoveAttachments = new List<JsonPatchOperation>();

                            int i = 0;
                            foreach (var item in workItem.Relations)
                            {
                                if (item.Attributes.ContainsKey("comment") && item.Attributes["comment"].Equals(deleteCommentKey))
                                {
                                    JsonPatchOperation jsonPatchOperationForEmbeddedObjs = new JsonPatchOperation();
                                    WorkItemRelation workItemRelation = new WorkItemRelation();

                                    jsonPatchOperationForEmbeddedObjs = new JsonPatchOperation()
                                    {
                                        Operation = Operation.Remove,
                                        Path = string.Format($"/relations/{i}")
                                    };

                                    jsonPatchOperationsForRemoveAttachments.Add(jsonPatchOperationForEmbeddedObjs);
                                }
                                i++;
                            }

                            jsonPatchDocumentForRemoveAttachments.AddRange(jsonPatchOperationsForRemoveAttachments);


                        }
                        else
                        {
                            //  importSB.AppendLine($"Unable to remove Attachments for work item {workItem.Id.Value}");
                        }


                        if (jsonPatchDocumentForRemoveAttachments.Count > 0)
                        {
                            //WorkItem updated;
                            try
                            {
                                updated = this.mWorkItemTrackingHttpClient.UpdateWorkItemAsync(jsonPatchDocumentForRemoveAttachments, workItem.Id.Value).Result;
                                workItem = updated;
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError(ex);
                                importSB.AppendLine(string.Format("Unable to remove Attachments for the work item ID: {0} due to the following error.", workItem.Id.Value));
                                importSB.AppendLine(ex.InnerException.Message);
                            }
                        }
                    }



                    //if (updated != null)
                    //{
                    //    workItem = this.mWorkItemTrackingHttpClient
                    //        .GetWorkItemAsync(workItem.Id.Value, expand: WorkItemExpand.All).Result;
                    //}

                    result = new OperationResult(OperationStatusTypes.Success, workItem);
                }
                else
                {
                    result = new OperationResult(OperationStatusTypes.Success, workItem);
                }

                if (result.OperationStatus == OperationStatusTypes.Success)
                {
                    this.mWiToReqIfBindings[specObject.Identifier] = workItem.Id.Value;

                    if (parentWorkItem == null)
                    {
                        if (workItem.Relations != null)
                        {
                            List<WorkItemRelation> relations = (from c in workItem.Relations
                                                                where c.Rel != "AttachedFile"
                                                                select c).ToList();

                            for (int i = 0; i < relations.Count; i++)
                            {
                                WorkItemRelation link = relations[i];

                                if (link.GetLinkName() == WI_LINK_PARENT)
                                {
                                    result = TfsUtility.RemoveLinkedWorkItem(this.mWorkItemTrackingHttpClient, workItem,
                                        link);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        bool created = false;

                        if (workItem.Relations != null)
                        {
                            List<WorkItemRelation> relations = (from c in workItem.Relations
                                                                where c.Rel != "AttachedFile"
                                                                select c).ToList();

                            for (int i = 0; i < relations.Count; i++)
                            {
                                WorkItemRelation link = relations[i];

                                if (link.GetLinkName() == WI_LINK_PARENT)
                                {
                                    if (link.RelatedWorkItemId() == parentWorkItem.Id)
                                    {
                                        created = true;
                                        break;
                                    }

                                    result = TfsUtility.AddLinkedWorkItem(this.mWorkItemTrackingHttpClient, workItem,
                                        this.mRelationTypes,
                                        parentWorkItem.Id.Value, WI_LINK_RELATED, string.Empty);
                                    created = true;

                                    if (result.OperationStatus == OperationStatusTypes.Success)
                                    {
                                        workItem = (WorkItem)result.Tag;
                                    }
                                }
                            }
                        }

                        if (!created)
                        {
                            result = TfsUtility.AddLinkedWorkItem(this.mWorkItemTrackingHttpClient, workItem,
                                this.mRelationTypes,
                                parentWorkItem.Id.Value, WI_LINK_PARENT, string.Empty);

                            if (result.OperationStatus == OperationStatusTypes.Success)
                            {
                                workItem = (WorkItem)result.Tag;
                            }
                        }
                    }

                    result = new OperationResult(OperationStatusTypes.Success, workItem);
                }
            }
            catch (ArgumentException e)
            {
                DebugLogger.LogError(e);
                importSB.AppendLine(e.Message + "--" + e.InnerException.Message);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                importSB.AppendLine(e.Message + "--" + e.InnerException.Message);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }

            DebugLogger.LogEnd("ReqIFImport", "ImportSpecObjectCore");
            return result;
        }



        private string UploadImageIntoADO(string imgPath,string fileName)
        {
            DebugLogger.LogStart("ReqIFImport", "UploadImageIntoADO");
            try
            {
                // if the path is same, concurrent thread can not use it.
                lock(StringLocker.GetLockObject(imgPath))
                {
                    using (FileStream fs = File.Open(imgPath, FileMode.Open))
                    {

                        //var filename = Path.GetFileName(imgPath);
                        var result = UploadFileToTfsServer(fs, fileName);
                        if (result.OperationStatus.Equals(OperationStatusTypes.Success))
                        {
                            var uri = (string)result.Tag;
                            //string a = @"&lt;img src=&quot;"+uri+"; alt=Image&gt;&lt";
                            //string a = "<img src="+uri+" width="+70+" height="++70" />";
                            //string a = $"<img src=\"{ uri}\" width=\"{300}\" height=\"{300}\" />";

                            //string a = @"<xhtml:object data = " + uri + " type = \"image/png\" />";

                            return uri;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("ReqIFImport", "UploadImageIntoADO");
            return null;
        }

        private bool ValidateFile(string absoluteFilePath)
        {
            DebugLogger.LogStart("ReqIFImport", "ValidateFile");
            try
            {
                if (string.IsNullOrWhiteSpace(absoluteFilePath) || absoluteFilePath.Length <=0)
                    throw new ArgumentNullException();

                if (File.Exists(absoluteFilePath))
                {
                    return true;
                }

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("ReqIFImport", "ValidateFile");

            return false;
        }

        private bool ValidateFiles(List<HtmlAttributeValues> links, string baseDir)
        {
            DebugLogger.LogStart("ReqIFImport", "ValidateFiles");
            bool result = false;
            try
            {
                foreach (var item in links)
                {
                    if (string.IsNullOrEmpty(item.Src))
                    {
                        throw new ArgumentNullException();
                    }

                    string absoluteFileURI = Path.Combine(baseDir, item.Src);

                    if (!ValidateFile(absoluteFileURI))
                    {
                        importSB.AppendLine(Environment.NewLine + item.Src + " file is not found in the provided ReqIF package.");
                        return true;
                    }
                }


                DebugLogger.LogEnd("ReqIFImport", "ValidateFiles");

                return result;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("ReqIFImport", "ValidateFiles");

            return false;
        }

        private OperationResult FindAndReplaceXHTMLObjectElement(string htmlElement, string dir, int workItemId)
        {
            DebugLogger.LogStart("ReqIFImport", "FindAndReplaceXHTMLObjectElement");
            OperationResult result = new OperationResult(OperationStatusTypes.Failed);
            try
            {
                if (string.IsNullOrWhiteSpace(htmlElement))
                    throw new ArgumentNullException();

                string tag = "img";
                string encTag = "//img";
                string attribute = "src";

                Dictionary<string, string> attachmentFilesDictionary = new Dictionary<string, string>();

                // Check for <reqif-xhtml:object> tag
                if (htmlElement.Contains("reqif-xhtml:object"))
                {
                    htmlElement = HtmlConverter.ConvertJamaToADO(htmlElement);
                }
                
                
                if (htmlElement.Contains("xhtml:object"))
                {
                    encTag = tag = "xhtml:object";
                    attribute = "data";
                }
                else if (htmlElement.Contains("object data"))
                {
                    encTag = tag = "object";
                    attribute = "data";
                }

                htmlElement = HtmlParsingUtility.ConvertUnicodeToHTML(htmlElement);

                List<HtmlAttributeValues> links = HtmlParsingUtility.GetAllObjectValues(htmlElement, tag, attribute, this.mReqIf);

                foreach (var item in links)
                {
                    if (string.IsNullOrEmpty(item.Name))
                    {
                        if (!string.IsNullOrEmpty(item.Type))
                        {
                            string splitForExt = item.Type.ToString().Substring(item.Type.LastIndexOf('/') + 1);
                            if (!string.IsNullOrEmpty(splitForExt))
                            {
                                item.Name = CommonUtility.GenerateRandomName(8) + "." + splitForExt;
                            }
                        }
                        else
                        {
                            item.Name = CommonUtility.GenerateRandomName(8);
                        }
                    }
                }

                List<HtmlAttributeValues> imgLinks = new List<HtmlAttributeValues>();

                if (ValidateFiles(links, dir))
                {
                    return result;
                }

                foreach (var link in links)
                {
                    string adoUrls = string.Empty;
                    string absoluteFileURI = Path.Combine(dir, link.Src);

                    var htmlAttributeValue = new HtmlAttributeValues();

                    adoUrls = UploadImageIntoADO(absoluteFileURI, link.Name);

                    htmlAttributeValue = new HtmlAttributeValues
                    {
                        Src = adoUrls,
                        Type = link.Type,
                        Name = link.Name,
                    };

                    imgLinks.Add(htmlAttributeValue);

                    // Add urls and name in dictionary
                    if (adoUrls != link.Src)
                    {
                        attachmentFilesDictionary.Add(adoUrls, link.Name);
                    }
                    else
                    {
                        return result;
                    }
                }

                string updatedHtml = HtmlParsingUtility.ReplaceImagesLink(htmlElement, imgLinks, encTag, attribute);

                if (!HtmlParsingUtility.IsXhtml(htmlElement))
                {
                    updatedHtml = HtmlParsingUtility.ConvertHTMLToUnicode(updatedHtml);
                }

                // add global object for workitemid and dictionary
                return new OperationResult(OperationStatusTypes.Success, new HtmlObjectResult
                {
                    Html = updatedHtml,
                    attachmentFiles = attachmentFilesDictionary,
                    WorkItemId = workItemId
                });

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                return result;
            }
        }



        private OperationResult GetWorkItem(string typeName, string reqIFId)
        {
            DebugLogger.LogStart("ReqIFImport", "GetWorkItem");
            OperationResult result = null;
            WorkItem workItem = null;

            try
            {
                if (this.mWiToReqIfBindings.ContainsKey(reqIFId))
                {
                    if (this.mWorkitemRecord.ContainsKey(this.mWiToReqIfBindings[reqIFId]))
                    {
                        workItem = this.mWorkitemRecord[this.mWiToReqIfBindings[reqIFId]];
                    }
                    else
                    {
                        this.mWiToReqIfBindings.Remove(reqIFId);
                    }

                    //if (workItem == null)
                    //{
                    //    workItem = this.mWorkItemTrackingHttpClient.GetWorkItemAsync(this.mWiToReqIfBindings[reqIFId], expand: WorkItemExpand.All).Result;
                    //}
                }

                if (workItem == null)
                {
                    JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();

                    jsonPatchDocument.Add(new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = "No Title " + reqIFId,
                        From = null
                    });

                    try
                    {
                        workItem = this.mWorkItemTrackingHttpClient.CreateWorkItemAsync(jsonPatchDocument, this.mProjectGuid, typeName, bypassRules:false).Result;
                    }
                    catch (AggregateException ex)
                    {
                        importSB.AppendLine(ex.InnerException.Message ?? ex.Message);
                        DebugLogger.LogError(ex);
                    }
                    catch (Exception ex)
                    {

                        importSB.AppendLine("Unsuccessful import of object "+ reqIFId + " as "+ typeName + " , due to mandatory field(s) conflict.");
                        DebugLogger.LogError(ex);
                    }



                    if (workItem == null)
                    {
                        result = new OperationResult(OperationStatusTypes.Failed, CommonUtility.WI_CREATION_FAILED_MSG);
                    }
                }

                if (workItem != null)
                {
                    result = new OperationResult(OperationStatusTypes.Success, workItem);
                }
            }
            catch (Exception e)
            {
                importSB.AppendLine(e.Message);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
                DebugLogger.LogError(e);
            }
            DebugLogger.LogEnd("ReqIFImport", "GetWorkItem");

            return result;
        }

        /// <summary>
        /// Gets the specification.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// No core content found.
        /// or
        /// Targeted Specification '{this.mTargetedReqIFSpec}' not found.
        /// </exception>
        private Specification GetSpecification()
        {
            DebugLogger.LogStart("ReqIFImport", "GetSpecification");
            ReqIFContent content = this.mReqIf.CoreContent;

            if (content == null)
            {
                throw new InvalidOperationException($"No core content found.");
            }

            if (string.IsNullOrEmpty(this.mTargetedReqIFSpec))
            {
                return content.Specifications.FirstOrDefault();
            }

            Specification retVal =
                content.Specifications.Find(specification => specification.LongName == this.mTargetedReqIFSpec);

            if (retVal == null)
            {
                throw new InvalidOperationException($"Targeted Specification '{this.mTargetedReqIFSpec}' not found.");
            }
            DebugLogger.LogEnd("ReqIFImport", "GetSpecification");

            return retVal;
        }


        private string GetEncodedText(string value)
        {

            DebugLogger.LogStart("ReqIFImport", "GetEncodedText");
            string encodedValue = string.Empty;

            try 
            { 
                encodedValue = value.Replace("&lt;", "&amp;lt;").Replace("&gt;", "&amp;gt;");

            }
            catch(Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("ReqIFImport", "GetEncodedText");
            return encodedValue;
        }

    }
}
