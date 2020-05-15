using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TVWBAPI.Controllers
{
    [Route("/v1/attendance/")]
    public class AttendanceController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> IndexAsync(string token)
        {
            int[] Scores = new int[4];
            bool cached = Request.Query.ContainsKey("cached");
            List<User> users = StaticFunctions.Users;
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            if (user == null)
                return BadRequest();
            var appToken = user.Tokens.FirstOrDefault(t => t.token == token);
            if (appToken == null)
                return BadRequest();
            if (!appToken.permissions.Contains("USER_ATTENDANCE"))
                return Unauthorized();

            if (cached)
            {
                if (user.AttendanceInfo != null)
                    return Content(JsonConvert.SerializeObject(user.AttendanceInfo.AsCached(), Formatting.Indented));
                return StatusCode(500);
            }

            var email = user.Username;
            var password = user.Password;
            var htmlDoc = new HtmlDocument();
            var uri = new Uri($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={email}&txtPwd={password}&btnSubmit=Login");
            try
            {
                string z;
                using (HttpClientHandler httpClientHandler = new HttpClientHandler())
                {
                    httpClientHandler.AllowAutoRedirect = false;
                    using (HttpClient client = new HttpClient(httpClientHandler))
                    {
                        var p = await client.GetAsync(uri);
                        if (p.StatusCode == System.Net.HttpStatusCode.OK)
                            return Unauthorized();
                        z = p.Headers.GetValues("Location").First();
                    }
                }

                List<Cookie> x = new List<Cookie>();
                var cookieContainer = new CookieContainer();
                using (var httpClientHandler = new HttpClientHandler { CookieContainer = cookieContainer })
                {
                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        await httpClient.GetAsync(z);
                        x = cookieContainer.GetCookies(new Uri(z)).Cast<Cookie>().ToList();
                    }
                }

                WebRequest webRequest = WebRequest.Create("https://schoolapps2.tvdsb.ca/students/portal_secondary/student_Info/stnt_attendance.asp");

                foreach (var cookie in x)
                {
                    webRequest.TryAddCookie(new Cookie(cookie.Name, cookie.Value, "/", "schoolapps2.tvdsb.ca"));
                }
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
                    int period;
                    bool canConvert = int.TryParse(ROWS[i].ChildNodes[1].InnerText.Substring(0, 1), out period);
                    if(!canConvert)
                        canConvert = int.TryParse(ROWS[i].ChildNodes[1].InnerText.Substring(3, 1), out period);
                    if (!canConvert)
                        period = 0;
                    ClassesMissed.Add(new Absence()
                    {
                        Date = ROWS[i].ChildNodes[0].InnerText,
                        Period = period.ToString(),
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
                if (AI.Absences.ToList().Count == 0 && AI.Lates.ToList().Count == 0)
                {
                    if (user.AttendanceInfo != null)
                        return Content(JsonConvert.SerializeObject(user.AttendanceInfo.AsCached(), Formatting.Indented));
                    return StatusCode(204);
                }
                user.Update(AI);
                StaticFunctions.SaveUsers(users);
                return Content(JsonConvert.SerializeObject(AI, Formatting.Indented));
            }
            catch
            {
                if (user.AttendanceInfo != null)
                    return Content(JsonConvert.SerializeObject(user.AttendanceInfo.AsCached(), Formatting.Indented));
                return StatusCode(500);
            }
        }
    }

    public class Absence
    {
        public string Date { get; set; }
        public string Period { get; set; }
        public string Class { get; set; }
        public string Code { get; set; }
        public string Reason { get; set; }
    }

    public class AttendanceInfo
    {
        public List<Absence> Absences { get; set; }
        public string Absents { get; set; }
        public string Lates { get; set; }
        public string Type { get; set; }
        public string CacheDate { get; set; }

        public AttendanceInfo AsCached()
        {
            AttendanceInfo TMP = this;
            TMP.Type = "Cached";
            return TMP;
        }
    }
}