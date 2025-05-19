using System;

namespace EasySECv2.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EditableAttribute : Attribute
    {
        /// <summary>
        /// Лейбл, который выведем перед контролом.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Порядковый номер — чем меньше, тем выше по списку.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Тип контрола: "Entry", "Picker" и пр. Если null — Entry.
        /// </summary>
        public string ControlType { get; set; }

        public EditableAttribute(string label)
        {
            Label = label;
            Order = int.MaxValue;
            ControlType = null;
        }
    }
}
