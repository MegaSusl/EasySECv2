using SQLite;
using EasySECv2.Attributes;

namespace EasySECv2.Models
{
    [Table("student")]
    public partial class Student
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long Id { get; set; }

        [NotNull]
        [Editable("Имя", Order = 20)]
        public string name { get; set; }

        [NotNull]
        [Editable("Фамилия", Order = 10)]
        public string surname { get; set; }

        [Editable("Отчество", Order = 30)]
        public string middleName { get; set; }

        // Внешние ключи (SQLite не проконтролирует FK, но для ясности оставляем свойства)
        [Indexed]
        [Editable("Группа", Order = 40, ControlType = "Picker")]

        public long groupId { get; set; }

        [Indexed]
        [Editable("Направление", Order = 50, ControlType = "Picker")]
        public long orientation { get; set; }

        [Indexed]
        [Editable("Формат обучения", Order = 50, ControlType = "Picker")]
        public long formOfEducation { get; set; }

        [Indexed]
        [Editable("Институт", Order = 50, ControlType = "Picker")]
        public long institute { get; set; }

        [Indexed]
        [Editable("Кафедра", Order = 50, ControlType = "Picker")]
        public long department { get; set; }
        
        [Indexed]
        [Editable("Допущен к защите", Order = 60, ControlType = "CheckBox")]
        public bool isAccessed { get; set; }

        [Indexed]
        [Editable("Дата выпуска", Order = 70, ControlType = "DatePicker")]
        public DateTime? ReleaseYear { get; set; }
    }
    public partial class Student
    {
        // Чтобы sqlite‑net не пытался мапить это свойство в столбец
        [Ignore]
        public string FullName
            => $"{surname} {name}{(string.IsNullOrWhiteSpace(middleName) ? "" : $" {middleName}")}";
        [Ignore]
        public string email { get; set; }
        [Ignore]
        public string phone { get; set; }
        [Ignore]
        public string GroupName { get; set; }
        [Ignore]
        public string OrientationName { get; set; }

        public string GetPropertyValue(string? property)
        {
            return property switch
            {
                "ФИО" => FullName,
                "ГРУППА" => GroupName,
                _ => string.Empty
            };
        }
    }
}
