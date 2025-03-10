using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
            List<HouseholdDataModel> householdList = GetHouseholdData(); // Fetch all household data

            // Prepare the CSV content
            StringBuilder csvContent = new StringBuilder();

            // Add the header row
            csvContent.AppendLine("ID,Name,CNIC,Contact No,Gender,Marital Status,Relation,Head,Age (Years),Education,Occupation,Address,Religion,Status,Upload Status,Enumerator Name,Enumerator ID,Created Date");

            // Add data rows
            foreach (var household in householdList)
            {
                csvContent.AppendLine($"{household.ID},{household.Name},{household.CNIC},{household.ContactNo},{household.Gender},{household.MaritalStatus},{household.Relation},{household.Head},{household.AgeYears},{household.Education},{household.Occupation},{household.Address},{household.Religion},{household.Status},{household.UploadStatus},{household.EnumeratorName},{household.EnumeratorID},{household.CreatedDate?.ToString("yyyy-MM-dd")}");
            }

            // Return the CSV file as a download
            var fileName = "Household_Data.csv";
            var contentType = "text/csv";
            return File(Encoding.UTF8.GetBytes(csvContent.ToString()), contentType, fileName);
        }

        private List<HouseholdDataModel> GetHouseholdData()
        {
            List<HouseholdDataModel> householdList = new List<HouseholdDataModel>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (OracleConnection conn = new OracleConnection(connectionString))
            {
                string query = "SELECT * FROM NRSP.HH_MM_DATA";
                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    conn.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            householdList.Add(new HouseholdDataModel
                            {
                                ID = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                                UUID = reader["UUID"]?.ToString(),
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
                                Status = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0, // Handles NULL
                                UploadStatus = reader["UPLOAD_STATUS"] != DBNull.Value ? Convert.ToInt32(reader["UPLOAD_STATUS"]) : 0, // Handles NULL
                                EnumeratorName = reader["ENUMERATOR_NAME"]?.ToString(),
                                EnumeratorID = reader["ENUMERATOR_ID"]?.ToString(),
                                CreatedDate = reader["CREATED_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CREATED_DATE"]) : (DateTime?)null, // Handles NULL
                                Religion = reader["RELIGION"]?.ToString()
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
    cd.DISTRICT AS DISTRICT_NAME,  -- Fetching the district name
    ct.TEHSIL AS TEHSIL_NAME,      -- Fetching the tehsil name
    cu.UNIONCOUNCIL AS UC_NAME,    -- Fetching the UC (Union Council) name
    cr.REVENUEVILLAGE AS RV_NAME,  -- Fetching the revenue village name
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
INNER JOIN NRSP.CENSUS_REVENUEVILLAGE cr ON pss.RV_VILLAGE_ID = cr.REVENUEVILLAGE_ID";


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

        public IActionResult PSCServeyScore()
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("User")))
            {
                return RedirectToAction("Login", "Main");
            }
            List<PSCServeyScore> model = GetPSCServeyScores();
            return View(model);
        }
    }
}
