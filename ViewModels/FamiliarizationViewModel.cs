using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySECv2.Models;
using EasySECv2.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace EasySECv2.ViewModels;

public partial class FamiliarizationViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly IFolderPickerService _folderPicker;
    private readonly ITemplateService _templateService;
    private readonly IDocumentGenerationService _generator;

    public ObservableCollection<DocumentTemplate> Templates { get; } = new();
    public ObservableCollection<SelectableGroup> FilteredGroups { get; } = new();

    [ObservableProperty] private string outputFolder = string.Empty;
    [ObservableProperty] private string searchQuery = string.Empty;
    [ObservableProperty] private DocumentTemplate? selectedTemplate;

    public bool CanGenerate => !string.IsNullOrEmpty(OutputFolder) && SelectedTemplate != null;
    public bool CanDeleteTemplate => SelectedTemplate != null;
    public FamiliarizationViewModel()
    {
        _db = MauiProgram.GetService<DatabaseService>();
        _folderPicker = MauiProgram.GetService<IFolderPickerService>();
        _templateService = MauiProgram.GetService<ITemplateService>();
        _generator = MauiProgram.GetService<IDocumentGenerationService>();

        LoadTemplates();
        LoadGroups();
    }
    partial void OnSelectedTemplateChanged(DocumentTemplate? value)
    {
        OnPropertyChanged(nameof(CanDeleteTemplate)); // для кнопки «Удалить»
        OnPropertyChanged(nameof(CanGenerate));       // заодно починим «Сгенерировать»
    }

    private async void LoadTemplates()
    {
        var list = await _templateService.GetTemplatesAsync("familiarization");
        Templates.Clear();
        foreach (var tpl in list)
            Templates.Add(tpl);
        SelectedTemplate = Templates.FirstOrDefault();
    }

    private async void LoadGroups()
    {
        var all = await _db.GetAllGroupsAsync();
        FilteredGroups.Clear();
        foreach (var s in all)
            FilteredGroups.Add(new SelectableGroup(s));
    }

    partial void OnSearchQueryChanged(string value)
    {
        foreach (var s in FilteredGroups)
            s.IsVisible = string.IsNullOrWhiteSpace(value) || s.Group.name.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private async Task SelectFolderAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (!string.IsNullOrEmpty(path))
            OutputFolder = path;
        OnPropertyChanged(nameof(CanGenerate));
    }

    [RelayCommand]
    private async Task GenerateAsync(Group group)
    {
        var template = SelectedTemplate;
        if (template == null || group == null) return;

        var staff = await _db.GetAllStaffAsync();
        var institutes = await _db.GetAllInstitutesAsync();

        var vm = new FormViewModel();
        vm.Load(template.Mappings, staff, institutes);

        var formPage = new Views.FormPage { BindingContext = vm };
        await Shell.Current.Navigation.PushAsync(formPage);
        var manual = await vm.Completion;
        await Shell.Current.Navigation.PopAsync();
        Debug.WriteLine(group.name);
        await _generator.GenerateDocumentsAsync(template, new List<Group> { group }, manual, OutputFolder);
    }

    [RelayCommand]
    private async Task AddTemplateAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Выберите шаблон протокола",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".docx" } }
                }
            )
        });

        if (file == null) return;

        using var stream = await file.OpenReadAsync();
        await _templateService.AddTemplateAsync(stream, file.FileName, "familiarization");
        LoadTemplates();
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
}
