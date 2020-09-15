
//Will be called on document ready
function init(){
    
    setValueFields(PlayerCommands,TwoValueCommands,true);

    if(!MultiRcon)
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
        if(servers.length===1 &&typeof servers[0] !== undefined && servers[0] !== "" && !MultiRcon)
        {
            UpdatePlayers(servers[0]);
        }
    });
    let servers = [];
    $("#SingleServer :selected").each(function(){
        servers.push($(this).val())
    });
    if(servers.length===1 &&typeof servers[0] !== undefined && servers[0] !== "" && !MultiRcon)
    {
        UpdatePlayers(servers[0]);
    }

};

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
    $( ".mapPanel" ).each(function( index ) {
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

    $(".overlay").show();
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
            $(data).each(function (){
                if($(this.playerList).length<=0)
                {
                    dropdown.append($("<option />").val("-").text("--There are no players--"));
                }
                $(this.playerList).each(function (){
                    dropdown.append($("<option />").val(this.uniqueId).text(this.username));
                });
            });
            $(".overlay").hide();
        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            if(typeof XMLHttpRequest.status !== "undefined" && XMLHttpRequest.status===400&&typeof XMLHttpRequest.responseText !== "undefined" && XMLHttpRequest.responseText !== "")
            {
                jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest.responseText));
            }else{
                alert('Could not update players!');
            }

            $(".overlay").hide();
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
    $("#playerCommands :selected").each(function(){
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
    $(".overlay").show();
    let data = {};
    let servers = [];
    $("#SingleServer :selected").each(function(){
        servers.push($(this).val())
    });
    let controller = "Rcon";
    if(MultiRcon) {
        controller = "MultiRcon";
        data =  { servers: servers, command: command };
    }else{
        data =  { server: servers[0], command: command };
    }
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
                    debugger;
                    if(MultiRcon) {
                        SingleServerInfoPartialView(result,servers);
                    }else{
                        SingleServerInfoPartialView(result,servers[0]);
                    }
                }
                else {
                    if(MultiRcon)
                    {
                        jsonTOHtmlPartialView(result.toString());
                    }else{
                        jsonTOHtmlPartialView(result.toString());
                    }
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
    if(MultiRcon) {
        controller = "MultiRcon";
        data = { servers: result ,serverIds: ServerIds};
    }else{
        data = { server: result ,serverId: ServerIds[0]};
    }

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
//Todo: merge all that modal functions

function RconChooseMapPartialView()
{
    var data = {};
    let servers = [];
    $("#SingleServer :selected").each(function(){
        servers.push($(this).val())
    });

    if(!MultiRcon) {
        data = { serverId: servers[0]};
    }
    $(".overlay").show();
    $.ajax({
        type: 'POST',
        url: "/Rcon/RconChooseMapPartialView",
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
            jsonTOHtmlPartialView(JSON.stringify(XMLHttpRequest))
        }
    });
}

function setValueFields(playerCommands,twoValueCommands,documentReady = false)
{
    
    if(!MultiRcon) {
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

    }
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