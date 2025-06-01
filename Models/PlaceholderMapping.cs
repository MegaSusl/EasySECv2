using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySECv2.Models
{
    public class PlaceholderMapping
    {
        public string Placeholder { get; set; } = string.Empty;
        public MappingSourceType SourceType { get; set; }
        public string? Property { get; set; } // напр. ФИО, ГРУППА и т.п.
        public string? Format { get; set; }
        public int? ManualMinLines { get; set; }
    }
    public enum MappingSourceType
    {
        Manual,     // ручной ввод
        ManualText,  // длинный текст — Editor
        ManualTimeFull,  // время пикер
        ManualDate,  // DatePicker
        Group,      // данные из таблицы групп
        Calculated, // текущая дата, месяц и т.п.
        Table,       // выбор произвольной таблицы

        FormOfEducation,
        Institute,
        Orientation,
        Department,
        Staff,
        Student,    // данные из таблицы студентов

        TableMembersAndSecretary,     // [ТАБЛИЦА_ПРЕДСЕДАТЕЛЬ]
        TableChairman,      // [ТАБЛИЦА_ЧЛЕНЫ]        

    }

}
