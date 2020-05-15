using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TVWBAPI.Controllers;

namespace TVWBAPI.TVDSB
{
    public class Authentication
    {
        public async static Task<bool> Authenticate(string username, string password, string appid)
        {
            username = username.ToLower().Trim();
            List<App> Apps = StaticFunctions.Apps;
            App app;
            if (!Apps.Select(t => t.AppId).Contains(appid))
                return false;
            app = Apps.FirstOrDefault(t => t.AppId == appid);
            var htmlDoc = new HtmlDocument();
            var uri = new Uri($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={username}&txtPwd={password}&btnSubmit=Login");

            using (HttpClientHandler httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.AllowAutoRedirect = false;
                using (HttpClient client = new HttpClient(httpClientHandler))
                {
                    var response = await client.GetAsync(uri);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        return false;
                }
            }
            List<User> users = StaticFunctions.Users;
            var user = users.FirstOrDefault(t => t.Username == username);
            if (user != null)
                if (user.Password != password)
                    user.Password = password;
            StaticFunctions.SaveUsers(users);
            try
            {
                if (users.FirstOrDefault(t => t.Username == username).Tokens.Any(t => t.appid == appid))
                    return true;
                else
                {
                    Guid tmpGuid = Guid.NewGuid();
                    users.FirstOrDefault(t => t.Username == username).Tokens.Add(new Token() { appid = appid, token = tmpGuid.ToString(), permissions = (app.CreatedBy.ToString() == "00000000-0000-0000-0000-000000000000" ? new List<string> { "USER_INFO", "USER_TIMETABLE", "USER_GRADES", "USER_ATTENDANCE" } : new List<string>()) });
                    StaticFunctions.SaveUsers(users);
                    return true;
                }

            }
            catch { }

            Guid guid = Guid.NewGuid();
            User usr = new User() { Username = username, Tokens = new List<Token>() { new Token() { appid = appid, token = guid.ToString(), permissions = (app.CreatedBy.ToString() == "00000000-0000-0000-0000-000000000000" ? new List<string> { "USER_INFO", "USER_TIMETABLE", "USER_GRADES", "USER_ATTENDANCE" } : new List<string>()) } }, Password = password, UUID = Guid.NewGuid() };
            users.Add(usr);
            StaticFunctions.SaveUsers(users);
            return true;
        }

        public class AuthResponse
        {
            public bool Success { get; set; }
            public Cookie Cookie { get; set; }
            public static AuthResponse Ok(Cookie cookie)
            {
                return new AuthResponse()
                {
                    Cookie = cookie,
                    Success = true
                };
            }
            public static AuthResponse Failed()
            {
                return new AuthResponse()
                {
                    Success = false
                };
            }
        }

        public async static Task<AuthResponse> Authenticate(string username, string password)
        {
            username = username.ToLower().Trim();
            var htmlDoc = new HtmlDocument();
            var uri = new Uri($"https://schoolapps2.tvdsb.ca/students/student_login/lgn.aspx?__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE=%2FwEPDwULLTE2MDk1ODI3MTFkZMUq3L2kXLCgWE%2BxPNKGiR2aDkz5&__VIEWSTATEGENERATOR=00958D10&__EVENTVALIDATION=%2FwEWBALO%2BPagDALT8dy8BQKd%2B7qdDgLCi9reA9VqLcMs82KsM9lnbdFM5U4r7vSJ&txtUserID={username}&txtPwd={password}&btnSubmit=Login");

            string PostLocation = string.Empty;
            using (HttpClientHandler httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.AllowAutoRedirect = false;
                using (HttpClient client = new HttpClient(httpClientHandler))
                {
                    var response = await client.GetAsync(uri);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        return AuthResponse.Failed();
                    PostLocation = response.Headers.GetValues("Location").First();
                }
            }

            List<Cookie> Cookies = new List<Cookie>();
            var cookieContainer = new CookieContainer();
            using (var httpClientHandler = new HttpClientHandler { CookieContainer = cookieContainer })
            {
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    await httpClient.GetAsync(PostLocation);
                    Cookies = cookieContainer.GetCookies(new Uri(PostLocation)).Cast<Cookie>().ToList();
                }
            }
            Cookie Cookie = Cookies.FirstOrDefault();

            List<User> users = StaticFunctions.Users;
            var user = users.FirstOrDefault(t => t.Username == username);
            if (user != null)
                if (user.Password != password)
                    user.Password = password;
            return AuthResponse.Ok(Cookie);
        }
    }
}
