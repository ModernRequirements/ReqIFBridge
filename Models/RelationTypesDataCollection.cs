using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ReqIFBridge.Models
{
    public class RelationTypesDataCollection
    {
        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "ReferenceName")]
        public string ReferenceName { get; set; }

        [DataMember(Name = "Id")]
        public int Id { get; set; }

        [DataMember(Name = "LinkType")]
        public string LinkType { get; set; }

        [DataMember(Name = "IsForwardLink")]
        public bool IsForwardLink { get; set; }

        [DataMember(Name = "OppositeLinkName")]
        public string OppositeLinkName { get; set; }       
       
    }
}