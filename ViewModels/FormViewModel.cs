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
                    field.isVisible = true;
                    break;
                case MappingSourceType.ManualDate:
                    field.IsDate = true;
                    field.isVisible = true;
                    field.DateValue = DateTime.Now;
                    field.FormatMap = new()
                        {
                            { "dd.MM.yyyy", "00.00.0000" },
                            { "d MMMM yyyy", "00 Месяц 0000" }
                        };
                    field.FormatOptions = field.FormatMap.Values.ToList();
                    field.SelectedFormatKey = field.FormatMap.Keys.First();
                    break;
                case MappingSourceType.Table:
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = m.Property == "Staff"
                        ? staff.Select(s => s.FullName).ToList()
                        : institutes.Select(i => i.name).ToList();
                    break;
                case MappingSourceType.ManualTimeFull:
                    field.IsTime = true;
                    field.isVisible = true;
                    field.TimeValue = DateTime.Now.TimeOfDay;
                    //field.FormatMap = new()
                    //    {
                    //        { "HH:mm", "00:00" },
                    //        { "HH", "Часы (00)" },
                    //        { "mm", "Минуты (00)" }
                    //    };
                    //field.FormatOptions = field.FormatMap.Values.ToList();
                    //field.SelectedFormatKey = field.FormatMap.Keys.First();
                    break;
                case MappingSourceType.Group:
                    field.isVisible = false;
                    break;

                case MappingSourceType.Institute:
                    var institutesList = MauiProgram.GetService<DatabaseService>().GetAllInstitutesAsync().Result;
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = institutes.Select(i => i.name).ToList();
                    field.ColumnOptions = new List<string> { "name", "shortName", "code" }; // зависит от таблицы
                    field.SelectedColumn = "name"; // по умолчанию

                    break;

                case MappingSourceType.FormOfEducation:
                    var formList = MauiProgram.GetService<DatabaseService>().GetAllFormsOfEducationAsync().Result;
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = formList.Select(f => f.name).ToList();
                    field.ColumnOptions = new List<string> { "name", "shortName", "code" }; // зависит от таблицы
                    field.SelectedColumn = "name"; // по умолчанию

                    break;

                case MappingSourceType.Orientation:
                    var orientations = MauiProgram.GetService<DatabaseService>().GetAllOrientationsAsync().Result;
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = orientations.Select(o => o.name).ToList();
                    field.ColumnOptions = new List<string> { "name", "shortName", "code" }; // зависит от таблицы
                    field.SelectedColumn = "name"; // по умолчанию

                    break;

                case MappingSourceType.Department:
                    var departments = MauiProgram.GetService<DatabaseService>().GetAllDepartmentsAsync().Result;
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = departments.Select(d => d.name).ToList();
                    field.ColumnOptions = new List<string> { "name", "shortName", "code" }; // зависит от таблицы
                    field.SelectedColumn = "name"; // по умолчанию
                    break;

                default:
                    field.IsEntry = true;
                    field.isVisible = true;
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
            string value;

            if (f.IsDate)
            {
                string realFormat = f.FormatMap.ElementAtOrDefault(f.SelectedFormatIndex).Key;
                value = f.DateValue.ToString(realFormat, new CultureInfo("ru-RU"));
            }
            else if (f.IsTime)
            {
                string realFormat = f.FormatMap.ElementAtOrDefault(f.SelectedFormatIndex).Key;
                value = DateTime.Today.Add(f.TimeValue).ToString(realFormat);
            }
            else
            {
                value = f.Value;
            }

            if (f.SourceType == MappingSourceType.ManualText)
            {
                result[f.Placeholder] = f.Value; // сам текст
                result[$"{f.Placeholder}__min"] = f.MinLines.ToString(); // мин. строки
            }
            else
            {
                result[f.Placeholder] = value;
            }


        }

        _tcs.TrySetResult(result);
    }


    public partial class FormField : ObservableObject
    {
        public string Placeholder { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public MappingSourceType SourceType { get; set; }
        public int MinLines { get; set; } = 3; // можно 3 по умолчанию
        public bool IsEntry { get; set; }
        public bool IsEditor { get; set; }
        public bool IsDate { get; set; }
        public bool IsPicker { get; set; }
        public bool IsTime { get; set; }
        public string Format { get; set; } = ""; // по умолчанию
        public string TimeFormat { get; set; }
        public bool isVisible { get; set; }
        public List<string> FormatOptions { get; set; } = new();
        public Dictionary<string, string> FormatMap { get; set; } = new();
        [ObservableProperty] private string selectedFormatKey = "";


        [ObservableProperty]
        private TimeSpan timeValue = DateTime.Now.TimeOfDay;

        public List<string> Options { get; set; } = new();

        [ObservableProperty] private string value = string.Empty;
        [ObservableProperty] private DateTime dateValue = DateTime.Now;

        public int SelectedFormatIndex { get; set; } = 0;
        public List<string> ColumnOptions { get; set; } = new();
        [ObservableProperty] private string selectedColumn = string.Empty;
        string GetSelectedFormat() => FormatMap.ElementAtOrDefault(SelectedFormatIndex).Key;

    }
}
