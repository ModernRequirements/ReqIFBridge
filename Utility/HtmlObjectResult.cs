using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReqIFBridge.Utility
{
    public class HtmlObjectResult
    {
        public string Html { get; set; }
        public int WorkItemId { get; set; }
        public Dictionary<string, string> attachmentFiles = 
            new Dictionary<string, string>();
    }
}