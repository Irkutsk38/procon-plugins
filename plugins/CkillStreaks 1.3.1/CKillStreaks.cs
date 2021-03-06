/*  This was written [LAB]HeliMagnet for use with ProCon for Battlefield Bad Company 2
 *  Feel free to alter this file, but please keep me in mind when posting on the interwebs :D
 * */

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
    public class CKillStreaks : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();   //Players
        private Dictionary<string, int> m_playerKills = new Dictionary<string, int>();  // Keeps track of players and their current kill streaks
        private Dictionary<string, int> m_playerDeaths = new Dictionary<string, int>(); // Keeps track of players and their current death streaks
        private List<string> m_msgKillStreaks;                                          // User defined list of kills and messages
        private int m_endKillStreakValue;                                               // Check against if kill streak ends
        private string m_endKillStreakMessage;
        private List<string> m_strDyingSpree;
        private string m_strFirstBlood;
        private bool firstBlood;

        private int m_iDisplayTime;
        private enumBoolYesNo m_enYellResponses;
        private enumBoolYesNo m_consoleKillStreaks;

        public CKillStreaks()
        {
            this.m_iDisplayTime = 8000;
            this.m_enYellResponses = enumBoolYesNo.No;
            this.m_consoleKillStreaks = enumBoolYesNo.No;
            this.m_msgKillStreaks = new List<string>();
            this.m_msgKillStreaks.Add("5");
            this.m_msgKillStreaks.Add("has a 5 kill streak going!");
            this.m_endKillStreakValue = 5;
            this.m_endKillStreakMessage = "%pk% has ended %pv%'s %nk% kill streak!";
            this.m_strFirstBlood = "%pk% killed %pv% for first blood!";
            this.firstBlood = false;
            this.m_strDyingSpree = new List<string>();
            this.m_strDyingSpree.Add("5");
            this.m_strDyingSpree.Add("needs to learn how to shoot!");
        }

        public string GetPluginName()
        {
            return "Kill Streaks";
        }

        public string GetPluginVersion()
        {
            return "1.3.1";
        }

        public string GetPluginAuthor()
        {
            return "[LAB]HeliMagnet and QuarterEvil";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            return @"Shows kill streaks at admin's discretion (you customize which kill numbers to display and the message associated with it). Message will be displayed when player kills player with kill streak (e.g.: HeliMagnet ended Phogue's kill streak!.

Now includes the ability to add a custom kill streak end message. Use %pk% for the killer (who ended the kill streak), %pv% for the victim (the one who had a kill streak going), and %nk% if you want the kill streak value (number).
An example: %pk% has ended %pv%'s %nk% kill streak! Which could mean: HeliMagnet has ended Phogue's 8 kill streak!
Now includes customizable first kill message in each round (leave empty if you don't want this to display).
Now includes custom death streak messages (same system as kill streaks, but for deaths in a row)";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bKill Streak Plugin ^2Enabled!");
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bKill Streak Plugin ^1Disabled =(");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Communication|Yell Kill Streaks", typeof(enumBoolYesNo), this.m_enYellResponses));
            if (this.m_enYellResponses == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Communication|Show Message (seconds)", this.m_iDisplayTime.GetType(), this.m_iDisplayTime / 1000));
            }
            lstReturn.Add(new CPluginVariable("KillStreaks|Kill Streak List", typeof(string[]), this.m_msgKillStreaks.ToArray()));
            lstReturn.Add(new CPluginVariable("KillStreaks|Kill Streak End Message", typeof(string), this.m_endKillStreakMessage));
            lstReturn.Add(new CPluginVariable("KillStreaks|Kill Streak End Value", typeof(int), this.m_endKillStreakValue));
            lstReturn.Add(new CPluginVariable("Console|Show Kill Streaks?", typeof(enumBoolYesNo), this.m_consoleKillStreaks));

            lstReturn.Add(new CPluginVariable("DeathStreaks|Death Streak List", typeof(string[]), this.m_strDyingSpree.ToArray()));
            lstReturn.Add(new CPluginVariable("First Blood|First Blood Message", typeof(string), this.m_strFirstBlood));


            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Yell Kill Streaks", typeof(enumBoolYesNo), this.m_enYellResponses));
            lstReturn.Add(new CPluginVariable("Show Message (seconds)", this.m_iDisplayTime.GetType(), this.m_iDisplayTime / 1000));
            lstReturn.Add(new CPluginVariable("Kill Streak List", typeof(string[]), this.m_msgKillStreaks.ToArray()));
            lstReturn.Add(new CPluginVariable("Kill Streak End Message", typeof(string), this.m_endKillStreakMessage));
            lstReturn.Add(new CPluginVariable("Kill Streak End Value", typeof(int), this.m_endKillStreakValue));
            lstReturn.Add(new CPluginVariable("Show Kill Streaks?", typeof(enumBoolYesNo), this.m_consoleKillStreaks));

            lstReturn.Add(new CPluginVariable("Death Streak List", typeof(string[]), this.m_strDyingSpree.ToArray()));
            lstReturn.Add(new CPluginVariable("First Blood Message", typeof(string), this.m_strFirstBlood));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int killStreakValue = 5;
            int iTimeSeconds = 8;

            if (strVariable.CompareTo("Yell Kill Streaks") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enYellResponses = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Show Kill Streaks?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_consoleKillStreaks = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Show Message (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
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
            else if (strVariable.CompareTo("Kill Streak End Message") == 0)
            {
                this.m_endKillStreakMessage = strValue;
            }
            else if (strVariable.CompareTo("Kill Streak List") == 0)
            {
                this.m_msgKillStreaks = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Kill Streak End Value") == 0 && int.TryParse(strValue, out killStreakValue) == true)
            {
                this.m_endKillStreakValue = killStreakValue;
            }
            else if (strVariable.CompareTo("Death Streak List") == 0)
            {
                this.m_strDyingSpree = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("First Blood Message") == 0)
            {
                this.m_strFirstBlood = strValue;
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
            if (this.m_playerKills.ContainsKey(strSoldierName) == false)
            {
                this.m_playerKills.Add(strSoldierName, 0);
            }
            if (this.m_playerDeaths.ContainsKey(strSoldierName) == false)
            {
                this.m_playerDeaths.Add(strSoldierName, 0);
            }
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {
            if (this.m_playerKills.ContainsKey(strSoldierName) == true)
            {
                this.m_playerKills.Remove(strSoldierName);
            }
            if (this.m_playerDeaths.ContainsKey(strSoldierName) == true)
            {
                this.m_playerDeaths.Remove(strSoldierName);
            }

            if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayers.Remove(strSoldierName);
            }
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {
            int previousKills;
            int deaths;
            int kills;
            bool showKillStreak = true;

            if (this.firstBlood == true)
            {
                if (String.Compare(strKillerSoldierName, strVictimSoldierName) != 0)
                {                           // Not a suicide
                    if (this.GetPlayerTeamID(strKillerSoldierName) != this.GetPlayerTeamID(strVictimSoldierName))
                    {       // Not a team kill
                        this.firstBlood = false;
                        if (this.m_strFirstBlood != "")
                        {
                            this.ExecuteCommand("procon.protected.tasks.add", "CKillStreaks", "0", "1", "1", "procon.protected.send", "admin.yell", this.m_strFirstBlood.Replace("%pk%", strKillerSoldierName).Replace("%pv%", strVictimSoldierName), this.m_iDisplayTime.ToString(), "all");
                        }
                    }
                }
            }

            //Add soldier to dictionary if not already added, this helps for players that joined before Plugin was started.
            if (this.m_playerKills.ContainsKey(strKillerSoldierName) == false)
            {
                this.m_playerKills.Add(strKillerSoldierName, 0);
            }
            if (this.m_playerDeaths.ContainsKey(strKillerSoldierName) == false)
            {
                this.m_playerDeaths.Add(strKillerSoldierName, 0);
            }
            //Do the same for the Victim
            if (this.m_playerKills.ContainsKey(strVictimSoldierName) == false)
            {
                this.m_playerKills.Add(strVictimSoldierName, 0);
            }
            if (this.m_playerDeaths.ContainsKey(strVictimSoldierName) == false)
            {
                this.m_playerDeaths.Add(strVictimSoldierName, 0);
            }
            this.m_playerKills.TryGetValue(strVictimSoldierName, out previousKills);
            this.m_playerKills[strVictimSoldierName] = 0;
            if (String.Compare(strKillerSoldierName, strVictimSoldierName) != 0)
            {
                if (this.GetPlayerTeamID(strKillerSoldierName) != this.GetPlayerTeamID(strVictimSoldierName))
                {
                    this.m_playerKills[strKillerSoldierName]++;
                    this.m_playerDeaths[strKillerSoldierName] = 0;
                }
                else
                {
                    showKillStreak = false;         // Player killed a teammate, don't increment kill count or show a previous kill streak
                }
            }
            else
            {
                showKillStreak = false;             // Player killed themselves, don't increment kill count or show a previous kill streak
            }
            this.m_playerKills.TryGetValue(strKillerSoldierName, out kills);
            if (previousKills >= this.m_endKillStreakValue && showKillStreak)
            {
                //string strMessage = "%pn% has ended " + strVictimSoldierName + "'s kill streak!";
                if (this.m_enYellResponses == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.tasks.add", "CKillStreaks", "0", "1", "1", "procon.protected.send", "admin.yell", this.m_endKillStreakMessage.Replace("%pk%", strKillerSoldierName).Replace("%pv%", strVictimSoldierName).Replace("%nk%", previousKills.ToString()), this.m_iDisplayTime.ToString(), "all");
                }
                else
                {
                    this.ExecuteCommand("procon.protected.tasks.add", "CKillStreaks", "0", "1", "1", "procon.protected.send", "admin.say", this.m_endKillStreakMessage.Replace("%pk%", strKillerSoldierName).Replace("%pv%", strVictimSoldierName).Replace("%nk%", previousKills.ToString()), "all");
                }
                //Write message to plugin console as well
                if (this.m_consoleKillStreaks == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", this.m_endKillStreakMessage.Replace("%pk%", strKillerSoldierName).Replace("%pv%", strVictimSoldierName).Replace("%nk%", previousKills.ToString()));
                }
            }

            bool valueInList = false;
            int loc = 0;
            for (int i = 0; i < this.m_msgKillStreaks.Count; i = i + 2)
            {
                if (this.m_msgKillStreaks[i].Equals(kills.ToString()))
                {
                    valueInList = true;
                    loc = i;
                    break;
                }
            }

            if (valueInList == true)
            {
                string killStreak;
                try
                {
                    killStreak = this.m_msgKillStreaks[loc + 1];
                }
                catch (Exception)
                {
                    killStreak = "has a " + kills.ToString() + " kill streak going!";
                }
                string strMessage = "%pn% " + killStreak;
                if (this.m_enYellResponses == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.tasks.add", "CKillStreaks", "0", "1", "1", "procon.protected.send", "admin.yell", strMessage.Replace("%pn%", strKillerSoldierName), this.m_iDisplayTime.ToString(), "all");
                }
                else
                {
                    this.ExecuteCommand("procon.protected.tasks.add", "CKillStreaks", "0", "1", "1", "procon.protected.send", "admin.say", strMessage.Replace("%pn%", strKillerSoldierName), "all");
                }
                //Write message to plugin console as well
                if (this.m_consoleKillStreaks == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", strMessage.Replace("%pn%", strKillerSoldierName));
                }
            }

            this.m_playerDeaths[strVictimSoldierName]++;
            deaths = this.m_playerDeaths[strVictimSoldierName];
            valueInList = false;
            loc = 0;
            for (int i = 0; i < this.m_strDyingSpree.Count; i = i + 2)
            {
                if (this.m_strDyingSpree[i].Equals(deaths.ToString()))
                {
                    valueInList = true;
                    loc = i;
                    break;
                }
            }

            if (valueInList == true)
            {
                string deathStreak;
                try
                {
                    deathStreak = this.m_strDyingSpree[loc + 1];
                }
                catch (Exception)
                {
                    deathStreak = "has to learn how to shoot!";
                }
                string strMessage = "%pn% " + deathStreak;
                if (this.m_enYellResponses == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.tasks.add", "CKillStreaks", "0", "1", "1", "procon.protected.send", "admin.yell", strMessage.Replace("%pn%", strVictimSoldierName), this.m_iDisplayTime.ToString(), "all");
                }
                else
                {
                    this.ExecuteCommand("procon.protected.tasks.add", "CKillStreaks", "0", "1", "1", "procon.protected.send", "admin.say", strMessage.Replace("%pn%", strVictimSoldierName), "all");
                }
                if (this.m_consoleKillStreaks == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", strMessage.Replace("%pn%", strVictimSoldierName));
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
            // Set boolean so next kill is yelled (first blood)
            this.firstBlood = true;
            //On loading a level clear all player's kill counts.
            List<string> killPlayers = new List<string>(this.m_playerKills.Keys);
            foreach (string killPlayer in killPlayers)
            {
                this.m_playerKills[killPlayer] = 0;
            }
            //On loading a level clear all player's death counts.
            List<string> deathPlayers = new List<string>(this.m_playerDeaths.Keys);
            foreach (string deathPlayer in deathPlayers)
            {
                this.m_playerDeaths[deathPlayer] = 0;
            }
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
            if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayers[strSoldierName].TeamID = iTeamID;
            }
        }

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