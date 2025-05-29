using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySECv2.Models;
using EasySECv2.Services;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;

namespace EasySECv2.ViewModels;

public partial class BatchCertificateViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly IDocumentGenerationService _generator;
    private readonly IFolderPickerService _folderPicker;
    private readonly ITemplateService _templateService;

    public ObservableCollection<Group> Groups { get; } = new();
    public ObservableCollection<SelectableStudent> FilteredStudents { get; } = new();
    public ObservableCollection<DocumentTemplate> Templates { get; } = new();

    private List<Student> _allStudents = new();

    [ObservableProperty] private Group? selectedGroup;
    [ObservableProperty] private string outputFolder = string.Empty;
    [ObservableProperty] private string searchQuery = string.Empty;
    [ObservableProperty] private DocumentTemplate? selectedTemplate;

    public BatchCertificateViewModel()
    {
        _db = MauiProgram.GetService<DatabaseService>();
        _generator = MauiProgram.GetService<IDocumentGenerationService>();
        _folderPicker = MauiProgram.GetService<IFolderPickerService>();
        _templateService = MauiProgram.GetService<ITemplateService>();

        LoadGroups();
        LoadAllStudents();
        LoadTemplates();
    }

    private async void LoadGroups()
    {
        var all = await _db.GetAllGroupsAsync();
        Groups.Clear();
        Groups.Add(new Group { id = 0, name = "— не выбрано —" });
        foreach (var g in all)
            Groups.Add(g);
        SelectedGroup = Groups.FirstOrDefault();
    }

    private async void LoadAllStudents()
    {
        _allStudents = await _db.GetStudentsAsync();
        ApplyFilters();
    }

    private async void LoadTemplates()
    {
        Templates.Clear();
        var list = await _templateService.GetTemplatesAsync("batch-certificate");
        foreach (var t in list)
            Templates.Add(t);
        SelectedTemplate = Templates.FirstOrDefault();
    }

    partial void OnSelectedGroupChanged(Group? value) => ApplyFilters();
    partial void OnSearchQueryChanged(string value) => ApplyFilters();
    partial void OnOutputFolderChanged(string value) => RefreshCanExecute();

    private void ApplyFilters()
    {
        var filtered = _allStudents.Where(s =>
            (SelectedGroup == null || SelectedGroup.id == 0 || s.groupId == SelectedGroup.id) &&
            (string.IsNullOrWhiteSpace(SearchQuery) || s.FullName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)));

        FilteredStudents.Clear();
        foreach (var s in filtered)
            FilteredStudents.Add(new SelectableStudent(s, RefreshCanExecute));

        RefreshCanExecute();
    }

    [RelayCommand]
    private async Task SelectFolderAsync()
    {
        var path = await _folderPicker.PickFolderAsync();
        if (!string.IsNullOrEmpty(path))
            OutputFolder = path;
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateAsync()
    {
        if (SelectedTemplate == null) return;
        var selected = FilteredStudents.Where(s => s.IsSelected).Select(s => s.Student).ToList();
        var manual = new Dictionary<string, string>
        {
            { "ОЦЕНКА", "Хорошо" },
            { "КОЛВО ЛИСТОВ", "20" },
            { "МЕСЯЦ", DateTime.Now.ToString("MMMM", new System.Globalization.CultureInfo("ru-RU")) }
        };
        await _generator.GenerateDocumentsAsync(SelectedTemplate, selected, manual, OutputFolder);
    }

    [RelayCommand(CanExecute = nameof(CanGenerateGroup))]
    private async Task GenerateGroupAsync()
    {
        if (SelectedGroup == null || SelectedTemplate == null) return;
        var students = _allStudents.Where(s => s.groupId == SelectedGroup.id).ToList();
        var manual = new Dictionary<string, string>
        {
            { "ОЦЕНКА", "Хорошо" },
            { "КОЛВО ЛИСТОВ", "20" },
            { "МЕСЯЦ", DateTime.Now.ToString("MMMM", new System.Globalization.CultureInfo("ru-RU")) }
        };
        await _generator.GenerateDocumentsAsync(SelectedTemplate, students, manual, OutputFolder);
    }

    [ObservableProperty] private bool canGenerate;
    [ObservableProperty] private bool canGenerateGroup;

    private void RefreshCanExecute()
    {
        CanGenerate = FilteredStudents.Any(s => s.IsSelected) && !string.IsNullOrEmpty(OutputFolder) && SelectedTemplate != null;
        CanGenerateGroup = SelectedGroup != null && SelectedGroup.id != 0 && !string.IsNullOrEmpty(OutputFolder) && SelectedTemplate != null;
        GenerateCommand.NotifyCanExecuteChanged();
        GenerateGroupCommand.NotifyCanExecuteChanged();
        DeleteTemplateCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task AddTemplateAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Выберите шаблон .docx",
            //FileTypes = FilePickerFileType.WordDocument
        });
        if (result != null)
        {
            using var stream = await result.OpenReadAsync();
            await _templateService.AddTemplateAsync(stream, result.FileName, "batch-certificate");
            LoadTemplates();
        }
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

    public bool CanDeleteTemplate => SelectedTemplate != null;

    public class SelectableStudent : ObservableObject
    {
        public Student Student { get; }
        private readonly Action _onSelectionChanged;

        public SelectableStudent(Student s, Action onSelectionChanged)
        {
            Student = s;
            _onSelectionChanged = onSelectionChanged;
        }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (SetProperty(ref isSelected, value))
                    _onSelectionChanged?.Invoke();
            }
        }

        public string FullName => Student.FullName;
    }
}