using System;
using System.Collections;
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
    public class GenericEditViewModel<T> : INotifyPropertyChanged, IGenericEditViewModel
        where T : class, new()
    {
        readonly ICrudService<T> _service;

        public T Item { get; private set; }
        public bool IsNew { get; private set; }

        // Всё ещё нам удобно хранить именно список PropertyInfo,
        // но интерфейс требует IList, поэтому мы явно экспонируем его:
        private readonly ObservableCollection<PropertyInfo> _fields
            = new ObservableCollection<PropertyInfo>();
        public IList Fields => _fields;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<bool> CloseRequested;
        public event PropertyChangedEventHandler PropertyChanged;

        public GenericEditViewModel(ICrudService<T> service, T existing = null)
        {
            _service = service;
            Item = existing ?? new T();
            IsNew = existing == null;

            // собираем все [Editable]
            var props = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new { Prop = p, Attr = p.GetCustomAttribute<EditableAttribute>() })
                .Where(x => x.Attr != null)
                .OrderBy(x => x.Attr.Order)
                .Select(x => x.Prop);

            foreach (var pi in props)
                _fields.Add(pi);

            SaveCommand = new Command(async () =>
            {
                await _service.SaveAsync(Item).ConfigureAwait(false);
                CloseRequested?.Invoke(true);
            });

            CancelCommand = new Command(() =>
                CloseRequested?.Invoke(false));
        }

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

        void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
