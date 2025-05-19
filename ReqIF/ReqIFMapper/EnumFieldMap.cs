using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReqIFBridge.ReqIF.ReqIFMapper
{
    public class EnumFieldMap : FieldMap
    {
        /// <summary>
        /// Gets or sets the enum value maps.
        /// </summary>
        /// <value>
        /// The enum value maps.
        /// </value>
        public List<EnumValueMap> EnumValueMaps { get; set; }
    }
}
