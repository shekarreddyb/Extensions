db.cvr_record.aggregate([
    {
        $match: {
            createdAt: {
                $gte: new Date(new Date().getFullYear(), new Date().getMonth(), 1), // First day of the current month
                $lte: new Date() // Current date
            }
        }
    },
    {
        $lookup: {
            from: "app_metadata",               // The foreign collection
            localField: "distributedappId",     // The local field for matching
            foreignField: "distributedappId",   // The foreign field for matching
            as: "app_metadata"                  // The output array field
        }
    },
    {
        $unwind: "$app_metadata"                // Deconstructs the array field
    },
    {
        $merge: {
            into: "combined_collection",         // The target collection for merging
            whenMatched: "merge",
            whenNotMatched: "insert"
        }
    }
]);


//case insensitive match uaing regex

db.cvr_record.aggregate([
    {
        $match: {
            createdAt: {
                $gte: new Date(new Date().getFullYear(), new Date().getMonth(), 1), // First day of the current month
                $lte: new Date() // Current date
            }
        }
    },
    {
        $lookup: {
            from: "app_metadata",
            let: { distributedAppId: "$distributedappId" }, // Define the local variable
            pipeline: [
                {
                    $match: {
                        $expr: {
                            $regexMatch: {
                                input: "$distributedappId", // Field from the app_metadata collection
                                regex: "$$distributedAppId", // Local variable from the cvr_record collection
                                options: "i" // Case-insensitive match
                            }
                        }
                    }
                }
            ],
            as: "app_metadata"
        }
    },
    {
        $unwind: {
            path: "$app_metadata",
            preserveNullAndEmptyArrays: true // This will keep records from cvr_record even if the lookup finds no matches in app_metadata
        }
    },
    {
        $merge: {
            into: "combined_collection", // The target collection for merging
            whenMatched: "merge",
            whenNotMatched: "insert"
        }
    }
]);
