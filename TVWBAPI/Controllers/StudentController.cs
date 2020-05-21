using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;

namespace TVWBAPI.Controllers
{
    [Route("/v1/student/")]
    public class StudentOAuthController : Controller
    {
        public UserManager userManager;
        List<User> users => userManager.users;
        public StudentOAuthController(UserManager UM)
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
            if (!appToken.permissions.Contains("USER_INFO"))
                return Unauthorized();

            if (cached)
            {
                if (user.StudentInfo != null)
                    return Content(JsonConvert.SerializeObject(user.StudentInfo.AsCached(), Formatting.Indented));
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

                var studentinfo = htmlDoc.DocumentNode.Descendants("body").FirstOrDefault().Descendants("table").ElementAt(1).Descendants("tr").ElementAt(0).Descendants("td");
                StudentInfo sti = new StudentInfo();
                sti.FirstName = studentinfo.ElementAt(0).ChildNodes[0].InnerText.Split(",").LastOrDefault().Trim();
                sti.LastName = studentinfo.ElementAt(0).ChildNodes[0].InnerText.Split(",").FirstOrDefault().Trim();
                sti.StudentNumber = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Student#")).InnerText.Split(" ")[2].Substring(0, 9);
                sti.OEN = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Student#")).InnerText.Split(" ").LastOrDefault();
                sti.Grade = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Grade: ")).InnerText.Split(" ").LastOrDefault();
                sti.LockerNumber = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Locker #: ")).InnerText.Split(" ").LastOrDefault();
                sti.Email = user.Username + "@gotvdsb.ca";
                sti.School = htmlDoc.DocumentNode.Descendants("font").FirstOrDefault().InnerText.Trim();
                Console.WriteLine(htmlDoc.DocumentNode.Descendants("font").FirstOrDefault().InnerText.Trim());
                sti.Type = "Source";
                sti.CacheDate = DateTime.Now.ToString();
                user.Update(sti);
                return Content(JsonConvert.SerializeObject(sti, Formatting.Indented));
            }
            catch
            {
                if (user.StudentInfo != null)
                    return Content(JsonConvert.SerializeObject(user.StudentInfo.AsCached(), Formatting.Indented));
                return StatusCode(500);
            }
        }
    }

    // Does this actually do anything? Old code? Set to Cors Private just in case.
    [EnableCors("Private")]
    [Route("/v1/basic/student/")]
    public class StudentBasicController : Controller
    {
        public async Task<IActionResult> IndexAsync([FromHeader]string email, [FromHeader]string password)
        {
            var htmlDoc = new HtmlDocument();
            var uri = new Uri($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={email}&txtPwd={password}&btnSubmit=Login");

            string z;
            using (HttpClientHandler httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.AllowAutoRedirect = false;
                using (HttpClient client = new HttpClient(httpClientHandler))
                {
                    var p = await client.GetAsync(uri);
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

            var studentinfo = htmlDoc.DocumentNode.Descendants("body").FirstOrDefault().Descendants("table").ElementAt(1).Descendants("tr").ElementAt(0).Descendants("td");
            StudentInfo tbi = new StudentInfo();
            tbi.FirstName = studentinfo.ElementAt(0).ChildNodes[0].InnerText.Split(",").LastOrDefault().Trim();
            tbi.LastName = studentinfo.ElementAt(0).ChildNodes[0].InnerText.Split(",").FirstOrDefault().Trim();
            tbi.StudentNumber = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Student#")).InnerText.Split(" ")[2].Substring(0, 9);
            tbi.OEN = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Student#")).InnerText.Split(" ").LastOrDefault();
            tbi.Grade = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Grade: ")).InnerText.Split(" ").LastOrDefault();
            tbi.LockerNumber = htmlDoc.DocumentNode.Descendants("td").FirstOrDefault(t => t.InnerText.Contains("Locker #: ")).InnerText.Split(" ").LastOrDefault();
            tbi.School = htmlDoc.DocumentNode.Descendants("font").FirstOrDefault().InnerText.Trim();
            Console.WriteLine(htmlDoc.DocumentNode.Descendants("font").FirstOrDefault().InnerText.Trim());
            tbi.Email = email + "@gotvdsb.ca";

            return Json(tbi);
        }
    }
}