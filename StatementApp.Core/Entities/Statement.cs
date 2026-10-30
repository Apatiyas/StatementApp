using StatementApp.Core.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace StatementApp.Core.Entities
{
   public class Statement
    {
        public int Id { get; set; }

        [JsonInclude] public bool IsClosed { get; private set; }

        // Реквизиты шапки 
        public required string FormOfEducation { get; init; }
        public required string AcademicYear { get; init; }
        public AssessmentForm AssessmentForm { get; init; }
        public required string GroupName { get; init; }
        public required string Discipline { get; init; }
        public int TotalHours { get; init; }
        public required string TeacherName { get; init; }
        public DateOnly Date { get; init; }

        // Семестр и специальность.
        
        public string Semester { get; init; } = string.Empty;
        public string Specialty { get; init; } = string.Empty;

       
        [JsonInclude] public List<StatementRow> RowList { get; private set; } = new();

        [JsonIgnore] public IReadOnlyList<StatementRow> Rows => RowList;

        public Statement() { }

        private void EnsureNotClosed()
        {
            if (IsClosed)
                throw new InvalidOperationException("Ведомость закрыта и подписана. Изменения запрещены.");
        }

        public void Close()
        {
            EnsureNotClosed();

            if (RowList.Count == 0)
                throw new InvalidOperationException("Нельзя закрыть пустую ведомость.");

            if (RowList.Any(r => r.Grade.Status == GradeStatus.NotSet))
                throw new InvalidOperationException("Не всем студентам выставлен результат.");

            IsClosed = true;
        }

        /// Добавляет в ведомость всех студентов группы
        public void PopulateStudents(Group group) => Populate(group, group.Students);

        ///Добавляет в ведомость только выбранных студентов группы 
        public void PopulateStudents(Group group, IReadOnlyCollection<int> studentIds)
        {
            ArgumentNullException.ThrowIfNull(group);
            ArgumentNullException.ThrowIfNull(studentIds);

            var chosen = group.Students.Where(s => studentIds.Contains(s.Id)).ToList();
            if (chosen.Count == 0)
                throw new InvalidOperationException("Не выбрано ни одного студента.");

            Populate(group, chosen);
        }

        private void Populate(Group group, IEnumerable<Student> students)
        {
            EnsureNotClosed();

            if (RowList.Count > 0)
                throw new InvalidOperationException("Список студентов уже заполнен.");

            if (!string.Equals(group.Name, GroupName, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Группа не совпадает с указанной в шапке ведомости.");

            var russianComparer = StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), false);
            var sortedStudents = students.OrderBy(s => s.Name, russianComparer).ToList();

            int sequenceNumber = 1;
            foreach (var student in sortedStudents)
            {
                RowList.Add(new StatementRow(sequenceNumber++, student));
            }
        }

        public void SetResult(int sequenceNumber, int? score, GradeStatus status)
        {
            EnsureNotClosed();

            var row = RowList.FirstOrDefault(r => r.SequenceNumber == sequenceNumber);

            if (row == null)
                throw new ArgumentException($"Строка ведомости не найдена.");

            if (status != GradeStatus.Graded && score.HasValue)
                throw new ArgumentException("балл указывается только для статуса Graded.");

            switch (status)
            {
                case GradeStatus.Graded:
                    if (score is not int value)
                        throw new ArgumentException("Для статуса Graded нужен балл.");
                    row.Grade.SetScore(value);
                    break;
                case GradeStatus.Absent:
                    row.Grade.MarkAbsent();
                    break;
                case GradeStatus.NotAdmitted:
                    row.Grade.MarkNotAdmitted();
                    break;
                case GradeStatus.NotSet:
                    row.Grade.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Неизвестный статус.");
            }
        }
    }
}
