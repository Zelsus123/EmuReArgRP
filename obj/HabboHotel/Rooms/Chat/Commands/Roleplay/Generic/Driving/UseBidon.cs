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
using Plus.HabboHotel.RolePlay.PlayRoom;
using Plus.HabboHotel.GameClients;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.HabboRoleplay.VehicleOwned;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Self
{
    class UseBidonCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_use_bidon"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Permite ingresar 5L de Combustible a tu Vehículo."; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (!Room.DriveEnabled)
            {
                Session.SendWhisper("¡No es posible realizar esa acción en esta zona!", 1);
                return;
            }
            if(Session.GetPlay().Bidon <= 0)
            {
                Session.SendWhisper("¡No tienes ningún Bidón en tu Inventario a usar!", 1);
                return;
            }
            if (Session.GetPlay().TryGetCooldown("bidon"))
                return;
            #endregion

            #region Execute
            List<VehiclesOwned> VO = null;
            int MaxFuel = 0;
            // Si está conduciendo, obtiene info del auto desde su variable DrivingCarId (Diccionario)
            if (Session.GetPlay().DrivingCar)
            {
                VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Session.GetPlay().DrivingCarId);
                MaxFuel = Session.GetPlay().CarMaxFuel;
            }
            // Si no está conduciendo, obtiene info del auto desde el furni encima y consulta el itemid al diccionario
            else
            {
                #region Get Veihcle Info (play_vehicles)  
                Vehicle vehicle = null;
                bool found = false;
                int itemfurni = 0, corp = 0;
                Item BTile = null;
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
                    Session.SendWhisper("¡Debes estar sobre un vehículo para hacer eso!", 1);
                    return;
                }
                #endregion

                VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
                MaxFuel = vehicle.MaxFuel;
            }
            if (VO == null || VO.Count <= 0)
            {
                Session.SendWhisper("((No se encontró información del vehículo))", 1);
                return;
            }
            if ((VO[0].Fuel + 5) > MaxFuel)
            {
                Session.SendWhisper("A este vehículo ya no le caben 5 L. más de combustible.", 1);
                return;
            }
            
            VO[0].Fuel = (VO[0].Fuel + 5);// Actualizamos State en Diccionario
            if (Session.GetPlay().DrivingCar)
                Session.GetPlay().CarFuel = VO[0].Fuel;
            RoleplayManager.UpdateVehicleStat(VO[0].Id, "fuel", VO[0].Fuel);// Actualizamos en DB
            RoleplayManager.Shout(Session, "*Usa un Bidón y vierte 5 L. de Combustible a su Vehículo.*", 5);
            Session.GetPlay().Bidon -= 1;

            if(Session.GetPlay().DrivingCar)
                PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Session, "event_vehicle", "open");

            Session.GetPlay().CooldownManager.CreateCooldown("bidon", 1000, 5);
            return;
            #endregion
        }

    }
}
