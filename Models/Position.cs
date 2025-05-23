using EasySECv2.Attributes;
using SQLite;

namespace EasySECv2.Models
{
    [Table("position")]
    public class Position
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long id { get; set; }

        [NotNull]
        [Editable("Должность", Order = 10)]
        public string name { get; set; }
    }
}
