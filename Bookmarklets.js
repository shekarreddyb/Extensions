javascript:(function() {
    var div = document.createElement('div');
    div.style.position = 'fixed';
    div.style.zIndex = '9999';
    div.style.backgroundColor = '#f1f1f1';
    div.style.padding = '10px';

    var input = document.createElement('input');
    input.type = 'text';
    input.name = 'apprequestid';
    input.id = 'apprequestid';
    input.placeholder = 'App Request ID';
    div.appendChild(input);

    div.appendChild(document.createElement('br'));

    var radio1 = document.createElement('input');
    radio1.type = 'radio';
    radio1.name = 'serviceaccounts';
    radio1.value = 'ServiceAccount1';
    div.appendChild(radio1);
    var label1 = document.createElement('label');
    label1.appendChild(document.createTextNode('ServiceAccount1'));
    div.appendChild(label1);

    div.appendChild(document.createElement('br'));

    var radio2 = document.createElement('input');
    radio2.type = 'radio';
    radio2.name = 'serviceaccounts';
    radio2.value = 'ServiceAccount2';
    div.appendChild(radio2);
    var label2 = document.createElement('label');
    label2.appendChild(document.createTextNode('ServiceAccount2'));
    div.appendChild(label2);

    div.appendChild(document.createElement('br'));
    div.appendChild(document.createElement('br'));

    var runBtn = document.createElement('button');
    runBtn.innerHTML = 'Run';
    runBtn.onclick = function() {
        var apprequestid = document.getElementById('apprequestid').value;
        var serviceaccounts = document.querySelector('input[name="serviceaccounts"]:checked').value;
        window.open("http://example.com/?apprequestid=" + encodeURIComponent(apprequestid) + "&serviceaccount=" + encodeURIComponent(serviceaccounts));
    };
    div.appendChild(runBtn);

    var forceRunBtn = document.createElement('button');
    forceRunBtn.innerHTML = 'ForceRun';
    forceRunBtn.onclick = function() {
        var apprequestid = document.getElementById('apprequestid').value;
        var serviceaccounts = document.querySelector('input[name="serviceaccounts"]:checked').value;
        window.open("http://example.com/forceRun?apprequestid=" + encodeURIComponent(apprequestid) + "&serviceaccount=" + encodeURIComponent(serviceaccounts));
    };
    div.appendChild(forceRunBtn);

    document.body.appendChild(div);
})();

