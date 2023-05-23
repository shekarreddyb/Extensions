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
