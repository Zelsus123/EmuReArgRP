﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Plus.HabboHotel.Items;
using Plus.HabboRoleplay.Vehicles;
using Plus.HabboHotel.Rooms;
using Plus.Communication.Packets.Outgoing.Rooms.Avatar;
using Plus.HabboHotel.Groups;
using Plus.HabboRoleplay.Misc;
using Plus.Database.Interfaces;
using System.Drawing;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using System.Data;
using Plus.Communication.Packets.Outgoing.Notifications;
using Plus.HabboRoleplay.VehicleOwned;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Self
{
    class CheckCarInfoCommand :IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_driving_checkinfo"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Permite ver la información de un vehículo."; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(Session, "infocar") && !Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            {
                Session.SendWhisper("¡Solo un oficial de policía puede hacer eso!", 1);
                return;
            }

            if (!Session.GetPlay().IsWorking && !Session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            {
                Session.SendWhisper("Debes estar trabajando para hacer eso.", 1);
                return;
            }
            if (Session.GetPlay().IsDead)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                return;
            }

            if (Session.GetPlay().IsJailed)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras estás encarcelad@!", 1);
                return;
            }

            if (Session.GetPlay().Cuffed)
            {
                Session.SendWhisper("¡No hacer eso mientras estás esposad@!", 1);
                return;
            }
            if (Session.GetPlay().TryGetCooldown("carcheck"))
                return;
            #endregion

            #region Execute

            #region Check Vehicle   
            Vehicle vehicle = null;
            bool found = false;
            int itemfurni = 0, corp = 0;
            Item BTile=null;
            string itemnm = null;
            foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
            {
                if (!found)
                {
                    BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Session.GetRoomUser().Coordinate);
                    if (BTile != null)
                    {
                        vehicle = Vehicle;
                        itemfurni = BTile.Id;
                        itemnm = Vehicle.ItemName;
                        corp = Convert.ToInt32(Vehicle.CarCorp);
                        found = true;
                    }
                }
            }
            //Al examinar todos los autos ninguno conincide con el item donde está parado el user...
            if (!found)
            {
                Session.SendWhisper("¡Debes estar sobre un vehículo o conduciendolo para ver su información!", 1);
                return;
            }
            #endregion
            List<VehiclesOwned> VO = null;
            if (!Session.GetPlay().DrivingCar)
               VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
            else
               VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Session.GetPlay().DrivingCarId);
            if (VO == null || VO.Count <= 0)
            {
                Session.SendWhisper("((No se pudo obtener información de este vehículo))", 1);
                return;
            }

            #region Get Estado
            int state = VO[0].State;
            string mode = "";
            if (state == 0)
                mode = "Óptimo & Abierto";
            else if (state == 1)
                mode = "Óptimo & Cerrado";
            else if (state == 2)
                mode = "Averiado & Abierto";
            else if (state == 3)
                mode = "Averiado & Cerrado";
            else if (state == 4)//?
                mode = "En Grúa";
            #endregion

            StringBuilder List = new StringBuilder();
            List.Append("INFORMACIÓN DEL VEHÍCULO\n\n");
            List.Append("Modelo: "+ vehicle.DisplayName +"\n");
            
            if (corp > 1)
            {
                Group CorpName = PlusEnvironment.GetGame().GetGroupManager().GetJobByID(corp);
                List.Append("Empresa: " + Convert.ToString(CorpName.Name) + "\n");

                // Camión
                if(CorpName.Name.Contains("Camioneros"))
                {
                    List.Append("Chofer: " + (VO[0].CamOwnId > 0 ? PlusEnvironment.GetGame().GetClientManager().GetNameById(VO[0].CamOwnId) : "Ninguno") + "\n");
                    List.Append("Carga: " + RoleplayManager.getCamCargName(VO[0].CamCargId) + "\n");
                }
            }else
            {
                List.Append("Dueño: "+ (VO[0].OwnerId > 0 ? PlusEnvironment.GetGame().GetClientManager().GetNameById(VO[0].OwnerId) : "Sin Dueño")+"\n");
            }

            List.Append("Última persona en manejarlo: "+ PlusEnvironment.GetGame().GetClientManager().GetNameById(VO[0].LastUserId) + "\n\n");

            List.Append("ESTADÍSTICAS DEL VEHÍCULO\n\n");
            List.Append("Vida: " + VO[0].CarLife + "/100\n");
            List.Append("Combustible: " + VO[0].Fuel + "/" + vehicle.MaxFuel + "\n");
            List.Append("KM: " + VO[0].Km + "\n");
            List.Append("Estado: "+mode+"\n");
            List.Append("Traba: " + (VO[0].Traba ? "Sí" : "No") + "\n");
            List.Append("Alarma: " + (VO[0].Alarm ? "Sí" : "No") + "\n");
            List.Append("Max. Pasajeros: " + vehicle.MaxDoors + "\n\n");

            if (Session.GetHabbo().Rank >= 6)// DEVS
            {
                List.Append("ESTADÍSTICAS DEL VEHÍCULO (STAFF)\n\n");
                List.Append("ID: "+ VO[0].Id + "\n");
                List.Append("Base Item: "+vehicle.ItemID+"\n");
                List.Append("Base Name: "+vehicle.ItemName+"\n");
                List.Append("Command Name: " + vehicle.Model + "\n");
            }
            Session.SendMessage(new MOTDNotificationComposer(List.ToString()));
            return;
            #endregion
        }

    }
}
