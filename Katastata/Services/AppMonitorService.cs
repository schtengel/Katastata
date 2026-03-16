using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Katastata.Contracts;
using Katastata.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Katastata.Services
{
    public class AppMonitorService
    {
        private readonly ApiClient _apiClient;

        private readonly Dictionary<int, Session> activeSessions = new Dictionary<int, Session>();
        private System.Timers.Timer? monitoringTimer;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public AppMonitorService(ApiClient apiClient) => _apiClient = apiClient;

        public void StartMonitoring(int userId)
        {
            if (monitoringTimer == null)
            {
                monitoringTimer = new System.Timers.Timer(10000);
                monitoringTimer.Elapsed += (sender, e) => MonitorProcesses(userId);
                monitoringTimer.Start();
            }
        }

        private void MonitorProcesses(int userId)
        {
            try
            {
                var currentProcesses = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                    .ToDictionary(p => p.Id, p => p);

                foreach (var sessionId in activeSessions.Keys.ToList())
                {
                    if (!currentProcesses.ContainsKey(sessionId))
                    {
                        var session = activeSessions[sessionId];
                        session.EndTime = DateTime.Now;
                        _apiClient.CreateSession(new SessionCreateRequest
                        {
                            UserId = session.UserId,
                            ProgramId = session.ProgramId,
                            StartTime = session.StartTime,
                            EndTime = session.EndTime
                        });
                        activeSessions.Remove(sessionId);
                    }
                }

                IntPtr foregroundWindow = GetForegroundWindow();
                GetWindowThreadProcessId(foregroundWindow, out var fgProcessId);

                if (fgProcessId > 0 && currentProcesses.TryGetValue((int)fgProcessId, out var fgProcess) &&
                    !activeSessions.ContainsKey(fgProcess.Id))
                {
                    string path = "";
                    try { path = fgProcess.MainModule?.FileName ?? ""; } catch { }

                    if (!string.IsNullOrEmpty(path))
                    {
                        var program = _apiClient.EnsureProgram(new ProgramSyncRequest
                        {
                            Name = fgProcess.ProcessName ?? "Unknown",
                            Path = path
                        });

                        activeSessions[fgProcess.Id] = new Session
                        {
                            UserId = userId,
                            ProgramId = program.Id,
                            StartTime = DateTime.Now,
                            EndTime = DateTime.Now
                        };
                    }
                }
            }
            catch { }
        }

        public void ScanRunningPrograms(int userId)
        {
            var requests = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .Select(proc =>
                {
                    try
                    {
                        var path = proc.MainModule?.FileName ?? "";
                        if (string.IsNullOrEmpty(path))
                        {
                            return null;
                        }

                        return new ProgramSyncRequest
                        {
                            Name = proc.ProcessName ?? "Unknown",
                            Path = path
                        };
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(x => x != null)
                .Cast<ProgramSyncRequest>()
                .ToList();

            if (requests.Count > 0)
            {
                _apiClient.BulkEnsurePrograms(requests);
            }
        }

        public List<Program> GetAllPrograms(int userId) => _apiClient.GetPrograms(userId);
        public List<Session> GetSessions(int userId) => _apiClient.GetSessions(userId);
        public List<Statistics> GetStatistics(int userId) => _apiClient.GetStatistics(userId).OrderByDescending(st => st.TotalTime).ToList();

        public bool CategoryExists(string name) => _apiClient.CategoryExists(name);
        public void AddCategory(string name) => _apiClient.AddCategory(name);
        public List<Program> GetProgramsByCategory(int categoryId) => _apiClient.GetProgramsByCategory(categoryId);
        public List<Category> GetAllCategories() => _apiClient.GetCategories();
        public void UpdateProgram(Program program) => _apiClient.UpdateProgram(program);
        public void DeleteCategory(int categoryId) => _apiClient.DeleteCategory(categoryId);
        public void DeleteUser(int userId) => _apiClient.DeleteUser(userId);

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

                Row headerRow = new Row();
                headerRow.Append(CreateTextCell("Программа"));
                headerRow.Append(CreateTextCell("Общее время"));
                headerRow.Append(CreateTextCell("Последний запуск"));
                sheetData.Append(headerRow);

                foreach (var stat in stats)
                {
                    Row dataRow = new Row();
                    dataRow.Append(CreateTextCell(stat.Program?.Name ?? "Unknown"));
                    dataRow.Append(CreateTextCell(stat.TotalTime.ToString()));
                    dataRow.Append(CreateTextCell(stat.LastLaunch?.ToString() ?? "N/A"));
                    dataRow.Append(CreateTextCell(stat.UserId.ToString()));
                    sheetData.Append(dataRow);
                }

                workbookPart.Workbook.Save();
            }
        }

        private Cell CreateTextCell(string text)
        {
            return new Cell { CellValue = new CellValue(text), DataType = CellValues.String };
        }

        public void ExportStatisticsToWord(int userId, string filePath)
        {
            var stats = GetStatistics(userId);
            using (WordprocessingDocument document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());
                Table table = new Table();
                TableRow headerRow = new TableRow();
                headerRow.Append(new TableCell(new Paragraph(new Run(new Text("Программа")))));
                headerRow.Append(new TableCell(new Paragraph(new Run(new Text("Общее время")))));
                headerRow.Append(new TableCell(new Paragraph(new Run(new Text("Последний запуск")))));
                table.Append(headerRow);
                foreach (var stat in stats)
                {
                    TableRow dataRow = new TableRow();
                    dataRow.Append(new TableCell(new Paragraph(new Run(new Text(stat.Program?.Name ?? "Unknown")))));
                    dataRow.Append(new TableCell(new Paragraph(new Run(new Text(stat.TotalTime.ToString())))));
                    dataRow.Append(new TableCell(new Paragraph(new Run(new Text(stat.LastLaunch?.ToString() ?? "N/A")))));
                    table.Append(dataRow);
                }
                body.Append(table);
                mainPart.Document.Save();
            }
        }
    }
}
