(function ($) {
    var currentRowIndex = -1;
    var maxLength = 50;
    var enumControlType = {
        simpleDropDown: 0,
        MultiSelectDropDown: 1
    };

    var enumValuesType = {
        string: 0,
        integer: 1
    };

    
    var enumToggleValue = {

        toggle_off: 'toggle_off',
        toggle_on: 'toggle_on'

    }

    var toggleValue = enumToggleValue.toggle_off;


    var fieldTypes = [
        'Numeric',
        'String',
        'Enum',
        'RichText',
        'DateTime'
    ];

    var $toggleSwitchTemplate = $(
        '<script id="toggleSwitchTemplate" type="text/x-kendo-template">' +
        '<div class="req-if-import-table-import-column-switch-container">' +
        '<input type="checkbox" class="req-if-import-table-import-column-switch" aria-label="Notifications Switch" checked="checked"/> <span class="req-if-import-table-import-column-switch-label">#: Import #</span></div>' +
        '</script>'
    );

    var $ADOTemplate = $(
        '<script id="ADOTemplate" type="text/x-kendo-template">' +
        '<div class="req-if-import-ado-column-combobox-container">' +
        '<input class="req-if-import-ado-column-combobox" placeholder="--not selected--" style="width: 100%;"/>' +
        '</div>' +
        '</script>'
    );

    var $toggleFieldTypeTemplate = $(
        '<script id="toggleFieldTypeTemplate" type="text/x-kendo-template">' +
        '<div class="req-if-import-field-type-column-combobox-container">' +
        '<input class="req-if-import-field-type-column-combobox" placeholder="--not selected--" style="width: 100%;"/>' +
        '</div>' +
        '</script>'
    );

    var $toggleReqIFFieldTypeTemplate = $(
        '<script id="toggleReqIFFieldTypeTemplate" type="text/x-kendo-template">' +
        '<div class="req-if-import-field-type-column-combobox-container">' +
        '<input class="req-if-import-reqif-field-type-column-combobox" placeholder="--not selected--" style="width: 100%;"/>' +
        '</div>' +
        '</script>'
    );


    $.fn.reqIFImport = function () {

        var reqIFImportContainer = this;
        var reqIFImportContainerId = "";
        var popupNotification = null;

        var currentReqIFImportGridHasUnsavedChangesStatus = false;

        var enumValueFilter = {
            field: "type", operator: "neq", value: "EnumValueMap"
        }

       
       
        var tableTreeListDatasource = [];
       /* var ImportedMappingFileXmlString = "";*/

        var defaults = {
            MappingXML: [],
            MappingJSON: [],
            workItemAndFields: [],
            workItemTypes: [],
            workItemLinkTypes: [],
            onSaveFile: null,
            onOpenFile: null,
            onInitComplete: null,
            showProgressBar: null,
            hideProgressBar: null,
            onExportMapping: null,
            onImportMapping: null,
            onSaveTemplate: null,
            getWorkItemTypesFieldsData: null
        };

        var settings = null;

        //type : error / info
        var showPopupNotification = function (message, type) {
            popupNotification.hide();
            message = " " + message;
            popupNotification.show(message, type);
        }

        var getReqIFImportContainerId = function () {
            return reqIFImportContainerId;
        }


        var getDataFromGrid = function () {
            let typeMaps = [];
            let linkMaps = [];

            $.each(tableTreeListDatasource.data, function (index, dataSourceItem1) { // Iterates through a collection

                if (dataSourceItem1.type === 'TypeMap' && dataSourceItem1.Import) {

                        //get EnumFieldMaps items on datasource for that TypeMap
                        let EnumFieldMaps = [];
                        $.each(tableTreeListDatasource.data, function (index, dataSourceItem2) {
                            if (dataSourceItem2.parentId === dataSourceItem1.id && dataSourceItem2.Import) {

                                //get EnumValueMaps items on datasource for that EnumFieldMap
                                let EnumValueMaps = [];
                                $.each(tableTreeListDatasource.data, function (index, dataSourceItem3) {
                                    if (dataSourceItem3.parentId === dataSourceItem2.id && dataSourceItem3.Import) {
                                        EnumValueMaps.push(dataSourceItem3);
                                    }
                                });


                                EnumFieldMaps.push({
                                    properties: dataSourceItem2,
                                    EnumValueMaps: EnumValueMaps
                                });
                            }
                        });

                        typeMaps.push({
                            WITypeName: dataSourceItem1.Ado,
                            ReqIFTypeName: dataSourceItem1.Reqif,
                            EnumFieldMaps: EnumFieldMaps
                        });
                 

                }

                if (dataSourceItem1.type === 'LinkMap' && dataSourceItem1.Import) {

                
                        //get children
                        let children = [];
                        $.each(tableTreeListDatasource.data, function (index, child) {
                            if (child.parentId === dataSourceItem1.id) {
                                children.push(child);
                            }
                        });

                        linkMaps.push({
                            WILinkName: dataSourceItem1.Ado,
                            ReqIFRelationName: dataSourceItem1.Reqif,
                            EnumFieldMaps: children
                        });

                }
                

            });

            return {
                typeMaps: typeMaps,
                linkMaps: linkMaps
            }
        }


        var createJSON = function () {
            //Building the Configuration on JSON
            var json = {
                "TemplateName": "basic",
                "TemplateVersion": "0.1.0",
                "WIProcessTemplateType": null,
                "TypeMaps": [],
                "LinkMaps": []
            };

           
            var dataFromGrid = getDataFromGrid();
            var typeMaps = dataFromGrid.typeMaps;
            var linkMaps = dataFromGrid.linkMaps;

            $.each(typeMaps, function (index, typeMap) { // Iterates through a collection
         
                $typeMap = {
                    "WITypeName": typeMap.WITypeName,
                    "ReqIFTypeName": typeMap.ReqIFTypeName,
                    "EnumFieldMaps": null
                };

                if (typeMap.EnumFieldMaps.length > 0) {
                  
                    $typeMap.EnumFieldMaps = [];

                    $.each(typeMap.EnumFieldMaps, function (index, fieldMap) { // Iterates through a collection

                        let EnumValueMaps = [];

                        $.each(fieldMap.EnumValueMaps, function (index, valueMap) { // Iterates through a collection

                            EnumValueMaps.push({
                                "WIEnumFieldValue": valueMap.Ado,
                                "ReqIFEnumFieldValue": valueMap.Reqif
                            });

                        });

                        $typeMap.EnumFieldMaps.push({
                            "WIFieldName": fieldMap.properties.Ado,
                            "ReqIFFieldName": fieldMap.properties.Reqif,
                            "ReqIFFieldType": fieldMap.properties.reqifFieldType,
                            "FieldType": fieldMap.properties.FieldType,
                            "ReqIfFieldNullThen": null,
                            "EnumValueMaps": EnumValueMaps
                        });

                    });
             

                }
                json.TypeMaps.push($typeMap);
            });

            $.each(linkMaps, function (index, item) {  // Iterates through a collection
               $linkMap = {
                    "WILinkName": item.WILinkName,
                    "ReqIFRelationName": item.ReqIFRelationName,
                    "FieldMaps": []
                };

               json.LinkMaps.push($linkMap);

            });

            return json;

        }


        var getWorkItemData = function () {

            var linkTypes = [];
            $.each(settings.workItemLinkTypes.LinkTypes, function (index, item) {
                linkTypes.push(item.Name);
            });

             var allFields = [];
            $.each(settings.workItemAndFields.workItemAndFieldsData, function (index, item) {
                $.each(item, function (index, value) {
                    allFields.push(value);
                });
            });
           

            workItemData = {
                workItemAndFields: settings.workItemAndFields,
                workItemTypes: settings.workItemTypes.WorkItemTypes,
                workItemFields: allFields,
                workItemLinkTypes: linkTypes,

            }

            return workItemData;
        }

        var getWorkitemFieldTypeFromName = function (propertyName) {
            var propertyType = "";
            $.each(getWorkItemData().workItemFields, function (index, value) {
                if (value.name === propertyName) {

                    propertyType = value.type;
                    return false;
                }
            });

            return propertyType;
        }


        this.InitializeReqIFImport = function (options) {



            settings = $.extend({}, defaults, options);

            

          

            return this.buildReqIFImportLayout();
        };

        var getWorkItemFieldsDatasourceFromWorkItemType = function (workitemType) {
            var workitemFields = [];

            $.each(getWorkItemData().workItemAndFields.workItemAndFieldsData, function (index, element) {

                if (index === workitemType) {
                    workitemFields = element;
                    return false;
                }

            });    

            // check empty fields
            if (workitemType != "" && workitemFields.length == 0) {


                if (typeof settings.getWorkItemTypesFieldsData == 'function') { // make sure the callback is a function

                  
                    //if (typeof settings.showProgressBar == 'function') { // make sure the callback is a function
                    //    settings.showProgressBar.call(this); // brings the scope to the callback
                    //}


                    var successCallbackForWIfield = function (result) {
                       
                        //if (typeof settings.hideProgressBar == 'function') { // make sure the callback is a function
                        //    settings.hideProgressBar.call(this); // brings the scope to the callback
                        //}
                        var data = JSON.parse(result);
                        if (data == null) {
                            //showPopupNotification(workitemType + " work item type does not exist in current project.","error")

                        } else {
                            workitemFields = data[workitemType];

                            settings.workItemAndFields.workItemAndFieldsData[workitemType] = workitemFields;

                        }

                    }

                    var failureCallbackForWIfield = function () {

                        //if (typeof settings.hideProgressBar == 'function') { // make sure the callback is a function
                        //    settings.hideProgressBar.call(this); // brings the scope to the callback
                        //}
                    }

                    settings.getWorkItemTypesFieldsData.call(this, workitemType, successCallbackForWIfield, failureCallbackForWIfield); // brings the scope to the callback

                }

            }
            //
            
            var fieldDatasource = [];

            $.each(workitemFields, function (index, item) {
                fieldDatasource.push({
                    text: item.name, value: item.name
                });

            });           


            var fieldDataSource = new kendo.data.DataSource({
                data: fieldDatasource
            });

            return fieldDataSource;
        }


        var getWorkItemFieldValuesDatasourceFromWorkItemField = function (workitemType, workitemField) {
            var workitemFields = [];
            var workitemFieldValues = [];

            $.each(getWorkItemData().workItemAndFields.workItemAndFieldsData, function (index, element) {

                if (element.Key === workitemType) {
                    workitemFields = element.Value;

                    $.each(workitemFields, function (index, element0) {

                        if (element0.name === workitemField) {
                            workitemFieldValues = element0.allowedValues;
                            return false;
                        }

                    });
                    return false;
                }

            });


            var fieldValueDatasource = [];

            $.each(workitemFieldValues, function (index, item) {
                fieldValueDatasource.push({
                    text: item, value: item
                });

            });


            var fieldValueDataSource = new kendo.data.DataSource({
                data: fieldValueDatasource
            });

            return fieldValueDataSource;
        }


        var onReqIFImportTableDataBound = function (arg) {

            var treeList = $("#req-if-import-table-" + reqIFImportContainerId).data('kendoTreeList');          

            $(reqIFImportContainer).find('.req-if-import-table-import-column-switch').each(function () {
              
                var data = treeList.dataItem($(this).closest('tr'));


                if (data.Import !== null) {                   

                    let onOFF = data.Import ? "On" : "Off";
                    let label = $(this).closest('.req-if-import-table-import-column-switch-container').find('.req-if-import-table-import-column-switch-label');
                    label.text(onOFF);

                    $(this).kendoMobileSwitch({
                        //enable: data.Ado === 'System.Title' || (data.type == 'TypeMap' && countWorkItemOnTableTreeListDatasource(tableTreeListDatasource) === 1) ? false : true,
                        checked: data.Import,
                        messages: {
                            checked: "",
                            unchecked: ""
                        },
                        change: function (e) {  

                         
                        
                            let onOFF = e.checked ? "On" : "Off";
                            let label = this.element.closest('.req-if-import-table-import-column-switch-container').find('.req-if-import-table-import-column-switch-label');
                            label.text(onOFF);

                            //Updating row on global datasource
                            var row = this.element.closest('tr');
                            currentRowIndex = row.index();
                            var dataItemRow = treeList.dataItem(row);
                            // Bug Fixing - 29830
                            if (dataItemRow !== undefined && dataItemRow !== null) {                             

                                var dataItemIndex = tableTreeListDatasource.data.findIndex(x => x.id === dataItemRow.id);

                                var newObj = tableTreeListDatasource.data[dataItemIndex];


                                newObj.Import = e.checked;

                                tableTreeListDatasource.data[dataItemIndex] = newObj;      

                                ////Toggle children
                                //var childrenIndexes = [];
                                //var childrenIds = [];
                                //$.each(tableTreeListDatasource.data, function (index, item) {
                                //    if (item.parentId === dataItemRow.id) {
                                //        childrenIndexes.push(index);
                                //        childrenIds.push(item.id);
                                //    }
                                //});
                                //$.each(tableTreeListDatasource.data, function (index, item) {
                                //    if (childrenIds.includes(item.parentId)) {
                                //        childrenIndexes.push(index);
                                //    }
                                //});

                                //$.each(childrenIndexes, function (index, indexVal) {
                                //    var newObj = tableTreeListDatasource.data[indexVal];

                                //    newObj.Import = e.checked;

                                //    tableTreeListDatasource.data[indexVal] = newObj;


                                //});

                                // Put Parent to ON if current checked status is ON
                                if (e.checked && data.parentId !== null) {
                                    var parentIndex = tableTreeListDatasource.data.findIndex(x => x.id === data.parentId);

                                    var newParentObj = tableTreeListDatasource.data[parentIndex];

                                    newParentObj.Import = e.checked;

                                    tableTreeListDatasource.data[parentIndex] = newParentObj;


                                }                             

                                //treeList.setDataSource(tableTreeListDatasource);

                                //All other controls on row go gray/not gray
                                e.checked ? row.find('.req-if-import-ado-column-combobox[data-role="combobox"]').data('kendoComboBox').enable(true) : row.find('.req-if-import-ado-column-combobox[data-role="combobox"]').data('kendoComboBox').enable(false);

                                // ReqIFImport Unsaved changes
                                currentReqIFImportGridHasUnsavedChanges();

                               // bug - fix-- scroll to top on toggle when data is updated
                                if (currentRowIndex != -1) {

                                    var currentRow = $('table[role="treegrid"]').find('tr').eq(currentRowIndex);
                                    var rowPosition = currentRow.position().top;
                                    $("#" + getReqIFImportContainerId() + "-main").scrollTop(rowPosition);
                                }
                                
                            }





                        }
                    });

                    if (data.type != null && data.type == 'TypeMap') {
                        //$(this).closest('.req-if-import-table-import-column-switch-container').remove();
                        $(this).closest('.req-if-import-table-import-column-switch-container').addClass('req-if-import-toggle-readonly');
                    }

                } else {
                    $(this).closest('.req-if-import-table-import-column-switch-container').remove();
                }

                });

            $(reqIFImportContainer).find(".req-if-import-ado-column-combobox").each(function () {

                var data = treeList.dataItem($(this).closest('tr'));
                var $row = $(this).closest('tr');

    
               if (data.Ado !== 'n/a') {
                   var dataSourceArray = getWorkitemDatasourceForComboBox(data, treeList);
               
                    var adoComboBox = $(this).kendoComboBox({
                        dataTextField: "text",
                        dataValueField: "value",
                        dataSource: dataSourceArray,
                        filter: "contains",
                        suggest: true,
                        index: -1,
                        change: function (e) {
                         
                            if (typeof settings.showProgressBar == 'function') { // make sure the callback is a function
                               
                                settings.showProgressBarWithMessage.call(this); // brings the scope to the callback
                            }

                            var row = this.element.closest('tr');
                            var value = this.value();

                            setTimeout(function () {

                                //Updating row on global datasource
                                //var row = this.element.closest('tr');
                                var dataItemRow = treeList.dataItem(row);
                                // Bug Fixing - 29830
                                if (dataItemRow !== undefined && dataItemRow !== null) {

                                    var dataItemIndex = tableTreeListDatasource.data.findIndex(x => x.id === dataItemRow.id);

                                    var newObj = tableTreeListDatasource.data[dataItemIndex];

                                    newObj.Ado = value;//this.value();


                                    tableTreeListDatasource.data[dataItemIndex] = newObj;


                                    //Updating children datasources.
                                    updateChildrenWorkItemComboBoxesDatasources(dataItemRow.id);

                                    updateWorkitemFieldType(dataItemRow.id, dataItemRow.parentId);

                                    //ReqIFImport Unsaved changes
                                    currentReqIFImportGridHasUnsavedChanges();

                                    if (typeof settings.hideProgressBar == 'function') { // make sure the callback is a function
                                        settings.hideProgressBar.call(this); // brings the scope to the callback
                                    }
                                }
                                else
                                {
                                    if (typeof settings.hideProgressBar == 'function') { // make sure the callback is a function
                                        settings.hideProgressBar.call(this); // brings the scope to the callback
                                    }
                                }

                            }, 100);

                        }
                    });


                   if (data.Ado === null) {
                       var reqIFInWorkitemsIndex = Array.from(dataSourceArray.data()).findIndex(x => x.text === data.Reqif);
                       
                       if (reqIFInWorkitemsIndex !== -1) {
                           adoComboBox.data('kendoComboBox').value(data.Reqif);
                           //ReqIFImport Unsaved changes
                           currentReqIFImportGridHasUnsavedChanges();
                       }
                           

                   } else {
                       adoComboBox.data('kendoComboBox').value(data.Ado);
                   }


                    //Disable if Import == false
                    if (!data.Import) {
                        $row.find('.req-if-import-ado-column-combobox[data-role="combobox"]').data('kendoComboBox').enable(false);
                   }


                } else {
                    $(this).remove();
                }
      
          

            });

            $(reqIFImportContainer).find(".req-if-import-field-type-column-combobox").each(function () {
               
                var data = treeList.dataItem($(this).closest('tr'));
                if (data.type === 'FieldMap') {
                    var dataSourceArray = [];

                    for (var index = 0; index < fieldTypes.length; index++) {
                        dataSourceArray.push({
                            text: fieldTypes[index], value: index
                        });
                    }


                    var fieldTypeComboBox = $(this).kendoComboBox({
                        dataTextField: "text",
                        dataValueField: "value",
                        dataSource: dataSourceArray,
                        filter: "contains",
                        suggest: true,
                        index: -1
                    });

                    fieldTypeComboBox.data('kendoComboBox').readonly(true);
                    fieldTypeComboBox.data('kendoComboBox').enable(false);
                    fieldTypeComboBox.data('kendoComboBox').value(data.FieldType);
                } else {
                    $(this).remove();
                } 

            });

            $(reqIFImportContainer).find(".req-if-import-reqif-field-type-column-combobox").each(function () {

                

                var data = treeList.dataItem($(this).closest('tr'));
                if (data.type === 'FieldMap') {
                    var dataSourceArray = [];

                    for (var index = 0; index < fieldTypes.length; index++) {
                        dataSourceArray.push({
                            text: fieldTypes[index], value: index
                        });
                    }


                    var fieldTypeComboBox = $(this).kendoComboBox({
                        dataTextField: "text",
                        dataValueField: "value",
                        dataSource: dataSourceArray,
                        filter: "contains",
                        suggest: true,
                        index: -1
                    });

                    fieldTypeComboBox.data('kendoComboBox').readonly(true);
                    fieldTypeComboBox.data('kendoComboBox').enable(false);
                    fieldTypeComboBox.data('kendoComboBox').value(data.reqifFieldType);
                } else {
                    $(this).remove();
                }

            });


           

        }



        var onReqIFImportTableDataBoundToggle = function (onOFF, fromDeleteBtn) {

            if (typeof settings.showProgressBar === 'function') {
                settings.showProgressBar.call(this);
            }
           
            var treeList = $("#req-if-import-table-" + reqIFImportContainerId).data('kendoTreeList');

            let isChecked = onOFF == 'On' ? true : false;

            $.each(tableTreeListDatasource.data, function (index, dataSourceItem1) { // Iterates through a collection
               
                if (dataSourceItem1.type === 'TypeMap' || dataSourceItem1.type === 'LinkTypes') {


                    if (dataSourceItem1.Import !== null) {                    

                        let label = $(this).closest('.req-if-import-table-import-column-switch-container').find('.req-if-import-table-import-column-switch-label');
                        label.text(onOFF);



                        //Updating row on global datasource

                        var dataItemRow = dataSourceItem1;
                        // Bug Fixing - 29830
                        if (dataItemRow !== undefined && dataItemRow !== null) {

                            var dataItemIndex = tableTreeListDatasource.data.findIndex(x => x.id === dataItemRow.id);

                            var newObj = tableTreeListDatasource.data[dataItemIndex];

                            if (fromDeleteBtn) {
                                newObj.Import = isChecked;
                            } else {
                                newObj.Import = newObj.type === 'TypeMap' && !isChecked ? true : isChecked;
                            }                            
                           

                            tableTreeListDatasource.data[dataItemIndex] = newObj;

                            //Toggle children
                            var childrenIndexes = [];
                            var childrenIds = [];
                            $.each(tableTreeListDatasource.data, function (index, item) {
                                if (item.parentId === dataItemRow.id) {
                                    childrenIndexes.push(index);
                                    childrenIds.push(item.id);
                                }
                            });
                            $.each(tableTreeListDatasource.data, function (index, item) {
                                if (childrenIds.includes(item.parentId)) {
                                    childrenIndexes.push(index);
                                }
                            });

                            $.each(childrenIndexes, function (index, indexVal) {
                                var newObj = tableTreeListDatasource.data[indexVal];

                                newObj.Import = isChecked;

                                tableTreeListDatasource.data[indexVal] = newObj;


                            });

                            // Put Parent to ON if current checked status is ON
                            if (isChecked && dataSourceItem1.parentId !== null) {
                                var parentIndex = tableTreeListDatasource.data.findIndex(x => x.id === dataSourceItem1.parentId);

                                var newParentObj = tableTreeListDatasource.data[parentIndex];

                                newParentObj.Import = isChecked;

                                tableTreeListDatasource.data[parentIndex] = newParentObj;


                            }

                            treeList.setDataSource(tableTreeListDatasource);

                            //All other controls on row go gray/not gray
                            //  isChecked ? row.find('.req-if-import-ado-column-combobox[data-role="combobox"]').data('kendoComboBox').enable(true) : row.find('.req-if-import-ado-column-combobox[data-role="combobox"]').data('kendoComboBox').enable(false);

                            // ReqIFImport Unsaved changes
                            currentReqIFImportGridHasUnsavedChanges();

                        }


                    } else {
                        $(this).closest('.req-if-import-table-import-column-switch-container').remove();
                    }


                }

               

            });


            setTimeout(function () {
                if (typeof settings.hideProgressBar === 'function') {
                    settings.hideProgressBar.call(this);
                }
            }, 1000);
        }

        var updateChildrenWorkItemComboBoxesDatasources = function (parentId) {

            var treeList = $("#req-if-import-table-" + reqIFImportContainerId).data('kendoTreeList');

            tableTreeListDatasource.data.forEach((dataItem) => {
                try {
                    if (dataItem.parentId === parentId) {
                        var row = treeList.itemFor(dataItem.id);
                        if (row.length > 0) {
                            $(row).find('.req-if-import-ado-column-combobox[data-role="combobox"]').data('kendoComboBox').setDataSource(getWorkitemDatasourceForComboBox(dataItem, treeList));
                        }

                    }
                }
                catch (err) {
                    if (err.message != null) {
                        console.log(err.message);
                    }
                }

            });
        }

        var updateWorkitemFieldType = function (itemId, parentId) {

            if (parentId == null || parentId == "LinkTypes") return false;

            var treeList = $("#req-if-import-table-" + reqIFImportContainerId).data('kendoTreeList');
            var row;   var indexOfField;
            var workitemType = "";
            var fieldName = "";

            tableTreeListDatasource.data.forEach((dataItem, index) => {

                if (dataItem.id === parentId) {
                    var dataRow = treeList.itemFor(dataItem.id);

                    if (workitemType == "") {
                        workitemType = $(dataRow).find('.req-if-import-ado-column-combobox[data-role="combobox"]').data('kendoComboBox').value();
                    }                    
                    //.req-if-import-field-type-column-combobox
                } else if (dataItem.id === itemId) {
                    row = treeList.itemFor(dataItem.id);
                    if (fieldName == "") {
                        fieldName = $(row).find('.req-if-import-ado-column-combobox[data-role="combobox"]').data('kendoComboBox').value();
                        indexOfField = index;
                    }                    
                }

            });

            var workitemFields = [];

            $.each(getWorkItemData().workItemAndFields.workItemAndFieldsData, function (index, element) {

                if (index === workitemType) {
                    workitemFields = element;
                    return false;
                }

            });


            workitemFields.forEach((item) => {

                if (item.referenceName === fieldName)
                {

                    $(row).find('.req-if-import-field-type-column-combobox[data-role="combobox"]').data('kendoComboBox').text(item.type);
                    $(row).find('.req-if-import-field-type-column-combobox[data-role="combobox"]').data('kendoComboBox').value(item.type);
                    tableTreeListDatasource.data[indexOfField].FieldType = fieldTypes.indexOf(item.type);
                    return false;
                }

            });

        }

        var getWorkitemDatasourceForComboBox = function (data, treeList) {

            var dataSourceArray = [];
            var workItemTypesDataSourceArray = [];
            var workItemFieldsDataSourceArray = [];
            var workItemFieldValuesDataSourceArray = [];
            var workItemLinkTypesDataSourceArray = [];
           

            if (data.parentId === 'LinkTypes') {
                $.each(getWorkItemData().workItemLinkTypes, function (index, item) {
                    workItemLinkTypesDataSourceArray.push({
                        text: item, value: item
                    });

                });
            } else {
                $.each(getWorkItemData().workItemTypes, function (index, item) {
                    workItemTypesDataSourceArray.push({
                        text: item, value: item
                    });

                });
            }


            var workItemTypesDataSourceArray = new kendo.data.DataSource({
                data: workItemTypesDataSourceArray
            });

            if (data.type === 'FieldMap') {
                var parentDataItemIndex = tableTreeListDatasource.data.findIndex(x => x.id === data.parentId);

                var parentRowDataItem = tableTreeListDatasource.data[parentDataItemIndex];

                workItemFieldsDataSourceArray = getWorkItemFieldsDatasourceFromWorkItemType(parentRowDataItem.Ado);
            }

            if (data.type === 'EnumValueMap') {

                var parentDataItemIndex_FieldMap = tableTreeListDatasource.data.findIndex(x => x.id === data.parentId);
                var parentRowDataItem_FieldMap = tableTreeListDatasource.data[parentDataItemIndex_FieldMap];


                var parentDataItemIndex_TypeMap = tableTreeListDatasource.data.findIndex(x => x.id === parentRowDataItem_FieldMap.parentId);
                var parentRowDataItem_TypeMap = tableTreeListDatasource.data[parentDataItemIndex_TypeMap];

                workItemFieldValuesDataSourceArray = getWorkItemFieldValuesDatasourceFromWorkItemField(parentRowDataItem_TypeMap.Ado, parentRowDataItem_FieldMap.Ado);
            }


            if (data.type === 'TypeMap') {
                dataSourceArray = workItemTypesDataSourceArray;
            } else if (data.type === 'FieldMap') {
                dataSourceArray = workItemFieldsDataSourceArray;
            } else if (data.type === 'EnumValueMap') {
                dataSourceArray = workItemFieldValuesDataSourceArray;
            } else if (data.type === 'LinkMap') {
                dataSourceArray = workItemLinkTypesDataSourceArray;
            }
            return dataSourceArray;
        }


        var applyMappingJSON = function (mappingJSON) {

            
            
            var mappingFileData = [];

            let id = 0;
            $TypeMaps = mappingJSON.TypeMaps ? mappingJSON.TypeMaps:[];
            if ($TypeMaps.length > 0) {



                //TypeMaps
                $.each($TypeMaps, function (index, typeMap) {
                    let typeId = ++id;
                   
                    let ReqIFTypeName = typeMap.ReqIFTypeName;
                    let WITypeName = typeof typeMap.WITypeName !== 'undefined' ? typeMap.WITypeName: null;
                    

                    mappingFileData.push({
                        id: typeId,
                        Reqif: ReqIFTypeName,
                       /* Import: WITypeName == '' ? false : true,*/
                        Import: true,
                        Ado: WITypeName,
                        FieldType: null,
                        parentId: null,
                        type: 'TypeMap'
                        
                    });

                    
                    //EnumFieldMaps
                    $EnumFieldMaps = typeMap.EnumFieldMaps ? typeMap.EnumFieldMaps : [];

                    if ($EnumFieldMaps.length > 0) {

                        $.each($EnumFieldMaps, function (index, enumFieldMap) {
                            let fieldId = ++id;   



                            let WIFieldName = typeof enumFieldMap.WIFieldName !== 'undefined' ? enumFieldMap.WIFieldName: null;
                            let ReqIFFieldName = enumFieldMap.ReqIFFieldName;
                            let FieldType = enumFieldMap.FieldType;
                            let ReqIFFieldType = enumFieldMap.ReqIFFieldType;

                            mappingFileData.push({
                                id: fieldId,
                                Reqif: ReqIFFieldName,
                                Import: WIFieldName == '' ? false : true,                           
                                Ado: WIFieldName,
                                FieldType: FieldType,
                                parentId: typeId,
                                type: 'FieldMap',
                                reqifFieldType: ReqIFFieldType
                            });

                            //EnumValueMaps
                            $.each(enumFieldMap.EnumValueMaps, function (index, enumValueMap) {
                                let enumValueId = ++id;

                                let WIEnumFieldValue = typeof enumValueMap.WIEnumFieldValue !== 'undefined' ? enumValueMap.WIEnumFieldValue: null;
                                let ReqIFEnumFieldValue = enumValueMap.ReqIFEnumFieldValue;

                                mappingFileData.push({
                                    id: enumValueId,
                                    Reqif: ReqIFEnumFieldValue,
                                    Import: WIEnumFieldValue == '' ? false : true,
                                    Ado: WIEnumFieldValue,
                                    FieldType: null,
                                    parentId: fieldId,
                                    type: 'EnumValueMap'
                                });

                            });


                        });
                    }


                });


            }
            $LinkMaps = mappingJSON.LinkMaps ? mappingJSON.LinkMaps : [];                  
            if ($LinkMaps.length > 0) {

                let WILinkNameCount = $LinkMaps.filter(i => i.WILinkName != "").length;

                mappingFileData.push({
                    id: 'LinkTypes',
                    Reqif: 'Link Types',
                    //Import: false,
                    Import: WILinkNameCount > 0 ? true : false,
                    Ado: 'n/a',
                    FieldType: null,
                    parentId: null,
                    type: 'LinkTypes'
                });
                $.each($LinkMaps, function (index, linkMap) {

                    let linkId = ++id;

                    let WILinkName = linkMap.WILinkName;
                    let ReqIFRelationName = linkMap.ReqIFRelationName;
                    
                    mappingFileData.push({
                        id: linkId,
                        Reqif: ReqIFRelationName,
                        Import: WILinkName == '' ? false : true,
                        //Import: false,
                        Ado: WILinkName,
                        FieldType: null,
                        parentId: 'LinkTypes',
                        type: 'LinkMap'
                    });
                });

            }


            tableTreeListDatasource = {
                data: mappingFileData,
                schema: {
                    model: {
                        id: "id",
                        expanded: false
                    }
                },
                filter: enumValueConfig ? {} : enumValueFilter
            };


            var dataSource = new kendo.data.TreeListDataSource(tableTreeListDatasource);


            $("#req-if-import-table-" + reqIFImportContainerId).data('kendoTreeList').setDataSource(dataSource);


            //Trigger change on comboboxes to update datasources
            var previousReqIFImportGridHasUnsavedChangesStatus = currentReqIFImportGridHasUnsavedChangesStatus;
            $(reqIFImportContainer).find('.req-if-import-ado-column-combobox[data-role="combobox"]').data('kendoComboBox').trigger('change');
            currentReqIFImportGridHasUnsavedChangesStatus = previousReqIFImportGridHasUnsavedChangesStatus;

           /* showPopupNotification("Grid successfully updated!", "success");*/
            currentReqIFImportGridIsSaved();

        }

        var countWorkItemOnTableTreeListDatasource = function (tableTreeListDatasourceParam) {
            let count = 0;
            $.each(tableTreeListDatasourceParam.data, function (index, item) {
                if (item.type === 'TypeMap')
                    count++;
            });

            return count;
        }

        var handleMappingFileImport = function (file,isMerge = false) {
            var reader = new FileReader();
           
            // Closure to capture the file information.
            reader.onload = (function (theFile) {
                return function (e) {
                    ImportedMappingFileXmlString = e.target.result;
                    var reqifSessionObject = null;

                    if (isMerge) {
                        reqifSessionObject = sessionStorage.getItem("AdoFilePath");
                    }                        

                    if (typeof settings.onImportMapping === 'function') {
                        settings.onImportMapping(ImportedMappingFileXmlString, reqifSessionObject, applyMappingJSON, failureCallback);
                    }

                };
            })(file);
            
            reader.readAsText(file, 'UTF-8');
            clearKendoUploadFileSelection();
        }


        var clearKendoUploadFileSelection = function () {           
            var id = "#req-if-import-import-mapping-file-input-" + reqIFImportContainerId;
            var upload = $(id).data("kendoUpload");
            upload.removeAllFiles();
        }
        var resetConfiguration = function () {
            if (typeof settings.showProgressBar === 'function') {
                settings.showProgressBar.call(this); 
            }

            applyMappingJSON(settings.MappingJSON);

            if (typeof settings.hideProgressBar === 'function') {
                settings.hideProgressBar.call(this);
            }
            var id = "#req-if-import-import-mapping-file-input-" + reqIFImportContainerId;
            var upload = $(id).data("kendoUpload");
            upload.removeAllFiles();
        }

        var showDialogNotificationBox = function (title, contentMsg, okButtonText, okButtonCallback, cancelButtonText, cancelButtonCallback) {
            var dialogDiv = $('<div id="req-if-import-dialog-msg-box-' + reqIFImportContainerId + '" class="req-if-import-dialog-msg-box"></div>');
            reqIFImportContainer.prepend(dialogDiv);

            var actionsArray = [];

            okButtonText !== null && actionsArray.push({
                text: okButtonText,
                action: function (e) {
                    // e.sender is a reference to the dialog widget object
                    // OK action was clicked
                    // Returning false will prevent the closing of the dialog
                    if (typeof okButtonCallback === 'function') {
                        return okButtonCallback();
                    }
                }
            });


            cancelButtonText !== null && actionsArray.push({
                text: cancelButtonText,
                action: function (e) {
                    if (typeof cancelButtonCallback === 'function') {
                        return cancelButtonCallback();
                    }
                }
            });


            $('#req-if-import-dialog-msg-box-' + reqIFImportContainerId).kendoDialog({
                title: title,
                content: contentMsg,
                actions: actionsArray,
                close: function (e) {
                    this.destroy();
                }
            });
        }

        var failureCallback = function (errorMessage) {

            showPopupNotification("Incompatible mapping file!", "error");
            if (typeof settings.hideProgressBar === 'function') {
                settings.hideProgressBar.call(this);
            }
        }
        var failureCallBackExport = function (errorMessage) {

            showPopupNotification(" " + errorMessage, "error");
            if (typeof settings.hideProgressBar === 'function') {
                settings.hideProgressBar.call(this);
            }
        }
       

        var onExportMappingSuccessCallback = function (xmlMapping) {
            // Function to call after successfull export of mapping file

            

            setTimeout(function () {
                if (typeof settings.hideProgressBar === 'function') {
                    settings.hideProgressBar.call(this);
                }
            }, 2000);


            currentReqIFImportGridIsSaved();
        }
        var onSaveTemplateSuccessCallback = function (isSaved, message, isSaveAndClose) {

            if (typeof settings.hideProgressBar === 'function') {
                settings.hideProgressBar.call(this);

            }
            if (isSaved) {
                showPopupNotification("Mapping template has been successfully updated!", "success");

            } else {
                showPopupNotification(message, "error");
            }
            
            setTimeout(function () {
                if (isSaveAndClose && isSaved) {
                    var projectGuid = settings.projectGuid;
                    window.location.href = serverUrl + '/Default/ImportReqIFDialog?isClose=true&projectGuid=' + projectGuid;
                }
            }, 1000);                        

           
        }


        var toggleOnOffParentIds = function (fromDeleteBtn = false) {
          
            if (toggleValue == enumToggleValue.toggle_off) {
                $('#req-if-import-export-mapping-toggle-button-' + reqIFImportContainerId)
                    .text("Toggle On");
                toggleValue = enumToggleValue.toggle_on;

                onReqIFImportTableDataBoundToggle('Off', fromDeleteBtn);
                  
            } else {
                $('#req-if-import-export-mapping-toggle-button-' + reqIFImportContainerId)
                    .text("Toggle Off");
                toggleValue = enumToggleValue.toggle_off;

                onReqIFImportTableDataBoundToggle('On');

            }
            if (typeof settings.hideProgressBar === 'function') {
                settings.hideProgressBar.call(this);
            }
        }


        var exportMappingFile = function () {
            //showDialogNotificationBox("Download Confirmation", "Are you sure you want to download the current mapping file?", "Yes", function () {


            //    if (typeof settings.showProgressBar === 'function') {
            //        settings.showProgressBar.call(this);
            //    }
                
            //    var mappingJSON = createJSON();

            //    var projectGuid = settings.projectGuid;

            //    if (typeof settings.onSaveTemplate === 'function') {
            //        settings.onExportMapping(mappingJSON, projectGuid, onExportMappingSuccessCallback, failureCallBackExport);


            //    }

            //    setTimeout(function () {
            //        if (typeof settings.hideProgressBar === 'function') {
            //            settings.hideProgressBar.call(this);
            //        }
            //    }, 2000);

            //}, "No", function () {
            //        $("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).kendoWindow();
            //        var dialog = $("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).data("kendoWindow")
            //    dialog.close();
            //});


            $("#import-plugin-confirmation-dialog").openConfirmationDialog({
                onConfirmationDialogOk: () => {


                    if (typeof settings.showProgressBar === 'function') {
                        settings.showProgressBar.call(this);
                    }

                    var mappingJSON = createJSON();

                    var projectGuid = settings.projectGuid;

                    if (typeof settings.onSaveTemplate === 'function') {
                        settings.onExportMapping(mappingJSON, projectGuid, onExportMappingSuccessCallback, failureCallBackExport);


                    }

                    setTimeout(function () {
                        if (typeof settings.hideProgressBar === 'function') {
                            settings.hideProgressBar.call(this);
                        }
                    }, 2000);
                },
                onConfirmationDialogNo: () => {
                    //$("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).kendoWindow();
                    //var dialog = $("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).data("kendoWindow")
                    //dialog.close();
                },
                showCancelButton: true,
                showNoButton: false,
                OkBtnText: "Yes",
                CancelBtnText: "No",
                confirmDialogTitle: "Download Confirmation",
                confirmDialogTxtMsg: "Are you sure you want to download the current mapping file?"
            });







        }


        var saveMappingFile = function () {

            //showDialogNotificationBox("Save Confirmation", "Are you sure you want to save mapping file?", "Yes", function () {
                  
            //    if (typeof settings.showProgressBar === 'function') {
            //        settings.showProgressBar.call(this);
            //    }
                
            //    var mappingJSON = createJSON();

            //    var projectGuid = settings.projectGuid;

            //    if (typeof settings.onSaveTemplate === 'function') {
            //        settings.onSaveTemplate(mappingJSON, projectGuid, onSaveTemplateSuccessCallback, failureCallback);
            //    }
                

            //}, "No", function () {
            //          $("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).kendoWindow();
            //          var dialog = $("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).data("kendoWindow")
            //    dialog.close();
            //});
            
            $("#import-plugin-confirmation-dialog").openConfirmationDialog({
                onConfirmationDialogOk: () => {
                    if (typeof settings.showProgressBar === 'function') {
                        settings.showProgressBar.call(this);
                    }
                    
                    var mappingJSON = createJSON();

                    var projectGuid = settings.projectGuid;

                    if (typeof settings.onSaveTemplate === 'function') {
                        settings.onSaveTemplate(mappingJSON, projectGuid, onSaveTemplateSuccessCallback, failureCallback);
                    }
                },
                onConfirmationDialogNo: () => {

                    if (typeof settings.showProgressBar === 'function') {
                        settings.showProgressBar.call(this);
                    }

                    var mappingJSON = createJSON();

                    var projectGuid = settings.projectGuid;

                    if (typeof settings.onSaveTemplate === 'function') {
                        settings.onSaveTemplate(mappingJSON, projectGuid, onSaveTemplateSuccessCallback, failureCallback, isSaveAndClose = true);

                    }

                    //$("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).kendoWindow();
                    //var dialog = $("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).data("kendoWindow")
                    //dialog.close();
                },
                onConfirmationDialogCancel: () => {            

                    

                },
                
                NoBtnText: "Save & Close",
                showCancelButton: true,
                showNoButton: true,
                OkBtnText: "Yes",
                CancelBtnText: "No",
                confirmDialogTitle: "Save Confirmation",
                confirmDialogTxtMsg: "Are you sure you want to save mapping file?"
            });



        }

        var currentReqIFImportGridHasUnsavedChanges = function () {
            currentReqIFImportGridHasUnsavedChangesStatus = true;
            
        }

        var currentReqIFImportGridIsSaved = function () {
            currentReqIFImportGridHasUnsavedChangesStatus = false;
        }



        this.buildReqIFImportLayout = function () {

            //Setup HTML page

            $(reqIFImportContainer).addClass("req-if-import-container req-if-import");

            reqIFImportContainerId = $(reqIFImportContainer).attr("id");


            var toggleLayout = '<div id="req-if-import-form-' + reqIFImportContainerId + '" class="req-if-import-form form-horizontal">' +
                '<span id="req-if-import-popup-notification-' + reqIFImportContainerId + '"></span>' +


                '<div id="req-if-import-table-' + reqIFImportContainerId + '" class="req-if-import-table">' +
                '</div>';

            var btnToggleOn = '<div class="row" style="border: 1px solid #ddd; padding-bottom: 10px; margin-bottom: 10px;margin-top: 30px; margin-left: 1px;">' +
                '<div class="col-md-6">' +
                '<input name="files" id="req-if-import-import-mapping-file-input-' + reqIFImportContainerId + '" type="file" aria-label="files" accept=".xml"/>' +
                '</div>' +
                '<div class="col-md-6 text-right" style="margin-top:15px;">' +
                '<button id="req-if-import-delete-button-' + reqIFImportContainerId + '" class="btn btn-light">Delete</button>' +
                '<button id="req-if-import-export-mapping-toggle-button-' + reqIFImportContainerId + '" class="btn btn-light">Toggle Off</button>' +
                '<button id="req-if-import-export-mapping-file-button-' + reqIFImportContainerId + '" class="btn btn-light">Download</button>' +
                '<button id="req-if-import-reset-button-' + reqIFImportContainerId + '" class="btn btn-light" >Reset</button>' +
                '<button id="req-if-import-save-req-if-button-' + reqIFImportContainerId + '" class="btn btn-info" >Save</button>' +
                '<button id="req-if-import-close-button-' + reqIFImportContainerId + '" class="btn btn-light" >Close</button>' +
                '</div>' +

                '</div>';


         var btnToggleOff = '<div class="row" style="border: 1px solid #ddd; padding-top: 10px; padding-bottom: 10px; margin-bottom: 10px;margin-top: 30px; margin-left: 1px;">' +
                '<div class="col-md-6">' +
                '<input name="files" id="req-if-import-import-mapping-file-input-' + reqIFImportContainerId + '" type="file" aria-label="files" accept=".xml"/>' +
                '</div>' +
                '<div class="col-md-6 text-right" style="margin-top:15px;">' +
                '<button id="req-if-import-delete-button-' + reqIFImportContainerId + '" class="btn btn-light">Delete</button>' +
                '<button id="req-if-import-export-mapping-file-button-' + reqIFImportContainerId + '" class="btn btn-light">Download</button>' +
                '<button id="req-if-import-reset-button-' + reqIFImportContainerId + '" class="btn btn-light" >Reset</button>' +
                '<button id="req-if-import-save-req-if-button-' + reqIFImportContainerId + '" class="btn btn-info" >Save</button>' +
                '<button id="req-if-import-close-button-' + reqIFImportContainerId + '" class="btn btn-light" >Close</button>' +
                '</div>' +

                '</div>';

            if (toggleConfig) {

                $(reqIFImportContainer).append(
                    $(
                        toggleLayout
                    ));
                $("#req-if-import-button-container").append(
                    $(
                        btnToggleOn
                    ));
            }
            else {
                $(reqIFImportContainer).append(
                    $(
                        toggleLayout
                    ));
                $("#req-if-import-button-container").append(
                    $(
                        btnToggleOff
                    ));
            }




          

            popupNotification = $("#req-if-import-popup-notification-" + reqIFImportContainerId).kendoNotification({
                position: {
                    pinned: true,
                    top: 20,
                    right: 40
                }
            }).data("kendoNotification");
        

            //Import Mapping File Button
            $(reqIFImportContainer).find("#req-if-import-import-mapping-file-button-" + reqIFImportContainerId).kendoButton({
                click: function (e) {
                   
                    if (currentReqIFImportGridHasUnsavedChangesStatus) {
                        e.preventDefault();
                        setTimeout(function () {
                            $(e.node).find(".k-state-focused").removeClass("k-state-focused");
                        });

                        //showDialogNotificationBox("Unsaved changes", "You have unsaved changes. Do you want to continue?", "Yes", function () {

                        //    $("#req-if-import-import-mapping-file-input-" + reqIFImportContainerId).click();

                        //}, "No", function () {
                        //    e.preventDefault();

                        //});


                        $("#import-plugin-confirmation-dialog").openConfirmationDialog({
                            onConfirmationDialogOk: () => {
                                $("#req-if-import-import-mapping-file-input-" + reqIFImportContainerId).click();
                            },
                            onConfirmationDialogNo: () => {
                                e.preventDefault();
                            },
                            showCancelButton: true,
                            showNoButton: false,
                            OkBtnText: "Yes",
                            CancelBtnText: "No",
                            confirmDialogTitle: "Unsaved changes",
                            confirmDialogTxtMsg: "You have unsaved changes. Do you want to continue?"
                        });




                    } else {
                        $("#req-if-import-import-mapping-file-input-" + reqIFImportContainerId).click();
                    }
                }
            });


            //Import Toggle Parent Fields Button
            $("#req-if-import-button-container").find("#req-if-import-export-mapping-toggle-button-" + reqIFImportContainerId).kendoButton({
                click: function () {
                    if (typeof settings.showProgressBar === 'function') {
                        settings.showProgressBar.call(this);
                    }

                    setTimeout(function () {
                        toggleOnOffParentIds();

                    }, 1000);
                }
            });

            //Export Mapping File Button
            $("#req-if-import-button-container").find("#req-if-import-export-mapping-file-button-" + reqIFImportContainerId).kendoButton({
                click: function () {
                    exportMappingFile();
                }
            });

            //Save template ReqIF Button
            $("#req-if-import-button-container").find("#req-if-import-save-req-if-button-" + reqIFImportContainerId).kendoButton({
                click: function () {
                    saveMappingFile();
                }
            });

            //Reset Button
            $("#req-if-import-button-container").find("#req-if-import-reset-button-" + reqIFImportContainerId).kendoButton({
                click: function (e) {
                    if (currentReqIFImportGridHasUnsavedChangesStatus) {
                        e.preventDefault();
                        setTimeout(function () {
                            $(e.node).find(".k-state-focused").removeClass("k-state-focused");
                        });

                        //showDialogNotificationBox("Unsaved changes", "You have unsaved changes which will be lost. Are you sure you want to continue?", "Yes", function () {

                        //    resetConfiguration();

                        //}, "No", function () {
                        //    e.preventDefault();
                        //});


                        $("#import-plugin-confirmation-dialog").openConfirmationDialog({
                            onConfirmationDialogOk: () => {
                                resetConfiguration();
                            },
                            onConfirmationDialogNo: () => {
                                e.preventDefault();
                            },
                            showCancelButton: true,
                            showNoButton: false,
                            OkBtnText: "Yes",
                            CancelBtnText: "No",
                            confirmDialogTitle: "Unsaved changes",
                            confirmDialogTxtMsg: "You have unsaved changes which will be lost. Are you sure you want to continue?"
                        });


                    } else {
                        resetConfiguration();
                    }
                    //resetConfiguration();
                }
            });

            //Close Button
            $("#req-if-import-button-container").find("#req-if-import-close-button-" + reqIFImportContainerId).kendoButton({
                click: function (e) {

                    $("#import-plugin-confirmation-dialog").openConfirmationDialog({
                       onConfirmationDialogOk: onMappingDesignerOK,
                        onConfirmationDialogNo: onMappingDesignerCancel,
                        showCancelButton: true,
                        showNoButton: false,
                        OkBtnText: "Yes",
                        CancelBtnText: "No",
                        confirmDialogTitle: "Close Confirmation",
                        confirmDialogTxtMsg: "Are you sure you want to close mapping designer?"
                    });                    

                }
            });

            var projectGuid = settings.projectGuid;
            var onMappingDesignerOK = function () {
                window.location.href = serverUrl + '/Default/ImportReqIFDialog?isClose=true&projectGuid=' + projectGuid;
            }

            var onMappingDesignerCancel = function () {

                $("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).kendoWindow();
                 var dialog = $("#req-if-import-dialog-msg-box-" + reqIFImportContainerId).data("kendoWindow")
                 dialog.close();
            }

            //Delete Button
            $("#req-if-import-button-container").find("#req-if-import-delete-button-" + reqIFImportContainerId).kendoButton({
                click: function (e) {

                    $("#import-plugin-confirmation-dialog").openConfirmationDialog({
                        onConfirmationDialogOk: onDeleteMappingConfrimationOK,                       
                        showCancelButton: true,
                        showNoButton: false,
                        OkBtnText: "Yes",
                        CancelBtnText: "No",
                        confirmDialogTitle: "Delete Existing Mapping",
                        confirmDialogTxtMsg: "Are you sure you want to delete the existing mapping? It is advised to download existing mapping before deleting."
                    });

                }
            });
            
            var onDeleteMappingConfrimationOK = function () {
                toggleValue == enumToggleValue.toggle_off
                if (typeof settings.showProgressBar === 'function') {
                    settings.showProgressBar.call(this);
                }

                toggleOnOffParentIds(true);

                var mappingJSON = createJSON();
               

                if (typeof settings.onSaveTemplate === 'function') {
                    settings.onSaveTemplate(mappingJSON, projectGuid, onDeleteTemplateSuccessCallback, failureCallback);
                }

                
            }

           function onDeleteTemplateSuccessCallback() {
              
              
              showPopupNotification("Mapping template has been successfully deleted!", "success");              

               setTimeout(function () {
                   if (typeof settings.hideProgressBar === 'function') {
                       settings.hideProgressBar.call(this);
                   }

                   filePath = sessionStorage.getItem("AdoFilePath");
                   if (filePath !== undefined && filePath !== null) {
                       window.location.href = filePath;
                   } else {
                       window.location.href = serverUrl + '/Default/ImportReqIFDialog?isClose=true&projectGuid=' + settings.projectGuid;                   
                   }
                   
               }, 1000);  
            }

            //Kendo Upload
            $("#req-if-import-import-mapping-file-input-" + reqIFImportContainerId).kendoUpload({
                multiple: false,
                async: {
                    chunkSize: 11000,// bytes
                    autoUpload: false
                },
                validation: {
                    maxFileSize: 4194304,
                    allowedExtensions: ['xml']
                },
                localization: {
                    select: "Upload Mapping File",
                    uploadSelectedFiles: "Add",
                    headerStatusUploaded: "Process complete",
                    headerStatusUploading: "Uploading file, please wait...",
                    statusFailed: "File processing failed",
                    statusUploaded: "File validating...",
                    statusUploading: "On-process..."
                },
                select: function (element) {
                   
                    $("#import-plugin-confirmation-dialog").openConfirmationDialog({
                        onConfirmationDialogOk: () => {
                            $.each(element.files, function (index, value) {
                                handleMappingFileImport(value.rawFile);
                            });
                            currentReqIFImportGridHasUnsavedChanges();
                        },
                      
                        onConfirmationDialogCancel: () => {
                            $.each(element.files, function (index, value) {
                                handleMappingFileImport(value.rawFile,true);
                            });
                            currentReqIFImportGridHasUnsavedChanges();
                        },
                        showCancelButton: true,
                        showNoButton: false,
                        OkBtnText: "Overwrite",
                        CancelBtnText: "Merge",
                        NoBtnText: "Cancel",
                        confirmDialogTitle: "Upload Mapping",
                        confirmDialogTxtMsg: "Do you want to overwrite the mapping file or merge with the existing ReqIF?"
                    });

                   
                }
            }).data("kendoUpload");

            $('#import-plugin-confirmation-dialog').on('hidden.bs.modal', function (e) {
                console.log('Modal has been closed');
              
                clearKendoUploadFileSelection();
            });
            $("#req-if-import-table-" + reqIFImportContainerId).kendoTreeList({
                height: 400,
                filterable: false,
                sortable: false,
                scrollable: true,
                columns: [
                    {
                        field: "Reqif",
                        title: "ReqIF",
                        width: '30%',
                        template: function (dataItem) {
                            if (!(dataItem === null || dataItem === undefined))
                            {
                                var reqIf = dataItem.Reqif;

                                // Trim the text if it exceeds the allowed Length
                                var trimmedText = (reqIf.length > maxLength) ? reqIf.substring(0, maxLength - 3) + "..." : reqIf;

                                // Add tooltip attribute with full untrimmed text
                                return "<span title='" + reqIf + "'>" + trimmedText + "</span>";
                            }
                            
                            return "<span title='No Data'>No Data</span>";
                        }
                        
                    },
                    {
                        field: "ReqifFieldType",
                        title: "ReqIF Field Type",
                        width: '15%',
                        template: kendo.template($toggleReqIFFieldTypeTemplate.html())
                    },

                    {
                        field: "Import",
                        title: "Import",
                        width: '10%',
                        template: kendo.template($toggleSwitchTemplate.html())
                    },
                    {
                        field: "ado",
                        title: "ADO",
                        width: '30%',
                        template: kendo.template($ADOTemplate.html())
                    },
                    {
                        field: "FieldType",
                        title: "Field Type",
                        width: '15%',
                        template: kendo.template($toggleFieldTypeTemplate.html())
                    }
                ],

                dataBound: function () {
                    onReqIFImportTableDataBound();
                    }
            });

           

            $("#req-if-import-table-" + reqIFImportContainerId).find('.k-grid-header').after(
                $('<div>' +
                    '<img id="img-expand" src="/Content/images/plus-active.png" class="req-if-import-table-expand-all" alt="Expand All" >'+
                    '<img id="img-collapse" src="/Content/images/minus.png" class="req-if-import-table-collapse-all" alt="Collapse All" >' +                  
                    '</div>')                
            );


            $("#req-if-import-table-" + reqIFImportContainerId).find('.req-if-import-table-expand-all').on('click', function () {
                var treelist = $("#req-if-import-table-" + reqIFImportContainerId).data('kendoTreeList');
                tableTreeListDatasource.schema.model.expanded = true;

                treelist.setDataSource(tableTreeListDatasource);
                document.getElementById("img-expand").src = "/Content/images/plus-active.png";
                document.getElementById("img-collapse").src = "/Content/images/minus.png";
            });


            $("#req-if-import-table-" + reqIFImportContainerId).find('.req-if-import-table-collapse-all').on('click', function () {
                var treelist = $("#req-if-import-table-" + reqIFImportContainerId).data('kendoTreeList');
                tableTreeListDatasource.schema.model.expanded = false;

                treelist.setDataSource(tableTreeListDatasource);
                document.getElementById("img-expand").src = "/Content/images/plus.png";
                document.getElementById("img-collapse").src = "/Content/images/minus-active.png";
            });


            applyMappingJSON(settings.MappingJSON);

            // Show warning notification for duplicateLongName if it exists
            if (duplicateLongName) {
                showPopupNotification("Following fields have the same name but contain duplicate data types or different enum values (" + duplicateLongName + ")", "warning");
            }          

            if (typeof settings.onInitComplete === 'function') {
                settings.onInitComplete();
            }

            if (typeof settings.hideProgressBar == 'function') {
                settings.hideProgressBar.call(this);
            }
            return this;
            

        };        

        return this;
    }    
})(jQuery);