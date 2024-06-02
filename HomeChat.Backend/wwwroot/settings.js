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

        if (model.isSelected)
            newModel.style.fontWeight = 'bold';

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