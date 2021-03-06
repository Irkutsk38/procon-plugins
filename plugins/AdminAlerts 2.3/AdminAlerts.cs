/*
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;

using System.Threading;
using System.ComponentModel;

using PRoCon.Plugin;
*/

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using System.Threading;
using System.ComponentModel;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    //public class AdminAlerts : CPRoConMarshalByRefObject, IPRoConPluginInterface
    public class AdminAlerts : PRoConPluginAPI, IPRoConPluginInterface
    {
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;
        private string m_strPreviousMessage;
        private int m_iAlertCount;
        private Dictionary<int, Alert> m_dicAlerts = new Dictionary<int, Alert>();
        enumBoolOnOff m_enumLogToChat;
        //enumBoolOnOff m_enumLogToConsole;
        enumBoolOnOff m_enumLogToPluginConsole;

        public AdminAlerts()
        {
            this.m_iAlertCount = 0;
            this.m_enumLogToPluginConsole = enumBoolOnOff.On;
            //this.m_enumLogToConsole = enumBoolOnOff.Off;
            this.m_enumLogToChat = enumBoolOnOff.On;
        }

        public string GetPluginName()
        {
            return "Admin Alerts";
        }
        public string GetPluginVersion()
        {
            return "2.3";
        }
        public string GetPluginAuthor()
        {
            return "Original by [TG-9th] Lorax74 - Updated by E-Bastard";
        }
        public string GetPluginWebsite()
        {
            return "";
        }
        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>			
<p>AdminAlerts allows you to create and customize as many alerts as you need.</p>  
<h2>Configuration</h2>
<blockquote><h4>Number of Alerts</h4>
The number of alerts you would like to configure.  Increasing this number adds alerts to the bottom of the list, decreasing it removes alerts from the bottom of the list.  Removed alerts do not retain their configs.</blockquote>	
<blockquote><h4>Log to Chat</h4>Show the alert on the chat-tab</blockquote>		
<blockquote><h4>Log to Pluginconsole</h4>Show the alert on the plugin-console</blockquote>		
<h3>Alert #</h3>
<blockquote><h4>[1.1 Name]</h4>A friendly name for you to quickly and easily tell which alert has fired.</blockquote>
<blockquote><h4>[1.2 Enabled]</h4>Allows you to turn on and off individual alerts without having to remove the alert or disable the plugin.</blockquote>
<blockquote><h4>[1.3 Keywords]</h4>An array of words, one per line, to watch and alert for.</blockquote>
<blockquote><h4>[1.4 Enable Audio alerts]</h4>Enables a sound to be played when this alert fires.</blockquote>
<blockquote><h4>[1.4a     Audio File]</h4>The name of the audio file in WAV format. Place audio files in the Media directory. Default is chimes.wav.</blockquote>
<blockquote><h4>[1.4b     Repeat Count]</h4>The number of times the audio file will repeat before stopping. Default is 1.</blockquote>
<blockquote><h4>[1.5 Enable system tray alerts]</h4>Enables system tray notification for this alert.</blockquote>
<blockquote><h4>[1.6 Test alert]</h4>Setting this to Yes will play the audio file if enabled and show the system tray test meassage if enabled.</blockquote>
<br />
<h2>Notes</h2>
<ul>
<li>This plugin will examine each line of chat against each alert configured.</li>  
<li>Only plays .wav files in PCM format.</li>  
<li>Only one sound will play at a time.  If a new alert comes in as an alert is playing, the new alert will play and the old will stop</li>
</ul>
<br /><br />
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
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAdmin Alerts ^2Enabled.");
        }
        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAdmin Alerts ^1Disabled.");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("~Config|Number of alerts", this.m_iAlertCount.GetType(), this.m_iAlertCount));
            lstReturn.Add(new CPluginVariable("~Config|Log to chat", typeof(enumBoolOnOff), this.m_enumLogToChat));
            lstReturn.Add(new CPluginVariable("~Config|Log to pluginconsole", typeof(enumBoolOnOff), this.m_enumLogToPluginConsole));
            //lstReturn.Add(new CPluginVariable("~Config|Log to console", typeof(enumBoolOnOff), this.m_enumLogToConsole));

            if (this.m_dicAlerts.Count < this.m_iAlertCount)
            {
                do
                {
                    this.m_dicAlerts.Add(m_dicAlerts.Count + 1, new Alert(m_dicAlerts.Count + 1));
                }
                while (this.m_dicAlerts.Count < this.m_iAlertCount);
            }
            else if (this.m_dicAlerts.Count > this.m_iAlertCount)
            {
                do
                {
                    this.m_dicAlerts.Remove(m_dicAlerts.Count);
                }
                while (this.m_dicAlerts.Count > this.m_iAlertCount);
            }

            foreach (Alert alert in m_dicAlerts.Values)
            {
                lstReturn.Add(new CPluginVariable(String.Format("Alert {0}|{0}.1 Name", alert.ID), alert.Name.GetType(), alert.Name));
                lstReturn.Add(new CPluginVariable(String.Format("Alert {0}|{0}.2 Enabled", alert.ID), typeof(enumBoolYesNo), alert.Enabled));
                lstReturn.Add(new CPluginVariable(String.Format("Alert {0}|{0}.3 Key Words", alert.ID), alert.Keywords.GetType(), alert.Keywords));
                lstReturn.Add(new CPluginVariable(String.Format("Alert {0}|{0}.4 Enable audio alerts", alert.ID), typeof(enumBoolYesNo), alert.EnableAudioAlerts));
                if (alert.EnableAudioAlerts == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable(String.Format("Alert {0}|{0}.4a     Audio File", alert.ID), alert.AudioFile.GetType(), alert.AudioFile));
                    lstReturn.Add(new CPluginVariable(String.Format("Alert {0}|{0}.4b     Audio File Repeat Count", alert.ID), alert.RepeatCount.GetType(), alert.RepeatCount));
                }
                lstReturn.Add(new CPluginVariable(String.Format("Alert {0}|{0}.5 Enable system tray alerts", alert.ID), typeof(enumBoolYesNo), alert.EnableSystemTrayAlerts));
                lstReturn.Add(new CPluginVariable(String.Format("Alert {0}|{0}.6 Test alert", alert.ID), typeof(enumBoolYesNo), alert.TestAlertSettings));
                if (alert.TestAlertSettings == enumBoolYesNo.Yes)
                {
                    if (alert.EnableAudioAlerts == enumBoolYesNo.Yes)
                    {
                        this.PlaySound(alert.AudioFile, alert.RepeatCount);
                    }
                    if (alert.EnableSystemTrayAlerts == enumBoolYesNo.Yes)
                    {
                        this.SysTrayAlert(alert.Name, alert.Name, "Testing system tray alerts.");
                    }
                    alert.TestAlertSettings = enumBoolYesNo.No;
                    this.ReportStatus(string.Format("Testing alert: {0}.", alert.Name));
                }
            }
            return lstReturn;
        }
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Number of alerts", this.m_iAlertCount.GetType(), this.m_iAlertCount));
            lstReturn.Add(new CPluginVariable("Log to chat", typeof(enumBoolOnOff), this.m_enumLogToChat));
            lstReturn.Add(new CPluginVariable("Log to pluginconsole", typeof(enumBoolOnOff), this.m_enumLogToPluginConsole));
            //lstReturn.Add(new CPluginVariable("Log to console", typeof(enumBoolOnOff), this.m_enumLogToConsole));

            foreach (Alert alert in m_dicAlerts.Values)
            {
                lstReturn.Add(new CPluginVariable(String.Format("{0}.1 Name", alert.ID), alert.Name.GetType(), alert.Name));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.2 Enabled", alert.ID), typeof(enumBoolYesNo), alert.Enabled));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.3 Key Words", alert.ID), alert.Keywords.GetType(), alert.Keywords));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.4 Enable audio alerts", alert.ID), typeof(enumBoolYesNo), alert.EnableAudioAlerts));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.4a     Audio File", alert.ID), alert.AudioFile.GetType(), alert.AudioFile));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.4b     Audio File Repeat Count", alert.ID), alert.RepeatCount.GetType(), alert.RepeatCount));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.5 Enable system tray alerts", alert.ID), typeof(enumBoolYesNo), alert.EnableSystemTrayAlerts));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.6 Test alert", alert.ID), typeof(enumBoolYesNo), alert.TestAlertSettings));
            }
            return lstReturn;
        }
        public void SetPluginVariable(string strVariable, string strValue)
        {
            int intOut = 0;
            int a = strVariable.IndexOf("|");
            int b = strVariable.IndexOf(".");
            int c = b - a;

            if (c > 0)
            {
                int.TryParse(strVariable.Substring(strVariable.IndexOf("|") + 1, (strVariable.IndexOf(".") - strVariable.IndexOf("|") - 1)), out intOut);

                Alert alert = m_dicAlerts[intOut];

                if (strVariable.CompareTo(string.Format("{0}.1 Name", alert.ID)) == 0)
                {
                    alert.Name = strValue;
                }
                else if (strVariable.CompareTo(string.Format("{0}.2 Enabled", alert.ID)) == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                {
                    alert.Enabled = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                }
                else if (strVariable.CompareTo(string.Format("{0}.3 Key Words", alert.ID)) == 0)
                {
                    alert.Keywords = CPluginVariable.DecodeStringArray(strValue);
                }
                else if (strVariable.CompareTo(string.Format("{0}.4 Enable audio alerts", alert.ID)) == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                {
                    alert.EnableAudioAlerts = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                }
                else if (strVariable.CompareTo(string.Format("{0}.4a     Audio File", alert.ID)) == 0)
                {
                    alert.AudioFile = strValue;
                }
                else if (strVariable.CompareTo(string.Format("{0}.4b     Audio File Repeat Count", alert.ID)) == 0 && int.TryParse(strValue, out intOut) == true)
                {
                    alert.RepeatCount = intOut;
                }
                else if (strVariable.CompareTo(string.Format("{0}.5 Enable system tray alerts", alert.ID)) == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                {
                    alert.EnableSystemTrayAlerts = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                }
                else if (strVariable.CompareTo(string.Format("{0}.6 Test alert", alert.ID)) == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                {
                    alert.TestAlertSettings = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                }
            }

            if (strVariable.CompareTo("Number of alerts") == 0 && int.TryParse(strValue, out intOut) == true)
            {
                this.m_iAlertCount = intOut;
            }
            else if (strVariable.CompareTo("Log to chat") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.m_enumLogToChat = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Log to pluginconsole") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.m_enumLogToPluginConsole = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            //else if (strVariable.CompareTo("Log to console") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            //{
            //    this.m_enumLogToConsole = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            //}
        }

        private class Alert
        {
            private int _id;
            private int _soundrepeatcount;
            private string[] _buzzwords;
            private string _soundfile;
            private string _name;
            public enumBoolYesNo Enabled;
            public enumBoolYesNo EnableSystemTrayAlerts;
            public enumBoolYesNo EnableAudioAlerts;
            public enumBoolYesNo TestAlertSettings;

            public Alert()
            {
                this._id = 0;
                this._name = "Alert";
                this._soundfile = "chimes.wav";
                this._soundrepeatcount = 1;
                this._buzzwords = new string[] { "keyword list" };
                this.TestAlertSettings = enumBoolYesNo.No;
                this.Enabled = enumBoolYesNo.No;
                this.EnableSystemTrayAlerts = enumBoolYesNo.No;
                this.EnableAudioAlerts = enumBoolYesNo.No;
            }
            public Alert(int alertID)
            {
                this._id = alertID;
                this._name = String.Format("Alert {0}", alertID);
                this._soundfile = "chimes.wav";
                this._soundrepeatcount = 1;
                this._buzzwords = new string[] { "keyword list" };
                this.TestAlertSettings = enumBoolYesNo.No;
                this.Enabled = enumBoolYesNo.No;
                this.EnableSystemTrayAlerts = enumBoolYesNo.No;
                this.EnableAudioAlerts = enumBoolYesNo.No;
            }
            public Alert(string strName)
            {
                this._id = 0;
                this._name = "Alert";
                this._soundfile = "chimes.wav";
                this._soundrepeatcount = 1;
                this._buzzwords = new string[] { "keyword list" };
                this.Enabled = enumBoolYesNo.No;
                this.EnableSystemTrayAlerts = enumBoolYesNo.No;
                this.EnableAudioAlerts = enumBoolYesNo.No;
                this.TestAlertSettings = enumBoolYesNo.No;
            }
            public int ID
            {
                set
                {
                    _id = value;
                }
                get
                {
                    return _id;
                }
            }
            public string Name
            {
                set
                {
                    _name = value;
                }
                get
                {
                    return _name;
                }
            }
            public string[] Keywords
            {
                set
                {
                    _buzzwords = value;
                }
                get
                {
                    return _buzzwords;
                }
            }
            public string AudioFile
            {
                set
                {
                    _soundfile = value;
                }
                get
                {
                    return _soundfile;
                }
            }
            public int RepeatCount
            {
                set
                {
                    _soundrepeatcount = value;
                }
                get
                {
                    return _soundrepeatcount;
                }
            }
        }
        private void ReportStatus(string strMessage)
        {
            if (this.m_enumLogToPluginConsole == enumBoolOnOff.On)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", strMessage);
            }

            if (this.m_enumLogToChat == enumBoolOnOff.On)
            {
                this.ExecuteCommand("procon.protected.chat.write", strMessage);
            }

            //if (this.m_enumLogToConsole == enumBoolOnOff.On)
            //{
            //    this.ExecuteCommand("procon.protected.console.write", strMessage);
            //}
        }
        private void PlaySound(string strPathToSound, int iRepeatCount)
        {
            this.ExecuteCommand("procon.protected.playsound", strPathToSound, iRepeatCount.ToString());
        }
        private void SysTrayAlert(string strTitle, string strSpeaker, string strMessage)
        {
            this.ExecuteCommand("procon.protected.notification.write", strTitle, strSpeaker + ": " + strMessage);
        }
        private string HightlightWord(string strBuzzword, string strMessage)
        {
            return strMessage.Replace(strBuzzword, string.Format("^b^8{0}^n^0", strBuzzword));
        }

        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            // Bug from server sends messages multiple times.
            if ((String.Compare(m_strPreviousMessage, strMessage) != 0) && (String.Compare(strSpeaker, "Server") != 0))
            {
                foreach (Alert alert in this.m_dicAlerts.Values)
                {
                    if (alert.Enabled == enumBoolYesNo.Yes)
                    {
                        string[] chatline = strMessage.ToLower().Split(new char[] { ' ' });
                        foreach (string word in chatline)
                        {
                            foreach (string buzzword in alert.Keywords)
                            {
                                if (word.ToLower().Contains(buzzword.ToLower()) == true)
                                {
                                    if (alert.EnableAudioAlerts == enumBoolYesNo.Yes)
                                    {
                                        this.PlaySound(alert.AudioFile, alert.RepeatCount);
                                    }
                                    if (alert.EnableSystemTrayAlerts == enumBoolYesNo.Yes)
                                    {
                                        this.SysTrayAlert(alert.Name, strSpeaker, strMessage);
                                    }
                                    string strConsoleMessage = String.Format("^bBuzzword caught\n\t {0} > {1}", strSpeaker, this.HightlightWord(buzzword, strMessage));
                                    this.ReportStatus("^b" + strConsoleMessage);
                                }
                            }
                        }
                    }
                }
                this.m_strPreviousMessage = strMessage;
            }
        }
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        #region Unused IPRoConPluginInterface Members


        public void On3dSpotting(bool blEnabled)
        {

        }

        public void OnAccountCreated(string strUsername)
        {

        }

        public void OnAccountDeleted(string strUsername)
        {

        }

        public void OnAccountPrivilegesUpdate(string strUsername, CPrivileges spPrivs)
        {

        }

        public void OnBanAdded(CBanInfo cbiBan)
        {

        }

        public void OnBanList(List<CBanInfo> lstBans)
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

        public void OnBanRemoved(CBanInfo cbiUnban)
        {

        }

        public void OnBannerURL(string strURL)
        {


        }

        public void OnConnectionClosed()
        {

        }

        public void OnCrossHair(bool blEnabled)
        {

        }

        public void OnCurrentLevel(string strCurrentLevel)
        {

        }

        public void OnCurrentPlayerLimit(int iCurrentPlayerLimit)
        {

        }

        public void OnFriendlyFire(bool blEnabled)
        {

        }

        public void OnGamePassword(string strGamePassword)
        {

        }

        public void OnHardcore(bool blEnabled)
        {

        }

        public void OnHelp(List<string> lstCommands)
        {

        }

        public void OnKillCam(bool blEnabled)
        {

        }

        public void OnLevelStarted()
        {

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {

        }

        public void OnListPlaylists(List<string> lstPlaylists)
        {

        }

        public void OnLoadingLevel(string strMapFileName)
        {

        }

        public void OnLogin()
        {

        }

        public void OnLogout()
        {

        }

        public void OnMaplistCleared()
        {

        }

        public void OnMaplistConfigFile(string strConfigFilename)
        {

        }

        public void OnMaplistList(List<string> lstMapFileNames)
        {

        }

        public void OnMaplistLoad()
        {

        }

        public void OnMaplistMapAppended(string strMapFileName)
        {

        }

        public void OnMaplistMapInserted(int iMapIndex, string strMapFileName)
        {

        }

        public void OnMaplistMapRemoved(int iMapIndex)
        {

        }

        public void OnMaplistNextLevelIndex(int iMapIndex)
        {

        }

        public void OnMaplistSave()
        {

        }

        public void OnMaxPlayerLimit(int iMaxPlayerLimit)
        {

        }

        public void OnMiniMap(bool blEnabled)
        {

        }

        public void OnMiniMapSpotting(bool blEnabled)
        {

        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerJoin(string strSoldierName)
        {

        }

        public void OnPlayerKicked(string strSoldierName, string strReason)
        {

        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {

        }

        public void OnPlayerLimit(int iPlayerLimit)
        {

        }

        public void OnPlayerSquadChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

        public void OnPlaylistSet(string strPlaylist)
        {

        }

        public void OnPunkbuster(bool blEnabled)
        {

        }

        public void OnPunkbusterBanInfo(CBanInfo cbiPunkbusterBan)
        {

        }

        public void OnPunkbusterMessage(string strPunkbusterMessage)
        {

        }

        public void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {

        }

        public void OnQuit()
        {

        }

        public void OnRankLimit(int iRankLimit)
        {

        }

        public void OnRanked(bool blEnabled)
        {

        }

        public void OnReceiveProconVariable(string strVariableName, string strValue)
        {

        }

        public void OnReservedSlotsCleared()
        {

        }

        public void OnReservedSlotsConfigFile(string strConfigFilename)
        {

        }

        public void OnReservedSlotsList(List<string> lstSoldierNames)
        {

        }

        public void OnReservedSlotsLoad()
        {

        }

        public void OnReservedSlotsPlayerAdded(string strSoldierName)
        {

        }

        public void OnReservedSlotsPlayerRemoved(string strSoldierName)
        {

        }

        public void OnReservedSlotsSave()
        {

        }

        public void OnResponseError(List<string> lstRequestWords, string strError)
        {

        }

        public void OnRestartLevel()
        {

        }

        public void OnRunNextLevel()
        {

        }

        public void OnRunScript(string strScriptFileName)
        {

        }

        public void OnRunScriptError(string strScriptFileName, int iLineError, string strErrorDescription)
        {

        }

        public void OnSaying(string strMessage, CPlayerSubset cpsSubset)
        {

        }

        public void OnServerDescription(string strServerDescription)
        {

        }

        public void OnServerInfo(CServerInfo csiServerInfo)
        {

        }

        public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps)
        {

        }

        public void OnTeamBalance(bool blEnabled)
        {

        }

        public void OnThirdPersonVehicleCameras(bool blEnabled)
        {

        }

        public void OnVersion(string strServerType, string strVersion)
        {

        }

        public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset)
        {

        }
        #endregion		
    }
}
