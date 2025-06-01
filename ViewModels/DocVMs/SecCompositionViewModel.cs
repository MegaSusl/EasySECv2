using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySECv2.Models;
using EasySECv2.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace EasySECv2.ViewModels;

public partial class SecCompositionViewModel : ObservableObject
{
    private readonly ITemplateService _templateService;
    private readonly IFolderPickerService _folderPicker;
    private readonly IDocumentGenerationService _generator;
    private readonly DatabaseService _db;

    public ObservableCollection<DocumentTemplate> Templates { get; } = new();

    [ObservableProperty] private DocumentTemplate? selectedTemplate;
    [ObservableProperty] private string outputFolder = string.Empty;

    public bool CanGenerate => SelectedTemplate != null && !string.IsNullOrEmpty(OutputFolder);
    public bool CanDeleteTemplate => SelectedTemplate != null;

    public SecCompositionViewModel()
    {
        _templateService = MauiProgram.GetService<ITemplateService>();
        _folderPicker = MauiProgram.GetService<IFolderPickerService>();
        _generator = MauiProgram.GetService<IDocumentGenerationService>();
        _db = MauiProgram.GetService<DatabaseService>();

        LoadTemplates();
    }

    private async void LoadTemplates()
    {
        var list = await _templateService.GetTemplatesAsync("sec_composition");
        Templates.Clear();
        foreach (var tpl in list)
            Templates.Add(tpl);

        SelectedTemplate = Templates.FirstOrDefault();
    }

    partial void OnSelectedTemplateChanged(DocumentTemplate? value)
    {
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(CanDeleteTemplate));
    }

    [RelayCommand]
    private async Task SelectFolderAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (!string.IsNullOrEmpty(path))
            OutputFolder = path;

        OnPropertyChanged(nameof(CanGenerate));
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        var template = SelectedTemplate;
        if (template == null)
            return;

        var staff = await _db.GetAllStaffAsync();
        var institutes = await _db.GetAllInstitutesAsync();

        var vm = new FormViewModel();
        vm.Load(template.Mappings, staff, institutes);

        var page = new Views.FormPage { BindingContext = vm };
        await Shell.Current.Navigation.PushAsync(page);

        var manual = await vm.Completion;
        await Shell.Current.Navigation.PopAsync();

        await _generator.GenerateDocumentsAsync(template, new List<object> { new object() }, manual, OutputFolder);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteTemplate))]
    private async Task DeleteTemplateAsync()
    {
        if (SelectedTemplate != null)
        {
            await _templateService.DeleteTemplateAsync(SelectedTemplate);
            LoadTemplates();
        }
    }

    [RelayCommand]
    private async Task AddTemplateAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Выберите шаблон документа",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".docx" } }
            })
        });

        if (file == null) return;

        using var stream = await file.OpenReadAsync();
        await _templateService.AddTemplateAsync(stream, file.FileName, "sec_composition");
        LoadTemplates();
    }
}
