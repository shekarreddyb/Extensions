// to find max usage of each opp in a month
db.collectionName.aggregate([
    {
        $group: {
            _id: {
                appname: "$appname",
                platformName: "$platform name",
                recordedDate: {
                    $dateToString: { 
                        format: "%Y-%m-%d", 
                        date: "$recorded date" 
                    }
                }
            },
            totalMemoryUsed: {
                $sum: "$memoryUsed"
            }
        }
    },
    {
        $group: {
            _id: "$_id.appname",
            maxMemoryUsed: {
                $max: "$totalMemoryUsed"
            }
        }
    }
])


apply date filter

db.collectionName.aggregate([
    {
        $match: {
            $expr: {
                $and: [
                    { $eq: [ { $month: "$recorded date" }, YOUR_MONTH ] },  // replace YOUR_MONTH with the desired month (1-12)
                    { $eq: [ { $year: "$recorded date" }, YOUR_YEAR ] }    // replace YOUR_YEAR with the desired year
                ]
            }
        }
    },
    {
        $group: {
            _id: {
                appname: "$appname",
                platformName: "$platform name",
                recordedDate: {
                    $dateToString: { 
                        format: "%Y-%m-%d", 
                        date: "$recorded date" 
                    }
                }
            },
            totalMemoryUsed: {
                $sum: "$memoryUsed"
            }
        }
    },
    {
        $group: {
            _id: "$_id.appname",
            maxMemoryUsed: {
                $max: "$totalMemoryUsed"
            }
        }
    }
])
