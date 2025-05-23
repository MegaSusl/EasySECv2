using EasySECv2.Models;
using EasySECv2.Models.DocumentTemplates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Services
{
    public interface ITemplateService
    {
        Task<IEnumerable<DocumentTemplate>> GetTemplatesAsync(string pageKey);
        Task AddOrUpdateAsync(string pageKey, string fileName, Stream docxStream);
        Task UpdateAsync(Guid templateId, string fileName, Stream docxStream);  // ← новое
        Task DeleteAsync(Guid templateId);

        /// <summary>
        /// Возвращает конфигурацию шаблона по его ключу
        /// </summary>
        Task<TemplateConfig?> GetTemplateConfigAsync(string templateKey);

        /// <summary>
        /// Извлекает список мэппингов (PlaceholderMapping) для шаблона
        /// </summary>
        /// <param name="templateKey">Ключ шаблона (TemplateKey из JSON-конфига)</param>
        /// <param name="templatePath">Путь к файлу .docx</param>
        Task<List<PlaceholderMapping>> GetMappingsAsync(string templateKey, string templatePath);
    }

}
