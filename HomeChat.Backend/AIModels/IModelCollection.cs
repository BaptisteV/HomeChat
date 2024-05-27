namespace HomeChat.Backend.AIModels;

public interface IModelCollection
{
    void SelectModel(string modelShortName);
    Task<List<ModelDescription>> GetModels();
    Task<ModelDescription> GetSelectedModel();
}
