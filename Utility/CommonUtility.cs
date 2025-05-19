using Newtonsoft.Json;
using ReqIFBridge.Models;
using ReqIFBridge.ReqIF.ReqIFMapper;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;

namespace ReqIFBridge.Utility
{
    public static class CommonUtility
    {
        
        public static CultureInfo CONST_LOCALE_SETTINGS = CultureInfo.InvariantCulture;
        public const string INVALID_PROXY_CONFIGURATION_MSG = "Invalid Proxy Configuration";
        public const string REQIF_PATH_IS_EMPTY = "reqIf path is empty";
        public const string KEY_NOT_FOUND = "Key not found";
        public const string EMPTY_MSG = "Required field(s) are empty.";
        public const string DUPLICATE_MSG = "Multiple work item type(s) has duplicate mapping.";
        public const string DUPLICATE_FIELDS_MSG = "Multiple work item(s) field has duplicate mapping.See the logs for more detail.";
        public const string NOT_FOUND_MSG = "work item type(s) does not exist in current project.";
        public const string TITLE_FIELD_VALIDATION_MSG = "No mapping found for Title, It is a mandatory field.";
        public const string DUPLICATE_MSG_FOR_REQIF = "Multiple ReqIF type(s) has duplicate mapping.";
        public const string DUPLICATE_FIELDS_MSG_FOR_REQIF = "Multiple ReqIF field has duplicate mapping.";
        public const string DUPLICATE_MSG_FOR_LINK= "Multiple ReqIF Link type(s) has duplicate mapping.";
        public const string UNABLE_TO_GET_WORKITEM_TYPES = "Unable to get workitem type:";
        public const string UNABLE_TO_GET_FIELDS_FOR_WORKITEM_MSG = "Unable to get fields for workitem type";
        public const string TIME_TAKEN_TO_GET_ALL_WORKITEMS_MSG="Time taken to get all workitems";
        public const string RESULT_IS_NULL_MSG = "Result is null";
        public const string TIME_TAKEN_TO_GET_PROCESS_ID_MSG = "Time taken to get process ids";
        public const string UNABLE_TO_GET_PROJECT_LIST_MSG = "Unable to get project list";
        public const string TRAIL_ACTIVATE_MSG = "Trial has been activated successfully.";
        public const string LICENSE_INFO_FOUND_MSG = "License information found in session.";
        public const string TRIAL_LICENSE_FOUND_MSG = "Trial license han been found.";
        public const string USERNAME_NULL_OR_EMPTY_MSG = "The provided user name is null or empty.";
        public const string FLOATING_SERVER_URL_NOT_VALID_MSG = "The provided floating server Url is not valid.";
        public const string WI_CREATION_FAILED_MSG = "WorkItem creation failed";
        public const string CONST_REQIF_TRIAL_IMPORT_MSG = "Import failed! The file exceeds the limit of 30 transactions for trial version.";
        public const string CONST_CORRUPT_FILE_MSG = "Import Failed: The file is either empty or corrupt. Please upload a valid file.";
        public const string CONST_SPECIFICATION_COUNT_MSG = "Import Failed: The file does not contain any specifications. Please upload a valid file with specifications for import.";
        public const string CONST_SPECIAL_CHAR_MSG = "Special characters are not allowed";
        internal static string LogConfigurationFile
        {
            get { return ConfigurationManager.AppSettings["LogConfig"]; }
        }


        public static ReqIFMappingTemplate LoadMapping(string path)
        {
            DebugLogger.LogStart("DefaultController", "LoadMapping");
            DebugLogger.LogInfo("Loading-Path:" + path);
            ReqIFMappingTemplate mappingTemplate = null;
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {

                        fs.Position = 0;
                        fs.CopyTo(stream);

                        fs.Flush();
                        stream.Flush();

                        ReqIFTemplateReader reqIfTemplateReader = new ReqIFTemplateReader(stream);

                        mappingTemplate = reqIfTemplateReader.Template;

                        fs.Close();
                        stream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("DefaultController", "LoadMapping");
            return mappingTemplate;
        }

        public static Dictionary<string, int> GetBinding(string path)
        {
            DebugLogger.LogStart("DefaultController", "GetBinding");
            DebugLogger.LogInfo("GetBinding-Path:" + path);
            byte[] bytes;
            string json;
            Dictionary<string, int> reqIFBinding = null;

            if (System.IO.File.Exists(path))
            {
                using (FileStream reader = new FileStream(path, FileMode.OpenOrCreate))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {

                        reader.Position = 0;
                        reader.CopyTo(memoryStream);

                        reader.Flush();
                        memoryStream.Flush();

                        reader.Close();

                        bytes = memoryStream.ToArray();

                        json = Encoding.UTF8.GetString(bytes);

                        reqIFBinding = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                    }
                }
            }

            if (reqIFBinding == null)
            {
                reqIFBinding = new Dictionary<string, int>();
            }
            DebugLogger.LogEnd("DefaultController", "GetBinding");

            return reqIFBinding;
        }



        public static string GetParameterValueFromUri(string uri, string addParameterName)
        {
            DebugLogger.LogStart( "CommonUtility", "GetParameterValueFromUri");
            string resultString = string.Empty;
            try
            {
                string urlDecode = HttpUtility.HtmlDecode(uri);
               
                if (urlDecode != null)
                {
                    UriBuilder uriBuilder = new UriBuilder(urlDecode);
                    NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
                    resultString = query[addParameterName];
                }
            }
            catch (Exception e)
            {
                DebugLogger.LogError(e.Message);
            }
            DebugLogger.LogEnd( "CommonUtility", "GetParameterValueFromUri");
            return resultString;
        }

        public static string GetAssemblyVersion()
        {
            DebugLogger.LogStart("CommonUtility", "GetAssemblyVersion");
            string version = string.Empty;
            try
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                version = fvi.FileVersion;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("CommonUtility", "GetAssemblyVersion");
            return version;
        }
      
        public static string GenerateRandomName(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be greater than 0", nameof(length));

            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "y", "z" };
            string[] vowels = { "a", "e", "i", "o", "u" };

            Random random = new Random();
            StringBuilder name = new StringBuilder();

            // Alternate between consonants and vowels
            bool useConsonant = true;
            for (int i = 0; i < length; i++)
            {
                if (useConsonant)
                {
                    name.Append(consonants[random.Next(consonants.Length)]);
                }
                else
                {
                    name.Append(vowels[random.Next(vowels.Length)]);
                }
                useConsonant = !useConsonant;
            }

            // Capitalize the first letter
            name[0] = char.ToUpper(name[0]);

            return name.ToString();
        }


        public static string GetConversationExchangeId(ReqIFSharp.ReqIF reqIF)
        {

            string exchangeId = string.Empty;
            if (reqIF.ToolExtension != null)
            {
                foreach (var toolExtension in reqIF.ToolExtension)
                {
                    if (toolExtension is ReqIFSharp.ReqIFToolExtension extension)
                    {
                        // Wrap the content in a single root element
                        string wrappedXml = $"<Root>{extension.InnerXml}</Root>";

                        var xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(wrappedXml);

                        // Check for <reqif-common:IDENTIFIER>
                        var identifierNode = xmlDocument.GetElementsByTagName("reqif-common:IDENTIFIER").Cast<XmlNode>().FirstOrDefault();
                        if (identifierNode != null && !string.IsNullOrEmpty(identifierNode.InnerText))
                        {
                            exchangeId = identifierNode.InnerText;
                            break;
                        }
                    }
                }
            }

            return exchangeId;
        }


    }
}