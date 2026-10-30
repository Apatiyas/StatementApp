using System;
using System.Collections.Generic;
using System.Text;

namespace StatementApp.Core.Validation
{
    public class ValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }

        private ValidationResult(bool isValid, string? error)
        {
            IsValid = isValid;
            ErrorMessage = error;
        }

        public static ValidationResult Success() => new(true, null);
        public static ValidationResult Fail(string message) => new(false, message);
    }
}
