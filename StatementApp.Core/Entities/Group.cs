using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace StatementApp.Core.Entities
{
    public class Group
    {
        public int Id { get; set; }

        private string _name = string.Empty;
        private string _specialty = string.Empty;
        private List<Student> _students = new();

        public required string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Название группы не может быть пустым.")
                : value.Trim();
        }

        
        public string Specialty
        {
            get => _specialty;
            set => _specialty = value?.Trim() ?? string.Empty;
        }

       
        public List<Student> StudentList
        {
            get => _students;
            set => _students = value ?? new();
        }

        [JsonIgnore]
        public IReadOnlyList<Student> Students => _students.AsReadOnly();

        public Group() { }

        [SetsRequiredMembers]
        public Group(string name) => Name = name;

        public bool AddStudent(Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            if (_students.Any(s => string.Equals(s.GradeBookNumber.Trim(), student.GradeBookNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            _students.Add(student);
            return true;
        }

        public bool RemoveStudent(string gradeBookNumber)
        {
            ArgumentNullException.ThrowIfNull(gradeBookNumber);

            return _students.RemoveAll(s =>
                string.Equals(s.GradeBookNumber.Trim(), gradeBookNumber.Trim(), StringComparison.OrdinalIgnoreCase)) > 0;
        }

        public override string ToString() => Name; // Для отображения в WinForms ComboBox
    }
}
