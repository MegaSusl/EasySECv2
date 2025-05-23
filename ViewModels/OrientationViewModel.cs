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
    public class OrientationViewModel : INotifyPropertyChanged
    {
        readonly ICrudService<Orientation> _service;

        public ObservableCollection<Orientation> AllItems { get; } = new();
        public ObservableCollection<Orientation> Filtered { get; } = new();

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

        Orientation _selectedItem;
        public Orientation SelectedItem
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

        public OrientationViewModel(ICrudService<Orientation> service)
        {
            _service = service;

            AddCommand = new Command(OnAdd);
            EditCommand = new Command<Orientation>(OnEdit);
            DeleteCommand = new Command<Orientation>(OnDelete);
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
            foreach (var item in AllItems.Where(o =>
                         string.IsNullOrWhiteSpace(SearchQuery)
                         || (o.name?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (o.code?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false)))
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
            Shell.Current.GoToAsync(nameof(EditOrientationPage));
        }

        void OnEdit(Orientation item)
        {
            if (item == null) return;
            Shell.Current.GoToAsync($"{nameof(EditOrientationPage)}?id={item.id}");
        }

        async void OnDelete(Orientation item)
        {
            if (item == null) return;
            bool ok = await Application.Current.MainPage.DisplayAlert(
                "Удалить запись",
                $"Удалить направление «{item.name} ({item.code})»?",
                "Да", "Нет");
            if (!ok) return;

            await _service.DeleteAsync(item);
            await LoadData();
        }
    }
}
