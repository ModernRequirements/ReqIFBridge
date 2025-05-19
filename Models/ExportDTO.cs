using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReqIFBridge.Models
{
    public class ExportDTO
    {
        public string ExportType { get; set; }
        public string Specification_Title { get; set; }
        public string ReqIF_FileName { get; set; }
        public string Exchange_Id { get; set; }
    }

}