using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace ReqIFBridge.Models
{
    [DataContract]
    [BsonIgnoreExtraElements]
    public class ReqIFBindingInfo
    {

        [DataMember]
        [BsonElement("specObjectId")]
        [BsonRequired]
        public string SpecObjectId { get; set; }

        [DataMember]
        [BsonElement("adoWorkItemId")]
        [BsonRequired]
        public int AdoWorkItemId { get; set; }



    }

   
}