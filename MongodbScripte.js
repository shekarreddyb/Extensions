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

// we are grouping my a whole month now
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
            _id: {
                appname: "$_id.appname",
                platformName: "$_id.platformName"
            },
            maxMemoryUsed: {
                $max: "$totalMemoryUsed"
            }
        }
    }
])


// combine collections

db.metric.aggregate([
  {
    $lookup: {
      from: "capacity",
      localField: "name",
      foreignField: "resourceid",
      as: "resource"
    }
  },
  {
    $unwind: "$resource"
  },
  {
    $project: {
      _id: 0,
      name: 1,
      quantity: 1,
      maxVal: "$resource.maxVal",
      percentage: { $multiply: [{ $divide: ["$quantity", "$resource.maxVal"] }, 100] }
    }
  }
])


