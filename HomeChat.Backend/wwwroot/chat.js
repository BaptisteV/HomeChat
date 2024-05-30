import newTextEvent from "./event.js"

const aiEvent = "aiMessage";
const url = "api/Prompt";

let eventSource = null;
function initEventSource(prompt, maxTokens) {
    const query = url + "?prompt=" + encodeURIComponent(prompt) + "&maxTokens=" + maxTokens;
    eventSource = new EventSource(query, {
        withCredentials: false,
    });

    eventSource.onerror = (e) => {
        console.error(e);
    };
    return eventSource;
}

function onNewTextHandler(e) {
    const data = JSON.parse(e.data);
    if (data == null || data.newText == null || data.newText == "") {
        eventSource.close();
        handlerAiMessage(null);
        return;
    }
    const message = data.newText;
    handlerAiMessage(message);
}

function queryBackend(prompt, maxTokens) {
    console.log(`querying backend with (${maxTokens}) (${prompt})`);
    initEventSource(prompt, maxTokens);

    eventSource.addEventListener(aiEvent, onNewTextHandler);
}

function getLastResponse() {
    return document.querySelector("#chat-container > :last-child");
}

function handleEventDetail(eventDetail) {
    //console.log(eventDetail);
    const newText = eventDetail.detail;

    const lastResponse = getLastResponse();
    if (lastResponse.classList.contains("userMessage")) {
        createNewMessage("", getAiTemplate());
    }
    console.log("adding :(" + newText + ")", typeof newText);
    if (newText !== null)
    getLastResponse().innerHTML += newText;
}

window.addEventListener(newTextEvent.name, handleEventDetail);

function handlerAiMessage(aiData) {
    const event = new CustomEvent(newTextEvent.name, { detail: aiData });
    window.dispatchEvent(event);
}

function createNewMessage(message, template) {
    const messageDiv = template.cloneNode(false);
    messageDiv.id = "";
    messageDiv.innerHTML = message;
    messageDiv.classList.remove("hidden");

    const chatContainer = document.getElementById("chat-container");
    chatContainer.appendChild(messageDiv);
}

function getUserTemplate() {
    return document.getElementById("userMessageTemplate");
}

function getAiTemplate() {
    return document.getElementById("aiMessageTemplate");
}

function onPromptClick(e) {
    const newMessageContent = document.getElementById("prompt-input").value;
    console.log("creating new user message : " + newMessageContent);
    createNewMessage(newMessageContent, getUserTemplate());
    queryBackend(newMessageContent, 250);
}

// binding
const promptButton = document.getElementById("prompt-button");
promptButton.onclick = onPromptClick;