using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace StatementApp.Core.Entities
{
    public class StatementRow
    {
        [JsonInclude] public int SequenceNumber { get; internal set; }

        //ИсSnapshot 
        public required int StudentId { get; init; }
        public required string StudentName { get; init; }
        public required string GradeBookNumber { get; init; }

        [JsonInclude] public Grade Grade { get; private set; } = new();

        [JsonConstructor]
        private StatementRow() { }

        [SetsRequiredMembers]
        public StatementRow(int sequenceNumber, Student student)
        {
            ArgumentNullException.ThrowIfNull(student);

            if (sequenceNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(sequenceNumber));

            SequenceNumber = sequenceNumber;
            StudentId = student.Id;
            StudentName = student.Name;
            GradeBookNumber = student.GradeBookNumber;
        }
    }
}
