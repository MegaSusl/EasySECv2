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
    public class OrientationViewModel : INotifyPropertyChanged
    {
        readonly DatabaseService _db;

        // вся коллекция из БД
        ObservableCollection<Orientation> _all = new();

        // та, что отображается
        public ObservableCollection<Orientation> Filtered { get; } = new();

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

        public Orientation SelectedItem { get; set; }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }

        public OrientationViewModel(DatabaseService db)
        {
            _db = db;

            AddCommand = new Command(OnAdd);
            EditCommand = new Command<Orientation>(OnEdit);
            DeleteCommand = new Command<Orientation>(OnDelete);
            RefreshCommand = new Command(async () => await LoadAsync());

            // загрузим сразу при старте
            _ = LoadAsync();
        }

        async System.Threading.Tasks.Task LoadAsync()
        {
            _all = new ObservableCollection<Orientation>(
                await _db.GetAllOrientationsAsync());
            ApplyFilter();
        }

        void ApplyFilter()
        {
            Filtered.Clear();
            var items = string.IsNullOrWhiteSpace(SearchQuery)
                ? _all
                : _all.Where(o =>
                    (o.name?.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase) ?? false)
                 || (o.code?.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase) ?? false));
            foreach (var o in items)
                Filtered.Add(o);
        }

        void OnAdd()
        {
            var tmp = new Orientation { name = "", code = "" };
            _ = _db.SaveOrientationAsync(tmp)
                 .ContinueWith(_ => LoadAsync(),
                               System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }

        async void OnEdit(Orientation o)
        {
            if (o == null) return;
            await _db.SaveOrientationAsync(o);
            await LoadAsync();
        }

        async void OnDelete(Orientation o)
        {
            if (o == null) return;
            bool ok = await Application.Current.MainPage.DisplayAlert(
                "Удалить?", $"Точно удалить «{o.name} ({o.code})»?", "Да", "Отмена");
            if (!ok) return;
            await _db.DeleteOrientationAsync(o);
            await LoadAsync();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
