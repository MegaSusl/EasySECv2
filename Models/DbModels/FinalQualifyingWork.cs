using SQLite;

namespace EasySECv2.Models
{
    [Table("fqw")]
    public class FinalQualifyingWork
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        public string Topic { get; set; }

        [Indexed]
        public long StudentId { get; set; }

        [Indexed]
        public long SupervisorId { get; set; }

        public int Mark { get; set; }

        public string Questions { get; set; }

        public string Description { get; set; }

        public string Disadvantages { get; set; }

        public string AddInfo { get; set; }

        public DateTime Date { get; set; }

        public bool IsAttended { get; set; }
    }
}
