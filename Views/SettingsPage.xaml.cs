using EasySECv2.Services;
using EasySECv2.Models;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace EasySECv2.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly DatabaseService _db;
        private readonly IPageSettingsService _pageSettings;
        private readonly ITemplateService _templateService;

        private readonly List<string> _pageKeys = new();
        private static readonly List<string> PagesForGeneration = new()
        {
            nameof(SecCompositionPage),
            // Добавить другие страницы по мере необходимости
        };

        public ObservableCollection<PlaceholderMapping> GlobalMappings { get; } = new();
        public List<MappingSourceType> SourceTypes { get; } = Enum.GetValues(typeof(MappingSourceType)).Cast<MappingSourceType>().ToList();

        public SettingsPage(DatabaseService dbService, IPageSettingsService pageSettings, ITemplateService templateService)
        {
            InitializeComponent();
            _db = dbService;
            _pageSettings = pageSettings;
            _templateService = templateService;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var tables = await _db.GetAllTableNamesAsync();
            TablesPicker.ItemsSource = tables;

            _pageKeys.Clear();
            _pageKeys.AddRange(PagesForGeneration);
            PagePicker.ItemsSource = _pageKeys;

            if (_pageKeys.Any())
                PagePicker.SelectedIndex = 0;

            await LoadMappingsAsync();
        }

        private async void OnPageSelected(object sender, EventArgs e)
        {
            if (PagePicker.SelectedIndex < 0) return;

            var key = _pageKeys[PagePicker.SelectedIndex];
            var settings = await _pageSettings.GetSettingsAsync(key);
            BatchSwitch.IsToggled = settings.AllowBatch;
        }

        private async void OnSavePageSettingsClicked(object sender, EventArgs e)
        {
            if (PagePicker.SelectedIndex < 0) return;

            var key = _pageKeys[PagePicker.SelectedIndex];
            var settings = new PageTemplateSettings
            {
                PageKey = key,
                AllowBatch = BatchSwitch.IsToggled
            };
            await _pageSettings.SaveSettingsAsync(settings);

            await DisplayAlert("Сохранено", "Настройки сохранены.", "OK");
        }

        private async void OnDeleteAllClicked(object sender, EventArgs e)
        {
            var table = TablesPicker.SelectedItem as string;
            if (string.IsNullOrEmpty(table))
            {
                await DisplayAlert("Ошибка", "Сначала выберите таблицу", "OK");
                return;
            }

            bool confirm = await DisplayAlert(
                "Подтвердите",
                $"Удалить все записи из таблицы «{table}»?",
                "Да", "Отмена");
            if (!confirm) return;

            try
            {
                int deleted = await _db.DeleteAllFromTableAsync(table);
                await DisplayAlert("Готово", $"Удалено примерно {deleted} строк из «{table}».", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnSeedOrientationsClicked(object sender, EventArgs e)
        {
            await _db.SeedOrientationsIfNeededAsync();
            await DisplayAlert("Готово", "Таблица «Orientation» обновлена из JSON.", "OK");
        }

        private async Task LoadMappingsAsync()
        {
            GlobalMappings.Clear();
            var templates = await _templateService.GetTemplatesAsync("batch-certificate");
            var mappings = templates.SelectMany(t => t.Mappings).DistinctBy(m => m.Placeholder);
            foreach (var m in mappings)
                GlobalMappings.Add(new PlaceholderMapping { Placeholder = m.Placeholder, SourceType = m.SourceType, Property = m.Property });
        }

        private async void OnSaveMappingsClicked(object sender, EventArgs e)
        {
            var batchTemplates = await _templateService.GetTemplatesAsync("batch-certificate");

            foreach (var tpl in batchTemplates)
            {
                tpl.Mappings = GlobalMappings
                    .Select(m => new PlaceholderMapping
                    {
                        Placeholder = m.Placeholder,
                        SourceType = m.SourceType,
                        Property = m.Property
                    }).ToList();
            }

            var allTemplates = await _templateService.GetAllTemplatesAsync();

            foreach (var t in allTemplates)
            {
                var updated = batchTemplates.FirstOrDefault(x => x.LocalPath == t.LocalPath);
                if (updated != null)
                {
                    t.Mappings = updated.Mappings;
                }
            }

            await _templateService.SaveAllTemplatesAsync(allTemplates);

            await DisplayAlert("Сохранено", "Маркеры обновлены.", "OK");
        }

    }
}