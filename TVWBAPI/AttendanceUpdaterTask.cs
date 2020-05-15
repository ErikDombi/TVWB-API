using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TVWBAPI.Controllers;

namespace TVWBAPI
{
    internal class AttendanceUpdaterTask
    {
        public async static Task Update()
        {
            await Task.Run(async () =>
            {
                var users = StaticFunctions.Users;
                foreach(var user in users)
                {
                    var Auth = await TVDSB.Authentication.Authenticate(user.Username, user.Password);
                    if (Auth.Success)
                    {
                        var htmlDoc = new HtmlDocument();
                        WebRequest webRequest = WebRequest.Create("https://schoolapps2.tvdsb.ca/students/portal_secondary/student_Info/stnt_attendance.asp");
                        webRequest.TryAddCookie(Auth.Cookie);
                        HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                        var encoding = ASCIIEncoding.ASCII;
                        string responseText;
                        using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                        {
                            responseText = reader.ReadToEnd();
                        }
                        htmlDoc.LoadHtml(responseText);
                        var ROWS = htmlDoc.DocumentNode.Descendants("tr").Where(t => (t.GetAttributeValue("bgcolor", "") == "#E0E5EE") || (t.GetAttributeValue("bgcolor", "") == "white")).ToList();
                        List<Absence> ClassesMissed = new List<Absence>();
                        for (int i = 0; i < ROWS.Count; i++)
                        {
                            ClassesMissed.Add(new Absence()
                            {
                                Date = ROWS[i].ChildNodes[0].InnerText,
                                Period = ROWS[i].ChildNodes[1].InnerText.Substring(0, 1),
                                Class = ROWS[i].ChildNodes[2].InnerText,
                                Code = ROWS[i].ChildNodes[3].InnerText,
                                Reason = ROWS[i].ChildNodes[4].InnerText.Trim()
                            });
                        }
                        AttendanceInfo AI = new AttendanceInfo();
                        AI.Absences = ClassesMissed;
                        AI.Absents = htmlDoc.DocumentNode.Descendants("tr").LastOrDefault().ChildNodes[0].InnerText.Split("=").LastOrDefault().Trim();
                        AI.Lates = htmlDoc.DocumentNode.Descendants("tr").LastOrDefault().ChildNodes[1].InnerText.Split("=").LastOrDefault().Trim();
                        AI.Type = "Source";
                        AI.CacheDate = DateTime.Now.ToString();

                        int lateDifferences = int.Parse(AI.Lates) - int.Parse(user.AttendanceInfo.Lates);
                        int absentDifferences = int.Parse(AI.Absents) - int.Parse(user.AttendanceInfo.Absents);
                        if(lateDifferences != 0)
                        {
                            if(absentDifferences != 0)
                            {
                                if(lateDifferences > 0 && absentDifferences > 0)
                                {
                                    user.SendNotification($"{user.StudentInfo.FirstName} has {Math.Abs(lateDifferences)} new late(s) and {Math.Abs(absentDifferences)} new absent(s)");
                                }
                                else if(lateDifferences > 0 && absentDifferences < 0)
                                {
                                    user.SendNotification($"{user.StudentInfo.FirstName} has {Math.Abs(lateDifferences)} new late(s) and {Math.Abs(absentDifferences)} removed absent(s)");
                                }
                                else if(lateDifferences < 0 && absentDifferences > 0)
                                {
                                    user.SendNotification($"{user.StudentInfo.FirstName} has {Math.Abs(lateDifferences)} removed late(s) and {Math.Abs(absentDifferences)} new absent(s)");
                                }
                                else if(lateDifferences < 0 && absentDifferences < 0)
                                {
                                    user.SendNotification($"{user.StudentInfo.FirstName} has {Math.Abs(lateDifferences)} removed late(s) and {Math.Abs(absentDifferences)} removed absent(s)");
                                }
                            }
                            else
                            {
                                if(lateDifferences < 0)
                                    user.SendNotification($"{Math.Abs(lateDifferences)} Late(s) has been removed from {user.StudentInfo.FirstName}'s attendance.");
                                else if(lateDifferences > 0)
                                    user.SendNotification($"{Math.Abs(lateDifferences)} Late(s) have been added to {user.StudentInfo.FirstName}'s attendance.");
                            }
                        }else if(absentDifferences != 0)
                        {
                            if (absentDifferences < 0)
                                user.SendNotification($"{Math.Abs(absentDifferences)} {((Math.Abs(absentDifferences) > 1) ? "Absences have" : "Absent has")} been removed from {user.StudentInfo.FirstName}'s attendance.");
                            else if (absentDifferences > 0)
                                user.SendNotification($"{Math.Abs(absentDifferences)} {((Math.Abs(absentDifferences) > 1) ? "Absences have" : "Absent has")} been added to {user.StudentInfo.FirstName}'s attendance.");
                        }
                        // Don't save users before checking if there is a difference between the new & old
                        if (AI.Absences.ToList().Count != 0 && AI.Absents.ToList().Count != 0)
                            user.Update(AI);
                    }

                    //Delay The App by 3 seconds. Abusing TVDSB's site isn't the goal.
                    await Task.Delay(3000);
                }

                //This is sort of risky. If any part of the user table is updated while this function is running, then it will be overwritten.
                //This includes running two scheduled tasks at the same time. We need to look into a better way to do this. 
                StaticFunctions.SaveUsers(users);
            });
        }
    }
}