using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using Oracle.ManagedDataAccess.Client;
using SRSO_PPRP.Models;
using System;
using System.Data;
using System.Globalization;
using System.Text;

namespace SRSO_PPRP.Controllers
{
    public class MainController : Controller
    {
        private readonly IConfiguration _configuration;

        public MainController(IConfiguration configuration)
        {
            _configuration = configuration;
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

                // Define headers
                var headers = new List<string>
                {
                    "ID", "UUID", "House Head", "Name", "CNIC", "Contact No", "Gender", "Marital Status",
                    "Relation", "Head", "Age (Years)", "Education", "Occupation", "Address", "Religion",
                    "Status", "Upload Status", "Enumerator Name", "Enumerator ID", "Created Date",
                    "District", "Tehsil", "UC", "RV", "Village", "PSC Score"
                };

                // Add headers to the worksheet
                for (int col = 0; col < headers.Count; col++)
                {
                    worksheet.Cells[1, col + 1].Value = headers[col];
                    worksheet.Cells[1, col + 1].Style.Font.Bold = true; // Bold headers
                }

                // Populate data
                int row = 2; // Start from row 2 (after headers)
                foreach (var household in householdList)
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

                // Auto-fit columns for better readability
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

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
                                HouseHead = houseHead, // Add house head name
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
                        cd.DISTRICT AS DISTRICT_NAME,
                        ct.TEHSIL AS TEHSIL_NAME,
                        cu.UNIONCOUNCIL AS UC_NAME,
                        cr.REVENUEVILLAGE AS RV_NAME,
                        cv.VILLAGE_NAME AS VILLAGE_NAME, -- Fetching village name
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
                    INNER JOIN NRSP.CENSUS_DISTRICT cd ON pss.DISTRICT_ID = cd.DISTRICT_ID
                    INNER JOIN NRSP.CENSUS_TEHSIL ct ON pss.TEHSIL_ID = ct.TEHSIL_ID
                    INNER JOIN NRSP.CENSUS_UNIONCOUNCIL cu ON pss.UC_ID = cu.UC_ID
                    INNER JOIN NRSP.CENSUS_REVENUEVILLAGE cr ON pss.RV_VILLAGE_ID = cr.REVENUEVILLAGE_ID
                    LEFT JOIN NRSP.PPRP_SERVAY_CENSUS_DATA cv ON pss.VILLAGE_ID = cv.VILLAGENAME_ID";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    con.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            scores.Add(new PSCServeyScore
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                UUID = reader["UUID"].ToString(),
                                DISTRICT_NAME = reader["DISTRICT_NAME"]?.ToString(),
                                TEHSIL_NAME = reader["TEHSIL_NAME"]?.ToString(),
                                UC_NAME = reader["UC_NAME"]?.ToString(),
                                RV_NAME = reader["RV_NAME"]?.ToString(),
                                VILLAGE_NAME = reader["VILLAGE_NAME"]?.ToString(), // Add village name
                                HH_MEM_ID = reader["HH_MEM_ID"].ToString(),
                                RV_VILLAGE_ID = reader["RV_VILLAGE_ID"].ToString(),
                                HOUSEHOLD_MEMBERS_COUNT_SCORE = reader["HOUSEHOLD_MEMBERS_COUNT_SCORE"].ToString(),
                                ROOM_SCORE = reader["ROOM_SCORE"].ToString(),
                                TOILET_SCORE = reader["TOILET_SCORE"].ToString(),
                                TV_SCORE = reader["TV_SCORE"].ToString(),
                                REFRIGERATOR_SCORE = reader["REFRIGERATOR_SCORE"].ToString(),
                                AIRCONDITIONER_SCORE = reader["AIRCONDITIONER_SCORE"].ToString(),
                                COOKING_SCORE = reader["COOKING_SCORE"].ToString(),
                                ENGINE_DRIVEN_SCORE = reader["ENGINE_DRIVEN_SCORE"].ToString(),
                                LIVESTOCK_SCORE = reader["LIVESTOCK_SCORE"].ToString(),
                                LAND_SCORE = reader["LAND_SCORE"].ToString(),
                                HEAD_EDUCATION_SCORE = reader["HEAD_EDUCATION_SCORE"].ToString(),
                                TOTAL_PSC_SCORE = reader["TOTAL_PSC_SCORE"].ToString(),
                                CREATED_DATE = Convert.ToDateTime(reader["CREATED_DATE"]),
                                CELL_PHONE = reader["CELL_PHONE"].ToString(),
                                ELECTRICITY = reader["ELECTRICITY"].ToString(),
                                SOURCE_OF_DRINKING_WATER = reader["SOURCE_OF_DRINKING_WATER"].ToString(),
                                LATITUDE = reader["LATITUDE"].ToString(),
                                LONGITUDE = reader["LONGITUDE"].ToString(),
                                LOCATION_ADDRESS = reader["LOCATION_ADDRESS"].ToString(),
                                BUFFALO = reader["BUFFALO"].ToString(),
                                COW = reader["COW"].ToString(),
                                GOAT = reader["GOAT"].ToString(),
                                SHEEP = reader["SHEEP"].ToString(),
                                CAMEL = reader["CAMEL"].ToString(),
                                DONKEY = reader["DONKEY"].ToString(),
                                MULE_HORSE = reader["MULE_HORSE"].ToString(),
                                VILLAGE_ID = reader["VILLAGE_ID"].ToString(),
                                SCHOOL_GOING_SCORE = reader["SCHOOL_GOING_SCORE"].ToString()
                            });
                        }
                    }
                }
            }
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
        public IActionResult PSC_Survey_Report_Updated()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }

            // Fetch PSC survey scores
            var pscScores = GetPSCServeyScores();

            // Ensure each UUID appears only once (take the first occurrence)
            var uniquePscScores = pscScores
                .GroupBy(s => s.UUID)
                .Select(g => g.First())
                .ToList();

            // Dictionary to store HH_MM_DATA details for each UUID (head info)
            var hhDataMap = new Dictionary<string, HHMMData>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

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
                            if (!string.IsNullOrEmpty(uuid) && !hhDataMap.ContainsKey(uuid))
                            {
                                hhDataMap[uuid] = new HHMMData
                                {
                                    HeadName = reader["NAME"]?.ToString(),
                                    ContactNo = reader["CONTACT_NO"]?.ToString(),
                                    EnumeratorName = reader["ENUMERATOR_NAME"]?.ToString(),
                                    EnumeratorID = reader["ENUMERATOR_ID"]?.ToString(),
                                    CreatedDate = reader["CREATED_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CREATED_DATE"]) : (DateTime?)null,
                                    Address = reader["ADDRESS"]?.ToString() // Fetch the address
                                };
                            }
                        }
                    }
                }
            }

            // Create an Excel package
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
                    if (hhDataMap.ContainsKey(score.UUID))
                    {
                        var hhData = hhDataMap[score.UUID];
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
                var fileBytes = package.GetAsByteArray();
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PSC_Survey_Report.xlsx");
            }
        }
    }
}
