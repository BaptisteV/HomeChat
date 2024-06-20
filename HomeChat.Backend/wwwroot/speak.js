import newTextEvent from "./event.js"

const synth = window.speechSynthesis;
const voices = synth.getVoices();
let currentVoice = voices[0];
const mutedKey = "muted";
const defaultMutedValue = false;

function setMuted(muted) {
    localStorage.setItem(mutedKey, JSON.stringify(muted));
}

if (localStorage.getItem(mutedKey) == null)
    setMuted(defaultMutedValue);

function isMuted() {
    return JSON.parse(localStorage.getItem(mutedKey));
}

/**
 * @param {string} detail
 */
function speak(detail) {
    if (isMuted())
        return;
    const utterThis = new SpeechSynthesisUtterance(detail);
    utterThis.voice = currentVoice;
    utterThis.rate = 1.3;
    utterThis.volume = 1;
    synth.speak(utterThis);
};


const muteButton = document.getElementById("mute-button");
muteButton.addEventListener("click", (e) => {
    let mutedBefore = JSON.parse(localStorage.getItem(mutedKey));
    setMuted(!mutedBefore);
    const mutedAfter = JSON.parse(localStorage.getItem(mutedKey));
    if (mutedAfter) {
        synth.cancel();
    }
    e.target.innerText = mutedAfter ? "Unmute" : "Mute";
}, false);
const langItems = document.querySelectorAll("[data-lang]");
langItems.forEach((langItem) => {
    langItem.onclick = (e) => {
        speak.changeLang(e.target.dataset.lang);
        currentVoice = voices.find(voice => voice.lang === e.target.dataset.lang);
        if (!currentVoice) {
            console.error(`Voice for language "${e.target.dataset.lang}" not found.`);
        }
    }
});

class PhraseBuffer {
    splitters = ["\n", ".", ",", "?", "!", ":", ";"]
    data = "";
    onNewPhrase = (phrase) => speak(phrase);

    flush() {
        this.data = "";
    }

    end() {
        this.onNewPhrase(this.data);
        this.flush();
    }

    addText(text) {
        this.data += text;
        if (this.splitters.some(s => this.data.includes(s)) || this.data.length > 101) {
            this.onNewPhrase(this.data);
            this.flush();
        }
    }
}
const phraseBuffer = new PhraseBuffer();

window.addEventListener(newTextEvent.name, (newText) => {
    if (newText.detail == null) {
        phraseBuffer.end();
        return;
    }
    phraseBuffer.addText(newText.detail);
});