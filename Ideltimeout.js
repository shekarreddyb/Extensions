window.addEventListener('storage', function(e) {  
    if (e.key === 'logout') {
        location.href = '/Account/Logout';
    } else if (e.key === 'keep-alive') {
        clearTimeout(timeout);
        timeout = setTimeout(logout, 20 * 60 * 1000); // 20 minutes
    }
});

let timeout;
let debounce;
document.onmousemove = resetTimer;
document.onkeypress = resetTimer;

function logout() {
    localStorage.setItem('logout', Date.now()); 
}

function keepAlive() {
    // Use AJAX to make a request to your "keep-alive" endpoint
    fetch('/Account/KeepAlive');
}

function resetTimer() {
    clearTimeout(timeout);
    clearTimeout(debounce);  // Clear any previous debounce timers

    localStorage.removeItem('keep-alive');
    localStorage.setItem('keep-alive', Date.now()); 

    debounce = setTimeout(keepAlive, 5000);  // Make an API call 5 seconds after the last interaction
    timeout = setTimeout(logout, 20 * 60 * 1000); // 20 minutes
}
