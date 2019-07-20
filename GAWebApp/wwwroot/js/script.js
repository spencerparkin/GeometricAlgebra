// script.js

var calculatorID = '';

function refreshHistoryViewCallback(renderedHtml) {
    $('#historyView').html(renderedHtml);

    // TODO: This doesn't work.  Why?  Maybe do it after page has refreshed?
    $("#historyView").scrollTop($("#historyView")[0].scrollHeight);

    document.getElementById('expressionBox').value = '';
}

function refreshHistoryView() {
    $.ajax({
        url: 'Home/History',
        type: 'GET',
        data: {
            'calculatorID': calculatorID
        },
        success: refreshHistoryViewCallback
    });
}

function showLatexCheckboxClicked(event) {
    var checkbox = document.getElementById('showLatexCheckbox');
    $.ajax({
        url: 'Home/ShowLatex',
        type: 'GET',
        data: {
            'calculatorID': calculatorID,
            'showLatex': checkbox.checked
        },
        success: refreshHistoryViewCallback
    });
}

function clearHistoryButtonClicked(event) {
    $.ajax({
        url: 'Home/ClearHistory',
        type: 'GET',
        data: {
            'calculatorID': calculatorID
        },
        success: refreshHistoryViewCallback
    });
}

function textboxKeydown(textbox) {
    if (event.key === 'Enter') {
        let expression = textbox.value;
        $.ajax({
            url: 'Home/Calculate',
            type: 'GET',
            data: {
                'calculatorID': calculatorID,
                'expression': expression
            },
            success: refreshHistoryViewCallback
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

$(document).ready(function () {
    calculatorID = localStorage.getItem('calculatorID');
    if (!calculatorID) {
        $.getJSON('https://ipapi.co/json', function (data) {
            calculatorID = data.country_name + '-' + data.city + '-' + data.ip;
            localStorage.setItem('calculatorID', calculatorID);
            refreshHistoryView();
        });
    } else {
        refreshHistoryView();
    }
});