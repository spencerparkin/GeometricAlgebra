// script.js

var calculatorID = '';

function refreshHistoryViewCallback(renderedHtml) {
    $('#historyView').html(renderedHtml);

    // This is a bit of a hack.  We're waiting for the html to finish so that our scrol is correct.
    setTimeout(function () {
        $("#historyView").scrollTop($("#historyView")[0].scrollHeight);
    }, 500);

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
        // This is far from a fool-proof way of uniquely identifying the visitor.
        $.getJSON('https://ipapi.co/json', function (data) {
            calculatorID = data.country_name + '-' + data.city + '-' + data.ip + '-' + Math.floor(Math.random() * 1000).toString();
            localStorage.setItem('calculatorID', calculatorID);
            refreshHistoryView();
        });
    } else {
        refreshHistoryView();
    }
});