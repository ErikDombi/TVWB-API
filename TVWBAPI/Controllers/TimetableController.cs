using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TVWBAPI.Controllers
{
    [Route("/v1/timetable/")]
    public class TimetableController : Controller
    {
        [EnableCors("Public")]
        [HttpGet]
        public async Task<IActionResult> IndexAsync(string token)
        {
            bool cached = Request.Query.ContainsKey("cached");
            List<User> users = StaticFunctions.Users;
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            if (user == null)
                return BadRequest();
            var appToken = user.Tokens.FirstOrDefault(t => t.token == token);
            if (appToken == null)
                return BadRequest();
            if (!appToken.permissions.Contains("USER_TIMETABLE"))
                return Unauthorized();

            if (cached)
            {
                if (user.TimetableInfo != null)
                    return Content(JsonConvert.SerializeObject(user.TimetableInfo.AsCached(), Formatting.Indented));
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

                WebRequest webRequest = WebRequest.Create("https://schoolapps2.tvdsb.ca/students/portal_secondary/student_Info/timetable2.asp");

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
                var Rows = htmlDoc.DocumentNode.Descendants("td").ToList();
                var SemesterRows = Rows.Where(t => t.InnerText.Contains("Semester")).ToList();
                List<Term> Terms = new List<Term>();
                if (htmlDoc.DocumentNode.InnerText.Contains("Day&nbsp;1") && htmlDoc.DocumentNode.InnerText.Contains("Day&nbsp;2"))
                {
                    Rows = htmlDoc.DocumentNode.Descendants("tr").ToList();
                    SemesterRows = Rows.Where(t => t.InnerText.Contains("Semester")).ToList();
                    for (int i = 3; i < Rows.Count; i++)
                    {
                        if (Rows[i].InnerText.Contains("Semester")){
                            Terms.Add(Term.parse(Rows[i].InnerText));
                            var thisTerm = Terms.LastOrDefault();
                            for (int j = 2; j < (i != Rows.IndexOf(SemesterRows.LastOrDefault()) ?
                                (Rows.IndexOf(SemesterRows[SemesterRows.IndexOf(Rows[i]) + 1]) - Rows.IndexOf(SemesterRows[SemesterRows.IndexOf(Rows[i])])) :
                                (Rows.IndexOf(SemesterRows[SemesterRows.IndexOf(Rows[i])]) - Rows.IndexOf(SemesterRows[SemesterRows.IndexOf(Rows[i]) - 1]))
                                ); j++)
                            {
                                string cleanText = Rows[i + j].ChildNodes[1].InnerText.Replace("&nbsp;", "").Trim();
                                if (!string.IsNullOrEmpty(cleanText))
                                    thisTerm.AddClass(Rows[i + j].ChildNodes[1].InnerText, Rows[i + j].ChildNodes[0].InnerText, "1");
                                if (Rows[i+j].ChildNodes.Count >= 3)
                                    thisTerm.AddClass(Rows[i + j].ChildNodes[2].InnerText, Rows[i + j].ChildNodes[0].InnerText, "2");
                            }
                        }
                    }
                }
                else
                    for (int i = 0; i < Rows.Count; i++)
                    {
                        if (SemesterRows.Contains(Rows[i]))
                        {
                            Terms.Add(Term.parse(Rows[i].InnerText));
                            for (int j = 4; j < (i != Rows.IndexOf(SemesterRows.LastOrDefault()) ?
                                (Rows.IndexOf(SemesterRows[SemesterRows.IndexOf(Rows[i]) + 1]) - Rows.IndexOf(SemesterRows[SemesterRows.IndexOf(Rows[i])])) :
                                (Rows.IndexOf(SemesterRows[SemesterRows.IndexOf(Rows[i])]) - Rows.IndexOf(SemesterRows[SemesterRows.IndexOf(Rows[i]) - 1]))
                                ); j += 2)
                            {
                                Terms.LastOrDefault().AddClass(Rows[i + j].InnerText, Rows[i + j - 1].InnerText, "1"); //Day Variable is always '1' Here becuase there is no Second day.
                            }
                        }
                    }
                if(Terms.Select(t => t.Classes).ToList().Count < 1)
                {
                    if (user.TimetableInfo != null)
                        return Content(JsonConvert.SerializeObject(user.TimetableInfo.AsCached(), Formatting.Indented));
                    return StatusCode(204);
                }
                TimetableInfo TI = new TimetableInfo();
                TI.Terms = Terms;
                TI.Type = "Source";
                TI.CacheDate = DateTime.Now.ToString();
                user.Update(TI);
                StaticFunctions.SaveUsers(users);
                return Content(JsonConvert.SerializeObject(TI, Formatting.Indented));
            }
            catch
            {
                if (user.TimetableInfo != null)
                    return Content(JsonConvert.SerializeObject(user.TimetableInfo.AsCached(), Formatting.Indented));
                return StatusCode(500);
            }
        }
    }
}