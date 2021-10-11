
//Will be called on document ready
function init(){

    setInterval(function() {
        UpdatePlayerList();
        UpdatePlayers();
    }, 60 * 1000); // 30 * 1000 milsec = 30 sec
 
    setValueFields(PlayerCommands,TwoValueCommands,true);

    $("#playerCommands").change(function(){
        
        setValueFields(PlayerCommands,TwoValueCommands,false);
    });    
    
    $("#twoValueCommands").change(function(){
        
        setValueFields(PlayerCommands,TwoValueCommands,false);
    });

    $("#SingleServer").change(function(){
        let servers = [];
        $("#SingleServer :selected").each(function(){
            servers.push($(this).val())
        });
        if(servers.length===1 &&typeof servers[0] !== undefined && servers[0] !== "")
        {
            var PlayerListBig = $("#PlayerListBig");
            PlayerListBig.html("");
            UpdatePlayers(servers[0]);
            UpdatePlayerList();
        }
    });
    let servers = [];
    $("#SingleServer :selected").each(function(){
        servers.push($(this).val())
    });
    if(servers.length===1 &&typeof servers[0] !== undefined && servers[0] !== "")
    {
        var PlayerListBig = $("#PlayerListBig");
        PlayerListBig.html("");
        UpdatePlayers(servers[0]);
        UpdatePlayerList();
    }

}

function setItem(id)
{
    $("#PlayerAction").find("#PlayerValue").val(id);
}

function setMap(id)
{
    var mapId = "UGC";
    if(isNaN(id))
    {
        mapId = "";
    }


    $("#TwoValueInputs").find("#PlayerValue").val(mapId+id);
}

function TwoValuesSendCommand()
{    
    let command = "";

    let playerCommand = "";
    $("#twoValueCommands :selected").each(function(){
        playerCommand = $(this).val();
    });
    command += playerCommand+" ";

    let playerValue = "";
    playerValue = $("#TwoValueInputs").find("#PlayerValue").val();
    command += playerValue+" ";

    let playerValueTwo = "";
    playerValueTwo = $("#TwoValueInputs").find("#PlayerValueTwo").val();
    command += playerValueTwo+" ";
    sendSingleCommand(command);
}



function search(searchInput,attr)
{
    $( ".mapcard" ).each(function( ) {
        if($(this).attr(attr).toLowerCase().indexOf(searchInput)>=0)
        {
            $(this).parent().show();
        }
        else{

            $(this).parent().hide();
        }

    });
}

function UpdatePlayers(server){

    if(typeof server == "undefined")
    {
        server  = $("#SingleServer").val();
    }
    var dropdown = $("#Players");
    dropdown.empty();
    //foreach Player
    $.ajax({
        type: 'POST',
        url: "/Rcon/GetAllPlayers",
        data: { serverId: server },
        success:  function(data)
        { 
            if($(data.playerList).length<=0)
            {
                dropdown.append($("<option />").val("-").text("--There are no players--"));
            }
            $(data.playerList).each(function (){
                dropdown.append($("<option />").val(this.uniqueId).text(this.username));
            });

        },
        error: function(XMLHttpRequest)
        {
            if(typeof XMLHttpRequest.status !== "undefined" && XMLHttpRequest.status===400&&typeof XMLHttpRequest.responseText !== "undefined" && XMLHttpRequest.responseText !== "")
            {
                jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest.responseText));
            }else{
                dropdown.append($("<option />").val("-").text('Could not update players!'));
            }

        }
    });


}



function UpdatePlayerList(){


    var server  = $("#SingleServer").val();

    var PlayerListBig = $("#PlayerListBig");
    PlayerListBig.html("");
    //foreach Player
    $.ajax({
        type: 'POST',
        url: "/Rcon/GetTeamList",
        data: { serverId: server },
        success:  function(data)
        {
            PlayerListBig.html(data);
        },
        error: function(XMLHttpRequest)
        {
            if(typeof XMLHttpRequest.status !== "undefined" && XMLHttpRequest.status===400&&typeof XMLHttpRequest.responseText !== "undefined" && XMLHttpRequest.responseText !== "")
            {
                jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest.responseText));
            }else{
                PlayerListBig.html("<div class'bg-danger'>Could not update players list!</div>");
            }

        }
    });


}

function PlayerAction()
{
    let command = "";

    let playersSelected = "";
    $("#Players :selected").each(function(){
        playersSelected = $(this).val();
    });
    let playerCommand = "";
    $("#playerCommands :selected").each(function(){
        playerCommand = $(this).val();
    });
    
    if(playerCommand === "SetLimitedAmmoType" || playerCommand === "ItemList")
    {
        command += playerCommand+" ";
    }else{

        command += playerCommand+" %Player% ";
    }
    
    

    
    let playerValue = "";
    playerValue = $("#PlayerAction").find("#PlayerValue").val();

    let withInput = false;
    $(PlayerCommands).each(function(){
        if(this.Name === playerCommand)
        {
            withInput = this.InputValue;
        }
    });

    if(withInput)
    {
        command += playerValue;
    }

    if(playerCommand === "Ban" && playersSelected === "-")
    {
        alert("You have to choose a player!");
        return false;
    }
    else if(playerCommand === "Ban" && playersSelected !== "-")
    {
        AddBanPlayer(playersSelected,playerValue); // also makes the command
        return; 
    }
    var tmpCommand = command.replace("%Player%",playersSelected);
    sendSingleCommand(tmpCommand);

}

function sendSingleCommand(command)
{
    $(".overlay").show();
    let data = {};
    let servers = [];
    
    $("#SingleServer :selected").each(function(){
        servers.push($(this).val())
    });

    let controller = "Rcon";
    data =  { server: servers[0], command: command };
    $.ajax({
        type: 'POST',
        url: "/"+controller+"/sendCommand",
        data: data,
        success:  function(result)
        {
            if(result.toString()==="")
            {
                alert("Did nothing!");
            }
            else{
                if(command==="ServerInfo")
                {
                    SingleServerInfoPartialView(result,servers[0]);
                }
                else {
                    jsonTOHtmlPartialView(result.toString());
                }
                $(".overlay").hide();
            }
        },
        error: function(XMLHttpRequest)
        {
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
            $(".overlay").hide();
        }
    });
   

}

function SingleServerInfoPartialView(result,ServerIds)
{
    let controller = "Rcon";
    let data = {};
    data = { server: result ,serverId: ServerIds};

    $.ajax({
        type: 'POST',
        url: "/"+controller+"/SingleServerInfoPartialView",
        data: data,
        success:  function(data)
        {
            $('#modal-placeholder').html(data);
            $('#modal-placeholder > .modal').modal('show');
            
        },
        error: function(XMLHttpRequest)
        {
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
        }
    });
}


function RconChooseItemPartialView()
{

    $.ajax({
        type: 'POST',
        url: "/Rcon/RconChooseItemPartialView",
        success:  function(data)
        {
            $('#modal-placeholder').html(data);
            $('#modal-placeholder > .modal').modal('show');
        },
        error: function(XMLHttpRequest)
        {
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
        }
    });
}

function RconChooseMapPartialView()
{
    var data = {};
    let servers = [];
    $("#SingleServer :selected").each(function(){
        servers.push($(this).val())
    });

    data = { serverId: servers[0]};
    $(".overlay").show();
    $.ajax({
        type: 'POST',
        url: "/Rcon/PavlovChooseMapPartialView",
        data : data,
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

function BanMenu()
{
    $(".overlay").show();
    let servers = [];
    $("#SingleServer :selected").each(function(){
        servers.push($(this).val())
    });
    $.ajax({
        type: 'POST',
        url: "/Rcon/GetBansFromServers",
        data: { serverId: servers[0]},
        success:  function(data)
        {

            $(".overlay").hide();
            $('#modal-placeholder').html(data);
            $('#modal-placeholder > .modal').modal('show');

        },
        error: function(XMLHttpRequest)
        {

            $(".overlay").hide();
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
        }
    });
}


// There is AddBanPlayer and RemoveBanPlayer
function RemoveBannedPlayer(steamId)
{
    if(!confirm("Are you willing to unban "+steamId)) return;
    $(".overlay").show();
    let data = {};
    let servers = [];
    $("#SingleServer :selected").each(function(){
        servers.push($(this).val())
    });
    $.ajax({
        type: 'POST',
        url: "/Rcon/RemoveBanPlayer",
        data: {serverId: servers[0],steamId: steamId},
        success:  function(result)
        {
            if(result.toString()==="")
            {
                alert("Did nothing!");
            }
            else{
                alert(result.toString());
            }
            $(".overlay").hide();
        },
        error: function(XMLHttpRequest)
        {
            alert(JSON.stringify(XMLHttpRequest));
            $(".overlay").hide();
        }
    });
}

function AddBanPlayer(steamId,timespan)
{
    $("#SingleServer :selected").each(function(){
        servers.push($(this).val())
    });
    $.ajax({
        type: 'POST',
        url: "/Rcon/AddBanPlayer",
        data: {serverId: servers[0],steamId: steamId,timeSpan: timespan},
        success:  function(result)
        {
            if(result.toString()==="")
            {
                alert("Did nothing!");
            }
            else{
                jsonTOHtmlPartialView(result.toString());
            }
        },
        error: function(XMLHttpRequest)
        {
            alert(JSON.stringify(XMLHttpRequest));
        }
    });
}




function ValueFieldPartialView(playerCommands,twoValueCommands,atualCommandName,isNormalCommand,firstValue,callbackPositive)
{
    
    $.ajax({
        type: 'POST',
        url: "/Rcon/ValueFieldPartialView",
        data: {playerCommands: playerCommands, twoValueCommands: twoValueCommands, atualCommandName: atualCommandName, isNormalCommand: isNormalCommand,firstValue: firstValue },
        success:  function(data)
        {
            
            callbackPositive(data);
        },
        error: function(XMLHttpRequest)
        {
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
        }
    });
}

function setValueFields(playerCommands,twoValueCommands,documentReady = false)
{
    
    // PlayerValue
    // Make Object
    // get actual Command = 
    if (!documentReady) {
        $("#PlayerAction").find("#PlayerValueParent").find("input").remove();
        $("#PlayerAction").find("#PlayerValueParent").find("select").remove();
        $("#PlayerAction").find("#PlayerValueParent").find("a").remove();
        $("#PlayerAction").find("#PlayerValueParent").find(".valueFieldButtons").remove();
    }
    ValueFieldPartialView(playerCommands, twoValueCommands, $("#playerCommands :selected").val(), true, true, function (data) {
        $("#PlayerAction").find("#PlayerValueParent").append(data);
    })
    // ActionsWithTwovalues
    // Make Object
    if (!documentReady) {
        $("#TwoValueInputs").find("#PlayerValueParent").find("input").remove();
        $("#TwoValueInputs").find("#PlayerValueParent").find("select").remove();
        $("#TwoValueInputs").find("#PlayerValueParent").find("a").remove();
        $("#PlayerAction").find("#PlayerValueParent").find(".valueFieldButtons").remove();
    }
    ValueFieldPartialView(playerCommands, twoValueCommands, $("#twoValueCommands :selected").val(), false, true, function (data) {
        $("#TwoValueInputs").find("#PlayerValueParent").append(data);
    })
    // ActionsWithTwovalues
    // Make Object
    if(!documentReady)
    {
        $("#TwoValueInputs").find("#PlayerValueTwoParent").find("input").remove();
        $("#TwoValueInputs").find("#PlayerValueTwoParent").find("select").remove();
        $("#TwoValueInputs").find("#PlayerValueTwoParent").find("a").remove();
        $("#TwoValueInputs").find(".valueFieldButtons").remove();
    }
    ValueFieldPartialView(playerCommands,twoValueCommands,$("#twoValueCommands :selected").val(),false,false,function(data){
        $("#TwoValueInputs").find("#PlayerValueTwoParent").append(data);
    })
    
    
}