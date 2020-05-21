using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TVWBAPI.Controllers;

namespace TVWBAPI
{
    public class UserManager
    {
        public List<User> users;
        public UserManager()
        {
            Load();
        }

        public void Load()
        {
            users = StaticFunctions.Users;
            Console.WriteLine("[UserManager] Loading Data");
        }

        public void Save()
        {
            StaticFunctions.SaveUsers(users);
            Console.WriteLine("[UserManager] Saving Data");
        }
    }
}
