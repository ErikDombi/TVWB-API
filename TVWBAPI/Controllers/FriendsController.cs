using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TVWBAPI.Controllers
{
    public class FriendsController : Controller
    {
        public UserManager userManager;
        List<User> users => userManager.users;
        public FriendsController(UserManager UM)
        {
            userManager = UM;
        }

        [HttpPost]
        [Route("/Friends/")]
        public IActionResult Add(string token, string friend)
        {
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            if (user == null)
                return BadRequest();
            var fuser = users.FirstOrDefault(t => t.UUID.ToString().ToLower() == friend);
            if (fuser == null)
                return BadRequest();
            user.PendingOutgoingFriendRequest.Add(fuser.UUID.ToString());
            user.AddUser(user);
            return Ok();
        }

        [HttpGet]
        [Route("/Friends/")]
        public IActionResult Get(string token)
        {
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            return Content(JsonConvert.SerializeObject(user.Friends));
        }

        [HttpGet]
        [Route("/Friends/Pending/")]
        public IActionResult GetRequests(string token)
        {
            var user = users.FirstOrDefault(t => t.Tokens.Select(c => c.token).Any(c => c == token));
            return Content(JsonConvert.SerializeObject(user.PendingIncomingFriendRequest));
        }
    }
}