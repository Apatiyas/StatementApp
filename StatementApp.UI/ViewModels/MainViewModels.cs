using StatementApp.Core.Entities;
using StatementApp.Core.Enums;

namespace StatementApp.UI.ViewModels
{
    
    public record StatementRowViewModel(
        int Number,
        string StudentName,
        string GradeBookNumber,
        string Result,
        string ResultText,
        int StudentId = 0);

    ///Что пользователь выбрал и ввёл в шапке
    public record StatementHeaderInput(
        string AcademicYear,
        string FormOfEducation,
        AssessmentForm AssessmentForm,
        string Semester,
        Group? Group,
        Subject? Subject,   
        string Discipline,  
        int Hours,          
        Teacher? Teacher,
        DateOnly Date,
        IReadOnlyList<int> StudentIds); 

    ///Строка в списке сохранённых ведомостей
    public record StatementSummary(int Id, DateOnly Date, string GroupName, string Discipline, string Status);

    public enum PickAction { Open, Delete }

    public record StatementPick(int Id, PickAction Action);

    public class GradeEditedEventArgs : EventArgs
    {
        public int SequenceNumber { get; }
        public string Input { get; }

        public GradeEditedEventArgs(int sequenceNumber, string input)
        {
            SequenceNumber = sequenceNumber;
            Input = input;
        }
    }

    ///Состояние вкладки «Ведомость
    public enum EditorState
    {
       
        Header,
        
        Editing,
      
        Closed
    }
}
