using StatementApp.Core.Entities;

namespace StatementApp.Core.Abstractions
{
    /// Выгрузка ведомости в файл 
    public interface IStatementExporter
    {
      
        string SuggestFileName(Statement statement);

        void Export(Statement statement, string filePath);
    }
}
