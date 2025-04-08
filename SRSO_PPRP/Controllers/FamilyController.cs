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
using OfficeOpenXml;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Layout.Element;
using iText.Layout.Element;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Layout.Borders;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        [HttpGet]
        public IActionResult DownloadPSCReportByEnumerator()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }

            var model = new PSCReportFilterViewModel();

            // Populate the District dropdown
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT DISTRICT_NAME FROM NRSP.PPRP_SERVAY_CENSUS_DATA ORDER BY DISTRICT_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.Districts.Add(new SelectListItem
                                {
                                    Value = reader["DISTRICT_NAME"]?.ToString(),
                                    Text = reader["DISTRICT_NAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation($"Fetched {model.Districts.Count} districts for PSC report.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching districts for PSC report.");
                TempData["Message"] = "An error occurred while loading districts. Please try again.";
                return View(model); // Return the view with an empty Districts list
            }

            return View(model);
        }

        [HttpGet]
        public JsonResult GetPSCTehsils(string district)
        {
            var tehsils = new List<SelectListItem>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT TEHSIL_NAME FROM NRSP.PPRP_SERVAY_CENSUS_DATA WHERE UPPER(DISTRICT_NAME) = UPPER(:district) ORDER BY TEHSIL_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":district", OracleDbType.Varchar2).Value = district;
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tehsils.Add(new SelectListItem
                                {
                                    Value = reader["TEHSIL_NAME"]?.ToString(),
                                    Text = reader["TEHSIL_NAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation($"Fetched {tehsils.Count} tehsils for district: {district}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching tehsils for district: {district}");
                throw; // Re-throw the exception to let the client-side handle it
            }
            return Json(tehsils);
        }

        [HttpGet]
        public JsonResult GetPSCUCs(string tehsil)
        {
            var ucs = new List<SelectListItem>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT UC_NAME FROM NRSP.PPRP_SERVAY_CENSUS_DATA WHERE UPPER(TEHSIL_NAME) = UPPER(:tehsil) ORDER BY UC_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":tehsil", OracleDbType.Varchar2).Value = tehsil;
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ucs.Add(new SelectListItem
                                {
                                    Value = reader["UC_NAME"]?.ToString(),
                                    Text = reader["UC_NAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation($"Fetched {ucs.Count} union councils for tehsil: {tehsil}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching union councils for tehsil: {tehsil}");
                throw; // Re-throw the exception to let the client-side handle it
            }
            return Json(ucs);
        }

        [HttpGet]
        public JsonResult GetPSCRVs(string uc)
        {
            var rvs = new List<SelectListItem>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT REVEUNE_VILLAGE_NAME FROM NRSP.PPRP_SERVAY_CENSUS_DATA WHERE UPPER(UC_NAME) = UPPER(:uc) ORDER BY REVEUNE_VILLAGE_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":uc", OracleDbType.Varchar2).Value = uc;
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                rvs.Add(new SelectListItem
                                {
                                    Value = reader["REVEUNE_VILLAGE_NAME"]?.ToString(),
                                    Text = reader["REVEUNE_VILLAGE_NAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation($"Fetched {rvs.Count} revenue villages for UC: {uc}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching revenue villages for UC: {uc}");
                throw; // Re-throw the exception to let the client-side handle it
            }
            return Json(rvs);
        }

        [HttpGet]
        public JsonResult GetPSCVillages(string rv)
        {
            var villages = new List<SelectListItem>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT VILLAGE_NAME FROM NRSP.PPRP_SERVAY_CENSUS_DATA WHERE UPPER(REVEUNE_VILLAGE_NAME) = UPPER(:rv) ORDER BY VILLAGE_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":rv", OracleDbType.Varchar2).Value = rv;
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                villages.Add(new SelectListItem
                                {
                                    Value = reader["VILLAGE_NAME"]?.ToString(),
                                    Text = reader["VILLAGE_NAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation($"Fetched {villages.Count} villages for revenue village: {rv}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching villages for revenue village: {rv}");
                throw; // Re-throw the exception to let the client-side handle it
            }
            return Json(villages);
        }

        [HttpGet]
        public JsonResult GetPSCEnumerators(string village)
        {
            var enumerators = new List<SelectListItem>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                SELECT DISTINCT hh.ENUMERATOR_NAME 
                FROM NRSP.HH_MM_DATA hh
                INNER JOIN NRSP.PSC_SERVEY_SCORE pss ON hh.UUID = pss.UUID
                INNER JOIN NRSP.PPRP_SERVAY_CENSUS_DATA cv ON pss.VILLAGE_ID = cv.VILLAGENAME_ID
                WHERE UPPER(cv.VILLAGE_NAME) = UPPER(:village)
                ORDER BY hh.ENUMERATOR_NAME";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":village", OracleDbType.Varchar2).Value = village;
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                enumerators.Add(new SelectListItem
                                {
                                    Value = reader["ENUMERATOR_NAME"]?.ToString(),
                                    Text = reader["ENUMERATOR_NAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
                _logger.LogInformation($"Fetched {enumerators.Count} enumerators for village: {village}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching enumerators for village: {village}");
                throw;
            }
            return Json(enumerators);
        }


        // POST: Handle the form submission and generate the Excel report
        [HttpPost]
        public IActionResult DownloadPSCReportByEnumerator(PSCReportFilterViewModel model)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }

            // Validate the selected values
            if (string.IsNullOrEmpty(model.SelectedDistrict) ||
                string.IsNullOrEmpty(model.SelectedTehsil) ||
                string.IsNullOrEmpty(model.SelectedUC) ||
                string.IsNullOrEmpty(model.SelectedRV) ||
                string.IsNullOrEmpty(model.SelectedVillage) ||
                string.IsNullOrEmpty(model.SelectedEnumerator))
            {
                TempData["Message"] = "Please select all filters (District, Tehsil, UC, Revenue Village, Village, and Enumerator).";
                return RedirectToAction("DownloadPSCReportByEnumerator");
            }

            // Fetch all UUIDs for the selected enumerator in the selected village
            List<PSCServeyScore> pscScores = new List<PSCServeyScore>();
            Dictionary<string, List<HouseholdDataModel>> householdDataByUUID = new Dictionary<string, List<HouseholdDataModel>>();
            HashSet<string> processedUUIDs = new HashSet<string>(); // To track processed UUIDs
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    // Fetch PSC survey scores with DISTINCT UUID to avoid duplicates
                    string pscQuery = @"
                SELECT DISTINCT 
                    pss.ID,
                    pss.UUID,
                    cv.DISTRICT_NAME,
                    cv.TEHSIL_NAME,
                    cv.UC_NAME,
                    cv.REVEUNE_VILLAGE_NAME AS RV_NAME,
                    cv.VILLAGE_NAME,
                    pss.HOUSEHOLD_MEMBERS_COUNT_SCORE,
                    pss.ROOM_SCORE,
                    pss.TOILET_SCORE,
                    pss.TV_SCORE,
                    pss.REFRIGERATOR_SCORE,
                    pss.AIRCONDITIONER_SCORE,
                    pss.COOKING_SCORE,
                    pss.ENGINE_DRIVEN_SCORE,
                    pss.LIVESTOCK_SCORE,
                    pss.LAND_SCORE,
                    pss.HEAD_EDUCATION_SCORE,
                    pss.TOTAL_PSC_SCORE,
                    pss.CREATED_DATE,
                    pss.CELL_PHONE,
                    pss.ELECTRICITY,
                    pss.SOURCE_OF_DRINKING_WATER,
                    pss.LATITUDE,
                    pss.LONGITUDE,
                    pss.LOCATION_ADDRESS,
                    pss.BUFFALO,
                    pss.COW,
                    pss.GOAT,
                    pss.SHEEP,
                    pss.CAMEL,
                    pss.DONKEY,
                    pss.MULE_HORSE,
                    pss.VILLAGE_ID,
                    pss.SCHOOL_GOING_SCORE
                FROM 
                    NRSP.PSC_SERVEY_SCORE pss
                LEFT JOIN NRSP.PPRP_SERVAY_CENSUS_DATA cv 
                    ON pss.VILLAGE_ID = cv.VILLAGENAME_ID
                INNER JOIN NRSP.HH_MM_DATA hh 
                    ON pss.UUID = hh.UUID
                WHERE UPPER(cv.VILLAGE_NAME) = UPPER(:village) 
                    AND UPPER(hh.ENUMERATOR_NAME) = UPPER(:enumerator)";

                    using (OracleCommand cmd = new OracleCommand(pscQuery, conn))
                    {
                        cmd.Parameters.Add(":village", OracleDbType.Varchar2).Value = model.SelectedVillage;
                        cmd.Parameters.Add(":enumerator", OracleDbType.Varchar2).Value = model.SelectedEnumerator;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var uuid = reader["UUID"]?.ToString();
                                if (string.IsNullOrEmpty(uuid) || processedUUIDs.Contains(uuid))
                                {
                                    _logger.LogWarning($"Skipping duplicate or invalid UUID: {uuid}");
                                    continue; // Skip if UUID is null or already processed
                                }

                                processedUUIDs.Add(uuid);
                                pscScores.Add(new PSCServeyScore
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    UUID = uuid,
                                    DISTRICT_NAME = reader["DISTRICT_NAME"]?.ToString(),
                                    TEHSIL_NAME = reader["TEHSIL_NAME"]?.ToString(),
                                    UC_NAME = reader["UC_NAME"]?.ToString(),
                                    RV_NAME = reader["RV_NAME"]?.ToString(),
                                    VILLAGE_NAME = reader["VILLAGE_NAME"]?.ToString(),
                                    HOUSEHOLD_MEMBERS_COUNT_SCORE = reader["HOUSEHOLD_MEMBERS_COUNT_SCORE"]?.ToString(),
                                    ROOM_SCORE = reader["ROOM_SCORE"]?.ToString(),
                                    TOILET_SCORE = reader["TOILET_SCORE"]?.ToString(),
                                    TV_SCORE = reader["TV_SCORE"]?.ToString(),
                                    REFRIGERATOR_SCORE = reader["REFRIGERATOR_SCORE"]?.ToString(),
                                    AIRCONDITIONER_SCORE = reader["AIRCONDITIONER_SCORE"]?.ToString(),
                                    COOKING_SCORE = reader["COOKING_SCORE"]?.ToString(),
                                    ENGINE_DRIVEN_SCORE = reader["ENGINE_DRIVEN_SCORE"]?.ToString(),
                                    LIVESTOCK_SCORE = reader["LIVESTOCK_SCORE"]?.ToString(),
                                    LAND_SCORE = reader["LAND_SCORE"]?.ToString(),
                                    HEAD_EDUCATION_SCORE = reader["HEAD_EDUCATION_SCORE"]?.ToString(),
                                    TOTAL_PSC_SCORE = reader["TOTAL_PSC_SCORE"]?.ToString(),
                                    CREATED_DATE = Convert.ToDateTime(reader["CREATED_DATE"]),
                                    CELL_PHONE = reader["CELL_PHONE"]?.ToString(),
                                    ELECTRICITY = reader["ELECTRICITY"]?.ToString(),
                                    SOURCE_OF_DRINKING_WATER = reader["SOURCE_OF_DRINKING_WATER"]?.ToString(),
                                    LATITUDE = reader["LATITUDE"]?.ToString(),
                                    LONGITUDE = reader["LONGITUDE"]?.ToString(),
                                    LOCATION_ADDRESS = reader["LOCATION_ADDRESS"]?.ToString(),
                                    BUFFALO = reader["BUFFALO"]?.ToString(),
                                    COW = reader["COW"]?.ToString(),
                                    GOAT = reader["GOAT"]?.ToString(),
                                    SHEEP = reader["SHEEP"]?.ToString(),
                                    CAMEL = reader["CAMEL"]?.ToString(),
                                    DONKEY = reader["DONKEY"]?.ToString(),
                                    MULE_HORSE = reader["MULE_HORSE"]?.ToString(),
                                    VILLAGE_ID = reader["VILLAGE_ID"]?.ToString(),
                                    SCHOOL_GOING_SCORE = reader["SCHOOL_GOING_SCORE"]?.ToString()
                                });
                            }
                        }
                    }

                    if (!pscScores.Any())
                    {
                        TempData["Message"] = $"No PSC survey data found for enumerator '{model.SelectedEnumerator}' in village '{model.SelectedVillage}'.";
                        return RedirectToAction("DownloadPSCReportByEnumerator");
                    }

                    _logger.LogInformation($"Fetched {pscScores.Count} unique PSC survey scores for village: {model.SelectedVillage}, enumerator: {model.SelectedEnumerator}");

                    // Fetch household data for all UUIDs (removed DISTINCT to get all members)
                    string hhQuery = @"
                SELECT 
                    hh.ID,
                    hh.UUID,
                    hh.NAME,
                    hh.CONTACT_NO,
                    hh.GENDER,
                    hh.MARITAL_STATUS,
                    hh.ADDRESS,
                    hh.CNIC_STATUS_ID,
                    hh.RELATION,
                    hh.HEAD,
                    hh.EDUCATION,
                    hh.DISABILITY,
                    hh.OCCUPATION,
                    hh.CNIC,
                    hh.AGE_YEARS,
                    hh.STATUS,
                    hh.UPLOAD_STATUS,
                    hh.ENUMERATOR_NAME,
                    hh.ENUMERATOR_ID,
                    hh.CREATED_DATE,
                    hh.RELIGION
                FROM NRSP.HH_MM_DATA hh
                INNER JOIN NRSP.PSC_SERVEY_SCORE pss ON pss.UUID = hh.UUID
                INNER JOIN NRSP.PPRP_SERVAY_CENSUS_DATA cv ON pss.VILLAGE_ID = cv.VILLAGENAME_ID
                WHERE UPPER(cv.VILLAGE_NAME) = UPPER(:village) 
                    AND UPPER(hh.ENUMERATOR_NAME) = UPPER(:enumerator)";

                    using (OracleCommand cmd = new OracleCommand(hhQuery, conn))
                    {
                        cmd.Parameters.Add(":village", OracleDbType.Varchar2).Value = model.SelectedVillage;
                        cmd.Parameters.Add(":enumerator", OracleDbType.Varchar2).Value = model.SelectedEnumerator;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var uuid = reader["UUID"]?.ToString();
                                if (string.IsNullOrEmpty(uuid))
                                {
                                    _logger.LogWarning($"Skipping invalid UUID in household data: {uuid}");
                                    continue; // Skip if UUID is null
                                }

                                // Initialize the list for the UUID if it doesn't exist
                                if (!householdDataByUUID.ContainsKey(uuid))
                                {
                                    householdDataByUUID[uuid] = new List<HouseholdDataModel>();
                                }

                                // Add the household member to the list for this UUID
                                householdDataByUUID[uuid].Add(new HouseholdDataModel
                                {
                                    ID = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                                    UUID = uuid,
                                    Name = reader["NAME"]?.ToString(),
                                    ContactNo = reader["CONTACT_NO"]?.ToString(),
                                    Gender = reader["GENDER"]?.ToString(),
                                    MaritalStatus = reader["MARITAL_STATUS"]?.ToString(),
                                    Address = reader["ADDRESS"]?.ToString(),
                                    CNICStatusID = reader["CNIC_STATUS_ID"]?.ToString(),
                                    Relation = reader["RELATION"]?.ToString(),
                                    Head = reader["HEAD"]?.ToString(),
                                    Education = reader["EDUCATION"]?.ToString(),
                                    Disability = reader["DISABILITY"]?.ToString(),
                                    Occupation = reader["OCCUPATION"]?.ToString(),
                                    CNIC = reader["CNIC"]?.ToString(),
                                    AgeYears = reader["AGE_YEARS"]?.ToString(),
                                    Status = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0,
                                    UploadStatus = reader["UPLOAD_STATUS"] != DBNull.Value ? Convert.ToInt32(reader["UPLOAD_STATUS"]) : 0,
                                    EnumeratorName = reader["ENUMERATOR_NAME"]?.ToString(),
                                    EnumeratorID = reader["ENUMERATOR_ID"]?.ToString(),
                                    CreatedDate = reader["CREATED_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CREATED_DATE"]) : (DateTime?)null,
                                    Religion = reader["RELIGION"]?.ToString()
                                });
                            }
                        }
                    }

                    if (!householdDataByUUID.Any())
                    {
                        TempData["Message"] = $"No household data found for enumerator '{model.SelectedEnumerator}' in village '{model.SelectedVillage}'.";
                        return RedirectToAction("DownloadPSCReportByEnumerator");
                    }

                    _logger.LogInformation($"Fetched household data for {householdDataByUUID.Count} UUIDs with {householdDataByUUID.Values.Sum(list => list.Count)} total members for village: {model.SelectedVillage}, enumerator: {model.SelectedEnumerator}");

                    // Create the PDF document for all UUIDs
                    using (var memoryStream = new MemoryStream())
                    {
                        PdfWriter writer = new PdfWriter(memoryStream);
                        PdfDocument pdf = new PdfDocument(writer);
                        Document document = new Document(pdf, PageSize.A4);
                        document.SetMargins(15, 15, 15, 15);

                        iText.Kernel.Font.PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                        float smallFontSize = 7f;
                        float headerFontSize = 9f;

                        foreach (var pscScore in pscScores)
                        {
                            var uuid = pscScore.UUID;
                            if (!householdDataByUUID.ContainsKey(uuid))
                            {
                                _logger.LogWarning($"No household data found for UUID: {uuid}. Skipping...");
                                continue; // Skip if no household data for this UUID
                            }

                            var householdData = householdDataByUUID[uuid];
                            _logger.LogInformation($"Processing UUID: {uuid} with {householdData.Count} household members");

                            // Add a page break between reports (except for the first report)
                            if (pscScores.IndexOf(pscScore) > 0)
                            {
                                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                            }

                            document.Add(new Paragraph("UCP: Household PSC")
                                .SetFont(font)
                                .SetFontSize(headerFontSize)
                                .SetBold()
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginBottom(1));

                            document.Add(new Paragraph($"SALAHDIN_PSC Score: [{pscScore.TOTAL_PSC_SCORE}]")
                                .SetFont(font)
                                .SetFontSize(smallFontSize)
                                .SetBold()
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginBottom(1));

                            document.Add(new Paragraph($"Address: {householdData.First().Address}")
                                .SetFont(font)
                                .SetFontSize(smallFontSize)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginBottom(1));

                            document.Add(new Paragraph($"Unique Identifier: {uuid}")
                                .SetFont(font)
                                .SetFontSize(smallFontSize)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginBottom(1));

                            document.Add(new Paragraph($"Date: {pscScore.CREATED_DATE:dd, MMM yyyy}")
                                .SetFont(font)
                                .SetFontSize(smallFontSize)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginBottom(1));

                            document.Add(new Paragraph($"Enumerator: {householdData.First().EnumeratorName} ({householdData.First().EnumeratorID})")
                                .SetFont(font)
                                .SetFontSize(smallFontSize)
                                .SetTextAlignment(TextAlignment.LEFT)
                                .SetMarginBottom(3));

                            document.Add(new Paragraph("Location Details")
                                .SetFont(font)
                                .SetFontSize(headerFontSize)
                                .SetBold()
                                .SetMarginBottom(1));

                            Table locationTable = new Table(UnitValue.CreatePercentArray(new float[] { 20, 20, 20, 20, 20 })).UseAllAvailableWidth();
                            locationTable.SetFont(font).SetFontSize(smallFontSize);

                            locationTable.AddCell(new Cell().Add(new Paragraph("District").SetBold()));
                            locationTable.AddCell(new Cell().Add(new Paragraph("Tehsil").SetBold()));
                            locationTable.AddCell(new Cell().Add(new Paragraph("Union Council").SetBold()));
                            locationTable.AddCell(new Cell().Add(new Paragraph("Revenue Village").SetBold()));
                            locationTable.AddCell(new Cell().Add(new Paragraph("Settlement").SetBold()));

                            locationTable.AddCell(new Cell().Add(new Paragraph(pscScore.DISTRICT_NAME ?? "N/A")));
                            locationTable.AddCell(new Cell().Add(new Paragraph(pscScore.TEHSIL_NAME ?? "N/A")));
                            locationTable.AddCell(new Cell().Add(new Paragraph(pscScore.UC_NAME ?? "N/A")));
                            locationTable.AddCell(new Cell().Add(new Paragraph(pscScore.RV_NAME ?? "N/A")));
                            locationTable.AddCell(new Cell().Add(new Paragraph(pscScore.VILLAGE_NAME ?? "N/A")));

                            document.Add(locationTable);

                            document.Add(new Paragraph("\n").SetMarginBottom(1));

                            document.Add(new Paragraph("Household Assets and Facilities")
                                .SetFont(font)
                                .SetFontSize(headerFontSize)
                                .SetBold()
                                .SetMarginBottom(1));

                            // Map numeric values to their corresponding labels
                            string toiletLabel = pscScore.TOILET_SCORE switch
                            {
                                "1" => "Dry Raised or Dry Pit Latrine",
                                "2" => "Dry Raised",
                                "3" => "No Toilet",
                                _ => "N/A"
                            };

                            string refrigeratorLabel = pscScore.REFRIGERATOR_SCORE switch
                            {
                                "1" => "Yes",
                                "0" => "No",
                                _ => "N/A"
                            };

                            string freezerLabel = "No"; // Hardcoded as per the original code

                            string washingMachineLabel = "No"; // Hardcoded as per the original code

                            string geyserLabel = "No"; // Hardcoded as per the original code

                            string heaterLabel = "No"; // Hardcoded as per the original code

                            string airCoolerLabel = "No"; // Hardcoded as per the original code

                            string airConditionerLabel = pscScore.AIRCONDITIONER_SCORE switch
                            {
                                "1" => "Yes",
                                "0" => "No",
                                _ => "N/A"
                            };

                            string cookingStoveLabel = pscScore.COOKING_SCORE switch
                            {
                                "1" => "Yes",
                                "0" => "No",
                                _ => "N/A"
                            };

                            string cookingRangeLabel = "No"; // Hardcoded as per the original code

                            string microwaveOvenLabel = "No"; // Hardcoded as per the original code

                            string motorcycleLabel = pscScore.ENGINE_DRIVEN_SCORE switch
                            {
                                "1" => "Yes",
                                "0" => "No",
                                _ => "N/A"
                            };

                            string tractorLabel = "No"; // Hardcoded as per the original code

                            string carLabel = "No"; // Hardcoded as per the original code

                            string electricityLabel = pscScore.ELECTRICITY switch
                            {
                                "1" => "Yes",
                                "0" => "No",
                                _ => "N/A"
                            };

                            string sourceOfDrinkingWaterLabel = pscScore.SOURCE_OF_DRINKING_WATER switch
                            {
                                "1" => "Piped Water",
                                "2" => "Hand Pump",
                                "3" => "Public Tap",
                                "4" => "Private Borehole",
                                "5" => "Public Borehole",
                                "6" => "Protected Well",
                                "7" => "Unprotected Well",
                                "8" => "Protected Spring",
                                "9" => "Rainwater Collection",
                                "10" => "Bottled Water",
                                _ => "N/A"
                            };

                            string cellPhoneLabel = pscScore.CELL_PHONE switch
                            {
                                "1" => "Yes",
                                "0" => "No",
                                _ => "N/A"
                            };

                            string tvLabel = pscScore.TV_SCORE switch
                            {
                                "1" => "Yes",
                                "0" => "No",
                                _ => "N/A"
                            };

                            string agriLandLabel = pscScore.LAND_SCORE switch
                            {
                                "1" => "Yes",
                                "0" => "No",
                                _ => "N/A"
                            };

                            string agriAreaUnitLabel = "Square Feet"; // Hardcoded as per the original code
                            string agriAreaLabel = "1"; // Hardcoded as per the original code

                            var assets = new List<(string Label, string Value)>
                    {
                        ("Rooms #", pscScore.ROOM_SCORE ?? "N/A"),
                        ("Latrine Type", toiletLabel),
                        ("Refrigerator", refrigeratorLabel),
                        ("Freezer", freezerLabel),
                        ("Cell Phone", cellPhoneLabel),
                        ("Electricity", electricityLabel),
                        ("Source Drinking Water", sourceOfDrinkingWaterLabel),
                        ("Washing Machine", washingMachineLabel),
                        ("Air Conditioner", airConditionerLabel),
                        ("Air Cooler", airCoolerLabel),
                        ("Geyser", geyserLabel),
                        ("Heater", heaterLabel),
                        ("Cooking Stove", cookingStoveLabel),
                        ("Cooking Range", cookingRangeLabel),
                        ("Microwave Oven", microwaveOvenLabel),
                        ("TV", tvLabel),
                        ("Motorcycle / Scooter", motorcycleLabel),
                        ("Tractor", tractorLabel),
                        ("Car", carLabel),
                        ("Buffalo #", pscScore.BUFFALO ?? "N/A"),
                        ("Cow/Bull #", pscScore.COW ?? "N/A"),
                        ("Camel #", pscScore.CAMEL ?? "N/A"),
                        ("Goat #", pscScore.GOAT ?? "N/A"),
                        ("Sheep #", pscScore.SHEEP ?? "N/A"),
                        ("Donkey #", pscScore.DONKEY ?? "N/A"),
                        ("Mules/Horse #", pscScore.MULE_HORSE ?? "N/A"),
                        ("Agri Land", agriLandLabel),
                        ("Agri Land Unit", agriAreaUnitLabel),
                        ("Agri Land Area", agriAreaLabel),
                        ("GPS-Latitude", pscScore.LATITUDE ?? "N/A"),
                        ("GPS-Longitude", pscScore.LONGITUDE ?? "N/A"),
                        ("GPS-Altitude", "0.00000000000m"), // Hardcoded as per the original code
                        ("GPS-Accuracy", "4.00000000000m")  // Hardcoded as per the original code
                    };

                            Table assetsTable = new Table(UnitValue.CreatePercentArray(new float[] { 14.28f, 14.28f, 14.28f, 14.28f, 14.28f, 14.28f, 14.28f })).UseAllAvailableWidth();
                            assetsTable.SetFont(font).SetFontSize(smallFontSize);

                            int assetsPerRow = 7;
                            for (int i = 0; i < assets.Count; i += assetsPerRow)
                            {
                                for (int j = 0; j < assetsPerRow && (i + j) < assets.Count; j++)
                                {
                                    assetsTable.AddCell(new Cell().Add(new Paragraph(assets[i + j].Label).SetBold()));
                                }
                                for (int j = (i + assetsPerRow); j % assetsPerRow != 0; j++)
                                {
                                    assetsTable.AddCell(new Cell().Add(new Paragraph("")));
                                }
                                for (int j = 0; j < assetsPerRow && (i + j) < assets.Count; j++)
                                {
                                    assetsTable.AddCell(new Cell().Add(new Paragraph(assets[i + j].Value)));
                                }
                                for (int j = (i + assetsPerRow); j % assetsPerRow != 0; j++)
                                {
                                    assetsTable.AddCell(new Cell().Add(new Paragraph("")));
                                }
                            }
                            document.Add(assetsTable);

                            document.Add(new Paragraph("\n").SetMarginBottom(1));

                            document.Add(new Paragraph("Household Members")
                                .SetFont(font)
                                .SetFontSize(headerFontSize)
                                .SetBold()
                                .SetMarginBottom(1));

                            var memberHeaders = new List<string>
                    {
                        "#", "Name", "Sex", "Age", "Relationship", "Marital Status", "Fath./Hus. Name",
                        "B.Cert", "CNIC", "CNIC #", "School", "Education", "Disability", "Occupation", "Religion"
                    };

                            Table memberTable = new Table(UnitValue.CreatePercentArray(new float[] { 5, 10, 5, 5, 10, 10, 10, 5, 5, 10, 5, 10, 5, 10, 5 })).UseAllAvailableWidth();
                            memberTable.SetFont(font).SetFontSize(smallFontSize);

                            foreach (var header in memberHeaders)
                            {
                                memberTable.AddHeaderCell(new Cell().Add(new Paragraph(header).SetBold()));
                            }

                            int memberIndex = 1;
                            foreach (var member in householdData.OrderBy(m => m.Relation == "1" ? 0 : 1).ThenBy(m => m.Name)) // Sort by Relation (Household Head first), then by Name
                            {
                                // Map Gender
                                string genderLabel = member.Gender switch
                                {
                                    "1" => "Male",
                                    "2" => "Female",
                                    "3" => "Transgender",
                                    _ => "N/A"
                                };

                                // Map Marital Status
                                string maritalStatusLabel = member.MaritalStatus switch
                                {
                                    "1" => "Married",
                                    "2" => "Single",
                                    "3" => "Widowed",
                                    "4" => "Widower",
                                    "5" => "Divorced",
                                    _ => "N/A"
                                };

                                // Map Relation
                                string relationLabel = member.Relation switch
                                {
                                    "1" => "Household Head",
                                    "2" => "Self",
                                    "3" => "Husband",
                                    "4" => "Wife",
                                    "5" => "Son",
                                    "6" => "Daughter",
                                    "7" => "Father",
                                    "8" => "Mother",
                                    "9" => "Brother",
                                    "10" => "Sister",
                                    "11" => "Grandfather",
                                    "12" => "Grandmother",
                                    "13" => "Grandson",
                                    "14" => "Granddaughter",
                                    "15" => "Uncle",
                                    "16" => "Aunt",
                                    "17" => "Nephew",
                                    "18" => "Niece",
                                    "19" => "Cousin",
                                    "20" => "Brother-in-Law",
                                    "21" => "Sister-in-Law",
                                    "22" => "Father-in-Law",
                                    "23" => "Mother-in-Law",
                                    "24" => "Sister-in-Law",
                                    "25" => "Daughter-in-Law",
                                    "26" => "Other",
                                    "27" => "Not Related",
                                    _ => "N/A"
                                };

                                // Map Religion
                                string religionLabel = member.Religion switch
                                {
                                    "1" => "Islam",
                                    "2" => "Hinduism",
                                    "3" => "Christianity",
                                    "4" => "Parsi",
                                    "5" => "Sikh",
                                    "6" => "Ahmadi",
                                    _ => "N/A"
                                };

                                // Map Education
                                string educationLabel = member.Education switch
                                {
                                    "1" => "Prep",
                                    "2" => "Class 1",
                                    "3" => "Class 2",
                                    "4" => "Class 3",
                                    "5" => "Class 4",
                                    "6" => "Class 5",
                                    "7" => "Class 6",
                                    "8" => "Class 7",
                                    "9" => "Class 8",
                                    "10" => "Class 9",
                                    "11" => "Class 10",
                                    "12" => "Class 11",
                                    "13" => "Class 12",
                                    "14" => "Bachelor",
                                    "15" => "Master or Above",
                                    "16" => "Religious Education",
                                    "17" => "Not Literate",
                                    "18" => "Not Applicable",
                                    _ => "N/A"
                                };

                                // Map Disability
                                string disabilityLabel = member.Disability switch
                                {
                                    "1" => "No Disability",
                                    "2" => "Blind",
                                    "3" => "Deaf and Dumb",
                                    "4" => "Mentally Disordered",
                                    "5" => "Physically Disabled",
                                    _ => "N/A"
                                };

                                // Map Occupation
                                string occupationLabel = member.Occupation switch
                                {
                                    "1" => "Not Applicable",
                                    "2" => "Off-Farm Unskilled",
                                    "3" => "Housewife",
                                    "4" => "Government Job",
                                    "5" => "Student",
                                    "6" => "Idle (Not Working)",
                                    "7" => "Looking for Work",
                                    "8" => "Private Job",
                                    "9" => "Handicrafts/Cottage",
                                    "10" => "Farm Labor",
                                    "11" => "Own Farming",
                                    "12" => "Business",
                                    "13" => "Services",
                                    "14" => "Off-Farm Skilled",
                                    "15" => "Household Chores",
                                    "16" => "Don't Know",
                                    "17" => "Driver",
                                    "18" => "Food Processing",
                                    "19" => "Religious Scholar",
                                    "20" => "Retired",
                                    "21" => "Working Abroad",
                                    "22" => "Tailor",
                                    "23" => "Teacher",
                                    "24" => "Doctor",
                                    "25" => "Other (Specify)",
                                    _ => "N/A"
                                };

                                memberTable.AddCell(new Cell().Add(new Paragraph(memberIndex.ToString())));
                                memberTable.AddCell(new Cell().Add(new Paragraph(member.Name ?? "N/A")));
                                memberTable.AddCell(new Cell().Add(new Paragraph(genderLabel)));
                                memberTable.AddCell(new Cell().Add(new Paragraph(member.AgeYears ?? "N/A")));
                                memberTable.AddCell(new Cell().Add(new Paragraph(relationLabel)));
                                memberTable.AddCell(new Cell().Add(new Paragraph(maritalStatusLabel)));
                                memberTable.AddCell(new Cell().Add(new Paragraph("N/A"))); // Father/Husband Name
                                memberTable.AddCell(new Cell().Add(new Paragraph(member.CNICStatusID == "1" ? "YES, Available" : "YES, Not Available")));
                                memberTable.AddCell(new Cell().Add(new Paragraph(string.IsNullOrEmpty(member.CNIC) ? "N/A" : member.CNIC)));
                                memberTable.AddCell(new Cell().Add(new Paragraph(string.IsNullOrEmpty(member.CNIC) ? "N/A" : member.CNIC)));
                                memberTable.AddCell(new Cell().Add(new Paragraph("N/A"))); // School
                                memberTable.AddCell(new Cell().Add(new Paragraph(educationLabel)));
                                memberTable.AddCell(new Cell().Add(new Paragraph(disabilityLabel)));
                                memberTable.AddCell(new Cell().Add(new Paragraph(occupationLabel)));
                                memberTable.AddCell(new Cell().Add(new Paragraph(religionLabel)));

                                memberIndex++;
                            }
                            document.Add(memberTable);
                        }

                        document.Close();

                        var fileBytes = memoryStream.ToArray();
                        return File(fileBytes, "application/pdf", $"PSC_Report_{model.SelectedEnumerator}_{model.SelectedVillage}.pdf");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating PSC report for village: {model.SelectedVillage}, enumerator: {model.SelectedEnumerator}");
                TempData["Message"] = "An error occurred while generating the report. Please try again.";
                return RedirectToAction("DownloadPSCReportByEnumerator");
            }
        }
    }

        

}