using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text.Json;
using System.Threading.Tasks;
using EasySECv2.Models;
using EasySECv2.Models.DocumentTemplates;
using EasySECv2.Services;

namespace EasySECv2.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly string _dataFolder;
        private string _jsonPath => Path.Combine(_dataFolder, "templates.json");
        private string _templatesDir => _dataFolder;
        private readonly ITemplateParserService _parser;
        private readonly IDefaultMappingService _defaults;
        private readonly string _configPath = Path.Combine(FileSystem.AppDataDirectory, "Resources/Raw");
        public TemplateService(
            ITemplateParserService parser,
            IDefaultMappingService defaults)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));

            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _dataFolder = Path.Combine(docs, "EasySEC", "Templates");
            Directory.CreateDirectory(_dataFolder);

            if (!File.Exists(_jsonPath))
                File.WriteAllText(_jsonPath, "[]");
        }

        public async Task<IEnumerable<DocumentTemplate>> GetTemplatesAsync(string pageKey)
        {
            var all = JsonSerializer.Deserialize<List<DocumentTemplate>>(
                          await File.ReadAllTextAsync(_jsonPath))
                      ?? new List<DocumentTemplate>();

            bool updated = false;
            foreach (var tpl in all.Where(t => t.PageKey == pageKey))
            {
                // ——————————————————————————————————————————————
                // 1) Если мэппингов нет, создаём их из шаблона
                if (tpl.Mappings == null || !tpl.Mappings.Any())
                {
                    var names = _parser.ExtractPlaceholders(tpl.LocalPath);
                    tpl.Mappings = names.Select(name => new PlaceholderMapping { Placeholder = name })
                                    .ToList();
                    updated = true;
                }

                // 2) Всегда подмешиваем дефолтные настройки (SourceType, SourceName, FilterProperty, DisplayTemplate)
                foreach (var pm in tpl.Mappings)
                {
                    var dm = _defaults.Get(pm.Placeholder);
                    if (dm != null)
                    {
                        pm.SourceType = dm.SourceType;
                        pm.SourceName = dm.SourceName;
                        pm.FilterProperty = dm.FilterProperty;
                        pm.DisplayTemplate = dm.DisplayTemplate;
                        updated = true;
                    }
                }
            }

            if (updated)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(_jsonPath, JsonSerializer.Serialize(all, options));
            }

            return all.Where(t => t.PageKey == pageKey);
        }

        public async Task AddOrUpdateAsync(string pageKey, string fileName, Stream docxStream)
        {
            var all = JsonSerializer.Deserialize<List<DocumentTemplate>>(
                          await File.ReadAllTextAsync(_jsonPath))
                      ?? new List<DocumentTemplate>();

            var tpl = new DocumentTemplate
            {
                Id = Guid.NewGuid(),
                PageKey = pageKey,
                FileName = fileName
            };
            all.Add(tpl);

            var target = Path.Combine(_templatesDir, $"{tpl.Id}.docx");
            using (var fs = File.Create(target))
            {
                await docxStream.CopyToAsync(fs);
            }
            tpl.LocalPath = target;

            var placeholders = _parser.ExtractPlaceholders(target);
            tpl.Mappings = placeholders.Select(name =>
            {
                var pm = new PlaceholderMapping { Placeholder = name };
                var dm = _defaults.Get(name);
                if (dm != null)
                {
                    pm.SourceType = dm.SourceType;
                    pm.SourceName = dm.SourceName;
                    pm.FilterProperty = dm.FilterProperty;
                    pm.DisplayTemplate = dm.DisplayTemplate;
                }
                return pm;
            }).ToList();

            await File.WriteAllTextAsync(_jsonPath,
                JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
        }

        public async Task UpdateAsync(Guid templateId, string fileName, Stream docxStream)
        {
            var all = JsonSerializer.Deserialize<List<DocumentTemplate>>(
                          await File.ReadAllTextAsync(_jsonPath))
                      ?? new List<DocumentTemplate>();

            var tpl = all.FirstOrDefault(t => t.Id == templateId)
                   ?? throw new InvalidOperationException("Шаблон не найден");

            if (File.Exists(tpl.LocalPath))
                File.Delete(tpl.LocalPath);

            var target = Path.Combine(_templatesDir, $"{tpl.Id}.docx");
            using (var fs = File.Create(target))
            {
                await docxStream.CopyToAsync(fs);
            }

            tpl.FileName = fileName;
            tpl.LocalPath = target;

            var placeholders = _parser.ExtractPlaceholders(target);
            tpl.Mappings = placeholders.Select(name =>
            {
                var pm = new PlaceholderMapping { Placeholder = name };
                var dm = _defaults.Get(name);
                if (dm != null)
                {
                    pm.SourceType = dm.SourceType;
                    pm.SourceName = dm.SourceName;
                    pm.FilterProperty = dm.FilterProperty;
                    pm.DisplayTemplate = dm.DisplayTemplate;
                }
                return pm;
            }).ToList();

            await File.WriteAllTextAsync(_jsonPath,
                JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
        }

        public async Task DeleteAsync(Guid templateId)
        {
            var all = JsonSerializer.Deserialize<List<DocumentTemplate>>(
                          await File.ReadAllTextAsync(_jsonPath))
                      ?? new List<DocumentTemplate>();

            var tpl = all.FirstOrDefault(t => t.Id == templateId);
            if (tpl != null)
            {
                if (File.Exists(tpl.LocalPath))
                    File.Delete(tpl.LocalPath);
                all.Remove(tpl);
                await File.WriteAllTextAsync(_jsonPath,
                    JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        public async Task<TemplateConfig?> GetTemplateConfigAsync(string templateKey)
        {
            var file = Path.Combine(_configPath, "TemplateConfig.json");
            if (!File.Exists(file)) return null;

            var json = await File.ReadAllTextAsync(file);
            var configs = JsonSerializer.Deserialize<List<TemplateConfig>>(json);
            return configs?.FirstOrDefault(c => c.TemplateKey == templateKey);
        }

        public async Task<List<PlaceholderMapping>> GetMappingsAsync(string templateKey, string templatePath)
        {
            // 1) Извлечь все маркеры из .docx
            var extracted = _parser.ExtractPlaceholders(templatePath);

            // 2) Загрузить дефолты из конфига
            var config = await GetTemplateConfigAsync(templateKey);
            var defaults = config?.Placeholders.ToDictionary(p => p.Name)
                                 ?? new Dictionary<string, PlaceholderDefinition>();

            // 3) Сформировать мэппинги
            var mappings = extracted.Select(name =>
            {
                // Создаем базовый mapping
                var mapping = new PlaceholderMapping
                {
                    Placeholder = name
                };

                if (defaults.TryGetValue(name, out var def))
                {
                    // Устанавливаем тип источника
                    mapping.SourceType = def.Type switch
                    {
                        "Composite" => MappingSourceType.Composite,
                        "FromTable" => MappingSourceType.FromTable,
                        _ => MappingSourceType.Manual
                    };
                    // Дополнительные параметры
                    mapping.DisplayTemplate = def.DisplayTemplate;
                    mapping.TableName = def.TableName;
                    mapping.FilterProperties = def.FilterProperties ?? new List<string>();
                }
                else
                {
                    mapping.SourceType = MappingSourceType.Manual;
                }

                return mapping;
            })
            .ToList();

            return mappings;
        }
    }
}