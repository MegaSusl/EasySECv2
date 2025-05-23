using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySECv2.Models.DocumentTemplates;
using EasySECv2.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using EasySECv2.Views;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace EasySECv2.ViewModels
{
    public class SecCompositionViewModel : ObservableObject
    {
        private readonly ITemplateService _templateService;
        private readonly DatabaseService _databaseService;
        private readonly IFolderPickerService _folderPickerService;
        private readonly IDocumentGenerationService _docGenService;
        private readonly IPageSettingsService _pageSettingsService;
        private const string PageKey = nameof(SecCompositionPage);

        // Templates collection
        private ObservableCollection<DocumentTemplate> _templates = new();
        public ObservableCollection<DocumentTemplate> Templates
        {
            get => _templates;
            set => SetProperty(ref _templates, value);
        }

        // Batch settings
        private bool _allowBatch;
        public bool AllowBatch
        {
            get => _allowBatch;
            private set
            {
                if (SetProperty(ref _allowBatch, value))
                {
                    ((RelayCommand)GenerateCommand).NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(CanGenerate));
                }
            }
        }

        private bool _isBatchMode;
        public bool IsBatchMode
        {
            get => _isBatchMode;
            set
            {
                if (SetProperty(ref _isBatchMode, value))
                {
                    ((RelayCommand)GenerateCommand).NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(CanGenerate));
                }
            }
        }

        // Selected template
        private DocumentTemplate _selectedTemplate;
        public DocumentTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (SetProperty(ref _selectedTemplate, value))
                {
                    ((RelayCommand)OpenTemplateCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)EditTemplateCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)DeleteTemplateCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)GenerateCommand).NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(CanGenerate));
                    UpdateFieldMappings(value);
                }
            }
        }

        // Field mappings
        public ObservableCollection<FieldMappingViewModel> FieldMappings { get; } = new();

        // Output folder path
        private string? _outputFolder;
        public string? OutputFolder
        {
            get => _outputFolder;
            set
            {
                if (SetProperty(ref _outputFolder, value))
                {
                    ((RelayCommand)GenerateCommand).NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(CanGenerate));
                }
            }
        }

        // Commands
        public IRelayCommand LoadTemplateCommand { get; }
        public IRelayCommand OpenTemplateCommand { get; }
        public IRelayCommand EditTemplateCommand { get; }
        public IRelayCommand DeleteTemplateCommand { get; }
        public IRelayCommand PickFolderCommand { get; }
        public IRelayCommand GenerateCommand { get; }

        // Docx file picker filter
        private static readonly FilePickerFileType DocxFileType = new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".docx" } }
            });

        public SecCompositionViewModel(
            ITemplateService templateService,
            DatabaseService databaseService,
            IFolderPickerService folderPickerService,
            IDocumentGenerationService docGenService,
            IPageSettingsService pageSettingsService)
        {
            _templateService = templateService;
            _databaseService = databaseService;
            _folderPickerService = folderPickerService;
            _docGenService = docGenService;
            _pageSettingsService = pageSettingsService;

            LoadTemplateCommand = new RelayCommand(async () => await LoadNewTemplateAsync());
            OpenTemplateCommand = new RelayCommand(async () => await OpenTemplateAsync(), () => SelectedTemplate != null);
            EditTemplateCommand = new RelayCommand(async () => await EditTemplateAsync(), () => SelectedTemplate != null);
            DeleteTemplateCommand = new RelayCommand(async () => await DeleteTemplateAsync(), () => SelectedTemplate != null);
            PickFolderCommand = new RelayCommand(async () => await PickFolderAsync());
            GenerateCommand = new RelayCommand(
                async () => await GenerateAsync(),
                () => SelectedTemplate != null
                          && !string.IsNullOrEmpty(OutputFolder)
                          && (!AllowBatch || IsBatchMode));

            // Initialize settings and load templates
            _ = InitPageSettingsAsync();
            _ = LoadExistingTemplatesAsync();
        }

        private async Task InitPageSettingsAsync()
        {
            var settings = await _pageSettingsService.GetSettingsAsync(PageKey);
            AllowBatch = settings.AllowBatch;
            IsBatchMode = false;
        }

        private async Task LoadExistingTemplatesAsync()
        {
            var list = await _templateService.GetTemplatesAsync(PageKey);
            Templates = new ObservableCollection<DocumentTemplate>(list);

            if (Templates.Any())
                SelectedTemplate = Templates.First();
        }

        private async Task LoadNewTemplateAsync()
        {
            var pickResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите .docx шаблон",
                FileTypes = DocxFileType
            });
            if (pickResult == null)
                return;

            using var stream = await pickResult.OpenReadAsync();
            await _templateService.AddOrUpdateAsync(PageKey, pickResult.FileName, stream);
            await LoadExistingTemplatesAsync();
        }

        private async Task OpenTemplateAsync()
        {
            if (SelectedTemplate == null)
                return;

            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(SelectedTemplate.LocalPath)
            });
        }

        private async Task EditTemplateAsync()
        {
            if (SelectedTemplate == null)
                return;

            var pickResult = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите новый .docx для замены",
                FileTypes = DocxFileType
            });
            if (pickResult == null)
                return;

            using var stream = await pickResult.OpenReadAsync();
            await _templateService.UpdateAsync(SelectedTemplate.Id, pickResult.FileName, stream);
            await LoadExistingTemplatesAsync();
        }

        private async Task DeleteTemplateAsync()
        {
            if (SelectedTemplate == null)
                return;

            await _templateService.DeleteAsync(SelectedTemplate.Id);
            await LoadExistingTemplatesAsync();
        }

        private async Task PickFolderAsync()
        {
            var path = await _folderPickerService.PickFolderAsync();
            if (path != null)
                OutputFolder = path;
        }

        private async Task GenerateAsync()
        {
            if (SelectedTemplate == null || string.IsNullOrEmpty(OutputFolder))
                return;

            if (!AllowBatch || !IsBatchMode)
            {
                var values = FieldMappings.ToDictionary(
                    vm => $"[{vm.Placeholder}]",
                    vm => vm.SourceType == MappingSourceType.Manual
                          ? vm.ManualValue as object
                          : vm.SelectedItem);
                await _docGenService.GenerateAsync(SelectedTemplate, values, OutputFolder!);
            }
            else
            {
                var batchVm = FieldMappings.FirstOrDefault(vm =>
                    vm is FieldMappingViewModel fm && fm.SelectedItems.Any()) as FieldMappingViewModel;
                if (batchVm == null)
                    return;

                var batchItems = new List<IDictionary<string, object?>>();
                foreach (var rec in batchVm.SelectedItems)
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var vm in FieldMappings)
                    {
                        var key = $"[{vm.Placeholder}]";
                        object? val = vm == batchVm
                                      ? rec
                                      : (vm.SourceType == MappingSourceType.Manual
                                          ? vm.ManualValue
                                          : vm.SelectedItem);
                        dict[key] = val;
                    }
                    batchItems.Add(dict);
                }

                await _docGenService.GenerateBatchAsync(SelectedTemplate, batchItems, OutputFolder!);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var cancellationTokenSource = new CancellationTokenSource();
                var snackbar = Snackbar.Make(
                    $"Документ '{SelectedTemplate.FileName}' создан",
                    () => _ = Launcher.OpenAsync(OutputFolder),
                    "Открыть папку",
                    TimeSpan.FromSeconds(5)
                );
                snackbar.Show(cancellationTokenSource.Token);
            });
        }

        private void UpdateFieldMappings(DocumentTemplate tpl)
        {
            FieldMappings.Clear();
            if (tpl?.Mappings != null)
            {
                foreach (var pm in tpl.Mappings)
                    FieldMappings.Add(new FieldMappingViewModel(pm, _databaseService));
            }
        }
        public bool CanGenerate => SelectedTemplate != null
            && !string.IsNullOrEmpty(OutputFolder)
            && (!AllowBatch || IsBatchMode);
    }
}