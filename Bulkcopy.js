// Define the source and target collection names
const sourceCollectionName = 'sourceCollection';
const targetCollectionName = 'targetCollection';

// Define your search criteria
const searchCriteria = {
  // Your search criteria here
  // Example: { status: 'active' }
};

// Find documents in the source collection that match the search criteria
const cursor = db[sourceCollectionName].find(searchCriteria);

// Initialize the bulk operation
const bulkOp = db[targetCollectionName].initializeUnorderedBulkOp();
let count = 0;

cursor.forEach(document => {
  bulkOp.insert(document);
  count++;

  // Execute the bulk operation in batches of 1000 (or another suitable number)
  if (count % 1000 === 0) {
    // Execute the bulk operation
    bulkOp.execute();
    // Reinitialize the bulk operation for the next batch
    bulkOp = db[targetCollectionName].initializeUnorderedBulkOp();
  }
});

// Execute any remaining operations in the last batch
if (count % 1000 !== 0) {
  bulkOp.execute();
}

console.log('Documents copied successfully.');
