
//Will be called on document ready
function init(){

};

function ChangeMap(mapId,object)
{
    if($(object).parent().parent().parent().parent().parent().parent().prop("id")==="Save") // This is some how ridiculous cause .parents() gave back delete and save. I will get e bether solution xD
    {
        callApi("Save",serverId,mapId,object);
        
    }else if($(object).parent().parent().parent().parent().parent().parent().prop("id")==="Delete"){

        callApi("Delete",serverId,mapId,object);
    }
}


function callApi(method,serverId,mapId,object)
{
    $.ajax({
        type: 'GET',
        url: "/RconServer/"+method+"ServerSelectedMap",
        data: { serverId: serverId, mapId: mapId  },
        success:  function(data)
        {
            if(method==="Save")
            {
                $("#DeletePanelBody").append($(object).parent().parent().parent());
            }else
            {
                $("#SavePanelBody").append($(object).parent().parent().parent());
            }
        },
        error: function(XMLHttpRequest, textStatus, errorThrown)
        {
            alert('Could not '+ method+' Map!');
        }
    });
}

$("#searchAuthor").bind("keyup change", function (e) {
    search($("#searchAuthor").val().toLowerCase(),"data-Author");
});
$("#searchName").bind("keyup change", function (e) {
    search($("#searchName").val().toLowerCase(),"data-Name");
});
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