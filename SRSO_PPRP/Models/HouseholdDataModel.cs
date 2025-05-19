using Microsoft.AspNetCore.Mvc.Rendering;

namespace SRSO_PPRP.Models
{
    public class HouseholdDataModel
    {
        public int ID { get; set; }
        public string UUID { get; set; }
        public string HouseHead { get; set; } // New property for house head name
        public string HH_MEM_ID { get; set; }
        public string Name { get; set; }
        public string ContactNo { get; set; }
        public string Gender { get; set; }
        public string MaritalStatus { get; set; }
        public string Address { get; set; }
        public string CNICStatusID { get; set; }
        public string Relation { get; set; }
        public string Head { get; set; }
        public string Education { get; set; }
        public string Disability { get; set; }
        public string Occupation { get; set; }
        public string CNIC { get; set; }
        public string AgeYears { get; set; }
        public int Status { get; set; }
        public int UploadStatus { get; set; }
        public string EnumeratorName { get; set; }
        public string EnumeratorID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Religion { get; set; }
        public string DistrictName { get; set; }
        public string TehsilName { get; set; }
        public string UcName { get; set; }
        public string RvName { get; set; }
        public string VillageName { get; set; }
        public string PscScore { get; set; }
    }

    public class VillageDetails
    {
        public string DistrictName { get; set; }
        public string TehsilName { get; set; }
        public string UcName { get; set; }
        public string RvName { get; set; }
        public string VillageName { get; set; }
    }

    public class PSCServeyScore
    {
        public int ID { get; set; }
        public string? UUID { get; set; }
        public string? DISTRICT_ID { get; set; }

        public string? TEHSIL_ID { get; set; }

        public string DISTRICT_NAME { get; set; }  // Correct property name
        public string TEHSIL_NAME { get; set; }  // Correct property name
        public string UC_NAME { get; set; }  // Correct property name
        public string RV_NAME { get; set; }   // Correct property name
        public string? UC_ID { get; set; }

        public string? HH_MEM_ID { get; set; }
        public string? RV_VILLAGE_ID { get; set; }

        public string? HOUSEHOLD_MEMBERS_COUNT_SCORE { get; set; }
        public string? ROOM_SCORE { get; set; }
        public string? TOILET_SCORE { get; set; }
        public string? TV_SCORE { get; set; }
        public string? REFRIGERATOR_SCORE { get; set; }
        public string? AIRCONDITIONER_SCORE { get; set; }
        public string? COOKING_SCORE { get; set; }
        public string ENGINE_DRIVEN_SCORE { get; set; }
        public string LIVESTOCK_SCORE { get; set; }
        public string LAND_SCORE { get; set; }
        public string HEAD_EDUCATION_SCORE { get; set; }
        public string TOTAL_PSC_SCORE { get; set; }
        public DateTime CREATED_DATE { get; set; }
        public string CELL_PHONE { get; set; }
        public string ELECTRICITY { get; set; }
        public string SOURCE_OF_DRINKING_WATER { get; set; }
        public string LATITUDE { get; set; }
        public string LONGITUDE { get; set; }
        public string LOCATION_ADDRESS { get; set; }
        public string BUFFALO { get; set; }
        public string COW { get; set; }
        public string? GOAT { get; set; }
        public string? SHEEP { get; set; }
        public string? CAMEL { get; set; }
        public string? DONKEY { get; set; }
        public string? MULE_HORSE { get; set; }
        public string? VILLAGE_ID { get; set; }
        public string VILLAGE_NAME { get; set; }
        public string SCHOOL_GOING_SCORE { get; set; }

    }
    public class PSCReportFilterViewModel
    {
        public string SelectedDistrict { get; set; }
        public string SelectedTehsil { get; set; }
        public string SelectedUC { get; set; }
        public string SelectedRV { get; set; }
        public string SelectedVillage { get; set; }
        public string SelectedEnumerator { get; set; }

        public List<SelectListItem> Districts { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Tehsils { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> UCs { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> RVs { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Villages { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Enumerators { get; set; } = new List<SelectListItem>();
    }

    public class HHMMData
    {
        public string UUID { get; set; }
        public string HeadName { get; set; }
        public string ContactNo { get; set; }
        public string EnumeratorName { get; set; }
        public string EnumeratorID { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public string AgeYears { get; set; }

        public string CNIC { get; set; }
        public string MaritalStatus { get; set; }
        public string Education { get; set; }
        public string Disability { get; set; }
        public string Occupation { get; set; }
        public string Head { get; set; } // Added to store the HEAD value
    }


    public class HouseholdEditViewModel
    {
        public string UUID { get; set; } // Read-only
        public int PSCId { get; set; }   // Read-only
        public PSCServeyScore PSCSurveyScore { get; set; }
        public List<HouseholdDataModel> FamilyMembers { get; set; } = new List<HouseholdDataModel>();

        // Dropdown options for non-editable fields
        public List<SelectListItem> GenderOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Male" },
        new SelectListItem { Value = "2", Text = "Female" },
        new SelectListItem { Value = "3", Text = "Transgender" }
    };

        public List<SelectListItem> MaritalStatusOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Married" },
        new SelectListItem { Value = "2", Text = "Single" },
        new SelectListItem { Value = "3", Text = "Widowed" },
        new SelectListItem { Value = "4", Text = "Widower" },
        new SelectListItem { Value = "5", Text = "Divorced" }
    };

        public List<SelectListItem> OccupationOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Not Applicable" },
        new SelectListItem { Value = "2", Text = "Off-Farm Unskilled" },
        new SelectListItem { Value = "3", Text = "Housewife" },
        new SelectListItem { Value = "4", Text = "Government Job" },
        new SelectListItem { Value = "5", Text = "Student" },
        new SelectListItem { Value = "6", Text = "Idle (Not Working)" },
        new SelectListItem { Value = "7", Text = "Looking for Work" },
        new SelectListItem { Value = "8", Text = "Private Job" },
        new SelectListItem { Value = "9", Text = "Handicrafts/Cottage" },
        new SelectListItem { Value = "10", Text = "Farm Labor" },
        new SelectListItem { Value = "11", Text = "Own Farming" },
        new SelectListItem { Value = "12", Text = "Business" },
        new SelectListItem { Value = "13", Text = "Services" },
        new SelectListItem { Value = "14", Text = "Off-Farm Skilled" },
        new SelectListItem { Value = "15", Text = "Household Chores" },
        new SelectListItem { Value = "16", Text = "Don't Know" },
        new SelectListItem { Value = "17", Text = "Driver" },
        new SelectListItem { Value = "18", Text = "Food Processing" },
        new SelectListItem { Value = "19", Text = "Religious Scholar" },
        new SelectListItem { Value = "20", Text = "Retired" },
        new SelectListItem { Value = "21", Text = "Working Abroad" },
        new SelectListItem { Value = "22", Text = "Tailor" },
        new SelectListItem { Value = "23", Text = "Teacher" },
        new SelectListItem { Value = "24", Text = "Doctor" },
        new SelectListItem { Value = "25", Text = "Other (Specify)" }
    };

        public List<SelectListItem> DisabilityOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "No Disability" },
        new SelectListItem { Value = "2", Text = "Blind" },
        new SelectListItem { Value = "3", Text = "Deaf and Dumb" },
        new SelectListItem { Value = "4", Text = "Mentally Disordered" },
        new SelectListItem { Value = "5", Text = "Physically Disabled" }
    };

        public List<SelectListItem> EducationOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Prep" },
        new SelectListItem { Value = "2", Text = "Class 1" },
        new SelectListItem { Value = "3", Text = "Class 2" },
        new SelectListItem { Value = "4", Text = "Class 3" },
        new SelectListItem { Value = "5", Text = "Class 4" },
        new SelectListItem { Value = "6", Text = "Class 5" },
        new SelectListItem { Value = "7", Text = "Class 6" },
        new SelectListItem { Value = "8", Text = "Class 7" },
        new SelectListItem { Value = "9", Text = "Class 8" },
        new SelectListItem { Value = "10", Text = "Class 9" },
        new SelectListItem { Value = "11", Text = "Class 10" },
        new SelectListItem { Value = "12", Text = "Class 11" },
        new SelectListItem { Value = "13", Text = "Class 12" },
        new SelectListItem { Value = "14", Text = "Bachelor" },
        new SelectListItem { Value = "15", Text = "Master or Above" },
        new SelectListItem { Value = "16", Text = "Religious Education" },
        new SelectListItem { Value = "17", Text = "Not Literate" },
        new SelectListItem { Value = "18", Text = "Not Applicable" }
    };

        public List<SelectListItem> RelationOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Household Head" },
        new SelectListItem { Value = "2", Text = "Self" },
        new SelectListItem { Value = "3", Text = "Husband" },
        new SelectListItem { Value = "4", Text = "Wife" },
        new SelectListItem { Value = "5", Text = "Son" },
        new SelectListItem { Value = "6", Text = "Daughter" },
        new SelectListItem { Value = "7", Text = "Father" },
        new SelectListItem { Value = "8", Text = "Mother" },
        new SelectListItem { Value = "9", Text = "Brother" },
        new SelectListItem { Value = "10", Text = "Sister" },
        new SelectListItem { Value = "11", Text = "Grandfather" },
        new SelectListItem { Value = "12", Text = "Grandmother" },
        new SelectListItem { Value = "13", Text = "Grandson" },
        new SelectListItem { Value = "14", Text = "Granddaughter" },
        new SelectListItem { Value = "15", Text = "Uncle" },
        new SelectListItem { Value = "16", Text = "Aunt" },
        new SelectListItem { Value = "17", Text = "Nephew" },
        new SelectListItem { Value = "18", Text = "Niece" },
        new SelectListItem { Value = "19", Text = "Cousin" },
        new SelectListItem { Value = "20", Text = "Brother-in-Law" },
        new SelectListItem { Value = "21", Text = "Sister-in-Law" },
        new SelectListItem { Value = "22", Text = "Father-in-Law" },
        new SelectListItem { Value = "23", Text = "Mother-in-Law" },
        new SelectListItem { Value = "24", Text = "Sister-in-Law" },
        new SelectListItem { Value = "25", Text = "Daughter-in-Law" },
        new SelectListItem { Value = "26", Text = "Other" },
        new SelectListItem { Value = "27", Text = "Not Related" }
    };

        public List<SelectListItem> ReligionOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Islam" },
        new SelectListItem { Value = "2", Text = "Hinduism" },
        new SelectListItem { Value = "3", Text = "Christianity" },
        new SelectListItem { Value = "4", Text = "Parsi" },
        new SelectListItem { Value = "5", Text = "Sikh" },
        new SelectListItem { Value = "6", Text = "Ahmadi" }
    };

        // Dropdown options for PSC_SERVEY_SCORE fields
        public List<SelectListItem> YesNoOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Yes" },
        new SelectListItem { Value = "0", Text = "No" }
    };

        public List<SelectListItem> ToiletScoreOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Dry Raised or Dry Pit Latrine" },
        new SelectListItem { Value = "2", Text = "Dry Raised" },
        new SelectListItem { Value = "3", Text = "No Toilet" }
    };

        public List<SelectListItem> SourceOfDrinkingWaterOptions { get; set; } = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Piped Water" },
        new SelectListItem { Value = "2", Text = "Hand Pump" },
        new SelectListItem { Value = "3", Text = "Public Tap" },
        new SelectListItem { Value = "4", Text = "Private Borehole" },
        new SelectListItem { Value = "5", Text = "Public Borehole" },
        new SelectListItem { Value = "6", Text = "Protected Well" },
        new SelectListItem { Value = "7", Text = "Unprotected Well" },
        new SelectListItem { Value = "8", Text = "Protected Spring" },
        new SelectListItem { Value = "9", Text = "Rainwater Collection" },
        new SelectListItem { Value = "10", Text = "Bottled Water" }
    };
    }




}







