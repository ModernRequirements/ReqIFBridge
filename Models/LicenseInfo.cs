using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace ReqIFBridge.Models
{
    [Serializable]
    [System.Runtime.Serialization.DataContract(Name = "LicenseInfo", Namespace = "")]
    public class LicenseInfo
    {
        /// <summary>
        /// Gets or sets a value indicating whether [product defined].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [product defined]; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Name = "productDefined")]
        public bool ProductDefined { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [product activated].
        /// It is used in the validation of the license.
        /// </summary>
        /// <value>
        ///   <c>true</c> if product is already activated or it will become true in the result of the license activation; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Name = "activated")]
        public bool ProductActivated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [license validated].
        /// It is used in the activation of license.
        /// </summary>
        /// <value>
        ///   <c>true</c> if license activation from key or trial version is successfull; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Name = "licenseValidated")]
        public bool LicenseValidated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is trial expired.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is trial expired; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Name = "isTrialExpired")]
        public bool IsTrialExpired { get; set; }

        /// <summary>
        /// Gets or sets the type of the license.
        /// </summary>
        /// <value>
        /// The type of the license.
        /// </value>
        [DataMember(Name = "licenseType")]
        public LicenseType LicenseType { get; set; }

        /// <summary>
        /// Gets or sets the days left.
        /// </summary>
        /// <value>
        /// The days left.
        /// </value>
        [DataMember(Name = "daysLeft")]
        public int DaysLeft { get; set; }

        /// <summary>
        /// Gets or sets the license activation failed message.
        /// </summary>
        /// <value>
        /// The license activation failed message.
        /// </value>
        [DataMember(Name = "licenseActivationFailedMessage")]
        public string LicenseActivationFailedMessage { get; set; }

        /// <summary>
        /// Gets or sets the allowed features.
        /// </summary>
        /// <value>
        /// The allowed features.
        /// </value>
        [DataMember(Name = "allowedFeatures")]
        public List<string> AllowedFeatures { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is all floating seat reserved.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is all floating seat reserved; otherwise, <c>false</c>.
        /// </value>
        [DataMember(Name = "isAllFloatingSeatReserved")]
        public bool IsAllFloatingSeatReserved { get; set; }

        //[DataMember(Name = "stakeHolderOperations")]
        //public List<StakeHolderFeaturesAndOperations> StakeHolderOperations { get; set; }

    }

    [Serializable]
    [DataContract(Name = "LicenseType", Namespace = "")]
    public enum LicenseType
    {
        [EnumMember]
        None,

        [EnumMember]
        FloatingLicense,

        [EnumMember]
        OfflineLicense,

        [EnumMember]
        OnLineLicense,

        [EnumMember]
        TrialLicense,

        [EnumMember]
        StakeHolderLicense,

        [EnumMember]
        NodeLockedLicense,

        [EnumMember]
        NodeLockedUserBasedLicense,

        [EnumMember]
        NodeLockedFloatingLicense,


        [EnumMember]
        MicrosoftAzureLicense
    }
}