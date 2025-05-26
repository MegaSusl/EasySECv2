using SQLite;
using EasySECv2.Attributes;

namespace EasySECv2.Models
{
    [Table("institute")]
    public class Institute
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 0, ControlType = "Entry")]
        public long id { get; set; }

        [NotNull]
        [Editable("Название", Order = 10)]
        public string name { get; set; }

        [Editable("Короткое название", Order = 20)]
        public string shortName { get; set; }
    }
}
