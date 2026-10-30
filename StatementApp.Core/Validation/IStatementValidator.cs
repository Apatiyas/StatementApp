using StatementApp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace StatementApp.Core.Validation
{
    public interface IStatementValidator
    {
        IStatementValidator SetNext(IStatementValidator next);

        ValidationResult Validate(Statement statement);
    }
} 
