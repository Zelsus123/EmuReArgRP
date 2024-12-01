﻿using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Plus.HabboHotel.Groups;
using Plus.HabboHotel.GameClients;
using Plus.HabboRoleplay.Misc;
using Plus.HabboRoleplay.Weapons;
using System.Data;
using Plus.HabboRoleplay.Vehicles;
using Plus.HabboHotel.Items;
using Plus.HabboRoleplay.VehicleOwned;

namespace Plus.HabboHotel.Rooms.Chat.Commands.Users.Jobs.Types.Bank
{
    class ReviewMecCommand : IChatCommand
    {
        public string PermissionRequired
        {
            get { return "command_jobs_mec_review"; }
        }

        public string Parameters
        {
            get { return ""; }
        }

        public string Description
        {
            get { return "Siendo Mecánico, revisa el vehículo que tengas en frente. O bien, siendo armero, revisar el arma de alguien a reparar."; }
        }

        public void Execute(GameClients.GameClient Session, Rooms.Room Room, string[] Params)
        {
            string Job = "";
            int Dir = 0;
            GameClient Target = null;
            RoomUser TargetUser = null;

            if (RoleplayManager.PurgeEvent)
            {
                Session.SendWhisper("¡No puedes trabajar durante la purga!", 1);
                return;
            }

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

            if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(Session, "reparar"))
            {
                Session.SendWhisper("Tu trabajo actual no te permite hacer uso de ese comando. ¡Solo Mecánicos ó Armeros!", 1);
                return;
            }

            if (!Session.GetPlay().IsWorking)
            {

                if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(Session, "armero"))
                {
                    Session.SendWhisper("Debes tener el uniforme de Mecánico para hacer eso.", 1);
                    return;
                }

                Job = "armero";
            }
            else if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(Session, "mecanico"))
            {
                Session.SendWhisper("Tu trabajo actual no te permite hacer uso de ese comando. ¡Solo Mecánicos ó Armeros!", 1);
                return;
            }

            #endregion

            #region Conditions
            if (Params.Length == 1)
            {
                // Mecánico
                Dir = 1;
            }
            if (!Job.Contains("armero") && Params.Length != 1)
            {
                Session.SendWhisper("Comándo inválido, quizás quisiste decir: ':revisar' frente a un vehículo", 1);
                return;
            }
            else if (Params.Length > 2)
            {
                Session.SendWhisper("Comándo inválido, quizás quisiste decir: ':revisar [usuario]'", 1);
                return;
            }
            else if(Dir != 1)
            {
                Target = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(Params[1]);
                if (Target == null)
                {
                    Session.SendWhisper("No se ha podido encontrar al usuario.", 1);
                    return;
                }

                TargetUser = Room.GetRoomUserManager().GetRoomUserByHabbo(Target.GetHabbo().Username);
                if (TargetUser == null)
                {
                    Session.SendWhisper("Ha ocurrido un error en encontrar al usuario, probablemente esté desconectado o no está en la Zona.", 1);
                    return;
                }
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
                Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                return;
            }
            if (Session.GetPlay().IsDying)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras estás muert@!", 1);
                return;
            }
            if (Session.GetPlay().DrivingCar)
            {
                Session.SendWhisper("¡No puedes hacer eso mientras conduces!", 1);
                return;
            }
            #endregion

            if (Session.GetPlay().TryGetCooldown("reviewmec"))
                return;

            #endregion

            if (!Job.Contains("armero") && Dir == 1)
            {
                #region Check Vehicle InFront
                int FuelSize = 0;
                Vehicle vehicle = null;
                bool found = false;
                int itemfurni = 0, corp = 0;
                Item BTile = null;
                string itemnm = null;
                foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                {
                    if (!found)
                    {
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == Vehicle.ItemName && x.Coordinate == Session.GetRoomUser().SquareInFront);
                        if (BTile != null)
                        {
                            vehicle = Vehicle;
                            itemfurni = BTile.Id;
                            itemnm = Vehicle.ItemName;
                            corp = Convert.ToInt32(Vehicle.CarCorp);
                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    Session.SendWhisper("¡Debes estar frente al vehículo a revisar!", 1);
                    return;
                }

                #endregion

                #region Select Vehicle State
                int state = 0;
                List<VehiclesOwned> VO = PlusEnvironment.GetGame().GetVehiclesOwnedManager().getVehiclesOwnedByFurniId(itemfurni);
                if (VO == null || VO.Count <= 0)
                {
                    Session.SendWhisper("Este vehículo no necesita ser reparado.", 1);
                    return;
                }
                state = VO[0].State;                
                #endregion

                #region Check Vehicle State
                if (state != 2 && state != 3 && VO[0].CarLife > 0)
                {
                    Session.SendWhisper("Este vehículo no necesita ser reparado.", 1);
                    return;
                }
                #endregion

                #region Calc Repair Kit Cant
                int MecParts = 0;
                if (FuelSize <= 90)// Tanque Pequeño
                {
                    MecParts = 3;
                }
                else if (FuelSize > 90 && FuelSize <= 100)// Tanque Mediano
                {
                    MecParts = 6;
                }
                else // Tanque Grande
                {
                    MecParts = 9;
                }
                #endregion

                #region Execute
                RoleplayManager.Shout(Session, "*Abre el capó del Vehículo y procede a revisarlo*", 5);
                Session.SendWhisper("Este vehículo requiere " + MecParts + " repuestos para ser reparado.", 1);
                Session.GetPlay().CooldownManager.CreateCooldown("reviewmec", 1000, 3);
                #endregion
            }
            else
            {
                if (Params.Length != 2)
                {
                    Session.SendWhisper("Comándo inválido, quizás quisiste decir: ':revisar [usuario]'", 1);
                    return;
                }

                #region Conditions Arm
                if (!Target.GetPlay().PediArm)
                {
                    Session.SendWhisper("Esa persona no ha solicitado los servicios de un Armero.", 1);
                    return;
                }
                if (Target.GetPlay().EquippedWeapon == null)
                {
                    Session.SendWhisper("Esa persona no lleva ningún arma Equipada a ser revisada.", 1);
                    return;
                }
                if (Target.GetPlay().WLife > 0)
                {
                    Session.SendWhisper("Esa arma no necesita una reparación.", 1);
                    return;
                }
                #endregion

                #region Execute
                int NeedPieces = Target.GetPlay().EquippedWeapon.Cost / 4;
                Session.GetPlay().ArmPiecesTo = NeedPieces;
                Session.GetPlay().ArmUserTo = Target.GetHabbo().Id;
                RoleplayManager.Shout(Session, "*Observa el arma de " + Target.GetHabbo().Username + " y procede a examinarla*", 5);
                Session.SendWhisper("Esta arma necesita " + NeedPieces + " pieza(s) para ser reparada.", 1);
                
                Session.GetPlay().CooldownManager.CreateCooldown("reviewmec", 1000, 3);
                #endregion
            }
        }
    }
}
