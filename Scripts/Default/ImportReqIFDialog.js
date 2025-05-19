var importReqIFDialog = null;
var filePath = null;
var adoParamModel;
var errorFilePath = null;
var isValidReqIFFile = true;
$(document).ready(function () {
    VSS.init();

    VSS.ready(function () {
        var extensionCtx = VSS.getExtensionContext();
        var contributionId = extensionCtx.publisherId +
            "." +
            extensionCtx.extensionId +
            ".dialog-ReqIFBridge-ReqIF-Import";

        importReqIFDialog = (function () {
        })();

        // Register form object to be used accross this extension
        VSS.register(contributionId, importReqIFDialog);


        var webContext = VSS.getWebContext();
        VSS.getAccessToken().then(function (token) {
            // Format the auth header
            var accessToken = token.token;
            var collectionId = webContext.collection.id;
            var collectionName = webContext.collection.name;
            var projectId = webContext.project.id;
            var serverUri = encodeURI(webContext.account.uri);
            var userName = webContext.user.uniqueName;
            var accountName = webContext.account.name;
            var projectName = webContext.project.name;

       
            var adoParams = {
                accessToken: accessToken,
                collectionId: collectionId,
                collectionName: collectionName,
                projectId: projectId,
                serverUri: GetCompatiableServerUri(serverUri, collectionName),
                userName: userName,
                accountName: accountName,
                projectName: projectName
            };

            adoParamModel = adoParams;


            hideElementsOnSessionExpiration("main-div", adoParamModel);
            if (adoParamModel) {
                validateLicense(adoParams, '#reqIFBridge-ReqIFImport-MainDiv', '#reqIFBridge-ReqIFExport-LicenseDiv');
            } else {
                $('#main-div').addClass('alert-danger');
            }

        });


    });
    $('#import-confirmation-dialog').initializeConfirmationDialogControl();

    disabledImportBtn();
    disableAnchorTag("expmappinglink");

    $('input[type="file"]').change(function (e) {

        $('#result').removeClass('alert');
        $('#result').removeClass('alert-success');
        $('#result').removeClass('alert-danger');
        $('#result').html('');
        try {
            var fileName = e.target.files[0].name;
            if (fileName) {
                var fileExtension = fileName.split('.').pop();

                if (fileExtension.includes("reqif")) {
                    enableImportBtn();
                } else {
                    disabledImportBtn();
                    $('#result').removeClass('alert');
                    $('#result').removeClass('alert-success');
                    $('#result').removeClass('alert-danger');

                    $('#result').html('');

                    $('#result').addClass('alert');
                    $('#result').text('Unsupported file extension, only reqif files are allowed to upload.');

                    $('#result').removeClass('alert-success');
                    $('#result').addClass('alert-danger');
                }
            } else {
                disabledImportBtn();
            }

        } catch (e) {
            disabledImportBtn();

        }


    });
    kendo.ui.progress.messages = {
        loading: ""
    };


    $("#reqIFFile").kendoUpload({
        multiple: false,
        async: {
            chunkSize: 11000,// bytes
            saveUrl: "/Default/GetReqIFServerPath",
            contentType: "multipart/form-data",

            autoUpload: true
        },

        validation: {
            //allowedExtensions: [".reqif", ".reqifz", ".reqifw", ".7z", ".zip", ".rar"],
            allowedExtensions: [".reqif", ".reqifz"],
            maxFileSize: 1073741823
        },
        localization: {
            select: "Upload ReqIF File",
            uploadSelectedFiles: "Add",
            headerStatusUploaded: "Process complete",
            headerStatusUploading: "Uploading file, please wait...",
            statusFailed: "File processing failed",
            statusUploaded: "File validating...",
            statusUploading: "On-process..."
        },
        select: onSelect,
        remove: onRemove,
        error: onError,
        success: onSuccess,
        
    });

    function onError(e) {
    }


    function onSelect(e) {
        var fileName = e.files[0].name;
        sessionStorage.setItem("onKendoSelectFileName", fileName);
        disableAnchorTag("expmappinglink");
    }

    function onSuccess(data) {

        if (data.response.success && data.response.path != null) {

            if (data.response.message.trim() !== '') {

                isValidReqIFFile = false;
                disabledImportBtn();

                data.preventDefault();
                $('#result').html('&nbsp; '+ data.response.message);

                $('#result').removeClass('alert-success');
                $('#result').addClass('alert-danger');

            }

            else {

                let fileName = sessionStorage.getItem('onKendoSelectFileName');

                filePath = '/Default/ReqIFImport?callingViewId=ImportReqIFDialog&adoParamModel=' + JSON.stringify(adoParamModel) + '&reqIFFilePath=' + JSON.stringify(data.response.path) + '&reqIFFileName=' + JSON.stringify(encodeURIComponent(fileName));


                document.getElementById("expmappinglink").href = filePath;
                sessionStorage.setItem('AdoFilePath', filePath);
                sessionStorage.setItem('reqIFFilePath', data.response.path);

                isValidReqIFFile = true;
                $('#result').removeClass('alert-danger');
                $('#result').html('');
                enableImportBtn();
                enableAnchorTag("expmappinglink");

            }
        }

        else {

            isValidReqIFFile = false;
            disabledImportBtn();

            data.preventDefault();
            $('#result').html('&nbsp; Invalid compression! only zip compression is allowed.');

            $('#result').removeClass('alert-success');
            $('#result').addClass('alert-danger');


        }
    }


    function onRemove(e) {
       
        filePath = null;
        document.getElementById("expmappinglink").href = '#';
    }

    if (reqIfImportFilePath !== null && reqIfImportFilePath) {
        ToggleFileMappingControls();
    }

    if (!isCookiesEnabled()) {

        disablekendoUploadButton();
        $('#result').addClass('alert-danger');
        $('#result').html('&nbsp; Cookies are blocked. please enable the cookies.');
        return;
    }

});
function GetCompatiableServerUri(serverUri, collectionName) {
    var currentServerUri = serverUri.toLowerCase();

    if (!currentServerUri.includes("dev.azure.com") && !currentServerUri.includes("visualstudio.com")) {
        currentServerUri = currentServerUri + collectionName;
    }
    return currentServerUri;
}
function performImport() {

    var webContext = VSS.getWebContext();
    VSS.getAccessToken().then(function (token) {
        var accessToken = token.token;
        var collectionId = webContext.collection.id;
        var collectionName = webContext.collection.name;
        var projectId = webContext.project.id;
        var serverUri = encodeURI(webContext.account.uri);
        var userName = webContext.user.uniqueName;
        var projectName = webContext.project.name;
        var reqIFPath = sessionStorage.getItem('reqIFFilePath');
        var uri = serverUrl + '/Default/PerformImport';
        var formData = new FormData();

        $('#result').addClass('alert');
        //If path is in cache, then no need to validate
        if (!ValidateReqIFFile('Processing ReqIF data for Import...')) {
            return;
        }
        
        formData.append("accessToken", accessToken);
        formData.append("collectionId", collectionId);
        formData.append("collectionName", collectionName);
        formData.append("projectId", projectId);
        formData.append("serverUri", GetCompatiableServerUri(serverUri, collectionName));
        formData.append("userName", userName);
        formData.append("projectName", projectName);
        formData.append("reqIFPath", reqIFPath);
                
        
        $.ajax({
            url: uri,
            data: formData,
            type: 'POST',
            cache: false,
            timeout: 108000000,
            processData: false,
            contentType: false,
            beforeSend: function (xhr) {


            },
            success: function (data) {

                var result = JSON.parse(data);

                if (result.OperationStatus === 1) {

                    startResultTimer(result.Tag);
                } else {
                    $('#result').text('Import failed!');

                    $('#result').removeClass('alert-success');
                    $('#result').addClass('alert-danger');
                }
            },
            error: function (xhr, status, error) {
                $('#result').removeClass('alert-success');
                $('#result').addClass('alert-danger');

                $('#result').html('<span>' + error + '</span>');
                kendo.ui.progress($("body"), false);
                kendo.ui.progress.messages = {
                    loading: ""
                };

            },
            complete: function (xhr, status, error) {

            }
        });
    });
}
function validateMappingFile() {


    var uri = serverUrl + '/Default/ValidateMappingFile';


    if (typeof adoParamModel == "undefined" || adoParamModel == null) {
        $('#result').removeClass('alert');
        $('#result').removeClass('alert-success');
        $('#result').removeClass('alert-danger');

        $('#result').html('');

        $('#result').addClass('alert');
        $('#result').text('Session has been expired. Please refresh the page to continue.');

        $('#result').removeClass('alert-success');
        $('#result').addClass('alert-danger');
        return;
    }

    $.ajax({
        url: uri,
        type: 'POST',
        data: JSON.stringify(adoParamModel),
        cache: false,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        beforeSend: function (xhr) {
            kendo.ui.progress($("body"), true);
        },
        success: function (result) {

            kendo.ui.progress($("body"), false);
            var isfile = false;

            if (result.validation) {
                isfile = true;
            }
            openConfirmationDialog(isfile);
        },
        error: function (xhr, status, error) {
            kendo.ui.progress($("body"), false);
        },
        complete: function (xhr, status, error) {
            kendo.ui.progress($("body"), false);
        }
    });
}
function getFilename(fullPath) {
    return fullPath.replace(/^.*[\\\/]/, '');
}
function ToggleFileMappingControls() {

    var adoFilePath = sessionStorage.getItem('AdoFilePath');
    if (adoFilePath !== null && adoFilePath.length > 0) {
        //1. Disable Kendo Upload
        var upload = $("#reqIFFile").data("kendoUpload");
        upload.disable();

        
        //2. Set File Selection Text
        var fileName = sessionStorage.getItem("onKendoSelectFileName");
        document.getElementById("indicateFileSelection").innerText = "File [" + fileName + "]" + " has already been selected."
        document.getElementById("reloadPageLink").innerText = "(Click here)";
        document.getElementById("clearSelection").innerText = "to clear file selection";


        //3. Assign Configure Link
        document.getElementById("expmappinglink").href = adoFilePath;

        //4. Enable Import Button
        enableImportBtn();
        enableAnchorTag("expmappinglink");
    }
}
function ValidateReqIFFile(text, element) {

    var upload = $("#reqIFFile").data("kendoUpload");
    reqIFFile = upload.getFiles()[0];

    if ((reqIFFile != undefined && isValidReqIFFile) || reqIfImportFilePath) {

        kendo.ui.progress.messages = {
            loading: text
        };
        kendo.ui.progress($("body"), true);

    }
    if (reqIfImportFilePath) {

        return true;
    }

    if (!reqIFFile ) {
        $('#result').removeClass('alert-success');
        $('#result').addClass('alert-danger');

        $('#result').html('<span>Error: ReqIF file is not selected.</span>');
        return false;
    }
    if (!reqIFFile?.name?.toLowerCase().includes('reqif') ) {
        $('#result').removeClass('alert-success');
        $('#result').addClass('alert-danger');

        $('#result').html('<span>Unsupported file extension, only reqif files are allowed to upload..</span>');
        return false;
    }
    if (reqIFFile.size > 1073741823 ) {
        $('#result').removeClass('alert-success');
        $('#result').addClass('alert-danger');

        $('#result').html('<span>Error: File greater than 1GB is not allowed.</span>');
        return false;
    }



    return true;
}
function ReloadMainPage() {
    window.location.href = serverUrl + '/Default/ImportReqIFDialog';
}
function onViewMappingConfirmationOk() {

    var fileName = sessionStorage.getItem('onKendoSelectFileName');
    if (reqIfImportFilePath) {

        filePath = sessionStorage.getItem("AdoFilePath");
        if (filePath !== undefined && filePath !== null) {
            window.location.href = filePath;
        }

        return true;
    }
    var upload = $("#reqIFFile").data("kendoUpload");
    reqIFFile = upload.getFiles()[0].rawFile;

    if (!reqIFFile) {
        $('#result').removeClass('alert-success');
        $('#result').addClass('alert-danger');

        $('#result').html('<span>Error: ReqIF file is not selected.</span>');
        return;
    }

    if (!reqIFFile.name.toLowerCase().includes('reqif')) {
        $('#result').removeClass('alert-success');
        $('#result').addClass('alert-danger');

        $('#result').html('<span>Error: Only ReqIF file is allowed.</span>');
        return;
    }

    if (reqIFFile.size > 1073741823) {
        $('#result').removeClass('alert-success');
        $('#result').addClass('alert-danger');

        $('#result').html('<span>Error: File greater than 1GB is not allowed.</span>');
        return;
    }

    if (typeof adoParamModel == "undefined" || adoParamModel == null) {
        $('#result').removeClass('alert');
        $('#result').removeClass('alert-success');
        $('#result').removeClass('alert-danger');

        $('#result').html('');

        $('#result').addClass('alert');
        $('#result').text('Session has been expired. Please refresh the page to continue.');

        $('#result').removeClass('alert-success');
        $('#result').addClass('alert-danger');
        return;
    }

    var formData = new FormData();
    formData.append("reqIFFile", reqIFFile);

    var uri = serverUrl + '/Default/GetReqIFServerPath';

    $.ajax({
        url: uri,
        data: formData,
        type: 'POST',
        cache: false,
        timeout: 108000000,
        processData: false,
        contentType: false,
        beforeSend: function (xhr) {

            kendo.ui.progress.messages = {
                loading: "Connecting to Azure DevOps Services..."
            };
            kendo.ui.progress($("body"), true);
        },
        success: function (data) {            

            if (data.success && data.path != null) {
                

                filePath = '/Default/ReqIFImport?callingViewId=ImportReqIFDialog&adoParamModel=' + JSON.stringify(adoParamModel) + '&reqIFFilePath=' + JSON.stringify(data.path) + '&reqIFFileName=' + JSON.stringify(encodeURIComponent(fileName));
                window.location.href = filePath;
            }
        },
        error: function (xhr, status, error) {
            kendo.ui.progress($("body"), false);
        },
        complete: function (xhr, status, error) {
            /*kendo.ui.progress($("body"), false);*/
        }
    });



}
function onViewMappingConfirmationNo() {
    $('#import-confirmation-dialog').attr("data-myval", "true"); //setter
    performImport();


}
function onViewMappingConfirmationCancel() {
    performImport();
}
function openConfirmationDialog(isfile) {

    if (isfile) {
        $("#import-confirmation-dialog").openConfirmationDialog({
            onConfirmationDialogOk: onViewMappingConfirmationCancel,
            onConfirmationDialogCancel: onViewMappingConfirmationOk,
            onConfirmationDialogNo: onViewMappingConfirmationNo,
            showCancelButton: true,
            showNoButton: false,
            OkBtnText: "Proceed to Import",
            CancelBtnText: "Configure Mapping",
            NoBtnText: "Import & Close",
            showNoButton: false,
            confirmDialogTitle: "File Exists",
            confirmDialogTxtMsg: "Mapping file has been found. Only defined field values would be considered for mapping." + "\n Do you want to open mapping designer? "
        });


    } else {
        $("#import-confirmation-dialog").openConfirmationDialog({
            onConfirmationDialogOk: onViewMappingConfirmationOk,
            showCancelButton: false,
            OkBtnText: "Configure Mapping",
            confirmDialogTitle: "File not found",
            confirmDialogTxtMsg: "No Mapping file has been found.\n Click 'Configure Mapping' to open mapping designer."
        });
    }
}
function openMappingTemplateConfirmation() {
    $("#import-confirmation-dialog").openConfirmationDialog({
        onConfirmationDialogOk: onViewMappingConfirmationCancel,
        onConfirmationDialogCancel: onViewMappingConfirmationOk,
        onConfirmationDialogNo: onViewMappingConfirmationNo,
        showCancelButton: true,
        showNoButton: false,
        OkBtnText: "Create New Mapping",
        CancelBtnText: "Add Into Existing",
        NoBtnText: "Import & Close",
        showNoButton: false,
        confirmDialogTitle: "File Exists",
        confirmDialogTxtMsg: "Do you want to create new mapping file or wanted to add into existing?"
    });
}
async function startResultTimer(key) {
    var timer = setInterval(function () {
        var status = getResult(key);

        if (status) {
            clearInterval(timer);
        }
    }, 2000);
}
function getResult(key) {

    var uri = serverUrl + '/Default/GetResult' + '?key=' + key;

    var retVal = false;

    $.ajax({
        url: uri,
        type: 'GET',
        cache: false,
        timeout: 108000000,
        async: false,
        contentType: 'text/plain',
        beforeSend: function (xhr) {

        },
        success: function (data) {
           
            var result = JSON.parse(data);
            if (result.OperationStatus !== 4) {
                retVal = true;

                if (result.OperationStatus === 1) {
                    let successMessage = '';

                    if (result.SecondaryObject != null) {
                        var downloadUri = serverUrl + '/Default/DownloadFile?fileName=' + result.SecondaryObject;
                        errorFilePath = downloadUri;
                        successMessage = '<span>ReqIF imported successfully with some error/warnings. Click <a href="' + downloadUri + '">here</a> to view error/warning detail.';
                    }
                    else {
                        successMessage = '<span>ReqIF imported successfully!';
                    }

                    // Add the refresh reminder and close the span
                    successMessage += ' Please refresh to view updates.</span>';


                    // Check if we have an exchange ID (indicated by non-empty Message)
                    if (result.Message && result.Message.trim() !== '') {
                        //successMessage += ' Exchange ID has been found. <a href="#" onclick="createRoundTrip(); return false;">Click here</a> to save ReqIF for roundtrip.';
                        //automatically trigger the roundtrip creation method createRoundTrip
                        createRoundTrip();
                    }             

                    // Set the complete message
                    $('#result').html(successMessage);
                    $('#result').removeClass('alert-danger');
                    $('#result').addClass('alert-success');
                
                }

                else if (result.OperationStatus == 8)
                {
                    $('#result').text(result.Message);

                    $('#result').removeClass('alert-success');
                    $('#result').addClass('alert-danger');
                }
                else {
                    $('#result').text('Import failed!');

                    $('#result').removeClass('alert-success');
                    $('#result').addClass('alert-danger');


                }

                kendo.ui.progress($("body"), false);
                kendo.ui.progress.messages = {
                    loading: ""
                };
            }
        },
        error: function (xhr, status, error) {
            retVal = true;

            $('#result').text('Some error occured!');

            $('#result').removeClass('alert-success');
            $('#result').addClass('alert-danger');
            kendo.ui.progress($("body"), false);
            kendo.ui.progress.messages = {
                loading: ""
            };

        },
        complete: function (xhr, status, error) {
           
        }
    });

    return retVal;
}
function disabledImportBtn() {

    $("#importReqIF").prop("disabled", true);
    $("#importReqIF").addClass("btn-disabled");
    $("#importReqIF").removeClass("btn-info");
}
function enableImportBtn() {

    $("#importReqIF").prop("disabled", false);
    $("#importReqIF").removeClass("btn-disabled");
    $("#importReqIF").addClass("btn-info");
}
function onCloseDialogOk() {
    sessionStorage.setItem("reqIFBridge-ReqIFExport-Action-Dialog-Close", "true");

}
function onImportCloseDialog() {

    $("#import-confirmation-dialog").openConfirmationDialog({
        onConfirmationDialogOk: onCloseDialogOk,
        confirmDialogTitle: "Close Confirmation",
        confirmDialogTxtMsg: "Do you want to close ReqIF Import window?"

    })
}
function onClearCacheOK() {


    var uri = serverUrl + '/Default/ClearCache';


    if (typeof adoParamModel == "undefined" || adoParamModel == null) {
        $('#result').removeClass('alert');
        $('#result').removeClass('alert-success');
        $('#result').removeClass('alert-danger');

        $('#result').html('');

        $('#result').addClass('alert');
        $('#result').text('Session has been expired. Please refresh the page to continue.');

        $('#result').removeClass('alert-success');
        $('#result').addClass('alert-danger');
        return;
    }
    $.ajax({
        url: uri,
        type: 'POST',
        data: JSON.stringify(adoParamModel),
        cache: false,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        beforeSend: function (xhr) {
            kendo.ui.progress($("body"), true);
        },
        success: function (result) {

            kendo.ui.progress($("body"), false);
            $('#result').removeClass('alert');
            $('#result').removeClass('alert-success');
            $('#result').removeClass('alert-danger');
            $('#result').html('');
            $('#result').addClass('alert');
            if (result.operation) {

                $('#result').addClass('alert-success');
                $('#result').removeClass('alert-danger');

                $('#result').text('Cache has been successfully removed.');

            } else {

                $('#result').removeClass('alert-success');
                $('#result').addClass('alert-danger');

                $('#result').text(result.message);
            }



        },
        error: function (xhr, status, error) {
            kendo.ui.progress($("body"), false);
        },
        complete: function (xhr, status, error) {
            kendo.ui.progress($("body"), false);
        }
    });
}
function onClearCache() {

    $("#import-confirmation-dialog").openConfirmationDialog({
        onConfirmationDialogOk: onClearCacheOK,
        showCancelButton: true,
        showNoButton: false,
        OkBtnText: "Yes",
        CancelBtnText: "No",
        confirmDialogTitle: "Clear Cache Confirmation",
        confirmDialogTxtMsg: "It will clear the complete cache of connected project. Are you sure you want to clear cache?"
    });


}
function disablekendoUploadButton() {
    var upload = $("#reqIFFile").data("kendoUpload");
    upload.enable(false);
}
function createRoundTrip() {
    var reqIFPath = sessionStorage.getItem('reqIFFilePath');
    if (!reqIFPath) {
        console.log("Error: ReqIF path is not set.");
        return;
    }

    // Extract the filename from the errorFilePath
    let logFileName = null;
    if (errorFilePath != null) {
        try {
            var url = new URL(errorFilePath);
            logFileName = url.searchParams.get("fileName");
        } catch (e) {
            console.log("Invalid error file path URL.", e);
        }
    }
    let fileName = sessionStorage.getItem('onKendoSelectFileName');

    // Show progress
    kendo.ui.progress($("body"), true);
    kendo.ui.progress.messages = {
        loading: "Creating RoundTrip file..."
    };

    // Call server to create roundtrip file
    $.ajax({
        url: serverUrl + '/Mongo/SaveReqIF',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify({
            model: adoParamModel,
            reqIF_FilePath: reqIFPath,
            logFileName: logFileName,
            reqIF_FileName: fileName
        }),
        success: function (response) {
            if (response.success) {
                console.log("Round trip document has been saved successfully.");
                // Optionally, disable the roundtrip link after success
                $('a[onclick="createRoundTrip(); return false;"]')
                    .replaceWith('<span class="text-muted">Round trip document saved</span>');
            } else {
                console.log("Failed to save round trip document:", response.message);
            }
        },
        error: function (xhr, status, error) {
            console.log("Error saving round trip document:", error);
        },
        complete: function () {
            kendo.ui.progress($("body"), false);
        }
    });
}

