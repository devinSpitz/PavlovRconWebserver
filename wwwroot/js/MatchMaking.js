
//Will be called on document ready
function init(){

    $("#GameMode").change(function(){
        GetPlayerTeamSelectPartialView($(this).val(),
            function (data) {
                $("#GameModeSpecificPanel").html(data);
            });
    });

    GetPlayerTeamSelectPartialView($("#GameMode").val(),
        function (data) {
            $("#GameModeSpecificPanel").html(data);
        });
};

function bindSelectButtons()
{
    //No Teams button
    $("#SelectNoTeamsSteamIdentities").click(function(e)
    {
        e.preventDefault();
        SelectSteamIdentities("#AllSteamIdentities","#SelectedSteamIdentities");
    });

    $("#selectButtonTeam0").click(function(e)
    {
        e.preventDefault();
        SelectSteamIdentities("#SelectIdTeam0","#SelectedIdTeam0");
    });

    $("#selectButtonTeam1").click(function(e)
    {
        e.preventDefault();
        SelectSteamIdentities("#SelectIdTeam1","#SelectedIdTeam1");
    });
    //DropDowns
    let team0Dropdown = $("#dropDownTeam0");
    $(team0Dropdown).change(function()
    {
        PopulateTeamsSteamIdentities($(this).val(),"#SelectIdTeam0","#SelectedIdTeam0");
    });
    PopulateTeamsSteamIdentities($(team0Dropdown).val(),"#SelectIdTeam0","#SelectedIdTeam0");
    let team1Dropdown = $("#dropDownTeam1");
    $(team1Dropdown).change(function()
    {
        PopulateTeamsSteamIdentities($(team1Dropdown).val(),"#SelectIdTeam1","#SelectedIdTeam1")
    });
    PopulateTeamsSteamIdentities($(team1Dropdown).val(),"#SelectIdTeam1","#SelectedIdTeam1")
}

function PopulateTeamsSteamIdentities(chosenTeam,toId,toremove)
{
    $(toremove).empty();
    let dropdown = $(toId);
    //foreach Player
    $.ajax({
        type: 'POST',
        url: "/MatchMaking/GetAvailableSteamIdentities",
        data: { teamId: chosenTeam },
        success:  function(data)
        {

            if($(data).length<=0)
            {
                dropdown.append($("<option />").val("-").text("--There are no players--"));
            }
            $(data).each(function (){
                dropdown.append($("<option />").val(this.steamIdentity.id).text(this.steamIdentity.name));
            });

        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            if(typeof XMLHttpRequest.status !== "undefined" && XMLHttpRequest.status===400&&typeof XMLHttpRequest.responseText !== "undefined" && XMLHttpRequest.responseText !== "")
            {
                jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest.responseText));
            }else{
                alert('Could not update players!');
            }

        }
    });
    
    
}

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
        url: "/MatchMaking/PartialViewPerGameModeWithId/",
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

function SelectSteamIdentities(toId,fromId)
{
    var dropdown = $(fromId);
    $(toId+" :selected").each(function(){
        dropdown.append($("<option />").val($(this).val()).text($(this).text()));
        $(this).remove();
    })
    
}

function SaveMatch()
{
    debugger;
    match.Name = $("#Name").val();
    

    debugger;
    if($("#SelectedIdTeam0").length) // Teams
    {
        
    }else{
        
    }
    
    
    
}

