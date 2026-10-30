namespace StatementApp.UI.Views
{
    internal enum FieldKind { Text, Number, Combo, Multiline }

    /// <summary>Описание одного поля в диалоге ввода.</summary>
    internal sealed class FieldSpec
    {
        public required string Label { get; init; }
        public string Value { get; init; } = "";
        public FieldKind Kind { get; init; } = FieldKind.Text;
        public bool Required { get; init; } = true;

        /// <summary>Подсказки для Combo: можно выбрать из списка или ввести своё.</summary>
        public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Универсальный диалог «заполните поля»: из него собираются все окна добавления и правки в справочниках.
    /// Пока validate возвращает текст ошибки, окно остаётся открытым.
    /// </summary>
    internal sealed class EntryDialog : Form
    {
        private readonly IReadOnlyList<FieldSpec> _fields;
        private readonly Func<IReadOnlyList<string>, string?>? _validate;
        private readonly List<Control> _inputs = new();

        public IReadOnlyList<string> Values { get; private set; } = Array.Empty<string>();

        public EntryDialog(
            string title,
            IReadOnlyList<FieldSpec> fields,
            Func<IReadOnlyList<string>, string?>? validate = null,
            string? hint = null)
        {
            _fields = fields;
            _validate = validate;

            Text = title;
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            Padding = new Padding(12);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var contentHeight = 0;
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var multiline = field.Kind == FieldKind.Multiline;
                var rowHeight = multiline ? 230 : 38;

                table.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
                contentHeight += rowHeight;

                var label = new Label
                {
                    Text = field.Label + ":",
                    AutoSize = true,
                    Anchor = multiline ? AnchorStyles.Top | AnchorStyles.Left : AnchorStyles.Left,
                    Margin = new Padding(0, multiline ? 8 : 0, 8, 0)
                };

                Control input = CreateInput(field);
                if (multiline)
                {
                    input.Dock = DockStyle.Fill;
                }
                else
                {
                    input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                }

                _inputs.Add(input);
                table.Controls.Add(label, 0, i);
                table.Controls.Add(input, 1, i);
            }
            table.RowCount = fields.Count;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0)
            };
            var cancel = UiStyle.MakeButton("Отмена");
            cancel.DialogResult = DialogResult.Cancel;
            var ok = UiStyle.MakeButton("OK");
            ok.Click += (_, _) => Accept();
            buttons.Controls.Add(cancel); // при RightToLeft первым добавленный окажется правее
            buttons.Controls.Add(ok);

            var hintHeight = 0;
            Label? hintLabel = null;
            if (!string.IsNullOrWhiteSpace(hint))
            {
                hintHeight = 56;
                hintLabel = new Label
                {
                    Text = hint,
                    Dock = DockStyle.Bottom,
                    Height = hintHeight,
                    ForeColor = SystemColors.GrayText
                };
            }

            // Fill добавляется первым: Dock обрабатывается от последнего добавленного к первому
            Controls.Add(table);
            if (hintLabel != null) Controls.Add(hintLabel);
            Controls.Add(buttons);

            ClientSize = new Size(580, contentHeight + hintHeight + 52 + 24);
            AcceptButton = ok;
            CancelButton = cancel;

            Shown += (_, _) => _inputs[0].Focus();
        }

        private static Control CreateInput(FieldSpec field)
        {
            switch (field.Kind)
            {
                case FieldKind.Combo:
                    var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown };
                    combo.Items.AddRange(field.Options.Cast<object>().ToArray());
                    combo.Text = field.Value;
                    return combo;

                case FieldKind.Multiline:
                    return new TextBox
                    {
                        Multiline = true,
                        AcceptsReturn = true,
                        ScrollBars = ScrollBars.Vertical,
                        Text = field.Value
                    };

                default:
                    return new TextBox { Text = field.Value };
            }
        }

        private void Accept()
        {
            var values = new List<string>();

            for (var i = 0; i < _fields.Count; i++)
            {
                var field = _fields[i];
                var text = _inputs[i].Text.Trim();

                if (field.Required && text.Length == 0)
                {
                    Warn($"Поле «{field.Label}» не заполнено.", _inputs[i]);
                    return;
                }

                if (field.Kind == FieldKind.Number && text.Length > 0 && !int.TryParse(text, out _))
                {
                    Warn($"В поле «{field.Label}» нужно ввести целое число.", _inputs[i]);
                    return;
                }

                values.Add(text);
            }

            var error = _validate?.Invoke(values);
            if (error != null)
            {
                Warn(error, null);
                return;
            }

            Values = values;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Warn(string message, Control? focus)
        {
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            focus?.Focus();
        }
    }
}
