// Connect to your database
const dbName = 'your_database_name';
const collectionName = 'your_collection_name';
const db = db.getSiblingDB(dbName);
const collection = db.getCollection(collectionName);

// Define the aggregation pipeline to find current month's records
const currentMonthPipeline = [
    {
        $match: {
            // Assuming you have a date field to identify the current month's records
            // Replace with actual date range or criteria
            dateField: { $gte: new Date("YYYY-MM-DD"), $lte: new Date("YYYY-MM-DD") } 
        }
    }
];

// Fetch current month's records
const currentMonthRecords = collection.aggregate(currentMonthPipeline).toArray();

// Iterate through current month's records and update previous records
currentMonthRecords.forEach(record => {
    const { appidentifier, DistributedAppId, Environment } = record;

    collection.updateMany(
        { 
            appidentifier: appidentifier,
            DistributedAppId: { $exists: false } // Targeting records without DistributedAppId
        },
        {
            $set: {
                DistributedAppId: DistributedAppId,
                Environment: Environment
            }
        }
    );
});

print('Update completed.');
