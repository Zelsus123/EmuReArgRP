﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Plus.HabboHotel.Rooms;
using Plus.HabboRoleplay.Misc;

namespace Plus.Communication.Packets.Incoming.Rooms.Action
{
    class BanUserEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            Room Room = Session.GetHabbo().CurrentRoom;
            if (Room == null)
                return;

            if (((Room.WhoCanBan == 0 && !Room.CheckRights(Session, true) && Room.Group == null) || (Room.WhoCanBan == 1 && !Room.CheckRights(Session)) && Room.Group == null) || (Room.Group != null && !Room.CheckRights(Session, false, true)))
                return;

            int UserId = Packet.PopInt();
            int RoomId = Packet.PopInt();
            string R = Packet.PopString();

            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Convert.ToInt32(UserId));
            if (User == null || User.IsBot)
                return;

            var House = PlusEnvironment.GetGame().GetHouseManager().GetHouseByInsideRoom(Room.Id);
            var ApartInside = PlusEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentByInsideRoom(Room.Id);
            if (House == null)
            {
                if (ApartInside == null)
                {
                    Session.SendWhisper("¡No estás dentro de ninguna Casa/Apartamento para hacer eso!", 1);
                    return;
                }
            }

            #region House
            if (House != null)
            {
                //if (Room.OwnerId == UserId)
                //  return;

                if (House.OwnerId == UserId || User.GetClient().GetHabbo().GetPermissions().HasRight("mod_tool"))
                {
                    Session.SendWhisper("No puedes echar a los propietarios de la casa o Administradores", 1);
                    return;
                }

                long Time = 0;
                if (R.ToLower().Contains("hour"))
                    Time = 3600;
                else if (R.ToLower().Contains("day"))
                    Time = 86400;
                else if (R.ToLower().Contains("perm"))
                    Time = 78892200;

                Room.AddBan(UserId, Time);
                // Habbo Original
                //Room.GetRoomUserManager().RemoveUserFromRoom(User.GetClient(), true, true);
                //PlusEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_SelfModBanSeen", 1);

                // New RP
                RoleplayManager.Shout(Session, "*Ha expulsado a " + User.GetUsername() + " de la Casa*", 5);
                // Enviar a la Sala Exterior y Posición de la Puerta
                User.GetClient().GetPlay().ExitingHouse = true;
                User.GetClient().GetPlay().HouseX = House.DoorX;
                User.GetClient().GetPlay().HouseY = House.DoorY;
                User.GetClient().GetPlay().HouseZ = House.DoorZ;
                RoleplayManager.SendUser(User.GetClient(), House.RoomId, Session.GetHabbo().Username + " te ha expulsado de la casa.");
            }
            #endregion

            #region Apartment
            else if (ApartInside != null)
            {
                if (ApartInside.Owner == UserId || User.GetClient().GetHabbo().GetPermissions().HasRight("mod_tool"))
                {
                    Session.SendWhisper("No puedes echar a los propietarios de la casa o Administradores", 1);
                    return;
                }

                long Time = 0;
                if (R.ToLower().Contains("hour"))
                    Time = 3600;
                else if (R.ToLower().Contains("day"))
                    Time = 86400;
                else if (R.ToLower().Contains("perm"))
                    Time = 78892200;

                Room.AddBan(UserId, Time);
                // Habbo Original
                //Room.GetRoomUserManager().RemoveUserFromRoom(User.GetClient(), true, true);
                //PlusEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_SelfModBanSeen", 1);

                // New RP
                RoleplayManager.Shout(Session, "*Ha expulsado a " + User.GetUsername() + " del apartamento*", 5);
                RoleplayManager.SendUser(User.GetClient(), ApartInside.LobbyId, Session.GetHabbo().Username + " te ha expulsado de l apartamento.");
            }
            #endregion
        }
    }
}