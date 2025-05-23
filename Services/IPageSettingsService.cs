using EasySECv2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Services
{
    public interface IPageSettingsService
    {
        /// <summary>
        /// Возвращает настройки для данной страницы (PageKey).
        /// Если для key ничего не найдено, возвращает AllowBatch=false.
        /// </summary>
        Task<PageTemplateSettings> GetSettingsAsync(string pageKey);

        /// <summary>
        /// Сохраняет (или обновляет) настройки для pageKey.
        /// </summary>
        Task SaveSettingsAsync(PageTemplateSettings settings);
    }
}