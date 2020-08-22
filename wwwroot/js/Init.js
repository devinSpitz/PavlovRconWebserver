// Little bit on the tricky side may should be done better

$(document).ready(function() {
    if(typeof(init) !== 'undefined' && jQuery.isFunction(init))
        init();

});