import newTextEvent from "./event.js"

const synth = window.speechSynthesis;
const voices = synth.getVoices();
let currentVoice = voices[0];

/**
 * @param {string} detail
 */
function speak(detail) {
    const utterThis = new SpeechSynthesisUtterance(detail);
    utterThis.voice = currentVoice;
    utterThis.rate = 1.1;
    synth.speak(utterThis);
};

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

export default {
    stopSpeak: function () {
        synth.cancel()
    },
    changeLang: function (lang) {
        currentVoice = voices.find(voice => voice.lang === lang);
        if (!currentVoice) {
            console.error(`Voice for language "${lang}" not found.`);
        }
    }
};