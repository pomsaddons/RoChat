const axios = require('axios');

async function test() {
    try {
        const placeId = 189707;
        
        // Step 1: Get Universe ID
        console.log('Fetching Universe ID...');
        const universeRes = await axios.get(`https://apis.roblox.com/universes/v1/places/${placeId}/universe`);
        console.log('Universe Response:', universeRes.data);
        const universeId = universeRes.data.universeId;

        // Step 2: Get Game Details
        console.log(`Fetching Game Details for Universe ${universeId}...`);
        const gameRes = await axios.get(`https://games.roblox.com/v1/games?universeIds=${universeId}`);
        console.log('Game Response:', JSON.stringify(gameRes.data, null, 2));

    } catch (e) {
        console.error(e.message);
        if (e.response) {
            console.error(e.response.data);
        }
    }
}

test();
