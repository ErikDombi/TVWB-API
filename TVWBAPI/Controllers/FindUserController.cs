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
        public IActionResult ByUUID(string UUID)
        {
            return Content(JsonConvert.SerializeObject(users.FirstOrDefault(t => t.UUID.ToString() == UUID).PublicProfile()));
        }

        [Route("/FindUser/ByUsername/")]
        public IActionResult ByUsername(string Username)
        {
            return Content(JsonConvert.SerializeObject(users.FirstOrDefault(t => t.Username.ToLower() == Username.ToLower()).PublicProfile()));
        }

        [Route("/FindUser/Suggestions/")]
        public IActionResult Suggestions(string Username)
        {
            return Content(JsonConvert.SerializeObject(users.Where(t => t.Username.ToLower().Contains(Username)).Select(c => c.PublicProfile())));
        }
    }
}