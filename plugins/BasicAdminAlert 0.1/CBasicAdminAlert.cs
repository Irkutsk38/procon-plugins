
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Odbc;
using System.Configuration;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Net.Mail;

using PRoCon.Plugin;

namespace PRoConEvents
{
    public class CBasicAdminAlert : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private List<string> m_lstWords;
        private string str_Sound;
        private int m_strRepeats;

        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();


        public CBasicAdminAlert()
        {
            this.m_lstWords = new List<string>();
            this.m_lstWords.Add("admin");
            this.m_lstWords.Add("hacker");
            this.m_strRepeats = 1;
            this.str_Sound = "tada.wav";

        }

        public string GetPluginName()
        {
            return "Admin Alerter BETA";
        }

        public string GetPluginVersion()
        {
            return "0.1 Beta";
        }

        public string GetPluginAuthor()
        {
            return "<{AU}> Zaeed";
        }

        public string GetPluginWebsite()
        {
            return "www.au-clan.com";
        }

        public string GetPluginDescription()
        {
            return @"Monitors chat, will beep when a flagged word is detected.  The default sound is tada.wav.
This file should be located in the Media folder within the main Procon folder, along with the Plugin folder etc.  ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
        }



        public void OnPluginEnable()
        {

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bChat watcher ^2Enabled");
        }

        public void OnPluginDisable()
        {

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bChat watcher ^1Disabled");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Flagged words (one per line)", typeof(string[]), this.m_lstWords.ToArray()));
            lstReturn.Add(new CPluginVariable("Alert sound (including extension)", this.str_Sound.GetType(), this.str_Sound));
            lstReturn.Add(new CPluginVariable("Number of times to repeat sound", this.m_strRepeats.GetType(), this.m_strRepeats));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Flagged words (one per line)", typeof(string[]), this.m_lstWords.ToArray()));
            lstReturn.Add(new CPluginVariable("Alert sound (including extension)", this.str_Sound.GetType(), this.str_Sound));
            lstReturn.Add(new CPluginVariable("Number of times to repeat sound", this.m_strRepeats.GetType(), this.m_strRepeats));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iCount = 8;

            if (strVariable.CompareTo("Alert sound (including extension)") == 0)
            {
                this.str_Sound = strValue;
            }
            else if (strVariable.CompareTo("Flagged words (one per line)") == 0)
            {
                this.m_lstWords = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Number of times to repeat sound") == 0 && int.TryParse(strValue, out iCount) == true)
            {
                this.m_strRepeats = iCount;
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

        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

        }

        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            string[] messageWords = strMessage.Split(new Char[] { ' ' });
            string m_Repeats = m_strRepeats.ToString();

            foreach (string words in messageWords)
            {
                foreach (string match in m_lstWords)
                {
                    if ((match.ToLower()).CompareTo((words.ToLower())) == 0)
                    {
                        this.ExecuteCommand("procon.protected.playsound", (string)this.str_Sound, m_Repeats);
                    }
                }
            }
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
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