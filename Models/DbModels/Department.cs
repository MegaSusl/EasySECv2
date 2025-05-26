using EasySECv2.Attributes;
using SQLite;

namespace EasySECv2.Models
{
    [Table("department")]
    public class Department
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long id { get; set; }

        [NotNull]
        [Editable("Название", Order = 10)]
        public string name { get; set; }

        [Editable("Короткая форма", Order = 20)]
        public string shortName { get; set; }
    }
}
