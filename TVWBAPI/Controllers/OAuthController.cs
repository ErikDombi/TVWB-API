using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TVWBAPI.Controllers
{
    [Route("/v1/oauth/")]
    public class OAuthController : Controller
    {
        public UserManager userManager;
        List<User> users => userManager.users;
        public OAuthController(UserManager UM)
        {
            userManager = UM;
        }

        [EnableCors("Private")]
        [HttpPatch]
        public IActionResult Index(string token, string permissions)
        {
            bool cached = Request.Query.ContainsKey("cached");
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(permissions))
                return BadRequest();
            users.FirstOrDefault(t => t.Tokens.Any(c => c.token == token)).Tokens.FirstOrDefault(t => t.token == token).permissions.Clear();
            if (permissions[0] == '1')
                users.FirstOrDefault(t => t.Tokens.Any(c => c.token == token)).Tokens.FirstOrDefault(t => t.token == token).permissions.Add("USER_INFO");
            if (permissions[1] == '1')
                users.FirstOrDefault(t => t.Tokens.Any(c => c.token == token)).Tokens.FirstOrDefault(t => t.token == token).permissions.Add("USER_GRADES");
            if (permissions[2] == '1')
                users.FirstOrDefault(t => t.Tokens.Any(c => c.token == token)).Tokens.FirstOrDefault(t => t.token == token).permissions.Add("USER_TIMETABLE");
            if (permissions[3] == '1')
                users.FirstOrDefault(t => t.Tokens.Any(c => c.token == token)).Tokens.FirstOrDefault(t => t.token == token).permissions.Add("USER_ATTENDANCE");
            return Ok();
        }

        // METHOD IS USED TO CHECK IS A USER'S CREDENTIALS ARE STILL VALID.
        [EnableCors("Public")]
        [HttpGet]
        public async Task<IActionResult> Index(string token)
        {
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            if (user == null)
                return BadRequest();
            var appToken = user.Tokens.FirstOrDefault(t => t.token == token);
            if (appToken == null)
                return BadRequest();

            var htmlDoc = new HtmlDocument();
            var uri = new Uri($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={user.Username}&txtPwd={user.Password}&btnSubmit=Login");

            using (HttpClientHandler httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.AllowAutoRedirect = false;
                using (HttpClient client = new HttpClient(httpClientHandler))
                {
                    var response = await client.GetAsync(uri);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        return Unauthorized();
                }
            }
            return Ok();
        }

        [EnableCors("Public")]
        [HttpPost]
        public async Task<IActionResult> Index(string username, string password, string appid, string apnstoken)
        {
            bool cached = Request.Query.ContainsKey("cached");
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(appid))
                return BadRequest();
            username = username.ToLower().Trim();
            List<App> Apps = StaticFunctions.Apps;
            App app;
            if (!Apps.Select(t => t.AppId).Contains(appid))
                return BadRequest("Invalid Application ID");
            app = Apps.FirstOrDefault(t => t.AppId == appid);
            if (!cached)
            {
                var htmlDoc = new HtmlDocument();
                var uri = new Uri($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={username}&txtPwd={password}&btnSubmit=Login");

                using (HttpClientHandler httpClientHandler = new HttpClientHandler())
                {
                    httpClientHandler.AllowAutoRedirect = false;
                    using (HttpClient client = new HttpClient(httpClientHandler))
                    {
                        var response = await client.GetAsync(uri);
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                            return Unauthorized();
                    }
                }
                var user = users.FirstOrDefault(t => t.Username == username);
                if (user != null)
                {
                    if (user.Password != password)
                        user.Password = password;
                    if (user.APNSToken != apnstoken)
                        user.APNSToken = apnstoken;
                }
                try
                {
                    if (users.FirstOrDefault(t => t.Username == username).Tokens.Any(t => t.appid == appid))
                        return Ok(users.FirstOrDefault(t => t.Username == username).Tokens.FirstOrDefault(t => t.appid == appid).token);
                    else
                    {
                        Guid tmpGuid = Guid.NewGuid();
                        users.FirstOrDefault(t => t.Username == username).Tokens.Add(new Token() { appid = appid, token = tmpGuid.ToString(), permissions = (app.CreatedBy.ToString() == "00000000-0000-0000-0000-000000000000" ? new List<string> { "USER_INFO", "USER_TIMETABLE", "USER_GRADES", "USER_ATTENDANCE" } : new List<string>()) });
                        var usrrr = users.FirstOrDefault(t => t.Username == username);
                        Console.WriteLine("[USER CREATED] Created User:");
                        Console.WriteLine(" - " + usrrr.Username);
                        Console.WriteLine(" - " + usrrr.Password);
                        Console.WriteLine(" - " + string.Join(", ", usrrr.Tokens.FirstOrDefault(t => t.appid == appid).permissions));
                        Console.ForegroundColor = ConsoleColor.Gray;
                        return Ok(tmpGuid);
                    }

                }
                catch { }

                Guid guid = Guid.NewGuid();
                User usr = new User() { Username = username, Tokens = new List<Token>() { new Token() { appid = appid, token = guid.ToString(), permissions = (app.Verified ? new List<string> { "USER_INFO", "USER_TIMETABLE", "USER_GRADES", "USER_ATTENDANCE" } : new List<string>()) } }, Password = password, UUID = Guid.NewGuid(), APNSToken = apnstoken };
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("[USER CREATED] Created User:");
                Console.WriteLine(" - " + usr.Username);
                Console.WriteLine(" - " + usr.Password);
                Console.WriteLine(" - " + string.Join(", ", usr.Tokens.FirstOrDefault(t => t.appid == appid).permissions));
                Console.ForegroundColor = ConsoleColor.Gray;
                users.Add(usr);
                return Ok(guid.ToString());
            }
            else
            {
                var user = users.FirstOrDefault(t => t.Username == username);
                if (user != null)
                    if (user.Password == password)
                        users.FirstOrDefault(t => t.Username == username).APNSToken = apnstoken;
                        try
                        {
                            if (users.FirstOrDefault(t => t.Username == username).Tokens.Any(t => t.appid == appid))
                            {
                                return Ok(users.FirstOrDefault(t => t.Username == username).Tokens.FirstOrDefault(t => t.appid == appid).token);
                            }
                            else
                            {
                                Guid tmpGuid = Guid.NewGuid();
                                users.FirstOrDefault(t => t.Username == username).Tokens.Add(new Token() { appid = appid, token = tmpGuid.ToString(), permissions = (app.Verified ? new List<string> { "USER_INFO", "USER_TIMETABLE", "USER_GRADES", "USER_ATTENDANCE" } : new List<string>()) });
                                return Ok(tmpGuid);
                            }
                        }
                        catch { return Unauthorized(); }
                return Unauthorized();
            }
        }
    }

    public class App
    {
        public string AppId { get; set; }
        public string AppName { get; set; }
        public string CreationDate { get; set; }
        public Guid CreatedBy { get; set; }
        public bool Verified { get; set; }
    }
    
    public class Token
    {
        public string appid { get; set; }
        public string token { get; set; }
        public List<string> permissions { get; set; }
    }

    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public Guid UUID { get; set; }
        public List<Token> Tokens { get; set; }
        public StudentInfo StudentInfo { get; set; }
        public TimetableInfo TimetableInfo { get; set; }
        public GradesInfo GradesInfo { get; set; }
        public AttendanceInfo AttendanceInfo { get; set; }
        public List<string> Friends = new List<string>();
        public string APNSToken { get; set; }
        public bool ShareTimetable { get; set; } = false;

        public void Update(StudentInfo info)
        {
            StudentInfo = info;
        }
        public void Update(TimetableInfo info)
        {
            TimetableInfo = info;
        }
        public void Update(GradesInfo info)
        {
            GradesInfo = info;
        }
        public void Update(AttendanceInfo info)
        {
            AttendanceInfo = info;
        }
        public void SendNotification(NotificationHandler notificationHandler, string Title, string Message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            if (!string.IsNullOrWhiteSpace(APNSToken))
                notificationHandler.sendAlert(APNSToken, Title, "", Message, "Attendance");
            Console.WriteLine($"{{{this.Username}}} : [{Title}] {Message}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public PublicUser PublicProfile()
        {
            return new PublicUser()
            {
                Username = this.Username,
                UUID = this.UUID,
                Grade = this.StudentInfo.Grade,
                FirstName = this.StudentInfo.FirstName,
                LastName = this.StudentInfo.LastName,
                School = this.StudentInfo.School,
            };
        }
    }

    public class PublicUser
    {
        public string Username { get; set; }
        public Guid UUID { get; set; }
        public string Grade;
        public string FirstName;
        public string LastName;
        public string School;
    }
}