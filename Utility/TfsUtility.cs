using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;


namespace ReqIFBridge.Utility
{
    internal class TfsUtility
    {
        public static OperationResult AddLinkedWorkItem(WorkItemTrackingHttpClient workItemTrackingHttpClient, WorkItem workItem, IDictionary<string, WorkItemRelationType> relationTypes, int linkedWorkItemId, string linkType, string comments)
        {
            OperationResult result = new OperationResult(OperationStatusTypes.Failed);

            try
            {
                string url = workItem.Url;

                url = url.Substring(0, url.LastIndexOf("/", StringComparison.CurrentCulture));
                url = string.Format("{0}/{1}", url, linkedWorkItemId);

                JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();
                JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();
                WorkItemRelation workItemRelation = new WorkItemRelation();

                jsonPatchOperation.Operation = Operation.Add;
                jsonPatchOperation.Path = "/relations/-";

                workItemRelation.Attributes = new Dictionary<string, object>();
                workItemRelation.Attributes.Add("comment", comments);
                workItemRelation.Rel = relationTypes[linkType].ReferenceName;
                workItemRelation.Title = linkType;
                workItemRelation.Url = url;
                jsonPatchOperation.Value = workItemRelation;

                jsonPatchDocument.Add(jsonPatchOperation);

                // Validate work Item; exception will occur if have any issue.
                //WorkItem workItem = workItemTrackingHttpClient.UpdateWorkItemAsync(jsonPatchDocument, this.mWorkItem.Id.Value, true).Result;

                // Update work Item.
                WorkItem updated = workItemTrackingHttpClient.UpdateWorkItemAsync(jsonPatchDocument, workItem.Id.Value).Result;
                
                result = new OperationResult(OperationStatusTypes.Success, updated);
            }
            catch (Exception err)
            {
                result = new OperationResult(OperationStatusTypes.Failed, null, err);
            }

            return result;
        }

        public static OperationResult RemoveLinkedWorkItem(WorkItemTrackingHttpClient workItemTrackingHttpClient, WorkItem workItem, WorkItemRelation linkToRemove)
        {
            OperationResult result = null;

            int indexToRemove = -1;

            if (workItem.Relations == null)
            {
                return new OperationResult(OperationStatusTypes.Aborted, "No relations found");
            }

            try
            {
                List<WorkItemRelation> relations = (from c in workItem.Relations
                    where c.Rel != "AttachedFile"
                    select c).ToList();

                for (int i = 0; i < relations.Count; i++)
                {
                    if (
                        string.Compare(relations[i].Rel, linkToRemove.Rel,
                            StringComparison.OrdinalIgnoreCase) == 0
                        &&
                        string.Compare(relations[i].Url, linkToRemove.Url,
                            StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        indexToRemove = i;
                        break;
                    }
                }

                if (indexToRemove != -1)
                {
                    JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();
                    JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();

                    jsonPatchOperation.Operation = Operation.Remove;
                    jsonPatchOperation.Path = "/relations/" + indexToRemove;

                    jsonPatchDocument.Add(jsonPatchOperation);

                    try
                    {
                        // Update work item.
                        WorkItem updated = workItemTrackingHttpClient
                            .UpdateWorkItemAsync(jsonPatchDocument, workItem.Id.Value).Result;
                    }
                    catch (Exception err)
                    {
                        result = new OperationResult(OperationStatusTypes.Failed, null, err);
                    }
                }
            }
            catch (Exception err)
            {
                result = new OperationResult(OperationStatusTypes.Failed, null, err);
            }

            return result;
        }
    }
}