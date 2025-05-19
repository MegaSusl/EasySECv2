using SQLite;

namespace EasySECv2.Models
{
    [Table("institute")]
    public class Institute
    {
        [PrimaryKey, AutoIncrement]
        public long id { get; set; }

        [NotNull]
        public string name { get; set; }

        public string shortName { get; set; }
    }
}
