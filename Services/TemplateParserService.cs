using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xceed.Words.NET;

namespace EasySECv2.Services
{
    public class TemplateParserService : ITemplateParserService
    {
        private static readonly Regex PlaceholderRx = new(@"\[([A-Za-zА-Яа-яЁё0-9_]+)\]", RegexOptions.Compiled);

        public IEnumerable<string> ExtractPlaceholders(string docxPath)
        {
            using var doc = DocX.Load(docxPath);
            var text = doc.Text;
            var matches = PlaceholderRx.Matches(text);
            var set = new HashSet<string>();
            foreach (Match m in matches)
                set.Add(m.Groups[1].Value);
            return set;
        }
    }
}
