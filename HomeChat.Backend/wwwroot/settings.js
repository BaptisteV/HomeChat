import httpClient from "./httpClient.js"
import speak from "./speak.js"

async function showModels() {
    let models = await httpClient.GetModels();
    const modelContainer = document.getElementById("model-container");
    modelContainer.replaceChildren();
    for (let element of models) {
        let model = element;
        let newModel = document.getElementById("modelTemplate").cloneNode(false);
        newModel.id = "";
        newModel.classList.remove("hidden");

        newModel.innerHTML = model.description;

        if (model.isSelected) {
            newModel.classList.add("bg-gray-100");
            newModel.style.fontWeight = 'bold';
        }

        modelContainer.appendChild(newModel);
        newModel.onclick = async (e) => {
            e.preventDefault();
            console.log("newModel onclick...");
            const spinner = document.getElementById("spinner");
            spinner.classList.remove("hidden");
            await httpClient.SetModel(model.shortName);
            spinner.classList.add("hidden");

            await showModels();
        }

    }
}

await showModels();

const muteButton = document.getElementById("mute-button");
muteButton.onclick = (e) => {
    speak.stopSpeak();
};
const langItems = document.querySelectorAll("[data-lang]");
langItems.forEach((langItem) => {
    langItem.onclick = (e) => {
        console.log(e.target.dataset);
        speak.changeLang(e.target.dataset.lang);
    }
});

export default {
    getResponseSize: function () {
        const slider = document.getElementById("response-size-slider");
        return slider.value;
    }
}