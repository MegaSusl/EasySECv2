using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySECv2.Models;
using EasySECv2.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace EasySECv2.ViewModels;

public partial class FormViewModel : ObservableObject
{
    public ObservableCollection<FormField> Fields { get; } = new();

    private readonly TaskCompletionSource<Dictionary<string, string>> _tcs = new();
    public Task<Dictionary<string, string>> Completion => _tcs.Task;
    public FormViewModel() { }
    private Student? _ctxStudent;
    private FinalQualifyingWork? _ctxFqw;
    private Staff? _ctxSupervisor;
    public void Load(List<PlaceholderMapping> mappings, List<Staff> staff, List<Institute> institutes, Student? studentCtx = null, FinalQualifyingWork? fqwCtx = null, Staff? supervisorCtx = null)
    {
        _ctxStudent = studentCtx;
        _ctxFqw = fqwCtx;
        _ctxSupervisor = supervisorCtx;
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
                        : institutes.Select(i => i.Name).ToList();
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
                    field.Options = institutes.Select(i => i.Name).ToList();
                    field.ColumnOptions = new List<string> { "Name", "ShortName" }; // зависит от таблицы
                    field.SelectedColumn = "name"; // по умолчанию

                    break;

                case MappingSourceType.FormOfEducation:
                    var formList = MauiProgram.GetService<DatabaseService>().GetAllFormsOfEducationAsync().Result;
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = formList.Select(f => f.Name).ToList();
                    field.ColumnOptions = new List<string> { "Name" }; // зависит от таблицы
                    field.SelectedColumn = "Name"; // по умолчанию

                    break;

                case MappingSourceType.Orientation:
                    var orientations = MauiProgram.GetService<DatabaseService>().GetAllOrientationsAsync().Result;
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = orientations.Select(o => o.Name).ToList();
                    field.ColumnOptions = new List<string> { "Name", "Code" }; // зависит от таблицы
                    field.SelectedColumn = "Name"; // по умолчанию

                    break;

                case MappingSourceType.Department:
                    var departments = MauiProgram.GetService<DatabaseService>().GetAllDepartmentsAsync().Result;
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = departments.Select(d => d.Name).ToList();
                    field.ColumnOptions = new List<string> { "Name", "ShortName"}; // зависит от таблицы
                    field.SelectedColumn = "Name"; // по умолчанию
                    break;

                case MappingSourceType.Staff:
                    var staffList = MauiProgram.GetService<DatabaseService>().GetAllStaffAsync().Result;
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = staffList.Select(s => s.FullName).ToList();
                    field.ColumnOptions = new List<string> { "FullName", "Position" };
                    field.SelectedColumn = "FullName";
                    break;

                case MappingSourceType.Student:
                    var students = MauiProgram.GetService<DatabaseService>().GetStudentsAsync().Result;
                    field.IsPicker = true;
                    field.isVisible = true;
                    field.Options = students.Select(s => s.FullName).ToList();
                    field.ColumnOptions = new List<string> { "FullName", "GroupName" };
                    field.SelectedColumn = "FullName";
                    break;

                case MappingSourceType.TableChairman:
                    field.IsChairmanSelector = true;
                    field.isVisible = true;
                    field.DisplayName = "Председатель ГЭК";
                    field.AllStaff = staff;
                    field.SelectedChairmanId = (int)(staff.FirstOrDefault()?.Id ?? 0);
                    break;

                case MappingSourceType.TableMembersAndSecretary:
                    field.IsMemberAndSecretarySelector = true;
                    field.isVisible = true;
                    field.DisplayName = "Члены и секретарь ГЭК";
                    field.AllStaff = staff;
                    field.SecretaryCandidates = staff
                        .Where(s => s.Position.ToLower().Contains("секретарь"))
                        .ToList();
                    field.SelectedSecretaryId = (int)(field.SecretaryCandidates.FirstOrDefault()?.Id ?? 0);
                    field.MemberPickers = new ObservableCollection<MemberSelection>
                        {
                            new MemberSelection
                            {
                                SelectedStaffId = staff.FirstOrDefault()?.Id ?? 0,
                                StaffOptions = staff
                            }
                        };
                    break;

                case MappingSourceType.TableVkrTopic:
                    field.isVisible = false;                    
                    break;
                case MappingSourceType.TableReport:
                    field.isVisible = false;                    
                    break;

                case MappingSourceType.ProtocolAutoFio:
                case MappingSourceType.ProtocolAutoDate:
                case MappingSourceType.ProtocolAutoTime:
                case MappingSourceType.ProtocolAutoOrientationCode:
                case MappingSourceType.ProtocolAutoOrientationName:
                case MappingSourceType.ProtocolAutoGroupName:
                case MappingSourceType.ProtocolAutoSupervisorFio:
                case MappingSourceType.ProtocolAutoFqwTopic:
                case MappingSourceType.ProtocolAutoDay:
                case MappingSourceType.ProtocolAutoMonth:
                case MappingSourceType.ProtocolAutoYear:
                case MappingSourceType.ProtocolAutoInstitute:
                    field.isVisible = false;
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
    //private void Submit()
    //{
    //    var result = new Dictionary<string, string>();
    //    foreach (var f in Fields)
    //    {
    //        string value;

    //        if (f.IsDate)
    //        {
    //            string realFormat = f.FormatMap.ElementAtOrDefault(f.SelectedFormatIndex).Key;
    //            value = f.DateValue.ToString(realFormat, new CultureInfo("ru-RU"));
    //        }
    //        else if (f.IsTime)
    //        {
    //            string realFormat = f.FormatMap.ElementAtOrDefault(f.SelectedFormatIndex).Key;
    //            value = DateTime.Today.Add(f.TimeValue).ToString(realFormat);
    //        }
    //        else
    //        {
    //            value = f.Value;
    //        }

    //        if (f.SourceType == MappingSourceType.ManualText)
    //        {
    //            result[f.Placeholder] = f.Value; // сам текст
    //            result[$"{f.Placeholder}__min"] = f.MinLines.ToString(); // мин. строки
    //        }
    //        else
    //        {
    //            result[f.Placeholder] = value;
    //        }


    //    }

    //    _tcs.TrySetResult(result);
    //}
    //private async void Submit()
    //{
    //    var result = new Dictionary<string, string>();
    //    var db = MauiProgram.GetService<DatabaseService>();

    //    foreach (var f in Fields)
    //    {
    //        string value = f.Value;

    //        // Форматирование даты
    //        if (f.IsDate)
    //        {
    //            string realFormat = f.FormatMap.ElementAtOrDefault(f.SelectedFormatIndex).Key;
    //            value = f.DateValue.ToString(realFormat, new CultureInfo("ru-RU"));
    //        }
    //        // Форматирование времени
    //        else if (f.IsTime)
    //        {
    //            string realFormat = f.FormatMap.ElementAtOrDefault(f.SelectedFormatIndex).Key;
    //            value = DateTime.Today.Add(f.TimeValue).ToString(realFormat);
    //        }

    //        // Спец. случай — длинный текст с мин. строками
    //        if (f.SourceType == MappingSourceType.ManualText)
    //        {
    //            result[f.Placeholder] = f.Value;
    //            result[$"{f.Placeholder}__min"] = f.MinLines.ToString();
    //            continue;
    //        }

    //        // Обработка справочных таблиц с выбором колонки
    //        if (f.IsPicker && !string.IsNullOrEmpty(f.SelectedColumn))
    //        {
    //            object? match = null;

    //            switch (f.SourceType)
    //            {
    //                case MappingSourceType.Institute:
    //                    match = (await db.GetAllInstitutesAsync()).FirstOrDefault(i => i.Name == f.Value);
    //                    break;

    //                case MappingSourceType.Department:
    //                    match = (await db.GetAllDepartmentsAsync()).FirstOrDefault(d => d.Name == f.Value);
    //                    break;

    //                case MappingSourceType.FormOfEducation:
    //                    match = (await db.GetAllFormsOfEducationAsync()).FirstOrDefault(foe => foe.Name == f.Value);
    //                    break;

    //                case MappingSourceType.Orientation:
    //                    match = (await db.GetAllOrientationsAsync()).FirstOrDefault(o => o.Name == f.Value);
    //                    break;

    //                case MappingSourceType.Staff:
    //                    match = (await db.GetAllStaffAsync()).FirstOrDefault(s => s.FullName == f.Value);
    //                    break;

    //                case MappingSourceType.Student:
    //                    match = (await db.GetStudentsAsync()).FirstOrDefault(s => s.FullName == f.Value);
    //                    break;
    //                    // Добавляй другие таблицы здесь по аналогии
    //            }

    //            if (match != null)
    //            {
    //                var prop = match.GetType().GetProperty(f.SelectedColumn);
    //                if (prop != null)
    //                {
    //                    var extracted = prop.GetValue(match)?.ToString() ?? "";
    //                    Debug.WriteLine($"f.Placeholder = {f.Placeholder} extracted = {extracted}");
    //                    result[f.Placeholder] = extracted;
    //                    continue;
    //                }
    //            }
    //        }

    //        if (f.IsChairmanSelector)
    //        {
    //            result[f.Placeholder + "_CHAIRMAN_ID"] = f.SelectedChairmanId.ToString();
    //            continue;
    //        }

    //        if (f.IsMemberAndSecretarySelector)
    //        {
    //            result[f.Placeholder + "_SECRETARY_ID"] = f.SelectedSecretaryId.ToString();
    //            for (int i = 0; i < f.MemberPickers.Count; i++)
    //            {
    //                result[$"{f.Placeholder}_MEMBER{i + 1}_ID"] = f.MemberPickers[i].SelectedStaffId.ToString();
    //                Debug.WriteLine("[Members check] " + f.MemberPickers[i].SelectedStaffId.ToString());
    //            }
    //            continue;
    //        }


    //        // Обычная запись
    //        Debug.WriteLine($"f.Placeholder = {f.Placeholder} val = {value}");
    //        result[f.Placeholder] = value;
    //    }

    //    _tcs.TrySetResult(result);
    //}
    /// <summary>
    /// Собирает значения всех полей + «авто-маркеров» в словарь result
    /// и завершает TaskCompletionSource.
    /// </summary>
    private async void Submit()
    {
        var result = new Dictionary<string, string>();
        var db = MauiProgram.GetService<DatabaseService>();

        // ------------------------------------------------------------------
        // 1.  Проходимся по полям формы
        // ------------------------------------------------------------------
        foreach (var f in Fields)
        {
            string value = f.Value;

            /* ---------- форматируем дату / время ---------- */
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

            /* ---------- длинный текст с ограничением по строкам ---------- */
            if (f.SourceType == MappingSourceType.ManualText)
            {
                result[f.Placeholder] = f.Value;
                result[$"{f.Placeholder}__min"] = f.MinLines.ToString();
                continue;
            }

            /* ---------- справочники с выбором колонки ---------- */
            if (f.IsPicker && !string.IsNullOrEmpty(f.SelectedColumn))
            {
                object? match = f.SourceType switch
                {
                    MappingSourceType.Institute => (await db.GetAllInstitutesAsync()).FirstOrDefault(i => i.Name == f.Value),
                    MappingSourceType.Department => (await db.GetAllDepartmentsAsync()).FirstOrDefault(d => d.Name == f.Value),
                    MappingSourceType.FormOfEducation => (await db.GetAllFormsOfEducationAsync()).FirstOrDefault(e => e.Name == f.Value),
                    MappingSourceType.Orientation => (await db.GetAllOrientationsAsync()).FirstOrDefault(o => o.Name == f.Value),
                    MappingSourceType.Staff => (await db.GetAllStaffAsync()).FirstOrDefault(s => s.FullName == f.Value),
                    MappingSourceType.Student => (await db.GetStudentsAsync()).FirstOrDefault(s => s.FullName == f.Value),
                    _ => null
                };

                if (match is not null)
                {
                    var prop = match.GetType().GetProperty(f.SelectedColumn);
                    var extracted = prop?.GetValue(match)?.ToString() ?? "";
                    result[f.Placeholder] = extracted;
                    continue;
                }
            }

            /* ---------- председатель, члены, секретарь ---------- */
            if (f.IsChairmanSelector)
            {
                result[$"{f.Placeholder}_CHAIRMAN_ID"] = f.SelectedChairmanId.ToString();
                continue;
            }
            if (f.IsMemberAndSecretarySelector)
            {
                result[$"{f.Placeholder}_SECRETARY_ID"] = f.SelectedSecretaryId.ToString();
                for (int i = 0; i < f.MemberPickers.Count; i++)
                    result[$"{f.Placeholder}_MEMBER{i + 1}_ID"] = f.MemberPickers[i].SelectedStaffId.ToString();
                continue;
            }

            /* ---------- обычное текстовое поле ---------- */
            result[f.Placeholder] = value;
        }

        // ------------------------------------------------------------------
        // 2.  Дописываем значения авто-маркеров ProtocolAuto…  (если есть контекст)
        // ------------------------------------------------------------------
        if (_ctxStudent is not null)
        {
            result["ProtocolAutoFIO"] = _ctxStudent.FullName;
            result["ProtocolAutoGroupName"] =
                (await db.GetGroupByIdAsync(_ctxStudent.groupId))?.Name ?? "";
            var orient = await db.GetOrientationByIdAsync(_ctxStudent.orientation);
            result["ProtocolAutoOrientationCode"] = orient?.Code ?? "";
            result["ProtocolAutoOrientationName"] = orient?.Name ?? "";
        }

        if (_ctxFqw is not null)
        {
            result["ProtocolAutoDate"] = _ctxFqw.Date.ToString("dd.MM.yyyy");
            result["ProtocolAutoTime"] = _ctxFqw.Date.ToString("HH:mm");
            result["ProtocolAutoFqwTopic"] = _ctxFqw.Topic ?? "";
        }

        if (_ctxSupervisor is not null)
            result["ProtocolAutoSupervisorFio"] = _ctxSupervisor.FullName;

        // ------------------------------------------------------------------
        // 3.  Завершаем TaskCompletionSource
        // ------------------------------------------------------------------
        _tcs.TrySetResult(result);
    }


    public partial class FormField : ObservableObject
    {
        public bool IsChairmanSelector { get; set; }
        public bool IsMemberAndSecretarySelector { get; set; }


        public List<string> SelectedMembers { get; set; } = new();
        public List<string> AllStaffNames { get; set; } = new();

        [ObservableProperty]
        private string selectedSecretary = string.Empty;
        [ObservableProperty]
        private string selectedChairman = string.Empty;

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
        public List<Staff> AllStaff { get; set; } = new();
        public ObservableCollection<MemberSelection> MemberPickers { get; set; } = new();

        [ObservableProperty]
        private int selectedChairmanId;
        [ObservableProperty]
        private int selectedSecretaryId;

        public List<Staff> SecretaryCandidates { get; set; } = new();

    }
    public partial class MemberSelection : ObservableObject
    {
        // Сам объект Staff, который выбирает пользователь
        [ObservableProperty]
        private Staff? selectedStaff;

        [ObservableProperty]
        private long selectedStaffId;
        public List<Staff> StaffOptions { get; set; } = new(); // не строки, а целые объекты Staff
                                                               // При смене Staff сразу обновляем Id
        partial void OnSelectedStaffChanged(Staff? value)
        {
            SelectedStaffId = value?.Id ?? 0;
        }
    }



}
