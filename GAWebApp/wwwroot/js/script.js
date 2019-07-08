// script.js

function textbox_keydown(textbox) {
    if (event.key === 'Enter') {
        let expression = textbox.value;
        $.ajax({
            url: 'Home/Calculate',
            type: 'GET',
            data: {
                'expression': expression
            },
            success: renderedHtml => {
                $('#historyView').html(renderedHtml);
            }
        });
    }
}