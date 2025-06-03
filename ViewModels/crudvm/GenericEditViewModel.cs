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
using EasySECv2.Models;
using EasySECv2.Services;
using Microsoft.Maui.Controls;

namespace EasySECv2.ViewModels
{
    public partial class GenericEditViewModel<T> : INotifyPropertyChanged, IGenericEditViewModel
        where T : class, new()
    {
        readonly ICrudService<T> _service;

        public T Item { get; private set; }
        public bool IsNew { get; private set; }

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
    public partial class GenericEditViewModel<T>
    {
        // ---------- lookup-коллекции, которые ищет GenericEditPage ----------
        public ObservableCollection<Group> Groups { get; } = new();
        public ObservableCollection<Orientation> Orientations { get; } = new();
        public ObservableCollection<FormOfEducation> FormOfEducations { get; } = new();
        public ObservableCollection<Institute> Institutes { get; } = new();
        public ObservableCollection<Department> Departments { get; } = new();

        // ---------- DB для их заполнения ----------
        private readonly DatabaseService _db;

        // ---------- расширенный конструктор ----------
        public GenericEditViewModel(
            ICrudService<T> service,
            DatabaseService dbService,
            T existing = null) : this(service, existing)   // вызывает ваш «старый» ctor
        {
            _db = dbService;
            _ = LoadLookupsAsync();        // заполняем списки
        }

        // ---------- асинхронная подкачка справочников ----------
        private async Task LoadLookupsAsync()
        {
            // если редактируем не Student – можно ничего не подкачивать
            if (typeof(T) != typeof(Student)) return;

            // NB: методы у DatabaseService уже есть
            Groups.Clear();
            foreach (var g in await _db.GetAllGroupsAsync()) Groups.Add(g);

            Orientations.Clear();
            foreach (var o in await _db.GetAllOrientationsAsync()) Orientations.Add(o);

            FormOfEducations.Clear();
            foreach (var f in await _db.GetAllFormsOfEducationAsync()) FormOfEducations.Add(f);

            Institutes.Clear();
            foreach (var i in await _db.GetAllInstitutesAsync()) Institutes.Add(i);

            Departments.Clear();
            foreach (var d in await _db.GetAllDepartmentsAsync()) Departments.Add(d);

            // оповестим UI
            OnPropertyChanged(nameof(Groups));
            OnPropertyChanged(nameof(Orientations));
            OnPropertyChanged(nameof(FormOfEducations));
            OnPropertyChanged(nameof(Institutes));
            OnPropertyChanged(nameof(Departments));
        }
    }
}
