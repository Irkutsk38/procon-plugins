// Version 1.5.1 //
// Feel free to use this code in the manner you see fit, but give back to the community if you make something cool!
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Plugin;

namespace PRoConEvents
{
    public class CTeamKillKicker : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();   //Dictionary for maintaining players
        private Dictionary<string, int> m_dicPlayersTeamKills = new Dictionary<string, int>();          //Teamkills per player
        private string m_TKWarnMessage;                         //Warning Message - Message sent to player warning them of the coming kick if they keep team killing.
        private string m_TKKickMessagePrivate;                  //Kick message (Private) – Message that is displayed in the “You have been kicked” dialog. (Kick reason).
        private string m_TKKickMessagePublic;                   //Kick message (Public) – Message that is displayed to all players in global chat (only used if “Display kicks in global chat” is on).
        private string m_MirrorTKMessage;                       //Warning message before killing the TKer.
        private int m_iDisplayTime;                             //Yell Time – Time to display the yelled warning in milliseconds.
        private int m_iTKKickValue;                             //Kick at (TKs) – Number of team kills before player is kicked.
        private int m_iTKWarnValue;                             //Warn at (TKs) - Number of kills before player is warned. Player will be warned for every team kill between warn and kick values.
        private int m_iBanTime;                                 //Time to ban players for if tbanning is enabled in seconds.
        private int m_iMTKkillValue;                            //Number of TKs before killing kicks in.
        private enumBoolYesNo m_enYellWarn;                     //Yell Warnings – When this is enabled warnings will be yelled to the player that did the team killing.
        private enumBoolYesNo m_enChatKicks;                    //Display kicks in global chat – Option to enable display of kicks in global chat for all players.
        private enumBoolYesNo m_enConsoleDisplay;               //Log in Console – Display a log of all team kills and actions in the plugin console.
        private enumBoolYesNo m_enTBan;                         //Temp ban team killers rather than kicking them.
        private enumBoolYesNo m_enMirrorTKs;                    //Kill the player who performed the TK.

        public CTeamKillKicker()
        {
            this.m_iDisplayTime = 12000;    //12 seconds
            this.m_enYellWarn = enumBoolYesNo.Yes;
            this.m_enChatKicks = enumBoolYesNo.No;
            this.m_enConsoleDisplay = enumBoolYesNo.No;
            this.m_enMirrorTKs = enumBoolYesNo.No;
            this.m_iTKKickValue = 5;
            this.m_iTKWarnValue = 2;
            this.m_iMTKkillValue = 3;
            this.m_TKKickMessagePrivate = "You were kicked for excess teamkilling!";
            this.m_TKKickMessagePublic = "%pk% is being kicked for teamkilling!";
            this.m_TKWarnMessage = "WARNING: Stop teamkilling!";
            this.m_MirrorTKMessage = "You have been killed for Teamkilling.";
            this.m_enTBan = enumBoolYesNo.No;
            this.m_iBanTime = 1800;      //1800 seconds (30mins)
        }

        public string GetPluginName()
        {
            return "Team Kill Kicker";
        }

        public string GetPluginVersion()
        {
            return "1.5.1";
        }

        public string GetPluginAuthor()
        {
            return "QuarterEvil";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net/forum/viewtopic.php?f=18&t=528";
        }

        public string GetPluginDescription()
        {
            return @"This plugin monitors the server for team killing. When a set number of team kills is reached by any player, they are warned to stop team killing. After a second set number of team kills that player is removed from the server.

The team kill count is reset for each level.

Options
=======================================
Yell Warnings – When this is enabled warnings will be yelled to the player that did the team killing.
Yell Time – Time to display the yelled warning.
Display kicks in global chat – Option to enable display of kicks in global chat for all players.
Log in Console – Display a log of all team kills and actions in the plugin console.
Kick at (TKs) – Number of team kills before player is kicked.
Kick message (Private) – Message that is displayed in the “You have been kicked” dialog. (Kick reason).
Kick message (Public) – Message that is displayed to all players in global chat (only used if “Display kicks in global chat” is on).
Warn at (TKs) - Number of kills before player is warned. Player will be warned for every team kill between warn and kick values.
Warning Message - Message sent to player warning them of the coming kick if they keep team killing.
Ban Team Killers - Temp ban team killers rather than kicking them.
Ban Time (Minutes) - Time to ban players for if tbanning is enabled in minutes.
Enable Mirror TK - Kill the team killer as punishment.
Kill at (TKs) - Number of TKs before killing starts if Mirror TKs is enabled.
Mirror TK Warning Message - Message that the team killer will recieve before being killed.
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bTK Kicker Plugin ^2Enabled!");
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bTK Kicker Plugin ^1Disabled =(");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Communication|Yell Warnings", typeof(enumBoolYesNo), this.m_enYellWarn));
            if (this.m_enYellWarn == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Communication|Yell Time (seconds)", this.m_iDisplayTime.GetType(), this.m_iDisplayTime / 1000));
            }
            lstReturn.Add(new CPluginVariable("Communication|Display kicks in global chat", typeof(enumBoolYesNo), this.m_enChatKicks));
            lstReturn.Add(new CPluginVariable("TKActions|Warning at (TKs)", typeof(int), this.m_iTKWarnValue));
            lstReturn.Add(new CPluginVariable("TKActions|Warning Message", typeof(string), this.m_TKWarnMessage));
            lstReturn.Add(new CPluginVariable("TKActions|Kick at (TKs)", typeof(int), this.m_iTKKickValue));
            lstReturn.Add(new CPluginVariable("TKActions|Kick message (Private)", typeof(string), this.m_TKKickMessagePrivate));
            if (this.m_enChatKicks == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("TKActions|Kick message (Public)", typeof(string), this.m_TKKickMessagePublic));
            }
            lstReturn.Add(new CPluginVariable("TKActions|Ban Team Killers", typeof(enumBoolYesNo), this.m_enTBan));
            if (this.m_enTBan == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("TKActions|Ban Time (Minutes)", typeof(int), this.m_iBanTime / 60));
            }
            lstReturn.Add(new CPluginVariable("Console|Log in Console", typeof(enumBoolYesNo), this.m_enConsoleDisplay));
            lstReturn.Add(new CPluginVariable("MirrorTKs|Enable Mirror TK", typeof(enumBoolYesNo), this.m_enMirrorTKs));
            if (this.m_enMirrorTKs == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("MirrorTKs|Mirror TK Warning Message", typeof(string), this.m_MirrorTKMessage));
                lstReturn.Add(new CPluginVariable("MirrorTKs|Kill at (TKs)", typeof(int), this.m_iMTKkillValue));
            }

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Warning at (TKs)", typeof(int), this.m_iTKWarnValue));
            lstReturn.Add(new CPluginVariable("Warning Message", typeof(string), this.m_TKWarnMessage));
            lstReturn.Add(new CPluginVariable("Kick at (TKs)", typeof(int), this.m_iTKKickValue));
            lstReturn.Add(new CPluginVariable("Kick message (Private)", typeof(string), this.m_TKKickMessagePrivate));
            lstReturn.Add(new CPluginVariable("Kick message (Public)", typeof(string), this.m_TKKickMessagePublic));
            lstReturn.Add(new CPluginVariable("Log in Console", typeof(enumBoolYesNo), this.m_enConsoleDisplay));
            lstReturn.Add(new CPluginVariable("Display kicks in global chat", typeof(enumBoolYesNo), this.m_enChatKicks));
            lstReturn.Add(new CPluginVariable("Yell Time (seconds)", this.m_iDisplayTime.GetType(), this.m_iDisplayTime / 1000));
            lstReturn.Add(new CPluginVariable("Yell Warnings", typeof(enumBoolYesNo), this.m_enYellWarn));
            lstReturn.Add(new CPluginVariable("Ban Time (Minutes)", typeof(int), this.m_iBanTime / 60));
            lstReturn.Add(new CPluginVariable("Ban Team Killers", typeof(enumBoolYesNo), this.m_enTBan));
            lstReturn.Add(new CPluginVariable("Enable Mirror TK", typeof(enumBoolYesNo), this.m_enMirrorTKs));
            lstReturn.Add(new CPluginVariable("Mirror TK Warning Message", typeof(string), this.m_MirrorTKMessage));
            lstReturn.Add(new CPluginVariable("Kill at (TKs)", typeof(int), this.m_iMTKkillValue));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int TKKickValue = 5;
            int TKKillValue = 3;
            int TKWarnValue = 2;
            int iTimeSeconds = 12;
            int iTimeMinutes = 30;
            int iTimeSeconds2 = 8;

            if (strVariable.CompareTo("Yell Warnings") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enYellWarn = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Log in Console") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enConsoleDisplay = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Display kicks in global chat") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enChatKicks = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Ban Team Killers") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enTBan = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Enable Mirror TK") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enMirrorTKs = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Yell Time (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iDisplayTime = iTimeSeconds * 1000;

                if (iTimeSeconds <= 0)
                {
                    this.m_iDisplayTime = 1000;
                }
                else if (iTimeSeconds > 60)
                {
                    this.m_iDisplayTime = 59999;
                }
            }
            else if (strVariable.CompareTo("Ban Time (Minutes)") == 0 && int.TryParse(strValue, out iTimeMinutes) == true)
            {
                this.m_iBanTime = iTimeMinutes * 60;

                if (iTimeMinutes <= 0)
                {
                    this.m_iBanTime = 300;
                }
                else if (iTimeMinutes > 1440)
                {
                    this.m_iBanTime = 86400;
                }
            }
            else if (strVariable.CompareTo("Warning Message") == 0)
            {
                this.m_TKWarnMessage = strValue;
            }
            else if (strVariable.CompareTo("Kick message (Private)") == 0)
            {
                this.m_TKKickMessagePrivate = strValue;
            }
            else if (strVariable.CompareTo("Kick message (Public)") == 0)
            {
                this.m_TKKickMessagePublic = strValue;
            }
            else if (strVariable.CompareTo("Mirror TK Warning Message") == 0)
            {
                this.m_MirrorTKMessage = strValue;
            }
            else if (strVariable.CompareTo("Warning at (TKs)") == 0 && int.TryParse(strValue, out TKWarnValue) == true)
            {
                this.m_iTKWarnValue = TKWarnValue;
            }
            else if (strVariable.CompareTo("Kick at (TKs)") == 0 && int.TryParse(strValue, out TKKickValue) == true)
            {
                this.m_iTKKickValue = TKKickValue;
            }
            else if (strVariable.CompareTo("Kill at (TKs)") == 0 && int.TryParse(strValue, out TKKillValue) == true)
            {
                this.m_iMTKkillValue = TKKillValue;
            }
        }

        // Account created
        public void OnAccountCreated(string strUsername)
        {

        }

        public void OnAccountDeleted(string strUsername)
        {

        }

        public void OnAccountPrivilegesUpdate(string strUsername, CPrivileges cpPrivs)
        {

        }

        public void OnReceiveProconVariable(string strVariableName, string strValue)
        {

        }

        // Connection
        public void OnConnectionClosed()
        {

        }

        // Player events
        public void OnPlayerJoin(string strSoldierName)
        {

        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {
            //Remove the player and team kills to tidy up.
            if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayers.Remove(strSoldierName);
            }

            if (this.m_dicPlayersTeamKills.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayersTeamKills.Remove(strSoldierName);
            }
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {
            bool bWarned = false;
            // If they didn't kill themselves.
            if (String.Compare(strKillerSoldierName, strVictimSoldierName) != 0)
            {
                // And killed someone on the same team.
                if (this.GetPlayerTeamID(strKillerSoldierName) == this.GetPlayerTeamID(strVictimSoldierName))
                {
                    // Check if they already have a team kill item, if so add 1 team kill to it.
                    if (this.m_dicPlayersTeamKills.ContainsKey(strKillerSoldierName) == true)
                    {
                        this.m_dicPlayersTeamKills[strKillerSoldierName] += 1;
                    }
                    else
                    {
                        // Add a new item for the team killer... 
                        this.m_dicPlayersTeamKills.Add(strKillerSoldierName, 1);
                    }

                    // Log entry in plugin console if logging is enabled.
                    if (this.m_enConsoleDisplay == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has %tk% teamkills.").Replace("%pn%", strKillerSoldierName).Replace("%tk%", this.m_dicPlayersTeamKills[strKillerSoldierName].ToString()));
                    }

                    // Check if Mirror TK is enabled.
                    if (this.m_enMirrorTKs == enumBoolYesNo.Yes)
                    {
                        // Check if TK value is greater than the defined variable.
                        if (this.m_dicPlayersTeamKills[strKillerSoldierName] >= this.m_iMTKkillValue)
                        {
                            // Warn the player with a yell message for X seconds.
                            this.ExecuteCommand("procon.protected.tasks.add", "CTeamKillKicker", "0", "1", "1", "procon.protected.send", "admin.yell", this.m_MirrorTKMessage, "8000", "player", strKillerSoldierName);
                            bWarned = true;

                            // Kill the player that performed the TK.
                            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", strKillerSoldierName);

                            // Log entry in plugin console if logging is enabled.
                            if (this.m_enConsoleDisplay == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been killed as punishment for a TK.").Replace("%pn%", strKillerSoldierName));
                            }
                        }
                    }

                    // Check if player has more TKs then the required kick value, if not check TKs against the warning value.
                    if (this.m_dicPlayersTeamKills[strKillerSoldierName] >= this.m_iTKKickValue)
                    {
                        // Check if global chatting is enabled, if so add a message into global chat.
                        if (this.m_enChatKicks == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.tasks.add", "CTeamKillKicker", "0", "1", "1", "procon.protected.send", "admin.say", this.m_TKKickMessagePublic.Replace("%pk%", strKillerSoldierName), "all");
                        }

                        // If banning is enabled, ban the team killer.
                        if (this.m_enTBan == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.send", "banList.add", "name", strKillerSoldierName, "seconds", (this.m_iBanTime).ToString(), this.m_TKKickMessagePrivate);

                            // Check if console logging is enabled, if so add a new entry explaining the tban.
                            if (this.m_enConsoleDisplay == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been banned for team killing!").Replace("%pn%", strKillerSoldierName));
                            }
                        }
                        else
                        {
                            // Kick the rat bastard!
                            this.ExecuteCommand("procon.protected.tasks.add", "CTeamKillKicker", "0", "1", "1", "procon.protected.send", "admin.kickPlayer", strKillerSoldierName, this.m_TKKickMessagePrivate);

                            // Check if console logging is enabled, if so add a new entry explaining the kick.
                            if (this.m_enConsoleDisplay == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been kicked for team killing!").Replace("%pn%", strKillerSoldierName));
                            }
                        }


                    }
                    else if (this.m_dicPlayersTeamKills[strKillerSoldierName] >= this.m_iTKWarnValue)
                    {
                        // If the player has not already been warned by the Mirror TK module,
                        if (bWarned == false)
                        {
                            // If yell warnings is enabled yell warning otherwise send a private chat to player with warning message.
                            if (this.m_enYellWarn == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "CTeamKillKicker", "0", "1", "1", "procon.protected.send", "admin.yell", this.m_TKWarnMessage, this.m_iDisplayTime.ToString(), "player", strKillerSoldierName);
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "CTeamKillKicker", "0", "1", "1", "procon.protected.send", "admin.say", this.m_TKWarnMessage, "player", strKillerSoldierName);
                            }

                            // Check if console logging is enabled, if so add a new entry for the warning.
                            if (this.m_enConsoleDisplay == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been warned for team killing.").Replace("%pn%", strKillerSoldierName));
                            }
                        }
                    }
                }
            }
        }

        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {

        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {

        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {

        }

        public void OnLoadingLevel(string strMapFileName)
        {
            // Clear all the recorded team kills, start fresh every level.
            this.m_dicPlayersTeamKills.Clear();
        }

        public void OnLevelStarted()
        {

        }

        public void OnPunkbusterMessage(string strPunkbusterMessage)
        {

        }

        public void OnPunkbusterBanInfo(CBanInfo cbiPunkbusterBan)
        {

        }

        public void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {

        }

        // Global or misc..
        public void OnResponseError(List<string> lstRequestWords, string strError)
        {

        }

        // Login events
        public void OnLogin()
        {

        }

        public void OnLogout()
        {

        }

        public void OnQuit()
        {

        }

        public void OnVersion(string strServerType, string strVersion)
        {

        }

        public void OnHelp(List<string> lstCommands)
        {

        }

        public void OnRunScript(string strScriptFileName)
        {

        }

        public void OnRunScriptError(string strScriptFileName, int iLineError, string strErrorDescription)
        {

        }

        // Query Events
        public void OnServerInfo(CServerInfo csiServerInfo)
        {

        }

        // Communication Events
        public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset)
        {

        }

        public void OnSaying(string strMessage, CPlayerSubset cpsSubset)
        {

        }

        // Level Events
        public void OnRunNextLevel()
        {

        }

        public void OnCurrentLevel(string strCurrentLevel)
        {

        }

        public void OnSetNextLevel(string strNextLevel)
        {

        }

        public void OnRestartLevel()
        {

        }

        // Does not work in R3, never called for now.
        public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps)
        {

        }

        public void OnPlaylistSet(string strPlaylist)
        {

        }

        public void OnListPlaylists(List<string> lstPlaylists)
        {

        }

        // Player Kick/List Events
        public void OnPlayerKicked(string strSoldierName, string strReason)
        {

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            // Keep the player dictionary up to date.
            foreach (CPlayerInfo cpiPlayer in lstPlayers)
            {
                if (this.m_dicPlayers.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                    this.m_dicPlayers[cpiPlayer.SoldierName] = cpiPlayer;
                }
                else
                {
                    this.m_dicPlayers.Add(cpiPlayer.SoldierName, cpiPlayer);
                }
            }
        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {
            // Keep the player dictionary up to date when players change teams, this ensures teamid is knowen even before a listplayers is done.
            if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayers[strSoldierName].TeamID = iTeamID;
            }
        }

        //function to get a players team ID from the players dictionary. Returns the teamID.
        private int GetPlayerTeamID(string strSoldierName)
        {
            int iTeamID = 0; // Neutral Team ID

            if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
            {
                iTeamID = this.m_dicPlayers[strSoldierName].TeamID;
            }

            return iTeamID;
        }

        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {

        }

        // Banning and Banlist Events
        public void OnBanList(List<CBanInfo> lstBans)
        {

        }

        public void OnBanAdded(CBanInfo cbiBan)
        {

        }

        public void OnBanRemoved(CBanInfo cbiUnban)
        {

        }

        public void OnBanListClear()
        {

        }

        public void OnBanListLoad()
        {

        }

        public void OnBanListSave()
        {

        }

        // Reserved Slots Events
        public void OnReservedSlotsConfigFile(string strConfigFilename)
        {

        }

        public void OnReservedSlotsLoad()
        {

        }

        public void OnReservedSlotsSave()
        {

        }

        public void OnReservedSlotsPlayerAdded(string strSoldierName)
        {

        }

        public void OnReservedSlotsPlayerRemoved(string strSoldierName)
        {

        }

        public void OnReservedSlotsCleared()
        {

        }

        public void OnReservedSlotsList(List<string> lstSoldierNames)
        {

        }

        // Maplist Events
        public void OnMaplistConfigFile(string strConfigFilename)
        {

        }

        public void OnMaplistLoad()
        {

        }

        public void OnMaplistSave()
        {

        }

        public void OnMaplistMapAppended(string strMapFileName)
        {

        }

        public void OnMaplistMapRemoved(int iMapIndex)
        {

        }

        public void OnMaplistCleared()
        {

        }

        public void OnMaplistList(List<string> lstMapFileNames)
        {

        }

        public void OnMaplistNextLevelIndex(int iMapIndex)
        {

        }

        public void OnMaplistMapInserted(int iMapIndex, string strMapFileName)
        {

        }

        // Vars
        public void OnGamePassword(string strGamePassword)
        {

        }

        public void OnPunkbuster(bool blEnabled)
        {

        }

        public void OnHardcore(bool blEnabled)
        {

        }

        public void OnRanked(bool blEnabled)
        {

        }

        public void OnRankLimit(int iRankLimit)
        {

        }

        public void OnTeamBalance(bool blEnabled)
        {

        }

        public void OnFriendlyFire(bool blEnabled)
        {

        }

        public void OnMaxPlayerLimit(int iMaxPlayerLimit)
        {

        }

        public void OnCurrentPlayerLimit(int iCurrentPlayerLimit)
        {

        }

        public void OnPlayerLimit(int iPlayerLimit)
        {

        }

        public void OnBannerURL(string strURL)
        {

        }

        public void OnServerDescription(string strServerDescription)
        {

        }

        public void OnKillCam(bool blEnabled)
        {

        }

        public void OnMiniMap(bool blEnabled)
        {

        }

        public void OnCrossHair(bool blEnabled)
        {

        }

        public void On3dSpotting(bool blEnabled)
        {

        }

        public void OnMiniMapSpotting(bool blEnabled)
        {

        }

        public void OnThirdPersonVehicleCameras(bool blEnabled)
        {

        }

    }
}