﻿// script.js

function refreshHistoryView(renderedHtml) {
    $('#historyView').html(renderedHtml);

    // TODO: This doesn't work.  Why?  Maybe do it after page has refreshed?
    $("#historyView").scrollTop($("#historyView")[0].scrollHeight);
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

function processScriptFile(event) {
    var file = event.target.files[0];
    if (!file)
        return;
    var reader = new FileReader();
    reader.onload = function (event) {
        var scriptText = event.target.result;
        $.ajax({
            url: 'Home/RunScript',
            type: 'GET',
            data: {
                'script': scriptText
            },
            success: refreshHistoryView
        });
    }
    reader.readAsText(file);
}

$(document).ready(function () {
    document.getElementById('scriptBox').addEventListener('change', processScriptFile, false);
});

// TODO: Add button to clear all output.  This amounts to an ajax request.