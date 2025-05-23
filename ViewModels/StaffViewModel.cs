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
    public class StaffViewModel : INotifyPropertyChanged
    {
        readonly ICrudService<Staff> _service;

        public ObservableCollection<Staff> AllItems { get; } = new();
        public ObservableCollection<Staff> Filtered { get; } = new();

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

        Staff _selectedItem;
        public Staff SelectedItem
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

        public StaffViewModel(ICrudService<Staff> service)
        {
            _service = service;

            AddCommand = new Command(OnAdd);
            EditCommand = new Command<Staff>(OnEdit);
            DeleteCommand = new Command<Staff>(OnDelete);
            RefreshCommand = new Command(async () => await LoadData());

            _ = LoadData();
        }

        async Task LoadData()
        {
            var list = await _service.Query.ToListAsync();
            AllItems.Clear();
            foreach (var item in list)
                AllItems.Add(item);
            ApplyFilter();
        }

        void ApplyFilter()
        {
            Filtered.Clear();
            foreach (var item in AllItems.Where(s =>
                         string.IsNullOrWhiteSpace(SearchQuery)
                         || ($"{s.surname} {s.name} {s.middleName}".Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))))
                Filtered.Add(item);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(Filtered));
            });
        }

        void OnAdd()
        {
            Shell.Current.GoToAsync(nameof(EditStaffPage));
        }

        void OnEdit(Staff item)
        {
            if (item == null) return;
            Shell.Current.GoToAsync($"{nameof(EditStaffPage)}?id={item.id}");
        }

        async void OnDelete(Staff item)
        {
            if (item == null) return;
            bool ok = await Application.Current.MainPage.DisplayAlert(
                "Удалить запись",
                $"Удалить сотрудника «{item.surname} {item.name}»?",
                "Да", "Нет");
            if (!ok) return;

            await _service.DeleteAsync(item);
            await LoadData();
        }
    }
}
