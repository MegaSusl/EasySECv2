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
    public class InstituteViewModel : INotifyPropertyChanged
    {
        readonly ICrudService<Institute> _service;

        public ObservableCollection<Institute> AllItems { get; } = new();
        public ObservableCollection<Institute> Filtered { get; } = new();

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

        Institute _selectedItem;
        public Institute SelectedItem
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

        public InstituteViewModel(ICrudService<Institute> service)
        {
            _service = service;

            AddCommand = new Command(OnAdd);
            EditCommand = new Command<Institute>(OnEdit);
            DeleteCommand = new Command<Institute>(OnDelete);
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
            foreach (var item in AllItems
                .Where(i => string.IsNullOrWhiteSpace(SearchQuery)
                         || i.name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
                         || (i.shortName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false)))
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
            Shell.Current.GoToAsync(nameof(EditInstitutePage));
        }

        void OnEdit(Institute item)
        {
            if (item == null) return;
            Shell.Current.GoToAsync($"{nameof(EditInstitutePage)}?id={item.id}");
        }

        async void OnDelete(Institute item)
        {
            if (item == null) return;
            bool ok = await Application.Current.MainPage.DisplayAlert(
                "Удалить запись",
                $"Удалить институт «{item.name}»?",
                "Да", "Нет");
            if (!ok) return;

            await _service.DeleteAsync(item);
            await LoadData();
        }
    }
}
