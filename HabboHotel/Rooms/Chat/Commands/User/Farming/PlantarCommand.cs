using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using Plus.HabboRoleplay.Misc;
using Plus.HabboRoleplay.Vehicles;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.GameClients;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.RolePlay.PlayRoom;
using Plus.HabboRoleplay.VehiclesJobs;
using Plus.HabboRoleplay.VehicleOwned;
using System.Drawing;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Users.Generic.Interactions.Items
{
    class PlantarCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_sembrar"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Siembra plantines para cultivar marihuana."; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            if (Session.GetPlay().TryGetCooldown("sembrar", true))
            {
                Session.SendWhisper("Por favor espera un poco para hacer eso nuevamente.", 1);
                return;
            }

            #region Basic Conditions
            if (Session.GetPlay().Cuffed)
            {
                Session.SendWhisper("No puedes hacer eso mientras estás esposad@", 1);
                return;
            }
            if (!Session.GetRoomUser().CanWalk)
            {
                Session.SendWhisper("Al parecer no puedes ni moverte para hacer eso.", 1);
                return;
            }
            if (Session.GetPlay().Pasajero)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras vas de Pasajer@!", 1);
                return;
            }
            if (Session.GetPlay().IsDead)
            {
                Session.SendWhisper("¡No puedes hacer esto mientras estás muert@!", 1);
                return;
            }
            if (Session.GetPlay().IsJailed)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras estás encarcelad@!", 1);
                return;
            }
            if (Session.GetPlay().IsDying)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras estás muriendo!", 1);
                return;
            }
            if (Session.GetPlay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                return;
            }
            if (Session.GetPlay().EquippedWeapon != null)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras equipas un arma!", 1);
                return;
            }
            #endregion

            #region Conditons
            if (!Room.WeedLabEnabled)
            {
                Session.SendWhisper("Debes estar en un laboratorio de marihuana para cultivar.", 1);
                return;
            }
            if (Session.GetPlay().Plantines < 1)
            {
                Session.SendWhisper("¡No tienes Plantines para cultivar!", 1);
                return;
            }
            if (Session.GetPlay().IsWeedFarming)
            {
                Session.SendWhisper("¡Ya te encuentras cultivando una planta de marihuana.", 1);
                return;
            }
            #endregion

            #region Comodin Conditions
            // Verifica si hay un surco adyacente donde sembrar
            Item BTile = null;
            foreach (var item in Room.GetRoomItemHandler().GetFloor)
            {
                if (item.GetBaseItem().ItemName.ToLower() == "nft_h24_cefalu_pot1" && TilesTouching(item.Coordinate.X, item.Coordinate.Y, Session.GetRoomUser().Coordinate.X, Session.GetRoomUser().Coordinate.Y))
                {
                    BTile = item;
                    break;
                }
            }

            if (BTile == null)
            {
                Session.SendWhisper("Debes estar cerca de un punto de cultivo para sembrar.", 1);
                return;
            }

            // Verifica si ya hay semillas sembradas en el surco adyacente
            if (Room.GetRoomItemHandler().GetFloor.Any(x => x.GetBaseItem().ItemName.ToLower() == "gm_saumuraiono" && x.Coordinate == BTile.Coordinate))
            {
                Session.SendWhisper("Ya hay un plantin en el punto de cultivo adyacente.", 1);
                return;
            }

            if (Session.GetPlay().WateringCan)
            {
                Session.SendWhisper("No puedes sembrar con la regadera en la mano. ((Usa :tirar regadera))", 1);
                return;
            }
            #endregion

            #region Execute
            Session.GetPlay().IsWeedFarming = true;
            Session.GetPlay().LoadingTimeLeft = RoleplayManager.SembrarTime;
            Session.GetPlay().TimerManager.CreateTimer("general", 1000, true);

            RoleplayManager.Shout(Session, "*Coloca un plantin de marihuana con mucho cuidado*", 5);
            Session.GetPlay().CooldownManager.CreateCooldown("sembrar", 1000, 3);
            
            return;
            #endregion
        }

        #region Helper Method
        // Método para verificar si las casillas están tocándose
        public static bool TilesTouching(int X1, int Y1, int X2, int Y2)
        {
            // Verifica si la diferencia absoluta en X y Y es <= 1
            return Math.Abs(X1 - X2) <= 1 && Math.Abs(Y1 - Y2) <= 1;
        }
        #endregion
    }
}
