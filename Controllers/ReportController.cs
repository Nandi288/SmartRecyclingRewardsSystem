// iTextSharp for PDF
using iTextSharp.text;
using iTextSharp.text.pdf;
// EPPlus for Excel
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Helpers;
using System.Web.Mvc;

using System.Windows.Forms.DataVisualization.Charting;
using Chart = System.Windows.Forms.DataVisualization.Charting.Chart;
using Color = System.Drawing.Color;
using ColorTranslator = System.Drawing.ColorTranslator;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;

namespace SmartRecyclingRewardsSystem.Controllers
{
    // UC-23: Monthly Community Recycling Report
    // UC-24: Export to PDF
    // UC-25: Export to Excel
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: /Report/Index
        // Shows the report filter form and renders the report if month/year selected
        public ActionResult Index(int? month, int? year)
        {
            // Default to current month/year
            var selectedMonth = month ?? DateTime.Now.Month;
            var selectedYear = year ?? DateTime.Now.Year;

            // Populate month/year dropdowns
            ViewBag.Months = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(),
                    Text = new DateTime(2000, m, 1).ToString("MMMM"),
                    Selected = m == selectedMonth
                }).ToList();

            ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 3, 5)
                .Select(y => new SelectListItem
                {
                    Value = y.ToString(),
                    Text = y.ToString(),
                    Selected = y == selectedYear
                }).ToList();

            var vm = BuildReport(selectedMonth, selectedYear);
            return View(vm);
        }

        // GET: /Report/ExportPdf?month=6&year=2026
        public ActionResult ExportPdf(int month, int year)
        {
            var report = BuildReport(month, year);
            var bytes = GeneratePdf(report);

            var fileName = string.Format("EcoRewardsSA_Report_{0}_{1}.pdf",
                new DateTime(year, month, 1).ToString("MMMM"), year);

            return File(bytes, "application/pdf", fileName);
        }

        // GET: /Report/ExportExcel?month=6&year=2026
        public ActionResult ExportExcel(int month, int year)
        {
            var report = BuildReport(month, year);
            var bytes = GenerateExcel(report);

            var fileName = string.Format("EcoRewardsSA_Report_{0}_{1}.xlsx",
                new DateTime(year, month, 1).ToString("MMMM"), year);

            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ── Core report builder ────────────────────────────────────────
        private CommunityReportViewModel BuildReport(int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            // All verified submissions in that month
            var submissions = _db.RecyclingSubmissions
                .Where(s => s.Status == SubmissionStatus.Verified
                         && s.SubmissionDate >= startDate
                         && s.SubmissionDate < endDate)
                .Include(s => s.MaterialType)
                .Include(s => s.Resident)
                .Include(s => s.DropOffPoint)
                .ToList();

            // Summary totals
            var totalWeight = submissions.Sum(s => s.WeightKg);
            var totalCo2 = submissions.Sum(s => s.CO2SavedKg);
            var totalPoints = submissions.Sum(s => s.PointsAwarded);
            var totalSubmissions = submissions.Count;
            var uniqueResidents = submissions.Select(s => s.ResidentId).Distinct().Count();

            // Breakdown by material
            var byMaterial = submissions
                .GroupBy(s => s.MaterialType.Name)
                .Select(g => new ReportMaterialRow
                {
                    MaterialName = g.Key,
                    ColourCode = g.First().MaterialType.ColourCode,
                    TotalWeightKg = g.Sum(s => s.WeightKg),
                    TotalCo2Kg = g.Sum(s => s.CO2SavedKg),
                    TotalPoints = g.Sum(s => s.PointsAwarded),
                    SubmissionCount = g.Count()
                })
                .OrderByDescending(x => x.TotalWeightKg)
                .ToList();

            // Top 10 residents by points this month
            var topResidents = submissions
                .GroupBy(s => new { s.ResidentId, s.Resident.FirstName, s.Resident.LastName })
                .Select(g => new ReportResidentRow
                {
                    FullName = g.Key.FirstName + " " + g.Key.LastName,
                    WeightKg = g.Sum(s => s.WeightKg),
                    Co2Kg = g.Sum(s => s.CO2SavedKg),
                    PointsEarned = g.Sum(s => s.PointsAwarded),
                    Submissions = g.Count()
                })
                .OrderByDescending(x => x.PointsEarned)
                .Take(10)
                .ToList();

            // CO2 equivalents
            var carKmEquivalent = totalCo2 > 0 ? Math.Round(totalCo2 / 0.21m, 1) : 0;
            var treesEquivalent = totalCo2 > 0 ? Math.Round(totalCo2 / 21m, 2) : 0;

            return new CommunityReportViewModel
            {
                Month = month,
                Year = year,
                MonthLabel = startDate.ToString("MMMM yyyy"),
                TotalWeightKg = totalWeight,
                TotalCo2Kg = totalCo2,
                TotalPoints = totalPoints,
                TotalSubmissions = totalSubmissions,
                UniqueResidents = uniqueResidents,
                CarKmEquivalent = carKmEquivalent,
                TreesEquivalent = treesEquivalent,
                ByMaterial = byMaterial,
                TopResidents = topResidents
            };
        }

        // ── Renders a bar chart of weight-by-material as a PNG, for embedding in the PDF ──
        private byte[] GenerateMaterialChartImage(CommunityReportViewModel report)
        {
            using (var chart = new Chart())
            {
                chart.Width = 700;
                chart.Height = 350;
                chart.BackColor = Color.White;

                var chartArea = new ChartArea("MainArea");
                chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(235, 235, 235);
                chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(235, 235, 235);
                chartArea.AxisX.LabelStyle.Font = new Font("Arial", 9);
                chartArea.AxisY.LabelStyle.Font = new Font("Arial", 9);
                chart.ChartAreas.Add(chartArea);

                var series = new Series("Weight (kg)")
                {
                    ChartType = SeriesChartType.Column,
                    IsValueShownAsLabel = true,
                    Font = new Font("Arial", 8)
                };
                chart.Series.Add(series);

                for (int i = 0; i < report.ByMaterial.Count; i++)
                {
                    var m = report.ByMaterial[i];
                    series.Points.AddXY(m.MaterialName, m.TotalWeightKg);

                    series.Points[i].Color = string.IsNullOrWhiteSpace(m.ColourCode)
                        ? ColorTranslator.FromHtml("#2d6a4f")   // fallback to brand green if a material has no colour set
                        : ColorTranslator.FromHtml(m.ColourCode);
                }

                chart.Titles.Add(new Title
                {
                    Text = "Recycling Volume by Material Type (kg)",
                    Font = new Font("Arial", 11, FontStyle.Bold),
                    ForeColor = ColorTranslator.FromHtml("#1a3a2a")
                });

                using (var ms = new MemoryStream())
                {
                    chart.SaveImage(ms, ChartImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }

        // ── PDF Generator (iTextSharp) ─────────────────────────────────
        private byte[] GeneratePdf(CommunityReportViewModel report)
        {
            using (var ms = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 40f, 40f, 50f, 50f);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                // Fonts
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(26, 58, 42));
                var headingFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(45, 106, 79));
                var labelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, new BaseColor(107, 124, 110));
                var bodyFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);
                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, new BaseColor(107, 124, 110));
                var whiteFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.WHITE);

                var greenDark = new BaseColor(26, 58, 42);
                var greenMid = new BaseColor(45, 106, 79);
                var greenLight = new BaseColor(237, 247, 241);
                var lime = new BaseColor(183, 224, 74);

                // ── Header block ───────────────────────────────────
                var headerTable = new PdfPTable(1) { WidthPercentage = 100 };
                var headerCell = new PdfPCell(new Phrase("EcoRewards SA\nCommunity Recycling Report", titleFont))
                {
                    BackgroundColor = greenDark,
                    Padding = 20f,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Border = Rectangle.NO_BORDER
                };
                headerTable.AddCell(headerCell);

                var subCell = new PdfPCell(new Phrase(report.MonthLabel + "  |  eThekwini, KwaZulu-Natal", labelFont))
                {
                    BackgroundColor = greenMid,
                    Padding = 8f,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Border = Rectangle.NO_BORDER,
                    PaddingBottom = 10f
                };
                headerTable.AddCell(subCell);
                doc.Add(headerTable);
                doc.Add(new Paragraph(" "));

                // ── Summary stats ──────────────────────────────────
                doc.Add(new Paragraph("Summary", headingFont) { SpacingBefore = 10f, SpacingAfter = 6f });

                var statsTable = new PdfPTable(5) { WidthPercentage = 100 };
                statsTable.SetWidths(new float[] { 1f, 1f, 1f, 1f, 1f });

                Action<string, string> addStat = (label, value) => {
                    var cell = new PdfPCell
                    {
                        BackgroundColor = greenLight,
                        Padding = 10f,
                        Border = Rectangle.NO_BORDER,
                        PaddingBottom = 12f
                    };
                    cell.AddElement(new Paragraph(value, headingFont) { Alignment = Element.ALIGN_CENTER });
                    cell.AddElement(new Paragraph(label, smallFont) { Alignment = Element.ALIGN_CENTER });
                    statsTable.AddCell(cell);
                };

                addStat("Total Weight (kg)", report.TotalWeightKg.ToString("0.#"));
                addStat("CO₂ Saved (kg)", report.TotalCo2Kg.ToString("0.##"));
                addStat("Points Awarded", report.TotalPoints.ToString("N0"));
                addStat("Submissions", report.TotalSubmissions.ToString());
                addStat("Residents Recycled", report.UniqueResidents.ToString());

                doc.Add(statsTable);
                doc.Add(new Paragraph(" "));

                // CO2 equivalents
                doc.Add(new Paragraph(
                    string.Format("CO₂ Impact: equivalent to {0} km driven by car, or {1} trees absorbing CO₂ for a year.",
                        report.CarKmEquivalent, report.TreesEquivalent), bodyFont)
                { SpacingAfter = 12f });

                // ── Material breakdown table ───────────────────────
                doc.Add(new Paragraph("Recycling by Material Type", headingFont) { SpacingAfter = 6f });

                var matTable = new PdfPTable(6) { WidthPercentage = 100 };
                matTable.SetWidths(new float[] { 0.5f, 2.3f, 1.4f, 1.4f, 1.4f, 1f });

                Action<string, bool> addMatHeader = (text, last) => {
                    matTable.AddCell(new PdfPCell(new Phrase(text, whiteFont))
                    {
                        BackgroundColor = greenMid,
                        Padding = 8f,
                        Border = Rectangle.NO_BORDER
                    });
                };

                addMatHeader("", false);         
                addMatHeader("Material", false);
                addMatHeader("Weight (kg)", false);
                addMatHeader("CO₂ Saved (kg)", false);
                addMatHeader("Points Awarded", false);
                addMatHeader("Submissions", true);
                bool alt = false;
                foreach (var row in report.ByMaterial)
                {
                    var bg = alt ? greenLight : BaseColor.WHITE;

                    var swatchCell = new PdfPCell
                    {
                        BackgroundColor = HexToBaseColor(row.ColourCode),
                        Padding = 7f,
                        Border = Rectangle.NO_BORDER
                    };
                    matTable.AddCell(swatchCell);

                    Action<string> addCell = (text) => {
                        matTable.AddCell(new PdfPCell(new Phrase(text, bodyFont))
                        {
                            BackgroundColor = bg,
                            Padding = 7f,
                            Border = Rectangle.NO_BORDER
                        });
                    };
                    addCell(row.MaterialName);
                    addCell(row.TotalWeightKg.ToString("0.##"));
                    addCell(row.TotalCo2Kg.ToString("0.####"));
                    addCell(row.TotalPoints.ToString("N0"));
                    addCell(row.SubmissionCount.ToString());
                    alt = !alt;
                }
                doc.Add(matTable);
                doc.Add(new Paragraph(" "));

                // ── Material breakdown chart ────────────────────────
                if (report.ByMaterial.Any())
                {
                    var chartBytes = GenerateMaterialChartImage(report);
                    var chartImage = iTextSharp.text.Image.GetInstance(chartBytes);
                    chartImage.ScaleToFit(500f, 250f);
                    chartImage.Alignment = Element.ALIGN_CENTER;
                    chartImage.SpacingAfter = 12f;
                    doc.Add(chartImage);
                }

                // ── Top residents table ────────────────────────────
                if (report.TopResidents.Any())
                {
                    doc.Add(new Paragraph("Top Residents This Month", headingFont) { SpacingAfter = 6f });

                    var resTable = new PdfPTable(5) { WidthPercentage = 100 };
                    resTable.SetWidths(new float[] { 2.5f, 1.5f, 1.5f, 1.5f, 1f });

                    foreach (var h in new[] { "Resident", "Weight (kg)", "CO₂ (kg)", "Points", "Submissions" })
                    {
                        resTable.AddCell(new PdfPCell(new Phrase(h, whiteFont))
                        { BackgroundColor = greenMid, Padding = 8f, Border = Rectangle.NO_BORDER });
                    }

                    alt = false;
                    foreach (var row in report.TopResidents)
                    {
                        var bg = alt ? greenLight : BaseColor.WHITE;
                        Action<string> addCell = (text) => {
                            resTable.AddCell(new PdfPCell(new Phrase(text, bodyFont))
                            { BackgroundColor = bg, Padding = 7f, Border = Rectangle.NO_BORDER });
                        };
                        addCell(row.FullName);
                        addCell(row.WeightKg.ToString("0.##"));
                        addCell(row.Co2Kg.ToString("0.####"));
                        addCell(row.PointsEarned.ToString("N0"));
                        addCell(row.Submissions.ToString());
                        alt = !alt;
                    }
                    doc.Add(resTable);
                }

                // ── Footer ─────────────────────────────────────────
                doc.Add(new Paragraph(" "));
                doc.Add(new Paragraph(
                    string.Format("Generated on {0}  |  EcoRewards SA  |  DUT Application Development  |  2026",
                        DateTime.Now.ToString("dd MMM yyyy HH:mm")), smallFont)
                { Alignment = Element.ALIGN_CENTER });

                doc.Close();
                return ms.ToArray();
            }
        }

        // ── Excel Generator (EPPlus) ───────────────────────────────────
        
        private byte[] GenerateExcel(CommunityReportViewModel report)
        {
            ExcelPackage.License.SetNonCommercialPersonal("EcoRewards SA");

            using (var package = new ExcelPackage())
            {
                // ── Sheet 1: Summary ───────────────────────────────
                var summary = package.Workbook.Worksheets.Add("Summary");

                var greenDark = System.Drawing.Color.FromArgb(26, 58, 42);
                var greenMid = System.Drawing.Color.FromArgb(45, 106, 79);
                var greenLight = System.Drawing.Color.FromArgb(237, 247, 241);
                var lime = System.Drawing.Color.FromArgb(183, 224, 74);

                // Title
                summary.Cells["A1"].Value = "EcoRewards SA — Community Recycling Report";
                summary.Cells["A1:F1"].Merge = true;
                summary.Cells["A1"].Style.Font.Bold = true;
                summary.Cells["A1"].Style.Font.Size = 16;
                summary.Cells["A1"].Style.Font.Color.SetColor(System.Drawing.Color.White);
                summary.Cells["A1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                summary.Cells["A1"].Style.Fill.BackgroundColor.SetColor(greenDark);
                summary.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                summary.Row(1).Height = 30;

                summary.Cells["A2"].Value = report.MonthLabel + " | eThekwini, KwaZulu-Natal";
                summary.Cells["A2:F2"].Merge = true;
                summary.Cells["A2"].Style.Font.Italic = true;
                summary.Cells["A2"].Style.Font.Color.SetColor(System.Drawing.Color.White);
                summary.Cells["A2"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                summary.Cells["A2"].Style.Fill.BackgroundColor.SetColor(greenMid);
                summary.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Summary stats
                var statsHeaders = new[] { "Total Weight (kg)", "CO₂ Saved (kg)", "Points Awarded", "Total Submissions", "Residents Recycled" };
                var statsValues = new object[] { report.TotalWeightKg, report.TotalCo2Kg, report.TotalPoints, report.TotalSubmissions, report.UniqueResidents };

                for (int i = 0; i < statsHeaders.Length; i++)
                {
                    summary.Cells[4, i + 1].Value = statsHeaders[i];
                    summary.Cells[4, i + 1].Style.Font.Bold = true;
                    summary.Cells[4, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    summary.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(greenLight);
                    summary.Cells[4, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    summary.Cells[5, i + 1].Value = statsValues[i];
                    summary.Cells[5, i + 1].Style.Font.Bold = true;
                    summary.Cells[5, i + 1].Style.Font.Size = 14;
                    summary.Cells[5, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                summary.Cells["A7"].Value = string.Format(
                    "CO₂ equivalent: {0} km driven by car, or {1} trees absorbing CO₂ for a year.",
                    report.CarKmEquivalent, report.TreesEquivalent);
                summary.Cells["A7:F7"].Merge = true;
                summary.Cells["A7"].Style.Font.Italic = true;

                summary.Cells[summary.Dimension.Address].AutoFitColumns();

                // ── Sheet 2: By Material ───────────────────────────
                var matSheet = package.Workbook.Worksheets.Add("By Material");
                var matHeaders = new[] { "Material", "Weight (kg)", "CO₂ Saved (kg)", "Points Awarded", "Submissions", "Colour" };

                for (int i = 0; i < matHeaders.Length; i++)
                {
                    matSheet.Cells[1, i + 1].Value = matHeaders[i];
                    matSheet.Cells[1, i + 1].Style.Font.Bold = true;
                    matSheet.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                    matSheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    matSheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(greenMid);
                }

                int row = 2;
                foreach (var m in report.ByMaterial)
                {
                    matSheet.Cells[row, 1].Value = m.MaterialName;
                    matSheet.Cells[row, 2].Value = m.TotalWeightKg;
                    matSheet.Cells[row, 3].Value = m.TotalCo2Kg;
                    matSheet.Cells[row, 4].Value = m.TotalPoints;
                    matSheet.Cells[row, 5].Value = m.SubmissionCount;

                    // Colour swatch cell — filled block matching this material's system colour
                    var swatchColor = string.IsNullOrWhiteSpace(m.ColourCode)
                        ? greenMid
                        : System.Drawing.ColorTranslator.FromHtml(m.ColourCode);
                    matSheet.Cells[row, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    matSheet.Cells[row, 6].Style.Fill.BackgroundColor.SetColor(swatchColor);

                    if (row % 2 == 0)
                    {
                        matSheet.Cells[row, 1, row, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        matSheet.Cells[row, 1, row, 5].Style.Fill.BackgroundColor.SetColor(greenLight);
                    }
                    row++;
                }
                matSheet.Cells[matSheet.Dimension.Address].AutoFitColumns();

                // ── Native Excel chart: weight by material ─────────
                if (report.ByMaterial.Any())
                {
                    var lastDataRow = row - 1;

                    var barChart = matSheet.Drawings.AddChart("WeightByMaterialChart", eChartType.ColumnClustered);
                    barChart.Title.Text = "Recycling Volume by Material Type (kg)";
                    barChart.SetPosition(1, 0, 8, 0);
                    barChart.SetSize(600, 350);

                    var series = barChart.Series.Add(
                        matSheet.Cells[2, 2, lastDataRow, 2],
                        matSheet.Cells[2, 1, lastDataRow, 1]);
                    series.Header = "Weight (kg)";

                    barChart.VaryColors = true;
                    barChart.Legend.Position = eLegendPosition.Bottom;
                }

                // ── Sheet 3: Top Residents ─────────────────────────
                var resSheet = package.Workbook.Worksheets.Add("Top Residents");
                var resHeaders = new[] { "Resident", "Weight (kg)", "CO₂ (kg)", "Points", "Submissions" };

                for (int i = 0; i < resHeaders.Length; i++)
                {
                    resSheet.Cells[1, i + 1].Value = resHeaders[i];
                    resSheet.Cells[1, i + 1].Style.Font.Bold = true;
                    resSheet.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                    resSheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    resSheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(greenMid);
                }

                row = 2;
                foreach (var r in report.TopResidents)
                {
                    resSheet.Cells[row, 1].Value = r.FullName;
                    resSheet.Cells[row, 2].Value = r.WeightKg;
                    resSheet.Cells[row, 3].Value = r.Co2Kg;
                    resSheet.Cells[row, 4].Value = r.PointsEarned;
                    resSheet.Cells[row, 5].Value = r.Submissions;

                    if (row % 2 == 0)
                    {
                        resSheet.Cells[row, 1, row, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        resSheet.Cells[row, 1, row, 5].Style.Fill.BackgroundColor.SetColor(greenLight);
                    }
                    row++;
                }
                resSheet.Cells[resSheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
        }
        private BaseColor HexToBaseColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return new BaseColor(107, 124, 110); // muted grey fallback
            hex = hex.TrimStart('#');
            if (hex.Length != 6) return new BaseColor(107, 124, 110);

            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return new BaseColor(r, g, b);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }

    // ── ViewModels used by Report views ──────────────────────────────
    public class CommunityReportViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthLabel { get; set; }
        public decimal TotalWeightKg { get; set; }
        public decimal TotalCo2Kg { get; set; }
        public int TotalPoints { get; set; }
        public int TotalSubmissions { get; set; }
        public int UniqueResidents { get; set; }
        public decimal CarKmEquivalent { get; set; }
        public decimal TreesEquivalent { get; set; }
        public List<ReportMaterialRow> ByMaterial { get; set; }
        public List<ReportResidentRow> TopResidents { get; set; }
    }

    public class ReportMaterialRow
    {
        public string MaterialName { get; set; }
        public string ColourCode { get; set; }
        public decimal TotalWeightKg { get; set; }
        public decimal TotalCo2Kg { get; set; }
        public int TotalPoints { get; set; }
        public int SubmissionCount { get; set; }
    }

    public class ReportResidentRow
    {
        public string FullName { get; set; }
        public decimal WeightKg { get; set; }
        public decimal Co2Kg { get; set; }
        public int PointsEarned { get; set; }
        public int Submissions { get; set; }
    }
}