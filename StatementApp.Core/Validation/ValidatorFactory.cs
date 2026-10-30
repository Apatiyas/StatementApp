using System;
using System.Collections.Generic;
using System.Text;

namespace StatementApp.Core.Validation
{
    public class ValidatorFactory
    {
        public static IStatementValidator Create()
        {
            var header = new HeaderValidator();
            var empty = new EmptyRowsValidator();
            var rows = new RowsValidator();
            var grades = new GradesValidator();

           
            header.SetNext(empty)
                  .SetNext(rows)
                  .SetNext(grades);

            return header;
        }

    }
}
