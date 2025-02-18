﻿using System;
using System.Linq;
using System.Collections.Generic;

using Plus.Core;
using Plus.Communication.Packets.Incoming;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;
using Plus.HabboHotel.Groups;
using Plus.HabboHotel.Users.Effects;
using Plus.HabboHotel.Items.Utilities;
using Plus.HabboHotel.Users.Inventory.Bots;

using Plus.HabboHotel.Rooms.AI;
using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.Communication.Packets.Outgoing.Inventory.Bots;
using Plus.Communication.Packets.Outgoing.Inventory.Pets;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.AvatarEffects;
using Plus.Database.Interfaces;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.HabboHotel.Groups.Forums;

namespace Plus.Communication.Packets.Incoming.Catalog
{
    public class PurchaseFromCatalogEvent : IPacketEvent
    {
        public void Parse(GameClient Session, ClientPacket Packet)
        {
            ICollection<Item> FloorItems = Session.GetHabbo().GetInventoryComponent().GetFloorItems();
            ICollection<Item> WallItems = Session.GetHabbo().GetInventoryComponent().GetWallItems();

            if (PlusEnvironment.GetDBConfig().DBData["catalogue_enabled"] != "1")
            {
                Session.SendNotification("The hotel managers have disabled the catalogue");
                return;
            }

            int PageId = Packet.PopInt();
            int ItemId = Packet.PopInt();
            string ExtraData = Packet.PopString();
            int Amount = Packet.PopInt();


            CatalogPage Page = null;
            if (!PlusEnvironment.GetGame().GetCatalog().TryGetPage(PageId, out Page))
                return;

            if (!Page.Enabled || !Page.Visible || Page.MinimumRank  >  Session.GetHabbo().Rank || (Page.MinimumVIP > Session.GetHabbo().VIPRank && Session.GetHabbo().Rank == 1))
                return;

            CatalogItem Item = null;
            if (!Page.Items.TryGetValue(ItemId, out Item))
            {
                if (Page.ItemOffers.ContainsKey(ItemId))
                {
                    Item = (CatalogItem)Page.ItemOffers[ItemId];
                    if (Item == null)
                        return;
                }
                else
                    return;
            }

            if (Amount < 1 || Amount > 100 || !Item.HaveOffer)
                Amount = 1;

            int AmountPurchase = Item.Amount > 1 ? Item.Amount : Amount;

            int TotalCreditsCost = Amount > 1 ? ((Item.CostCredits * Amount) - ((int)Math.Floor((double)Amount / 6) * Item.CostCredits)) : Item.CostCredits;
            int TotalPixelCost = Amount > 1 ? ((Item.CostPixels * Amount) - ((int)Math.Floor((double)Amount / 6) * Item.CostPixels)) : Item.CostPixels;
            int TotalDiamondCost = Amount > 1 ? ((Item.CostDiamonds * Amount) - ((int)Math.Floor((double)Amount / 6) * Item.CostDiamonds)) : Item.CostDiamonds;
            int TotalGotwCost = Amount > 1 ? ((Item.CostGotw * Amount) - ((int)Math.Floor((double)Amount / 6) * Item.CostGotw)) : Item.CostGotw;
            if (Session.GetHabbo().Credits < TotalCreditsCost || Session.GetHabbo().Duckets < TotalPixelCost || Session.GetHabbo().Diamonds < TotalDiamondCost || Session.GetHabbo().GOTWPoints < TotalGotwCost)
                return;

            int LimitedEditionSells = 0;
            int LimitedEditionStack = 0;

            int TotalCount = FloorItems.Count + WallItems.Count;
            if (TotalCount >= 2750)
                {
                Session.SendNotification("Has Superado el máximo de furnis en el inventario, porfavor retira algunos furnis de tu inventario.");
                Session.SendMessage(new PurchaseOKComposer(Item, Item.Data));
                return;
            }
                #region Create the extradata
                switch (Item.Data.InteractionType)
            {
                case InteractionType.NONE:
                    ExtraData = "";
                    break;

                case InteractionType.GUILD_ITEM:
                case InteractionType.GUILD_GATE:
                case InteractionType.GUILD_FORUM:
                case InteractionType.GUILD_CHAT:
                    break;

                #region Pet handling

                case InteractionType.pet0:
                case InteractionType.pet1:
                case InteractionType.pet2:
                case InteractionType.pet3:
                case InteractionType.pet4:
                case InteractionType.pet5:
                case InteractionType.pet6:
                case InteractionType.pet7:
                case InteractionType.pet8:
                case InteractionType.pet9:
                case InteractionType.pet10:
                case InteractionType.pet11:
                case InteractionType.pet12:
                case InteractionType.pet13: //Caballo
                case InteractionType.pet14:
                case InteractionType.pet15:
                case InteractionType.pet16: //Mascota agregada
                case InteractionType.pet17: //Mascota agregada
                case InteractionType.pet18: //Mascota agregada
                case InteractionType.pet19: //Mascota agregada
                case InteractionType.pet20: //Mascota agregada
                case InteractionType.pet21: //Mascota agregada
                case InteractionType.pet22: //Mascota agregada
                case InteractionType.pet23:
                case InteractionType.pet24:
                case InteractionType.pet25:
                case InteractionType.pet26:
                case InteractionType.pet28:
                case InteractionType.pet29:
                case InteractionType.pet30:
                case InteractionType.pet31:
                case InteractionType.pet32:
                case InteractionType.pet33:
                case InteractionType.pet34:
                case InteractionType.pet35:
                case InteractionType.pet36:
                case InteractionType.pet37:
                case InteractionType.pet38:
                case InteractionType.pet39:
                case InteractionType.pet40:
                case InteractionType.pet41:
                case InteractionType.pet42:
                case InteractionType.pet43:
                    try
                    {
                       
                        string[] Bits = ExtraData.Split('\n');
                        string PetName = Bits[0];
                        string Race = Bits[1];
                        string Color = Bits[2];

                        int.Parse(Race); // to trigger any possible errors

                        if (!PetUtility.CheckPetName(PetName))
                            return;

                        if (Race.Length > 2)
                            return;

                        if (Color.Length != 6)
                            return;

                        PlusEnvironment.GetGame().GetAchievementManager().ProgressAchievement(Session, "ACH_PetLover", 1);
                    }
                    catch (Exception e)
                    {
                        Logging.LogException(e.ToString());
                        return;
                    }

                    break;

                #endregion

                case InteractionType.FLOOR:
                case InteractionType.WALLPAPER:
                case InteractionType.LANDSCAPE:

                    Double Number = 0;

                    try
                    {
                        if (string.IsNullOrEmpty(ExtraData))
                            Number = 0;
                        else
                            Number = Double.Parse(ExtraData, PlusEnvironment.CultureInfo);
                    }
                    catch (Exception e)
                    {
                        Logging.HandleException(e, "Catalog.HandlePurchase: " + ExtraData);
                    }

                    ExtraData = Number.ToString().Replace(',', '.');
                    break; // maintain extra data // todo: validate

                case InteractionType.POSTIT:
                    ExtraData = "FFFF33";
                    break;

                case InteractionType.MOODLIGHT:
                    ExtraData = "1,1,1,#000000,255";
                    break;

                case InteractionType.TROPHY:
                    ExtraData = Session.GetHabbo().Username + Convert.ToChar(9) + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + Convert.ToChar(9) + ExtraData;
                    break;

                case InteractionType.MANNEQUIN:
                    ExtraData = "m" + Convert.ToChar(5) + ".ch-210-1321.lg-285-92" + Convert.ToChar(5) + "Default Mannequin";
                    break;

                case InteractionType.BADGE_DISPLAY:
                    if (!Session.GetHabbo().GetBadgeComponent().HasBadge(ExtraData))
                    {
                        Session.SendMessage(new BroadcastMessageAlertComposer("Oops, it appears that you do not own this badge."));
                        return;
                    }

                    ExtraData = ExtraData + Convert.ToChar(9) + Session.GetHabbo().Username + Convert.ToChar(9) + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year;
                    break;

                case InteractionType.BADGE:
                    {
                        if (Session.GetHabbo().GetBadgeComponent().HasBadge(Item.Data.ItemName))
                        {
                            Session.SendMessage(new PurchaseErrorComposer(1));
                            return;
                        }
                        break;
                    }
                default:
                    ExtraData = "";
                    break;
            }
            #endregion


            if (Item.IsLimited)
            {
                if (Item.LimitedEditionStack <= Item.LimitedEditionSells)
                {
                    Session.SendNotification("This item has sold out!\n\n" + "Please note, you have not recieved another item (You have also not been charged for it!)");
                    Session.SendMessage(new CatalogUpdatedComposer());
                    Session.SendMessage(new PurchaseOKComposer());
                    return;
                }

                Item.LimitedEditionSells++;
                using (IQueryAdapter dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
                {
                    dbClient.RunQuery("UPDATE `catalog_items` SET `limited_sells` = '" + Item.LimitedEditionSells + "' WHERE `id` = '" + Item.Id + "' LIMIT 1");
                    LimitedEditionSells = Item.LimitedEditionSells;
                    LimitedEditionStack = Item.LimitedEditionStack;
                }
            }

            if (Item.CostCredits > 0)
            {
                Session.GetHabbo().Credits -= TotalCreditsCost;
                Session.SendMessage(new CreditBalanceComposer(Session.GetHabbo().Credits));
                Session.GetPlay().UpdateInteractingUserDialogues();
                Session.GetPlay().RefreshStatDialogue();
            }

            if (Item.CostPixels > 0)
            {
                Session.GetHabbo().Duckets -= TotalPixelCost;
                Session.SendMessage(new HabboActivityPointNotificationComposer(Session.GetHabbo().Duckets, Session.GetHabbo().Duckets));//Love you, Tom.
            }

            if (Item.CostDiamonds > 0)
            {
                Session.GetHabbo().Diamonds -= TotalDiamondCost;
                Session.SendMessage(new HabboActivityPointNotificationComposer(Session.GetHabbo().Diamonds, 0, 5));
            }
            if (Item.CostGotw > 0)
            {
                Session.GetHabbo().GOTWPoints -= TotalGotwCost;
                Session.SendMessage(new HabboActivityPointNotificationComposer(Session.GetHabbo().GOTWPoints, 0, 103));
            }
            Item NewItem = null;
            switch (Item.Data.Type.ToString().ToLower())
            {
                default:
                    List<Item> GeneratedGenericItems = new List<Item>();

                    switch (Item.Data.InteractionType)
                    {
                        default:
                            if (AmountPurchase > 1)
                            {
                                List<Item> Items = ItemFactory.CreateMultipleItems(Item.Data, Session.GetHabbo(), ExtraData, AmountPurchase);

                                if (Items != null)
                                {
                                    GeneratedGenericItems.AddRange(Items);
                                }
                            }
                            else
                            {
                                NewItem = ItemFactory.CreateSingleItemNullable(Item.Data, Session.GetHabbo(), ExtraData, ExtraData, 0, LimitedEditionSells, LimitedEditionStack);

                                if (NewItem != null)
                                {
                                    GeneratedGenericItems.Add(NewItem);
                                }
                            }
                            break;

                        case InteractionType.GUILD_GATE:
                            if (AmountPurchase > 1)
                            {
                                List<Item> Items = ItemFactory.CreateMultipleItems(Item.Data, Session.GetHabbo(), ExtraData, AmountPurchase, Convert.ToInt32(ExtraData));

                                if (Items != null)
                                {
                                    GeneratedGenericItems.AddRange(Items);
                                }
                            }
                            else
                            {
                                NewItem = ItemFactory.CreateSingleItemNullable(Item.Data, Session.GetHabbo(), ExtraData, ExtraData, Convert.ToInt32(ExtraData));

                                if (NewItem != null)
                                {
                                    GeneratedGenericItems.Add(NewItem);
                                }
                            }
                            break;
                        case InteractionType.GUILD_ITEM:
                            if (AmountPurchase > 1)
                            {
                                List<Item> Items = ItemFactory.CreateMultipleItems(Item.Data, Session.GetHabbo(), ExtraData, AmountPurchase, Convert.ToInt32(ExtraData));

                                if (Items != null)
                                {
                                    GeneratedGenericItems.AddRange(Items);
                                }
                            }
                            else
                            {
                                NewItem = ItemFactory.CreateSingleItemNullable(Item.Data, Session.GetHabbo(), ExtraData, ExtraData, Convert.ToInt32(ExtraData));

                                if (NewItem != null)
                                {
                                    GeneratedGenericItems.Add(NewItem);
                                }
                            }
                            break;
                        case InteractionType.GUILD_CHAT:
                            Group thegroup;
                            if (!PlusEnvironment.GetGame().GetGroupManager().TryGetGroup(Convert.ToInt32(ExtraData), out thegroup))
                                return;
                            if (!(PlusEnvironment.GetGame().GetGroupManager().GetGroupsForUser(Session.GetHabbo().Id).Contains(thegroup)))
                            {
                                return;
                            }

                            int groupID = Convert.ToInt32(ExtraData);
                            if (!thegroup.ForumEnabled && thegroup.CreatorId == Session.GetHabbo().Id)
                            {
                                thegroup.CreateGroupChat(thegroup);

                                /*
                                group.initializeForum();

                                Map<String, String> notificationParams = Maps.newHashMap();

                                notificationParams.put("groupId", groupId + "");
                                notificationParams.put("groupName", group.getData().getTitle());

                                client.send(new NotificationMessageComposer("forums.delivered", notificationParams));
                                */
                                
                            }
                            else if(thegroup.CreatorId != Session.GetHabbo().Id)
                            {
                                Session.SendNotification("Solo el dueño del grupo puede comprar esto");
                                return;
                            }
                            ExtraData = "" + groupID;
                            NewItem = ItemFactory.CreateSingleItemNullable(Item.Data, Session.GetHabbo(), ExtraData, ExtraData, Convert.ToInt32(ExtraData));

                            if (NewItem != null)
                            {
                                GeneratedGenericItems.Add(NewItem);
                            }
                            break;
                        case InteractionType.GUILD_FORUM:

                            Group thegroup1;
                            if (!PlusEnvironment.GetGame().GetGroupManager().TryGetGroup(Convert.ToInt32(ExtraData), out thegroup1))
                                return;
                            if (!(PlusEnvironment.GetGame().GetGroupManager().GetGroupsForUser(Session.GetHabbo().Id).Contains(thegroup1)))
                            {
                                return;
                            }

                            int groupID1 = Convert.ToInt32(ExtraData);
                            if (!thegroup1.ForumEnabled && thegroup1.CreatorId == Session.GetHabbo().Id)
                            {
                                thegroup1.InitForum(true);


                                Session.SendMessage(new Outgoing.Rooms.Notifications.RoomNotificationComposer("forums.delivered"));
                            }
                            else if (thegroup1.CreatorId != Session.GetHabbo().Id)
                            {
                                Session.SendNotification("Solo el dueño del grupo puede comprar esto.");
                                return;
                            }
                            ExtraData = "" + groupID1;
                            NewItem = ItemFactory.CreateSingleItemNullable(Item.Data, Session.GetHabbo(), ExtraData, ExtraData, Convert.ToInt32(ExtraData));

                            if (NewItem != null)
                            {
                                GeneratedGenericItems.Add(NewItem);
                            }

                            break;

                        case InteractionType.MUSIC_DISC:
                            string flags = Convert.ToString(Item.ExtradataInt);
                            if (AmountPurchase > 1)
                            {
                                List<Item> Items = ItemFactory.CreateMultipleItems(Item.Data, Session.GetHabbo(), flags, AmountPurchase);

                                if (Items != null)
                                {
                                    GeneratedGenericItems.AddRange(Items);
                                }
                            }
                            else
                            {
                                NewItem = ItemFactory.CreateSingleItemNullable(Item.Data, Session.GetHabbo(), flags, flags);

                                if (NewItem != null)
                                {
                                    GeneratedGenericItems.Add(NewItem);
                                }
                            }
                            break;

                        case InteractionType.ARROW:
                        case InteractionType.TELEPORT:
                            for (int i = 0; i < AmountPurchase; i++)
                            {
                                List<Item> TeleItems = ItemFactory.CreateTeleporterItems(Item.Data, Session.GetHabbo());

                                if (TeleItems != null)
                                {
                                    GeneratedGenericItems.AddRange(TeleItems);
                                }
                            }
                            break;

                        case InteractionType.MOODLIGHT:
                            {
                                if (AmountPurchase > 1)
                                {
                                    List<Item> Items = ItemFactory.CreateMultipleItems(Item.Data, Session.GetHabbo(), ExtraData, AmountPurchase);

                                    if (Items != null)
                                    {
                                        GeneratedGenericItems.AddRange(Items);
                                        foreach (Item I in Items)
                                        {
                                            ItemFactory.CreateMoodlightData(I);
                                        }
                                    }
                                }
                                else
                                {
                                    NewItem = ItemFactory.CreateSingleItemNullable(Item.Data, Session.GetHabbo(), ExtraData, ExtraData);

                                    if (NewItem != null)
                                    {
                                        GeneratedGenericItems.Add(NewItem);
                                        ItemFactory.CreateMoodlightData(NewItem);
                                    }
                                }
                            }
                            break;

                        case InteractionType.TONER:
                            {
                                if (AmountPurchase > 1)
                                {
                                    List<Item> Items = ItemFactory.CreateMultipleItems(Item.Data, Session.GetHabbo(), ExtraData, AmountPurchase);

                                    if (Items != null)
                                    {
                                        GeneratedGenericItems.AddRange(Items);
                                        foreach (Item I in Items)
                                        {
                                            ItemFactory.CreateTonerData(I);
                                        }
                                    }
                                }
                                else
                                {
                                    NewItem = ItemFactory.CreateSingleItemNullable(Item.Data, Session.GetHabbo(), ExtraData, ExtraData);

                                    if (NewItem != null)
                                    {
                                        GeneratedGenericItems.Add(NewItem);
                                        ItemFactory.CreateTonerData(NewItem);
                                    }
                                }
                            }
                            break;

                        case InteractionType.DEAL:
                            {
                                //Fetch the deal where the ID is this items ID.
                                var DealItems = (from d in Page.Deals.Values.ToList() where d.Id == Item.Id select d);

                                //This bit, iterating ONE item? How can I make this simpler
                                foreach (CatalogDeal DealItem in DealItems)
                                {
                                    //Here I loop the DealItems ItemDataList.
                                    foreach (CatalogItem CatalogItem in DealItem.ItemDataList.ToList())
                                    {
                                        List<Item> Items = ItemFactory.CreateMultipleItems(CatalogItem.Data, Session.GetHabbo(), "", AmountPurchase);

                                        if (Items != null)
                                        {
                                            GeneratedGenericItems.AddRange(Items);
                                        }
                                    }
                                }
                                break;
                            }

                    }

                    foreach (Item PurchasedItem in GeneratedGenericItems)
                    {
                        if (Session.GetHabbo().GetInventoryComponent().TryAddItem(PurchasedItem))
                        {
                            //Session.SendMessage(new FurniListAddComposer(PurchasedItem));
                            Session.SendMessage(new FurniListNotificationComposer(PurchasedItem.Id, 1));
                        }
                    }
                    break;

                case "e":
                    AvatarEffect Effect = null;

                    if (Session.GetHabbo().Effects().HasEffect(Item.Data.SpriteId))
                    {
                        Effect = Session.GetHabbo().Effects().GetEffectNullable(Item.Data.SpriteId);

                        if (Effect != null)
                        {
                            Effect.AddToQuantity();
                        }
                    }
                    else
                        Effect = AvatarEffectFactory.CreateNullable(Session.GetHabbo(), Item.Data.SpriteId, 3600);

                    if (Effect != null)// && Session.GetHabbo().Effects().TryAdd(Effect))
                    {
                        Session.SendMessage(new AvatarEffectAddedComposer(Item.Data.SpriteId, 3600));
                    }
                    break;

                case "r":
                    Bot Bot = BotUtility.CreateBot(Item.Data, Session.GetHabbo().Id);
                    if (Bot != null)
                    {
                        Session.GetHabbo().GetInventoryComponent().TryAddBot(Bot);
                        Session.SendMessage(new BotInventoryComposer(Session.GetHabbo().GetInventoryComponent().GetBots()));
                        Session.SendMessage(new FurniListNotificationComposer(Bot.Id, 5));
                    }
                    else
                        Session.SendNotification("Oops! There was an error whilst purchasing this bot. It seems that there is no bot data for the bot!");
                    break;

                case "b":
                    {
                        Session.GetHabbo().GetBadgeComponent().GiveBadge(Item.Data.ItemName, true, Session);
                        Session.SendMessage(new FurniListNotificationComposer(0, 4));
                        break;
                    }

                case "p":
                    {
                        switch (Item.Data.InteractionType)
                        {
                            #region Pets
                            #region Pet 0
                            case InteractionType.pet0:
                                string[] PetData = ExtraData.Split('\n');
                                Pet GeneratedPet = PetUtility.CreatePet(Session.GetHabbo().Id, PetData[0], 0, PetData[1], PetData[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet);

                                break;
                            #endregion
                            #region Pet 1
                            case InteractionType.pet1:
                                string[] PetData1 = ExtraData.Split('\n');
                                Pet GeneratedPet1 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData1[0], 1, PetData1[1], PetData1[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet1);

                                break;
                            #endregion
                            #region Pet 2
                            case InteractionType.pet2:
                                string[] PetData5 = ExtraData.Split('\n');
                                Pet GeneratedPet5 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData5[0], 2, PetData5[1], PetData5[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet5);

                                break;
                            #endregion
                            #region Pet 3
                            case InteractionType.pet3:
                                string[] PetData2 = ExtraData.Split('\n');
                                Pet GeneratedPet2 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData2[0], 3, PetData2[1], PetData2[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet2);

                                break;
                            #endregion
                            #region Pet 4
                            case InteractionType.pet4:
                                string[] PetData3 = ExtraData.Split('\n');
                                Pet GeneratedPet3 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData3[0], 4, PetData3[1], PetData3[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet3);

                                break;
                            #endregion
                            #region Pet 5
                            case InteractionType.pet5:
                                string[] PetData7 = ExtraData.Split('\n');
                                Pet GeneratedPet7 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData7[0], 5, PetData7[1], PetData7[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet7);

                                break;
                            #endregion
                            #region Pet 6 (wrong?)
                            case InteractionType.pet6:
                                string[] PetData4 = ExtraData.Split('\n');
                                Pet GeneratedPet4 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData4[0], 6, PetData4[1], PetData4[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet4);

                                break;
                            #endregion
                            #region Pet 7 (wrong?)
                            case InteractionType.pet7:
                                string[] PetData6 = ExtraData.Split('\n');
                                Pet GeneratedPet6 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData6[0], 7, PetData6[1], PetData6[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet6);

                                break;
                            #endregion
                            #region Pet 8
                            case InteractionType.pet8:
                                string[] PetData8 = ExtraData.Split('\n');
                                Pet GeneratedPet8 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData8[0], 8, PetData8[1], PetData8[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet8);

                                break;
                            #endregion
                            #region Pet 8
                            case InteractionType.pet9:
                                string[] PetData9 = ExtraData.Split('\n');
                                Pet GeneratedPet9 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData9[0], 9, PetData9[1], PetData9[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet9);

                                break;
                            #endregion
                            #region Pet 10
                            case InteractionType.pet10:
                                string[] PetData10 = ExtraData.Split('\n');
                                Pet GeneratedPet10 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData10[0], 10, PetData10[1], PetData10[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet10);

                                break;
                            #endregion
                            #region Pet 11
                            case InteractionType.pet11:
                                string[] PetData11 = ExtraData.Split('\n');
                                Pet GeneratedPet11 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData11[0], 11, PetData11[1], PetData11[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet11);

                                break;
                            #endregion
                            #region Pet 12
                            case InteractionType.pet12:
                                string[] PetData12 = ExtraData.Split('\n');
                                Pet GeneratedPet12 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData12[0], 12, PetData12[1], PetData12[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet12);

                                break;
                            #endregion
                            #region Pet 13
                            case InteractionType.pet13: //Caballo - Horse
                                string[] PetData13 = ExtraData.Split('\n');
                                Pet GeneratedPet13 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData13[0], 13, PetData13[1], PetData13[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet13);

                                break;
                            #endregion
                            #region Pet 14
                            case InteractionType.pet14:
                                string[] PetData14 = ExtraData.Split('\n');
                                Pet GeneratedPet14 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData14[0], 14, PetData14[1], PetData14[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet14);

                                break;
                            #endregion
                            #region Pet 15
                            case InteractionType.pet15:
                                string[] PetData15 = ExtraData.Split('\n');
                                Pet GeneratedPet15 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData15[0], 15, PetData15[1], PetData15[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet15);

                                break;
                            #endregion
                            #region Pet 16
                            case InteractionType.pet16: // Mascota Agregada
                                string[] PetData16 = ExtraData.Split('\n');
                                Pet GeneratedPet16 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData16[0], 16, PetData16[1], PetData16[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet16);

                                break;
                            #endregion
                            #region Pet 17
                            case InteractionType.pet17: // Mascota Agregada
                                string[] PetData17 = ExtraData.Split('\n');
                                Pet GeneratedPet17 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData17[0], 17, PetData17[1], PetData17[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet17);

                                break;
                            #endregion
                            #region Pet 18
                            case InteractionType.pet18: // Mascota Agregada
                                string[] PetData18 = ExtraData.Split('\n');
                                Pet GeneratedPet18 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData18[0], 18, PetData18[1], PetData18[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet18);

                                break;
                            #endregion
                            #region Pet 19
                            case InteractionType.pet19: // Mascota Agregada
                                string[] PetData19 = ExtraData.Split('\n');
                                Pet GeneratedPet19 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData19[0], 19, PetData19[1], PetData19[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet19);

                                break;
                            #endregion
                            #region Pet 20
                            case InteractionType.pet20: // Mascota Agregada
                                string[] PetData20 = ExtraData.Split('\n');
                                Pet GeneratedPet20 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData20[0], 20, PetData20[1], PetData20[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet20);

                                break;
                            #endregion
                            #region Pet 21
                            case InteractionType.pet21: // Mascota Agregada
                                string[] PetData21 = ExtraData.Split('\n');
                                Pet GeneratedPet21 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData21[0], 21, PetData21[1], PetData21[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet21);

                                break;
                            #endregion
                            #region Pet 22
                            case InteractionType.pet22: // Mascota Agregada
                                string[] PetData22 = ExtraData.Split('\n');
                                Pet GeneratedPet22 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData22[0], 22, PetData22[1], PetData22[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet22);

                                break;
                            #endregion
                            #region Pet 23
                            case InteractionType.pet23: // Mascota Agregada
                                string[] PetData23 = ExtraData.Split('\n');
                                Pet GeneratedPet23 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData23[0], 23, PetData23[1], PetData23[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet23);

                                break;
                            #endregion
                            #region Pet 24
                            case InteractionType.pet24: // Mascota Agregada
                                string[] PetData24 = ExtraData.Split('\n');
                                Pet GeneratedPet24 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData24[0], 24, PetData24[1], PetData24[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet24);

                                break;
                            #endregion
                            #region Pet 25
                            case InteractionType.pet25: // Mascota Agregada
                                string[] PetData25 = ExtraData.Split('\n');
                                Pet GeneratedPet25 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData25[0], 25, PetData25[1], PetData25[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet25);

                                break;
                            #endregion
                            #region Pet 26
                            case InteractionType.pet26: // Mascota Agregada
                                string[] PetData26 = ExtraData.Split('\n');
                                Pet GeneratedPet26 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData26[0], 26, PetData26[1], PetData26[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet26);

                                break;
                            #endregion
                            #region Pet 28
                            case InteractionType.pet28: // Mascota Agregada
                                string[] PetData28 = ExtraData.Split('\n');
                                Pet GeneratedPet28 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData28[0], 28, PetData28[1], PetData28[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet28);

                                break;
                            #endregion
                            #region Pet 29
                            case InteractionType.pet29:
                                string[] PetData29 = ExtraData.Split('\n');
                                Pet GeneratedPet29 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData29[0], 29, PetData29[1], PetData29[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet29);

                                break;
                            #endregion
                            #region Pet 30
                            case InteractionType.pet30:
                                string[] PetData30 = ExtraData.Split('\n');
                                Pet GeneratedPet30 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData30[0], 30, PetData30[1], PetData30[2]);

                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet30);

                                break;
                            #endregion
                            #region Pet 31
                            case InteractionType.pet31:
                                string[] PetData31 = ExtraData.Split('\n');
                                Pet GeneratedPet31 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData31[0], 31, PetData31[1], PetData31[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet31);
                                break;
                            #endregion
                            #region Pet 32
                            case InteractionType.pet32:
                                string[] PetData32 = ExtraData.Split('\n');
                                Pet GeneratedPet32 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData32[0], 32, PetData32[1], PetData32[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet32);
                                break;
                            #endregion
                            #region Pet 33
                            case InteractionType.pet33:
                                string[] PetData33 = ExtraData.Split('\n');
                                Pet GeneratedPet33 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData33[0], 33, PetData33[1], PetData33[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet33);
                                break;
                            #endregion
                            #region Pet 34
                            case InteractionType.pet34:
                                string[] PetData34 = ExtraData.Split('\n');
                                Pet GeneratedPet34 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData34[0], 34, PetData34[1], PetData34[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet34);
                                break;
                            #endregion
                            #region Pet 35
                            case InteractionType.pet35:
                                string[] PetData35 = ExtraData.Split('\n');
                                Pet GeneratedPet35 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData35[0], 35, PetData35[1], PetData35[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet35);
                                break;
                            #endregion
                            #region Pet 36
                            case InteractionType.pet36:
                                string[] PetData36 = ExtraData.Split('\n');
                                Pet GeneratedPet36 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData36[0], 36, PetData36[1], PetData36[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet36);
                                break;
                            #endregion
                            #region Pet 37
                            case InteractionType.pet37:
                                string[] PetData37 = ExtraData.Split('\n');
                                Pet GeneratedPet37 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData37[0], 37, PetData37[1], PetData37[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet37);
                                break;
                            #endregion
                            #region Pet 38
                            case InteractionType.pet38:
                                string[] PetData38 = ExtraData.Split('\n');
                                Pet GeneratedPet38 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData38[0], 38, PetData38[1], PetData38[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet38);
                                break;
                            #endregion
                            #region Pet 39
                            case InteractionType.pet39:
                                string[] PetData39 = ExtraData.Split('\n');
                                Pet GeneratedPet39 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData39[0], 39, PetData39[1], PetData39[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet39);
                                break;
                            #endregion
                            #region Pet 40
                            case InteractionType.pet40:
                                string[] PetData40 = ExtraData.Split('\n');
                                Pet GeneratedPet40 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData40[0], 40, PetData40[1], PetData40[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet40);
                                break;
                            #endregion
                            #region Pet 41
                            case InteractionType.pet41:
                                string[] PetData41 = ExtraData.Split('\n');
                                Pet GeneratedPet41 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData41[0], 41, PetData41[1], PetData41[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet41);
                                break;
                            #endregion
                            #region Pet 42
                            case InteractionType.pet42:
                                string[] PetData42 = ExtraData.Split('\n');
                                Pet GeneratedPet42 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData42[0], 42, PetData42[1], PetData42[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet42);
                                break;
                            #endregion
                            #region Pet 43
                            case InteractionType.pet43:
                                string[] PetData43 = ExtraData.Split('\n');
                                Pet GeneratedPet43 = PetUtility.CreatePet(Session.GetHabbo().Id, PetData43[0], 43, PetData43[1], PetData43[2]);
                                Session.GetHabbo().GetInventoryComponent().TryAddPet(GeneratedPet43);
                                break;
                                #endregion
                                #endregion
                        }

                        Session.SendMessage(new FurniListNotificationComposer(0, 3));
                        Session.SendMessage(new PetInventoryComposer(Session.GetHabbo().GetInventoryComponent().GetPets()));

                        ItemData PetFood = null;
                        if (PlusEnvironment.GetGame().GetItemManager().GetItem(320, out PetFood))
                        {
                            Item Food = ItemFactory.CreateSingleItemNullable(PetFood, Session.GetHabbo(), "", "");
                            if (Food != null)
                            {
                                Session.GetHabbo().GetInventoryComponent().TryAddItem(Food);
                                Session.SendMessage(new FurniListNotificationComposer(Food.Id, 1));
                            }
                        }
                        break;
                    }
            }


            Session.SendMessage(new PurchaseOKComposer(Item, Item.Data));
            Session.SendMessage(new FurniListUpdateComposer());
            Session.SendMessage(new RoomNotificationComposer("icons/" + Item.Data.ItemName + "_icon", 3, "Has comprado: " + Item.Name, "inventory/open/furni"));
        }
    }
}