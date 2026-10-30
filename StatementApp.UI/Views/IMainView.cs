using StatementApp.Core.Entities;
using StatementApp.UI.ViewModels;

namespace StatementApp.UI.Views
{
    /// <summary>
    /// Что презентер знает о главной форме. Форма не обращается к хранилищу и не считает оценки:
    /// она показывает данные и сообщает о действиях пользователя.
    /// </summary>
    public interface IMainView
    {
        // что сделал пользователь
        event EventHandler? AssessmentFormChanged;
        event EventHandler? GroupChanged;
        event EventHandler? SubjectChanged;
        event EventHandler? CreateClicked;
        event EventHandler? NewClicked;
        event EventHandler? OpenClicked;
        event EventHandler? SaveClicked;
        event EventHandler? CloseStatementClicked;
        event EventHandler? ExportClicked;
        event EventHandler<GradeEditedEventArgs>? GradeEdited;

        /// <summary>Изменился справочник (группы, студенты, дисциплины, преподаватели).</summary>
        event EventHandler? DirectoriesChanged;

        // что ввёл пользователь
        StatementHeaderInput Header { get; }

        // что показать
        void SetGroups(IReadOnlyList<Group> groups);
        void SetSubjects(IReadOnlyList<Subject> subjects);
        void SetTeachers(IReadOnlyList<Teacher> teachers);
        void SetGradeOptions(IReadOnlyList<string> options);
        void SetSpecialty(string text);
        void SetHours(int hours);
        void ShowSummary(string text);
        void ShowRows(IReadOnlyList<StatementRowViewModel> rows);
        void SetRowResult(int sequenceNumber, string result, string resultText);
        void SetState(EditorState state);

        // диалоги
        StatementPick? PickStatement(IReadOnlyList<StatementSummary> statements);
        string? AskSavePath(string suggestedFileName);
        void OpenFile(string path);
        void ShowError(string message);
        void ShowInfo(string message);
        bool Confirm(string message);
    }
}
