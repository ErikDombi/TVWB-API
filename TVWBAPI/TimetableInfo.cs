using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TVWBAPI
{
    public class Class
    {
        public string ClassCode { get; set; }
        public string ClassRoom { get; set; }
        public string Teacher { get; set; }
        public bool isHomeRoom = false;
        public string Period { get; set; }
        public string Day { get; set; }

        public static Class parse(string innerText)
        {
            Class cObj = new Class();
            cObj.ClassCode = string.Join("-", innerText.Split("-").Take(2));
            cObj.ClassRoom = innerText.Split(" ")[1].Substring(3, innerText.Split(" ")[1].Length - 3);
            // Strange note, without teacher names, I only need to subtract -3, but normally it should be -5 at the end above
            cObj.Teacher = innerText.Substring(cObj.ClassCode.Length + cObj.ClassRoom.Length + 5);
            if (string.IsNullOrEmpty(cObj.Teacher))
                cObj.Teacher = "-";
            return cObj;
        }

        public static Class parse(string innerText, string periodInnerText)
        {
            Class cObj = parse(innerText);
            cObj.isHomeRoom = periodInnerText.Contains("/HR");
            cObj.Period = periodInnerText.Split(";")[1].ToCharArray()[0].ToString(); // TODO: FIX
            return cObj;
        }

        public static Class Null = new Class() { ClassCode = "Spare", ClassRoom = "", Teacher = "" };
    }

    public class Term
    {
        public string Semester { get; set; }
        public string TermValue { get; set; }
        public bool isMultiday { get; set; }
        public List<Class> Classes = new List<Class>();
        public void AddClass(string innerText, string Day)
        {
            Classes.Add(Class.parse(innerText));
            Classes.LastOrDefault().Day = Day;
            if (int.Parse(Day) > 1)
                isMultiday = true;
        }
        public void AddClass(string innerText, string periodInnerText, string Day)
        {
            Classes.Add(Class.parse(innerText, periodInnerText));
            Classes.LastOrDefault().Day = Day;
            if (int.Parse(Day) > 1)
                isMultiday = true;
        }
        public static Term parse(string innerText)
        {
            Term tObj = new Term();
            try
            {
                tObj.Semester = innerText[21].ToString();
            }
            catch { }
            try
            {
                tObj.TermValue = innerText[57].ToString();
            }
            catch { }
            return tObj;
        }
    }
}
