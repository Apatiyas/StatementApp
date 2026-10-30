using StatementApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StatementApp.Core.Validation
{
    public class EmptyRowsValidator : BaseValidator
    {
        public override ValidationResult Validate(Statement statement)
        {
            if (statement.Rows.Count == 0) return ValidationResult.Fail("В ведомости нет ни одного студента.");

            return base.Validate(statement);
        }
    }
}
