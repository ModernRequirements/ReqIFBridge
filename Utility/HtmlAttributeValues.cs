using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReqIFBridge.Utility
{
    public class HtmlAttributeValues
    {
        public string Src { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }

        public bool ValidateSrcUrl { get; set; } = false;
    }
}