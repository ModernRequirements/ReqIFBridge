using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using HtmlAgilityPack;


namespace ReqIFBridge.Utility
{
    public class HtmlParsingUtility
    {

        public static string ConvertUnicodeToHTML(string unicode)
        {
            string xhtml = null;
            try
            {
                if (string.IsNullOrWhiteSpace(unicode) || unicode.Length <= 0)
                    throw new ArgumentNullException();


                

                xhtml = HttpUtility.HtmlDecode(unicode);

                //HtmlDocument htmlDocument = new HtmlDocument();
                //htmlDocument.LoadHtml(xhtml);


                //htmlDocument.valida

                return xhtml;

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            return xhtml;
        }
        public static string ConvertHTMLToUnicode(string html)
        {
            string unicode = null;
            try
            {
                if (string.IsNullOrWhiteSpace(html) || html.Length <= 0)
                    throw new ArgumentNullException();



                unicode = HttpUtility.HtmlEncode(html);
                

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            return unicode;
        }

        private static readonly string[] ImageExtensions = {
                    ".jpeg", ".jpg", ".png" , ".bmp"
                };

        public static List<HtmlAttributeValues> GetAllObjectValues(string html, string tag = "img", string attribute = "src", ReqIFSharp.ReqIF reqIf = null)
        {
            try
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                Dictionary<string, string> dicEmbededObjectFileNames = new Dictionary<string, string>();
                htmlDocument.LoadHtml(html);

                // Check for <reqif-xhtml:object> tag
                if (html.Contains("reqif-xhtml:object"))
                {
                    tag = "reqif-xhtml:object";
                    attribute = "data";
                }

                var embededDataCollection = htmlDocument.DocumentNode.Descendants(tag)
                                .Select(e => new HtmlAttributeValues
                                {
                                    Src = e.GetAttributeValue(attribute, null),
                                    Type = e.GetAttributeValue("type", null),
                                    Name = e.GetAttributeValue("name", null)
                                })
                                .ToList();

                if (embededDataCollection == null || embededDataCollection.Count <= 0)
                {
                    return null;
                }

                foreach (var embededDataItem in embededDataCollection)
                {
                    if (!string.IsNullOrEmpty(embededDataItem.Name) || string.IsNullOrEmpty(embededDataItem.Src))
                    {
                        continue;
                    }
                    embededDataItem.Name = dicEmbededObjectFileNames.Keys.Contains(embededDataItem.Src)
                        ? dicEmbededObjectFileNames[embededDataItem.Src]
                        : GetNameOfEmbededObject(embededDataItem, reqIf, dicEmbededObjectFileNames);
                }
                return embededDataCollection;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            return null;
        }

        public static string GetNameOfEmbededObject(HtmlAttributeValues url, ReqIFSharp.ReqIF reqIf, Dictionary<string, string> dicEmbededObjectFileNames)
        {
            
            string reference_Object_Name = "ReqIF.Name";
            if (reqIf == null)
            {
                return null;
            }
            var toolExtension = reqIf.ToolExtension.FirstOrDefault();
            string randomFileName = Path.GetRandomFileName() + ".xml";
            string filePath = Path.GetTempPath() + randomFileName;
            string textToAddAtBeginning = "<root>";
            string textToAddAtEnd = "</root>";
            // Read the existing content of the file
            string existingContent = toolExtension?.InnerXml.ToString();
            // Create a new StreamWriter to write to the file
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write the text to add at the beginning
                writer.WriteLine(textToAddAtBeginning);
                // Write the existing content
                writer.WriteLine(existingContent);
                // Write the text to add at the end
                writer.WriteLine(textToAddAtEnd);
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);

            var nodes = xmlDoc.GetElementsByTagName("rm:SPEC-OBJECT-EXTENSION");

            foreach (XmlElement node in nodes)
            {

                if ((node.LastChild?.Name) != "rm:WRAPPED-RESOURCE-REF")
                {
                    continue;
                }

                string[] split = node.InnerText.ToString().Split('_').Reverse().ToArray();

                foreach (var item in reqIf.CoreContent.SpecObjects)
                {
                    if (item.Identifier != "_" + split[1].ToString())
                    {
                        continue;
                    }
                    foreach (var attribute in item.Values)
                    {
                        if (!attribute.AttributeDefinition.DatatypeDefinition.GetType().Name.Equals("DatatypeDefinitionXHTML")
                            || !attribute.AttributeDefinition.LongName.Equals(reference_Object_Name))
                        {
                            continue;
                        }
                        string attname = HttpUtility.HtmlDecode(attribute.ObjectValue?.ToString());
                        if(!dicEmbededObjectFileNames.Keys.Contains("_" + split[0].ToString()))
                        {
                            string objectName = ConvertHTMLIntoString(attname).ToString().TrimStart();
                            dicEmbededObjectFileNames.Add("_" + split[0].ToString(),objectName);
                        }
                       
                    }
                }

            }

            if (dicEmbededObjectFileNames.Count > 0 && dicEmbededObjectFileNames.Keys.Contains(url.Src))
            {
                return dicEmbededObjectFileNames[url.Src];
            }

            return null;
        }

        public static string ReplaceImagesLink(string html, List<HtmlAttributeValues> filePaths, string tag = "//img", string attribute = "src")
        {
            string newHtml = null;
            try
            {
                if (string.IsNullOrWhiteSpace(html) || html.Length <= 0)
                    throw new ArgumentNullException();

                
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                //handles xHtml markup
                if (tag.Equals("xhtml:object"))
                {

                    //access only nested elements, what to do when not nested?
                    var getXHTMLDiv = htmlDocument.DocumentNode.ChildNodes.FirstOrDefault();
                    int i = 0;
                    foreach (var item in getXHTMLDiv.ChildNodes.ToList())
                    {
                        //Replaces object tag to img tag
                        if (item.Name.Equals("xhtml:object"))
                        {
                            string imgWrap = GetEmbeddedElement(filePaths[i]);
                             HtmlDocument htmlDocumentDecoded = new HtmlDocument();
                             htmlDocumentDecoded.LoadHtml(imgWrap);
                             item.ParentNode.ReplaceChild(htmlDocumentDecoded.DocumentNode, item);
                            i++;

                        }
                        //Replaces object xhtml:br to br tag
                        else if (item.Name.Equals("xhtml:br"))
                        {
                            HtmlDocument htmlDocumentDecoded = new HtmlDocument();
                            htmlDocumentDecoded.LoadHtml("<br/>");
                            item.ParentNode.ReplaceChild(htmlDocumentDecoded.DocumentNode, item);
                        }
                    }
                    newHtml = getXHTMLDiv.OuterHtml;
                }
                else if (tag.Equals("object"))
                {

                    if (string.IsNullOrWhiteSpace(html) || html.Length <= 0)
                        throw new ArgumentNullException();
                   
                    int i = 0;
                    var objectTags = htmlDocument.DocumentNode.Descendants("object").ToList();
                    foreach (var objectTag in objectTags)
                    {
                                string imgWrap = GetEmbeddedElement(filePaths[i]);
                                HtmlDocument htmlDocumentDecoded = new HtmlDocument();
                                htmlDocumentDecoded.LoadHtml(imgWrap);
                        objectTag.ParentNode.ReplaceChild(htmlDocumentDecoded.DocumentNode, objectTag);
                        i++;

                    }                      
                    
                    newHtml = htmlDocument.DocumentNode.OuterHtml;
                }
                //handles html markup
                else
                {
                    int i = 0;
                    foreach (var node in htmlDocument.DocumentNode.SelectNodes(tag))
                    {
                        var src = node.Attributes[attribute].Value;

                        if (filePaths.Count > i)
                        {
                            // need to replace this GetEmbeddedElement function, as it supports both img and <a> tag, if html supports excel attachments

                            node.SetAttributeValue(attribute, filePaths[i].Src);

                        }
                        i++;
                    }

                    newHtml = htmlDocument.DocumentNode.WriteTo();

                }
                

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            return newHtml;
        }


        public static HtmlNode GetChildNodes(string html)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(html) || html.Length <= 0)
                    throw new ArgumentNullException();

                html = ConvertUnicodeToHTML(html);
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                
                //need fixing, getting only first child
                var getXHTMLDiv = htmlDocument.DocumentNode;
                if (getXHTMLDiv != null && getXHTMLDiv.HasChildNodes)
                    return getXHTMLDiv;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            return null;


        }


        public static bool IsXhtml(string xhtml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xhtml) || xhtml.Length <= 0)
                    throw new ArgumentNullException();

                xhtml = ConvertUnicodeToHTML(xhtml);
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(xhtml);

                if(xhtml.Contains("xhtml:object") || xhtml.Contains("xhtml:div") || xhtml.Contains("xmlns:xhtml"))
                {
                    return true;
                }

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            return false;



        }

        public static bool ValidateImageExtension(string path)
        {
            try
            {
                string ext = Path.GetExtension(path);
                if (ImageExtensions.Contains(ext.ToLower()))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                throw new Exception(ex.Message);
            }
            return false;
        }

        private static bool ValidateAttachmentExtension(string path)
        {
            try
            {
                string ext = Path.GetExtension(path);

                string[] extensions = {

                    ".pdf", ".txt", ".ppt" , ".pptx",".xls",".xlsx",".doc",".docm",".dotx",".ocx", ".vsdx"

                };

                if (extensions.Contains(ext.ToLower()))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                throw new Exception(ex.Message);
            }
            return false;
        }

        private static string GetEmbeddedElement(HtmlAttributeValues htmlAttributeValue)
        {

            string imgWrap = "";
            try
            {
                if (htmlAttributeValue == null)
                    throw new ArgumentNullException();

                if (ValidateImageExtension(htmlAttributeValue.Src))
                {
                        imgWrap = $"<img src=\"{ htmlAttributeValue.Src }\" name=\" {htmlAttributeValue.Name} \" />";                   
                }
                else
                {
                    imgWrap = $"<br><a href=\"{ htmlAttributeValue.Src }\" target=\"_blank\">{ htmlAttributeValue.Name}</a>";
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                throw new Exception(ex.Message);
            }

            return imgWrap;
        }

        public static  string ConvertHTMLIntoString(string source)
        {
            try
            {
                string result;

                // Remove HTML Development formatting
                // Replace line breaks with space
                // because browsers inserts space
                result = source.Replace("\r", " ");
                // Replace line breaks with space
                // because browsers inserts space
                result = result.Replace("\n", " ");
                // Remove step-formatting
                result = result.Replace("\t", string.Empty);
                // Remove repeating spaces because browsers ignore them
                result = System.Text.RegularExpressions.Regex.Replace(result,
                                                                      @"( )+", " ");

                // Remove the header (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*head([^>])*>", "<head>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*head( )*>)", "</head>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(<head>).*(</head>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // remove all scripts (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*script([^>])*>", "<script>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*script( )*>)", "</script>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                //result = System.Text.RegularExpressions.Regex.Replace(result,
                //         @"(<script>)([^(<script>\.</script>)])*(</script>)",
                //         string.Empty,
                //         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<script>).*(</script>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // remove all styles (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*style([^>])*>", "<style>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*style( )*>)", "</style>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(<style>).*(</style>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert tabs in spaces of <td> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*td([^>])*>", "\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert line breaks in places of <BR> and <LI> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*br( )*>", "\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*li( )*>", "\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert line paragraphs (double line breaks) in place
                // if <P>, <DIV> and <TR> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*div([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*tr([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*p([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Remove remaining tags like <a>, links, images,
                // comments etc - anything that's enclosed inside < >
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<[^>]*>", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // replace special characters:
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @" ", " ",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&bull;", " * ",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&lsaquo;", "<",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&rsaquo;", ">",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&trade;", "(tm)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&frasl;", "/",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&lt;", "<",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&gt;", ">",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&copy;", "(c)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&reg;", "(r)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove all others. More can be added, see
                // http://hotwired.lycos.com/webmonkey/reference/special_characters/
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&(.{2,6});", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // for testing
                //System.Text.RegularExpressions.Regex.Replace(result,
                //       this.txtRegex.Text,string.Empty,
                //       System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // make line breaking consistent
                result = result.Replace("\n", "\r");

                // Remove extra line breaks and tabs:
                // replace over 2 breaks with 2 and over 4 tabs with 4.
                // Prepare first to remove any whitespaces in between
                // the escaped characters and remove redundant tabs in between line breaks
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)( )+(\r)", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\t)( )+(\t)", "\t\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\t)( )+(\r)", "\t\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)( )+(\t)", "\r\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove redundant tabs
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)(\t)+(\r)", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove multiple tabs following a line break with just one tab
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)(\t)+", "\r\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Initial replacement target string for line breaks
                string breaks = "\r\r\r";
                // Initial replacement target string for tabs
                string tabs = "\t\t\t\t\t";
                for (int index = 0; index < result.Length; index++)
                {
                    result = result.Replace(breaks, "\r\r");
                    result = result.Replace(tabs, "\t\t\t\t");
                    breaks = breaks + "\r";
                    tabs = tabs + "\t";
                }

                // That's it.
                Debug.WriteLine(result);
                return result;
            }
            catch
            {

                return source;
            }
        }

     
        
    }
}