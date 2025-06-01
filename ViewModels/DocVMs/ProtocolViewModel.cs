using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySECv2.Models;
using EasySECv2.Services;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;

namespace EasySECv2.ViewModels;

public partial class ProtocolViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly IFolderPickerService _folderPicker;
    private readonly ITemplateService _templateService;
    private readonly IDocumentGenerationService _generator;

    public ObservableCollection<DocumentTemplate> Templates { get; } = new();
    public ObservableCollection<SelectableStudent> FilteredStudents { get; } = new();

    [ObservableProperty] private string outputFolder = string.Empty;
    [ObservableProperty] private string searchQuery = string.Empty;
    [ObservableProperty] private DocumentTemplate? selectedTemplate;

    public bool CanGenerate => !string.IsNullOrEmpty(OutputFolder) && SelectedTemplate != null;
    public bool CanDeleteTemplate => SelectedTemplate != null;

    public ProtocolViewModel()
    {
        _db = MauiProgram.GetService<DatabaseService>();
        _folderPicker = MauiProgram.GetService<IFolderPickerService>();
        _templateService = MauiProgram.GetService<ITemplateService>();
        _generator = MauiProgram.GetService<IDocumentGenerationService>();

        LoadTemplates();
        LoadStudents();
    }
    partial void OnSelectedTemplateChanged(DocumentTemplate? value)
    {
        OnPropertyChanged(nameof(CanDeleteTemplate)); // для кнопки «Удалить»
        OnPropertyChanged(nameof(CanGenerate));       // заодно починим «Сгенерировать»
    }

    private async void LoadTemplates()
    {
        var list = await _templateService.GetTemplatesAsync("protocol-vkr");
        Templates.Clear();
        foreach (var tpl in list)
            Templates.Add(tpl);
        SelectedTemplate = Templates.FirstOrDefault();
    }

    private async void LoadStudents()
    {
        var all = await _db.GetStudentsAsync();
        var filtered = all.Where(s => s.isAccessed && s.ReleaseYear == null).ToList();
        FilteredStudents.Clear();
        foreach (var s in filtered)
            FilteredStudents.Add(new SelectableStudent(s));
    }

    partial void OnSearchQueryChanged(string value)
    {
        foreach (var s in FilteredStudents)
            s.IsVisible = string.IsNullOrWhiteSpace(value) || s.Student.FullName.Contains(value, StringComparison.OrdinalIgnoreCase);
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
    private async Task GenerateAsync(Student student)
    {
        var template = SelectedTemplate;
        if (template == null || student == null) return;

        var staff = await _db.GetAllStaffAsync();
        var institutes = await _db.GetAllInstitutesAsync();

        var vm = new FormViewModel();
        vm.Load(template.Mappings, staff, institutes);

        var formPage = new Views.FormPage { BindingContext = vm };
        await Shell.Current.Navigation.PushAsync(formPage);
        var manual = await vm.Completion;
        await Shell.Current.Navigation.PopAsync();

        await _generator.GenerateDocumentsAsync(template, new List<Student> { student }, manual, OutputFolder);
    }

    public partial class SelectableStudent : ObservableObject
    {
        public Student Student { get; }
        [ObservableProperty] private bool isVisible = true;
        public SelectableStudent(Student s) => Student = s;
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
        await _templateService.AddTemplateAsync(stream, file.FileName, "protocol-vkr");
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