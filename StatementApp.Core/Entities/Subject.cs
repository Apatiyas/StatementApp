using System;
using System.Diagnostics.CodeAnalysis;

namespace StatementApp.Core.Entities
{
    ///Дисциплина из справочника
    public class Subject
    {
        public int Id { get; set; } 

        private string _name = string.Empty;
        private int _hours;

        public required string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Название дисциплины не может быть пустым.")
                : value.Trim();
        }

        public int Hours
        {
            get => _hours;
            set => _hours = value <= 0
                ? throw new ArgumentException("Количество часов должно быть больше нуля.")
                : value;
        }

        public Subject() { }

        [SetsRequiredMembers]
        public Subject(string name, int hours)
        {
            Name = name;
            Hours = hours;
        }

        public override string ToString() => Name; // Для отображения в WinForms ComboBox
    }
}
