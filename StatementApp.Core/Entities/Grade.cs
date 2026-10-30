using StatementApp.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace StatementApp.Core.Entities
{
    public class Grade
    {
        [JsonInclude] public GradeStatus Status { get; private set; } = GradeStatus.NotSet;

        [JsonIgnore]
        public bool IsAcademicDebt =>
         Status is GradeStatus.Absent or GradeStatus.NotAdmitted
        || (Status == GradeStatus.Graded && Score < 3);

        [JsonInclude] public int? Score { get; private set; }

       
        internal void SetScore(int score)
        {
           
            if (score is < 2 or > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(score), "Оценка должна быть от 2 до 5.");
            }
            Status = GradeStatus.Graded;
            Score = score;
        }

        internal void MarkAbsent()
        {
            Status = GradeStatus.Absent;
            Score = null;
        }

        internal void MarkNotAdmitted()
        {
            Status = GradeStatus.NotAdmitted;
            Score = null;
        }

        internal void Clear()
        {
            Status = GradeStatus.NotSet;
            Score = null;
        }

        public string ToDisplayText(AssessmentForm form)
        {
            return Status switch
            {
                GradeStatus.NotSet => "",
                GradeStatus.Absent => "Неявка",
                GradeStatus.NotAdmitted => "Не допущен",
                GradeStatus.Graded => form switch
                {
                    AssessmentForm.Exam or AssessmentForm.DifferentiatedCredit => Score switch
                    {
                        5 => "Отлично",
                        4 => "Хорошо",
                        3 => "Удовлетворительно",
                        2 => "Неудовлетворительно",
                        _ => "Ошибка"
                    },
                    AssessmentForm.Credit => Score >= 3 ? "Зачтено" : "Не зачтено",
                    _ => "Ошибка"
                },
                _ => "Ошибка"
            };
        }
    }
}
