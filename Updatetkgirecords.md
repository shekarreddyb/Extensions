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


Here's an updated aggregation pipeline incorporating these steps:db.yourCollectionName.aggregate([
    {
        $match: { "AppIdentifier": { "$exists": true } }
    },
    {
        $addFields: {
            "identifierWithoutGUID": {
                $arrayElemAt: [
                    { $split: [ "$AppIdentifier", "." ] },
                    0
                ]
            }
        }
    },
    {
        $addFields: {
            "identifierWithoutGUIDAndHyphen": {
                $function: {
                    body: function(identifier) {
                        var parts = identifier.split("-");
                        parts.pop(); // Removes the last part after the last hyphen
                        return parts.join("-");
                    },
                    args: ["$identifierWithoutGUID"],
                    lang: "js"
                }
            }
        }
    },
    {
        $group: {
            _id: "$identifierWithoutGUIDAndHyphen",
            firstDocId: { $first: "$_id" }
        }
    }
])2. Update Records Based on Each Distinct IdentifierThen, use the distinct identifiers obtained from the aggregation to update your documents:db.yourCollectionName.aggregate([
    // ... (the aggregation stages from above)
]).forEach(group => {
    db.yourCollectionName.updateMany(
        { "AppIdentifier": { "$regex": `^${group._id}(-|\\.)` } },
        { $set: { /* your update parameters */ } }
    );
});


db.yourCollectionName.aggregate([
    {
        $match: { "AppIdentifier": { "$exists": true } }
    },
    {
        $addFields: {
            "identifierWithoutGUID": {
                $arrayElemAt: [
                    { $split: [ "$AppIdentifier", "." ] },
                    0
                ]
            }
        }
    },
    {
        $group: {
            _id: "$identifierWithoutGUID",
            firstDocId: { $first: "$_id" }
        }
    }
])
