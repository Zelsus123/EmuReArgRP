﻿using log4net;
using Plus.Database.Interfaces;
using Plus.HabboHotel.Cache.Process;
using Plus.HabboHotel.GameClients;
using Plus.HabboRoleplay.Web.Outgoing.Statistics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;

namespace Plus.HabboHotel.Cache
{
    public class CacheManager
    {
        private static readonly ILog log = LogManager.GetLogger("Plus.HabboHotel.Cache.CacheManager");
        public ConcurrentDictionary<int, UserCache> _usersCached;
        private ProcessComponent _process;

        public CacheManager()
        {
            this._usersCached = new ConcurrentDictionary<int, UserCache>();
            this._process = new ProcessComponent();
            this._process.Init();
            log.Info("Cache Manager -> LOADED");
        }
        public bool ContainsUser(int Id)
        {
            return _usersCached.ContainsKey(Id);
        }

        public UserCache GenerateUser(int Id)
        {
            UserCache User = null;

            if (_usersCached.ContainsKey(Id))
                if (TryGetUser(Id, out User))
                    return User;

            GameClient Client = PlusEnvironment.GetGame().GetClientManager().GetClientByUserID(Id);
            if (Client != null)
                if (Client.GetHabbo() != null)
                {
                    User = new UserCache(Id, Client.GetHabbo().Username, Client.GetHabbo().Motto, Client.GetHabbo().Look, GetUserComponent.ReturnUserStatistics(Client));
                    _usersCached.TryAdd(Id, User);
                    return User;
                }

            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `username`, `motto`, `look`, `credits`, `vip_points` FROM users WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);

                DataRow dRow = dbClient.getRow();

                dbClient.SetQuery("SELECT * FROM `play_stats` WHERE `id` = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                DataRow dRowRP = dbClient.getRow();

                if (dRow != null)
                {
                    User = new UserCache(Id, dRow["username"].ToString(), dRow["motto"].ToString(), dRow["look"].ToString(), GetUserComponent.ReturnUserStatistics(dRow, dRowRP));
                    _usersCached.TryAdd(Id, User);
                }

                dRow = null;
                dRowRP = null;
            }

            return User;
        }

        public bool TryRemoveUser(int Id, out UserCache User)
        {
            return _usersCached.TryRemove(Id, out User);
        }

        public bool TryGetUser(int Id, out UserCache User)
        {
            return _usersCached.TryGetValue(Id, out User);
        }

        public ICollection<UserCache> GetUserCache()
        {
            return this._usersCached.Values;
        }
    }
}