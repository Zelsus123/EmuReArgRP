﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fleck;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using System.IO;
using Plus.HabboRoleplay.Misc;
using Plus.Communication.Packets.Incoming.Groups;
using Plus.Communication.Packets.Outgoing;
using Plus.Communication.Packets.Incoming;
using Plus.Communication.Packets.Outgoing.Groups;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Messenger;
using System.Collections.Generic;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Cache;
using Plus.Communication.Packets.Outgoing.Rooms.Permissions;
using Plus.Database.Interfaces;
using System.Text.RegularExpressions;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.HabboRoleplay.Vehicles;
using Plus.HabboRoleplay.PhoneChat;
using System.Data;
using Plus.HabboHotel.Users.Messenger;
using Plus.Utilities;
using Plus.HabboHotel.Quests;
using Plus.Communication.Packets.Outgoing.Users;
using Plus.HabboRoleplay.Phones;
using Plus.HabboRoleplay.PhoneOwned;
using Plus.HabboRoleplay.PhoneAppOwned;
using Plus.HabboRoleplay.PhonesApps;
using System.Web;
using Plus.Communication.Packets.Incoming.Inventory.Purse;
using Plus.HabboRoleplay.API;
using Plus.HabboHotel.RolePlay.PlayInternet;

namespace Plus.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// PhoneWebEvent class.
    /// </summary>
    class PhoneWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, IWebSocketConnection Socket)
        {

            if (!PlusEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PlusEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;
            
            /*
            if (Client.GetPlay().Phone == 0)
            {
                Client.SendNotification("No tienes ningún teléfono comprado para hacer eso.");
                return;
            }
            /*
            if (!Client.GetPlay().UsingPhone)
            {
                Client.SendNotification("Buen intento, tratando de injectar el systema, abre el teléfono.");
                return;
            }
            */

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);

            if (Action != "show_button" && Action != "load_apps" && Client.GetPlay().IsSanc)
            {
                Client.SendNotification("No tienes permitido usar el teléfono mientras estás sancionad@.");
                return;
            }

            switch (Action)
            {
                #region Close Shop
                case "close_shop":
                    {
                        Client.GetPlay().ViewShopPhones = false;
                        Socket.Send("compose_phone|close_shop|");
                    }
                    break;
                #endregion

                #region Buy Phone
                case "buy_phone":
                    {
                        if (Client.GetPlay().TryGetCooldown("buy"))
                            return;

                        Room Room = RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId);
                        if (Room == null)
                            return;

                        #region Conditions
                        if (!Room.PhoneStoreEnabled)
                        {
                            Client.SendWhisper("Debes ir a una Tienda de Teléfonos.", 1);
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes acercarte al mostrador para comprar un teléfono.", 1);
                            return;
                        }
                        if (Client.GetPlay().InTutorial && Client.GetPlay().TutorialStep < 18)
                        {
                            Client.SendWhisper("¡Hey, no tan rápido! Ve siguiendo el Tutorial paso a paso para guiarte de la mejor manera.", 1);
                            return;
                        }
                        #endregion

                        #region Get WS Data
                        string[] ReceivedData = Data.Split(',');
                        int GetPhoneID;
                        string GetPhoneModel = ReceivedData[2];
                        if (!int.TryParse(ReceivedData[1], out GetPhoneID))
                        {
                            Socket.Send("compose_phone|shopmsg|Ha ocurrido un problema al obtener la Información del Teléfono.");
                            return;
                        }
                        GetPhoneModel = Regex.Replace(GetPhoneModel, "<(.|\\n)*?>", string.Empty);

                        Phone phone = PhoneManager.getPhone(GetPhoneModel);
                        if (phone == null)
                        {
                            Socket.Send("compose_phone|shopmsg|Ha ocurrido un problema al obtener la Información del Teléfono. [2]");
                            return;
                        }
                        
                        if (Client.GetPlay().PhoneModelId == phone.ID)
                        {
                            Socket.Send("compose_phone|shopmsg|¡Ya tienes comprado ese teléfono!");

                            #region Tutorial Step Check
                            if (Client.GetPlay().TutorialStep == 18 && Room.PhoneStoreEnabled && Room.Type.Equals("public"))
                            {
                                Client.GetPlay().TutorialStep = 21;
                                PlusEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|20");
                            }
                            #endregion
                            return;
                        }

                        if (Client.GetHabbo().Credits < phone.Price)
                        {
                            Socket.Send("compose_phone|shopmsg|No tienes dinero suficiente.");
                            return;
                        }
                        #endregion

                        #region Execute
                        String NewNumber = Client.GetPlay().PhoneNumber;
                        string NumberInfo = "Tu número para tu nuevo teléfono sigue siendo el mismo: " + NewNumber;
                        if (NewNumber.Length <= 0)
                        {
                            // Obtenemos Numero Random con Formato (xxx)-xxx-xxxx
                            NewNumber = RoleplayManager.GeneratePhoneNumber(Client.GetHabbo().Id);
                            NumberInfo = "Tu número es: " + NewNumber + ". ((Para volverlo a consultar usa :minumero))";

                            if (!PlusEnvironment.GetGame().GetPhonesOwnedManager().TryCreatePhoneOwned(Client, phone.ID, Client.GetHabbo().Id, NewNumber, out PhonesOwned nPO))
                            {
                                Socket.Send("compose_phone|shopmsg|No se pudo autorizar el registro de papeles para tu nuevo teléfono. Inténtalo de nuevo.");
                                return;
                            }

                            PlusEnvironment.GetGame().GetClientManager().RegisterClientPhone(Client.GetRoomUser().GetClient(), Client.GetHabbo().Id, NewNumber);

                            RoleplayManager.SetDefaultApps(Client);
                        }
                        else
                        {
                            if (!PlusEnvironment.GetGame().GetPhonesOwnedManager().UpdatePhoneOwner(Client, phone.ID, true, out PhonesOwned nPO))
                            {
                                Socket.Send("compose_phone|shopmsg|No se pudo autorizar el registro de papeles para tu nuevo teléfono. Inténtalo de nuevo. [2]");
                                return;
                            }
                        }

                        Client.GetHabbo().Credits -= phone.Price;
                        Client.GetHabbo().UpdateCreditsBalance();
                        RoleplayManager.Shout(Client, "*Compra un " + phone.DisplayName + " nuevo y paga $" + phone.Price + " por él*", 5);
                        Client.SendWhisper("Has comprado un " + phone.DisplayName + " y pagaste $" + phone.Price, 1);
                        Client.SendWhisper("Ahora podrás agregar contactos, enviar mensajes y realizar llamadas.", 1);
                        Client.SendWhisper(NumberInfo, 1);

                        Socket.Send("compose_phone|shopmsg_green|¡Has comprado un teléfono nuevo!");
                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "load_apps");
                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "show_button");
                        Client.GetPlay().CooldownManager.CreateCooldown("buy", 1000, 5);

                        #region Tutorial Step Check
                        if (Client.GetPlay().TutorialStep == 18 && Room.PhoneStoreEnabled && Room.Type.Equals("public"))
                        {
                            Client.GetPlay().TutorialStep = 21;
                            PlusEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|20");
                        }
                        #endregion
                        #endregion
                    }
                    break;
                #endregion

                #region Open Shop Phone
                case "open_shop_phone":
                    {
                        if (Client.GetPlay().TryGetCooldown("openshopphone"))
                            return;

                        Room Room = RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId);
                        if (Room == null)
                            return;

                        #region Conditions
                        if (!Room.PhoneStoreEnabled)
                        {
                            Client.SendWhisper("Debes ir a una Tienda de Teléfonos.", 1);
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes acercarte al mostrador para comprar un teléfono.", 1);
                            return;
                        }
                        #endregion

                        if (!Client.GetPlay().ViewShopPhones)
                            Client.GetPlay().ViewShopPhones = true;

                        #region HTML
                        string html = "";
                        List<Phone> PO = PhoneManager.getAllPhones();
                        if(PO != null && PO.Count > 0)
                        {
                            foreach(var phone in PO)
                            {
                                html += "<div class=\"product\">";
                                html += "<div class=\"product-box\" style=\"background-image: url('"+RoleplayManager.CdnURL+ "/ws_overlays/Phones/resources/images/"+phone.ModelName+".png');background-position: center;background-repeat: no-repeat;background-size: 35px;\">";
                                html += "<center><b>"+phone.DisplayName+"</b></center>";
                                html += "</div>";
                                html += "<div class=\"product-footer\">";
                                html += "<div id=\"Prod_Price\">$ "+ String.Format("{0:N0}", phone.Price) + "</div>";
                                html += "<div style=\"margin-top:3px;margin-bottom: 3px;\"><hr></div>";
                                html += "<div id=\""+phone.ID+","+phone.ModelName+"\" class=\"shopphone myButton\">Comprar</div>";
                                html += "</div>";
                                html += "</div>";
                            }
                        }
                        #endregion

                        string SendData = "";
                        SendData += html;
                        Socket.Send("compose_phone|open_shop_phone|" + SendData);
                        Client.GetPlay().CooldownManager.CreateCooldown("openshopphone", 1000, 1);
                    }
                    break;
                #endregion

                #region Load Apps
                case "load_apps":
                    {
                        #region HTML
                        string html = "";
                        string html2 = "";
                        List<Phone> PH = PhoneManager.getPhoneById(Client.GetPlay().PhoneModelId);
                        if (PH != null)
                        {
                            int TotalScreens = 0;
                            int MaxApps = PH[0].ScreenSlots - PH[0].DockSlots;

                            #region Get the Total Screens
                            foreach (KeyValuePair<int, PhonesAppsOwned> App in Client.GetPlay().OwnedPhonesApps.ToList().OrderByDescending(S => S.Value.ScreenId))
                            {
                                TotalScreens = App.Value.ScreenId;
                                break;
                            }
                            #endregion

                            #region Generate Screens
                            for (int i = 0; i <= TotalScreens; i++)
                            {
                                #region Dock Apps
                                if(i == 0)
                                {
                                    #region Apps Icons in Dock
                                    foreach (KeyValuePair<int, PhonesAppsOwned> App in Client.GetPlay().OwnedPhonesApps.ToList().Where(x => x.Value.ScreenId == 0).OrderBy(S => S.Value.SlotId))
                                    {
                                        List<PhoneApp> AI = PhoneAppManager.getPhoneAppById(App.Value.AppId);
                                        if (AI != null)
                                        {
                                            html2 += "<div class=\"column\">";
                                            html2 += "<div class=\"appicon count\" data-app=\""+ AI[0].Name +"\" data-col-moves=\"0\" data-icon=\"" + AI[0].Icon + "\" style=\"background-image: url('" + AI[0].Icon + "');\">";
                                            html += (AI[0].Name.Equals("Calendar")) ? "<div class=\"app_special-first\">Sábado</div><div class=\"app_special-second\">7</div>" : "";
                                            html2 += "</div>";
                                            html2 += "<div class=\"app_name\" data-name-app=\"" + AI[0].Name + "\">" + AI[0].DisplayName + "</div>";
                                            html2 += "</div>";
                                        }
                                    }
                                    #endregion

                                }
                                #endregion

                                #region Screen Apps
                                if (i > 0)
                                {
                                    html += "<div class=\"swiper-slide\">";
                                    html += "<div id=\"columns-full\">";

                                    #region Apps Icons by Screen
                                    foreach (KeyValuePair<int, PhonesAppsOwned> App in Client.GetPlay().OwnedPhonesApps.ToList().Where(x => x.Value.ScreenId == i).OrderBy(S => S.Value.SlotId))
                                    {
                                        List<PhoneApp> AI = PhoneAppManager.getPhoneAppById(App.Value.AppId);
                                        if (AI != null)
                                        {
                                            html += "<div class=\"column swiper-no-swiping\">";
                                            html += "<div class=\"appicon count\" data-app=\"" + AI[0].Name + "\" data-col-moves=\"0\" data-icon=\"" + AI[0].Icon + "\" style=\"background-image: url('" + AI[0].Icon + "');\">";
                                            html += (AI[0].Name.Equals("Calendar")) ? "<div class=\"app_special-first\">Sábado</div><div class=\"app_special-second\">7</div>" : "";
                                            html += "</div>";
                                            html += "<div class=\"app_name\" data-name-app=\"" + AI[0].Name + "\">" + AI[0].DisplayName + "</div>";
                                            html += "</div>";
                                        }
                                    }
                                    #endregion

                                    html += "</div>";
                                    html += "</div>";
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion

                        string SendData = "";
                        SendData += html + "|" + html2;
                        Socket.Send("compose_phone|load_apps|" + SendData);
                    }
                    break;
                #endregion

                #region Show Button
                case "show_button":
                    {
                        Socket.Send("compose_phone|show_button|");
                    }
                    break;
                #endregion

                #region Toggle Phone
                case "toggle_phone":
                    {
                        if (Client.GetPlay().UsingPhone)
                        {
                            Client.GetPlay().UsingPhone = false;
                            Socket.Send("compose_phone|close_phone|");
                        }
                        else
                        {
                            Client.GetPlay().UsingPhone = true;
                            Socket.Send("compose_phone|open_phone|");
                        }
                    }
                    break;
                #endregion

                #region Open Phone
                case "open_phone":
                    {
                        if(!Client.GetPlay().UsingPhone)
                            Client.GetPlay().UsingPhone = true;

                        Socket.Send("compose_phone|open_phone|");
                    }
                    break;
                #endregion

                #region Close Phone
                case "close_phone":
                    {
                        if (Client.GetPlay().UsingPhone)
                            Client.GetPlay().UsingPhone = false;
                        Socket.Send("compose_phone|close_phone|");
                    }
                    break;
                #endregion

                #region In APP
                case "in_app":
                    {
                        string[] ReceivedData = Data.Split(',');
                        string App = ReceivedData[1];//NameAPP

                        // Filtramos por seguridad
                        App = Regex.Replace(App, "<(.|\\n)*?>", string.Empty);

                        PhoneApp AI = PhoneAppManager.getPhoneApp(App);

                        if (AI == null)
                            return;

                        if (!Client.GetPlay().OwnedPhonesApps.ContainsKey(AI.ID))
                            return;

                        Client.GetPlay().InApp = App;

                        #region APP Mobile
                        if (AI.Name != "Safari")
                        { 
                            if (AI.Code.Length > 0)
                            {
                                // Generate API Token
                                string tkn = TokenGenerator.GenTkn("GetPhoneAPP", Client.GetHabbo().Id, 0, AI.ID);

                                string iframe = "<iframe id=\"" + AI.Name + "\" src=\"" + RoleplayManager.APIUrl + "/?tkn=" + tkn + "&user_id=" + Client.GetHabbo().Id + "\" style=\"width:100%;height:100%;border:0;\"></iframe>";

                                Socket.Send("compose_phone|in_app|" + AI.Name + "|" + iframe);
                            }
                            else
                                PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>No se ha podido abrir la aplicaci&oacute;n</b><br>No se ha obtenido informaci&oacute;n de esta Aplicaci&oacute;n.|");
                        }
                        #endregion

                        #region Internet
                        else
                        {
                            string CurPage = (Client.GetPlay().InternetCurPage.Length > 0) ? Client.GetPlay().InternetCurPage : RoleplayManager.DefaultWebPage;

                            PlayInternetManager.TryToGetWebPage(CurPage, out PlayInternet PI);

                            if (PI == null)
                                return;

                            if (Client.GetPlay().InternetHisto == null)
                            {
                                List<string> IH = new List<string>();
                                IH.Add(CurPage);

                                Client.GetPlay().InternetHisto = IH;
                            }

                            // Generate API Token
                            string tkn = TokenGenerator.GenTkn("GetWebPage", Client.GetHabbo().Id, 0, 0, CurPage);
                            string if_name = PI.URL.Replace(".", "");

                            string iframe = "<iframe id=\"" + if_name + "\" src=\"" + RoleplayManager.APIUrl + "/?tkn=" + tkn + "\" style=\"width: 100%;height: calc(100% - 41px);border: 0;\"></iframe>";

                            Socket.Send("compose_phone|in_web_page|" + CurPage + "|" + iframe + "|" + Client.GetPlay().InternetHisto);
                        }
                        #endregion

                        #region Special Apps WS
                        // Si entra a WhatsApp actualizar sus chats y contactos
                        if (Client.GetPlay().InApp == "WhatsApp")
                        {
                            //open_whatsapp
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_whatsapp");
                        }
                        // Si entra a Mensajes actualziar chats
                        else if (Client.GetPlay().InApp == "Messages")
                        {
                            //open_chatrooms
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_chatrooms");
                        }
                        // Si entra a Contactos actualizar contactos
                        else if (Client.GetPlay().InApp == "Contacts")
                        {
                            //open_contacs
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_contacts");
                        }
                        // Si entra a Safari actualizar info relevante a Internet
                        else if (Client.GetPlay().InApp == "Safari")
                        {
                            //open_safari
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_safari");
                        }
                        #endregion
                    }
                    break;
                #endregion

                #region In APP Error
                case "in_app_error":
                    {
                        string[] PData = Data.Split(',');
                        string[] ReceivedData = PData[1].Split('|');
                        string Msg = ReceivedData[0];

                        Socket.Send("compose_phone|in_app_error|" + Msg);
                    }
                    break;
                #endregion

                #region Open ChatRooms
                case "open_chatrooms":
                    {
                        string SendData = "";
                        string ChatRooms = "";
                        List<PhoneChat> Chats = PlusEnvironment.GetGame().GetPhoneChatManager().GetPhoneChatsByMyID(Client.GetHabbo().Id);

                        // Si tiene chats guardados.
                        if (Chats != null && Chats.Count > 0)
                        {
                            #region ChatRooms
                            // Almacenamos ChatRooms en SendData
                            Chats.Reverse();
                            foreach (PhoneChat PChats in Chats)
                            {
                                int ChatUsId = PChats.EmisorId;
                                string ChatName = PChats.EmisorName;

                                // Si el receptor es difenrete a mí, el emisor soy yo...
                                if (PChats.ReceptorName != Client.GetHabbo().Username) {
                                    ChatName = PChats.ReceptorName;
                                    ChatUsId = PChats.ReceptorId;
                                }
                                // Guardamos los nombres de Chats existentes
                                ChatRooms += ChatName + ";";

                                // Variable para el encabezado visual del chat
                                string DisplayName = ChatName;

                                // Verificamos si el chat es un Amigo
                                List<MessengerBuddy> Friend = (from TG in Client.GetHabbo().GetMessenger().GetFriends().ToList() where TG != null && TG.mUsername == ChatName select TG).ToList();
                                if(Friend == null || Friend.Count <= 0)
                                {
                                    // Si no está en la lista de amigos buscamos su número.
                                    DisplayName = PlusEnvironment.GetGame().GetClientManager().GetNumberById(ChatUsId);
                                }

                                // Spliteamos los chatrooms almacenados
                                string[] stringSeparators = new string[] { ";" };
                                string[] result;
                                int count = 0;
                                result = ChatRooms.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string inf in result)
                                {
                                    if (inf == ChatName)
                                        count++;
                                }
                                if (count <= 1)
                                {
                                    string msg = PChats.Msg;
                                    // Si el emisor soy yo, anteponer 'Yo:'
                                    if (PChats.EmisorName == Client.GetHabbo().Username)
                                        msg = "Yo: " + PChats.Msg;

                                    #region Chats
                                    SendData += "<div data-chatname=\"" + ChatName + "\" class=\"app_msg_content\" onclick=\"";
                                    SendData += "$('.Messages_Title').hide();";
                                    SendData += "$('#WS_Messages').hide();";
                                    SendData += "$('#WS_Messages_Chatting').show();";
                                    SendData += "\">";
                                    SendData += "<div class=\"app_msg_indicator\"></div>";
                                    SendData += "<div class=\"app_msg_dest\">" + DisplayName + "</div>";
                                    SendData += "<div class=\"app_msg_time\">" + PChats.TimeStamp + " ></div>";
                                    SendData += "<div class=\"app_msg_lastmsg\">" + Regex.Replace(msg, "::br::", " ") + "</div>";
                                    SendData += "</div>";
                                    #endregion

                                    // Verifica si se quedó en un Chat específico abierto
                                    if (Client.GetPlay().LastChat == ChatName)
                                    {
                                        Client.GetPlay().UpdateChats = true;
                                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_messages," + ChatName);
                                        Client.GetPlay().UpdateChats = false;
                                    }
                                }
                            }
                            #endregion
                        }

                        Socket.Send("compose_phone|open_chatrooms|" + SendData);
                    }
                    break;
                #endregion

                #region Send Message
                case "send_message":
                    {
                        // Recibimos mensaje y target, ya filtrados.
                        #region Conditions
                        if (Client.GetPlay().TryGetCooldown("msg", true))
                        {
                            Client.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                            return;
                        }

                        string[] PData = Data.Split(',');
                        string[] ReceivedData = PData[1].Split('|');

                        if(ReceivedData.Count() < 2)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>No se pudo obtener la información del mensaje correctamente.|");
                            return;
                        }

                        string Target = ReceivedData[1];
                        string Text = ReceivedData[0];

                        // Filtramos
                        //Text = Regex.Replace(Text, "<(.|\\n)*?>", string.Empty); <- Por emojis
                        Target = Regex.Replace(Target, "<(.|\\n)*?>", string.Empty);

                        if(Target.Length <= 0)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>No puedes enviar mensajes sin destinatario.|");
                            return;
                        }
                        if (Text.Length <= 0)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>No puedes enviar mensajes sin texto.|");
                            return;
                        }
                        #endregion

                        /*
                         * El Target recibido puede ser
                         * número telefónico, ya sea con o sin formato
                         * ó el nombre de usuario.
                         * Si es nombre de usuario éste debe ser "amigo"
                         * Nota: Validar cuando el Target esté online o no.
                        */

                        int Targetid = PlusEnvironment.GetGame().GetPhoneChatManager().GetIDbyContact(Client, Target);
                        
                        #region Extra Conditions
                        if (Targetid <= 0)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>Ese número telefónico no existe.|");
                            return;
                        }
                        if (Targetid == Client.GetHabbo().Id)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>No puedes enviarte mensajes a ti mism@.|");
                            return;
                        }

                        // Envió a un nombre de contacto
                        if (Client.GetPlay().SendToName)
                        {
                            // Verificamos que sean amigos
                            List<MessengerBuddy> Friend = (from TG in Client.GetHabbo().GetMessenger().GetFriends().ToList() where TG != null && TG.UserId == Targetid select TG).ToList();

                            if (Friend == null || Friend.Count <= 0)
                            {
                                PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>No se encontró ese nombre en tus Contactos. Intenta escribiendo el Número Telefónico.|");
                                return;
                            }
                        }
                        #endregion

                        #region Execute
                        int ID = RoleplayManager.ChatsID += 1;
                        DateTime TimeStamp = DateTime.Now;
                        string TargetName = PlusEnvironment.GetUserInfoBy("username", "id", Convert.ToString(Targetid));

                        // TryAdd al Diccionario de PhoneChat
                        PlusEnvironment.GetGame().GetPhoneChatManager().NewPhoneChat(ID, 1, Client.GetHabbo().Id, Client.GetHabbo().Username, Targetid, TargetName, Text, TimeStamp);

                        RoleplayManager.Shout(Client, "*Ha enviado un Mensaje de Texto*", 5);

                        // Comprobamos si destinatario está online
                        GameClient TargetClient = PlusEnvironment.GetGame().GetClientManager().GetClientByUserID(Targetid);
                        if (TargetClient != null)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_phone", "open_chatrooms");
                            RoleplayManager.Shout(TargetClient, "*Recibe un Mensaje de Texto*", 5);

                            // Verificamos si el último chat del destinatario es el nuestro y actualizamos
                            if (TargetClient.GetPlay().LastChat == Client.GetHabbo().Username)
                            {
                                // Refresh ChatRooms Target
                                TargetClient.GetPlay().UpdateChats = true;
                                PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_phone", "open_messages," + Client.GetHabbo().Username);
                                TargetClient.GetPlay().UpdateChats = false;

                                // Refresh Msgs Target
                                if(TargetClient.GetPlay().LastChat == Client.GetHabbo().Username)
                                    PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_phone", "open_chatrooms");
                            }
                        }

                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_chatrooms");
                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_messages," + TargetName);
                        
                        Client.GetPlay().CooldownManager.CreateCooldown("msg", 1000, 1);
                        #endregion
                    }
                    break;
                #endregion

                #region Open Messages
                case "open_messages":
                    {
                        string SendData = "";
                        string[] ReceivedData = Data.Split(',');
                        string ChatName = ReceivedData[1];

                        // Filtramos
                        ChatName = Regex.Replace(ChatName, "<(.|\\n)*?>", string.Empty);
                        string DisplayName = ChatName;
                        int ChatUserID = 0;

                        #region Messages
                        List<PhoneChat> Chattings = PlusEnvironment.GetGame().GetPhoneChatManager().GetPhoneChatsByMyID(Client.GetHabbo().Id);
                        foreach (PhoneChat PChattings in Chattings)
                        {
                            if (PChattings.EmisorName == ChatName || PChattings.ReceptorName == ChatName)
                            {
                                if (PChattings.EmisorName != Client.GetHabbo().Username)
                                {
                                    if (ChatUserID == 0)
                                        ChatUserID = PChattings.EmisorId;

                                    SendData += "<div class=\"Msg_Container\">";
                                    SendData += "<div class=\"Msg_From\" title=\"" + PChattings.TimeStamp + "\">" + Regex.Replace(PChattings.Msg, "::br::", "<br>") + "</div>";
                                    SendData += "</div>";
                                }
                                else
                                {
                                    if (ChatUserID == 0)
                                        ChatUserID = PChattings.ReceptorId;

                                    SendData += "<div class=\"Msg_Container\">";
                                    SendData += "<div class=\"Msg_To\" title=\"" + PChattings.TimeStamp + "\">" + Regex.Replace(PChattings.Msg, "::br::", "<br>") + "</div>";
                                    SendData += "</div>";
                                }
                            }
                        }
                        #endregion

                        // Verificamos si el chat es un Amigo
                        List<MessengerBuddy> Friend = (from TG in Client.GetHabbo().GetMessenger().GetFriends().ToList() where TG != null && TG.mUsername == ChatName select TG).ToList();
                        if (Friend == null || Friend.Count <= 0)
                        {
                            // Si no está en la lista de amigos buscamos su número.
                            DisplayName = PlusEnvironment.GetGame().GetClientManager().GetNumberById(ChatUserID);
                        }

                        // Separamos
                        SendData += "|";

                        SendData += DisplayName;
                        Client.GetPlay().LastChat = ChatName;
                        if(Client.GetPlay().UpdateChats)
                            Socket.Send("compose_phone|update_messages|" + SendData);
                        else
                            Socket.Send("compose_phone|open_messages|" + SendData);
                    }
                    break;
                #endregion

                #region Open WhatsApp (ChatRooms & Contatcs)
                case "open_whatsapp":
                    {
                        string SendData = "";
                        string ChatRooms = "";
                        List<PhoneChat> Chats = PlusEnvironment.GetGame().GetPhoneChatManager().GetPhoneWhatsChatsByMyID(Client.GetHabbo().Id);

                        // Si tiene chats guardados.
                        if (Chats != null && Chats.Count > 0)
                        {
                            #region ChatRooms
                            // Almacenamos ChatRooms en SendData
                            Chats.Reverse();
                            foreach (PhoneChat PChats in Chats)
                            {
                                int ChatUsId = PChats.EmisorId;
                                string ChatName = PChats.EmisorName;

                                // Si el receptor es difenrete a mí, el emisor soy yo...
                                if (PChats.ReceptorName != Client.GetHabbo().Username)
                                {
                                    ChatName = PChats.ReceptorName;
                                    ChatUsId = PChats.ReceptorId;
                                }
                                // Guardamos los nombres de Chats existentes
                                ChatRooms += ChatName + ";";

                                // Variable para el encabezado visual del chat
                                string DisplayName = ChatName;

                                // Verificamos si el chat es un Amigo
                                List<MessengerBuddy> Friend = (from TG in Client.GetHabbo().GetMessenger().GetFriends().ToList() where TG != null && TG.mUsername == ChatName select TG).ToList();
                                if (Friend == null || Friend.Count <= 0)
                                {
                                    // Si no está en la lista de amigos buscamos su número.
                                    DisplayName = PlusEnvironment.GetGame().GetClientManager().GetNumberById(ChatUsId);
                                }

                                // Spliteamos los chatrooms almacenados
                                string[] stringSeparators = new string[] { ";" };
                                string[] result;
                                int count = 0;
                                result = ChatRooms.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string inf in result)
                                {
                                    if (inf == ChatName)
                                        count++;
                                }
                                if (count <= 1)
                                {
                                    string msg = PChats.Msg;
                                    string TargetLook = PlusEnvironment.GetUserInfoBy("look", "username", ChatName);
                                    // Si el emisor soy yo, anteponer 'Yo:'
                                    if (PChats.EmisorName == Client.GetHabbo().Username)
                                        msg = "Yo: " + PChats.Msg;

                                    #region Chats
                                    SendData += "<div data-whatsname=\"" + ChatName + "\" class=\"app_msg_content\" onclick=\"";
                                    SendData += "$('.Whats_Title').hide();";
                                    SendData += "$('#What_Menu').hide();";
                                    SendData += "$('#WS_WhatsApp').show();";
                                    SendData += "$('#WS_WhatsApp_Contacts').show();";
                                    SendData += "$('#WS_WhatsApp_Chatting').show();";
                                    SendData += "\">";
                                    SendData += "<div class=\"app_whats_profile_photo\">";
                                    SendData += "<div class=\"app_contacts_avatar\" style=\"background-image: url(" + RoleplayManager.AVATARIMG + ""+ TargetLook + "&headonly=1);\"></div>";
                                    SendData += "</div>";
                                    SendData += "<div class=\"app_whats_chat_info\">";
                                    SendData += "<div class=\"app_msg_dest\">" + DisplayName + "</div>";
                                    SendData += "<div class=\"app_msg_time\">" + PChats.TimeStamp.ToString("dd/MM/yyyy hh:mm tt") + "</div>";
                                    SendData += "<div class=\"app_msg_lastmsg\">" + Regex.Replace(msg, "::br::", " ") + "</div>";
                                    SendData += "</div>";
                                    SendData += "</div>";
                                    #endregion

                                    // Verifica si se quedó en un Chat específico abierto
                                    if (Client.GetPlay().LastWhatsChat == ChatName)
                                    {
                                        Client.GetPlay().UpdateWhatsChats = true;
                                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_whatschats," + ChatName);
                                        Client.GetPlay().UpdateWhatsChats = false;
                                    }
                                }
                            }
                            #endregion
                        }

                        // Separamos
                        SendData += "|";

                        // Contactos
                        #region Contacts
                        foreach (MessengerBuddy Buddy in Client.GetHabbo().GetMessenger().GetFriends().OrderBy(o => o.mUsername).ToList())
                        {
                            if (Buddy != null)
                            {
                                #region Contacts
                                SendData += "<div data-whatsname=\"" + Buddy.mUsername + "\" class=\"app_msg_content\" onclick=\"";
                                SendData += "$('.Whats_Title').hide();";
                                SendData += "$('#What_Menu').hide();";
                                SendData += "$('#WS_WhatsApp').hide();";
                                SendData += "$('#WS_WhatsApp_Contacts').hide();";
                                SendData += "$('#WS_WhatsApp_Chatting').show();";
                                SendData += "\">";
                                SendData += "<div class=\"app_whats_profile_photo\">";
                                SendData += "<div class=\"app_contacts_avatar\" style=\"background-image: url(" + RoleplayManager.AVATARIMG + "" + Buddy.mLook + "&headonly=1);\"></div>";
                                SendData += "</div>";
                                SendData += "<div class=\"app_whats_chat_info\">";
                                SendData += "<div class=\"app_msg_dest\">" + Buddy.mUsername + "</div>";
                                SendData += "<div class=\"app_msg_lastmsg\">Hey there, I'm using WhatsApp!</div>";
                                SendData += "</div>";
                                SendData += "</div>";
                                #endregion
                            }
                        }
                        #endregion

                        Socket.Send("compose_phone|open_whatsapp|" + SendData);
                    }
                    break;
                #endregion

                #region Send Whatsapp
                case "send_whatsapp":
                    {
                        // Recibimos mensaje y target, ya filtrados.
                        #region Conditions
                        if (Client.GetPlay().TryGetCooldown("msg", true))
                        {
                            Client.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                            return;
                        }

                        string[] ReceivedData = Data.Split('|');
                        string Text = ReceivedData[1];
                        string Target = ReceivedData[2];

                        // Filtramos
                        //Text = Regex.Replace(Text, "<(.|\\n)*?>", string.Empty); <- Por emojis
                        Target = Regex.Replace(Target, "<(.|\\n)*?>", string.Empty);

                        if (Target.Length <= 0)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>No puedes enviar mensajes sin destinatario.|");
                            return;
                        }
                        if (Text.Length <= 0)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>No puedes enviar mensajes sin texto.|");
                            return;
                        }
                        #endregion

                        /*
                         * El Target recibido debe ser un nombre de usuario.
                         * Y éste debe ser "amigo".
                         * Nota: Validar cuando el Target esté online o no.
                        */

                        int Targetid = PlusEnvironment.GetGame().GetPhoneChatManager().GetIDbyContact(Client, Target);

                        #region Extra Conditions
                        if (Targetid <= 0)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>Ese contacto no existe.|");
                            return;
                        }
                        if (Targetid == Client.GetHabbo().Id)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>No puedes enviarte mensajes a ti mis@.|");
                            return;
                        }

                        // Envió a un nombre de contacto
                        if (Client.GetPlay().SendToName)
                        {
                            // Verificamos que sean amigos
                            List<MessengerBuddy> Friend = (from TG in Client.GetHabbo().GetMessenger().GetFriends().ToList() where TG != null && TG.UserId == Targetid select TG).ToList();

                            if (Friend == null || Friend.Count <= 0)
                            {
                                PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>No se encontró ese nombre en tu lista de contactos. Intenta mandarle un mensaje de texto.|");
                                return;
                            }
                        }
                        else
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>Solo puedes usar Whatsapp con tus contactos.|");
                            return;
                        }
                        #endregion

                        #region Execute
                        int ID = RoleplayManager.ChatsID += 1;
                        DateTime TimeStamp = DateTime.Now;
                        string TargetName = PlusEnvironment.GetUserInfoBy("username", "id", Convert.ToString(Targetid));

                        // TryAdd al Diccionario de PhoneChat
                        PlusEnvironment.GetGame().GetPhoneChatManager().NewPhoneChat(ID, 2, Client.GetHabbo().Id, Client.GetHabbo().Username, Targetid, TargetName, Text, TimeStamp);

                        RoleplayManager.Shout(Client, "*Ha enviado un Whatsapp*", 5);

                        // Comprobamos si destinatario está online
                        GameClient TargetClient = PlusEnvironment.GetGame().GetClientManager().GetClientByUserID(Targetid);
                        if (TargetClient != null)
                        {
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_phone", "open_whatsapp");
                            RoleplayManager.Shout(TargetClient, "*Recibe un Whatsapp*", 5);

                            // Verificamos si el último chat del destinatario es el nuestro y actualizamos
                            if (TargetClient.GetPlay().LastWhatsChat == Client.GetHabbo().Username)
                            {
                                // Refresh ChatRooms Target
                                TargetClient.GetPlay().UpdateWhatsChats = true;
                                PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_phone", "open_whatschats," + Client.GetHabbo().Username);
                                TargetClient.GetPlay().UpdateWhatsChats = false;

                                // Refresh Msgs Target
                                if (TargetClient.GetPlay().LastWhatsChat == Client.GetHabbo().Username)
                                    PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(TargetClient, "event_phone", "open_whatsapp");
                            }
                        }

                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_whatsapp");
                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_whatschats," + TargetName);
                        
                        Client.GetPlay().CooldownManager.CreateCooldown("msg", 1000, 1);
                        #endregion
                    }
                    break;
                #endregion

                #region Open Whatschats
                case "open_whatschats":
                    {
                        string SendData = "";
                        string[] ReceivedData = Data.Split(',');
                        string ChatName = ReceivedData[1];
                        // Filtramos
                        ChatName = Regex.Replace(ChatName, "<(.|\\n)*?>", string.Empty);
                        string look = "";
                        string lastonline = "";
                        int ChatUserID = 0;

                        #region Messages
                        List<PhoneChat> Chattings = PlusEnvironment.GetGame().GetPhoneChatManager().GetPhoneWhatsChatsByMyID(Client.GetHabbo().Id);
                        if (Chattings != null)
                        { 
                            foreach (PhoneChat PChattings in Chattings)
                            {
                                if (PChattings.EmisorName == ChatName || PChattings.ReceptorName == ChatName)
                                {
                                    if (PChattings.EmisorName != Client.GetHabbo().Username)
                                    {
                                        if (ChatUserID == 0)
                                            ChatUserID = PChattings.EmisorId;

                                        SendData += "<div class=\"Msg_Container\">";
                                        SendData += "<div class=\"Msg_From_Whats\" title=\"" + PChattings.TimeStamp + "\">" + Regex.Replace(PChattings.Msg, "::br::", "<br>") + "</div>";
                                        SendData += "</div>";
                                    }
                                    else
                                    {
                                        if (ChatUserID == 0)
                                            ChatUserID = PChattings.ReceptorId;

                                        SendData += "<div class=\"Msg_Container\">";
                                        SendData += "<div class=\"Msg_To_Whats\" title=\"" + PChattings.TimeStamp + "\">" + Regex.Replace(PChattings.Msg, "::br::", "<br>") + "</div>";
                                        SendData += "</div>";
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Contact Info
                        List<MessengerBuddy> Contacts = Client.GetHabbo().GetMessenger().GetFriends().ToList();
                        //Contacts.Sort();
                        foreach (MessengerBuddy Buddy in Contacts)
                        {
                            if (Buddy != null)
                            {
                                if (Buddy.mUsername == ChatName)
                                {
                                    look = Buddy.mLook;
                                    lastonline = (Buddy.IsOnline) ? "en l&iacute;nea" : PlusEnvironment.UnixTimeStampToDateTime(Buddy.mLastOnline).ToString("MMMM dd \\d\\e yyyy \\a \\l\\a\\s hh:mm tt") + "";
                                }
                            }
                        }
                        #endregion

                        // Separamos
                        SendData += "|";
                        SendData += "<div class=\"app_contacts_avatar\" style=\"background-image: url(" + RoleplayManager.AVATARIMG + "" + look + "&headonly=1);\"></div>";
                        
                        // Separamos
                        SendData += "|";
                        SendData += ChatName;

                        // Separamos
                        SendData += "|";
                        SendData += lastonline;

                        Client.GetPlay().LastWhatsChat = ChatName;
                        if (Client.GetPlay().UpdateWhatsChats)
                            Socket.Send("compose_phone|update_whatschats|" + SendData);
                        else
                            Socket.Send("compose_phone|open_whatschats|" + SendData);
                    }
                    break;
                #endregion

                #region Contacts
                case "open_contacts":
                    {
                        string SendData = "";
                        #region Contacts
                        List<MessengerBuddy> Contacts = Client.GetHabbo().GetMessenger().GetFriends().OrderBy(o => o.mUsername).ToList();
                        //Contacts.Sort();
                        //Contacts.OrderBy(o => o.mUsername).ToList();
                        foreach (MessengerBuddy Buddy in Contacts)
                        {
                            if (Buddy != null)
                            {
                                string Number = PlusEnvironment.GetGame().GetClientManager().GetNumberById(Buddy.UserId);
                                #region Contacts
                                SendData += "<div class=\"app_contacts_content\">";
                                SendData += "<div class=\"app_contatcs_info\">";
                                SendData += "<div class=\"app_contacts_avatar\" style=\"background-image: url(" + RoleplayManager.AVATARIMG + "" + Buddy.mLook + "&headonly=1);\"></div>";
                                SendData += "<div class=\"app_contacts_name\">" + Buddy.mUsername + "</div>";
                                SendData += "<div class=\"app_contacts_num\">" + Number + "</div>";
                                SendData += "</div>";
                                SendData += "<div class=\"app_contacts_buttons\">";
                                SendData += "<div data-contact=\"" + Buddy.mUsername + "\" class=\"app_contacts_msg\" title=\"Enviar Mensaje\"></div>";
                                SendData += "<div data-contact=\"" + Buddy.mUsername + "\" class=\"app_contacts_call\" title=\"Llamar Contacto\"></div>";
                                SendData += "<div data-contact=\"" + Buddy.mUsername + "\" class=\"app_contacts_del\" title=\"Eliminar Contacto\"></div>";
                                SendData += "</div>";
                                SendData += "</div>";
                                #endregion
                            }
                        }
                        #endregion

                        Socket.Send("compose_phone|open_contacts|" + SendData);
                        break;
                    }
                #endregion

                #region Call Contact
                case "call_contact":
                    {
                        if (Client.GetPlay().TryGetCooldown("call", true))
                        {
                            Client.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                            return;
                        }

                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "in_app_error,<b>Ha ocurrido un error</b><br>¡Oh oh! Actualmente nuestra red móvil no soporta llamadas telefónicas.|");
                        Client.GetPlay().CooldownManager.CreateCooldown("call", 1000, 5);
                    }
                break;
                #endregion

                #region Delete Contact
                case "del_contact":
                    {
                        if (Client.GetPlay().TryGetCooldown("delete", true))
                        {
                            Client.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                            return;
                        }

                        string[] ReceivedData = Data.Split(',');
                        string Username = ReceivedData[1];

                        // Filtramos
                        Username = Regex.Replace(Username, "<(.|\\n)*?>", string.Empty);

                        #region RemoveBuddyEvent
                        int Id = PlusEnvironment.GetGame().GetClientManager().GetIdByName(Username);

                        if (Client.GetHabbo().Relationships.Where(x => x.Value.UserId == Id).ToList().Count > 0)
                        {
                            using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                            {
                                dbClient.SetQuery("DELETE FROM `user_relationships` WHERE `user_id` = @id AND `target` = @target OR `target` = @id AND `user_id` = @target");
                                dbClient.AddParameter("id", Client.GetHabbo().Id);
                                dbClient.AddParameter("target", Id);
                                dbClient.RunQuery();
                            }
                        }

                        if (Client.GetHabbo().Relationships.ContainsKey(Convert.ToInt32(Id)))
                            Client.GetHabbo().Relationships.Remove(Convert.ToInt32(Id));

                        GameClient Target = PlusEnvironment.GetGame().GetClientManager().GetClientByUserID(Id);
                        if (Target != null)
                        {
                            if (Target.GetHabbo().Relationships.ContainsKey(Convert.ToInt32(Client.GetHabbo().Id)))
                                Target.GetHabbo().Relationships.Remove(Convert.ToInt32(Client.GetHabbo().Id));
                        }

                        Client.GetHabbo().GetMessenger().DestroyFriendship(Id);
                        #endregion

                        // Simulamos eliminar posible Chat de Whastapp poniendo un Type de 3.
                        List<PhoneChat> Chats = PlusEnvironment.GetGame().GetPhoneChatManager().GetPhoneWhatsChatsByChatting(Client.GetHabbo().Id, Id);
                        
                        if (Chats != null && Chats.Count > 0)
                        {
                            foreach (PhoneChat PChats in Chats)
                            {
                                PChats.Type = 3;//3 no entra ni en msg ni en whats.
                            }
                        }

                        Client.GetPlay().CooldownManager.CreateCooldown("delete", 1000, 3);
                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_phone", "open_contacts");
                    }
                break;
                #endregion

                #region Search Contacts
                case "search_friend":
                    {
                        if (Client.GetPlay().TryGetCooldown("search", true))
                        {
                            Client.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                            return;
                        }

                        string[] ReceivedData = Data.Split(',');
                        string search = ReceivedData[1];

                        // Filtramos
                        search = Regex.Replace(search, "<(.|\\n)*?>", string.Empty);

                        #region HabboSearchEvent
                        if (Client == null || Client.GetHabbo() == null || Client.GetHabbo().GetMessenger() == null)
                            return;

                        string Query = StringCharFilter.Escape(search.Replace("%", ""));
                        if (Query.Length < 1 || Query.Length > 100)
                            return;

                        string SendData = "";

                        List<SearchResult> Results = SearchResultFactory.GetSearchResult(Query);
                        foreach (SearchResult Result in Results.ToList())
                        {
                            string Number = PlusEnvironment.GetGame().GetClientManager().GetNumberById(Result.UserId);
                            #region Contacts
                            // Validamos si son amigos (contactos)
                            if (Client.GetHabbo().GetMessenger().FriendshipExists(Result.UserId))
                            {
                                SendData += "<div class=\"app_contacts_content\">";
                                SendData += "<div class=\"app_contatcs_info\">";
                                SendData += "<div class=\"app_contacts_avatar\" style=\"background-image: url(" + RoleplayManager.AVATARIMG + "" + Result.Figure + "&headonly=1);\"></div>";
                                SendData += "<div class=\"app_contacts_name\">" + Result.Username + "</div>";
                                SendData += "<div class=\"app_contacts_num\">" + Number + "</div>";
                                SendData += "</div>";
                                SendData += "<div class=\"app_contacts_buttons\">";
                                SendData += "<div data-contact=\"" + Result.Username + "\" class=\"app_contacts_msg\" title=\"Enviar Mensaje\"></div>";
                                SendData += "<div data-contact=\"" + Result.Username + "\" class=\"app_contacts_call\" title=\"Llamar Contacto\"></div>";
                                SendData += "<div data-contact=\"" + Result.Username + "\" class=\"app_contacts_del\" title=\"Eliminar Contacto\"></div>";
                                SendData += "</div>";
                                SendData += "</div>";
                            }
                            #endregion

                        }
                        #endregion

                        Client.GetPlay().CooldownManager.CreateCooldown("search", 1000, 3);
                        Socket.Send("compose_phone|open_contacts|" + SendData);
                        break;
                    }
                #endregion

                #region Servicios
                case "servicios":
                    {
                        if (RoleplayManager.PurgeEvent)
                        {
                            Client.SendWhisper("¡Todos los servicios no están disponibles durante la purga!", 1);
                            return;
                        }
                        string[] ReceivedData = Data.Split(',');
                        string type = ReceivedData[1];
                        int roomid = 0;
                        Room ThisRoom = null;
                        switch (type)
                        {
                            #region Médico
                            case "medico":
                                #region Conditions
                                if (!Client.GetPlay().IsDying)
                                {
                                    Client.SendWhisper("¡Debes estar inconsciente para llamar a un paramédico!", 1);
                                    return;
                                }
                                if (Client.GetPlay().TryGetCooldown("servmedico", true))
                                    return;
                                #endregion

                                #region Execute
                                roomid = Client.GetRoomUser().RoomId;
                                ThisRoom = RoleplayManager.GenerateRoom(roomid, false);
                                if (ThisRoom == null)
                                {
                                    Client.SendWhisper("Ha ocurrido un error inesperado, contacta con un Administrador. [1]", 1);
                                    return;
                                }
                                RoleplayManager.Shout(Client, "*Llamó a un paramédico y espera*", 5);
                                Client.SendWhisper("Has mandado una solicitud de Ambulancia. Un Médico atenderá tu llamado. Por favor espera.", 1);
                                Client.GetRoomUser().ApplyEffect(599);
                                Client.GetPlay().PediMedico = true;
                                PlusEnvironment.GetGame().GetClientManager().sendWorkAlert(Client.GetHabbo().Username + " ha solicitado servicio de médico en " + ThisRoom.Name, "heal", true);
                                Client.GetPlay().CooldownManager.CreateCooldown("servmedico", 1000, 30);
                                #endregion
                                break;
                            #endregion

                            #region Armero
                            case "armero":
                                #region Basic Conditions
                                if (Client.GetPlay().EquippedWeapon == null)
                                {
                                    Client.SendWhisper("Debes equipar un arma para solicitar los servicios de un Armero.", 1);
                                    return;
                                }
                                if (Client.GetPlay().Cuffed)
                                {
                                    Client.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                                    return;
                                }
                                if (!Client.GetRoomUser().CanWalk)
                                {
                                    Client.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                                    return;
                                }
                                if (Client.GetPlay().Pasajero)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().IsDead)
                                {
                                    Client.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().IsJailed)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().IsDying)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().DrivingCar)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                                    return;
                                }
                                if (Client.GetPlay().TryGetCooldown("servarm", true))
                                {
                                    Client.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                                    return;
                                }
                                #endregion

                                #region Execute
                                roomid = Client.GetRoomUser().RoomId;
                                ThisRoom = RoleplayManager.GenerateRoom(roomid, false);
                                if (ThisRoom == null)
                                {
                                    Client.SendWhisper("Ha ocurrido un error inesperado, contacta con un Administrador. [1]", 1);
                                    return;
                                }
                                RoleplayManager.Shout(Client, "*Ha publicado una solicitud de Armero*", 5);
                                Client.SendWhisper("Has mandado una solicitud de Armero. Un Armero atenderá tu llamado. Por favor espera.", 1);
                                Client.GetPlay().PediArm = true;
                                PlusEnvironment.GetGame().GetClientManager().sendWorkAlert(Client.GetHabbo().Username + " ha solicitado servicio de Armero en " + ThisRoom.Name, "armero");
                                Client.GetPlay().CooldownManager.CreateCooldown("servarm", 1000, 30);
                                #endregion
                                break;
                            #endregion

                            #region Taxi OFF
                                /*
                            case "taxi":
                                #region Basic Conditions

                                if (Client.GetPlay().Cuffed)
                                {
                                    Client.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                                    return;
                                }
                                if (!Client.GetRoomUser().CanWalk)
                                {
                                    Client.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                                    return;
                                }
                                if (Client.GetPlay().Pasajero)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().IsDead)
                                {
                                    Client.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().IsJailed)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().IsDying)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().DrivingCar)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                                    return;
                                }
                                if (Client.GetPlay().TryGetCooldown("servtaxi", true))
                                {
                                    Client.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                                    return;
                                }
                                #endregion

                                #region Execute
                                roomid = Client.GetRoomUser().RoomId;
                                ThisRoom = RoleplayManager.GenerateRoom(roomid, false);
                                if (ThisRoom == null)
                                {
                                    Client.SendWhisper("Ha ocurrido un error inesperado, contacta con un Administrador. [1]", 1);
                                    return;
                                }
                                RoleplayManager.Shout(Client, "*Ha solicitado un Taxi*", 5);
                                Client.SendWhisper("Has mandado una solicitud de Taxi. Un Taxista atenderá tu llamado. Por favor espera.", 1);
                                PlusEnvironment.GetGame().GetClientManager().sendWorkAlert(Client.GetHabbo().Username + " ha solicitado un taxi en " + ThisRoom.Name, "taxi");
                                Client.GetPlay().CooldownManager.CreateCooldown("servtaxi", 1000, 30);
                                #endregion
                                break;
                                */
                            #endregion

                            #region Mecánico
                            case "mecanico":
                                #region Basic Conditions

                                if (Client.GetPlay().Cuffed)
                                {
                                    Client.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                                    return;
                                }
                                if (!Client.GetRoomUser().CanWalk)
                                {
                                    Client.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                                    return;
                                }
                                if (Client.GetPlay().Pasajero)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().IsDead)
                                {
                                    Client.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().IsJailed)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().IsDying)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                                    return;
                                }
                                if (Client.GetPlay().DrivingCar)
                                {
                                    Client.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                                    return;
                                }
                                if (Client.GetPlay().TryGetCooldown("servmeca", true))
                                {
                                    Client.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                                    return;
                                }
                                #endregion

                                #region Execute
                                roomid = Client.GetRoomUser().RoomId;
                                ThisRoom = RoleplayManager.GenerateRoom(roomid, false);
                                if (ThisRoom == null)
                                {
                                    Client.SendWhisper("Ha ocurrido un error inesperado, contacta con un Administrador. [2]", 1);
                                    return;
                                }
                                RoleplayManager.Shout(Client, "*Ha solicitado un Mecánico*", 5);
                                Client.SendWhisper("Has mandado una solicitud de Mécánico. Un Mecánico atenderá tu llamado. Por favor espera.", 1);
                                PlusEnvironment.GetGame().GetClientManager().sendWorkAlert(Client.GetHabbo().Username + " ha solicitado un mecánico en " + ThisRoom.Name, "mecanico", true);
                                Client.GetPlay().PediMec = true;
                                Client.GetPlay().CooldownManager.CreateCooldown("servmeca", 1000, 30);
                                #endregion
                                break;
                            #endregion

                            default:
                                Client.SendWhisper("No se ha econtrado el Servicio solicitado.", 1);
                                break;
                        }

                        break;
                    }
                #endregion

                #region Safari
                case "open_safari":
                    {
                        string SendData = "";

                        Socket.Send("compose_phone|open_safari|" + SendData);
                        break;
                    }
                #endregion
            }
        }
    }
}
