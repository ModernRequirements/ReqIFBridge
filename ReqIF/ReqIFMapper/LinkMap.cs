using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReqIFBridge.ReqIF.ReqIFMapper
{
    public class LinkMap
    {
        /// <summary>
        /// Gets or sets the name of the wi link.
        /// </summary>
        /// <value>
        /// The name of the wi link.
        /// </value>
        public string WILinkName { get; set; }

        /// <summary>
        /// Gets or sets the name of the req if relation.
        /// </summary>
        /// <value>
        /// The name of the req if relation.
        /// </value>
        public string ReqIFRelationName { get; set; }

        /// <summary>
        /// Gets or sets the field maps.
        /// </summary>
        /// <value>
        /// The field maps.
        /// </value>
        public List<FieldMap> FieldMaps { get; set; }
    }
}
