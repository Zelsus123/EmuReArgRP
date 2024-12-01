﻿using System;

using Plus.Net;
using Plus.Core;
using Plus.Communication.Packets.Incoming;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Plus.Communication.Interfaces;
using Plus.HabboHotel.Users.UserDataManagement;

using ConnectionManager;

using Plus.Communication.Packets.Outgoing.Sound;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Inventory.AvatarEffects;
using Plus.Communication.Packets.Outgoing.Inventory.Achievements;

using Plus.HabboHotel.Items;
using Plus.Communication.Encryption.Crypto.Prng;
using Plus.HabboHotel.Users.Messenger.FriendBar;
using Plus.Communication.Packets.Outgoing.BuildersClub;
using Plus.HabboHotel.Moderation;

using Plus.Database.Interfaces;
using Plus.Utilities;
using Plus.HabboHotel.Achievements;
using Plus.HabboHotel.Subscriptions;
using Plus.HabboHotel.Permissions;
using Plus.Communication.Packets.Outgoing.Notifications;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.Communication.Packets.Outgoing.Campaigns;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Users;
using Plus.Communication.Packets.Outgoing;
using Plus.Communication.Packets.Outgoing.Catalog;
using System.Collections.Generic;
using System.Linq;
using Plus.HabboHotel.Users.Messenger;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.Communication.Packets.Outgoing.LandingView;
using Plus.HabboRoleplay.RoleplayUsers;
using System.Data;
using Plus.HabboRoleplay.Events;
using Plus.HabboRoleplay.Weapons;
using Plus.HabboRoleplay.Misc;
using Plus.HabboRoleplay.Vehicles;
using Plus.HabboRoleplay.VehicleOwned;
using Plus.HabboHotel.RolePlay.PlayRoom;
using System.Text;
using Plus.HabboRoleplay.PhoneOwned;
using Plus.Messages.Net.MusCommunication;
using Plus.Messages.Net;
using System.Net.Sockets;
using System.Net;

namespace Plus.HabboHotel.GameClients
{
    public class GameClient
    {
        private readonly int _id;
        private Habbo _habbo;
        private RoleplayUser _roleplay;
        public string MachineId;
        private bool _disconnected;
        public ARC4 RC4Client = null;
        private GamePacketParser _packetParser;
        private ConnectionInformation _connection;
        public bool LoggingOut = false;
        public int PingCount { get; set; }

        public GameClient(int ClientId, ConnectionInformation pConnection)
        {
            this._id = ClientId;
            this._connection = pConnection;
            this._packetParser = new GamePacketParser(this);

            this.PingCount = 0;
        }

        private void SwitchParserRequest()
        {
            _packetParser.SetConnection(_connection);
            _packetParser.onNewPacket += parser_onNewPacket;
            byte[] data = (_connection.parser as InitialPacketParser).currentData;
            _connection.parser.Dispose();
            _connection.parser = _packetParser;
            _connection.parser.handlePacketData(data);
        }

        private void parser_onNewPacket(ClientPacket Message)
        {
            try
            {
                PlusEnvironment.GetGame().GetPacketManager().TryExecutePacket(this, Message);
            }
            catch (Exception e)
            {
                Logging.LogPacketException(Message.ToString(), e.ToString());
            }
        }

        private void PolicyRequest()
        {
            _connection.SendData(PlusEnvironment.GetDefaultEncoding().GetBytes("<?xml version=\"1.0\"?>\r\n" +
                   "<!DOCTYPE cross-domain-policy SYSTEM \"/xml/dtds/cross-domain-policy.dtd\">\r\n" +
                   "<cross-domain-policy>\r\n" +
                   "<allow-access-from domain=\"*\" to-ports=\"1-31111\" />\r\n" +
                   "</cross-domain-policy>\x0"));
        }


        public void StartConnection()
        {
            if (_connection == null)
                return;

            this.PingCount = 0;

            (_connection.parser as InitialPacketParser).PolicyRequest += PolicyRequest;
            (_connection.parser as InitialPacketParser).SwitchParserRequest += SwitchParserRequest;
            _connection.startPacketProcessing();
        }

        public bool TryAuthenticate(string AuthTicket)
        {
            try
            {
                byte errorCode = 0;
                UserData userData = UserDataFactory.GetUserData(AuthTicket, out errorCode);
                if (errorCode == 1 || errorCode == 2)
                {
                    Disconnect();
                    return false;
                }

                #region Ban Checking
                //Let's have a quick search for a ban before we successfully authenticate..
                ModerationBan BanRecord = null;
                if (!string.IsNullOrEmpty(MachineId))
                {
                    if (PlusEnvironment.GetGame().GetModerationManager().IsBanned(MachineId, out BanRecord))
                    {
                        if (PlusEnvironment.GetGame().GetModerationManager().MachineBanCheck(MachineId))
                        {
                            Disconnect();
                            return false;
                        }
                    }
                }

                if (userData.user != null)
                {
                    //Now let us check for a username ban record..
                    BanRecord = null;
                    if (PlusEnvironment.GetGame().GetModerationManager().IsBanned(userData.user.Username, out BanRecord))
                    {
                        if (PlusEnvironment.GetGame().GetModerationManager().UsernameBanCheck(userData.user.Username))
                        {
                            Disconnect();
                            return false;
                        }
                    }
                }
                #endregion

                #region RoleplayData [Play]
                using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    #region Checked Products Owned First Time
                    dbClient.SetQuery("SELECT `id` FROM `play_products_owned` WHERE `product_id` = '"+ RoleplayManager.WeedID +"' AND `user_id` = '" + userData.userID + "' LIMIT 1");
                    if (dbClient.getRow() == null)
                        dbClient.RunQuery("INSERT INTO `play_products_owned` (`product_id`,`user_id`,`extradata`) VALUES ('"+RoleplayManager.WeedID+"', '" + userData.userID + "', '0')");

                    dbClient.SetQuery("SELECT `id` FROM `play_products_owned` WHERE `product_id` = '" + RoleplayManager.CocaineID + "' AND `user_id` = '" + userData.userID + "' LIMIT 1");
                    if (dbClient.getRow() == null)
                        dbClient.RunQuery("INSERT INTO `play_products_owned` (`product_id`,`user_id`,`extradata`) VALUES ('" + RoleplayManager.CocaineID + "', '" + userData.userID + "', '0')");

                    dbClient.SetQuery("SELECT `id` FROM `play_products_owned` WHERE `product_id` = '" + RoleplayManager.MedicinesID + "' AND `user_id` = '" + userData.userID + "' LIMIT 1");
                    if (dbClient.getRow() == null)
                        dbClient.RunQuery("INSERT INTO `play_products_owned` (`product_id`,`user_id`,`extradata`) VALUES ('" + RoleplayManager.MedicinesID + "', '" + userData.userID + "', '0')");

                    dbClient.SetQuery("SELECT `id` FROM `play_products_owned` WHERE `product_id` = '" + RoleplayManager.BidonID + "' AND `user_id` = '" + userData.userID + "' LIMIT 1");
                    if (dbClient.getRow() == null)
                        dbClient.RunQuery("INSERT INTO `play_products_owned` (`product_id`,`user_id`,`extradata`) VALUES ('" + RoleplayManager.BidonID + "', '" + userData.userID + "', '0')");

                    dbClient.SetQuery("SELECT `id` FROM `play_products_owned` WHERE `product_id` = '" + RoleplayManager.MecPartsID + "' AND `user_id` = '" + userData.userID + "' LIMIT 1");
                    if (dbClient.getRow() == null)
                        dbClient.RunQuery("INSERT INTO `play_products_owned` (`product_id`,`user_id`,`extradata`) VALUES ('" + RoleplayManager.MecPartsID + "', '" + userData.userID + "', '0')");

                    dbClient.SetQuery("SELECT `id` FROM `play_products_owned` WHERE `product_id` = '" + RoleplayManager.ArmMatID + "' AND `user_id` = '" + userData.userID + "' LIMIT 1");
                    if (dbClient.getRow() == null)
                        dbClient.RunQuery("INSERT INTO `play_products_owned` (`product_id`,`user_id`,`extradata`) VALUES ('" + RoleplayManager.ArmMatID + "', '" + userData.userID + "', '0')");

                    dbClient.SetQuery("SELECT `id` FROM `play_products_owned` WHERE `product_id` = '" + RoleplayManager.ArmPiecesID + "' AND `user_id` = '" + userData.userID + "' LIMIT 1");
                    if (dbClient.getRow() == null)
                        dbClient.RunQuery("INSERT INTO `play_products_owned` (`product_id`,`user_id`,`extradata`) VALUES ('" + RoleplayManager.ArmPiecesID + "', '" + userData.userID + "', '0')");
                    #endregion

                    dbClient.SetQuery("SELECT * FROM `play_stats` WHERE `id` = '" + userData.userID + "' LIMIT 1");
                    DataRow UserRPRow = dbClient.getRow();

                    dbClient.SetQuery("SELECT * FROM `play_stats_cooldowns` WHERE `id` = '" + userData.userID + "' LIMIT 1");
                    DataRow UserRPCooldowns = dbClient.getRow();

                    if (UserRPCooldowns == null)
                    {
                        dbClient.RunQuery("INSERT INTO `play_stats_cooldowns` (`id`) VALUES ('" + userData.userID + "')");
                        dbClient.SetQuery("SELECT * FROM `play_stats_cooldowns` WHERE `id` = '" + userData.userID + "' LIMIT 1");
                        UserRPCooldowns = dbClient.getRow();
                    }

                    _roleplay = new RoleplayUser(this, UserRPRow, UserRPCooldowns);

                    if (UserRPRow != null) {
                        string Number = "";

                        List<PhonesOwned> PO = PlusEnvironment.GetGame().GetPhonesOwnedManager().getMyPhonesOwned(userData.userID);
                        if (PO != null && PO.Count > 0)
                        {
                            Number = PO[0].PhoneNumber;
                        }                        
                        
                        if(Number.Length > 0)//(xxx)-xxx-xxxx  14 lenght
                            PlusEnvironment.GetGame().GetClientManager().RegisterClientPhone(this, userData.userID, Number);
                    }
                }
                #endregion


                    PlusEnvironment.GetGame().GetClientManager().RegisterClient(this, userData.userID, userData.user.Username);
                    
                _habbo = userData.user;
                if (_habbo != null)
                {
                    userData.user.Init(this, userData);

                    SendMessage(new AuthenticationOKComposer());
                    SendMessage(new AvatarEffectsComposer(_habbo.Effects().GetAllEffects));
                    //FurniListNotification -> why?
                    SendMessage(new NavigatorSettingsComposer(_habbo.HomeRoom));
                    SendMessage(new RoomForwardComposer(_habbo.HomeRoom));
                    SendMessage(new FavouritesComposer(userData.user.FavoriteRooms));
                    SendMessage(new FigureSetIdsComposer(_habbo.GetClothing().GetClothingAllParts));
                    //1984
                    //2102
                    SendMessage(new UserRightsComposer(_habbo.Rank, this));
                    SendMessage(new AvailabilityStatusComposer());
                    //1044
                    SendMessage(new AchievementScoreComposer(_habbo.GetStats().AchievementPoints));
                    //3674
                    //3437
                    SendMessage(new BuildersClubMembershipComposer());
                    SendMessage(new CfhTopicsInitComposer());

                    SendMessage(new BadgeDefinitionsComposer(PlusEnvironment.GetGame().GetAchievementManager()._achievements));
                    SendMessage(new SoundSettingsComposer(_habbo.ClientVolume, _habbo.ChatPreference, _habbo.AllowMessengerInvites, _habbo.FocusPreference, FriendBarStateUtility.GetInt(_habbo.FriendbarState)));  
                    //SendMessage(new TalentTrackLevelComposer());

                    if (!string.IsNullOrEmpty(MachineId))
                    {
                        if (this._habbo.MachineId != MachineId)
                        {
                            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("UPDATE `users` SET `machine_id` = @MachineId WHERE `id` = @id LIMIT 1");
                                dbClient.AddParameter("MachineId", MachineId);
                                dbClient.AddParameter("id", _habbo.Id);
                                dbClient.RunQuery();
                            }
                        }

                        _habbo.MachineId = MachineId;
                    }

                    PermissionGroup PermissionGroup = null;
                    if (PlusEnvironment.GetGame().GetPermissionManager().TryGetGroup(_habbo.Rank, out PermissionGroup))
                    {
                        if (!String.IsNullOrEmpty(PermissionGroup.Badge))
                            if (!_habbo.GetBadgeComponent().HasBadge(PermissionGroup.Badge))
                                _habbo.GetBadgeComponent().GiveBadge(PermissionGroup.Badge, true, this);
                    }

                    SubscriptionData SubData = null;
                    if (PlusEnvironment.GetGame().GetSubscriptionManager().TryGetSubscriptionData(this._habbo.VIPRank, out SubData))
                    {
                        if (!String.IsNullOrEmpty(SubData.Badge))
                        {
                            if (!_habbo.GetBadgeComponent().HasBadge(SubData.Badge))
                                _habbo.GetBadgeComponent().GiveBadge(SubData.Badge, true, this);
                        }
                    }

                    if (!PlusEnvironment.GetGame().GetCacheManager().ContainsUser(_habbo.Id))
                        PlusEnvironment.GetGame().GetCacheManager().GenerateUser(_habbo.Id);

                    _habbo.InitProcess();
          
                    if (userData.user.GetPermissions().HasRight("mod_tickets"))
                    {
                        SendMessage(new ModeratorInitComposer(
                          PlusEnvironment.GetGame().GetModerationManager().UserMessagePresets,
                          PlusEnvironment.GetGame().GetModerationManager().RoomMessagePresets,
                          PlusEnvironment.GetGame().GetModerationManager().UserActionPresets,
                          PlusEnvironment.GetGame().GetModerationTool().GetTickets));
                    }

                    if (!string.IsNullOrWhiteSpace(PlusEnvironment.GetDBConfig().DBData["welcome_message"]))
                    {
                        this.SendMessage(new SuperNotificationComposer("welcome", "Bienvenid@", PlusEnvironment.GetDBConfig().DBData["welcome_message"], "Entendido", "event:toolbar/highlight/inventory"));
                    }
                    /* This is the habbopages welcome page pop-up. Too cumbersome atm.
                    ServerPacket notif = new ServerPacket(ServerPacketHeader.NuxAlertMessageComposer);
                    notif.WriteString("habbopages/bienvenida.txt");
                    SendMessage(notif);
                    */

                    /*
                                    var nuxStatuss = new ServerPacket(ServerPacketHeader.NuxUserStatus);
                                    nuxStatuss.WriteInteger(2);
                                    SendMessage(nuxStatuss);
                                    SendMessage(new NuxAlertMessageComposer("nux/lobbyoffer/hide"));
                                    ServerPacket nuxshow = new ServerPacket(ServerPacketHeader.NuxAlertMessageComposer);
                                    nuxshow.WriteString("nux/lobbyoffer/show");
                                    SendMessage(nuxshow);
                                    */

                    if (PlusEnvironment.GetDBConfig().DBData["targeted_offers_enabled"] == "1")
                            this.SendMessage(new TargetedOffersComposer());
                    //this.SendMessage(new CampaignCalendarDataComposer(this.GetHabbo().GetStats().openedGifts));
                    PlusEnvironment.GetGame().GetRewardManager().CheckRewards(this);

                    ICollection<MessengerBuddy> Friends = new List<MessengerBuddy>();
                    foreach (MessengerBuddy Buddy in this.GetHabbo().GetMessenger().GetFriends().ToList())
                    {
                        if (Buddy == null)
                            continue;

                        GameClient Friend = PlusEnvironment.GetGame().GetClientManager().GetClientByUserID(Buddy.Id);
                        if (Friend == null)
                            continue;
                        string figure = this.GetHabbo().Look;
                        
                        
                        Friend.SendMessage(new RoomNotificationComposer("fig/" + figure , 3, this.GetHabbo().Username + " se ha conectado", ""));
                        
                    }

                    // RP
                    this.GetPlay().StaffLoginTime = PlusEnvironment.GetUnixTimestamp();
                    EventManager.TriggerEvent("OnLogin", this);

                    return true;
                }
            }
            catch (Exception e)
            {
                Logging.LogCriticalException("Bug during user login: " + e);
            }
            return false;
        }

        public void SendWhisper(string Message, int Colour = 0)
        {
            if (this == null || GetHabbo() == null || GetHabbo().CurrentRoom == null)
                return;

            RoomUser User = GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(GetHabbo().Username);
            if (User == null)
                return;

            SendMessage(new WhisperComposer(User.VirtualId, Message, 0, (Colour == 0 ? User.LastBubble : Colour)));
        }

        internal void SendNotifWithScroll(string Message)
        {
            SendMessage(new MOTDNotificationComposer(Message));
        }

        public void SendNotification(string Message)
        {
            SendMessage(new BroadcastMessageAlertComposer(Message));
        }

        public void SendMessage(IServerPacket Message)
        {
            byte[] bytes = Message.GetBytes();

            if (Message == null)
                return;

            if (GetConnection() == null)
                return;

            GetConnection().SendData(bytes);
        }

        public int ConnectionID
        {
            get { return _id; }
        }

        public ConnectionInformation GetConnection()
        {
            return _connection;
        }

        public Habbo GetHabbo()
        {
            return _habbo;
        }
        // RP
        public RoleplayUser GetPlay()
        {
            return _roleplay;
        }

        public RoomUser GetRoomUser()
        {
            RoomUser RUser = null;
            try
            {
                if (this == null || GetHabbo() == null || GetHabbo().CurrentRoom == null)
                    return null;

                RUser = GetHabbo().CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(GetHabbo().Id);
            }
            catch
            {
                return RUser;
            }

            return RUser;
        }
        public void Disconnect()
        {
            // RP
            if (LoggingOut)
                return;

            LoggingOut = true;

            try
            {
                if (GetHabbo() != null)
                {
                    ICollection<MessengerBuddy> Friends = new List<MessengerBuddy>();
                    foreach (MessengerBuddy Buddy in this.GetHabbo().GetMessenger().GetFriends().ToList())
                    {
                        if (Buddy == null)
                            continue;

                        GameClient Friend = PlusEnvironment.GetGame().GetClientManager().GetClientByUserID(Buddy.Id);
                        if (Friend == null)
                            continue;
                        string figure = (this.GetHabbo() != null) ? this.GetHabbo().Look : "";

                        
                        
                        Friend.SendMessage(new RoomNotificationComposer("fig/" + figure, 3, this.GetHabbo().Username + " se ha desconectado", ""));

                    }

                    using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.RunQuery(GetHabbo().GetQueryString);
                    }

                    // RP
                    EventManager.TriggerEvent("OnDisconnect", this);

                    if (GetPlay() != null)
                    {
                        if (GetPlay().UserDataHandler != null)
                        {
                            GetPlay().UserDataHandler.SaveData();
                            GetPlay().UserDataHandler = null;
                        }

                        PlusEnvironment.GetGame().GetClientManager().UnregisterClientPhone(this.GetHabbo().Id, GetPlay().PhoneNumber);

                        #region If Have Weapon
                        if (GetPlay().EquippedWeapon != null)
                        {
                            RoleplayManager.UpdateMyWeaponStats(this, "bullets", GetPlay().Bullets, GetPlay().EquippedWeapon.Name);
                            RoleplayManager.UpdateMyWeaponStats(this, "life", GetPlay().WLife, GetPlay().EquippedWeapon.Name);
                        }
                        #endregion

                        #region CheckDriving OFF
                        /*
                        if (GetPlay().DrivingCar)
                        {
                            #region Vehicle Check
                            Vehicle vehicle = null;
                            int corp = 0;
                            bool ToDB = true;
                            foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                            {
                                if (GetPlay().CarEnableId == Convert.ToInt32(Vehicle.EffectID))
                                {
                                    vehicle = Vehicle;
                                    if (vehicle.CarCorp > 0)
                                    {
                                        corp = vehicle.CarCorp;
                                        ToDB = false;
                                    }
                                }
                            }
                            #endregion

                            int ItemPlaceId = 0;
                            int roomid = GetRoomUser().RoomId;
                            Room Room = RoleplayManager.GenerateRoom(roomid, false);

                            if (GetPlay().DrivingInCar || !RoleplayManager.isValidPark(Room, GetRoomUser().Coordinate))
                            {
                                RoleplayManager.Shout(this, "* Una Grúa se ha llevado el vehículo que " + GetHabbo().Username + " conducía por encontrarse mal estacionado.", 4);
                                // Actualizamos datos del auto en el diccionario y DB
                                PlusEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(this, 0, ToDB, out VehiclesOwned VOD);
                                if (VOD != null)
                                    ItemPlaceId = VOD.Id;

                                if (corp > 0)
                                {
                                    PlusEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(GetPlay().DrivingCarId);
                                    RoleplayManager.CheckCorpCarp(this);
                                }

                            }
                            else if (GetPlay().DrivingCar)
                            {
                                if (corp > 0)
                                {
                                    RoleplayManager.Shout(this, "* Una Grúa se ha llevado el vehículo que " + GetHabbo().Username + " conducía por encontrarse mal estacionado.", 4);
                                    PlusEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(GetPlay().DrivingCarId);
                                    RoleplayManager.CheckCorpCarp(this);
                                }
                                else
                                {
                                    // Colocamos Furni en Sala
                                    GetPlay().isParking = true;
                                    Item ItemPlace = RoleplayManager.PlaceItemToRoom(this, vehicle.ItemID, 0, GetRoomUser().X, GetRoomUser().Y, GetRoomUser().Z, GetRoomUser().RotBody, false, Room.Id, ToDB, "");
                                    GetPlay().isParking = false;
                                    ItemPlaceId = ItemPlace.Id;
                                    // Actualizamos datos del auto en el diccionario y DB
                                    PlusEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwner(this, ItemPlaceId, ToDB, out VehiclesOwned VOD);
                                }
                            }

                            #region Extra Conditions & Checks
                            #region CorpCar Respawn
                            if (corp > 0)
                            {
                                GetPlay().CarJobLastItemId = ItemPlaceId;
                            }
                            #endregion

                            #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                            //Vars
                            string Pasajeros = GetPlay().Pasajeros;
                            string[] stringSeparators = new string[] { ";" };
                            string[] result;
                            result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string psjs in result)
                            {
                                GameClient PJ = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                if (PJ != null)
                                {
                                    if (PJ.GetPlay().ChoferName == GetHabbo().Username)
                                    {
                                        RoleplayManager.Shout(PJ, "*Baja del vehículo de " + GetHabbo().Username + "*", 5);
                                    }
                                    // PASAJERO
                                    PJ.GetPlay().Pasajero = false;
                                    PJ.GetPlay().ChoferName = "";
                                    PJ.GetPlay().ChoferID = 0;
                                    PJ.GetRoomUser().CanWalk = true;
                                    PJ.GetRoomUser().FastWalking = false;
                                    PJ.GetRoomUser().TeleportEnabled = false;
                                    PJ.GetRoomUser().AllowOverride = false;

                                    // Descontamos Pasajero
                                    GetPlay().PasajerosCount--;
                                    StringBuilder builder = new StringBuilder(GetPlay().Pasajeros);
                                    builder.Replace(PJ.GetHabbo().Username + ";", "");
                                    GetPlay().Pasajeros = builder.ToString();

                                    // CHOFER 
                                    GetPlay().Chofer = (GetPlay().PasajerosCount <= 0) ? false : true;
                                    GetRoomUser().AllowOverride = (GetPlay().PasajerosCount <= 0) ? false : true;

                                    // SI EL PASAJERO ES UN HERIDO
                                    if (PJ.GetPlay().IsDying)
                                    {
                                        #region Send To Hospital
                                        RoleplayManager.Shout(PJ, "*Pierde el conocimiento y es trasladad@ al hospital*", 5);
                                        Room Room2 = RoleplayManager.GenerateRoom(PJ.GetRoomUser().RoomId);
                                        string MyCity2 = Room2.City;

                                        PlayRoom Data2;
                                        int ToHosp2 = PlusEnvironment.GetGame().GetPlayRoomManager().TryToGetHospital(MyCity2, out Data2);

                                        if (ToHosp2 > 0)
                                        {
                                            Room Room3 = RoleplayManager.GenerateRoom(ToHosp2);
                                            if (Room3 != null)
                                            {
                                                PJ.GetPlay().IsDead = true;
                                                PJ.GetPlay().DeadTimeLeft = RoleplayManager.DeathTime;

                                                PJ.GetHabbo().HomeRoom = ToHosp2;

                                                /*
                                                if (PJ.GetHabbo().CurrentRoomId != ToHosp)
                                                    RoleplayManager.SendUserTimer(Client, ToHosp, "", "death");
                                                else
                                                    GetPlay().TimerManager.CreateTimer("death", 1000, true);
                                                * /
                                                RoleplayManager.SendUserTimer(PJ, ToHosp2, "", "death");
                                            }
                                            else
                                            {
                                                PJ.SendNotification("[Error][102] -> Lamentablemente ha habido un error. No se encontró ningún Hospital disponible en esta ciudad. Comunícaselo a un Administrador. ¡Gracias!");
                                                PJ.GetPlay().CurHealth = PJ.GetPlay().MaxHealth;
                                                PJ.GetPlay().RefreshStatDialogue();
                                                PJ.GetRoomUser().Frozen = false;
                                                PJ.SendWhisper("Se te ha revivido a causa que no hay ningún hospital en esta Ciudad");
                                            }
                                        }
                                        else
                                        {
                                            PJ.SendNotification("[Error][103] -> Lamentablemente ha habido un error. No se encontró ningún Hospital disponible en esta ciudad. Comunícaselo a un Administrador. ¡Gracias!");
                                            PJ.GetPlay().CurHealth = PJ.GetPlay().MaxHealth;
                                            PJ.GetPlay().RefreshStatDialogue();
                                            PJ.GetRoomUser().Frozen = false;
                                            PJ.SendWhisper("Se te ha revivido a causa que no hay ningún hospital en esta Ciudad");
                                        }
                                        PJ.GetPlay().IsDying = false;
                                        PJ.GetPlay().DyingTimeLeft = 0;
                                        #endregion
                                    }
                                }
                            }
                            #endregion

                            #region Check Jobs

                            #region Taxista
                            if (GetPlay().Ficha != 0)
                            {
                                GetPlay().Ficha = 0;
                                GetPlay().FichaTimer = 0;
                                RoleplayManager.Shout(this, "*Apaga el Taxímetro y deja de trabajar*", 5);
                            }
                            #endregion

                            #region Basurero
                            if (GetPlay().BasuTeamId <= 0)
                                GetPlay().IsBasuChofer = false;
                            #endregion

                            #endregion
                            #endregion

                            #region Online ParkVars
                            //Retornamos a valores predeterminados
                            GetPlay().DrivingCar = false;
                            GetPlay().DrivingInCar = false;
                            GetPlay().DrivingCarId = 0;// Id de VehiclesOwned;

                            //Combustible System
                            GetPlay().CarType = 0;// Define el gasto de combustible
                            GetPlay().CarFuel = 0;
                            GetPlay().CarMaxFuel = 0;
                            GetPlay().CarTimer = 0;
                            GetPlay().CarLife = 0;

                            GetPlay().CarEnableId = 0;//Coloca el enable para conducir
                            GetPlay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                            GetRoomUser().ApplyEffect(0);
                            GetRoomUser().FastWalking = false;
                            #endregion
                        }
                        */
                        #endregion
                    }                    

                    // Repeat this cuz' de Roleplay conditions can modify some vars from GetHabbo().
                    using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                    {
                        dbClient.RunQuery(GetHabbo().GetQueryString);
                    }

                    GetHabbo().OnDisconnect();
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e.ToString());
            }


            if (!_disconnected)
            {
                if (_connection != null)
                    _connection.Dispose();
                _disconnected = true;
            }
        }

        public void Dispose()
        {
            if (GetHabbo() != null)
                Disconnect();
                //GetHabbo().OnDisconnect();

            this.MachineId = string.Empty;
            this._disconnected = true;
            this._habbo = null;
            this._connection = null;
            this.RC4Client = null;
            this._packetParser = null;
        }
    }
}