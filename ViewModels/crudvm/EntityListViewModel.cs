using EasySECv2.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EasySECv2.ViewModels
{
    /// <summary>
    /// Универсальный VM для CRUD-страниц, где у сущности есть id и name.
    /// </summary>
    public class EntityListViewModel<T> : INotifyPropertyChanged
        where T : class, new()
    {
        readonly DatabaseService _db;
        readonly Func<DatabaseService, Task<ObservableCollection<T>>> _loadFunc;
        readonly Func<DatabaseService, T, Task> _saveFunc;
        readonly Func<DatabaseService, T, Task> _deleteFunc;

        public ObservableCollection<T> Items { get; } = new();

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public EntityListViewModel(
            DatabaseService db,
            Func<DatabaseService, Task<ObservableCollection<T>>> loadFunc,
            Func<DatabaseService, T, Task> saveFunc,
            Func<DatabaseService, T, Task> deleteFunc)
        {
            _db = db;
            _loadFunc = loadFunc;
            _saveFunc = saveFunc;
            _deleteFunc = deleteFunc;

            AddCommand = new Command(OnAdd);
            EditCommand = new Command<T>(async item => await OnEdit(item));
            DeleteCommand = new Command<T>(async item => await OnDelete(item));
            RefreshCommand = new Command(async () => await LoadItems());

            // сразу загрузим данные
            _ = LoadItems();
        }

        async Task LoadItems()
        {
            Items.Clear();
            var list = await _loadFunc(_db);
            foreach (var x in list)
            {
                System.Diagnostics.Debug.WriteLine($"Загрузка: {x.GetType().Name} - {x.GetType().GetProperty("name")?.GetValue(x)}");
                Items.Add(x);
            }
            System.Diagnostics.Debug.WriteLine($"Items.Count = {Items.Count}");
        }

        void OnAdd()
        {
            // Создаём новую пустую сущность и сразу запускаем редактирование
            var item = new T();
            _ = OnEdit(item);
        }

        async Task OnEdit(T item)
        {
            // Здесь можно вынести логику показа модального диалога или навигации
            // Например, Shell.Current.GoToAsync($"Edit{typeof(T).Name}?id={id}")
            // Но для простоты пока просто сохраняем пустую запись
            await _saveFunc(_db, item);
            await LoadItems();
        }

        async Task OnDelete(T item)
        {
            bool ok = await Application.Current.MainPage.DisplayAlert(
                "Удалить запись",
                "Вы уверены?",
                "Да", "Нет");
            if (!ok) return;

            await _deleteFunc(_db, item);
            await LoadItems();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
