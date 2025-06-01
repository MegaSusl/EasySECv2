using EasySECv2.Attributes;
using SQLite;

namespace EasySECv2.Models
{
    [Table("room")]
    public class Room
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long Id { get; set; }

        [NotNull]
        [Editable("Номер", Order = 10, ControlType = "Entry")]
        public string Name { get; set; }
    }
}
