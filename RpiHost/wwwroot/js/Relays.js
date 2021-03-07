(function ($) {
    'use strict'

    var token = $('meta[name="api-token"]').attr('content');
    var apiUrls = {
        get: $('#api-get-devices').val(),
        activate: $('#api-activate-device').val(),
    }

    var buttonsTemplate = $('#buttonTemplate').html();
    Mustache.parse(buttonsTemplate);   // optional, speeds up future uses
    var alertTemplate = $('#alertTemplate').html();
    Mustache.parse(alertTemplate);   // optional, speeds up future uses

    $("button[data-action='refresh']").click(function (e) {
        e.stopPropagation();
        generateDeviceList();
    })

    function setupButtonHandler() {
        $("#doorList button[data-activateRelay]").on("click", function () {
            var $btn = $(this)
            var deviceId = $btn.attr('data-activateRelay');
            $btn.addClass("btn-warning disabled");
            $.ajax
                ({
                    type: "POST",
                    url: apiUrls.activate,
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    data: JSON.stringify({
                        id: deviceId
                    }),
                    beforeSend: function (xhr) {
                        xhr.setRequestHeader("Authorization", "Bearer " + token);
                    },
                    success: function () {
                        var tile = getInfoTile("Device with id " + deviceId + " activated.")
                        $("#doorList").append(tile);
                        $btn.removeClass("btn-warning disabled");
                    },
                    error: function (xhr, ajaxOptions, thrownError) {
                        var tile = getErrorTile("Failed to activate device with id " + deviceId + ". Status: " + xhr.status);
                        $("#doorList").append(tile);
                        $btn.removeClass("btn-warning disabled");
                    }
                });
        });
    }

    function getErrorTile(message) {
        return Mustache.render(alertTemplate, { message: message, header: 'Error!', type: 'danger', icon: 'ban' });
    }
    function getInfoTile(message) {
        return Mustache.render(alertTemplate, { message: message, header: 'Info', type: 'info', icon: 'info' });
    }

    function generateDeviceList() {
        $("#doorList").html("Loading list of devices...");
        $.ajax
            ({
                type: "GET",
                url: apiUrls.get,
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("Authorization", "Bearer " + token);
                },
                success: function (data) {
                    var rendered = Mustache.render(buttonsTemplate, { devices: data });
                    $('#doorList').html(rendered);
                    setupButtonHandler();
                },
                error: function (xhr, ajaxOptions, thrownError) {
                    var errorTile = getErrorTile("An error occured while getting the list. Status: " + xhr.status);
                    $("#doorList").html(errorTile);
                }
            });
    }

    generateDeviceList();
    // Hide alerts handler
    $('#doorList').on('click', 'button[data-dismiss="alert"]', function () {
        $(this).parent().remove()
    });
})(jQuery)