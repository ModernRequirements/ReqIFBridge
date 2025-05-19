var JSUtility = {

    showVstsMessageDialog: function (title, message, callback) {
        VSS.getService(VSS.ServiceIds.Dialog).then(function (dialogService) {
            var dialogOptions = {
                title: title,
                width: 500,
                useBowtieStyle: false,
                resizable: false,
                buttons: [{ "text": "OK" }]
            };
            dialogService.openMessageDialog(message, dialogOptions).then(function (dialog) {
                if (typeof callback == 'function') {
                    callback();
                }
            });
        });
    }
};