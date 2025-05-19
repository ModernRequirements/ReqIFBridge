using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using ReqIFBridge.Models;
using ReqIFBridge.ReqIF.ReqIFMapper;
using ReqIFBridge.Utility;
using ReqIFSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ReqIFBridge.ReqIF
{
    public class ReqIFExport
    {
        const string WI_LINK_PARENT = "Parent";
        const string WI_LINK_CHILD = "Child";

        private readonly WorkItemTrackingHttpClient mWorkItemTrackingHttpClient = null;
        private readonly ReqIFMappingTemplate mMappingTemplate = null;
        private readonly ReqIFSharp.ReqIF mReqIf = null;
        private readonly int[] mWorkItemsToExport = null;
        private readonly IDictionary<string, WorkItemRelationType> mRelationTypes = null;
        private readonly string reqIfPath = string.Empty;
        private Dictionary<string, int> mWiToReqIfBindings = null;
        private Dictionary<int, WorkItem> mWorkitemRecord = new Dictionary<int, WorkItem>();
        private string mTargetedReqIFSpec = string.Empty;
        StringBuilder importSB = new StringBuilder();
        private WorkItemsCountSummary workItemsCountSummary;
        private readonly Dictionary<string, List<string>> _workItemFieldEnumDataCollection;
        private ExportDTO mExportDTO = new ExportDTO();
      


        private readonly string ExportAttachment = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReqIFExport"/> class.
        /// </summary>
        /// <param name="reqIf">The req if.</param>
        /// <param name="wiProject">The wi project.</param>
        /// <param name="mappingTemplate">The mapping template.</param>
        /// <param name="wiToReqIfBindings">The wi to req if bindings.</param>
        /// <param name="workItemsToExport">The work items to export.</param>
        /// <param name="targetedReqIFSpec">The targeted req if spec.</param>
        public ReqIFExport(ReqIFSharp.ReqIF reqIf, WorkItemTrackingHttpClient workItemTrackingHttpClient, ReqIFMappingTemplate mappingTemplate, Dictionary<string, int> wiToReqIfBindings, int[] workItemsToExport, IDictionary<string, WorkItemRelationType> relationTypes, string reqIfPath, ExportDTO exportDto, Dictionary<string, List<string>> workItemFieldEnumDataCollection, string targetedReqIFSpec = null)
        {
            DebugLogger.LogStart("ReqIFExport", "ReqIFExport()");
            
            this.mReqIf = reqIf;
            this.mWorkItemTrackingHttpClient = workItemTrackingHttpClient;
            this.mMappingTemplate = mappingTemplate;
            this.mWiToReqIfBindings = wiToReqIfBindings;
            this.mWorkItemsToExport = workItemsToExport;
            this.mRelationTypes = relationTypes;
            this.reqIfPath = reqIfPath;
            this.mExportDTO = exportDto;
            this._workItemFieldEnumDataCollection = workItemFieldEnumDataCollection;
            

            if (!string.IsNullOrEmpty(targetedReqIFSpec))
            {
                this.mTargetedReqIFSpec = targetedReqIFSpec;
            }

            if (string.IsNullOrEmpty(this.mExportDTO.Specification_Title))
            {
                this.mExportDTO.Specification_Title = this.mExportDTO.ReqIF_FileName;
            }

            this.mReqIf = this.mReqIf == null ? GenerateReqIFObject() : GenerateRoundTripReqIFObject();


            DebugLogger.LogEnd("ReqIFExport", "ReqIFExport()");
        }

        private ReqIFSharp.ReqIF GenerateRoundTripReqIFObject()
        {
            DebugLogger.LogStart("ReqIFExport", "GenerateRoundTripReqIFObject");          



            // Creating ReqIF Header.
            ReqIFHeader reqIFHeader = new ReqIFHeader()
            {
                Identifier = $"_{Guid.NewGuid()}",
                ReqIFToolId = "ReqIF4DevOps" + " " + CommonUtility.GetAssemblyVersion(),
                ReqIFVersion = "1.0",
                SourceToolId = this.mExportDTO.ExportType,
                CreationTime = DateTime.Now,
                Title = this.mExportDTO.Specification_Title,

            };
            //get the first object of specification
            Specification spec = this.mReqIf.CoreContent.Specifications.FirstOrDefault();

            spec.LastChange = DateTime.Now;
            spec.Children.Clear();

         


            this.mReqIf.TheHeader = reqIFHeader;
           
            this.mReqIf.CoreContent.SpecObjects.Clear();
            this.mReqIf.CoreContent.SpecRelations.Clear();
            this.mReqIf.CoreContent.SpecRelationGroups.Clear();

        

            DebugLogger.LogEnd("ReqIFExport", "GenerateRoundTripReqIFObject");

            return this.mReqIf;
        }

        /// <summary>
        /// Generates the req if object.
        /// </summary>
        /// <returns></returns>
        private ReqIFSharp.ReqIF GenerateReqIFObject()
        {
            DebugLogger.LogStart("ReqIFExport", "GenerateReqIFObject");
            ReqIFSharp.ReqIF newReqIf = new ReqIFSharp.ReqIF();
            ReqIFContent reqIfContent = new ReqIFContent();
            reqIfContent.DocumentRoot = newReqIf;

            // Creating ReqIF Object Types from mapping template.
            foreach (TypeMap typeMap in mMappingTemplate.TypeMaps)
            {
                var attributeDefinitions = new List<AttributeDefinition>();

                // ReqIF Content is passed in the constructor, so Spec Type does not need to be explicitly added.
                var specObjectType = new SpecObjectType
                {
                    Identifier = $"_{Guid.NewGuid()}",
                    LongName = $"{typeMap.ReqIFTypeName}"
                };

                foreach (EnumFieldMap fieldMap in typeMap.EnumFieldMaps)
                {
                    AttributeDefinition attributeDefinition = GetAttributeDefinition(fieldMap, reqIfContent, specObjectType);
                    if (attributeDefinition != null)
                    {
                        attributeDefinitions.Add(attributeDefinition);
                    }
                }

                specObjectType.SpecAttributes.AddRange(attributeDefinitions);
                reqIfContent.SpecTypes.Add(specObjectType);
            }

            // Creating ReqIF Link Types from mapping template.
            foreach (LinkMap linkMap in this.mMappingTemplate.LinkMaps)
            {
                List<AttributeDefinition> attributeDefinitions = new List<AttributeDefinition>();

                // ReqIF Content is passed in the constructor, so Spec Type does not need to be explicitly added.
                SpecRelationType specRelationType = new SpecRelationType()
                {
                    Identifier = $"_{Guid.NewGuid()}",
                    LongName = linkMap.ReqIFRelationName,
                    ReqIFContent = reqIfContent
                };

                foreach (FieldMap fieldMap in linkMap.FieldMaps)
                {
                    AttributeDefinition attributeDefinition = GetAttributeDefinition(fieldMap, reqIfContent, specRelationType);
                    attributeDefinitions.Add(attributeDefinition);
                }

                specRelationType.SpecAttributes.AddRange(attributeDefinitions);

                reqIfContent.SpecTypes.Add(specRelationType);
            }


            // Creating ReqIF Specification Type from mapping template

            // Retrieve the String datatype definition
            DatatypeDefinitionString stringDataType = GetDataTypeDefinition(reqIfContent, FieldTypes.String)
                as DatatypeDefinitionString;

            if (stringDataType == null)
            {
                throw new InvalidOperationException("String datatype definition could not be retrieved.");
            }

            // Generate the Specification Type using the retrieved datatype
            SpecificationType specificationType = GetSpecificationType(reqIfContent, stringDataType);

            // Add the generated Specification Type to the SpecTypes collection
            reqIfContent.SpecTypes.Add(specificationType);


          
            // Creating ReqIF Header.
            ReqIFHeader reqIFHeader = new ReqIFHeader()
            {
                Identifier = $"_{Guid.NewGuid()}",
                ReqIFToolId = "ReqIF4DevOps" + " " + CommonUtility.GetAssemblyVersion(),
                ReqIFVersion = "1.0",
                SourceToolId = this.mExportDTO.ExportType,
                CreationTime = DateTime.Now,
                Title = this.mExportDTO.Specification_Title,
                
            };

            newReqIf.TheHeader = reqIFHeader;

            // Create ReqIF Specification.
            Specification spec = new Specification()
            {
                Identifier = $"_{Guid.NewGuid()}",
                LongName = this.mExportDTO.Specification_Title,
                ReqIFContent = reqIfContent,
                Type = (SpecificationType)reqIfContent.SpecTypes.Find(specType => specType is SpecificationType)
            };

            AttributeValue attributeValue = new AttributeValueString()
            {
                ObjectValue = this.mExportDTO.Specification_Title,
                Definition = (AttributeDefinitionString)spec.Type.SpecAttributes.Find(specAttr => specAttr.LongName == "Description"),
                SpecElAt = spec,
                TheValue = this.mExportDTO.Specification_Title
            };

            spec.Values.Add(attributeValue);

            reqIfContent.Specifications.Add(spec);

            newReqIf.CoreContent = reqIfContent;
            DebugLogger.LogEnd("ReqIFExport", "GenerateReqIFObject");

            return newReqIf;
        }        

        /// <summary>
        /// Gets the type of the specification.
        /// </summary>
        /// <param name="reqIfContent">Content of the req if.</param>
        /// <param name="stringDatatypeDefinition">The string datatype definition.</param>
        /// <returns></returns>
        private SpecificationType GetSpecificationType(ReqIFContent reqIfContent, DatatypeDefinitionString stringDatatypeDefinition)
        {
            DebugLogger.LogStart("ReqIFExport", "GetSpecificationType");
            SpecificationType specificationType = new SpecificationType()
            {
                ReqIFContent = reqIfContent,
                Identifier = $"_{Guid.NewGuid()}",
                LongName = "Specification Type"
            };

            AttributeDefinitionString attributeDefinitionString = new AttributeDefinitionString()
            {
                Identifier = $"_{Guid.NewGuid()}",
                LongName = "Description",
                Type = stringDatatypeDefinition,
                SpecType = specificationType,
            };

            specificationType.SpecAttributes.Add(attributeDefinitionString);
            DebugLogger.LogEnd("ReqIFExport", "GetSpecificationType");

            return specificationType;
        }

        /// <summary>
        /// Gets the attribute definition.
        /// </summary>
        /// <param name="fieldMap">The field map.</param>
        /// <param name="reqIfContent">Content of the req if.</param>
        /// <param name="specType">Type of the spec.</param>
        /// <returns></returns>
        private AttributeDefinition GetAttributeDefinition(FieldMap fieldMap, ReqIFContent reqIfContent, SpecType specType)
        {
            DebugLogger.LogStart("ReqIFExport", "GetAttributeDefinition");
            AttributeDefinition attributeDefinition = null;

            switch (fieldMap.FieldType)
            {
                case FieldTypes.Numeric:
                    attributeDefinition = new AttributeDefinitionInteger()
                    {
                        Type = GetDataTypeDefinition(reqIfContent, FieldTypes.Numeric) as DatatypeDefinitionInteger
                    };
                    break;
                case FieldTypes.String:
                    attributeDefinition = new AttributeDefinitionString()
                    {
                        Type = GetDataTypeDefinition(reqIfContent, FieldTypes.String) as DatatypeDefinitionString
                    };
                    break;
                case FieldTypes.DateTime:
                    attributeDefinition = new AttributeDefinitionDate()
                    {
                        Type = GetDataTypeDefinition(reqIfContent, FieldTypes.DateTime) as DatatypeDefinitionDate
                    };
                    break;
                case FieldTypes.RichText:
                    attributeDefinition = new AttributeDefinitionXHTML()
                    {
                        Type = GetDataTypeDefinition(reqIfContent, FieldTypes.RichText) as DatatypeDefinitionXHTML
                    };
                    break;
                case FieldTypes.Enum:
                    EnumFieldMap enumFieldMap = fieldMap as EnumFieldMap;
                    if (enumFieldMap == null)
                    {
                        throw new InvalidCastException("FieldMap cannot be cast to EnumFieldMap.");
                    }

                    DatatypeDefinitionEnumeration enumDataTypeDefinition = reqIfContent.DataTypes
                        .OfType<DatatypeDefinitionEnumeration>()
                        .FirstOrDefault(d => d.LongName == enumFieldMap.WIFieldName);

                    if (enumDataTypeDefinition == null)
                    {
                        enumDataTypeDefinition = new DatatypeDefinitionEnumeration
                        {
                            Description = $"{enumFieldMap.ReqIFFieldName}",
                            Identifier = $"_{Guid.NewGuid()}",
                            ReqIFContent = reqIfContent,
                            LongName = $"{enumFieldMap.ReqIFFieldName}",
                            LastChange = DateTime.UtcNow
                        };

                        if (_workItemFieldEnumDataCollection.TryGetValue(enumFieldMap.WIFieldName, out var specifiedValues))
                        {
                            int keyCounter = 1;
                            foreach (var value in specifiedValues)
                            {
                                EnumValue enumValue = new EnumValue
                                {
                                    Identifier = $"_{Guid.NewGuid()}",
                                    LastChange = DateTime.UtcNow,
                                    LongName = value,
                                    Description = value,

                                    Properties = new EmbeddedValue
                                    {
                                        Key = keyCounter++,
                                        OtherContent = string.Empty
                                    }
                                };
                                enumDataTypeDefinition.SpecifiedValues.Add(enumValue);
                            }
                        }


                        reqIfContent.DataTypes.Add(enumDataTypeDefinition);
                    }

                    attributeDefinition = new AttributeDefinitionEnumeration
                    {
                        Type = enumDataTypeDefinition,
                        Identifier = $"_{Guid.NewGuid()}",
                        LongName = fieldMap.ReqIFFieldName,
                        SpecType = specType
                    };
                    break;
            }

            if (attributeDefinition != null)
            {
                attributeDefinition.Identifier = $"_{Guid.NewGuid()}";
                attributeDefinition.LongName = fieldMap.ReqIFFieldName;
                attributeDefinition.SpecType = specType;
            }
            DebugLogger.LogEnd("ReqIFExport", "GetAttributeDefinition");

            return attributeDefinition;
        }

        /// <summary>
        /// Gets the data type definition.
        /// </summary>
        /// <param name="reqIfContent">Content of the req if.</param>
        /// <param name="fieldType">Type of the field.</param>
        /// <returns></returns>
        private DatatypeDefinition GetDataTypeDefinition(ReqIFContent reqIfContent, FieldTypes fieldType)
        {
            DebugLogger.LogStart("ReqIFExport", "GetDataTypeDefinition");
            DatatypeDefinition datatypeDefinition = null;

            if (fieldType == FieldTypes.String)
            {
                datatypeDefinition = reqIfContent.DataTypes.Find(definition => definition is DatatypeDefinitionString);

                if (datatypeDefinition == null)
                {
                    datatypeDefinition = new DatatypeDefinitionString()
                    {
                        Description = "Text Field",
                        Identifier = $"_{Guid.NewGuid()}",
                        ReqIFContent = reqIfContent,
                        LongName = "STRING"
                        
                    };

                    reqIfContent.DataTypes.Add(datatypeDefinition);
                }
            }
            else if (fieldType == FieldTypes.DateTime)
            {
                datatypeDefinition = reqIfContent.DataTypes.Find(definition => definition is DatatypeDefinitionDate);

                if (datatypeDefinition == null)
                {
                    datatypeDefinition = new DatatypeDefinitionDate()
                    {
                        Description = "Date",
                        Identifier = $"_{Guid.NewGuid()}",
                        ReqIFContent = reqIfContent,                        
                        LongName = "DATE"
                        
                    };

                    reqIfContent.DataTypes.Add(datatypeDefinition);
                }
            }
            else if (fieldType == FieldTypes.Numeric)
            {
                datatypeDefinition = reqIfContent.DataTypes.Find(definition => definition is DatatypeDefinitionInteger);

                if (datatypeDefinition == null)
                {
                    datatypeDefinition = new DatatypeDefinitionInteger()
                    {
                        Description = "Integer",
                        Identifier = $"_{Guid.NewGuid()}",
                        ReqIFContent = reqIfContent,
                        LongName = "INTEGER"                     
                       
                    };

                    reqIfContent.DataTypes.Add(datatypeDefinition);
                }
            }
            else if (fieldType == FieldTypes.RichText)
            {
                datatypeDefinition = reqIfContent.DataTypes.Find(definition => definition is DatatypeDefinitionXHTML);

                if (datatypeDefinition == null)
                {
                    datatypeDefinition = new DatatypeDefinitionXHTML()
                    {
                        Description = "Rich Text",
                        Identifier = $"_{Guid.NewGuid()}",
                        ReqIFContent = reqIfContent,                       
                        LongName = "TEXT"
                    };

                    reqIfContent.DataTypes.Add(datatypeDefinition);
                }
            }
            DebugLogger.LogEnd("ReqIFExport", "GetDataTypeDefinition");

            return datatypeDefinition;
        }

        /// <summary>
        /// Exports this instance.
        /// </summary>
        /// <returns></returns>
        public OperationResult Export()
        {
            return this.ExportCore();
        }

        /// <summary>
        /// Exports the core.
        /// </summary>
        /// <returns></returns>
        private OperationResult ExportCore()
        {
            DebugLogger.LogStart("ReqIFExport", "ExportCore");
            importSB.Clear();
            workItemsCountSummary = new WorkItemsCountSummary();
            DebugLogger.LogInfo("Export Process Start:");

            OperationResult result = this.GetAllWorkItems();

            var mappingDetails = this.mMappingTemplate.TypeMaps.SelectMany(x =>
            {
                var lst = new List<string>();
                string mappingInfo = $"{x.WITypeName} : {string.Join(",", x.EnumFieldMaps.Select(y => y.WIFieldName))}";
                lst.Add(mappingInfo);
                return lst;
            });

            DebugLogger.LogInfo($"Template Mapping Detail -> {string.Join(Environment.NewLine, mappingDetails)}");


            if (result.OperationStatus == OperationStatusTypes.Success)
            {
                List<WorkItem> workitemCount = result.Tag as List<WorkItem>;
                workItemsCountSummary.TotalWorkItems = workitemCount.Count;
                try
                {
                    List<Tuple<WorkItem, WorkItem, string>> referenceLinks =
                        new List<Tuple<WorkItem, WorkItem, string>>();
                    Tree<WorkItem> tree = new Tree<WorkItem>();

                    this.BuildTree(tree, referenceLinks);

                    result = ExportTreeToReqIF(tree);

                    result = this.CreateReferenceLinks(referenceLinks);

                    if (result.OperationStatus == OperationStatusTypes.Success)
                    {
                        // Validate the ReqIF content before proceeding
                        //if (ValidateReqIFSchema(out List<string> validationErrors))
                        //{
                        //    DebugLogger.LogInfo("ReqIF content is valid against the schema.");
                        //}
                        //else
                        //{
                        //    string errorMessage = $"ReqIF Validation Failed: {string.Join("; ", validationErrors)}";
                        //    importSB.AppendLine(errorMessage);
                        //    DebugLogger.LogError(errorMessage);
                        //    return new OperationResult(OperationStatusTypes.Failed, errorMessage);
                        //}

                        result = new OperationResult(OperationStatusTypes.Success, this.mReqIf);
                    }

                    var mReqIFCount = this.mReqIf.CoreContent.SpecObjects.Count;
                    workItemsCountSummary.RevertedDueToMapping = workitemCount.Count - mReqIFCount;


                    if (workitemCount.Count != mReqIFCount)
                    {
                        importSB.Append("Some of the Work Items are unable to export. ");
                    }
                }
                catch (Exception e)
                {
                    importSB.Append(e.Message == null ? e?.InnerException?.Message : e.Message);
                    DebugLogger.LogError(e);
                    result = new OperationResult(OperationStatusTypes.Failed, null, e);
                }
            }

            if (importSB.Length > 0)
            {
                try
                {
                    DebugLogger.LogError(importSB.ToString());
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError(ex);
                }

                string fileName = $"{this.mExportDTO.Specification_Title}.txt";
                string importLogPath = Path.Combine(Path.GetTempPath(), fileName);

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(importLogPath))
                {
                    file.WriteLine(importSB.ToString());
                }

                result = new OperationResult(OperationStatusTypes.Success, this.mReqIf, fileName);
            }
            workItemsCountSummary.WorkItemsCountReport("Export");

            DebugLogger.LogEnd("ReqIFExport", "ExportCore");
            return result;
        }

        /// <summary>
        /// Creates the reference links.
        /// </summary>
        /// <param name="referenceLinks">The reference links.</param>
        /// <returns></returns>
        private OperationResult CreateReferenceLinks(List<Tuple<WorkItem, WorkItem, string>> referenceLinks)
        {
            DebugLogger.LogStart("ReqIFExport", "CreateReferenceLinks");
            OperationResult result = null;

            try
            {
                ReqIFContent content = this.mReqIf.CoreContent;
                Specification specification = this.GetSpecification();

                foreach (Tuple<WorkItem, WorkItem, string> referenceLink in referenceLinks)
                {
                    WorkItem fromWorkItem = referenceLink.Item1;
                    WorkItem toWorkItem = referenceLink.Item2;

                    string fromReqIFId = string.Empty;
                    string toReqIFId = string.Empty;

                    foreach (KeyValuePair<string, int> binding in this.mWiToReqIfBindings)
                    {
                        if (binding.Value == fromWorkItem.Id)
                        {
                            fromReqIFId = binding.Key;
                        }
                        else if (binding.Value == toWorkItem.Id)
                        {
                            toReqIFId = binding.Key;
                        }

                        if (fromReqIFId != string.Empty && toReqIFId != string.Empty)
                        {
                            break;
                        }
                    }

                    if (fromReqIFId == string.Empty || toReqIFId == string.Empty)
                    {
                        continue;
                    }
                    SpecHierarchy fromSpecHierarchy = specification.FindSpecObject(fromReqIFId);
                    SpecHierarchy toSpecHierarchy = specification.FindSpecObject(toReqIFId);

                    if (fromSpecHierarchy == null || toSpecHierarchy == null)
                    {
                        continue;
                    }

                    SpecType specType = content.SpecTypes.Find(type => type.LongName == referenceLink.Item3);

                    if (specType == null)
                    {
                        continue;
                    }

                    SpecRelation specRelation = content.SpecRelations.Find(relation =>
                        relation.SpecType.LongName == referenceLink.Item3 &&
                        relation.Source.Identifier == fromReqIFId && relation.Target.Identifier == toReqIFId);

                    if (specRelation != null)
                    {
                        continue;
                    }

                    specRelation = new SpecRelation()
                    {
                        Identifier = $"_{Guid.NewGuid()}",
                        SpecType = specType,
                        ReqIFContent = content,
                        Source = fromSpecHierarchy.Object,
                        Target = toSpecHierarchy.Object,
                    };

                    content.SpecRelations.Add(specRelation);
                }

                result = OperationResult.SuccessWithNoMessage;
            }
            catch (Exception e)
            {
                importSB.AppendLine(e.Message == null ? e?.InnerException?.Message : e.Message);
                DebugLogger.LogError(e);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            DebugLogger.LogEnd("ReqIFExport", "CreateReferenceLinks");
            return result;
        }

        /// <summary>
        /// Exports the tree to req if.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <returns></returns>
        private OperationResult ExportTreeToReqIF(Tree<WorkItem> tree)
        {
            DebugLogger.LogStart("ReqIFExport", "ExportTreeToReqIF");
            OperationResult result = null;

            try
            {
                foreach (TreeNode<WorkItem> rootNode in tree.RootNodes)
                {
                    result = ExportTreeNode(rootNode);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                importSB.Append(e.Message == null ? e?.InnerException?.Message : e.Message);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            DebugLogger.LogEnd("ReqIFExport", "ExportTreeToReqIF");

            return result;
        }

        /// <summary>
        /// Exports the tree node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="parentSpecHierarchy">The parent spec hierarchy.</param>
        /// <returns></returns>
        private OperationResult ExportTreeNode(TreeNode<WorkItem> node, SpecHierarchy parentSpecHierarchy = null)
        {
            DebugLogger.LogStart("ReqIFExport", "ExportTreeNode");
            OperationResult result = null;

            try
            {
                WorkItem workItem = node.Data;
                string bindedSpecObjId = this.GetBindedSpecObjId(workItem.Id.Value);
                var content = this.mReqIf.CoreContent;
                SpecHierarchy specHierarchy = null;

                if (content.Specifications.Count == 0)
                {
                    return new OperationResult(OperationStatusTypes.Failed, "No specification found.");
                }

                Specification specification = this.GetSpecification();

                if (bindedSpecObjId == string.Empty
                    || (specHierarchy = specification.FindSpecObject(bindedSpecObjId)) == null)
                {
                    result = CreateSpecHierarchyFromWorkItem(content, specification, workItem);

                    if (result.OperationStatus == OperationStatusTypes.Success)
                    {
                        specHierarchy = (SpecHierarchy)result.Tag;

                        if (parentSpecHierarchy == null)
                        {
                            specification.Children.Add(specHierarchy);
                        }

                        string val = string.Empty;

                        this.mWiToReqIfBindings[specHierarchy.Object.Identifier] = workItem.Id.Value;
                        result = new OperationResult(OperationStatusTypes.Success);
                    }
                }
                else
                {
                    result = this.CopyFieldValuesToSpecObject(workItem, specHierarchy.Object);
                }

                if (parentSpecHierarchy == null)
                {
                    if (!specification.Children.Exists(child =>
                            child.Identifier == specHierarchy.Identifier))
                    {
                        SpecHierarchy oldParentHierarchy = specHierarchy.FindParentHierarchy();

                        if (oldParentHierarchy != null)
                        {
                            SpecHierarchy toBeDeleted = oldParentHierarchy.Children.Find(child => child.Identifier == specHierarchy.Identifier);

                            if (toBeDeleted != null)
                            {
                                oldParentHierarchy.Children.Remove(toBeDeleted);
                            }
                        }

                        specification.Children.Add(specHierarchy);
                    }
                }
                else if (parentSpecHierarchy.Children.Find(hierarchy =>
                                 hierarchy.Object.Identifier == specHierarchy.Object.Identifier) == null)
                {
                    parentSpecHierarchy.Children.Add(specHierarchy);
                }

                // Sort the children before processing them
                var sortedChildren = node.Children.OrderBy(child => child.Data.Id).ToList();

                foreach (TreeNode<WorkItem> treeNode in sortedChildren)
                {
                    result = ExportTreeNode(treeNode, specHierarchy);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                importSB.AppendLine(e.Message == null ? e?.InnerException?.Message : e.Message);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            DebugLogger.LogEnd("ReqIFExport", "ExportTreeNode");
            return result;
        }


        /// <summary>
        /// Creates the spec hierarchy from work item.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="specification">The specification.</param>
        /// <param name="workItem">The work item.</param>
        /// <param name="parentHierarchy">The parent hierarchy.</param>
        /// <returns></returns>
        private OperationResult CreateSpecHierarchyFromWorkItem(ReqIFContent content, Specification specification, WorkItem workItem, SpecHierarchy parentHierarchy = null)
        {
            DebugLogger.LogStart("ReqIFExport", "CreateSpecHierarchyFromWorkItem");
            OperationResult result = null;

            try
            {
                string specObjectIdentifier = string.Empty;
                SpecHierarchy specHierarchy = new SpecHierarchy();
                SpecObject specObject = new SpecObject();
                TypeMap typeMap = this.mMappingTemplate.TypeMaps.Find(map => map.WITypeName == workItem.Fields["System.WorkItemType"].ToString());
                SpecType specType = content.SpecTypes.Find(type => type.LongName == typeMap.ReqIFTypeName || type.LongName == $"{typeMap.ReqIFTypeName} Type");

                specObjectIdentifier = this.GetBindedSpecObjId(workItem.Id.Value);

                specObject.Description = CleanInvalidChars(workItem.Fields["System.Title"])?.ToString();
                specObject.Type = (SpecObjectType)specType;
                if (specObjectIdentifier == string.Empty)
                {
                    specObject.Identifier = "_" + Guid.NewGuid().ToString();
                }
                else
                {
                    specObject.Identifier = specObjectIdentifier;
                }


                specObject.ReqIFContent = content;
                content.SpecObjects.Add(specObject);

                specHierarchy.Object = specObject;
                specHierarchy.Identifier = "_" + Guid.NewGuid().ToString();
                specHierarchy.ReqIfContent = content;
                specHierarchy.Root = specification;

                result = CopyFieldValuesToSpecObject(workItem, specObject);

                if (result.OperationStatus == OperationStatusTypes.Success)
                {
                    result = new OperationResult(OperationStatusTypes.Success, specHierarchy);
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                importSB.Append(e.Message == null ? e?.InnerException?.Message : e.Message);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            DebugLogger.LogEnd("ReqIFExport", "CreateSpecHierarchyFromWorkItem");
            return result;
        }

        /// <summary>
        /// Copies the field values to spec object.
        /// </summary>
        /// <param name="workItem">The work item.</param>
        /// <param name="specObject">The spec object.</param>
        /// <returns></returns>
        private OperationResult CopyFieldValuesToSpecObject(WorkItem workItem, SpecObject specObject)
        {
            DebugLogger.LogStart("ReqIFExport", "CopyFieldValuesToSpecObject");
            OperationResult result = null;

            try
            {
                TypeMap typeMap = this.mMappingTemplate.TypeMaps.Find(map => map.WITypeName == workItem.Fields["System.WorkItemType"].ToString());

                if (typeMap == null)
                {
                    workItemsCountSummary.RevertedDueToMapping += 1;
                    return new OperationResult(OperationStatusTypes.Failed, "Type map not found in mapping template");
                }

                foreach (FieldMap fieldMap in typeMap.EnumFieldMaps)
                {
                    AttributeValue attributeValue =
                        specObject.Values.Find(value => value.AttributeDefinition.LongName == fieldMap.WIFieldName);

                    if (attributeValue == null)
                    {
                        AttributeDefinition attributeDefinition = specObject.SpecType.SpecAttributes.Find(definition => definition.LongName == fieldMap.ReqIFFieldName);

                        if (attributeDefinition == null)
                        {
                            continue;
                        }

                        attributeValue = this.GetAttributeValue(attributeDefinition);

                        if (attributeValue == null)
                        {
                            continue;
                        }

                        attributeValue.AttributeDefinition = attributeDefinition;
                        attributeValue.SpecElAt = specObject;

                        specObject.Values.Add(attributeValue);
                    }

                    string dataAttribute = attributeValue.GetType().FullName;

                    switch (dataAttribute)
                    {
                        #region AttributeValueEnumeration
                        case "ReqIFSharp.AttributeValueEnumeration":

                            ReqIFSharp.AttributeValueEnumeration attributeValue1 = attributeValue as ReqIFSharp.AttributeValueEnumeration;
                            EnumValue enumValue = new EnumValue();
                            List<EnumValue> listOfEnumValues = ((ReqIFSharp.AttributeValueEnumeration)attributeValue).Definition.Type.SpecifiedValues;
                            if (workItem.Fields.ContainsKey(fieldMap.WIFieldName))
                            {
                                bool isEnumValue = false;
                                foreach (var item in listOfEnumValues)
                                {
                                    if (item.LongName == workItem.Fields[fieldMap.WIFieldName].ToString())
                                    {
                                        enumValue = item;
                                        isEnumValue = true;
                                        break;
                                    }
                                }

                                if (!isEnumValue && listOfEnumValues.Count > 0)
                                {
                                    importSB.AppendLine($"This Workitem Id {workItem.Id.Value}: Value for the enum field {fieldMap.WIFieldName} does not match with the allowed values");
                                }
                            }
                            else
                            {
                                importSB.AppendLine($"This Workitem Id {workItem.Id.Value}: Configuration for the enum field {fieldMap.WIFieldName} has been defined in mapping but it doesn't contain any value.");
                                // if any default value exist we have have this
                            }


                            attributeValue1.Values.Add(enumValue);
                            attributeValue = attributeValue1;
                            break;
                        #endregion
                        #region AttributeValueXHTML
                        case "ReqIFSharp.AttributeValueXHTML":
                            try
                            {
                                if (workItem.Fields.TryGetValue(fieldMap.WIFieldName, out var fieldValue))
                                {
                                    string value = CleanInvalidChars(fieldValue)?.ToString();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        if (value.Contains("&lt;img"))
                                        {
                                            value = GetModifiedHtml(value, workItem.Id.Value);
                                        }

                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            switch (this.mExportDTO.ExportType)
                                            {
                                                case "Jama Connect":
                                                    value = HttpUtility.HtmlDecode(value);
                                                    attributeValue.ObjectValue = HtmlConverter.ConvertADOToJama(value);
                                                    break;
                                                case "Polarion":
                                                case "Azure Devops":
                                                default:
                                                    attributeValue.ObjectValue = value;
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        attributeValue.ObjectValue = string.Empty;                                        
                                        
                                    }

                                    DebugLogger.LogInfo($"ReqIFSharp.AttributeValueXHTML {fieldValue}");
                                }
                                else
                                {
                                    DebugLogger.LogInfo("ReqIFSharp.AttributeValueXHTML-Empty");
                                    attributeValue.ObjectValue = string.Empty;
                                }
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError(ex);
                                
                            }
                            break;
                        #endregion

                        #region AttributeValueInteger
                        case "ReqIFSharp.AttributeValueInteger":

                            if (workItem.Fields.ContainsKey(fieldMap.WIFieldName))
                            {
                                var cleanedValue = CleanInvalidChars(workItem.Fields[fieldMap.WIFieldName]);
                                if (long.TryParse(cleanedValue?.ToString(), out long temp))
                                {
                                    attributeValue.ObjectValue = temp;
                                }
                                else
                                {
                                    importSB.AppendLine($"Workitem Id {workItem.Id.Value}: Invalid integer value for field '{fieldMap.WIFieldName}'.");
                                    attributeValue.ObjectValue = 0;
                                }
                            }
                            else
                            {
                              importSB.AppendLine($"Workitem Id {workItem.Id.Value}: Configuration for the integer field '{fieldMap.WIFieldName}' has been defined in mapping but it doesn't contain any value.");
                                attributeValue.ObjectValue = 0;
                            }

                            break;
                        #endregion

                        #region AttributeValueDate
                        case "ReqIFSharp.AttributeValueDate":

                            if (workItem.Fields.ContainsKey(fieldMap.WIFieldName))
                            {
                                attributeValue.ObjectValue = CleanInvalidChars(workItem.Fields[fieldMap.WIFieldName]);
                            }
                            else
                            {
                                importSB.AppendLine($"Workitem Id {workItem.Id.Value}: Configuration for the DateTime field '{fieldMap.WIFieldName}' has been defined in mapping but it doesn't contain any value.");
                            }

                            break;
                        #endregion
                        default:
                            if (workItem.Fields.ContainsKey(fieldMap.WIFieldName))
                            {
                                attributeValue.ObjectValue = CleanInvalidChars(workItem.Fields[fieldMap.WIFieldName]);
                            }
                            else
                            {
                                attributeValue.ObjectValue = string.Empty;
                                // append work item with text data type not found

                                importSB.AppendLine($"Workitem Id {workItem.Id.Value}: Data type not found" );
                            }
                            break;

                    }
                }

                workItemsCountSummary.CreatedWorkItems += 1;
                result = OperationResult.SuccessWithNoMessage;
            }
            catch (Exception e)
            {
                //System.Diagnostics.Debugger.Break(); // Add here to catch unhandled export errors
                importSB.Append(e.Message == null ? e?.InnerException?.Message : e.Message);
                workItemsCountSummary.UpdatedWorkItems += 1;
                DebugLogger.LogError(e);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            finally
            {
                DebugLogger.LogEnd("ReqIFExport", "CopyFieldValuesToSpecObject");
            }

            return result;
        }


        /// <summary>
        /// Cleans the invalid chars.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private object CleanInvalidChars(object value)
        {
            DebugLogger.LogStart("ReqIFExport", "CleanInvalidChars");
            if (!(value is string))
            {
                return value;
            }

            string strVal = HttpUtility.HtmlEncode(value.ToString());
            DebugLogger.LogEnd("ReqIFExport", "CleanInvalidChars");

            return strVal;
        }

        private AttributeValue GetAttributeValue(AttributeDefinition attributeDefinition)
        {
            DebugLogger.LogStart("ReqIFExport", "GetAttributeValue");
            AttributeValue attributeValue = null;

            if (attributeDefinition is AttributeDefinitionString)
            {
                attributeValue = new AttributeValueString();
            }
            else if (attributeDefinition is AttributeDefinitionBoolean)
            {
                attributeValue = new AttributeValueBoolean();
            }
            else if (attributeDefinition is AttributeDefinitionDate)
            {
                attributeValue = new AttributeValueDate();
            }
            else if (attributeDefinition is AttributeDefinitionEnumeration)
            {
                attributeValue = new AttributeValueEnumeration();
            }
            else if (attributeDefinition is AttributeDefinitionInteger)
            {
                attributeValue = new AttributeValueInteger();
            }
            else if (attributeDefinition is AttributeDefinitionReal)
            {
                attributeValue = new AttributeValueReal();
            }
            else if (attributeDefinition is AttributeDefinitionDate)
            {
                attributeValue = new AttributeValueDate();
            }
            else if (attributeDefinition is AttributeDefinitionXHTML)
            {
                attributeValue = new AttributeValueXHTML();
            }
            DebugLogger.LogEnd("ReqIFExport", "GetAttributeValue");

            return attributeValue;
        }

        /// <summary>
        /// Gets the binded spec object identifier.
        /// </summary>
        /// <param name="workItemId">The work item identifier.</param>
        /// <returns></returns>
        private string GetBindedSpecObjId(int workItemId)
        {
            DebugLogger.LogStart("ReqIFExport", "GetBindedSpecObjId");

            string retVal = string.Empty;

            if (this.mWiToReqIfBindings.ContainsValue(workItemId))
            {
                foreach (string key in this.mWiToReqIfBindings.Keys)
                {
                    if (this.mWiToReqIfBindings[key] == workItemId)
                    {
                        retVal = key;
                        break;
                    }
                }
            }
            DebugLogger.LogEnd("ReqIFExport", "GetBindedSpecObjId");

            return retVal;
        }

        /// <summary>
        /// Builds the tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="referenceLinks">The reference links.</param>
        /// <returns></returns>
        private OperationResult BuildTree(Tree<WorkItem> tree, List<Tuple<WorkItem, WorkItem, string>> referenceLinks)
        {
            DebugLogger.LogStart("ReqIFExport", "BuildTree");

            OperationResult result = null;

            try
            {
                foreach (WorkItem workItem in this.mWorkitemRecord.Values)
                {
                    tree.RootNodes.Add(new TreeNode<WorkItem>(workItem, null));
                }

                foreach (WorkItem workItem in this.mWorkitemRecord.Values)
                {
                    if (workItem.Relations == null)
                    {
                        continue;
                    }

                    List<WorkItemRelation> relations = (from c in workItem.Relations
                                                        where c.Rel != "AttachedFile"
                                                        select c).ToList();

                    for (var i = 0; i < relations.Count; i++)
                    {
                        WorkItemRelation relatedLink = relations[i];

                        if (relatedLink == null)
                        {
                            continue;
                        }

                        if (relatedLink.GetLinkName() == WI_LINK_CHILD || relatedLink.GetLinkName() == WI_LINK_PARENT)
                        {
                            if (this.mWorkitemRecord.ContainsKey(relatedLink.RelatedWorkItemId()))
                            {
                                TreeNode<WorkItem> currentNode = tree.FindTreeNode(workItem.Id.Value);
                                TreeNode<WorkItem> relatedNode = tree.FindTreeNode(relatedLink.RelatedWorkItemId());

                                if (relatedLink.GetLinkName() == WI_LINK_CHILD)
                                {
                                    if (relatedNode.Parent == null)
                                    {
                                        tree.RootNodes.Remove(relatedNode);
                                    }
                                    else if (relatedNode.Parent == currentNode)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        relatedNode.Parent.Children.Remove(relatedNode);
                                    }

                                    currentNode.Children.Add(relatedNode);
                                    relatedNode.Parent = currentNode;
                                }
                                else
                                {
                                    if (currentNode.Parent == null)
                                    {
                                        tree.RootNodes.Remove(currentNode);
                                    }
                                    else
                                    {
                                        if (currentNode.Parent.Data.Id == relatedLink.RelatedWorkItemId())
                                        {
                                            continue;
                                        }

                                        currentNode.Parent.Children.Remove(currentNode);
                                    }

                                    relatedNode.Children.Add(currentNode);
                                    currentNode.Parent = relatedNode;
                                }
                            }
                        }
                        else
                        {
                            LinkMap linkMap = this.mMappingTemplate.LinkMaps.Find(map =>
                                map.WILinkName == relatedLink.GetLinkName() || map.WILinkName == relatedLink.ReverseName(this.mRelationTypes));

                            if (linkMap == null)
                            {
                                continue;
                            }

                            TreeNode<WorkItem> currentNode = tree.FindTreeNode(workItem.Id.Value);
                            TreeNode<WorkItem> relatedNode = tree.FindTreeNode(relatedLink.RelatedWorkItemId());

                            if (currentNode == null || relatedNode == null)
                            {
                                continue;
                            }

                            referenceLinks.Add(new Tuple<WorkItem, WorkItem, string>(currentNode.Data,
                                relatedNode.Data,
                                linkMap.ReqIFRelationName));
                        }
                    }
                }

                result = OperationResult.SuccessWithNoMessage;
            }
            catch (Exception e)
            {
                DebugLogger.LogError(e);
                importSB.AppendLine(e.Message == null ? e?.InnerException?.Message : e.Message);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            DebugLogger.LogEnd("ReqIFExport", "BuildTree");

            return result;
        }

        /// <summary>
        /// Gets all work items.
        /// </summary>
        /// <returns></returns>
        private OperationResult GetAllWorkItems()
        {
            DebugLogger.LogStart("ReqIFExport", "GetAllWorkItems");
            OperationResult result = null;

            try
            {
                this.mWorkitemRecord.Clear();
                DateTime dateTime = DateTime.Now;
                List<WorkItem> workItems = (List<WorkItem>)this.mWorkItemTrackingHttpClient.GetWorkItemsAsyncCustom(this.mWorkItemsToExport);
                DebugLogger.LogInfo($"Work items(count: {this.mWorkItemsToExport.Length}) fetched in {(DateTime.Now - dateTime).TotalMinutes} mins.");

                workItems.ForEach(
                    item =>
                    {
                        if (item != null)
                        {
                            TypeMap typeMap = this.mMappingTemplate.TypeMaps.Find(map => map.WITypeName == item.Fields["System.WorkItemType"].ToString());

                            if (typeMap != null)
                            {
                                this.mWorkitemRecord.Add(item.Id.Value, item);
                            }
                        }
                    });


                result = new OperationResult(OperationStatusTypes.Success, workItems);

                if (workItems == null && !workItems.Any())
                {
                    importSB.Append("No Work Items Type Found");
                    result = new OperationResult(OperationStatusTypes.Failed);
                }
            }


            catch (Exception e)
            {
                DebugLogger.LogError(e.Message);
                importSB.AppendLine(e.Message);
                result = new OperationResult(OperationStatusTypes.Failed, null, e);
            }
            DebugLogger.LogEnd("ReqIFExport", "GetAllWorkItems");
            return result;
        }

        /// <summary>
        /// Gets the specification.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// No core content found.
        /// or
        /// Targeted Specification '{this.mTargetedReqIFSpec}' not found.
        /// </exception>
        private Specification GetSpecification()
        {
            DebugLogger.LogStart("ReqIFExport", "GetSpecification");
            ReqIFContent content = this.mReqIf.CoreContent;

            if (content == null)
            {
                throw new InvalidOperationException($"No core content found.");
            }

            if (string.IsNullOrEmpty(this.mTargetedReqIFSpec))
            {
                DebugLogger.LogEnd("ReqIFExport", "GetSpecification");

                return content.Specifications.FirstOrDefault();
            }

            Specification retVal =
                content.Specifications.Find(specification => specification.LongName == this.mTargetedReqIFSpec);

            if (retVal == null)
            {
                throw new InvalidOperationException($"Targeted Specification '{this.mTargetedReqIFSpec}' not found.");
            }
            DebugLogger.LogEnd("ReqIFExport", "GetSpecification");

            return retVal;
        }
        /// <summary>
        /// Converts to html, update its html and return
        /// </summary>
        /// <param name="unicodedHtml">Unicoded Html</param>
        /// <returns></returns>
        private string GetModifiedHtml(string unicodedHtml, int? id)
        {
            DebugLogger.LogStart("ReqIFExport", "GetModifiedHtml");
            try
            {
                

                string html = HtmlParsingUtility.ConvertUnicodeToHTML(unicodedHtml);

                if(html != null && !string.IsNullOrWhiteSpace(html))
                {
                    var links = HtmlParsingUtility.GetAllObjectValues(html);

                    if(links != null)
                    {                        
                        var localFilePaths =  DownloadFilesOnServer(links.Select(x=>x.Src).ToList(), id);

                        if(localFilePaths != null && localFilePaths.Count > 0)
                        {
                           var updatedHtml =  ReplaceImagesLinkWithLocalPaths(html, localFilePaths);

                            string unicodeUpdatedHtml = HtmlParsingUtility.ConvertHTMLToUnicode(updatedHtml);
                            DebugLogger.LogEnd("ReqIFExport", "GetModifiedHtml");

                            return unicodeUpdatedHtml;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                throw new Exception(ex.Message);
            }
            DebugLogger.LogEnd("ReqIFExport", "GetModifiedHtml");

            return null;
        }


        private string DownloadAttachments(List<string> urls, int? workItemId)
        {
            DebugLogger.LogStart("ReqIFExport", "DownloadAttachments");
            try
            {
                var localFilePaths = DownloadFilesOnServer(urls, workItemId);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("ReqIFExport", "DownloadAttachments");

            return null;
        }

        /// <summary>
        /// Downloads file from Ado to local folder path
        /// </summary>
        /// <param name="uris">URLS</param>
        /// <returns></returns>
        private List<HtmlAttributeValues> DownloadFilesOnServer(List<string> uris, int? workItemId)
        {
            DebugLogger.LogStart("ReqIFExport", "DownloadFilesOnServer");
            List<HtmlAttributeValues> imgLinks = new List<HtmlAttributeValues>();

            try
            {              
                string filePath = Path.Combine(Path.GetDirectoryName(this.reqIfPath), "files");

                if (string.IsNullOrWhiteSpace(filePath) || filePath.Length <= 0)
                    throw new ArgumentNullException();
                
                foreach (string uri in uris)
                {                      
                    Uri uriObj = new Uri(uri);
                    string fileGuid = uriObj.Segments[uriObj.Segments.Length - 1]?.ToString();
                    string fileName = HttpUtility.ParseQueryString(uriObj.Query).Get("fileName");

                    //standardizing filename
                    string bindedSpecObjId = this.GetBindedSpecObjId(workItemId.Value);
                    string standardFileName = fileGuid + "_" + fileName;
                    string absFilePath = Path.Combine(filePath, standardFileName);
                    // Download the Web resource and save it into the current filesystem folder.
                    var fileStreamResult = GetFileStream(fileGuid, standardFileName);

                    if(fileStreamResult.OperationStatus == OperationStatusTypes.Success)
                    {
                            
                        Stream downloadedFile = fileStreamResult.Tag as Stream;

                        if (downloadedFile == null)
                        {
                            continue;
                        }

                        using (MemoryStream ms = new MemoryStream())
                        {
                            downloadedFile.CopyTo(ms);

                            using (FileStream fs = new FileStream(absFilePath, FileMode.Create))
                            {
                                ms.Seek(0, SeekOrigin.Begin);
                                ms.CopyTo(fs);
                                fs.Flush();
                                imgLinks.Add(new HtmlAttributeValues
                                {
                                    Src = Path.Combine("files", standardFileName),
                                    Name = standardFileName
                                });
                            }
                        }
                    }
                }                                
            }
            catch (UnauthorizedAccessException ex)
            {
                DebugLogger.LogError(ex);
                throw new UnauthorizedAccessException(ex.Message);
            }
            catch (Exception ex)
            {

                //log in StringBuilder 
                DebugLogger.LogError(ex);
                throw new Exception(ex.Message);
            }

           DebugLogger.LogEnd("ReqIFExport", "DownloadFilesOnServer");
           return imgLinks;

        }
        /// <summary>
        /// Converts file to Stream
        /// </summary>
        /// <param name="fileNameGuid">Filename GUID</param>
        /// <param name="fileName">Filename</param>
        /// <returns></returns>
        private OperationResult GetFileStream(string fileNameGuid, string fileName)
        {
            DebugLogger.LogStart("ReqIFExport", "GetFileStream");
            OperationResult result = null;
            Guid nameGuid = Guid.Empty;
        
            if (Guid.TryParse(fileNameGuid, out nameGuid))
            {

                Stream serverFileStream = mWorkItemTrackingHttpClient.GetAttachmentContentAsync(nameGuid, fileName).Result;
                result = new OperationResult(OperationStatusTypes.Success, serverFileStream);
            }
            else
            {
      
                result = new OperationResult(OperationStatusTypes.Failed, null);
            }
            DebugLogger.LogEnd("ReqIFExport", "GetFileStream");
            return result;
        }
        /// <summary>
        /// Replaces images src with local path 
        /// </summary>
        /// <param name="html">HTML string</param>
        /// <param name="filePaths">local path</param>
        /// <returns></returns>
        private string ReplaceImagesLinkWithLocalPaths(string html, List<HtmlAttributeValues> filePaths)
        {
            DebugLogger.LogStart("ReqIFExport", "ReplaceImagesLinkWithLocalPaths");
            string updatedHtml;
            try
            {
                string filePath = Path.Combine(Path.GetDirectoryName(this.reqIfPath), "files");

                if (string.IsNullOrWhiteSpace(html))
                    throw new ArgumentNullException();
          
                 updatedHtml = HtmlParsingUtility.ReplaceImagesLink(html, filePaths);
                
            }

            catch (Exception ex)
            {
                DebugLogger.LogError(ex);              
                throw new Exception(ex.Message);
            }

            DebugLogger.LogEnd("ReqIFExport", "ReplaceImagesLinkWithLocalPaths");

      
            return updatedHtml;
        }
    }
}