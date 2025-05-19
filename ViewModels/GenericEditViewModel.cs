using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using EasySECv2.Attributes;
using EasySECv2.Services;
using Microsoft.Maui.Controls;

namespace EasySECv2.ViewModels
{
    /// <summary>
    /// Универсальный VM для создания/редактирования сущности T.
    /// Поддерживает загрузку существующей записи по id.
    /// </summary>
    public class GenericEditViewModel<T> : INotifyPropertyChanged, ILoadableViewModel
        where T : class, new()
    {
        readonly ICrudService<T> _service;

        /// <summary>Текущая сущность, отображаемая в форме.</summary>
        public T Item { get; private set; }

        /// <summary>Признак: создаём новую или редактируем существующую.</summary>
        public bool IsNew { get; private set; }

        /// <summary>Список свойств, помеченных [Editable], для динамической генерации полей.</summary>
        public ObservableCollection<PropertyInfo> Fields { get; }
            = new ObservableCollection<PropertyInfo>();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>Событие для закрытия страницы (true — сохранено, false — отмена).</summary>
        public event Action<bool> CloseRequested;
        public event PropertyChangedEventHandler PropertyChanged;

        public GenericEditViewModel(ICrudService<T> service, T existing = null)
        {
            _service = service;
            Item = existing ?? new T();
            IsNew = existing == null;

            // собираем все свойства с атрибутом [Editable]
            var props = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new { Prop = p, Attr = p.GetCustomAttribute<EditableAttribute>(false) })
                .Where(x => x.Attr != null)
                .OrderBy(x => x.Attr.Order)
                .Select(x => x.Prop);

            foreach (var pi in props)
                Fields.Add(pi);

            SaveCommand = new Command(async () =>
            {
                await _service.SaveAsync(Item).ConfigureAwait(false);
                CloseRequested?.Invoke(true);
            });
            CancelCommand = new Command(() => CloseRequested?.Invoke(false));
        }

        /// <summary>
        /// Загружает существующую запись по её первичному ключу и переключает IsNew в false.
        /// </summary>
        public async Task LoadExistingAsync(long id)
        {
            var existing = await _service.FindByKeyAsync(id).ConfigureAwait(false);
            if (existing != null)
            {
                Item = existing;
                IsNew = false;
                OnPropertyChanged(nameof(Item));
                OnPropertyChanged(nameof(IsNew));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
