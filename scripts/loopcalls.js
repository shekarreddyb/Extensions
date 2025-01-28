// Array of GUIDs
const guids = [
  "guid1", "guid2", "guid3", /* Add your GUIDs here */
];

// Base URL
const baseUrl = "https://abcd.com/process/";

// Batch size and delay
const batchSize = 10; // Number of requests per batch
const delayBetweenBatches = 5000; // Delay in milliseconds (5 seconds)

async function fetchWithDelay(url) {
  try {
    const response = await fetch(url);
    console.log(`Fetched: ${url}, Status: ${response.status}`);
    return response.status;
  } catch (error) {
    console.error(`Error fetching: ${url}`, error);
  }
}

async function processBatch(batch) {
  const promises = batch.map(guid => fetchWithDelay(`${baseUrl}${guid}`));
  await Promise.all(promises);
}

async function processInBatches() {
  for (let i = 0; i < guids.length; i += batchSize) {
    const batch = guids.slice(i, i + batchSize); // Get the current batch
    console.log(`Processing batch: ${batch}`);
    await processBatch(batch); // Process the current batch

    if (i + batchSize < guids.length) {
      console.log(`Waiting for ${delayBetweenBatches / 1000} seconds before the next batch...`);
      await new Promise(resolve => setTimeout(resolve, delayBetweenBatches)); // Delay
    }
  }
  console.log("All batches processed!");
}

// Start processing the GUIDs
processInBatches();