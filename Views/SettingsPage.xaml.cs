using EasySECv2.Services;
using EasySECv2.Models;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace EasySECv2.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly DatabaseService _db;
        private readonly IPageSettingsService _pageSettings;

        // Список всех страниц (PageKey)
        private readonly List<string> _pageKeys = new();
        // Список страниц, для которых есть генерация
        private static readonly List<string> PagesForGeneration = new()
        {
            nameof(SecCompositionPage),
            //nameof(OrderStudentsPage),
            //nameof(DiplomaIssuePage),
            // TODO: добавьте сюда остальные PageKey, где нужен batch
        };
        public SettingsPage(DatabaseService dbService,
                            IPageSettingsService pageSettings)
        {
            InitializeComponent();
            _db = dbService;
            _pageSettings = pageSettings;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Таблицы
            var tables = await _db.GetAllTableNamesAsync();
            TablesPicker.ItemsSource = tables;

            _pageKeys.Clear();
            _pageKeys.AddRange(PagesForGeneration);
            PagePicker.ItemsSource = _pageKeys;

            // Если есть хотя бы одна, сразу выбрать первую
            if (_pageKeys.Any())
                PagePicker.SelectedIndex = 0;
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
                await DisplayAlert(
                    "Готово",
                    $"Удалено примерно {deleted} строк из «{table}».",
                    "OK");
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
    }
}
