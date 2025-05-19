using System;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EasySECv2.Services
{
    /// <summary>
    /// Обобщённый интерфейс для любых VM-редакторов (GenericEditViewModel<T>).
    /// </summary>
    public interface IGenericEditViewModel
    {
        /// <summary>
        /// Загружает уже существующий объект по id.
        /// </summary>
        Task LoadExistingAsync(long id);

        /// <summary>
        /// Создаём новую запись vs редактируем существующую
        /// </summary>
        bool IsNew { get; }

        /// <summary>
        /// Список PropertyInfo, помеченных [Editable], для динамической генерации полей.
        /// </summary>
        IList Fields { get; }

        /// <summary>Команда «Сохранить»</summary>
        ICommand SaveCommand { get; }

        /// <summary>Команда «Отменить» (закрыть)</summary>
        ICommand CancelCommand { get; }

        /// <summary>
        /// Вызывается, когда форма должна сама закрыться (true — сохранено, false — отмена).
        /// </summary>
        event Action<bool> CloseRequested;
    }
}
