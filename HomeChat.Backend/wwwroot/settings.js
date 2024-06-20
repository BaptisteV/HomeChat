import httpClient from "./httpClient.js"

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
            newModel.classList.add("bg-gray-200");
            newModel.style.fontWeight = 'bold';
        }

        modelContainer.appendChild(newModel);
        newModel.onclick = async (e) => {
            e.preventDefault();
            const spinner = document.getElementById("spinner");
            spinner.classList.remove("hidden");
            await httpClient.SetModel(model.shortName);
            spinner.classList.add("hidden");

            await showModels();
        }

    }
}

await showModels();

export default {
    getResponseSize: function () {
        const slider = document.getElementById("response-size-slider");
        return slider.value;
    }
}