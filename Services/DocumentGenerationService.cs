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
using Microsoft.Maui.Storage;
using static EasySECv2.Services.DatabaseService;

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

                    foreach (var m in map) { Debug.WriteLine("[DEBUG] ЗНАЧЕНИЯ " + m.Key + m.Value); }                    
                    using var doc = DocX.Load(template.LocalPath);

                    //// Спец-обработка для студента
                    //if (data is Student student && doc.Text.Contains("[СТУДЕНТ:ФИО]"))
                    //{
                    //    foreach (var p in doc.Paragraphs.ToList())
                    //    {
                    //        if (p.Text.Contains("[СТУДЕНТ:ФИО]"))
                    //        {
                    //            p.ReplaceText("[СТУДЕНТ:ФИО]", "");
                    //            p.Append(student.FullName).Font("Times New Roman").FontSize(14);
                    //        }
                    //    }
                    //}

                    var replacements = new Dictionary<string, string>();
                    var tables = new Dictionary<string, Table>();
                    foreach (var mapping in template.Mappings) { Debug.WriteLine("[DEBUG] ЗНАЧЕНИЯ " + map.GetValueOrDefault(mapping.Placeholder, "") + mapping.Placeholder); }
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

                            if (map.TryGetValue(key + "_CHAIRMAN_ID", out var chairmanIdStr)
                                && long.TryParse(chairmanIdStr, out var chairmanId))
                            {
                                var chairman = await _db.GetStaffByIdAsync(chairmanId);
                                if (chairman != null)
                                {
                                    // Вместо немедленной вставки — просто кладём в словарь
                                    var chairmanTable = BuildChairmanTable(doc, chairman);
                                    tables[key] = chairmanTable;
                                }
                            }

                            Debug.WriteLine("[DEBUG] Генерация тПред2");
                        }
                        else if (mapping.SourceType == MappingSourceType.TableMembersAndSecretary)
                        {
                            Debug.WriteLine("[DEBUG] Генерация тСекр1");

                            // Собираем список членов
                            var members = new List<Staff>();
                            for (int i = 1; i <= 4; i++)
                            {
                                var pkey = $"{key}_MEMBER{i}_ID";
                                if (map.TryGetValue(pkey, out var idStr) &&
                                    long.TryParse(idStr, out var memberId))
                                {
                                    Debug.WriteLine("memberId " + memberId);
                                    var s = await _db.GetStaffByIdAsync(memberId);
                                    if (s != null) members.Add(s);
                                }
                            }

                            // Находим секретаря (если есть)
                            Staff? secretary = null;
                            if (map.TryGetValue(key + "_SECRETARY_ID", out var secIdStr) &&
                                long.TryParse(secIdStr, out var secId))
                            {
                                Debug.WriteLine("secId " + secId);
                                secretary = await _db.GetStaffByIdAsync(secId);
                            }

                            // Строим одно объединённое «Commission»-та­блицу,
                            // которая умеет принимать либо список членов, либо одного секретаря в качестве member-list
                            // (то есть если secretary != null, передадим новый список с одним элементом)
                            var mainRows = members;
                            if (members.Count == 0 && secretary != null)
                            {
                                mainRows = new List<Staff> { secretary };
                            }

                            var commissionTable = BuildCommissionTable(doc, mainRows, secretary);

                            tables[key] = commissionTable;
                            Debug.WriteLine("[DEBUG] Генерация тСекр2");
                        }
                        else if (mapping.SourceType == MappingSourceType.TableVkrTopic)
                        {
                            // определяем группу
                            Group? Group = null;
                            if (data is Group gCtx) Group = gCtx;
                            else if (map.TryGetValue("ГРУППА_ID", out var gid) &&
                                     long.TryParse(gid, out var gId)) Group = await _db.GetGroupByIdAsync(gId);
                            else if (map.TryGetValue("ГРУППА", out var gName))
                                Group = await _db.GetGroupByNameAsync(gName);

                            if (Group != null)
                            {
                                var rows = await _db.GetVkrTopicsByGroupAsync(Group.Id);
                                var vkrTbl = BuildVkrTopicTable(doc, rows);
                                tables[key] = vkrTbl;   // key == "ТАБЛИЦА ТЕМЫ"
                            }
                        }
                        if (mapping.SourceType == MappingSourceType.TableReport)
                        {
                            // dataContexts содержит наш объект-сводку GekResult
                            var summary = dataContexts.OfType<GekResult>().FirstOrDefault();
                            if (summary != null)
                                tables[key] = BuildGekResultTable(doc, summary);
                            continue;               // к следующему mapping
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

        #region ───────── DTO с агрегированными данными ─────────
        public record GekResult(
            // 1.1  ─ допущены
            int ExamAdmittedAll, int ExamAdmittedFull, int ExamAdmittedExtra, int ExamAdmittedPart,
            // 1.1 оценки
            int ExamA, int ExamB, int ExamC, int ExamD,
            // 1.2  ─ не явились
            int ExamAbsentAll, int ExamAbsentFull, int ExamAbsentExtra, int ExamAbsentPart,

            // 2.1  ─ принято к защите
            int FqwAcceptedAll, int FqwAcceptedFull, int FqwAcceptedExtra, int FqwAcceptedPart,
            // 2.2  ─ защищено
            int FqwDefendedAll, int FqwDefendedFull, int FqwDefendedExtra, int FqwDefendedPart,
            // 2.3  оценки защиты
            int FqwA, int FqwB, int FqwC, int FqwD,
            // 2.4  ─ не явились
            int FqwAbsentAll, int FqwAbsentFull, int FqwAbsentExtra, int FqwAbsentPart,
            // 2.5  ─ переносы
            int FqwPostponedAll, int FqwPostponedFull, int FqwPostponedExtra, int FqwPostponedPart,

            // 2.6  ─ типы ВКР
            int FqwResearch, int FqwPractice, int FqwProject, int FqwStartup, int FqwSocial,
            // 2.7  ─ рекомендации
            int FqwToPublish, int FqwToImplement, int FqwImplemented,
            // 2.8  ─ дипломы с отличием
            int HonourDiplomas,
            // 2.9  ─ средняя оригинальность
            double? AvgOriginality
        );
        #endregion

        /*───────────────────────────────────────────────────────*\
         |      Таблица-«шахматка» + подробные DEBUG-логи        |
        \*───────────────────────────────────────────────────────*/
        private Table BuildGekResultTable(DocX doc, GekResult s)
        {
            const int COLS = 10;

            /* ---------- 0. каркас ---------- */
            var t = doc.AddTable(4 + 27, COLS);               // 3-стр. шапка + номератор + 27 строк
            t.Design = TableDesign.TableGrid;
            t.Alignment = Alignment.center;
            t.AutoFit = AutoFit.Window;

            var thin = new Border(BorderStyle.Tcbs_single, BorderSize.one, 0,
                                  Xceed.Drawing.Color.Black);
            foreach (TableBorderType b in Enum.GetValues<TableBorderType>())
                t.SetBorder(b, thin);

            /* ---------- 1. шапка ---------- */
            // --- Row 0 ---
            Debug.WriteLine("Row0 cells: " + t.Rows[0].Cells.Count);
            t.Rows[0].Cells[0].Paragraphs[0].Append("№\nп/п").Alignment = Alignment.center;
            t.Rows[0].Cells[1].Paragraphs[0].Append("Показатели").Alignment = Alignment.center;
            t.Rows[0].Cells[2].Paragraphs[0].Append("Всего").Alignment = Alignment.center;
            t.Rows[0].Cells[4].Paragraphs[0].Append("Форма обучения").Alignment = Alignment.center;

            t.Rows[0].MergeCells(4, 9);
            t.Rows[0].MergeCells(2, 3);

            // --- Row 1 ---
            Debug.WriteLine("Row1 cells: " + t.Rows[1].Cells.Count);
            //t.Rows[1].Cells[2].Paragraphs[0].Append("кол").Alignment = Alignment.center;
            //t.Rows[1].Cells[3].Paragraphs[0].Append("%").Alignment = Alignment.center;
            t.Rows[1].Cells[4].Paragraphs[0].Append("очная").Alignment = Alignment.center;
            t.Rows[1].Cells[6].Paragraphs[0].Append("очно-\nзаочная").Alignment = Alignment.center;
            t.Rows[1].Cells[8].Paragraphs[0].Append("заочная").Alignment = Alignment.center;

            /* горизонтальные слияния второй строки – строго справа-налево */
            t.Rows[1].MergeCells(8, 9);   // сначала 8–9
            t.Rows[1].MergeCells(6, 7);   // потом 6–7
            t.Rows[1].MergeCells(4, 5);   // и только потом 4–5
            t.Rows[1].MergeCells(2, 3);

            // --- Row 2 ---
            Debug.WriteLine("Row2 cells: " + t.Rows[2].Cells.Count);
            string[] sub = { "кол", "%", "кол", "%", "кол", "%", "кол", "%" };
            for (int i = 0; i < sub.Length; i++)
                t.Rows[2].Cells[2 + i].Paragraphs[0].Append(sub[i]).Alignment = Alignment.center;

            // вертикаль для «№» и «Показатели»
            t.MergeCellsInColumn(0, 0, 2);
            t.MergeCellsInColumn(1, 0, 2);
            t.MergeCellsInColumn(2, 0, 1);
            /* ---------- 2. нумератор 1‥10 ---------- */
            int rowNum = 3;
            Debug.WriteLine($"Row{rowNum} (нумератор) cells: {t.Rows[rowNum].Cells.Count}");
            for (int c = 0; c < COLS; c++)
                t.Rows[rowNum].Cells[c].Paragraphs[0]
                  .Append((c + 1).ToString()).Alignment = Alignment.center;
            rowNum++;   // → 1-я строка данных (индекс 4)

            /* ---------- 3. хелперы ---------- */
            void Caption(string text)
            {
                Debug.WriteLine($"Caption @row {rowNum}: {text}");
                Debug.WriteLine($"  cells: {t.Rows[rowNum].Cells.Count}");
                t.Rows[rowNum].MergeCells(0, 9);
                t.Rows[rowNum].Cells[0].Paragraphs[0].Append(text).Bold();
                rowNum++;
            }
            void Row(string? num, string caption,
                     string all, string full = "", string mixed = "", string part = "")
            {
                Debug.WriteLine($"Row  @row {rowNum}: {caption}");
                Debug.WriteLine($"  cells: {t.Rows[rowNum].Cells.Count}");
                if (t.Rows[rowNum].Cells.Count < 10)
                    throw new Exception($"Row {rowNum} has only {t.Rows[rowNum].Cells.Count} cells");

                t.Rows[rowNum].Cells[0].Paragraphs[0]
                  .Append(num ?? "").Alignment = Alignment.center;
                t.Rows[rowNum].Cells[1].Paragraphs[0].Append(caption);

                string[] vals = { all, "", full, "", mixed, "", part, "" };
                for (int i = 0; i < vals.Length; i++)
                    t.Rows[rowNum].Cells[2 + i].Paragraphs[0]
                      .Append(vals[i]).Alignment = Alignment.center;

                rowNum++;
            }

            /* ---------- 4. данные (как прежде) ---------- */
            Caption("1.   Государственный экзамен (при наличии)");
            Row("1.1", "Количество студентов, допущенных к экзамену",
                s.ExamAdmittedAll.ToString(), s.ExamAdmittedFull.ToString(),
                s.ExamAdmittedExtra.ToString(), s.ExamAdmittedPart.ToString());
            Row(null, "в том числе получивших оценки: отлично", s.ExamA.ToString());
            Row(null, "хорошо", s.ExamB.ToString());
            Row(null, "удовлетворительно", s.ExamC.ToString());
            Row(null, "неудовлетворительно", s.ExamD.ToString());
            Row("1.2",
                "Количество студентов, не явившихся на экзамен по уважительной / неуважительной причине",
                s.ExamAbsentAll.ToString(), s.ExamAbsentFull.ToString(),
                s.ExamAbsentExtra.ToString(), s.ExamAbsentPart.ToString());

            Caption("2.   Выпускная квалификационная работа (ВКР)");
            Row("2.1", "Принято к защите ВКР",
                s.FqwAcceptedAll.ToString(), s.FqwAcceptedFull.ToString(),
                s.FqwAcceptedExtra.ToString(), s.FqwAcceptedPart.ToString());
            Row("2.2", "Защищено ВКР",
                s.FqwDefendedAll.ToString(), s.FqwDefendedFull.ToString(),
                s.FqwDefendedExtra.ToString(), s.FqwDefendedPart.ToString());
            Row(null, "Результаты защиты ВКР: отлично", s.FqwA.ToString());
            Row(null, "хорошо", s.FqwB.ToString());
            Row(null, "удовлетворительно", s.FqwC.ToString());
            Row(null, "неудовлетворительно", s.FqwD.ToString());
            Row("2.4",
                "Количество студентов, не явившихся на защиту ВКР по уважительной / неуважительной причине",
                s.FqwAbsentAll.ToString(), s.FqwAbsentFull.ToString(),
                s.FqwAbsentExtra.ToString(), s.FqwAbsentPart.ToString());
            Row("2.5",
                "Количество переносов защит ВКР в соответствии с приказом по университету",
                s.FqwPostponedAll.ToString(), s.FqwPostponedFull.ToString(),
                s.FqwPostponedExtra.ToString(), s.FqwPostponedPart.ToString());

            Row("2.6", "Количество ВКР: исследовательского типа", s.FqwResearch.ToString());
            Row(null, "практико-ориентированного типа", s.FqwPractice.ToString());
            Row(null, "проектно-ориентированного типа", s.FqwProject.ToString());
            Row(null, "ВКР «Стартап как диплом»", s.FqwStartup.ToString());
            Row(null, "ВКР как социально-значимый проект", s.FqwSocial.ToString());

            Row("2.7", "Количество ВКР, рекомендованных: к опубликованию", s.FqwToPublish.ToString());
            Row(null, "к внедрению", s.FqwToImplement.ToString());
            Row(null, "внедрённых", s.FqwImplemented.ToString());

            Row("2.8", "Количество дипломов с отличием", s.HonourDiplomas.ToString());

            Row("2.9", "Результаты проверки ВКР на наличие заимствований:", "");
            Row(null, "среднее значение оригинальности ВКР, %¹",
                s.AvgOriginality?.ToString("0.0") ?? "");

            Debug.WriteLine($"ФИНАЛ: rowNum={rowNum}, tableRows={t.RowCount}");

            /* ---------- 5. шрифт ---------- */
            foreach (var p in t.Paragraphs)
                p.Font("Times New Roman").FontSize(11);

            // ---------- 6. вертикальное выравнивание ----------
            foreach (var row in t.Rows)
                foreach (var cell in row.Cells)
                    cell.VerticalAlignment = Xceed.Document.NET.VerticalAlignment.Center;

            // ---------- 8. высота строк ----------
            t.Rows[0].Height = 18;  // Верхняя строка «№ п/п», «Форма обучения»
            t.Rows[1].Height = 16;  // Средняя строка с «очная», «заочная»
            t.Rows[2].Height = 16;  // «кол / %»
            t.Rows[3].Height = 14;  // строка с номерами 1..10
            for (int i = 4; i < t.RowCount; i++)  // начиная с 4-й строки (данные)
            {
                t.Rows[i].MinHeight = 16;
            }

            return t;
        }


        private Table BuildChairmanTable(DocX doc, Staff chairman)
        {
            Debug.WriteLine("[Chairman] Start BuildChairmanTable");
            var t = doc.AddTable(4, 7);
            Debug.WriteLine($"[Chairman] Created table: Rows={t.RowCount}, Cols={t.ColumnCount}");
            t.Alignment = Alignment.center;
            t.Design = TableDesign.TableGrid;
            t.SetWidths(new float[] { 60f, 120f, 150f, 200f, 150f, 150f, 100f });
            Debug.WriteLine("[Chairman] Set column widths");

            // Заголовок
            t.Rows[0].MergeCells(0, 6);
            t.Rows[0].Cells[0].Paragraphs[0]
                .Append("Председатель ГЭК")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;

            // Заполняем вторую строку (r1) без горизонтального слияния
            var r1 = t.Rows[1];
            r1.Cells[0].Paragraphs[0].Append("Образовательная программа")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            r1.Cells[1].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12).Bold();
            r1.Cells[2].Paragraphs[0].Append("Фамилия, имя, отчество")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            r1.Cells[3].Paragraphs[0].Append("Основное место работы\n(субъект РФ, город), занимаемая должность")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            r1.Cells[4].Paragraphs[0].Append("Учёная степень\n(серия, №, дата)")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            r1.Cells[5].Paragraphs[0].Append("Учёное звание\n(серия, №, дата)")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            r1.Cells[6].Paragraphs[0].Append("Почётное звание")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;

            // Заполняем третью строку (r2)
            var r2 = t.Rows[2];
            r2.Cells[0].Paragraphs[0].Append("Шифр")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            r2.Cells[1].Paragraphs[0].Append("Наименование, профиль")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            // Остальные ячейки r2 пока пустые
            for (int i = 2; i < 7; i++)
            {
                r2.Cells[i].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);
            }

            Debug.WriteLine($"[Chairman] Before vertical merge: Rows={t.RowCount}, Cols={t.ColumnCount}");
            for (int col = 2; col <= 6; col++)
            {
                Debug.WriteLine($"[Chairman] Merging column {col} rows 1 and 2");
                t.MergeCellsInColumn(col, 1, 2);
            }

            // Теперь можно объединить r1.Cells[0] и r1.Cells[1]
            r1.MergeCells(0, 1);
            Debug.WriteLine("[Chairman] Merged r1 cells 0-1");

            Debug.WriteLine("[Chairman] Filling row 3 data");
            var data = new[]
            {
                "Тест шифр", "Тест профиль",
                chairman.FullName ?? "",
                chairman.Position ?? "",
                chairman.Degree ?? "",
                chairman.DegreeRank ?? "",
                chairman.DegreeAwards ?? ""
            };
            for (int c = 0; c < 7; c++)
            {
                Debug.WriteLine($"[Chairman] Row 3, Cell {c} -> '{data[c]}'");
                t.Rows[3].Cells[c].Paragraphs[0]
                    .Append(data[c])
                    .Font("Times New Roman").FontSize(12)
                    .Alignment = Alignment.center;
            }

            Debug.WriteLine("[Chairman] Finished BuildChairmanTable");
            return t;
        }

        private Table BuildCommissionTable(DocX doc, List<Staff> members, Staff? secretary = null)
        {
            Debug.WriteLine("[Commission] Start BuildCommissionTable");
            bool hasMembers = members != null && members.Count > 0;
            foreach (var m in members)
            {
                Debug.WriteLine("[Commission] All members: " + m.FullName);
            }
            bool hasSecretary = secretary != null;
            Debug.WriteLine($"[Commission] hasMembers={hasMembers}, hasSecretary={hasSecretary}");

            int rowsForMembers = 1 + 1 + 1 + 4;
            int rowsForSecretary = hasSecretary ? 3 : 0;
            int totalRows = rowsForMembers + rowsForSecretary;
            Debug.WriteLine($"[Commission] totalRows={totalRows}");

            var t = doc.AddTable(totalRows, 6);
            Debug.WriteLine($"[Commission] Created table: Rows={t.RowCount}, Cols={t.ColumnCount}");
            t.Alignment = Alignment.center;
            t.Design = TableDesign.TableGrid;
            t.SetWidths(new float[] { 50f, 70f, 140f, 180f, 160f, 100f });
            Debug.WriteLine("[Commission] Set column widths");
            t.MergeCellsInColumn(3, 1, 2);
            t.MergeCellsInColumn(4, 1, 2);
            t.MergeCellsInColumn(5, 1, 2);
            int currentRow = 0;

            // Заголовок
            Debug.WriteLine("[Commission] Merging row 0 cells 0-5 for title");
            t.Rows[currentRow].MergeCells(0, 5);
            t.Rows[currentRow].Cells[0].Paragraphs[0]
                .Append("Члены ГЭК по защите выпускной квалификационной работы")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            currentRow++;

            // Шапка
            Debug.WriteLine($"[Commission] Filling header row at index {currentRow}");
            var rHeader = t.Rows[currentRow];
            rHeader.Cells[0].Paragraphs[0].Append("№ п/п")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            rHeader.Cells[1].Paragraphs[0].Append("Образовательная программа")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            rHeader.Cells[2].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12).Bold();
            rHeader.Cells[3].Paragraphs[0].Append("Фамилия, имя, отчество")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            rHeader.Cells[4].Paragraphs[0].Append("Основное место работы, занимаемая должность")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            rHeader.Cells[5].Paragraphs[0].Append("Учёная степень, учёное звание")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;

            Debug.WriteLine($"[Commission] Before merging header cells: RowCount={t.RowCount}, ColCount={t.ColumnCount}");
            // Горизонтальное слияние в шапке (строка currentRow)
            rHeader.MergeCells(1, 2);
            Debug.WriteLine("[Commission] Merged header cells 1-2");
            currentRow++;

            // Подшапка
            Debug.WriteLine($"[Commission] Filling subheader row at index {currentRow}");
            var rSub = t.Rows[currentRow];
            rSub.Cells[0].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);
            rSub.Cells[1].Paragraphs[0].Append("Шифр")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            rSub.Cells[2].Paragraphs[0].Append("Наименование, профиль")
                .Font("Times New Roman").FontSize(12).Bold()
                .Alignment = Alignment.center;
            rSub.Cells[3].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);
            rSub.Cells[4].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);
            rSub.Cells[5].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);

            Debug.WriteLine($"[Commission] Before merging vertical: RowCount={t.RowCount}, ColCount={t.ColumnCount}");
            // Безопасная проверка на существование строк и столбцов перед vertical merge
            if (t.RowCount > currentRow && t.RowCount > currentRow - 1)
            {
                // Объединяем вертикально столбец 0 (№ п/п)
                if (t.Rows[currentRow - 1].Cells.Count > 0 && t.Rows[currentRow].Cells.Count > 0)
                {
                    Debug.WriteLine($"[Commission] Merging vertical column 0 rows {currentRow - 1} and {currentRow}");
                    t.MergeCellsInColumn(0, currentRow - 1, currentRow);
                }
                
                //// Объединяем вертикально столбцы 3, 4, 5
                //for (int col = 3; col <= 5; col++)
                //{
                //    if (t.Rows[currentRow - 1].Cells.Count > col && t.Rows[currentRow].Cells.Count > col)
                //    {
                //        Debug.WriteLine($"[Commission] Merging vertical column {col} rows {currentRow - 1} and {currentRow}");
                //        t.MergeCellsInColumn(col, currentRow - 1, currentRow);
                //    }
                //}
                Debug.WriteLine("[Commission] Merged vertical cells for cols 0, 3, 4, 5");
            }
            currentRow++;

            // Строки с членами
            Debug.WriteLine($"[Commission] Filling member rows starting at index {currentRow}");
            for (int i = 0; i < 4; i++)
            {
                int rowIndex = currentRow + i;
                if (rowIndex >= t.RowCount) break;
                Debug.WriteLine($"[Commission] Filling row {rowIndex}");
                var rowM = t.Rows[rowIndex];

                rowM.Cells[0].Paragraphs[0]
                    .Append((i + 1).ToString())
                    .Font("Times New Roman").FontSize(12)
                    .Alignment = Alignment.center;
                rowM.Cells[1].Paragraphs[0].Append("Тест шифр").Font("Times New Roman").FontSize(12);
                rowM.Cells[2].Paragraphs[0].Append("Тест профиль").Font("Times New Roman").FontSize(12);

                if (i < members.Count)
                {
                    var s = members[i];
                    rowM.Cells[3].Paragraphs[0]
                        .Append(s.FullName ?? "")
                        .Font("Times New Roman").FontSize(12)
                        .Alignment = Alignment.center;
                    rowM.Cells[4].Paragraphs[0]
                        .Append(s.Position ?? "")
                        .Font("Times New Roman").FontSize(12)
                        .Alignment = Alignment.center;
                    var degParts = new[] { s.Degree, s.DegreeRank }
                        .Where(v => !string.IsNullOrWhiteSpace(v));
                    rowM.Cells[5].Paragraphs[0]
                        .Append(string.Join(", ", degParts))
                        .Font("Times New Roman").FontSize(12)
                        .Alignment = Alignment.center;
                }
                else
                {
                    rowM.Cells[3].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);
                    rowM.Cells[4].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);
                    rowM.Cells[5].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);
                }

                rowM.MinHeight = 20;
            }
            currentRow += 4;

            // Блок секретаря (если есть)
            if (hasSecretary)
            {
                Debug.WriteLine($"[Commission] Adding secretary block at row {currentRow}");
                if (currentRow < t.RowCount)
                {
                    t.Rows[currentRow].MergeCells(0, 5);
                    t.Rows[currentRow].Cells[0].Paragraphs[0]
                        .Append("Секретарь ГЭК по защите выпускной квалификационной работы")
                        .Font("Times New Roman").FontSize(12).Bold()
                        .Alignment = Alignment.center;
                    currentRow++;
                }

                if (currentRow < t.RowCount)
                {
                    Debug.WriteLine($"[Commission] Filling secretary header at row {currentRow}");
                    var rSecH = t.Rows[currentRow];

                    rSecH.Cells[0].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);
                    rSecH.Cells[1].Paragraphs[0].Append("Шифр")
                        .Font("Times New Roman").FontSize(12).Bold()
                        .Alignment = Alignment.center;
                    rSecH.Cells[2].Paragraphs[0].Append("Наименование")
                        .Font("Times New Roman").FontSize(12).Bold()
                        .Alignment = Alignment.center;
                    rSecH.Cells[3].Paragraphs[0].Append("Фамилия, имя, отчество")
                        .Font("Times New Roman").FontSize(12).Bold()
                        .Alignment = Alignment.center;
                    rSecH.Cells[4].Paragraphs[0].Append("Основное место работы, занимаемая должность")
                        .Font("Times New Roman").FontSize(12).Bold()
                        .Alignment = Alignment.center;
                    rSecH.Cells[5].Paragraphs[0].Append("Учёная степень, учёное звание")
                        .Font("Times New Roman").FontSize(12).Bold()
                        .Alignment = Alignment.center;

                    
                    currentRow++;
                }

                if (currentRow < t.RowCount)
                {
                    Debug.WriteLine($"[Commission] Filling secretary data at row {currentRow}");
                    var rowS = t.Rows[currentRow];
                    rowS.Cells[0].Paragraphs[0].Append("").Font("Times New Roman").FontSize(12);
                    rowS.Cells[1].Paragraphs[0].Append("Тест шифр").Font("Times New Roman").FontSize(12);
                    rowS.Cells[2].Paragraphs[0].Append("Тест наименование").Font("Times New Roman").FontSize(12);
                    rowS.Cells[3].Paragraphs[0]
                        .Append(secretary.FullName ?? "")
                        .Font("Times New Roman").FontSize(12)
                        .Alignment = Alignment.center;
                    rowS.Cells[4].Paragraphs[0]
                        .Append(secretary.Position ?? "")
                        .Font("Times New Roman").FontSize(12)
                        .Alignment = Alignment.center;
                    var sDeg = new[] { secretary.Degree, secretary.DegreeRank }
                        .Where(v => !string.IsNullOrWhiteSpace(v));
                    rowS.Cells[5].Paragraphs[0]
                        .Append(string.Join(", ", sDeg))
                        .Font("Times New Roman").FontSize(12)
                        .Alignment = Alignment.center;
                    rowS.MinHeight = 20;
                }
            }

            Debug.WriteLine("[Commission] Finished BuildCommissionTable");
            return t;
        }

        private Table BuildVkrTopicTable(DocX doc, List<VkrTopicInfo> rows)
        {
            // +1 строка под шапку
            var t = doc.AddTable(rows.Count + 1, 6);
            t.Alignment = Alignment.center;
            t.Design = TableDesign.TableGrid;
            t.SetWidths(new float[] { 45f, 200f, 75f, 140f, 270f, 220f });

            // --- шапка ---
            string[] head = {
                "№",
                "ФИО студента",
                "Шифр ОП",
                "Профиль",
                "Тема ВКР",
                "Руководитель (ФИО, должность)"
            };
            for (int c = 0; c < head.Length; c++)
                t.Rows[0].Cells[c].Paragraphs[0]
                    .Append(head[c]).Font("Times New Roman").FontSize(12).Bold()
                    .Alignment = Alignment.center;

            // --- данные ---
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var row = t.Rows[i + 1];

                row.Cells[0].Paragraphs[0].Append((i + 1).ToString())
                   .Font("Times New Roman").FontSize(12)
                   .Alignment = Alignment.center;

                row.Cells[1].Paragraphs[0].Append(r.StudentFio)
                   .Font("Times New Roman").FontSize(12);

                row.Cells[2].Paragraphs[0].Append(r.OrientationCode)
                   .Font("Times New Roman").FontSize(12)
                   .Alignment = Alignment.center;

                row.Cells[3].Paragraphs[0].Append(r.OrientationName)
                   .Font("Times New Roman").FontSize(12);

                row.Cells[4].Paragraphs[0].Append(r.Topic)
                   .Font("Times New Roman").FontSize(12);

                row.Cells[5].Paragraphs[0].Append(
                      $"{r.SupervisorFio}{(string.IsNullOrWhiteSpace(r.SupervisorPosition) ? "" : ", " + r.SupervisorPosition)}")
                   .Font("Times New Roman").FontSize(12);
            }

            return t;
        }

        /// <summary>
        /// Строит таблицу-график заседаний ГЭК.
        /// ─ basic  (4 колонки)  – как на 1-м скрине  
        /// ─ full   (6 колонок)  – с направлением и количеством студентов (2-й скрин)
        /// </summary>
        /// <param name="doc">Документ-приёмник</param>
        /// <param name="extended">
        ///     false  ➜ 4-колоночный вариант (по-умолчанию)  
        ///     true   ➜ 6-колоночный вариант
        /// </param>
        /// <param name="dataRows">Сколько пустых строк снизу добавить</param>
        public static Table BuildGekScheduleTable(DocX doc,
                                                  bool extended = false,
                                                  int dataRows = 8)
        {
            /* -----------------------------------------------------------
             * 1) конфигурация под оба варианта
             * -----------------------------------------------------------*/
            string[] headers4 =
            {
                "Дата проведения",
                "Время работы ГЭК",
                "Место проведения (аудитория-корпус)",
                "Примечание (группа и т.д.)"
            };

            string[] headers6 =
            {
                "Дата проведения",
                "Время работы ГЭК",
                "Место проведения (аудитория-корпус)",
                "Направление\n(шифр, название)",
                "Количество\nстудентов",
                "Примечание\n(группа и т.д.)"
            };

            var headers = extended ? headers6 : headers4;

            // ширины колонок в процентном соотношении к 100 % ширины контента
            float[] widths4 = { 18, 18, 32, 32 };          // ≈ 100 %
            float[] widths6 = { 15, 15, 23, 22, 12, 13 };  // 100 %

            var widths = extended ? widths6 : widths4;

            /* -----------------------------------------------------------
             * 2) создаём таблицу
             * -----------------------------------------------------------*/
            int rows = dataRows + 1;
            int cols = headers.Length;

            var table = doc.AddTable(rows, cols);

            // базовые границы
            table.Design = TableDesign.TableGrid;
            table.Alignment = Alignment.center;

            // применяем тонкие границы ко всем сторонам
            foreach (var bType in Enum.GetValues<TableBorderType>())
                table.SetBorder(bType,
                     new Border(BorderStyle.Tcbs_single, BorderSize.one, 0, Xceed.Drawing.Color.Black));

            /* -----------------------------------------------------------
             * 3) шапка
             * -----------------------------------------------------------*/
            for (int c = 0; c < cols; c++)
            {
                var cell = table.Rows[0].Cells[c];

                cell.Paragraphs[0]
                    .Append(headers[c])
                    .Bold()
                    .FontSize(11)
                    .Alignment = Alignment.center;
                
                cell.VerticalAlignment = Xceed.Document.NET.VerticalAlignment.Center;
            }

            /* -----------------------------------------------------------
             * 4) ширины колонок
             * -----------------------------------------------------------*/
            // Word считает ширину в twentieth of a point (1/1440 inch) – используем doc.PageWidth?
            // Проще: берём 16 см рабочей области  =  16 cm * 567 twips/cm ≈ 9050 twips
            const float total = 9050f;
            for (int c = 0; c < cols; c++)
                table.SetColumnWidth(c, total * widths[c] / 100f);

            /* -----------------------------------------------------------
             * 5) заполняем пустые строки-заглушки
             * -----------------------------------------------------------*/
            for (int r = 1; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    table.Rows[r].Cells[c].Paragraphs[0].Append("").FontSize(11);

            return table;
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

        private void ReplaceAllSmart(DocX doc, Dictionary<string, string> replacements, Dictionary<string, Table> tables, double? fontsize = 10)
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
                        if (fontsize != null)
                        {
                            para.Append(value).Font("Times New Roman").FontSize((double)fontsize);
                        }
                        else
                        {
                            para.Append(value).Font("Times New Roman");
                        }
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
                    MappingSourceType.Student or
                    MappingSourceType.Staff or
                    MappingSourceType.Orientation => raw,

                    MappingSourceType.Table =>
                        dataContext?.GetType().GetProperty(m.Property)?.GetValue(dataContext)?.ToString() ?? string.Empty,

                    MappingSourceType.Group =>
                        dataContext is Group g ? await BuildGroupValue(g, db) : string.Empty,

                    MappingSourceType.Calculated => GetCalculatedValue(m.Placeholder),

                    MappingSourceType.ProtocolAutoFio
                        when dataContext is Student st
                            => st.FullName,

                    MappingSourceType.ProtocolAutoDate
                        => DateTime.Now.ToString("dd.MM.yyyy"),

                    MappingSourceType.ProtocolAutoTime
                        => DateTime.Now.ToString("HH:mm"),

                    MappingSourceType.ProtocolAutoOrientationCode
                        when dataContext is Student st2
                            => (await db.GetOrientationByIdAsync(st2.orientation))?.Code ?? "",

                    MappingSourceType.ProtocolAutoOrientationName
                        when dataContext is Student st3
                            => (await db.GetOrientationByIdAsync(st3.orientation))?.Name ?? "",

                    MappingSourceType.ProtocolAutoGroupName
                        when dataContext is Student st4
                            => (await db.GetGroupByIdAsync(st4.groupId))?.Name ?? "",
                    
                    MappingSourceType.ProtocolAutoInstitute
                        when dataContext is Student st6
                            => (await db.GetInstituteByIdAsync(st6.institute))?.Name ?? "",

                    MappingSourceType.ProtocolAutoSupervisorFio
                        when dataContext is Student st5
                            => (await db.GetFqwByStudentIdAsync(st5.Id)) is { } fqw &&
                               await db.GetStaffByIdAsync(fqw.SupervisorId) is { } sup
                                    ? sup.FullName
                                    : "",

                    MappingSourceType.ProtocolAutoDay
                        => DateTime.Now.Day.ToString(),                // «33»

                    MappingSourceType.ProtocolAutoMonth
                        => CultureInfo.GetCultureInfo("ru-RU")
                                      .DateTimeFormat
                                      .MonthGenitiveNames[DateTime.Now.Month - 1]
                                      .Replace(char.MinValue, ' ')
                                      .Trim()                                             // «мая»
                                         is var month && month.Length > 0
                                           ? char.ToUpper(month[0]) + month[1..]                  // «Мая»
                                           : "",

                    MappingSourceType.ProtocolAutoYear
                        => DateTime.Now.Year.ToString(),              // «2025»

                    MappingSourceType.ProtocolAutoFqwTopic
                        when dataContext is Student st6
                            => (await db.GetFqwByStudentIdAsync(st6.Id))?.Topic ?? "",

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