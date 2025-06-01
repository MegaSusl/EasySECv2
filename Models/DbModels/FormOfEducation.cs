using SQLite;
using EasySECv2.Attributes;

namespace EasySECv2.Models
{
    [Table("formOfEducation")]
    public class FormOfEducation
    {
        [PrimaryKey, AutoIncrement]
        [Editable("ID", Order = 10)]
        public long Id { get; set; }

        [NotNull]
        [Editable("Название", Order = 20)]
        public string Name { get; set; }
    }
}
