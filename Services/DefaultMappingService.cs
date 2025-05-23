using EasySECv2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySECv2.Services
{
    public class DefaultMappingService : IDefaultMappingService
    {
        private readonly List<DefaultMapping> _defaults = new();

        public DefaultMappingService()
        {
            // Читаем синхронно из EmbeddedResource
            var asm = Assembly.GetExecutingAssembly();
            var name = "EasySECv2.Resources.Raw.defaultMappings.json";

            using var stream = asm.GetManifestResourceStream(name);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                try
                {
                    var opts = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    opts.Converters.Add(new JsonStringEnumConverter());

                    var list = JsonSerializer.Deserialize<List<DefaultMapping>>(json, opts)
                               ?? new List<DefaultMapping>();
                    _defaults.AddRange(list);
                    System.Diagnostics.Debug.WriteLine(
                        $"[DefaultMappingService] loaded {_defaults.Count} mappings");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[DefaultMappingService] parse error: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DefaultMappingService] resource '{name}' not found");
            }
        }

        public DefaultMapping? Get(string placeholder)
            => _defaults.Find(d =>
                   string.Equals(d.Placeholder, placeholder,
                                 StringComparison.OrdinalIgnoreCase));
    }
}
