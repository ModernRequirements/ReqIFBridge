using HtmlAgilityPack;
using ReqIFBridge.Utility;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ReqIFBridge.ReqIF
{
    public static class HtmlConverter
    {
        private const string RequiredNamespace = "http://www.w3.org/1999/xhtml";
        private const string RequiredPrefix = "reqif-xhtml";

        public static string ConvertJamaToADO(string jamaHtml)
        {
            if (string.IsNullOrWhiteSpace(jamaHtml))
                return string.Empty;


            // Remove the "reqif-xhtml:" prefix from element names
            string withoutPrefixes = Regex.Replace(jamaHtml, @"<(/?)(reqif-xhtml:)", "<$1");

            // Remove the namespace declarations (xmlns:reqif-xhtml="...")
            string withoutNamespaces = Regex.Replace(withoutPrefixes, @"\s+xmlns:reqif-xhtml=""[^""]*""", string.Empty);
                       
            // Return the transformed HTML
            return withoutNamespaces;
        }


        public static string ConvertADOToJama(string adoHtml)
        {
            if (string.IsNullOrWhiteSpace(adoHtml))
                return string.Empty;

            // Replace &nbsp; with numeric entity
            string sanitizedHtml = adoHtml.Replace("&nbsp;", "&#160;");

            // Load HTML using HtmlAgilityPack
            HtmlDocument htmlDoc = new HtmlDocument
            {
                OptionWriteEmptyNodes = true // Ensure self-closing tags are written properly
            };
            htmlDoc.LoadHtml(sanitizedHtml);

            // Replace all <img> tags with <object> tags
            foreach (var img in htmlDoc.DocumentNode.Descendants("img").ToList())
            {
                var src = img.GetAttributeValue("src", string.Empty);
                var objectElement = HtmlNode.CreateNode("<object></object>");
                objectElement.SetAttributeValue("data", src);
                objectElement.SetAttributeValue("type", "image/png");
                objectElement.SetAttributeValue("width", "305");
                objectElement.SetAttributeValue("height", "365");

                // Optionally, add a space before closing the object tag
                objectElement.InnerHtml = " ";

                img.ParentNode.ReplaceChild(objectElement, img);
            }

            // Ensure all tags are properly closed
            if (!sanitizedHtml.Trim().EndsWith("</div>"))
            {
                var closingDiv = HtmlNode.CreateNode("</div>");
                if (closingDiv != null)
                {
                    htmlDoc.DocumentNode.AppendChild(closingDiv);
                }
                else
                {
                   
                    DebugLogger.LogWarn("Error: Failed to create closing div node:ConvertADOToJama");
                }
            }

            // Convert back to string
            sanitizedHtml = htmlDoc.DocumentNode.OuterHtml;

            // Log the sanitized HTML for debugging
     
            DebugLogger.LogInfo($"Sanitized HTML: {sanitizedHtml}");
            try
            {
                // Check if the content is valid XML
                if (!IsValidXml(sanitizedHtml))
                {
                    // If not valid XML, wrap it in a root element
                    sanitizedHtml = $"<div>{sanitizedHtml}</div>";
                }

                // Parse the HTML into XElement
                XElement htmlElement = XElement.Parse(sanitizedHtml);

                // Add the prefixed namespace to the root element
                AddNamespaceWithPrefix(htmlElement, RequiredNamespace, RequiredPrefix);              

                return htmlElement.ToString(SaveOptions.DisableFormatting);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError($"Error: {ex.Message}");
                return string.Empty;
            }
        }

        private static bool IsValidXml(string content)
        {
            try
            {
                XDocument.Parse(content);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }

        private static void AddNamespaceWithPrefix(XElement element, string namespaceUri, string prefix)
        {
            // Define the namespace with the specified prefix
            XNamespace ns = namespaceUri;

            // Add the namespace declaration to the root element
            XElement root = element;
            root.Add(new XAttribute(XNamespace.Xmlns + prefix, ns));

            // Recursively set each element's name with the prefix
            SetElementNameWithPrefix(root, ns, prefix);
        }

        private static void SetElementNameWithPrefix(XElement element, XNamespace ns, string prefix)
        {
            // Update the element's name to include the prefix
            element.Name = ns + element.Name.LocalName;

            foreach (var attr in element.Attributes().ToList())
            {
                // If attribute does not have a namespace, leave it as is
                if (attr.IsNamespaceDeclaration)
                    continue;

                // Optionally, handle namespaced attributes if needed
            }

            // Recursively apply to child elements
            foreach (var child in element.Elements())
            {
                SetElementNameWithPrefix(child, ns, prefix);
            }
        }


        private static void AddNamespaceToElement(XElement element, XNamespace namespaceToAdd)
        {
            // Update the name of the current element with the namespace
            element.Name = namespaceToAdd + element.Name.LocalName;

            // Recursively add namespace to child elements
            foreach (var childElement in element.Elements())
            {
                AddNamespaceToElement(childElement, namespaceToAdd);
            }
        }              

        private static void ProcessTables(XElement element)
        {
            foreach (var table in element.Descendants("table"))
            {
                var style = table.Attribute("style")?.Value;
                if (style != null && style.Contains("border-collapse:collapse"))
                {
                    string borderStyle = "border: 1px solid black;";
                    ApplyBorderStyle(table, borderStyle); // Apply to table itself

                    foreach (var row in table.Elements("tr"))
                    {
                        ApplyBorderStyle(row, borderStyle); // Apply to each row
                        foreach (var cell in row.Elements("td"))
                        {
                            ApplyBorderStyle(cell, borderStyle); // Apply to each cell
                        }
                        foreach (var cell in row.Elements("th"))
                        {
                            ApplyBorderStyle(cell, borderStyle); // Apply to each header cell
                        }
                    }
                }
            }
        }

        private static void ApplyBorderStyle(XElement element, string borderStyle)
        {
            var existingStyle = element.Attribute("style")?.Value;
            if (string.IsNullOrEmpty(existingStyle))
            {
                element.SetAttributeValue("style", borderStyle);
            }
            else
            {
                element.SetAttributeValue("style", existingStyle + " " + borderStyle);
            }
        }

        private static string RemoveExtraPrefixText(string html)
        {
            // Regex to match and remove extra prefix text like "reqif-xhtml:p"
            var regex = new Regex(@"reqif-xhtml:\w+");
            return regex.Replace(html, string.Empty);
        }
              

        
    }
}
