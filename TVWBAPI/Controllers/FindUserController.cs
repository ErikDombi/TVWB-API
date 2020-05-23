using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TVWBAPI.Controllers
{
    [Route("/FindUser/")]
    public class FindUserController : Controller
    {
        public UserManager userManager;
        List<User> users => userManager.users;
        public FindUserController(UserManager UM)
        {
            userManager = UM;
        }

        [Route("/FindUser/ByUUID/")]
        public IActionResult ByUUID(string token, string UUID)
        {
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            if (user == null)
                return Unauthorized();
            return Content(JsonConvert.SerializeObject(users.FirstOrDefault(t => t.UUID.ToString() == UUID).PublicProfile(user)));
        }

        [Route("/FindUser/ByUsername/")]
        public IActionResult ByUsername(string token, string Username)
        {
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            if (user == null)
                return Unauthorized();
            return Content(JsonConvert.SerializeObject(users.FirstOrDefault(t => t.Username.ToLower() == Username.ToLower()).PublicProfile(user)));
        }

        [Route("/FindUser/Suggestions/")]
        public IActionResult Suggestions(string token, string Username)
        {
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            if (user == null)
                return Unauthorized();
            return Content(JsonConvert.SerializeObject(users.Where(t => t.Username.ToLower().Contains(Username.ToLower()) || (t.StudentInfo.FirstName.ToLower() + " " + t.StudentInfo.LastName.ToLower()).Contains(Username.ToLower())).Select(c => c.PublicProfile(user))));
        }

        [Route("/FindUser/ByToken/")]
        public IActionResult ByToken(string token)
        {
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            if (user == null)
                return Unauthorized();
            return Content(JsonConvert.SerializeObject(user.SelfUser()));
        }
    }
}