db.yourCollectionName.find({ "AppIdentifier": { "$exists": true } }).forEach(document => {
    // Split the AppIdentifier and remove the GUID part
    let parts = document.AppIdentifier.split(".");
    parts.pop(); // removes the last part (GUID)
    let identifierWithoutGUID = parts.join(".");

    // Update other documents with the same identifier (without GUID)
    db.yourCollectionName.updateMany(
        { "AppIdentifier": { "$regex": `^${identifierWithoutGUID}\\.` } },
        { $set: { /* your update parameters */ } }
    );
});