/*  Copyright 2010 Zaeed (Matt Green)

    http://www.viridianphotos.com

    This file is part of Zaeed's Plugins for BFBC2 PRoCon.
    Zaeed's Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Zaeed's Plugins for PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with Zaeed's Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Xml;
using System.Globalization;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Events;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{

    using EventType = PRoCon.Core.Events.EventType;
    using CapturableEvent = PRoCon.Core.Events.CapturableEvents;

    public class PBHackDetect : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string strHostName;
        private string strPort;
        private string strPRoConVersion;
        private bool readySetGo;

        public Dictionary<String, CPlayerInfo> normalPlayer;
        private Dictionary<String, CPunkbusterInfo> punkbusterPlayer;
        private Dictionary<String, List<string>> nameChangeChecker;
        private Dictionary<String, int> nameChanges;
        private Dictionary<String, int> kickPlayer;
        private List<String> joinDelay;

        private string m_PBHackAction;
        private string m_NameHackAction;
        private string m_logMethod;
        private int m_iPBbanLength;
        private int m_iNamebanLength;
        private int m_iPBHitLimit;
        private int m_iNameHitLimit;

        public PBHackDetect()
        {

            this.normalPlayer = new Dictionary<String, CPlayerInfo>();
            this.punkbusterPlayer = new Dictionary<String, CPunkbusterInfo>();
            this.nameChangeChecker = new Dictionary<String, List<string>>();
            this.kickPlayer = new Dictionary<String, int>();
            this.joinDelay = new List<String>();
            this.readySetGo = false;

            this.m_PBHackAction = "Log";
            this.m_NameHackAction = "Log";
            this.m_logMethod = "Per Player";
            this.m_iPBbanLength = 5;
            this.m_iNamebanLength = 5;
            this.m_iPBHitLimit = 10;
            this.m_iNameHitLimit = 10;
        }

        #region InitStuff
        public string GetPluginName()
        {
            return "PB Hack Logger";
        }

        public string GetPluginVersion()
        {
            return "1.1.1.0";
        }

        public string GetPluginAuthor()
        {
            return "Zaeed";
        }

        public string GetPluginWebsite()
        {
            return "www.viridianphotos.com";
        }

        public string GetPluginDescription()
        {
            return @"
<p>If you find my plugins useful, please feel free to donate</p>
<blockquote>

<form action=""https://www.paypal.com/cgi-bin/webscr/"" method=""POST"" target=""_blank"">

<input type=""hidden"" name=""cmd"" value=""_s-xclick"">

<input type=""hidden"" name=""encrypted"" value=""-----BEGIN PKCS7-----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-----END PKCS7-----"">

<input type=""image"" src=""https://www.paypal.com/en_AU/i/btn/btn_donate_LG.gif"" border=""0"" name=""submit"" alt=""PayPal - The safer, easier way to pay online."">

<img alt="""" border=""0"" src=""https://www.paypal.com/en_AU/i/scr/pixel.gif"" width=""1"" height=""1"">

</form>

</blockquote>

<h2>Description</h2>
<p>The PB Hack Logger will detect the latest in Punkbuster hacks, which allows for full bypass of the Punkbuster anti-cheat system.

<h2>Name Changing</h2>
<p>One of the new hack features is the ability to change a players soldier name while ingame.  This plugin will detect and act on that change.</p>
<h2>PB Hack</h2>
<p>The core feature of the new hack is to remove or mask a players Punkbuster presence, which allows them to bypass all previous global bans.</p>
<h2>Action</h2>
<p>There are four possible actions for each detection method, Log|Kick|Temp Ban|Perm Ban</p>
<p>Choosing Log, will result in the plugin simply recording each detection.
<h2>Settings</h2>
<p>The Log method allows log files to be created either per server (one file for each connected game server), or one file for each player detected using the PB hack.</p>";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.strHostName = strHostName;
            this.strPort = strPort;
            this.strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnPunkbusterPlayerInfo", "OnPlayerLeft", "OnPlayerJoin", "OnListPlayers", "OnPlayerKilled", "OnPlayerAuthenticated");
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPB Hack Logger ^2Enabled!");
            this.normalPlayer.Clear();
            this.punkbusterPlayer.Clear();
            this.kickPlayer.Clear();
            this.nameChangeChecker.Clear();
            this.readySetGo = false;
            this.ExecuteCommand("procon.protected.tasks.add", "RemoveFalsPositiveDelay" + strHostName + strPort, "180", "1", "1", "procon.protected.plugins.call", "PBHackDetect", "StartupDelayOver");

        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPB Hack Logger ^1Disabled");
            this.normalPlayer.Clear();
            this.punkbusterPlayer.Clear();
            this.kickPlayer.Clear();
            this.nameChangeChecker.Clear();
            this.readySetGo = false;
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("PB Hack|Action after 10 PB Hack detections", "enum.enumPBHackAction(Log|Kick|Temp Ban|Perm Ban)", this.m_PBHackAction));
            if (String.Compare(this.m_PBHackAction, "Temp Ban") == 0)
            {
                lstReturn.Add(new CPluginVariable("PB Hack|Ban length for PB Hack (mins)", this.m_iPBbanLength.GetType(), this.m_iPBbanLength));
            }
            if (String.Compare(this.m_PBHackAction, "Log") != 0)
            {
                lstReturn.Add(new CPluginVariable("PB Hack|PB Hack warnings before action taken", this.m_iPBHitLimit.GetType(), this.m_iPBHitLimit));
            }
            lstReturn.Add(new CPluginVariable("Name Changing|Action when name change detected", "enum.enumNameHackAction(Log|Kick|Temp Ban|Perm Ban)", this.m_NameHackAction));
            if (String.Compare(this.m_NameHackAction, "Temp Ban") == 0)
            {
                lstReturn.Add(new CPluginVariable("Name Changing|Ban length for name change (mins)", this.m_iNamebanLength.GetType(), this.m_iNamebanLength));
            }
            lstReturn.Add(new CPluginVariable("Settings|Log file method", "enum.enumLogMethod(Per Player|Per Server)", this.m_logMethod));
            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Action after 10 PB Hack detections", "enum.enumPBHackAction(Log|Kick|Temp Ban|Perm Ban)", this.m_PBHackAction));
            lstReturn.Add(new CPluginVariable("Ban length for PB Hack (mins)", this.m_iPBbanLength.GetType(), this.m_iPBbanLength));
            lstReturn.Add(new CPluginVariable("PB Hack warnings before action taken", this.m_iPBHitLimit.GetType(), this.m_iPBHitLimit));
            lstReturn.Add(new CPluginVariable("Action when name change detected", "enum.enumNameHackAction(Log|Kick|Temp Ban|Perm Ban)", this.m_NameHackAction));
            lstReturn.Add(new CPluginVariable("Ban length for name change (mins)", this.m_iNamebanLength.GetType(), this.m_iNamebanLength));
            lstReturn.Add(new CPluginVariable("Log file method", "enum.enumLogMethod(Per Player|Per Server)", this.m_logMethod));
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {

            int intOut = 0;

            if (strVariable.CompareTo("Action after 10 PB Hack detections") == 0)
            {
                this.m_PBHackAction = strValue;
            }
            else if (strVariable.CompareTo("Log file method") == 0)
            {
                this.m_logMethod = strValue;
            }
            else if (strVariable.CompareTo("Action when name change detected") == 0)
            {
                this.m_NameHackAction = strValue;
            }
            else if (strVariable.CompareTo("Ban length for PB Hack (mins)") == 0 && int.TryParse(strValue, out intOut) == true)
            {
                this.m_iPBbanLength = intOut;
            }
            else if (strVariable.CompareTo("Ban length for name change (mins)") == 0 && int.TryParse(strValue, out intOut) == true)
            {
                this.m_iNamebanLength = intOut;
            }
            else if (strVariable.CompareTo("PB Hack warnings before action taken") == 0 && int.TryParse(strValue, out intOut) == true)
            {
                this.m_iPBHitLimit = intOut;
            }
        }
        #endregion

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {
            if (cpbiPlayer != null)
            {
                if (this.punkbusterPlayer.ContainsKey(cpbiPlayer.SoldierName) == false)
                {
                    this.punkbusterPlayer.Add(cpbiPlayer.SoldierName, cpbiPlayer);
                }
                else
                {
                    this.punkbusterPlayer[cpbiPlayer.SoldierName] = cpbiPlayer;
                }
                if (this.joinDelay.Contains(cpbiPlayer.SoldierName) == true)
                {
                    this.joinDelay.Remove(cpbiPlayer.SoldierName);
                }
            }
        }

        public override void OnPlayerJoin(string strSoldierName)
        {
            if (this.normalPlayer.ContainsKey(strSoldierName) == false)
            {
                this.normalPlayer.Add(strSoldierName, new CPlayerInfo(strSoldierName, "", 0, 24));
                this.joinDelay.Add(strSoldierName);
                this.ExecuteCommand("procon.protected.tasks.add", "RemoveJoinDelayProtection" + strHostName + strPort, "180", "1", "1", "procon.protected.plugins.call", "PBHackDetect", "RemoveProtection", strSoldierName);
            }
        }

        public void RemoveProtection(string strSoldierName)
        {
            if (this.joinDelay.Contains(strSoldierName) == true)
            {
                this.joinDelay.Remove(strSoldierName);
            }
        }

        public void StartupDelayOver()
        {
            if (readySetGo == false)
            { readySetGo = true; }
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPB Hack Logger ^2Now watching for hackers!");
        }

        public override void OnPlayerAuthenticated(string soldierName, string guid)
        {
            List<string> newList = new List<String>();
            newList.Add(soldierName);
            if (this.nameChangeChecker.ContainsKey(guid) == false)
            {
                this.nameChangeChecker.Add(guid, newList);
            }
            else
            {
                this.nameChangeChecker[guid] = newList;
            }
        }

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {
            RemoveFromLists(cpiPlayer.SoldierName, cpiPlayer.GUID);
        }

        public void RemoveFromLists(string strSoldierName, string strGUID)
        {
            if (this.normalPlayer.ContainsKey(strSoldierName) == true)
            {
                this.normalPlayer.Remove(strSoldierName);
            }

            if (this.punkbusterPlayer.ContainsKey(strSoldierName) == true)
            {
                this.punkbusterPlayer.Remove(strSoldierName);
            }
            if (this.kickPlayer.ContainsKey(strSoldierName) == true)
            {
                this.kickPlayer.Remove(strSoldierName);
            }
            if (this.nameChangeChecker.ContainsKey(strGUID) == true)
            {
                this.nameChangeChecker.Remove(strGUID);
            }
        }

        //Do all our checks here
        public override void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            if (readySetGo)
            {
                if (kKillerVictimDetails != null)
                {
                    CPlayerInfo killer = kKillerVictimDetails.Killer;
                    if ((String.IsNullOrEmpty(killer.SoldierName) == true) || (killer.Type == 3) || (this.joinDelay.Contains(killer.SoldierName)))
                    {
                        //	WriteLog("Killers name is blank.  They just killed " + kKillerVictimDetails.Victim.SoldierName);
                    }
                    else
                    {
                        if (this.normalPlayer.ContainsKey(killer.SoldierName) == true)
                        {
                            if (String.IsNullOrEmpty(killer.GUID) == true)
                            {
                                WriteLog(killer.SoldierName + " has missing EA_GUID", killer.SoldierName, "");
                                PBHackAction(killer.SoldierName, "");
                            }
                        }
                        else
                        {
                            WriteLog(killer.SoldierName + "(" + killer.GUID + ") has no record", killer.SoldierName, killer.GUID);

                        }

                        if (this.punkbusterPlayer.ContainsKey(killer.SoldierName) == true)
                        {
                            CPunkbusterInfo PBPlayer = this.punkbusterPlayer[killer.SoldierName];
                            if (String.IsNullOrEmpty(PBPlayer.GUID) == true)
                            {
                                WriteLog(killer.SoldierName + "(" + killer.GUID + ") has no PBGUID", killer.SoldierName, killer.GUID);
                                PBHackAction(killer.SoldierName, killer.GUID);
                            }
                            if (PBPlayer.GUID.Length != 32)
                            {
                                WriteLog(killer.SoldierName + "(" + killer.GUID + ") has incorrect PBGUID Length", killer.SoldierName, killer.GUID);
                                PBHackAction(killer.SoldierName, killer.GUID);
                            }

                            if (!(System.Text.RegularExpressions.Regex.IsMatch(PBPlayer.GUID, @"^[a-zA-Z0-9]+$")))
                            {
                                WriteLog(killer.SoldierName + "(" + killer.GUID + ") has invalid PBGUID", killer.SoldierName, killer.GUID);
                                PBHackAction(killer.SoldierName, killer.GUID);
                            }

                            if (String.IsNullOrEmpty(PBPlayer.Ip) == true)
                            {
                                WriteLog(killer.SoldierName + "(" + killer.GUID + ") has no IP Address", killer.SoldierName, killer.GUID);
                                PBHackAction(killer.SoldierName, killer.GUID);
                            }
                        }
                        else
                        {
                            WriteLog(killer.SoldierName + "(" + killer.GUID + ") has no PB record", killer.SoldierName, killer.GUID);
                            PBHackAction(killer.SoldierName, killer.GUID);
                        }
                    }
                }
                else
                {
                    WriteLog("Blank killer detected", "Log", "Log");
                }
            }

        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (this.normalPlayer.ContainsKey(cpiPlayer.SoldierName) == true)
                    {
                        this.normalPlayer[cpiPlayer.SoldierName] = cpiPlayer;
                    }
                    else
                    {
                        this.normalPlayer.Add(cpiPlayer.SoldierName, cpiPlayer);
                    }


                    //Check for changing names
                    if (readySetGo)
                    {
                        if (this.nameChangeChecker.ContainsKey(cpiPlayer.GUID) == true)
                        {
                            List<string> value;
                            if (this.nameChangeChecker.TryGetValue(cpiPlayer.GUID, out value))
                            {
                                if (value.Contains(cpiPlayer.SoldierName) == false)
                                {
                                    value.Add(cpiPlayer.SoldierName);
                                    this.nameChangeChecker[cpiPlayer.GUID] = value;
                                }
                                if (value.Count > 2)
                                {
                                    string names = "";
                                    foreach (string name in value)
                                    {
                                        if (names == "") { names = value.ToString(); }
                                        else { names = ", " + name.ToString(); }
                                    }
                                    WriteLog(cpiPlayer.SoldierName + "(" + cpiPlayer.GUID + ") name change hack detected.  Old values ( " + names + " )", cpiPlayer.SoldierName, cpiPlayer.GUID);
                                    NameHackAction(cpiPlayer.SoldierName, cpiPlayer.GUID);
                                }
                            }
                        }
                        else
                        {
                            List<string> newList = new List<string>();
                            newList.Add(cpiPlayer.SoldierName);
                            this.nameChangeChecker.Add(cpiPlayer.GUID, newList);
                        }
                    }

                }

                //Do some cleanup of lists
                //Find any orphaned soldiers due to disconnects not caught by OnPlayerleft
                List<string> orphanPlayers = new List<string>();
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    orphanPlayers.Add(cpiPlayer.SoldierName);  //Convert lstPlayers into a list of strings cause i'm lazy
                }

                foreach (KeyValuePair<string, CPlayerInfo> pair in normalPlayer)
                {
                    if (orphanPlayers.Contains(pair.Key) == false)
                    {
                        RemoveFromLists(pair.Key, pair.Value.GUID);
                    }
                }
                // Clear all lists just to be sure
                if (lstPlayers.Count == 0)
                {
                    this.normalPlayer.Clear();
                    this.punkbusterPlayer.Clear();
                    this.kickPlayer.Clear();
                    this.nameChangeChecker.Clear();
                }
            }
        }

        public void WriteLog(string message, string strSoldierName, string strGuid)
        {
            if (readySetGo)
            {
                string logFolder = "";
                string logFile = "";


                if (this.m_logMethod.CompareTo("Per Player") == 0)
                {
                    logFolder = Path.Combine("Plugins", "PBHackLog");
                    logFile = strSoldierName + " (" + strGuid + ").txt";
                }
                else
                {
                    logFolder = Path.Combine("Plugins", "PBHackLog");
                    logFile = "PBHackLog--" + strHostName.Replace(".", "-") + "--(" + strPort + ").txt";
                }


                string path = Path.Combine(Environment.CurrentDirectory, logFolder);
                string fullPath = Path.Combine(path, logFile);
                try
                {
                    if (!(Directory.Exists(path)))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(path);
                    }
                }
                catch (Exception ex)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPBHackLog: Error creating directory " + ex);
                }
                try
                {
                    if (File.Exists(fullPath))
                    {

                        try
                        {
                            using (StreamWriter file = new StreamWriter(fullPath, true))
                            {
                                file.WriteLine(DateTime.Now.ToString() + "-" + this.strHostName + ":" + this.strPort + "-   " + message + Environment.NewLine);
                                file.Close();

                            }
                        }
                        catch (Exception e)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPBHackLog: Error appending " + e);
                        }
                    }
                    else
                    {
                        System.IO.File.WriteAllText(fullPath, DateTime.Now.ToString() + "-" + this.strHostName + ":" + this.strPort + "-   " + message + Environment.NewLine + Environment.NewLine);
                    }
                }
                catch (Exception d)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPBHackLog: Error creating new text file " + d);
                }
            }
        }

        public void NameHackAction(string strSoldierName, string strGUID)
        {
            if (readySetGo)
            {
                switch (this.m_NameHackAction)
                {
                    case "Log":
                        break;
                    case "Kick":
                        this.ExecuteCommand("procon.protected.send", "banList.add", "guid", strGUID, "seconds", "60", DateTime.Now.ToString() + " - Name Change Hack detected for " + strSoldierName);
                        WriteLog("Kicking for Name Hack - " + strSoldierName + "(" + strGUID + ")", strSoldierName, strGUID);
                        break;
                    case "Temp Ban":
                        this.ExecuteCommand("procon.protected.send", "banList.add", "guid", strGUID, "seconds", (this.m_iNamebanLength * 60).ToString(), DateTime.Now.ToString() + " - Name Change Hack detected for " + strSoldierName);
                        WriteLog("Banning for Name Hack - " + strSoldierName + "(" + strGUID + ")", strSoldierName, strGUID);
                        break;
                    case "Perm Ban":
                        this.ExecuteCommand("procon.protected.send", "banList.add", "guid", strGUID, "perm", DateTime.Now.ToString() + " - Name Change Hack detected for " + strSoldierName);
                        WriteLog("Banning for Name Hack - " + strSoldierName + "(" + strGUID + ")", strSoldierName, strGUID);
                        break;
                }
            }
        }

        public void Debug(string message)
        {
        }

        public void PBHackAction(string strSoldierName, string strGUID)
        {
            if (readySetGo)
            {
                if (this.m_PBHackAction.CompareTo("Log") != 0)
                {
                    int kickFlags;
                    if (this.kickPlayer.ContainsKey(strSoldierName) == false)
                    {
                        this.kickPlayer.Add(strSoldierName, 1);
                    }
                    else
                    {
                        kickFlags = this.kickPlayer[strSoldierName];
                        this.kickPlayer[strSoldierName] = kickFlags + 1;
                    }
                    if (this.kickPlayer[strSoldierName] >= this.m_iPBHitLimit)
                    {
                        switch (this.m_PBHackAction)
                        {
                            case "Log":
                                break;
                            case "Kick":
                                this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSoldierName, "seconds", "60", DateTime.Now.ToString() + " - PB Hack detected for " + strSoldierName);
                                this.kickPlayer.Remove(strSoldierName);
                                WriteLog("Kicking for PB Hack - " + strSoldierName + "(" + strGUID + ")", strSoldierName, strGUID);
                                break;
                            case "Temp Ban":
                                this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSoldierName, "seconds", (this.m_iPBbanLength * 60).ToString(), DateTime.Now.ToString() + " - PB Hack detected for " + strSoldierName);
                                this.kickPlayer.Remove(strSoldierName);
                                WriteLog("Banning for PB Hack - " + strSoldierName + "(" + strGUID + ")", strSoldierName, strGUID);
                                break;
                            case "Perm Ban":
                                this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSoldierName, "perm", DateTime.Now.ToString() + " - PB Hack detected for " + strSoldierName);
                                this.kickPlayer.Remove(strSoldierName);
                                WriteLog("Banning for PB Hack - " + strSoldierName + "(" + strGUID + ")", strSoldierName, strGUID);
                                break;
                        }

                    }
                }
            }
        }
    }

}