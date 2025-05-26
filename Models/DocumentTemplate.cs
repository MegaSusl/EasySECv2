using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Models
{
    public class DocumentTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string LocalPath { get; set; } = string.Empty;
        public TemplateType Type { get; set; }
        public List<PlaceholderMapping> Mappings { get; set; } = new();
        public string PageKey { get; set; } = ""; // Например, "batch-certificate"
    }
    public enum TemplateType
    {
        Individual, // один документ на одного студента
        Tabular     // один документ на группу с таблицей
    }
}
