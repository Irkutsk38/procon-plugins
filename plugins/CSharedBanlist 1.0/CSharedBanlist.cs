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

 2.1.0.0: Updated to IPRoConPluginInterface6 and added a "Hello World!" for
          the http server as an example.
 
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;
using PRoCon.Core.HttpServer;
using PRoCon.Core.Remote;

namespace PRoConEvents
{
    public class CSharedBanlist : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo;
        private Dictionary<string, CPlayerInfo> m_dicPlayerInfo;

        private List<CGlobalBan> m_lstProcessList;

        private List<CGlobalBan> m_lstLocalBanList;
        private List<CGlobalBan> m_lstServersBanList;

        private string m_strHost;
        private string m_strDatabase;
        private string m_strUserName;
        private string m_strPassword;

        private bool m_isPluginEnabled;

        public CSharedBanlist()
        {

            this.m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
            this.m_dicPlayerInfo = new Dictionary<string, CPlayerInfo>();

            this.m_lstProcessList = new List<CGlobalBan>();

            this.m_lstLocalBanList = new List<CGlobalBan>();
            this.m_lstServersBanList = new List<CGlobalBan>();

            this.m_isPluginEnabled = false;
        }

        public string GetPluginName()
        {
            return "Shared Banlist";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.0";
        }

        public string GetPluginAuthor()
        {
            return "Zaeed";
        }

        public string GetPluginWebsite()
        {
            return "aussieunderdogs.com";
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
<p>This plugin will share all bans across servers running within the same instance of procon, allowing for global banning of players.</p>


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
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bGlobal Banlist ^2Enabled!");

            BanlistUpdate();

            this.m_isPluginEnabled = true;
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bGlobal Banlist ^1Disabled =(");

            this.m_isPluginEnabled = false;
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

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
        }

        public void BanlistUpdate()
        {

            string path = Environment.CurrentDirectory + "\\Plugins\\Banlists";
            this.m_lstLocalBanList.Clear();

            #region Load bans from local text file

            try
            {
                if (Directory.Exists(path))
                {
                }
                else
                {
                    DirectoryInfo di = Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
            }
            try
            {
                if (File.Exists(path + "\\" + this.m_strHostName + "-Bans.txt"))
                {

                    try
                    {
                        //////  Read text file into global list
                        using (StreamReader sr = new StreamReader(path + "\\" + this.m_strHostName + "-Bans.txt"))
                        {
                            string line;
                            string strReason = "";
                            int length;
                            while ((line = sr.ReadLine()) != null)
                            {
                                string[] words = Regex.Split(line, "#");
                                length = words.Length;
                                if (length > 9)
                                {
                                    for (int i = 8; i < words.Length - 1; i++)
                                    {
                                        strReason = strReason + words[i];
                                    }
                                    CGlobalBan localBan = new CGlobalBan(words[0], words[1], words[2], words[3], words[4], words[5], words[6], words[7], strReason);
                                    this.m_lstLocalBanList.Add(localBan);
                                }
                                else
                                {
                                    CGlobalBan localBan = new CGlobalBan(words[0], words[1], words[2], words[3], words[4], words[5], words[6], words[7], words[8]);
                                    this.m_lstLocalBanList.Add(localBan);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^bGlobal Banlist: File read error1 " + e);
                    }
                }
                else
                {
                    // Make new ban file

                    using (StreamWriter sw = File.CreateText(path + "\\" + this.m_strHostName + "-Bans.txt"))
                    {
                        foreach (CGlobalBan newBanList in this.m_lstLocalBanList)
                        {
                            sw.WriteLine("{0}#{1}#{2}#{3}#{4}#{5}#{6}#{7}#{8}", newBanList.DateStamp, newBanList.Action, newBanList.SoldierName, newBanList.GUID, newBanList.IpAddress, newBanList.BanType, newBanList.TimeType, newBanList.BanLength, newBanList.BanReason);
                        }
                        sw.Close();
                    }
                }

                this.m_lstProcessList.Clear();


                foreach (CGlobalBan localBan in this.m_lstLocalBanList)
                {
                    if (this.m_lstServersBanList.Contains(localBan))
                    {
                    }
                    else
                    {
                        this.m_lstServersBanList.Add(localBan);
                        if (this.m_lstProcessList.Contains(localBan))
                        {
                        }
                        else
                        {
                            this.m_lstProcessList.Add(localBan);
                        }
                    }
                }

                foreach (CGlobalBan processBan in this.m_lstProcessList)
                {
                    switch (processBan.BanType)
                    {
                        case "name":
                            if (processBan.Action.CompareTo("Remove") == 0)
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", processBan.BanType, processBan.SoldierName);
                            }
                            else
                            {
                                if (processBan.TimeType.CompareTo("seconds") == 0)
                                {
                                    this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.SoldierName, processBan.TimeType, processBan.BanLength, processBan.BanReason);
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.SoldierName, processBan.TimeType, processBan.BanReason);
                                }
                            }
                            break;
                        case "ip":
                            if (processBan.Action.CompareTo("Remove") == 0)
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", processBan.BanType, processBan.SoldierName);
                            }
                            else
                            {
                                if (processBan.TimeType.CompareTo("seconds") == 0)
                                {
                                    this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.IpAddress, processBan.TimeType, processBan.BanLength, processBan.BanReason);
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.IpAddress, processBan.TimeType, processBan.BanReason);
                                }
                            }
                            break;
                        case "guid":
                            if (processBan.Action.CompareTo("Remove") == 0)
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", processBan.BanType, processBan.SoldierName);
                            }
                            else
                            {
                                if (processBan.TimeType.CompareTo("seconds") == 0)
                                {
                                    this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.GUID, processBan.TimeType, processBan.BanLength, processBan.BanReason);
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.GUID, processBan.TimeType, processBan.BanReason);
                                }
                            }
                            break;
                    }
                    DateTime checkDate = Convert.ToDateTime(processBan.DateStamp);
                    TimeSpan diff = DateTime.Now.Subtract(checkDate);

                    if ((processBan.Action.CompareTo("Remove") == 0) && (diff.Days > 7))
                    {
                        this.m_lstServersBanList.Remove(processBan);
                    }
                }

                this.m_lstProcessList.Clear();
            }
            catch (Exception d)
            {
            }

            #endregion


            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] rgFiles = dir.GetFiles("*.txt");
            foreach (FileInfo banFile in rgFiles)
            {
                if (banFile.Name != this.m_strHostName + "-Bans.txt")
                {

                    try
                    {
                        using (StreamReader sr = new StreamReader(banFile.FullName))
                        {
                            string line;
                            string strReason = "";
                            int length;
                            CGlobalBan remoteBan;
                            while ((line = sr.ReadLine()) != null)
                            {
                                string[] words = Regex.Split(line, "#");
                                length = words.Length;

                                if (length > 9)
                                {
                                    for (int i = 8; i < words.Length - 1; i++)
                                    {
                                        strReason = strReason + words[i];
                                    }
                                    remoteBan = new CGlobalBan(words[0], words[1], words[2], words[3], words[4], words[5], words[6], words[7], strReason);

                                }
                                else
                                {
                                    remoteBan = new CGlobalBan(words[0], words[1], words[2], words[3], words[4], words[5], words[6], words[7], words[8]);

                                }

                                if (this.m_lstServersBanList.Contains(remoteBan))
                                {
                                }
                                else
                                {
                                    this.m_lstServersBanList.Add(remoteBan);
                                    if (this.m_lstProcessList.Contains(remoteBan))
                                    {
                                    }
                                    else
                                    {
                                        this.m_lstProcessList.Add(remoteBan);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^bGlobal Banlist: File read error2 " + e);
                    }
                }
            }


            foreach (CGlobalBan processBan in this.m_lstProcessList)
            {
                switch (processBan.BanType)
                {
                    case "name":
                        if (processBan.Action.CompareTo("Remove") == 0)
                        {
                            this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", processBan.BanType, processBan.SoldierName);
                        }
                        else
                        {
                            if (processBan.TimeType.CompareTo("seconds") == 0)
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.SoldierName, processBan.TimeType, processBan.BanLength, processBan.BanReason);
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.SoldierName, processBan.TimeType, processBan.BanReason);
                            }
                        }
                        break;
                    case "ip":
                        if (processBan.Action.CompareTo("Remove") == 0)
                        {
                            this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", processBan.BanType, processBan.SoldierName);
                        }
                        else
                        {
                            if (processBan.TimeType.CompareTo("seconds") == 0)
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.IpAddress, processBan.TimeType, processBan.BanLength, processBan.BanReason);
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.IpAddress, processBan.TimeType, processBan.BanReason);
                            }
                        }
                        break;
                    case "guid":
                        if (processBan.Action.CompareTo("Remove") == 0)
                        {
                            this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.remove", processBan.BanType, processBan.SoldierName);
                        }
                        else
                        {
                            if (processBan.TimeType.CompareTo("seconds") == 0)
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.GUID, processBan.TimeType, processBan.BanLength, processBan.BanReason);
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.tasks.add", "RemoveBan", "0", "1", "1", "procon.protected.send", "banList.add", processBan.BanType, processBan.GUID, processBan.TimeType, processBan.BanReason);
                            }
                        }
                        break;
                }
            }
            this.m_lstProcessList.Clear();

            using (StreamWriter sw = File.CreateText(path + "\\" + this.m_strHostName + "-Bans.txt"))
            {
                foreach (CGlobalBan writeBan in this.m_lstServersBanList)
                {
                    sw.WriteLine("{0}#{1}#{2}#{3}#{4}#{5}#{6}#{7}#{8}", writeBan.DateStamp, writeBan.Action, writeBan.SoldierName, writeBan.GUID, writeBan.IpAddress, writeBan.BanType, writeBan.TimeType, writeBan.BanLength, writeBan.BanReason);
                }
                sw.Close();
            }
        }


        public void OnPunkbusterBanInfo(CBanInfo cbiPunkbusterBan)
        {
            if (cbiPunkbusterBan.Reason.StartsWith("BC2!"))
            {

            }
        }

        // Banning and Banlist Events
        public void OnBanList(List<CBanInfo> lstBans)
        {
            string subset, subset2;
            foreach (CBanInfo serverBans in lstBans)
            {
                subset2 = serverBans.BanLength.Subset.ToString();
                subset = "";
                if (subset2.CompareTo("Permanent") == 0)
                {
                    subset = "perm";
                }
                else if ((subset2.CompareTo("Round") == 0))
                {
                    subset = "round";
                }
                else if ((subset2.CompareTo("Seconds") == 0))
                {
                    subset = "seconds";
                }
                CGlobalBan banList = new CGlobalBan(DateTime.Now.ToString(), "Add", serverBans.SoldierName, serverBans.Guid, serverBans.IpAddress, serverBans.IdType, subset, serverBans.BanLength.Seconds.ToString(), serverBans.Reason);
                if (this.m_lstServersBanList.Contains(banList))
                {
                }
                else
                {
                    this.m_lstServersBanList.Add(banList);
                }
            }
        }

        public void OnBanAdded(CBanInfo cbiBan)
        {
            string subset, subset2;
            subset2 = cbiBan.BanLength.Subset.ToString();
            subset = "";
            if (subset2.CompareTo("Permanent") == 0)
            {
                subset = "perm";
            }
            else if ((subset2.CompareTo("Round") == 0))
            {
                subset = "round";
            }
            else if ((subset2.CompareTo("Seconds") == 0))
            {
                subset = "seconds";
            }
            CGlobalBan addBan = new CGlobalBan(DateTime.Now.ToString(), "Add", cbiBan.SoldierName, cbiBan.Guid, cbiBan.IpAddress, cbiBan.IdType, subset, cbiBan.BanLength.Seconds.ToString(), cbiBan.Reason);

            if (this.m_lstServersBanList.Contains(addBan))
            {
            }
            else
            {
                this.m_lstServersBanList.Add(addBan);
            }
        }

        public void OnBanRemoved(CBanInfo cbiUnban)
        {
            string subset, subset2;
            subset2 = cbiUnban.BanLength.Subset.ToString();
            subset = "";
            if (subset2.CompareTo("Permanent") == 0)
            {
                subset = "perm";
            }
            else if ((subset2.CompareTo("Round") == 0))
            {
                subset = "round";
            }
            else if ((subset2.CompareTo("Seconds") == 0))
            {
                subset = "seconds";
            }
            CGlobalBan removeBan = new CGlobalBan(DateTime.Now.ToString(), "Remove", cbiUnban.SoldierName, cbiUnban.Guid, cbiUnban.IpAddress, cbiUnban.IdType, subset, cbiUnban.BanLength.Seconds.ToString(), cbiUnban.Reason);
            if (this.m_lstServersBanList.Contains(removeBan))
            {
            }
            else
            {
                this.m_lstServersBanList.Add(removeBan);
            }
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

        public void OnLoadingLevel(string strMapFileName)
        {

        }

        public void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.ExecuteCommand("procon.protected.tasks.add", "GetBanListTask1", "0", "1", "1", "procon.protected.send", "banList.list");
            BanlistUpdate();
        }

        #region General functions not used


        private void UnregisterAllCommands()
        {
        }


        private void RegisterAllCommands()
        {

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

            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName) == false)
            {
                this.m_dicPlayerInfo.Add(strSoldierName, new CPlayerInfo(strSoldierName, "", 0, 24));
            }

        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {
            if (this.m_dicPbInfo.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPbInfo.Remove(strSoldierName);

            }

            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayerInfo.Remove(strSoldierName);
            }

            this.RegisterAllCommands();
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


        public void OnLevelStarted()
        {

        }

        public void OnPunkbusterMessage(string strPunkbusterMessage)
        {

        }


        public void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {

            if (cpbiPlayer != null)
            {
                if (this.m_dicPbInfo.ContainsKey(cpbiPlayer.SoldierName) == false)
                {
                    this.m_dicPbInfo.Add(cpbiPlayer.SoldierName, cpbiPlayer);
                }
                else
                {
                    this.m_dicPbInfo[cpbiPlayer.SoldierName] = cpbiPlayer;
                }

                this.RegisterAllCommands();
            }
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

            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (this.m_dicPlayerInfo.ContainsKey(cpiPlayer.SoldierName) == true)
                    {
                        this.m_dicPlayerInfo[cpiPlayer.SoldierName] = cpiPlayer;
                    }
                    else
                    {
                        this.m_dicPlayerInfo.Add(cpiPlayer.SoldierName, cpiPlayer);
                    }
                }
            }

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

        #region IPRoConPluginInterface4

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage, object trespassState) { }

        public void RegisterZoneTags(params string[] tags) { }

        public void UnregisterZoneTags(params string[] tags) { }

        #endregion

        #region IPRoConPluginInterface5

        public void OnRoundOverPlayers(List<CPlayerInfo> lstPlayers) { }

        #endregion

        #region IPRoConPluginInterface6

        public HttpWebServerResponseData OnHttpRequest(HttpWebServerRequestData data)
        {

            if (data.Query.Get("echo") != null)
            {
                return new HttpWebServerResponseData(data.Query.Get("echo"));
            }
            else
            {
                return new HttpWebServerResponseData("Hello World!");
            }
        }

        #endregion

        #endregion

        class CGlobalBan
        {
            private string _Action;
            private string _SoldierName;
            private string _GUID;
            private string _IpAddress;
            private string _BanType;
            private string _BanLength;
            private string _BanReason;
            private string _DateStamp;
            private string _TimeType;

            public CGlobalBan(string dateStamp, string action, string name, string guid, string ipaddress, string type, string timeType, string length, string reason)
            {
                _DateStamp = dateStamp;
                _Action = action;
                _SoldierName = name;
                _GUID = guid;
                _IpAddress = ipaddress;
                _BanType = type;
                _BanLength = length;
                _BanReason = reason;
                _TimeType = timeType;
            }

            public override bool Equals(object obj)
            {
                CGlobalBan other = obj as CGlobalBan;

                if (this.BanType.CompareTo(other.BanType) == 0)
                {
                    switch (other.BanType)
                    {
                        case "name":
                            if ((this.Action.CompareTo(other.Action) == 0) && (this.SoldierName.CompareTo(other.SoldierName) == 0))
                            {
                                return true;
                            }
                            return false;
                        case "ip":
                            if ((this.Action.CompareTo(other.Action) == 0) && (this.IpAddress.CompareTo(other.IpAddress) == 0))
                            {
                                return true;
                            }
                            return false;
                        case "guid":
                            if ((this.Action.CompareTo(other.Action) == 0) && (this.GUID.CompareTo(other.GUID) == 0))
                            {
                                return true;
                            }
                            return false;
                        default:
                            return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            public string DateStamp
            {
                get { return _DateStamp; }
                set { _DateStamp = value; }
            }
            public string TimeType
            {
                get { return _TimeType; }
                set { _TimeType = value; }
            }
            public string Action
            {
                get { return _Action; }
                set { _Action = value; }
            }
            public string SoldierName
            {
                get { return _SoldierName; }
                set { _SoldierName = value; }
            }
            public string GUID
            {
                get { return _GUID; }
                set { _GUID = value; }
            }
            public string IpAddress
            {
                get { return _IpAddress; }
                set { _IpAddress = value; }
            }
            public string BanType
            {
                get { return _BanType; }
                set { _BanType = value; }
            }
            public string BanLength
            {
                get { return _BanLength; }
                set { _BanLength = value; }
            }
            public string BanReason
            {
                get { return _BanReason; }
                set { _BanReason = value; }
            }
        }
    }
}