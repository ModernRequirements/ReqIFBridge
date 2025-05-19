// ReqIFBridge/Models/WorkItemComparisonResult.cs
using System.Collections.Generic;
using System.Linq;

public class WorkItemComparisonResult
{
    public List<int> MissingInBinding { get; set; } = new List<int>();
    public List<int> MissingInWorkItems { get; set; } = new List<int>();
    public bool HasDiscrepancies => MissingInBinding.Any() || MissingInWorkItems.Any();
    public string Message { get; set; }
}
public class CompareWorkItemsRequest
{
    public int[] WorkItemIds { get; set; }
    public Dictionary<string, int> BindingInfos { get; set; }
}
