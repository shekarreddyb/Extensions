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
