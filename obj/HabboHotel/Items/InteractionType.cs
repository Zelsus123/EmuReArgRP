﻿using System;
using Plus;
using Plus.Core;
namespace Plus.HabboHotel.Items
{
    public enum InteractionType
    {
        NONE,
        GATE,
        POSTIT,
        MOODLIGHT,
        TROPHY,
        wired_score_board,
        BED,
        SCOREBOARD,
        VENDING_MACHINE,
        ALERT,
        ONE_WAY_GATE,
        LOVE_SHUFFLER,
        HABBO_WHEEL,
        DICE,
        BOTTLE,
        HOPPER,
        TELEPORT,
        POOL,
        ROLLER,
        FOOTBALL_GATE,
        pet0,
        pet1,
        pet2,
        pet3,
        pet4,
        pet5,
        pet6,
        pet7,
        pet8,
        pet9,
        pet10,
        pet11,
        pet12,
        pet13,
        pet14,
        pet15,
        pet16,
        pet17,
        pet18,
        pet19,
        pet20,
        pet21,
        pet22,
        pet23,
        pet24,
        pet25,
        pet26,
        pet28,
        pet29,
        pet30,
        pet31,
        pet32,
        pet33,
        pet34,
        pet35,
        pet36,
        pet37,
        pet38,
        pet39,
        pet40,
        pet41,
        pet42,
        pet43,
        ICE_SKATES,
        NORMAL_SKATES,
        lowpool,
        haloweenpool,
        FOOTBALL,
        FOOTBALL_GOAL_GREEN,
        FOOTBALL_GOAL_YELLOW,
        FOOTBALL_GOAL_BLUE,
        FOOTBALL_GOAL_RED,
        footballcountergreen,
        footballcounteryellow,
        footballcounterblue,
        footballcounterred,
        banzaigateblue,
        banzaigatered,
        banzaigateyellow,
        banzaigategreen,
        banzaifloor,
        banzaiscoreblue,
        banzaiscorered,
        banzaiscoreyellow,
        banzaiscoregreen,
        banzaicounter,
        banzaitele,
        banzaipuck,
        banzaipyramid,
        freezetimer,
        freezeexit,
        freezeredcounter,
        freezebluecounter,
        freezeyellowcounter,
        freezegreencounter,
        FREEZE_YELLOW_GATE,
        FREEZE_RED_GATE,
        FREEZE_GREEN_GATE,
        FREEZE_BLUE_GATE,
        FREEZE_TILE_BLOCK,
        FREEZE_TILE,
        JUKEBOX,
        MUSIC_DISC,
        TRAX,
        PUZZLE_BOX,
        TONER,


        PRESSURE_PAD,

        WF_FLOOR_SWITCH_1,
        WF_FLOOR_SWITCH_2,

        GIFT,
        BACKGROUND,
        MANNEQUIN,
        GATE_VIP,
        GUILD_ITEM,
        GUILD_GATE,
        GUILD_FORUM,

        TENT,
        TENT_SMALL,
        BADGE_DISPLAY,
        STACKTOOL,
        TELEVISION,

        WIRED_EFFECT,
        WIRED_TRIGGER,
        WIRED_CONDITION,
        WIRED_HIGHSCORE,

        WALLPAPER,
        FLOOR,
        LANDSCAPE,

        BADGE,
        CRACKABLE_EGG,
        EFFECT,
        DEAL,

        HORSE_SADDLE_1,
        HORSE_SADDLE_2,
        HORSE_HAIRSTYLE,
        HORSE_BODY_DYE,
        HORSE_HAIR_DYE,

        GNOME_BOX,
        BOT,
        PURCHASABLE_CLOTHING,
        PET_BREEDING_BOX,
        ARROW,
        LOVELOCK,
        MONSTERPLANT_SEED,
        CANNON,
        COUNTER,
        CAMERA_PICTURE,
        FX_PROVIDER,
        GUILD_CHAT,

        // RP
        ATM_MACHINE,
        TRASH_CAN,
        RENTABLE_SPACE,
        HOUSE_SIGN,
        SHOWER,
        WATER_PUMP,
        COMODIN,
        WARDROBE,
        SLOTS
    }


    public class InteractionTypes
    {
        public static InteractionType GetTypeFromString(string pType)
        {
            switch (pType.ToLower())
            {
                case "":
                case "default":
                    return InteractionType.NONE;
                case "gate":
                    return InteractionType.GATE;
                case "postit":
                    return InteractionType.POSTIT;
                case "dimmer":
                    return InteractionType.MOODLIGHT;
                case "wired_score_board":
                    return InteractionType.wired_score_board;
                case "trophy":
                    return InteractionType.TROPHY;
                case "bed":
                    return InteractionType.BED;
                case "scoreboard":
                    return InteractionType.SCOREBOARD;
                case "vendingmachine":
                    return InteractionType.VENDING_MACHINE;
                case "alert":
                    return InteractionType.ALERT;
                case "onewaygate":
                    return InteractionType.ONE_WAY_GATE;
                case "loveshuffler":
                    return InteractionType.LOVE_SHUFFLER;
                case "habbowheel":
                    return InteractionType.HABBO_WHEEL;
                case "dice":
                    return InteractionType.DICE;
                case "hopper":
                    return InteractionType.HOPPER;
                case "bottle":
                    return InteractionType.BOTTLE;
                case "teleport":
                    return InteractionType.TELEPORT;
                case "pool":
                    return InteractionType.POOL;
                case "roller":
                    return InteractionType.ROLLER;
                case "fbgate":
                    return InteractionType.FOOTBALL_GATE;
                case "pet0":
                    return InteractionType.pet0;
                case "pet1":
                    return InteractionType.pet1;
                case "pet2":
                    return InteractionType.pet2;
                case "pet3":
                    return InteractionType.pet3;
                case "pet4":
                    return InteractionType.pet4;
                case "pet5":
                    return InteractionType.pet5;
                case "pet6":
                    return InteractionType.pet6;
                case "pet7":
                    return InteractionType.pet7;
                case "pet8":
                    return InteractionType.pet8;
                case "pet9":
                    return InteractionType.pet9;
                case "pet10":
                    return InteractionType.pet10;
                case "pet11":
                    return InteractionType.pet11;
                case "pet12":
                    return InteractionType.pet12;
                case "pet13": // Caballo
                    return InteractionType.pet13;
                case "pet14":
                    return InteractionType.pet14;
                case "pet15":
                    return InteractionType.pet15;
                case "pet16": // Mascota agregada
                    return InteractionType.pet16;
                case "pet17": // Mascota agregada
                    return InteractionType.pet17;
                case "pet18": // Mascota agregada
                    return InteractionType.pet18;
                case "pet19": // Mascota agregada
                    return InteractionType.pet19;
                case "pet20": // Mascota agregada
                    return InteractionType.pet20;
                case "pet21": // Mascota agregada
                    return InteractionType.pet21;
                case "pet22": // Mascota agregada
                    return InteractionType.pet22;
                case "pet23":
                    return InteractionType.pet23;
                case "pet24":
                    return InteractionType.pet24;
                case "pet25":
                    return InteractionType.pet25;
                case "pet26":
                    return InteractionType.pet26;
                case "pet28":
                    return InteractionType.pet28;
                case "pet29":
                    return InteractionType.pet29;
                case "pet30":
                    return InteractionType.pet30;
                case "pet31":
                    return InteractionType.pet31;
                case "pet32":
                    return InteractionType.pet32;
                case "pet33":
                    return InteractionType.pet33;
                case "pet34":
                    return InteractionType.pet34;
                case "pet35":
                    return InteractionType.pet35;
                case "pet36":
                    return InteractionType.pet36;
                case "pet37":
                    return InteractionType.pet37;
                case "pet38":
                    return InteractionType.pet38;
                case "pet39":
                    return InteractionType.pet39;
                case "pet40":
                    return InteractionType.pet40;
                case "pet41":
                    return InteractionType.pet41;
                case "pet42":
                    return InteractionType.pet42;
                case "pet43":
                    return InteractionType.pet43;
                case "iceskates":
                    return InteractionType.ICE_SKATES;
                case "rollerskate":
                    return InteractionType.NORMAL_SKATES;
                case "lowpool":
                    return InteractionType.lowpool;
                case "haloweenpool":
                    return InteractionType.haloweenpool;
                case "ball":
                    return InteractionType.FOOTBALL;

                case "green_goal":
                    return InteractionType.FOOTBALL_GOAL_GREEN;
                case "yellow_goal":
                    return InteractionType.FOOTBALL_GOAL_YELLOW;
                case "red_goal":
                    return InteractionType.FOOTBALL_GOAL_RED;
                case "blue_goal":
                    return InteractionType.FOOTBALL_GOAL_BLUE;

                case "green_score":
                    return InteractionType.footballcountergreen;
                case "yellow_score":
                    return InteractionType.footballcounteryellow;
                case "blue_score":
                    return InteractionType.footballcounterblue;
                case "red_score":
                    return InteractionType.footballcounterred;

                case "bb_blue_gate":
                    return InteractionType.banzaigateblue;
                case "bb_red_gate":
                    return InteractionType.banzaigatered;
                case "bb_yellow_gate":
                    return InteractionType.banzaigateyellow;
                case "bb_green_gate":
                    return InteractionType.banzaigategreen;
                case "bb_patch":
                    return InteractionType.banzaifloor;

                case "bb_blue_score":
                    return InteractionType.banzaiscoreblue;
                case "bb_red_score":
                    return InteractionType.banzaiscorered;
                case "bb_yellow_score":
                    return InteractionType.banzaiscoreyellow;
                case "bb_green_score":
                    return InteractionType.banzaiscoregreen;

                case "banzaicounter":
                    return InteractionType.banzaicounter;
                case "bb_teleport":
                    return InteractionType.banzaitele;
                case "banzaipuck":
                    return InteractionType.banzaipuck;
                case "bb_pyramid":
                    return InteractionType.banzaipyramid;

                case "freezetimer":
                    return InteractionType.freezetimer;
                case "freezeexit":
                    return InteractionType.freezeexit;
                case "freezeredcounter":
                    return InteractionType.freezeredcounter;
                case "freezebluecounter":
                    return InteractionType.freezebluecounter;
                case "freezeyellowcounter":
                    return InteractionType.freezeyellowcounter;
                case "freezegreencounter":
                    return InteractionType.freezegreencounter;
                case "freezeyellowgate":
                    return InteractionType.FREEZE_YELLOW_GATE;
                case "freezeredgate":
                    return InteractionType.FREEZE_RED_GATE;
                case "freezegreengate":
                    return InteractionType.FREEZE_GREEN_GATE;
                case "freezebluegate":
                    return InteractionType.FREEZE_BLUE_GATE;
                case "freezetileblock":
                    return InteractionType.FREEZE_TILE_BLOCK;
                case "freezetile":
                    return InteractionType.FREEZE_TILE;

                case "jukebox":
                    return InteractionType.JUKEBOX;
                case "musicdisc":
                    return InteractionType.MUSIC_DISC;
                case "trax":
                    return InteractionType.TRAX;

                case "pressure_pad":
                    return InteractionType.PRESSURE_PAD;
                case "wf_floor_switch1":
                    return InteractionType.WF_FLOOR_SWITCH_1;
                case "wf_floor_switch2":
                    return InteractionType.WF_FLOOR_SWITCH_2;
                case "puzzlebox":
                    return InteractionType.PUZZLE_BOX;
                case "water":
                    return InteractionType.POOL;
                case "gift":
                    return InteractionType.GIFT;
                case "background":
                    return InteractionType.BACKGROUND;
                case "mannequin":
                    return InteractionType.MANNEQUIN;
                case "vip_gate":
                    return InteractionType.GATE_VIP;
                case "roombg":
                    return InteractionType.TONER;
                case "gld_item":
                    return InteractionType.GUILD_ITEM;
                case "gld_gate":
                    return InteractionType.GUILD_GATE;
                case "guild_forum":
                    return InteractionType.GUILD_FORUM;
                case "guild_chat":
                    return InteractionType.GUILD_CHAT;
                case "tent":
                    return InteractionType.TENT;
                case "tent_small":
                    return InteractionType.TENT_SMALL;

                case "badge_display":
                    return InteractionType.BADGE_DISPLAY;
                case "stacktool":
                    return InteractionType.STACKTOOL;
                case "television":
                    return InteractionType.TELEVISION;


                case "wired_effect":
                    return InteractionType.WIRED_EFFECT;
                case "wired_trigger":
                    return InteractionType.WIRED_TRIGGER;
                case "wired_condition":
                    return InteractionType.WIRED_CONDITION;
                case "wired_highscore":
                    return InteractionType.WIRED_HIGHSCORE;

                case "floor":
                    return InteractionType.FLOOR;
                case "wallpaper":
                    return InteractionType.WALLPAPER;
                case "landscape":
                    return InteractionType.LANDSCAPE;

                case "badge":
                    return InteractionType.BADGE;

                case "crackable_egg":
                    return InteractionType.CRACKABLE_EGG;
                case "effect":
                    return InteractionType.EFFECT;
                case "deal":
                    return InteractionType.DEAL;

                case "horse_saddle_1":
                    return InteractionType.HORSE_SADDLE_1;
                case "horse_saddle_2":
                    return InteractionType.HORSE_SADDLE_2;
                case "horse_hairstyle":
                    return InteractionType.HORSE_HAIRSTYLE;
                case "horse_body_dye":
                    return InteractionType.HORSE_BODY_DYE;
                case "horse_hair_dye":
                    return InteractionType.HORSE_HAIR_DYE;

                case "gnome_box":
                    return InteractionType.GNOME_BOX;
                case "bot":
                    return InteractionType.BOT;
                case "purchasable_clothing":
                    return InteractionType.PURCHASABLE_CLOTHING;
                case "pet_breeding_box":
                    return InteractionType.PET_BREEDING_BOX;
                case "arrow":
                    return InteractionType.ARROW;
                case "lovelock":
                    return InteractionType.LOVELOCK;
                case "cannon":
                    return InteractionType.CANNON;
                case "counter":
                    return InteractionType.COUNTER;
                case "camera_picture":
                    return InteractionType.CAMERA_PICTURE;
                case "fx_provider":
                    return InteractionType.FX_PROVIDER;
                //RP
                case "atm_machine":
                    return InteractionType.ATM_MACHINE;
                case "trash_can":
                    return InteractionType.TRASH_CAN;
                case "house_sign":
                    return InteractionType.HOUSE_SIGN;
                case "rentable_space":
                    return InteractionType.RENTABLE_SPACE;
                case "shower":
                    return InteractionType.SHOWER;
                case "water_pump":
                    return InteractionType.WATER_PUMP;
                case "comodin":
                    return InteractionType.COMODIN;
                case "wardrobe":
                    return InteractionType.WARDROBE;
                case "slots":
                    return InteractionType.SLOTS;
                default:
                    {
                        //Logging.WriteLine("Unknown interaction type in parse code: " + pType, ConsoleColor.Yellow);
                        return InteractionType.NONE;
                    }
            }
        }
    }
}