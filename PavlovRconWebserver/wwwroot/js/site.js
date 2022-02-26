// Write your JavaScript code.
function jsonTOHtmlPartialView(json)
{
    $.ajax({
        type: 'POST',
        url: "/Rcon/JsonToHtmlPartialView",
        data: { json: json},
        success:  function(data)
        {
            $('#modal-placeholder').html(data);
            $('#modal-placeholder > .modal').modal('show');

        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
        }
    });
}

$.fn.exists = function () {
    return this.length !== 0;
}