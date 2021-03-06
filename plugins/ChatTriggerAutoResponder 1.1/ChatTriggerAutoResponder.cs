using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Configuration;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;


namespace PRoConEvents
{
    public class ChatTriggerAutoResponder : PRoConPluginAPI, IPRoConPluginInterface
    {
        private string m_strPreviousMessage = string.Empty;
        private int t_numberOfRules;

        private string m_strServerType;
        private int m_iYellDivider;

        private bool m_isPluginEnabled;

        private Dictionary<int, Rule> t_dicRules = new Dictionary<int, Rule>();

        public ChatTriggerAutoResponder()
        {
            this.t_numberOfRules = 0;
            this.m_strServerType = "none";
            this.m_iYellDivider = 1;
            this.m_isPluginEnabled = false;
        }

        public string GetPluginName()
        {
            return "Chat Trigger Auto-Responder";
        }

        public string GetPluginVersion()
        {
            return "v1.1";
        }

        public string GetPluginAuthor()
        {
            return @"Original Authors: [CPC] blactionhero and [CPC] Cmeregirl. Fixes & Enhancements: EBassie";
        }

        public string GetPluginWebsite()
        {
            return "https://forum.myrcon.com/showthread.php?1165";
        }

        public string GetPluginDescription()
        {
            return @"		
<h2>Description</h2>
Based on CServerRulesOnRequest by Lorax74.<br>
<p></p>
<p>Allows the creation and editing of multiple chat trigger keywords to which multi-line responses may be associated. The responses are delivered privately to the person who activated the trigger via admin.say or admin.yell or to everyone in the server</p>

<h2>Configuration</h2>
<blockquote><h4>Number of triggers</h4>
The number of triggers you would like to configure. Increasing this number adds triggers to the bottom of the list, decreasing it removes triggers from the bottom of the list. Removed triggers do not retain their configs.</blockquote>
<p></p>
<h3>Trigger #</h3>
<blockquote><h4>Chat trigger</h4> Sets the chat trigger to match. Ex.: @rules</blockquote>
<blockquote><h4>Send to all?</h4> Set to On to output to everyone, or Off to output to only the person who triggered.</blockquote>
<blockquote><h4>Yell output?</h4> Set to On to admin.yell the Output text, or Off to admin.say it.</blockquote>
<blockquote><h4>Delay before sending output</h4> Seconds after a trigger to wait before sending the Output text. Ex.: 1</blockquote>
<blockquote><h4>How long to show yelled output</h4> econds to display each yelled Output text. Ex.: 8</blockquote>
<blockquote><h4>Delay between each line of output</h4> Seconds to wait between sending each line of Output text. Ex.: 1</blockquote>
<blockquote><h4>Output text</h4> The lines of text sent when the Chat Trigger is matched. Ex.: No hacks allowed!</blockquote>
<p></p>
<h2>Technical Support</h2>
<p>If you experience any issues with this plugin, or would like to request an additional<br>
feature, please use this plugin's thread at https://forum.myrcon.com/showthread.php?1165</p>
";
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bChat Trigger Auto-Responder ^2Enabled!");
            this.m_isPluginEnabled = true;
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bChat Trigger Auto-Responder ^1Disabled.");
            this.m_isPluginEnabled = false;
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            if (this.m_strServerType == "bf3" || this.m_strServerType == "bf4")
            {
                this.m_iYellDivider = 1000;
            }

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Step 1: Set Number of Triggers|Number of Triggers", this.t_numberOfRules.GetType(), this.t_numberOfRules));

            if (this.t_dicRules.Count < this.t_numberOfRules)
            {
                do
                {
                    this.t_dicRules.Add(t_dicRules.Count + 1, new Rule(t_dicRules.Count + 1));
                }
                while (this.t_dicRules.Count < this.t_numberOfRules);
            }
            else if (this.t_dicRules.Count > this.t_numberOfRules)
            {
                do
                {
                    this.t_dicRules.Remove(t_dicRules.Count);
                }
                while (this.t_dicRules.Count > this.t_numberOfRules);
            }

            foreach (Rule rule in t_dicRules.Values)
            {
                lstReturn.Add(new CPluginVariable(String.Format("Step 2: Config - Trigger {0}|{0}.1 Chat trigger:", rule.ID), rule.Trigger.GetType(), rule.Trigger));
                lstReturn.Add(new CPluginVariable(String.Format("Step 2: Config - Trigger {0}|{0}.2 Yell output?", rule.ID), typeof(enumBoolYesNo), rule.Yell));
                lstReturn.Add(new CPluginVariable(String.Format("Step 2: Config - Trigger {0}|{0}.3 Send to all?", rule.ID), typeof(enumBoolYesNo), rule.SendToAll));
                lstReturn.Add(new CPluginVariable(String.Format("Step 2: Config - Trigger {0}|{0}.4 Delay before sending output:", rule.ID), rule.DelaySend.GetType(), rule.DelaySend));
                lstReturn.Add(new CPluginVariable(String.Format("Step 2: Config - Trigger {0}|{0}.5 How long to show yelled output:", rule.ID), rule.DisplayTime.GetType(), rule.DisplayTime));
                lstReturn.Add(new CPluginVariable(String.Format("Step 2: Config - Trigger {0}|{0}.6 Delay between each line of output:", rule.ID), rule.DelayBetween.GetType(), rule.DelayBetween));
                lstReturn.Add(new CPluginVariable(String.Format("Step 2: Config - Trigger {0}|{0}.7 Output text:", rule.ID), rule.Output.GetType(), rule.Output));
            }
            return lstReturn;

        }

        public List<CPluginVariable> GetPluginVariables()
        {
            if (this.m_strServerType == "bf3" || this.m_strServerType == "bf4")
            {
                this.m_iYellDivider = 1000;
            }

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Number of Triggers", this.t_numberOfRules.GetType(), this.t_numberOfRules));

            foreach (Rule rule in t_dicRules.Values)
            {
                lstReturn.Add(new CPluginVariable(String.Format("{0}.1 Chat trigger:", rule.ID), rule.Trigger.GetType(), rule.Trigger));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.2 Yell output?", rule.ID), typeof(enumBoolYesNo), rule.Yell));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.3 Send to all?", rule.ID), typeof(enumBoolYesNo), rule.SendToAll));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.4 Delay before sending output:", rule.ID), rule.DelaySend.GetType(), rule.DelaySend));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.5 How long to show yelled output:", rule.ID), rule.DisplayTime.GetType(), rule.DisplayTime));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.6 Delay between each line of output:", rule.ID), rule.DelayBetween.GetType(), rule.DelayBetween));
                lstReturn.Add(new CPluginVariable(String.Format("{0}.7 Output text:", rule.ID), rule.Output.GetType(), rule.Output));
            }
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int intOut = 0;
            int a = strVariable.IndexOf("|");
            int b = strVariable.IndexOf(".");
            int c = b - a;

            if (strVariable.CompareTo("Number of Triggers") == 0 && int.TryParse(strValue, out intOut) == true)
            {
                this.t_numberOfRules = intOut;
            }

            if (c > 0)
            {
                int.TryParse(strVariable.Substring(strVariable.IndexOf("|") + 1, (strVariable.IndexOf(".") - strVariable.IndexOf("|") - 1)), out intOut);
                Rule rule = t_dicRules[intOut];

                if (strVariable.CompareTo(string.Format("{0}.1 Chat trigger:", rule.ID)) == 0)
                {
                    rule.Trigger = strValue;
                }
                else if (strVariable.CompareTo(string.Format("{0}.2 Yell output?", rule.ID)) == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                {
                    rule.Yell = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                }
                else if (strVariable.CompareTo(string.Format("{0}.3 Send to all?", rule.ID)) == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                {
                    rule.SendToAll = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                }
                else if (strVariable.CompareTo(string.Format("{0}.4 Delay before sending output:", rule.ID)) == 0)
                {
                    rule.DelaySend = int.Parse(strValue);
                }
                else if (strVariable.CompareTo(string.Format("{0}.5 How long to show yelled output:", rule.ID)) == 0)
                {
                    rule.DisplayTime = int.Parse(strValue);
                }
                else if (strVariable.CompareTo(string.Format("{0}.6 Delay between each line of output:", rule.ID)) == 0)
                {
                    rule.DelayBetween = int.Parse(strValue);
                }
                else if (strVariable.CompareTo(string.Format("{0}.7 Output text:", rule.ID)) == 0)
                {
                    rule.Output = CPluginVariable.DecodeStringArray(strValue);
                }
            }
        }

        private class Rule
        {
            private int _id;
            private string _trigger;
            public enumBoolYesNo Yell;
            public enumBoolYesNo SendToAll;
            private int _delaysend;
            private int _displaytime;
            private int _delaybetween;
            private string[] _output;


            public Rule()
            {
                this._id = 0;
                this._trigger = "Trigger Keyword";
                this.Yell = enumBoolYesNo.No;
                this.SendToAll = enumBoolYesNo.No;
                this._delaysend = 5;
                this._displaytime = 8000;
                this._delaybetween = 0;
                this._output = new string[] { "Output text goes here" };
            }

            public Rule(int ruleID)
            {
                this._id = ruleID;
                this._trigger = "Trigger Keyword";
                this.Yell = enumBoolYesNo.No;
                this.SendToAll = enumBoolYesNo.No;
                this._delaysend = 5;
                this._displaytime = 8000;
                this._delaybetween = 0;
                this._output = new string[] { "Output text goes here" };
            }

            public Rule(string strName)
            {
                this._id = 0;
                this._trigger = "Trigger Keyword";
                this.Yell = enumBoolYesNo.No;
                this.SendToAll = enumBoolYesNo.No;
                this._delaysend = 5;
                this._displaytime = 8000;
                this._delaybetween = 0;
                this._output = new string[] { "Output text goes here" };
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
            public string Trigger
            {
                set
                {
                    _trigger = value;
                }
                get
                {
                    return _trigger;
                }
            }
            public int DelaySend
            {
                set
                {
                    _delaysend = value;
                }
                get
                {
                    return _delaysend;
                }
            }
            public int DisplayTime
            {
                set
                {
                    _displaytime = value;
                }
                get
                {
                    return _displaytime;
                }
            }
            public int DelayBetween
            {
                set
                {
                    _delaybetween = value;

                }
                get
                {
                    return _delaybetween;
                }
            }
            public string[] Output
            {
                set
                {
                    _output = value;
                }
                get
                {
                    return _output;
                }
            }
        }

        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            if ((strSpeaker).Equals("Server") == false)
            {

                foreach (Rule rule in t_dicRules.Values)
                {
                    string t_isItATrigger = rule.Trigger;
                    string[] t_output = rule.Output;
                    enumBoolYesNo t_yellOrNot = rule.Yell;
                    enumBoolYesNo t_sendToAllOrNot = rule.SendToAll;
                    int t_delay = rule.DelaySend;
                    int t_displayTime = rule.DisplayTime;

                    Match mMatch;
                    mMatch = Regex.Match(strMessage, t_isItATrigger, RegexOptions.IgnoreCase);

                    if (mMatch.Success == true)
                    {
                        foreach (string line in t_output)
                        {
                            if (t_yellOrNot == enumBoolYesNo.Yes)
                            {
                                if (t_sendToAllOrNot == enumBoolYesNo.Yes)
                                {

                                    //this.ExecuteCommand("procon.protected.tasks.add", "ChatTriggerAutoResponder", t_delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", line, t_displayTime.ToString(), "all");
                                    //t_delay += (t_displayTime / 1000) + t_delay;

                                    this.ExecuteCommand("procon.protected.tasks.add", "ChatTriggerAutoResponder", t_delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", line, (t_displayTime / this.m_iYellDivider).ToString(), "all");
                                    t_delay += (t_displayTime / 1000) + t_delay;
                                    this.ExecuteCommand("procon.protected.chat.write", "^4ChatTriggerAutoResponder: " + line.ToUpper());

                                }
                                else
                                {

                                    //this.ExecuteCommand("procon.protected.tasks.add", "ChatTriggerAutoResponder", t_delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", line, t_displayTime.ToString(), "player", strSpeaker);
                                    //t_delay += (t_displayTime / 1000) + t_delay;

                                    this.ExecuteCommand("procon.protected.tasks.add", "ChatTriggerAutoResponder", t_delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", line, (t_displayTime / this.m_iYellDivider).ToString(), "player", strSpeaker);
                                    t_delay += (t_displayTime / 1000) + t_delay;
                                    this.ExecuteCommand("procon.protected.chat.write", "^4ChatTriggerAutoResponder: " + line.ToUpper());

                                }
                            }

                            else
                            {
                                if (t_sendToAllOrNot == enumBoolYesNo.Yes)
                                {

                                    this.ExecuteCommand("procon.protected.tasks.add", "ChatTriggerAutoResponder", t_delay.ToString(), "1", "1", "procon.protected.send", "admin.say", line, "all");
                                    this.ExecuteCommand("procon.protected.chat.write", "^4ChatTriggerAutoResponder: " + line);
                                }

                                else
                                {
                                    this.ExecuteCommand("procon.protected.tasks.add", "ChatTriggerAutoResponder", t_delay.ToString(), "1", "1", "procon.protected.send", "admin.say", line, "player", strSpeaker);
                                    this.ExecuteCommand("procon.protected.chat.write", "^4ChatTriggerAutoResponder: " + line);
                                }
                            }
                        }
                    }
                    else { }
                }
            }
            else { }

            this.m_strPreviousMessage = strMessage;
        }

        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        // Unused methods:

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {

        }

        public void OnMaplistMapRemoved(int iMapIndex)
        {

        }

        public void OnMaplistMapInserted(int iMapIndex, string strMapFileName)
        {

        }

        public void OnRunNextLevel()
        {

        }

        public void OnCurrentLevel(string strCurrentLevel)
        {

        }

        public void OnRestartLevel()
        {

        }

        public void OnLevelStarted()
        {

        }

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

        public void OnPunkbusterBanInfo(CBanInfo cbiPunkbusterBan)
        {

        }

        public void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {

        }

        public void OnResponseError(List<string> lstRequestWords, string strError)
        {

        }

        public void OnLogin()
        {

        }

        public void OnLogout()
        {

        }

        public void OnQuit()
        {

        }

        public void OnRunScript(string strScriptFileName)
        {

        }

        public void OnRunScriptError(string strScriptFileName, int iLineError, string strErrorDescription)
        {

        }

        public void OnServerInfo(CServerInfo csiServerInfo)
        {

        }

        public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset)
        {

        }

        public void OnSaying(string strMessage, CPlayerSubset cpsSubset)
        {

        }
        public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps)
        {

        }

        public void OnPlaylistSet(string strPlaylist)
        {

        }

        public void OnListPlaylists(List<string> lstPlaylists)
        {

        }

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

        public void OnMaplistCleared()
        {

        }

        public void OnMaplistList(List<string> lstMapFileNames)
        {

        }

        public void OnMaplistNextLevelIndex(int iMapIndex)
        {

        }

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

        public void OnConnectionClosed()
        {

        }

        public void OnPlayerJoin(string strSoldierName)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {

        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

        }

        public void OnPunkbusterMessage(string strPunkbusterMessage)
        {

        }

        public void OnVersion(string strServerType, string strVersion)
        {
            /*
                        this.m_strServerType = serverType.ToLower();

                        if (this.m_strServerType == "bf3" || this.m_strServerType == "bf4")
                        {
                            if (this.m_enDoDebugOutput == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "ChatTriggerAutoResponder: Discovered GameType: " + this.m_strServerType);
                            }
                            this.m_iYellDivider = 1000;
                        }
            */
        }

        public void OnHelp(List<string> lstCommands)
        {

        }

        public void OnLoadingLevel(string mapFileName)
        {

        }
    }
}
