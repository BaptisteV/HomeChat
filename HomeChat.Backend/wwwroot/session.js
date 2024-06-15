import httpClient from "./httpClient.js"

let sessionId = localStorage.getItem("sessionId");
await newSession();

function hexToRgb(hex) {
    let bigint = parseInt(hex, 16);
    let r = (bigint >> 16) & 255;
    let g = (bigint >> 8) & 255;
    let b = bigint & 255;
    return [r, g, b];
}

function hashGuidToHex(guid) {
    let hash = 0;
    for (let i = 0; i < guid.length; i++) {
        const char = guid.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
        hash |= 0; // Convert to 32bit integer
    }
    let hexColor = (hash & 0x00FFFFFF).toString(16).toUpperCase();
    return "00000".substring(0, 6 - hexColor.length) + hexColor;
}

async function newSession() {
    sessionId = crypto.randomUUID();
    localStorage.setItem("sessionId", sessionId);

    const hexColor = hashGuidToHex(sessionId);
    const [r, g, b] = hexToRgb(hexColor);
    const chatContainer = document.getElementById("chat-container");
    chatContainer.style.backgroundColor = `rgba(${r}, ${g}, ${b}, 0.115)`;
    chatContainer.replaceChildren();
}
async function endSession() {
    if (sessionId) {
        await httpClient.DeleteSession(sessionId);
        localStorage.removeItem("sessionId");
        sessionId = null;
    }
}

const resetSessionButton = document.getElementById("session-reset-button");
resetSessionButton.onclick = async () => await endSession();

export default {
    newSession: async () => await newSession(),
    sessionId: () => sessionId,
}