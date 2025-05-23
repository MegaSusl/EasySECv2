using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Services
{
    public interface IFolderPickerService
    {
        /// <returns>Путь к выбранной папке или null, если пользователь отменил.</returns>
        Task<string?> PickFolderAsync();
    }
}
