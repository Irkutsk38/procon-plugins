/*  Copyright 2010 Geoffrey 'Phogue' Green

    This file is part of BFBC2 PRoCon.

    BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class MagicEightBall : PRoConPluginAPI, IPRoConPluginInterface
    {

        private List<string> m_responsesList;

        Notifications m_notification;
        ResponseScope m_responseScope;

        public MagicEightBall()
        {

            this.m_notification = Notifications.Say;
            this.m_responseScope = ResponseScope.Public;

            this.m_responsesList = this.Listify<string>("As I see it, yes", "It is certain", "It is decidedly so", "Most likely", "Outlook good", "Signs point to yes", "Without a doubt", "Yes", "Yes, definitely", "You may rely on it", "Reply hazy, try again", "Ask again later", "Better not tell you now", "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful");
        }

        public string GetPluginName()
        {
            return "The Magic Eight Ball";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.1";
        }

        public string GetPluginAuthor()
        {
            return "Phogue";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>
    <p>Gives a random response while completely</p>
    <p>This plugin borrows some code written by <a href=""http://phogue.net/forum/memberlist.php?mode=viewprofile&u=770"">t-master</a></p>
    <p>Thanks to Talnoy for the reminder/kick to make this plugin =)</p>

<h2>Commands</h2>
    <blockquote><h4>@8ball [question]</h4>If the question is over 4 characters the plugin will simply spit out a random response, like an eight ball ;)</blockquote>

<h2>Settings</h2>
    <h3>General</h3>
        <blockquote><h4>Response Scope</h4>If the whole server or just the speaking player should see the response</blockquote>
        <blockquote><h4>Notification</h4>If the response should be output to the chat box or yelled in the middle of the screen</blockquote>
        <blockquote><h4>Responses</h4>A list of random responses to give to the player.  Eight balls usually have 10 affirmative, 5 ambiguous and 5 negative answers</blockquote>
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {

        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bThe Magic Eight Ball ^2Enabled!");

            this.RegisterAllCommands();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bThe Magic Eight Ball ^1Disabled =(");

            this.UnregisterAllCommands();
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("General|Response Scope", CreateEnumString(typeof(ResponseScope)), this.m_responseScope.ToString()));
            lstReturn.Add(new CPluginVariable("General|Notification", CreateEnumString(typeof(Notifications)), this.m_notification.ToString()));
            lstReturn.Add(new CPluginVariable("General|Responses", typeof(string[]), this.m_responsesList.ToArray()));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Response Scope", CreateEnumString(typeof(ResponseScope)), this.m_responseScope.ToString()));
            lstReturn.Add(new CPluginVariable("Notification", CreateEnumString(typeof(Notifications)), this.m_notification.ToString()));
            lstReturn.Add(new CPluginVariable("Responses", typeof(string[]), this.m_responsesList.ToArray()));

            return lstReturn;
        }

        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {

            if (strVariable.CompareTo("Response Scope") == 0 && Enum.IsDefined(typeof(ResponseScope), strValue) == true)
            {
                this.m_responseScope = (ResponseScope)Enum.Parse(typeof(ResponseScope), strValue);
            }
            else if (strVariable.CompareTo("Notification") == 0 && Enum.IsDefined(typeof(Notifications), strValue) == true)
            {
                this.m_notification = (Notifications)Enum.Parse(typeof(Notifications), strValue);
            }
            else if (strVariable.CompareTo("Responses") == 0)
            {
                this.m_responsesList = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
        }

        private void UnregisterAllCommands()
        {
            this.UnregisterCommand(new MatchCommand(this.Listify<string>("@", "!", "#"), "8ball", this.Listify<MatchArgumentFormat>()));
        }

        private void RegisterAllCommands()
        {
            this.RegisterCommand(
                new MatchCommand(
                    "MagicEightBall",
                    "OnCommandEightBall",
                    this.Listify<string>("@", "!", "#"),
                    "8ball",
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "question",
                            this.Listify<string>()
                        )
                    ),
                    new ExecutionRequirements(ExecutionScope.All),
                    "A plugin that looks through the folds of space and time to answer any question accurately"));
        }

        #region In Game Commands

        private void WriteMessage(string message, CPlayerSubset audience)
        {

            string strPrefix = "8Ball > ";

            List<string> wordWrappedLines = this.WordWrap(message, 100 - strPrefix.Length);

            foreach (string line in wordWrappedLines)
            {
                string formattedLine = String.Format("{0}{1}", strPrefix.Replace("{", "{{").Replace("}", "}}"), line);

                if (audience.Subset == CPlayerSubset.PlayerSubsetType.All)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", formattedLine, "all");
                }
                else if (audience.Subset == CPlayerSubset.PlayerSubsetType.Player)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", formattedLine, "player", audience.SoldierName);
                }
            }
        }

        private void YellMessage(string message, CPlayerSubset audience)
        {
            if (audience.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                this.ExecuteCommand("procon.protected.send", "admin.yell", message, "8000", "all");
            }
            else if (audience.Subset == CPlayerSubset.PlayerSubsetType.Player)
            {
                this.ExecuteCommand("procon.protected.send", "admin.yell", message, "8000", "player", audience.SoldierName);
            }
        }

        private int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        public void OnCommandEightBall(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

            if (capCommand.ExtraArguments.Length > 0 && this.m_responsesList.Count > 0)
            {

                string message = this.m_responsesList[this.RandomNumber(0, this.m_responsesList.Count)];
                CPlayerSubset targetAudience;

                if (this.m_responseScope == ResponseScope.Public)
                {
                    targetAudience = new CPlayerSubset(CPlayerSubset.PlayerSubsetType.All);
                }
                else
                {
                    targetAudience = new CPlayerSubset(CPlayerSubset.PlayerSubsetType.Player, strSpeaker);
                }

                if (this.m_notification == Notifications.Both || this.m_notification == Notifications.Say)
                {
                    this.WriteMessage(message, targetAudience);
                }

                if (this.m_notification == Notifications.Both || this.m_notification == Notifications.Yell)
                {
                    this.YellMessage(message, targetAudience);
                }
            }
        }

        #endregion

        internal enum Notifications
        {
            Yell,
            Say,
            Both,
        }

        internal enum ResponseScope
        {
            Public,
            Private,
        }

        public string CreateEnumString(Type enumeration)
        {
            return string.Format("enum.{0}_{1}({2})", this.GetType().Name, enumeration.Name, string.Join("|", Enum.GetNames(enumeration)));
        }

        #region Unused

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

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
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

        }

        public void OnRoundOverTeamScores(List<TeamScore> lstTeamScores)
        {

        }

        public void OnRoundOverPlayers(List<string> lstPlayers)
        {

        }

        public void OnRoundOver(int iWinningTeamID)
        {

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

        #region IPRoConPluginInterface3

        //
        // IPRoConPluginInterface3
        //
        public void OnAnyMatchRegisteredCommand(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

        }

        public void OnRegisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnUnregisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage)
        {

        }

        public void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {

        }

        public void OnMaplistList(List<MaplistEntry> lstMaplist)
        {

        }

        #endregion

        #endregion
    }
}