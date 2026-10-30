using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StatementApp.Core.Entities
{
   public class Teacher
    {
        public int Id { get; set; }

        private string _name = string.Empty;

        public required string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Имя преподавателя не может быть пустым.")
                : value.Trim();
        }

        public Teacher() { }

        [SetsRequiredMembers]
        public Teacher(string name) => Name = name;

        public override string ToString() => Name; // Для отображения в WinForms ComboBox
    }
}
