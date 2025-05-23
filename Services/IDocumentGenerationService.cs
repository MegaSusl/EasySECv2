using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySECv2.Models.DocumentTemplates;

namespace EasySECv2.Services
{
    public interface IDocumentGenerationService
    {
        /// <summary>
        /// Сгенерировать один документ из шаблона.
        /// </summary>
        Task GenerateAsync(DocumentTemplate tpl,
                           IDictionary<string, object?> values,
                           string outputFolder);
        Task GenerateBatchAsync(DocumentTemplate tpl,
                             IEnumerable<IDictionary<string, object?>> items,
                             string outputFolder);
    }
}
