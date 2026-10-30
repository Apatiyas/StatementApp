using StatementApp.Core.Entities;
using StatementApp.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace StatementApp.Core.Validation
{
   public class GradesValidator:BaseValidator
    {
        public override ValidationResult Validate(Statement statement)
        {
            foreach(var row in statement.Rows)
            {
                var grade = row.Grade;
                var who = $"№{row.SequenceNumber} {row.StudentName}";

                if (grade is null)
                    return ValidationResult.Fail($"{who}: оценка отсутствует.");

                if (!Enum.IsDefined(grade.Status))
                    return ValidationResult.Fail($"{who}: неизвестный статус результата.");

                if (grade.Status == GradeStatus.NotSet)
                    return ValidationResult.Fail($"{who}: не выставлен результат.");

                if (grade.Status == GradeStatus.Graded && grade.Score is not (>= 2 and <= 5))
                    return ValidationResult.Fail($"{who}: балл должен быть от 2 до 5.");

                if (grade.Status != GradeStatus.Graded && grade.Score.HasValue)
                    return ValidationResult.Fail($"{who}: балл не должен быть указан при статусе «{grade.Status}».");
            }

            return base.Validate(statement);

        }
    }
}
