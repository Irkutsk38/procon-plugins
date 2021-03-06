/*  Copyright 2010 [LAB]HeliMagnet (Gerry Wohlrab)

    http://www.luckyatbingo.net

    This file is part of [LAB]HeliMagnet's Plugins for BFBC2 PRoCon.

    [LAB]HeliMagnet's Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    [LAB]HeliMagnet's Plugins for BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with [LAB]HeliMagnet's Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Timers;
using System.Threading;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CAceGoldSquad : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private List<CPlayerInfo> playerStats;
        private bool yellInfo;
        private double displayLeaders;
        private System.Timers.Timer displayTimer = new System.Timers.Timer();
        private CMap m_currentMap;
        private List<string> m_squadNames = new List<string>();
        private bool endOfRound;

        public CAceGoldSquad()
        {

            this.yellInfo = false;
            this.displayLeaders = 2.5;
            this.displayTimer.Interval = this.displayLeaders * 1000 * 60;           // 2.5 minutes = 150000 milliseconds
            this.displayTimer.Elapsed += new ElapsedEventHandler(displayInformation);
            this.endOfRound = false;
        }

        public string GetPluginName()
        {
            return "Ace Player and Gold Squad Tracker";
        }

        public string GetPluginVersion()
        {
            return "1.0.1.0";
        }

        public string GetPluginAuthor()
        {
            return "HeliMagnet";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            return @"
<p>For support or to post comments regarding this plugin please visit <a href=""http://phogue.net/forum/viewtopic.php?f=13&t=806"" target=""_blank"">Plugin Thread</a></p>

<p>This plugin works with PRoCon and falls under the GNU GPL, please read the included gpl.txt for details.
I have decided to work on PRoCon plugins without compensation. However, if you would like to donate to support the development of PRoCon, click the link below:
<a href=""http://phogue.net"" target=""_new""><img src=""http://i398.photobucket.com/albums/pp65/scoop27585/phoguenetdonate.png"" width=""482"" height=""84"" border=""0"" alt=""PRoCon Donation"" /></a></p>
<p>Toward the right side of the page, there is a location to enter the amount you would like to donate and whether you want the donation to be made public. Your donations are greatly appreciated and will be sent to Phogue (original creator of PRoCon).</p>
<h2>Description</h2>
<p>Displays Ace Player and Gold Squad status updates throughout the match. Summarizes at end of match as well.</p>

<h2>Commands</h2>
<p>There are no commands needed for this plugin</p>

<h2>Settings</h2>
    <h3>Miscellaneous</h3>
        <blockquote><h4>Duration Inbetween Display (minutes)</h4>Set to a number greater than 0. The information about current ace player and gold squad will display every X specified minutes.</blockquote>
        <blockquote><h4>Yell Information</h4>Setting to true will yell to all players (center flashing text) or setting to false will display in chat.</blockquote>
<h2>Updates / Change Log</h2>
<h3>Version 1.0.0.0 --> 1.0.1.0</h3>
		<h4><ul><li>Fixed end of round display</li>
		<li>Fixed tie for ace pin or gold squad display</li>
		<li>Fixed ace pin display when player enters</li></ul></h4>
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
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAce Player and Gold Squad Tracker ^2Enabled!");
            this.displayTimer.Enabled = true;
            this.m_squadNames.Clear();
            this.m_squadNames.Add("None");
            this.m_squadNames.Add("Alpha");
            this.m_squadNames.Add("Bravo");
            this.m_squadNames.Add("Charlie");
            this.m_squadNames.Add("Delta");
            this.m_squadNames.Add("Echo");
            this.m_squadNames.Add("Foxtrot");
            this.m_squadNames.Add("Golf");
            this.m_squadNames.Add("Hotel");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAce Player and Gold Squad Tracker ^1Disabled =(");
            this.displayTimer.Enabled = false;
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Duration Inbetween Display (minutes)", typeof(double), this.displayLeaders));
            lstReturn.Add(new CPluginVariable("Yell Information", "enum.TrueFalseAceGold(True|False)", this.yellInfo.ToString()));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {
            bool enable = true;
            double dur = 2.5;
            if (strVariable.CompareTo("Yell Information") == 0 && bool.TryParse(strValue, out enable) == true)
            {
                this.yellInfo = bool.Parse(strValue);
            }
            else if (strVariable.CompareTo("Duration Inbetween Display (minutes)") == 0 && Double.TryParse(strValue, out dur) == true)
            {
                if (dur > 0)
                {
                    this.displayLeaders = double.Parse(strValue);
                    this.displayTimer.Interval = this.displayLeaders * 60000;
                }
            }
        }

        public void displayInformation(object source, ElapsedEventArgs e)
        {
            List<CPlayerInfo> temp = this.playerStats;      // Make a copy so we don't overwrite information if an update occurs mid method
            if (temp.Count > 0)
            {
                Dictionary<string, int> aceInfo = new Dictionary<string, int>();
                Dictionary<string, int> goldSquadInfo = new Dictionary<string, int>();
                int tempScore = 0;
                foreach (CPlayerInfo player in temp)
                {
                    aceInfo.Add(player.SoldierName, player.Score);
                    if (goldSquadInfo.ContainsKey(player.TeamID.ToString() + "-" + player.SquadID.ToString()) == false)
                    {
                        goldSquadInfo.Add(player.TeamID.ToString() + "-" + player.SquadID.ToString(), player.Score);
                    }
                    else
                    {
                        tempScore = goldSquadInfo[player.TeamID.ToString() + "-" + player.SquadID.ToString()] + player.Score;
                        goldSquadInfo[player.TeamID.ToString() + "-" + player.SquadID.ToString()] = tempScore;
                    }
                }
                List<string> tieAces = new List<string>();
                string acePlayer = "";
                int aceScore = 0;
                List<string> playerNames = new List<string>(aceInfo.Keys);
                List<string> squadNames = new List<string>(goldSquadInfo.Keys);
                string aceMessage = "";
                string goldMessage = "";
                foreach (string pName in playerNames)
                {
                    if (aceInfo[pName] > aceScore)
                    {
                        acePlayer = pName;
                        aceScore = aceInfo[pName];
                        tieAces.Clear();
                    }
                    else if (aceInfo[pName] == aceScore)
                    {
                        tieAces.Add(acePlayer);
                        tieAces.Add(pName);
                    }
                }
                if (tieAces.Count != 0)
                {       // A tie occurred
                    if (tieAces.Count > 2)
                    {       // We don't want to spam a bunch of people
                        aceMessage = "There is a " + tieAces.Count.ToString() + " way tie for ace pin with a score of " + aceScore.ToString() + "!";
                    }
                    else
                    {
                        if (tieAces[0].Equals(string.Empty) == false)
                        {           // There are player names for the two ties
                            aceMessage = "Players " + tieAces[0] + " and " + tieAces[1] + " are tied for ace pin with a score of " + aceScore.ToString() + "!";
                        }
                        else
                        {                                                   // Try using the ace player
                            aceMessage = "Players " + acePlayer + " and " + tieAces[1] + " are tied for ace pin with a score of " + aceScore.ToString() + "!";
                        }
                    }
                }
                else
                {
                    aceMessage = acePlayer + " currently holds the ace pin with a score of " + aceScore.ToString() + "!";
                }
                acePlayer = "";
                aceScore = 0;
                tieAces.Clear();
                string[] squadNumbers;
                foreach (string gName in squadNames)
                {
                    squadNumbers = Regex.Split(gName, "-");
                    if (squadNumbers[1].Equals("0"))
                    {           // Doesn't have a squad, skip player
                        continue;
                    }
                    if (goldSquadInfo[gName] > aceScore)
                    {
                        acePlayer = gName;
                        aceScore = goldSquadInfo[gName];
                        tieAces.Clear();
                    }
                    else if (goldSquadInfo[gName] == aceScore)
                    {
                        tieAces.Add(acePlayer);
                        tieAces.Add(gName);
                    }
                }
                List<string> teamNamesForMap = this.GetTeamList("{TeamName}", this.m_currentMap.PlayList);
                if (tieAces.Count != 0)
                {       // A tie occurred
                    if (tieAces.Count > 2)
                    {       // We don't want to spam a bunch of people
                        goldMessage = "There is a " + tieAces.Count.ToString() + " way tie for gold squad with a score of " + aceScore.ToString() + "!";
                    }
                    else
                    {
                        string[] teamOne = Regex.Split(tieAces[0], "-");
                        string[] teamTwo = Regex.Split(tieAces[1], "-");
                        if (teamOne[0].Equals(string.Empty) == false)
                        {           // There are player names for the two ties
                            goldMessage = "Team " + teamNamesForMap[int.Parse(teamOne[0])] + " Squad " + this.m_squadNames[int.Parse(teamOne[1])] + " and " + "Team " + teamNamesForMap[int.Parse(teamTwo[0])] + " Squad " + this.m_squadNames[int.Parse(teamTwo[1])] + " are tied for gold squad with a score of " + aceScore.ToString() + "!";
                        }
                        else
                        {                                                   // Try using the ace player
                            teamOne = Regex.Split(acePlayer, "-");
                            goldMessage = "Team " + teamNamesForMap[int.Parse(teamOne[0])] + " Squad " + this.m_squadNames[int.Parse(teamOne[1])] + " and " + "Team " + teamNamesForMap[int.Parse(teamTwo[0])] + " Squad " + this.m_squadNames[int.Parse(teamTwo[1])] + " are tied for gold squad with a score of " + aceScore.ToString() + "!";
                        }
                    }
                }
                else
                {
                    if (acePlayer != "")
                    {
                        string[] teamOne = Regex.Split(acePlayer, "-");
                        goldMessage = "Team " + teamNamesForMap[int.Parse(teamOne[0])] + " Squad " + this.m_squadNames[int.Parse(teamOne[1])] + " currently holds the gold squad pin with a score of " + aceScore.ToString() + "!";
                    }
                }
                if (this.yellInfo.Equals(true) || this.endOfRound.Equals(true))
                {
                    if (aceMessage != "")
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.yell", aceMessage, "5000", "all");
                    }
                    Thread.Sleep(5000);
                    if (goldMessage != "")
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.yell", goldMessage, "5000", "all");
                    }
                }
                else
                {
                    if (aceMessage != "")
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", aceMessage, "all");
                    }
                    if (goldMessage != "")
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", goldMessage, "all");
                    }
                }
            }
        }
        #region Blah
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
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

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

        }

        public void OnLevelStarted()
        {
            this.displayTimer.Enabled = true;
            this.endOfRound = false;
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
            this.m_currentMap = this.GetMapByFilename(csiServerInfo.Map);
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

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            this.playerStats = lstPlayers;
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

        #region IPRoConPluginInterface2

        //
        // IPRoConPluginInterface2
        //

        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {

        }
        public void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {

        }

        public void OnServerName(string strServerName)
        {

        }

        public void OnTeamKillCountForKick(int iLimit)
        {

        }

        public void OnTeamKillValueIncrease(int iLimit)
        {

        }

        public void OnTeamKillValueDecreasePerSecond(int iLimit)
        {

        }

        public void OnTeamKillValueForKick(int iLimit)
        {

        }

        public void OnIdleTimeout(int iLimit)
        {

        }

        public void OnProfanityFilter(bool isEnabled)
        {

        }

        public void OnEndRound(int iWinningTeamID)
        {
            /*this.endOfRound = true;
			this.displayTimer.AutoReset = false; 		// Set this to false so it raises just once here. Had to hack it a bit to force the display
			this.displayTimer.Interval = 200;			// This should fire quickly
			Thread.Sleep(1000);
			this.displayTimer.Enabled = false;
			this.displayTimer.Interval = this.displayLeaders * 60000;
			this.displayTimer.AutoReset = true;
			object source = new object();
			ElapsedEventArgs e = new ElapsedEventArgs;
			displayInformation(source, e);*/
        }

        public void OnRoundOverTeamScores(List<TeamScore> lstTeamScores)
        {

        }

        public void OnRoundOverPlayers(List<string> lstPlayers)
        {

        }

        public void OnRoundOver(int iWinningTeamID)
        {
            this.endOfRound = true;
            this.displayTimer.AutoReset = false;        // Set this to false so it raises just once here. Had to hack it a bit to force the display
            this.displayTimer.Interval = 200;           // This should fire quickly
            Thread.Sleep(1000);
            this.displayTimer.Enabled = false;
            this.displayTimer.Interval = this.displayLeaders * 60000;
            this.displayTimer.AutoReset = true;
            /*object source = new object();
			ElapsedEventArgs e = new ElapsedEventArgs;
			displayInformation(source, e);*/
        }

        public void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {

        }

        public void OnLevelVariablesList(LevelVariable lvRequestedContext, List<LevelVariable> lstReturnedValues)
        {

        }

        public void OnLevelVariablesEvaluate(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue)
        {

        }

        public void OnLevelVariablesClear(LevelVariable lvRequestedContext)
        {

        }

        public void OnLevelVariablesSet(LevelVariable lvRequestedContext)
        {

        }

        public void OnLevelVariablesGet(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue)
        {

        }

        #endregion

        #endregion
    }
}