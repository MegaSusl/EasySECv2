using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using EasySECv2.Models;
using EasySECv2.Services;
using Microsoft.Maui.Controls;

namespace EasySECv2.ViewModels
{
    public class DepartmentViewModel : INotifyPropertyChanged
    {
        readonly DatabaseService _db;

        // Вся коллекция
        ObservableCollection<Department> _allDepartments = new();
        // Отфильтрованная для UI
        public ObservableCollection<Department> FilteredDepartments { get; } = new();

        string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery == value) return;
                _searchQuery = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public Department SelectedDepartment { get; set; }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public DepartmentViewModel(DatabaseService db)
        {
            _db = db;

            AddCommand = new Command(OnAdd);
            EditCommand = new Command<Department>(OnEdit);
            DeleteCommand = new Command<Department>(OnDelete);
            RefreshCommand = new Command(async () => await LoadAsync());

            // начальная загрузка
            _ = LoadAsync();
        }

        async System.Threading.Tasks.Task LoadAsync()
        {
            _allDepartments = new ObservableCollection<Department>(await _db.GetAllDepartmentsAsync());
            ApplyFilter();
        }

        void ApplyFilter()
        {
            FilteredDepartments.Clear();
            var list = string.IsNullOrWhiteSpace(SearchQuery)
                ? _allDepartments
                : _allDepartments.Where(d => d.name?.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase) == true);
            foreach (var d in list)
                FilteredDepartments.Add(d);
        }

        void OnAdd()
        {
            var newDept = new Department { name = "" };
            // сразу сохраняем пустую и открываем в «редакторе»
            _ = _db.SaveDepartmentAsync(newDept)
             .ContinueWith(_ => _ = LoadAsync(),
                           System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }

        async void OnEdit(Department dept)
        {
            if (dept == null) return;
            // например, можно показать окно ввода (здесь просто сохраняем):
            await _db.SaveDepartmentAsync(dept);
            await LoadAsync();
        }

        async void OnDelete(Department dept)
        {
            if (dept == null) return;
            if (!await Application.Current.MainPage.DisplayAlert(
                    "Удалить?", $"Точно удалить «{dept.name}»?", "Да", "Отмена"))
                return;
            await _db.DeleteDepartmentAsync(dept);
            await LoadAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
