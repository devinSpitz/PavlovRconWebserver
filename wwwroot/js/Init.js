// Little bit on the tricky side may should be done better

$(document).ready(function() {
    if(typeof(init) !== 'undefined' && jQuery.isFunction(init))
        init();

    $.fn.serializeObject = function() {

        var form = {};
        $.each($(this).serializeArray(), function (i, field) {
            form[field.name] = field.value || "";
        });

        return form;
    };
    
});