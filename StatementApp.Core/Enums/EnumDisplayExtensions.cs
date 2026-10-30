namespace StatementApp.Core.Enums
{
    /// <summary>Подписи для перечислений: используются и в окне, и в документе Word.</summary>
    public static class EnumDisplayExtensions
    {
        public static string ToDisplayText(this AssessmentForm form) => form switch
        {
            AssessmentForm.Exam => "Экзамен",
            AssessmentForm.Credit => "Зачёт",
            AssessmentForm.DifferentiatedCredit => "Дифференцированный зачёт",
            _ => string.Empty
        };
    }
}
