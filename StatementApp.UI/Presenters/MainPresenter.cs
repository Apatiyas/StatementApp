using System.Globalization;
using StatementApp.Core.Abstractions;
using StatementApp.Core.Entities;
using StatementApp.Core.Enums;
using StatementApp.Core.Validation;
using StatementApp.UI.ViewModels;
using StatementApp.UI.Views;

namespace StatementApp.UI.Presenters
{
    public class MainPresenter
    {
       
        private const string AbsentText = "Неявка";
        private const string NotAdmittedText = "Не допущен";
        private const string PassedText = "Зачтено";
        private const string FailedText = "Не зачтено";

        private const int CreditPassedScore = 5;
        private const int CreditFailedScore = 2;

      
        private static readonly StringComparer RussianComparer =
            StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), false);

        private readonly IMainView _view;
        private readonly IStatementRepository _repository;
        private readonly IStatementValidator _validator;
        private readonly IStatementExporter _exporter;

        private Statement? _current;
        private bool _dirty; 

        public MainPresenter(
            IMainView view,
            IStatementRepository repository,
            IStatementValidator validator,
            IStatementExporter exporter)
        {
            _view = view;
            _repository = repository;
            _validator = validator;
            _exporter = exporter;

            _view.AssessmentFormChanged += (_, _) => _view.SetGradeOptions(GradeOptions(_view.Header.AssessmentForm));
            _view.GroupChanged += (_, _) => OnGroupChanged();
            _view.SubjectChanged += (_, _) => OnSubjectChanged();
            _view.DirectoriesChanged += (_, _) => Safe(ReloadDirectories);
            _view.CreateClicked += (_, _) => Safe(CreateStatement);
            _view.NewClicked += (_, _) => Safe(StartNew);
            _view.OpenClicked += (_, _) => Safe(OpenSaved);
            _view.SaveClicked += (_, _) => Safe(Save);
            _view.CloseStatementClicked += (_, _) => Safe(CloseStatement);
            _view.ExportClicked += (_, _) => Safe(Export);
            _view.GradeEdited += (_, e) => OnGradeEdited(e);

            ReloadDirectories();
            _view.SetGradeOptions(GradeOptions(_view.Header.AssessmentForm));
            _view.SetState(EditorState.Header);
        }

   
        private void OnGroupChanged()
        {
            _view.SetSpecialty(_view.Header.Group?.Specialty ?? "");
            ShowGroupPreview();
        }

        private void OnSubjectChanged()
        {
            
            var subject = _view.Header.Subject;
            if (subject is not null)
                _view.SetHours(subject.Hours);
        }

       
        private void ShowGroupPreview()
        {
            if (_current is not null) return;

            var group = _view.Header.Group;
            _view.ShowRows(group is null ? Array.Empty<StatementRowViewModel>() : PreviewRows(group));
        }

        private static IReadOnlyList<StatementRowViewModel> PreviewRows(Group group) =>
            group.Students
                .OrderBy(s => s.Name, RussianComparer)
                .Select((s, i) => new StatementRowViewModel(i + 1, s.Name, s.GradeBookNumber, "", "", s.Id))
                .ToList();

       

        private void ReloadDirectories()
        {
            _view.SetGroups(_repository.GetGroups().OrderBy(g => g.Name).ToList());
            _view.SetSubjects(_repository.GetSubjects().OrderBy(s => s.Name).ToList());
            _view.SetTeachers(_repository.GetTeacher().OrderBy(t => t.Name).ToList());
        }

        private void CreateStatement()
        {
            var h = _view.Header;

            if (h.Group is null) { _view.ShowError("Выберите группу. Если списка нет, добавьте группу на вкладке «Справочники»."); return; }
            if (string.IsNullOrWhiteSpace(h.AcademicYear)) { _view.ShowError("Укажите учебный год."); return; }
            if (string.IsNullOrWhiteSpace(h.Discipline)) { _view.ShowError("Укажите дисциплину: выберите её из списка или введите название."); return; }
            if (h.Hours <= 0) { _view.ShowError("Укажите количество часов по плану (больше нуля)."); return; }
            if (h.Teacher is null) { _view.ShowError("Выберите преподавателя. Если списка нет, добавьте его на вкладке «Справочники»."); return; }
            if (string.IsNullOrWhiteSpace(h.Group.Specialty)) { _view.ShowError($"У группы {h.Group.Name} не указана специальность. Заполните её на вкладке «Справочники», в разделе «Группы»."); return; }
            if (h.Group.Students.Count == 0) { _view.ShowError($"В группе {h.Group.Name} нет студентов. Добавьте их на вкладке «Справочники», в разделе «Студенты»."); return; }
            if (h.StudentIds.Count == 0) { _view.ShowError("Отметьте флажками хотя бы одного студента в списке под шапкой."); return; }

            if (_current is { IsClosed: false } && _dirty &&
                !_view.Confirm("Несохранённая ведомость будет потеряна. Продолжить?"))
                return;

           
            var statement = new Statement
            {
                FormOfEducation = h.FormOfEducation,
                AcademicYear = h.AcademicYear,
                AssessmentForm = h.AssessmentForm,
                Semester = h.Semester,
                GroupName = h.Group.Name,
                Specialty = h.Group.Specialty,
                Discipline = h.Discipline,
                TotalHours = h.Hours,
                TeacherName = h.Teacher.Name,
                Date = h.Date
            };

            statement.PopulateStudents(h.Group, h.StudentIds);

            _current = statement;
            _dirty = true;
            ShowCurrent();
        }

        private void OpenSaved()
        {
            if (_current is { IsClosed: false } && _dirty &&
                !_view.Confirm("Несохранённая ведомость будет потеряна. Продолжить?"))
                return;

            while (true)
            {
                var list = _repository.GetStatements()
                    .OrderByDescending(s => s.Date)
                    .ThenByDescending(s => s.Id)
                    .Select(s => new StatementSummary(s.Id, s.Date, s.GroupName, s.Discipline, s.IsClosed ? "Закрыта" : "Черновик"))
                    .ToList();

                if (list.Count == 0)
                {
                    _view.ShowInfo("Сохранённых ведомостей пока нет.");
                    return;
                }

                var pick = _view.PickStatement(list);
                if (pick is null) return;

                if (pick.Action == PickAction.Delete)
                {
                    if (_view.Confirm("Удалить выбранную ведомость без возможности восстановления?"))
                    {
                        _repository.DeleteStatement(pick.Id);
                        if (_current?.Id == pick.Id)
                        {
                            _current = null;
                            ShowGroupPreview();
                            _view.SetState(EditorState.Header);
                        }
                    }
                    continue;
                }

                var statement = _repository.GetStatementById(pick.Id);
                if (statement is null)
                {
                    _view.ShowError("Ведомость не найдена.");
                    continue;
                }

                _current = statement;
                _dirty = false;
                ShowCurrent();
                return;
            }
        }

        private void OnGradeEdited(GradeEditedEventArgs e)
        {
            if (_current is null) return;

            try
            {
                var (score, status) = ParseInput(e.Input);
                _current.SetResult(e.SequenceNumber, score, status);
                _dirty = true;
            }
            catch (Exception ex) when (IsUserError(ex))
            {
                _view.ShowError(ex.Message);
            }
            finally
            {
                
                RefreshRow(e.SequenceNumber);
            }
        }

        private void Save()
        {
            if (_current is null) return;

            _repository.SaveStatement(_current);
            _dirty = false;
            _view.ShowInfo("Ведомость сохранена.");
        }

        private void CloseStatement()
        {
            if (_current is null) return;

            var result = _validator.Validate(_current);
            if (!result.IsValid)
            {
                _view.ShowError(result.ErrorMessage ?? "Ведомость заполнена неверно.");
                return;
            }

            if (!_view.Confirm("После закрытия ведомость нельзя изменить. Закрыть ведомость?"))
                return;

            _current.Close();
            _repository.SaveStatement(_current);
            _dirty = false;
            _view.SetState(EditorState.Closed);

            if (_view.Confirm("Ведомость закрыта и сохранена. Выгрузить её в Word сейчас?"))
                Export();
        }

        private void Export()
        {
            if (_current is null) return;

            if (!_current.IsClosed &&
                !_view.Confirm("Ведомость ещё не закрыта. Выгрузить её как есть (оценки могут быть выставлены не все)?"))
                return;

            var path = _view.AskSavePath(_exporter.SuggestFileName(_current));
            if (path is null) return;

            _exporter.Export(_current, path);

            if (_view.Confirm($"Файл сохранён:\n{path}\n\nОткрыть его сейчас?"))
                _view.OpenFile(path);
        }

        private void StartNew()
        {
            if (_current is { IsClosed: false } && _dirty &&
                !_view.Confirm("Несохранённые изменения будут потеряны. Начать новую ведомость?"))
                return;

            _current = null;
            _dirty = false;
            ShowGroupPreview();
            _view.SetState(EditorState.Header);
        }

     
        private void ShowCurrent()
        {
            var statement = _current!;

         
            _view.SetGradeOptions(GradeOptions(statement.AssessmentForm));
            _view.ShowSummary(BuildSummary(statement));
            _view.ShowRows(BuildRows(statement));
            _view.SetState(statement.IsClosed ? EditorState.Closed : EditorState.Editing);
        }

        private void RefreshRow(int sequenceNumber)
        {
            var row = _current?.Rows.FirstOrDefault(r => r.SequenceNumber == sequenceNumber);
            if (row is null || _current is null) return;

            _view.SetRowResult(
                sequenceNumber,
                ToInput(row.Grade, _current.AssessmentForm),
                row.Grade.ToDisplayText(_current.AssessmentForm));
        }

        private static IReadOnlyList<string> GradeOptions(AssessmentForm form) =>
            form == AssessmentForm.Credit
                ? new[] { "", PassedText, FailedText, AbsentText, NotAdmittedText }
                : new[] { "", "5", "4", "3", "2", AbsentText, NotAdmittedText };

        private static string BuildSummary(Statement s)
        {
            var lines = new List<string>
            {
                $"{s.FormOfEducation} форма обучения · {s.AcademicYear}, семестр {s.Semester} · {s.AssessmentForm.ToDisplayText()}",
                $"Группа {s.GroupName} · {s.Specialty}",
                $"{s.Discipline} · {s.TotalHours} ч · {s.TeacherName} · {s.Date:dd.MM.yyyy}"
            };

            if (s.IsClosed)
                lines.Add("Ведомость закрыта, изменения запрещены.");

            return string.Join(Environment.NewLine, lines);
        }

        private static IReadOnlyList<StatementRowViewModel> BuildRows(Statement statement) =>
            statement.Rows
                .Select(r => new StatementRowViewModel(
                    r.SequenceNumber,
                    r.StudentName,
                    r.GradeBookNumber,
                    ToInput(r.Grade, statement.AssessmentForm),
                    r.Grade.ToDisplayText(statement.AssessmentForm)))
                .ToList();

        private static (int? Score, GradeStatus Status) ParseInput(string? input)
        {
            var text = input?.Trim() ?? "";

            if (text.Length == 0) return (null, GradeStatus.NotSet);
            if (text == AbsentText) return (null, GradeStatus.Absent);
            if (text == NotAdmittedText) return (null, GradeStatus.NotAdmitted);
            if (text == PassedText) return (CreditPassedScore, GradeStatus.Graded);
            if (text == FailedText) return (CreditFailedScore, GradeStatus.Graded);
            if (int.TryParse(text, out var score)) return (score, GradeStatus.Graded);

            throw new FormatException($"Неизвестное значение результата: «{text}».");
        }

        private static string ToInput(Grade grade, AssessmentForm form) => grade.Status switch
        {
            GradeStatus.Absent => AbsentText,
            GradeStatus.NotAdmitted => NotAdmittedText,
            GradeStatus.Graded when form == AssessmentForm.Credit =>
                grade.Score >= 3 ? PassedText : FailedText,
            GradeStatus.Graded => grade.Score?.ToString() ?? "",
            _ => ""
        };

        private static bool IsUserError(Exception ex) =>
            ex is ArgumentException or InvalidOperationException or FormatException
                or IOException or UnauthorizedAccessException;

        private void Safe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex) when (IsUserError(ex))
            {
                _view.ShowError(ex.Message);
            }
        }
    }
}
