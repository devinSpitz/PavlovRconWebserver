
//Will be called on document ready
function init(){
    $("#searchAuthor").bind("keyup change", function (e) {
        search($("#searchAuthor").val().toLowerCase(),"data-Author");
    });
    $("#searchName").bind("keyup change", function (e) {
        search($("#searchName").val().toLowerCase(),"data-Name");
    });
};

function ChangeMap(mapId,object,gameMode,onlyChange = false)
{
    //Todo fix gameMode has to specific the if  if its only change the gametype do not remove the stuff soo may add and remove a callback?
    if($(object).parent().parent().parent().parent().parent().parent().prop("id")==="Save") // This is some how ridiculous cause .parents() gave back delete and save. I will get e bether solution xD
    {
        callApi("Save",serverId,mapId,object,gameMode,onlyChange);
    }else if($(object).parent().parent().parent().parent().parent().parent().prop("id")==="Delete"){
        callApi("Delete",serverId,mapId,object,gameMode,onlyChange);
    }
}


function SaveApiCall(method, serverId, mapId, gameMode, object,Move) {
    $.ajax({
        type: 'GET',
        url: "/SshServer/" + method + "ServerSelectedMap",
        data: {serverId: serverId, mapId: mapId, gameMode: gameMode},
        success: function (data) {
            if (method === "Save" && Move) {
                $(object).parent().parent().parent().removeClass("mapPanel")
                $("#DeletePanelBody").append($(object).parent().parent().parent());
            } else if (method === "Delete" && Move) {
                $(object).parent().parent().parent().addClass("mapPanel")
                $("#SavePanelBody").append($(object).parent().parent().parent());
            }
        },
        error: function (XMLHttpRequest, textStatus, errorThrown) {
            alert('Could not ' + method + ' Map!');
        }
    });
}

function callApi(method,serverId,mapId,object,gameMode,onlyGameMode)
{
    if(method === "Save"&&onlyGameMode)
    {
        SaveApiCall(method, serverId, mapId, gameMode, object,true);
    }else if(method === "Save"&&!onlyGameMode)
    {
        SaveApiCall(method, serverId, mapId, gameMode, object,true);
    }else if(method === "Delete"&&onlyGameMode)
    {
        SaveApiCall("Save", serverId, mapId, gameMode, object,false);
    }else if(method === "Delete"&&!onlyGameMode)
    {
        SaveApiCall(method, serverId, mapId, gameMode, object,true);
    }
    
}


// for now just copy paste
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