using EasySECv2.Models.DocumentTemplates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Models
{
    public class DefaultMapping
    {
        public string Placeholder { get; set; } = string.Empty;
        public MappingSourceType SourceType { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string? FilterProperty { get; set; }
        public string? FilterLookupSource { get; set; }
        public string? FilterLookupKey { get; set; }
        public string? FilterLookupValue { get; set; }
        public string? DisplayTemplate { get; set; } = null;
    }

}
