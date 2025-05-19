using MongoDB.Bson; // Needed for ObjectId
using MongoDB.Driver;
using ReqIFBridge.Models;
using ReqIFBridge.ReqIF.ReqIFMapper;
using ReqIFBridge.Utility;
using ReqIFSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;

public class ReqIFRepository
{
    private readonly IMongoCollection<ReqIFDocument> _reqIFCollection;

    public ReqIFRepository(MongoDBService mongoDBService)
    {
        try
        {
            DebugLogger.LogStart("ReqIFRepository", "Constructor");
            if (mongoDBService == null)
            {
                throw new ArgumentNullException(nameof(mongoDBService));
            }
            _reqIFCollection = mongoDBService.GetCollection<ReqIFDocument>("ReqIFDocuments");
            DebugLogger.LogInfo("ReqIF collection initialized successfully");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(ex);
            throw;
        }
        finally
        {
            DebugLogger.LogEnd("ReqIFRepository", "Constructor");
        }
    }

    public async Task SaveReqIFAsync(SaveReqIFDTO saveReqIFDTO)
    {
        try
        {
            DebugLogger.LogStart("ReqIFRepository", "SaveReqIFAsync");

            // Validate input
            if (saveReqIFDTO == null || saveReqIFDTO.model == null)
            {
                throw new ArgumentNullException("saveReqIFDTO", "SaveReqIFDTO or its model cannot be null");
            }

            string currentReIFSpecification = string.Empty;

            List<string> lstSpecObjectIdentifier = new List<string>(); 

            // Create new ReqIF document
            ReqIFDocument document = new ReqIFDocument
            {
                Id = Guid.NewGuid().ToString(),
                ProjectId = saveReqIFDTO.model.ProjectId,
                CollectionId = saveReqIFDTO.model.CollectionId,
                CreatedDate = DateTime.UtcNow.ToString("o"),
                LastModifiedDate = DateTime.UtcNow.ToString("o"),
               
            };

            // Load and validate ReqIF file
            ReqIFDeserializer deserializer = new ReqIFDeserializer();
            ReqIFSharp.ReqIF reqIF_File = deserializer.Deserialize(saveReqIFDTO.reqIF_FilePath).FirstOrDefault();
            if (reqIF_File != null)
            {
                Specification specification = reqIF_File.CoreContent.Specifications.FirstOrDefault();
                if (specification != null && !string.IsNullOrEmpty(specification.Identifier))
                {
                    currentReIFSpecification = specification.Identifier;
                }

                if (!string.IsNullOrEmpty(reqIF_File.TheHeader.SourceToolId))
                {
                    document.ImportedTool = reqIF_File.TheHeader.SourceToolId;
                }


                document.ExchangeId = CommonUtility.GetConversationExchangeId(reqIF_File);


                //list out the identifier of all SPEC-OBJECT 
                var specObjects = reqIF_File.CoreContent.SpecObjects;
                foreach (var specObject in specObjects)
                {
                    lstSpecObjectIdentifier.Add(specObject.Identifier);
                }

            }

            ReqIFSerializer serializer = new ReqIFSerializer();         

            using (var memoryStream = new MemoryStream())
            {
                // Corrected method call to match the expected parameter type
                serializer.Serialize(new List<ReqIFSharp.ReqIF> { reqIF_File }, memoryStream, SupportedFileExtensionKind.Reqif);
                memoryStream.Position = 0; // Reset the stream position to the beginning
                using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
                {
                    string reqIFAsString = reader.ReadToEnd(); // Convert the serialized XML to a string
                    // Now you can store reqIFAsString in ReqIFDocument
                    document.ReqIfXml = reqIFAsString;
                }
            }


            // Load mapping template
            if (!string.IsNullOrEmpty(saveReqIFDTO.reqIF_MappingTemplatePath))
            {
                ReqIFMappingTemplate mappingTemplate = CommonUtility.LoadMapping(saveReqIFDTO.reqIF_MappingTemplatePath);
                if (mappingTemplate != null)
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ReqIFMappingTemplate));
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var writer = new StreamWriter(memoryStream, Encoding.UTF8))
                        {
                            xmlSerializer.Serialize(writer, mappingTemplate);
                            writer.Flush();
                            memoryStream.Position = 0; // Reset the stream position to the beginning
                            using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
                            {
                                string mappingTemplateAsString = reader.ReadToEnd(); // Convert the serialized XML to a string
                                                                                     // Now you can store mappingTemplateAsString in ReqIFDocument
                                document.MappingTemplateXml = mappingTemplateAsString;
                            }
                        }
                    }
                }
            }



            // Create version metadata

            ReqIFVersionMetadata versionMetadata = new ReqIFVersionMetadata();

            versionMetadata.Version = "1.0";
            versionMetadata.Timestamp = DateTime.UtcNow.ToString("o");
            versionMetadata.SpecificationId = currentReIFSpecification;
            versionMetadata.ImportedBy = saveReqIFDTO.model.UserName;
            versionMetadata.SourceFilename = saveReqIFDTO.reqIF_FileName;
            versionMetadata.Comment = "Initial import";


            if (!string.IsNullOrEmpty(saveReqIFDTO.logFileName))
            {

                string importLogPath = Path.Combine(Path.GetTempPath(), saveReqIFDTO.logFileName);
                using (var reader = new StreamReader(importLogPath))
                {
                    string errorLog = reader.ReadToEnd();
                    versionMetadata.ErrorLog = errorLog;
                }
            }



            List<ReqIFVersionMetadata> versionHistory = new List<ReqIFVersionMetadata>();
            versionHistory.Add(versionMetadata);

            document.VersionHistory = versionHistory;


            // Load binding information
            if (!string.IsNullOrEmpty(saveReqIFDTO.reqIF_BindingPath))
            {
                Dictionary<string, int> binding = CommonUtility.GetBinding(saveReqIFDTO.reqIF_BindingPath);
                if (binding != null)
                {
                    // Create a new dictionary that contains only the keys in lstSpecObjectIdentifier.
                    Dictionary<string, int> newBinding = lstSpecObjectIdentifier
                        .ToDictionary(specId => specId, specId => binding.ContainsKey(specId) ? binding[specId] : 0);

                    document.ReqIFBindingInfos = newBinding;
                }
            }



            // Check if document already exists for this project and exchange ID
            var existingDoc = await _reqIFCollection
                .Find(doc => doc.ExchangeId == document.ExchangeId &&
                             doc.ProjectId == document.ProjectId)
                .FirstOrDefaultAsync();

            // In SaveReqIFAsync method, before updating the existing document:
            if (existingDoc != null)
            {
                DebugLogger.LogInfo($"Found existing ReqIF document with ID: {existingDoc.Id}");

                // Preserve the original ID and creation date
                document.Id = existingDoc.Id;
                document.CreatedDate = existingDoc.CreatedDate;

                // Get the latest version metadata
                var latestVersion = existingDoc.VersionHistory
                    .OrderByDescending(v => Version.Parse(v.Version))
                    .FirstOrDefault();

                if (latestVersion != null)
                {
                    DebugLogger.LogInfo($"Current latest version: {latestVersion.Version}");

                    // Parse the current version and increment it
                    Version currentVersion = Version.Parse(latestVersion.Version);
                    string newVersion = new Version(currentVersion.Major, currentVersion.Minor + 1).ToString();

                    // Update the version metadata for the new entry
                    versionMetadata.Version = newVersion;
                    versionMetadata.Timestamp = DateTime.UtcNow.ToString("o");
                    versionMetadata.SpecificationId = currentReIFSpecification;
                    versionMetadata.ImportedBy = saveReqIFDTO.model.UserName;
                    versionMetadata.SourceFilename = saveReqIFDTO.reqIF_FileName;
                 

                    versionMetadata.ErrorLog = latestVersion.ErrorLog; // Keep the error log from the latest version
                    versionMetadata.Comment = $"Update version {newVersion}"; // You might want to make this configurable

                    // Combine existing version history with new version
                    document.VersionHistory = new List<ReqIFVersionMetadata>();
                    document.VersionHistory.AddRange(existingDoc.VersionHistory); // Keep existing history
                    document.VersionHistory.Add(versionMetadata); // Add new version

                    DebugLogger.LogInfo($"Added new version {newVersion} to version history");
                }
                else
                {
                    DebugLogger.LogWarn("No existing version history found, starting with version 1.0");
                    document.VersionHistory = new List<ReqIFVersionMetadata> { versionMetadata };
                }

                // Update the document
                await _reqIFCollection.ReplaceOneAsync(
                    doc => doc.Id == existingDoc.Id,
                    document
                );

                DebugLogger.LogInfo($"Updated existing ReqIF document for project {document.ProjectId} with new version {versionMetadata.Version}");
            }
            else
            {
             
                await _reqIFCollection.InsertOneAsync(document);
                DebugLogger.LogInfo($"Inserted new ReqIF document for project {document.ProjectId} with initial version 1.0");
            }


            DebugLogger.LogEnd("ReqIFRepository", "SaveReqIFAsync");
        }
        catch (Exception ex)
        {
            DebugLogger.LogError(ex);
       
        }
    }

    public async Task<ReqIFDocument> GetReqIFByExchangeIdAsync(string exchangeId)
    {
        try
        {
            DebugLogger.LogStart("ReqIFRepository", "GetReqIFByExchangeIdAsync");

            if (string.IsNullOrEmpty(exchangeId))
            {
                throw new ArgumentNullException(nameof(exchangeId));
            }

            DebugLogger.LogInfo($"Searching for ReqIF document with exchangeId: {exchangeId}");
            var document = await _reqIFCollection
                .Find(doc => doc.ExchangeId == exchangeId)
                .FirstOrDefaultAsync();

            if (document != null)
            {
                DebugLogger.LogInfo($"Found ReqIF document with exchangeId: {exchangeId}");
            }
            else
            {
                DebugLogger.LogInfo($"No ReqIF document found with exchangeId: {exchangeId}");
            }

            return document;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"Error retrieving ReqIF document with exchangeId {exchangeId}: {ex.Message}");
            throw;
        }
        finally
        {
            DebugLogger.LogEnd("ReqIFRepository", "GetReqIFByExchangeIdAsync");
        }
    }

    public async Task<List<ReqIFDocument>> GetExchangeIdByProjectAsync(string projectId)
    {
        try
        {
            DebugLogger.LogStart("ReqIFRepository", "GetExchangeIdByProjectAsync");

            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            DebugLogger.LogInfo($"Retrieving ReqIF documents for project: {projectId}");

            var projection = Builders<ReqIFDocument>.Projection
                .Include(x => x.ExchangeId)
                .Include(x => x.VersionHistory)
                .Include(x => x.ReqIFBindingInfos)
                .Exclude(x => x.Id);

            var documents = await _reqIFCollection
                .Find(doc => doc.ProjectId == projectId)
                .Project<ReqIFDocument>(projection)
                .ToListAsync();

            DebugLogger.LogInfo($"Found {documents.Count} ReqIF documents for project {projectId}");
            return documents;
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"Error retrieving ReqIF documents for project {projectId}: {ex.Message}");
            throw;
        }
        finally
        {
            DebugLogger.LogEnd("ReqIFRepository", "GetExchangeIdByProjectAsync");
        }
    }

    public async Task DeleteReqIFAsync(string id)
    {
        try
        {
            DebugLogger.LogStart("ReqIFRepository", "DeleteReqIFAsync");

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            DebugLogger.LogInfo($"Attempting to delete ReqIF document with ID: {id}");

            var result = await _reqIFCollection.DeleteOneAsync(doc => doc.Id == id);

            if (result.DeletedCount > 0)
            {
                DebugLogger.LogInfo($"Successfully deleted ReqIF document with ID: {id}");
            }
            else
            {
                DebugLogger.LogInfo($"No ReqIF document found to delete with ID: {id}");
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogError($"Error deleting ReqIF document with ID {id}: {ex.Message}");
            throw;
        }
        finally
        {
            DebugLogger.LogEnd("ReqIFRepository", "DeleteReqIFAsync");
        }
    }

}
