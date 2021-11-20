
//Will be called on document ready
function init(){

    $("#GameMode").change(function(){
        GetPlayerTeamSelectPartialView($(this).val(),
            function (data) {
                $("#GameModeSpecificcard").html(data);
            });
    });

    $("#RconChooseMapPartialView").click(function(e){
        e.preventDefault();
        RconChooseMapPartialView()
    });

    GetPlayerTeamSelectPartialView($("#GameMode").val(),
        function (data) {
            $("#GameModeSpecificcard").html(data);
        });
}

function bindSelectButtons()
{
    //No Teams button
    $("#SelectNoTeamsSteamIdentities").click(function(e)
    {
        e.preventDefault();
        SelectSteamIdentities("#AllSteamIdentities","#MatchSelectedSteamIdentitiesStrings");
    });

    $("#selectButtonTeam0").click(function(e)
    {
        e.preventDefault();
        SelectSteamIdentities("#SelectIdTeam0","#MatchTeam0SelectedSteamIdentitiesStrings");
    });

    $("#selectButtonTeam1").click(function(e)
    {
        e.preventDefault();
        SelectSteamIdentities("#SelectIdTeam1","#MatchTeam1SelectedSteamIdentitiesStrings");
    });
    //DropDowns
    let team0Dropdown = $("#Team0Id");
    $(team0Dropdown).change(function()
    {
        PopulateTeamsSteamIdentities($(this).val(),"#SelectIdTeam0","#MatchTeam0SelectedSteamIdentitiesStrings");
    });
    PopulateTeamsSteamIdentities($(team0Dropdown).val(),"#SelectIdTeam0","#MatchTeam0SelectedSteamIdentitiesStrings",true);
    let team1Dropdown = $("#Team1Id");
    $(team1Dropdown).change(function()
    {
        PopulateTeamsSteamIdentities($(team1Dropdown).val(),"#SelectIdTeam1","#MatchTeam1SelectedSteamIdentitiesStrings")
    });
    PopulateTeamsSteamIdentities($(team1Dropdown).val(),"#SelectIdTeam1","#MatchTeam1SelectedSteamIdentitiesStrings",true)
}

function PopulateTeamsSteamIdentities(chosenTeam,toId,toremove,first=false)
{
    let matchId = $("#Id").val();
    if(!first)
        $(toremove).empty();
    $(toId).empty();
    let dropdown = $(toId);
    //foreach Player
    $.ajax({
        type: 'POST',
        url: subPath+"MatchMaking/GetAvailableSteamIdentities",
        data: { teamId: chosenTeam,matchId:matchId,shack: shack },
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
        url: subPath+"Rcon/PavlovChooseMapPartialView",
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

function search(searchInput,attr)
{
    $( ".mapcard" ).each(function( index ) {
        if($(this).attr(attr).toLowerCase().indexOf(searchInput)>=0)
        {
            $(this).parent().show();
        }
        else{

            $(this).parent().hide();
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
    let matchId = $("#Id").val();
    $.ajax({
        type: 'POST',
        url: subPath+"MatchMaking/PartialViewPerGameModeWithId/",
        data: {gameMode: gameMode,matchId: matchId, shack: shack},
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
    match.Name = $("#Name").val();
    match.Id = $("#Id").val();
    match.PavlovServerId = $("#PavlovServerId").val();
    match.MapId = $("#MapId").val();
    match.GameMode = $("#GameMode").val();
    match.TimeLimit = $("#TimeLimit").val();
    match.PlayerSlots = $("#PlayerSlots").val();
    match.Team0Id = $("#Team0Id").val();
    match.Team1Id = $("#Team1Id").val();
    if($("#MatchTeam0SelectedSteamIdentitiesStrings").length) // Teams
    {
        $("#MatchTeam0SelectedSteamIdentitiesStrings option").each(function ()
        {
            match.MatchTeam0SelectedSteamIdentitiesStrings.push($(this).val());
        });
        $("#MatchTeam1SelectedSteamIdentitiesStrings option").each(function ()
        {
            match.MatchTeam1SelectedSteamIdentitiesStrings.push($(this).val());
        });
    }
    else{//no Teams or all in the same team
        $("#MatchSelectedSteamIdentitiesStrings option").each(function ()
        {
            match.MatchSelectedSteamIdentitiesStrings.push($(this).val());
        });
    }

    $.ajax({
        type: 'POST',
        url: subPath+"MatchMaking/SaveMatch/",
        data: {match: match},
        success:  function(data)
        {
          
            window.location = subPath+"MatchMaking/";
        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
          
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
        }
    });
    
    
    
}

