db.yourCollectionName.aggregate([
    {
        $project: {
            yearMonth: {
                $dateToString: { format: "%Y-%m", date: "$CreatedAt" }
            },
            CostDriverName: 1
        }
    },
    {
        $group: {
            _id: "$yearMonth",
            drivers: { $addToSet: "$CostDriverName" }
        }
    },
    {
        $project: {
            month: "$_id",
            missingAzure: { $not: { $in: ["Azure", "$drivers"] } },
            missingGCP: { $not: { $in: ["GCP", "$drivers"] } }
        }
    },
    {
        $match: {
            $or: [{ missingAzure: true }, { missingGCP: true }]
        }
    }
]).forEach(doc => print(`Month: ${doc.month}, Missing Azure: ${doc.missingAzure}, Missing GCP: ${doc.missingGCP}`));
