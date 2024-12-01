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
    class BuyFuelFillCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_buy_fuel_fill"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Llena el Tanque de combustible de tu vehículo."; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (!Session.GetPlay().DrivingCar)
            {
                Session.SendWhisper("Debes estar conduciendo el vehículo. Si tu vehículo no enciende, comprar Bidones.", 1);
                return;
            }
            if (!Room.GasEnabled)
            {
                Session.SendWhisper("Debes estar en una Gasolinera para comprar combustible.", 1);
                return;
            }
            List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Session.GetPlay().DrivingCarId);
            if (VO == null || VO.Count <= 0)
            {
                Session.SendWhisper("((No se pudo obtener la información del vehículo que conduces))", 1);
                return;
            }
            int Amount = Session.GetPlay().CarMaxFuel - VO[0].Fuel;            
            if(Amount <= 0)
            {
                Session.SendWhisper("Tu tanque ya se encuentra lleno.", 1);
                return;
            }
            int Price = Amount * RoleplayManager.FuelPrice;
            if (Session.GetHabbo().Credits < Price)
            {
                Session.SendWhisper("No cuentas con $"+ Price + " para comprar " + Amount + " L. de Combustible.", 1);
                return;
            }
            if (Session.GetPlay().TryGetCooldown("buyfuel"))
                return;
            #endregion

            #region Comodin Conditions
            Item BTile = null;
            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
            if (BTile == null)
            {
                Session.SendWhisper("Debes estar cerca de la despachadora de Combustible.", 1);
                return;
            }
            #endregion

            #region Execute

            Session.GetPlay().IsFuelCharging = true;
            Session.GetPlay().FuelChargingCant = Amount;
            Session.GetPlay().LoadingTimeLeft = Amount;
            Session.GetPlay().TimerManager.CreateTimer("general", 1000, true);
            Session.GetPlay().CooldownManager.CreateCooldown("buyfuel", 1000, 3);

            return;

            #endregion
        }

    }
}
