const historyMaxSize = 5;
const localStorageKey = "history"
localStorage.removeItem(localStorageKey);

function getHistory() {
    const item = JSON.parse(localStorage.getItem(localStorageKey));
    if (item == null)
        localStorage.setItem(localStorageKey, JSON.stringify([]));
    return JSON.parse(localStorage.getItem(localStorageKey));
}

function addToHistory(prompt) {
    const currentHistory = getHistory();
    if (currentHistory.length > historyMaxSize)
        currentHistory.pop();
    currentHistory.push(prompt);
    localStorage.setItem(localStorageKey, JSON.stringify(currentHistory));
    displayHistory();
}

function displayHistory() {
    const historyContainer = document.getElementById("history-container");
    const currentHistory = getHistory();

    const template = document.getElementById("history-template");
    historyContainer.replaceChildren();
    currentHistory.forEach((entry) => {
        const entryDiv = template.cloneNode(false);
        entryDiv.removeAttribute("id");
        entryDiv.classList.remove("hidden");
        entryDiv.textContent = entry;

        historyContainer.appendChild(entryDiv);
    });
}

const promptButton = document.getElementById("prompt-button");
promptButton.addEventListener("click", () => addToHistory(document.getElementById("prompt-input").value), false);