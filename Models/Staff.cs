using SQLite;

namespace EasySECv2.Models
{
    [Table("staff")]
    public class Staff
    {
        [PrimaryKey, AutoIncrement]
        public long id { get; set; }

        [NotNull]
        public string name { get; set; }

        [NotNull]
        public string surname { get; set; }

        public string middleName { get; set; }

        public string job { get; set; }

        public string degree { get; set; }

        public string degreeRank { get; set; }

        public string degreeAwards { get; set; }

        [Indexed]
        public long position { get; set; }
    }
}
