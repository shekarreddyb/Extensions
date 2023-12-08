To modify your MongoDB aggregation pipeline to remove both the part after the last period and the part after the last hyphen (`-`) from the `AppIdentifier`, you can use a combination of string manipulation operators within the aggregation framework.

Here's how you can adjust the pipeline:

### 1. Split by Period and Hyphen, Then Rejoin

In the `$addFields` stage, you'll first split the `AppIdentifier` by the period, remove the last element (the GUID), then join the remaining parts back together. After that, you'll repeat a similar process for the hyphen.

Here's an updated aggregation pipeline incorporating these steps:

```javascript
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
])
```

### 2. Update Records Based on Each Distinct Identifier

Then, use the distinct identifiers obtained from the aggregation to update your documents:

```javascript
db.yourCollectionName.aggregate([
    // ... (the aggregation stages from above)
]).forEach(group => {
    db.yourCollectionName.updateMany(
        { "AppIdentifier": { "$regex": `^${group._id}(-|\\.)` } },
        { $set: { /* your update parameters */ } }
    );
});
```

### Considerations

- The `$function` operator allows you to define a custom JavaScript function for more complex string manipulations. This is used here to handle the splitting and joining around the hyphen.
- Ensure that your regular expression in `updateMany` accurately reflects the logic you want for matching `AppIdentifier`.
- Always test this on a subset of your data or in a non-production environment to ensure it behaves as expected, especially since string manipulations can be tricky.
- This approach assumes that `AppIdentifier` always contains both a period and a hyphen. Adjust the logic if this is not the case.


```javascript
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
        $addFields: {
            "identifierWithoutGUIDAndHyphen": {
                $reduce: {
                    input: { $slice: [ { $split: [ "$identifierWithoutGUID", "-" ] }, 0, -1 ] },
                    initialValue: "",
                    in: { $concat: [ "$$value", "$$this", "-" ] }
                }
            }
        }
    },
    {
        $project: {
            "identifierWithoutGUIDAndHyphen": { $substrCP: [ "$identifierWithoutGUIDAndHyphen", 0, { $subtract: [ { $strLenCP: "$identifierWithoutGUIDAndHyphen" }, 1 ] } ] }
        }
    },
    {
        $group: {
            _id: "$identifierWithoutGUIDAndHyphen",
            firstDocId: { $first: "$_id" }
        }
    }
])
```
