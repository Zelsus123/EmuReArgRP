using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;

using Plus.HabboHotel.Users;
using Plus.HabboHotel.GameClients;

using Plus.Database.Interfaces;
using Plus.HabboHotel.Users.Effects;


namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator
{
    class GobCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "mod_tools"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Acivas inmunidad gubernamental"; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;


            User.GetClient().GetPlay().IsGobMode = !User.GetClient().GetPlay().IsGobMode;

            if (User.GetClient().GetPlay().IsGobMode)
            {
                User.GetClient().GetPlay().GobMode = true;
                User.GetClient().GetRoomUser().ApplyEffect(EffectsList.Staff);
                User.LastBubble = 23;
                //Session.SendWhisper("¡Has Entrado en modo Gobierno!, Ahora eres inmune a todo.", 23);
                string message = "He entrado en servicio de gobierno.";
                PlusEnvironment.GetGame().GetClientManager().StaffRadioAlert(message, Session);


            }
            else
            {
                Session.SendWhisper("¡Ya te encuentras en modo Gobierno!", 1);
            }
        }
    }
}
