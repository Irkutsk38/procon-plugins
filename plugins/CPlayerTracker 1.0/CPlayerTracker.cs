/*  Copyright 2010 [GWC]XpKillerhx (Christian S.)

    This plugin file is part of BFBC2 PRoCon.

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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Odbc;
using System.Configuration;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;

//Procon includes
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

namespace PRoConEvents
{
    public class CPlayerTracker : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Variables and Constructor
        //Proconvariables
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        //Logging
        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
        //Chatlog
        private static List<CLogger> ChatLog = new List<CLogger>();
        //Statslog
        private Dictionary<string, CStats> StatsTracker = new Dictionary<string, CStats>();

        // Timelogging
        private bool bool_roundStarted;
        private DateTime Time_RankingStarted;

        //Other
        private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();   //Players

        //ID Cache
        private Dictionary<string, C_ID_Cache> m_ID_cache = new Dictionary<string, C_ID_Cache>();

        //Various Variables
        private int m_strUpdateInterval;
        private bool isStreaming;
        private string serverName;
        private bool m_isPluginEnabled;
        private bool boolTableEXISTS;
        private int m_iDisplayTime;
        private string tableSuffix;
        private bool ODBC_Connection_is_activ;

        //Database Connection Variables
        private string m_strHost;
        private string m_strDBPort;
        private string m_strDatabase;
        private string m_strUserName;
        private string m_strPassword;

        //Bools for switch on and off funktions
        private enumBoolYesNo m_enNoServerMsg;  //Logging of Server Messages
        private enumBoolYesNo m_enDebugMode;        //Debug Mode
        private enumBoolYesNo m_enInstantChatlogging;   //Realtime Chatlogging
        private enumBoolYesNo m_UpdateEA_GUID;      //Update EA_GUID

        //More Database Variables
        //Commands
        private System.Data.Odbc.OdbcCommand OdbcComChat;
        private System.Data.Odbc.OdbcCommand OdbcCom;
        private System.Data.Odbc.OdbcCommand OdbcComm;
        //Transactions
        private System.Data.Odbc.OdbcTransaction OdbcTrans;
        //Connections
        private System.Data.Odbc.OdbcConnection OdbcCon; //instant Chatlog and Select Querys 1
        private System.Data.Odbc.OdbcConnection OdbcConn; //StartStreaming  2 
                                                          //Reader
        private System.Data.Odbc.OdbcDataReader OdbcDR;


        public CPlayerTracker()
        {
            this.m_strUpdateInterval = 30;
            this.isStreaming = true;
            this.serverName = "";
            this.m_iDisplayTime = 3000;
            this.m_ID_cache = new Dictionary<string, C_ID_Cache>();
            this.tableSuffix = "";
            this.ODBC_Connection_is_activ = false;

            this.m_strHost = "";
            this.m_strDBPort = "";
            this.m_strDatabase = "";
            this.m_strUserName = "";
            this.m_strPassword = "";

            this.m_isPluginEnabled = false;
            this.boolTableEXISTS = false;

            this.m_enDebugMode = enumBoolYesNo.No;
            this.m_enNoServerMsg = enumBoolYesNo.No;
            this.m_enInstantChatlogging = enumBoolYesNo.No;
            this.m_UpdateEA_GUID = enumBoolYesNo.No;

        }
        #endregion

        #region PluginSetup
        public string GetPluginName()
        {
            return "PRoCon Simple Playertracker";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.0";
        }

        public string GetPluginAuthor()
        {
            return "[GWC]XpKiller";
        }

        public string GetPluginWebsite()
        {
            return "www.german-wildcards.de";
        }

        public string GetPluginDescription()
        {
            return @"
 If you like my Plugins, please feel free to donate<br>
<p><form action='https://www.paypal.com/cgi-bin/webscr' method='post'>
<input type='hidden' name='cmd' value='_s-xclick'>
<input type='hidden' name='hosted_button_id' value='VW7CR5B8ZQ7S6'>
<input type='image' src='https://www.paypal.com/en_US/i/btn/btn_donate_LG.gif' border='0' name='submit' alt='PayPal - The safer, easier way to pay online!'>
<img alt='' border='0' src='https://www.paypal.com/de_DE/i/scr/pixel.gif' width='1' height='1'></p>
</form>

   
<h2>Description</h2>
    <p>This plugin is used to log Player chat and other general Playerinfos like GUID's .</p>
    <p>This inludes: Chat, Playername, PBGUID, EAGUID, IP, Country, ClanTag </p>
    <p>Data is written at the end of a Round</p>
    
<h2>Requirements</h2>
	<p>It reqiues the use of a MySQL database with INNODB engine that allows remote connections.(MYSQL Version 5.1.x or higher is recommendend!!!)</p>
	<p>You will also need to download the MySQL ODBC 5.1 Driver(the latest is 5.1.7(24.08.2010)), and enable ODBC connections in the procon options.</p>
	<p>The Plugin will create the tables by itself.</p>
	<p>Pls Give FEEDBACK !!!</p>

<h2>Installation</h2>
<p>Download and install this plugin</p>
<p>Download and install the <a href='http://www.mysql.com/downloads/connector/odbc/' target='_blank'>MYSQL ODBC Connector 5.1 Driver (the latest is 5.1.7(24.08.2010))</a></p>
<p>Setup your Database this means create a database and the user for it. I highly recommend NOT to use your root user. Just create a user with all rights for your newly created database </p>
<p>I recommend MySQL 5.1.x or greater (5.0.x should work too) Important: <b>Your database need INNODB Support</b></p>
<p>Start Procon</p>
<p>Go to Tools --> Options --> Plugins --> Enter you databaseserver under outgoing Connections and allow all outgoing connections</p>
<p>Restart Procon</p>
<p>Enter your settings into Plugin Settings and THEN enable the plugin</p>
<p>Now the plugin should work if not request help in the <a href='http://phogue.net/forum/viewtopic.php?f=18&t=694' target='_blank'>Forum</a></p>

	
<h2>Things you have to know:</h2>
Now you can have more than one server per database if you use the tableSuffix feature, if you dont want to use it keep this field blank.<br>


<h2>Ingame Commands</h2>
	<blockquote><h4>--None--</blockquote>

<h3>Known issues:</h3>
<p>--none--</p>
	
	
<h3>Changelog:</h3><br>
<b>1.0.0.0 </b><br>
First Release<br>


	
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerJoin", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnPunkbusterPlayerInfo", "OnServerInfo",
                                                     "OnPlayerLeft", "OnRoundOverPlayers", "OnLoadingLevel");
        }

        public void OnPluginEnable()
        {

            isStreaming = true;
            this.serverName = "";
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon Simple Playertracker ^2Enabled");
            // Register Commands
            this.m_isPluginEnabled = true;
            this.RegisterAllCommands();


        }

        public void OnPluginDisable()
        {
            isStreaming = false;
            if (OdbcCon != null)
                if (OdbcCon.State == ConnectionState.Open)
                {
                    OdbcCon.Close();
                }
            if (OdbcConn != null)
                if (OdbcConn.State == ConnectionState.Open)
                {
                    OdbcConn.Close();
                }

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon Simple Playertracker ^1Disabled");

            //Unregister Commands
            this.m_isPluginEnabled = false;
            this.UnregisterAllCommands();
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Server Details|Host", this.m_strHost.GetType(), this.m_strHost));
            lstReturn.Add(new CPluginVariable("Server Details|Port", this.m_strDBPort.GetType(), this.m_strDBPort));
            lstReturn.Add(new CPluginVariable("Server Details|Database Name", this.m_strDatabase.GetType(), this.m_strDatabase));
            lstReturn.Add(new CPluginVariable("Server Details|UserName", this.m_strUserName.GetType(), this.m_strUserName));
            lstReturn.Add(new CPluginVariable("Server Details|Password", this.m_strPassword.GetType(), this.m_strPassword));
            lstReturn.Add(new CPluginVariable("Chatlogging|Log ServerSPAM?", typeof(enumBoolYesNo), this.m_enNoServerMsg));
            lstReturn.Add(new CPluginVariable("Chatlogging|Instant Logging of Chat Messages?", typeof(enumBoolYesNo), this.m_enInstantChatlogging));
            lstReturn.Add(new CPluginVariable("Stats|Update EA GUID?", typeof(enumBoolYesNo), this.m_UpdateEA_GUID));
            lstReturn.Add(new CPluginVariable("Debug|Debugmode on?", typeof(enumBoolYesNo), this.m_enDebugMode));
            lstReturn.Add(new CPluginVariable("Table|tableSuffix", this.tableSuffix.GetType(), this.tableSuffix));
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Host", this.m_strHost.GetType(), this.m_strHost));
            lstReturn.Add(new CPluginVariable("Port", this.m_strDBPort.GetType(), this.m_strDBPort));
            lstReturn.Add(new CPluginVariable("Database Name", this.m_strDatabase.GetType(), this.m_strDatabase));
            lstReturn.Add(new CPluginVariable("UserName", this.m_strUserName.GetType(), this.m_strUserName));
            lstReturn.Add(new CPluginVariable("Password", this.m_strPassword.GetType(), this.m_strPassword));
            // Switch for Stats Logging
            lstReturn.Add(new CPluginVariable("Log ServerSPAM?", typeof(enumBoolYesNo), this.m_enNoServerMsg));
            lstReturn.Add(new CPluginVariable("Instant Logging of Chat Messages?", typeof(enumBoolYesNo), this.m_enInstantChatlogging));
            lstReturn.Add(new CPluginVariable("Update EA GUID?", typeof(enumBoolYesNo), this.m_UpdateEA_GUID));
            lstReturn.Add(new CPluginVariable("Debugmode on?", typeof(enumBoolYesNo), this.m_enDebugMode));
            lstReturn.Add(new CPluginVariable("tableSuffix", this.tableSuffix.GetType(), this.tableSuffix));
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {

            if (strVariable.CompareTo("Host") == 0)
            {
                this.m_strHost = strValue;
            }
            else if (strVariable.CompareTo("Port") == 0)
            {
                this.m_strDBPort = strValue;
            }
            else if (strVariable.CompareTo("Database Name") == 0)
            {
                this.m_strDatabase = strValue;
            }
            else if (strVariable.CompareTo("UserName") == 0)
            {
                this.m_strUserName = strValue;
            }
            else if (strVariable.CompareTo("Password") == 0)
            {
                this.m_strPassword = strValue;
            }
            else if (strVariable.CompareTo("Log ServerSPAM?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enNoServerMsg = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Instant Logging of Chat Messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enInstantChatlogging = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Update EA GUID?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_UpdateEA_GUID = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Debugmode on?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enDebugMode = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("tableSuffix") == 0)
            {
                this.tableSuffix = strValue;
            }
            this.RegisterAllCommands();

        }

        private List<string> GetExcludedCommandStrings(string strAccountName)
        {

            List<string> lstReturnCommandStrings = new List<string>();

            List<MatchCommand> lstCommands = this.GetRegisteredCommands();

            CPrivileges privileges = this.GetAccountPrivileges(strAccountName);

            foreach (MatchCommand mtcCommand in lstCommands)
            {

                if (mtcCommand.Requirements.HasValidPermissions(privileges) == true && lstReturnCommandStrings.Contains(mtcCommand.Command) == false)
                {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }

            return lstReturnCommandStrings;
        }

        private List<string> GetCommandStrings()
        {

            List<string> lstReturnCommandStrings = new List<string>();

            List<MatchCommand> lstCommands = this.GetRegisteredCommands();

            foreach (MatchCommand mtcCommand in lstCommands)
            {

                if (lstReturnCommandStrings.Contains(mtcCommand.Command) == false)
                {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }

            return lstReturnCommandStrings;
        }

        private void UnregisterAllCommands()
        {
        }

        private void SetupHelpCommands()
        {

        }

        private void RegisterAllCommands()
        {

        }
        #endregion

        #region IPRoConPluginInterface
        /*=======ProCon Events========*/


        // Player events
        public override void OnPlayerJoin(string strSoldierName)
        {
            if (this.StatsTracker.ContainsKey(strSoldierName) == false)
            {
                CStats newEntry = new CStats("", 0, 0, 0, 0, 0, 0, 0);
                StatsTracker.Add(strSoldierName, newEntry);
            }
            if (StatsTracker.ContainsKey(strSoldierName) == true)
            {
                if (StatsTracker[strSoldierName].PlayerOnServer == false)
                {
                    if (this.StatsTracker[strSoldierName].TimePlayerjoined == null)
                    {
                        this.StatsTracker[strSoldierName].TimePlayerjoined = DateTime.Now;
                    }
                    this.StatsTracker[strSoldierName].Playerjoined = DateTime.Now;
                    this.StatsTracker[strSoldierName].PlayerOnServer = true;
                }
            }

        }


        // Will receive ALL chat global/team/squad in R3.
        public override void OnGlobalChat(string strSpeaker, string strMessage)
        {
            this.LogChat(strSpeaker, strMessage, "Global");
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public override void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            this.LogChat(strSpeaker, strMessage, "Team");
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public override void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            this.LogChat(strSpeaker, strMessage, "Squad");
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {

            this.RegisterAllCommands();
            try
            {
                this.AddPBInfoToStats(cpbiPlayer);
                if (this.StatsTracker.ContainsKey(cpbiPlayer.SoldierName))
                {
                    if (this.StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined == null)
                    {
                        this.StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined = DateTime.Now;
                    }
                    this.StatsTracker[cpbiPlayer.SoldierName].IP = cpbiPlayer.Ip;
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnPunkbusterPlayerInfo: " + c);
            }

        }

        // Query Events
        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.serverName = csiServerInfo.ServerName;
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            try
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

                    if (this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == false)
                    {
                        CStats newEntry = new CStats("", 0, 0, 0, 0, 0, 0, 0);
                        StatsTracker.Add(cpiPlayer.SoldierName, newEntry);
                    }
                    //Timelogging
                    if (this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
                    {
                        if (this.StatsTracker[cpiPlayer.SoldierName].PlayerOnServer == false)
                        {
                            this.StatsTracker[cpiPlayer.SoldierName].Playerjoined = DateTime.Now;
                            this.StatsTracker[cpiPlayer.SoldierName].PlayerOnServer = true;
                        }
                        //EA-GUID, ClanTag, usw.
                        this.StatsTracker[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
                        this.StatsTracker[cpiPlayer.SoldierName].ClanTag = cpiPlayer.ClanTag;
                    }
                    //ID - Cache
                    if (this.m_ID_cache.ContainsKey(cpiPlayer.SoldierName))
                    {
                        this.m_ID_cache[cpiPlayer.SoldierName].PlayeronServer = true;
                    }
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnListPlayers: " + c);
            }
        }

        public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {
            new Thread(StartStreaming).Start();
            m_dicPlayers.Clear();
        }

        #endregion

        #region CChatGUIDStatsLogger Methodes 


        public void playerLeftServer(CPlayerInfo cpiPlayer)
        {
            if (this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
            {
                this.StatsTracker[cpiPlayer.SoldierName].Score = cpiPlayer.Score;
                this.StatsTracker[cpiPlayer.SoldierName].TimePlayerleft = DateTime.Now;
                this.StatsTracker[cpiPlayer.SoldierName].playerleft();
                //EA-GUID, ClanTag, usw.
                this.StatsTracker[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
                this.StatsTracker[cpiPlayer.SoldierName].ClanTag = cpiPlayer.ClanTag;
            }
            //ID cache System
            if (this.m_ID_cache.ContainsKey(cpiPlayer.SoldierName) == true)
            {
                this.m_ID_cache[cpiPlayer.SoldierName].PlayeronServer = false;
            }

        }

        public List<string> SQLquery(string str_selectSQL, int sort)
        {
            List<string> query = new List<string>();
            int int_counter = 0;
            string strRow = "";
            if ((m_strHost != null) || (m_strDatabase != null) || (m_strDBPort != null) || (m_strUserName != null) || (m_strPassword != null))
            {
                try
                {
                    this.ODBC_Connection_is_activ = true;
                    this.OpenOdbcConnection(1);
                    OdbcParameter param = new OdbcParameter();
                    if (OdbcCon.State == ConnectionState.Open)
                    {
                        //Reader
                        using (OdbcComm = new System.Data.Odbc.OdbcCommand(str_selectSQL, OdbcCon))
                        {
                            OdbcDR = OdbcComm.ExecuteReader();

                            // i known it is a work around got trouble return an array
                            switch (sort)
                            {
                                case 3:
                                    query = new List<string>();
                                    query.Add("0");
                                    while (OdbcDR.Read())
                                    {
                                        if (OdbcDR[0].ToString() != null)
                                        {
                                            query[0] = OdbcDR[0].ToString();
                                        }
                                        else
                                        {
                                            query[0] = "0";
                                        }
                                    }
                                    break;

                                case 8:
                                    query = new List<string>();
                                    query.Add("0");
                                    while (OdbcDR.Read())
                                    {
                                        query = new List<string>();
                                        query.Add(OdbcDR[0].ToString());
                                    }

                                    break;

                                default:
                                    query = new List<string>();
                                    query.Add("Error: No data");
                                    break;
                            }

                        }
                    }
                }

                catch (Exception c)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in SQLQuery: " + c);
                    if (OdbcConn.State == ConnectionState.Open)
                    {
                        OdbcCon.Close();
                    }
                    this.ODBC_Connection_is_activ = false;
                }


            }
            this.ODBC_Connection_is_activ = false;
            return query;
        }

        public int GetID(string strSoldierName)
        {
            string CHECK = "SELECT `PlayerID` FROM tbl_playerinfo" + this.tableSuffix + @" WHERE SoldierName ='" + strSoldierName + "'";
            int playerID;
            List<string> result;
            try
            {
                if (this.m_ID_cache.ContainsKey(strSoldierName))
                {
                    if (this.m_ID_cache[strSoldierName].Id != 0)
                    {

                        playerID = this.m_ID_cache[strSoldierName].Id;
                        //this.DebugInfo("Status ID-Cache: used ID from cache "+ playerID);
                    }
                    else
                    {
                        result = new List<string>(this.SQLquery(CHECK, 3));
                        playerID = Convert.ToInt32(result[0]);
                        //this.DebugInfo("Received ID from Database ID: "+ playerID);
                    }
                }
                else
                {
                    result = new List<string>(this.SQLquery(CHECK, 3));
                    playerID = Convert.ToInt32(result[0]);
                    if (playerID >= 1)
                    {
                        this.DebugInfo("Received ID from Database ID: " + playerID + "Added to cache");
                        C_ID_Cache AddID = new C_ID_Cache(playerID, true);
                        //this.m_ID_cache.Add(strSoldierName,AddID);
                    }
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "Error GetID: " + c);
                playerID = -1;
            }

            return playerID;


        }
        // Updates database with player stats and chatlogs
        public void StartStreaming()
        {
            //Make a copy of Statstracker to prevent unwanted errors
            Dictionary<string, CStats> StatsTrackerCopy = new Dictionary<string, CStats>(this.StatsTracker);
            int icharindex;
            int int_id = 0;
            //Clearing the old Dictionary
            StatsTracker.Clear();
            // Uploads chat logs and Stats for round to database
            this.tablebuilder(); //Build the tables if not exists
            if ((m_strHost != null) && (m_strDatabase != null) && (m_strDBPort != null) && (m_strUserName != null) && (m_strPassword != null))
            {
                try
                {
                    this.ODBC_Connection_is_activ = true;
                    OdbcParameter param = new OdbcParameter();
                    this.OpenOdbcConnection(2);

                    if (ChatLog.Count > 0 && OdbcConn.State == ConnectionState.Open)
                    {
                        string ChatSQL = @"INSERT INTO tbl_chatlog" + this.tableSuffix + @" (logDate, logServer, logSubset, logSoldierName, logMessage) 
													VALUES ";


                        lock (ChatLog)
                        {
                            foreach (CLogger log in ChatLog)
                            {
                                ChatSQL = string.Concat(ChatSQL, "(?,?,?,?,?),");
                            }
                            ChatSQL = ChatSQL.Remove(ChatSQL.LastIndexOf(","));
                            using (OdbcCommand OdbcCom = new OdbcCommand(ChatSQL, OdbcConn))
                            {
                                foreach (CLogger log in ChatLog)
                                {
                                    OdbcCom.Parameters.AddWithValue("@pr", log.Time);
                                    OdbcCom.Parameters.AddWithValue("@pr", this.serverName);
                                    OdbcCom.Parameters.AddWithValue("@pr", log.Subset);
                                    OdbcCom.Parameters.AddWithValue("@pr", log.Name);
                                    OdbcCom.Parameters.AddWithValue("@pr", log.Message);
                                }
                                OdbcCom.ExecuteNonQuery();
                            }
                            ChatLog.Clear();
                        }
                    }


                    if (OdbcConn.State == ConnectionState.Open)
                    {
                        OdbcTrans = OdbcConn.BeginTransaction();
                        foreach (KeyValuePair<string, CStats> kvp in StatsTrackerCopy)
                        {
                            if (kvp.Key.Length > 0 && StatsTrackerCopy[kvp.Key].Guid.Length > 0)
                            {
                                int_id = GetID(kvp.Key);//Call of the ID Cache
                                if (int_id >= 1)
                                {
                                    string UpdatedataSQL = "";
                                    if (this.m_UpdateEA_GUID == enumBoolYesNo.Yes)
                                    {
                                        UpdatedataSQL = @"UPDATE tbl_playerinfo" + this.tableSuffix + @" SET ClanTag = ?, EAGUID = ?, IP_Address = ?, CountryCode = ?  WHERE PlayerID = ?";
                                    }
                                    else
                                    {
                                        UpdatedataSQL = @"UPDATE tbl_playerinfo" + this.tableSuffix + @" SET ClanTag = ?, IP_Address = ?, CountryCode = ? WHERE PlayerID = ?";
                                    }
                                    using (OdbcCommand OdbcCom = new OdbcCommand(UpdatedataSQL, OdbcConn, OdbcTrans))
                                    {
                                        //Insert
                                        OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].ClanTag);
                                        if (this.m_UpdateEA_GUID == enumBoolYesNo.Yes)
                                        {
                                            OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].EAGuid);
                                        }
                                        OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].IP);
                                        OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].PlayerCountryCode);
                                        OdbcCom.Parameters.AddWithValue("@pr", int_id);
                                        OdbcCom.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    string InsertdataSQL = @"INSERT INTO tbl_playerinfo" + this.tableSuffix + @" (ClanTag, SoldierName, GUID, EAGUID, IP_Address, CountryCode) VALUES(?,?,?,?,?,?)";
                                    using (OdbcCommand OdbcCom = new OdbcCommand(InsertdataSQL, OdbcConn, OdbcTrans))
                                    {
                                        //Insert
                                        OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].ClanTag);
                                        OdbcCom.Parameters.AddWithValue("@pr", kvp.Key);
                                        OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Guid);
                                        OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].EAGuid);
                                        OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].IP);
                                        OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].PlayerCountryCode);
                                        OdbcCom.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        OdbcTrans.Commit();
                    }
                }
                catch (Exception c)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error: " + c);
                    OdbcTrans.Rollback();
                    this.m_ID_cache.Clear();
                }

                finally
                {
                    this.ODBC_Connection_is_activ = false;
                    this.CloseOdbcConnection(1);
                    this.CloseOdbcConnection(2);
                }

            }
            else
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Streaming cancelled.  Please enter all database information");
            }
        }

        public void AddPBInfoToStats(CPunkbusterInfo cpbiPlayer)
        {
            if (StatsTracker.ContainsKey(cpbiPlayer.SoldierName))
            {
                StatsTracker[cpbiPlayer.SoldierName].Guid = cpbiPlayer.GUID;
                StatsTracker[cpbiPlayer.SoldierName].PlayerCountryCode = cpbiPlayer.PlayerCountryCode;
                if (StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined == null)
                    StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined = DateTime.Now;
            }
            else
            {
                CStats newEntry = new CStats(cpbiPlayer.GUID, 0, 0, 0, 0, 0, 0, 0);
                StatsTracker.Add(cpbiPlayer.SoldierName, newEntry);
                StatsTracker[cpbiPlayer.SoldierName].PlayerCountryCode = cpbiPlayer.PlayerCountryCode;
            }

        }

        public void OpenOdbcConnection(int type)
        {
            try
            {
                switch (type)
                {
                    //OdbcCon
                    case 1:
                        if (OdbcCon == null)
                        {
                            OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                   "SERVER=" + m_strHost + ";" +
                                                                   "PORT=" + m_strDBPort + ";" +
                                                                   "DATABASE=" + m_strDatabase + ";" +
                                                                   "UID=" + m_strUserName + ";" +
                                                                   "PWD=" + m_strPassword + ";" +
                                                                   "OPTION=3;");
                        }
                        if (OdbcCon.State == ConnectionState.Closed)
                        {
                            OdbcCon = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                   "SERVER=" + m_strHost + ";" +
                                                                   "PORT=" + m_strDBPort + ";" +
                                                                   "DATABASE=" + m_strDatabase + ";" +
                                                                   "UID=" + m_strUserName + ";" +
                                                                   "PWD=" + m_strPassword + ";" +
                                                                   "OPTION=3;");
                            OdbcCon.Open();
                            this.DebugInfo("OdbcCon open");
                        }
                        break;
                    //ODBCConn
                    case 2:
                        if (OdbcConn == null)
                        {
                            OdbcConn = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                   "SERVER=" + m_strHost + ";" +
                                                                   "PORT=" + m_strDBPort + ";" +
                                                                   "DATABASE=" + m_strDatabase + ";" +
                                                                   "UID=" + m_strUserName + ";" +
                                                                   "PWD=" + m_strPassword + ";" +
                                                                   "OPTION=3;");
                        }
                        if (OdbcConn.State == ConnectionState.Closed)
                        {
                            OdbcConn = new System.Data.Odbc.OdbcConnection("DRIVER={MySQL ODBC 5.1 Driver};" +
                                                                   "SERVER=" + m_strHost + ";" +
                                                                   "PORT=" + m_strDBPort + ";" +
                                                                   "DATABASE=" + m_strDatabase + ";" +
                                                                   "UID=" + m_strUserName + ";" +
                                                                   "PWD=" + m_strPassword + ";" +
                                                                   "OPTION=3;");
                            OdbcConn.Open();
                            this.DebugInfo("OdbcConn open");
                        }
                        break;

                    default:
                        break;
                }

            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OpenConnection: " + c);
            }
        }

        public void CloseOdbcConnection(int type)
        {
            if (this.ODBC_Connection_is_activ == false)
            {
                try
                {
                    switch (type)
                    {
                        case 1:
                            //OdbcCon
                            if (this.OdbcCon != null)
                                if (this.OdbcCon.State == ConnectionState.Open)
                                {
                                    this.OdbcCon.Close();
                                    this.DebugInfo("Connection OdbcCon closed");
                                }
                            break;

                        case 2:
                            //ODBCConn
                            if (this.OdbcConn != null)
                                if (this.OdbcConn.State == ConnectionState.Open)
                                {
                                    this.OdbcConn.Close();
                                    this.DebugInfo("Connection OdbcConn closed");
                                }
                            break;
                        default:
                            break;
                    }

                }
                catch (Exception c)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in CloseOdbcConnection: " + c);
                }

            }
        }

        public void tablebuilder()
        {

            if ((m_strHost != null) && (m_strDatabase != null) && (m_strDBPort != null) && (m_strUserName != null) && (m_strPassword != null) && (boolTableEXISTS == false))
            {
                this.DebugInfo("Start tablebuilder");

                try
                {
                    this.ODBC_Connection_is_activ = true;
                    OdbcParameter param = new OdbcParameter();
                    this.OpenOdbcConnection(1);
                    //Chatlog Table
                    string SQLTable = @"CREATE TABLE IF NOT EXISTS `tbl_chatlog" + this.tableSuffix + @"` (
                            					`ID` INT NOT NULL AUTO_INCREMENT ,
  												`logDate` DATETIME NULL DEFAULT NULL ,
  												`logServer` TEXT NULL DEFAULT NULL ,
  												`logSubset` TEXT NULL DEFAULT NULL ,
  												`logSoldierName` TEXT NULL DEFAULT NULL ,
  												`logMessage` TEXT NULL DEFAULT NULL ,
  													PRIMARY KEY (`ID`) )
													ENGINE = MyISAM
													DEFAULT CHARACTER SET = latin1";
                    using (OdbcCommand OdbcCom = new OdbcCommand(SQLTable, OdbcCon))
                    {
                        OdbcCom.ExecuteNonQuery();
                    }

                    //Start of the Transaction
                    OdbcTrans = OdbcCon.BeginTransaction();

                    //Table playerdata
                    SQLTable = @"CREATE TABLE IF NOT EXISTS `tbl_playerinfo" + this.tableSuffix + @"` (
  												`PlayerID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  												`ClanTag` varchar(45) DEFAULT NULL,
  												`SoldierName` varchar(16) DEFAULT NULL,
  												`GUID` varchar(32) DEFAULT NULL,
 												`EAGUID` varchar(35) DEFAULT NULL,
 												`IP_Address` varchar(15) DEFAULT NULL,
  												`CountryCode` varchar(4) DEFAULT NULL,
  												PRIMARY KEY (`PlayerID`),
  												UNIQUE KEY `UNIQUE_playerdata` (`SoldierName`,`GUID`))
  												ENGINE = InnoDB DEFAULT CHARACTER SET = latin1";
                    using (OdbcCommand OdbcCom = new OdbcCommand(SQLTable, OdbcCon, OdbcTrans))
                    {
                        OdbcCom.ExecuteNonQuery();
                    }

                    //Commit the Transaction
                    OdbcTrans.Commit();
                    this.boolTableEXISTS = true;
                }
                catch (Exception c)
                {
                    OdbcTrans.Rollback();
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error: " + c);
                    this.boolTableEXISTS = false;
                    this.m_ID_cache.Clear();
                    if (OdbcCon.State == ConnectionState.Open)
                    {
                        OdbcCon.Close();
                    }
                }
                this.ODBC_Connection_is_activ = false;
            }
        }

        public void LogChat(string strSpeaker, string strMessage, string strType)
        {
            if (this.m_enNoServerMsg == enumBoolYesNo.No && strSpeaker.CompareTo("Server") == 0)
            {
            }
            else if (m_enInstantChatlogging == enumBoolYesNo.Yes)
            {
                string query = "INSERT INTO tbl_chatlog" + this.tableSuffix + @" (logDate, logServer, logSubset, logSoldierName, logMessage) VALUES (?,?,?,?,?)";
                this.tablebuilder();
                if ((m_strHost != null) || (m_strDatabase != null) || (m_strDBPort != null) || (m_strUserName != null) || (m_strPassword != null))
                {
                    try
                    {
                        this.ODBC_Connection_is_activ = true;
                        this.OpenOdbcConnection(1);
                        OdbcParameter param = new OdbcParameter();
                        if (OdbcCon.State == ConnectionState.Open)
                        {
                            using (OdbcCommand OdbcCom = new OdbcCommand(query, OdbcCon))
                            {
                                OdbcCom.Parameters.AddWithValue("@pr", DateTime.Now);
                                OdbcCom.Parameters.AddWithValue("@pr", this.serverName);
                                OdbcCom.Parameters.AddWithValue("@pr", strType);
                                OdbcCom.Parameters.AddWithValue("@pr", strSpeaker);
                                OdbcCom.Parameters.AddWithValue("@pr", strMessage);
                                OdbcCom.ExecuteNonQuery();
                            }
                            this.CloseOdbcConnection(1);
                        }

                    }
                    catch (Exception c)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in LogChat: " + c);
                        if (OdbcCon.State == ConnectionState.Open)
                        {
                            OdbcCon.Close();
                        }
                        this.ODBC_Connection_is_activ = false;
                    }
                    this.ODBC_Connection_is_activ = false;

                }

            }
            else
            {
                CLogger chat = new CLogger(DateTime.Now, strSpeaker, strMessage, strType);
                ChatLog.Add(chat);
            }
        }

        public ArrayList TextFileReader(string textfile)
        {
            StreamReader objReader = new StreamReader(textfile);
            string sline = "";
            ArrayList arrText = new ArrayList();

            while (sline != null)
            {
                sline = objReader.ReadLine();
                if (sline != null)
                {
                    arrText.Add(sline);
                }

            }
            objReader.Close();
            return arrText;
        }

        public void DebugInfo(string DebugMessage)
        {
            if (m_enDebugMode == enumBoolYesNo.Yes)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", DebugMessage);
            }
        }

    }

    #endregion

    #region Classes
    /*==========Classes========*/
    class CLogger
    {
        private readonly string _Name;
        private string _Message = "";
        private string _Subset = "";
        private DateTime _Time;

        public string Name
        {
            get { return _Name; }
        }

        public string Message
        {
            get { return _Message; }
        }

        public string Subset
        {
            get { return _Subset; }
        }

        public DateTime Time
        {
            get { return _Time; }
        }

        public CLogger(DateTime time, string name, string message, string subset)
        {
            _Name = name;
            _Message = message;
            _Subset = subset;
            _Time = time;
        }
    }

    class CStats
    {
        private string _ClanTag;
        private string _Guid;
        private string _EAGuid;
        private string _IP;
        private string _PlayerCountryCode;
        private int _Score = 0;
        private int _LastScore = 0;
        private int _Kills = 0;
        private int _Headshots = 0;
        private int _Deaths = 0;
        private int _Suicides = 0;
        private int _Teamkills = 0;
        private int _Playtime = 0;
        private DateTime _Playerjoined;
        private DateTime _TimePlayerleft;
        private DateTime _TimePlayerjoined;
        private int _PlayerleftServerScore = 0;
        private bool _playerOnServer = false;
        private int _rank = 0;
        //Streaks
        private int _Killstreak;
        private int _Deathstreak;
        private int _Killcount;
        private int _Deathcount;

        public Dictionary<string, CUsedWeapon> dicWeap = new Dictionary<string, CUsedWeapon>();


        public string ClanTag
        {
            get { return _ClanTag; }
            set { _ClanTag = value; }
        }

        public string Guid
        {
            get { return _Guid; }
            set { _Guid = value; }
        }

        public string EAGuid
        {
            get { return _EAGuid; }
            set { _EAGuid = value; }
        }

        public string IP
        {
            get { return _IP; }
            set { _IP = value.Remove(value.IndexOf(":")); }
        }


        public string PlayerCountryCode
        {
            get { return _PlayerCountryCode; }
            set { _PlayerCountryCode = value; }
        }

        public int Score
        {
            get { return _Score; }
            set { _Score = value; }
        }

        public int LastScore
        {
            get { return _LastScore; }
            set { _LastScore = value; }
        }

        public int Kills
        {
            get { return _Kills; }
            set { _Kills = value; }
        }

        public int Headshots
        {
            get { return _Headshots; }
            set { _Headshots = value; }
        }

        public int Deaths
        {
            get { return _Deaths; }
            set { _Deaths = value; }
        }

        public int Suicides
        {
            get { return _Suicides; }
            set { _Suicides = value; }
        }

        public int Teamkills
        {
            get { return _Teamkills; }
            set { _Teamkills = value; }
        }

        public int Playtime
        {
            get { return _Playtime; }
            set { _Playtime = value; }
        }

        public DateTime Playerjoined
        {
            get { return _Playerjoined; }
            set { _Playerjoined = value; }
        }

        public DateTime TimePlayerleft
        {
            get { return _TimePlayerleft; }
            set { _TimePlayerleft = value; }
        }

        public DateTime TimePlayerjoined
        {
            get { return _TimePlayerjoined; }
            set { _TimePlayerjoined = value; }
        }

        public int PlayerleftServerScore
        {
            get { return _PlayerleftServerScore; }
            set { _PlayerleftServerScore = value; }
        }

        public bool PlayerOnServer
        {
            get { return _playerOnServer; }
            set { _playerOnServer = value; }
        }

        public int Rank
        {
            get { return _rank; }
            set { _rank = value; }
        }

        public int Killstreak
        {
            get { return _Killstreak; }
            set { _Killstreak = value; }
        }

        public int Deathstreak
        {
            get { return _Deathstreak; }
            set { _Deathstreak = value; }
        }

        //Methodes	
        public void AddScore(int intScore)
        {
            if (intScore != 0)
            {
                this._Score = this._Score + (intScore - this._LastScore);
                this._LastScore = intScore;
            }
            else
            {
                this._LastScore = 0;
            }
        }

        public double KDR()
        {
            double ratio = 0;
            if (this._Deaths != 0)
            {
                ratio = Math.Round(Convert.ToDouble(this._Kills) / Convert.ToDouble(this._Deaths), 2);
            }
            else
            {
                ratio = this._Kills;
            }
            return ratio;
        }

        public Dictionary<string, CUsedWeapon> getWeaponKills()
        {
            return this.dicWeap;
        }

        public void addKill(string strweaponType, bool blheadshot)
        {
            //Start of the convert block
            strweaponType = strweaponType.Replace(" ", "");
            if ((String.Equals(strweaponType, "")) || (String.Equals(strweaponType, " ")))
            {
                strweaponType = "UNKNOWN";
            }
            if (strweaponType.Contains("#"))
            {
                int intindex = strweaponType.IndexOf("#");
                strweaponType = strweaponType.Remove(intindex);
            }
            strweaponType = strweaponType.ToUpper();
            //End of the convert block

            if (this.dicWeap.ContainsKey(strweaponType))
            {
                if (blheadshot)
                {
                    this.dicWeap[strweaponType].Kills++;
                    this.dicWeap[strweaponType].Headshots++;
                    this._Kills++;
                    this._Headshots++;
                }
                else
                {
                    this.dicWeap[strweaponType].Kills++;
                    this._Kills++;
                }
            }
            else
            {
                if (blheadshot)
                {
                    CUsedWeapon killinfo = new CUsedWeapon(1, 1, 0);
                    this.dicWeap.Add(strweaponType, killinfo);
                    this._Kills++;
                    this._Headshots++;
                }
                else
                {
                    CUsedWeapon killinfo = new CUsedWeapon(1, 0, 0);
                    this.dicWeap.Add(strweaponType, killinfo);
                    this._Kills++;
                }
            }
            //Killstreaks
            this._Killcount++;
            this._Deathcount = 0;
            if (this._Killcount > this._Killstreak)
            {
                this._Killstreak = this._Killcount;
            }


        }

        public void addDeath(string strweaponType)
        {
            //Start of the convert block
            strweaponType = strweaponType.Replace(" ", "");
            if ((String.Equals(strweaponType, "")) || (String.Equals(strweaponType, " ")))
            {
                strweaponType = "UNKNOWN";
            }
            if (strweaponType.Contains("#"))
            {
                int intindex = strweaponType.IndexOf("#");
                strweaponType = strweaponType.Remove(intindex);
            }
            strweaponType = strweaponType.ToUpper();
            //End of the convert block

            if (this.dicWeap.ContainsKey(strweaponType))
            {
                this.dicWeap[strweaponType].Deaths++;
                this._Deaths++;
            }
            else
            {
                CUsedWeapon deathinfo = new CUsedWeapon(0, 0, 1);
                this.dicWeap.Add(strweaponType, deathinfo);
                this._Deaths++;
            }

            //Deathstreak
            this._Deathcount++;
            this._Killcount = 0;
            if (this._Deathcount > this._Deathstreak)
            {
                this._Deathstreak = this._Deathcount;
            }
        }

        public void playerleft()
        {
            //Score
            this._PlayerleftServerScore += this._Score;
            this._Score = 0;
            //Time
            TimeSpan duration = DateTime.Now - this._Playerjoined;
            this._Playtime += Convert.ToInt32(duration.TotalSeconds);
            this._playerOnServer = false;
        }

        public int TotalScore
        {
            get { return (this._PlayerleftServerScore + this._Score); }
        }

        public int TotalPlaytime
        {
            get
            {
                if (this._playerOnServer)
                {
                    TimeSpan duration = DateTime.Now - this._Playerjoined;
                    this._Playtime += Convert.ToInt32(duration.TotalSeconds);
                }
                return this._Playtime;
            }
        }

        public class CUsedWeapon
        {
            private int _Kills = 0;
            private int _Headshots = 0;
            private int _Deaths = 0;

            public int Kills
            {
                get { return _Kills; }
                set { _Kills = value; }
            }

            public int Headshots
            {
                get { return _Headshots; }
                set { _Headshots = value; }
            }

            public int Deaths
            {
                get { return _Deaths; }
                set { _Deaths = value; }
            }

            public CUsedWeapon(int kills, int headshots, int deaths)
            {
                this._Kills = kills;
                this._Headshots = headshots;
                this._Deaths = deaths;
            }

        }

        public CStats(string guid, int score, int kills, int headshots, int deaths, int suicides, int teamkills, int playtime)
        {
            this._ClanTag = String.Empty;
            this._Guid = guid;
            this._EAGuid = String.Empty;
            this._IP = String.Empty;
            this._Score = score;
            this._LastScore = 0;
            this._Kills = kills;
            this._Headshots = headshots;
            this._Deaths = deaths;
            this._Suicides = suicides;
            this._Teamkills = teamkills;
            this._Playtime = playtime;
            this._PlayerleftServerScore = 0;
            this._PlayerCountryCode = String.Empty;
            this._TimePlayerjoined = DateTime.Now;
            this._TimePlayerleft = DateTime.MinValue;
            this._rank = 0;
            this._Killcount = 0;
            this._Killstreak = 0;
            this._Deathcount = 0;
            this._Deathstreak = 0;

        }

    }

    class C_ID_Cache
    {
        private int _Id;
        private bool _PlayeronServer;

        public int Id
        {
            get { return _Id; }
            set { _Id = value; }
        }

        public bool PlayeronServer
        {
            get { return _PlayeronServer; }
            set { _PlayeronServer = value; }
        }

        //Constructor
        public C_ID_Cache(int id, bool playeronServer)
        {
            this._Id = id;
            this._PlayeronServer = playeronServer;
        }

    }

    class CKillerVictim
    {
        string _Killer;
        string _Victim;


        public string Killer
        {
            get { return _Killer; }
            set { _Killer = value; }
        }

        public string Victim
        {
            get { return _Victim; }
            set { _Victim = value; }
        }

        public CKillerVictim(string killer, string victim)
        {
            this._Killer = killer;
            this._Victim = victim;
        }

    }

    #endregion
}