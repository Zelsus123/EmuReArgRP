using System;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat.Commands;
using Plus.HabboHotel.Users.Effects;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Moderator
{
    class GobOffCommand : IChatCommand
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
            get { return "Desactivas la inmunidad gubernamental (modo gobierno)."; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            // Obtén al usuario en la sala
            RoomUser User = Room.GetRoomUserManager().GetRoomUserByHabbo(Session.GetHabbo().Id);
            if (User == null)
                return;

            // Verifica si el usuario está en GobMode
            if (User.GetClient().GetPlay().IsGobMode)
            {
                // Desactiva GobMode y resetea propiedades
                User.GetClient().GetPlay().IsGobMode = false; // Aquí desactivamos explícitamente IsGobMode
                User.GetClient().GetPlay().GobMode = false;
                User.GetClient().GetRoomUser().ApplyEffect(EffectsList.None); // Remueve el efecto visual
                User.LastBubble = 0; // Resetea la burbuja de chat

                // Enviar mensaje al usuario confirmando la salida
                Session.SendWhisper("¡Has salido del servicio de gobierno!", 1);

                // Notificar a todos los staffs mediante StaffRadioAlert
                string message = $"{Session.GetHabbo().Username} ha salido del servicio de gobierno.";
                PlusEnvironment.GetGame().GetClientManager().StaffRadioAlert(message, Session);
            }
            else
            {
                // Si el usuario no está en GobMode, notifica que no está en servicio
                Session.SendWhisper("No estás en servicio de gobierno.", 1);
            }
        }
    }
}
