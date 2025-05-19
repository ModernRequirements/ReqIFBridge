using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ReqIFBridge.Models
{
    public class WorkItemFieldsDataCollection
    {

        [DataMember(Name = "allowedValues")]
        public object[] allowedValues { get; set; }

        [DataMember(Name = "isVisibleOnUI")]
        public bool isVisibleOnUI { get; set; }

        [DataMember(Name = "name")]
        public string name { get; set; }

        [DataMember(Name = "referenceName")]
        public string referenceName { get; set; }

        [DataMember(Name = "type")]
        public string type { get; set; }

        [DataMember(Name = "defaultValue")]
        public string defaultValue { get; set; }

        [DataMember(Name = "alwaysRequired")]
        public bool alwaysRequired { get; set; }



    }
}