// Get the 'Location' header from the response
const locationHeader = pm.response.headers.get('Location');

if (locationHeader) {
    // Use a regex to extract the 'code' query parameter from the URL
    const codeMatch = locationHeader.match(/[?&]code=([^&]+)/);
    if (codeMatch && codeMatch[1]) {
        const code = codeMatch[1];

        // Save the 'code' parameter to a collection variable
        pm.collectionVariables.set('auth_code', code);
        console.log('Code saved to collection variable:', code);
    } else {
        console.log('Code parameter not found in Location header.');
    }
} else {
    console.log('Location header not found in the response.');
}