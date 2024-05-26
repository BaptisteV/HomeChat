namespace HomeChat.Backend.AIModels;

public class ModelCollection : IModelCollection
{
    private readonly string ModelRootSearchPath = @"C:\Users\Bapt\.cache\lm-studio\models";
    private List<ModelDescription> _models = new();

    public ModelDescription SelectModel(string modelShortName)
    {
        var model = _models.Single(m => m.ShortName == modelShortName);
        _models.ForEach(m => m.Selected = false);
        model.Selected = true;
        return model;

    }

    private async Task<List<ModelDescription>> ScanForModels()
    {
        if (!Directory.Exists(ModelRootSearchPath)) throw new DirectoryNotFoundException($"Folder {ModelRootSearchPath}' introuvable");
        var filenames = Directory.GetFiles(ModelRootSearchPath, "*.gguf", SearchOption.AllDirectories);
        var files = filenames.Select(f => new FileInfo(f)).ToList();
        return files.Select(f =>
        {
            var size = f.Length / 1024 / 1024 / 1024 + "Gb";
            return new ModelDescription()
            {
                Filename = f.FullName,
                ShortName = f.Name,
                Description = f.Name + " " + size,
                Selected = false,
                SizeInMb = f.Length / 1024 / 1024,
            };
        }).ToList();
    }

    public async Task<List<ModelDescription>> GetModels()
    {
        if (_models.Count == 0)
        {
            _models = await ScanForModels();
            _models.First(m => m.ShortName.Contains("Llama-3", StringComparison.CurrentCultureIgnoreCase)).Selected = true;
        }
        return _models;
    }
}
