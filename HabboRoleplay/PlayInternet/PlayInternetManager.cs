﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using log4net;
using Plus.Database.Interfaces;
using System.Data;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.RolePlay.PlayInternet
{
    public static class PlayInternetManager
    {
        public static readonly ILog log = LogManager.GetLogger("Plus.HabboHotel.RolePlay.PlayInternet");
        public static readonly Dictionary<string, PlayInternet> WebPages = new Dictionary<string, PlayInternet>();

        public static void Init()
        {
            WebPages.Clear();

            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `play_internet`");
                DataTable GetPlayRooms = dbClient.getTable();

                if (GetPlayRooms != null)
                {
                    foreach (DataRow Row in GetPlayRooms.Rows)//ROOm.IsHospital;
                    {
                        WebPages.Add(Convert.ToString(Row["url"]), new PlayInternet(Convert.ToInt32(Row["id"]), Convert.ToString(Row["url"]), Convert.ToString(Row["name"]), Convert.ToString(Row["description"]), Convert.ToInt32(Row["author_id"]), Convert.ToString(Row["code"])));
                    }
                }
            }

            log.Info("Loaded " + WebPages.Count + " Web Page(s) from PlayInternet.");
        }

        public static int TryToGetWebPage(string url, out PlayInternet WP)
        {
            
            if(WebPages.TryGetValue(url, out WP))
            {
                return WebPages[url].Id;
            }
            else
            {
                return 0;
            }
        }

    }
}