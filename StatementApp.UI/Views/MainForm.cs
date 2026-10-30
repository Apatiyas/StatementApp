using System.Diagnostics;
using StatementApp.Core.Entities;
using StatementApp.Core.Enums;
using StatementApp.UI.ViewModels;

namespace StatementApp.UI.Views
{
    /// <summary>
    /// Главное окно. Логики здесь нет: форма показывает данные и сообщает презентеру
    /// о действиях пользователя (см. IMainView). Интерфейс строится кодом в BuildLayout(),
    /// поэтому в дизайнере форма пустая.
    /// </summary>
    public partial class MainForm : Form, IMainView
    {
        private sealed record AssessmentOption(AssessmentForm Value)
        {
            public override string ToString() => Value.ToDisplayText();
        }

        // ---------- вкладки ----------
        private readonly TabControl tabMain = new() { Dock = DockStyle.Fill };
        private readonly TabPage tabStatement = new("Ведомость");
        private readonly TabPage tabDirectories = new("Справочники");
        private readonly DirectoriesControl directories;

        // ---------- вкладка «Ведомость» ----------
        private readonly GroupBox gbParams = new();
        private readonly TableLayoutPanel tlpParams = new();
        private readonly Label lblSummary = new();
        private readonly DataGridView dgvRows = new();
        private readonly FlowLayoutPanel pnlButtons = new();

        // панель над списком студентов: видна, пока ведомость не сформирована
        private readonly FlowLayoutPanel pnlSelect = new();
        private readonly Button btnSelectAll = UiStyle.MakeButton("Выбрать всех");
        private readonly Button btnSelectNone = UiStyle.MakeButton("Снять выбор");
        private readonly Label lblSelected = new();

        private readonly Button btnOpen = UiStyle.MakeButton("Открыть сохранённую…");
        private readonly Button btnCreate = UiStyle.MakeButton("Сформировать");
        private readonly Button btnNew = UiStyle.MakeButton("Новая");
        private readonly Button btnSave = UiStyle.MakeButton("Сохранить");
        private readonly Button btnCloseStatement = UiStyle.MakeButton("Закрыть ведомость");
        private readonly Button btnExport = UiStyle.MakeButton("Выгрузить в Word");

        private readonly ComboBox cbYear = new() { DropDownStyle = ComboBoxStyle.DropDown };
        private readonly ComboBox cbSemester = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox cbForm = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox cbAssessment = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox cbGroup = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox txtSpecialty = new() { ReadOnly = true };
        // дисциплину можно выбрать из справочника или ввести вручную, часы можно править
        private readonly ComboBox cbSubject = new() { DropDownStyle = ComboBoxStyle.DropDown };
        private readonly NumericUpDown nudHours = new() { Minimum = 0, Maximum = 2000 };
        private readonly ComboBox cbTeacher = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly DateTimePicker dtpDate = new() { Format = DateTimePickerFormat.Short };

        private readonly DataGridViewCheckBoxColumn colInclude = new();
        private readonly DataGridViewTextBoxColumn colNo = new();
        private readonly DataGridViewTextBoxColumn colName = new();
        private readonly DataGridViewTextBoxColumn colBook = new();
        private readonly DataGridViewComboBoxColumn colResult = new();
        private readonly DataGridViewTextBoxColumn colResultText = new();

        // true, пока форма сама меняет ячейки: это не действие пользователя
        private bool _updating;

        public MainForm(DirectoriesControl directories)
        {
            this.directories = directories;

            InitializeComponent();
            BuildLayout();
        }

        // ================== IMainView: события ==================

        public event EventHandler? AssessmentFormChanged;
        public event EventHandler? GroupChanged;
        public event EventHandler? SubjectChanged;
        public event EventHandler? CreateClicked;
        public event EventHandler? NewClicked;
        public event EventHandler? OpenClicked;
        public event EventHandler? SaveClicked;
        public event EventHandler? CloseStatementClicked;
        public event EventHandler? ExportClicked;
        public event EventHandler<GradeEditedEventArgs>? GradeEdited;
        public event EventHandler? DirectoriesChanged;

        // ================== IMainView: данные из формы ==================

        public StatementHeaderInput Header => new(
            cbYear.Text.Trim(),
            cbForm.Text,
            (cbAssessment.SelectedItem as AssessmentOption)?.Value ?? AssessmentForm.Exam,
            cbSemester.Text,
            cbGroup.SelectedItem as Group,
            cbSubject.SelectedItem as Subject,
            cbSubject.Text.Trim(),
            (int)nudHours.Value,
            cbTeacher.SelectedItem as Teacher,
            DateOnly.FromDateTime(dtpDate.Value),
            CheckedStudentIds());

        // ================== IMainView: показ данных ==================

        public void SetGroups(IReadOnlyList<Group> groups) => FillCombo(cbGroup, groups, g => g.Id);

        public void SetSubjects(IReadOnlyList<Subject> subjects) => FillCombo(cbSubject, subjects, s => s.Id);

        public void SetTeachers(IReadOnlyList<Teacher> teachers) => FillCombo(cbTeacher, teachers, t => t.Id);

        public void SetGradeOptions(IReadOnlyList<string> options)
        {
            colResult.Items.Clear();
            colResult.Items.AddRange(options.Cast<object>().ToArray());
        }

        public void SetSpecialty(string text) => txtSpecialty.Text = text;

        public void SetHours(int hours) =>
            nudHours.Value = Math.Clamp(hours, (int)nudHours.Minimum, (int)nudHours.Maximum);

        public void ShowSummary(string text) => lblSummary.Text = text;

        public void ShowRows(IReadOnlyList<StatementRowViewModel> rows)
        {
            _updating = true;
            try
            {
                dgvRows.Rows.Clear();
                foreach (var r in rows)
                {
                    // новые строки отмечены: по умолчанию в ведомость идёт вся группа
                    var index = dgvRows.Rows.Add(true, r.Number, r.StudentName, r.GradeBookNumber, r.Result, r.ResultText);
                    dgvRows.Rows[index].Tag = r.StudentId;
                }
            }
            finally
            {
                _updating = false;
            }

            UpdateSelectedCount();
        }

        public void SetRowResult(int sequenceNumber, string result, string resultText)
        {
            foreach (DataGridViewRow row in dgvRows.Rows)
            {
                if (Convert.ToInt32(row.Cells[colNo.Index].Value) != sequenceNumber) continue;

                _updating = true;
                try
                {
                    row.Cells[colResult.Index].Value = result;
                    row.Cells[colResultText.Index].Value = resultText;
                }
                finally
                {
                    _updating = false;
                }
                return;
            }
        }

        public void SetState(EditorState state)
        {
            var header = state == EditorState.Header;

            // пока шапка заполняется, видны поля; когда ведомость открыта, вместо них краткая сводка
            gbParams.Visible = header;
            lblSummary.Visible = !header;

            btnCreate.Enabled = header;
            btnNew.Enabled = !header;
            btnSave.Enabled = state == EditorState.Editing;
            btnCloseStatement.Enabled = state == EditorState.Editing;
            btnExport.Enabled = !header;

            colResult.ReadOnly = state != EditorState.Editing;

            // до «Сформировать» в таблице только список студентов группы, без оценок
            colResult.Visible = !header;
            colResultText.Visible = !header;

            // флажки выбора студентов нужны только до «Сформировать»
            colInclude.Visible = header;
            pnlSelect.Visible = header;
        }

        // ================== IMainView: диалоги ==================

        public StatementPick? PickStatement(IReadOnlyList<StatementSummary> statements)
        {
            using var dialog = new StatementListDialog(statements);
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Result : null;
        }

        public string? AskSavePath(string suggestedFileName)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Сохранить ведомость",
                Filter = "Документ Word (*.docx)|*.docx",
                DefaultExt = "docx",
                AddExtension = true,
                FileName = suggestedFileName,
                OverwritePrompt = true
            };

            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
        }

        public void OpenFile(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                ShowError($"Не удалось открыть файл:\n{path}\n\n{ex.Message}");
            }
        }

        public void ShowError(string message) =>
            MessageBox.Show(this, message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public void ShowInfo(string message) =>
            MessageBox.Show(this, message, "Ведомость", MessageBoxButtons.OK, MessageBoxIcon.Information);

        public bool Confirm(string message) =>
            MessageBox.Show(this, message, "Подтверждение", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;

        // ================== Построение интерфейса ==================

        private void BuildLayout()
        {
            Font = new Font("Segoe UI", 10F);
            MinimumSize = new Size(1000, 640);

            tabMain.TabPages.Add(tabStatement);
            tabMain.TabPages.Add(tabDirectories);
            Controls.Add(tabMain);

            directories.Dock = DockStyle.Fill;
            tabDirectories.Controls.Add(directories);

            BuildButtons();
            BuildParams();
            BuildSummary();
            BuildSelectPanel();
            BuildGrid();

            // Порядок важен: Dock обрабатывается от последнего добавленного к первому.
            // Таблица (Fill) добавляется первой и занимает всё место, которое остаётся
            // между верхним блоком (параметры или сводка) и нижней панелью кнопок.
            tabStatement.Controls.Add(dgvRows);
            tabStatement.Controls.Add(pnlSelect);
            tabStatement.Controls.Add(lblSummary);
            tabStatement.Controls.Add(gbParams);
            tabStatement.Controls.Add(pnlButtons);

            WireEvents();
            SetState(EditorState.Header);
        }

        private void BuildButtons()
        {
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Height = 56;
            pnlButtons.FlowDirection = FlowDirection.RightToLeft;
            pnlButtons.Padding = new Padding(8);

            // при RightToLeft первый добавленный оказывается правее всех;
            // на экране слева направо: Открыть, Сформировать, Новая, Сохранить, Закрыть, Выгрузить
            pnlButtons.Controls.Add(btnExport);
            pnlButtons.Controls.Add(btnCloseStatement);
            pnlButtons.Controls.Add(btnSave);
            pnlButtons.Controls.Add(btnNew);
            pnlButtons.Controls.Add(btnCreate);
            pnlButtons.Controls.Add(btnOpen);
        }

        private void BuildParams()
        {
            gbParams.Text = "Параметры ведомости";
            gbParams.Dock = DockStyle.Top;
            gbParams.AutoSize = true;
            gbParams.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            gbParams.Padding = new Padding(8);

            tlpParams.Dock = DockStyle.Fill;
            tlpParams.AutoSize = true;
            tlpParams.ColumnCount = 4;
            tlpParams.RowCount = 5;
            // подпись | поле | подпись | поле
            tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpParams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (var i = 0; i < 5; i++)
                tlpParams.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Учебный год считается от текущей даты: с сентября начинается новый
            var startYear = DateTime.Today.Month >= 9 ? DateTime.Today.Year : DateTime.Today.Year - 1;
            foreach (var y in new[] { startYear - 1, startYear, startYear + 1 })
                cbYear.Items.Add($"{y}-{y + 1}");
            cbYear.SelectedItem = $"{startYear}-{startYear + 1}";

            cbSemester.Items.AddRange(new object[] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII" });
            cbSemester.SelectedIndex = 0;

            cbForm.Items.AddRange(new object[] { "Очная", "Очно-заочная", "Заочная" });
            cbForm.SelectedIndex = 0;

            cbAssessment.Items.AddRange(Enum.GetValues<AssessmentForm>()
                .Select(v => (object)new AssessmentOption(v)).ToArray());
            cbAssessment.SelectedIndex = 0;

            AddField("Учебный год", cbYear, 0, 0);
            AddField("Семестр", cbSemester, 2, 0);
            AddField("Форма обучения", cbForm, 0, 1);
            AddField("Форма аттестации", cbAssessment, 2, 1);
            AddField("Группа", cbGroup, 0, 2);
            AddField("Специальность", txtSpecialty, 2, 2);
            AddField("Дисциплина", cbSubject, 0, 3);
            AddField("Часов по плану", nudHours, 2, 3);
            AddField("Преподаватель", cbTeacher, 0, 4);
            AddField("Дата проведения", dtpDate, 2, 4);

            gbParams.Controls.Add(tlpParams);
        }

        private void BuildSummary()
        {
            lblSummary.Dock = DockStyle.Top;
            lblSummary.AutoSize = true;
            lblSummary.Padding = new Padding(8, 8, 8, 8);
            lblSummary.Font = new Font("Segoe UI", 10.5F);
        }

        private void BuildSelectPanel()
        {
            pnlSelect.Dock = DockStyle.Top;
            pnlSelect.AutoSize = true;
            pnlSelect.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlSelect.Padding = new Padding(4, 4, 4, 0);

            lblSelected.AutoSize = true;
            lblSelected.Anchor = AnchorStyles.Left;
            lblSelected.Margin = new Padding(12, 0, 0, 0);

            pnlSelect.Controls.Add(btnSelectAll);
            pnlSelect.Controls.Add(btnSelectNone);
            pnlSelect.Controls.Add(lblSelected);
        }

        // Id студентов, у которых в списке стоит флажок
        private IReadOnlyList<int> CheckedStudentIds() =>
            dgvRows.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Tag is int && Equals(r.Cells[colInclude.Index].Value, true))
                .Select(r => (int)r.Tag!)
                .ToList();

        private void SetAllChecked(bool value)
        {
            _updating = true;
            try
            {
                foreach (DataGridViewRow row in dgvRows.Rows)
                    row.Cells[colInclude.Index].Value = value;
            }
            finally
            {
                _updating = false;
            }

            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            var total = dgvRows.Rows.Count;
            lblSelected.Text = total == 0 ? "" : $"Выбрано студентов: {CheckedStudentIds().Count} из {total}";
        }

        private void BuildGrid()
        {
            UiStyle.StyleGrid(dgvRows);

            colInclude.HeaderText = "В ведомость";
            colInclude.FillWeight = 10;

            colNo.HeaderText = "№ п/п";
            colNo.FillWeight = 8;
            colNo.ReadOnly = true;

            colName.HeaderText = "Фамилия, имя, отчество обучающегося";
            colName.FillWeight = 45;
            colName.ReadOnly = true;

            colBook.HeaderText = "Номер зачётной книжки";
            colBook.FillWeight = 17;
            colBook.ReadOnly = true;

            colResult.HeaderText = "Оценка цифрой";
            colResult.FillWeight = 15;
            colResult.FlatStyle = FlatStyle.Flat;

            colResultText.HeaderText = "Оценка прописью";
            colResultText.FillWeight = 20;
            colResultText.ReadOnly = true;

            dgvRows.Columns.AddRange(colInclude, colNo, colName, colBook, colResult, colResultText);
        }

        private void WireEvents()
        {
            cbAssessment.SelectedIndexChanged += (_, _) => AssessmentFormChanged?.Invoke(this, EventArgs.Empty);
            cbGroup.SelectedIndexChanged += (_, _) => GroupChanged?.Invoke(this, EventArgs.Empty);
            cbSubject.SelectedIndexChanged += (_, _) => SubjectChanged?.Invoke(this, EventArgs.Empty);

            btnOpen.Click += (_, _) => OpenClicked?.Invoke(this, EventArgs.Empty);
            btnCreate.Click += (_, _) => CreateClicked?.Invoke(this, EventArgs.Empty);
            btnNew.Click += (_, _) => NewClicked?.Invoke(this, EventArgs.Empty);
            btnSave.Click += (_, _) => SaveClicked?.Invoke(this, EventArgs.Empty);
            btnCloseStatement.Click += (_, _) => CloseStatementClicked?.Invoke(this, EventArgs.Empty);
            btnExport.Click += (_, _) => ExportClicked?.Invoke(this, EventArgs.Empty);
            btnSelectAll.Click += (_, _) => SetAllChecked(true);
            btnSelectNone.Click += (_, _) => SetAllChecked(false);

            directories.DataChanged += (_, _) => DirectoriesChanged?.Invoke(this, EventArgs.Empty);

            // оценка из выпадающего списка применяется сразу, а не после ухода из ячейки
            dgvRows.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (dgvRows.IsCurrentCellDirty)
                    dgvRows.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvRows.CellValueChanged += DgvRows_CellValueChanged;
            dgvRows.DataError += (_, e) => e.ThrowException = false;
        }

        private void DgvRows_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_updating || e.RowIndex < 0) return;

            if (e.ColumnIndex == colInclude.Index)
            {
                UpdateSelectedCount();
                return;
            }

            if (e.ColumnIndex != colResult.Index) return;

            var row = dgvRows.Rows[e.RowIndex];
            var number = Convert.ToInt32(row.Cells[colNo.Index].Value);
            var input = Convert.ToString(row.Cells[colResult.Index].Value) ?? "";

            GradeEdited?.Invoke(this, new GradeEditedEventArgs(number, input));
        }

        // ================== Вспомогательные методы ==================

        private void AddField(string caption, Control field, int col, int row)
        {
            var label = new Label
            {
                Text = caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(6, 8, 6, 8)
            };
            field.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            field.Margin = new Padding(3, 4, 12, 4);

            tlpParams.Controls.Add(label, col, row);
            tlpParams.Controls.Add(field, col + 1, row);
        }

        // Обновляет список в ComboBox и сохраняет выбор, если выбранная запись осталась
        private static void FillCombo<T>(ComboBox box, IReadOnlyList<T> items, Func<T, int> idOf) where T : class
        {
            var previous = box.SelectedItem as T;

            // ToString() у Group, Subject и Teacher возвращает название: оно и показывается в списке
            box.DataSource = items.ToList();

            if (previous is null) return;

            var match = items.FirstOrDefault(x => idOf(x) == idOf(previous));
            if (match is not null) box.SelectedItem = match;
        }
    }
}
