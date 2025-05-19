$.fn.initializeExportConfirmationDialogControl = function () {
    var containerElementId = this[0].id;

    $.ajax({
        type: "GET",
        async: false,
        url: serverUrl + "/Default/ExportConfirmationModal?containerId=" + containerElementId,
        success: function (data) {
            $('#' + containerElementId).html(data);
        },
        error: function (data) {
            console.error("Error loading export confirmation modal:", data);
        }
    });
};

$.fn.openExportConfirmationDialog = function (options) {
    var defaultOptions = {
        onConfirmationDialogOk: function () { },
        onConfirmationDialogCancel: function () { },
        confirmDialogTitle: "Confirmation Dialog",
        confirmDialogTxtMsg: "",
        showCancelButton: true,
        OkBtnText: "OK",
        CancelBtnText: "Cancel"
    };

    var settings = $.extend(defaultOptions, options);
    var containerElementId = this[0].id;

    var dialogName = "confirm-export-dialog-window-" + containerElementId;
    var dialogTitleTextId = "confirm-export-dialog-titletext-" + containerElementId;
    var dialogTextMsgId = "confirm-export-dialog-textmsg-" + containerElementId;
    var oKbtnId = "confirm-export-dialog-ok-button-" + containerElementId;
    var cancelbtnId = "confirm-export-dialog-cancel-button-" + containerElementId;

    this.unbind("ConfirmationDialogOk").on("ConfirmationDialogOk", function () {
        settings.onConfirmationDialogOk();
        $("#" + dialogName).modal('hide');
    });

    if (settings.showCancelButton) {
        $("#" + cancelbtnId).show();
        this.unbind("ConfirmationDialogCancel").on("ConfirmationDialogCancel", function () {
            settings.onConfirmationDialogCancel();
            $("#" + dialogName).modal('hide');
        });
    } else {
        $("#" + cancelbtnId).hide();
    }

    $("#" + dialogTitleTextId).html(settings.confirmDialogTitle);
    $("#" + dialogTextMsgId).html(settings.confirmDialogTxtMsg);
    $("#" + cancelbtnId).text(settings.CancelBtnText);
    $("#" + oKbtnId).text(settings.OkBtnText);
    $("#" + dialogName).modal('show');
};

function onConfirmationDialogOkButtonClick(e) {
   
    var containerElementId = $(e).attr("data-parentcontainerid");
    $('#' + containerElementId).trigger("ConfirmationDialogOk");
}

function onConfirmationDialogCancelButtonClick(e) {

    var containerElementId = $(e).attr("data-parentcontainerid");
    $('#' + containerElementId).trigger("ConfirmationDialogCancel");
}
