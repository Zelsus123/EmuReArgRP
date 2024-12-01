using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Plus.HabboRoleplay.Misc;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.RolePlay.PlayRoom;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Misc
{
    // CONDICIONES CUANDO SE DESCONECTA EL USUARIO
    class DisconnectEvent : IPacketEvent
    {
        public void Parse(HabboHotel.GameClients.GameClient Session, ClientPacket Packet)
        {
            ChoferCheck(Session);
            PasajeroCheck(Session);
            EscortingCheck(Session);
            EscortedCheck(Session);

            Session.Disconnect();
        }
        // DriveCheck => Mandar a Grua
        // WantedCheck => isJailed = True; * Comprobar en Base de Datos

        #region Chofer Check
        public void ChoferCheck(HabboHotel.GameClients.GameClient Client)
        {
            if (!Client.GetPlay().Chofer)
                return;

            if (Client.GetPlay().IsBasuChofer)
                Client.GetPlay().IsBasuChofer = false;

            //Si lleva pasajeros
            #region Pasajeros
            //Vars
            string Pasajeros = Client.GetPlay().Pasajeros;
            string[] stringSeparators = new string[] { ";" };
            string[] result;
            result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (string psjs in result)
            {
                HabboHotel.GameClients.GameClient PJ = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                if (PJ != null)
                {
                    if (PJ.GetPlay().ChoferName == Client.GetHabbo().Username)
                    {
                        HabboRoleplay.Misc.RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Client.GetHabbo().Username + "*", 5);
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
                    Client.GetPlay().PasajerosCount--;
                    Client.GetPlay().Pasajeros.Replace(PJ.GetHabbo().Username + ";", "");

                    // CHOFER 
                    Client.GetPlay().Chofer = (Client.GetPlay().PasajerosCount <= 0) ? false : true;
                    Client.GetRoomUser().AllowOverride = (Client.GetPlay().PasajerosCount <= 0) ? false : true;

                    // SI EL PASAJERO ES UN HERIDO
                    if (PJ.GetPlay().IsDying)
                    {
                        PJ.SendNotification("((El Médico que te transportaba se ha desconectado. Te enviaremos al Hospital))");
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
        }
        #endregion

        // Pasajero CHECK
        // Descontarle el pasajero al Chofer
        #region Pasajero Check
        public void PasajeroCheck(HabboHotel.GameClients.GameClient Client)
        {
            if (!Client.GetPlay().Pasajero)
                return;

            GameClient Chofer = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(Client.GetPlay().ChoferName);
            if (Chofer != null)
            {
                // Descontamos Pasajero
                Chofer.GetPlay().PasajerosCount--;
                Chofer.GetPlay().Pasajeros.Replace(Client.GetHabbo().Username + ";", "");

                // CHOFER 
                Chofer.GetPlay().Chofer = (Client.GetPlay().PasajerosCount <= 0) ? false : true;
                Chofer.GetRoomUser().AllowOverride = (Client.GetPlay().PasajerosCount <= 0) ? false : true;
            }
        }
        #endregion

        #region Escorting Check
        public void EscortingCheck(HabboHotel.GameClients.GameClient Client)
        {
            if (!Client.GetPlay().EscortingWalk)
                return;

            GameClient Convicto = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(Client.GetPlay().EscortedName);
            if (Convicto != null)
            {
                Convicto.GetPlay().IsEscorted = false;
                Convicto.GetPlay().PoliceEscortingID = 0;
                Convicto.GetPlay().EscortPoliceName = "";
                Convicto.GetRoomUser().AllowOverride = false;
                Convicto.GetRoomUser().TeleportEnabled = false;
                Convicto.GetRoomUser().Frozen = false;
                Convicto.GetRoomUser().CanWalk = false;
            }
            //Client.GetPlay().EscortingWalk = false;
            //Client.GetPlay().EscortedID = 0;
            //Client.GetPlay().EscortedName = "";            
            //Client.GetRoomUser().AllowOverride = false;
        }
        #endregion

        #region Escorted Check
        public void EscortedCheck(HabboHotel.GameClients.GameClient Client)
        {
            if (!Client.GetPlay().IsEscorted)
                return;

            GameClient Police = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(Client.GetPlay().EscortPoliceName);
            /*
            Client.GetPlay().IsEscorted = false;
            Client.GetPlay().PoliceEscortingID = 0;
            Client.GetPlay().EscortPoliceName = "";
            Client.GetRoomUser().AllowOverride = false;
            Client.GetRoomUser().TeleportEnabled = false;
            Client.GetRoomUser().Frozen = false;
            Client.GetRoomUser().CanWalk = false;
            */
            if (Police != null)
            {
                Police.GetPlay().EscortingWalk = false;
                Police.GetPlay().EscortedID = 0;
                Police.GetPlay().EscortedName = "";
                Police.GetRoomUser().AllowOverride = false;
            }
        }
        #endregion
    }
}
