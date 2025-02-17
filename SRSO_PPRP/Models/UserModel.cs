using System.ComponentModel.DataAnnotations;

namespace SRSO_PPRP.Models
{
    public class UserModel
    {


        public int ID { get; set; }
        public string PWD { get; set; }
            public string USER_NAME { get; set; }
            public string USER_ID { get; set; }
            public int DISTRICT_ID { get; set; }


               // Changed from int to string
        public string Password { get; set; }     // Added Password field
        public string UserName { get; set; }
        public string UserID { get; set; }
        public string DistrictID { get; set; }

    }
}
