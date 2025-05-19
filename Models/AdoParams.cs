using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ReqIFBridge.Models
{
    [Serializable]
    [DataContract(Name = "adoParams", Namespace = "")]
    public class AdoParams
    {
        [DataMember(Name = "accessToken")]
        public string AccessToken { get; set; }

        [DataMember(Name = "collectionId")]
        public string CollectionId { get; set; }

        [DataMember(Name = "collectionName")]
        public string CollectionName { get; set; }

        [DataMember(Name = "projectId")]
        public string ProjectId { get; set; }

        [DataMember(Name = "serverUri")]
        public string ServerUri { get; set; }

        [DataMember(Name = "userName")]
        public string UserName { get; set; }


        [DataMember(Name = "accountName")]
        public string AccountName { get; set; }


        [DataMember(Name = "projectName")]
        public string ProjectName { get; set; }

        [DataMember(Name = "workItemIds")]
        public int[] WorkItemIds { get; set; }

        [DataMember(Name = "query")]
        public string Query { get; set; }
    }
}