using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReqIFBridge.Models
{
    public class WorkItemInfo
    {
        public int ID { get; set; }

        public string IconUrl { get; set; }
        public string Title { get; set; }
        public string WorkItemType { get; set; }
        public string AssignedTo { get; set; } 
        public string State { get; set; }
        public string Statecolor { get; set; }
    }
}