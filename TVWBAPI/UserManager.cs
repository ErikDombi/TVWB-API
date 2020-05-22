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
        public void init(NotificationHandler notificationHandler)
        {
            Load();
            foreach(var usr in users)
            {
                usr.notificationHandler = notificationHandler;
            }
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
