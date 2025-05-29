using EasySECv2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Services
{
    public interface IDocumentGenerationService
    {
        Task GenerateDocumentsAsync(DocumentTemplate template, IEnumerable<object> dataContexts, Dictionary<string, string> manualInputs, string outputDir);
        Task GenerateTabularAsync(DocumentTemplate template, Group group, List<Student> students, Dictionary<string, string> manualInputs, string outputDir);
        Task GenerateFamiliarizationAsync(DocumentTemplate template, Group group, IEnumerable<Student> students, DateTime orderDate, string orderNumber, string outputDir);
    }
}
