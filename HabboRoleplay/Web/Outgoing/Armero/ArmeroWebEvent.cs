﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plus.HabboHotel.Users;
using Fleck;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.GameClients;
using System.IO;
using Plus.HabboRoleplay.Misc;
using Plus.HabboRoleplay.Houses;
using System.Collections.Generic;
using Plus.HabboRoleplay.Apartments;
using Plus.HabboHotel.Rooms;
using Plus.HabboRoleplay.ApartmentsOwned;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.RolePlay.PlayRoom;
using Plus.HabboRoleplay.Weapons;
using Plus.HabboRoleplay.GangTurfs;

namespace Plus.HabboHotel.Roleplay.Web.Outgoing.Misc
{
    /// <summary>
    /// ArmeroWebEvent class.
    /// </summary>
    class ArmeroWebEvent : IWebEvent
    {
        /// <summary>
        /// Executes socket data.
        /// </summary>
        /// <param name="Client"></param>
        /// <param name="Data"></param>
        /// <param name="Socket"></param>
        public void Execute(GameClient Client, string Data, IWebSocketConnection Socket)
        {

            if (!PlusEnvironment.GetGame().GetWebEventManager().SocketReady(Client, true) || !PlusEnvironment.GetGame().GetWebEventManager().SocketReady(Socket))
                return;

            string Action = (Data.Contains(',') ? Data.Split(',')[0] : Data);

            switch (Action)
            {

                #region Open Pieces
                case "open_pieces":
                    {
                        if (Client.GetPlay().TryGetCooldown("viewarmero"))
                            return;

                        Socket.Send("compose_armero|open_pieces|" + Client.GetPlay().ArmPieces + "|" + Client.GetPlay().ArmMat + "|" + Client.GetPlay().ArmMat / 10);
                        Client.GetPlay().ViewArmeroPieces = true;
                        Client.GetPlay().CooldownManager.CreateCooldown("viewarmero", 1000, 3);
                    }
                    break;
                #endregion

                #region Open Armas
                case "open_weapons":
                    {
                        if (Client.GetPlay().TryGetCooldown("viewarmero"))
                            return;

                        Socket.Send("compose_armero|open_weapons|" + Client.GetPlay().ArmPieces);
                        Client.GetPlay().ViewArmeroWeapons = true;
                        Client.GetPlay().CooldownManager.CreateCooldown("viewarmero", 1000, 3);
                    }
                    break;
                #endregion

                #region Close Pieces
                case "close_pieces":
                    {
                        Client.GetPlay().ViewArmeroPieces = false;
                        Socket.Send("compose_armero|close_pieces|");
                        break;
                    }
                #endregion

                #region Close Weapons
                case "close_weapons":
                    {
                        Client.GetPlay().ViewArmeroWeapons = false;
                        Socket.Send("compose_armero|close_weapons|");
                        break;
                    }
                #endregion

                #region Create Pieces
                case "create_pieces":
                    {
                        if (Client.GetPlay().TryGetCooldown("crear"))
                            return;

                        if (Client.GetRoomUser() == null || Client.GetRoomUser().GetRoom() == null)
                            return;

                        Room Room = Client.GetRoomUser().GetRoom();

                        if (Room == null)
                            return;

                        string MyCity = Room.City;

                        #region Group Conditions
                        List<Group> Groups = PlusEnvironment.GetGame().GetGroupManager().GetJobsForUser(Client.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Socket.Send("compose_armero|armmsg|No tienes ningún trabajo para hacer eso.");
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Socket.Send("compose_armero|armmsg|((No perteneces a ningún trabajo usar ese comando))");
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Socket.Send("compose_armero|armmsg|((No perteneces a ningún trabajo para usar ese comando))");
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Client.GetPlay().JobId = Groups[GroupNumber].Id;
                        Client.GetPlay().JobRank = Groups[GroupNumber].Members[Client.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!PlusEnvironment.GetGame().GetGroupManager().JobExists(Client.GetPlay().JobId, Client.GetPlay().JobRank))
                        {
                            Client.GetPlay().TimeWorked = 0;
                            Client.GetPlay().JobId = 0; // Desempleado
                            Client.GetPlay().JobRank = 0;

                            //Room.Group.DeleteMember(Client.GetHabbo().Id);// OJO ACÁ

                            Socket.Send("compose_armero|armmsg|Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.");
                            return;
                        }

                        if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(Client, "armero"))
                        {
                            Socket.Send("compose_armero|armmsg|Debes tener el trabajo de Armero para usar ese comando.");
                            return;
                        }

                        int ArmID = PlusEnvironment.GetGame().GetPlayRoomManager().TryToGetArmeros(MyCity, out PlayRoom mData);//armeros de la cd.
                        if (Client.GetHabbo().CurrentRoomId != ArmID)
                        {
                            Socket.Send("compose_armero|armmsg|Debes ir a la Fábrica de Armas para hacer eso.");
                            return;
                        }

                        if (Client.GetPlay().PassiveMode)
                        {
                            Socket.Send("compose_armero|armmsg|No puedes hacer eso mientras estás en modo pasivo.");
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carro" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Socket.Send("compose_armero|armmsg|Debes acercarte al punto de creación de Piezas de la Fábrica.");
                            return;
                        }
                        #endregion

                        #region Execute
                        if (Client.GetPlay().ArmMat <= 0)
                        {
                            Socket.Send("compose_armero|armmsg|No tienes materiales para crear piezas.");
                            return;
                        }

                        int Pieces = Client.GetPlay().ArmMat / 10;

                        RoleplayManager.Shout(Client, "*Usa " + Client.GetPlay().ArmMat + " materiales para crear " + Pieces + " piezas*", 5);
                        Socket.Send("compose_armero|armmsg|<b style='color:green;'>¡Bien hecho! Ahora dirígte al punto de fabricación de armas.</b>");
                        Client.GetPlay().ArmPieces += Pieces;
                        Client.GetPlay().ArmMat = 0;
                        RoleplayManager.JobSkills(Client, Client.GetPlay().JobId, Client.GetPlay().ArmLvl, Client.GetPlay().ArmXP);
                        Client.GetPlay().CooldownManager.CreateCooldown("crear", 1000, 3);

                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_armero", "open_pieces");
                        #endregion
                    }
                    break;
                #endregion

                #region Create Weapons
                case "create_weapons":
                    {
                        if (Client.GetPlay().TryGetCooldown("crear"))
                            return;

                        if (Client.GetRoomUser() == null || Client.GetRoomUser().GetRoom() == null)
                            return;

                        Room Room = Client.GetRoomUser().GetRoom();

                        if (Room == null)
                            return;

                        string MyCity = Room.City;

                        #region Group Conditions
                        List<Group> Groups = PlusEnvironment.GetGame().GetGroupManager().GetJobsForUser(Client.GetHabbo().Id);

                        if (Groups.Count <= 0)
                        {
                            Socket.Send("compose_armero|armmsg|No tienes ningún trabajo para hacer eso.");
                            return;
                        }

                        int GroupNumber = -1;

                        if (Groups[0].GType != 2)
                        {
                            if (Groups.Count > 1)
                            {
                                if (Groups[1].GType != 2)
                                {
                                    Socket.Send("compose_armero|armmsg|((No perteneces a ningún trabajo usar ese comando))");
                                    return;
                                }
                                GroupNumber = 1; // Segundo indicie de variable
                            }
                            else
                            {
                                Socket.Send("compose_armero|armmsg|((No perteneces a ningún trabajo para usar ese comando))");
                                return;
                            }
                        }
                        else
                        {
                            GroupNumber = 0; // Primer indice de Variable Group
                        }

                        Client.GetPlay().JobId = Groups[GroupNumber].Id;
                        Client.GetPlay().JobRank = Groups[GroupNumber].Members[Client.GetHabbo().Id].UserRank;
                        #endregion

                        #region Extra Conditions            
                        // Existe el trabajo?
                        if (!PlusEnvironment.GetGame().GetGroupManager().JobExists(Client.GetPlay().JobId, Client.GetPlay().JobRank))
                        {
                            Client.GetPlay().TimeWorked = 0;
                            Client.GetPlay().JobId = 0; // Desempleado
                            Client.GetPlay().JobRank = 0;

                            //Room.Group.DeleteMember(Client.GetHabbo().Id);// OJO ACÁ

                            Socket.Send("compose_armero|armmsg|Lo sentimos, ese trabajo no existe. Te hemos removido ese trabajo.");
                            return;
                        }

                        if (!PlusEnvironment.GetGame().GetGroupManager().HasJobCommand(Client, "armero"))
                        {
                            Socket.Send("compose_armero|armmsg|Debes tener el trabajo de Armero para usar ese comando.");
                            return;
                        }

                        int ArmID = PlusEnvironment.GetGame().GetPlayRoomManager().TryToGetArmeros(MyCity, out PlayRoom mData);//armeros de la cd.
                        if (Client.GetHabbo().CurrentRoomId != ArmID)
                        {
                            Socket.Send("compose_armero|armmsg|Debes ir a la Fábrica de Armas para hacer eso.");
                            return;
                        }

                        if (Client.GetPlay().PassiveMode)
                        {
                            Socket.Send("compose_armero|armmsg|No puedes hacer eso mientras estás en modo pasivo.");
                            return;
                        }
                        #endregion

                        #region Comodin Conditions
                        Item BTile = null;
                        BTile = Room.GetRoomItemHandler().GetFloor.FirstOrDefault(x => x.GetBaseItem().ItemName.ToLower() == "comodin_carr2" && x.Coordinate == Client.GetRoomUser().Coordinate);
                        if (BTile == null)
                        {
                            Socket.Send("compose_armero|armmsg|Debes acercarte al punto de creación de Piezas de la Fábrica.");
                            return;
                        }
                        #endregion

                        string[] ReceivedData = Data.Split(',');
                        string Type = ReceivedData[1].ToString();

                        #region Weapon Check
                        Weapon weapon = null;
                        foreach (Weapon Weapon in WeaponManager.Weapons.Values)
                        {
                            if (Type.ToLower() == Weapon.Name.ToLower())
                            {
                                Type = "weapon";
                                weapon = Weapon;
                            }
                        }
                        #endregion

                        #region Weapon Conditions
                        if (weapon == null)
                        {
                            Socket.Send("compose_armero|armmsg|'" + Type + "' no es un arma válida.");
                            return;
                        }
                        if (Client.GetPlay().OwnedWeapons.ContainsKey(weapon.Name))
                        {
                            Socket.Send("compose_armero|armmsg|Ya tienes una " + weapon.PublicName + " en tu inventario. ¡No es posible tener dos armas del mismo tipo en tu Inventario!");
                            return;
                        }
                        if (Client.GetPlay().ArmPieces < weapon.Cost)
                        {
                            Socket.Send("compose_armero|armmsg|Necesitas al menos " + weapon.Cost + " piezas para crear una " + weapon.PublicName + ".");
                            return;
                        }
                        if (Client.GetPlay().ArmLvl < weapon.LevelRequirement)
                        {
                            Socket.Send("compose_armero|armmsg|Necesitas al menos nivel " + weapon.LevelRequirement + "de Armero para crear una " + weapon.PublicName + ".");
                            return;
                        }
                        #endregion

                        #region Execute
                        RoleplayManager.Shout(Client, "*Crea un/a " + weapon.PublicName + " y gasta " + weapon.Cost + " piezas*", 5);
                        Client.GetPlay().ArmPieces -= weapon.Cost;
                        RoleplayManager.AddWeapon(Client, weapon);
                        Client.GetPlay().OwnedWeapons = null;
                        Client.GetPlay().OwnedWeapons = Client.GetPlay().LoadAndReturnWeapons();
                        RoleplayManager.JobSkills(Client, Client.GetPlay().JobId, Client.GetPlay().ArmLvl, Client.GetPlay().ArmXP);
                        Client.GetPlay().CooldownManager.CreateCooldown("crear", 1000, 3);

                        Socket.Send("compose_armero|armmsg|<b style='color:green;'>¡Has fabricado una "+ weapon.PublicName +" nueva!.</b>");

                        PlusEnvironment.GetGame().GetWebEventManager().ExecuteWebEvent(Client, "event_armero", "open_weapons");

                        Client.GetPlay().GunsFab++;

                        #region Gang Bonif
                        List<Group> MyGang = PlusEnvironment.GetGame().GetGroupManager().GetGangsForUser(Client.GetHabbo().Id);
                        if (MyGang != null && MyGang.Count > 0)
                        {
                            if (MyGang[0].BankRuptcy)
                            {
                                Client.SendWhisper("Tu banda está en bancarota y no podrás gozar de los beneficios de ella.", 1);
                            }
                            else
                            {
                                int NewTurfsCount = 0;
                                List<GangTurfs> TF = PlusEnvironment.GetGame().GetGangTurfsManager().getTurfsbyGang(MyGang[0].Id);
                                if (TF != null && TF.Count > 0)
                                    NewTurfsCount = TF.Count;

                                int bonif = RoleplayManager.GangsTurfBonif * NewTurfsCount;
                                if (bonif > 0)
                                {
                                    MyGang[0].GangFabGuns++;
                                    MyGang[0].Bank += bonif;
                                    MyGang[0].UpdateStat("gang_fab_guns", MyGang[0].GangFabGuns);
                                    MyGang[0].SetBussines(MyGang[0].Bank, MyGang[0].Stock);

                                    Client.GetHabbo().Credits += bonif;
                                    Client.GetPlay().MoneyEarned += bonif;
                                    Client.GetHabbo().UpdateCreditsBalance();

                                    MyGang[0].AddLog(Client.GetHabbo().Id, Client.GetHabbo().Username + " ha obtenido $ " + String.Format("{0:N0}", bonif) + " para la banda al crear un arma.", bonif);
                                    Client.SendWhisper("¡Tu banda y tú han ganado una bonificación extra de $ " + String.Format("{0:N0}", bonif) + " por la creación de esta arma!", 1);
                                }
                            }
                        }
                        #endregion
                        #endregion
                    }
                    break;
                    #endregion
            }
        }
    }
}
