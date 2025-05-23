using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Models.DocumentTemplates
{
    public enum MappingSourceType { Manual, FromTable, Composite }

    public class PlaceholderMapping
    {
        public string Placeholder { get; set; }
        public MappingSourceType SourceType { get; set; } = MappingSourceType.Manual;
        public string SourceName { get; set; }      // имя таблицы, если FromTable
        public string? ManualValue { get; set; }     // если Manual
        public string? FilterProperty { get; set; }
        public string? FilterLookupSource { get; set; }
        public string? FilterLookupKey { get; set; }
        public string? FilterLookupValue { get; set; }
        public string? DisplayTemplate { get; set; }    // шаблон формата
        public string? TableName { get; set; }
        public List<string> FilterProperties { get; set; } = new();

        public PlaceholderMapping() { }

        public static PlaceholderMapping FromDefinition(PlaceholderDefinition def)
        {
            var mapping = new PlaceholderMapping
            {
                Placeholder = def.Name
            };

            switch (def.Type)
            {
                case "Composite":
                    mapping.SourceType = MappingSourceType.FromTable;
                    mapping.DisplayTemplate = def.DisplayTemplate;
                    mapping.TableName = def.TableName; // если нужно явно
                    break;

                case "FromTable":
                    mapping.SourceType = MappingSourceType.FromTable;
                    mapping.TableName = def.TableName;
                    mapping.DisplayTemplate = def.DisplayTemplate;
                    break;

                default:
                    mapping.SourceType = MappingSourceType.Manual;
                    break;
            }

            return mapping;
        }
    }
}

