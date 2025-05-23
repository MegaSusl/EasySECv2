using EasySECv2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EasySECv2.Services
{
    public class PageSettingsService : IPageSettingsService
    {
        private readonly string _jsonPath;
        private List<PageTemplateSettings> _cache;

        public PageSettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "EasySEC", "Templates");
            Directory.CreateDirectory(folder);
            _jsonPath = Path.Combine(folder, "pageSettings.json");

            if (File.Exists(_jsonPath))
            {
                var txt = File.ReadAllText(_jsonPath);
                _cache = JsonSerializer.Deserialize<List<PageTemplateSettings>>(txt)
                         ?? new List<PageTemplateSettings>();
            }
            else
            {
                _cache = new List<PageTemplateSettings>();
                File.WriteAllText(_jsonPath, "[]");
            }
        }

        public Task<PageTemplateSettings> GetSettingsAsync(string pageKey)
        {
            var st = _cache.FirstOrDefault(s => s.PageKey == pageKey)
                  ?? new PageTemplateSettings { PageKey = pageKey, AllowBatch = false };
            return Task.FromResult(st);
        }

        public async Task SaveSettingsAsync(PageTemplateSettings settings)
        {
            var idx = _cache.FindIndex(s => s.PageKey == settings.PageKey);
            if (idx >= 0) _cache[idx] = settings;
            else _cache.Add(settings);

            var txt = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_jsonPath, txt);
        }
    }
}
