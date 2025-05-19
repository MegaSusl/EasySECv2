using SQLite;

namespace EasySECv2.Models
{
    [Table("orientation")]
    public class Orientation
    {
        [PrimaryKey, AutoIncrement]
        public long id { get; set; }

        [NotNull]
        public string name { get; set; }

        public string code { get; set; }
    }

}
