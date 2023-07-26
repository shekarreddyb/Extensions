javascript:(function() {
    var value = prompt("Please enter value");
    if (value != null) {
        window.location.href = "http://example.com/?value=" + encodeURIComponent(value);
    }
})();
