using EasySECv2.Attributes;
using SQLite;

namespace EasySECv2.Models
{
    [Table("orientation")]
    public class Orientation
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long Id { get; set; }

        [NotNull]
        [Editable("Название", Order = 10)]
        public string Name { get; set; }

        [Editable("Код", Order = 20)]
        public string Code { get; set; }
    }

}
