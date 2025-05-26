using EasySECv2.Attributes;
using SQLite;

namespace EasySECv2.Models
{
    [Table("staff")]
    public partial class Staff
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long id { get; set; }

        [NotNull]
        [Editable("Имя", Order = 20)]
        public string name { get; set; }

        [NotNull]
        [Editable("Фамилия", Order = 10)]
        public string surname { get; set; }

        [Editable("Отчество", Order = 30)]
        public string middleName { get; set; }

        [Editable("Работа", Order = 40)]
        public string job { get; set; }

        [Editable("Степень", Order = 50)]
        public string degree { get; set; }
        
        [Editable("Степень другая??", Order = 60)]
        public string degreeRank { get; set; }

        [Editable("Награды", Order = 70)]
        public string degreeAwards { get; set; }

        [Indexed]
        [Editable("Должность?", Order = 80)]
        public long position { get; set; }
    }
    public partial class Staff
    {
        // Чтобы sqlite‑net не пытался мапить это свойство в столбец
        [Ignore]
        public string FullName
            => $"{surname} {name}{(string.IsNullOrWhiteSpace(middleName) ? "" : $" {middleName}")}";
        [Ignore]
        public string email { get; set; }
        [Ignore]
        public string phone { get; set; }
    }
}
