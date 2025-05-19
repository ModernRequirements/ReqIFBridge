using ReqIFBridge.Models;
using System;

/// <summary>
/// Data Transfer Object for saving ReqIF documents.
/// </summary>
[Serializable]
public class SaveReqIFDTO
{
    /// <summary>
    /// Azure DevOps parameters including project and authentication details.
    /// </summary>
    public AdoParams model { get; set; }

    /// <summary>
    /// File path to the ReqIF document.
    /// </summary>
    public string reqIF_FilePath { get; set; }

    /// <summary>
    /// Name of the ReqIF file.
    /// </summary>
    public string reqIF_FileName { get; set; }

    /// <summary>
    /// File path to the mapping template used for ReqIF processing.
    /// </summary>
    public string reqIF_MappingTemplatePath { get; set; }

    /// <summary>
    /// File path to the binding configuration for ReqIF processing.
    /// </summary>
    public string reqIF_BindingPath { get; set; }

    /// <summary>
    /// File path to store error logs generated during ReqIF processing.
    /// </summary>
    public string logFileName { get; set; }
}
