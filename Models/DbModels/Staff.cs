using EasySECv2.Attributes;
using SQLite;

namespace EasySECv2.Models
{
    [Table("staff")]
    public partial class Staff
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long Id { get; set; }

        [NotNull]
        [Editable("Имя", Order = 20)]
        public string Name { get; set; }

        [NotNull]
        [Editable("Фамилия", Order = 10)]
        public string Surname { get; set; }

        [Editable("Отчество", Order = 30)]
        public string MiddleName { get; set; }

        [Editable("Должность", Order = 40)]
        public string Position { get; set; }

        [Editable("Ученая степень", Order = 50)]
        public string Degree { get; set; }
        
        [Editable("Ученая звание", Order = 60)]
        public string DegreeRank { get; set; }

        [Editable("Награды", Order = 70)]
        public string DegreeAwards { get; set; }
        [Editable("Является гостем", Order = 80)]
        public bool IsGuest { get; set; }
        [Editable("Является дипломным руководителем", Order = 90)]
        public bool IsSupervisor { get; set; }

    }
    public partial class Staff
    {
        // Чтобы sqlite‑net не пытался мапить это свойство в столбец
        [Ignore]
        public string FullName
            => $"{Surname} {Name}{(string.IsNullOrWhiteSpace(MiddleName) ? "" : $" {MiddleName}")}";
        [Ignore]
        public string Email { get; set; }
        [Ignore]
        public string Phone { get; set; }
    }
}
