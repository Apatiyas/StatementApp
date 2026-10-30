using System.IO.Compression;
using System.Text;
using StatementApp.Core.Abstractions;
using StatementApp.Core.Entities;
using StatementApp.Core.Enums;

namespace StatementApp.Data.DocumentGeneration
{
    /// <summary>
    /// Собирает .docx по бланку «Зачетно-экзаменационная ведомость».
    /// Файл формируется напрямую (docx - это zip с XML), поэтому внешние библиотеки не нужны.
    /// </summary>
    public class DocTemplateGenerator : IStatementExporter
    {
        // Название организации в шапке бланка
        private const string OrganizationLine1 = "Автономная некоммерческая профессиональная образовательная организация";
        private const string OrganizationLine2 = "Московский Международный Колледж Цифровых Технологий «Академия ТОП»";

        private static readonly string[] MonthsGenitive =
        {
            "января", "февраля", "марта", "апреля", "мая", "июня",
            "июля", "августа", "сентября", "октября", "ноября", "декабря"
        };

        // Ширины столбцов таблицы в twips (1 см = 567): №, ФИО, зачётка, цифрой, прописью, подпись.
        // Сумма 9638 = ширина A4 (11906) минус поля по 1134 слева и справа.
        private static readonly int[] Widths = { 560, 2900, 1350, 1000, 2230, 1598 };

        // Размер шрифта в таблице, в половинах пункта (22 = 11 pt)
        private const int TableFontSize = 22;

        // ---------- IStatementExporter ----------

        public string SuggestFileName(Statement statement)
        {
            ArgumentNullException.ThrowIfNull(statement);

            var name = $"Ведомость {statement.GroupName} {statement.Discipline} {statement.Date:dd.MM.yyyy}";
            foreach (var bad in Path.GetInvalidFileNameChars())
                name = name.Replace(bad, '_');

            return name + ".docx";
        }

        public void Export(Statement statement, string filePath)
        {
            ArgumentNullException.ThrowIfNull(statement);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Create);

            AddEntry(zip, "[Content_Types].xml", ContentTypesXml);
            AddEntry(zip, "_rels/.rels", RootRelsXml);
            AddEntry(zip, "word/document.xml", BuildDocumentXml(statement));
            AddEntry(zip, "word/styles.xml", StylesXml);
            AddEntry(zip, "word/_rels/document.xml.rels", DocumentRelsXml);
        }

        private static void AddEntry(ZipArchive zip, string name, string content)
        {
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content);
        }

        // ---------- содержимое документа ----------

        private static string BuildDocumentXml(Statement s)
        {
            var body = new StringBuilder();

            body.Append(Paragraph(Run(OrganizationLine1), "center"));
            body.Append(Paragraph(Run(OrganizationLine2), "center", after: 240));
            body.Append(Paragraph(Run(s.FormOfEducation, bold: true, underline: true) + Run(" форма обучения"), "center", after: 240));
            body.Append(Paragraph(Run("ЗАЧЕТНО-ЭКЗАМЕНАЦИОННАЯ ВЕДОМОСТЬ", bold: true), "center", after: 240));

            body.Append(Line(Run("Учебный год: ") + Run(s.AcademicYear, bold: true, underline: true)
                + Run(" / семестр: ") + Run(s.Semester, bold: true, underline: true)));
            body.Append(Line(Run("Форма промежуточной аттестации: ") + Run(s.AssessmentForm.ToDisplayText(), bold: true, underline: true)));
            body.Append(Line(Run("Группа: ") + Run(s.GroupName, bold: true)));
            body.Append(Line(Run("Профессия/специальность: ") + Run(s.Specialty, bold: true)));
            body.Append(Line(Run("Учебный предмет, курс, дисциплина образовательной программы: ") + Run(s.Discipline, bold: true)));
            body.Append(Line(Run("Общая трудоемкость по учебному плану (час): ") + Run(s.TotalHours.ToString(), bold: true)));
            body.Append(Line(Run("Фамилия, Имя, Отчество преподавателя: ") + Run(s.TeacherName, bold: true)));
            body.Append(Paragraph(Run("Дата проведения: ") + Run(FormatDate(s.Date), bold: true), "left", after: 200));

            body.Append(BuildTable(s));

            // после таблицы Word требует хотя бы один абзац
            body.Append("<w:p/>");

            body.Append("<w:sectPr>"
                + "<w:pgSz w:w=\"11906\" w:h=\"16838\"/>"
                + "<w:pgMar w:top=\"1134\" w:right=\"1134\" w:bottom=\"1134\" w:left=\"1134\" w:header=\"708\" w:footer=\"708\" w:gutter=\"0\"/>"
                + "</w:sectPr>");

            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                + "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" "
                + "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">"
                + "<w:body>" + body + "</w:body></w:document>";
        }

        private static string BuildTable(Statement s)
        {
            var sb = new StringBuilder();

            sb.Append("<w:tbl>");
            sb.Append("<w:tblPr>"
                + $"<w:tblW w:w=\"{Widths.Sum()}\" w:type=\"dxa\"/>"
                + "<w:jc w:val=\"center\"/>"
                + "<w:tblBorders>"
                + "<w:top w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"000000\"/>"
                + "<w:left w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"000000\"/>"
                + "<w:bottom w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"000000\"/>"
                + "<w:right w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"000000\"/>"
                + "<w:insideH w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"000000\"/>"
                + "<w:insideV w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"000000\"/>"
                + "</w:tblBorders>"
                + "<w:tblLayout w:type=\"fixed\"/>"
                + "<w:tblCellMar>"
                + "<w:top w:w=\"40\" w:type=\"dxa\"/><w:left w:w=\"80\" w:type=\"dxa\"/>"
                + "<w:bottom w:w=\"40\" w:type=\"dxa\"/><w:right w:w=\"80\" w:type=\"dxa\"/>"
                + "</w:tblCellMar>"
                + "</w:tblPr>");

            sb.Append("<w:tblGrid>");
            foreach (var width in Widths)
                sb.Append($"<w:gridCol w:w=\"{width}\"/>");
            sb.Append("</w:tblGrid>");

            // Шапка таблицы из двух строк: «Оценка» объединяет два столбца,
            // остальные ячейки растянуты по вертикали на обе строки.
            sb.Append(Row(
                Cell(Widths[0], "№ п/п", "center", vMerge: "restart")
                + Cell(Widths[1], "Фамилия, имя, отчество обучающегося", "center", vMerge: "restart")
                + Cell(Widths[2], "Номер зачетной книжки", "center", vMerge: "restart")
                + Cell(Widths[3] + Widths[4], "Оценка", "center", span: 2)
                + Cell(Widths[5], "Подпись преподавателя", "center", vMerge: "restart"),
                header: true));

            sb.Append(Row(
                Cell(Widths[0], "", "center", vMerge: "continue")
                + Cell(Widths[1], "", "center", vMerge: "continue")
                + Cell(Widths[2], "", "center", vMerge: "continue")
                + Cell(Widths[3], "цифрой", "center")
                + Cell(Widths[4], "прописью", "center")
                + Cell(Widths[5], "", "center", vMerge: "continue"),
                header: true));

            foreach (var row in s.Rows)
            {
                sb.Append(Row(
                    Cell(Widths[0], row.SequenceNumber + ".", "center")
                    + Cell(Widths[1], row.StudentName, "left")
                    + Cell(Widths[2], row.GradeBookNumber, "center")
                    + Cell(Widths[3], DigitFor(row, s.AssessmentForm), "center")
                    + Cell(Widths[4], row.Grade.ToDisplayText(s.AssessmentForm), "left")
                    + Cell(Widths[5], "", "center")));
            }

            sb.Append("</w:tbl>");
            return sb.ToString();
        }

        // Цифра только для оценок 2-5. Для зачёта («Зачтено» / «Не зачтено») и для
        // неявки или недопуска колонка остаётся пустой, результат виден в колонке «прописью».
        private static string DigitFor(StatementRow row, AssessmentForm form)
        {
            if (row.Grade.Status != GradeStatus.Graded || form == AssessmentForm.Credit)
                return string.Empty;

            return row.Grade.Score?.ToString() ?? string.Empty;
        }

        private static string FormatDate(DateOnly date) =>
            $"«{date.Day:00}» {MonthsGenitive[date.Month - 1]} {date.Year} г.";

        // ---------- XML-кирпичики ----------

        private static string Run(string text, bool bold = false, bool underline = false, int size = 0)
        {
            var props = (bold ? "<w:b/>" : "")
                + (size > 0 ? $"<w:sz w:val=\"{size}\"/><w:szCs w:val=\"{size}\"/>" : "")
                + (underline ? "<w:u w:val=\"single\"/>" : "");

            var rPr = props.Length > 0 ? $"<w:rPr>{props}</w:rPr>" : "";
            return $"<w:r>{rPr}<w:t xml:space=\"preserve\">{Esc(text)}</w:t></w:r>";
        }

        private static string Paragraph(string runs, string align = "left", int before = 0, int after = 0) =>
            $"<w:p><w:pPr><w:spacing w:before=\"{before}\" w:after=\"{after}\"/><w:jc w:val=\"{align}\"/></w:pPr>{runs}</w:p>";

        private static string Line(string runs) => Paragraph(runs, "left", after: 60);

        private static string Cell(int width, string text, string align, int span = 1, string? vMerge = null)
        {
            var tcPr = $"<w:tcW w:w=\"{width}\" w:type=\"dxa\"/>";
            if (span > 1) tcPr += $"<w:gridSpan w:val=\"{span}\"/>";

            if (vMerge == "restart") tcPr += "<w:vMerge w:val=\"restart\"/>";
            else if (vMerge == "continue") tcPr += "<w:vMerge/>";

            tcPr += "<w:vAlign w:val=\"center\"/>";

            var runs = text.Length > 0 ? Run(text, size: TableFontSize) : "";
            return $"<w:tc><w:tcPr>{tcPr}</w:tcPr>{Paragraph(runs, align)}</w:tc>";
        }

        private static string Row(string cells, bool header = false)
        {
            var trPr = "<w:cantSplit/><w:trHeight w:val=\"480\"/>" + (header ? "<w:tblHeader/>" : "");
            return $"<w:tr><w:trPr>{trPr}</w:trPr>{cells}</w:tr>";
        }

        // Экранирование текста и выбрасывание символов, которые запрещены в XML 1.0
        private static string Esc(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                switch (ch)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    default:
                        if (ch >= ' ' || ch == '\t') sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        // ---------- служебные части пакета .docx ----------

        private const string ContentTypesXml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
            <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
            <Default Extension="xml" ContentType="application/xml"/>
            <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
            <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
            </Types>
            """;

        private const string RootRelsXml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
            <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
            </Relationships>
            """;

        private const string DocumentRelsXml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
            <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
            </Relationships>
            """;

        // Шрифт по умолчанию Times New Roman 12 pt, как на бланке
        private const string StylesXml = """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
            <w:docDefaults>
            <w:rPrDefault><w:rPr>
            <w:rFonts w:ascii="Times New Roman" w:hAnsi="Times New Roman" w:eastAsia="Times New Roman" w:cs="Times New Roman"/>
            <w:sz w:val="24"/><w:szCs w:val="24"/>
            <w:lang w:val="ru-RU" w:eastAsia="ru-RU" w:bidi="ar-SA"/>
            </w:rPr></w:rPrDefault>
            <w:pPrDefault><w:pPr><w:spacing w:after="0" w:line="240" w:lineRule="auto"/></w:pPr></w:pPrDefault>
            </w:docDefaults>
            <w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:qFormat/></w:style>
            </w:styles>
            """;
    }
}
