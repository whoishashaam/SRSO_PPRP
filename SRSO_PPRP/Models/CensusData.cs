// Models/CensusData.cs
namespace SRSO_PPRP.Models
{
    public class CensusData
    {
        public string DISTRICT_NAME { get; set; }
        public string DISTRICT_ID { get; set; }
        public string TEHSIL_NAME { get; set; }
        public string TEHSIL_ID { get; set; }
        public string UC_NAME { get; set; }
        public string UC_ID { get; set; }
        public string REVEUNE_VILLAGE_NAME { get; set; }
        public string REVEUNEVILLAGE_ID { get; set; }
        public string VILLAGE_NAME { get; set; }
        public string VILLAGENAME_ID { get; set; }
        public int ESTIMATED_HHS { get; set; }
    }
}

// Models/SurveyScore.cs
namespace SRSO_PPRP.Models
{
    public class SurveyScore
    {
        public string UUID { get; set; }
        public string VILLAGE_ID { get; set; }
        public string TOTAL_PSC_SCORE { get; set; }
    }
}

// Models/HouseholdMember.cs
namespace SRSO_PPRP.Models
{
    public class HouseholdMember
    {
        public string UUID { get; set; }
        public string NAME { get; set; }
        public string CONTACT_NO { get; set; }
        public string GENDER { get; set; }
        public string HEAD { get; set; }
        public string AGE_YEARS { get; set; }
        public string ADDRESS { get; set; }
    }
}

// Models/ReportViewModel.cs
namespace SRSO_PPRP.Models
{
    public class ReportViewModel
    {
        public string DistrictName { get; set; }
        public string TehsilName { get; set; }
        public string UcName { get; set; }
        public string RvName { get; set; }
        public string VillageName { get; set; }
        public string VillageId { get; set; }
        public List<HouseholdReport> Households { get; set; }
    }

    public class HouseholdReport
    {
        public string UUID { get; set; }
        public string Address { get; set; }
        public List<HouseholdMemberReport> Members { get; set; }
    }

    public class HouseholdMemberReport
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string PscScore { get; set; }
        public string ContactNo { get; set; }
        public bool IsHead { get; set; }
    }
}