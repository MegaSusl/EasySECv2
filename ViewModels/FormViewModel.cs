using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySECv2.Models;
using EasySECv2.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace EasySECv2.ViewModels;

public partial class FormViewModel : ObservableObject
{
    public ObservableCollection<FormField> Fields { get; } = new();

    private readonly TaskCompletionSource<Dictionary<string, string>> _tcs = new();
    public Task<Dictionary<string, string>> Completion => _tcs.Task;

    public FormViewModel() { }

    public void Load(List<PlaceholderMapping> mappings, List<Staff> staff, List<Institute> institutes)
    {
        Fields.Clear();
        foreach (var m in mappings)
        {
            var field = new FormField
            {
                Placeholder = m.Placeholder,
                DisplayName = m.Placeholder,
                SourceType = m.SourceType
            };

            switch (m.SourceType)
            {
                case MappingSourceType.ManualText:
                    field.IsEditor = true;
                    break;
                case MappingSourceType.ManualDate:
                    field.IsDate = true;
                    field.DateValue = DateTime.Now;
                    break;
                case MappingSourceType.Table:
                    field.IsPicker = true;
                    field.Options = m.Property == "Staff"
                        ? staff.Select(s => s.FullName).ToList()
                        : institutes.Select(i => i.name).ToList();
                    break;
                default:
                    field.IsEntry = true;
                    break;
            }

            Fields.Add(field);
        }
    }

    [RelayCommand]
    private void Submit()
    {
        var result = new Dictionary<string, string>();
        foreach (var f in Fields)
        {
            string value = f.IsDate ? f.DateValue.ToString("dd.MM.yyyy", new CultureInfo("ru-RU")) : f.Value;
            result[f.Placeholder] = value;
        }
        _tcs.TrySetResult(result);
    }

    public partial class FormField : ObservableObject
    {
        public string Placeholder { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public MappingSourceType SourceType { get; set; }

        public bool IsEntry { get; set; }
        public bool IsEditor { get; set; }
        public bool IsDate { get; set; }
        public bool IsPicker { get; set; }

        public List<string> Options { get; set; } = new();

        [ObservableProperty] private string value = string.Empty;
        [ObservableProperty] private DateTime dateValue = DateTime.Now;
    }
}
