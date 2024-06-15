namespace HomeChat.Backend.AIModels;

public class ModelCollectionConfiguration
{
    public string ModelLookupRootDirectory { get; set; } = "";
    public bool ApplyFilters { get; set; }
    public string[] ModelFilenameFilters { get; set; } = [""];
}

public class ModelCollection : IModelCollection
{
    private ModelCollectionConfiguration _configuration = new();
    private List<ModelDescription> _models = [];

    public ModelCollection(IConfiguration configuration)
    {
        ReadConfiguration(configuration);
        ScanForModels();
    }

    private void ReadConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(nameof(ModelCollectionConfiguration));
        _configuration = new ModelCollectionConfiguration()
        {
            ApplyFilters = section.GetValue<bool>(nameof(ModelCollectionConfiguration.ApplyFilters)),
            ModelFilenameFilters = section.GetSection(nameof(ModelCollectionConfiguration.ModelFilenameFilters)).Get<string[]>()!,
            ModelLookupRootDirectory = section.GetValue<string>(nameof(ModelCollectionConfiguration.ModelLookupRootDirectory))!,
        };
    }
    public void SelectModel(string modelShortName)
    {
        var model = _models.Single(m => m.ShortName == modelShortName);
        _models.ForEach(m => m.IsSelected = false);
        model.IsSelected = true;
    }

    private void ScanForModels()
    {
        if (!Directory.Exists(_configuration.ModelLookupRootDirectory)) throw new DirectoryNotFoundException($"Folder '{_configuration.ModelLookupRootDirectory}' introuvable");
        var filenames = Directory.GetFiles(_configuration.ModelLookupRootDirectory, "*.gguf", SearchOption.AllDirectories);
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

        if (_configuration.ApplyFilters)
        {
            _models = _models.Where(m => _configuration.ModelFilenameFilters.Any(c => m.Filename.Contains(c, StringComparison.OrdinalIgnoreCase))).ToList();

            if (_models.Count == 0)
                throw new FileNotFoundException($"No model found using filters: {string.Join(", ", _configuration.ModelFilenameFilters)}" +
                    $". Models found: {string.Join(", ", _models.Select(m => m.Filename))}");
        }

        if (_models.Count == 0)
            throw new FileNotFoundException($"No model found");

        _models.MinBy(m => m.SizeInMb)!.IsSelected = true;
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
