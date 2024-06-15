const historyMaxSize = 10;
const localStorageKey = "history"
function getHistory() {
    return localStorage.getItem(localStorageKey);
}

function addToHistory(prompt) {
    const existingHistory = getHistory();

    if (existingHistory == null) {
        console.error("Creating history")
        localStorage.setItem(localStorageKey, []);
    }

    const currentHistory = new Array(getHistory());
    if (currentHistory.length > historyMaxSize)
        currentHistory.pop();
    currentHistory.push(prompt);
    localStorage.setItem(localStorageKey, currentHistory);
}

const promptButton = document.getElementById("prompt-button");
promptButton.addEventListener("click", addToHistory(document.getElementById("prompt-input").value), false);