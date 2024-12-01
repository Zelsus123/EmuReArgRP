using System;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Administrator.Administrator
{
    class StaffRadioCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_staff_radio"; } // Permiso necesario
        }

        public string Parameters
        {
            get { return "%message%"; } // Indica que se necesita un mensaje
        }

        public string Description
        {
            get { return "Envía un mensaje de radio a todos los staff conectados."; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            // Validar que se ingresó un mensaje
            if (Params.Length == 1)
            {
                Session.SendWhisper("Por favor, ingresa el mensaje que deseas enviar.", 1);
                return;
            }

            // Verificar permisos de staff
            if (!Session.GetHabbo().GetPermissions().HasRight("staff_rights"))
            {
                Session.SendWhisper("¡No tienes permisos para usar este comando!", 1);
                return;
            }

            // Construir el mensaje
            string Message = CommandManager.MergeParams(Params, 1);

            // Llamar al método StaffRadioAlert de GameClientManager
            PlusEnvironment.GetGame().GetClientManager().StaffRadioAlert(Message, Session, Session.GetHabbo().Id);
        }
    }
}