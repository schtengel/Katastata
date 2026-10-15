using Katastata.Data;
using Katastata.Helpers;
using Katastata.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using DocumentFormat.OpenXml.Wordprocessing;

namespace Katastata.Services
{
    public class AppMonitorService
    {
        private const int TickMs = 10000;
        // Сессии короче этого значения не сохраняются
        private static readonly TimeSpan MinSession = TimeSpan.FromMinutes(1);

        private class ActiveSession
        {
            public int ProgramId;
            public DateTime ProcStart; // время запуска процесса (для защиты от повторного использования PID)
            public DateTime Start;     // начало сессии
            public DateTime LastSeen;  // когда процесс последний раз видели живым
        }

        private readonly DbContextOptions<AppDbContext> _options;
        private readonly object _sync = new object();
        private readonly Dictionary<int, ActiveSession> _active = new Dictionary<int, ActiveSession>();
        private readonly HashSet<int> _ignored = new HashSet<int>(); // процессы, к которым нет доступа
        private System.Timers.Timer? monitoringTimer;
        private int _userId;
        private DateTime _monitorStart;

        // Каждая операция работает со своим DbContext: контекст не потокобезопасен,
        // а таймер и UI живут в разных потоках.
        public AppMonitorService(DbContextOptions<AppDbContext> options) => _options = options;

        private AppDbContext NewDb() => new AppDbContext(_options);

        // ---------- Мониторинг ----------

        public void StartMonitoring(int userId)
        {
            if (monitoringTimer != null) return;
            _userId = userId;
            _monitorStart = DateTime.Now;
            monitoringTimer = new System.Timers.Timer(TickMs) { AutoReset = true };
            monitoringTimer.Elapsed += (sender, e) => Tick();
            monitoringTimer.Start();
        }

        // Вызывать при выходе: программы ещё работают, поэтому дописываем их сессии до текущего момента
        public void StopMonitoring()
        {
            monitoringTimer?.Stop();
            monitoringTimer?.Dispose();
            monitoringTimer = null;

            lock (_sync)
            {
                var now = DateTime.Now;
                foreach (var session in _active.Values)
                {
                    try { SaveSession(session, now); }
                    catch (Exception ex) { Log(ex); }
                }
                _active.Clear();
            }
        }

        private void Tick()
        {
            // если прошлый тик ещё не закончился - пропускаем этот
            if (!System.Threading.Monitor.TryEnter(_sync)) return;

            var visible = new Dictionary<int, Process>();
            try
            {
                var now = DateTime.Now;

                // программы = процессы с главным окном
                foreach (var proc in Process.GetProcesses())
                {
                    bool hasWindow = false;
                    try { hasWindow = !string.IsNullOrEmpty(proc.MainWindowTitle); } catch { }

                    if (hasWindow) visible[proc.Id] = proc;
                    else proc.Dispose();
                }

                // 1. Закрытие: процесс исчез или PID занял уже другой процесс
                foreach (var pid in _active.Keys.ToList())
                {
                    var session = _active[pid];
                    bool alive = visible.TryGetValue(pid, out var proc);
                    if (alive)
                    {
                        try { alive = proc!.StartTime == session.ProcStart; }
                        catch { } // не удалось прочитать - считаем тем же процессом
                    }

                    if (alive)
                    {
                        session.LastSeen = now;
                    }
                    else
                    {
                        _active.Remove(pid);
                        try { SaveSession(session, session.LastSeen); }
                        catch (Exception ex) { Log(ex); }
                    }
                }

                // 2. Открытие: новые процессы
                _ignored.RemoveWhere(pid => !visible.ContainsKey(pid));
                foreach (var kv in visible)
                {
                    if (_active.ContainsKey(kv.Key) || _ignored.Contains(kv.Key)) continue;
                    try { OpenSession(kv.Value, now); }
                    catch (Exception ex) { Log(ex); }
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
            finally
            {
                foreach (var proc in visible.Values) proc.Dispose();
                System.Threading.Monitor.Exit(_sync);
            }
        }

        private void OpenSession(Process proc, DateTime now)
        {
            DateTime procStart;
            string path;
            string name;
            try
            {
                procStart = proc.StartTime;
                path = proc.MainModule?.FileName ?? "";
                name = proc.ProcessName;
            }
            catch
            {
                _ignored.Add(proc.Id); // нет доступа (процесс администратора)
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                _ignored.Add(proc.Id);
                return;
            }

            using var db = NewDb();
            var program = db.Programs.FirstOrDefault(p => p.Path == path);
            if (program == null)
            {
                EnsureDefaultCategory(db);
                program = new Program
                {
                    Name = string.IsNullOrEmpty(name) ? System.IO.Path.GetFileNameWithoutExtension(path) : name,
                    Path = path,
                    CategoryId = 1
                };
                db.Programs.Add(program);
                db.SaveChanges();
            }

            // Если программа была запущена до старта мониторинга, считаем с момента старта мониторинга,
            // иначе при перезапуске Katastata одно и то же время попало бы в статистику дважды
            var start = procStart < _monitorStart ? _monitorStart : procStart;

            _active[proc.Id] = new ActiveSession
            {
                ProgramId = program.Id,
                ProcStart = procStart,
                Start = start,
                LastSeen = now
            };
        }

        // Сессия и статистика пишутся одним SaveChanges, т.е. в одной транзакции
        private void SaveSession(ActiveSession session, DateTime end)
        {
            if (end < session.Start) end = session.Start;
            if (end - session.Start < MinSession) return;

            using var db = NewDb();
            db.Sessions.Add(new Session
            {
                UserId = _userId,
                ProgramId = session.ProgramId,
                StartTime = session.Start,
                EndTime = end
            });

            var stat = db.Statistics.FirstOrDefault(s => s.UserId == _userId && s.ProgramId == session.ProgramId);
            if (stat == null)
            {
                stat = new Statistics { UserId = _userId, ProgramId = session.ProgramId, TotalTime = TimeSpan.Zero };
                db.Statistics.Add(stat);
            }
            stat.TotalTime += end - session.Start;
            if (stat.LastLaunch == null || session.Start > stat.LastLaunch) stat.LastLaunch = session.Start;

            db.SaveChanges();
        }

        private static void Log(Exception ex)
        {
            try
            {
                System.IO.Directory.CreateDirectory(AppPaths.DataDir);
                System.IO.File.AppendAllText(AppPaths.LogPath, $"{DateTime.Now:s} {ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { }
        }

        private static void EnsureDefaultCategory(AppDbContext db)
        {
            if (!db.Categories.Any(c => c.Id == 1))
            {
                db.Categories.Add(new Models.Category { Id = 1, Name = "Не классифицировано" });
                db.SaveChanges();
            }
        }

        // ---------- Сканирование ----------

        public void ScanRunningPrograms(int userId)
        {
            using var db = NewDb();
            EnsureDefaultCategory(db);

            var known = db.Programs.Select(p => p.Path).ToHashSet();

            foreach (var proc in Process.GetProcesses())
            {
                using (proc)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(proc.MainWindowTitle)) continue;
                        string path = proc.MainModule?.FileName ?? "";
                        if (string.IsNullOrEmpty(path) || !known.Add(path)) continue;

                        db.Programs.Add(new Program
                        {
                            Name = proc.ProcessName ?? "Unknown",
                            Path = path,
                            CategoryId = 1
                        });
                    }
                    catch { }
                }
            }
            db.SaveChanges();
        }

        // ---------- Чтение ----------

        public List<Program> GetAllPrograms(int userId)
        {
            using var db = NewDb();
            return db.Programs
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Statistics.Where(s => s.UserId == userId))
                .ToList();
        }

        public List<Session> GetSessions(int userId)
        {
            using var db = NewDb();
            return db.Sessions
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Include(s => s.Program)
                .ThenInclude(p => p.Category)
                .OrderByDescending(s => s.StartTime)
                .ToList();
        }

        public List<Statistics> GetStatistics(int userId)
        {
            using var db = NewDb();
            var stats = db.Statistics
                .AsNoTracking()
                .Where(st => st.UserId == userId)
                .Include(st => st.Program)
                .ToList();

            // TimeSpan в SQLite хранится строкой, сортируем уже в памяти
            return stats
                .OrderByDescending(st => st.TotalTime)
                .ToList();
        }

        // ---------- Категории и программы ----------

        public bool CategoryExists(string name)
        {
            using var db = NewDb();
            return db.Categories.Any(c => c.Name == name);
        }

        public void AddCategory(string name)
        {
            using var db = NewDb();
            db.Categories.Add(new Katastata.Models.Category { Name = name });
            db.SaveChanges();
        }

        public List<Program> GetProgramsByCategory(int categoryId)
        {
            using var db = NewDb();
            return db.Programs.AsNoTracking().Where(p => p.CategoryId == categoryId).ToList();
        }

        public List<Katastata.Models.Category> GetAllCategories()
        {
            using var db = NewDb();
            return db.Categories.AsNoTracking().ToList();
        }

        // Обновляем только изменяемые поля: Update(program) переписал бы и вложенную статистику устаревшими значениями
        public void UpdateProgram(Program program)
        {
            using var db = NewDb();
            var entity = db.Programs.Find(program.Id);
            if (entity == null) return;
            entity.Name = program.Name;
            entity.CategoryId = program.CategoryId;
            db.SaveChanges();
        }

        public void DeleteCategory(int categoryId)
        {
            using var db = NewDb();
            var category = db.Categories.Find(categoryId);
            if (category != null)
            {
                db.Categories.Remove(category);
                db.SaveChanges();
            }
        }

        // Удаление пользователя и связанных данных
        public void DeleteUser(int userId)
        {
            using var db = NewDb();
            var user = db.Users.Find(userId);
            if (user != null)
            {
                db.Users.Remove(user);
                db.SaveChanges();
            }
        }

        // ---------- Экспорт ----------

        public void ExportStatisticsToExcel(int userId, string filePath)
        {
            var stats = GetStatistics(userId);
            using (SpreadsheetDocument document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Statistics" };
                sheets.Append(sheet);

                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

                // Заголовки
                Row headerRow = new Row();
                headerRow.Append(CreateTextCell("Программа"));
                headerRow.Append(CreateTextCell("Общее время (ч)"));
                headerRow.Append(CreateTextCell("Последний запуск"));
                sheetData.Append(headerRow);

                // Данные
                foreach (var stat in stats)
                {
                    Row dataRow = new Row();
                    dataRow.Append(CreateTextCell(stat.Program?.Name ?? "Unknown"));
                    dataRow.Append(CreateNumberCell(Math.Round(stat.TotalTime.TotalHours, 2)));
                    dataRow.Append(CreateTextCell(stat.LastLaunch?.ToString() ?? "N/A"));
                    sheetData.Append(dataRow);
                }

                workbookPart.Workbook.Save();
            }
        }

        private Cell CreateTextCell(string text)
        {
            return new Cell { CellValue = new CellValue(text), DataType = CellValues.String };
        }

        // Число, а не текст: в Excel такую колонку можно суммировать
        private Cell CreateNumberCell(double value)
        {
            return new Cell
            {
                CellValue = new CellValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                DataType = CellValues.Number
            };
        }

        public void ExportStatisticsToWord(int userId, string filePath)
        {
            var stats = GetStatistics(userId);
            using (WordprocessingDocument document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());
                DocumentFormat.OpenXml.Wordprocessing.Table table = new DocumentFormat.OpenXml.Wordprocessing.Table();
                TableRow headerRow = new TableRow();
                headerRow.Append(new TableCell(new Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Программа")))));
                headerRow.Append(new TableCell(new Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Общее время")))));
                headerRow.Append(new TableCell(new Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Последний запуск")))));
                table.Append(headerRow);
                foreach (var stat in stats)
                {
                    TableRow dataRow = new TableRow();
                    dataRow.Append(new TableCell(new Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(stat.Program?.Name ?? "Unknown")))));
                    dataRow.Append(new TableCell(new Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(stat.TotalTime.ToString())))));
                    dataRow.Append(new TableCell(new Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(stat.LastLaunch?.ToString() ?? "N/A")))));
                    table.Append(dataRow);
                }
                body.Append(table);
                mainPart.Document.Save();
            }
        }
    }
}