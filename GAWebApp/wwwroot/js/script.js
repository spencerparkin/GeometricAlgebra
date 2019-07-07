// script.js

function textbox_keydown(textbox) {
    if (event.key === 'Enter') {
        let expression = textbox.value;
        $.ajax({
            url: 'Calculate',
            type: 'GET',
            data: {
                'expression': expression
            }
        });
    }
}