﻿using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat.Styles;
using Plus.HabboRoleplay.RoleplayUsers;
using Plus.HabboHotel.Groups;
using Plus.HabboRoleplay.Misc;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Police
{
    class UnStunCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_police_stun_undo"; }
        }

        public string Parameters
        {
            get { return "%user%"; }
        }

        public string Description
        {
            get { return "Desparaliza a una persona que ha sufrido alguno de estos ataques."; }
        }

        public void Execute(GameClient Session, Rooms.Room Room, string[] Params)
        {

            #region Conditions
            if (Params.Length == 1)
            {
                Session.SendWhisper("Debes ingresar el nombre de la persona.", 1);
                return;
            }
            
            GameClient TargetClient = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
            if (TargetClient == null)
            {
                Session.SendWhisper("Ha ocurrido un error al buscar a la persona, probablemente esté desconectada.", 1);
                return;
            }

            RoomUser RoomUser = Session.GetRoomUser();
            RoomUser TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(TargetClient.GetHabbo().Username);
            if (TargetUser == null)
            {
                Session.SendWhisper("Ha ocurrido un error al buscar a la persona, probablemente esté desconectada o no está en esta zona.", 1);
                return;
            }

            if (!Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                if (TargetClient.GetHabbo().Id == Session.GetHabbo().Id)
                {
                    Session.SendWhisper("No puedes hacerte eso a ti mism@.", 1);
                    return;
                }
                if (PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(TargetClient, "law"))
                {
                    Session.SendWhisper("¡No puedes hacer eso entre compañeros de trabajo!", 1);
                    return;
                }
            }

            if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(Session, "unstun") && !Session.GetPlay().PoliceTrial && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                Session.SendWhisper("¡Solo un oficial de policía puede hacer eso!", 1);
                return;
            }

            if (!Session.GetPlay().IsWorking && !Session.GetPlay().PoliceTrial && !Session.GetHabbo().GetPermissions().HasRight("corporation_rights"))
            {
                Session.SendWhisper("Debes estar trabajando para hacer eso.", 1);
                return;
            }
            if (Session != TargetClient && TargetClient.GetHabbo().Rank > 3 && TargetClient.GetHabbo().Rank >= Session.GetHabbo().Rank)
            {
                Session.SendWhisper("((No puedes hacerle eso a un miembro de la administración))", 1);
                return;
            }
            if (Session.GetHabbo().TaxiChofer > 0)
            {
                Session.SendWhisper("((No puedes hacer eso mientras vas en taxi))", 1);
                return;
            }
            if (TargetClient.GetPlay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona mientras va conduciendo!", 1);
                return;
            }
            if (TargetClient.GetPlay().Pasajero || TargetClient.GetHabbo().TaxiChofer > 0)
            {
                Session.SendWhisper("¡No puedes hacerle eso a una persona mientras va de pasajer@!", 1);
                return;
            }
            if (!TargetClient.GetRoomUser().Frozen)
            {
                Session.SendWhisper("No puedes desparalizar a alguien que no está paralizado.", 1);
                
                return;
            }

            if (TargetUser.IsAsleep)
            {
                Session.SendWhisper("¡No puedes desparalizar a un usuario ausente!", 1);
                return;
            }
            #endregion

            #region Execute
            Point ClientPos = new Point(RoomUser.X, RoomUser.Y);
            Point TargetClientPos = new Point(TargetUser.X, TargetUser.Y);
            double Distance = RoleplayManager.GetDistanceBetweenPoints2D(ClientPos, TargetClientPos);

            if (Distance <= 1)
            {
                RoleplayManager.Shout(Session, "*Ayuda a " + TargetClient.GetHabbo().Username + ", dándole tiempo para recuperarse de su aturdimiento*", 37);
                
                TargetClient.GetRoomUser().Frozen = false;
                TargetClient.GetRoomUser().CanWalk = true;

                return;
            }
            else
            {
                Session.SendWhisper("Debes estar más cerca de la persona.", 1);
                
                return;
            }
            #endregion
        }
    }
}