let sessionId = crypto.randomUUID();
function newSession() {
    sessionId = crypto.randomUUID();
}

const resetSessionButton = document.getElementById("session-reset-button");
resetSessionButton.onclick = (e) => {
    newSession();
    window.location.href = "/";
}

export default {
    newSession: newSession,
    sessionId: () => sessionId,
}