using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReqIFBridge.Utility
{
    public class AttachmentInfo
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public int WorkitemId { get; set; }
        public object FileBlob { get; set; }
        public string FileId { get; set; }
        public Uri Uri { get; set; }
        public string Tag { get; set; }
        public byte[] FileContent { get; set; }
        public string Base64String { get; set; }
    }
}