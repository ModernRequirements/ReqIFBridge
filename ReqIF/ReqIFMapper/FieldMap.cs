using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReqIFBridge.ReqIF.ReqIFMapper
{
    public class FieldMap
    {
        /// <summary>
        /// Gets or sets the name of the wi field.
        /// </summary>
        /// <value>
        /// The name of the wi field.
        /// </value>
        public string WIFieldName { get; set; }

        /// <summary>
        /// Gets or sets the name of the req if field.
        /// </summary>
        /// <value>
        /// The name of the req if field.
        /// </value>
        public string ReqIFFieldName { get; set; }

        /// <summary>
        /// Gets or sets the type of the field.
        /// </summary>
        /// <value>
        /// The type of the field.
        /// </value>
        public FieldTypes FieldType { get; set; }

        public FieldTypes ReqIFFieldType { get; set; }

        /// <summary>
        /// Gets or sets the req if field null then.
        /// </summary>
        /// <value>
        /// The req if field null then.
        /// </value>
        public string ReqIfFieldNullThen { get; set; }
    }
}
