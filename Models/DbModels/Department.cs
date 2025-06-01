using EasySECv2.Attributes;
using SQLite;

namespace EasySECv2.Models
{
    [Table("department")]
    public class Department
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long Id { get; set; }

        [NotNull]
        [Editable("Название", Order = 10)]
        public string Name { get; set; }

        [Editable("Короткая форма", Order = 20)]
        public string ShortName { get; set; }

        [Editable("Аудитория", Order = 30)]
        public long Room { get; set; }
    }
}
