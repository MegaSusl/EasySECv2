using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasySECv2.Models.DocumentTemplates
{
    public class DocumentTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string PageKey { get; set; }  // "GekComposition" и т. д.
        public string FileName { get; set; }  // например "GekComposition.docx"

        // Путь на диске, куда мы сохранили .docx-файл
        public string LocalPath { get; set; }
        [JsonInclude]
        public bool IsBatch { get; set; } = false;
        public List<PlaceholderMapping> Mappings { get; set; } = new();
    }
}
