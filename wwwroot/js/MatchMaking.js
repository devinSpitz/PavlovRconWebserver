
//Will be called on document ready
function init(){

    $("#GameMode").change(function(){
        GetPlayerTeamSelectPartialView($(this).val(),
            function (data) {
                $("#GameModeSpecificPanel").append(data);
            });
    });


};


function RconChooseMapPartialView()
{
    $(".overlay").show();
    $.ajax({
        type: 'POST',
        url: "/Rcon/PavlovChooseMapPartialView",
        success:  function(data)
        {
            $('#modal-placeholder').html(data);
            $('#modal-placeholder > .modal').modal('show');

            $(".overlay").hide();
            $("#searchAuthor").bind("keyup change", function (e) {
                search($("#searchAuthor").val().toLowerCase(),"data-Author");
            });
            $("#searchName").bind("keyup change", function (e) {
                search($("#searchName").val().toLowerCase(),"data-Name");
            });
        },
        error: function(XMLHttpRequest)
        {
            $(".overlay").hide();
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
        }
    });
}

function setMap(id)
{
    var mapId = "UGC";
    if(isNaN(id))
    {
        mapId = "";
    }
    $("#MapId").val(mapId+id);
}



function GetPlayerTeamSelectPartialView(gameMode,callbackPositive)
{

    $.ajax({
        type: 'POST',
        url: "/MatchMaking/PartialViewPerGameMode/",
        data: {gameMode: gameMode},
        success:  function(data)
        {
            callbackPositive(data);
        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
        }
    });
}