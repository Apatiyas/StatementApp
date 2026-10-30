using StatementApp.UI.ViewModels;

namespace StatementApp.UI.Views
{
    /// <summary>Список сохранённых ведомостей: открыть или удалить.</summary>
    internal sealed class StatementListDialog : Form
    {
        private readonly DataGridView _grid = new();

        public StatementPick? Result { get; private set; }

        public StatementListDialog(IReadOnlyList<StatementSummary> statements)
        {
            Text = "Сохранённые ведомости";
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(780, 460);

            UiStyle.StyleGrid(_grid);
            _grid.ReadOnly = true;
            _grid.Columns.Add("date", "Дата");
            _grid.Columns.Add("group", "Группа");
            _grid.Columns.Add("discipline", "Дисциплина");
            _grid.Columns.Add("status", "Статус");
            _grid.Columns[0].FillWeight = 18;
            _grid.Columns[1].FillWeight = 24;
            _grid.Columns[2].FillWeight = 40;
            _grid.Columns[3].FillWeight = 18;

            foreach (var s in statements)
            {
                var index = _grid.Rows.Add(s.Date.ToString("dd.MM.yyyy"), s.GroupName, s.Discipline, s.Status);
                _grid.Rows[index].Tag = s.Id;
            }

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            var cancel = UiStyle.MakeButton("Закрыть");
            cancel.DialogResult = DialogResult.Cancel;
            var delete = UiStyle.MakeButton("Удалить");
            delete.Click += (_, _) => Choose(PickAction.Delete);
            var open = UiStyle.MakeButton("Открыть");
            open.Click += (_, _) => Choose(PickAction.Open);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(delete);
            buttons.Controls.Add(open);

            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0) Choose(PickAction.Open);
            };

            Controls.Add(_grid);
            Controls.Add(buttons);

            AcceptButton = open;
            CancelButton = cancel;
        }

        private void Choose(PickAction action)
        {
            if (_grid.CurrentRow?.Tag is not int id)
            {
                MessageBox.Show(this, "Выберите ведомость в списке.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Result = new StatementPick(id, action);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
