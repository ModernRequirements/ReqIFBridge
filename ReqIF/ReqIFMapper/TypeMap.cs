using System.Collections.Generic;

namespace ReqIFBridge.ReqIF.ReqIFMapper
{
    public class TypeMap
    {
        /// <summary>
        /// Gets or sets the name of the wi type.
        /// </summary>
        /// <value>
        /// The name of the wi type.
        /// </value>
        public string WITypeName { get; set; }

        /// <summary>
        /// Gets or sets the name of the req if type.
        /// </summary>
        /// <value>
        /// The name of the req if type.
        /// </value>
        public string ReqIFTypeName { get; set; }

        /// <summary>
        /// Gets or sets the field maps.
        /// </summary>
        /// <value>
        /// The field maps.
        /// </value>
        public List<EnumFieldMap> EnumFieldMaps { get; set; }
    }
}
