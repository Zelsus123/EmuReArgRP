﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Plus.Database.Interfaces;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Pathfinding;
using log4net;

namespace Plus.HabboRoleplay.Apartments
{
    public class ApartmentManager
    {
        /// <summary>
        /// log4net
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger("Plus.HabboRoleplay.Houses.ApartmentManager");

        /// <summary>
        /// Thread-safe dictionary containing all apartments list
        /// </summary>
        public static ConcurrentDictionary<int, Apartment> Apartments = new ConcurrentDictionary<int, Apartment>();

        /// <summary>
        /// Initializes the apartment list dictionary
        /// </summary>
        public static void Init()
        {
            Apartments.Clear();
            DataTable AP;

            using (IQueryAdapter DB = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                DB.SetQuery("SELECT * from `play_apartments`");
                AP = DB.getTable();

                if (AP != null)
                {
                    foreach (DataRow Row in AP.Rows)
                    {
                        int ID = Convert.ToInt32(Row["id"]);
                        string ModelName = Convert.ToString(Row["model_name"]);
                        int Tiles = Convert.ToInt32(Row["tiles"]);
                        string Image = Convert.ToString(Row["image"]);
                        int Price = Convert.ToInt32(Row["price"]);

                        Apartment newApart = new Apartment(ID, ModelName, Tiles, Image, Price);
                        Apartments.TryAdd(ID, newApart);
                    }
                }
            }

            log.Info("Loaded " + Apartments.Count + " apartments.");
        }

        public static List<Apartment> GetApartments()
        {
            return Apartments.Values.ToList();
        }

        public static Apartment GetApartmentById(int ID)
        {
            if (Apartments.ContainsKey(ID))
                return Apartments[ID];
            else
                return null;
        }
    }
}
