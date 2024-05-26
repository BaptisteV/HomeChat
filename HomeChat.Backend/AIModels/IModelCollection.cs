namespace HomeChat.Backend.AIModels;

public interface IModelCollection
{
    ModelDescription SelectModel(string modelShortName);
    Task<List<ModelDescription>> GetModels();
}
