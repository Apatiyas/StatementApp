using StatementApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StatementApp.Core.Validation
{
    public class BaseValidator: IStatementValidator
    {
        private IStatementValidator? _next;

        public virtual ValidationResult Validate(Statement statement)
        {
            return _next?.Validate(statement) ?? ValidationResult.Success();
        }

       public IStatementValidator SetNext(IStatementValidator next)
        {
            _next = next;
            return next;
        }
    }
}
