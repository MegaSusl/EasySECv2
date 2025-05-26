using EasySECv2.Models;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace EasySECv2.Services;

public interface ITemplateService
{
    Task<IEnumerable<DocumentTemplate>> GetTemplatesAsync(string pageKey);
    Task AddTemplateAsync(Stream fileStream, string originalFileName, string pageKey);
    Task DeleteTemplateAsync(DocumentTemplate template);
}

public class TemplateService : ITemplateService
{
    private readonly string _templateFolder;
    private readonly string _jsonPath;

    public TemplateService()
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "EasySEC", "Templates"
        );
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

        using var file = File.Create(targetPath);
        await fileStream.CopyToAsync(file);

        var template = new DocumentTemplate
        {
            Name = Path.GetFileNameWithoutExtension(originalFileName),
            LocalPath = targetPath,
            PageKey = pageKey,
            Type = TemplateType.Individual,
            Mappings = new List<PlaceholderMapping>() // пока пусто, добавляется вручную позже
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
}
