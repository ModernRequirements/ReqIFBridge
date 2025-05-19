using ReqIFBridge.Utility;
using System;
using System.IO;

namespace ReqIFBridge.ReqIF.ReqIFMapper
{
    public class ReqIFTemplateReader
    {
        private ReqIFMappingTemplate mMappingTemplate = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReqIFTemplateReader"/> class.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <param name="version">The version.</param>
        /// <param name="wiProcessTemplateType">Type of the wi process template.</param>
        public ReqIFTemplateReader(string templateName, Version version, string wiProcessTemplateType = null)
        {
            this.mMappingTemplate = new ReqIFMappingTemplate()
            {
                TemplateName = templateName,
                TemplateVersion = version.ToString(),
                WIProcessTemplateType = wiProcessTemplateType
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReqIFTemplateReader"/> class.
        /// </summary>
        /// <param name="mappingTemplate">The mapping template.</param>
        public ReqIFTemplateReader(ReqIFMappingTemplate mappingTemplate)
        {
            this.mMappingTemplate = mappingTemplate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReqIFTemplateReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public ReqIFTemplateReader(Stream stream)
        {
            stream.Position = 0;
            StreamReader reader = new StreamReader(stream);
            string text = reader.ReadToEnd();

            this.mMappingTemplate = text.Deserialize<ReqIFMappingTemplate>();
        }

        /// <summary>
        /// Gets the template.
        /// </summary>
        /// <value>
        /// The template.
        /// </value>
        public ReqIFMappingTemplate Template
        {
            get { return this.mMappingTemplate; }
        }

        /// <summary>
        /// Saves this instance.
        /// </summary>
        /// <returns></returns>
        public Stream Save()
        {
            if (this.mMappingTemplate == null)
            {
                return null;
            }

            try
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(ReqIFMappingTemplate));
                var stream = new MemoryStream();

                // Serialize the mapping template to XML
                serializer.Serialize(stream, this.mMappingTemplate);

                // Reset stream position for reading
                stream.Position = 0;

                return stream;
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                return null;
            }
        }

    }
}
