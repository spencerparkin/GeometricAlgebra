// script.js

function textbox_keydown(textbox) {
    if (event.key === 'Enter') {
        let expression = textbox.value;
        $.ajax({
            url: 'Home/Calculate',
            type: 'GET',
            data: {
                'expression': expression
            }
            // TODO: Do we need to do something with the returned result to get the partial view to update?  If so, what?!
        });
    }
}