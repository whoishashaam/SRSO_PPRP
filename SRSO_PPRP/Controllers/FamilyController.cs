using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Collections.Generic;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using SRSO_PPRP.Models;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Logging;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;
using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using Document = iText.Layout.Document;
using Paragraph = iText.Layout.Element.Paragraph;

namespace SRSO_PPRP.Controllers
{
    public class FamilyController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FamilyController> _logger;

        public FamilyController(IConfiguration configuration, ILogger<FamilyController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }

            try
            {
                _logger.LogInformation("Fetching districts for Index view.");
                ViewBag.Districts = GetDistricts();
                return View(new ReportViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Index view.");
                return View("Error"); // Return an error view or handle appropriately
            }
        }

        [HttpPost]
        public JsonResult GetTehsils(string districtId)
        {
            try
            {
                _logger.LogInformation("Fetching tehsils for districtId: {DistrictId}", districtId);
                var tehsils = GetTehsilsByDistrict(districtId);
                _logger.LogInformation("Fetched {TehsilCount} tehsils.", tehsils?.Count ?? 0);
                return Json(tehsils);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tehsils for districtId: {DistrictId}", districtId);
                return Json(new { success = false, message = "Error loading tehsils." });
            }
        }

        [HttpPost]
        public JsonResult GetUCs(string tehsilId)
        {
            try
            {
                _logger.LogInformation("Fetching UCs for tehsilId: {TehsilId}", tehsilId);
                var ucs = GetUCsByTehsil(tehsilId);
                _logger.LogInformation("Fetched {UCCount} UCs.", ucs?.Count ?? 0);
                return Json(ucs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching UCs for tehsilId: {TehsilId}", tehsilId);
                return Json(new { success = false, message = "Error loading UCs." });
            }
        }

        [HttpPost]
        public JsonResult GetRVs(string ucId)
        {
            try
            {
                _logger.LogInformation("Fetching RVs for ucId: {UcId}", ucId);
                var rvs = GetRVsByUC(ucId);
                _logger.LogInformation("Fetched {RVCount} RVs.", rvs?.Count ?? 0);
                return Json(rvs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching RVs for ucId: {UcId}", ucId);
                return Json(new { success = false, message = "Error loading RVs." });
            }
        }

        [HttpPost]
        public JsonResult GetVillages(string rvId)
        {
            try
            {
                _logger.LogInformation("Fetching villages for rvId: {RvId}", rvId);
                var villages = GetVillagesByRV(rvId);
                _logger.LogInformation("Fetched {VillageCount} villages.", villages?.Count ?? 0);
                return Json(villages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching villages for rvId: {RvId}", rvId);
                return Json(new { success = false, message = "Error loading villages." });
            }
        }

        [HttpPost]
        public IActionResult GenerateReport(string villageId)
        {
            try
            {
                _logger.LogInformation("Generating report for villageId: {VillageId}", villageId);
                if (string.IsNullOrEmpty(villageId))
                {
                    _logger.LogWarning("VillageId is null or empty.");
                    return Json(new { success = false, message = "Please select a village." });
                }

                var reportModel = GenerateReportData(villageId);
                byte[] pdfBytes = GeneratePdfReport(reportModel);
                _logger.LogInformation("Report generated successfully for villageId: {VillageId}", villageId);
                return File(pdfBytes, "application/pdf", "FamilyReport.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report for villageId: {VillageId}", villageId);
                return Json(new { success = false, message = "Error generating report." });
            }
        }

        private List<CensusData> GetDistricts()
        {
            var districts = new List<CensusData>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                _logger.LogInformation("Opening database connection to fetch districts.");
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT DISTRICT_NAME, DISTRICT_ID FROM NRSP.PPRP_SERVAY_CENSUS_DATA ORDER BY DISTRICT_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        _logger.LogInformation("Executing query: {Query}", query);
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                districts.Add(new CensusData
                                {
                                    DISTRICT_NAME = reader["DISTRICT_NAME"].ToString(),
                                    DISTRICT_ID = reader["DISTRICT_ID"].ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation("Fetched {DistrictCount} districts.", districts.Count);
                return districts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching districts.");
                throw; // Re-throw to be handled by the calling method
            }
        }

        private List<CensusData> GetTehsilsByDistrict(string districtId)
        {
            var tehsils = new List<CensusData>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                _logger.LogInformation("Opening database connection to fetch tehsils for districtId: {DistrictId}", districtId);
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT TEHSIL_NAME, TEHSIL_ID FROM NRSP.PPRP_SERVAY_CENSUS_DATA WHERE DISTRICT_ID = :districtId ORDER BY TEHSIL_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("districtId", districtId));
                        _logger.LogInformation("Executing query: {Query} with districtId: {DistrictId}", query, districtId);
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tehsils.Add(new CensusData
                                {
                                    TEHSIL_NAME = reader["TEHSIL_NAME"].ToString(),
                                    TEHSIL_ID = reader["TEHSIL_ID"].ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation("Fetched {TehsilCount} tehsils for districtId: {DistrictId}", tehsils.Count, districtId);
                return tehsils;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tehsils for districtId: {DistrictId}", districtId);
                throw; // Re-throw to be handled by the calling method
            }
        }

        private List<CensusData> GetUCsByTehsil(string tehsilId)
        {
            var ucs = new List<CensusData>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                _logger.LogInformation("Opening database connection to fetch UCs for tehsilId: {TehsilId}", tehsilId);
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT UC_NAME, UC_ID FROM NRSP.PPRP_SERVAY_CENSUS_DATA WHERE TEHSIL_ID = :tehsilId ORDER BY UC_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("tehsilId", tehsilId));
                        _logger.LogInformation("Executing query: {Query} with tehsilId: {TehsilId}", query, tehsilId);
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ucs.Add(new CensusData
                                {
                                    UC_NAME = reader["UC_NAME"].ToString(),
                                    UC_ID = reader["UC_ID"].ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation("Fetched {UCCount} UCs for tehsilId: {TehsilId}", ucs.Count, tehsilId);
                return ucs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching UCs for tehsilId: {TehsilId}", tehsilId);
                throw; // Re-throw to be handled by the calling method
            }
        }

        private List<CensusData> GetRVsByUC(string ucId)
        {
            var rvs = new List<CensusData>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                _logger.LogInformation("Opening database connection to fetch RVs for ucId: {UcId}", ucId);
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT REVEUNE_VILLAGE_NAME, REVEUNEVILLAGE_ID FROM NRSP.PPRP_SERVAY_CENSUS_DATA WHERE UC_ID = :ucId ORDER BY REVEUNE_VILLAGE_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("ucId", ucId));
                        _logger.LogInformation("Executing query: {Query} with ucId: {UcId}", query, ucId);
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rvs.Add(new CensusData
                                {
                                    REVEUNE_VILLAGE_NAME = reader["REVEUNE_VILLAGE_NAME"].ToString(),
                                    REVEUNEVILLAGE_ID = reader["REVEUNEVILLAGE_ID"].ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation("Fetched {RVCount} RVs for ucId: {UcId}", rvs.Count, ucId);
                return rvs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching RVs for ucId: {UcId}", ucId);
                throw; // Re-throw to be handled by the calling method
            }
        }

        private List<CensusData> GetVillagesByRV(string rvId)
        {
            var villages = new List<CensusData>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                _logger.LogInformation("Opening database connection to fetch villages for rvId: {RvId}", rvId);
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT VILLAGE_NAME, VILLAGENAME_ID FROM NRSP.PPRP_SERVAY_CENSUS_DATA WHERE REVEUNEVILLAGE_ID = :rvId ORDER BY VILLAGE_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("rvId", rvId));
                        _logger.LogInformation("Executing query: {Query} with rvId: {RvId}", query, rvId);
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                villages.Add(new CensusData
                                {
                                    VILLAGE_NAME = reader["VILLAGE_NAME"].ToString(),
                                    VILLAGENAME_ID = reader["VILLAGENAME_ID"].ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation("Fetched {VillageCount} villages for rvId: {RvId}", villages.Count, rvId);
                return villages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching villages for rvId: {RvId}", rvId);
                throw; // Re-throw to be handled by the calling method
            }
        }

        private ReportViewModel GenerateReportData(string villageId)
        {
            var reportModel = new ReportViewModel { Households = new List<HouseholdReport>() };
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                _logger.LogInformation("Generating report data for villageId: {VillageId}", villageId);
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    // Fetch village details
                    string query = @"
                        SELECT DISTRICT_NAME, TEHSIL_NAME, UC_NAME, REVEUNE_VILLAGE_NAME, VILLAGE_NAME, VILLAGENAME_ID
                        FROM NRSP.PPRP_SERVAY_CENSUS_DATA
                        WHERE VILLAGENAME_ID = :villageId";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("villageId", villageId));
                        _logger.LogInformation("Executing query: {Query} with villageId: {VillageId}", query, villageId);
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                reportModel.DistrictName = reader["DISTRICT_NAME"].ToString();
                                reportModel.TehsilName = reader["TEHSIL_NAME"].ToString();
                                reportModel.UcName = reader["UC_NAME"].ToString();
                                reportModel.RvName = reader["REVEUNE_VILLAGE_NAME"].ToString();
                                reportModel.VillageName = reader["VILLAGE_NAME"].ToString();
                                reportModel.VillageId = reader["VILLAGENAME_ID"].ToString();
                                _logger.LogInformation("Fetched village details: District={District}, Tehsil={Tehsil}, UC={UC}, RV={RV}, Village={Village}",
                                    reportModel.DistrictName, reportModel.TehsilName, reportModel.UcName, reportModel.RvName, reportModel.VillageName);
                            }
                            else
                            {
                                _logger.LogWarning("No village details found for villageId: {VillageId}", villageId);
                            }
                        }
                    }

                    // Fetch UUIDs from SurveyScore
                    string uuidQuery = "SELECT DISTINCT UUID, TOTAL_PSC_SCORE FROM NRSP.PSC_SERVEY_SCORE WHERE VILLAGE_ID = :villageId";
                    using (OracleCommand cmd = new OracleCommand(uuidQuery, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("villageId", villageId));
                        _logger.LogInformation("Executing query: {Query} with villageId: {VillageId}", uuidQuery, villageId);
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var household = new HouseholdReport
                                {
                                    UUID = reader["UUID"].ToString(),
                                    Address = "", // Will be set from HouseholdMember
                                    Members = new List<HouseholdMemberReport>()
                                };

                                // Fetch household members
                                string memberQuery = @"
                                    SELECT NAME, CONTACT_NO, GENDER, HEAD, AGE_YEARS, ADDRESS
                                    FROM HH_MM_DATA
                                    WHERE UUID = :uuid";
                                using (OracleCommand memberCmd = new OracleCommand(memberQuery, conn))
                                {
                                    memberCmd.Parameters.Add(new OracleParameter("uuid", household.UUID));
                                    _logger.LogInformation("Executing member query for UUID: {Uuid}", household.UUID);
                                    using (OracleDataReader memberReader = memberCmd.ExecuteReader())
                                    {
                                        while (memberReader.Read())
                                        {
                                            var member = new HouseholdMemberReport
                                            {
                                                Name = memberReader["NAME"].ToString(),
                                                Gender = memberReader["GENDER"].ToString() == "1" ? "M" : "F",
                                                Age = memberReader["AGE_YEARS"].ToString(),
                                                PscScore = reader["TOTAL_PSC_SCORE"].ToString(),
                                                ContactNo = memberReader["CONTACT_NO"].ToString(),
                                                IsHead = memberReader["HEAD"].ToString() == "1"
                                            };
                                            household.Members.Add(member);
                                            if (member.IsHead) household.Address = memberReader["ADDRESS"].ToString();
                                        }
                                    }
                                }
                                reportModel.Households.Add(household);
                            }
                        }
                    }
                }
                _logger.LogInformation("Generated report data with {HouseholdCount} households for villageId: {VillageId}", reportModel.Households.Count, villageId);
                return reportModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report data for villageId: {VillageId}", villageId);
                throw; // Re-throw to be handled by the calling method
            }
        }

        private byte[] GeneratePdfReport(ReportViewModel model)
        {
            try
            {
                _logger.LogInformation("Starting PDF generation for village: {VillageName}", model?.VillageName ?? "Unknown");
                if (model == null)
                {
                    _logger.LogWarning("Report model is null.");
                    throw new ArgumentNullException(nameof(model), "Report model cannot be null.");
                }

                using (var memoryStream = new MemoryStream())
                {
                    _logger.LogInformation("Creating PdfWriter and PdfDocument.");
                    PdfWriter writer = new PdfWriter(memoryStream);
                    PdfDocument pdf = new PdfDocument(writer);
                    Document document = new Document(pdf);

                    // Header
                    var title = new Paragraph($"UCBPR: List of households within {model.VillageName ?? "Unknown Village"}")
                        .SetBold()
                        .SetFontSize(16);
                    _logger.LogInformation("Adding title to PDF: {Title}", title.GetTextRenderingMode());
                    document.Add(title);

                    document.Add(new Paragraph($"District: {model.DistrictName ?? "N/A"}"));
                    document.Add(new Paragraph($"Tehsil: {model.TehsilName ?? "N/A"}"));
                    document.Add(new Paragraph($"UC: {model.UcName ?? "N/A"}"));
                    document.Add(new Paragraph($"RV: {model.RvName ?? "N/A"}"));
                    document.Add(new Paragraph($"Village: {model.VillageName ?? "N/A"}"));
                    document.Add(new Paragraph("\n"));

                    // Households
                    int householdIndex = 1;
                    foreach (var household in model.Households ?? new List<HouseholdReport>())
                    {
                        _logger.LogInformation("Adding household {Index} to PDF", householdIndex);
                      //  document.Add(new Paragraph($"Thahim mohla {householdIndex++}"));
                        var head = household.Members?.FirstOrDefault(m => m.IsHead);
                        if (head != null)
                        {
                            document.Add(new Paragraph($"Head: {head.Name ?? "N/A"} / Contact: {head.ContactNo ?? "N/A"}"));
                        }
                        document.Add(new Paragraph($"Address: {household.Address ?? "N/A"}"));
                        document.Add(new Paragraph("Members:"));

                        var table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1, 1 }));
                        table.AddHeaderCell(new Cell().Add(new Paragraph("Name")));
                        table.AddHeaderCell(new Cell().Add(new Paragraph("Gender")));
                        table.AddHeaderCell(new Cell().Add(new Paragraph("Age")));
                        table.AddHeaderCell(new Cell().Add(new Paragraph("PSC")));

                        foreach (var member in household.Members ?? new List<HouseholdMemberReport>())
                        {
                            table.AddCell(new Cell().Add(new Paragraph(member.Name ?? "N/A")));
                            table.AddCell(new Cell().Add(new Paragraph(member.Gender ?? "N/A")));
                            table.AddCell(new Cell().Add(new Paragraph(member.Age ?? "N/A")));
                            table.AddCell(new Cell().Add(new Paragraph(member.PscScore ?? "N/A")));
                        }
                        document.Add(table);
                        document.Add(new Paragraph("\n"));
                    }

                    _logger.LogInformation("Closing PDF document.");
                    document.Close();
                    var pdfBytes = memoryStream.ToArray();
                    _logger.LogInformation("PDF generation completed. Size: {Size} bytes", pdfBytes.Length);
                    return pdfBytes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for village: {VillageName}", model?.VillageName ?? "Unknown");
                throw; // Re-throw to be handled by the calling method
            }
        }
    }
}