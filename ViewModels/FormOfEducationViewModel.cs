using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EasySECv2.Models;
using EasySECv2.Services;
using EasySECv2.Views;
using Microsoft.Maui.Controls;

namespace EasySECv2.ViewModels
{
    public class FormOfEducationViewModel : INotifyPropertyChanged
    {
        readonly ICrudService<FormOfEducation> _service;

        public ObservableCollection<FormOfEducation> AllItems { get; }
            = new ObservableCollection<FormOfEducation>();

        public ObservableCollection<FormOfEducation> Filtered { get; }
            = new ObservableCollection<FormOfEducation>();

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

        FormOfEducation _selectedItem;
        public FormOfEducation SelectedItem
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

        public FormOfEducationViewModel(ICrudService<FormOfEducation> service)
        {
            _service = service;

            AddCommand = new Command(OnAdd);
            EditCommand = new Command<FormOfEducation>(OnEdit);
            DeleteCommand = new Command<FormOfEducation>(OnDelete);
            RefreshCommand = new Command(async () => await LoadData());

            // сразу заполняем
            _ = LoadData();
        }

        async Task LoadData()
        {
            var list = await _service.Query.ToListAsync();
            AllItems.Clear();
            foreach (var item in list) AllItems.Add(item);
            ApplyFilter();
        }

        void ApplyFilter()
        {
            Filtered.Clear();
            foreach (var item in AllItems
                .Where(i => string.IsNullOrWhiteSpace(SearchQuery)
                         || i.name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)))
            {
                Filtered.Add(item);
            }
        }

        void OnAdd()
        {
            // откроем GenericEditPage для новой Item
            Shell.Current.GoToAsync(nameof(GenericEditPage));
        }

        void OnEdit(FormOfEducation item)
        {
            if (item == null) return;
            // передать id — GenericEditPage подхватит его и загрузит «existing»
            Shell.Current.GoToAsync($"{nameof(GenericEditPage)}?id={item.id}");
        }

        async void OnDelete(FormOfEducation item)
        {
            if (item == null) return;
            bool ok = await Application.Current.MainPage.DisplayAlert(
                "Удалить запись",
                $"Удалить форму «{item.name}»?",
                "Да", "Нет");
            if (!ok) return;

            await _service.DeleteAsync(item);
            await LoadData();
        }
    }
}
