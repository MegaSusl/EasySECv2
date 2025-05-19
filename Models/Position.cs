using SQLite;

namespace EasySECv2.Models
{
    [Table("position")]
    public class Position
    {
        [PrimaryKey, AutoIncrement]
        public long id { get; set; }

        [NotNull]
        public string name { get; set; }
    }
}
