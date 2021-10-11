
//Will be called on document ready
function init(){
    $("#searchAuthor").bind("keyup change", function (e) {
        search($("#searchAuthor").val().toLowerCase(),"data-Author");
    });
    $("#searchName").bind("keyup change", function (e) {
        search($("#searchName").val().toLowerCase(),"data-Name");
    });
}

function duplicateButton(object)
{
  
    let clone = $(object).parent().parent().parent().clone();
    $(object).parent().parent().parent().parent().prepend(clone);
    
}


function ChangeMap(mapId,object,gameMode,onlyChange = false)
{
    if($(object).parent().parent().parent().parent().parent().parent().parent().prop("id")==="Save") // This is some how ridiculous cause .parents() gave back delete and save. I will get e bether solution xD
    {
        callApi("Save",serverId,mapId,object,gameMode,onlyChange);
    }else if($(object).parent().parent().parent().parent().parent().parent().parent().prop("id")==="Delete"){
        callApi("Delete",serverId,mapId,object,gameMode,onlyChange);
    }
}


function SaveApiCall(method, serverId, mapId, gameMode, object,Move) {
    $.ajax({
        type: 'GET',
        url: "/SshServer/" + method + "ServerSelectedMap",
        data: {serverId: serverId, mapId: mapId, gameMode: gameMode},
        success: function (data) {
            if(data===true)
            {
                if (method === "Save" && Move) {

                    $("#DeletecardBodyAppend").append($(object).parent().parent().parent());
                } else if (method === "Delete" && Move) {

                    $("#SavecardBodyAppend").append($(object).parent().parent().parent());
                }
            }else if(data===false)
            {
                alert('Could not ' + method + ' Map!');
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