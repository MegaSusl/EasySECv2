using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Models
{
    public class TemplateConfig
    {
        public string TemplateKey { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public bool AllowBatch { get; set; }
        public BatchSourceConfig? BatchSource { get; set; }
        public List<PlaceholderDefinition> Placeholders { get; set; } = new();
        public bool HasTableSection { get; set; }
    }

    public class BatchSourceConfig
    {
        public string TableName { get; set; } = null!;
        public string Method { get; set; } = null!;
    }

    public class PlaceholderDefinition
    {
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!; // "Composite", "Manual", "FromTable"
        public string? DisplayTemplate { get; set; }
        public string? TableName { get; set; }
        public List<string>? FilterProperties { get; set; }
    }
}
