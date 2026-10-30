using System.Globalization;
using StatementApp.Core.Abstractions;
using StatementApp.Core.Entities;

namespace StatementApp.UI.Views
{
    /// <summary>
    /// Вкладка «Справочники»: группы, студенты, дисциплины и преподаватели.
    /// Всё заполняется вручную; каждое изменение сразу записывается в хранилище,
    /// после чего вызывается DataChanged, чтобы главная форма обновила свои списки.
    /// </summary>
    public sealed class DirectoriesControl : UserControl
    {
        private static readonly StringComparer Russian =
            StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: true);

        private const string SpecialtyHint =
            "Специальность печатается в ведомости, например: 54.01.20 Графический дизайнер.";

        private readonly IStatementRepository _repository;

        private readonly DataGridView _groups = new();
        private readonly DataGridView _students = new();
        private readonly DataGridView _subjects = new();
        private readonly DataGridView _teachers = new();
        private readonly Label _studentsTitle = MakeTitle("Студенты");

        // true, пока список перезаполняется: события выбора строки в это время игнорируются
        private bool _loading;

        public event EventHandler? DataChanged;

        public DirectoriesControl(IStatementRepository repository)
        {
            _repository = repository;

            BuildLayout();
            RefreshAll();
        }

        // ================== Построение интерфейса ==================

        private void BuildLayout()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };

            // --- группы и студенты рядом ---
            SetupGrid(_groups, ("Название группы", 32), ("Специальность", 48), ("Студентов", 20));
            SetupGrid(_students, ("№", 10), ("Фамилия, имя, отчество", 58), ("Номер зачётной книжки", 32));

            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            split.Controls.Add(BuildListPanel(MakeTitle("Группы"), _groups,
                Tool("Добавить", AddGroup),
                Tool("Изменить", EditGroup),
                Tool("Удалить", DeleteGroup)), 0, 0);

            split.Controls.Add(BuildListPanel(_studentsTitle, _students,
                Tool("Добавить", AddStudent),
                Tool("Изменить", EditStudent),
                Tool("Удалить", DeleteStudent),
                Tool("Вставить список…", ImportStudents)), 1, 0);

            var pageGroups = new TabPage("Группы и студенты");
            pageGroups.Controls.Add(split);

            // --- дисциплины ---
            SetupGrid(_subjects, ("Название дисциплины", 75), ("Часов по учебному плану", 25));
            var pageSubjects = new TabPage("Дисциплины");
            pageSubjects.Controls.Add(BuildListPanel(MakeTitle("Дисциплины"), _subjects,
                Tool("Добавить", AddSubject),
                Tool("Изменить", EditSubject),
                Tool("Удалить", DeleteSubject)));

            // --- преподаватели ---
            SetupGrid(_teachers, ("Фамилия, имя, отчество", 100));
            var pageTeachers = new TabPage("Преподаватели");
            pageTeachers.Controls.Add(BuildListPanel(MakeTitle("Преподаватели"), _teachers,
                Tool("Добавить", AddTeacher),
                Tool("Изменить", EditTeacher),
                Tool("Удалить", DeleteTeacher)));

            tabs.TabPages.Add(pageGroups);
            tabs.TabPages.Add(pageSubjects);
            tabs.TabPages.Add(pageTeachers);
            Controls.Add(tabs);

            _groups.SelectionChanged += (_, _) =>
            {
                if (!_loading) RefreshStudents();
            };

            _groups.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditGroup(); };
            _students.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditStudent(); };
            _subjects.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSubject(); };
            _teachers.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditTeacher(); };
        }

        private static Label MakeTitle(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            Padding = new Padding(0, 4, 0, 6)
        };

        private static Button Tool(string text, Action onClick)
        {
            var button = UiStyle.MakeButton(text);
            button.Click += (_, _) => onClick();
            return button;
        }

        private static void SetupGrid(DataGridView grid, params (string Header, float Weight)[] columns)
        {
            UiStyle.StyleGrid(grid);
            grid.ReadOnly = true;

            foreach (var (header, weight) in columns)
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, FillWeight = weight });
        }

        private static Panel BuildListPanel(Label title, DataGridView grid, params Button[] buttons)
        {
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 0, 0, 4)
            };
            toolbar.Controls.AddRange(buttons);

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };

            // Fill добавляется первым: Dock обрабатывается от последнего добавленного к первому
            panel.Controls.Add(grid);
            panel.Controls.Add(toolbar);
            panel.Controls.Add(title);
            return panel;
        }

        // ================== Обновление списков ==================

        private void RefreshAll()
        {
            RefreshGroups();
            RefreshSubjects();
            RefreshTeachers();
        }

        private Group? SelectedGroup => Current<Group>(_groups);

        private void RefreshGroups(int? selectId = null)
        {
            selectId ??= SelectedGroup?.Id;

            _loading = true;
            try
            {
                _groups.Rows.Clear();
                foreach (var group in _repository.GetGroups().OrderBy(x => x.Name, Russian))
                {
                    var index = _groups.Rows.Add(group.Name, group.Specialty, group.Students.Count);
                    _groups.Rows[index].Tag = group;
                }

                SelectRow(_groups, tag => tag is Group found && found.Id == selectId);
            }
            finally
            {
                _loading = false;
            }

            RefreshStudents();
        }

        private void RefreshStudents(Student? select = null)
        {
            _students.Rows.Clear();

            var group = SelectedGroup;
            _studentsTitle.Text = group is null ? "Студенты" : $"Студенты группы {group.Name}";
            if (group is null) return;

            var number = 1;
            foreach (var student in group.Students.OrderBy(x => x.Name, Russian))
            {
                var index = _students.Rows.Add(number++, student.Name, student.GradeBookNumber);
                _students.Rows[index].Tag = student;
            }

            if (select is not null)
                SelectRow(_students, tag => ReferenceEquals(tag, select));
        }

        private void RefreshSubjects(int? selectId = null)
        {
            selectId ??= Current<Subject>(_subjects)?.Id;

            _subjects.Rows.Clear();
            foreach (var subject in _repository.GetSubjects().OrderBy(x => x.Name, Russian))
            {
                var index = _subjects.Rows.Add(subject.Name, subject.Hours);
                _subjects.Rows[index].Tag = subject;
            }

            SelectRow(_subjects, tag => tag is Subject found && found.Id == selectId);
        }

        private void RefreshTeachers(int? selectId = null)
        {
            selectId ??= Current<Teacher>(_teachers)?.Id;

            _teachers.Rows.Clear();
            foreach (var teacher in _repository.GetTeacher().OrderBy(x => x.Name, Russian))
            {
                var index = _teachers.Rows.Add(teacher.Name);
                _teachers.Rows[index].Tag = teacher;
            }

            SelectRow(_teachers, tag => tag is Teacher found && found.Id == selectId);
        }

        private static void SelectRow(DataGridView grid, Func<object?, bool> match)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (!match(row.Tag)) continue;

                grid.CurrentCell = row.Cells[0];
                return;
            }
        }

        private static T? Current<T>(DataGridView grid) where T : class => grid.CurrentRow?.Tag as T;

        // ================== Группы ==================

        private void AddGroup()
        {
            var values = Ask("Новая группа",
                new[]
                {
                    new FieldSpec { Label = "Название группы" },
                    new FieldSpec { Label = "Специальность" }
                },
                v => GroupNameTaken(v[0], null) ? "Группа с таким названием уже есть." : null,
                SpecialtyHint);
            if (values is null) return;

            Run(() =>
            {
                var group = new Group(values[0]) { Specialty = values[1] };
                _repository.SaveGroup(group);
                RefreshGroups(group.Id);
                Changed();
            });
        }

        private void EditGroup()
        {
            var group = SelectedGroup;
            if (group is null) { Info("Выберите группу в списке."); return; }

            var values = Ask("Изменить группу",
                new[]
                {
                    new FieldSpec { Label = "Название группы", Value = group.Name },
                    new FieldSpec { Label = "Специальность", Value = group.Specialty }
                },
                v => GroupNameTaken(v[0], group) ? "Группа с таким названием уже есть." : null,
                SpecialtyHint);
            if (values is null) return;

            Run(() =>
            {
                group.Name = values[0];
                group.Specialty = values[1];
                _repository.SaveGroup(group);
                RefreshGroups(group.Id);
                Changed();
            });
        }

        private void DeleteGroup()
        {
            var group = SelectedGroup;
            if (group is null) { Info("Выберите группу в списке."); return; }

            if (!Confirm($"Удалить группу {group.Name} вместе со списком студентов ({group.Students.Count})?\n\n" +
                         "Уже сохранённые ведомости не изменятся."))
                return;

            Run(() =>
            {
                _repository.DeleteGroup(group.Id);
                RefreshGroups();
                Changed();
            });
        }

        private bool GroupNameTaken(string name, Group? except) =>
            _repository.GetGroups().Any(g => !ReferenceEquals(g, except) && Same(g.Name, name));

        // ================== Студенты ==================

        private void AddStudent()
        {
            var group = SelectedGroup;
            if (group is null) { Info("Сначала выберите или добавьте группу."); return; }

            var values = Ask($"Новый студент: группа {group.Name}", StudentFields("", ""),
                v => BookTaken(group, v[1], null) ? "В группе уже есть студент с таким номером зачётной книжки." : null);
            if (values is null) return;

            Run(() =>
            {
                var student = new Student(values[0], values[1]);
                group.AddStudent(student);
                _repository.SaveGroup(group);

                RefreshGroups(group.Id);
                RefreshStudents(student);
                Changed();
            });
        }

        private void EditStudent()
        {
            var group = SelectedGroup;
            var student = Current<Student>(_students);
            if (group is null || student is null) { Info("Выберите студента в списке."); return; }

            var values = Ask("Изменить студента", StudentFields(student.Name, student.GradeBookNumber),
                v => BookTaken(group, v[1], student) ? "В группе уже есть студент с таким номером зачётной книжки." : null);
            if (values is null) return;

            Run(() =>
            {
                student.Name = values[0];
                student.GradeBookNumber = values[1];
                _repository.SaveGroup(group);

                RefreshGroups(group.Id);
                RefreshStudents(student);
                Changed();
            });
        }

        private void DeleteStudent()
        {
            var group = SelectedGroup;
            var student = Current<Student>(_students);
            if (group is null || student is null) { Info("Выберите студента в списке."); return; }

            if (!Confirm($"Удалить студента {student.Name} из группы {group.Name}?\n\n" +
                         "Уже сохранённые ведомости не изменятся."))
                return;

            Run(() =>
            {
                group.RemoveStudent(student.GradeBookNumber);
                _repository.SaveGroup(group);

                RefreshGroups(group.Id);
                Changed();
            });
        }

        // Вставка списка: по одной строке на студента, «ФИО; номер» или два столбца из Excel
        private void ImportStudents()
        {
            var group = SelectedGroup;
            if (group is null) { Info("Сначала выберите или добавьте группу."); return; }

            var values = Ask($"Список студентов: группа {group.Name}",
                new[] { new FieldSpec { Label = "Список", Kind = FieldKind.Multiline } },
                v => ParseStudents(v[0]).Error,
                "Строка: ФИО; номер зачётной книжки (или через табуляцию, как в Excel). Повторяющиеся номера пропускаются.");
            if (values is null) return;

            Run(() =>
            {
                var added = 0;
                var skipped = 0;

                foreach (var (name, book) in ParseStudents(values[0]).Items)
                {
                    if (group.AddStudent(new Student(name, book))) added++;
                    else skipped++;
                }

                if (added > 0)
                {
                    _repository.SaveGroup(group);
                    Changed();
                }

                RefreshGroups(group.Id);
                Info($"Добавлено студентов: {added}. Пропущено (номер уже есть в группе): {skipped}.");
            });
        }

        private static (List<(string Name, string Book)> Items, string? Error) ParseStudents(string text)
        {
            var items = new List<(string Name, string Book)>();
            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;

                var parts = line.Split('\t', ';').Select(p => p.Trim()).ToArray();
                if (parts.Length < 2 || parts[0].Length == 0 || parts[1].Length == 0)
                    return (items, $"Строка {i + 1}: нужны ФИО и номер зачётной книжки через «;» или табуляцию.");

                items.Add((parts[0], parts[1]));
            }

            return items.Count == 0 ? (items, "Список пуст.") : (items, null);
        }

        private static FieldSpec[] StudentFields(string name, string book) => new[]
        {
            new FieldSpec { Label = "Фамилия, имя, отчество", Value = name },
            new FieldSpec { Label = "Номер зачётной книжки", Value = book }
        };

        private static bool BookTaken(Group group, string book, Student? except) =>
            group.Students.Any(s => !ReferenceEquals(s, except) && Same(s.GradeBookNumber, book));

        // ================== Дисциплины ==================

        private void AddSubject()
        {
            var values = Ask("Новая дисциплина", SubjectFields("", ""), v => ValidateSubject(v, null));
            if (values is null) return;

            Run(() =>
            {
                var subject = new Subject(values[0], int.Parse(values[1]));
                _repository.SaveSubject(subject);
                RefreshSubjects(subject.Id);
                Changed();
            });
        }

        private void EditSubject()
        {
            var subject = Current<Subject>(_subjects);
            if (subject is null) { Info("Выберите дисциплину в списке."); return; }

            var values = Ask("Изменить дисциплину", SubjectFields(subject.Name, subject.Hours.ToString()),
                v => ValidateSubject(v, subject));
            if (values is null) return;

            Run(() =>
            {
                subject.Name = values[0];
                subject.Hours = int.Parse(values[1]);
                _repository.SaveSubject(subject);
                RefreshSubjects(subject.Id);
                Changed();
            });
        }

        private void DeleteSubject()
        {
            var subject = Current<Subject>(_subjects);
            if (subject is null) { Info("Выберите дисциплину в списке."); return; }

            if (!Confirm($"Удалить дисциплину {subject.Name}?\n\nУже сохранённые ведомости не изменятся."))
                return;

            Run(() =>
            {
                _repository.DeleteSubject(subject.Id);
                RefreshSubjects();
                Changed();
            });
        }

        private string? ValidateSubject(IReadOnlyList<string> values, Subject? except)
        {
            if (int.Parse(values[1]) <= 0) return "Количество часов должно быть больше нуля.";

            return _repository.GetSubjects().Any(s => !ReferenceEquals(s, except) && Same(s.Name, values[0]))
                ? "Дисциплина с таким названием уже есть."
                : null;
        }

        private static FieldSpec[] SubjectFields(string name, string hours) => new[]
        {
            new FieldSpec { Label = "Название дисциплины", Value = name },
            new FieldSpec { Label = "Часов по учебному плану", Value = hours, Kind = FieldKind.Number }
        };

        // ================== Преподаватели ==================

        private void AddTeacher()
        {
            var values = Ask("Новый преподаватель", TeacherFields(""), v => ValidateTeacher(v, null));
            if (values is null) return;

            Run(() =>
            {
                var teacher = new Teacher(values[0]);
                _repository.SaveTeacher(teacher);
                RefreshTeachers(teacher.Id);
                Changed();
            });
        }

        private void EditTeacher()
        {
            var teacher = Current<Teacher>(_teachers);
            if (teacher is null) { Info("Выберите преподавателя в списке."); return; }

            var values = Ask("Изменить преподавателя", TeacherFields(teacher.Name), v => ValidateTeacher(v, teacher));
            if (values is null) return;

            Run(() =>
            {
                teacher.Name = values[0];
                _repository.SaveTeacher(teacher);
                RefreshTeachers(teacher.Id);
                Changed();
            });
        }

        private void DeleteTeacher()
        {
            var teacher = Current<Teacher>(_teachers);
            if (teacher is null) { Info("Выберите преподавателя в списке."); return; }

            if (!Confirm($"Удалить преподавателя {teacher.Name}?\n\nУже сохранённые ведомости не изменятся."))
                return;

            Run(() =>
            {
                _repository.DeleteTeacher(teacher.Id);
                RefreshTeachers();
                Changed();
            });
        }

        private string? ValidateTeacher(IReadOnlyList<string> values, Teacher? except) =>
            _repository.GetTeacher().Any(t => !ReferenceEquals(t, except) && Same(t.Name, values[0]))
                ? "Преподаватель с таким именем уже есть."
                : null;

        private static FieldSpec[] TeacherFields(string name) => new[]
        {
            new FieldSpec { Label = "Фамилия, имя, отчество", Value = name }
        };

        // ================== Вспомогательное ==================

        private IReadOnlyList<string>? Ask(
            string title,
            IReadOnlyList<FieldSpec> fields,
            Func<IReadOnlyList<string>, string?>? validate = null,
            string? hint = null)
        {
            using var dialog = new EntryDialog(title, fields, validate, hint);
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Values : null;
        }

        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
                                          or IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool Confirm(string text) =>
            MessageBox.Show(this, text, "Подтверждение", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;

        private void Info(string text) =>
            MessageBox.Show(this, text, "Справочники", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private void Changed() => DataChanged?.Invoke(this, EventArgs.Empty);

        private static bool Same(string a, string b) =>
            string.Equals(a.Trim(), b.Trim(), StringComparison.CurrentCultureIgnoreCase);
    }
}
