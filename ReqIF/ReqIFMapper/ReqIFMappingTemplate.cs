using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReqIFBridge.ReqIF.ReqIFMapper
{
    public class ReqIFMappingTemplate
    {
        /// <summary>
        /// Gets or sets the name of the template.
        /// </summary>
        /// <value>
        /// The name of the template.
        /// </value>
        public string TemplateName { get; set; }

        /// <summary>
        /// Gets or sets the template version.
        /// </summary>
        /// <value>
        /// The template version.
        /// </value>
        public string TemplateVersion { get; set; }

        /// <summary>
        /// Gets or sets the type of the wi process template.
        /// </summary>
        /// <value>
        /// The type of the wi process template.
        /// </value>
        public string WIProcessTemplateType { get; set; }

        /// <summary>
        /// Gets or sets the type maps.
        /// </summary>
        /// <value>
        /// The type maps.
        /// </value>
        public List<TypeMap> TypeMaps { get; set; }

        /// <summary>
        /// Gets or sets the link maps.
        /// </summary>
        /// <value>
        /// The link maps.
        /// </value>
        public List<LinkMap> LinkMaps { get; set; }
    }
}
