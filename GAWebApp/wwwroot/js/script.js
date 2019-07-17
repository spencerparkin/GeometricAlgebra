// script.js

function refreshHistoryView(renderedHtml) {
    $('#historyView').html(renderedHtml);

    // TODO: This doesn't work.  Why?  Maybe do it after page has refreshed?
    $("#historyView").scrollTop($("#historyView")[0].scrollHeight);

    document.getElementById('expressionBox').value = '';
}

function showLatexCheckboxClicked(event) {
    var checkbox = document.getElementById('showLatexCheckbox');
    $.ajax({
        url: 'Home/ShowLatex',
        type: 'GET',
        data: {
            'showLatex': checkbox.checked
        },
        success: refreshHistoryView
    });
}

function clearHistoryButtonClicked(event) {
    $.ajax({
        url: 'Home/ClearHistory',
        type: 'GET',
        data: {},
        success: refreshHistoryView
    });
}

function textboxKeydown(textbox) {
    if (event.key === 'Enter') {
        let expression = textbox.value;
        $.ajax({
            url: 'Home/Calculate',
            type: 'GET',
            data: {
                'expression': expression
            },
            success: refreshHistoryView
        });
    }
}

function toggleGuideButtonClicked() {
    let button = document.getElementById('toggleGuideButton');
    if (button.value === 'Show Quick Guide') {
        $('#quickGuide').show();
        button.value = 'Hide Quick Guide';
    } else if (button.value === 'Hide Quick Guide') {
        $('#quickGuide').hide();
        button.value = 'Show Quick Guide';
    }
}