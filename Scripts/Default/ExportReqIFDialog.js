var adoParamModel;
// Add this near the top of ExportReqIFDialog.js, after the var declarations
const EXCHANGE_ID_DROPDOWN = 'exchange-id-dropdown-export-option-confirmation-dialog';

$(document).ready(function () {

    VSS.init();

    var exportReqIFDialog = (function () {
    })();

    // Register form object to be used across this extension
    VSS.register("dialog-ReqIFBridge-ReqIF-Export", exportReqIFDialog);

    VSS.ready(function () {

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

            var workItems = sessionStorage.getItem('reqIFBridge-ReqIFExport-WorkItemIDs');
            var workItemsArray = Array.from(workItems.split(','), Number);

            var adoParams = {
                accessToken: accessToken,
                collectionId: collectionId,
                collectionName: collectionName,
                projectId: projectId,
                serverUri: GetCompatiableServerUri(serverUri, collectionName),
                userName: userName,
                accountName: accountName,
                projectName: projectName,
                workItemIds: workItemsArray
            };
            adoParamModel = adoParams;

            var wiql = sessionStorage.getItem('reqIFBridge-ReqIFExport-Wiql');

            if (wiql && workItems.length == 0) {
                adoParams.query = wiql;
                let uri = serverUrl + '/Default/GetWorkItemsIdsByQuery';

                $.ajax({
                    url: uri,
                    data: JSON.stringify(adoParams),
                    type: 'POST',
                    cache: false,
                    timeout: 108000000,
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    async: false,
                    beforeSend: function (xhr) {
                        kendo.ui.progress($("body"), true);
                    },
                    success: function (data) {
                        if (data.success) {
                            var workItemIds = data.result;
                            sessionStorage.setItem('reqIFBridge-ReqIFExport-WorkItemIDs', workItemIds);
                            adoParams.workItemIds = workItemIds;
                            if (data.nonFlat) {
                                $('#non_flat').html('<i style="font-size: smaller; color:#FF0000;">' + '*Depending on the Query/Selection, Only single instance of a work item with its links would be considered for export and shown below' + '</i>');
                            } else {
                                $('#non_flat').empty();
                            }
                        }
                    },
                    error: function (xhr, status, error) {
                        $('#result').addClass('alert');
                        $('#result').removeClass('alert-success');
                        $('#result').addClass('alert-danger');
                        $('#result').html('<span>' + error + '</span>');
                        kendo.ui.progress($("body"), false);
                    },
                    complete: function (xhr, status, error) {
                    }
                });
            }

            hideElementsOnSessionExpiration("main-div", adoParams);
            if (adoParams) {
                validateLicense(adoParams, '#reqIFBridge-ReqIFExport-MainDiv', '#reqIFBridge-ReqIFExport-LicenseDiv');
                uri = serverUrl + '/Default/ShowWorkItemGrid';

                $.ajax({
                    url: uri,
                    data: JSON.stringify(adoParams),
                    type: 'POST',
                    cache: false,
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    beforeSend: function () {
                        kendo.ui.progress($("body"), true);
                    },
                    success: function (data) {
                     
                        var grid = $("#Grid").data("kendoGrid");
                        if (grid) {
                            grid.dataSource.data(data);
                        } else {
                            // Initialize the Kendo Grid if it doesn't exist
                            $("#Grid").kendoGrid({
                                dataSource: {
                                    data: data,
                                    pageSize: 10
                                },
                                height: 380,
                                scrollable: true,
                                sortable: true,
                                pageable: {
                                    input: true,
                                    numeric: false
                                },
                                filterable: true,
                                columns: [
                                    { field: "ID", title: "ID", width: 70 },
                                    { field: "WorkItemType", title: "WorkItem Type", width: 130 },
                                    { field: "Title", title: "Title", width: 530, template: "<div style='display: flex; align-items: center;'><img src='#= IconUrl #' alt='Icon' style='width: 17px; height: 17px; margin-right: 10px;' /><span style='overflow: hidden; text-overflow: ellipsis; white-space: nowrap; width: 550px;'>#:Title#</span></div>" },
                                    { field: "AssignedTo", title: "Assigned To", width: 140 },
                                    { field: "State", title: "State", width: 90, template: "<div style='display: flex; align-items: center;'><div style='border-radius: 50%; margin-top: 7px; margin-bottom: 7px; margin-right: 6px; width: 8px; height: 8px; min-width: 8px; background-color: \\##=Statecolor #;'></div><span>#: State #</span></div>" }
                                ]
                            });

                        }
                    },
                    complete: function () {
                        adoParamModel = adoParams;
                        kendo.ui.progress($("body"), false);
                    },
                    error: function (xhr, status, error) {
                        $('#gridContent').html('<span>Some error occurred while processing the request.</span>');
                        $('#exportReqIF').attr('disabled', 'disabled');
                    },
                });
            } else {
                $('#main-div').addClass('alert-danger');
            }
        });
    });

    $('#export-confirmation-dialog').initializeConfirmationDialogControl();
    $('#export-option-confirmation-dialog').initializeExportConfirmationDialogControl();
    kendo.ui.progress.messages = {
        loading: ""
    };

    if (!isCookiesEnabled()) {
        $('#exportReqIF').attr('disabled', 'disabled');
        disableAnchorTag('expmappinglink');
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

// Update performExport to remove roundTrip parameter
function performExport(title, exportTool, reqIfFileName, exchangeId) {
    $('#result').removeClass('alert alert-success alert-danger').html('');

    var webContext = VSS.getWebContext();
    VSS.getAccessToken().then(function (token) {
        var adoParams = {
            accessToken: token.token,
            collectionId: webContext.collection.id,
            collectionName: webContext.collection.name,
            projectId: webContext.project.id,
            serverUri: GetCompatiableServerUri(encodeURI(webContext.account.uri), webContext.collection.name),
            userName: webContext.user.uniqueName,
            accountName: webContext.account.name,
            projectName: webContext.project.name,
            workItemIds: Array.from(sessionStorage.getItem('reqIFBridge-ReqIFExport-WorkItemIDs').split(','), Number)
        };

        var exportDto = {
            ExportType: exportTool,
            Specification_Title: title,
            ReqIF_FileName: reqIfFileName,
            Exchange_Id: exchangeId
        };

        $.ajax({
            url: serverUrl + '/Default/PerformExport',
            type: 'POST',
            data: JSON.stringify({
                adoParams,  // Send as separate properties instead of nested object
                exportDto
            }),
            traditional: true, // Add this to handle complex objects
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            beforeSend: function () {
                kendo.ui.progress($("body"), true);
            },
            success: function (data) {
                // Remove the extra JSON.parse since the dataType is already 'json'
                var result = JSON.parse(data);
                if (result.OperationStatus === 1) {
                    startResultTimer(result.Tag);
                } else {
                    $('#result').addClass('alert-danger').text('Export failed!');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error details:', {
                    status: status,
                    error: error,
                    responseText: xhr.responseText
                });
                $('#result').addClass('alert-danger').html('<span>' + error + '</span>');
                kendo.ui.progress($("body"), false);
            },
            complete: function () {
                //kendo.ui.progress($("body"), false);
            }
        });
    });
}

async function startResultTimer(key) {
    var timer = setInterval(function () {
        var status = getResult(key);

        if (status) {
            clearInterval(timer);
            kendo.ui.progress($("body"), false);
        }
    }, 3000);
}
function getResult(key) {

    var uri = serverUrl + '/Default/GetResult?key=' + key;


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
                $('#result').addClass('alert');
                if (result.OperationStatus === 1) {

                    var secObj = result.SecondaryObject;
                    var downloadUri = serverUrl + '/Default/DownloadFile?fileName=' + result.Tag;
                    var loggerUri = serverUrl + '/Default/DownloadFile?fileName=' + result.SecondaryObject;

                    $('#result').addClass('alert-success');
                    $('#result').removeClass('alert-danger');

                    if (secObj) {

                        $('#result').html(' ReqIF exported successfully with some errors/warnings (click<a href="' + loggerUri + '"> logs </a> to view) .Click <a href="' + downloadUri + '">here</a> to download ReqIF file');

                    } else {
                        $('#result').html('<span>ReqIF exported successfully! Click <a href="' + downloadUri + '">here</a> to download.</span>');
                    }

                   


                } else {
                    $('#result').text('Export failed!');

                    $('#result').removeClass('alert-success');
                    $('#result').addClass('alert-danger');
                }
            }
        },
        error: function (xhr, status, error) {
            retVal = true;

            $('#result').text('Some error occured!');

            $('#result').removeClass('alert-success');
            $('#result').addClass('alert-danger');

            kendo.ui.progress($("body"), false);
        },
        complete: function (xhr, status, error) {
            kendo.ui.progress($("body"), false);
        }
    });

    return retVal;
}
function validateMappingFile() {


    var uri = serverUrl + '/Default/ValidateMappingFile';


    if (typeof adoParamModel == "undefined" || adoParamModel == null) {
        ViewSessionError();
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
function onViewMappingConfirmationOk() {
    if (typeof adoParamModel == "undefined" || adoParamModel == null) {
        ViewSessionError();
        return;
    }
    kendo.ui.progress.messages = {
        loading: "Connecting to Azure DevOps Services..."
    };
    kendo.ui.progress($("body"), true);


    var uri = serverUrl + '/Default/ReqIFExportData';

    var data1 = JSON.stringify(adoParamModel);
    $.ajax({
        url: uri,
        type: 'POST',
        data: data1,
        cache: false,
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        beforeSend: function (xhr) {
            /* kendo.ui.progress($("body"), true);*/
        },
        success: function (result) {

            window.location.href = '/Default/ReqIFExport';
        },
        error: function (xhr, status, error) {
            kendo.ui.progress($("body"), false);
        },
        complete: function (xhr, status, error) {
            //kendo.ui.progress($("body"), false);
        }
    });

}
function ViewSessionError() {
    $('#result').removeClass('alert');
    $('#result').removeClass('alert-success');
    $('#result').removeClass('alert-danger');

    $('#result').html('');

    $('#result').addClass('alert');
    $('#result').text('Session has been expired. Please refresh the page to continue.');

    $('#result').removeClass('alert-success');
    $('#result').addClass('alert-danger');
}



function onExportConfirmationOk() {
    // Clear previous messages
    $('#result').removeClass('alert alert-success alert-warning alert-danger').html('');

    var modalId = 'export-option-confirmation-dialog';
    var exportType = $('#export-type-dropdown-' + modalId).val();
    var specification_title = $('#confirm-export-dialog-input-' + modalId).val();
    var exportTool = $('#confirm-export-dialog-dropdown-' + modalId).val();
    var reqIfFileName = $('#reqif-file-name-input-' + modalId).val();
    var exchangeId = $('#exchange-id-dropdown-' + modalId).val();

    // If export type is not roundtrip, directly perform export
    if (exportType !== 'useRoundTrip') {
        performExport(specification_title, exportTool, reqIfFileName, null);
        return;
    }

    // For roundtrip export, validate exchange ID and perform comparison
    var dropdown = document.getElementById(EXCHANGE_ID_DROPDOWN);
    if (!dropdown) {
        console.error('Exchange ID dropdown not found');
        $('#result').addClass('alert alert-danger')
            .html('<span>Exchange ID dropdown not found</span>');
        return false;
    }

    var selectedOption = dropdown.options[dropdown.selectedIndex];
    if (!selectedOption) {
        console.error('No option selected in Exchange ID dropdown');
        $('#result').addClass('alert alert-danger')
            .html('<span>Please select an Exchange ID</span>');
        return false;
    }

    // Get work item IDs
    var workItemIds = [];
    if (typeof adoParamModel === 'undefined' || !adoParamModel || !adoParamModel.WorkItemIds) {
        var workItemsStr = sessionStorage.getItem('reqIFBridge-ReqIFExport-WorkItemIDs');
        if (workItemsStr) {
            workItemIds = Array.from(workItemsStr.split(','), Number);
        } else {
            console.error('No work item IDs found in session storage');
            $('#result').addClass('alert alert-danger')
                .html('<span>Work item IDs not found. Please refresh the page and try again.</span>');
            return false;
        }
    } else {
        workItemIds = adoParamModel.WorkItemIds;
    }

    // Get binding infos from the selected option
    var bindingInfos = JSON.parse(selectedOption.getAttribute('data-bindinginfos') || "{}");

    // Make the server-side comparison only for roundtrip
    $.ajax({
        url: serverUrl + '/Default/CompareWorkItems',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            workItemIds: workItemIds,
            bindingInfos: bindingInfos
        }),
        success: function (response) {
            if (!response.success) {
                $('#result').addClass('alert alert-danger')
                    .html('<span>' + (response.message || 'Error comparing work items') + '</span>');
                return;
            }

            if (response.hasDiscrepancies) {
                // ... existing discrepancy handling code ...
                var messageWithLink = response.mainMessage.replace(
                    "Click here",
                    "<a href='#' class='details-link'>Click here</a>"
                );

                var alertClass = response.data.scenario === 3 ? 'alert-danger' : 'alert-warning';
                var messageHtml = '<div class="alert ' + alertClass + '">' +
                    '<div class="main-message">' + messageWithLink + '</div>';

                if (response.data.scenario !== 3) {
                    messageHtml +=
                        '<div class="action-buttons" style="margin-top: 10px;">' +
                        '<a href="#" class="proceed-link btn btn-sm btn-success" style="margin-right: 10px;">Yes, Proceed</a>' +
                        '<a href="#" class="cancel-link btn btn-sm btn-secondary">No, Cancel</a>' +
                        '</div>';
                }

                messageHtml += '</div>';
                $('#result').html(messageHtml);

                // Add handlers for details, proceed, and cancel links
                // ... existing handlers code ...
            } else {
                // No discrepancies, proceed with export
                performExport(specification_title, exportTool, reqIfFileName, exchangeId);
            }
        },
        error: function (xhr, status, error) {
            console.error('Error comparing work items:', error);
            $('#result').addClass('alert alert-danger')
                .html('<span>Error validating work items. Please try again.<br>Details: ' + error + '</span>');
        }
    });
}


function onProceedToExportOk() {
    $("#export-option-confirmation-dialog").openExportConfirmationDialog({
        onConfirmationDialogOk: onExportConfirmationOk,
        showCancelButton: true,
        showNoButton: false,
        OkBtnText: "Proceed",
        CancelBtnText: "Cancel",
        confirmDialogTitle: "Export Confirmation",
        confirmDialogTxtMsg: "Please review the following options before proceeding with export"
       })
}
function openConfirmationDialog(isfile) {

    if (isfile) {
        $("#export-confirmation-dialog").openConfirmationDialog({
            onConfirmationDialogOk: onProceedToExportOk,
            onConfirmationDialogCancel: onViewMappingConfirmationOk,
            showCancelButton: true,
            showNoButton: false,
            OkBtnText: "Proceed to Export",
            CancelBtnText: "Configure Mapping",
            confirmDialogTitle: "Mapping Exists",
            confirmDialogTxtMsg: "Mapping file has been found. Only defined field values would be considered for mapping.\r\n Do you want to open mapping designer?"
        });


    } else {
        $("#export-confirmation-dialog").openConfirmationDialog({
            onConfirmationDialogOk: onViewMappingConfirmationOk,
            showCancelButton: false,
            OkBtnText: "Configure Mapping",
            confirmDialogTitle: "No Mapping Exists",
            confirmDialogTxtMsg: "No Mapping file has been found.\n Click 'Configure Mapping' to open mapping designer."
        });
    }
}
function onCloseDialogOk() {
    sessionStorage.setItem("reqIFBridge-ReqIFExport-Action-Dialog-Close", "true");

}
function onCloseDialog() {
    $("#export-confirmation-dialog").openConfirmationDialog({
        onConfirmationDialogOk: onCloseDialogOk,
        confirmDialogTitle: "Close Confirmation",
        confirmDialogTxtMsg: "Are you sure you want to close ReqIF Export window?"

    })
}
function onClearCacheOK() {


    var uri = serverUrl + '/Default/ClearCache';


    if (typeof adoParamModel == "undefined" || adoParamModel == null) {
        ViewSessionError();
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

    $("#export-confirmation-dialog").openConfirmationDialog({
        onConfirmationDialogOk: onClearCacheOK,

        showCancelButton: true,
        showNoButton: false,
        OkBtnText: "Yes",
        CancelBtnText: "No",
        confirmDialogTitle: "Clear Cache Confirmation",
        confirmDialogTxtMsg: "It will clear the complete cache of connected project. Are you sure you want to clear cache?"
    });


}

// Add this function to handle modal cancellation
function onConfirmationDialogCancelButtonClick(button) {
    var parentContainerId = $(button).data('parentcontainerid');

    // Clear any pending operations
    if (window.currentTimer) {
        clearInterval(window.currentTimer);
    }

    // Clear any export-related session storage
    sessionStorage.removeItem('reqIFBridge-ReqIFExport-Action-Dialog-Close');

    // Reset UI state if needed
    $('#result').removeClass('alert alert-success alert-warning alert-danger').html('');

    // Hide progress indicator if it's showing
    kendo.ui.progress($("body"), false);
}
