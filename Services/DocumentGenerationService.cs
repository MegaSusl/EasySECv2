using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EasySECv2.Models;
using EasySECv2.Models.DocumentTemplates;
using EasySECv2.Services;
using EasySECv2.ViewModels;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace EasySECv2.Services
{
    /// <summary>
    /// Генерация документа из шаблона: замена маркеров и сохранение в указанную папку.
    /// Поддерживается композитная подстановка через DisplayTemplate из конфигурации.
    /// </summary>
    public class DocumentGenerationService : IDocumentGenerationService
    {
        private readonly ITemplateService _templateService;
        private static readonly Regex PlaceholderRx = new(@"\[([A-Za-z0-9_:]+)\]");

        public DocumentGenerationService(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        public async Task GenerateBatchAsync(
            DocumentTemplate tpl,
            IEnumerable<IDictionary<string, object?>> items,
            string outputFolder)
        {
            if (!File.Exists(tpl.LocalPath))
                throw new FileNotFoundException(tpl.LocalPath);
            Directory.CreateDirectory(outputFolder);

            // Загружаем конфигурацию шаблона для композитных маркеров
            var config = await _templateService.GetTemplateConfigAsync(tpl.PageKey);
            var defs = config?.Placeholders
                .ToDictionary(p => p.Name, p => p)
                ?? new Dictionary<string, PlaceholderDefinition>();

            foreach (var values in items)
            {
                using var doc = DocX.Load(tpl.LocalPath);

                // Заменяем маркеры
                foreach (var kv in values)
                {
                    var placeholderKey = kv.Key; // e.g. "[СТУДЕНТ]"
                    var nameMatch = PlaceholderRx.Match(placeholderKey);
                    var name = nameMatch.Success ? nameMatch.Groups[1].Value : string.Empty;
                    defs.TryGetValue(name, out var def);

                    string replacement;
                    if (def != null && def.Type == "Composite" && kv.Value != null)
                    {
                        replacement = FormatDisplay(def.DisplayTemplate!, kv.Value);
                    }
                    else
                    {
                        replacement = kv.Value?.ToString() ?? string.Empty;
                    }

                    doc.ReplaceText(
                        placeholderKey,
                        replacement,
                        false,
                        RegexOptions.None,
                        null,
                        null,
                        MatchFormattingOptions.SubsetMatch);
                }

                // Формируем имя файла
                var keyPart = values.ContainsKey("[ИД]")
                              ? values["[ИД]"]
                              : Guid.NewGuid();
                var outName = Path.GetFileNameWithoutExtension(tpl.FileName)
                              + "_" + keyPart
                              + Path.GetExtension(tpl.FileName);
                var outPath = Path.Combine(outputFolder, outName!);

                doc.SaveAs(outPath);
            }
        }

        public async Task GenerateAsync(
            DocumentTemplate tpl,
            IDictionary<string, object?> values,
            string outputFolder)
        {
            if (!File.Exists(tpl.LocalPath))
                throw new FileNotFoundException("Шаблон не найден", tpl.LocalPath);
            Directory.CreateDirectory(outputFolder);

            // Загружаем конфигурацию для композитных маркеров
            var config = await _templateService.GetTemplateConfigAsync(tpl.PageKey);
            var defs = config?.Placeholders
                .ToDictionary(p => p.Name, p => p)
                ?? new Dictionary<string, PlaceholderDefinition>();

            using var document = DocX.Load(tpl.LocalPath);

            foreach (var kv in values)
            {
                var placeholderKey = kv.Key;
                var nameMatch = PlaceholderRx.Match(placeholderKey);
                var name = nameMatch.Success ? nameMatch.Groups[1].Value : string.Empty;
                defs.TryGetValue(name, out var def);

                string replacement;
                if (def != null && def.Type == "Composite" && kv.Value != null)
                {
                    replacement = FormatDisplay(def.DisplayTemplate!, kv.Value);
                }
                else
                {
                    replacement = kv.Value?.ToString() ?? string.Empty;
                }

                document.ReplaceText(
                    placeholderKey,
                    replacement,
                    false,
                    RegexOptions.None,
                    null,
                    null,
                    MatchFormattingOptions.SubsetMatch);
            }

            var outFileName = tpl.FileName;
            var outPath = Path.Combine(outputFolder, outFileName);

            document.SaveAs(outPath);
        }

        /// <summary>
        /// Форматирует объект item в строку согласно шаблону вида "{Prop1} {Prop2}".
        /// </summary>
        private string FormatDisplay(string template, object item)
        {
            var result = template;
            var props = item.GetType().GetProperties();
            foreach (var prop in props)
            {
                var placeholder = "{" + prop.Name + "}";
                var propVal = prop.GetValue(item)?.ToString() ?? string.Empty;
                result = result.Replace(placeholder, propVal);
            }
            return result;
        }
    }
}
