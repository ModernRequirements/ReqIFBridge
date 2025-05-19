using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReqIFBridge.Utility
{
    public class WorkItemsCountSummary
    {
        public int TotalWorkItems { get; set; } = 0;
        public int UpdatedWorkItems { get; set; } = 0;
        public int CreatedWorkItems { get; set; } = 0;
        public int RevertedDueToMapping { get; set; } = 0;



        //Possible values for importOrExport Parameters are Import or Export
        public void WorkItemsCountReport(string importOrExport)
        {
            DebugLogger.LogInfo($"Total Number of WorkItems to be {importOrExport}ed: {TotalWorkItems}");
            DebugLogger.LogInfo($"Total Number of WorkItems in {importOrExport} Excluded Due to Mapping: {RevertedDueToMapping}");
            DebugLogger.LogInfo($"Total Number of WorkItems in {importOrExport} to be Processed: {TotalWorkItems - RevertedDueToMapping}");
            DebugLogger.LogInfo($"Total Number of WorkItems in {importOrExport} Created Successfully: {CreatedWorkItems}");

            if (importOrExport.ToLower().Equals("import")) {
                int diff = (TotalWorkItems - RevertedDueToMapping) - CreatedWorkItems;
                DebugLogger.LogInfo($"Total Number of WorkItems in Import Created with Some Errors: " + diff);
                    }
            else
            {
                DebugLogger.LogInfo($"Total Number of WorkItems in Export unable to create: {UpdatedWorkItems}");
            }

        }
    }

}




 