using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Maui.Dispatching;
using EasySECv2.Models.DocumentTemplates;
using EasySECv2.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EasySECv2.ViewModels
{
    /// <summary>
    /// ViewModel для одного поля мэппинга с поддержкой фильтрации и отображения шаблона
    /// </summary>
    public partial class FieldMappingViewModel : ObservableObject
    {
        private readonly DatabaseService _db;
        private readonly PlaceholderMapping _mapping;

        // Хранит все записи для таблицы
        private List<object> _allItems = new();
        // Словарь справочников: источник -> список пар (Key, Value)
        private readonly Dictionary<string, List<LookupItem>> _lookups = new();

        /// <summary>Оригинальная модель мэппинга</summary>
        public PlaceholderMapping Mapping => _mapping;

        /// <summary>Флаг композитного маркера (с шаблоном)</summary>
        public bool IsComposite => !string.IsNullOrEmpty(Mapping.DisplayTemplate);

        public FieldMappingViewModel(PlaceholderMapping mapping, DatabaseService db)
        {
            _mapping = mapping;
            _db = db;

            // Связываем публичное свойство
            // (Mapping уже возвращает _mapping)

            // Инициализируем свойства из модели
            SourceType = mapping.SourceType;
            ManualValue = mapping.ManualValue;
            SelectedTable = mapping.SourceName ?? mapping.TableName ?? string.Empty;

            TableNames = new ObservableCollection<string>();
            SelectedItems = new ObservableCollection<object>();
            FilterOptions = new ObservableCollection<string>();
            FilteredItems = new ObservableCollection<DisplayItem>();

            // Загружаем список таблиц
            _ = LoadTableNamesAsync();

            // Если композитный или режим «из таблицы», грузим данные
            if ((IsComposite || IsFromTable) && !string.IsNullOrEmpty(SelectedTable))
            {
                _ = LoadTableItemsAsync();
            }
        }

        #region Общие свойства
        /// <summary>Маркер без скобок</summary>
        public string Placeholder => _mapping.Placeholder;

        /// <summary>Ручное или из таблицы</summary>
        private MappingSourceType _sourceType;
        public MappingSourceType SourceType
        {
            get => _sourceType;
            set
            {
                if (SetProperty(ref _sourceType, value))
                {
                    _mapping.SourceType = value;
                    OnPropertyChanged(nameof(IsManual));
                    OnPropertyChanged(nameof(IsFromTable));
                    if ((IsComposite || IsFromTable) && !string.IsNullOrEmpty(SelectedTable))
                        _ = LoadTableItemsAsync();
                }
            }
        }
        public bool IsManual => SourceType == MappingSourceType.Manual;
        public bool IsFromTable => SourceType == MappingSourceType.FromTable || IsComposite;

        /// <summary>Value for manual input</summary>
        private string _manualValue;
        public string ManualValue
        {
            get => _manualValue;
            set
            {
                if (SetProperty(ref _manualValue, value))
                    _mapping.ManualValue = value;
            }
        }
        #endregion

        #region Таблицы и выбор
        /// <summary>Список доступных таблиц</summary>
        public ObservableCollection<string> TableNames { get; }

        private string _selectedTable;
        public string SelectedTable
        {
            get => _selectedTable;
            set
            {
                if (SetProperty(ref _selectedTable, value))
                {
                    _mapping.SourceName = value;
                    if ((IsComposite || IsFromTable) && !string.IsNullOrEmpty(value))
                        _ = LoadTableItemsAsync();
                }
            }
        }

        /// <summary>Выбор нескольких элементов (batch)</summary>
        public ObservableCollection<object> SelectedItems { get; }
        #endregion

        #region Фильтрация и отображение
        /// <summary>Список доступных значений для фильтрации</summary>
        public ObservableCollection<string> FilterOptions { get; }

        [ObservableProperty]
        private string _selectedFilter;

        [ObservableProperty]
        private string _filterText;

        /// <summary>Результирующий список с текстом для отображения</summary>
        public ObservableCollection<DisplayItem> FilteredItems { get; }

        private DisplayItem _selectedItem;
        public DisplayItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }
        #endregion

        #region Загрузка данных и фильтрация
        private async Task LoadTableNamesAsync()
        {
            var names = await _db.GetAllTableNamesAsync().ConfigureAwait(false);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TableNames.Clear();
                foreach (var n in names)
                    TableNames.Add(n);
            });
        }

        private async Task LoadTableItemsAsync()
        {
            // 1. Загружаем справочник, если задан
            if (!string.IsNullOrEmpty(_mapping.FilterLookupSource)
                && !_lookups.ContainsKey(_mapping.FilterLookupSource))
            {
                var rawLookup = await _db.GetAllByTableNameAsync(_mapping.FilterLookupSource);
                var list = rawLookup.Select(r => new LookupItem(
                     r.GetType().GetProperty(_mapping.FilterLookupKey!)!.GetValue(r)!,
                     r.GetType().GetProperty(_mapping.FilterLookupValue!)!.GetValue(r)!.ToString()!
                )).ToList();
                _lookups[_mapping.FilterLookupSource] = list;
            }

            // 2. Загружаем все записи
            var items = await _db.GetAllByTableNameAsync(SelectedTable).ConfigureAwait(false);
            _allItems = items.ToList();

            // 3. Инициализируем FilterOptions
            if (!string.IsNullOrEmpty(_mapping.FilterLookupSource))
            {
                var vals = _lookups[_mapping.FilterLookupSource]
                    .Select(l => l.Value)
                    .Distinct()
                    .OrderBy(v => v);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FilterOptions.Clear();
                    foreach (var v in vals)
                        FilterOptions.Add(v);
                });
            }

            // 4. Применяем фильтр и поиск
            ApplyFilter();
        }

        partial void OnSelectedFilterChanged(string oldValue, string newValue) => ApplyFilter();
        partial void OnFilterTextChanged(string oldValue, string newValue) => ApplyFilter();

        private void ApplyFilter()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FilteredItems.Clear();
                IEnumerable<object> seq = _allItems;

                // Фильтрация по выбранному значению
                if (!string.IsNullOrEmpty(SelectedFilter)
                    && !string.IsNullOrEmpty(_mapping.FilterLookupSource))
                {
                    var lookup = _lookups[_mapping.FilterLookupSource]!;
                    var keys = lookup.Where(l => l.Value == SelectedFilter)
                                     .Select(l => l.Key)
                                     .ToHashSet();
                    seq = seq.Where(raw => keys.Contains(
                        raw.GetType().GetProperty(_mapping.FilterProperty!)!
                           .GetValue(raw)!));
                }

                // Текстовый поиск по DisplayTemplate или ToString()
                foreach (var raw in seq)
                {
                    var text = FormatDisplay(raw);
                    if (string.IsNullOrEmpty(FilterText)
                        || text.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                    {
                        FilteredItems.Add(new DisplayItem(raw, text));
                    }
                }
            });
        }

        private string FormatDisplay(object raw)
        {
            var tmpl = _mapping.DisplayTemplate;
            if (string.IsNullOrEmpty(tmpl))
                return raw.ToString()!;

            // Заменяем все {Prop} в шаблоне
            return Regex.Replace(tmpl, @"\{(\w+)\}", m =>
            {
                var propName = m.Groups[1].Value;
                // Подстановка из справочника по FilterLookupSource
                if (propName.EndsWith("Name") && !string.IsNullOrEmpty(_mapping.FilterLookupSource))
                {
                    var key = raw.GetType().GetProperty(_mapping.FilterProperty!)!
                                 .GetValue(raw)!;
                    var lookup = _lookups[_mapping.FilterLookupSource]!;
                    return lookup.First(l => l.Key.Equals(key)).Value;
                }
                var prop = raw.GetType().GetProperty(propName);
                return prop?.GetValue(raw)?.ToString() ?? string.Empty;
            });
        }
        #endregion
    }

    /// <summary>
    /// Пара ключ-значение для справочника
    /// </summary>
    public record LookupItem(object Key, string Value);

    /// <summary>
    /// Упаковка элемента и его отображаемого текста
    /// </summary>
    public class DisplayItem
    {
        public object Raw { get; }
        public string Text { get; }
        public DisplayItem(object raw, string text)
        {
            Raw = raw;
            Text = text;
        }
    }
}