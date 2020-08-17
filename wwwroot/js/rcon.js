
//Will be called on document ready
function init(){

    setValueFields(PlayerCommands,TwoValueCommands,true);
    
    $("#PlayerCommands").change(function(){
        setValueFields(PlayerCommands,TwoValueCommands,false);
    });    
    
    $("#TwoValueCommands").change(function(){
        setValueFields(PlayerCommands,TwoValueCommands,false);
    });
    
    $("#RconServer").change(function(){
        let servers = [];
        $("#RconServer :selected").each(function(){
            servers.push($(this).val())
        });

        if(servers.length===1)
            UpdatePlayers(servers);
    });

};

function TwoValuesSendCommand()
{
    let servers = [];
    $("#RconServer :selected").each(function(){
        servers.push($(this).val())
    });

    let command = "";

    let playerCommand = "";
    $("#TwoValueCommands :selected").each(function(){
        playerCommand = $(this).val();
    });
    command += playerCommand+"";

    let playerValue = "";
    playerValue = $("#TwoValueInputs").find("#PlayerValue").val();
    command += playerValue+"";

    let playerValueTwo = "";
    playerValueTwo = $("#TwoValueInputs").find("#PlayerValueTwo").val();
    command += playerValueTwo+"";

    sendSingleCommand(tmpCommand);
}

function UpdatePlayers(servers = null){
    if(servers === null)
    {
        servers = [];
        $("#RconServer :selected").each(function(){
            servers.push($(this).val())
        });
    }
    if(servers.length!==1) return;
    var dropdown = $("#Players");
    dropdown.empty();
    //foreach Player
    $.ajax({
        type: 'POST',
        url: "/Rcon/GetAllPlayers",
        data: { serverId: servers[0] },
        success:  function(data)
        {   //TODO: Check if Commands Work
            $(data).each(function (){
                $(this.playerList).each(function (){
                    dropdown.append($("<option />").val(this.uniqueId).text(this.username));
                });
            });
        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            alert('Could not update players!');
        }
    });


}

function PlayerAction()
{
    let command = "";

    let playersSelected = [];
    $("#Players :selected").each(function(){
        playersSelected.push($(this).val())
    });
    let playerCommand = "";
    $("#PlayerCommands :selected").each(function(){
        playerCommand = $(this).val();
    });
    command += playerCommand+" %Player% ";

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
    if(playersSelected.length > 1)
    {
        $(playersSelected).each(function(){
            var tmpCommand = command.replace("%Player%",$(this).val());
            sendSingleCommand(tmpCommand);
        }); 
    }
    else {
        var tmpCommand = command.replace("%Player%",playersSelected);
        sendSingleCommand(tmpCommand);
    }

}
function sendSingleCommand(command)
{
    let servers = [];
    $("#RconServer :selected").each(function(){
        servers.push($(this).val())
    });

    $.ajax({
        type: 'POST',
        url: "/Rcon/sendCommand",
        data: { servers: servers, command: command },
        success:  function(result)
        {
            if(result.toString()==="")
            {
                if(servers.length<=0)
                {
                    alert("Please select at least one server to send to command!");
                }else{
                    alert("Did nothing!");
                }
            }
            else{
                if(command==="ServerInfo")
                {
                    RconServerInfoPartialView(result,servers);
                }
                else{
                    jsonTOHtmlPartialView(result.toString())
                }
            }
        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            if(typeof XMLHttpRequest.status !== "undefined" && XMLHttpRequest.status === 500)
            {
                console.log(XMLHttpRequest.responseText);
                alert("Command failed. To see more logs go to console.");
            }
            else if(typeof XMLHttpRequest.responseText !== "undefined" && XMLHttpRequest.responseText!=="") {
                alert(XMLHttpRequest.responseText);
            }
            else{
                console.log(XMLHttpRequest.responseText);
                alert("Unknown error. To see more logs go to console.");
            }
            
        }
    });

}

function RconServerInfoPartialView(result,ServerIds)
{

    $.ajax({
        type: 'POST',
        url: "/Rcon/RconServerInfoPartialView",
        data: { servers: result ,serverIds: ServerIds},
        success:  function(data)
        {
            $('#modal-placeholder').html(data);
            $('#modal-placeholder > .modal').modal('show');
        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            alert('Could not get ServerInfoParialView!');
        }
    });
}
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
            alert('Could not get ServerInfoParialView!');
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
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            alert('Could not get ValueFieldPartialView!');
        }
    });
}

function setValueFields(playerCommands,twoValueCommands,documentReady = false)
{
    // PlayerValue
    // Make Object
    // get actual Command = 
    if(!documentReady)
    {
        $("#PlayerAction").find("#PlayerValueParent").find("input").remove();
        $("#PlayerAction").find("#PlayerValueParent").find("select").remove();
        $("#PlayerAction").find("#PlayerValueParent").find("a").remove();
    }
    ValueFieldPartialView(playerCommands,twoValueCommands,$("#PlayerCommands :selected").val(),true,true,function(data){
        $("#PlayerAction").find("#PlayerValueParent").append(data);
    })
    
    // ActionsWithTwovalues
    // Make Object
    if(!documentReady)
    {
        $("#TwoValueInputs").find("#PlayerValueParent").find("input").remove();
        $("#TwoValueInputs").find("#PlayerValueParent").find("select").remove();
        $("#TwoValueInputs").find("#PlayerValueParent").find("a").remove();
    }
    ValueFieldPartialView(playerCommands,twoValueCommands,$("#TwoValueCommands :selected").val(),false,true,function(data){
        $("#TwoValueInputs").find("#PlayerValueParent").append(data);
    })
    
    // ActionsWithTwovalues
    // Make Object
    if(!documentReady)
    {
        $("#TwoValueInputs").find("#PlayerValueTwoParent").find("input").remove();
        $("#TwoValueInputs").find("#PlayerValueTwoParent").find("select").remove();
        $("#TwoValueInputs").find("#PlayerValueTwoParent").find("a").remove();
    }
    ValueFieldPartialView(playerCommands,twoValueCommands,$("#TwoValueCommands :selected").val(),false,false,function(data){
        $("#TwoValueInputs").find("#PlayerValueTwoParent").append(data);
    })
    
    
}