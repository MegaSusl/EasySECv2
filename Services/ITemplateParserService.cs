using System.Collections.Generic;

namespace EasySECv2.Services
{
    public interface ITemplateParserService
    {
        /// <summary>
        /// Возвращает все уникальные маркеры вида [МАРКЕР] из переданного docx-файла.
        /// </summary>
        IEnumerable<string> ExtractPlaceholders(string docxPath);
    }
}
