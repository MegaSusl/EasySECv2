using SQLite;
using EasySECv2.Attributes;

namespace EasySECv2.Models
{
    [Table("fqw")]
    public class FinalQualifyingWork
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }

        [Editable("Тема", Order = 0, ControlType = "Entry")]
        public string Topic { get; set; }

        [Indexed]
        public long StudentId { get; set; }

        [Indexed]
        [Editable("Дипломный руководитель", Order = 10, ControlType = "Picker")]
        public long SupervisorId { get; set; }
        [Editable("Оценка", Order = 20)]
        public int Mark { get; set; }
        [Editable("Вопросы", Order = 30)]
        public string Questions { get; set; }
        [Editable("Характеристика", Order = 40)]
        public string Description { get; set; }
        [Editable("Минусы", Order = 50)]
        public string Disadvantages { get; set; }
        [Editable("Доп. инфо", Order = 60)]
        public string AddInfo { get; set; }
        [Editable("Дата", Order = 70, ControlType = "DatePicker")]
        public DateTime Date { get; set; }
        [Editable("Присутствовал?", Order = 80, ControlType = "CheckBox")]
        public bool IsAttended { get; set; }
    }
}
