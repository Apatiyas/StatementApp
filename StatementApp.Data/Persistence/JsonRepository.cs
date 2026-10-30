using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using StatementApp.Core.Abstractions;
using StatementApp.Core.Entities;

namespace StatementApp.Data.Persistence
{
    
    public class JsonRepository : IStatementRepository
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            // кириллица
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // перечисления словами (Exam, Graded)
            Converters = { new JsonStringEnumConverter() }
        };

        public static string DefaultPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StatementApp", "data.json");

        private readonly string _path;
        private readonly AppData _data;

        public JsonRepository() : this(DefaultPath) { }

        public JsonRepository(string path)
        {
            _path = Path.GetFullPath(path);
            _data = Load();
        }

        
        public IReadOnlyList<Statement> GetStatements() => _data.Statements.ToList();

        public IReadOnlyList<Group> GetGroups() => _data.Groups.ToList();

        public IReadOnlyList<Teacher> GetTeacher() => _data.Teachers.ToList();

        public IReadOnlyList<Subject> GetSubjects() => _data.Subjects.ToList();

        public Statement? GetStatementById(int id) =>
            _data.Statements.FirstOrDefault(s => s.Id == id);

     

        public void SaveStatement(Statement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);

            if (statement.Id == 0)
                statement.Id = NextId(_data.Statements.Select(s => s.Id));

            Upsert(_data.Statements, statement, s => s.Id);
            Persist();
        }

        public void SaveGroup(Group group)
        {
            ArgumentNullException.ThrowIfNull(group);

            if (_data.Groups.Any(g => g.Id != group.Id && SameName(g.Name, group.Name)))
                throw new InvalidOperationException($"Группа «{group.Name}» уже есть в справочнике.");

            if (group.Id == 0)
                group.Id = NextId(_data.Groups.Select(g => g.Id));

          
            var nextStudentId = NextId(_data.Groups.Concat(new[] { group })
                .SelectMany(g => g.Students)
                .Select(s => s.Id));
            foreach (var student in group.Students.Where(s => s.Id == 0).ToList())
                student.Id = nextStudentId++;

            Upsert(_data.Groups, group, g => g.Id);
            Persist();
        }

        public void SaveTeacher(Teacher teacher)
        {
            ArgumentNullException.ThrowIfNull(teacher);

            if (_data.Teachers.Any(t => t.Id != teacher.Id && SameName(t.Name, teacher.Name)))
                throw new InvalidOperationException($"Преподаватель «{teacher.Name}» уже есть в справочнике.");

            if (teacher.Id == 0)
                teacher.Id = NextId(_data.Teachers.Select(t => t.Id));

            Upsert(_data.Teachers, teacher, t => t.Id);
            Persist();
        }

        public void SaveSubject(Subject subject)
        {
            ArgumentNullException.ThrowIfNull(subject);

            if (_data.Subjects.Any(s => s.Id != subject.Id && SameName(s.Name, subject.Name)))
                throw new InvalidOperationException($"Дисциплина «{subject.Name}» уже есть в справочнике.");

            if (subject.Id == 0)
                subject.Id = NextId(_data.Subjects.Select(s => s.Id));

            Upsert(_data.Subjects, subject, s => s.Id);
            Persist();
        }

       

        public void DeleteStatement(int id)
        {
            if (_data.Statements.RemoveAll(s => s.Id == id) > 0) Persist();
        }

        public void DeleteGroup(int id)
        {
            if (_data.Groups.RemoveAll(g => g.Id == id) > 0) Persist();
        }

        public void DeleteTeacher(int id)
        {
            if (_data.Teachers.RemoveAll(t => t.Id == id) > 0) Persist();
        }

        public void DeleteSubject(int id)
        {
            if (_data.Subjects.RemoveAll(s => s.Id == id) > 0) Persist();
        }

     

        private static bool SameName(string a, string b) =>
            string.Equals(a.Trim(), b.Trim(), StringComparison.CurrentCultureIgnoreCase);

        private static int NextId(IEnumerable<int> existing) =>
            existing.DefaultIfEmpty(0).Max() + 1;

        private static void Upsert<T>(List<T> list, T item, Func<T, int> idOf)
        {
            var index = list.FindIndex(x => idOf(x) == idOf(item));
            if (index >= 0) list[index] = item;
            else list.Add(item);
        }

        private AppData Load()
        {
            if (!File.Exists(_path)) return new AppData();

            try
            {
                var json = File.ReadAllText(_path);
                var data = JsonSerializer.Deserialize<AppData>(json, Options) ?? new AppData();
                EnsureIds(data);
                return data;
            }
            catch (JsonException ex)
            {
                
                throw new InvalidDataException(
                    $"Файл данных повреждён и не может быть прочитан:\n{_path}\n\n{ex.Message}", ex);
            }
        }

      
        private static void EnsureIds(AppData data)
        {
            FillMissingIds(data.Groups, g => g.Id, (g, id) => g.Id = id);
            FillMissingIds(data.Teachers, t => t.Id, (t, id) => t.Id = id);
            FillMissingIds(data.Subjects, s => s.Id, (s, id) => s.Id = id);
            FillMissingIds(data.Statements, s => s.Id, (s, id) => s.Id = id);
            FillMissingIds(data.Groups.SelectMany(g => g.Students).ToList(), s => s.Id, (s, id) => s.Id = id);
        }

        private static void FillMissingIds<T>(List<T> items, Func<T, int> getId, Action<T, int> setId)
        {
            var next = NextId(items.Select(getId));
            foreach (var item in items.Where(i => getId(i) == 0).ToList())
                setId(item, next++);
        }

        private void Persist()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

           
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_data, Options));
            File.Move(tmp, _path, overwrite: true);
        }
    }
}
