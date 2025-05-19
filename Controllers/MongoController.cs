using ReqIFBridge.Models;
using ReqIFBridge.ReqIF.ReqIFMapper;
using ReqIFBridge.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;

namespace ReqIFBridge.Controllers
{
    
    public class MongoController : Controller
    {
        private readonly ReqIFRepository _reqIFRepository;
        private readonly MongoDBService _mongoDBService;

        public MongoController()
        {
            _mongoDBService = new MongoDBService();
            _reqIFRepository = new ReqIFRepository(_mongoDBService);
        }


        public async Task<ActionResult> GetExchangeIdDetailsByProject(string projectId)
        {
            try
            {
                var documents = await _reqIFRepository.GetExchangeIdByProjectAsync(projectId);

                // Transform the data to include only ExchangeId and the last SourceFilename
                var result = documents.Select(doc => new
                {
                    ExchangeId = doc.ExchangeId,
                    SourceFilename = doc.VersionHistory?.LastOrDefault()?.SourceFilename, // Get the last SourceFilename
                    ReqIFBindingInfos = doc.ReqIFBindingInfos // Include binding info for validation
                }).ToList();

                return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        public ReqIFDocument GetReqIFByExchangeId(string exchangeId)
        {
            try
            {
                ReqIFDocument documents =  _reqIFRepository.GetReqIFByExchangeIdAsync(exchangeId).Result;
                return documents;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                return null;
            }
        }



        public async Task<ActionResult> SaveReqIF([FromBody] SaveReqIFDTO saveReqIFDTO)
        {
            try
            {
                // Validate user authentication
                if (LicenseController.LicenseInformation == null)
                {
                    DebugLogger.LogInfo("Session has been expired.");
                    LicenseController licenseController = new LicenseController();
                    JsonResult jsonResult = licenseController.ValidateLicense(saveReqIFDTO.model);
                }

                // Validate input parameters
                if (saveReqIFDTO.model == null || string.IsNullOrEmpty(saveReqIFDTO.reqIF_FilePath) ||
                    string.IsNullOrEmpty(saveReqIFDTO.model.CollectionId) || string.IsNullOrEmpty(saveReqIFDTO.model.ProjectId))
                {
                    string errorMessage = saveReqIFDTO.model == null || string.IsNullOrEmpty(saveReqIFDTO.reqIF_FilePath)
                        ? "Model or ReqIF path cannot be null"
                        : string.IsNullOrEmpty(saveReqIFDTO.model.CollectionId)
                            ? "Collection ID cannot be null"
                            : "Project ID cannot be null";

                    return Json(new { success = false, message = errorMessage });
                }

                string reqIFMappingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Templates/{saveReqIFDTO.model.ProjectId}.xml");
                string reqIFBindingPath = HttpContext.Server.MapPath($"~/App_Data/ReqIF_Bindings/{saveReqIFDTO.model.ProjectId}-binding.json");

                // Check that both paths are not null or empty
                if (string.IsNullOrEmpty(reqIFMappingPath) || string.IsNullOrEmpty(reqIFBindingPath))
                {
                    return Json(new { success = false, message = "Mapping or Binding path cannot be null" });
                }

                saveReqIFDTO.reqIF_MappingTemplatePath = reqIFMappingPath;
                saveReqIFDTO.reqIF_BindingPath = reqIFBindingPath;
               

                await _reqIFRepository.SaveReqIFAsync(saveReqIFDTO);
                return Json(new { success = true, message = "Document saved successfully" });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                return Json(new { success = false, message = ex.Message });
            }
        }


        public async Task<ActionResult> DeleteReqIF(string id)
        {
            try
            {
                await _reqIFRepository.DeleteReqIFAsync(id);
                return Json(new { success = true, message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

}