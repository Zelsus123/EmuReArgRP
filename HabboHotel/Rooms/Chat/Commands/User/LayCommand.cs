﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Plus.HabboHotel.Rooms;
using MySql.Data.MySqlClient.Memcached;

namespace Plus.HabboHotel.Rooms.Chat.Commands.User
{
    class LayCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_lay"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Haz que tu personaje se acueste."; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            if (Session.GetPlay().DrivingCar || Session.GetPlay().Pasajero || Session.GetPlay().EquippedWeapon != null
                || Session.GetPlay().IsFarming || Session.GetPlay().WateringCan || Session.GetPlay().IsDying || Session.GetPlay().IsDead || Session.GetHabbo().TaxiChofer > 0)
                return;

            if (!Room.GetGameMap().ValidTile(User.X + 2, User.Y + 2) && !Room.GetGameMap().ValidTile(User.X + 1, User.Y + 1))
            {
                Session.SendWhisper("No es posible acostarse en esta posición. Busca otro sitio.", 1);
                return;
            }

            if (User.Statusses.ContainsKey("sit") || User.isSitting || User.RidingHorse || User.IsWalking)
                return;

            if (Session.GetHabbo().Effects().CurrentEffect > 0 && Session.GetHabbo().Effects().CurrentEffect != 102)
                Session.GetHabbo().Effects().ApplyEffect(0);

            if (!User.Statusses.ContainsKey("lay"))
            {
                if ((User.RotBody % 2) == 0)
                {
                    if (User == null)
                        return;

                    try
                    {
                        User.Statusses.Add("lay", "1.0 null");
                        User.Z -= 0.35;
                        User.isLying = true;
                        User.UpdateNeeded = true;
                    }
                    catch { }
                }
                else
                {
                    User.RotBody--;//
                    User.Statusses.Add("lay", "1.0 null");
                    User.Z -= 0.35;
                    User.isLying = true;
                    User.UpdateNeeded = true;
                }

            }
            else
            {
                User.Z += 0.35;
                User.Statusses.Remove("lay");
                User.Statusses.Remove("1.0");
                User.isLying = false;
                User.UpdateNeeded = true;
            }
        }
    }
}