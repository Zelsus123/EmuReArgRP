﻿using System;
using System.Collections.Generic;
using System.Text;
using ConnectionManager;

using Plus.Core;
using Plus.HabboHotel.Users.Messenger;


using System.Linq;
using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing;

using log4net;
using System.Data;
using Plus.Communication.Packets.Outgoing.Notifications;
using Plus.Database.Interfaces;
using System.Collections;
using Plus.Communication.Packets.Outgoing.Handshake;
using System.Diagnostics;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using System.Text.RegularExpressions;
using System.Threading;

namespace Plus.HabboHotel.GameClients
{
    public class GameClientManager
    {
        private static readonly ILog log = LogManager.GetLogger("Plus.HabboHotel.GameClients.GameClientManager");

        public ConcurrentDictionary<int, GameClient> _clients;
        private ConcurrentDictionary<int, GameClient> _userIDRegister;
        private ConcurrentDictionary<string, GameClient> _usernameRegister;
        private ConcurrentDictionary<string, GameClient> _usernameRegisterPhone;

        private readonly Queue timedOutConnections;

        private readonly Stopwatch clientPingStopwatch;

        public GameClientManager()
        {
            this._clients = new ConcurrentDictionary<int, GameClient>();
            this._userIDRegister = new ConcurrentDictionary<int, GameClient>();
            this._usernameRegister = new ConcurrentDictionary<string, GameClient>();
            this._usernameRegisterPhone = new ConcurrentDictionary<string, GameClient>();

            timedOutConnections = new Queue();

            clientPingStopwatch = new Stopwatch();
            clientPingStopwatch.Start();
        }

        public void OnCycle()
        {
            TestClientConnections();
            HandleTimeouts();
        }

        public GameClient GetClientByIp(string ip_last)
        {
            return null;
            //return this._clients.Values.FirstOrDefault(x => x.GetHabbo().IpLast == ip_last);
        }

        public GameClient GetClientByUserID(int userID)
        {
            if (_userIDRegister.ContainsKey(userID))
                return _userIDRegister[userID];
            return null;
        }

        public GameClient GetClientByUsername(string username)
        {
            if (_usernameRegister.ContainsKey(username.ToLower()))
                return _usernameRegister[username.ToLower()];
            return null;
        }

        public GameClient GetClientByPhoneNumber(string number)
        {
            if (_usernameRegisterPhone.ContainsKey(number.ToLower()))
                return _usernameRegisterPhone[number.ToLower()];
            return null;
        }

        public bool TryGetClient(int ClientId, out GameClient Client)
        {
            return this._clients.TryGetValue(ClientId, out Client);
        }

        public bool UpdateClientUsername(GameClient Client, string OldUsername, string NewUsername)
        {
            if (Client == null || !_usernameRegister.ContainsKey(OldUsername.ToLower()))
                return false;

            _usernameRegister.TryRemove(OldUsername.ToLower(), out Client);
            _usernameRegister.TryAdd(NewUsername.ToLower(), Client);
            return true;
        }

        public int GetIdByName(string username)
        {
            GameClient client = GetClientByUsername(username);

            if (client != null && client.GetHabbo() != null)
                return client.GetHabbo().Id;

            int id;
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT id FROM users WHERE username = @username LIMIT 1");
                dbClient.AddParameter("username", username);
                id = dbClient.getInteger();
            }

            return id;
        }

        public string GetNameById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null && client.GetHabbo() != null)
                return client.GetHabbo().Username;

            string username;
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT username FROM users WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                username = dbClient.getString();
            }

            return username;
        }

        public string GetLookById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null && client.GetHabbo() != null)
                return client.GetHabbo().Look;

            string look;
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT look FROM users WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                look = dbClient.getString();
            }

            return look;
        }

        public string GetNumberById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null)
                return client.GetPlay().PhoneNumber;

            string number;
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT phone_number FROM play_phones_owned WHERE user_id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                number = dbClient.getString();
            }

            return number;
        }

        public int GetVipById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null)
                return client.GetHabbo().VIPRank;

            int viptype;
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT rank_vip FROM users WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                viptype = dbClient.getInteger();
            }

            return viptype;
        }

        public int GetLevelById(int Id)
        {
            GameClient client = GetClientByUserID(Id);

            if (client != null)
                return client.GetPlay().Level;

            int level;
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT level FROM play_stats WHERE id = @id LIMIT 1");
                dbClient.AddParameter("id", Id);
                level = dbClient.getInteger();
            }

            return level;
        }

        public IEnumerable<GameClient> GetClientsById(Dictionary<int, MessengerBuddy>.KeyCollection users)
        {
            foreach (int id in users)
            {
                GameClient client = GetClientByUserID(id);
                if (client != null)
                    yield return client;
            }
        }

        public void StaffAlert(ServerPacket Message, int Exclude = 0)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client.GetHabbo().Rank < 4 || client.GetHabbo().Id == Exclude)
                    continue;

                client.SendMessage(Message);
            }
        }

        public void StaffAlertMsg(string Message, int Exclude = 0)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client.GetHabbo().Rank < 3 || client.GetHabbo().Id == Exclude)
                    continue;

                client.SendWhisper(Message, 1);
            }
        }

        public void StaffRadioAlert(string Message, GameClient Session, int Exclude = 0)
        {
            string re = "";

            if (Session != null)
            {
                re = "[" + Session.GetHabbo().Username + "]";
            }

            // Enviar mensaje a todos los staff conectados
            foreach (GameClient client in PlusEnvironment.GetGame().GetClientManager().GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                // Verificar que el cliente sea Staff (Rank >= 3) y no sea el emisor excluido
                if (client.GetHabbo().Rank < 3)
                    continue;

                // Verificar si el radio está deshabilitado para el cliente

                // Enviar el mensaje de radio a los staff conectados
                client.SendWhisper("[Radio Gubernamental] " + re + ": " + Message, 23);
            }
        }

        public void RadioAlert(string Message, GameClient Session, bool RadioPolice = false, bool RadioMedic = false)
        {
            string cmd = (RadioPolice) ? "radio" : (RadioMedic) ? "radio_medic" : "radio_gang";
            string re = "";

            if (Session != null)
            {
                re = "[" + Session.GetHabbo().Username + "]";

                if (!Session.GetHabbo().GetPermissions().HasRight("advertisement_filter_override"))
                {
                    string Phrase = "";
                    if (PlusEnvironment.GetGame().GetChatManager().GetFilter().CheckBannedWords(Message, out Phrase))
                    {
                        Session.GetHabbo().AdvertisingStrikes++;

                        if (Session.GetHabbo().AdvertisingStrikes < 2)
                        {
                            Session.SendMessage(new RoomNotificationComposer("¡Aviso!", "Abstente de anunciar otros sitios web que no estén respaldados, afiliados u ofrecidos por nuestro servidor. ¡Se te silenciará si lo vuelves a hacer!<br><br>Palabras en la Lista Negra: '" + Phrase + "'", "frank10", "ok", "event:"));
                            return;
                        }

                        if (Session.GetHabbo().AdvertisingStrikes >= 2)
                        {
                            Session.GetHabbo().TimeMuted = 3600;

                            using (var dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.RunQuery("UPDATE `users` SET `time_muted` = '3600' WHERE `id` = '" + Session.GetHabbo().Id + "' LIMIT 1");
                            }

                            Session.SendMessage(new RoomNotificationComposer("¡Estás mutead@!", "Lo sentimos, te hemos mutueado por escribir '" + Phrase + "'.<br><br>¡El equipo de Moderación fue alertado y podrían tomar acciones con tu cuenta!", "frank10", "ok", "event:"));

                            List<string> Messages = new List<string>();
                            Messages.Add(Message);
                            PlusEnvironment.GetGame().GetModerationTool().SendNewTicket(Session, 9, Session.GetHabbo().Id, "[Server] Ciudadano atrapado escribiendo: " + Phrase + ".", Messages);
                            return;
                        }

                        return;
                    }
                }

                if (!Session.GetHabbo().GetPermissions().HasRight("word_filter_override"))
                    Message = PlusEnvironment.GetGame().GetChatManager().GetFilter().CheckMessage(Message);
            }

            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (!cmd.Equals("radio_gang"))
                {
                    if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(client, cmd) && !client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                        continue;
                }
                else
                {
                    List<Groups.Group> MyGang = PlusEnvironment.GetGame().GetGroupManager().GetGangsForUser(Session.GetHabbo().Id);
                    List<Groups.Group> thegroup = PlusEnvironment.GetGame().GetGroupManager().GetGangsForUser(client.GetHabbo().Id);

                    if (thegroup == null || thegroup.Count <= 0)
                        continue;

                    if (thegroup[0] != MyGang[0])
                        continue;
                }

                if (client.GetPlay().DisableRadio == true)
                    continue;

                client.SendWhisper("[RADIO]" + re + ": " + Message, 30);
            }
        }

        public void sendWorkAlert(string Message, string work, bool uniform = false)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (uniform)
                {
                    if (!client.GetPlay().IsWorking)
                        continue;
                }

                if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(client, work))
                    continue;

                if (client.GetPlay().DisableRadio == true)
                    continue;

                client.SendWhisper("[SERVICIO]" + Message, 1);
            }
        }

        public void JailAlert(string Message)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(client, "radio") && !client.GetHabbo().GetPermissions().HasRight("corporation_rights"))
                    continue;

                if (client.GetPlay().DisableRadio)
                    continue;

                client.SendWhisper(Message, 30);
                //client.SendMessage(new RoomNotificationComposer("police_announcement", "message", Message.Replace("[RADIO Alert] ", "")));
            }
        }

        public void ModAlert(string Message)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                if (client.GetHabbo().GetPermissions().HasRight("mod_tool") && !client.GetHabbo().GetPermissions().HasRight("staff_ignore_mod_alert"))
                {
                    try { client.SendWhisper(Message, 5); }
                    catch { }
                }
            }
        }

        public void CanalN(string Message, GameClient Session)
        {
            if (!Session.GetHabbo().GetPermissions().HasRight("advertisement_filter_override"))
            {
                string Phrase = "";
                if (PlusEnvironment.GetGame().GetChatManager().GetFilter().CheckBannedWords(Message, out Phrase))
                {
                    Session.GetHabbo().AdvertisingStrikes++;

                    if (Session.GetHabbo().AdvertisingStrikes < 2)
                    {
                        Session.SendMessage(new RoomNotificationComposer("¡Aviso!", "Abstente de anunciar otros sitios web que no estén respaldados, afiliados u ofrecidos por nuestro servidor. ¡Se te silenciará si lo vuelves a hacer!<br><br>Palabras en la Lista Negra: '" + Phrase + "'", "frank10", "ok", "event:"));
                        return;
                    }

                    if (Session.GetHabbo().AdvertisingStrikes >= 2)
                    {
                        Session.GetHabbo().TimeMuted = 3600;

                        using (var dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.RunQuery("UPDATE `users` SET `time_muted` = '3600' WHERE `id` = '" + Session.GetHabbo().Id + "' LIMIT 1");
                        }

                        Session.SendMessage(new RoomNotificationComposer("¡Estás mutead@!", "Lo sentimos, te hemos mutueado por escribir '" + Phrase + "'.<br><br>¡El equipo de Moderación fue alertado y podrían tomar acciones con tu cuenta!", "frank10", "ok", "event:"));

                        List<string> Messages = new List<string>();
                        Messages.Add(Message);
                        PlusEnvironment.GetGame().GetModerationTool().SendNewTicket(Session, 9, Session.GetHabbo().Id, "[Server] Ciudadano atrapado escribiendo: " + Phrase + ".", Messages);
                        return;
                    }

                    return;
                }
            }

            if (!Session.GetHabbo().GetPermissions().HasRight("word_filter_override"))
                Message = PlusEnvironment.GetGame().GetChatManager().GetFilter().CheckMessage(Message);

            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                
                if (client.GetPlay().ChNDisabled)
                    continue;
                
                string Label = "";

                #region Set Label
                if (Session.GetHabbo().Rank == 3)
                    Label = "[AYUDANTE]";
                else if (Session.GetHabbo().Rank == 4)
                    Label = "[MOD]";
                else if (Session.GetHabbo().Rank == 5)
                    Label = "[ADM]";
                else if (Session.GetHabbo().Rank > 5)
                    Label = "[DEV]";
                #endregion

                client.SendWhisper("[Canal :n]"+ Label +"[" + Session.GetHabbo().Username + "][Nivel: " + Session.GetPlay().Level + "]: " + Message, 34);
            }
        }

        public void AlertMessage(string Message, string Label)
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null || client.GetHabbo() == null)
                    continue;

                client.SendWhisper(Label + Message, 34);
            }
        }

        public void DoAdvertisingReport(GameClient Reporter, GameClient Target)
        {
            if (Reporter == null || Target == null || Reporter.GetHabbo() == null || Target.GetHabbo() == null)
                return;

            StringBuilder Builder = new StringBuilder();
            Builder.Append("New report submitted!\r\r");
            Builder.Append("Reporter: " + Reporter.GetHabbo().Username + "\r");
            Builder.Append("Reported User: " + Target.GetHabbo().Username + "\r\r");
            Builder.Append(Target.GetHabbo().Username + "s last 10 messages:\r\r");

            DataTable GetLogs = null;
            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("SELECT `message` FROM `chatlogs` WHERE `user_id` = '" + Target.GetHabbo().Id + "' ORDER BY `id` DESC LIMIT 10");
                GetLogs = dbClient.getTable();

                if (GetLogs != null)
                {
                    int Number = 11;
                    foreach (DataRow Log in GetLogs.Rows)
                    {
                        Number -= 1;
                        Builder.Append(Number + ": " + Convert.ToString(Log["message"]) + "\r");
                    }
                }
            }

            foreach (GameClient Client in this.GetClients.ToList())
            {
                if (Client == null || Client.GetHabbo() == null)
                    continue;

                if (Client.GetHabbo().GetPermissions().HasRight("mod_tool") && !Client.GetHabbo().GetPermissions().HasRight("staff_ignore_advertisement_reports"))
                    Client.SendMessage(new MOTDNotificationComposer(Builder.ToString()));
            }
        }


        public void SendMessage(ServerPacket Packet, string fuse = "")
        {
            foreach (GameClient Client in this._clients.Values.ToList())
            {
                if (Client == null || Client.GetHabbo() == null)
                    continue;

                if (!string.IsNullOrEmpty(fuse))
                {
                    if (!Client.GetHabbo().GetPermissions().HasRight(fuse))
                        continue;
                }

                Client.SendMessage(Packet);
            }
        }

        public void CreateAndStartClient(int clientID, ConnectionInformation connection)
        {
            GameClient Client = new GameClient(clientID, connection);
            if (this._clients.TryAdd(Client.ConnectionID, Client))
                Client.StartConnection();
            else
                connection.Dispose();
        }

        public void DisposeConnection(int clientID)
        {
            GameClient Client = null;
            if (!TryGetClient(clientID, out Client))
                return;

            if (Client != null)
                Client.Dispose(clientID);
        }

        public void removeConnection(int clientID)
        {
            GameClient Client = null;
            this._clients.TryRemove(clientID, out Client);
        }

        public void LogClonesOut(int UserID)
        {
            GameClient client = GetClientByUserID(UserID);
            if (client != null)
                client.Disconnect();
        }

        public void LogClonesByIpOut(string ip_last)
        {
            GameClient client = GetClientByIp(ip_last);
            if (client != null)
            {
                new Thread(() =>
                {
                    client.SendNotification("Se ha detectado la conexión de otro usuario con tu misma IP. Se te ha desconectado del servidor...");
                    Thread.Sleep(3000);
                    client.Disconnect();
                }).Start();
            }
        }

        public void RegisterClient(GameClient client, int userID, string username, string ip_last)
        {
            if (_usernameRegister.ContainsKey(username.ToLower()))
                _usernameRegister[username.ToLower()] = client;
            else
                _usernameRegister.TryAdd(username.ToLower(), client);

            if (_userIDRegister.ContainsKey(userID))
                _userIDRegister[userID] = client;
            else
                _userIDRegister.TryAdd(userID, client);
        }

        public void RegisterClientPhone(GameClient client, int userID, string number)
        {
            if (_usernameRegisterPhone.ContainsKey(number.ToLower()))
                _usernameRegisterPhone[number.ToLower()] = client;
            else
                _usernameRegisterPhone.TryAdd(number.ToLower(), client);

            if (_userIDRegister.ContainsKey(userID))
                _userIDRegister[userID] = client;
            else
                _userIDRegister.TryAdd(userID, client);
        }

        public void UnregisterClient(int userid, string username, string ip_last)
        {
            GameClient Client = null;
            _userIDRegister.TryRemove(userid, out Client);
            _usernameRegister.TryRemove(username.ToLower(), out Client);
        }

        public void UnregisterClientPhone(int userid, string number)
        {
            GameClient Client = null;
            _userIDRegister.TryRemove(userid, out Client);
            _usernameRegisterPhone.TryRemove(number.ToLower(), out Client);
        }

        public void CloseAll()
        {
            foreach (GameClient client in this.GetClients.ToList())
            {
                if (client == null)
                    continue;

                if (client.GetHabbo() != null)
                {
                    try
                    {
                        using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                        {
                            dbClient.RunQuery(client.GetHabbo().GetQueryString);
                        }
                        Console.Clear();
                        log.Info("<<- SERVER SHUTDOWN ->> IVNENTORY IS SAVING");
                    }
                    catch
                    {
                    }
                }
            }

            log.Info("Done saving users inventory!");
            log.Info("Closing server connections...");
            try
            {
                foreach (GameClient client in this.GetClients.ToList())
                {
                    if (client == null || client.GetConnection() == null)
                        continue;

                    try
                    {
                        client.GetConnection().Dispose();
                    }
                    catch { }

                    Console.Clear();
                    log.Info("<<- SERVER SHUTDOWN ->> CLOSING CONNECTIONS");

                }
            }
            catch (Exception e)
            {
                Logging.LogCriticalException(e.ToString());
            }

            if (this._clients.Count > 0)
                this._clients.Clear();

            log.Info("Connections closed!");
        }

        private void TestClientConnections()
        {
            if (clientPingStopwatch.ElapsedMilliseconds >= 30000)
            {
                clientPingStopwatch.Restart();

                try
                {
                    List<GameClient> ToPing = new List<GameClient>();

                    foreach (GameClient client in this._clients.Values.ToList())
                    {
                        if (client.PingCount < 6)
                        {
                            client.PingCount++;

                            ToPing.Add(client);
                        }
                        else
                        {
                            lock (timedOutConnections.SyncRoot)
                            {
                                timedOutConnections.Enqueue(client);
                            }
                        }
                    }

                    DateTime start = DateTime.Now;

                    foreach (GameClient Client in ToPing.ToList())
                    {
                        try
                        {
                            Client.SendMessage(new PongComposer());
                        }
                        catch
                        {
                            lock (timedOutConnections.SyncRoot)
                            {
                                timedOutConnections.Enqueue(Client);
                            }
                        }
                    }

                }
                catch (Exception e)
                {

                }
            }
        }

        private void HandleTimeouts()
        {
            if (timedOutConnections.Count > 0)
            {
                lock (timedOutConnections.SyncRoot)
                {
                    while (timedOutConnections.Count > 0)
                    {
                        GameClient client = null;

                        if (timedOutConnections.Count > 0)
                            client = (GameClient)timedOutConnections.Dequeue();

                        if (client != null)
                            client.Disconnect();
                    }
                }
            }
        }

        public int Count
        {
            get { return this._clients.Count; }
        }

        public ICollection<GameClient> GetClients
        {
            get
            {
                return this._clients.Values;
            }
        }

        public string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, @"[^0-9A-Za-z]", "", RegexOptions.None);
        }

        public string ClearNumbers(string str)
        {
            return Regex.Replace(str, @"[^0-9]", "", RegexOptions.None);
        }
        public string NumberFormatRP(string number)
        {
            // (xxx)-xxx-xxxx
            return "("+number.Substring(0,3)+ ")-" + number.Substring(3, 3) + "-" + number.Substring(6, 4);
        }

        public string TimeFormat(int time)
        {
            string hr, min;
            min = Convert.ToString(time % 100);
            hr = Convert.ToString(time / 100);
            if (hr.Length == 1) hr = "0" + hr;
            if (min.Length == 1) min = "0" + min;
            return hr + ":" + min;
        }
    }
}