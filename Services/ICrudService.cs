using SQLite;
using System.Threading.Tasks;

namespace EasySECv2.Services
{
    public interface ICrudService<T>
        where T : class, new()    // <-- добавляем ограничение
    {
        /// <summary>
        /// Асинхронный запрос к таблице T
        /// </summary>
        AsyncTableQuery<T> Query { get; }

        /// <summary>
        /// Сохраняет (insert/update) сущность
        /// </summary>
        Task SaveAsync(T item);

        /// <summary>
        /// Удаляет сущность
        /// </summary>
        Task DeleteAsync(T item);
        Task<T> FindByKeyAsync(object pk);
    }
}
