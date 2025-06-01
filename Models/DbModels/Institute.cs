using SQLite;
using EasySECv2.Attributes;

namespace EasySECv2.Models
{
    [Table("institute")]
    public class Institute
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long Id { get; set; }

        [NotNull]
        [Editable("Название", Order = 10)]
        public string Name { get; set; }

        [Editable("Короткое название", Order = 20)]
        public string ShortName { get; set; }
    }
}
