$.fn.initializeConfirmationDialogControl = function () {
    var containerElementId = this[0].id;

    $.ajax({
        type: "GET",
        async: false,
        url: serverUrl + "/Default/ConfirmationModal?containerId=" + containerElementId,
        
        success: function (data) {
            $('#' + containerElementId).html(data);
        },
        error: function (data) {
        }       
    });
};

$.fn.openConfirmationDialog = function (options) {

    var defaultOpions = {
        onConfirmationDialogOk: function() {},
        onConfirmationDialogCancel: function () { },
        onConfirmationDialogNo: function () { },
        confirmDialogTitle: "Confirmation Dialog",
        confirmDialogTxtMsg: "",
        showCancelButton: true,           
        OkBtnText: "OK",
        CancelBtnText: "Cancel",
        NoBtnText: "",
        showNoButton: false,
        dialogWidth: 380,
        dialogHeight: 130
    };
   
    var settings = $.extend(defaultOpions, options);

    var showCancelBtn = settings.showCancelButton;
    var showNoButton = settings.showNoButton;

    var containerElementId = this[0].id;

    var dialogName = "confirm-dialog-window-" + containerElementId;    
    var dialogTextMsgId = "confirm-dialog-textmsg-" + containerElementId;
    var cancelbtnId = "confirm-dialog-cancel-button-" + containerElementId;
    var oKbtnId = "confirm-dialog-ok-button-" + containerElementId;
    var nobtnId = "confirm-dialog-no-button-" + containerElementId;
    var dialogTitleTextId = "confirm-dialog-titletext-" + containerElementId;

    this.unbind("ConfirmationDialogOk").on("ConfirmationDialogOk", function (e, args) {
       
        settings.onConfirmationDialogOk();
        $("#" + dialogName).modal({
            show: 'false'
        });
    });

    if (showCancelBtn) {
        $("#" + cancelbtnId).show();
        this.unbind("ConfirmationDialogCancel").on("ConfirmationDialogCancel", function (e, args) {
            settings.onConfirmationDialogCancel();
            $("#" + dialogName).modal({
                show: 'false'
            });
            
        });
        $("#" + cancelbtnId).show();
    } else {
        $("#" + cancelbtnId).hide();
    }

    if (showNoButton) {
        this.unbind("ConfirmationDialogNo").on("ConfirmationDialogNo", function (e, args) {
            $("#" + nobtnId).show();
            settings.onConfirmationDialogNo();
            $("#" + dialogName).modal({
                show: 'false'
            });
        });
        $("#" + nobtnId).show();
    } else {
        $("#" + nobtnId).hide();
    }    

    //$("#" + dialogName).modal({
    //    width: settings.dialogWidth,
    //    height: settings.dialogHeight
    //});
   
    
    $("#" + dialogTitleTextId).html(settings.confirmDialogTitle);
    $("#" + dialogTextMsgId).html(settings.confirmDialogTxtMsg);
 
 
    $("#" + cancelbtnId).text(settings.CancelBtnText);
    $("#" + oKbtnId).text(settings.OkBtnText);
    $("#" + nobtnId).text(settings.NoBtnText);
    $("#" + dialogName).modal('show');
};


function onConfirmationDialogActivate(e) {
  
    var containerId = $(this.element[0]).attr("data-parentcontainerid");
    var cancelbtnId = "confirm-dialog-cancel-button-" + containerId;
    var okbtnId = "confirm-dialog-ok-button-" + containerId;
    $('#' + okbtnId).focus();
    
}

function onConfirmationDialogOkButtonClick(e) {
   
    var containerElementId = $(e).attr("data-parentcontainerid");
    $('#' +containerElementId).trigger("ConfirmationDialogOk");
}

function onConfirmationDialogCancelButtonClick(e) {
    
    var containerElementId = $(e).attr("data-parentcontainerid");
    $('#' +containerElementId).trigger("ConfirmationDialogCancel");
}

function onConfirmationDialogNoButtonClick(e) {
    var containerElementId = $(e).attr("data-parentcontainerid");
    $('#' + containerElementId).trigger("ConfirmationDialogNo");

}

function isCookiesEnabled() {

    var result = true;
    try {

        var cookiesEnabled = navigator.cookieEnabled;
        var result = null;
        if (cookiesEnabled) {
            result = true;
        } else {
            result = false;
        }

    } catch (e) {
        //what to do if there is an exception?
    }

    return result;
}

function enableAnchorTag(id) {
    document.getElementById(id).style.pointerEvents = "auto";
    document.getElementById(id).style.cursor = "pointer";
    document.getElementById(id).style.color = "#007bff";
}

function disableAnchorTag(id) {
    document.getElementById(id).style.pointerEvents = "none";
    document.getElementById(id).style.cursor = "default";
    document.getElementById(id).style.color = "#a6a6a6";
}

function hideElementsOnSessionExpiration(mainDivId, adoParams) {
    let parentDiv = document.getElementById(mainDivId);
    if (adoParams === null || adoParams === undefined) {
        if (parentDiv.style.display == '' || parentDiv.style.display == 'none') {
            parentDiv.style.display = 'block';
        }
        parentDiv.innerHTML = "<p>Session has been expired, please refresh to load again</p>";
    } else {
        parentDiv.style.display = 'block';
    }
}
