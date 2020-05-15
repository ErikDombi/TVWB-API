using System.Collections.Generic;
using System.Linq;

namespace TVWBAPI.Controllers
{
    public class StudentInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StudentNumber { get; set; }
        public string OEN { get; set; }
        public string Grade { get; set; }
        public string LockerNumber { get; set; }
        public string Email { get; set; }
        public string Type { get; set; }
        public string CacheDate { get; set; }

        public StudentInfo AsCached()
        {
            StudentInfo TMP = this;
            TMP.Type = "Cached";
            return TMP;
        }
    }

    public class TimetableInfo
    {
        public List<Term> Terms { get; set; }
        public List<Class> Classes { get
            {
                return Terms.SelectMany(t => t.Classes).ToList();
            }
        }
        public string Type { get; set; }
        public string CacheDate { get; set; }

        public TimetableInfo AsCached()
        {
            TimetableInfo TMP = this;
            TMP.Type = "Cached";
            return TMP;
        }
    }
}