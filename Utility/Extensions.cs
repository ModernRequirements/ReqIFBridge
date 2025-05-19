using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace ReqIFBridge.Utility
{
    internal static class Extensions
    {
        public static List<WorkItem> GetWorkItemsAsyncCustom(this WorkItemTrackingHttpClient workItemTrackingHttpClient, IEnumerable<int> ids, WorkItemExpand? expand = WorkItemExpand.All, DateTime? asofDateTime = null)
        {
            Task<WorkItem> taskWorkItem = null;
            List<WorkItem> retWorkItems = new List<WorkItem>();
            Dictionary<int, WorkItem> getttedWorkItems = new Dictionary<int, WorkItem>();
            int smallBatchCount = 0;
            List<int> smallBatch = new List<int>();
            List<Task> lstTasks = new List<Task>();


            if (ids.Count() == 0)
            {
                return retWorkItems;
            }

            try
            {
                //Getting work items in a batch of 200 due to VSTS WebAPI limitation
                //https://www.visualstudio.com/en-us/docs/integrate/api/wit/work-items

                //if (ids.LongCount() > 200)
                {
                    int i = 0;
                    foreach (int id in ids)
                    {
                        smallBatch.Add(id);
                        smallBatchCount++;

                        if (smallBatchCount == 200 || (i + 1) == ids.Count())
                        {
                            Task<List<WorkItem>> batchWorkItemTask = workItemTrackingHttpClient.GetWorkItemsAsync(smallBatch, asOf: asofDateTime, expand: expand, errorPolicy: WorkItemErrorPolicy.Omit);

                            lstTasks.Add(batchWorkItemTask);

                            smallBatch.Clear();
                            smallBatchCount = 0;

                        }
                        i = i + 1;

                    }

                    try
                    {
                        Task.WaitAll(lstTasks.ToArray());

                        lstTasks.ForEach(task =>
                        {
                            if (task.Status == TaskStatus.Faulted)
                            {
                                return;
                            }

                            retWorkItems.AddRange(((Task<List<WorkItem>>)task).Result);
                        });
                    }
                    catch (AggregateException err)
                    {
                        
                    }

                }

            }
            catch (AggregateException err)
            {
                
            }

            return retWorkItems;

            #region Previous work
            //foreach (int id in ids)
            //{
            //    taskWorkItem = workItemTrackingHttpClient.GetWorkItemAsync(id, expand: expand);

            //    try
            //    {
            //        WorkItem gettedWorkItem = taskWorkItem.Result;
            //        getttedWorkItems.Add(gettedWorkItem.Id.Value, gettedWorkItem);
            //    }
            //    catch (Exception err)
            //    {
            //        Helper.Log(LogLevel.Error, err);
            //    }
            //}

            //foreach (int id in ids)
            //{
            //    if (getttedWorkItems.ContainsKey(id))
            //    {
            //        retWorkItems.Add(getttedWorkItems[id]);
            //    }
            //    else
            //    {
            //        retWorkItems.Add(null);
            //    }
            //}

            //return retWorkItems;
            #endregion

        }

        public static int RelatedWorkItemId(this WorkItemRelation workItemRelation)
        {
            int retVal = -1;
            string subStr = workItemRelation.Url.Substring(workItemRelation.Url.LastIndexOf("/", StringComparison.CurrentCulture) + 1);

            int.TryParse(subStr, out retVal);

            return retVal;
        }

        public static string ReverseName(this WorkItemRelation workItemRelation, IDictionary<string, WorkItemRelationType> relationTypes)
        {
            string reverseName = string.Empty;

            if (workItemRelation.Title == null)
            {
                foreach (string key in relationTypes.Keys)
                {
                    if (
                        string.Compare(relationTypes[key].ReferenceName, workItemRelation.Rel,
                            StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string relInternalName = relationTypes[key].ReferenceName;
                        string[] splittedRelationName = relInternalName.Split('-');

                        if (splittedRelationName.Length == 1)
                        {
                            reverseName = key;
                        }
                        else
                        {
                            string direction =
                                string.Compare(splittedRelationName[1], "forward", StringComparison.OrdinalIgnoreCase) ==
                                0
                                    ? "Reverse"
                                    : "Forward";
                            string valueToFind = string.Format("{0}-{1}", splittedRelationName[0], direction);

                            foreach (string key2 in relationTypes.Keys)
                            {
                                if (
                                    string.Compare(relationTypes[key2].ReferenceName, valueToFind,
                                        StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    reverseName = key2;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            return reverseName;
        }

        /// <summary>
        /// Gets the name of the link.
        /// </summary>
        /// <param name="workItemRelation">The work item relation.</param>
        /// <returns></returns>
        public static string GetLinkName(this WorkItemRelation workItemRelation)
        {
            // Bug Fixing 30023 - Start
            string linkName = string.Empty;

            try
            {
                linkName = (workItemRelation.Attributes["name"] ?? string.Empty).ToString();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            // Bug Fixing 30023 - End
            return linkName;
        }
    }
}