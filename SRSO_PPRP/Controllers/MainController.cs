using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Oracle.ManagedDataAccess.Client;
using SRSO_PPRP.Models;
using System;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace SRSO_PPRP.Controllers
{
    public class MainController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MainController> _logger; // Add ILogger
        public MainController(IConfiguration configuration, ILogger<MainController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Use appropriate license

        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Message = "Username and password are required.";
                return View();
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    Console.WriteLine("Database Connection Successful!");

                    string query = "SELECT USER_NAME FROM NRSP.PHSIP_ASSESSMENT WHERE USER_NAME = :username AND PWD = :password";

                    using (var cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("username", username));
                        cmd.Parameters.Add(new OracleParameter("password", password));

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) // More efficient check
                            {
                                HttpContext.Session.SetString("User", reader["USER_NAME"].ToString());
                                return RedirectToAction("Dashboard", "Main");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database Connection Failed: " + ex.Message);
                ViewBag.Message = "Database Connection Failed. Please contact the administrator.";
            }

            ViewBag.Message = "Invalid Username or Password";
            return View();
        }

        public IActionResult Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }

            ViewBag.Username = HttpContext.Session.GetString("User");
            return View();
        }

        [HttpPost]
        public IActionResult UpdateUser(string UserID, string UserName, string Password, string DistrictID)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (OracleConnection con = new OracleConnection(connectionString))
            {
                con.Open();

                // Get max ID and increment
                int newId = 1;
                using (OracleCommand cmdMax = new OracleCommand("SELECT NVL(MAX(TO_NUMBER(ID)), 0) + 1 FROM NRSP.PHSIP_ASSESSMENT", con))
                {
                    newId = Convert.ToInt32(cmdMax.ExecuteScalar());
                }

                // Insert new user with manually entered USER_ID
                using (OracleCommand cmdInsert = new OracleCommand("INSERT INTO NRSP.PHSIP_ASSESSMENT (ID, USER_ID, USER_NAME, PWD, DISTRICT_ID) VALUES (:id, :userid, :uname, :pwd, :district)", con))
                {
                    cmdInsert.Parameters.Add(":id", OracleDbType.Varchar2).Value = newId.ToString(); // Auto-incremented ID
                    cmdInsert.Parameters.Add(":userid", OracleDbType.Varchar2).Value = UserID; // Manually entered USER_ID
                    cmdInsert.Parameters.Add(":uname", OracleDbType.Varchar2).Value = UserName;
                    cmdInsert.Parameters.Add(":pwd", OracleDbType.Varchar2).Value = Password;
                    cmdInsert.Parameters.Add(":district", OracleDbType.Varchar2).Value = DistrictID;

                    cmdInsert.ExecuteNonQuery();
                }
            }

            TempData["Message"] = "User created successfully!";
            return RedirectToAction("Dashboard");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Main");
        }





        public ActionResult UsersUpload()
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }
            List<UserModel> usersList = new List<UserModel>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT ID, PWD, USER_NAME, USER_ID, DISTRICT_ID FROM NRSP.PHSIP_ASSESSMENT";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                usersList.Add(new UserModel
                                {
                                    ID = Convert.ToInt32(reader["ID"].ToString()),
                                    Password = reader["PWD"].ToString(),
                                    UserName = reader["USER_NAME"].ToString(),
                                    UserID = reader["USER_ID"].ToString(),
                                    DistrictID = reader["DISTRICT_ID"].ToString()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error retrieving data: " + ex.Message;
                }
            }

            return View(usersList);

        }


        // GET: Edit User
        public ActionResult EditUser(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }
            UserModel user = new UserModel();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT ID, PWD, USER_NAME, USER_ID, DISTRICT_ID FROM NRSP.PHSIP_ASSESSMENT WHERE ID = :id";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":id", OracleDbType.Int32).Value = id;

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user.ID = Convert.ToInt32(reader["ID"]);
                                user.Password = reader["PWD"].ToString();
                                user.UserName = reader["USER_NAME"].ToString();
                                user.UserID = reader["USER_ID"].ToString();
                                user.DistrictID = reader["DISTRICT_ID"].ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error retrieving data: " + ex.Message;
                }
            }

            return View(user);
        }

        // POST: Update User
        [HttpPost]
        public ActionResult EditUser(UserModel user)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "UPDATE NRSP.PHSIP_ASSESSMENT SET PWD = :password, USER_NAME = :userName, USER_ID = :userId, DISTRICT_ID = :districtId WHERE ID = :id";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":password", OracleDbType.Varchar2).Value = user.Password;
                        cmd.Parameters.Add(":userName", OracleDbType.Varchar2).Value = user.UserName;
                        cmd.Parameters.Add(":userId", OracleDbType.Varchar2).Value = user.UserID;
                        cmd.Parameters.Add(":districtId", OracleDbType.Varchar2).Value = user.DistrictID;
                        cmd.Parameters.Add(":id", OracleDbType.Int32).Value = user.ID;

                        cmd.ExecuteNonQuery();
                    }

                    TempData["Message"] = "User updated successfully!";
                    return RedirectToAction("UsersUpload");
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error updating user: " + ex.Message;
                    return View(user);
                }
            }
        }

        // DELETE: User
        public ActionResult DeleteUser(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "DELETE FROM NRSP.PHSIP_ASSESSMENT WHERE ID = :id";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(":id", OracleDbType.Int32).Value = id;
                        cmd.ExecuteNonQuery();
                    }

                    TempData["Message"] = "User deleted successfully!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Error deleting user: " + ex.Message;
                }
            }

            return RedirectToAction("UsersUpload");
        }


        [HttpPost]
        public IActionResult DataUpload(IFormFile file)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }
            if (file == null || file.Length == 0)
            {
                TempData["Message"] = "Please select a valid CSV or Excel file.";
                return RedirectToAction("Dashboard");
            }

            // File upload path
            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "holder");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string filePath = Path.Combine(uploadPath, file.FileName);

            try
            {
                // Save file temporarily
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                string fileExtension = Path.GetExtension(file.FileName).ToLower();

                // Process based on file type
                //if (fileExtension == ".csv")
                //{
                //    UploadCsvToDatabase(filePath);
                //}
                if (fileExtension == ".xlsx")
                {
                    UploadExcelToDatabase(filePath);
                }
                else
                {
                    TempData["Message"] = "Invalid file format. Please upload a CSV or XLSX file.";
                    return RedirectToAction("Dashboard");
                }

                TempData["Message"] = "File uploaded and data inserted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error uploading file: " + ex.Message;
            }

            return RedirectToAction("Dashboard");
        }

       

        private void UploadExcelToDatabase(string filePath)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    string connectionString = _configuration.GetConnectionString("DefaultConnection");

                    using (var conn = new OracleConnection(connectionString))
                    {
                        conn.Open();

                        // Clear existing data
                        using (var cmd = new OracleCommand("DELETE FROM NRSP.PPRP_SERVAY_CENSUS_DATA", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        bool isHeader = true;
                        int rowNumber = 0; // Track row number for error logging

                        while (reader.Read())
                        {
                            rowNumber++; // Increment row number

                            if (isHeader)
                            {
                                isHeader = false;
                                continue; // Skip header row
                            }

                            string[] values = new string[11];

                            for (int i = 0; i < 11; i++)
                            {
                                values[i] = reader.GetValue(i)?.ToString() ?? "";
                            }

                            try
                            {
                                InsertDataIntoDatabase(conn, values);
                            }
                            catch (OracleException ex)
                            {
                                // Log the error with row number and values
                                Console.WriteLine($"Error inserting row {rowNumber}: {string.Join(", ", values)}. Error: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                // Log any other exceptions
                                Console.WriteLine($"Unexpected error inserting row {rowNumber}: {string.Join(", ", values)}. Error: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // Delete temp file
            System.IO.File.Delete(filePath);
        }

        private void InsertDataIntoDatabase(OracleConnection conn, string[] values)
        {
            using (var cmd = new OracleCommand("INSERT INTO NRSP.PPRP_SERVAY_CENSUS_DATA (DISTRICT_NAME, DISTRICT_ID, TEHSIL_NAME, TEHSIL_ID, UC_NAME, UC_ID, REVEUNE_VILLAGE_NAME, REVEUNEVILLAGE_ID, VILLAGE_NAME, VILLAGENAME_ID, ESTIMATED_HHS) VALUES (:districtName, :districtID, :tehsilName, :tehsilID, :ucName, :ucID, :revVillageName, :revVillageID, :villageName, :villageID, :estimatedHHS)", conn))
            {
                // Trim and handle null/empty values
                cmd.Parameters.Add(":districtName", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[0]) ? DBNull.Value : (object)values[0].Trim();
                cmd.Parameters.Add(":districtID", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[1]) ? DBNull.Value : (object)values[1].Trim();
                cmd.Parameters.Add(":tehsilName", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[2]) ? DBNull.Value : (object)values[2].Trim();
                cmd.Parameters.Add(":tehsilID", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[3]) ? DBNull.Value : (object)values[3].Trim();
                cmd.Parameters.Add(":ucName", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[4]) ? DBNull.Value : (object)values[4].Trim();
                cmd.Parameters.Add(":ucID", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[5]) ? DBNull.Value : (object)values[5].Trim();
                cmd.Parameters.Add(":revVillageName", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[6]) ? DBNull.Value : (object)values[6].Trim();
                cmd.Parameters.Add(":revVillageID", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[7]) ? DBNull.Value : (object)values[7].Trim();
                cmd.Parameters.Add(":villageName", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[8]) ? DBNull.Value : (object)values[8].Trim();

                // Ensure VILLAGENAME_ID is not null (replace null/empty with a default value, e.g., "0")
                cmd.Parameters.Add(":villageID", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[9]) ? "0" : values[9].Trim();

                // Ensure ESTIMATED_HHS is not null (replace null/empty with a default value, e.g., "0")
                cmd.Parameters.Add(":estimatedHHS", OracleDbType.Varchar2).Value = string.IsNullOrEmpty(values[10]) ? "0" : values[10].Trim();

                cmd.ExecuteNonQuery();
            }
        }



        [HttpPost]
        public IActionResult UserDataUpload(IFormFile file)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }
            if (file == null || file.Length == 0)
            {
                TempData["Message"] = "Please select a valid CSV or Excel file.";
                return RedirectToAction("Dashboard");
            }

            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string filePath = Path.Combine(uploadPath, file.FileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                string fileExtension = Path.GetExtension(file.FileName).ToLower();

               
                 if (fileExtension == ".xlsx")
                {
                    ProcessExcelFile(filePath);
                }
                else
                {
                    TempData["Message"] = "Invalid file format. Please upload a CSV or XLSX file.";
                    return RedirectToAction("Dashboard");
                }

                TempData["Message"] = "File uploaded and data inserted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Error uploading file: " + ex.Message;
            }

            return RedirectToAction("Dashboard");
        }

       
        private void ProcessExcelFile(string filePath)
        {

            
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    string connectionString = _configuration.GetConnectionString("DefaultConnection");

                    using (var conn = new OracleConnection(connectionString))
                    {
                        conn.Open();

                        // Delete existing records from the USER_ACCOUNTS table before inserting new ones
                        using (var deleteCmd = new OracleCommand("DELETE FROM NRSP.PHSIP_ASSESSMENT", conn))
                        {
                            deleteCmd.ExecuteNonQuery();
                            Console.WriteLine("Deleted existing records from USER_ACCOUNTS.");
                        }

                        // Read all rows into a list
                        var rows = new List<string[]>();
                        bool isHeader = true;

                        while (reader.Read())
                        {
                            if (isHeader)
                            {
                                isHeader = false;
                                continue;
                            }

                            string[] values = new string[5];

                            for (int i = 0; i < 5; i++)
                            {
                                values[i] = reader.GetValue(i)?.ToString() ?? "";
                            }

                            rows.Add(values);
                        }

                        // Sort the rows by ID in ascending order
                        var sortedRows = rows.OrderBy(row => Convert.ToInt64(row[0], CultureInfo.InvariantCulture)).ToList();

                        // Insert all rows into the database
                        foreach (var values in sortedRows)
                        {
                            // Insert the row into the database, no duplicate checking
                            InsertUserAccount(conn, values);
                        }
                    }
                }
            }

            System.IO.File.Delete(filePath);
        }



        private void InsertUserAccount(OracleConnection conn, string[] values)
        {
            using (var insertCmd = new OracleCommand(@"
BEGIN
    INSERT INTO NRSP.PHSIP_ASSESSMENT (ID, PWD, USER_NAME, USER_ID, DISTRICT_ID) 
    VALUES (:id, :password, :username, :userID, :districtID);
EXCEPTION
    WHEN DUP_VAL_ON_INDEX THEN
        NULL; -- Ignore duplicate record error
END;", conn))
            {
                insertCmd.Parameters.Add(":id", OracleDbType.Varchar2).Value = values[0];           // ID
                insertCmd.Parameters.Add(":password", OracleDbType.Varchar2).Value = values[1];     // PWD
                insertCmd.Parameters.Add(":username", OracleDbType.Varchar2).Value = values[2];     // USER_NAME
                insertCmd.Parameters.Add(":userID", OracleDbType.Varchar2).Value = values[3];       // USER_ID
                insertCmd.Parameters.Add(":districtID", OracleDbType.Varchar2).Value = values[4];    // DISTRICT_ID

                try
                {
                    insertCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    // Log any other exceptions if needed
                    Console.WriteLine($"Error inserting ID {values[0]}: {ex.Message}");
                }
            }
        }





        public IActionResult HouseholdData()
        {
            // Fetch all household data with village details
            var householdList = GetHouseholdData();

            // Create an Excel package
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Household Data");
                int sheetIndex = 1;
                int row = 2; // Start from row 2 (after headers)
                const int maxRowsPerSheet = 1048576; // Excel row limit

                // Define headers
                var headers = new List<string>
            {
                "ID", "UUID", "House Head", "Name", "CNIC", "Contact No", "Gender", "Marital Status",
                "Relation", "Head", "Age (Years)", "Education", "Occupation", "Address", "Religion",
                "Status", "Upload Status", "Enumerator Name", "Enumerator ID", "Created Date",
                "District", "Tehsil", "UC", "RV", "Village", "PSC Score"
            };

                // Add headers to the first worksheet
                for (int col = 0; col < headers.Count; col++)
                {
                    worksheet.Cells[1, col + 1].Value = headers[col];
                    worksheet.Cells[1, col + 1].Style.Font.Bold = true; // Bold headers
                }

                // Populate data
                foreach (var household in householdList ?? new List<HouseholdDataModel>())
                {
                    // Skip null entries
                    if (household == null)
                    {
                        _logger.LogWarning("Skipping null household entry.");
                        continue;
                    }

                    // Check if we exceed the row limit for the current sheet
                    if (row > maxRowsPerSheet)
                    {
                        sheetIndex++;
                        worksheet = package.Workbook.Worksheets.Add($"Household Data {sheetIndex}");
                        row = 2; // Reset row for the new sheet

                        // Add headers to the new worksheet
                        for (int col = 0; col < headers.Count; col++)
                        {
                            worksheet.Cells[1, col + 1].Value = headers[col];
                            worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                        }
                    }

                    try
                    {
                        worksheet.Cells[row, 1].Value = household.ID;
                        worksheet.Cells[row, 2].Value = household.UUID;
                        worksheet.Cells[row, 3].Value = household.HouseHead; // House Head column
                        worksheet.Cells[row, 4].Value = household.Name;

                        // Bold the name if this member is the house head
                        if (household.Head == "1")
                        {
                            worksheet.Cells[row, 4].Style.Font.Bold = true;
                        }

                        worksheet.Cells[row, 5].Value = household.CNIC;
                        worksheet.Cells[row, 6].Value = household.ContactNo;
                        worksheet.Cells[row, 7].Value = household.Gender;
                        worksheet.Cells[row, 8].Value = household.MaritalStatus;
                        worksheet.Cells[row, 9].Value = household.Relation;
                        worksheet.Cells[row, 10].Value = household.Head;
                        worksheet.Cells[row, 11].Value = household.AgeYears;
                        worksheet.Cells[row, 12].Value = household.Education;
                        worksheet.Cells[row, 13].Value = household.Occupation;
                        worksheet.Cells[row, 14].Value = household.Address;
                        worksheet.Cells[row, 15].Value = household.Religion;
                        worksheet.Cells[row, 16].Value = household.Status;
                        worksheet.Cells[row, 17].Value = household.UploadStatus;
                        worksheet.Cells[row, 18].Value = household.EnumeratorName;
                        worksheet.Cells[row, 19].Value = household.EnumeratorID;
                        worksheet.Cells[row, 20].Value = household.CreatedDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 21].Value = household.DistrictName;
                        worksheet.Cells[row, 22].Value = household.TehsilName;
                        worksheet.Cells[row, 23].Value = household.UcName;
                        worksheet.Cells[row, 24].Value = household.RvName;
                        worksheet.Cells[row, 25].Value = household.VillageName;
                        worksheet.Cells[row, 26].Value = household.PscScore;

                        row++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error writing household data to Excel at row {row}: {ex.Message}");
                        continue; // Skip this row and continue with the next
                    }
                }

                // Auto-fit columns for better readability in all sheets
                foreach (var sheet in package.Workbook.Worksheets)
                {
                    sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
                }

                // Convert to byte array for download
                var fileBytes = package.GetAsByteArray();
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Household_Data.xlsx");
            }
        }

        private List<HouseholdDataModel> GetHouseholdData()
        {
            List<HouseholdDataModel> householdList = new List<HouseholdDataModel>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Step 1: Fetch distinct UUIDs and their village IDs from PSC_SERVEY_SCORE
                    string uuidQuery = @"
                    SELECT DISTINCT s.UUID, s.VILLAGE_ID, s.TOTAL_PSC_SCORE
                    FROM NRSP.PSC_SERVEY_SCORE s
                    WHERE s.UUID IN (SELECT UUID FROM NRSP.HH_MM_DATA)";

                    Dictionary<string, (string VillageId, string PscScore)> uuidVillageMapping = new Dictionary<string, (string, string)>();

                    using (OracleCommand cmd = new OracleCommand(uuidQuery, conn))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string uuid = reader["UUID"]?.ToString();
                                string villageId = reader["VILLAGE_ID"]?.ToString();
                                string pscScore = reader["TOTAL_PSC_SCORE"]?.ToString();
                                if (!string.IsNullOrEmpty(uuid) && !string.IsNullOrEmpty(villageId))
                                {
                                    uuidVillageMapping[uuid] = (villageId, pscScore);
                                }
                            }
                        }
                    }

                    // Step 2: Fetch village details for each VILLAGE_ID
                    Dictionary<string, VillageDetails> villageDetailsMapping = new Dictionary<string, VillageDetails>();
                    string villageQuery = @"
                    SELECT VILLAGENAME_ID, DISTRICT_NAME, TEHSIL_NAME, UC_NAME, REVEUNE_VILLAGE_NAME, VILLAGE_NAME
                    FROM NRSP.PPRP_SERVAY_CENSUS_DATA
                    WHERE VILLAGENAME_ID IN (SELECT DISTINCT VILLAGE_ID FROM NRSP.PSC_SERVEY_SCORE)";

                    using (OracleCommand cmd = new OracleCommand(villageQuery, conn))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string villageId = reader["VILLAGENAME_ID"]?.ToString();
                                villageDetailsMapping[villageId] = new VillageDetails
                                {
                                    DistrictName = reader["DISTRICT_NAME"]?.ToString(),
                                    TehsilName = reader["TEHSIL_NAME"]?.ToString(),
                                    UcName = reader["UC_NAME"]?.ToString(),
                                    RvName = reader["REVEUNE_VILLAGE_NAME"]?.ToString(),
                                    VillageName = reader["VILLAGE_NAME"]?.ToString()
                                };
                            }
                        }
                    }

                    // Step 3: Fetch house heads for each UUID
                    Dictionary<string, string> houseHeads = new Dictionary<string, string>();
                    string headQuery = @"
                    SELECT UUID, NAME
                    FROM NRSP.HH_MM_DATA
                    WHERE HEAD = '1'";

                    using (OracleCommand cmd = new OracleCommand(headQuery, conn))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string uuid = reader["UUID"]?.ToString();
                                string name = reader["NAME"]?.ToString();
                                if (!string.IsNullOrEmpty(uuid) && !string.IsNullOrEmpty(name))
                                {
                                    houseHeads[uuid] = name;
                                }
                            }
                        }
                    }

                    // Step 4: Fetch all household data from HH_MM_DATA
                    string query = "SELECT * FROM NRSP.HH_MM_DATA";
                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string uuid = reader["UUID"]?.ToString();
                                if (string.IsNullOrEmpty(uuid)) continue;

                                // Get village details and PSC score for this UUID
                                VillageDetails villageDetails = null;
                                string pscScore = null;
                                if (uuidVillageMapping.ContainsKey(uuid))
                                {
                                    var (villageId, score) = uuidVillageMapping[uuid];
                                    pscScore = score;
                                    if (villageDetailsMapping.ContainsKey(villageId))
                                    {
                                        villageDetails = villageDetailsMapping[villageId];
                                    }
                                }

                                // Get house head for this UUID
                                string houseHead = houseHeads.ContainsKey(uuid) ? houseHeads[uuid] : "N/A";

                                householdList.Add(new HouseholdDataModel
                                {
                                    ID = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                                    UUID = uuid,
                                    HouseHead = houseHead,
                                    HH_MEM_ID = reader["HH_MEM_ID"]?.ToString(),
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
                                    Religion = reader["RELIGION"]?.ToString(),
                                    DistrictName = villageDetails?.DistrictName,
                                    TehsilName = villageDetails?.TehsilName,
                                    UcName = villageDetails?.UcName,
                                    RvName = villageDetails?.RvName,
                                    VillageName = villageDetails?.VillageName,
                                    PscScore = pscScore
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching household data from database: {Message}", ex.Message);
                    throw; // Re-throw to handle at a higher level if needed
                }
            }

            _logger.LogInformation($"Fetched {householdList.Count} household records.");
            return householdList;
        }



        public List<PSCServeyScore> GetPSCServeyScores()
        {
            List<PSCServeyScore> scores = new List<PSCServeyScore>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (OracleConnection con = new OracleConnection(connectionString))
            {
                string query = @"
            SELECT 
                pss.ID,
                pss.UUID,
                cv.DISTRICT_NAME,
                cv.TEHSIL_NAME,
                cv.UC_NAME,
                cv.REVEUNE_VILLAGE_NAME AS RV_NAME, -- Corrected column name
                cv.VILLAGE_NAME,
                pss.HH_MEM_ID,
                pss.RV_VILLAGE_ID,
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
                ON pss.VILLAGE_ID = cv.VILLAGENAME_ID";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    con.Open();
                    _logger.LogInformation("Executing query to fetch PSC survey scores.");
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            scores.Add(new PSCServeyScore
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                UUID = reader["UUID"]?.ToString(),
                                DISTRICT_NAME = reader["DISTRICT_NAME"]?.ToString(),
                                TEHSIL_NAME = reader["TEHSIL_NAME"]?.ToString(),
                                UC_NAME = reader["UC_NAME"]?.ToString(),
                                RV_NAME = reader["RV_NAME"]?.ToString(),
                                VILLAGE_NAME = reader["VILLAGE_NAME"]?.ToString(),
                                HH_MEM_ID = reader["HH_MEM_ID"]?.ToString(),
                                RV_VILLAGE_ID = reader["RV_VILLAGE_ID"]?.ToString(),
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
            }

            _logger.LogInformation($"Fetched {scores.Count} records from PSC_SERVEY_SCORE after joining with PPRP_SERVAY_CENSUS_DATA.");
            return scores;
        }

        // Existing method (unchanged)
        public IActionResult PSCServeyScore()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }
            List<PSCServeyScore> model = GetPSCServeyScores();
            return View(model);
        }

        // Updated method to include Village Name after RV Name
        // Updated method to include Full Address
        [HttpGet]
        public IActionResult PSC_Survey_Report_Updated()
        {
            _logger.LogInformation("Starting PSC_Survey_Report_Updated method.");

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                _logger.LogWarning("User not logged in. Redirecting to Login page.");
                return RedirectToAction("Login", "Main");
            }

            // Fetch PSC survey scores
            _logger.LogInformation("Fetching PSC survey scores.");
            var pscScores = GetPSCServeyScores();

            // Ensure each UUID appears only once (take the first occurrence)
            _logger.LogInformation("Ensuring unique UUIDs in PSC survey scores.");
            var uniquePscScores = pscScores
                .GroupBy(s => s.UUID)
                .Select(g => g.First())
                .ToList();

            // Log if there were duplicates in PSC_SERVEY_SCORE
            int totalPscRecords = pscScores.Count;
            int uniquePscRecords = uniquePscScores.Count;
            if (totalPscRecords > uniquePscRecords)
            {
                _logger.LogWarning($"Found {totalPscRecords - uniquePscRecords} duplicate UUIDs in PSC_SERVEY_SCORE. Total records: {totalPscRecords}, Unique records: {uniquePscRecords}.");
            }

            // Dictionary to store HH_MM_DATA details for HEAD = '1' (house head)
            var hhDataMapHead1 = new Dictionary<string, HHMMData>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Fetch HEAD = '1' records for the main report
            _logger.LogInformation("Fetching HH_MM_DATA for HEAD = '1' for the main report.");
            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                conn.Open();
                string hhQuery = @"
                SELECT UUID, NAME, CONTACT_NO, ENUMERATOR_NAME, ENUMERATOR_ID, CREATED_DATE, ADDRESS
                FROM NRSP.HH_MM_DATA
                WHERE HEAD = '1'"; // Fetch house head name, address, and details

                using (OracleCommand cmd = new OracleCommand(hhQuery, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string uuid = reader["UUID"]?.ToString();
                            if (!string.IsNullOrEmpty(uuid) && !hhDataMapHead1.ContainsKey(uuid))
                            {
                                hhDataMapHead1[uuid] = new HHMMData
                                {
                                    HeadName = reader["NAME"]?.ToString(),
                                    ContactNo = reader["CONTACT_NO"]?.ToString(),
                                    EnumeratorName = reader["ENUMERATOR_NAME"]?.ToString(),
                                    EnumeratorID = reader["ENUMERATOR_ID"]?.ToString(),
                                    CreatedDate = reader["CREATED_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CREATED_DATE"]) : (DateTime?)null,
                                    Address = reader["ADDRESS"]?.ToString()
                                };
                            }
                        }
                    }
                }
            }

            // Create an Excel package for the main report
            _logger.LogInformation("Generating PSC_Survey_Report.xlsx.");
            byte[] fileBytesMain;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("PSC Survey Report");

                // Define headers (add Full Address after Village Name)
                var headers = new List<string>
            {
                "ID", "UUID", "District Name", "Tehsil Name", "UC Name", "RV Name", "Village Name",
                "Full Address", // Added Full Address here
                "Head", "Contact No", "Enumerator Name", "Enumerator ID", "Created Date",
                "Household Members Count Score", "Room Score", "Toilet Score", "TV Score",
                "Refrigerator Score", "Air Conditioner Score", "Cooking Score", "Engine Driven Score",
                "Livestock Score", "Land Score", "Head Education Score", "Total PSC Score",
                "Cell Phone", "Electricity", "Source of Drinking Water", "Latitude", "Longitude",
                "Location Address", "Buffalo", "Cow", "Goat", "Sheep", "Camel", "Donkey",
                "Mule/Horse", "Village ID", "School Going Score"
            };

                // Add headers to the worksheet
                for (int col = 0; col < headers.Count; col++)
                {
                    worksheet.Cells[1, col + 1].Value = headers[col];
                    worksheet.Cells[1, col + 1].Style.Font.Bold = true; // Bold headers
                }

                // Populate data
                int row = 2; // Start from row 2 (after headers)
                foreach (var score in uniquePscScores)
                {
                    worksheet.Cells[row, 1].Value = score.ID;
                    worksheet.Cells[row, 2].Value = score.UUID;
                    worksheet.Cells[row, 3].Value = score.DISTRICT_NAME;
                    worksheet.Cells[row, 4].Value = score.TEHSIL_NAME;
                    worksheet.Cells[row, 5].Value = score.UC_NAME;
                    worksheet.Cells[row, 6].Value = score.RV_NAME;
                    worksheet.Cells[row, 7].Value = score.VILLAGE_NAME;

                    // Fetch HH_MM_DATA details for this UUID (house head)
                    if (hhDataMapHead1.ContainsKey(score.UUID))
                    {
                        var hhData = hhDataMapHead1[score.UUID];
                        worksheet.Cells[row, 8].Value = hhData.Address; // Add Full Address
                        var headCell = worksheet.Cells[row, 9];
                        headCell.Value = hhData.HeadName; // Set house head name
                        headCell.Style.Font.Bold = true; // Bold the house head name
                        worksheet.Cells[row, 10].Value = hhData.ContactNo; // Contact No for house head
                        worksheet.Cells[row, 11].Value = hhData.EnumeratorName;
                        worksheet.Cells[row, 12].Value = hhData.EnumeratorID;
                        worksheet.Cells[row, 13].Value = hhData.CreatedDate?.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        worksheet.Cells[row, 8].Value = "N/A"; // Full Address
                        worksheet.Cells[row, 9].Value = "N/A"; // Head
                        worksheet.Cells[row, 10].Value = "N/A"; // Contact No
                        worksheet.Cells[row, 11].Value = "N/A"; // Enumerator Name
                        worksheet.Cells[row, 12].Value = "N/A"; // Enumerator ID
                        worksheet.Cells[row, 13].Value = "N/A"; // Created Date
                    }

                    worksheet.Cells[row, 14].Value = score.HOUSEHOLD_MEMBERS_COUNT_SCORE;
                    worksheet.Cells[row, 15].Value = score.ROOM_SCORE;
                    worksheet.Cells[row, 16].Value = score.TOILET_SCORE;
                    worksheet.Cells[row, 17].Value = score.TV_SCORE;
                    worksheet.Cells[row, 18].Value = score.REFRIGERATOR_SCORE;
                    worksheet.Cells[row, 19].Value = score.AIRCONDITIONER_SCORE;
                    worksheet.Cells[row, 20].Value = score.COOKING_SCORE;
                    worksheet.Cells[row, 21].Value = score.ENGINE_DRIVEN_SCORE;
                    worksheet.Cells[row, 22].Value = score.LIVESTOCK_SCORE;
                    worksheet.Cells[row, 23].Value = score.LAND_SCORE;
                    worksheet.Cells[row, 24].Value = score.HEAD_EDUCATION_SCORE;
                    worksheet.Cells[row, 25].Value = score.TOTAL_PSC_SCORE;
                    worksheet.Cells[row, 26].Value = score.CELL_PHONE;
                    worksheet.Cells[row, 27].Value = score.ELECTRICITY;
                    worksheet.Cells[row, 28].Value = score.SOURCE_OF_DRINKING_WATER;
                    worksheet.Cells[row, 29].Value = score.LATITUDE;
                    worksheet.Cells[row, 30].Value = score.LONGITUDE;
                    worksheet.Cells[row, 31].Value = score.LOCATION_ADDRESS;
                    worksheet.Cells[row, 32].Value = score.BUFFALO;
                    worksheet.Cells[row, 33].Value = score.COW;
                    worksheet.Cells[row, 34].Value = score.GOAT;
                    worksheet.Cells[row, 35].Value = score.SHEEP;
                    worksheet.Cells[row, 36].Value = score.CAMEL;
                    worksheet.Cells[row, 37].Value = score.DONKEY;
                    worksheet.Cells[row, 38].Value = score.MULE_HORSE;
                    worksheet.Cells[row, 39].Value = score.VILLAGE_ID;
                    worksheet.Cells[row, 40].Value = score.SCHOOL_GOING_SCORE;

                    row++;
                }

                // Freeze the header row
                worksheet.View.FreezePanes(2, 1); // Freeze starting from row 2, column 1

                // Auto-fit columns for better readability
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Convert to byte array for download
                fileBytesMain = package.GetAsByteArray();
                _logger.LogInformation($"Main PSC Survey Report data generation completed. Total records: {row - 2}.");
            }

            // Generate the other two reports
            var fileBytesHead2 = GenerateHead2Report(uniquePscScores);
            var fileBytesDuplicates = GenerateDuplicateReport(uniquePscScores);

            // Create a ZIP file containing all three Excel files
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var entryMain = archive.CreateEntry("PSC_Survey_Report.xlsx");
                    using (var entryStream = entryMain.Open())
                    {
                        entryStream.Write(fileBytesMain, 0, fileBytesMain.Length);
                    }

                    var entryHead2 = archive.CreateEntry("Head_2_Data.xlsx");
                    using (var entryStream = entryHead2.Open())
                    {
                        entryStream.Write(fileBytesHead2, 0, fileBytesHead2.Length);
                    }

                    var entryDuplicates = archive.CreateEntry("Duplicate_Data.xlsx");
                    using (var entryStream = entryDuplicates.Open())
                    {
                        entryStream.Write(fileBytesDuplicates, 0, fileBytesDuplicates.Length);
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                _logger.LogInformation("All reports have been bundled into a ZIP file for download.");
                return File(memoryStream.ToArray(), "application/zip", "PSC_Survey_Reports.zip");
            }
        }

        private byte[] GenerateHead2Report(List<PSCServeyScore> uniquePscScores)
        {
            _logger.LogInformation("Generating Head_2_Data.xlsx.");

            // Dictionaries to store HH_MM_DATA for HEAD = '1' and HEAD = '2'
            var hhDataMapHead1 = new Dictionary<string, HHMMData>();
            var hhDataHead2 = new Dictionary<string, List<HHMMData>>();
            var pscNotInHh = new List<string>();
            var hhNotInPsc = new List<string>();

            // Fetch data for HEAD = '1' and HEAD = '2'
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                conn.Open();
                _logger.LogInformation("Fetching HH_MM_DATA for HEAD = '1' and HEAD = '2' for Head 2 report.");

                string hhQuery = @"
                SELECT UUID, NAME, CONTACT_NO, ENUMERATOR_NAME, ENUMERATOR_ID, CREATED_DATE, ADDRESS,
                       GENDER, AGE_YEARS, CNIC, MARITAL_STATUS, EDUCATION, DISABILITY, OCCUPATION, HEAD
                FROM NRSP.HH_MM_DATA
                WHERE HEAD IN ('1', '2')";

                using (OracleCommand cmd = new OracleCommand(hhQuery, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string uuid = reader["UUID"]?.ToString();
                            string head = reader["HEAD"]?.ToString();
                            if (string.IsNullOrEmpty(uuid)) continue;

                            var hhData = new HHMMData
                            {
                                UUID = uuid,
                                HeadName = reader["NAME"]?.ToString(),
                                ContactNo = reader["CONTACT_NO"]?.ToString(),
                                EnumeratorName = reader["ENUMERATOR_NAME"]?.ToString(),
                                EnumeratorID = reader["ENUMERATOR_ID"]?.ToString(),
                                CreatedDate = reader["CREATED_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CREATED_DATE"]) : (DateTime?)null,
                                Address = reader["ADDRESS"]?.ToString(),
                                Gender = reader["GENDER"]?.ToString(),
                                AgeYears = reader["AGE_YEARS"]?.ToString(),
                                CNIC = reader["CNIC"]?.ToString(),
                                MaritalStatus = reader["MARITAL_STATUS"]?.ToString(),
                                Education = reader["EDUCATION"]?.ToString(),
                                Disability = reader["DISABILITY"]?.ToString(),
                                Occupation = reader["OCCUPATION"]?.ToString(),
                                Head = head
                            };

                            if (head == "1")
                            {
                                if (!hhDataMapHead1.ContainsKey(uuid))
                                {
                                    hhDataMapHead1[uuid] = hhData;
                                }
                            }
                            else if (head == "2")
                            {
                                if (!hhDataHead2.ContainsKey(uuid))
                                {
                                    hhDataHead2[uuid] = new List<HHMMData>();
                                }
                                hhDataHead2[uuid].Add(hhData);
                            }
                        }
                    }
                }

                // Identify UUIDs that are in one table but not the other
                var pscUuids = new HashSet<string>(uniquePscScores.Select(s => s.UUID));
                var hhUuids = new HashSet<string>(hhDataMapHead1.Keys.Concat(hhDataHead2.Keys));
                pscNotInHh = pscUuids.Except(hhUuids).ToList();
                hhNotInPsc = hhUuids.Except(pscUuids).ToList();
            }

            // Log discrepancies
            if (pscNotInHh.Any())
            {
                _logger.LogWarning($"[Head 2 Report] UUIDs found in PSC scores but not in HH_MM_DATA: {string.Join(", ", pscNotInHh)}.");
            }
            if (hhNotInPsc.Any())
            {
                _logger.LogWarning($"[Head 2 Report] UUIDs found in HH_MM_DATA but not in PSC scores: {string.Join(", ", hhNotInPsc)}.");
            }

            // Define headers
            var headers = new List<string>
        {
            "ID", "UUID", "District Name", "Tehsil Name", "UC Name", "RV Name", "Village Name",
            "Full Address", "Head", "Contact No", "Enumerator Name", "Enumerator ID", "Created Date",
            "Household Members Count Score", "Room Score", "Toilet Score", "TV Score",
            "Refrigerator Score", "Air Conditioner Score", "Cooking Score", "Engine Driven Score",
            "Livestock Score", "Land Score", "Head Education Score", "Total PSC Score",
            "Cell Phone", "Electricity", "Source of Drinking Water", "Latitude", "Longitude",
            "Location Address", "Buffalo", "Cow", "Goat", "Sheep", "Camel", "Donkey",
            "Mule/Horse", "Village ID", "School Going Score"
        };

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Head 2 Data");

                // Add headers
                for (int col = 0; col < headers.Count; col++)
                {
                    worksheet.Cells[1, col + 1].Value = headers[col];
                    worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                }

                int row = 2;
                foreach (var score in uniquePscScores)
                {
                    // Include only UUIDs that have no HEAD = '1' but have at least one HEAD = '2'
                    if (hhDataMapHead1.ContainsKey(score.UUID) || !hhDataHead2.ContainsKey(score.UUID))
                    {
                        continue;
                    }

                    // Take the first HEAD = '2' record
                    var hhData = hhDataHead2[score.UUID].First();

                    worksheet.Cells[row, 1].Value = score.ID.ToString() ?? "N/A";
                    worksheet.Cells[row, 2].Value = score.UUID ?? "N/A";
                    worksheet.Cells[row, 3].Value = score.DISTRICT_NAME ?? "N/A";
                    worksheet.Cells[row, 4].Value = score.TEHSIL_NAME ?? "N/A";
                    worksheet.Cells[row, 5].Value = score.UC_NAME ?? "N/A";
                    worksheet.Cells[row, 6].Value = score.RV_NAME ?? "N/A";
                    worksheet.Cells[row, 7].Value = score.VILLAGE_NAME ?? "N/A";
                    worksheet.Cells[row, 8].Value = hhData.Address ?? "N/A";
                    var headCell = worksheet.Cells[row, 9];
                    headCell.Value = "N/A"; // No HEAD = '1', so Head is N/A
                    headCell.Style.Font.Bold = true;
                    worksheet.Cells[row, 10].Value = hhData.ContactNo ?? "N/A";
                    worksheet.Cells[row, 11].Value = hhData.EnumeratorName ?? "N/A";
                    worksheet.Cells[row, 12].Value = hhData.EnumeratorID ?? "N/A";
                    worksheet.Cells[row, 13].Value = hhData.CreatedDate?.ToString("yyyy-MM-dd") ?? "N/A";
                    worksheet.Cells[row, 14].Value = score.HOUSEHOLD_MEMBERS_COUNT_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 15].Value = score.ROOM_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 16].Value = score.TOILET_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 17].Value = score.TV_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 18].Value = score.REFRIGERATOR_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 19].Value = score.AIRCONDITIONER_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 20].Value = score.COOKING_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 21].Value = score.ENGINE_DRIVEN_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 22].Value = score.LIVESTOCK_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 23].Value = score.LAND_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 24].Value = score.HEAD_EDUCATION_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 25].Value = score.TOTAL_PSC_SCORE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 26].Value = score.CELL_PHONE ?? "N/A";
                    worksheet.Cells[row, 27].Value = score.ELECTRICITY ?? "N/A";
                    worksheet.Cells[row, 28].Value = score.SOURCE_OF_DRINKING_WATER ?? "N/A";
                    worksheet.Cells[row, 29].Value = score.LATITUDE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 30].Value = score.LONGITUDE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 31].Value = score.LOCATION_ADDRESS ?? "N/A";
                    worksheet.Cells[row, 32].Value = score.BUFFALO?.ToString() ?? "N/A";
                    worksheet.Cells[row, 33].Value = score.COW?.ToString() ?? "N/A";
                    worksheet.Cells[row, 34].Value = score.GOAT?.ToString() ?? "N/A";
                    worksheet.Cells[row, 35].Value = score.SHEEP?.ToString() ?? "N/A";
                    worksheet.Cells[row, 36].Value = score.CAMEL?.ToString() ?? "N/A";
                    worksheet.Cells[row, 37].Value = score.DONKEY?.ToString() ?? "N/A";
                    worksheet.Cells[row, 38].Value = score.MULE_HORSE?.ToString() ?? "N/A";
                    worksheet.Cells[row, 39].Value = score.VILLAGE_ID?.ToString() ?? "N/A";
                    worksheet.Cells[row, 40].Value = score.SCHOOL_GOING_SCORE?.ToString() ?? "N/A";

                    row++;
                }

                worksheet.View.FreezePanes(2, 1);
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                _logger.LogInformation($"Head 2 Data report generation completed. Total records: {row - 2}.");
                return package.GetAsByteArray();
            }
        }

        private byte[] GenerateDuplicateReport(List<PSCServeyScore> uniquePscScores)
        {
            _logger.LogInformation("Generating Duplicate_Data.xlsx.");

            // List to store duplicate records
            var duplicateRecords = new List<HHMMData>();
            var nameContactMap = new Dictionary<string, Dictionary<string, List<HHMMData>>>();
            var pscNotInHh = new List<string>();
            var hhNotInPsc = new List<string>();

            // Fetch data for duplicates
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                conn.Open();
                _logger.LogInformation("Fetching HH_MM_DATA for HEAD = '1' and HEAD = '2' for Duplicate report.");

                string hhQuery = @"
                SELECT UUID, NAME, CONTACT_NO, ENUMERATOR_NAME, ENUMERATOR_ID, CREATED_DATE, ADDRESS,
                       GENDER, AGE_YEARS, CNIC, MARITAL_STATUS, EDUCATION, DISABILITY, OCCUPATION, HEAD
                FROM NRSP.HH_MM_DATA
                WHERE HEAD IN ('1', '2')";

                using (OracleCommand cmd = new OracleCommand(hhQuery, conn))
                {
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string uuid = reader["UUID"]?.ToString();
                            if (string.IsNullOrEmpty(uuid)) continue;

                            var hhData = new HHMMData
                            {
                                UUID = uuid,
                                HeadName = reader["NAME"]?.ToString(),
                                ContactNo = reader["CONTACT_NO"]?.ToString(),
                                EnumeratorName = reader["ENUMERATOR_NAME"]?.ToString(),
                                EnumeratorID = reader["ENUMERATOR_ID"]?.ToString(),
                                CreatedDate = reader["CREATED_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CREATED_DATE"]) : (DateTime?)null,
                                Address = reader["ADDRESS"]?.ToString(),
                                Gender = reader["GENDER"]?.ToString(),
                                AgeYears = reader["AGE_YEARS"]?.ToString(),
                                CNIC = reader["CNIC"]?.ToString(),
                                MaritalStatus = reader["MARITAL_STATUS"]?.ToString(),
                                Education = reader["EDUCATION"]?.ToString(),
                                Disability = reader["DISABILITY"]?.ToString(),
                                Occupation = reader["OCCUPATION"]?.ToString(),
                                Head = reader["HEAD"]?.ToString()
                            };

                            // Check for duplicates based on NAME and CONTACT_NO
                            string nameContactKey = $"{hhData.HeadName}#{hhData.ContactNo}";
                            if (!nameContactMap.ContainsKey(uuid))
                            {
                                nameContactMap[uuid] = new Dictionary<string, List<HHMMData>>();
                            }
                            if (!nameContactMap[uuid].ContainsKey(nameContactKey))
                            {
                                nameContactMap[uuid][nameContactKey] = new List<HHMMData>();
                            }
                            nameContactMap[uuid][nameContactKey].Add(hhData);
                            if (nameContactMap[uuid][nameContactKey].Count > 1)
                            {
                                duplicateRecords.Add(hhData);
                            }
                        }
                    }
                }

                // Identify UUIDs that are in one table but not the other
                var pscUuids = new HashSet<string>(uniquePscScores.Select(s => s.UUID));
                var hhUuids = new HashSet<string>(nameContactMap.Keys);
                pscNotInHh = pscUuids.Except(hhUuids).ToList();
                hhNotInPsc = hhUuids.Except(pscUuids).ToList();
            }

            // Log discrepancies
            if (pscNotInHh.Any())
            {
                _logger.LogWarning($"[Duplicate Report] UUIDs found in PSC scores but not in HH_MM_DATA: {string.Join(", ", pscNotInHh)}.");
            }
            if (hhNotInPsc.Any())
            {
                _logger.LogWarning($"[Duplicate Report] UUIDs found in HH_MM_DATA but not in PSC scores: {string.Join(", ", hhNotInPsc)}.");
            }
            if (duplicateRecords.Any())
            {
                _logger.LogWarning($"[Duplicate Report] Duplicate records (same NAME and CONTACT_NO) found in HH_MM_DATA for UUIDs: {string.Join(", ", duplicateRecords.Select(d => d.UUID).Distinct())}.");
            }

            // Define headers
            var headers = new List<string>
        {
            "UUID", "Head", "Name", "Contact No", "Enumerator Name", "Enumerator ID", "Created Date", "Address",
            "Gender", "Age", "CNIC", "Marital Status", "Education", "Disability", "Occupation"
        };

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Duplicate Data");

                // Add headers
                for (int col = 0; col < headers.Count; col++)
                {
                    worksheet.Cells[1, col + 1].Value = headers[col];
                    worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                }

                int row = 2;
                foreach (var hhData in duplicateRecords)
                {
                    // Only include duplicates for UUIDs that exist in PSC_SERVEY_SCORE
                    if (!uniquePscScores.Any(s => s.UUID == hhData.UUID))
                    {
                        continue;
                    }

                    worksheet.Cells[row, 1].Value = hhData.UUID ?? "N/A";
                    worksheet.Cells[row, 2].Value = hhData.Head ?? "N/A";
                    worksheet.Cells[row, 3].Value = hhData.HeadName ?? "N/A";
                    worksheet.Cells[row, 4].Value = hhData.ContactNo ?? "N/A";
                    worksheet.Cells[row, 5].Value = hhData.EnumeratorName ?? "N/A";
                    worksheet.Cells[row, 6].Value = hhData.EnumeratorID ?? "N/A";
                    worksheet.Cells[row, 7].Value = hhData.CreatedDate?.ToString("yyyy-MM-dd") ?? "N/A";
                    worksheet.Cells[row, 8].Value = hhData.Address ?? "N/A";
                    worksheet.Cells[row, 9].Value = hhData.Gender == "1" ? "Male" : (hhData.Gender == "2" ? "Female" : "N/A");
                    worksheet.Cells[row, 10].Value = hhData.AgeYears ?? "N/A";
                    worksheet.Cells[row, 11].Value = hhData.CNIC ?? "N/A";
                    worksheet.Cells[row, 12].Value = hhData.MaritalStatus == "1" ? "Married" : (hhData.MaritalStatus == "2" ? "Single" : "N/A");
                    worksheet.Cells[row, 13].Value = hhData.Education ?? "N/A";
                    worksheet.Cells[row, 14].Value = hhData.Disability ?? "N/A";
                    worksheet.Cells[row, 15].Value = hhData.Occupation ?? "N/A";

                    row++;
                }

                worksheet.View.FreezePanes(2, 1);
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                _logger.LogInformation($"Duplicate Data report generation completed. Total records: {row - 2}.");
                return package.GetAsByteArray();
            }
        }
    }
}

