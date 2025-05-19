using SQLite;

namespace EasySECv2.Models
{
    [Table("department")]
    public class Department
    {
        [PrimaryKey, AutoIncrement]
        public long id { get; set; }

        [NotNull]
        public string name { get; set; }

        public string shortName { get; set; }
    }
}
