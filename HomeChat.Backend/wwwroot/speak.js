import newTextEvent from "./event.js"

const synth = window.speechSynthesis;
const voices = synth.getVoices();
const voice = voices[0];

/**
 * @param {string} detail
 */
function speak(detail) {
    const utterThis = new SpeechSynthesisUtterance(detail);
    utterThis.voice = voice;
    utterThis.rate = 1.5;
    synth.speak(utterThis);
};

class PhraseBuffer {
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
        if (this.data.includes(".") || this.data.includes("\n") || this.data.length > 101) {
            this.onNewPhrase(this.data);
            this.flush();
        }
    }
}
const phraseBuffer = new PhraseBuffer();

window.addEventListener(newTextEvent.name, (newText) => {
    console.log("speak from event: ", newText)
    if (newText.detail == null) {
        phraseBuffer.end();
        return;
    }
    phraseBuffer.addText(newText.detail);
});
