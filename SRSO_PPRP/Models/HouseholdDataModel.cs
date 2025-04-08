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
        public string   ? GOAT { get; set; }
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

}
