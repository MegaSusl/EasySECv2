using EasySECv2.Services;
using EasySECv2.Models;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EasySECv2.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly DatabaseService _db;
        private readonly IPageSettingsService _pageSettings;
        private readonly ITemplateService _templateService;

        public List<string> PageKeys { get; } = new() { "batch-certificate", "protocol-vkr", "familiarization", "sec_composition", "sec_member_replace", "sec_secretary_replace" };

        private string selectedPageKey = "sec_composition";
        public string SelectedPageKey
        {
            get => selectedPageKey;
            set
            {
                if (selectedPageKey != value)
                {
                    selectedPageKey = value;
                    LoadMappingsAsync(); // вызываем вручную при изменении
                }
            }
        }


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
            TablesPicker.ItemsSource = await _db.GetAllTableNamesAsync();
            PagePicker.ItemsSource = PageKeys;
            PagePicker.SelectedItem = selectedPageKey;

            await LoadMappingsAsync();
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
            if (string.IsNullOrEmpty(SelectedPageKey)) return;

            GlobalMappings.Clear();
            var templates = await _templateService.GetTemplatesAsync(SelectedPageKey);
            var mappings = templates.SelectMany(t => t.Mappings).DistinctBy(m => m.Placeholder);
            foreach (var m in mappings)
                GlobalMappings.Add(new PlaceholderMapping
                {
                    Placeholder = m.Placeholder,
                    SourceType = m.SourceType,
                    Property = m.Property
                });
        }


        private async void OnSaveMappingsClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SelectedPageKey)) return;

            var pageTemplates = await _templateService.GetTemplatesAsync(SelectedPageKey);

            foreach (var tpl in pageTemplates)
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
                var updated = pageTemplates.FirstOrDefault(x => x.LocalPath == t.LocalPath);
                if (updated != null)
                    t.Mappings = updated.Mappings;
            }

            await _templateService.SaveAllTemplatesAsync(allTemplates);
            await DisplayAlert("Сохранено", "Маркеры обновлены.", "OK");
        }
        private async void OnPagePickerChanged(object sender, EventArgs e)
        {
            if (PagePicker.SelectedItem is string newKey)
            {
                selectedPageKey = newKey;
                await LoadMappingsAsync();
            }
        }

    }
}