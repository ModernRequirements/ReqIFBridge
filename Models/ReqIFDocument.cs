using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace ReqIFBridge.Models
{
    [DataContract]
    [BsonIgnoreExtraElements]
    public class ReqIFDocument
    {

     
        [BsonId]
        [DataMember(Name = "id")]
        public string Id { get; set; }

    
        [DataMember(Name = "collectionId")]
        public string CollectionId { get; set; }


        [BsonElement("projectId")]        
        public string ProjectId { get; set; }
  

        [BsonElement("importedTool")]       
        public string ImportedTool { get; set; }


        [BsonElement("exchangeId")]       
        public string ExchangeId { get; set; }


        [BsonElement("reqIfXml")]       
        public string ReqIfXml { get; set; }

      
        [BsonElement("mappingTemplateXml")]
        public string MappingTemplateXml { get; set; }   
      

        [BsonElement("createdDate")]
        public string CreatedDate { get; set; }


        [BsonElement("lastModifiedDate")]   
        public string LastModifiedDate { get; set; }


        [BsonElement("reqIFBindingInfos")]
        public Dictionary<string, int> ReqIFBindingInfos = new Dictionary<string, int>();

        [BsonElement("reqIFVersionMetadata")]
        public List<ReqIFVersionMetadata> VersionHistory { get; set; }

    }
}
