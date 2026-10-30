using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StatementApp.Core.Entities
{
    public class Student
    {
        public int Id { get; set; } 

        private string _name = string.Empty;
        private string _gradeBookNumber = string.Empty;

        public required string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("ФИО не может быть пустым.")
                : value.Trim();
        }

        public required string GradeBookNumber
        {
            get => _gradeBookNumber;
            set => _gradeBookNumber = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Номер зачётной книжки не может быть пустым.")
                : value.Trim();
        }

        public Student() { }

        [SetsRequiredMembers]
        public Student(string name, string gradeBookNumber)
        {
            Name = name;
            GradeBookNumber = gradeBookNumber;
        }
    }
}
