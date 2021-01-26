
//Will be called on document ready
function init(){

};

function AddSteamIdentitiy(teamId,object)
{
    callApi("Save",teamId,$(object).parent().parent().attr("data-steamId"),object)
}




function callApi(method,teamId,steamIdentitiyId)
{
    $.ajax({
        type: 'GET',
        url: "/Team/"+method+"TeamSelectedSteamIdentity",
        data: { teamId: teamId, steamIdentityId: steamIdentitiyId  },
        success:  function(data)
        {
            location.reload();
        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            alert('Could not '+ method+' SteamIdentity!');
        }
    });
}

