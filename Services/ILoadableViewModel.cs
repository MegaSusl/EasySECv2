using System.Threading.Tasks;

namespace EasySECv2.Services
{
    /// <summary>
    /// Позволяет VM подгрузить существующую запись по ключу.
    /// </summary>
    public interface ILoadableViewModel
    {
        Task LoadExistingAsync(long id);
    }
}
