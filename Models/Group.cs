using SQLite;

namespace EasySECv2.Models
{
    [Table("group")]
    public class Group
    {
        [PrimaryKey, AutoIncrement]
        public long id { get; set; }

        [NotNull]
        public string name { get; set; }
    }
}
