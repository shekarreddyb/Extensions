let guids = pm.environment.get("guids").split(",");
let index = parseInt(pm.environment.get("currentIndex") || 0, 10);

if (index < guids.length) {
  let guid = guids[index];
  pm.environment.set("guid", guid);
  pm.environment.set("currentIndex", index + 1);

  // Introduce a delay of 5 seconds for every 10 requests
  if (index > 0 && index % 10 === 0) {
    const delay = 5000; // 5 seconds
    console.log(`Delaying for ${delay / 1000} seconds...`);
    postman.setNextRequest(null); // Temporarily stop the collection

    setTimeout(() => {
      postman.setNextRequest(pm.info.requestName); // Restart the current request
    }, delay);
  }
} else {
  console.log("All GUIDs processed!");
  postman.setNextRequest(null); // Stop the collection after processing all GUIDs
}