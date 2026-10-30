using System;
using System.Collections.Generic;
using System.Text;
using StatementApp.Core.Entities;
namespace StatementApp.Core.Abstractions
{
    public interface IStatementRepository
    {
        IReadOnlyList<Statement> GetStatements();
        IReadOnlyList<Group> GetGroups();
        IReadOnlyList<Teacher> GetTeacher();
        IReadOnlyList<Subject> GetSubjects();

        Statement? GetStatementById(int id);

        
        void SaveStatement(Statement statement);
        void SaveGroup(Group group);
        void SaveTeacher(Teacher teacher);
        void SaveSubject(Subject subject);

        void DeleteStatement(int id);
        void DeleteGroup(int id);
        void DeleteTeacher(int id);
        void DeleteSubject(int id);
    }
}
