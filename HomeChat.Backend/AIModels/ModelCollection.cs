namespace HomeChat.Backend.AIModels;

public class ModelCollection : IModelCollection
{
    private readonly string ModelRootSearchPath = @"C:\Users\Bapt\.cache\lm-studio\models";
    private List<ModelDescription> _models = [];

    public ModelCollection()
    {
        ScanForModels();
    }

    public void SelectModel(string modelShortName)
    {
        var model = _models.Single(m => m.ShortName == modelShortName);
        _models.ForEach(m => m.IsSelected = false);
        model.IsSelected = true;
    }

    private void ScanForModels()
    {
        if (!Directory.Exists(ModelRootSearchPath)) throw new DirectoryNotFoundException($"Folder {ModelRootSearchPath}' introuvable");
        var filenames = Directory.GetFiles(ModelRootSearchPath, "*.gguf", SearchOption.AllDirectories);
        var files = filenames.Select(f => new FileInfo(f)).ToList();
        _models = files.Select(f =>
        {
            var size = f.Length / 1024 / 1024 / 1024 + "Gb";
            return new ModelDescription()
            {
                Filename = f.FullName,
                ShortName = f.Name,
                Description = f.Name + " " + size,
                IsSelected = false,
                SizeInMb = f.Length / 1024 / 1024,
            };
        }).ToList();

        _models.First(m => m.ShortName.Contains("Llama-3", StringComparison.CurrentCultureIgnoreCase)).IsSelected = true;
    }

    public Task<ModelDescription> GetSelectedModel()
    {
        if (_models.Count == 0)
            ScanForModels();
        return Task.FromResult(_models.Single(m => m.IsSelected));
    }

    public Task<List<ModelDescription>> GetModels()
    {
        if (_models.Count == 0)
            ScanForModels();
        return Task.FromResult(_models);
    }
}
