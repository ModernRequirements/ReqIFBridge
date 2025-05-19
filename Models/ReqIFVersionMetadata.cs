using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Runtime.Serialization;

namespace ReqIFBridge.Models
{
    [DataContract]
    [BsonIgnoreExtraElements]
    public class ReqIFVersionMetadata
    {
        [DataMember]
        [BsonElement("version")]
        public string Version { get; set; }

        [DataMember]
        [BsonElement("timestamp")]       
        public string Timestamp { get; set; }

        [DataMember]
        [BsonElement("importedBy")]
        public string ImportedBy { get; set; }

        [DataMember]
        [BsonElement("comment")]
        public string Comment { get; set; }


        [DataMember]
        [BsonElement("errorLog")]
        public string ErrorLog { get; set; }


        [DataMember]
        [BsonElement("sourceFilename")]
        public string SourceFilename { get; set; }

        [DataMember]
        [BsonElement("specificationId")]
        public string SpecificationId { get; set; }

       
    }
}
