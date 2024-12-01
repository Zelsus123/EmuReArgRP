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
using System.Data;
using Plus.HabboRoleplay.VehicleOwned;
using Plus.HabboHotel.RolePlay.PlayRoom;
using Plus.HabboRoleplay.Products;
using System.ServiceModel.Security.Tokens;
using Plus.HabboRoleplay.ProductOwned;

namespace Plus.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// VehiclesWebEvent class.
    /// </summary>
    class ProductsWebEvent : IWebEvent
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
            if (!Client.GetPlay().UsingAtm)
            {
                Client.SendNotification("Buen intento, tratando de injectar el systema, ve a un ATM!");
                return;
            }
            */
            Room Room = RoleplayManager.GenerateRoom(Client.GetRoomUser().RoomId);
            if (Room == null)
                return;

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);


            switch (Action)
            {
                // General
                #region Close
                case "close":
                    {
                        Client.GetPlay().ViewProducts = false;
                        Socket.Send("compose_products|close_products|");
                    }
                    break;
                #endregion

                #region Buy Products (play_products)
                case "buy_radiocomunicador":
                case "buy_traba":
                case "buy_alarm":
                case "buy_balde":
                case "buy_palanca":
                case "buy_destornillador":
                case "buy_grua":
                case "buy_sellcar":
                    {
                        if (Client.GetPlay().TryGetCooldown("buy"))
                            return;

                        #region Products Conditions
                        string[] Received = Action.Split('_');
                        string pname = Received[1];
                        Product PO = ProductsManager.getProduct(pname);
                        if (PO == null)
                        {
                            Socket.Send("compose_products|productmsg|Producto inválido. Vuelve a intentarlo.");
                            return;
                        }

                        if (Client.GetPlay().OwnedProducts.ContainsKey(PO.ID))
                        {
                            int getCant;
                            if (!int.TryParse(Client.GetPlay().OwnedProducts[PO.ID].Extradata, out getCant))
                                getCant = Client.GetPlay().OwnedProducts.Where(x => x.Value.ProductId == PO.ID).Count();

                            if (getCant >= PO.MaxCant && PO.MaxCant != -1)
                            {
                                Socket.Send("compose_products|productmsg|¡Ya cuentas con " + PO.MaxCant + " " + PO.DisplayName + " en tu inventario! No es posible tener más a la vez.");
                                return;
                            }
                        }

                        if (PO.Type == "productos")
                        {
                            if (!Room.SupermarketEnabled)
                            {
                                Client.SendWhisper("Debes ir a un supermercado para comprar productos.", 1);
                                return;
                            }
                        }
                        else if (PO.Type == "herramientas")
                        {
                            if (!Room.FerreteriaEnabled)
                            {
                                Client.SendWhisper("Debes ir a una ferretería para comprar herramientas.", 1);
                                return;
                            }
                        }
                        else if (PO.Type == "municipalidad")
                        {
                            if (!Room.MunicipalidadEnabled)
                            {
                                Client.SendWhisper("Debes ir a la Municipalidad para solicitar ese servicio.", 1);
                                return;
                            }
                        }
                        #endregion

                        #region All Products
                        if (Action == "buy_radiocomunicador")
                        {                            
                            #region Buy Radio

                            #region Comodin Conditions
                            Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Client.SendWhisper("Debes acercarte a la caja para comprar productos.", 1);
                                return;
                            }
                            #endregion

                            // Calc Price
                            int price = PO.Price;

                            if (Client.GetHabbo().Credits < price)
                            {
                                Socket.Send("compose_products|productmsg|No tienes dinero suficiente.");
                                return;
                            }

                            // Insertamos nuevo producto en play_products_owned
                            RoleplayManager.AddProduct(Client, PO);
                            Client.GetPlay().OwnedProducts = Client.GetPlay().LoadAndReturnProducts();

                            Client.GetHabbo().Credits -= price;
                            Client.GetHabbo().UpdateCreditsBalance();

                            Socket.Send("compose_products|productmsg_green|¡Radio comprado exitosamente!");
                            RoleplayManager.Shout(Client, "*Ha comprado un Radio comunicador*", 5);
                            Client.SendWhisper("¡Radio Comprado! Usa :radio [mensaje] para comunicarte con los miembros de tu Banda.", 1);
                            #endregion
                        }
                        else if(Action == "buy_traba")
                        {
                            #region Buy Traba

                            #region Comodin Conditions
                            Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Client.SendWhisper("Debes acercarte a la caja para comprar productos.", 1);
                                return;
                            }
                            #endregion

                            if (Client.GetPlay().DrivingInCar)
                            {
                                Socket.Send("compose_products|productmsg|Primero debes detener el vehículo que tienes afuera.");
                                return;
                            }
                            if (Client.GetPlay().InTutorial && Client.GetPlay().TutorialStep < 27)
                            {
                                Client.SendWhisper("¡Hey, no tan rápido! Ve siguiendo el Tutorial paso a paso para guiarte de la mejor manera.", 1);
                                return;
                            }

                            string[] ReceivedData = Data.Split(',');
                            int GetCarID;
                            if (!int.TryParse(ReceivedData[1], out GetCarID))
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            bool Mine = RoleplayManager.IsMyVehicle(Client, GetCarID);
                            if (!Mine)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            string model = "";
                            List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(GetCarID);
                            if (VO == null || VO.Count <= 0)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            model = VO[0].Model;
                            Vehicle vehicle = VehicleManager.getVehicle(model);
                            if (vehicle == null)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }

                            // FÓRUMULA CÁLCULO DE TRABAS
                            int price = ((vehicle.Price / 2) / 10) + 200;

                            if (Client.GetHabbo().Credits < price)
                            {
                                Socket.Send("compose_products|productmsg|No tienes dinero suficiente.");
                                return;
                            }
                            VO[0].Traba = true;
                            RoleplayManager.UpdateVehicleStat(GetCarID, "traba", "1");

                            Client.GetHabbo().Credits -= price;
                            Client.GetHabbo().UpdateCreditsBalance();

                            Socket.Send("compose_products|productmsg_green|Ahora puedes usar :abrir o :cerrar sobre tu vehículo.");
                            RoleplayManager.Shout(Client, "*Ha comprado una traba de seguridad para su vehículo*", 5);
                            Client.SendWhisper("¡Traba Comprada! Ahora puedes usar :abrir o :cerrar sobre tu vehículo para hacer uso de ella.", 1);
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_products", "open_trabas");

                            #region Tutorial Step Check
                            if (Client.GetPlay().TutorialStep == 27 && Room.MallEnabled && Room.Type.Equals("public"))
                            {
                                Client.GetPlay().TutorialStep = 32;
                                PlusEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|30");
                            }
                            #endregion
                            #endregion
                        }
                        else if(Action == "buy_alarm")
                        {
                            #region Buy Alarmas

                            #region Comodin Conditions
                            Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Client.SendWhisper("Debes acercarte a la caja para comprar productos.", 1);
                                return;
                            }
                            #endregion

                            if (Client.GetPlay().DrivingInCar)
                            {
                                Socket.Send("compose_products|productmsg|Primero debes detener el vehículo que tienes afuera.");
                                return;
                            }

                            string[] ReceivedData = Data.Split(',');
                            int GetCarID;
                            if (!int.TryParse(ReceivedData[1], out GetCarID))
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            bool Mine = RoleplayManager.IsMyVehicle(Client, GetCarID);
                            if (!Mine)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            string model = "";
                            List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(GetCarID);
                            if (VO == null || VO.Count <= 0)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            model = VO[0].Model;
                            Vehicle vehicle = VehicleManager.getVehicle(model);
                            if (vehicle == null)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }

                            // FÓRMULA CÁLCULO DE ALARMAS
                            int price = ((vehicle.Price / 10) / 2) + 50;

                            if (Client.GetHabbo().Credits < price)
                            {
                                Socket.Send("compose_products|productmsg|No tienes dinero suficiente.");
                                return;
                            }

                            VO[0].Alarm = true;
                            RoleplayManager.UpdateVehicleStat(GetCarID, "alarm", "1");

                            Client.GetHabbo().Credits -= price;
                            Client.GetHabbo().UpdateCreditsBalance();

                            Socket.Send("compose_products|productmsg_green|Ahora recibirás una alerta cuando alguien tome tu auto.");
                            RoleplayManager.Shout(Client, "*Ha comprado una Alarma para su vehículo*", 5);
                            Client.SendWhisper("¡Alarma Comprada! Ahora recibirás una alerta cada que alguien tome tu vehículo.", 1);
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_products", "open_alarmas");
                            #endregion
                        }
                        else if(Action == "buy_balde")
                        {
                            #region Buy Balde

                            #region Comodin Conditions
                            Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Client.SendWhisper("Debes acercarte a la caja para comprar herramientas.", 1);
                                return;
                            }
                            #endregion

                            // Calc Price
                            int price = PO.Price;

                            if (Client.GetHabbo().Credits < price)
                            {
                                Socket.Send("compose_products|productmsg|No tienes dinero suficiente.");
                                return;
                            }

                            // Insertamos nuevo producto en play_products_owned
                            RoleplayManager.AddProduct(Client, PO);
                            Client.GetPlay().OwnedProducts = Client.GetPlay().LoadAndReturnProducts();

                            Client.GetHabbo().Credits -= price;
                            Client.GetHabbo().UpdateCreditsBalance();

                            Socket.Send("compose_products|productmsg_green|¡Balde comprado exitosamente!");
                            RoleplayManager.Shout(Client, "*Ha comprado un Balde*", 5);
                            Client.SendWhisper("¡Balde Comprado! Usa :ayuda marihuana para saber todo acerca sobre su plantación y uso del Balde.", 1);
                            #endregion
                        }
                        else if(Action == "buy_palanca")
                        {
                            #region Buy Palanca

                            #region Comodin Conditions
                            Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Client.SendWhisper("Debes acercarte a la caja para comprar herramientas.", 1);
                                return;
                            }
                            #endregion

                            // Calc Price
                            int price = PO.Price;

                            if (Client.GetHabbo().Credits < price)
                            {
                                Socket.Send("compose_products|productmsg|No tienes dinero suficiente.");
                                return;
                            }

                            // Insertamos nuevo producto en play_products_owned
                            RoleplayManager.AddProduct(Client, PO);
                            Client.GetPlay().OwnedProducts = Client.GetPlay().LoadAndReturnProducts();

                            Client.GetHabbo().Credits -= price;
                            Client.GetHabbo().UpdateCreditsBalance();

                            Socket.Send("compose_products|productmsg_green|¡Palanca comprada exitosamente!");
                            RoleplayManager.Shout(Client, "*Ha comprado una Palanca*", 5);
                            Client.SendWhisper("¡Palanca Comprada! Ahora puedes :robar cajero.", 1);
                            #endregion
                        }
                        else if(Action == "buy_destornillador")
                        {
                            #region Buy Destornillador

                            #region Comodin Conditions
                            Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Client.SendWhisper("Debes acercarte a la caja para comprar herramientas.", 1);
                                return;
                            }
                            #endregion

                            // Calc Price
                            int price = PO.Price;

                            if (Client.GetHabbo().Credits < price)
                            {
                                Socket.Send("compose_products|productmsg|No tienes dinero suficiente.");
                                return;
                            }

                            // Insertamos nuevo producto en play_products_owned
                            RoleplayManager.AddProduct(Client, PO);
                            Client.GetPlay().OwnedProducts = Client.GetPlay().LoadAndReturnProducts();

                            Client.GetHabbo().Credits -= price;
                            Client.GetHabbo().UpdateCreditsBalance();

                            Socket.Send("compose_products|productmsg_green|¡Destornillador comprado exitosamente!");
                            RoleplayManager.Shout(Client, "*Ha comprado un Destornillador*", 5);
                            Client.SendWhisper("¡Destornillador Comprado! Ahora puedes ':forzarcerradura' en las casas de Robo. ((Usa ':ayuda trabajos'))", 1);
                            #endregion
                        }
                        else if(Action == "buy_grua")
                        {
                            #region Buy Grua
                            #region Action Point Conditions
                            Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "actionpoint01" && x.Coordinate == Client.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Client.SendWhisper("Debes acercarte al mostrador para solicitar el servicio de grúa.", 1);
                                return;
                            }
                            #endregion

                            string[] ReceivedData = Data.Split(',');
                            int GetCarID;
                            if (!int.TryParse(ReceivedData[1], out GetCarID))
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            bool Mine = RoleplayManager.IsMyVehicle(Client, GetCarID);
                            if (!Mine)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            string model = "";
                            List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(GetCarID);
                            if (VO == null || VO.Count <= 0)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            model = VO[0].Model;
                            Vehicle vehicle = VehicleManager.getVehicle(model);
                            if (vehicle == null)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }

                            // FÓRMULA CÁLCULO DE GRÚA
                            int price = ((vehicle.Price / 10) / 2);

                            if (Client.GetHabbo().VIPRank == 2)
                                price = 0;

                            if (Client.GetHabbo().Credits < price)
                            {
                                Socket.Send("compose_products|productmsg|No tienes dinero suficiente.");
                                return;
                            }

                            RoleplayManager.PickItem(Client, VO[0].FurniId, VO[0].Location);

                            VO[0].Location = 0;
                            RoleplayManager.UpdateVehicleStat(GetCarID, "location", 0);

                            if (VO[0].CarLife <= 0)
                            {
                                VO[0].CarLife = 1;
                                RoleplayManager.UpdateVehicleStat(GetCarID, "life", 1);
                            }

                            Client.GetHabbo().Credits -= price;
                            Client.GetHabbo().UpdateCreditsBalance();

                            #region Colocar DrivingINCarg
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_products", "loader-on");
                            #region Online DriveVars
                            //Lo conduce
                            Client.GetPlay().DrivingCar = false;
                            Client.GetPlay().DrivingInCar = true;
                            Client.GetPlay().DrivingCarId = VO[0].Id;
                            Client.GetPlay().DrivingCarItem = VO[0].FurniId;

                            //Combustible System
                            Client.GetPlay().CarType = vehicle.CarType;// Define el gasto de combustible
                            Client.GetPlay().CarFuel = vehicle.MaxFuel;
                            Client.GetPlay().CarMaxFuel = vehicle.MaxFuel;
                            Client.GetPlay().CarTimer = 0;
                            Client.GetPlay().CarLife = VO[0].CarLife;

                            Client.GetPlay().CarEnableId = vehicle.EffectID;//Coloca el enable para conducir
                            Client.GetPlay().CarEffectId = vehicle.EffectID;//Guarda el enable del último auto en conducción.
                                                                            //Session.GetRoomUser().ApplyEffect(vehicle.EffectID); - No Pone efecto con DrivingIncar = True
                                                                            //Session.GetRoomUser().FastWalking = true; - No FastWalking sin efecto
                            #endregion
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_products", "loader-off");
                            #endregion

                            #region Remove Vehicle From Target
                            List<GameClient> TargetDriver = (from TG in PlusEnvironment.GetGame().GetClientManager().GetClients.ToList() where TG != null && TG.GetHabbo() != null && TG.GetPlay() != null && TG.GetPlay().DrivingCarId == GetCarID && TG.GetHabbo().Id != Client.GetHabbo().Id select TG).ToList();
                            foreach (GameClient Target in TargetDriver)
                            {
                                #region Extra Conditions & Checks

                                #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                                //Vars
                                string Pasajeros = Target.GetPlay().Pasajeros;
                                string[] stringSeparators = new string[] { ";" };
                                string[] result;
                                result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string psjs in result)
                                {
                                    GameClient PJ = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                    if (PJ != null)
                                    {
                                        if (PJ.GetPlay().ChoferName == Target.GetHabbo().Username)
                                        {
                                            RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Target.GetHabbo().Username + "*", 5);
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
                                        Target.GetPlay().PasajerosCount--;
                                        StringBuilder builder = new StringBuilder(Target.GetPlay().Pasajeros);
                                        builder.Replace(PJ.GetHabbo().Username + ";", "");
                                        Target.GetPlay().Pasajeros = builder.ToString();

                                        // CHOFER 
                                        Target.GetPlay().Chofer = (Target.GetPlay().PasajerosCount <= 0) ? false : true;
                                        Target.GetRoomUser().AllowOverride = (Target.GetPlay().PasajerosCount <= 0) ? false : true;

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
                                                        Client.GetPlay().TimerManager.CreateTimer("death", 1000, true);
                                                    */
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

                                        // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                                        if (PJ.GetPlay().IsBasuPasaj)
                                            PJ.GetPlay().IsBasuPasaj = false;
                                    }
                                }
                                #endregion

                                #region Online ParkVars
                                //Retornamos a valores predeterminados
                                Target.GetPlay().DrivingCar = false;
                                Target.GetPlay().DrivingInCar = false;
                                Target.GetPlay().DrivingCarId = 0;// Id de VehiclesOwned;

                                //Combustible System
                                Target.GetPlay().CarType = 0;// Define el gasto de combustible
                                Target.GetPlay().CarFuel = 0;
                                Target.GetPlay().CarMaxFuel = 0;
                                Target.GetPlay().CarTimer = 0;
                                Target.GetPlay().CarLife = 0;

                                Target.GetPlay().CarEnableId = 0;//Coloca el enable para conducir
                                Target.GetPlay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                                Target.GetRoomUser().ApplyEffect(0);
                                Target.GetRoomUser().FastWalking = false;
                                PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Target, "event_vehicle", "close");// WS FUEL
                                #endregion

                                #endregion
                            }
                            #endregion

                            Socket.Send("compose_products|productmsg_green|La Grúa ha recogido tu Vehículo.");
                            RoleplayManager.Shout(Client, "*Ha solicitado una Grúa*", 5);
                            Client.SendWhisper("La Grúa ha dejado tu Vehículo afuera. Dirígete allá para recibirlo.", 1);
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_products", "open_grua");
                            #endregion
                        }
                        else if(Action == "buy_sellcar")
                        {
                            #region Sell Car
                            if (Client.GetPlay().DrivingInCar)
                            {
                                Socket.Send("compose_products|productmsg|Primero debes detener el vehículo que tienes afuera.");
                                return;
                            }

                            #region Action Point Conditions
                            Item BTile = null;
                            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "actionpoint01" && x.Coordinate == Client.GetRoomUser().Coordinate);
                            if (BTile == null)
                            {
                                Client.SendWhisper("Debes acercarte al mostrador para vender vehículos.", 1);
                                return;
                            }
                            #endregion

                            string[] ReceivedData = Data.Split(',');
                            int GetCarID;
                            if (!int.TryParse(ReceivedData[1], out GetCarID))
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            bool Mine = RoleplayManager.IsMyVehicle(Client, GetCarID);
                            if (!Mine)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            string model = "";
                            List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(GetCarID);
                            if (VO == null || VO.Count <= 0)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }
                            model = VO[0].Model;
                            Vehicle vehicle = VehicleManager.getVehicle(model);
                            if (vehicle == null)
                            {
                                Socket.Send("compose_products|productmsg|ERROR: Vehículo no encontrado.");
                                return;
                            }

                            RoleplayManager.PickItem(Client, VO[0].FurniId, VO[0].Location);

                            // Eliminamos vehículo de diccionario y db
                            PlusEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(GetCarID, true);

                            // FÓRMULA CÁCULO DE VENTA DE VEHÍCULO
                            int price = vehicle.Price / 3;

                            if (vehicle.Price > 100)
                            {
                                Client.GetHabbo().Credits += price;
                                Client.GetPlay().MoneyEarned += price;
                                Client.GetHabbo().UpdateCreditsBalance();
                            }
                            else
                            {
                                Client.GetHabbo().Diamonds += price;
                                Client.GetPlay().PLEarned += price;
                                Client.GetHabbo().UpdateDiamondsBalance();
                            }

                            #region Remove Vehicle From Target
                            List<GameClient> TargetDriver = (from TG in PlusEnvironment.GetGame().GetClientManager().GetClients.ToList() where TG != null && TG.GetHabbo() != null && TG.GetPlay() != null && TG.GetPlay().DrivingCarId == GetCarID && TG.GetHabbo().Id != Client.GetHabbo().Id select TG).ToList();
                            foreach (GameClient Target in TargetDriver)
                            {
                                #region Extra Conditions & Checks

                                #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                                //Vars
                                string Pasajeros = Target.GetPlay().Pasajeros;
                                string[] stringSeparators = new string[] { ";" };
                                string[] result;
                                result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string psjs in result)
                                {
                                    GameClient PJ = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                                    if (PJ != null)
                                    {
                                        if (PJ.GetPlay().ChoferName == Target.GetHabbo().Username)
                                        {
                                            RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Target.GetHabbo().Username + "*", 5);
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
                                        Target.GetPlay().PasajerosCount--;
                                        StringBuilder builder = new StringBuilder(Target.GetPlay().Pasajeros);
                                        builder.Replace(PJ.GetHabbo().Username + ";", "");
                                        Target.GetPlay().Pasajeros = builder.ToString();

                                        // CHOFER 
                                        Target.GetPlay().Chofer = (Target.GetPlay().PasajerosCount <= 0) ? false : true;
                                        Target.GetRoomUser().AllowOverride = (Target.GetPlay().PasajerosCount <= 0) ? false : true;

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
                                                        Client.GetPlay().TimerManager.CreateTimer("death", 1000, true);
                                                    */
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

                                        // SI EL PASAJERO ES COMPAÑERO DE BASURERO
                                        if (PJ.GetPlay().IsBasuPasaj)
                                            PJ.GetPlay().IsBasuPasaj = false;
                                    }
                                }
                                #endregion

                                #region Online ParkVars
                                //Retornamos a valores predeterminados
                                Target.GetPlay().DrivingCar = false;
                                Target.GetPlay().DrivingInCar = false;
                                Target.GetPlay().DrivingCarId = 0;// Id de VehiclesOwned;

                                //Combustible System
                                Target.GetPlay().CarType = 0;// Define el gasto de combustible
                                Target.GetPlay().CarFuel = 0;
                                Target.GetPlay().CarMaxFuel = 0;
                                Target.GetPlay().CarTimer = 0;
                                Target.GetPlay().CarLife = 0;

                                Target.GetPlay().CarEnableId = 0;//Coloca el enable para conducir
                                Target.GetPlay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                                Target.GetRoomUser().ApplyEffect(0);
                                Target.GetRoomUser().FastWalking = false;
                                PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Target, "event_vehicle", "close");// WS FUEL
                                #endregion

                                #endregion
                            }
                            #endregion

                            string pricedisplay = "$ " + String.Format("{0:N0}", price);

                            if (vehicle.Price < 100)
                                pricedisplay = String.Format("{0:N0}", price) + " PL";

                            Socket.Send("compose_products|productmsg_green|¡Vehículo vendido exitosamente!");
                            RoleplayManager.Shout(Client, "*Ha vendido su " + vehicle.Model + " por " + pricedisplay + "*", 5);
                            PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_products", "open_sellcar");
                            #endregion
                        }
                        #endregion

                        Client.GetPlay().CooldownManager.CreateCooldown("buy", 1000, 5);
                    }
                    break;
                #endregion

                // 24/7
                #region Open Mall
                case "open_mall":
                    {
                        #region Conditions
                        if (!Room.SupermarketEnabled)
                        {
                            Client.SendWhisper("Debes ir a un supermercado para comprar productos.", 1);
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes acercarte a la caja para comprar productos.", 1);
                            return;
                        }
                        #endregion

                        #region HTML
                        string html = "";
                        List<Product> PO = ProductsManager.getProductsByType("productos");
                        if (PO != null && PO.Count > 0)
                        {
                            foreach (var product in PO)
                            {
                                string DItem = "Auto";
                                string Price = "$ " + String.Format("{0:N0}", product.Price);
                                string ButtonText = "Comprar";
                                string ButtonAction = "buy_" + product.ProductName;

                                if (product.Price <= -1)
                                {
                                    Price = "Depende " + DItem;
                                    ButtonText = "Elegir >>";
                                    ButtonAction = "go" + product.ProductName;
                                }

                                html += "<div class=\"product\">";
                                html += "<div class=\"product-box\" style=\"background-image: url('"+ RoleplayManager.CdnURL +"/ws_overlays/Products/resources/images/"+ product.ProductName +".png');background-position: center bottom;background-repeat: no-repeat;\">";
                                html += "<center><b>"+ product.DisplayName +"</b></center>";
                                html += "</div>";
                                html += "<div class=\"product-footer\">";
                                html += "<div id=\"Prod_Price\">"+ Price +"</div>";
                                html += "<div style=\"margin-top:3px;margin-bottom: 3px;\"><hr></div>";
                                html += "<div id=\""+ ButtonAction +"\" class=\"myButton\">"+ ButtonText + "</div>";
                                html += "</div>";
                                html += "</div>";
                            }
                        }
                        #endregion

                        string SendData = "";
                        SendData += html;
                        Client.GetPlay().ViewProducts = true;
                        Socket.Send("compose_products|open_mall|" + SendData);
                    }
                    break;
                #endregion

                #region Open Trabas
                case "open_trabas":
                    {
                        string SendData = "";
                        int Count = 0;
                        List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getMyVehiclesOwned(Client.GetHabbo().Id);

                        if (VO != null && VO.Count > 0)
                        {
                            foreach (VehiclesOwned _vo in VO)
                            {
                                if (!_vo.Traba)
                                {
                                    Count++;
                                    Vehicle vehicle = VehicleManager.getVehicle(_vo.Model.ToString());
                                    int price = ((vehicle.Price / 2) / 10) + 200;
                                    #region Set HTML
                                    SendData += "<div class='product'>";
                                    SendData += "<div class=\"product-box\" style=\"background-image: url('" + RoleplayManager.CdnURL + "ws_resources/images/Vehicles/" + vehicle.EffectID + ".png');background-position: left -90px center;background-repeat: no-repeat;\">";
                                    SendData += "</div>";
                                    SendData += "<div class='product-footer'>";
                                    SendData += "<div id = 'Model'>" + _vo.Model + "</div >";
                                    SendData += "<div id='WTraba'><b>Traba: </b> <i style = 'color:red;'> No </i></div>";
                                    SendData += "<div id='Prod_Price'>$ " + String.Format("{0:N0}", price) + "</div>";
                                    SendData += "<div style='margin-top:3px;margin-bottom: 3px;'><hr></div>";
                                    SendData += "<div id='" + _vo.Id + "' class='myButton'>Comprar</div>";
                                    SendData += "</div>";
                                    SendData += "</div>";
                                    #endregion
                                }                                
                            }
                            if (Count <= 0)
                            {
                                SendData += "<br><center><b style='color:red;'>No tienes ningún vehículo sin Traba. Si tienes vehículos, probablemente ya tengan Traba.</center></b><br><br><br><br><br><br>";

                                #region Tutorial Step Check
                                if (Client.GetPlay().TutorialStep == 27 && Room.MallEnabled && Room.Type.Equals("public"))
                                {
                                    Client.GetPlay().TutorialStep = 32;
                                    PlusEnvironment.GetGame().GetWebEventManager().SendDataDirect(Client, "compose_tutorial|30");
                                }
                                #endregion
                            }
                        }
                        else
                        {
                            SendData += "<br><center><b style='color:red;'>No tienes ningún vehículo a tu nombre para comprar una Traba.</center></b><br><br><br><br><br><br>";
                        }                      

                        Socket.Send("compose_products|open_trabas|" + SendData);
                    }
                    break;
                #endregion

                #region Open Alarmas
                case "open_alarmas":
                    {
                        string SendData = "";
                        int Count = 0;
                        List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getMyVehiclesOwned(Client.GetHabbo().Id);

                        if (VO != null && VO.Count > 0)
                        {
                            foreach (VehiclesOwned _vo in VO)
                            {
                                if (!_vo.Alarm)
                                {
                                    Count++;
                                    Vehicle vehicle = VehicleManager.getVehicle(_vo.Model.ToString());
                                    int price = ((vehicle.Price / 10) / 2) + 5000;
                                    #region Set HTML
                                    SendData += "<div class='product'>";
                                    SendData += "<div class=\"product-box\" style=\"background-image: url('" + RoleplayManager.CdnURL + "ws_resources/images/Vehicles/" + vehicle.EffectID + ".png');background-position: left -90px center;background-repeat: no-repeat;\">";
                                    SendData += "</div>";
                                    SendData += "<div class='product-footer'>";
                                    SendData += "<div id = 'Model'>" + _vo.Model + "</div >";
                                    SendData += "<div id='WTraba'><b>Alarma: </b> <i style = 'color:red;'> No </i></div>";
                                    SendData += "<div id='Prod_Price'>$ " + String.Format("{0:N0}", price) + "</div>";
                                    SendData += "<div style='margin-top:3px;margin-bottom: 3px;'><hr></div>";
                                    SendData += "<div id='" + _vo.Id + "' class='myButton2'>Comprar</div>";
                                    SendData += "</div>";
                                    SendData += "</div>";
                                    #endregion
                                }
                            }
                            if (Count <= 0)
                            {
                                SendData += "<br><center><b style='color:red;'>No tienes ningún vehículo sin Alarma. Si tienes vehículos, probablemente ya tengan Alarma.</b></center><br><br><br><br><br><br>";
                            }
                        }
                        else
                        {
                            SendData += "<br><center><b style='color:red;'>No tienes ningún vehículo a tu nombre para comprar una Alarma.</b></center><br><br><br><br><br><br>";
                        }
                        Socket.Send("compose_products|open_alarmas|" + SendData);
                    }
                    break;
                #endregion

                // Herramientas
                #region Open Ferretería
                case "open_ferre":
                    {
                        #region Conditions
                        if (!Room.FerreteriaEnabled)
                        {
                            Client.SendWhisper("Debes ir a una ferretería para comprar herramientas.", 1);
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Client.SendWhisper("Debes acercarte al mostrador para comprar herramientas.", 1);
                            return;
                        }
                        #endregion

                        #region HTML
                        string html = "";
                        List<Product> PO = ProductsManager.getProductsByType("herramientas");
                        if (PO != null && PO.Count > 0)
                        {
                            foreach (var product in PO)
                            {
                                html += "<div class= \"product\">";
                                html += "<div class=\"product-box\" style=\"background-image: url('" + RoleplayManager.CdnURL + "/ws_overlays/Products/resources/images/" + product.ProductName +".png');background-position: center bottom;background-repeat: no-repeat;\">";
                                html += "<center><b>"+ product.DisplayName +"</b></center>";
                                html += "</div>";
                                html += "<div class=\"product-footer\">";
                                html += "<div id=\"Prod_Price\">$ "+ String.Format("{0:N0}", product.Price) + "</div>";
                                html += "<div style=\"margin-top:3px;margin-bottom: 3px;\"><hr></div>";
                                html += "<div id=\"buy_"+ product.ProductName +"\" class=\"myButton\">Comprar</div>";
                                html += "</div>";
                                html += "</div>";
                            }
                        }
                        #endregion

                        string SendData = "";
                        SendData += html;
                        Client.GetPlay().ViewProducts = true;
                        Socket.Send("compose_products|open_ferre|" + SendData);
                    }
                    break;
                #endregion

                // Municipalidad
                #region Open Grua
                case "open_grua":
                    {
                        string SendData = "";
                        int Count = 0;
                        List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getMyVehiclesOwned(Client.GetHabbo().Id);

                        if (VO != null && VO.Count > 0)
                        {
                            foreach (VehiclesOwned _vo in VO)
                            {
                                if (_vo.Location != 0)
                                {
                                    Count++;
                                    Vehicle vehicle = VehicleManager.getVehicle(_vo.Model.ToString());
                                    int price = ((vehicle.Price / 10) / 2);
                                    if (Client.GetHabbo().VIPRank == 2)
                                        price = 0;
                                    #region Set HTML
                                    SendData += "<div class='product'>";
                                    SendData += "<div class=\"product-box\" style=\"background-image: url('" + RoleplayManager.CdnURL + "ws_resources/images/Vehicles/" + vehicle.EffectID + ".png');background-position: left -90px center;background-repeat: no-repeat;\">";
                                    SendData += "</div>";
                                    SendData += "<div class='product-footer'>";
                                    SendData += "<div id = 'Model'>" + _vo.Model + "</div >";
                                    SendData += "<div id='Prod_Price'><b style='color: black;'>Costo:</b> $ " + String.Format("{0:N0}", price) + "</div>";
                                    SendData += "<div style='margin-top:3px;margin-bottom: 3px;'><hr></div>";
                                    SendData += "<div id='" + _vo.Id + "' class='myButton3'>Seleccionar</div>";
                                    SendData += "</div>";
                                    SendData += "</div>";
                                    #endregion
                                }
                            }
                            if (Count <= 0)
                            {
                                SendData += "<br><center><b style='color:red;'>No tienes ningún vehículo que pueda ser atendido por el Servicio de Grúa.</center></b><br><br><br><br><br><br>";
                            }
                        }
                        else
                        {
                            SendData += "<br><center><b style='color:red;'>No tienes ningún vehículo para ser atendido por el Servicio de Grúa.</center></b><br><br><br><br><br><br>";
                        }
                        
                        Client.GetPlay().ViewProducts = true;
                        Socket.Send("compose_products|open_grua|" + SendData);
                    }
                    break;
                #endregion

                #region Open SellCar
                case "open_sellcar":
                    {
                        string SendData = "";
                        List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getMyVehiclesOwned(Client.GetHabbo().Id);

                        if (VO != null && VO.Count > 0)
                        {
                            foreach (VehiclesOwned _vo in VO)
                            {
                                Vehicle vehicle = VehicleManager.getVehicle(_vo.Model.ToString());
                                int price = vehicle.Price / 3;

                                string pricedisplay = "$ " + String.Format("{0:N0}", price);

                                if (vehicle.Price < 100)
                                    pricedisplay = String.Format("{0:N0}", price) + " PL";

                                #region Set HTML
                                SendData += "<div class='product'>";
                                SendData += "<div class=\"product-box\" style=\"background-image: url('" + RoleplayManager.CdnURL + "ws_resources/images/Vehicles/" + vehicle.EffectID + ".png');background-position: left -90px center;background-repeat: no-repeat;\">";
                                SendData += "</div>";
                                SendData += "<div class='product-footer'>";
                                SendData += "<div id = 'Model'>" + _vo.Model + "</div >";
                                SendData += "<div id='Prod_Price'><b style='color: black;'>Precio:</b> " + pricedisplay + "</div>";
                                SendData += "<div style='margin-top:3px;margin-bottom: 3px;'><hr></div>";
                                SendData += "<div id='" + _vo.Id + "' class='myButton3' style='width: 85%;'>Vender</div>";
                                SendData += "</div>";
                                SendData += "</div>";
                                #endregion
                            }
                        }
                        else
                        {
                            SendData += "<br><center><b style='color:red;'>No tienes ningún vehículo para vender.</center></b><br><br><br><br><br><br>";
                        }

                        Client.GetPlay().ViewProducts = true;
                        Socket.Send("compose_products|open_sellcar|" + SendData);
                    }
                    break;
                #endregion
            }
        }
    }
}
