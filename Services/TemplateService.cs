using EasySECv2.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasySECv2.Services;

public interface ITemplateService
{
    Task<IEnumerable<DocumentTemplate>> GetTemplatesAsync(string pageKey);
    Task AddTemplateAsync(Stream fileStream, string originalFileName, string pageKey);
    Task DeleteTemplateAsync(DocumentTemplate template);
    Task<List<DocumentTemplate>> GetAllTemplatesAsync();
    Task SaveAllTemplatesAsync(List<DocumentTemplate> templates);
}

public class TemplateService : ITemplateService
{
    private readonly string _templateFolder;
    private readonly string _jsonPath;

    public TemplateService()
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "EasySEC", "Templates");

        Directory.CreateDirectory(basePath);
        _templateFolder = basePath;
        _jsonPath = Path.Combine(basePath, "templates.json");
    }

    public async Task<IEnumerable<DocumentTemplate>> GetTemplatesAsync(string pageKey)
    {
        var all = await LoadAllAsync();
        return all.Where(t => t.PageKey == pageKey);
    }

    public async Task AddTemplateAsync(Stream fileStream, string originalFileName, string pageKey)
    {
        var uniqueName = Guid.NewGuid().ToString() + Path.GetExtension(originalFileName);
        var targetPath = Path.Combine(_templateFolder, uniqueName);


        using (var file = File.Create(targetPath))
        {
            await fileStream.CopyToAsync(file);
        }

        System.Diagnostics.Debug.WriteLine("Trying to parse: " + targetPath);

        var placeholders = ExtractPlaceholdersFromDocx(targetPath);

        System.Diagnostics.Debug.WriteLine("Found: " + string.Join(", ", placeholders));
        var template = new DocumentTemplate
        {
            Name = Path.GetFileNameWithoutExtension(originalFileName),
            LocalPath = targetPath,
            PageKey = pageKey,
            Type = TemplateType.Individual,
            Mappings = placeholders
                .Select(ph => new PlaceholderMapping { Placeholder = ph, SourceType = MappingSourceType.Manual })
                .ToList()
        };

        var all = await LoadAllAsync();
        all.Add(template);
        await SaveAllAsync(all);
    }

    public async Task DeleteTemplateAsync(DocumentTemplate template)
    {
        if (File.Exists(template.LocalPath))
            File.Delete(template.LocalPath);

        var all = await LoadAllAsync();
        all.RemoveAll(t => t.LocalPath == template.LocalPath);
        await SaveAllAsync(all);
    }

    private async Task<List<DocumentTemplate>> LoadAllAsync()
    {
        if (!File.Exists(_jsonPath)) return new List<DocumentTemplate>();
        var json = await File.ReadAllTextAsync(_jsonPath);
        return JsonSerializer.Deserialize<List<DocumentTemplate>>(json) ?? new();
    }

    private async Task SaveAllAsync(List<DocumentTemplate> templates)
    {
        var json = JsonSerializer.Serialize(templates, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_jsonPath, json);
    }

    private List<string> ExtractPlaceholdersFromDocx(string filePath)
    {
        var matches = new List<string>();
        try
        {
            using var doc = Xceed.Words.NET.DocX.Load(filePath);
            var text = doc.Text;
            var rx = new Regex("\\[(.+?)\\]");
            matches = rx.Matches(text).Select(m => m.Groups[1].Value.Trim()).Distinct().ToList();
        }
        catch { }
        return matches;
    }
    public async Task<List<DocumentTemplate>> GetAllTemplatesAsync()
    {
        return await LoadAllAsync();
    }

    public async Task SaveAllTemplatesAsync(List<DocumentTemplate> templates)
    {
        await SaveAllAsync(templates);
    }

}
