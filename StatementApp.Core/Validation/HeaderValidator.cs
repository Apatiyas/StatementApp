using StatementApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StatementApp.Core.Validation
{
    public class HeaderValidator : BaseValidator
    {
        public override ValidationResult Validate(Statement statement)
        {
            if (string.IsNullOrWhiteSpace(statement.FormOfEducation)) return ValidationResult.Fail("Не указана форма обучения.");
            if (string.IsNullOrWhiteSpace(statement.AcademicYear)) return ValidationResult.Fail("Не указан учебный год.");
            if (string.IsNullOrWhiteSpace(statement.Semester)) return ValidationResult.Fail("Не указан семестр.");
            if (string.IsNullOrWhiteSpace(statement.GroupName)) return ValidationResult.Fail("Не указана группа.");
            if (string.IsNullOrWhiteSpace(statement.Specialty)) return ValidationResult.Fail("Не указана специальность.");
            if (string.IsNullOrWhiteSpace(statement.Discipline)) return ValidationResult.Fail("Не указана дисциплина.");
            if (statement.TotalHours <= 0) return ValidationResult.Fail("Количество часов должно быть больше нуля.");
            if (string.IsNullOrWhiteSpace(statement.TeacherName)) return ValidationResult.Fail("Не указан преподаватель.");
            if (statement.Date > DateOnly.FromDateTime(DateTime.Today)) return ValidationResult.Fail("Дата проведения не может быть в будущем.");
            if (!Enum.IsDefined(statement.AssessmentForm)) return ValidationResult.Fail("Неизвестная форма аттестации.");

            return base.Validate(statement);
        }
    }
}
