using EasySECv2.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xceed.Words.NET;
using Xceed.Document.NET;

namespace EasySECv2.Services
{
    public class DocumentGenerationService : IDocumentGenerationService
    {
        public async Task GenerateBatchAsync(DocumentTemplate template, List<Student> students, Dictionary<string, string> manualInputs, string outputDir)
        {
            foreach (var student in students)
            {
                var map = BuildMap(template.Mappings, student, manualInputs);
                using var doc = DocX.Load(template.LocalPath);

                foreach (var p in doc.Paragraphs.ToList())
                {
                    if (p.Text.Contains("[СТУДЕНТ:ФИО]"))
                    {
                        p.ReplaceText("[СТУДЕНТ:ФИО]", "");
                        p.Append(student.FullName);
                    }
                }

                foreach (var kvp in map)
                {
                    if (kvp.Key != "СТУДЕНТ:ФИО")
                        doc.ReplaceText($"[{kvp.Key}]", kvp.Value ?? "");
                }

                var fileName = $"{student.surname}_{template.Name}.docx";
                doc.SaveAs(Path.Combine(outputDir, fileName));
            }
        }

        public async Task GenerateTabularAsync(DocumentTemplate template, Group group, List<Student> students, Dictionary<string, string> manualInputs, string outputDir)
        {
            var doc = DocX.Load(template.LocalPath);
            var table = doc.Tables.FirstOrDefault();
            if (table == null) return;

            for (int i = 0; i < students.Count; i++)
            {
                var row = table.InsertRow();
                row.ReplaceText("[СТУДЕНТ:ФИО]", students[i].FullName);
                row.ReplaceText("[INDEX]", (i + 1).ToString());
            }

            foreach (var kvp in manualInputs)
                doc.ReplaceText($"[{kvp.Key}]", kvp.Value);

            doc.SaveAs(Path.Combine(outputDir, $"{group.name}_{template.Name}.docx"));
        }

        private Dictionary<string, string> BuildMap(List<PlaceholderMapping> mappings, Student student, Dictionary<string, string> manual)
        {
            var map = new Dictionary<string, string>();
            foreach (var m in mappings)
            {
                string value = m.SourceType switch
                {
                    MappingSourceType.Manual => manual.GetValueOrDefault(m.Placeholder, string.Empty),
                    MappingSourceType.Student => student.GetPropertyValue(m.Property) ?? string.Empty,
                    MappingSourceType.Calculated => GetCalculatedValue(m.Placeholder),
                    _ => string.Empty
                };
                map[m.Placeholder] = value;
            }
            return map;
        }

        private string GetCalculatedValue(string placeholder)
        {
            return placeholder switch
            {
                "МЕСЯЦ" => DateTime.Now.ToString("MMMM", new CultureInfo("ru-RU")),
                _ => string.Empty
            };
        }
    }
}