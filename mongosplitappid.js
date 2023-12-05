
db.cvrrecord.aggregate([
    {
        $addFields: {
            DistributedAppId: {
                $cond: {
                    if: {
                        $regexMatch: {
                            input: "$AppIdentifier",
                            regex: /^CA[0-9]+-env$/ // Adjust the regex as needed
                        }
                    },
                    then: "$AppIdentifier",
                    else: {
                        $arrayElemAt: [{ $split: ["$AppIdentifier", "-"] }, 0]
                    }
                }
            }
        }
    }
    // Optionally, you can add additional stages like $out to save the output to another collection
]);