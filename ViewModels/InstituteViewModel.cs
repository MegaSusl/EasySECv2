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
    public class InstituteViewModel : INotifyPropertyChanged
    {
        readonly DatabaseService _db;
        ObservableCollection<Institute> _all = new();
        public ObservableCollection<Institute> Filtered { get; } = new();

        string _search;
        public string SearchQuery
        {
            get => _search;
            set
            {
                if (_search == value) return;
                _search = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public Institute SelectedItem { get; set; }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public InstituteViewModel(DatabaseService db)
        {
            _db = db;
            AddCommand = new Command(OnAdd);
            EditCommand = new Command<Institute>(OnEdit);
            DeleteCommand = new Command<Institute>(OnDelete);
            RefreshCommand = new Command(async () => await LoadAsync());
            _ = LoadAsync();
        }

        async System.Threading.Tasks.Task LoadAsync()
        {
            _all = new ObservableCollection<Institute>(
                await _db.GetAllInstitutesAsync());
            ApplyFilter();
        }

        void ApplyFilter()
        {
            Filtered.Clear();
            var list = string.IsNullOrWhiteSpace(SearchQuery)
                ? _all
                : _all.Where(i =>
                    i.name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase)
                 || (i.shortName?.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase) ?? false));
            foreach (var i in list) Filtered.Add(i);
        }

        void OnAdd()
        {
            var tmp = new Institute { name = "", shortName = "" };
            _ = _db.SaveInstituteAsync(tmp)
                 .ContinueWith(_ => LoadAsync(),
                               System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }

        async void OnEdit(Institute inst)
        {
            if (inst == null) return;
            await _db.SaveInstituteAsync(inst);
            await LoadAsync();
        }

        async void OnDelete(Institute inst)
        {
            if (inst == null) return;
            if (!await Application.Current.MainPage.DisplayAlert(
                    "Удалить?", $"Удалить институт «{inst.name}»?", "Да", "Нет"))
                return;
            await _db.DeleteInstituteAsync(inst);
            await LoadAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string p = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
