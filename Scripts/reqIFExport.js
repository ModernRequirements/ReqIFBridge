(function ($) {
    var currentRowIndex = -1;
    var maxLength=50;
    var enumControlType = {
        simpleDropDown: 0,
        MultiSelectDropDown: 1
    };

    var enumValuesType = {
        string: 0,
        integer: 1
    };

    var fieldTypes = [
        'Numeric',
        'String',
        'Enum',
        'RichText',
        'DateTime'
    ];
    

    var $toggleSwitchTemplate = $(
        '<script id="toggleSwitchTemplate" type="text/x-kendo-template">' +
        '<div class="req-if-export-table-import-column-switch-container">' +
        '<input type="checkbox" class="req-if-export-table-import-column-switch" aria-label="Notifications Switch" checked="checked"/> <span class="req-if-export-table-import-column-switch-label">#: Export #</span></div>' +
        '</script>'
    );

    var $ReqifTemplate = $(
        '<script id="ReqifTemplate" type="text/x-kendo-template">' +
        '<div class="req-if-export-reqif-column-textbox-container">' +
        '<input class="req-if-export-req-if-column-textbox" style="width: 250px;"/>' +
        '</div>' +
        '</script>'
    );

    var $toggleFieldTypeTemplate = $(
        '<script id="toggleFieldTypeTemplate" type="text/x-kendo-template">' +
        '<div class="req-if-export-field-type-column-combobox-container">' +
        '<input class="req-if-export-field-type-column-combobox" placeholder="--not selected--" style="width: 100%;"/>' +
        '</div>' +
        '</script>'
    );

    var $toggleReqIFFieldTypeTemplate = $(
        '<script id="toggleReqIFFieldTypeTemplate" type="text/x-kendo-template">' +
        '<div class="req-if-import-field-type-column-combobox-container">' +
        '<input class="req-if-export-reqif-field-type-column-combobox" placeholder="--not selected--" style="width: 100%;"/>' +
        '</div>' +
        '</script>'
    );

    $.fn.reqIFExport = function () {
        
        var reqIFExportContainer = this;
        var reqIFExportContainerId = "";
        var popupNotification = null;
        let globalUniqueId = 0;

        var currentReqIFExportGridHasUnsavedChangesStatus = false;

        var enumValueFilter = {
            field: "type", operator: "neq", value: "EnumValueMap"
        }

        var tableTreeListDatasource = {
            data: [],
            schema: {
                model: {
                    id: "id",
                    expanded: true
                }
            },
            filter: enumValueConfig ? {} : enumValueFilter
        }

       // tableTreeListDatasource.filter = enumValueFilter;

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
            onImportMapping: null
        };

        var settings = null;

        //type : error / info
        var showPopupNotification = function (message, type) {
            popupNotification.hide();
            message = " " + message;
            popupNotification.show(message, type);
        }

        var getReqIFExportContainerId = function () {
            return reqIFExportContainerId;
        }

        var getDataFromGrid = function () {
            let typeMaps = [];
            let linkMaps = [];


            $.each(tableTreeListDatasource.data, function (index, dataSourceItem1) { // Iterates through a collection

                if (dataSourceItem1.type === 'TypeMap' && dataSourceItem1.Export) {

                    //get EnumFieldMaps items on datasource for that TypeMap
                    let EnumFieldMaps = [];
                    $.each(tableTreeListDatasource.data, function (index, dataSourceItem2) {
                        if (dataSourceItem2.parentId === dataSourceItem1.id && dataSourceItem2.Export) {

                            //get EnumValueMaps items on datasource for that EnumFieldMap
                            let EnumValueMaps = [];
                            $.each(tableTreeListDatasource.data, function (index, dataSourceItem3) {
                                if (dataSourceItem3.parentId === dataSourceItem2.id && dataSourceItem3.Export) {
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

                if (dataSourceItem1.type === 'LinkMap' && dataSourceItem1.Export) {


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
                            "FieldType": fieldMap.properties.FieldType,
                            "ReqIFFieldType": fieldMap.properties.FieldType,
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

        var getWorkitemFieldTypeFromName = function (workitemFields,propertyName) {
            
            var fieldValue = "";
            $.each(workitemFields, function (index, value) {
                if (value.name === propertyName) {
                    fieldValue = value;
                    return false;
                }
            });
            return fieldValue;
        }

        var getworkitemFieldsByType= function (workitemType) {

            var workitemFields = [];

            $.each(getWorkItemData().workItemAndFields.workItemAndFieldsData, function (index, element) {

                if (index === workitemType) {
                    workitemFields = element;
                    return false;
                }

            });

            return workitemFields;
        }


        var getWorkitemFieldAllowedValues = function (workitemType, propertyName) {

            var workitemFields = [];

            $.each(getWorkItemData().workItemAndFields.workItemAndFieldsData, function (index, element) {

                if (index === workitemType) {
                    workitemFields = element;
                    return false;
                }

            });

            var allowedValues = [];
            $.each(workitemFields, function (index, value) {
                if (value.name === propertyName) {

                    allowedValues = value.allowedValues;
                    return false;
                }
            });

            return allowedValues;
        }

       

        this.InitializeReqIFExport = function (options) {

            settings = $.extend({}, defaults, options);

            if (typeof settings.showProgressBar == 'function') { // make sure the callback is a function
                //settings.showProgressBar.call(this); // brings the scope to the callback
            }

            return this.buildReqIFExportLayout();
        };

        var getWorkItemFieldsDatasourceFromWorkItemType = function (workitemType, exceptedFields = []) {
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
                if (!exceptedFields.includes(item.name))
                    fieldDatasource.push({
                        text: item.name, value: item.name
                    });

            });


            var fieldDataSource = new kendo.data.DataSource({
                data: fieldDatasource
            });

            return fieldDataSource;
        }

        var onReqIFExportTableDataBound = function (arg) {

            var treeList = $("#req-if-export-table-" + reqIFExportContainerId).data('kendoTreeList');


            $(reqIFExportContainer).find('.req-if-export-table-import-column-switch').each(function () {

                var data = treeList.dataItem($(this).closest('tr'));


                if (data.Export !== null) {
                    let onOFF = data.Export ? "On" : "Off";
                    let label = $(this).closest('.req-if-export-table-import-column-switch-container').find('.req-if-export-table-import-column-switch-label');
                    label.text(onOFF);

                    $(this).kendoMobileSwitch({
                        enable: data.Ado === 'System.Title' || (data.type == 'TypeMap' && countWorkItemOnTableTreeListDatasource(tableTreeListDatasource) === 1) ? false:true,
                        checked: data.Export,
                        messages: {
                            checked: "",
                            unchecked: ""
                        },
                        change: function (e) {
                            let onOFF = e.checked ? "On" : "Off";
                            let label = this.element.closest('.req-if-export-table-import-column-switch-container').find('.req-if-export-table-import-column-switch-label');
                            label.text(onOFF);

                            // Updating row on global datasource
                            var row = this.element.closest('tr');
                            currentRowIndex = row.index();
                            var dataItemRow = treeList.dataItem(row);

                            var dataItemIndex = tableTreeListDatasource.data.findIndex(x => x.id === dataItemRow.id);

                            var newObj = tableTreeListDatasource.data[dataItemIndex];
                     
                            newObj.Export = e.checked;

                            tableTreeListDatasource.data[dataItemIndex] = newObj;

                            // All other controls on row go gray/not gray
                            e.checked ? row.find('.req-if-export-req-if-column-textbox').prop("disabled", false) : row.find('.req-if-export-req-if-column-textbox').prop("disabled", true);

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

                                newObj.Export = e.checked;

                                tableTreeListDatasource.data[indexVal] = newObj;

                     
                            });


                            // Put Parent to ON if current checked status is ON
                            if (e.checked && data.parentId !== null) {
                                var parentIndex = tableTreeListDatasource.data.findIndex(x => x.id === data.parentId);
                                var titleIndex = tableTreeListDatasource.data.findIndex(x => x.parentId === data.parentId && x.Ado === 'System.Title');
                                var newtitleObj = tableTreeListDatasource.data[titleIndex];
                                var newParentObj = tableTreeListDatasource.data[parentIndex];

                                newtitleObj.Export = e.checked;
                                newParentObj.Export = e.checked;

                                tableTreeListDatasource.data[parentIndex] = newParentObj;
                                tableTreeListDatasource.data[titleIndex] = newtitleObj;
                            }

                            //tableTreeListDatasource.filter = enumValueFilter;

                            treeList.setDataSource(tableTreeListDatasource);


                            // ReqIFExport Unsaved changes
                            currentReqIFExportGridHasUnsavedChanges();

                            //bug-fix -- scroll to top on toggle when data is updated
                            if (currentRowIndex != -1) {

                                var currentRow = $('table[role="treegrid"]').find('tr').eq(currentRowIndex);
                                var rowPosition = currentRow.position().top;
                                $("#" + getReqIFExportContainerId() + "-main").scrollTop(rowPosition);
                            }

                        }
                    });
                } else {
                    $(this).closest('.req-if-export-table-import-column-switch-container').remove();
                }
            });



            $(reqIFExportContainer).find(".req-if-export-req-if-column-textbox").each(function () {
                var dataItemRow = treeList.dataItem($(this).closest('tr'));
                var $row = $(this).closest('tr');

                if (dataItemRow.Reqif !== null) {
                    $(this).val(dataItemRow.Reqif);

                    $(this).on('change', function () {
                        //Updating row on global datasource

                        var dataItemIndex = tableTreeListDatasource.data.findIndex(x => x.id === dataItemRow.id);

                        var newObj = tableTreeListDatasource.data[dataItemIndex];

                        newObj.Reqif = $(this).val();;

                        tableTreeListDatasource.data[dataItemIndex] = newObj;

                        // ReqIFExport Unsaved changes
                        currentReqIFExportGridHasUnsavedChanges();


                    });

                    //Disable if Export == false
                    if (!dataItemRow.Export) {
                        $row.find('.req-if-export-req-if-column-textbox').prop("disabled", true);
                    }
                } else {

                    $(this).closest('.req-if-export-reqif-column-textbox-container').remove();
                }

            
                      

            });


            $(reqIFExportContainer).find(".req-if-export-field-type-column-combobox").each(function () {
                var data = treeList.dataItem($(this).closest('tr'));
                var $row = $(this).closest('tr');
             
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
                } else if (data.type === 'TypeMap') {

                    // Add button

                    $(this).css('width', '88%');
                    $(this).after($('<button type="button" class="k-button req-if-export-workitem-add-field" aria-label="Add Field">' +
                        '<span class="k-icon k-i-plus" title="Add Field"></span></button>'));
                    var dataSourceArray = [];

                    for (var index = 0; index < fieldTypes.length; index++) {
                        dataSourceArray.push({
                            text: fieldTypes[index], value: index
                        });
                    }

                    // Excepted fields
                    let exceptedFields = [];

                    $.each(tableTreeListDatasource.data, function (index, item) {
                        if (item.parentId === data.id) {
                            exceptedFields.push(item.Ado);
                        }
                    });


                    var fieldsComboBox = $(this).kendoComboBox({
                        dataTextField: "text",
                        dataValueField: "value",
                        dataSource: getWorkItemFieldsDatasourceFromWorkItemType(data.Ado, exceptedFields),
                        filter: "contains",
                        suggest: true,
                        index: -1
                    });


                    // Add field button onclick
                    $row.find('.req-if-export-workitem-add-field').on('click', function (e) {
                        //GET CONTROL REFERENCE
                        

                        var workItemFieldComboBox = fieldsComboBox.data('kendoComboBox');
                        var treeList = $("#req-if-export-table-" + reqIFExportContainerId).data('kendoTreeList');
                        var data = treeList.dataItem($(this).closest('tr'));

                        if (data.Export) {
                           
                            var fieldValue = workItemFieldComboBox.value();                           
                            if (fieldValue !== '') {
                             
                                var workitemFields = getworkitemFieldsByType(data.Ado);

                                var workitemFieldValue = getWorkitemFieldTypeFromName(workitemFields, fieldValue);

                                if (workitemFieldValue !== '') {
                                    var fieldType = workitemFieldValue.type;
                                    var allowedValues = workitemFieldValue.allowedValues; //getWorkitemFieldAllowedValues(data.Ado, fieldValue);

                                    var treelist = $("#req-if-export-table-" + reqIFExportContainerId).data('kendoTreeList');
                                    var fieldMapId = ++globalUniqueId;
                                    tableTreeListDatasource.data.push({
                                        id: fieldMapId,
                                        Reqif: fieldValue,
                                        Export: true,
                                        Ado: fieldValue,
                                        FieldType: fieldType,//??
                                        parentId: data.id,
                                        type: 'FieldMap'
                                    });

                                    if (allowedValues.length > 0) {
                                        $.each(allowedValues, function (index, value) {
                                            tableTreeListDatasource.data.push({
                                                id: ++globalUniqueId,
                                                Reqif: value,
                                                Export: true,
                                                Ado: value,
                                                FieldType: null,
                                                parentId: fieldMapId,
                                                type: 'EnumValueMap'
                                            });
                                        })
                                    }

                                    //var enumValueFilter = {
                                    //    field: "type", operator: "neq", value: "EnumValueMap"
                                    //}
                                   // tableTreeListDatasource.filter = enumValueFilter;
                                    
                                    treelist.setDataSource(tableTreeListDatasource);

                                    // ReqIFExport Unsaved changes
                                    currentReqIFExportGridHasUnsavedChanges();
                                } else {
                                    showPopupNotification(" Please select a valid work item field!", "warning");
                                }

                               
                            } else {
                                showPopupNotification(" Please select a valid work item field!", "warning");
                            }
                        }


                       
                    });


                }
                else {
                    $(this).remove();
                }


                // Freeze Ado Title
                if (data.Ado === 'Title' || data.Ado === 'Title') {
                    //$row.find('.req-if-export-req-if-column-textbox').prop("disabled", true);

                }


            });



            $(reqIFExportContainer).find(".req-if-export-reqif-field-type-column-combobox").each(function () {
                var data = treeList.dataItem($(this).closest('tr'));
                var $row = $(this).closest('tr');

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

                    fieldTypeComboBox.data('kendoComboBox').readonly(false);
                    fieldTypeComboBox.data('kendoComboBox').enable(true);
                    fieldTypeComboBox.data('kendoComboBox').value(data.FieldType);
                } 
                else {
                    $(this).remove();
                }

            });

        }


        var applyMappingJSON = function (mappingJSON) {
           
        
            var mappingFileData = [];


            $TypeMaps = mappingJSON.TypeMaps ? mappingJSON.TypeMaps : [];
           
            if ($TypeMaps.length > 0) {
               
                // TypeMaps
                $.each($TypeMaps, function (index, typeMap) {
                    let typeId = ++globalUniqueId;

                  

                    let ReqIFTypeName = typeMap.ReqIFTypeName;
                    let WITypeName = typeof typeMap.WITypeName !== 'undefined' ? typeMap.WITypeName: null;

                    mappingFileData.push({
                        id: typeId,
                        Reqif: ReqIFTypeName,
                        Export: true,
                        Ado: WITypeName,
                        FieldType: null,
                        parentId: null,
                        type: 'TypeMap',
                      //  ReqifFieldType: 
                    });

                    //EnumFieldMaps
                    $EnumFieldMaps = typeMap.EnumFieldMaps ? typeMap.EnumFieldMaps : [];


                    if ($EnumFieldMaps.length > 0) {

                        $.each($EnumFieldMaps, function (index, enumFieldMap) {
                            let fieldId = ++globalUniqueId;

                            let WIFieldName = typeof enumFieldMap.WIFieldName !== 'undefined' ? enumFieldMap.WIFieldName: null;
                            let ReqIFFieldName = enumFieldMap.ReqIFFieldName;
                            let FieldType = enumFieldMap.FieldType;

                          
                            mappingFileData.push({
                                id: fieldId,
                                Reqif: ReqIFFieldName,
                                Export: true,
                                Ado: WIFieldName,
                                FieldType: FieldType,
                                parentId: typeId,
                                type: 'FieldMap',
                                reqifFieldType: FieldType
                            });

                            //EnumValueMaps
                            $.each(enumFieldMap.EnumValueMaps, function (index, enumValueMap) {
                                let enumValueId = ++globalUniqueId;

                                let WIEnumFieldValue = typeof enumValueMap.WIEnumFieldValue !== 'undefined' ? enumValueMap.WIEnumFieldValue: null;
                                let ReqIFEnumFieldValue = enumValueMap.ReqIFEnumFieldValue;

                  
                                mappingFileData.push({
                                    id: enumValueId,
                                    Reqif: ReqIFEnumFieldValue,
                                    Export: true,
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
                mappingFileData.push({
                    id: 'LinkTypes',
                    Reqif: null,
                    Export: true,
                    Ado: 'Link Types',
                    FieldType: null,
                    parentId: null,
                    type: 'LinkTypes'
                });
                $.each($LinkMaps, function (index, linkMap) {

                    let linkId = ++globalUniqueId;

                    let WILinkName = linkMap.WILinkName;
                    let ReqIFRelationName = linkMap.ReqIFRelationName;

                    mappingFileData.push({
                        id: linkId,
                        Reqif: ReqIFRelationName,
                        Export: true,
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
                        expanded: true
                    }
                },
                filter: enumValueConfig ? {} : enumValueFilter
            };


            var dataSource = new kendo.data.TreeListDataSource(tableTreeListDatasource);


            $("#req-if-export-table-" + reqIFExportContainerId).data('kendoTreeList').setDataSource(dataSource);


            //showPopupNotification("Grid successfully updated!", "success");

            currentReqIFExportGridIsSaved();


        }


        var countWorkItemOnTableTreeListDatasource = function (tableTreeListDatasourceParam) {
            let count = 0;
            $.each(tableTreeListDatasourceParam.data, function (index, item) {
                if (item.type === 'TypeMap')
                    count++;
            });

            return count;
        }


        var handleMappingFileImport = function (file) {
            var reader = new FileReader();

            // Closure to capture the file information.
            reader.onload = (function (theFile) {
                return function (e) {
                    ImportedMappingFileXmlString = e.target.result
                    if (typeof settings.onImportMapping === 'function') {
                        settings.onImportMapping(ImportedMappingFileXmlString, applyMappingJSON, failureCallback);
                    }

                    //$xmlDoc = $.parseXML(ImportedMappingFileXmlString);
                    //$xml = $($xmlDoc);
                    //console.log($xml);

                };
            })(file);

            reader.readAsText(file, 'UTF-8');
        }

        var resetConfiguration = function () {
            if (typeof settings.showProgressBar === 'function') {
                settings.showProgressBar.call(this);
            }

            applyMappingJSON(settings.MappingJSON);

            if (typeof settings.hideProgressBar === 'function') {
                settings.hideProgressBar.call(this);
            }

            var id = "#req-if-export-import-mapping-file-input-" + reqIFExportContainerId;
            var upload = $(id).data("kendoUpload");
            upload.removeAllFiles();

        }

        var showDialogNotificationBox = function (title, contentMsg, okButtonText, okButtonCallback, cancelButtonText, cancelButtonCallback) {
            var dialogDiv = $('<div id="req-if-export-dialog-msg-box-' + reqIFExportContainerId + '" class="req-if-export-dialog-msg-box"></div>');
            reqIFExportContainer.prepend(dialogDiv);

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


            $('#req-if-export-dialog-msg-box-' + reqIFExportContainerId).kendoDialog({
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
            currentReqIFExportGridIsSaved();
            if (typeof settings.showProgressBar === 'function') {
                settings.hideProgressBar.call(this);
            }
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
                    window.location.href = serverUrl + '/Default/ExportReqIFDialog';
                }
            }, 1000);
        }
                
        
        var exportMappingFile = function () {
                  

            //showDialogNotificationBox("Download Confirmation", "Are you sure you want to download the current mapping file?", "Yes", function () {

                
            //        if (typeof settings.showProgressBar === 'function') {
            //            settings.showProgressBar.call(this);
            //        }          
                

            //    var mappingJSON = createJSON();

            //    var projectGuid = settings.projectGuid;

            //    if (typeof settings.onSaveTemplate === 'function') {
            //        settings.onExportMapping(mappingJSON, projectGuid, onExportMappingSuccessCallback, failureCallBackExport);
            //    }
             

            //}, "No", function () {
            //    $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).kendoWindow();
            //    var dialog = $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).data("kendoWindow")
            //    dialog.close();
            //});


            $("#export-plugin-confirmation-dialog").openConfirmationDialog({
                onConfirmationDialogOk: () => {

                    if (typeof settings.showProgressBar === 'function') {
                        settings.showProgressBar.call(this);
                    }

                    var mappingJSON = createJSON();

                    var projectGuid = settings.projectGuid;

                    if (typeof settings.onSaveTemplate === 'function') {
                        settings.onExportMapping(mappingJSON, projectGuid, onExportMappingSuccessCallback, failureCallBackExport);
                    }

                },
                onConfirmationDialogNo: () => {
                    $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).kendoWindow();
                    var dialog = $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).data("kendoWindow")
                    dialog.close();
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
            //        $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).kendoWindow();
            //        var dialog = $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).data("kendoWindow")
            //    dialog.close();
            //});


            $("#export-plugin-confirmation-dialog").openConfirmationDialog({
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

                NoBtnText: "Save & Close",
                showCancelButton: true,
                showNoButton: true,
                OkBtnText: "Yes",
                CancelBtnText: "No",
                confirmDialogTitle: "Save Confirmation",
                confirmDialogTxtMsg: "Are you sure you want to save mapping file?"
            });



            
        }

        var currentReqIFExportGridHasUnsavedChanges = function () {
            currentReqIFExportGridHasUnsavedChangesStatus = true;
        }

        var currentReqIFExportGridIsSaved = function () {
            currentReqIFExportGridHasUnsavedChangesStatus = false;
      
        }


        this.buildReqIFExportLayout = function () {

            //Setup HTML page

            $(reqIFExportContainer).addClass("req-if-export-container req-if-export");

            reqIFExportContainerId = $(reqIFExportContainer).attr("id");

            $(reqIFExportContainer).append(
                $('<div id="req-if-export-form-' + reqIFExportContainerId + '" class="req-if-export-form form-horizontal">' +
                    '<span id="req-if-export-popup-notification-' + reqIFExportContainerId + '"></span>' +


                   

                    '<div id="req-if-export-table-' + reqIFExportContainerId + '" class="req-if-export-table">' +
                    '</div>' +


                    '<div class="row" style="border: 1px solid #ddd; padding-top: 10px; padding-bottom: 10px; margin-bottom: 10px;margin-top: 30px; margin-left: 1px;">' +
                    '<div class="col-md-6">' +  
                    '<input name="files" id="req-if-export-import-mapping-file-input-' + reqIFExportContainerId + '" type="file" aria-label="files" accept=".xml"/>' +
                    '</div>' +
                    '<div class="col-md-6 text-right" style="margin-top:15px;">' +                    
                    '<button id="req-if-export-export-mapping-file-button-' + reqIFExportContainerId + '" class="btn btn-light">Download</button>' +
                    '<button id="req-if-export-reset-button-' + reqIFExportContainerId + '" class="btn btn-light" >Reset</button>' +
                    '<button id="req-if-export-import-req-if-button-' + reqIFExportContainerId + '" class="btn btn-info" >Save</button>' +
                    //'<button id="req-if-export-saveclose-req-if-button-' + reqIFExportContainerId + '" class="btn btn-info" >Save & Close</button>' +
                    '<button id="req-if-export-close-button-' + reqIFExportContainerId + '" class="btn btn-light" >Close</button>' +
                    '</div>' +
                    '</div>' 

                    
                  

                ));

            popupNotification = $("#req-if-export-popup-notification-" + reqIFExportContainerId).kendoNotification({
                position: {
                    pinned: true,
                    top: 20,
                    right: 40
                }
            }).data("kendoNotification");



            //Import Mapping File Button
            $(reqIFExportContainer).find("#req-if-export-import-mapping-file-button-" + reqIFExportContainerId).kendoButton({
                click: function (e) {

                    if (currentReqIFExportGridHasUnsavedChangesStatus) {
                        e.preventDefault();
                        setTimeout(function () {
                            $(e.node).find(".k-state-focused").removeClass("k-state-focused");
                        });

                        //showDialogNotificationBox("Unsaved changes", "You have unsaved changes. Do you want to continue?", "Yes", function () {

                        //    $("#req-if-export-import-mapping-file-input-" + reqIFExportContainerId).click();

                        //}, "No", function () {
                        //    e.preventDefault();

                        //});


                        $("#export-plugin-confirmation-dialog").openConfirmationDialog({
                            onConfirmationDialogOk: () => {

                                $("#req-if-export-import-mapping-file-input-" + reqIFExportContainerId).click();

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
                        $("#req-if-export-import-mapping-file-input-" + reqIFExportContainerId).click();
                    }
                }
            });

            //Export Mapping File Button
            $(reqIFExportContainer).find("#req-if-export-export-mapping-file-button-" + reqIFExportContainerId).kendoButton({
                click: function () {
                    exportMappingFile();
                }
            });
                                                                                                             
            //Save template ReqIF Button
            $(reqIFExportContainer).find("#req-if-export-import-req-if-button-" + reqIFExportContainerId).kendoButton({
                click: function () {
                    saveMappingFile();
                }
                
            });

        
            //Reset Button
            $(reqIFExportContainer).find("#req-if-export-reset-button-" + reqIFExportContainerId).kendoButton({
                click: function (e) {
          
                    if (currentReqIFExportGridHasUnsavedChangesStatus) {
                        e.preventDefault();
                        setTimeout(function () {
                            $(e.node).find(".k-state-focused").removeClass("k-state-focused");
                        });

                        //showDialogNotificationBox("Unsaved changes", "You have unsaved changes which will be lost. Are you sure you want to continue?", "Yes", function () {

                        //    resetConfiguration();

                        //}, "No", function () {
                        //    e.preventDefault();
                        //});

                        $("#export-plugin-confirmation-dialog").openConfirmationDialog({
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
                }
            });

            //Close Button
            $(reqIFExportContainer).find("#req-if-export-close-button-" + reqIFExportContainerId).kendoButton({
                click: function (e) {
                    //showDialogNotificationBox("Close Confirmation", "Are you sure you want to close mapping designer?", "Yes", function () {
                   
                    //    window.location.href = serverUrl + '/Default/ExportReqIFDialog';

                    //}, "No", function () {
                    //    $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).kendoWindow();
                    //    var dialog = $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).data("kendoWindow")
                    //    dialog.close();
                    //});


                    $("#export-plugin-confirmation-dialog").openConfirmationDialog({
                        onConfirmationDialogOk: () => {

                            window.location.href = serverUrl + '/Default/ExportReqIFDialog';

                        },
                        onConfirmationDialogNo: () => {
                            $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).kendoWindow();
                            var dialog = $("#req-if-export-dialog-msg-box-" + reqIFExportContainerId).data("kendoWindow")
                            dialog.close();
                        },
                        showCancelButton: true,
                        showNoButton: false,
                        OkBtnText: "Yes",
                        CancelBtnText: "No",
                        confirmDialogTitle: "Close Confirmation",
                        confirmDialogTxtMsg: "Are you sure you want to close mapping designer?"
                    });                    
                }
            });

            $("#req-if-export-import-mapping-file-input-" + reqIFExportContainerId).kendoUpload({
                multiple: false,
                async: {
                    chunkSize: 11000,// bytes
                    //saveUrl: "chunkSave",
                    //removeUrl: "remove",
                    autoUpload: false
                },
                validation: {
                    maxFileSize : 4194304,
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
                   
                    $.each(element.files, function (index, value) {
                        handleMappingFileImport(value.rawFile);
                        
                    });
                    currentReqIFExportGridHasUnsavedChanges();
                }
            }).data("kendoUpload");


            $("#req-if-export-table-" + reqIFExportContainerId).kendoTreeList({
                /*height: 500,*/
                filterable: false,
                sortable: false,
                scrollable: false,
                columns: [
                    {
                        field: "Ado",
                        title: "ADO",
                        width: '20%',
                        template: function (dataItem) {
                            if (!(dataItem === null || dataItem === undefined)) {
                            var ado = dataItem.Ado;

                            //trim the text if it exceeds the allowed length
                            var trimmedText = (ado.length > maxLength) ? ado.substring(0, maxLength - 3) + "...":ado;

                            // Add tooltip attribute with full untrimmed text
                            return "<span title='" + ado + "'>" + trimmedText + "</span>";
                            }
                            return "<span title='No Data'>No Data</span>";
                            
                        }
                       
                    },
                    {
                        field: "FieldType",
                        title: "Field Type",
                        width: '25%',
                        template: kendo.template($toggleFieldTypeTemplate.html())
                    },
                    {
                        field: "Export",
                        title: "Export",
                        width: '10%',
                        template: kendo.template($toggleSwitchTemplate.html())
                    },
                    {
                        field: "Reqif",
                        title: "ReqIF",
                        width: '20%',
                        template: kendo.template($ReqifTemplate.html())               
                    },
                    //{
                    //    field: "ReqifFieldType",
                    //    title: "ReqIF Field Type",
                    //    width: '15%',
                    //    template: kendo.template($toggleReqIFFieldTypeTemplate.html())
                    //},
               
                ],
                //filter: {
                //    field: "FieldType", operator: "neq", value: "EnumValueMap" },
                dataBound: onReqIFExportTableDataBound
            });


            $("#req-if-export-table-" + reqIFExportContainerId).find('.k-grid-header').after(
                $('<div>' +
                    '<img id="img-export-expand" src="/Content/images/plus-active.png" class="req-if-export-table-expand-all" alt="Expand All" >' +
                    '<img id="img-export-collapse" src="/Content/images/minus.png" class="req-if-export-table-collapse-all" alt="Collapse All" >' +
                    '</div>')
            );


            $("#req-if-export-table-" + reqIFExportContainerId).find('.req-if-export-table-expand-all').on('click', function () {
                var treelist = $("#req-if-export-table-" + reqIFExportContainerId).data('kendoTreeList');
                tableTreeListDatasource.schema.model.expanded = true;

                treelist.setDataSource(tableTreeListDatasource);
                document.getElementById("img-export-expand").src = "/Content/images/plus-active.png";
                document.getElementById("img-export-collapse").src = "/Content/images/minus.png";
            });


            $("#req-if-export-table-" + reqIFExportContainerId).find('.req-if-export-table-collapse-all').on('click', function () {
                var treelist = $("#req-if-export-table-" + reqIFExportContainerId).data('kendoTreeList');
                tableTreeListDatasource.schema.model.expanded = false;

                treelist.setDataSource(tableTreeListDatasource);
                document.getElementById("img-export-expand").src = "/Content/images/plus.png";
                document.getElementById("img-export-collapse").src = "/Content/images/minus-active.png";
            });




            var tooltip = $("#req-if-export-form-" + reqIFExportContainerId).kendoTooltip({
                filter: ".req-if-export-tooltip",
                width: 120,
                position: "top",
                animation: {
                    open: {
                        effects: "zoom",
                        duration: 150
                    }
                }
            }).data("kendoTooltip");


            //applyMappingXML(settings.MappingJSON);
            applyMappingJSON(settings.MappingJSON);

            if (typeof settings.onInitComplete === 'function') {
                settings.onInitComplete();
            }

            if (typeof settings.hideProgressBar === 'function') {
                settings.hideProgressBar.call(this);

            }
            return this;

        };

        return this;
    }
})(jQuery);