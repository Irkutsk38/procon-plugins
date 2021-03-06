/*  CZombieMode - Copyright 2012 by PapaCharlie9, m4xx

Permission to use, copy, modify, and/or distribute this software for any purpose 
with or without fee is hereby granted without restriction.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES 
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF 
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR 
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES 
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN 
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT 
OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

enum NoticeDisplayType { yell, say };

namespace PRoConEvents
{

    public class CZombieMode : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Constants

        const string HUMAN_TEAM = "1";

        const string ZOMBIE_TEAM = "2";

        const string BLANK_SQUAD = "0";

        const string FORCE_MOVE = "true";

        #endregion

        #region PluginSettings

        private int DebugLevel = 2; // 3 while in development, 2 when released

        private string CommandPrefix = "!zombie";

        private int AnnounceDisplayLength = 10;

        private NoticeDisplayType AnnounceDisplayType = NoticeDisplayType.yell;

        private int WarningDisplayLength = 15;

        private static string[] DefaultAdminUsers = { "PapaCharlieNiner" };

        private List<String> AdminUsers = new List<String>(DefaultAdminUsers);

        private List<String> PlayerKickQueue = new List<String>();

        private ZombieModeKillTracker KillTracker = new ZombieModeKillTracker();

        private bool RematchEnabled = true; // true: round does not end, false: round ends

        private int MatchesBeforeNextMap = 3;

        private int HumanMaxIdleSeconds = 3 * 60; // aggressively kick idle humans

        private int MaxIdleSeconds = 10 * 60; // maximum idle for any player

        private int WarnsBeforeKickForRulesViolations = 1;

        private bool NewPlayersJoinHumans = true;

        private bool TempBanInsteadOfKick = false;

        private int VotesNeededToKick = 3;

        private int TempBanSeconds = 60 * 60; // one hour

        #endregion


        #region GamePlayVars

        private List<CPlayerInfo> PlayerList = new List<CPlayerInfo>();

        private bool ZombieModeEnabled = true;

        private int MaxPlayers = 32;

        private int MinimumHumans = 3;

        private int MinimumZombies = 1;

        private int DeathsNeededToBeInfected = 1;

        //private int ZombiesKilledToSurvive = 50;

        private bool ZombieKillLimitEnabled = true;

        private bool InfectSuicides = false;

        private static string[] DEFAULT_RULES =
        {
            "US team are humans, RU team are zombies",
            "Zombies use knife/defib/repair tool only!",
            "Zombies are hard to kill",
            "Humans use guns only, no explosives (nades, RPG, M320, C4, Claymore, ...)!",
            "Zombies win by infecting all humans",
            "When a zombie kills you, you are infected and moved to the zombie team!"
        };

        private List<String> RuleList = new List<String>(DEFAULT_RULES);

        private List<String> TeamHuman = new List<String>();

        private List<String> TeamZombie = new List<String>();

        private List<String> FreshZombie = new List<String>();

        private List<String> PatientZeroes = new List<String>();
        /* PatientZeroes keeps track of all the players that have been selected to
           be the first zombie, to prevent the same player from being selected
           over and over again. */

        private int KnownPlayerCount = 0;

        private int ServerSwitchedCount = 0;

        private List<String> Lottery = new List<String>();
        /* Pool of players to select first zombie from */

        private string PatientZero = null; // name of first zombie for the round

        private ZombieModePlayerState PlayerState = new ZombieModePlayerState();

        private SynchronizedNumbers NumRulesThreads = new SynchronizedNumbers();

        private SynchronizedNumbers LastRequestPlayersList = new SynchronizedNumbers();

        private SynchronizedNumbers StateLock = new SynchronizedNumbers();

        private enum GState
        {
            Idle,           // No players, no match in progress, or just reset
            Waiting,        // Waiting for minimum number of players to spawn
            Playing,        // Playing a match
            CountingDown,   // Match over, counting down to next round/match
            Moving,         // Counting down and moving players
            BetweenRounds,  // Between map levels/rounds
            RoundStarting,  // Finished moving between rounds
            NeedSpawn       // Ready to play next match, waiting for spawn
        };

        private GState GameState = GState.Idle;

        private GState OldGameState = GState.BetweenRounds;

        private DescriptionClass Description = new DescriptionClass();

        private List<String> JoinQueue = new List<String>();

        private int MatchesCount = 0;

        private String LastMover = null;

        private String TestWeapon = String.Empty;

        #endregion


        #region DamagePercentageVars

        int Against1Or2Zombies = 5;  // 3+ to 1 ratio humans:zombies

        int AgainstAFewZombies = 15; // 3:1 to 3:2 ratio humans:zombies

        int AgainstEqualNumbers = 30; // 3:2 to 2:3 ratio humans:zombies

        int AgainstManyZombies = 50; // 2:3 to 1:4 ratio humans:zombies

        int AgainstCountlessZombies = 100; // 1 to 4+ ratio humans:zombies

        int BulletDamage = 100; // Current setting

        #endregion

        #region HumanVictoryVars

        int KillsIf8OrLessPlayers = 12;

        int KillsIf12To9Players = 18;

        int KillsIf16To13Players = 24;

        int KillsIf20To17Players = 30;

        int KillsIf24To21Players = 35;

        int KillsIf28To25Players = 40;

        int KillsIf32To29Players = 45;

        #endregion

        public enum GameVersion { BF3, BF4, BFH };

        private GameVersion fGameVersion = GameVersion.BF4;

        // BF3
        private string[] ZombieWeapons =
        {
            "Melee",
            "Defib",
            "Knife_RazorBlade",
            "Knife",
            "Repair Tool"
        };

        #region WeaponList
        // BF3
        private List<String> WeaponList = new List<String>(new string[] {
            "870MCS",
            "AEK-971",
            "AKS-74u",
            "AN-94 Abakan",
            "AS Val",
            "DAO-12",
            "Defib",
            "F2000",
            "FAMAS",
            "FGM-148",
            "FIM92",
            "Glock18",
            "HK53",
            "jackhammer",
            "JNG90",
            "Knife_RazorBlade",
            "L96",
            "LSAT",
            "M416",
            "M417",
            "M1014",
            "M15 AT Mine",
            "M16A4",
            "M1911",
            "M240",
            "M249",
            "M26Mass",
            "M27IAR",
            "M320",
            "M39",
            "M40A5",
            "M4A1",
            "M60",
            "M67",
            "M9",
            "M93R",
            "Melee",
            "MG36",
            "Mk11",
            "Model98B",
            "MP7",
            "Pecheneg",
            "PP-19",
            "PP-2000",
            "QBB-95",
            "QBU-88",
            "QBZ-95",
            "Repair Tool",
            "RoadKill",
            "RPG-7",
            "RPK-74M",
            "SCAR-L",
            "SG 553 LB",
            "Siaga20k",
            "SKS",
            "SMAW",
            "SPAS-12",
            "SV98",
            "SVD",
            "Steyr AUG",
            "Taurus .44",
            "Type88",
            "USAS-12",
            "Weapons/A91/A91",
            "Weapons/AK74M/AK74",
            "Weapons/G36C/G36C",
            "Weapons/G3A3/G3A3",
            "Weapons/Gadgets/C4/C4",
            "Weapons/Gadgets/Claymore/Claymore",
            "Weapons/KH2002/KH2002",
            "Weapons/Knife/Knife",
            "Weapons/MagpulPDR/MagpulPDR",
            "Weapons/MP412Rex/MP412REX",
            "Weapons/MP443/MP443",
            "Weapons/MP443/MP443_GM",
            "Weapons/P90/P90",
            "Weapons/P90/P90_GM",
            "Weapons/Sa18IGLA/Sa18IGLA",
            "Weapons/SCAR-H/SCAR-H",
            "Weapons/UMP45/UMP45",
            "Weapons/XP1_L85A2/L85A2",
            "Weapons/XP2_ACR/ACR",
            "Weapons/XP2_L86/L86",
            "Weapons/XP2_MP5K/MP5K",
            "Weapons/XP2_MTAR/MTAR",
            "CrossBow"
        });

        private List<String> DefWeaponList = new List<String>();
        #endregion

        #region ZombieWeaponList
        // BF4 and BFH
        private List<String> DefZombieWeaponsEnabled = new List<String>(new string[] {
            "Repairtool",
            "Defib",
            "Melee",
            "Knife"
        });
        // BF3
        private List<String> ZombieWeaponsEnabled = new List<String>(new string[] {
            "Repair Tool",
            "Defib",
            "Melee",
            "Knife_RazorBlade",
            "Weapons/Knife/Knife"
        });
        #endregion

        #region HumanWeaponList
        // BF3
        private List<String> HumanWeaponsEnabled = new List<String>(new string[] {
            "870MCS",
            "AEK-971",
            "AKS-74u",
            "AN-94 Abakan",
            "AS Val",
            "DAO-12",
            "Defib",
            "F2000",
            "FAMAS",
            "FGM-148",
            "FIM92",
            "Glock18",
            "HK53",
            "jackhammer",
            "JNG90",
            "Knife_RazorBlade",
            "L96",
            "LSAT",
            "M416",
            "M417",
            "M1014",
            // Off: "M15 AT Mine",
            "M16A4",
            "M1911",
            "M240",
            "M249",
            "M26Mass",
            "M27IAR",
            // Off: "M320",
            "M39",
            "M40A5",
            "M4A1",
            "M60",
            // Off: "M67",
            "M9",
            "M93R",
            "Melee",
            "MG36",
            "Mk11",
            "Model98B",
            "MP7",
            "Pecheneg",
            "PP-19",
            "PP-2000",
            "QBB-95",
            "QBU-88",
            "QBZ-95",
            "Repair Tool",
            "RoadKill",
            // Off: "RPG-7",
            "RPK-74M",
            "SCAR-L",
            "SG 553 LB",
            "Siaga20k",
            "SKS",
            // Off: "SMAW",
            "SPAS-12",
            "SV98",
            "SVD",
            "Steyr AUG",
            "Taurus .44",
            "Type88",
            "USAS-12",
            "Weapons/A91/A91",
            "Weapons/AK74M/AK74",
            "Weapons/G36C/G36C",
            "Weapons/G3A3/G3A3",
            // Off: "Weapons/Gadgets/C4/C4",
            // Off: "Weapons/Gadgets/Claymore/Claymore",
            "Weapons/KH2002/KH2002",
            "Weapons/Knife/Knife",
            "Weapons/MagpulPDR/MagpulPDR",
            "Weapons/MP412Rex/MP412REX",
            "Weapons/MP443/MP443",
            "Weapons/MP443/MP443_GM",
            "Weapons/P90/P90",
            "Weapons/P90/P90_GM",
            "Weapons/Sa18IGLA/Sa18IGLA",
            "Weapons/SCAR-H/SCAR-H",
            "Weapons/UMP45/UMP45",
            "Weapons/XP1_L85A2/L85A2",
            "Weapons/XP2_ACR/ACR",
            "Weapons/XP2_L86/L86",
            "Weapons/XP2_MP5K/MP5K",
            "Weapons/XP2_MTAR/MTAR",
            "CrossBow"
        });

        // BF4
        private List<String> BF4HumanWeaponsDisabled = new List<String>(new string[] {
            // Explosives
            "C4",
            "Claymore",
            "M15",
            "M320",
            "MGL",
            "SLAM",
            "UCAV",
            "XM25",
            "Grenade",
            "M67",
            "V40",
            // Rocket launchers
            "AT4",
            "FGM148",
            "FIM92",
            "NLAW",
            "RPG7",
            "Sa18IGLA",
            "SMAW",
            "SRAW",
            "Starstreak"
        });

        // BFH
        private List<String> BFHHumanWeaponsDisabled = new List<String>(new string[] {
            // Explosives
            "CS Gas",
                // "Flashbang",
                // "IncendiaryDevice",
            "M18",
            "M320",
            "M67",
            "M79",
                // "Molotov",
                // "SabotageTool",
                // "TripMine"

            // Rocket launchers
            "FIM92",
            "RPG7",
            "smaw",
            "SP RPG7",
            "SP smaw",
            "u_smaw",
            "u_sp_smaw"
        });


        #endregion

        #region EventHandlers

        /** EVENT HANDLERS **/

        public override void OnPlayerJoin(string SoldierName)
        {
            // Comes before OnPlayerAuthenticated
            if (ZombieModeEnabled)
            {
                KillTracker.AddPlayer(SoldierName);
                AddJoinQueue(SoldierName);
                RequestPlayersList();
            }
            else
            {
                SetState(GState.Idle);
            }
        }


        public override void OnPlayerAuthenticated(string SoldierName, string guid)
        {
            // Comes after OnPlayerJoin
            if (ZombieModeEnabled == false)
                return;

            DebugWrite("OnPlayerAuthenticated: " + SoldierName + ", Player.Count=" + PlayerList.Count + " + JoinQueue.Count=" + JoinQueue.Count + " ? MaxPlayers=" + MaxPlayers, 3);

            if (PlayerList.Count + JoinQueue.Count <= MaxPlayers)
            {
                PlayerState.AddPlayer(SoldierName);
                return;
            }

            // Otherwise, we have too many players, kick this one

            DebugWrite("OnPlayerAuthenticated: MaxPlayers of " + MaxPlayers + " exceeded, need to kick " + SoldierName, 2);

            base.OnPlayerAuthenticated(SoldierName, guid);

            /*
			Add name to the kick queue. We can't kick this player until
			the server has registered his name, which happens after
			authenticate but before team changing or spawning.
			*/

            PlayerKickQueue.Add(SoldierName);
        }

        public override void OnPlayerKilled(Kill info)
        {
            if (ZombieModeEnabled == false)
                return;

            if (GetState() != GState.Idle)
            {
                PlayerState.UpdateSpawnTime(info.Killer.SoldierName);
                PlayerState.UpdateSpawnTime(info.Victim.SoldierName);
                PlayerState.SetSpawned(info.Victim.SoldierName, false);
                // Since we can't log vars values in plugin.log, at least log to console.log
                if (DebugLevel > 5) ExecuteCommand("procon.protected.send", "vars.bulletDamage");
            }

            if (GetState() != GState.Playing)
                return;

            // Extract the short weapon name
            Match WeaponMatch = Regex.Match(info.DamageType, @"Weapons/[^/]*/([^/]*)", RegexOptions.IgnoreCase);
            String WeaponName = (WeaponMatch.Success) ? WeaponMatch.Groups[1].Value : info.DamageType;

            if (fGameVersion == GameVersion.BF4)
            {
                WeaponName = FriendlyWeaponName(WeaponName);
            }

            const String INDIRECT_KILL = "INDIRECT KILL";

            String KillerName = (String.IsNullOrEmpty(info.Killer.SoldierName)) ? INDIRECT_KILL : info.Killer.SoldierName;

            String KillerTeam = info.Killer.TeamID.ToString();

            String VictimName = info.Victim.SoldierName;

            String VictimTeam = info.Victim.TeamID.ToString();

            String DamageType = info.DamageType;

            String InfectMessage = null;

            int RemainingHumans = 0;


            // Killed by admin?
            if (info.DamageType == "Death")
                return;

            DebugWrite("OnPlayerKilled: " + KillerName + " killed " + VictimName + " with " + WeaponName, 4);

            lock (TeamHuman)
            {
                RemainingHumans = TeamHuman.Count - 1;
            }

            if (RemainingHumans > 0)
            {
                InfectMessage = "*** Only " + RemainingHumans + " humans left!"; // $$$ - custom message
            }
            else
            {
                InfectMessage = "*** No humans left!"; // $$$ - custom message
            }

            RemainingHumans = TeamHuman.Count;

            String Notice = WeaponName + " results in non-scoring accidental death, respawn and try again";  // $$$ - custom message

            // Weapon validation

            if (KillerName == INDIRECT_KILL)
            {
                TellPlayer(VictimName + ": " + Notice, VictimName, false);
                return;
            }
            else if (Regex.Match(DamageType, @"(?:DamageArea|RoadKill|SoldierCollision)").Success)
            {
                DebugWrite("OnPlayerKilled: " + DamageType + " is a non-scoring kill of both parties", 3);

                TellPlayer(Notice, KillerName, false);

                if (KillerName != VictimName) TellPlayer(Notice, VictimName, false);

                KillPlayerAfterDelay(KillerName, 5);
                return;
            }
            else if (KillerName != VictimName && ValidateWeapon(DamageType, KillerTeam) == false)
            {
                String msg = "ZOMBIE RULE VIOLATION! " + WeaponName + " can't be used by " + ((KillerTeam == ZOMBIE_TEAM) ? "Zombie!" : "Human!");  // $$$ - custom message

                DebugWrite(msg + " " + KillerName + " killed " + VictimName, 2);

                TellAll(KillerName + " -> " + msg);

                int Count = KillTracker.GetViolations(KillerName);

                if (Count < WarnsBeforeKickForRulesViolations)
                {
                    // Warning
                    KillPlayerAfterDelay(KillerName, 5);
                }
                else if (TempBanInsteadOfKick)
                {
                    DebugWrite("OnPlayerKilled: ^b^8TEMP BAN " + KillerName, 1);

                    String unit = "seconds";
                    double dur = TempBanSeconds;
                    if (TempBanSeconds > 60 && TempBanSeconds < (90 * 60))
                    {
                        unit = "minutes";
                        dur = dur / 60.0;
                    }
                    else if (TempBanSeconds < (24 * 60 * 60))
                    {
                        unit = "hours";
                        dur = dur / (60.0 * 60.0);
                    }
                    else if (TempBanSeconds >= (24 * 60 * 60))
                    {
                        unit = "days";
                        dur = dur / (24.0 * 60.0 * 60.0);
                    }


                    TellAll("::::: Banning " + KillerName + " for " + dur.ToString("F0") + "  " + unit + "! :::::"); // $$$ - custom message

                    TempBanPlayer(KillerName, "ZOMBIE RULE VIOLATION: bad weapon"); // $$$  - custom message
                }
                else
                {
                    DebugWrite("OnPlayerKilled: ^b^8KICK " + KillerName, 2);

                    KickPlayer(KillerName, msg);
                }

                KillTracker.SetViolations(KillerName, Count + 1);

                return;
            }

            // Scoring logic

            if (KillerName == VictimName)
            {
                if (KillerTeam == HUMAN_TEAM && InfectSuicides)
                {
                    DebugWrite("Suicide infected: " + VictimName, 3);
                    Infect("Suicide", VictimName);
                    TellAll(InfectMessage, false); // do not overwrite Infect yell
                    --RemainingHumans;
                }
                else
                {
                    TellPlayer(VictimName + ": Suicide with " + Notice, VictimName, false);
                    return;
                }
            }
            else if (KillerTeam == HUMAN_TEAM && VictimTeam == ZOMBIE_TEAM)
            {
                KillTracker.ZombieKilled(KillerName, VictimName);

                DebugWrite(String.Concat("Human ", KillerName, " just killed zombie ", VictimName, " with ", WeaponName), 3);

                int TotalCount = 0;

                lock (TeamHuman)
                {
                    TotalCount = TeamHuman.Count + TeamZombie.Count;
                }

                TellAll("*** Humans killed " + KillTracker.GetZombiesKilled() + " of " + GetKillsNeeded(TotalCount) + " zombies needed to win!"); // $$$ - custom message

                // Check for self-infecting kill
                if (Regex.Match(info.DamageType, @"(?:Knife|Melee|Defib|Repair)", RegexOptions.IgnoreCase).Success)
                {
                    // Infect player
                    Infect("Contact Kill", KillerName);
                    // overwrite infect yell
                    TellPlayer("You infected yourself with that " + WeaponName + " kill!", KillerName); // $$$ - custom message
                }
            }
            else if (KillerTeam == ZOMBIE_TEAM && VictimTeam == HUMAN_TEAM)
            {
                DebugWrite(String.Concat("Zombie ", KillerName, " just killed human ", VictimName, " with ", WeaponName), 3);

                if (Regex.Match(WeaponName, @"(?:Knife|Melee|Defib|Repair)", RegexOptions.IgnoreCase).Success)
                {
                    KillTracker.HumanKilled(KillerName, VictimName);

                    if (KillTracker.GetPlayerHumanDeathCount(VictimName) == DeathsNeededToBeInfected)
                    {
                        Infect(KillerName, VictimName);
                        TellAll(InfectMessage, false); // do not overwrite Infect yell
                        --RemainingHumans;
                    }
                }
                else
                {
                    Notice = "Zombie " + KillerName + " using " + WeaponName + " did NOT infect human " + VictimName;
                    DebugWrite(Notice, 3);
                    TellPlayer(Notice, KillerName);
                    TellPlayer(Notice, VictimName, false);
                }
            }

            lock (TeamHuman)
            {
                DebugWrite("OnPlayerKilled: " + RemainingHumans + " humans vs " + ((RemainingHumans == TeamHuman.Count) ? TeamZombie.Count : (TeamZombie.Count + 1)) + " zombies with " + KillTracker.GetZombiesKilled() + " of " + GetKillsNeeded(TeamZombie.Count + TeamHuman.Count) + " zombies killed", (VictimTeam == HUMAN_TEAM) ? 2 : 3);
            }

            CheckVictoryConditions(RemainingHumans == 0);
        }

        public override void OnListPlayers(List<CPlayerInfo> Players, CPlayerSubset Subset)
        {
            PlayerList = Players;

            if (ZombieModeEnabled == false)
                return;

            if (Players.Count + JoinQueue.Count > 0 && GetState() != GState.Idle) DebugWrite("OnListPlayers: " + Players.Count + " players, " + JoinQueue.Count + " joining", 5);

            if (OldGameState != GetState()) DebugWrite("OnListPlayers: GameState = " + GetState(), 3);
            OldGameState = GetState();

            if (CheckIdle(Players))
            {
                // We kicked some idle players, so update the player list again
                RequestPlayersList();
                return;
            }

            List<String> HumanCensus = new List<String>();
            List<String> ZombieCensus = new List<String>();

            foreach (CPlayerInfo Player in Players)
            {
                KillTracker.AddPlayer(Player.SoldierName.ToString());

                // Team tracking
                if (Player.TeamID == 1)
                {
                    HumanCensus.Add(Player.SoldierName);
                    DebugWrite("OnListPlayers: counted " + Player.SoldierName + " as human (" + HumanCensus.Count + ")", 6);
                }
                else if (Player.TeamID == 2)
                {
                    // Othewise, add
                    ZombieCensus.Add(Player.SoldierName);
                    DebugWrite("OnListPlayers: counted " + Player.SoldierName + " as zombie (" + ZombieCensus.Count + ")", 6);
                }
                else
                {
                    DebugWrite("OnListPlayers: unknown team " + Player.TeamID + " for player " + Player.SoldierName, 5);
                }

                RemoveJoinQueue(Player.SoldierName);
            }

            // Check for differences

            KnownPlayerCount = HumanCensus.Count + ZombieCensus.Count;

            bool SomeoneMoved = false;

            lock (TeamHuman)
            {
                if (Players.Count > 0) DebugWrite("OnListPlayers: human count " + TeamHuman.Count + " vs " + HumanCensus.Count + ", zombie count " + TeamZombie.Count + " vs " + ZombieCensus.Count, 6);

                SomeoneMoved = (TeamHuman.Count != HumanCensus.Count);
                SomeoneMoved |= (TeamZombie.Count != ZombieCensus.Count);
            }

            if (GetState() != GState.Idle && (HumanCensus.Count + ZombieCensus.Count) == 0)
            {
                Reset();
                return;
            }

            if (GetState() == GState.Playing)
            {
                if (SomeoneMoved)
                {
                    DebugWrite("OnListPlayers: playing, checking victory conditions", 5);
                }
                CheckVictoryConditions(false);
            }
            else if (GetState() == GState.BetweenRounds)
            {
                DebugWrite("OnListPlayers: between rounds", 5);
            }
            else if (GetState() == GState.Idle || GetState() == GState.Waiting || GetState() == GState.RoundStarting)
            {
                // force update when not playing a match
                if (SomeoneMoved)
                {
                    DebugWrite("OnListPlayers: teams updated, not playing yet", 5);

                    lock (TeamHuman)
                    {
                        TeamHuman.Clear();
                        TeamHuman.AddRange(HumanCensus);
                        TeamZombie.Clear();
                        TeamZombie.AddRange(ZombieCensus);
                    }
                }
            }
        }

        public override void OnGlobalChat(string PlayerName, string Message)
        {
            HandleChat(PlayerName, Message, -1, -1);
        }

        public override void OnTeamChat(string PlayerName, string Message, int TeamId)
        {
            HandleChat(PlayerName, Message, TeamId, -1);
        }

        public override void OnSquadChat(string PlayerName, string Message, int TeamId, int SquadId)
        {
            HandleChat(PlayerName, Message, TeamId, SquadId);
        }

        public void HandleChat(string PlayerName, string Message, int TeamId, int SquadId)
        {
            String CleanMessage = Message.Trim();

            List<string> MessagePieces = new List<string>(CleanMessage.Split(' '));

            String Command = MessagePieces[0].ToLower();

            if (PlayerName == "Server")
            {
                DebugWrite("------ CHAT: " + CleanMessage, 7);
                return;
            }

            if (!Command.StartsWith(CommandPrefix.ToLower()))
            {
                if (Regex.Match(CleanMessage, @"(?:help|zombie|rules|work)", RegexOptions.IgnoreCase).Success)
                {
                    TellPlayer("Type: !zombie help", PlayerName, false);
                }
                return;
            }

            DebugWrite(PlayerName + " tried command: " + CleanMessage, 2);

            if (CommandPrefix.Length > 1 && Command == CommandPrefix)
            {
                /*
				If Message is: !zombie command arg1 arg2
				Then remove "!zombie" from the MessagePieces and reset Command
				to be the value of 'command'.
				*/
                MessagePieces.Remove(CommandPrefix);
                if (MessagePieces.Count == 0)
                {
                    Command = "help";
                }
                else
                {
                    Command = MessagePieces[0].ToLower();
                }
            }
            else
            {
                /*
				If command is: !zcmd arg1 arg2
				Then remove "!z" from Command
				*/
                Match CommandMatch = Regex.Match(Command, "^" + CommandPrefix + @"([^\s]+)", RegexOptions.IgnoreCase);
                if (CommandMatch.Success)
                {
                    Command = CommandMatch.Groups[1].Value.ToLower();
                }
            }

            if (String.IsNullOrEmpty(Command)) Command = "help";

            DebugWrite("Command without prefix: " + Command, 6);

            String Target = null;

            switch (Command)
            {
                case "infect":
                    if (ZombieModeEnabled == false || GetState() == GState.Idle || GetState() == GState.Waiting)
                        return;

                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Only admins can use that command!", PlayerName);
                        return;
                    }
                    if (MessagePieces.Count != 2) return;
                    Target = PlayerNameMatch(MessagePieces[1]);
                    if (Target == null)
                    {
                        TellPlayer("No player name matches '" + MessagePieces[1] + "'", PlayerName, false);
                        return;
                    }
                    TellPlayer("Infecting " + Target, PlayerName, false);
                    Infect("Admin", Target); // Does TellAll
                    break;
                case "heal":
                    if (ZombieModeEnabled == false || GetState() == GState.Idle || GetState() == GState.Waiting)
                        return;

                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Only admins can use that command!", PlayerName);
                        return;
                    }
                    if (MessagePieces.Count != 2) return;
                    Target = PlayerNameMatch(MessagePieces[1]);
                    if (Target == null)
                    {
                        TellPlayer("No player name matches '" + MessagePieces[1] + "'", PlayerName, false);
                        return;
                    }
                    TellPlayer("Attempting move of " + Target + " to human team", PlayerName, false);
                    MakeHuman(Target);
                    break;
                case "rematch":
                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Only admins can use that command!", PlayerName);
                        return;
                    }
                    if (MessagePieces.Count != 2) return;
                    if (MessagePieces[1] == "on")
                        RematchEnabled = true;
                    else if (MessagePieces[1] == "off")
                        RematchEnabled = false;
                    TellPlayer("RematchEnabled is now " + RematchEnabled, PlayerName, false);
                    break;
                case "restart":
                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Only admins can use that command!", PlayerName);
                        return;
                    }
                    RestartRound();
                    Reset();
                    break;
                case "force":
                    // Force a match/round to start
                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Only admins can use that command!", PlayerName);
                        return;
                    }
                    TellAll("Admin has forced the start of a new match ...");
                    DebugWrite("***** HALT: by admin!", 2);
                    HaltMatch();
                    CountdownNextRound(ZOMBIE_TEAM);
                    break;
                case "next":
                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Only admins can use that command!", PlayerName);
                        return;
                    }
                    NextRound();
                    Reset();
                    break;
                case "mode":
                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Only admins can use that command!", PlayerName);
                        return;
                    }
                    if (MessagePieces.Count != 2) return;
                    if (MessagePieces[1] == "on")
                        ZombieModeEnabled = true;
                    else if (MessagePieces[1] == "off")
                        ZombieModeEnabled = false;
                    TellPlayer("ZombieModeEnabled is now " + ZombieModeEnabled, PlayerName, false);
                    break;
                    Reset();
                case "rules":
                    TellRules(PlayerName);
                    break;
                case "warn":
                    if (ZombieModeEnabled == false || GetState() == GState.Idle)
                        return;
                    if (MessagePieces.Count < 3) return;
                    string WarningMessage = String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray());
                    Target = PlayerNameMatch(MessagePieces[1]);
                    if (Target == null)
                    {
                        TellPlayer("No player name matches '" + MessagePieces[1] + "'", PlayerName, false);
                        return;
                    }
                    DebugWrite("Warning sent by " + PlayerName + " to " + Target + ": " + WarningMessage, 2);
                    Warn(Target, WarningMessage);
                    TellPlayer("Warning sent to " + Target, PlayerName, false);
                    break;

                case "kill":
                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Only admins can use that command!", PlayerName);
                        return;
                    }
                    if (MessagePieces.Count < 2) return;
                    string KillMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";
                    Target = PlayerNameMatch(MessagePieces[1]);
                    if (Target == null)
                    {
                        TellPlayer("No player name matches '" + MessagePieces[1] + "'", PlayerName, false);
                        return;
                    }
                    DebugWrite(PlayerName + " killing " + Target + " for '" + KillMessage + "'", 1);
                    TellPlayer(KillMessage, Target);
                    KillPlayerAfterDelay(Target, AnnounceDisplayLength);
                    TellPlayer("Attempting to kill " + Target + " in " + AnnounceDisplayLength + " seconds", PlayerName, false);
                    break;

                case "kick":
                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Only admins can use that command!", PlayerName);
                        return;
                    }
                    if (MessagePieces.Count < 2) return;
                    string KickMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";
                    Target = PlayerNameMatch(MessagePieces[1]);
                    if (Target == null)
                    {
                        TellPlayer("No player named '" + MessagePieces[1] + "'", PlayerName, false);
                        return;
                    }
                    DebugWrite(PlayerName + " kicking " + Target + " for '" + KickMessage + "'", 1);
                    KickPlayer(Target, KickMessage);
                    TellPlayer("Kicking " + Target, PlayerName, false);
                    break;
                case "votekick":
                    if (ZombieModeEnabled == false || GetState() == GState.Idle)
                        return;
                    if (MessagePieces.Count < 2) return;
                    Target = PlayerNameMatch(MessagePieces[1]);
                    if (Target == null)
                    {
                        TellPlayer("No player name matches '" + MessagePieces[1] + "'", PlayerName, false);
                        return;
                    }
                    DebugWrite(PlayerName + " voted to kick " + Target, 3);
                    VoteKick(PlayerName, Target);
                    break;
                case "votekill":
                    if (ZombieModeEnabled == false || GetState() == GState.Idle)
                        return;
                    if (MessagePieces.Count < 2) return;
                    Target = PlayerNameMatch(MessagePieces[1]);
                    if (Target == null)
                    {
                        TellPlayer("No player name matches '" + MessagePieces[1] + "'", PlayerName, false);
                        return;
                    }
                    DebugWrite(PlayerName + " voted to kill " + Target, 3);
                    VoteKill(PlayerName, Target);
                    break;
                case "status":
                    TellStatus(PlayerName);
                    break;
                case "idle":
                    {
                        double st = PlayerState.GetLastSpawnTime(PlayerName);
                        String isw = (PlayerState.GetSpawned(PlayerName)) ? "spawned" : "dead";
                        TellPlayer("Zombie plugin version: " + GetPluginVersion(), PlayerName, false);
                        TellPlayer("You are " + isw + " and your last action was " + st.ToString("F0") + " seconds ago", PlayerName, false);
                        break;
                    }
                case "test":
                    DebugWrite("loopz", 2);
                    DebugWrite(FrostbitePlayerInfoList.Values.Count.ToString(), 2);
                    foreach (CPlayerInfo Player in FrostbitePlayerInfoList.Values)
                    {
                        DebugWrite("looping", 2);
                        String testmessage = Player.SoldierName;
                        DebugWrite(testmessage, 2);
                    }
                    break;
                default: // "help"
                         //TellPlayer("Spawn to get things started", PlayerName);
                    if (!IsAdmin(PlayerName))
                    {
                        TellPlayer("Type !zombie <command>\nCommands: rules, help, status, idle, warn, votekick, votekill", PlayerName, false);
                    }
                    else
                    {
                        TellPlayer("Type !zombie <command>\nTo force a match to start, type: !zombie force", PlayerName, false);
                        TellPlayer("Admin commands: infect, heal, rematch, restart, next, force, mode, kill, kick", PlayerName, false);
                        TellPlayer("Player commands: rules, help, status, idle, warn, votekick, votekill", PlayerName, false);
                    }
                    break;
            }

        }

        public override void OnServerInfo(CServerInfo serverInfo)
        {
            // This is just to test debug logging
            DebugWrite("OnServerInfo: Debug level = " + DebugLevel + " ....", 9);
            DebugWrite("GameState = " + GetState(), 8);

            if (GetState() == GState.BetweenRounds)
            {
                lock (TeamHuman)
                {
                    KnownPlayerCount = TeamHuman.Count + TeamZombie.Count;
                }
            }
        }

        public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId)
        {
            if (ZombieModeEnabled == false)
                return;

            RemoveJoinQueue(soldierName);

            // Kick any over max players
            bool KickIt = false;
            lock (PlayerKickQueue)
            {
                KickIt = PlayerKickQueue.Contains(soldierName);
            }

            if (KickIt)
            {
                DebugWrite("OnPlayerTeamChange: Over max with " + soldierName, 5);
                ScheduleKick(soldierName, "Zombie Mode max players of " + MaxPlayers + " exceeded, try later"); // $$$ - custom message
                return;
            }

            // Update spawn time to prevent early idle timeout

            PlayerState.UpdateSpawnTime(soldierName);

            // Sanity checks

            bool wasZombie = false;
            bool wasHuman = false;

            lock (TeamHuman)
            {
                wasZombie = TeamZombie.Contains(soldierName);
                wasHuman = TeamHuman.Contains(soldierName);
            }

            // Ignore squad changes within team
            if (teamId == 1 && wasHuman) return;
            if (teamId == 2 && wasZombie) return;
            if (!(teamId == 1 || teamId == 2))
            {
                ConsoleError("OnPlayerTeamChange unknown teamId = " + teamId);
                return;
            }

            if (GetState() == GState.Idle || GetState() == GState.Waiting || GetState() == GState.CountingDown)
            {
                UpdateTeams(soldierName, teamId);
                return;
            }

            string team = (wasHuman) ? "HUMAN" : "JOINING";
            team = (wasZombie) ? "ZOMBIE" : "JOINING";
            DebugWrite("OnPlayerTeamChange: " + soldierName + "(" + team + ") to " + teamId + " {" + GetState().ToString() + "}", 3);


            // Short-cut update states

            if (GetState() == GState.Moving || GetState() == GState.RoundStarting)
            {
                DebugWrite("OnPlayerTeamChange: ^bUPDATING TEAMS^0", 6);
                UpdateTeams(soldierName, teamId);

                if (GetState() == GState.Moving && soldierName == LastMover && teamId == 2)
                {
                    LastMover = null;
                }

                return;
            }

            if (GetState() == GState.Playing || GetState() == GState.NeedSpawn)
            {
                if (teamId == 1 && wasZombie) // to humans
                {
                    // Switching to human team is not allowed
                    TellPlayer("Don't switch to the human team! Sending you back to zombies!", soldierName); // $$$ - custom message

                    ForceMove(soldierName, ZOMBIE_TEAM, AnnounceDisplayLength);
                }
                else if (teamId == 2 && wasHuman) // to zombies
                {
                    // Switching to the zombie team is okay
                    FreshZombie.Add(soldierName);
                    UpdateTeams(soldierName, teamId);

                    CheckVictoryConditions(false);
                }
                else if (!wasHuman && !wasZombie)
                {
                    // New player joining in the middle of the match

                    DebugWrite("OnPlayerTeamChange: new player " + soldierName + " just joined on team " + teamId, 3);

                    if (teamId != ((NewPlayersJoinHumans) ? 1 : 2))
                    {
                        string Which = (NewPlayersJoinHumans) ? HUMAN_TEAM : ZOMBIE_TEAM;

                        DebugWrite("OnPlayerTeamChange: switching new player " + soldierName + " to team " + Which, 3);

                        ForceMove(soldierName, Which);
                    }

                    UpdateTeams(soldierName, ((NewPlayersJoinHumans) ? 1 : 2));

                    CheckVictoryConditions(false);
                }
                else
                {
                    ConsoleError("OnPlayerTeamChange: Playing/NeedSpawn " + soldierName + " not in expected team state, forcing update!");

                    UpdateTeams(soldierName, teamId);

                    CheckVictoryConditions(false);
                }
            }
            else if (GetState() == GState.BetweenRounds)
            { // server is swapping teams

                int ZombieCount = 0;

                DebugWrite("OnPlayerTeamChange: ServerSwitchedCount=" + (ServerSwitchedCount + 1) + ", KnownPlayerCount=" + KnownPlayerCount, 5);

                if (teamId == 1) // to humans
                {
                    ++ServerSwitchedCount;

                    // Add to the lottery if eligible
                    if (!PatientZeroes.Contains(soldierName)) Lottery.Add(soldierName);

                    UpdateTeams(soldierName, 1);
                }
                else if (teamId == 2) // to zombies
                {
                    ++ServerSwitchedCount;

                    // Switch back
                    MakeHumanFast(soldierName);

                    UpdateTeams(soldierName, 1);
                }


                // When the server is done swapping players, process patient zero
                if (ServerSwitchedCount >= KnownPlayerCount)
                {
                    while (ZombieCount < MinimumZombies)
                    {
                        // Sanity checks
                        if (PlayerList.Count < (MinimumZombies + MinimumHumans))
                        {
                            DebugWrite("OnPlayerTeamChange: not enough players " + PlayerList.Count, 2);
                            Reset();
                            return;
                        }

                        if (Lottery.Count == 0)
                        {
                            // loop through players, adding to Lottery if eligible
                            foreach (CPlayerInfo p in PlayerList)
                            {
                                if (!PatientZeroes.Contains(p.SoldierName))
                                {
                                    Lottery.Add(p.SoldierName);
                                }
                            }
                        }

                        if (Lottery.Count == 0)
                        {
                            DebugWrite("OnPlayerTeamChange, can't find an eligible player for patient zero!", 3);
                            PatientZeroes.Clear();
                            Lottery.Add(soldierName);
                        }

                        Random rand = new Random();
                        int choice = (Lottery.Count == 1) ? 0 : (rand.Next(Lottery.Count));
                        PatientZero = Lottery[choice];
                        DebugWrite("OnPlayerTeamChange: lottery selected " + PatientZero + " as a zombie!", 3);
                        Lottery.Remove(PatientZero);


                        MakeZombie(PatientZero);

                        UpdateTeams(PatientZero, 2);

                        if (PatientZeroes.Count > (KnownPlayerCount / 2)) PatientZeroes.Clear();

                        PatientZeroes.Add(PatientZero);

                        ++ZombieCount;
                    }

                    DebugWrite("OnPlayerTeamChange: making " + PatientZero + " the first zombie!", 2);

                    ServerSwitchedCount = 0;

                    SetState(GState.RoundStarting);
                }
                /*
				GameState stays in BetweenRounds state because we don't know when the
				actual round starts until a player spawns. See OnPlayerSpawned.
				*/
            }

        }


        public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            if (ZombieModeEnabled == false)
            {
                SetState(GState.Idle);
                return;
            }

            RemoveJoinQueue(soldierName);

            // Kick any over max players
            bool KickIt = false;
            lock (PlayerKickQueue)
            {
                KickIt = PlayerKickQueue.Contains(soldierName);
            }

            if (KickIt)
            {
                DebugWrite("OnPlayerSpawned: Over max with " + soldierName, 4);
                ScheduleKick(soldierName, "Zombie Mode max players of " + MaxPlayers + " exceeded, try later"); // $$$ - custom message
                return;
            }


            String WhichTeam = (GetState() == GState.Playing) ? "UNKNOWN" : GetState().ToString();

            bool NeedUpdate = true;

            lock (TeamHuman)
            {
                if (TeamZombie.Contains(soldierName))
                {
                    WhichTeam = "ZOMBIE";
                    NeedUpdate = false;
                }
                else if (TeamHuman.Contains(soldierName))
                {
                    WhichTeam = "HUMAN";
                    NeedUpdate = false;
                }
            }
            DebugWrite("OnPlayerSpawned: " + soldierName + "(" + WhichTeam + ")", 4);

            PlayerState.UpdateSpawnTime(soldierName);
            PlayerState.SetSpawned(soldierName, true);

            if (NeedUpdate)
            {
                int ToTeam = (NewPlayersJoinHumans) ? 1 : 2;

                DebugWrite("OnPlayerSpawned: ^1^bforcing move to team " + ToTeam, 3);

                TellPlayer(soldierName + ": Sorry, need to force you to respawn ...", soldierName, false);

                UpdateTeams(soldierName, ToTeam);

                if (ToTeam == 1)
                {
                    ForceMove(soldierName, HUMAN_TEAM, 0);
                }
                else
                {
                    ForceMove(soldierName, ZOMBIE_TEAM, 0);
                }

                return;
            }


            // Check if we have enough players spawned
            int Need = MinimumHumans + MinimumZombies;
            if (CountAllTeams() < Need)
            {
                if (GetState() == GState.Playing)
                {
                    TellAll("Not enough players left to finish match ... MATCH HALTED!");
                    DebugWrite("***** HALT: not enough players!", 1);
                    HaltMatch(); // Sets GameState to Waiting
                }
                else
                {
                    TellAll("Welcome to Zombie Mode! Need " + (Need - CountAllTeams()) + " more players to join AND spawn to start the match ..."); // $$$ - custom message
                }
                SetState(GState.Waiting);
                return; // Don't count this spawn
            }
            else if (CountAllTeams() >= Need && (GetState() == GState.Waiting || GetState() == GState.Idle))
            {
                TellAll("New match starting ... counting down ..."); // $$$ - custom message
                CountdownNextRound(ZOMBIE_TEAM); // Sets GameState to CountingDown or NeedSpawn
                return;
            }

            // Sanity check
            if (GetState() == GState.BetweenRounds)
            {
                int cz = 0;

                lock (TeamHuman)
                {
                    cz = TeamZombie.Count;
                }

                if (PatientZero == null || cz == 0)
                {
                    // We failed to pick a zombie, so use this spawner
                    DebugWrite("OnPlayerSpawned: BetweenRounds sanity check failed, forcing zombie " + soldierName, 5);

                    SetState(GState.RoundStarting);
                    ServerSwitchedCount = 0;

                    PatientZero = soldierName;
                    PatientZeroes.Add(PatientZero);

                    DebugWrite("OnPlayerSpawned: making " + PatientZero + " the first zombie!", 2);

                    MakeZombie(soldierName);

                    UpdateTeams(soldierName, 2);
                }
            }

            // Check if this is the first spawn of the round/match
            if (GetState() == GState.BetweenRounds || GetState() == GState.NeedSpawn || GetState() == GState.RoundStarting)
            {
                SetState(GState.Playing);
                if (RematchEnabled)
                {
                    ++MatchesCount;
                    DebugWrite("Match " + MatchesCount + " of " + MatchesBeforeNextMap, 2);
                }
                DebugWrite("--- Version " + GetPluginVersion() + " ---", 1);
                DebugWrite("^b^2****** MATCH STARTING WITH " + CountAllTeams() + " players!^0^n", 1);
                DebugWrite("OnPlayerSpawned: announcing first zombie is " + PatientZero, 5);
                TellAll(PatientZero + " is the first zombie!"); // $$$ - custom message
            }
            else if (GetState() == GState.Moving)
            {
                DebugWrite("OnPlayerSpawned: early spawner while still moving " + soldierName, 3);
                TellPlayer("The match hasn't started yet, you might have to respawn ...", soldierName);
                return;
            }

            // Otherwise, GameState is Playing

            int n = PlayerState.GetSpawnCount(soldierName);

            // Tell zombies they can only use hand to hand weapons
            if (FreshZombie.Contains(soldierName))
            {
                DebugWrite("OnPlayerSpawned " + soldierName + " is fresh zombie!", 5);
                FreshZombie.Remove(soldierName);
                TellPlayer("You are now a zombie! Use a knife/defib/repair tool only!", soldierName); // $$$ - custom message
            }
            else if (PlayerState.GetWelcomeCount(soldierName) == 0)
            {
                String Separator = " ";
                if (CommandPrefix.Length == 1) Separator = "";
                TellPlayer("Welcome to Zombie Mode! Type '" + CommandPrefix + Separator + "rules' for instructions on how to play", soldierName); // $$$ - custom message
                PlayerState.SetWelcomeCount(soldierName, 1);
            }
            else if (n == 0)
            {
                lock (TeamHuman)
                {
                    if (!TeamHuman.Contains(soldierName))
                    {
                        ConsoleError("OnPlayerSpawned: " + soldierName + " should be human, but not present in TeamHuman list!");

                        if (GetState() == GState.Playing)
                        {
                            DebugWrite("ZOMBIE MODE STOPPED - teams are not right!", 1);
                            TellAll("ZOMBIE MODE STOPPED - teams are not right!");
                            TellAll("Respawn or run next map round/level to fix it", false);
                            Reset();
                            RequestPlayersList(); // Should fix up teams
                            return;
                        }
                    }
                }
                TellPlayer("You are a human! Shoot zombies, don't use explosives, don't let zombies get near you!", soldierName); // $$$ - custom message
            }

            PlayerState.SetSpawnCount(soldierName, n + 1);

            AdaptDamage();
        }

        public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)
        {
            if (ZombieModeEnabled == false)
            {
                SetState(GState.Idle);
                return;
            }

            DebugWrite("OnLevelLoaded, updating player list", 3);

            // We have 5 seconds before the server swaps teams, make sure we are up to date
            RequestPlayersList();

            // Reset the team switching counter
            ServerSwitchedCount = 0;

            // Reset the known player count
            KnownPlayerCount = 0;

            // Reset the utility lists
            FreshZombie.Clear();
            Lottery.Clear();

            // Reset patient zero
            PatientZero = null;

            // Reset per-round player states
            PlayerState.ResetPerRound();

            // Reset kill tracker
            KillTracker.ResetPerRound();

            // Sanity check
            DebugWrite("OnLevelLoaded: GameState is " + GetState(), 3);
        }

        public override void OnRoundOver(int winningTeamId)
        {
            if (ZombieModeEnabled == false)
            {
                SetState(GState.Idle);
                return;
            }

            DebugWrite("OnRoundOver, GameState set to BetweenRounds", 3);

            SetState(GState.BetweenRounds);

            // Reset the team switching counter
            ServerSwitchedCount = 0;

            // Reset the known player count
            KnownPlayerCount = 0;

            // Reset the utility lists
            FreshZombie.Clear();
            Lottery.Clear();

            // Reset patient zero
            PatientZero = null;

            // Reset per-round player states
            PlayerState.ResetPerRound();

            // Reset kill tracker
            KillTracker.ResetPerRound();
        }

        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            if (ZombieModeEnabled == false)
            {
                SetState(GState.Idle);
                return;
            }

            DebugWrite("OnPlayerLeft: " + playerInfo.SoldierName, 3);

            lock (TeamHuman)
            {
                if (TeamHuman.Contains(playerInfo.SoldierName)) TeamHuman.Remove(playerInfo.SoldierName);
                if (TeamZombie.Contains(playerInfo.SoldierName)) TeamZombie.Remove(playerInfo.SoldierName);
            }

            KillTracker.RemovePlayer(playerInfo.SoldierName);

            RemoveJoinQueue(playerInfo.SoldierName);

            lock (PlayerKickQueue)
            {
                if (PlayerKickQueue.Contains(playerInfo.SoldierName))
                {
                    PlayerKickQueue.Remove(playerInfo.SoldierName);
                }
            }

            if (GetState() == GState.Playing)
            {
                AdaptDamage();

                CheckVictoryConditions(false);
            }
        }

        public override void OnPlayerKickedByAdmin(string SoldierName, string reason)
        {
            if (ZombieModeEnabled == false)
                return;

            DebugWrite("OnPlayerKickedByAdmin: " + SoldierName + ", reason: " + reason, 1);

            KillTracker.RemovePlayer(SoldierName);

            RemoveJoinQueue(SoldierName);

            lock (PlayerKickQueue)
            {
                if (PlayerKickQueue.Contains(SoldierName))
                {
                    PlayerKickQueue.Remove(SoldierName);
                }
            }
        }

        #endregion


        #region PluginMethods
        /** PLUGIN RELATED SHIT **/
        #region PluginEventHandlers

        public void OnPluginLoadingEnv(List<string> lstPluginEnv)
        {
            foreach (String env in lstPluginEnv)
            {
                DebugWrite("^9OnPluginLoadingEnv: " + env, 8);
            }
            switch (lstPluginEnv[1])
            {
                case "BF3": fGameVersion = GameVersion.BF3; break;
                case "BF4": fGameVersion = GameVersion.BF4; break;
                case "BFHL": fGameVersion = GameVersion.BFH; break;
                default: break;
            }

            if (fGameVersion != GameVersion.BF3)
            { // GameVersion.BF4 or GameVersion.BFH
                // initialize values for all known weapons
                WeaponDictionary dic = GetWeaponDefines();
                DefWeaponList.Clear();
                foreach (Weapon weapon in dic)
                {
                    if (weapon == null || String.IsNullOrEmpty(weapon.Name)) continue;
                    String wn = FriendlyWeaponName(weapon.Name);
                    if (!DefWeaponList.Contains(wn))
                        DefWeaponList.Add(wn);
                }
                DefWeaponList.Sort();
                // Initialize the human weapon list
                HumanWeaponsEnabled.Clear();
                HumanWeaponsEnabled.AddRange(DefWeaponList);
                if (fGameVersion == GameVersion.BF4)
                {
                    foreach (String disable in BF4HumanWeaponsDisabled)
                    {
                        if (HumanWeaponsEnabled.Contains(disable))
                            HumanWeaponsEnabled.Remove(disable);
                    }
                }
                else if (fGameVersion == GameVersion.BFH)
                {
                    foreach (String disable in BFHHumanWeaponsDisabled)
                    {
                        if (HumanWeaponsEnabled.Contains(disable))
                            HumanWeaponsEnabled.Remove(disable);
                    }
                }
                // Initialize the zombie weapon list
                ZombieWeaponsEnabled.Clear();
                //ZombieWeaponsEnabled.AddRange(BF4ZombieWeaponsEnabled);
                foreach (String okz in DefWeaponList)
                {
                    if (DefZombieWeaponsEnabled.Contains(okz))
                    {
                        ZombieWeaponsEnabled.Add(okz);
                    }
                }


                // Change mode to say until yell is available
                //AnnounceDisplayType = NoticeDisplayType.say;
            }
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            RegisterEvents(GetType().Name,
                "OnPlayerKilled",
                "OnListPlayers",
                "OnSquadChat",
                "OnTeamChat",
                "OnGlobalChat",
                "OnPlayerJoin",
                "OnPlayerAuthenticated",
                "OnPlayerKickedByAdmin",
                "OnServerInfo",
                "OnPlayerTeamChange",
                "OnPlayerSpawned",
                "OnLevelLoaded",
                "OnRoundOver",
                "OnPlayerLeft"
                );
        }

        public void OnPluginEnable()
        {
            //System.Diagnostics.Debugger.Break();
            ConsoleLog("^b^2Enabled... It's Game Time!");
            ConsoleLog("--- Version " + GetPluginVersion() + " --- " + fGameVersion);
        }

        public void OnPluginDisable()
        {
            ConsoleLog("--- Version " + GetPluginVersion() + " ---");
            ConsoleLog("^b^2Disabled :(");
            Reset();
        }
        #endregion

        // Plugin details
        public string GetPluginName()
        {
            return "Zombie Mode";
        }

        public string GetPluginVersion()
        {
            return "1.1.4.0";
        }

        public string GetPluginAuthor()
        {
            return "PapaCharlie9, m4xxd3v";
        }

        public string GetPluginWebsite()
        {
            return "https://github.com/m4xxd3v/BF3ZombieMode";
        }

        public string GetPluginDescription()
        {
            return Description.HTML;
        }


        // Plugin variables
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Game Settings|Zombie Mode Enabled", typeof(enumBoolYesNo), ZombieModeEnabled ? enumBoolYesNo.Yes : enumBoolYesNo.No));

            lstReturn.Add(new CPluginVariable("Admin Settings|Command Prefix", CommandPrefix.GetType(), CommandPrefix));

            lstReturn.Add(new CPluginVariable("Admin Settings|Announce Display Length", AnnounceDisplayLength.GetType(), AnnounceDisplayLength));

            lstReturn.Add(new CPluginVariable("Admin Settings|Warning Display Length", WarningDisplayLength.GetType(), WarningDisplayLength));

            lstReturn.Add(new CPluginVariable("Admin Settings|Human Max Idle Seconds", HumanMaxIdleSeconds.GetType(), HumanMaxIdleSeconds));

            lstReturn.Add(new CPluginVariable("Admin Settings|Max Idle Seconds", MaxIdleSeconds.GetType(), MaxIdleSeconds));

            lstReturn.Add(new CPluginVariable("Admin Settings|Warns Before Kick For Rules Violations", WarnsBeforeKickForRulesViolations.GetType(), WarnsBeforeKickForRulesViolations));

            lstReturn.Add(new CPluginVariable("Admin Settings|Temp Ban Instead Of Kick", typeof(enumBoolOnOff), TempBanInsteadOfKick ? enumBoolOnOff.On : enumBoolOnOff.Off));

            if (TempBanInsteadOfKick)
            {
                lstReturn.Add(new CPluginVariable("Admin Settings|Temp Ban Seconds", TempBanSeconds.GetType(), TempBanSeconds));
            }

            lstReturn.Add(new CPluginVariable("Admin Settings|Votes Needed To Kick", VotesNeededToKick.GetType(), VotesNeededToKick));

            lstReturn.Add(new CPluginVariable("Admin Settings|Debug Level", DebugLevel.GetType(), DebugLevel));

            lstReturn.Add(new CPluginVariable("Admin Settings|Rule List", typeof(string[]), RuleList.ToArray()));

            lstReturn.Add(new CPluginVariable("Admin Settings|Admin Users", typeof(string[]), AdminUsers.ToArray()));

            lstReturn.Add(new CPluginVariable("Admin Settings|Test Weapon", TestWeapon.GetType(), TestWeapon));

            lstReturn.Add(new CPluginVariable("Game Settings|Max Players", MaxPlayers.GetType(), MaxPlayers));

            lstReturn.Add(new CPluginVariable("Game Settings|Minimum Zombies", MinimumZombies.GetType(), MinimumZombies));

            lstReturn.Add(new CPluginVariable("Game Settings|Minimum Humans", MinimumHumans.GetType(), MinimumHumans));

            lstReturn.Add(new CPluginVariable("Game Settings|Zombie Kill Limit Enabled", typeof(enumBoolOnOff), ZombieKillLimitEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));

            /* to be removed
			if (ZombieKillLimitEnabled)
				lstReturn.Add(new CPluginVariable("Game Settings|Zombies Killed To Survive", ZombiesKilledToSurvive.GetType(), ZombiesKilledToSurvive));
			*/

            lstReturn.Add(new CPluginVariable("Game Settings|Deaths Needed To Be Infected", DeathsNeededToBeInfected.GetType(), DeathsNeededToBeInfected));

            lstReturn.Add(new CPluginVariable("Game Settings|Infect Suicides", typeof(enumBoolOnOff), InfectSuicides ? enumBoolOnOff.On : enumBoolOnOff.Off));

            lstReturn.Add(new CPluginVariable("Game Settings|New Players Join Humans", typeof(enumBoolOnOff), NewPlayersJoinHumans ? enumBoolOnOff.On : enumBoolOnOff.Off));

            lstReturn.Add(new CPluginVariable("Game Settings|Rematch Enabled", typeof(enumBoolOnOff), RematchEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));

            if (RematchEnabled)
            {
                lstReturn.Add(new CPluginVariable("Game Settings|Matches Before Next Map", MatchesBeforeNextMap.GetType(), MatchesBeforeNextMap));
            }


            if (ZombieKillLimitEnabled)
            {

                lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 8 Or Less Players", KillsIf8OrLessPlayers.GetType(), KillsIf8OrLessPlayers));

                lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 12 To 9 Players", KillsIf12To9Players.GetType(), KillsIf12To9Players));

                lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 16 To 13 Players", KillsIf16To13Players.GetType(), KillsIf16To13Players));

                lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 20 To 17 Players", KillsIf20To17Players.GetType(), KillsIf20To17Players));

                lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 24 To 21 Players", KillsIf24To21Players.GetType(), KillsIf24To21Players));

                lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 28 To 25 Players", KillsIf28To25Players.GetType(), KillsIf28To25Players));

                lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 32 To 29 Players", KillsIf32To29Players.GetType(), KillsIf32To29Players));
            }

            lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against 1 Or 2 Zombies", Against1Or2Zombies.GetType(), Against1Or2Zombies));

            lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against A Few Zombies", AgainstAFewZombies.GetType(), AgainstAFewZombies));

            lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Equal Numbers", AgainstEqualNumbers.GetType(), AgainstEqualNumbers));

            lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Many Zombies", AgainstManyZombies.GetType(), AgainstManyZombies));

            lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Countless Zombies", AgainstCountlessZombies.GetType(), AgainstCountlessZombies));

            /*
			foreach (PRoCon.Core.Players.Items.Weapon Weapon in WeaponDictionaryByLocalizedName.Values)
			{
				String WeaponDamage = Weapon.Damage.ToString();

				if (WeaponDamage.Equals("Nonlethal") || WeaponDamage.Equals("None") || WeaponDamage.Equals("Suicide"))
					continue;

				String WeaponName = Weapon.Name.ToString();
				lstReturn.Add(new CPluginVariable(String.Concat("Zombie Weapons|Z -", WeaponName), typeof(enumBoolOnOff), ZombieWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
				lstReturn.Add(new CPluginVariable(String.Concat("Human Weapons|H -", WeaponName), typeof(enumBoolOnOff), HumanWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
			}
            */

            List<String> wlist = new List<String>();
            if (fGameVersion != GameVersion.BF3)
            { // GameVersion.BF4 or GameVersion.BFH
                wlist.AddRange(DefWeaponList);
            }
            else
            {
                wlist.AddRange(WeaponList);
            }
            foreach (String WeaponName in wlist)
            {
                lstReturn.Add(new CPluginVariable(String.Concat("Zombie Weapons|Z -", WeaponName), typeof(enumBoolOnOff), ZombieWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
                lstReturn.Add(new CPluginVariable(String.Concat("Human Weapons|H -", WeaponName), typeof(enumBoolOnOff), HumanWeaponsEnabled.Contains(WeaponName) ? enumBoolOnOff.On : enumBoolOnOff.Off));
            }


            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = GetDisplayPluginVariables();

            return lstReturn;
        }

        public void SetPluginVariable(string Name, string Value)
        {
            //ThreadStart MyThread = delegate
            //{
            try
            {
                int PipeIndex = Name.IndexOf('|');
                if (PipeIndex >= 0)
                {
                    PipeIndex++;
                    Name = Name.Substring(PipeIndex, Name.Length - PipeIndex);
                }

                BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

                String PropertyName = Name.Replace(" ", "");

                FieldInfo Field = GetType().GetField(PropertyName, Flags);

                Dictionary<int, Type> EasyTypeDict = new Dictionary<int, Type>();
                EasyTypeDict.Add(0, typeof(int));
                EasyTypeDict.Add(1, typeof(Int16));
                EasyTypeDict.Add(2, typeof(Int32));
                EasyTypeDict.Add(3, typeof(Int64));
                EasyTypeDict.Add(4, typeof(float));
                EasyTypeDict.Add(5, typeof(long));
                EasyTypeDict.Add(6, typeof(String));
                EasyTypeDict.Add(7, typeof(string));

                Dictionary<int, Type> BoolDict = new Dictionary<int, Type>();
                BoolDict.Add(0, typeof(Boolean));
                BoolDict.Add(1, typeof(bool));

                Dictionary<int, Type> ListStrDict = new Dictionary<int, Type>();
                ListStrDict.Add(0, typeof(List<String>));
                ListStrDict.Add(1, typeof(List<string>));



                if (Field != null)
                {

                    Type FieldType = Field.GetValue(this).GetType();
                    if (EasyTypeDict.ContainsValue(FieldType))
                        Field.SetValue(this, TypeDescriptor.GetConverter(FieldType).ConvertFromString(Value));
                    else if (ListStrDict.ContainsValue(FieldType))
                        Field.SetValue(this, new List<string>(CPluginVariable.DecodeStringArray(Value)));
                    else if (BoolDict.ContainsValue(FieldType))
                        if (Value == "Yes" || Value == "On")
                            Field.SetValue(this, true);
                        else
                            Field.SetValue(this, false);
                }
                else
                {
                    String WeaponName = Name.Substring(3, Name.Length - 3);

                    List<String> wlist = (fGameVersion == GameVersion.BF3) ? WeaponList : DefWeaponList;

                    if (wlist.IndexOf(WeaponName) >= 0)
                    {
                        String WeaponType = Name.Substring(0, 3);

                        if (WeaponType == "H -")
                        {
                            if (Value == "On")
                                EnableHumanWeapon(WeaponName);
                            else
                                DisableHumanWeapon(WeaponName);
                        }
                        else
                        {
                            if (Value == "On")
                                EnableZombieWeapon(WeaponName);
                            else
                                DisableZombieWeapon(WeaponName);
                        }

                    }
                }
            }
            catch (System.Exception e)
            {
                ConsoleException("MyThread: " + e.ToString());
            }
            finally
            {
                // Validate all values and correct if needed
                if (DebugLevel < 0)
                {
                    DebugValue("Debug Level", DebugLevel.ToString(), "must be greater than 0", "3");
                    DebugLevel = 3; // default
                }
                if (String.IsNullOrEmpty(CommandPrefix))
                {
                    DebugValue("Command Prefix", "(empty)", "must not be empty", "!zombie");
                    CommandPrefix = "!zombie"; // default
                }
                if (AnnounceDisplayLength < 5 || AnnounceDisplayLength > 20)
                {
                    DebugValue("Announce Display Length", AnnounceDisplayLength.ToString(), "must be between 5 and 20, inclusive", "10");
                    AnnounceDisplayLength = 10; // default
                }
                if (WarningDisplayLength < 5 || WarningDisplayLength > 20)
                {
                    DebugValue("Warning Display Length", WarningDisplayLength.ToString(), "must be between 5 and 20, inclusive", "15");
                    WarningDisplayLength = 15; // default
                }
                if (MaxPlayers < 8 || MaxPlayers > 32)
                {
                    DebugValue("Max Players", MaxPlayers.ToString(), "must be between 8 and 32, inclusive", "32");
                    MaxPlayers = 32; // default
                }
                if (MinimumHumans < 2 || MinimumHumans > (MaxPlayers - 1))
                {
                    DebugValue("Minimum Humans", MinimumHumans.ToString(), "must be between 3 and " + (MaxPlayers - 1), "2");
                    MinimumHumans = 3; // default
                }
                if (MinimumZombies < 1 || MinimumZombies > (MaxPlayers - MinimumHumans))
                {
                    DebugValue("Minimum Zombies", MinimumZombies.ToString(), "must be between 1 and " + (MaxPlayers - MinimumHumans), "1");
                    MinimumZombies = 1; // default
                }
                if (DeathsNeededToBeInfected < 1 || DeathsNeededToBeInfected > 10)
                {
                    DebugValue("Deaths Needed To Be Infected", DeathsNeededToBeInfected.ToString(), "must be between 1 and 10, inclusive", "1");
                    DeathsNeededToBeInfected = 1; // default
                }
                /* To be removed
                if (ZombiesKilledToSurvive < MaxPlayers)
                {
                    DebugValue("Zombies Killed To Survive", ZombiesKilledToSurvive.ToString(), "must be more than " + MaxPlayers, "50");
                    ZombiesKilledToSurvive = 50; // default
                }
                */
                if (HumanMaxIdleSeconds < 0)
                {
                    DebugValue("Human Max Idle Seconds", HumanMaxIdleSeconds.ToString(), "must not be negative", "120");
                    HumanMaxIdleSeconds = 120; // default
                }
                if (MaxIdleSeconds < 0)
                {
                    DebugValue("Max Idle Seconds", MaxIdleSeconds.ToString(), "must not be negative", "600");
                    MaxIdleSeconds = 600; // default
                }
                if (KillsIf8OrLessPlayers < 6)
                {
                    DebugValue("Kills If 8 Or Less Players", KillsIf8OrLessPlayers.ToString(), "must be 6 or more", "6");
                    KillsIf8OrLessPlayers = 6; // default
                }
            }
            //};

            //Thread t = new Thread(MyThread);

            //t.Start();

            if (!String.IsNullOrEmpty(TestWeapon))
            {
                try
                {
                    // Map to weapon name
                    String wn = TestWeapon;
                    List<String> found = new List<String>();
                    List<String> raw = new List<String>(); // raw weapon code names
                    if (fGameVersion != GameVersion.BF3)
                    { // GameVersion.BF4 or GameVersion.BFH
                        WeaponDictionary wd = GetWeaponDefines();
                        foreach (Weapon ww in wd)
                        {
                            if (ww != null && !String.IsNullOrEmpty(ww.Name))
                                raw.Add(ww.Name);
                        }
                    }
                    else
                    {
                        raw.AddRange(WeaponList);
                    }
                    foreach (String wraw in raw)
                    {
                        if (Regex.Match(wraw, wn, RegexOptions.IgnoreCase).Success)
                        {
                            found.Add(wraw);
                        }
                    }
                    if (found.Count == 1)
                    {
                        wn = found[0];
                        ConsoleLog("^1Testing weapon: ^b" + wn + "^n (" + TestWeapon + ")");
                        String h = "ON";
                        String z = "off";
                        if (!ValidateWeapon(wn, HUMAN_TEAM)) h = "off";
                        if (ValidateWeapon(wn, ZOMBIE_TEAM)) z = "ON";
                        ConsoleLog("^1^b" + wn + "^n: Humans(^b" + h + "^n), Zombies(^b" + z + "^n)");
                    }
                    else if (found.Count == 0)
                    {
                        ConsoleLog("^1Test Weapon (" + TestWeapon + ") not found, try again!");
                    }
                    else
                    {
                        ConsoleLog("^1Test Weapon ^b" + TestWeapon + "^n matches " + found.Count + " weapon names, which do you mean?");
                        ConsoleLog(String.Join(", ", found.ToArray()));
                    }
                }
                catch (Exception e)
                {
                }
                TestWeapon = String.Empty;
            }


        }
        #endregion



        /** PRIVATE METHODS **/

        #region RoundCommands

        private void RestartRound()
        {
            ExecuteCommand("procon.protected.send", "mapList.restartRound");
        }

        private void NextRound()
        {
            ExecuteCommand("procon.protected.send", "mapList.runNextRound");
        }

        private void CountdownNextRound(string WinningTeam)
        {

            SetState(GState.CountingDown);

            DebugWrite("CountdownNextRound started", 2);

            ThreadStart countdown = delegate
            {
                try
                {
                    if (RematchEnabled && MatchesCount < MatchesBeforeNextMap)
                    {
                        Sleep(AnnounceDisplayLength);
                        TellAll("New match will start in 5 seconds ... prepare to be moved!");
                        Sleep(5);

                        DebugWrite("CountdownNextRound ended with rematch mode enabled", 2);

                        MakeTeams(); // Sets GameState to NeedSpawn
                    }
                    else
                    {
                        MatchesCount = 0;

                        Sleep(AnnounceDisplayLength);
                        TellAll("Next round will start in 5 seconds");
                        Sleep(5);

                        DebugWrite("CountdownNextRound thread: end round with winner teamID = " + WinningTeam, 2);

                        ExecuteCommand("procon.protected.send", "mapList.endRound", WinningTeam);

                        SetState(GState.BetweenRounds);
                    }

                }
                catch (Exception e)
                {
                    ConsoleException("countdown: " + e.ToString());
                }
            };

            String Separator = " ";
            if (CommandPrefix.Length == 1) Separator = "";
            TellAll("Type '" + CommandPrefix + Separator + "rules' for instructions on how to play", false); // $$$ - custom message

            Thread t = new Thread(countdown);

            t.Start();

            Thread.Sleep(2);
        }

        #endregion


        #region PlayerPunishmentCommands

        private void Warn(String PlayerName, String Message)
        {
            ExecuteCommand("procon.protected.send", "admin.yell", Message, WarningDisplayLength.ToString(), PlayerName);
        }

        private void KillPlayerAfterDelay(string PlayerName, int Delay)
        {
            DebugWrite("KillPlayerAfterDelay: " + PlayerName + " after " + Delay + " seconds", 3);

            ThreadStart killerThread = delegate
            {
                try
                {
                    Sleep(Delay);
                    ExecuteCommand("procon.protected.send", "admin.killPlayer", PlayerName);
                }
                catch (Exception e)
                {
                    ConsoleException(e.ToString());
                }
            };

            Thread t = new Thread(killerThread);
            t.Start();
            Thread.Sleep(1);
        }

        private void KillPlayer(string PlayerName)
        {
            KillPlayerAfterDelay(PlayerName, 1);
        }

        private void KickPlayer(string PlayerName, string Reason)
        {
            ExecuteCommand("procon.protected.send", "admin.kickPlayer", PlayerName, Reason);
        }

        private void TempBanPlayer(string PlayerName, string Reason)
        {
            ExecuteCommand("procon.protected.send", "banList.add", "name", PlayerName, "seconds", TempBanSeconds.ToString(), Reason + " (Temporary/" + (TempBanSeconds / 60) + ")");

            ExecuteCommand("procon.protected.send", "banList.save");

            ExecuteCommand("procon.protected.send", "banList.list");

            ExecuteCommand("procon.protected.send", "banList.list 100");

            ExecuteCommand("procon.protected.send", "banList.list 200");

            KickPlayer(PlayerName, Reason);
        }

        private void ScheduleKick(string PlayerName, string Reason)
        {
            ThreadStart kickSchedule = delegate
            {
                try
                {
                    int maxTries = 0;
                    while (maxTries++ < 3)
                    {
                        if (!PlayerKickQueue.Contains(PlayerName))
                            break;

                        KickPlayer(PlayerName, Reason); // $$$ - custom message

                        DebugWrite("ScheduleKick: trying to kick " + PlayerName, 5);

                        Sleep(5); // Need time to get kick event
                    }
                }
                catch (System.Exception e)
                {
                    ConsoleException("kickSchedule: " + e.ToString());
                }
            };

            DebugWrite("^b^8ScheduleKick:^0^n " + PlayerName + " for: " + Reason, 1);

            Thread t = new Thread(kickSchedule);

            t.Start();

            Thread.Sleep(1);
        }

        private bool CheckIdle(List<CPlayerInfo> Players)
        {
            bool KickedSomeone = false;

            foreach (CPlayerInfo Player in Players)
            {
                String Name = Player.SoldierName;
                double MaxTime = MaxIdleSeconds;
                lock (TeamHuman)
                {
                    if (GetState() == GState.Playing && TeamHuman.Contains(Name))
                    {
                        MaxTime = HumanMaxIdleSeconds;
                    }
                }
                if (PlayerState.IdleTimeExceedsMax(Name, MaxTime, MaxIdleSeconds))
                {
                    DebugWrite("CheckIdle: " + Name + " ^8^bexceeded idle time of " + MaxTime + " seconds, KICKING ...^n^0", 2);
                    KickPlayer(Name, "Idle for more than " + MaxTime + " seconds");
                    KickedSomeone = true;
                }
            }

            return KickedSomeone;
        }

        #endregion

        #region TeamMethods

        private void ImmediatePlayersList()
        {
            lock (LastRequestPlayersList)
            {
                LastRequestPlayersList.TimeVal = DateTime.Now;
            }

            ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        private void RequestPlayersList()
        {
            lock (LastRequestPlayersList)
            {
                TimeSpan since = DateTime.Now - LastRequestPlayersList.TimeVal;

                // Don't request list more frequently than every 2 seconds

                if (since.TotalSeconds < 2) return;
            }

            ImmediatePlayersList();
        }

        public void Infect(string Carrier, string Victim)
        {
            TellAll(String.Concat(Carrier, " just infected ", Victim)); // $$$ - custom message

            MakeZombie(Victim);

            AdaptDamage();
        }

        private void MakeHuman(string PlayerName)
        {
            DebugWrite("MakeHuman: " + PlayerName, 3);

            ForceMove(PlayerName, HUMAN_TEAM);
        }

        private void MakeHumanFast(string PlayerName)
        {
            DebugWrite("MakeHumanFast: " + PlayerName, 3);

            /*
			Bug fix: Can't use ForceMove here because it makes a thread,
			after about 16 players are in the server, when a team switch
			happens between rounds, too many threads get created and
			the move of players is delayed until too late, confusing
			the rest of the team checking code. So we just do it in the
			main thread and only do the move, since we are between
			rounds anyway.
			*/

            ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, HUMAN_TEAM, BLANK_SQUAD, FORCE_MOVE);
        }

        private void ForceMove(string PlayerName, string TeamId, int DelaySecs)
        {
            ThreadStart forceMove = delegate
            {
                try
                {
                    // Delay for message?
                    if (DelaySecs != 0)
                    {
                        DebugWrite("ForceMove for " + PlayerName + ", delay " + DelaySecs, 5);
                        Sleep(DelaySecs);
                    }
                    else
                    {
                        Thread.Sleep(30);
                    }

                    // Kill player requires a delay to work correctly

                    ExecuteCommand("procon.protected.send", "admin.killPlayer", PlayerName);

                    Thread.Sleep(20);

                    // Now do the move

                    DebugWrite("ForceMove: Executing move of player " + PlayerName + " to " + TeamId + " now!", 5);

                    ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, TeamId, BLANK_SQUAD, FORCE_MOVE);

                    Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    ConsoleException("forceMove: " + e.ToString());
                }
            };

            Thread t = new Thread(forceMove);

            t.Start();

            Thread.Sleep(2);

            DebugWrite("ForceMove " + PlayerName + " to " + TeamId, 3);
        }

        private void ForceMove(string PlayerName, string TeamId)
        {
            ForceMove(PlayerName, TeamId, 0);
        }


        private void MakeZombie(string PlayerName)
        {
            DebugWrite("MakeZombie: " + PlayerName, 3);

            ForceMove(PlayerName, ZOMBIE_TEAM);

            FreshZombie.Add(PlayerName);
        }


        private void MakeTeams()
        {
            GState FinalState = GState.NeedSpawn;

            SetState(GState.Moving);

            ThreadStart makeTeams = delegate
            {
                try
                {
                    Sleep(5); // allow time to update player list

                    // Move all the zombies to the human team

                    List<String> AllPlayerCopy = new List<String>();
                    List<String> HumanCopy = new List<String>();

                    int TotalNum = 0;

                    lock (TeamHuman) // Only lock this object for both humans and zombies
                    {
                        AllPlayerCopy.AddRange(TeamHuman);
                        HumanCopy.AddRange(TeamHuman);
                        AllPlayerCopy.AddRange(TeamZombie);
                        TotalNum = TeamHuman.Count + TeamZombie.Count;
                    }

                    // Move them to human team
                    // We can't use MakeHuman here, because we are in a thread
                    // We can't use TeamZombie here, because we are modifying it

                    foreach (String Mover in AllPlayerCopy)
                    {

                        Thread.Sleep(30);

                        // Kill player requires a delay to work correctly

                        ExecuteCommand("procon.protected.send", "admin.killPlayer", Mover);

                        // Only kill humans, no need to move them
                        if (HumanCopy.Contains(Mover)) continue;

                        Thread.Sleep(20);

                        // Now do the move

                        DebugWrite("makeTeams: Executing move of player " + Mover + " to 1 now!", 5);

                        ExecuteCommand("procon.protected.send", "admin.movePlayer", Mover, HUMAN_TEAM, BLANK_SQUAD, FORCE_MOVE);

                    }

                    // Fill the lottery pool for selecting patient zero

                    Lottery.Clear();

                    List<CPlayerInfo> PlayersCopy = new List<CPlayerInfo>();
                    PlayersCopy.AddRange(PlayerList);

                    foreach (CPlayerInfo p in PlayersCopy)
                    {
                        String PName = p.SoldierName;
                        if (!PatientZeroes.Contains(PName))
                        {
                            Lottery.Add(PName);
                        }
                    }

                    // Sanity check

                    if (Lottery.Count < MinimumZombies)
                    {
                        DebugWrite("MakeTeams, can't find enough eligible players for patient zero!", 3);

                        Sleep(2); // Let TeamChange catch up

                        PatientZeroes.Clear();
                        Lottery.Clear();

                        lock (TeamHuman)
                        {
                            if (TeamHuman.Count > 0) for (int i = 0; i < TeamHuman.Count; ++i)
                                {
                                    Lottery.Add(TeamHuman[i]);
                                    if ((i + 1) >= MinimumZombies) break;
                                }
                        }

                        if (Lottery.Count < MinimumZombies)
                        {
                            DebugWrite("***** HALT: MakeTeams: not enough players to make " + MinimumZombies + " minimum zombies, patient zero lottery failed!", 1);
                            TellAll("Not enough players to make teams, match halted!");
                            Reset();
                            FinalState = GState.Idle;
                            RequestPlayersList();
                            return;
                        }
                    }

                    // Choose patient zero randomly from lottery pool

                    FreshZombie.Clear();

                    Random rand = new Random();

                    int ZombieCount = 0;

                    while (ZombieCount < MinimumZombies)
                    {
                        int choice = (Lottery.Count == 1) ? 0 : (rand.Next(Lottery.Count));
                        PatientZero = Lottery[choice];
                        Lottery.Remove(PatientZero);

                        Infect("Patient Zero", PatientZero);
                        ++ZombieCount;

                        if (PatientZeroes.Count > (KnownPlayerCount / 2)) PatientZeroes.Clear();

                        PatientZeroes.Add(PatientZero);

                        if (Lottery.Count == 0) break;
                    }


                    DebugWrite("MakeTeams: lottery selected " + PatientZero + " as first zombie!", 2);

                    LastMover = PatientZero;

                    // Reset state

                    Lottery.Clear();
                    ServerSwitchedCount = 0;

                    PlayerState.ResetPerMatch();
                    KillTracker.ResetPerMatch();

                    /* GameState is set to Playing in OnPlayerSpawned */

                    DebugWrite("MakeTeams: let TeamChange catch up", 5);

                    /* 
					Sleep to let TeamChange events catch up. Handshake
					with OnPlayerTeamChange by testing LastMover for null.
					Time out after 5 seconds (6.5 total).
					*/

                    Thread.Sleep(1500);

                    for (int i = 0; i < 10; ++i)
                    {
                        bool Handshake = false;

                        lock (TeamHuman)
                        {
                            Handshake = (LastMover == null);
                        }

                        if (Handshake)
                        {
                            DebugWrite("MakeTeams: handshake received", 5);
                            break;
                        }

                        Thread.Sleep(500);
                    }

                    DebugWrite("MakeTeams: ready for another round with " + TotalNum + " players!", 2);

                    TellAll("*** Spawn now, Zombie Mode is on! " + TotalNum + " players"); // $$$ - custom message

                }
                catch (Exception e)
                {
                    ConsoleException("MakeTeams: " + e.ToString());
                }
                finally
                {
                    SetState(FinalState);
                }
            };


            Thread t = new Thread(makeTeams);

            t.Start();

            Thread.Sleep(2);
        }

        private int GetKillsNeeded(int TotalCount)
        {
            int Needed = 0;

            if (TotalCount <= 8)
            {
                Needed = KillsIf8OrLessPlayers;
            }
            else if (TotalCount <= 12 && TotalCount >= 9)
            {
                Needed = KillsIf12To9Players;
            }
            else if (TotalCount <= 16 && TotalCount >= 13)
            {
                Needed = KillsIf16To13Players;
            }
            else if (TotalCount <= 20 && TotalCount >= 17)
            {
                Needed = KillsIf20To17Players;
            }
            else if (TotalCount <= 24 && TotalCount >= 21)
            {
                Needed = KillsIf24To21Players;
            }
            else if (TotalCount <= 28 && TotalCount >= 25)
            {
                Needed = KillsIf28To25Players;
            }
            else if (TotalCount <= 32 && TotalCount >= 29)
            {
                Needed = KillsIf32To29Players;
            }
            else
            {
                ConsoleError("GetKillsNeeded: bad TotalCount");
                return 0;
            }

            return Needed;
        }

        private void CheckVictoryConditions(bool ZombieWinOnKill)
        {
            // Victory conditions

            int Needed = 0;
            int TotalCount = 0;
            int HCount = 0;
            int ZCount = 0;
            string msg = null;
            string Winner = null;
            lock (TeamHuman)
            {
                HCount = TeamHuman.Count;
                ZCount = TeamZombie.Count;
                TotalCount = HCount + ZCount;
                if (HCount == 1) Winner = TeamHuman[0];
            }

            Needed = GetKillsNeeded(TotalCount);

            // All zombies left the server?
            if (ZCount == 0 && HCount > 0)
            {
                msg = "HUMANS WIN, no zombies left on the server!"; // $$$ - custom message
                DebugWrite("^4^b ***** " + msg + "^n^0", 1);
                TellAll(msg);
                CountdownNextRound(HUMAN_TEAM);
                return;
            }

            if (ZombieKillLimitEnabled == false && Winner != null && ZCount > MinimumZombies) // Last man standing?
            {
                msg = "WINNER: " + Winner + " is the last human survivor!"; // $$$ - custom message
                DebugWrite("^4^b ***** " + msg + "^n^0", 1);
                TellAll(msg);
                CountdownNextRound(HUMAN_TEAM);
            }
            else if (HCount > 0 && KillTracker.GetZombiesKilled() >= Needed) // Humans got enough kills?
            {
                msg = "HUMANS WIN with " + KillTracker.GetZombiesKilled() + " zombies killed!"; // $$$ - custom message
                DebugWrite("^4^b ***** " + msg + "^n^0", 1);
                TellAll(msg);
                CountdownNextRound(HUMAN_TEAM);
            }
            else
            {
                // All humans infected?
                if ((HCount == 0 || ZombieWinOnKill) && ZCount > MinimumZombies)
                {
                    msg = "ZOMBIES WIN, all humans infected!"; // $$$ - custom message
                    DebugWrite("^7^b ***** " + msg + "^n^0", 1);
                    TellAll(msg);
                    CountdownNextRound(ZOMBIE_TEAM);
                }
            }
        }

        private void VoteKick(string Voter, string Suspect)
        {
            bool AlreadyVoted = PlayerState.AddKickVote(Suspect, Voter);

            if (AlreadyVoted)
            {
                TellPlayer("You already voted to kick " + Suspect, Voter, false);
                return;
            }

            int Votes = PlayerState.GetKickVotes(Suspect);

            DebugWrite("VoteKick: " + Votes + " of " + VotesNeededToKick + " have been cast against " + Suspect, 2);

            if (Votes >= VotesNeededToKick)
            {
                TellPlayer("Your vote to kick " + Suspect + " was the last one needed. Kicking now!", Voter, false);
                TellAll(Suspect + " has been kicked by vote!");
                KickPlayer(Suspect, "Players voted to kick you!");
                PlayerState.ClearKickVotes(Suspect);
            }
            else
            {
                TellPlayer("Your vote was " + Votes + " of " + VotesNeededToKick + " needed to kick " + Suspect, Voter, false);
            }
        }

        private void VoteKill(string Voter, string Suspect)
        {
            bool AlreadyVoted = PlayerState.AddKillVote(Suspect, Voter);

            if (AlreadyVoted)
            {
                TellPlayer("You already voted to kill " + Suspect, Voter, false);
                return;
            }

            int Votes = PlayerState.GetKillVotes(Suspect);

            DebugWrite("VoteKill: " + Votes + " of " + VotesNeededToKick + " have been cast against " + Suspect, 2);

            if (Votes >= VotesNeededToKick)
            {
                TellPlayer("Your vote to kill " + Suspect + " was the last one needed. Kill in 5 seconds!", Voter, false);
                TellAll(Suspect + " has been killed by vote!");
                KillPlayerAfterDelay(Suspect, 3);
                PlayerState.ClearKillVotes(Suspect);
            }
            else
            {
                TellPlayer("Your vote was " + Votes + " of " + VotesNeededToKick + " needed to kill " + Suspect, Voter, false);
            }
        }

        private int CountAllTeams()
        {
            lock (TeamHuman)
            {
                return TeamHuman.Count + TeamZombie.Count;
            }
        }

        private void AddJoinQueue(String Name)
        {
            if (!JoinQueue.Contains(Name)) JoinQueue.Add(Name);
        }

        private void RemoveJoinQueue(String Name)
        {
            if (JoinQueue.Contains(Name)) JoinQueue.Remove(Name);
        }

        private void UpdateTeams(String Name, int TeamId)
        {
            lock (TeamHuman)
            {
                if (TeamId == 1)
                {
                    if (TeamZombie.Contains(Name)) TeamZombie.Remove(Name);
                    if (!TeamHuman.Contains(Name)) TeamHuman.Add(Name);
                }
                else if (TeamId == 2)
                {
                    if (TeamHuman.Contains(Name)) TeamHuman.Remove(Name);
                    if (!TeamZombie.Contains(Name)) TeamZombie.Add(Name);
                }
            }

            RequestPlayersList();
        }

        #endregion

        #region WeaponMethods

        private void DisableZombieWeapon(String WeaponName)
        {
            int Index = ZombieWeaponsEnabled.IndexOf(WeaponName);
            if (Index >= 0)
                ZombieWeaponsEnabled.RemoveAt(Index);
        }

        private void DisableHumanWeapon(String WeaponName)
        {
            int Index = HumanWeaponsEnabled.IndexOf(WeaponName);
            if (Index >= 0)
                HumanWeaponsEnabled.RemoveAt(Index);
            if (!HumanWeaponsEnabled.Contains(WeaponName)) DebugWrite("^9DisableHumanWeapon(" + WeaponName + ")", 4);
        }

        private void EnableZombieWeapon(String WeaponName)
        {
            int Index = ZombieWeaponsEnabled.IndexOf(WeaponName);
            if (Index < 0)
                ZombieWeaponsEnabled.Add(WeaponName);
        }

        private void EnableHumanWeapon(String WeaponName)
        {
            int Index = HumanWeaponsEnabled.IndexOf(WeaponName);
            if (Index < 0)
                HumanWeaponsEnabled.Add(WeaponName);
            if (HumanWeaponsEnabled.Contains(WeaponName)) DebugWrite("^9EnableHumanWeapon(" + WeaponName + ")", 4);
        }

        private bool ValidateWeapon(string Weapon, string TEAM_CONST)
        {
            if (Regex.Match(Weapon, @"(?:Suicide|Death|SoldierCollision|RoadKill|DamageArea)").Success)
                return true;

            String wn = Weapon;
            if (fGameVersion != GameVersion.BF3)
            { // GameVersion.BF4 or GameVersion.BFH
                wn = FriendlyWeaponName(Weapon);
            }

            if (
                (TEAM_CONST == HUMAN_TEAM && HumanWeaponsEnabled.IndexOf(wn) >= 0) ||
                (TEAM_CONST == ZOMBIE_TEAM && ZombieWeaponsEnabled.IndexOf(wn) >= 0)
                )
                return true;

            return false;
        }

        private void AdaptDamage()
        {
            double HumanCount = 1;
            double ZombieCount = 1;
            lock (TeamHuman)
            {
                HumanCount = (TeamHuman.Count == 0) ? 1 : TeamHuman.Count;
                ZombieCount = (TeamZombie.Count == 0) ? 1 : TeamZombie.Count;
            }
            double RatioHumansToZombies = (HumanCount / ZombieCount);
            int NewBulletDamage = 5;
            int OldBulletDamage = BulletDamage;


            if (RatioHumansToZombies >= 3.0)
            {
                NewBulletDamage = Against1Or2Zombies;
            }
            else if (RatioHumansToZombies < 3.0 && RatioHumansToZombies >= 1.5)
            {
                NewBulletDamage = AgainstAFewZombies;
            }
            else if (RatioHumansToZombies < 1.5 && RatioHumansToZombies >= 0.4)
            {
                NewBulletDamage = AgainstEqualNumbers;
            }
            else if (RatioHumansToZombies < 0.4 && RatioHumansToZombies > 0.20)
            {
                NewBulletDamage = AgainstManyZombies;
            }
            else // <= 0.20
            {
                NewBulletDamage = AgainstCountlessZombies;
            }

            // Cap damage for small numbers of players
            if (NewBulletDamage > AgainstManyZombies && HumanCount == 1 && ZombieCount <= 6)
            {
                NewBulletDamage = AgainstManyZombies;
            }


            if (NewBulletDamage != BulletDamage)
            {
                BulletDamage = NewBulletDamage;

                ExecuteCommand("procon.protected.send", "vars.bulletDamage", BulletDamage.ToString());

                TellAll("Bullet damage is now " + BulletDamage + "%", false);
            }

            if (BulletDamage != OldBulletDamage) DebugWrite("AdaptDamage: Humans(" + HumanCount + "):Zombies(" + ZombieCount + "), bullet damage set to " + BulletDamage + "% (was " + OldBulletDamage + "%)", 3);


        }

        #endregion


        #region Utilities

        private bool IsAdmin(string PlayerName)
        {
            bool AdminFlag = AdminUsers.Contains(PlayerName);
            if (AdminFlag)
            {
                TellAll(PlayerName + " is an admin", false);
                DebugWrite("IsAdmin: " + PlayerName + " is an admin", 3);
            }
            return AdminFlag;
        }

        private void ConsoleWrite(string str)
        {
            ExecuteCommand("procon.protected.pluginconsole.write", str);
        }

        private void LogChat(string Message, string Who)
        {
            ExecuteCommand("procon.protected.chat.write", "ZMODE to " + Who + "> " + Message);
        }

        private void LogChat(string Message)
        {
            ExecuteCommand("procon.protected.chat.write", "ZMODE> " + Message);
        }

        private void Announce(string Message)
        {
            if (GetState() == GState.BetweenRounds) return;
            ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), "all");
            LogChat(Message);
        }

        private void TellAll(string Message, bool AlsoYell)
        {
            // Yell and say
            if (GetState() == GState.BetweenRounds) return;
            if (AlsoYell) Announce(Message);
            ExecuteCommand("procon.protected.send", "admin.say", Message, "all");
            if (!AlsoYell) LogChat(Message);
        }

        private void TellAll(string Message)
        {
            TellAll(Message, true);
        }

        private void TellTeam(string Message, string TeamId, bool AlsoYell)
        {
            // Yell and say
            if (GetState() == GState.BetweenRounds) return;
            if (AlsoYell) ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), "team", TeamId);
            ExecuteCommand("procon.protected.send", "admin.say", Message, "team", TeamId);
            LogChat(Message, (TeamId == HUMAN_TEAM) ? "humans" : "zombies");
        }

        private void TellTeam(string Message, string TeamId)
        {
            TellTeam(Message, TeamId, true);
        }

        private void TellPlayer(string Message, string SoldierName, bool AlsoYell)
        {
            // Yell and say
            if (GetState() == GState.BetweenRounds) return;
            if (AlsoYell) ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), "player", SoldierName);
            ExecuteCommand("procon.protected.send", "admin.say", Message, "player", SoldierName);
            LogChat(Message, SoldierName);
        }

        private void TellPlayer(string Message, string SoldierName)
        {
            TellPlayer(Message, SoldierName, true);
        }

        private void TellRules(string SoldierName)
        {
            int Delay = 4;
            List<String> Rules = new List<String>();
            Rules.AddRange(RuleList);
            // $$$ - custom message
            if (ZombieKillLimitEnabled)
            {
                int TotalCount = CountAllTeams();
                Rules.Add("Humans win by killing " + GetKillsNeeded(TotalCount) + " zombies");
            }

            String RuleNum = null;
            int i = 1;

            ThreadStart tellRules = delegate
            {
                try
                {
                    foreach (String r in Rules)
                    {
                        RuleNum = "R" + i + " of " + Rules.Count + ") ";
                        i = i + 1;
                        TellPlayer(RuleNum + r, SoldierName);
                        Sleep(Delay);
                    }
                }
                catch (Exception e)
                {
                    ConsoleException("tellRules: " + e.ToString());
                }
                finally
                {
                    lock (NumRulesThreads)
                    {
                        NumRulesThreads.IntVal = NumRulesThreads.IntVal - 1;
                        if (NumRulesThreads.IntVal < 0) NumRulesThreads.IntVal = 0;
                    }
                }
            };

            bool IsTooMany = false;

            lock (NumRulesThreads)
            {
                if (NumRulesThreads.IntVal >= 4)
                {
                    IsTooMany = true;
                }
                else
                {
                    NumRulesThreads.IntVal = NumRulesThreads.IntVal + 1;
                }
            }

            if (IsTooMany)
            {
                TellPlayer("Rules plugin is busy, try again in 15 seconds", SoldierName);
                return;
            }

            Thread t = new Thread(tellRules);

            t.Start();

            Thread.Sleep(2);
        }

        private void TellStatus(string SoldierName)
        {
            String status = "Zombie mode is disabled!";
            bool IsPlaying = false;


            if (ZombieModeEnabled) switch (GetState())
                {
                    case GState.Idle:
                        status = "No one is playing zombie mode (Idle)!";
                        break;
                    case GState.Waiting:
                        status = "Waiting for " + (MinimumHumans + MinimumZombies - CountAllTeams()) + " more players to spawn (Waiting)!";
                        break;
                    case GState.Playing:
                        status = "A match is in progress (Playing)!";
                        IsPlaying = true;
                        break;
                    case GState.CountingDown:
                        status = "Counting down to next match/round (CountingDown)!";
                        break;
                    case GState.BetweenRounds:
                        status = "ERROR (BetweenRounds)!"; // should never happen
                        break;
                    case GState.NeedSpawn:
                        status = "ERROR (NeedSpawn)!"; // should never happen
                        break;
                    default:
                        status = "Unknown";
                        break;
                }

            TellPlayer("Status: " + status, SoldierName);

            if (IsPlaying)
            {
                int HCount = 0;
                int ZCount = 0;
                lock (TeamHuman)
                {
                    HCount = TeamHuman.Count;
                    ZCount = TeamZombie.Count;
                }
                TellPlayer("HUMANS: N=" + HCount + ",K=" + KillTracker.GetZombiesKilled() + ",G=" + GetKillsNeeded(HCount + ZCount), SoldierName, false);
                TellPlayer("ZOMBIES: N=" + ZCount + ",D=" + BulletDamage, SoldierName, false);

                int KillVotes = PlayerState.GetKillVotes(SoldierName);
                if (KillVotes > 0) TellPlayer("You have " + KillVotes + " of " + VotesNeededToKick + " kill votes against you!", SoldierName, false); // $$$ - custom message

                int KickVotes = PlayerState.GetKickVotes(SoldierName);
                if (KickVotes > 0) TellPlayer("You have " + KickVotes + " of " + VotesNeededToKick + " KICK votes against you!", SoldierName, false); // $$$ - custom message
            }
        }

        private void Reset()
        {
            PlayerList.Clear();
            lock (TeamHuman)
            {
                TeamHuman.Clear();
                TeamZombie.Clear();
            }
            lock (PlayerKickQueue)
            {
                PlayerKickQueue.Clear();
            }
            JoinQueue.Clear();
            FreshZombie.Clear();
            PatientZeroes.Clear();
            Lottery.Clear();
            PlayerState.ClearAll();
            KnownPlayerCount = 0;
            ServerSwitchedCount = 0;
            PatientZero = null;
            SetState(GState.Idle);
            MatchesCount = 0;
            LastMover = null;

            BulletDamage = 100;
            ExecuteCommand("procon.protected.send", "vars.bulletDamage", BulletDamage.ToString());
        }

        private void HaltMatch()
        {
            FreshZombie.Clear();
            Lottery.Clear();
            PlayerState.ResetPerMatch();
            PatientZero = null;
            SetState(GState.Waiting);
            LastMover = null;

            BulletDamage = 100;
            ExecuteCommand("procon.protected.send", "vars.bulletDamage", BulletDamage.ToString());
        }

        private enum MessageType { Warning, Error, Exception, Normal };

        private String FormatMessage(String msg, MessageType type)
        {
            String prefix = "[^b" + GetPluginName() + "^n] ";

            if (type.Equals(MessageType.Warning))
                prefix += "^1^bWARNING^0^n: ";
            else if (type.Equals(MessageType.Error))
                prefix += "^1^bERROR^0^n: ";
            else if (type.Equals(MessageType.Exception))
                prefix += "^1^bEXCEPTION^0^n: ";

            return prefix + msg;
        }


        private void ConsoleLog(string msg, MessageType type)
        {
            ConsoleWrite(FormatMessage(msg, type));
        }

        private void ConsoleLog(string msg)
        {
            ConsoleLog(msg, MessageType.Normal);
        }

        private void ConsoleWarn(String msg)
        {
            ConsoleLog(msg, MessageType.Warning);
        }

        private void ConsoleError(String msg)
        {
            ConsoleLog(msg, MessageType.Error);
        }

        private void ConsoleException(String msg)
        {
            ConsoleLog(msg, MessageType.Exception);
        }

        private void DebugWrite(string msg, int level)
        {
            if (DebugLevel >= level) ConsoleLog("[" + level + "] " + msg, MessageType.Normal);
        }

        private void DebugValue(string Name, string BadValue, string Message, string NewValue)
        {
            DebugWrite("^b^8SetPluginVariable: ^0" + Name + "^n set to invalid value = " + BadValue + ", " + Message + ". Value forced to = " + NewValue, 0);
        }

        private void Sleep(int Seconds)
        {
            Thread.Sleep(Seconds * 1000);
        }

        private String PlayerNameMatch(string Name)
        {
            if (String.IsNullOrEmpty(Name) || PlayerList.Count == 0) return null;

            foreach (CPlayerInfo Player in PlayerList)
            {
                try
                {
                    if (Regex.Match(Player.SoldierName, Name, RegexOptions.IgnoreCase).Success)
                    {
                        return Player.SoldierName;
                    }
                }
                catch (Exception e)
                {
                    return null;
                }
            }

            // Otherwise
            return null;
        }

        private void SetState(GState state)
        {
            lock (StateLock)
            {
                GameState = state;
            }
        }

        private GState GetState()
        {
            lock (StateLock)
            {
                return GameState;
            }
        }

        public String FriendlyWeaponName(String killWeapon)
        {
            String _name = killWeapon;

            if (_name.Contains("Vehicle") || _name.Contains("havana")) // Filter our unwanted BFHL weapons
                return "Melee";
            if (_name == "U_CS_Gas")
                return "CS Gas";

            if (killWeapon.StartsWith("U_"))
            {
                String[] tParts = killWeapon.Split(new[] { '_' });

                if (tParts.Length == 2)
                { // U_Name
                    _name = tParts[1];
                    // BFHL
                    if (_name.Contains("Knife"))
                        _name = "Knife";
                }
                else if (tParts.Length == 3)
                { // U_Name_Detail
                    _name = tParts[1];
                    if (_name == "SP" || _name == "sp") // BFHL, U_SP_RPG7 and U_sp_smaw
                        _name = "SP " + tParts[2];
                }
                else if (tParts.Length >= 4)
                { // U_AttachedTo_Name_Detail
                    _name = tParts[2];
                }
                else
                {
                    DebugWrite("Warning: unrecognized weapon code: " + killWeapon, 5);
                }
            }
            return _name;
        }


        #endregion

    }

    enum ZombieModeTeam { Human, Zombie };

    class ZombieModeKillTrackerKills
    {
        public int KillsAsZombie = 0;

        public int KillsAsHuman = 0;

        public int DeathsAsZombie = 0;

        public int DeathsAsHuman = 0;

        public int RulesViolations = 0; // never reset this value
    }

    class ZombieModeKillTracker
    {
        protected Dictionary<String, ZombieModeKillTrackerKills> Kills = new Dictionary<String, ZombieModeKillTrackerKills>();

        protected int ZombiesKilled = 0;

        protected int HumansKilled = 0;

        public void HumanKilled(String KillerName, String VictimName)
        {
            ZombieModeKillTrackerKills Killer = Kills[KillerName];
            Killer.KillsAsZombie++;

            ZombieModeKillTrackerKills Victim = Kills[VictimName];
            Victim.DeathsAsHuman++;

            HumansKilled++;
        }

        public void ZombieKilled(String KillerName, String VictimName)
        {
            ZombieModeKillTrackerKills Killer = Kills[KillerName];
            Killer.KillsAsHuman++;

            ZombieModeKillTrackerKills Victim = Kills[VictimName];
            Victim.DeathsAsZombie++;

            ZombiesKilled++;
        }

        protected Boolean PlayerExists(String PlayerName)
        {
            return Kills.ContainsKey(PlayerName);
        }

        public void AddPlayer(String PlayerName)
        {
            if (!PlayerExists(PlayerName))
                Kills.Add(PlayerName, new ZombieModeKillTrackerKills());
        }

        public void RemovePlayer(String PlayerName)
        {
            if (!PlayerExists(PlayerName))
                return;

            Kills.Remove(PlayerName);
        }

        public int GetZombiesKilled()
        {
            return ZombiesKilled;
        }

        public int GetHumansKilled()
        {
            return HumansKilled;
        }

        public int GetPlayerHumanDeathCount(String PlayerName)
        {
            return Kills[PlayerName].DeathsAsHuman;
        }

        public int GetViolations(String PlayerName)
        {
            return Kills[PlayerName].RulesViolations;
        }

        public void SetViolations(String PlayerName, int Times)
        {
            Kills[PlayerName].RulesViolations = Times;
        }

        public void ResetPerMatch()
        {
            HumansKilled = 0;
            ZombiesKilled = 0;

            foreach (String key in Kills.Keys)
            {
                ZombieModeKillTrackerKills Tracker = Kills[key];
                Tracker.KillsAsZombie = 0;
                Tracker.KillsAsHuman = 0;
                Tracker.DeathsAsZombie = 0;
                Tracker.DeathsAsHuman = 0;
            }
        }

        public void ResetPerRound()
        {
            ResetPerMatch();
        }

    }

    class APlayerState
    {
        // A bunch of counters and flags

        public int WelcomeCount = 0;

        public int SpawnCount = 0;

        public DateTime LastSpawnTime = DateTime.Now;

        public bool IsSpawned = false;

        public List<String> VotesToKick = new List<String>();

        public List<String> VotesToKill = new List<String>();

        public void PartialReset()
        {
            WelcomeCount = 0;
            SpawnCount = 0;
            LastSpawnTime = DateTime.Now;
            IsSpawned = false;
        }
    }

    class ZombieModePlayerState
    {
        protected Dictionary<String, APlayerState> AllPlayerStates = new Dictionary<String, APlayerState>();

        public void AddPlayer(String soldierName)
        {
            if (AllPlayerStates.ContainsKey(soldierName))
            {
                AllPlayerStates[soldierName].PartialReset();
                return;
            }
            AllPlayerStates[soldierName] = new APlayerState();
        }

        public int GetWelcomeCount(String soldierName)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
            return AllPlayerStates[soldierName].WelcomeCount;
        }

        public void SetWelcomeCount(String soldierName, int n)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
            AllPlayerStates[soldierName].WelcomeCount = n;
        }

        public int GetSpawnCount(String soldierName)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
            return AllPlayerStates[soldierName].SpawnCount;
        }

        public void SetSpawnCount(String soldierName, int n)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
            AllPlayerStates[soldierName].SpawnCount = n;
        }

        public void UpdateSpawnTime(String soldierName)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
            AllPlayerStates[soldierName].LastSpawnTime = DateTime.Now;
        }

        public bool IdleTimeExceedsMax(String soldierName, double maxSecs, double highMax)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) return false;
            APlayerState ps = AllPlayerStates[soldierName];
            if (maxSecs != highMax && ps.IsSpawned == true) return false;
            // Fix for idle kicks before someone spawns the first time!
            if (maxSecs != highMax && ps.SpawnCount == 0) return false;
            DateTime last = ps.LastSpawnTime;
            TimeSpan time = DateTime.Now - last;
            return (time.TotalSeconds > maxSecs);
        }


        public void SetSpawned(String soldierName, bool SpawnStatus)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
            AllPlayerStates[soldierName].IsSpawned = SpawnStatus;
        }

        public bool GetSpawned(String soldierName)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
            return AllPlayerStates[soldierName].IsSpawned;
        }

        public double GetLastSpawnTime(String soldierName)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) return 0;
            APlayerState ps = AllPlayerStates[soldierName];
            DateTime last = ps.LastSpawnTime;
            TimeSpan time = DateTime.Now - last;
            return (time.TotalSeconds);
        }

        public int GetKickVotes(String soldierName)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) return 0;
            return AllPlayerStates[soldierName].VotesToKick.Count;
        }

        public bool AddKickVote(String soldierName, String voter)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
            if (AllPlayerStates[soldierName].VotesToKick.Contains(voter)) return true;
            AllPlayerStates[soldierName].VotesToKick.Add(voter);
            return false;
        }

        public void ClearKickVotes(String soldierName)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) return;
            AllPlayerStates[soldierName].VotesToKick.Clear();
        }

        public int GetKillVotes(String soldierName)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) return 0;
            return AllPlayerStates[soldierName].VotesToKill.Count;
        }

        public bool AddKillVote(String soldierName, String voter)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
            if (AllPlayerStates[soldierName].VotesToKill.Contains(voter)) return true;
            AllPlayerStates[soldierName].VotesToKill.Add(voter);
            return false;
        }

        public void ClearKillVotes(String soldierName)
        {
            if (!AllPlayerStates.ContainsKey(soldierName)) return;
            AllPlayerStates[soldierName].VotesToKill.Clear();
        }

        public void ResetPerMatch()
        {
            foreach (String key in AllPlayerStates.Keys)
            {
                SetSpawnCount(key, 0);
            }
        }

        public void ResetPerRound()
        {
            ResetPerMatch();

            foreach (String key in AllPlayerStates.Keys)
            {
                ClearKickVotes(key);
            }
        }

        public void ClearAll()
        {
            AllPlayerStates.Clear();
        }
    }

    class SynchronizedNumbers
    {
        public int IntVal = 0;
        public DateTime TimeVal = DateTime.Now;
    }

    /* Always at the end of the file */

    class DescriptionClass
    {
        public String HTML = @"
<h2>Description</h2>

<p>Zombie Mode is a ProCon 1.0 plugin that turns Team Deathmatch into the <i>Infected</i> or <i>Zombie</i> variant play.</p>

<p><font color='#FF0000'><b>NOTE:</b> the game server <b>must be run in unranked mode</b> (BF3: vars.ranked false, BF4 or BFHL: vars.serverType Unranked). Zombie Mode will not work on a ranked server.</font></p>

<p>When there are a minimum number of players spawned, all of the players are moved to the human team (US), except for one zombie (RU). With default settings, Zombies can use knife/defib/repair tool <i>only</i> for weapons and Humans can use any weapon <i>except</i> explosives (grenades, C4, Claymores) or missiles; the allowed/forbidden weapon settings are configurable. Zombies are hard to kill. Every time a zombie kills a human, the human becomes infected and is moved to the zombie team. Humans win by killing a minimum number of zombies (configurable) or when all the zombies leave the server. Zombies win by infecting all the humans or when all the humans leave the server.</p>

<p>The maximum number of players is half your server slots, so if you have a 64-slot server, you can have a max of 32 players.</p>

<p>The plugin is driven by players spawning. Until a minimum number of individual players spawns, the match won't start. See <b>Minimum Zombies</b> and <b>Minimum Humans</b> below.</p>

<p>Recommended BF3 server settings are here: <a href=https://github.com/m4xxd3v/BF3ZombieMode/wiki/Recommended-server-settings>https://github.com/m4xxd3v/BF3ZombieMode/wiki/Recommended-server-settings</a></p>

<h2>Settings</h2>
<p>There are a large number of configurable setttings, divided into sections.</p>

<h3>Admin Settings</h3>
<p><b>Zombie Mode Enabled</b>: <i>On/Off</i>, default is <i>On</i>.</p>

<p><b>Command Prefix</b>: Chat text that represents an in-game command, default is <i>!zombie</i>. May be set to a single character, for example <i>@</i>, so that instead of the rules command being <i>!zombie rules</i>, the command would just be <i>@rules</i>.</p>

<p><b>Announce Display Length</b>: Time in seconds that announcements are shown as yells, default is <i>10</i>.</p>

<p><b>Warning Display Length</b>: Time in seconds that warnings are shown as yells, default is <i>15</i>.</p>

<p><b>Human Max Idle Seconds</b>: Time in seconds that a human is allowed to be idle (no spawns and no kills/deaths) before being kicked. This idle time applies only when a match is in progress. Since zombies can't win unless they can kill humans, the match can stall if a human remains idle and never spawns. The idle time for humans should therefore be relatively short. The default value is <i>180</i> seconds, or 3 minutes.</p>

<p><b>Max Idle Seconds</b>: Time in seconds that any player is allowed to be idle (no spawns and no kills/deaths) before being kicked, regardless of whether a match is running or not, or whether spawned or not. This idle time applies as long as Zombie Mode is enabled (On). The default value is <i>600</i> seconds, or 10 minutes.</p>

<p><b>Warns Before Kick For Rules Violations</b>: Number of warnings given before a player is kicked for violating the Zombie Mode rules, particularly for using a forbidden weapon type. The default value is <i>1</i>.</p>

<p><b>Temp Ban Instead Of Kick</b>: <i>On/Off</i>, default is <i>Off</i>. If <i>On</i>, a rules violation results in a temporary ban for <b>Temp Ban Seconds</b>. If <i>Off</i>, a rules violation results in a kick. In both cases, the punishment happens after <b>Warns Before Kick For Rules Violations</b> warnings have been issued to the violator.</p>

<p><b>Temp Ban Seconds</b>: Time in seconds that a player is temporarily banned if <b>Temp Ban Instead Of Kick</b> is <i>On</i>. The default value is <i>3600</i> seconds, or 1 hour.</p>

<p><b>Votes Needed To Kick</b>: Number of votes needed to kick a player with the <b>!zombie votekick</b> command or kill a player with the <b>!zombie votekill</b> command. The default value is <i>3</i>.</p>

<p><b>Debug Level</b>: A number that represents the amount of debug logging  that is sent to the plugin.log file in PRoCon. The higher the number, the more spam is logged. The default value is <i>2</i>. Note: if you have a problem using the plugin, set your <b>Debug Level</b> to <i>5</i> and save the plugin.log for posting to phogue.net.</p>

<p><b>Rule List</b>: A table of rules, one chat/yell line per rule, displayed when players type the <b>!zombie rules</b> in-game command. The default set of rules reflect the default settings, such as humans not using explosives. Useful for when you change the default weapon limitations for humans and zombies, you can tell players what weapons are allowed or forbidden. Also useful if you want to add more rules, like kicking players for using MAV.</p>

<p><b>Admin Users</b>: A table of soldier names that will be permitted to use in-game admin commands (see below). The default value is <i>PapaCharlieNiner</i>.</p>

<p><b>Test Weapon</b>: For debugging the plugin only, type in the name of a weapon and test if Humans or Zombies are allowed to use (ON) or not use (off) that weapon.</p>

<h3>Game Settings</h3>

<p><b>Max Players</b>: Any players that try to join above this number will be kicked immediately. Make sure this number is equal to or less than <b>half</b> of your maximum slot count for your game server. For example, if you have a 48 slot server, set the maximum no higher than 24. This is a limitation of BF3 game servers, you can only use half your slots for this mode. The default value is <i>32</i>.</p>

<p><b>Minimum Zombies</b>: The number of players that will start a match as zombies. The default value is <i>1</i>.</p>

<p><b>Minimum Humans</b>: The number of players that will start a match as humans. The default value is <i>3</i>. Note: the sum of <b>Minimum Zombies</b> and <b>Minimum Humans</b> (default: 4) is the minimum number of players needed to start a match. Until that minimum number spawns into the round, the Zombie Mode will wait and normal Team Deathmatch rules will apply.</p>

<p><b>Zombie Kill Limit Enabled</b>: <i>On/Off</i>, default is <i>On</i>. If <i>On</i>, Humans must kill the number of zombies specified in <b>Goal For Humans</b> in order to win. If <i>Off</i>, the last human left standing is the winner.</p>

<p><b>Deaths Needed To Be Infected</b>: The number of times a human must be killed by a zombie before the human becomes infected and is forced to switch to the zombie team. The default value is <i>1</i>.</p>

<p><b>Infect Suicides</b>: <i>On/Off</i>, default is <i>Off</i>. If <i>On</i>, a human that suicides becomes a zombie. If <i>Off</i>, the human stays human but still dies. Neither setting changes suicides for zombies, they are always non-scoring.</p>

<p><b>New Players Join Humans</b>: <i>On/Off</i>, default is <i>On</i>. If <i>On</i>, any new players that join the server will be force moved to the human team. If <i>Off</i>, any new players that join the server will be force moved to the zombie team.</p>

<p><b>Rematch Enabled</b>: <i>On/Off</i>, default is <i>On</i>.  If <i>On</i>, when a team wins and the match is over, a new match will be started after a short countdown during the same map round/level. <b>Matches Before Next Map</b> will be played before the next map is loaded. When <i>Off</i>, the current map round/level will be ended, the winning team will be declared the winner of the whole round and the next map round/level will be loaded and started. Turning this <i>On</i> makes matches happen quicker and back-to-back on the same map, while turning this <i>Off</i> takes longer between matches, but lets your players try out all the maps in your rotation.</p>

<p><b>Matches Before Next Map</b>: The default value is <i>3</i>. If <b>Rematch Enabled</b> is <i>On</i>, this is the number of matches that are played in the same map round/level before the next map is loaded. This assumes the map list is set up to only play eacy map level 1 round.</p>

<h3>Goal For Humans</h3>

<p>If <b>Zombie Kill Limit Enabled</b> is <i>On</i>, humans musts kill the specified number of zombies in order to win. The kill goal is adaptive to the number of players in the match, specified in intervals of four, as follows:</p>

<p><b>Kills If 8 Or Less Players</b>: the default value is <i>12</i>.</p>

<p><b>Kills If 12 To 9 Players</b>: the default value is <i>18</i>.</p>

<p><b>Kills If 16 To 13 Players</b>: the default value is <i>24</i>.</p>

<p><b>Kills If 20 To 17 Players</b>: the default value is <i>30</i>.</p>

<p><b>Kills If 24 To 21 Players</b>: the default value is <i>35</i>.</p>

<p><b>Kills If 28 To 25 Players</b>: the default value is <i>40</i>.</p>

<p><b>Kills If 32 To 29 Players</b>: the default value is <i>50</i>.</p>

<h3>Human Damage Percentage</h3>

<p>At the start of a match, when there is only one or a very few zombies, zombies have to be very tough and hard to kill or else they will never get close to a human to infect them. This is implemented with vars.bulletDamage. The values of the following settings specify the vars.bulletDamage depending on the number of zombies that the humans face. The lower the numbers, the harder the zombies are to kill.</p>

<p><b>Against 1 Or 2 Zombies</b>: the default value is <i>5</i>. When humans outnumber zombies 3-to-1 or more (e.g., 18 vs 1).</p>

<p><b>Against A Few Zombies</b>: the default value is <i>15</i>. When humans outnumber zombies between 3-to-1 and 3-to-2 (e.g., 12 vs 7).</p>

<p><b>Against Equal Numbers</b>: the default value is <i>30</i>. When humans and zombies are roughly equal in number, betwee 3-to-2 and 2-to-3 (e.g., 8 vs 11).</p>

<p><b>Against Many Zombies</b>: the default value is <i>50</i>. When zombies outnumber humans between 3-to-2 and 4-to-1 (e.g., 5 vs 14).</p>

<p><b>Against Countless Zombies</b>: the default value is <i>100</i>. When zombies outnumber humans 4-to-1 or more (e.g., 2 vs 17).</p>

<h3>Zombie Weapons</h3>

<p>This is a lists of weapon types zombies are allowed to use. Weapons that are <i>On</i> are allowed, weapons that are <i>Off</i> are not allowed and will result in warnings and a kick if a zombie player uses them. The default settings allow knife, melee, defib and repair tool and do not allow anything else.</p>

<h3>Human Weapons</h3>

<p>This is a lists of weapon types humans are allowed to use. Weapons that are <i>On</i> are allowed, weapons that are <i>Off</i> are not allowed and will result in warnings and a kick if a human player uses them. The default settings are all guns allowed and do not allow explosives (grenades, C4, claymore, M320 noob tube, etc.) or missiles (RPG, SMAW).</p>

<h2>Commands</h2>

<p>These are in-game commands for managing players and the mode. Some are available to all players, some are for admins only (see <b>Admin Users</b> in <b>Settings</b>). For all of the following descriptions, the default <b>Command Prefix</b> of <i>!zombie</i> is assumed. If you set a different prefix, substitute your prefix into the following.</p>

<h3>Commands for all players</h3>

<p><b>!zombie help</b>: Shows list of commands available to the player.</p>

<p><b>!zombie idle</b>: Shows how long the player typing the command has been idle (no spawns and no deaths/kills) and whether or not the player is spawned into the round.</p>

<p><b>!zombie rules</b>: Scrolls all of the Zombie Mode rules to the player.</p>

<p><b>!zombie status</b>: Shows the status of the match to the player, for example, if the mode is waiting for more players to join, or if it is Idle (waiting for a player to spawn so that it can reset), counting down to the next match, etc. If a match is in progress (Playing), it also shows some statistics for the match, for example:<pre>
HUMANS: N=4,K=23,G=30
ZOMBIES: N=16,D=100</pre><br/>
Where <b>N</b> is the number of players on that team, <b>K</b> is the number of zombies the humans have killed, <b>G</b> is the number of zombies the humans need to kill to win, and <b>D</b> is the current bullet damage. If there are votekicks or votekills against you, the current vote counts will also be shown.</p>

<p><b>!zombie warn</b> <i>name</i> <i>reason</i>: Sends a warning yell to the player with the specified <i>name</i>. The <i>reason</i> is one or more words. For example:
<pre>!zombie warn PapaCharlie9 Quit glitching u noob!</pre><br/>
will yell the message 'Quit glitching u noob!' to PapaCharlie9.</p>

<p><b>!zombie votekick</b> <i>name</i>: Adds a vote to kick the player with the specified <i>name</i>. Only one vote is counted per voter. Once <b>Votes Needed To Kick</b> votes have been reached, the player is kicked. Votes are cleared after the player is kicked.</p>

<p><b>!zombie votekill</b> <i>name</i>: Adds a vote to kill the player with the specified <i>name</i>. Only one vote is counted per voter. Once <b>Votes Needed To Kick</b> votes have been reached, the player is killed. The kill does not count for scoring or infection. Votes are cleared after the player is killed. This is useful when humans camp in a spot unreachable by zombies without using an illegal weapon. The zombies can vote to kill the human, which forces him to spawn in a random location.</p>

<h3>Commands for Admins only</h3>

<p><b>!zombie force</b>: Force a match to start, even if there are not enough players. Useful if players aren't spawning fast enough to get a match started or if the plugin gets into a confused state (please report a bug so we can fix it).</p>

<p><b>!zombie heal</b> <i>name</i>: Kills the player with the specified <i>name</i> and if they are on the zombie team, force moves them to the human team. Useful for correting mistakes that the plugin might make (please report a bug so we can fix it).</p>

<p><b>!zombie infect</b> <i>name</i>: Kills the player with the specified <i>name</i> and if they are on the human team, force moves them to the zombie team. Useful for dealing with human glitchers or idlers.</p>

<p><b>!zombie kick</b> <i>name</i> <i>reason</i>: Kicks the player with the specified <i>name</i>. The <i>reason</i> is one or more words. For example:
<pre>!zombie kick PapaCharlie9 Too much glitching!</pre><br/>
will kick PapaCharlie9 for 'Too much glitching!'. Useful to get rid of cheaters.</p>

<p><b>!zombie kill</b> <i>name</i>: Kills the player with the specified <i>name</i>. Useful to force a glitcher to respawn or a player ignoring warnings to pay more attention.</p>

<p><b>!zombie mode</b> <i>on</i>/<i>off</i>: Changes the <b>Zombie Mode Enabled</b> setting. Useful if you want to switch a normal TDM round to Zombie Mode or vice versa.</p>

<p><b>!zombie next</b>: Ends the current map round/level and loads the next map round/level. Useful to try a new map if you have <b>Rematch Enabled</b> set to <i>On</i>.</p>

<p><b>!zombie rematch</b> <i>on</i>/<i>off</i>: Changes the <b>Rematch Enabled</b> setting</p>

<p><b>!zombie restart</b>: Restarts the current map round/level. Useful if the tickets/kills for TDM are getting close to the maximum to end a normal TDM round, which might happen in the middle of a quick rematch.</p>

<h3>Changelog</h3>
<blockquote><h4>1.1.4.0 (19-APR-2015)</h4>
	- V1.1 Patch 4: Added support for BFHL, mostly different weapon list.<br/>
</blockquote>
<blockquote><h4>1.1.3.0 (07-JAN-2014)</h4>
	- V1.1 Patch 3: Added support for BF4, mostly different weapon list.<br/>
</blockquote>
<blockquote><h4>1.1.2.0 (31-OCT-2012)</h4>
	- V1.1 Patch 2: Fixed problem with making teams between rounds.<br/>
</blockquote>
<blockquote><h4>1.1.0.0 (28-OCT-2012)</h4>
	- V1.1 Update: Added <b>Matches Before Next Map</b> setting, more improvements to MakeTeams.<br/>
</blockquote>
<blockquote><h4>1.0.1.0 (27-OCT-2012)</h4>
	- V1.0 Patch 1: Improved timing between MakeTeams and TeamChange event<br/>
</blockquote>
<blockquote><h4>1.0.0.0 (26-OCT-2012)</h4>
	- initial version<br/>
</blockquote>
";

    }
}


