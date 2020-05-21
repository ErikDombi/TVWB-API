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
    [Route("/v1/grades/")]
    public class GradesController : Controller
    {
        public UserManager userManager;
        List<User> users => userManager.users;
        public GradesController(UserManager UM)
        {
            userManager = UM;
        }

        [EnableCors("Public")]
        [HttpGet]
        public async Task<IActionResult> IndexAsync(string token)
        {
            bool cached = Request.Query.ContainsKey("cached");
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            if (user == null)
                return BadRequest();
            var appToken = user.Tokens.FirstOrDefault(t => t.token == token);
            if (appToken == null)
                return BadRequest();
            if (!appToken.permissions.Contains("USER_GRADES"))
                return Unauthorized();

            if (cached)
            {
                if (user.GradesInfo != null)
                    return Content(JsonConvert.SerializeObject(user.GradesInfo.AsCached(), Formatting.Indented));
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

                WebRequest webRequest = WebRequest.Create("https://schoolapps2.tvdsb.ca/students/portal_secondary/student_Info/stnt_transcript.asp");

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

                var rows = htmlDoc.DocumentNode.Descendants("tr");
                var rows2 = rows.Where(t => (t.GetAttributeValue("bgcolor", "")) == "ebebeb" || (t.GetAttributeValue("bgcolor", "")) == "ffffff");
                var finalRows = new List<HtmlNode>();
                foreach (var row in rows2)
                {
                    var conditionA = new string(row.InnerText.Skip(0).Take(2).ToArray()) == "20";
                    var conditionB = new string(row.InnerText.Skip(4).Take(2).ToArray()) == "20";
                    if (conditionA && conditionB)
                    {
                        int index = 1;
                        var nextRow = rows2.ToArray()[rows2.ToList().IndexOf(row) + index];
                        var startsWithTwenty = nextRow.InnerText.StartsWith(new string(row.InnerText.Take(4).ToArray()) + ".");
                        var startsWithTwentyAtFour = nextRow.InnerText.StartsWith(new string(row.InnerText.Skip(4).Take(4).ToArray()) + ".");
                        while ((startsWithTwenty || startsWithTwentyAtFour) && nextRow.InnerText.Length != 8)
                        {
                            finalRows.Add(nextRow);
                            if (index + 1 < rows2.ToList().Count)
                                try
                                {
                                    nextRow = rows2.ToArray()[rows2.ToList().IndexOf(row) + ++index];
                                }
                                catch { break; }
                            else
                                break;
                        }
                    }
                }
                GradesInfo GI = new GradesInfo();
                foreach (var row in finalRows)
                {
                    var s = GradeInfo.parse(row);
                    GI.Grades.Add(s);
                }
                GI.Type = "Source";
                GI.CacheDate = DateTime.Now.ToString();
                if(GI.Grades.Count == 0)
                {
                    if (user.GradesInfo != null)
                        return Content(JsonConvert.SerializeObject(user.GradesInfo.AsCached(), Formatting.Indented));
                    return StatusCode(204);
                }
                user.Update(GI);
                return Content(JsonConvert.SerializeObject(GI, Formatting.Indented));
            }
            catch
            {
                if (user.GradesInfo != null)
                    return Content(JsonConvert.SerializeObject(user.GradesInfo.AsCached(), Formatting.Indented));
                return StatusCode(500);
            }
        }
    }

    public class GradesInfo
    {
        public List<GradeInfo> Grades = new List<GradeInfo>();
        public string Type { get; set; }
        public string CacheDate { get; set; }

        public GradesInfo AsCached()
        {
            GradesInfo TMP = this;
            TMP.Type = "Cached";
            return TMP;
        }
    }

    public class GradeInfo
    {
        public string Date { get; set; }
        public string Course { get; set; }
        public string Mark { get; set; }
        public string Comment { get; set; }
        public string Ind { get; set; }
        public string Team { get; set; }
        public string Org { get; set; }
        public string Homework { get; set; }
        public string Init { get; set; }

        public static GradeInfo parse(HtmlNode Node)
        {
            var Children = Node.ChildNodes.Select(t => t.InnerText.Replace("&nbsp;", "").Trim()).ToArray();
            GradeInfo GI = new GradeInfo();
            GI.Date = Children[0];
            GI.Course = Children[1];
            GI.Mark = Children[2];
            GI.Comment = Children[3];
            GI.Ind = Children[4];
            GI.Team = Children[5];
            GI.Org = Children[6];
            GI.Homework = Children[7];
            GI.Init = Children[8];
            return GI;
        }
    }
}