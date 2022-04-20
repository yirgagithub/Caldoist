(function ($) {
    function Index() {
        var $this = this;
        function initialize() {

            $(".popup").on('click', function (e) {
                modelPopup(this);
            });

            $("#modal-create-edit-integration").on('click', '#btnSubmit', function (e) {
                e.preventDefault();
                var calendarId = $('#calendarId').val();
                var calendarName = $('#calendarId option:selected').text();
                var todoistItemId = $('#todoistItemId').val().toString();
                var todoistItemName = $('#todoistItemId option:selected').text();
                var isFilter = $('#allItems').attr('checked') == 'checked' ? true : false;
                var completeOptions = ''
                let colorChange = ''
                let moveCalendarId = ''

                if ($('#delete').attr('checked') == 'checked') {
                    completeOptions = '1'
                } else if ($('#keep').attr('checked') == 'checked') {
                    completeOptions = '2'
                } else if ($('#changeColor').attr('checked') == 'checked') {
                    completeOptions = '3'
                    colorChange = $('#colorChange').val()
                } else if ($('#move').attr('checked') == 'checked') {
                    completeOptions = '4'
                    moveCalendarId = $('#moveCalendarId').val();
                }

                var assignedFilter = ''
                if ($('#notAssigned').attr('checked') == 'checked') {
                    assignedFilter = '1'
                } else if ($('#assignedMe').attr('checked') == 'checked') {
                    assignedFilter = '2'
                } else if ($('#assigned').attr('checked') == 'checked') {
                    assignedFilter = '3'
                }

                var labelFilter = $('#label').attr('checked') == 'checked' ? true : false;
                var priorityFilter = $('#priority').attr('checked') == 'checked' ? true : false;
                var newLabel = $('#newLabel').val();
                var duration = $('#duration').val();

                var labelIds = ''
                if (labelFilter == true) {
                    labelIds = $('#labelIds').val().toString();
                } else {
                    var options = $('#labelIds option')
                    var labelEx = $('#labelIds').val();
                    for (var i = 0; i < options.length; i++) {
                        if (labelEx.length > 0 && !labelEx.includes(options[i].value)) {
                            labelIds = labelIds == '' ? options[i].value : labelIds + ',' + options[i].value
                        }
                    }
                }

                var priorityIds = ''
                if (priorityFilter == true) {
                    priorityIds = $('#priorityIds').val().toString();
                } else {
                    var options = $('#priorityIds option')
                    var priorityEx = $('#priorityIds').val();
                    for (var i = 0; i < options.length; i++) {
                        if (priorityEx.length > 0 && !priorityEx.includes(options[i].value)) {
                            priorityIds = priorityIds == '' ? options[i].value : priorityIds + ',' + options[i].value
                        }
                    }
                }

                var operationType = $('#assignTask').attr('checked') == 'checked' ? 'Assign' : 'UnAssign'
                var formValid = true;

                if (calendarId == '0') {
                    formValid = false;
                    $('#calendarErr').show();
                }

                if (todoistItemId == '') {
                    formValid = false;
                    $('#projectErr').show()
                }

                if (operationType == 'Assign') {
                    if (newLabel == '') {
                        formValid = false;
                        $('#labelErr').show()
                    }

                    if (duration == '') {
                        formValid = false
                        $('#durationErr').show()
                    }
                }

                if (completeOptions == '3') {
                    if (colorChange == '') {
                        formValid = false
                        $('#colorErr').show()
                    }
                }

                if (completeOptions == '4') {
                    if (moveCalendarId == '0') {
                        formValid = false;
                        $('#moveCalendarErr').show()
                    }
                }

                let formData = {
                    id: 0, operationType: operationType,
                    googleCalendarId: calendarId, calendarName: calendarName, todoistItemId: todoistItemId,
                    todoistItemName: todoistItemName, isFilter: isFilter, assignedFilter: assignedFilter, labelFilter: labelFilter, labelIds: labelIds, priorityFilter: priorityFilter,
                    priorityIds: priorityIds, newLabel: newLabel, duration: duration, email: '', completedOptions: completeOptions, colorChange: colorChange, moveCalendarId: moveCalendarId
                }


                if (formValid == true) {

                    let url = e.target.getAttribute('data-url')
                    let location = window.location.href
                    let index = location.indexOf('Home')
                    url = location.substring(0, index) + url

                    $.post(url, formData).done(function (res) {
                        if (res) {
                            $('#modal-create-edit-integration.modal').modal("toggle");
                            console.log('successfully saved')
                        }
                    })
                }
            })

            $("#modal-create-edit-integration").on('click', '#assignTask', function (e) {
                $('#assignTask').attr('checked', 'checked');
                $('#unassignTask').removeAttr('checked')
                $('#groupByAssign').show()
            })

            $("#modal-create-edit-integration").on('click', '#unassignTask', function (e) {
                $('#unassignTask').attr('checked', 'checked');
                $('#assignTask').removeAttr('checked')
                $('#groupByAssign').hide()
            })

            $("#modal-create-edit-integration").on('click', '#allItems', function (e) {
                $('#allItems').attr('checked', 'checked');
                $('#isFilter').removeAttr('checked')
                $('#filterGroup').hide()
            })

            $("#modal-create-edit-integration").on('click', '#isFilter', function (e) {
                $('#isFilter').attr('checked', 'checked');
                $('#allItems').removeAttr('checked')
                $('#filterGroup').show()
            })

            $("#modal-create-edit-integration").on('click', '#notAssigned', function (e) {
                $('#notAssigned').attr('checked', 'checked');
                $('#assigned').removeAttr('checked')
                $('#assignedMe').removeAttr('checked');
            })

            $("#modal-create-edit-integration").on('click', '#assigned', function (e) {
                $('#assigned').attr('checked', 'checked');
                $('#notAssigned').removeAttr('checked');
                $('#assignedMe').removeAttr('checked');
            })
            $("#modal-create-edit-integration").on('click', '#assignedMe', function (e) {
                $('#assignedMe').attr('checked','')
                $('#assigned').removeAttr('checked');
                $('#notAssigned').removeAttr('checked');
            })

            $("#modal-create-edit-integration").on('click', '#labelexcept', function (e) {
                $('#labelexcept').attr('checked', 'checked');
                $('#label').removeAttr('checked')
            })

            $("#modal-create-edit-integration").on('click', '#delete', function (e) {
                $('#delete').attr('checked', 'checked');
                $('#keep').removeAttr('checked')
                $('#changeColor').removeAttr('checked');
                $('#move').removeAttr('checked');
                $('#colorChangeDiv').hide()
                $('#moveCalendarDiv').hide()
            })

            $("#modal-create-edit-integration").on('click', '#keep', function (e) {
                $('#delete').removeAttr('checked')
                $('#keep').attr('checked', 'checked');
                $('#changeColor').removeAttr('checked');
                $('#move').removeAttr('checked');
                $('#colorChangeDiv').hide()
                $('#moveCalendarDiv').hide()
            })
            $("#modal-create-edit-integration").on('click', '#changeColor', function (e) {
                $('#delete').removeAttr('checked');
                $('#keep').removeAttr('checked')
                $('#changeColor').attr('checked', 'checked');
                $('#move').removeAttr('checked');
                $('#colorChangeDiv').show()
                $('#moveCalendarDiv').hide()
            })

            $("#modal-create-edit-integration").on('click', '#move', function (e) {
                $('#delete').removeAttr('checked');
                $('#keep').removeAttr('checked')
                $('#changeColor').removeAttr('checked');
                $('#move').attr('checked', 'checked');
                $('#colorChangeDiv').hide()
                $('#moveCalendarDiv').show()
            })
            $("#modal-create-edit-integration").on('click', '#label', function (e) {
                $('#label').attr('checked', 'checked');
                $('#labelexcept').removeAttr('checked');
            })

            $("#modal-create-edit-integration").on('click', '#priorityexcept', function (e) {
                $('#priorityexcept').attr('checked', 'checked');
                $('#priority').removeAttr('checked')
            })

            $("#modal-create-edit-integration").on('click', '#priority', function (e) {
                $('#priority').attr('checked', 'checked');
                $('#priorityexcept').removeAttr('checked');
            })


            function modelPopup(reff) {
                let url = $(reff).data('url');
                let location = window.location.href
                let index = location.indexOf('Home')
                url = location.substring(0, index) + url
                $.get(url).done(function (res) {
                    debugger;
                    $('#modal-create-edit-integration').find(".modal-dialog").html(res);
                    $('#modal-create-edit-integration.modal').modal("show");

                    $('#assignTask').attr('checked', 'checked');
                    $('#allItems').attr('checked', 'checked');
                    $('#delete').attr('checked', 'checked');

                    $('#todoistItemId').multiselect({
                        includeSelectAllOption: true,
                    });
                    $('#labelIds').multiselect({
                        includeSelectAllOption: true,
                    });
                    $('#priorityIds').multiselect({
                        includeSelectAllOption: true,
                    });
                   
                });

            }
        }

        $this.init = function () {
            initialize();
        };
    }
    $(function () {
        var self = new Index();
        self.init();
    });
}(jQuery));