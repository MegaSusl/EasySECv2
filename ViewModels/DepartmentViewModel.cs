using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using EasySECv2.Models;
using EasySECv2.Services;
using EasySECv2.Views;
using Microsoft.Maui.Controls;

namespace EasySECv2.ViewModels
{
    public class DepartmentViewModel : INotifyPropertyChanged
    {
        readonly ICrudService<Department> _service;

        public ObservableCollection<Department> AllItems { get; } = new();
        public ObservableCollection<Department> Filtered { get; } = new();

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

        Department _selectedItem;
        public Department SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public DepartmentViewModel(ICrudService<Department> service)
        {
            _service = service;

            AddCommand = new Command(OnAdd);
            EditCommand = new Command<Department>(OnEdit);
            DeleteCommand = new Command<Department>(OnDelete);
            RefreshCommand = new Command(async () => await LoadData());

            _ = LoadData();
        }

        async Task LoadData()
        {
            var list = await _service.Query.ToListAsync();
            AllItems.Clear();
            foreach (var item in list)
            {
                System.Diagnostics.Debug.WriteLine($"Загрузка: {item.name}");
                AllItems.Add(item);
            }
            ApplyFilter();
        }

        void ApplyFilter()
        {
            Filtered.Clear();
            foreach (var item in AllItems.Where(d =>
                         string.IsNullOrWhiteSpace(SearchQuery)
                         || (d.name?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false)))
            {
                Filtered.Add(item);
            }
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(Filtered));
            });
        }

        void OnAdd()
        {
            Shell.Current.GoToAsync(nameof(EditDepartmentPage));
        }

        void OnEdit(Department item)
        {
            if (item == null) return;
            Shell.Current.GoToAsync($"{nameof(EditDepartmentPage)}?id={item.id}");
        }

        async void OnDelete(Department item)
        {
            if (item == null) return;
            bool ok = await Application.Current.MainPage.DisplayAlert(
                "Удалить запись",
                $"Удалить кафедру «{item.name}»?",
                "Да", "Нет");
            if (!ok) return;

            await _service.DeleteAsync(item);
            await LoadData();
        }
    }
}
