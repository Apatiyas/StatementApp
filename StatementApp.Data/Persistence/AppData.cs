using System.Collections.Generic;
using StatementApp.Core.Entities;

namespace StatementApp.Data.Persistence
{
    
    internal class AppData
    {
        public List<Statement> Statements { get; set; } = new();
        public List<Group> Groups { get; set; } = new();
        public List<Teacher> Teachers { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
    }
}
