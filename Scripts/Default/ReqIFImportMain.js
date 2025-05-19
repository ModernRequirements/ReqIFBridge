function onImportMapping(xml,reqifFilePath, successCallback, failureCallback) {
    var uri = serverUrl + '/Default/OnImportMappingFile';

    var returnJson;

    $.ajax({
        url: uri,
        type: "POST",
        data: { "xmlAsString": xml, "reqifSessionObject": reqifFilePath},
        async: false,
        beforeSend: function (xhr) {
            kendo.ui.progress($("body"), true);
        },
        success: function (result) {

            kendo.ui.progress($("body"), false);

            if (result.success) {
                returnJson = JSON.parse(result.mappingjson);
                successCallback(returnJson);
            } else {
                failureCallback();
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
function onExportMapping(json, projectGuid, successCallback, failureCallback) {

    var uri = serverUrl + '/Default/ValidateTemplateOnDownload';
    $.ajax({
        url: uri,
        type: "POST",
        data: { mappingJson: json, projectGuid: projectGuid },
        async: false,
        beforeSend: function (xhr) {
            kendo.ui.progress($("body"), true);
        },
        success: function (result) {

            kendo.ui.progress($("body"), false);

            if (result.validation) {
                $.fileDownload(serverUrl + "/Default/DownloadMappingFile", {
                    successCallback: function (response) {

                    },
                    failCallback: function (response) {

                    },
                    httpMethod: "POST",
                    data: { mappingJson: json, projectGuid: projectGuid }
                });

            } else {
                failureCallback(result.message);
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
function onSaveTemplate(json, projectGuid, successCallback, failureCallback, isSaveAndClose = false) {
    $.ajax({
        type: "POST",
        url: "/Default/SaveMappingFile",
        data: { mappingJson: json, projectGuid: projectGuid },
        success: function (data) {
            if (data) {
                successCallback(data.validation, data.message, isSaveAndClose);
            }
        }
    });

}
function onReqIFImportModalInfoClose() {

    var licStatusViewDiv = document.querySelector('#lic-status-viewid');
    var viewId = licStatusViewDiv.dataset.viewname;

    if (viewId != "" || typeof viewId != 'undefined') {
        window.location.href = serverUrl + '/Default/' + viewId;
    }

}
function getWorkitemFields(workitemType, successCallback, failureCallback) {

    $.ajax({
        type: "GET",
        url: "/Default/GetWorkItemTypeFieldData",
        async: false,
        data: { AdoParams: _adoParams, selectedWorkitemsType: workitemType },
        success: function (data) {
            successCallback(data);
        },
        error: function (xhr, err) {
            failureCallback();
        }
    });

}
$(document).ready(function () {

    $('#import-plugin-confirmation-dialog').initializeConfirmationDialogControl();
    $('#req-if-import-container').reqIFImport().InitializeReqIFImport({

        MappingJSON: projectMappingtemplate,
        workItemAndFields: {
            "workItemAndFieldsData": workitemfieldDataCollection
        },
        workItemTypes: {
            "WorkItemTypes": projectWorkitemtypes
        },
        workItemLinkTypes: {
            "LinkTypes": projectAlllinkstypes

        }
        ,
        projectGuid: currentProjectGuid,
        onSaveFile: null,
        onOpenFile: null,
        onInitComplete: null,
        showProgressBar: function () {
            kendo.ui.progress.messages = {
                loading: ""
            };
            kendo.ui.progress($("body"), true);
        },
        hideProgressBar: function () {
            kendo.ui.progress($("body"), false);
        },
        showProgressBarWithMessage: function () {
            kendo.ui.progress.messages = {
                loading: "Connecting to Azure DevOps Services..."
            };
            kendo.ui.progress($("body"), true);
        },
        onExportMapping: onExportMapping,
        onImportMapping: onImportMapping,
        onSaveTemplate: onSaveTemplate,
        getWorkItemTypesFieldsData: function (workitemType, successCallback, failureCallback) {
            return getWorkitemFields(workitemType, successCallback, failureCallback);
        }
    });

});