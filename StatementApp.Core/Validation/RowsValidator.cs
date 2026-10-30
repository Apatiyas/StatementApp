using System;
using System.Collections.Generic;
using System.Text;
using StatementApp.Core.Entities;

namespace StatementApp.Core.Validation
{
    internal class RowsValidator : BaseValidator
    {
        public override ValidationResult Validate(Statement statement)
        {
               
           
            var numbers = new HashSet<int>();
            var gradeBooks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in statement.Rows)
            {
                if (row is null)
                    return ValidationResult.Fail("В ведомости есть пустая строка.");

                if (row.SequenceNumber < 1)
                    return ValidationResult.Fail($"{row.StudentName}: порядковый номер должен быть не меньше 1.");

                if (!numbers.Add(row.SequenceNumber))
                    return ValidationResult.Fail($"Порядковый номер {row.SequenceNumber} повторяется.");

                if (string.IsNullOrWhiteSpace(row.StudentName))
                    return ValidationResult.Fail($"№{row.SequenceNumber}: не указано ФИО студента.");

                if (string.IsNullOrWhiteSpace(row.GradeBookNumber))
                    return ValidationResult.Fail($"№{row.SequenceNumber} {row.StudentName}: не указан номер зачётной книжки.");

                if (!gradeBooks.Add(row.GradeBookNumber.Trim()))
                    return ValidationResult.Fail($"№{row.SequenceNumber} {row.StudentName}: номер зачётной книжки {row.GradeBookNumber} уже есть в ведомости.");
            }

           
            if (numbers.Count > 0 && numbers.Max() != statement.Rows.Count)
                return ValidationResult.Fail("В нумерации строк есть пропуски.");

            return base.Validate(statement);
        }
    }
}
    

