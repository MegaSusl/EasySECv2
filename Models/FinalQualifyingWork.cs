using SQLite;

namespace EasySECv2.Models
{
    [Table("fqw")]
    public class FinalQualifyingWork
    {
        [PrimaryKey, AutoIncrement]
        public long id { get; set; }

        public string topic { get; set; }

        [Indexed]
        public long studentId { get; set; }

        [Indexed]
        public long supervisorId { get; set; }

        public int mark { get; set; }

        public string recomendation { get; set; }

        public string recomendation2 { get; set; }

        public string questions { get; set; }

        public string description { get; set; }

        public string disadvantage { get; set; }

        public string addInfo { get; set; }

        public DateTime dateTime { get; set; }

        public bool attended { get; set; }
    }
}
