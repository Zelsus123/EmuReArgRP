﻿using System;
using System.Data;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

using Plus.Core;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Rooms.AI;
using Plus.HabboHotel.Rooms.Games;
using Plus.Communication.Interfaces;
using Plus.Communication.Packets.Outgoing;


using Plus.HabboHotel.Rooms.Instance;

using Plus.HabboHotel.Items.Data.Toner;
using Plus.HabboHotel.Rooms.Games.Freeze;
using Plus.HabboHotel.Items.Data.Moodlight;

using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Rooms.Session;


using Plus.HabboHotel.Rooms.Games.Football;
using Plus.HabboHotel.Rooms.Games.Banzai;
using Plus.HabboHotel.Rooms.Games.Teams;
using Plus.HabboHotel.Rooms.Trading;
using Plus.HabboHotel.Rooms.AI.Speech;
using Plus.Database.Interfaces;
using Plus.Communication.Packets.Outgoing.Rooms.Polls;
using Plus.Communication.Packets.Outgoing.Notifications;
using Plus.HabboHotel.Rooms.TraxMachine;
using Plus.HabboRoleplay.Houses;

namespace Plus.HabboHotel.Rooms
{
    public class Room : RoomData
    {
        public bool isCrashed;
        public bool mDisposed;
        public bool RoomMuted;
        public bool DiscoMode;
        public DateTime lastTimerReset;
        public DateTime lastRegeneration;

        public delegate void FurnisLoaded();
        public event FurnisLoaded OnFurnisLoad;

        public Task ProcessTask;
        public ArrayList ActiveTrades;

        public TonerData TonerData;
        public MoodlightData MoodlightData;

        public Dictionary<int, double> Bans;
        public Dictionary<int, double> MutedUsers;


        private Dictionary<int, List<RoomUser>> Tents;
        private String question;
        private HashSet<int> yesVotes;
        private HashSet<int> noVotes;
        public List<int> UsersWithRights;
        private GameManager _gameManager;
        private Freeze _freeze;
        private Soccer _soccer;
        private BattleBanzai _banzai;

        private Gamemap _gamemap;
        private GameItemHandler _gameItemHandler;

        private RoomData _roomData;
        public TeamManager teambanzai;
        public TeamManager teamfreeze;

        private RoomUserManager _roomUserManager;
        private RoomItemHandling _roomItemHandling;

        private List<string> _wordFilterList;
        private RoomTraxManager _traxManager;
        private FilterComponent _filterComponent = null;
        private WiredComponent _wiredComponent = null;

        internal int SalePrice;
       internal bool ForSale;

        public int IsLagging { get; set; }
        public int IdleTime { get; set; }

        private bool _hideWired;

        //private ProcessComponent _process = null;

        public Room(RoomData Data)
        {
            this.IsLagging = 0;
            this.IdleTime = 0;

            this._roomData = Data;
            RoomMuted = false;
            mDisposed = false;

            this.Id = Data.Id;
            this.Name = Data.Name;
            this.Description = Data.Description;
            this.OwnerName = Data.OwnerName;
            this.OwnerId = Data.OwnerId;

            this.WiredScoreBordDay = Data.WiredScoreBordDay;
            this.WiredScoreBordWeek = Data.WiredScoreBordWeek;
            this.WiredScoreBordMonth = Data.WiredScoreBordMonth;
            this.WiredScoreFirstBordInformation = Data.WiredScoreFirstBordInformation;

            this.ForSale = false;
            this.SalePrice = 0;
            this.Category = Data.Category;
            this.Type = Data.Type;
            this.Access = Data.Access;
            this.UsersNow = 0;
            this.UsersMax = Data.UsersMax;
            this.ModelName = Data.ModelName;
            this.Score = Data.Score;
            this.Tags = new List<string>();
            foreach (string tag in Data.Tags)
            {
                Tags.Add(tag);
            }

            this.AllowPets = Data.AllowPets;
            this.AllowPetsEating = Data.AllowPetsEating;
            this.RoomBlockingEnabled = Data.RoomBlockingEnabled;
            this.Hidewall = Data.Hidewall;
            this.Group = Data.Group;

            this.Password = Data.Password;
            this.Wallpaper = Data.Wallpaper;
            this.Floor = Data.Floor;
            this.Landscape = Data.Landscape;
            this._hideWired = Data.HideWired;

            this.WallThickness = Data.WallThickness;
            this.FloorThickness = Data.FloorThickness;

            this.chatMode = Data.chatMode;
            this.chatSize = Data.chatSize;
            this.chatSpeed = Data.chatSpeed;
            this.chatDistance = Data.chatDistance;
            this.extraFlood = Data.extraFlood;

            this.TradeSettings = Data.TradeSettings;

            this.WhoCanBan = Data.WhoCanBan;
            this.WhoCanKick = Data.WhoCanKick;
            this.WhoCanBan = Data.WhoCanBan;

            this.PushEnabled = Data.PushEnabled;
            this.PullEnabled = Data.PullEnabled;
            this.SPullEnabled = Data.SPullEnabled;
            this.SPushEnabled = Data.SPushEnabled;
            this.EnablesEnabled = Data.EnablesEnabled;
            this.RespectNotificationsEnabled = Data.RespectNotificationsEnabled;
            this.PetMorphsAllowed = Data.PetMorphsAllowed;

            this.ActiveTrades = new ArrayList();
            this.Bans = new Dictionary<int, double>();
            this.MutedUsers = new Dictionary<int, double>();
            this.Tents = new Dictionary<int, List<RoomUser>>();

            _gamemap = new Gamemap(this);
            if (_roomItemHandling == null)
                _roomItemHandling = new RoomItemHandling(this);
            _roomUserManager = new RoomUserManager(this);

            this._filterComponent = new FilterComponent(this);
            this._wiredComponent = new WiredComponent(this);

            this._traxManager = new RoomTraxManager(this);
            OnFurnisLoad();
            GetRoomItemHandler().LoadFurniture();
            GetGameMap().GenerateMaps();

            this.LoadPromotions();
            this.LoadRights();
            this.LoadBans();
            this.LoadFilter();
            this.InitBots();
            this.InitPets();

            Data.UsersNow = 1;

            this.City = Data.City;
            this.BankEnabled = Data.BankEnabled;
            this.ShootEnabled = Data.ShootEnabled;
            this.HitEnabled = Data.HitEnabled;
            this.SafeZoneEnabled = Data.SafeZoneEnabled;
            this.SexCommandsEnabled = Data.SexCommandsEnabled;
            this.TurfEnabled = Data.TurfEnabled;
            this.RobEnabled = Data.RobEnabled;
            this.GymEnabled = Data.GymEnabled;
            this.DriveEnabled = Data.DriveEnabled;
            this.EnterRoomMessage = Data.EnterRoomMessage;
            this.BuyCarEnabled = Data.BuyCarEnabled;
            this.SupermarketEnabled = Data.SupermarketEnabled;
            this.MunicipalidadEnabled = Data.MunicipalidadEnabled;
            this.NoHungerEnabled = Data.NoHungerEnabled;
            this.IsHospital = Data.IsHospital;
            this.IsPrison = Data.IsPrison;
            this.IsPolStation = Data.IsPolStation;
            this.IsCourt = Data.IsCourt;
            this.IsCamionero = Data.IsCamionero;
            this.IsMecanico = Data.IsMecanico;
            this.IsBasurero = Data.IsBasurero;
            this.IsMinero = Data.IsMinero;
            this.IsArmero = Data.IsArmero;
            this.MallEnabled = Data.MallEnabled;
            this.FerreteriaEnabled = Data.FerreteriaEnabled;
            this.GrangeEnabled = Data.GrangeEnabled;
            this.GasEnabled = Data.GasEnabled;
            this.RobStoreEnabled = Data.RobStoreEnabled;
            this.WardrobeEnabled = Data.WardrobeEnabled;
            this.PhoneStoreEnabled = Data.PhoneStoreEnabled;
        }

        public List<ServerPacket> HideWiredMessages(bool hideWired)
        {
            List<ServerPacket> list = new List<ServerPacket>();
            Item[] items = this.GetRoomItemHandler().GetFloor.ToArray();
            if (hideWired)
            {
                foreach(Item item in items)
                {
                    if (!item.IsWired)
                        continue;
                    list.Add(new ObjectRemoveComposer(item, 0));
                }
            }
            else
            {
                foreach (Item item in items)
                {
                    if (!item.IsWired)
                        continue;
                    list.Add(new ObjectAddComposer(item, this));
                }
            }
            return list;
        }

        public bool HideWired
        {
            get { return this._hideWired; }
            set { this._hideWired = value; }
        }

        public List<string> WordFilterList
        {
            get { return this._wordFilterList; }
            set { this._wordFilterList = value; }
        }

        #region Room Bans

        public bool UserIsBanned(int pId)
        {
            return Bans.ContainsKey(pId);
        }

        public void RemoveBan(int pId)
        {
            Bans.Remove(pId);
        }

        public void AddBan(int pId, long Time)
        {
            if (!Bans.ContainsKey(Convert.ToInt32(pId)))
                Bans.Add(pId, PlusEnvironment.GetUnixTimestamp() + Time);

            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("REPLACE INTO `room_bans` VALUES (" + pId + ", " + Id + ", " + (PlusEnvironment.GetUnixTimestamp() + Time) + ")");
            }
        }

        public List<int> BannedUsers()
        {
            var Bans = new List<int>();

            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT user_id FROM room_bans WHERE expire > UNIX_TIMESTAMP() AND room_id=" + Id);
                DataTable Table = dbClient.getTable();

                foreach (DataRow Row in Table.Rows)
                {
                    Bans.Add(Convert.ToInt32(Row[0]));
                }
            }

            return Bans;
        }

        public bool HasBanExpired(int pId)
        {
            if (!UserIsBanned(pId))
                return true;

            if (Bans[pId] < PlusEnvironment.GetUnixTimestamp())
                return true;

            return false;
        }

        public void Unban(int UserId)
        {
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.RunQuery("DELETE FROM `room_bans` WHERE `user_id` = '" + UserId + "' AND `room_id` = '" + Id + "' LIMIT 1");
            }

            if (Bans.ContainsKey(UserId))
                Bans.Remove(UserId);
        }

        #endregion

        #region Trading

        public bool HasActiveTrade(RoomUser User)
        {
            if (User.IsBot)
                return false;

            return HasActiveTrade(User.GetClient().GetHabbo().Id);
        }

        public bool HasActiveTrade(int UserId)
        {
            if (ActiveTrades.Count == 0)
                return false;

            foreach (Trade Trade in ActiveTrades.ToArray())
            {
                if (Trade.ContainsUser(UserId))
                    return true;
            }
            return false;
        }

        public Trade GetUserTrade(int UserId)
        {
            foreach (Trade Trade in ActiveTrades.ToArray())
            {
                if (Trade.ContainsUser(UserId))
                {
                    return Trade;
                }
            }

            return null;
        }

        public void TryStartTrade(RoomUser UserOne, RoomUser UserTwo)
        {
            if (UserOne == null || UserTwo == null || UserOne.IsBot || UserTwo.IsBot || UserOne.IsTrading ||
                UserTwo.IsTrading || HasActiveTrade(UserOne) || HasActiveTrade(UserTwo))
                return;

            ActiveTrades.Add(new Trade(UserOne.GetClient().GetHabbo().Id, UserTwo.GetClient().GetHabbo().Id, RoomId));
        }

        public void TryStopTrade(int UserId)
        {
            Trade Trade = GetUserTrade(UserId);

            if (Trade == null)
                return;

            Trade.CloseTrade(UserId);
            ActiveTrades.Remove(Trade);
        }

        #endregion

        public RoomTraxManager GetTraxManager()
        {
            return this._traxManager;
        }

        public int UserCount
        {
            get { return _roomUserManager.GetRoomUsers().Count; }
        }

        public int RoomId
        {
            get { return Id; }
        }

        public bool CanTradeInRoom
        {
            get { return true; }
        }

        public RoomData RoomData
        {
            get { return _roomData; }
        }

        

        public Gamemap GetGameMap()
        {
            return _gamemap;
        }

        public RoomItemHandling GetRoomItemHandler()
        {
            if (_roomItemHandling == null)
            {
                _roomItemHandling = new RoomItemHandling(this);
            }
            return _roomItemHandling;
        }

        public RoomUserManager GetRoomUserManager()
        {
            return _roomUserManager;
        }

        public HashSet<int> getNoVotes()
        {
            return noVotes;
        }

        public HashSet<int> getYesVotes()
        {
            return yesVotes;
        }

        public String getQuestion()
        {
            return question;
        }

        public Soccer GetSoccer()
        {
            if (_soccer == null)
                _soccer = new Soccer(this);

            return _soccer;
        }

        public TeamManager GetTeamManagerForBanzai()
        {
            if (teambanzai == null)
                teambanzai = TeamManager.createTeamforGame("banzai");
            return teambanzai;
        }

        public TeamManager GetTeamManagerForFreeze()
        {
            if (teamfreeze == null)
                teamfreeze = TeamManager.createTeamforGame("freeze");
            return teamfreeze;
        }

        public BattleBanzai GetBanzai()
        {
            if (_banzai == null)
                _banzai = new BattleBanzai(this);
            return _banzai;
        }

        public Freeze GetFreeze()
        {
            if (_freeze == null)
                _freeze = new Freeze(this);
            return _freeze;
        }

        public GameManager GetGameManager()
        {
            if (_gameManager == null)
                _gameManager = new GameManager(this);
            return _gameManager;
        }

        public GameItemHandler GetGameItemHandler()
        {
            if (_gameItemHandler == null)
                _gameItemHandler = new GameItemHandler(this);
            return _gameItemHandler;
        }

        public bool GotSoccer()
        {
            return (_soccer != null);
        }

        public bool GotBanzai()
        {
            return (_banzai != null);
        }

        public bool GotFreeze()
        {
            return (_freeze != null);
        }

        public void ClearTags()
        {
            Tags.Clear();
        }

        public void AddTagRange(List<string> tags)
        {
            Tags.AddRange(tags);
        }

        public void InitBots()
        {
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `id`,`room_id`,`name`,`motto`,`look`,`x`,`y`,`z`,`rotation`,`gender`,`user_id`,`ai_type`,`walk_mode`,`automatic_chat`,`speaking_interval`,`mix_sentences`,`chat_bubble` FROM `bots` WHERE `room_id` = '" + RoomId + "' AND `ai_type` != 'pet'");
                DataTable Data = dbClient.getTable();
                if (Data == null)
                    return;

                foreach (DataRow Bot in Data.Rows)
                {
                    dbClient.SetQuery("SELECT `text` FROM `bots_speech` WHERE `bot_id` = '" + Convert.ToInt32(Bot["id"]) + "'");
                    DataTable BotSpeech = dbClient.getTable();

                    List<RandomSpeech> Speeches = new List<RandomSpeech>();

                    foreach (DataRow Speech in BotSpeech.Rows)
                    {
                        Speeches.Add(new RandomSpeech(Convert.ToString(Speech["text"]), Convert.ToInt32(Bot["id"])));
                    }

                    _roomUserManager.DeployBot(new RoomBot(Convert.ToInt32(Bot["id"]), Convert.ToInt32(Bot["room_id"]), Convert.ToString(Bot["ai_type"]), Convert.ToString(Bot["walk_mode"]), Convert.ToString(Bot["name"]), Convert.ToString(Bot["motto"]), Convert.ToString(Bot["look"]), int.Parse(Bot["x"].ToString()), int.Parse(Bot["y"].ToString()), int.Parse(Bot["z"].ToString()), int.Parse(Bot["rotation"].ToString()), 0, 0, 0, 0, ref Speeches, "M", 0, Convert.ToInt32(Bot["user_id"].ToString()), Convert.ToBoolean(Bot["automatic_chat"]), Convert.ToInt32(Bot["speaking_interval"]), PlusEnvironment.EnumToBool(Bot["mix_sentences"].ToString()), Convert.ToInt32(Bot["chat_bubble"])), null);
                }
            }
        }

        public void InitPets()
        {
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `id`,`user_id`,`room_id`,`name`,`x`,`y`,`z` FROM `bots` WHERE `room_id` = '" + RoomId + "' AND `ai_type` = 'pet'");
                DataTable Data = dbClient.getTable();

                if (Data == null)
                    return;

                foreach (DataRow Row in Data.Rows)
                {
                    dbClient.SetQuery("SELECT `type`,`race`,`color`,`experience`,`energy`,`nutrition`,`respect`,`createstamp`,`have_saddle`,`anyone_ride`,`hairdye`,`pethair`,`gnome_clothing` FROM `bots_petdata` WHERE `id` = '" + Row[0] + "' LIMIT 1");
                    DataRow mRow = dbClient.getRow();
                    if (mRow == null)
                        continue;

                    Pet Pet = new Pet(Convert.ToInt32(Row["id"]), Convert.ToInt32(Row["user_id"]), Convert.ToInt32(Row["room_id"]), Convert.ToString(Row["name"]), Convert.ToInt32(mRow["type"]), Convert.ToString(mRow["race"]),
                        Convert.ToString(mRow["color"]), Convert.ToInt32(mRow["experience"]), Convert.ToInt32(mRow["energy"]), Convert.ToInt32(mRow["nutrition"]), Convert.ToInt32(mRow["respect"]), Convert.ToDouble(mRow["createstamp"]), Convert.ToInt32(Row["x"]), Convert.ToInt32(Row["y"]),
                        Convert.ToDouble(Row["z"]), Convert.ToInt32(mRow["have_saddle"]), Convert.ToInt32(mRow["anyone_ride"]), Convert.ToInt32(mRow["hairdye"]), Convert.ToInt32(mRow["pethair"]), Convert.ToString(mRow["gnome_clothing"]));

                    var RndSpeechList = new List<RandomSpeech>();

                    _roomUserManager.DeployBot(new RoomBot(Pet.PetId, RoomId, "pet", "freeroam", Pet.Name, "", Pet.Look, Pet.X, Pet.Y, Convert.ToInt32(Pet.Z), 0, 0, 0, 0, 0, ref RndSpeechList, "", 0, Pet.OwnerId, false, 0, false, 0), Pet);
                }
            }
        }

        public FilterComponent GetFilter()
        {
            return this._filterComponent;
        }

        public WiredComponent GetWired()
        {
            return this._wiredComponent;
        }

        public void LoadPromotions()
        {
            DataRow GetPromotion = null;
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `room_promotions` WHERE `room_id` = " + this.Id + " LIMIT 1;");
                GetPromotion = dbClient.getRow();

                if (GetPromotion != null)
                {
                    if (Convert.ToDouble(GetPromotion["timestamp_expire"]) > PlusEnvironment.GetUnixTimestamp())
                        RoomData._promotion = new RoomPromotion(Convert.ToString(GetPromotion["title"]), Convert.ToString(GetPromotion["description"]), Convert.ToDouble(GetPromotion["timestamp_start"]), Convert.ToDouble(GetPromotion["timestamp_expire"]), Convert.ToInt32(GetPromotion["category_id"]));
                }
            }
        }

        public void LoadRights()
        {
            UsersWithRights = new List<int>();
            if (Group != null)
                return;

            DataTable Data = null;

            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT room_rights.user_id FROM room_rights WHERE room_id = @roomid");
                dbClient.AddParameter("roomid", Id);
                Data = dbClient.getTable();
            }

            if (Data != null)
            {
                foreach (DataRow Row in Data.Rows)
                {
                    UsersWithRights.Add(Convert.ToInt32(Row["user_id"]));
                }
            }
        }

        private void LoadFilter()
        {
            this._wordFilterList = new List<string>();

            DataTable Data = null;
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT * FROM `room_filter` WHERE `room_id` = @roomid;");
                dbClient.AddParameter("roomid", Id);
                Data = dbClient.getTable();
            }

            if (Data == null)
                return;

            foreach (DataRow Row in Data.Rows)
            {
                this._wordFilterList.Add(Convert.ToString(Row["word"]));
            }
        }

        public void LoadBans()
        {
            this.Bans = new Dictionary<int, double>();

            DataTable Bans;

            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT user_id, expire FROM room_bans WHERE room_id = " + Id);
                Bans = dbClient.getTable();
            }

            if (Bans == null)
                return;

            foreach (DataRow ban in Bans.Rows)
            {
                this.Bans.Add(Convert.ToInt32(ban[0]), Convert.ToDouble(ban[1]));
            }
        }

        public bool CheckRights(GameClient Session)
        {
            return CheckRights(Session, false);
        }

        public bool CheckRights(GameClient Session, bool RequireOwnership, bool CheckForGroups = false)
        {
            try
            {
                if (Session == null || Session.GetHabbo() == null)
                    return false;

                if (Session.GetHabbo().Username == OwnerName && Type == "private")
                    return true;

                if (Session.GetHabbo().GetPermissions().HasRight("room_any_owner"))
                    return true;

                if (!RequireOwnership && Type == "public")
                {
                    // Si es dueño de la Casa (Interiores)
                    House House;
                    if (this.TryGetHouse(out House))
                    {
                        if (House != null && House.OwnerId == Session.GetHabbo().Id && !House.ForSale)
                            return true;
                    }

                    if (Session.GetHabbo().GetPermissions().HasRight("room_any_rights"))
                        return true;
                }

                if (UsersWithRights.Contains(Session.GetHabbo().Id))
                    return true;

                if (CheckForGroups && Type == "private")
                {
                    if (Group == null)
                        return false;

                    if (Group.IsAdmin(Session.GetHabbo().Id))
                        return true;

                    if (Group.AdminOnlyDeco == 0)
                    {
                        if (Group.IsAdmin(Session.GetHabbo().Id))
                            return true;
                    }
                }
            }
            catch (Exception e) { Logging.HandleException(e, "Room.CheckRights"); }
            return false;
        }

        // New Poner items en área Terreno
        public bool CheckTerrain(GameClient Session, string[] Data)
        {
            if (Data.Length < 4)
                return false;

            int X = 0;
            int Y = 0;

            if (!int.TryParse(Data[1], out X)) { return false; }
            if (!int.TryParse(Data[2], out Y)) { return false; }

            List<House> Houses = PlusEnvironment.GetGame().GetHouseManager().GetTerrainsBySignRoomId(this.Id);

            if (Houses.Count <= 0)
                return false;

            foreach (House House in Houses)
            {
                if (House.OwnerId == Session.GetHabbo().Id)
                {
                    int lix = 0;// Límite inferior X
                    int lsx = 0;// Límite superior X
                    int liy = 0;// Límite inferior Y
                    int lsy = 0;// Límite superior Y

                    try
                    {
                        int.TryParse(House.Space[0].Split(',')[0], out lix);//1
                        int.TryParse(House.Space[1].Split(',')[0], out lsx);//10
                        int.TryParse(House.Space[2].Split(',')[0], out liy);//1
                        int.TryParse(House.Space[3].Split(',')[0], out lsy);//10

                        if (X >= lix && X <= lsx && Y >= liy && Y <= lsy)
                            return true;
                    }
                    catch { }
                }
            }
            
            return false;
        }

        // New Poner items en área Terreno
        public bool CheckTerrain(GameClient Session, int furnix, int furniy)
        {
            if (Session == null)
                return false;

            int X = furnix;
            int Y = furniy;
            
            List<House> Houses = PlusEnvironment.GetGame().GetHouseManager().GetTerrainsBySignRoomId(this.Id);

            if (Houses.Count <= 0)
                return false;

            foreach (House House in Houses)
            {
                if (House.OwnerId == Session.GetHabbo().Id)
                {
                    int lix = 0;// Límite inferior X
                    int lsx = 0;// Límite superior X
                    int liy = 0;// Límite inferior Y
                    int lsy = 0;// Límite superior Y

                    try
                    {
                        int.TryParse(House.Space[0].Split(',')[0], out lix);//1
                        int.TryParse(House.Space[1].Split(',')[0], out lsx);//10
                        int.TryParse(House.Space[2].Split(',')[0], out liy);//1
                        int.TryParse(House.Space[3].Split(',')[0], out lsy);//10

                        if (X >= lix && X <= lsx && Y >= liy && Y <= lsy)
                            return true;
                    }
                    catch { }
                }
            }

            return false;
        }

        public void OnUserShoot(RoomUser User, Item Ball)
        {
            Func<Item, bool> predicate = null;
            string Key = null;
            foreach (Item item in this.GetRoomItemHandler().GetFurniObjects(Ball.GetX, Ball.GetY).ToList())
            {
                if (item.GetBaseItem().ItemName.StartsWith("fball_goal_"))
                {
                    Key = item.GetBaseItem().ItemName.Split(new char[] { '_' })[2];
                    User.UnIdle();
                    User.DanceId = 0;


                    PlusEnvironment.GetGame().GetAchievementManager().ProgressAchievement(User.GetClient(), "ACH_FootballGoalScored", 1);

                    SendMessage(new ActionComposer(User.VirtualId, 1));
                }
            }

            if (Key != null)
            {
                if (predicate == null)
                {
                    predicate = p => p.GetBaseItem().ItemName == ("fball_score_" + Key);
                }

                foreach (Item item2 in this.GetRoomItemHandler().GetFloor.Where<Item>(predicate).ToList())
                {
                    if (item2.GetBaseItem().ItemName == ("fball_score_" + Key))
                    {
                        if (!String.IsNullOrEmpty(item2.ExtraData))
                            item2.ExtraData = (Convert.ToInt32(item2.ExtraData) + 1).ToString();
                        else
                            item2.ExtraData = "1";
                        item2.UpdateState();
                    }
                }
            }
        }
        public void startQuestion(String question)
        {
            this.question = question;
            this.yesVotes = new HashSet<int>();
            this.noVotes = new HashSet<int>();

            this.SendMessage(new QuickPollMessageComposer(question));
        }

        public void endQuestion()
        {
            this.question = null;
            this.SendMessage(new QuickPollResultsMessageComposer(this.yesVotes.Count(), this.noVotes.Count()));
            if(this.yesVotes != null)
            {
                this.yesVotes.Clear();
                this.yesVotes.Clear();
            }
            if(this.noVotes != null)
            {
                this.noVotes.Clear();
            }
        }
        public void ProcessRoom()
        {
            if (isCrashed || mDisposed)
                return;

            try
            {
                if (this.GetRoomUserManager().GetRoomUsers().Count == 0)
                    this.IdleTime++;
                else if (this.IdleTime > 0)
                    this.IdleTime = 0;

                if (this.RoomData.HasActivePromotion && this.RoomData.Promotion.HasExpired)
                {
                    this.RoomData.EndPromotion();
                }

                if (this.IdleTime >= 60 && !this.RoomData.HasActivePromotion)
                {
                    PlusEnvironment.GetGame().GetRoomManager().UnloadRoom(this);
                    return;
                }

                try { GetRoomItemHandler().OnCycle(); }
                catch (Exception e)
                {
                    Logging.LogException("Room ID [" + RoomId + "] is currently having issues cycling the room items." + e.ToString());
                }

                try { GetRoomUserManager().OnCycle(); }
                catch (Exception e)
                {
                    Logging.LogException("Room ID [" + RoomId + "] is currently having issues cycling the room users." + e.ToString());
                }

                #region Status Updates
                try
                {
                    GetRoomUserManager().SerializeStatusUpdates();
                }
                catch (Exception e)
                {
                    Logging.LogException("Room ID [" + RoomId + "] is currently having issues cycling the room user statuses." + e.ToString());
                }
                #endregion

                #region Game Item Cycle
                try
                {
                    if (_gameItemHandler != null)
                        _gameItemHandler.OnCycle();
                }
                catch (Exception e)
                {
                    Logging.LogException("Room ID [" + RoomId + "] is currently having issues cycling the game items." + e.ToString());
                }
                #endregion

                try { GetWired().OnCycle(); }
                catch (Exception e)
                {
                    Logging.LogException("Room ID [" + RoomId + "] is currently having issues cycling wired." + e.ToString());
                }

                try { this._traxManager.OnCycle(); }
                catch (Exception e)
                {
                    Logging.LogException("Room ID [" + RoomId + "] is currently having issues cycling Trax Jukebox." + e.ToString());
                }


            }
            catch (Exception e)
            {
                Logging.WriteLine("Room ID [" + RoomId + "] has crashed.");
                Logging.LogException("Room ID [" + RoomId + "] has crashed." + e.ToString());
                OnRoomCrash(e);
            }
        }

        private void OnRoomCrash(Exception e)
        {
            Logging.LogThreadException(e.ToString(), "Room cycle task for room " + RoomId);

            try
            {
                foreach (RoomUser user in _roomUserManager.GetRoomUsers().ToList())
                {
                    if (user == null || user.GetClient() == null)
                        continue;

                    user.GetClient().SendNotification("Sorry, it appears that room has crashed!");//Unhandled exception in room: " + e);

                    try
                    {
                        GetRoomUserManager().RemoveUserFromRoom(user.GetClient(), true, false);
                    }
                    catch (Exception e2) { Logging.LogException(e2.ToString()); }
                }
            }
            catch (Exception e3) { Logging.LogException(e3.ToString()); }

            isCrashed = true;
            PlusEnvironment.GetGame().GetRoomManager().UnloadRoom(this, true);
        }


        public bool CheckMute(GameClient Session)
        {
            if (MutedUsers.ContainsKey(Session.GetHabbo().Id))
            {
                if (MutedUsers[Session.GetHabbo().Id] < PlusEnvironment.GetUnixTimestamp())
                {
                    MutedUsers.Remove(Session.GetHabbo().Id);
                }
                else
                {
                    return true;
                }
            }

            if (Session.GetHabbo().TimeMuted > 0 || (RoomMuted && Session.GetHabbo().Username != OwnerName))
                return true;

            return false;
        }

        public void AddChatlog(int Id, string Message)
        {
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("INSERT INTO `chatlogs` (user_id, room_id, message, timestamp) VALUES (@user, @room, @message, @time)");
                dbClient.AddParameter("user", Id);
                dbClient.AddParameter("room", RoomId);
                dbClient.AddParameter("message", Message);
                dbClient.AddParameter("time", PlusEnvironment.GetUnixTimestamp());
                dbClient.RunQuery();
            }
        }

        public bool TryGetHouse(out House House)
        {
            return PlusEnvironment.GetGame().GetHouseManager().HouseList.TryGetValue(this.RoomId, out House);
        }

        public void SendObjects(GameClient Session)
        {
            Room Room = Session.GetHabbo().CurrentRoom;

            Session.SendMessage(new HeightMapComposer(Room.GetGameMap().Model.Heightmap));
            Session.SendMessage(new FloorHeightMapComposer(Room.GetGameMap().Model.GetRelativeHeightmap(), Room.GetGameMap().StaticModel.WallHeight));

            foreach (RoomUser RoomUser in _roomUserManager.GetUserList().ToList())
            {
                if (RoomUser == null)
                    continue;

                Session.SendMessage(new UsersComposer(RoomUser));

                if (RoomUser.IsBot && RoomUser.BotData.DanceId > 0)
                    Session.SendMessage(new DanceComposer(RoomUser, RoomUser.BotData.DanceId));
                else if (!RoomUser.IsBot && !RoomUser.IsPet && RoomUser.IsDancing)
                    Session.SendMessage(new DanceComposer(RoomUser, RoomUser.DanceId));

                if (RoomUser.IsAsleep)
                    Session.SendMessage(new SleepComposer(RoomUser, true));

                if (RoomUser.CarryItemID > 0 && RoomUser.CarryTimer > 0)
                    Session.SendMessage(new CarryObjectComposer(RoomUser.VirtualId, RoomUser.CarryItemID));

                if (!RoomUser.IsBot && !RoomUser.IsPet && RoomUser.CurrentEffect > 0)
                    Room.SendMessage(new AvatarEffectComposer(RoomUser.VirtualId, RoomUser.CurrentEffect));
            }

            Session.SendMessage(new UserUpdateComposer(_roomUserManager.GetUserList().ToList()));
            Session.SendMessage(new ObjectsComposer(Room.GetRoomItemHandler().GetFloor.ToArray(), this));
            Session.SendMessage(new ItemsComposer(Room.GetRoomItemHandler().GetWall.ToArray(), this));
        }

        #region Tents
        public void AddTent(int TentId)
        {
            if (Tents.ContainsKey(TentId))
                Tents.Remove(TentId);

            Tents.Add(TentId, new List<RoomUser>());
        }

        public void RemoveTent(int TentId, Item Item)
        {
            if (!Tents.ContainsKey(TentId))
                return;

            List<RoomUser> Users = Tents[TentId];
            foreach (RoomUser User in Users.ToList())
            {
                if (User == null || User.GetClient() == null || User.GetClient().GetHabbo() == null)
                    continue;

                User.GetClient().GetHabbo().TentId = 0;
            }

            if (Tents.ContainsKey(TentId))
                Tents.Remove(TentId);
        }

        public void AddUserToTent(int TentId, RoomUser User, Item Item)
        {
            if (User != null && User.GetClient() != null && User.GetClient().GetHabbo() != null)
            {
                if (!Tents.ContainsKey(TentId))
                    Tents.Add(TentId, new List<RoomUser>());

                if (!Tents[TentId].Contains(User))
                    Tents[TentId].Add(User);
                User.GetClient().GetHabbo().TentId = TentId;
            }
        }

        public void RemoveUserFromTent(int TentId, RoomUser User, Item Item)
        {
            if (User != null && User.GetClient() != null && User.GetClient().GetHabbo() != null)
            {
                if (!Tents.ContainsKey(TentId))
                    Tents.Add(TentId, new List<RoomUser>());

                if (Tents[TentId].Contains(User))
                    Tents[TentId].Remove(User);

                User.GetClient().GetHabbo().TentId = 0;
            }
        }

        public void SendToTent(int Id, int TentId, IServerPacket Packet)
        {
            if (!Tents.ContainsKey(TentId))
                return;

            foreach (RoomUser User in Tents[TentId].ToList())
            {
                if (User == null || User.GetClient() == null || User.GetClient().GetHabbo() == null || User.GetClient().GetHabbo().MutedUsers.Contains(Id) || User.GetClient().GetHabbo().TentId != TentId)
                    continue;

                User.GetClient().SendMessage(Packet);
            }
        }
        #endregion

        #region Communication (Packets)
        public void SendMessage(IServerPacket Message, bool UsersWithRightsOnly = false)
        {
            if (Message == null)
                return;

            try
            {

                List<RoomUser> Users = this._roomUserManager.GetUserList().ToList();

                if (this == null || this._roomUserManager == null || Users == null)
                    return;

                foreach (RoomUser User in Users)
                {
                    if (User == null || User.IsBot)
                        continue;

                    if (User.GetClient() == null || User.GetClient().GetConnection() == null)
                        continue;

                    if (UsersWithRightsOnly && !this.CheckRights(User.GetClient()))
                        continue;

                    User.GetClient().SendMessage(Message);
                }
            }
            catch (Exception e)
            {
                Logging.HandleException(e, "Room.SendMessage");
            }
        }

        public void BroadcastPacket(byte[] Packet)
        {
            foreach (RoomUser User in this._roomUserManager.GetUserList().ToList())
            {
                if (User == null || User.IsBot)
                    continue;

                if (User.GetClient() == null || User.GetClient().GetConnection() == null)
                    continue;

                User.GetClient().GetConnection().SendData(Packet);
            }
        }

        public void SendMessage(List<ServerPacket> Messages)
        {
            if (Messages.Count == 0)
                return;

            try
            {
                byte[] TotalBytes = new byte[0];
                int Current = 0;

                foreach (ServerPacket Packet in Messages.ToList())
                {
                    byte[] ToAdd = Packet.GetBytes();
                    int NewLen = TotalBytes.Length + ToAdd.Length;

                    Array.Resize(ref TotalBytes, NewLen);

                    for (int i = 0; i < ToAdd.Length; i++)
                    {
                        TotalBytes[Current] = ToAdd[i];
                        Current++;
                    }
                }

                this.BroadcastPacket(TotalBytes);
            }
            catch (Exception e)
            {
                Logging.HandleException(e, "Room.SendMessage List<ServerPacket>");
            }
        }
        #endregion

        private void SaveAI()
        {
            foreach (RoomUser User in GetRoomUserManager().GetRoomUsers().ToList())
            {
                if (User == null || !User.IsBot)
                    continue;

                if (User.IsBot)
                {
                    using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.SetQuery("UPDATE bots SET x=@x, y=@y, z=@z, name=@name, look=@look, rotation=@rotation WHERE id=@id LIMIT 1;");
                        dbClient.AddParameter("name", User.BotData.Name);
                        dbClient.AddParameter("look", User.BotData.Look);
                        dbClient.AddParameter("rotation", User.BotData.Rot);
                        dbClient.AddParameter("x", User.X);
                        dbClient.AddParameter("y", User.Y);
                        dbClient.AddParameter("z", User.Z);
                        dbClient.AddParameter("id", User.BotData.BotId);
                        dbClient.RunQuery();
                    }
                }
            }
        }

        public void Dispose()
        {
            SendMessage(new CloseConnectionComposer());

            if (!mDisposed)
            {
                isCrashed = false;
                mDisposed = true;

                try
                {
                    if (ProcessTask != null && ProcessTask.IsCompleted)
                        ProcessTask.Dispose();
                }
                catch { }

                GetRoomItemHandler().SaveFurniture();

                using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("UPDATE `rooms` SET `users_now` = '0' WHERE `id` = '" + Id + "' LIMIT 1");
                }

                if (this._roomUserManager.PetCount > 0)
                    this._roomUserManager.UpdatePets();

                this.SaveAI();

                UsersNow = 0;
                RoomData.UsersNow = 0;

                UsersWithRights.Clear();
                Bans.Clear();
                MutedUsers.Clear();
                Tents.Clear();

                this.TonerData = null;
                this.MoodlightData = null;
                if(this.yesVotes != null)
                {
                    this.yesVotes.Clear();
                }
                if(this.noVotes != null)
                {
                    this.noVotes.Clear();
                }
                this._filterComponent.Cleanup();
                this._wiredComponent.Cleanup();

                if (this._gameItemHandler != null)
                    this._gameItemHandler.Dispose();

                if (this._gameManager != null)
                    this._gameManager.Dispose();

                if (this._freeze != null)
                    this._freeze.Dispose();

                if (this._banzai != null)
                    this._banzai.Dispose();

                if (this._soccer != null)
                    this._soccer.Dispose();

                if (this._gamemap != null)
                    this._gamemap.Dispose();

                if (this._roomUserManager != null)
                    this._roomUserManager.Dispose();

                if (this._roomItemHandling != null)
                    this._roomItemHandling.Dispose();



                if (ActiveTrades.Count > 0)
                    ActiveTrades.Clear();
            }
        }
    }
}