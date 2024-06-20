const promptButton = document.getElementById("prompt-button");

promptButton.addEventListener("click", () => {
    let ding = new Audio("./chatding.mp3");
    ding.volume = 0.1;
    ding.play()
}, false);
function getPentatonicNoteFrequency(char) {
    const scaleNotes = [
        { note: 'A3', frequency: 220 },
        { note: 'C4', frequency: 261.63 },
        { note: 'D4', frequency: 293.66 },
        { note: 'E4', frequency: 329.63 },
        { note: 'G4', frequency: 392 },
    ];

    const charCode = char.charCodeAt(0); // Get the character code of the char
    const index = charCode % scaleNotes.length; // Use modulo to wrap around the scale notes
    return scaleNotes[index].frequency;
}

function playNoteFromChar(inputChar) {
    const frequency = getPentatonicNoteFrequency(inputChar); // Pass the character directly
    const audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    const oscillator = audioCtx.createOscillator();
    const gainNode = audioCtx.createGain();

    oscillator.type = 'sine'; // Sine wave
    oscillator.frequency.setValueAtTime(frequency, audioCtx.currentTime); // Set frequency

    oscillator.connect(gainNode);
    gainNode.connect(audioCtx.destination);

    // Total duration
    const totalDuration = 0.15; // 150ms

    // ADSR envelope parameters calculated to fit within the total duration
    const attackTime = totalDuration * 0.1; // 10% of total duration
    const decayTime = totalDuration * 0.2; // 20% of total duration
    const sustainTime = totalDuration * 0.5; // 50% of total duration
    const releaseTime = totalDuration * 0.2; // 20% of total duration

    const master = 0.01;
    const sustainLevel = 0.8 * master;

    // Envelope
    gainNode.gain.setValueAtTime(0, audioCtx.currentTime);
    gainNode.gain.linearRampToValueAtTime(1 * master, audioCtx.currentTime + attackTime); // Attack
    gainNode.gain.linearRampToValueAtTime(sustainLevel, audioCtx.currentTime + attackTime + decayTime); // Decay
    gainNode.gain.setValueAtTime(sustainLevel, audioCtx.currentTime + attackTime + decayTime + sustainTime); // Sustain
    gainNode.gain.linearRampToValueAtTime(0, audioCtx.currentTime + totalDuration); // Release
    
    oscillator.start();
    oscillator.stop(audioCtx.currentTime + totalDuration); // Stop after the envelope
}
function onNewContent(mutationsList, observer) {
    for (let mutation of mutationsList) {
        if (mutation.type === 'childList') {
            mutation.addedNodes.forEach(node => {
                if (node.nodeType === Node.TEXT_NODE) {
                    console.log(node.data); // Log new text content
                    //playSound();
                    playNoteFromChar(node.data.slice(-1));
                } else if (node.nodeType === Node.ELEMENT_NODE) {
                    node.childNodes.forEach(childNode => {
                        if (childNode.nodeType === Node.TEXT_NODE) {
                            console.log(childNode.data); // Log new text content within element
                            //playSound();
                            playNoteFromChar(childNode.data.slice(-1));
                        }
                    });
                }
            });
        } else if (mutation.type === 'characterData') {
            console.log(mutation.target.data); // Log updated text content
            //playSound();
            playNoteFromChar(mutation.target.data);
        }
    }
}


// Create a MutationObserver instance and pass in the callback function
const observer = new MutationObserver(onNewContent);

// Options for the observer (which mutations to observe)
const config = { childList: true, subtree: true, characterData: true };
const chatContainer = document.getElementById("chat-container");
// Start observing the chat container with the specified configuration
observer.observe(chatContainer, config);









