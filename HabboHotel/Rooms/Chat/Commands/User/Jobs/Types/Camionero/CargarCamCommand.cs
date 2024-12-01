﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboRoleplay.Misc;
using Plus.HabboRoleplay.Weapons;
using Plus.HabboHotel.RolePlay.PlayRoom;
using Plus.HabboRoleplay.Vehicles;
using Plus.HabboRoleplay.VehicleOwned;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Bank
{
    class CargarCamCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_jobs_cargar_camion"; }
        }

        public string Parameters
        {
            get { return "%id%"; }
        }

        public string Description
        {
            get { return "Conduciendo un camión, elije una Carga. Usa :cargas para ver el listado de ellas."; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            #region Conditions
            if (RoleplayManager.PurgeEvent)
            {
                Session.SendWhisper("¡No puedes trabajar durante la purga!", 1);
                return;
            }
            if (Params.Length != 2)
            {
                Session.SendWhisper("Comando Inválido. ((Usa ':cargarcamion [id]'))", 1);
                return;
            }
            
            if (Session.GetPlay().TryGetCooldown("cargcam"))
                return;

            #endregion
            
            #region Group Conditions
            List<Group> Groups = PlusEnvironment.GetGame().GetGroupManager().GetJobsForUser(Session.GetHabbo().Id);

            if (Groups.Count <= 0)
            {
                Session.SendWhisper("No tienes ningún trabajo para hacer eso.", 1);
                return;
            }

            int GroupNumber = -1;

            if (Groups[0].GType != 2)
            {
                if (Groups.Count > 1)
                {
                    if (Groups[1].GType != 2)
                    {
                        Session.SendWhisper("((No perteneces a ningún trabajo usar ese comando))", 1);
                        return;
                    }
                    GroupNumber = 1; // Segundo indicie de variable
                }
                else
                {
                    Session.SendWhisper("((No perteneces a ningún trabajo para usar ese comando))", 1);
                    return;
                }
            }
            else
            {
                GroupNumber = 0; // Primer indice de Variable Group
            }

            Session.GetPlay().JobId = Groups[GroupNumber].Id;
            Session.GetPlay().JobRank = Groups[GroupNumber].Members[Session.GetHabbo().Id].UserRank;
            #endregion

            #region Extra Conditions            
            // Existe el trabajo?
            if (!PlusEnvironment.GetGame().GetGroupManager().JobExists(Session.GetPlay().JobId, Session.GetPlay().JobRank))
            {
                Session.GetPlay().TimeWorked = 0;
                Session.GetPlay().JobId = 0; // Desempleado
                Session.GetPlay().JobRank = 0;

                //Room.Group.DeleteMember(Session.GetHabbo().Id);// OJO ACÁ

                Session.SendWhisper("Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.", 1);
                return;
            }

            if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(Session, "camionero"))
            {
                Session.SendWhisper("Debes tener el trabajo de Camionero para usar ese comando.", 1);
                return;
            }

            if (!Session.GetPlay().DrivingCar)
            {
                Session.SendWhisper("Debes conducir un Camión para hacer eso.", 1);
                return;
            }

            #region Get Information form VehiclesManager
            Vehicle vehicle = null;
            int corp = 0;
            foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
            {
                if (Session.GetPlay().CarEffectId == Vehicle.EffectID)
                {
                    vehicle = Vehicle;
                    corp = Convert.ToInt32(Vehicle.CarCorp);
                }
            }
            if (vehicle == null)
            {
                Session.SendWhisper("¡Ha ocurrido un error al buscar los datos del vehículo que conduces!", 1);
                return;
            }
            #endregion

            if (!PlusEnvironment.GetGame().GetGroupManager().GetJob(corp).Name.Contains("Camioneros"))
            {
                Session.SendWhisper("Debes conducir un Camión para hacer eso.", 1);
                return;
            }

            string MyCity1 = Room.City;
            int CamRoomID = PlusEnvironment.GetGame().GetPlayRoomManager().TryToGetCamioneros(MyCity1, out PlayRoom Data);//camioneros de la cd.
            if (Session.GetHabbo().CurrentRoomId != CamRoomID)
            {
                Session.SendWhisper("¡Debes estar en la zona de cargamento para Camioneros!", 1);
                return;
            }
            #endregion

            #region Camionero Conditions
            List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedList(Session.GetPlay().DrivingCarId);
            if (VO == null || VO.Count <= 0)
            {
                Session.SendWhisper("((No se pudo obtener información del vehículo que conduces))", 1);
                return;
            }
            if (VO[0].CamOwnId > 0 && VO[0].CamOwnId != Session.GetHabbo().Id)
            {
                Session.SendWhisper("Este camión ya se encuentra cargado por otra persona.", 1);
                return;
            }
            // Para controlar que cargue solo un camion a la vez.
            if (Session.GetPlay().CamCargId > 0)
            {
                Session.SendWhisper("Ya has cargado un Camión. No puedes hacer más de un recorrido a la vez. Usa ':abandonarcarga' para comenzar uno nuevo.", 1);
                return;
            }
            if (VO[0].CamState > 0)
            {
                Session.SendWhisper("Tu camión ya fue cargado. ¡Ve a entregar la carga a tu destino!", 1);
                return;
            }
            
            #region Comodin Conditions
            Item BTile = null;
            BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Session.GetRoomUser().Coordinate);
            if (BTile == null)
            {
                Session.SendWhisper("Debes estar en la zona de Cargamento para Cargar tu camión", 1);
                return;
            }
            #endregion

            if (Session.GetPlay().IsCamLoading)
            {
                Session.SendWhisper("Ya te encuentras cargando el camión. Por favor espera...", 1);
                return;
            }
            #endregion

            #region Execute
            int ID;
            if (int.TryParse(Params[1], out ID))
            {
                if (ID < 1 || ID > 4)
                {
                    Session.SendWhisper("ID de carga inválida. Usa :cargas para ver un listado de ellas.", 1);
                    return;
                }

                if (Session.GetPlay().PassiveMode && (ID == 3 || ID == 4))
                {
                    Session.SendWhisper("¡No puedes llevar cargamentos ilegales en modo pasivo!", 1);
                    return;
                }

                if (RoleplayManager.getCamCargDest(Room, ID) < 1)
                {
                    Session.SendNotification("Al parecer no hay destinos para entregar "+ RoleplayManager.getCamCargName(ID) + " en esta Ciudad. ((Contacta con un Administrador))");
                    return;
                }

                
                VO[0].CamDest = RoleplayManager.getCamCargDest(Room, ID);
                Session.GetPlay().CamCargId = ID;

                // Timer
                Session.GetPlay().IsCamLoading = true;
                Session.GetPlay().LoadingTimeLeft = RoleplayManager.CamCargTime;
                
                RoleplayManager.Shout(Session, "*Comienza a cargar su camión*", 5);
                Session.SendWhisper("Debes esperar " + Session.GetPlay().LoadingTimeLeft + " segundo(s)...", 1);
                Session.GetPlay().TimerManager.CreateTimer("general", 1000, true);
                Session.GetPlay().CooldownManager.CreateCooldown("cargcam", 1000, 5);
            }
            else
            {
                Session.SendWhisper("Ingresa una ID válida. ((:cargarcamion [ID]))", 1);
                return;
            }
            #endregion
        }
    }
}
