/*  Copyright 2010 Tom 'Myth' Gill
    This file is to be used with BFBC2 PRoCon.

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

using PRoCon.Plugin;

namespace PRoConEvents
{
    public class CSquadlessNag : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private List<string> m_lstMessages;
        private int m_iDisplayTime;
        private int m_iIntervalBetweenMessages;

        private bool m_blPluginEnabled = false;

        private enumBoolYesNo m_enYellResponses;

        public CSquadlessNag()
        {
            this.m_lstMessages = new List<string>();
            this.m_lstMessages.Add("Example message 1");
            this.m_lstMessages.Add("Example message 2");

            this.m_enYellResponses = enumBoolYesNo.No;

            this.m_iDisplayTime = 8000;
            this.m_iIntervalBetweenMessages = 60;
        }

        public string GetPluginName()
        {
            return "Squadless Nag";
        }

        public string GetPluginVersion()
        {
            return "1.0";
        }

        public string GetPluginAuthor()
        {
            return "Myth";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            return @"Spams a message to all the players NOT in a squad at a set interval to encourage them to join a squad and play as a team.  This is a Modified Spambot plugin.  Thanks, Phogue!";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
        }

        public void OnPluginEnable()
        {
            this.m_blPluginEnabled = true;
            this.UpdateSpamBot();

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bNag ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.m_blPluginEnabled = false;
            this.ExecuteCommand("procon.protected.tasks.remove", "CSquadlessNag");

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bNag ^1Disabled =(");
        }

        private void UpdateSpamBot()
        {

            this.ExecuteCommand("procon.protected.tasks.remove", "CSquadlessNag");

            if (this.m_blPluginEnabled == true)
            {

                int iDelay = 0;
                foreach (string strMessage in this.m_lstMessages)
                {
                    if (this.m_enYellResponses == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.tasks.add", "CSquadlessNag", iDelay.ToString(), (this.m_lstMessages.Count * this.m_iIntervalBetweenMessages).ToString(), "-1", "procon.protected.send", "admin.yell", strMessage, this.m_iDisplayTime.ToString(), "squad", "1", "0");
                        this.ExecuteCommand("procon.protected.tasks.add", "CSquadlessNag", iDelay.ToString(), (this.m_lstMessages.Count * this.m_iIntervalBetweenMessages).ToString(), "-1", "procon.protected.send", "admin.yell", strMessage, this.m_iDisplayTime.ToString(), "squad", "2", "0");
                    }
                    else
                    {

                        this.ExecuteCommand("procon.protected.tasks.add", "CSquadlessNag", iDelay.ToString(), (this.m_lstMessages.Count * this.m_iIntervalBetweenMessages).ToString(), "-1", "procon.protected.send", "admin.say", strMessage, "squad", "1", "0");
                        this.ExecuteCommand("procon.protected.tasks.add", "CSquadlessNag", iDelay.ToString(), (this.m_lstMessages.Count * this.m_iIntervalBetweenMessages).ToString(), "-1", "procon.protected.send", "admin.say", strMessage, "squad", "2", "0");
                    }

                    iDelay += this.m_iIntervalBetweenMessages;
                }
            }
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Messages (one per line)|Messages", typeof(string[]), this.m_lstMessages.ToArray()));

            lstReturn.Add(new CPluginVariable("Communication|Superspam! (Yell the spam)", typeof(enumBoolYesNo), this.m_enYellResponses));
            if (this.m_enYellResponses == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Communication|Show spam (seconds)", this.m_iDisplayTime.GetType(), this.m_iDisplayTime / 1000));
            }

            lstReturn.Add(new CPluginVariable("Timing|Interval between messages (seconds)", this.m_iIntervalBetweenMessages.GetType(), this.m_iIntervalBetweenMessages));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Messages", typeof(string[]), this.m_lstMessages.ToArray()));

            lstReturn.Add(new CPluginVariable("Superspam! (Yell the spam)", typeof(enumBoolYesNo), this.m_enYellResponses));
            lstReturn.Add(new CPluginVariable("Show spam (seconds)", this.m_iDisplayTime.GetType(), this.m_iDisplayTime / 1000));
            lstReturn.Add(new CPluginVariable("Interval between messages (seconds)", this.m_iIntervalBetweenMessages.GetType(), this.m_iIntervalBetweenMessages));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iTimeSeconds = 8;

            if (strVariable.CompareTo("Messages") == 0)
            {
                this.m_lstMessages = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Superspam! (Yell the spam)") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enYellResponses = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Interval between messages (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iIntervalBetweenMessages = iTimeSeconds;
            }
            else if (strVariable.CompareTo("Show spam (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iDisplayTime = iTimeSeconds * 1000;

                if (iTimeSeconds <= 0)
                {
                    this.m_iDisplayTime = 1000;
                }
                else if (iTimeSeconds > 60)
                {
                    this.m_iDisplayTime = 59000;
                }
            }

            this.UpdateSpamBot();
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

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {

        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {

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