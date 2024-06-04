import session from "./session.js"

export default {
    GetModels: async function () {
        const response = await fetch(`api/${session.sessionId()}/Models`, {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
            },
        });
        const models = await response.json();
        console.log("retrieved models: ", models);
        models.sort((a, b) => a.sizeInMb - b.sizeInMb);
        return models;
    },
    SetModel: async function (newModelShortName) {
        await fetch(`api/${session.sessionId()}/Models`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({ "newModelShortName": newModelShortName }),
        });
    },

    GetPrompt: async function (prompt, maxTokens) {
        // TODO
    }

};