using System;
using Plus.HabboHotel.GameClients;
using Plus.Core;
using Plus.HabboRoleplay.Misc;
using Plus.HabboRoleplay.VehiclesJobs;
using Plus.HabboHotel.RolePlay.PlayRoom;
using System.Text;
using Plus.HabboRoleplay.Vehicles;
using Plus.HabboHotel.Rooms;
using Plus.HabboRoleplay.VehicleOwned;
using Plus.HabboHotel.Items;
using Plus.Database.Interfaces;
using System.Drawing;

namespace Plus.HabboRoleplay.Events.Methods
{
    /// <summary>
    /// Triggered when the user disconnects
    /// </summary>
    public class OnDisconnect : IEvent
    {
        /// <summary>
        /// Responds to the event
        /// </summary>
        public void Execute(object Source, object[] Params)
        {
            GameClient Client = (GameClient)Source;

            if (Client == null)
                return;

            if (Client.GetPlay() == null)
                return;

            if (Client.GetHabbo() == null)
                return;

            if (Client.GetHabbo()._disconnected)
                return;

            if(Client.GetHabbo().TaxiChofer > 0)
                RoleplayManager.DropTaxi(Client, Client.GetHabbo().Username);

            if (Client.GetPlay().IsWorking)
                WorkManager.RemoveWorkerFromList(Client);

            if(Client.GetHabbo().Rank > 2)
                RoleplayManager.StaffLogOut(Client);

            #region Check Stay in House or Apartment
            CheckHouseApart(Client);
            #endregion

            #region Check CorpCar Respawn
            RoleplayManager.CheckCorpCarp(Client);
            #endregion

            #region CheckDriving 
            CheckDriving(Client);
            #endregion

            #region CheckOnGrua
            PlusEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleGruaBug(Client.GetHabbo().Id, out VehiclesOwned VOD);
            #endregion
            
            #region CheckBasurero
            RoleplayManager.CheckBasurero(Client);
            #endregion

            #region CheckAntiLaw
            RoleplayManager.CheckAntiLaw(Client);
            #endregion

            RoleplayManager.Shout(Client, "((Se ha desconectado))", 7);

            Client.GetPlay().CloseInteractingUserDialogues();
            Client.GetPlay().EndCycle();
            Logging.WriteLine(Client.GetHabbo().Username + " se ha Desconectado.", ConsoleColor.DarkGray);
        }

        #region CheckDriving
        public void CheckDriving(GameClient Client)
        {
            if (Client.GetPlay().DrivingCar)
            {
                #region Vehicle Check
                Vehicle vehicle = null;
                int corp = 0;
                bool ToDB = true;
                foreach (Vehicle Vehicle in VehicleManager.Vehicles.Values)
                {
                    if (Client.GetPlay().CarEnableId == Convert.ToInt32(Vehicle.EffectID))
                    {
                        vehicle = Vehicle;
                        if (vehicle.CarCorp > 0)
                        {
                            corp = vehicle.CarCorp;
                            ToDB = false;
                        }
                    }
                }
                #endregion

                int ItemPlaceId = 0;
                int roomid = Client.GetHabbo().HomeRoom;
                Room Room = RoleplayManager.GenerateRoom(roomid, false);

                object[] Coords = Client.GetPlay().LastCoordinates.Split(',');
                Point SCoords = new Point(Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]));

                if (Client.GetPlay().DrivingInCar || !RoleplayManager.isValidPark(Room, SCoords))
                {
                    RoleplayManager.Shout(Client, "* Una Grúa se ha llevado el vehículo que " + Client.GetHabbo().Username + " conducía por encontrarse mal estacionado.", 4);
                    // Actualizamos datos del auto en el diccionario y DB
                    PlusEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwnerDisc(Client, 0, ToDB, roomid, Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]), Convert.ToInt32(Coords[3]), out VehiclesOwned VOD);
                    if (VOD != null)
                        ItemPlaceId = VOD.Id;

                    if (corp > 0)
                    {
                        PlusEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Client.GetPlay().DrivingCarId);
                        RoleplayManager.CheckCorpCarp(Client);
                    }

                }
                else if (Client.GetPlay().DrivingCar)
                {
                    if (corp > 0)
                    {
                        RoleplayManager.Shout(Client, "* Una Grúa se ha llevado el vehículo que " + Client.GetHabbo().Username + " conducía por encontrarse mal estacionado.", 4);
                        PlusEnvironment.GetGame().GetVehiclesOwnedManager().DeleteVehicleOwned(Client.GetPlay().DrivingCarId);
                        RoleplayManager.CheckCorpCarp(Client);
                    }
                    else
                    {
                        // Colocamos Furni en Sala
                        Client.GetPlay().isParking = true;
                        Item ItemPlace = RoleplayManager.PutItemToRoom(Client, Client.GetPlay().DrivingCarItem, roomid, vehicle.ItemID, Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]), Convert.ToInt32(Coords[3]), ToDB);
                        //Item ItemPlace = RoleplayManager.PlaceItemToRoom(Client, vehicle.ItemID, 0, Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]), Convert.ToInt32(Coords[2]), Convert.ToInt32(Coords[3]), false, Room.Id, ToDB, "");
                        Client.GetPlay().isParking = false;
                        if (ItemPlace != null)
                            ItemPlaceId = ItemPlace.Id;
                        else
                            ItemPlaceId = 0;
                        // Actualizamos datos del auto en el diccionario y DB
                        PlusEnvironment.GetGame().GetVehiclesOwnedManager().UpdateVehicleOwnerDisc(Client, ItemPlaceId, ToDB, roomid, Convert.ToInt32(Coords[0]), Convert.ToInt32(Coords[1]), Convert.ToInt32(Coords[3]), out VehiclesOwned VOD);
                    }
                }

                #region Extra Conditions & Checks
                #region CorpCar Respawn
                if (corp > 0)
                {
                    Client.GetPlay().CarJobLastItemId = ItemPlaceId;
                }
                #endregion

                #region Pasajeros (Algoritmo replicado en ConditionCheckTimer por seguridad)
                //Vars
                string Pasajeros = Client.GetPlay().Pasajeros;
                string[] stringSeparators = new string[] { ";" };
                string[] result;
                result = Pasajeros.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string psjs in result)
                {
                    GameClient PJ = PlusEnvironment.GetGame().GetClientManager().GetClientByUsername(psjs);
                    if (PJ != null)
                    {
                        if (PJ.GetPlay().ChoferName == Client.GetHabbo().Username)
                        {
                            RoleplayManager.Shout(PJ, "*Baja del vehículo de " + Client.GetHabbo().Username + "*", 5);
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
                        StringBuilder builder = new StringBuilder(Client.GetPlay().Pasajeros);
                        builder.Replace(PJ.GetHabbo().Username + ";", "");
                        Client.GetPlay().Pasajeros = builder.ToString();

                        // CHOFER 
                        Client.GetPlay().Chofer = (Client.GetPlay().PasajerosCount <= 0) ? false : true;
                        //Client.GetRoomUser().AllowOverride = (Client.GetPlay().PasajerosCount <= 0) ? false : true;

                        // SI EL PASAJERO ES UN HERIDO
                        if (PJ.GetPlay().IsDying)
                        {
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
                    }
                }
                #endregion

                #region Check Jobs

                #region Taxista
                if (Client.GetPlay().Ficha != 0)
                {
                    Client.GetPlay().Ficha = 0;
                    Client.GetPlay().FichaTimer = 0;
                    RoleplayManager.Shout(Client, "*Apaga el Taxímetro y deja de trabajar*", 5);
                }
                #endregion

                #region Basurero
                if (Client.GetPlay().BasuTeamId <= 0)
                    Client.GetPlay().IsBasuChofer = false;
                #endregion

                #endregion
                #endregion

                #region Online ParkVars
                //Retornamos a valores predeterminados
                Client.GetPlay().DrivingCar = false;
                Client.GetPlay().DrivingInCar = false;
                Client.GetPlay().DrivingCarId = 0;// Id de VehiclesOwned;

                //Combustible System
                Client.GetPlay().CarType = 0;// Define el gasto de combustible
                Client.GetPlay().CarFuel = 0;
                Client.GetPlay().CarMaxFuel = 0;
                Client.GetPlay().CarTimer = 0;
                Client.GetPlay().CarLife = 0;

                Client.GetPlay().CarEnableId = 0;//Coloca el enable para conducir
                Client.GetPlay().CarEffectId = 0;//Guarda el enable del último auto en conducción.
                //Client.GetRoomUser().ApplyEffect(0);
                //Client.GetRoomUser().FastWalking = false;
                #endregion
            }
        }
        #endregion

        #region CheckHouseApart
        public void CheckHouseApart(GameClient Client)
        {
            if (Client.GetRoomUser() != null)
            {
                var HouseInside = PlusEnvironment.GetGame().GetHouseManager().GetHouseByInsideRoom(Client.GetRoomUser().RoomId);

                if (HouseInside != null)//Estamos dentro de una casa
                {
                    Client.GetPlay().LastCoordinates = HouseInside.DoorX + "," + HouseInside.DoorY + "," + HouseInside.DoorZ + "," + 0;
                    Client.GetHabbo().HomeRoom = HouseInside.RoomId;
                }
                else
                {
                    var ApartInside = PlusEnvironment.GetGame().GetApartmentOwnedManager().GetApartmentByInsideRoom(Client.GetRoomUser().RoomId);

                    if (ApartInside != null)
                    {
                        Client.GetPlay().LastCoordinates = 0 + "," + 0 + "," + 0 + "," + 0;
                        Client.GetHabbo().HomeRoom = ApartInside.LobbyId;
                    }
                    else
                    {
                        Client.GetPlay().LastCoordinates = Client.GetRoomUser().X + "," + Client.GetRoomUser().Y + "," + Client.GetRoomUser().Z + "," + Client.GetRoomUser().RotBody;
                        Client.GetHabbo().HomeRoom = Client.GetRoomUser().RoomId;
                    }
                }
            }
        }
        #endregion

    }
}