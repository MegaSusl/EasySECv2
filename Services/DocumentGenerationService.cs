using EasySECv2.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xceed.Words.NET;
using Xceed.Document.NET;
using System.Diagnostics;
using EasySECv2.Services;
using System.Text.RegularExpressions;
using Group = EasySECv2.Models.Group;
using Border = Xceed.Document.NET.Border;
using VerticalAlignment = Xceed.Document.NET.VerticalAlignment;

namespace EasySECv2.Services
{
    public class DocumentGenerationService : IDocumentGenerationService
    {
        private readonly DatabaseService _db;
        private List<string> SplitByMaxLength(string input, int maxLength)
        {
            var result = new List<string>();

            while (input.Length > maxLength)
            {
                int breakIndex = input.LastIndexOf(' ', maxLength);

                if (breakIndex <= 0) breakIndex = maxLength;

                result.Add(input.Substring(0, breakIndex).Trim());
                input = input.Substring(breakIndex).Trim();
            }

            if (!string.IsNullOrWhiteSpace(input))
                result.Add(input);

            return result;
        }

        public async Task GenerateDocumentsAsync(DocumentTemplate template, IEnumerable<object> dataContexts, Dictionary<string, string> manualInputs, string outputDir)
        {
            Debug.WriteLine($"[Генерация] Шаблон: {template.Name} ({template.LocalPath})");
            Debug.WriteLine($"[Генерация] Кол-во объектов: {dataContexts.Count()}");
            Debug.WriteLine($"[Генерация] Выходная папка: {outputDir}");

            foreach (var data in dataContexts)
            {
                try
                {
                    var map = await BuildMap(template.Mappings, data, manualInputs, _db);
                    using var doc = DocX.Load(template.LocalPath);

                    // Спец-обработка для студента
                    if (data is Student student && doc.Text.Contains("[СТУДЕНТ:ФИО]"))
                    {
                        foreach (var p in doc.Paragraphs.ToList())
                        {
                            if (p.Text.Contains("[СТУДЕНТ:ФИО]"))
                            {
                                p.ReplaceText("[СТУДЕНТ:ФИО]", "");
                                p.Append(student.FullName).Font("Times New Roman").FontSize(14);
                            }
                        }
                    }

                    var replacements = new Dictionary<string, string>();
                    var tables = new Dictionary<string, Table>();

                    foreach (var mapping in template.Mappings)
                    {
                        Debug.WriteLine("[DEBUG] Документ до замены:\n" + mapping);
                        Debug.WriteLine("[DEBUG] Документ до замены:\n" + mapping.SourceType.ToString());
                        var key = mapping.Placeholder;
                        var value = map.GetValueOrDefault(key, "");

                        if (mapping.SourceType == MappingSourceType.ManualText)
                        {
                            var normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
                            var lines = normalized
                                .Split('\n', StringSplitOptions.None)
                                .SelectMany(line => SplitByMaxLength(line, 50))
                                .ToList();

                            int minLines = 0;
                            if (manualInputs.TryGetValue($"{key}__min", out var minVal))
                                int.TryParse(minVal, out minLines);
                            while (lines.Count < minLines)
                                lines.Add("");

                            if (lines.Count == 0)
                                continue; // или return;

                            var table = doc.AddTable(lines.Count, 1);
                            table.Alignment = Alignment.left;
                            var noBorder = new Xceed.Document.NET.Border(BorderStyle.Tcbs_none, 0, 0, Xceed.Drawing.Color.White);
                            var bottomBorder = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Xceed.Drawing.Color.Black);

                            for (int i = 0; i < lines.Count; i++)
                            {
                                var cell = table.Rows[i].Cells[0];
                                var cellPara = cell.Paragraphs.FirstOrDefault() ?? cell.InsertParagraph();
                                cellPara.Append(lines[i].Trim()).Font("Times New Roman").FontSize(14);
                                cell.SetBorder(TableCellBorderType.Top, noBorder);
                                cell.SetBorder(TableCellBorderType.Left, noBorder);
                                cell.SetBorder(TableCellBorderType.Right, noBorder);
                                cell.SetBorder(TableCellBorderType.InsideH, noBorder);
                                cell.SetBorder(TableCellBorderType.InsideV, noBorder);
                                cell.SetBorder(TableCellBorderType.Bottom, bottomBorder);
                            }

                            tables[key] = table;
                        }
                        else if (mapping.SourceType == MappingSourceType.Group && data is Group group)
                        {
                            var students = await _db.GetStudentsByGroupAsync(group.Id);
                            var table = doc.AddTable(students.Count + 1, 3);
                            table.Alignment = Alignment.center;
                            table.SetWidths(new float[] { 100f, 300f, 200f });

                            if (students == null || students.Count == 0)
                                continue; // пропускаем эту группу

                            var headers = new[] { "№ п/п", "ФИО", "Подпись, дата" };
                            for (int i = 0; i < 3; i++)
                            {
                                var cell = table.Rows[0].Cells[i];
                                var para = cell.Paragraphs.FirstOrDefault() ?? cell.InsertParagraph();
                                para.Append(headers[i]).Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
                                cell.VerticalAlignment = Xceed.Document.NET.VerticalAlignment.Center;
                                cell.MarginTop = 5;
                                cell.MarginBottom = 5;
                            }

                            for (int i = 0; i < students.Count; i++)
                            {
                                var row = table.Rows[i + 1];
                                row.MinHeight = 20;
                                row.Cells[0].Paragraphs[0].Append((i + 1).ToString()).Font("Times New Roman").FontSize(14);
                                row.Cells[1].Paragraphs[0].Append(students[i].FullName).Font("Times New Roman").FontSize(14);
                                row.Cells[2].Paragraphs[0].Append("").Font("Times New Roman").FontSize(14);

                                foreach (var cell in row.Cells)
                                {
                                    cell.VerticalAlignment = Xceed.Document.NET.VerticalAlignment.Center;
                                    cell.MarginTop = 5;
                                    cell.MarginBottom = 5;
                                }
                            }

                            tables[key] = table;
                        }
                        else if (mapping.SourceType == MappingSourceType.TableChairman)
                        {
                            Debug.WriteLine("[DEBUG] Генерация тПред1");
                            if (map.TryGetValue(mapping.Placeholder + "_CHAIRMAN_ID", out var chairmanIdStr)
                                && long.TryParse(chairmanIdStr, out var chairmanId))
                            {
                                var chairman = await _db.GetStaffByIdAsync(chairmanId);
                                if (chairman != null)
                                {
                                    var chairmanTable = BuildChairmanTable(doc, chairman);

                                    var placeholderParagraph = doc.Paragraphs
                                        .FirstOrDefault(p => p.Text.Contains($"[{mapping.Placeholder}]"));

                                    if (placeholderParagraph != null)
                                    {
                                        var anchor = placeholderParagraph.InsertParagraphAfterSelf("");
                                        anchor.InsertTableAfterSelf(chairmanTable);
                                        placeholderParagraph.ReplaceText($"[{mapping.Placeholder}]", "");
                                    }
                                }
                            }
                            Debug.WriteLine("[DEBUG] Генерация тПред2");
                        }
                        else if (mapping.SourceType == MappingSourceType.TableMembersAndSecretary)
                        {
                            Debug.WriteLine("[DEBUG] Генерация тСекр1");
                            var members = new List<Staff>();

                            for (int i = 1; i <= 4; i++)
                            {
                                var pkey = $"{mapping.Placeholder}_MEMBER{i}_ID";
                                if (map.TryGetValue(pkey, out var idStr) && long.TryParse(idStr, out var memberId))
                                {
                                    var staff = await _db.GetStaffByIdAsync(memberId);
                                    if (staff != null)
                                        members.Add(staff);
                                }
                            }

                            Staff? secretary = null;
                            if (map.TryGetValue(mapping.Placeholder + "_SECRETARY_ID", out var secIdStr)
                                && long.TryParse(secIdStr, out var secId))
                            {
                                secretary = await _db.GetStaffByIdAsync(secId);
                            }

                            var mainTable = BuildCommissionTable(doc, members);
                            var placeholderParagraph = doc.Paragraphs
                                .FirstOrDefault(p => p.Text.Contains($"[{mapping.Placeholder}]"));

                            if (placeholderParagraph != null)
                            {
                                var afterMain = placeholderParagraph.InsertParagraphAfterSelf("");
                                afterMain.InsertTableAfterSelf(mainTable);
                                placeholderParagraph.ReplaceText($"[{mapping.Placeholder}]", "");

                                if (secretary != null)
                                {
                                    var secTable = BuildCommissionTable(doc, new List<Staff>(), secretary);
                                    var afterSec = afterMain.InsertParagraphAfterSelf("");
                                    afterSec.InsertTableAfterSelf(secTable);
                                }
                            }
                            Debug.WriteLine("[DEBUG] Генерация тСекр2");
                        }





                        else
                        {
                            replacements[key] = value;
                        }
                    }

                    Debug.WriteLine("[DEBUG] Документ до замены:\n" + doc.Text);

                    // Лог всех замен
                    foreach (var pair in replacements)
                        Debug.WriteLine($"[ЗАМЕНА] [{pair.Key}] → {pair.Value}");

                    // Лог всех таблиц
                    foreach (var t in tables)
                        Debug.WriteLine($"[ТАБЛИЦА] [{t.Key}] содержит {t.Value.RowCount} строк, {t.Value.ColumnCount} колонок");

                    // Один проход по всем параграфам и таблицам
                    ReplaceAllSmart(doc, replacements, tables);


                    var fileName = GetFileName(data, template.Name);
                    var outputPath = Path.Combine(outputDir, fileName);

                    if (File.Exists(outputPath))
                    {
                        bool overwrite = await MainThread.InvokeOnMainThreadAsync(() =>
                            Application.Current.MainPage.DisplayAlert(
                                "Файл уже существует",
                                $"Файл {fileName} уже есть. Перезаписать?",
                                "Да", "Нет"
                            )
                        );

                        if (!overwrite)
                        {
                            Debug.WriteLine($"[Пропущено] Пользователь отменил перезапись файла {fileName}");
                            continue;
                        }
                    }

                    doc.SaveAs(outputPath);
                    Debug.WriteLine($"[OK] {fileName} создан успешно.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Ошибка] Не удалось создать документ: {ex.Message}");
                }
            }

            Debug.WriteLine("[Генерация] Завершено.");
        }
        // ──────────────────────────────────────────────
        // 1. Таблица председателя ГЭК
        // ──────────────────────────────────────────────
        private Table BuildChairmanTable(DocX doc, Staff chairman)
        {
            var t = doc.AddTable(4, 7);
            t.Alignment = Alignment.center;
            t.Design = TableDesign.TableGrid;

            // ─ строка-0
            t.Rows[0].MergeCells(0, 6);
            t.Rows[0].Cells[0].Paragraphs[0]
                .Append("Председатель ГЭК")
                .Font("Times New Roman").FontSize(14).Bold()
                .Alignment = Alignment.center;

            // ─ строка-1 — верхние заголовки (7 ячеек пока неизменённые)
            var r1 = t.Rows[1];
            r1.Cells[0].Paragraphs[0].Append("Образовательная программа")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r1.Cells[1].Paragraphs[0].Append("") /* будет объединено */ .Font("Times New Roman").FontSize(14).Bold();
            r1.Cells[2].Paragraphs[0].Append("Фамилия,\nимя, отчество")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r1.Cells[3].Paragraphs[0].Append("Основное место работы\n(субъект РФ, город), занимаемая должность")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r1.Cells[4].Paragraphs[0].Append("Учёная степень\n(серия, №, дата)")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r1.Cells[5].Paragraphs[0].Append("Учёное звание\n(серия, №, дата)")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r1.Cells[6].Paragraphs[0].Append("Почётное звание")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            // ─ строка-2 — подзаголовки
            var r2 = t.Rows[2];
            r2.Cells[0].Paragraphs[0].Append("Шифр")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r2.Cells[1].Paragraphs[0].Append("Наименование, профиль")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            // 1️⃣ СНАЧАЛА вертикально объединяем колонки 2-6 (строки 1-2)
            for (int col = 2; col <= 6; col++)
                t.MergeCellsInColumn(col, 1, 2);

            // 2️⃣ ПОТОМ объединяем по строке 0-1 ячейки «Образоват. программа»
            r1.MergeCells(0, 1);

            // ─ строка-3 — данные
            var vals = new[]
            {
        "", "",                         // шифр/профиль
        chairman.FullName    ?? "",
        chairman.Position     ?? "",
        chairman.Degree       ?? "",
        chairman.DegreeRank   ?? "",
        chairman.DegreeAwards ?? ""
    };
            for (int c = 0; c < 7; c++)
                t.Rows[3].Cells[c].Paragraphs[0]
                    .Append(vals[c])
                    .Font("Times New Roman").FontSize(14)
                    .Alignment = Alignment.center;

            return t;
        }


        // ──────────────────────────────────────────────
        // 2. Таблица членов комиссии / секретаря
        // ──────────────────────────────────────────────
        private Table BuildCommissionTable(DocX doc, List<Staff> members, Staff? secretary = null)
        {
            var t = doc.AddTable(7, 6);
            t.Alignment = Alignment.center;
            t.Design = TableDesign.TableGrid;

            // ─ строка-0
            t.Rows[0].MergeCells(0, 5);
            t.Rows[0].Cells[0].Paragraphs[0]
                .Append(secretary == null
                    ? "Члены ГЭК по защите выпускной квалификационной работы"
                    : "Секретарь ГЭК по защите выпускной квалификационной работы")
                .Font("Times New Roman").FontSize(14).Bold()
                .Alignment = Alignment.center;

            // ─ строка-1 — верхние заголовки
            var r1 = t.Rows[1];
            r1.Cells[0].Paragraphs[0].Append("№ п/п")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r1.Cells[1].Paragraphs[0].Append("Образовательная программа")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r1.Cells[2].Paragraphs[0].Append("")  /* объединят */
                .Font("Times New Roman").FontSize(14).Bold();
            r1.Cells[3].Paragraphs[0].Append("Фамилия,\nимя, отчество")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r1.Cells[4].Paragraphs[0].Append("Основное место работы,\nзанимаемая должность")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r1.Cells[5].Paragraphs[0].Append("Учёная степень,\nучёное звание")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            // ─ строка-2 — подзаголовки
            var r2 = t.Rows[2];
            r2.Cells[1].Paragraphs[0].Append("Шифр")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;
            r2.Cells[2].Paragraphs[0].Append("Наименование, профиль")
                .Font("Times New Roman").FontSize(14).Bold().Alignment = Alignment.center;

            // 1️⃣ СНАЧАЛА вертикальные merge-ы
            foreach (int col in new[] { 0, 3, 4, 5 })
                t.MergeCellsInColumn(col, 1, 2);

            // 2️⃣ ПОТОМ горизонтальное объединение 1-2 столбцов
            r1.MergeCells(1, 2);

            // ─ строки-3…6
            var people = secretary == null
                ? members.Take(4).ToList()
                : new List<Staff> { secretary };

            for (int i = 0; i < 4; i++)
            {
                var row = t.Rows[i + 3];
                var s = i < people.Count ? people[i] : null;

                row.Cells[0].Paragraphs[0].Append(s != null ? (i + 1).ToString() : "")
                    .Font("Times New Roman").FontSize(14);
                row.Cells[1].Paragraphs[0].Append("")                          // Шифр
                    .Font("Times New Roman").FontSize(14);
                row.Cells[2].Paragraphs[0].Append("")                          // Наим./профиль
                    .Font("Times New Roman").FontSize(14);
                row.Cells[3].Paragraphs[0].Append(s?.FullName ?? "")
                    .Font("Times New Roman").FontSize(14);
                row.Cells[4].Paragraphs[0].Append(s?.Position ?? "")
                    .Font("Times New Roman").FontSize(14);
                row.Cells[5].Paragraphs[0]
                    .Append(string.Join(", ",
                        new[] { s?.Degree, s?.DegreeRank }
                            .Where(v => !string.IsNullOrWhiteSpace(v))))
                    .Font("Times New Roman").FontSize(14);
            }

            return t;
        }



        private void ReplaceAllPlaceholdersWithRegex(DocX document, Dictionary<string, string> map)
        {
            var options = new FunctionReplaceTextOptions
            {
                FindPattern = @"\[(.+?)\]",
                RegExOptions = RegexOptions.None,
                RegexMatchHandler = (matchStr) =>
                {
                    var marker = matchStr.Trim('[', ']');
                    return map.TryGetValue(marker, out var value)
                        ? value
                        : matchStr;
                }
            };

            document.ReplaceText(options);
        }

        private void ReplaceAllSmart(DocX doc, Dictionary<string, string> replacements, Dictionary<string, Table> tables)
        {
            var paragraphs = doc.Paragraphs.ToList();

            foreach (var para in paragraphs)
            {
                var matches = Regex.Matches(para.Text, @"\[(.+?)\]");
                foreach (Match match in matches)
                {
                    var key = match.Groups[1].Value;

                    if (tables.TryGetValue(key, out var table))
                    {
                        var newPara = para.InsertParagraphAfterSelf("");
                        newPara.InsertTableAfterSelf(table);
                        doc.RemoveParagraph(para);
                        break; // обработали параграф — больше не нужен
                    }
                    else if (replacements.TryGetValue(key, out var value))
                    {
                        // Удаляем старый текст и создаем новый с нужным стилем
                        para.ReplaceText($"[{key}]", string.Empty);
                        para.Append(value).Font("Times New Roman").FontSize(14);
                    }

                }
            }
        }


        private string GetFileName(object data, string templateName)
        {
            if (data is Student s)
                return $"{s.surname}_{templateName}.docx";

            if (data.GetType().GetProperty("FullName")?.GetValue(data) is string fullName)
                return $"{fullName}_{templateName}.docx";

            return $"Документ_{templateName}_{Guid.NewGuid()}.docx";
        }

        public async Task GenerateTabularAsync(DocumentTemplate template, Group group, List<Student> students, Dictionary<string, string> manualInputs, string outputDir)
        {
            var doc = DocX.Load(template.LocalPath);
            var table = doc.Tables.FirstOrDefault();
            if (table == null) return;

            for (int i = 0; i < students.Count; i++)
            {
                var row = table.InsertRow();
                row.ReplaceText("[СТУДЕНТ:ФИО]", students[i].FullName);
                row.ReplaceText("[INDEX]", (i + 1).ToString());
            }

            foreach (var kvp in manualInputs)
                doc.ReplaceText($"[{kvp.Key}]", kvp.Value);

            doc.SaveAs(Path.Combine(outputDir, $"{group.Name}_{template.Name}.docx"));
        }

        private async Task<Dictionary<string, string>> BuildMap(List<PlaceholderMapping> mappings, object dataContext, Dictionary<string, string> manual, DatabaseService db)
        {
            var map = new Dictionary<string, string>();

            foreach (var m in mappings)
            {
                string raw = manual.GetValueOrDefault(m.Placeholder, string.Empty);

                string value = m.SourceType switch
                {
                    MappingSourceType.ManualText => NormalizeNewlines(raw),
                    MappingSourceType.ManualDate or
                    MappingSourceType.ManualTimeFull or
                    MappingSourceType.Manual => raw,

                    MappingSourceType.Institute or
                    MappingSourceType.Department or
                    MappingSourceType.FormOfEducation or
                    MappingSourceType.Orientation => raw,

                    MappingSourceType.Student or MappingSourceType.Table =>
                        dataContext?.GetType().GetProperty(m.Property)?.GetValue(dataContext)?.ToString() ?? string.Empty,
                    MappingSourceType.Group =>
                        dataContext is Group g ? await BuildGroupValue(g, db) : string.Empty,
                    MappingSourceType.Calculated => GetCalculatedValue(m.Placeholder),
                    _ => string.Empty,
                };


                //Debug.WriteLine("BUILDMAP: " + value);
                map[m.Placeholder] = value;

                if (m.SourceType is MappingSourceType.TableChairman
                  or MappingSourceType.TableMembersAndSecretary)
                {
                    foreach (var kv in manual.Where(k => k.Key.StartsWith(m.Placeholder + "_")))
                        map[kv.Key] = kv.Value;
                }
            }

            return map;
        }
        private static async Task<string> BuildGroupValue(Group g, DatabaseService db)
        {
            Debug.WriteLine($"[GroupMapping] Группа: {g.Name}, ID: {g.Id}");

            var students = await db.GetStudentsByGroupAsync(g.Id);
            Debug.WriteLine($"[GroupMapping] Найдено студентов: {students.Count}");

            var result = $"{g.Name}; " + string.Join("; ", students.Select(s => s.FullName));
            Debug.WriteLine($"[GroupMapping] Сформировано значение: {result}");

            return result;
        }

        public async Task GenerateFamiliarizationAsync(
                        DocumentTemplate template,
                        Group group,
                        IEnumerable<Student> students,
                        DateTime orderDate,
                        string orderNumber,
                        string outputDir)
        {
            // 0. Подстраховка аргументов
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (group == null) throw new ArgumentNullException(nameof(group));
            if (string.IsNullOrWhiteSpace(outputDir))
                throw new ArgumentException("Не указан путь сохранения", nameof(outputDir));

            // 1. Убеждаемся, что выходная папка существует
            Directory.CreateDirectory(outputDir);

            // 2. Имя результирующего файла
            var outPath = Path.Combine(outputDir,
                $"Лист_ознакомления_{group.Name}_{DateTime.Now:yyyyMMddHHmmss}.docx");

            // 3. Пока делаем простое копирование шаблона
            //using (var src = File.OpenRead(template.FilePath))       // свойство, которое реально есть в модели
            //using (var dst = File.Create(outPath))
            //{
            //    await src.CopyToAsync(dst);
            //}

            // 4. Место для будущей логики (таблица, ReplaceText и т.п.)
            // TODO: реализовать заполнение DocX, когда убедимся, что система работает
        }

        private string TryFormatDate(string input, string? format)
        {
            if (DateTime.TryParse(input, out var date))
                return date.ToString(format ?? "dd.MM.yyyy", new CultureInfo("ru-RU"));
            return input;
        }

        private string TryFormatTime(string input, string? format)
        {
            if (TimeSpan.TryParse(input, out var time))
                return DateTime.Today.Add(time).ToString(format ?? "HH:mm");
            return input;
        }

        private string NormalizeNewlines(string input)
        {
            return input.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }

        private string GetCalculatedValue(string placeholder)
        {
            return placeholder switch
            {
                "МЕСЯЦ" => DateTime.Now.ToString("MMMM", new CultureInfo("ru-RU")),
                _ => string.Empty
            };
        }
        public DocumentGenerationService(DatabaseService db)
        {
            _db = db;
        }

    }
}