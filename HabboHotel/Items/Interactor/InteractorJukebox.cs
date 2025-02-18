﻿using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Interactor
{
    class InteractorJukebox : IFurniInteractor
    {
        public Room Room
        {
            get;
            private set;
        }

        public void OnPlace(GameClient Session, Item Item)
        {
            /*Item.GetRoom().GetTraxManager().ClearPlayList();
            Item.ExtraData = "0";
            Item.UpdateState();*/
            Item.GetRoom().GetRoomItemHandler().JukeboxCount++;
        }

        public void OnRemove(GameClient Session, Item Item)
        {
            Item.ExtraData = "0";
            Item.UpdateState();
            Item.GetRoom().GetRoomItemHandler().JukeboxCount--;
        }

        public void OnWiredTrigger(Item Item)
        {
            if (Item.GetRoom().GetTraxManager().IsPlaying)
                Item.GetRoom().GetTraxManager().StopPlayList();
            else
                Item.GetRoom().GetTraxManager().PlayPlaylist();
        }

        public void OnTrigger(GameClient Session, Item Item, int Request, bool HasRights)
        {
            Room room = Item.GetRoom();
            bool flag = Request == 0 || Request == 1;
            if (flag)
            {
                room.GetTraxManager().TriggerPlaylistState();
            }
        }
    }
}
