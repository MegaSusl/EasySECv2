using SQLite;
using EasySECv2.Attributes;

namespace EasySECv2.Models
{
    [Table("formOfEducation")]
    public class FormOfEducation
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 10)]
        public long id { get; set; }

        [NotNull]
        [Editable("Название", Order = 20)]
        public string name { get; set; }
    }
}
