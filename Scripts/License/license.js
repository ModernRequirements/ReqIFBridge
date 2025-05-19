var mandatoryFieldErrorMessage = "Please fill the mandatory field(s).";
var failResponseMessage = "Unable to create file in specified directory.";
var successResponseMessage = "Your request file has been generated successfully.";
var invalidLicenseCodeMessage = "Your license code is invalid.";
var CONST_PRODUCT_NOT_ACTIVATED = "Your product is not activated.";
var invalidLicenseKeyMessage = "Entered Key is invalid";
var trialLicenseTemplate = '<span>You are using a trial version of this product.</span> <br> <br><span style = "color:#FF0000";>* <i style="font-size: smaller;">only 30 records per transaction are allowed in trial version</i></span >';
var notActivatedTemplate = '<div id="lic-status-msg" class="alert alert-warning" style="padding-left: 0.5rem !important;">Your product is not activated.</div >';
var licenseExpireMessage = "This license will expire today.";
var licenseExpiredMessage = "Your license has expired.";
var licensePermanentMessage = "License duration is <b>PERMANENT</b>.";
var activatedProductMessage = "Your Product is <b>ACTIVATED</b>.";
// License Activation Methods

function validateLicense(adoParams,viewDivID,licenseDivID) {
   
      $.ajax({
        url: serverUrl + '/License/ValidateLicense',
        data: JSON.stringify(adoParams),
        type: 'POST',
        cache: false,
        async: false,
        contentType: "application/json; charset=utf-8",
        beforeSend: function () {

        },
        success: function (result) {

            if (result != "" || typeof result !== 'undefined') {
                if (result.isActivated) {
                    $(viewDivID).show();
                    $(licenseDivID).hide();
                    if (result.data.LicenseType == 4) {
                        $("#lic-trial-notification").show();
                    }
                    $("#LicenseStatusDiv").show();
                } else {
                    $(viewDivID).hide();
                    $(licenseDivID).show();
                    setOfflineActivationCheckbox();

                    if (result.message == CONST_PRODUCT_NOT_ACTIVATED) {
                        $('#lic-msg').addClass('alert-warning');
                        $('#lic-msg').removeClass('alert-danger');
                        $('#lic-msg').removeClass('alert-success');

                    } else {
                        $('#lic-msg').removeClass('alert-warning');
                        $('#lic-msg').addClass('alert-danger');
                        $('#lic-msg').removeClass('alert-success');

                    }
                    $("#lic-msg").html(result.message);
                    $("#LicenseStatusDiv").hide();
                }
            }

        },
        complete: function () {

        },
        error: function (xhr, status, error) {

        },
    });

}

function setOfflineActivationCheckbox() {

    if ($("#lic-chkOfflineActivation").is(":checked")) {

        $("#btnGenerateRequestFile").show();
        $("#lic-offlineActivation-container").show();
        $("#btnActivate").hide();
        $("#lic-offline-response-area").show();

    } else {

        $("#btnGenerateRequestFile").hide();
        $("#lic-offlineActivation-container").hide();
        $("#btnActivate").show();
        $("#lic-offline-response-area").hide();
    }

}

function onChkOfflineActivationClick(event) {
    setOfflineActivationCheckbox();
}


function regexValidation(pattern, text) {
    var result = pattern.test(text.toLowerCase());
    if (result) {
        return true;
    }
    return false;
}

function onActivateClick() {
       
    var licKey = $.trim($("#txtLicKey").val());

    if (licKey == "" || typeof licKey == 'undefined')
    {
        $('#lic-msg').removeClass('alert-warning');
        $('#lic-msg').addClass('alert-danger');
        $("#lic-msg").html(mandatoryFieldErrorMessage);
        return;
    }


    if (!ValidateLicenseKey(licKey)){
        $('#lic-msg').removeClass('alert-warning');
        $('#lic-msg').addClass('alert-danger');
        $("#lic-msg").html(invalidLicenseKeyMessage);
        return;
    }



    if (licKey.length < 39)
    {
        $('#lic-msg').removeClass('alert-warning');
        $('#lic-msg').addClass('alert-danger');
        $("#lic-msg").html(invalidLicenseCodeMessage);
        return;
    }    

    var licenseType = 'OnLineLicense';
    var licenseServerAddress = '';

    $.ajax({
        url: serverUrl + '/License/ActivateProductLicense?licenseType=' + licenseType + '&licenseServerURL=' + licenseServerAddress,
        type: 'post',
        data: { licKey: licKey },
        complete: function () {
            kendo.ui.progress($("body"), false);
        },
        beforeSend: function () {
            kendo.ui.progress.messages = {
                loading: ""
            };
             kendo.ui.progress($("body"), true);
        },
        success: function (data) {
            onLicenseActivationSuccess(data);
        },
        error: function (a, bn, sd) {

        }
    }); 
   
}

function onTrialClick() {
    kendo.ui.progress.messages = {
        loading: ""
    };
    kendo.ui.progress($("body"), true)  

    window.setTimeout(function () {
        $.ajax({
            url: serverUrl + '/License/ActivateTrial',
            type: 'post',
            data: {},
            complete: function () {
                kendo.ui.progress($("body"), false)
            },
            beforeSend: function () {

            },
            success: function (data) {
                onLicenseActivationSuccess(data);

            },
            error: function (a, bn, sd) {
                kendo.ui.progress($("body"), false)
            }
        });

    }, 2000);

   



}

function onLicenseActivationSuccess(data) {

   
    if (data.validated) {
        $('#lic-msg').removeClass('alert-warning');
        $('#lic-msg').removeClass('alert-danger');
        $('#lic-msg').addClass('alert-success');
        $("#lic-msg").html(data.reason);
        window.setTimeout(function () {           
            location.reload();

        }, 3000);
    }
    else {

        if (data.reason == CONST_PRODUCT_NOT_ACTIVATED) {
            $('#lic-msg').addClass('alert-warning');
            $('#lic-msg').removeClass('alert-danger');
            $('#lic-msg').removeClass('alert-success');

        } else {
            $('#lic-msg').removeClass('alert-warning');
            $('#lic-msg').addClass('alert-danger');
            $('#lic-msg').removeClass('alert-success');

        }
        $("#lic-msg").html(data.reason);

    }
 
}

function ontxtLicKeyKeyPress(e) {
   
    var licKey = $.trim($("#txtLicKey").val());
    if (!licKey) {
        disableActivateBTn();
    } else {
        enableActivateBTn();
        if (e.keyCode == 13) {
            onActivateClick();
        }
    }
}

function onGenerateRequestFileClick() {
   
    var licKey = '';
    licKey = $.trim($("#txtLicKey").val());

    if (!ValidateLicenseKey(licKey)) {
        $('#lic-msg').removeClass('alert-warning');
        $('#lic-msg').addClass('alert-danger');
        $("#lic-msg").html('Entered Key is invalid');
        return;
    }

    var specialChars = "<>!#$%^&*()_+[]{}?:;|'\"\\,./~`=";
    var check = function (string) {
        for (i = 0; i < specialChars.length; i++) {
            if (string.indexOf(specialChars[i]) > -1) {
                return true;
            }
        }
        return false;
    };

    if (check(licKey) == true) {

        $('#lic-msg').removeClass('alert-warning');
        $('#lic-msg').removeClass('alert-success');
        $('#lic-msg').addClass('alert-danger');
        $("#lic-msg").html('Incorrect activation key.');
        return;
    }


    try {
        licenseServerAddress = '';        
        kendo.ui.progress.messages = {
            loading: ""
        };
        kendo.ui.progress($("body"), true);
        $.fileDownload(serverUrl + "/License/GenerateOfflineRequestFile", {
            successCallback: function (response) {

                $("#lic-chkOfflineActivation").prop('checked', true);
                kendo.ui.progress($("body"), false);
            },
            failCallback: function (response) {
                alert('Your file download just failed for this URL:' + response);
                $('#lic-msg').removeClass('alert-warning');
                $('#lic-msg').removeClass('alert-success');
                $('#lic-msg').addClass('alert-danger');
                $("#lic-msg").html(failResponseMessage);

                kendo.ui.progress($("body"), false);
            },
            httpMethod: "POST",
            data: { "licKey": licKey }
        });
        window.setTimeout(function () {
            kendo.ui.progress($("body"), false);
            $('#lic-chkOfflineActivation').prop('checked', true);
            setOfflineActivationCheckbox();
            $('#lic-msg').removeClass('alert-warning');
            $('#lic-msg').removeClass('alert-danger');
            $('#lic-msg').addClass('alert-success');
            $("#lic-msg").html(successResponseMessage);

        }, 2000);
    }
    catch (err) {
        console.log(err.message);
        kendo.ui.progress($("body"), false);
    }

}

function onUploadFileBeforeUploading(e) {
    //resetErrorNotification();
}

function onUploadFileAfterUploading(e) {
  
    if (e == undefined || e.response == undefined) {
        return;
    }
    
    onLicenseActivationSuccess(e.response)
   
}

function onUploadFileError(e) {

    var errorMsg = 'Unable to upload the file.';

    if (e.XMLHttpRequest.status != undefined) {
        var fileName = null;
        if (e.files != undefined && e.files[0] != undefined) {
            fileName = e.files[0].name;
            errorMsg = errorMsg + ' \'' + fileName + '\'';
        }
    }


    $('#lic-msg').removeClass('alert-warning');
    $('#lic-msg').addClass('alert-danger');
    $("#lic-msg").html(errorMsg);
    uploadFileWindowRemoveSelectedFile();

}

function uploadFileWindowRemoveSelectedFile() {

    //reset all the upload things!
    $(".k-upload-files").remove();
    $(".k-upload-status").remove();
    $(".k-upload.k-header").addClass("k-upload-empty");
    $(".k-upload-button").removeClass("k-state-focused");
}

function onUploadFileProgress(e) {
    // disable the remove action while file is uploading
    $(".k-button-bare").removeClass("k-upload-action");
    $(".k-button-bare").addClass("k-state-disabled");
}

// License Status Methods

$(document).ready(function () {

disableActivateBTn();


    $('#txtLicKey').on('input', function (e) {
        ontxtLicKeyKeyPress(e)
    });
    $('#open-lic-dialog').on('click', function (e) {

        $.ajax({
            url: serverUrl + '/License/LicenseStatus',
            data: "",
            type: 'POST',
            cache: false,
            async: false,
            contentType: "application/json; charset=utf-8",
            success: function (result) {
                if (result.ProductActivated && result.LicenseType.toLowerCase() != "trial") {
                    $("#lic-prod-div").html(activatedProductMessage);
                    $("#lic-type-div").html("License type is <b>" + result.LicenseType.toUpperCase() + "</b>.");
                    if (result.LicenseStatus.toString() != "Permanent") {

                        switch (result.LicenseStatus.toString()) {
                            case "0":
                                $("#lic-status-type-div").html(licenseExpireMessage);
                                break;
                            case "Expired":
                                $("#lic-status-type-div").html(licenseExpiredMessage);
                                break;
                            default: $("#lic-status-type-div").html("<label>This license will be expired in</label>" + " " + result.LicenseStatus + " days.")
                        }
                    }
                    else {
                        $("#lic-status-type-div").html(licensePermanentMessage)
                    }

                }
                else if (result.ProductActivated && result.LicenseType.toLowerCase() == "trial") {
                    $("#lic-status-type-div").html(trialLicenseTemplate);
                }
                else {
                    $("#lic-status-type-div").html(notActivatedTemplate);
                }
                if (result.ProductActivated) {
                    $("#lic-cancel-btn").show();
                }
            },
            complete: function () {

                $('#licenseModalInfo').modal('show');
            },
            error: function () {
                console.log("failed to retreive license status");
            }

        });
    })


});

function onBtnLicenseStatusClose() {
 
    var licStatusViewDiv = document.querySelector('#lic-status-viewid');
    var viewId = licStatusViewDiv.dataset.viewname;

    if (viewId != ""  || typeof viewId != 'undefined') {
        window.location.href = serverUrl + '/Default/' + viewId;           
    }
    
}

function onBtnClearLicense(viewId) {
    kendo.ui.progress.messages = {
        loading: ""
    };
    kendo.ui.progress($("#licenseModalInfo"), true);

    window.setTimeout(function () {
        $.ajax({
            url: serverUrl + '/License/ClearLicenseInfo',
            data: "",
            type: 'POST',
            cache: false,
            async: false,
            contentType: "application/json; charset=utf-8",
            complete: function () {

                kendo.ui.progress($("#licenseModalInfo"), false);
            },
            beforeSend: function () {



            },
            success: function (result) {


                if (result != "" || typeof result !== 'undefined') {
                    $("#lic-clear-msg").show();
                    if (result.isRemoved) {

                        $('#lic-clear-msg').removeClass('alert-warning');
                        $('#lic-clear-msg').removeClass('alert-danger');
                        $('#lic-clear-msg').addClass('alert-success');
                        $("#lic-clear-msg").html(result.message);

                        window.setTimeout(function () {
                            onBtnLicenseStatusClose();

                        }, 3000);
                    } else {
                        $('#lic-clear-msg').removeClass('alert-warning');
                        $('#lic-clear-msg').addClass('alert-danger');
                        $('#lic-clear-msg').removeClass('alert-success');
                        $("#lic-clear-msg").html(result.message);
                    }
                }

            },

            error: function (xhr, status, error) {
                kendo.ui.progress($("#licenseModalInfo"), false);
            },
        });
    }, 2000);

   

}

function enableActivateBTn() {
    
    $("#btnActivate").prop("disabled", false);
    
    $("#btnActivate").removeClass("btn-disabled");
    $("#btnActivate").addClass("btn-info");
}
function disableActivateBTn() {

    $("#btnActivate").prop("disabled", true);
    
    $("#btnActivate").addClass("btn-disabled");
    $("#btnActivate").removeClass("btn-info");
}


function ValidateLicenseKey(key) {

    var result = false;
    //function will return true if and only if the key is valid.
    if (!key) {
        return result;
    }

    var alphaNumerics = key.split('-');
    if (alphaNumerics.length == 8) {

        var pattern = /^[a-z0-9]{4}$/;
        for (var i = 0; i < alphaNumerics.length; i++) {            
            var result = regexValidation(pattern, alphaNumerics[i]);

            if (!result) {
                return result;
            }
        }
        result = true;       
    } else {
        result = false;
    }

    return result;

}