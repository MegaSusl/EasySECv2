using EasySECv2.Services;
using EasySECv2.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using EasySECv2.Views;

namespace EasySECv2.ViewModels
{
    public class StudentsViewModel : INotifyPropertyChanged
    {
        private readonly ICrudService<Student> _studentService;
        private readonly DatabaseService _dbService;
        private readonly ExcelAdapter _excelAdapter;

        public ObservableCollection<Student> Students { get; } = new();
        public ObservableCollection<Group> Groups { get; } = new();
        public ObservableCollection<Orientation> Orientations { get; } = new();
        public ObservableCollection<string> SortOptions { get; } = new()
        {
            "По ФИО A→Я",
            "По ФИО Я→A",
            "По ID"
        };

        // Фильтры и текущее выделение
        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery == value) return;
                _searchQuery = value;
                OnPropertyChanged();
                RefreshCommand.Execute(null);
            }
        }

        private Group _selectedGroup;
        public Group SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (_selectedGroup == value) return;
                _selectedGroup = value;
                OnPropertyChanged();
                RefreshCommand.Execute(null);
            }
        }

        private Orientation _selectedOrientation;
        public Orientation SelectedOrientation
        {
            get => _selectedOrientation;
            set
            {
                if (_selectedOrientation == value) return;
                _selectedOrientation = value;
                OnPropertyChanged();
                RefreshCommand.Execute(null);
            }
        }

        private string _selectedSortOption;
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (_selectedSortOption == value) return;
                _selectedSortOption = value;
                OnPropertyChanged();
                RefreshCommand.Execute(null);
            }
        }

        private Student _selectedStudent;
        public Student SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                if (_selectedStudent == value) return;
                _selectedStudent = value;
                OnPropertyChanged();
            }
        }

        // Команды
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand RefreshCommand { get; }

        public StudentsViewModel(ICrudService<Student> studentService, DatabaseService dbService, ExcelAdapter excelAdapter)
        {
            _dbService = dbService;
            _excelAdapter = excelAdapter;
            _studentService = studentService;

            AddCommand = new Command(OnAdd);
            EditCommand = new Command<Student>(OnEdit);
            DeleteCommand = new Command<Student>(OnDelete);
            ImportCommand = new Command(async () => await OnImport());
            RefreshCommand = new Command(async () => await LoadData());

            // Инициализация фильтров и данных
            _ = LoadFiltersAsync();
            _ = LoadData();
        }

        private async Task LoadFiltersAsync()
        {
            var groups = await _dbService.GetAllGroupsAsync();
            Groups.Clear();
            Groups.Add(null);              // "Все группы"
            foreach (var g in groups) Groups.Add(g);

            var orients = await _dbService.GetAllOrientationsAsync();
            Orientations.Clear();
            Orientations.Add(null);        // "Все направления"
            foreach (var o in orients) Orientations.Add(o);
        }

        private async Task LoadData()
        {
            // Получаем все группы один раз
            var allGroups = await _dbService.GetAllGroupsAsync();
            var groupDict = allGroups.ToDictionary(g => g.id, g => g.name);
            // Получаем всех студентов
            var list = await _dbService.GetStudentsAsync();
            System.Diagnostics.Debug.WriteLine($"Загрузка: {list.Count} студентов");
            // Фильтрация
            if (!string.IsNullOrWhiteSpace(SearchQuery))
                list = list.Where(s =>
                    s.FullName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
                    || s.id.ToString().Contains(SearchQuery))
                    .ToList();

            if (SelectedGroup != null)
                list = list.Where(s => s.groupId == SelectedGroup.id).ToList();

            if (SelectedOrientation != null)
                list = list.Where(s => s.orientation == SelectedOrientation.id).ToList();

            // Сортировка
            list = SelectedSortOption switch
            {
                "По ФИО Я→A" => list.OrderByDescending(s => s.FullName).ToList(),
                "По ID" => list.OrderBy(s => s.id).ToList(),
                _ => list.OrderBy(s => s.FullName).ToList()
            };

            // Обновляем коллекцию
            Students.Clear();
            foreach (var s in list)
            {
                // Подменяем groupId на название группы
                if (groupDict.TryGetValue(s.groupId, out var groupName))
                    s.GroupName = groupName;
                else
                    s.GroupName = "—";
                System.Diagnostics.Debug.WriteLine($"Группа: {s.GroupName}");
                Students.Add(s);
            }
        }
        async void OnAdd()
        {
            // теперь действительно ждём, пока Shell выполнит навигацию
            await Shell.Current.GoToAsync(nameof(GenericEditPage));
        }
        async void OnEdit(Student s)
        {
            if (s == null) return;
            await Shell.Current.GoToAsync($"{nameof(GenericEditPage)}?id={s.id}");
        }

        private async void OnDelete(Student s)
        {
            if (s == null) return;
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Удалить студента",
                $"Вы точно хотите удалить {s.FullName}?",
                "Да", "Нет");
            if (!confirm) return;

            await _dbService.DeleteStudentAsync(s);
            await LoadData();
        }

        private async Task OnImport()
        {
            try
            {
                var pick = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите Excel-файл",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.WinUI,   new[] { ".xls", ".xlsx" } },
                            { DevicePlatform.Android, new[] { "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                            { DevicePlatform.iOS,     new[] { "org.openxmlformats.spreadsheetml.sheet" } }
                        })
                });

                if (pick == null) return;

                var newStudents = await _excelAdapter.ReadStudentsFromExcel(pick.FullPath, _dbService);
                foreach (var st in newStudents)
                    await _dbService.SaveStudentAsync(st);

                await LoadData();
                await Application.Current.MainPage.DisplayAlert("Импорт", $"Добавлено {newStudents.Count} студентов", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка импорта", ex.Message, "OK");
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        #endregion
    }
}
