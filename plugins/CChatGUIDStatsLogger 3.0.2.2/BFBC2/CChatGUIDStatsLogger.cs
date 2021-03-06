/*  Copyright 2010 [GWC]XpKillerhx

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
using System.Net;

//Procon includes
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

namespace PRoConEvents {
    public class CChatGUIDStatsLogger : PRoConPluginAPI, IPRoConPluginInterface  {
		#region Variables and Constructor
		//Proconvariables
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;
        
        //Dateoffset
        private myDateTime_W MyDateTime;
        private double m_dTimeOffset;
        
		//Logging
        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
        //Chatlog
        private static List<CLogger> ChatLog = new List<CLogger>();
        //Statslog
        private Dictionary<string, CStats> StatsTracker = new Dictionary<string, CStats>();
        //Dogtags
        private Dictionary<CKillerVictim, int> m_dicKnifeKills = new Dictionary<CKillerVictim, int>();
        //Session
        private Dictionary<string, CStats> m_dicSession = new Dictionary<string, CStats>();
        private CMapstats Mapstats;
        private CMapstats Nextmapinfo;
        
        //GameMod
        private string m_strGameMod;
        
        //Spamprotection
        private int numberOfAllowedRequests;
        private CSpamprotection Spamprotection;
        
        //Keywords
        private List<string> m_lstTableconfig = new List<string>();
        private List<string> m_lstTableschema = new List<string>();
        private List<string> m_lstTableconfig_bc2 = new List<string>();
        private List<string> m_lstTableschema_bc2 = new List<string>();
        private List<string> m_lstTableconfig_bfv = new List<string>();
        private List<string> m_lstTableschema_bfv = new List<string>();
        private Dictionary<string, List<string>> m_dicKeywords = new Dictionary<string, List<string>>();
        
        //Tablenames
        private string tbl_playerdata;
        private string tbl_playerstats;
        private string tbl_weaponstats;
        private string tbl_dogtags;
        private string tbl_mapstats;
        private string tbl_chatlog;
        private string tbl_bfbcs;
        
        
        // Timelogging
       	private bool bool_roundStarted;
       	private DateTime Time_RankingStarted;
       	
		//Other
		private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();   //Players
		
		//ID Cache
		private Dictionary<string,C_ID_Cache> m_ID_cache = new Dictionary<string, C_ID_Cache>();
		
		//Various Variables
        private int m_strUpdateInterval;
        private bool isStreaming;
        private string serverName;
        private bool m_isPluginEnabled;
        private bool boolTableEXISTS;
		private int m_iDisplayTime;
		private bool boolKeywordDicReady;
		private string tableSuffix;
		private bool ODBC_Connection_is_activ;
		
		//BFBCS
		private double BFBCS_UpdateInterval;
        private int BFBCS_Min_Request;
		
        //Database Connection Variables
        private string m_strHost;
        private string m_strDBPort;
        private string m_strDatabase;
        private string m_strUserName;
        private string m_strPassword;
        
        //Stats Message Variables        
        private List<string> m_lstPlayerStatsMessage;
        private List<string> m_lstWeaponstatsMsg;
        private string m_strPlayerWelcomeMsg;
        private string m_strNewPlayerWelcomeMsg;
        private int int_welcomeStatsDelay;
		//Session
		private List<string> m_lstSessionMessage;
		
		//Cheaterprotection
		private double m_dMaxAllowedKDR;
		private double m_dMaxScorePerMinute;
		private double m_dminimumPlaytime; // hours
		private string m_strRemoveMethode;
		private string m_strReasonMsg;
        
        //Bools for switch on and off funktions
        private enumBoolYesNo m_enNoServerMsg;	//Logging of Server Messages
        private enumBoolYesNo m_enLogSTATS; 	//Statslogging
		private enumBoolYesNo m_enWelcomeStats;	//WelcomeStats
		private enumBoolYesNo m_enYellWelcomeMSG;	// Yell Welcome Message
		private enumBoolYesNo m_enTop10ingame;		//Top10 ingame
		private enumBoolYesNo m_enDebugMode;		//Debug Mode
		private enumBoolYesNo m_enRankingByScore;	//Ranking by Score
		private enumBoolYesNo m_enInstantChatlogging;	//Realtime Chatlogging
		private enumBoolYesNo m_enChatloggingON;	// Chatlogging On
		private enumBoolYesNo m_enSendStatsToAll;	//All Player see the Stats if someone enter @stats  @rank
		private enumBoolYesNo m_mapstatsON;			//Mapstats
		private enumBoolYesNo m_sessionON; 			//Sessionstats
		private enumBoolYesNo m_UpdateEA_GUID; 		//Update EA_GUID
		private enumBoolYesNo m_UpdatePB_GUID;		//Upate PB_GUID NOT recommended
		private enumBoolYesNo m_weaponstatsON;		//Turn Weaponstats On and Off
		private enumBoolYesNo m_getStatsfromBFBCS;  //Turn Statsfetching from BFBCS On and Off
		private enumBoolYesNo m_cheaterProtection; // Turn Statschecks On or Off
		
	
        //More Database Variables
        //Commands
        private System.Data.Odbc.OdbcCommand OdbcComChat;
        private System.Data.Odbc.OdbcCommand OdbcCom;
        private System.Data.Odbc.OdbcCommand OdbcComm;
        //Transactions
        private System.Data.Odbc.OdbcTransaction OdbcTrans;
        //Connections
        private System.Data.Odbc.OdbcConnection OdbcCon; //instant Chatlog and Select Querys 1
        private System.Data.Odbc.OdbcConnection OdbcConn; //StartStreaming and InstantKillLogging 2 
        //Reader
		private System.Data.Odbc.OdbcDataReader OdbcDR;
        
        public CChatGUIDStatsLogger()
        {
        	//Timeoffset
          	this.m_dTimeOffset = 0;
          	this.MyDateTime = new myDateTime_W(this.m_dTimeOffset);
        	
            this.m_strUpdateInterval = 30;
            this.isStreaming = true;
            this.serverName = "";
            this.m_iDisplayTime = 3000;
            this.m_ID_cache = new Dictionary<string, C_ID_Cache>();
            this.m_dicKeywords = new Dictionary<string, List<string>>();
            this.boolKeywordDicReady = false;
            this.tableSuffix = "";
          	this.Mapstats = new CMapstats(MyDateTime.Now,"START",0,0,this.m_dTimeOffset);
          	this.ODBC_Connection_is_activ = false;
          	this.numberOfAllowedRequests = 10;
          	
          	//BFBCS
          	this.BFBCS_UpdateInterval = 72; // hours
          	this.BFBCS_Min_Request = 2; //min Packrate
          	
          	//Cheaterprotection
          	this.m_dMaxAllowedKDR = 5;
          	this.m_dMaxScorePerMinute = 500;
          	this.m_dminimumPlaytime = 10;
          	this.m_strRemoveMethode = "Warn";
          	this.m_strReasonMsg = "%SoldierName% has been kicked for violating KDR or SPM Limits!";
			
          	//Databasehost
            this.m_strHost = "";
            this.m_strDBPort ="";
            this.m_strDatabase = "";
            this.m_strUserName = "";
            this.m_strPassword = "";
			
            //Various Bools
            this.bool_roundStarted = false;
            this.m_isPluginEnabled = false;
            this.boolTableEXISTS = false;
           
            //Functionswitches
            this.m_enLogSTATS = enumBoolYesNo.No;
			this.m_enWelcomeStats = enumBoolYesNo.No;
			this.m_enYellWelcomeMSG = enumBoolYesNo.No;
			this.m_enTop10ingame = enumBoolYesNo.No;
			this.m_enDebugMode = enumBoolYesNo.No;
			this.m_enRankingByScore = enumBoolYesNo.No;
			this.m_enNoServerMsg = enumBoolYesNo.No;
			this.m_enInstantChatlogging = enumBoolYesNo.No;
			this.m_enChatloggingON = enumBoolYesNo.No;
			this.m_enSendStatsToAll = enumBoolYesNo.No;
			this.m_mapstatsON = enumBoolYesNo.No;
			this.m_sessionON = enumBoolYesNo.No;
			this.m_UpdateEA_GUID = enumBoolYesNo.No;
			this.m_UpdatePB_GUID = enumBoolYesNo.No;
			this.m_weaponstatsON = enumBoolYesNo.Yes;
			this.m_getStatsfromBFBCS = enumBoolYesNo.No;
			this.m_cheaterProtection = enumBoolYesNo.No;

			//Welcomestats
			this.m_strPlayerWelcomeMsg = "Nice to see you on our Server again, %playerName%";
			this.m_strNewPlayerWelcomeMsg ="Welcome to the %serverName% Server, %playerName%";
			this.int_welcomeStatsDelay = 60;
			
			//Playerstats
			this.m_lstPlayerStatsMessage = new List<string>();
			this.m_lstPlayerStatsMessage.Add("Serverstats for %playerName%:");
			this.m_lstPlayerStatsMessage.Add("Score: %playerScore%  %playerKills% Kills %playerHeadshots% HS  %playerDeaths% Deaths K/D: %playerKDR%");
			this.m_lstPlayerStatsMessage.Add("Your Serverrank is: %playerRank% of %allRanks%");
			
			//Weaponstats
			this.m_lstWeaponstatsMsg = new List<string>();
			this.m_lstWeaponstatsMsg.Add("%playerName%'s Stats for %Weapon%:");
			this.m_lstWeaponstatsMsg.Add("%playerKills% Kills  %playerHeadshots% Headshots  Headshotrate: %playerKHR%%  %playerDeaths% died by this Weapon");
			this.m_lstWeaponstatsMsg.Add("Your Weaponrank is: %playerRank% of %allRanks%");
			
			//Session
			this.m_lstSessionMessage = new List<string>();
			this.m_lstSessionMessage.Add("%playerName%'s Session Data  Session started %SessionStarted%");
			this.m_lstSessionMessage.Add("Score: %playerScore%  %playerKills% Kills  %playerHeadshots% HS  %playerDeaths% Deaths K/D: %playerKDR%");
			this.m_lstSessionMessage.Add("Your Rank: %playerRank% (%RankDif%)  Sessionlength: %SessionDuration% Minutes");
			
			//GameMod
       		this.m_strGameMod = "BC2";
			
			//Tableconfig BFBC2
			this.m_lstTableconfig_bc2.Add("//YOU WILL NEED TO START AND STOP THE PLUGIN SO THAT CHANGES IN THIS SSETTINGS WILL TAKE EFFECT");
			this.m_lstTableconfig_bc2.Add("//USE SLASHES FOR COMMENTS ONLY OTHERWISE STRANGE ERROR WILL APPEAR");
			this.m_lstTableconfig_bc2.Add("//DONT CHANGE THE FIRST WORD ADD WORDS TO THE BRACKETS ONLY, IF THERE ARE NO BRACKETS U CAN ADD THEM");
			this.m_lstTableconfig_bc2.Add("//USE UPPERCASE ONLY!!!");
			this.m_lstTableconfig_bc2.Add("//ASSAULT");
			this.m_lstTableconfig_bc2.Add("AEK971{AEK-971}");
			this.m_lstTableconfig_bc2.Add("XM8{XM8P,XM8-P}");
			this.m_lstTableconfig_bc2.Add("F2000{F-2000}");
			this.m_lstTableconfig_bc2.Add("AUG");
			this.m_lstTableconfig_bc2.Add("AN94{AN-94}");
			this.m_lstTableconfig_bc2.Add("M416{M-416}");
			this.m_lstTableconfig_bc2.Add("M16{M-16,M16A2}");
			this.m_lstTableconfig_bc2.Add("M16K{M-16K,M16SA}");
			this.m_lstTableconfig_bc2.Add("40MMGL{NOOBTUBE,GL,40MM,GRENADELAUNCHER}");
			this.m_lstTableconfig_bc2.Add("40MMSG{40MMSHOTGUN}");
			this.m_lstTableconfig_bc2.Add("40MMSMK{SMOKELAUNCHER,SMOKE}");
			this.m_lstTableconfig_bc2.Add("//ENGINEER");
			this.m_lstTableconfig_bc2.Add("9A91{9A-91}");
			this.m_lstTableconfig_bc2.Add("SCAR{SCAR-L,SCARL}");
			this.m_lstTableconfig_bc2.Add("XM8C{COMPACT,XM8-C,XM8COMPACT}");
			this.m_lstTableconfig_bc2.Add("AKS74U{AKS-74U,AKS74,AKS-74,AKS}");
			this.m_lstTableconfig_bc2.Add("UZI");
			this.m_lstTableconfig_bc2.Add("PP2000{PP-2000}");
			this.m_lstTableconfig_bc2.Add("UMP{UMP-45,UMP45}");
			this.m_lstTableconfig_bc2.Add("UMPK{UMP-45K,UMP45SA}");
			this.m_lstTableconfig_bc2.Add("//Repairtool");
			this.m_lstTableconfig_bc2.Add("PWR-700{REPAIRTOOL,REPAIR,REPAIR-TOOL,PWR}");
			this.m_lstTableconfig_bc2.Add("//AT-Rockets");
			this.m_lstTableconfig_bc2.Add("RPG7{RPG-7,RPG}");
			this.m_lstTableconfig_bc2.Add("M2CG{HOTCARL,GUSTAV,CG,CARL,CARLGUSTAV,M2}");
			this.m_lstTableconfig_bc2.Add("M136{AT4,M136AT4}");
			this.m_lstTableconfig_bc2.Add("//AT-Mine");
			this.m_lstTableconfig_bc2.Add("ATM-00{ATMINE,AT-MINE,ANTITANKMINE}");
			this.m_lstTableconfig_bc2.Add("//MEDIC");
			this.m_lstTableconfig_bc2.Add("PKM");
			this.m_lstTableconfig_bc2.Add("M249{SAW,M249SAW}");
			this.m_lstTableconfig_bc2.Add("QJU88{T88LMG}");
			this.m_lstTableconfig_bc2.Add("M60");
			this.m_lstTableconfig_bc2.Add("XM8LMG");
			this.m_lstTableconfig_bc2.Add("MG36");
			this.m_lstTableconfig_bc2.Add("MG3");
			this.m_lstTableconfig_bc2.Add("MG3K{MG3SA}");
			this.m_lstTableconfig_bc2.Add("DEFIB{DEFI}");
			this.m_lstTableconfig_bc2.Add("//SNIPER");
			this.m_lstTableconfig_bc2.Add("M24{M-24}");
			this.m_lstTableconfig_bc2.Add("QBU88{T88SNIPER}");
			this.m_lstTableconfig_bc2.Add("SV98{SV-98}");
			this.m_lstTableconfig_bc2.Add("SVU");
			this.m_lstTableconfig_bc2.Add("GOL{MAGNUMSNIPER}");
			this.m_lstTableconfig_bc2.Add("VSS");
			this.m_lstTableconfig_bc2.Add("M95");
			this.m_lstTableconfig_bc2.Add("M95K{M95SA,M95K}");
			this.m_lstTableconfig_bc2.Add("//Mortar");
			this.m_lstTableconfig_bc2.Add("MRTR-5{MORTAR}");
			this.m_lstTableconfig_bc2.Add("//PISTOLS");
			this.m_lstTableconfig_bc2.Add("M9");
			this.m_lstTableconfig_bc2.Add("M1911{COLT,M-1911}");
			this.m_lstTableconfig_bc2.Add("MP443{MP-443}");
			this.m_lstTableconfig_bc2.Add("MP412{MP-412,REX}");
			this.m_lstTableconfig_bc2.Add("M9-3{M93R}");
			this.m_lstTableconfig_bc2.Add("//ALLKITS");
			this.m_lstTableconfig_bc2.Add("//SHOTGUN");
			this.m_lstTableconfig_bc2.Add("870MCS{COMBATSHOTGUN}");
			this.m_lstTableconfig_bc2.Add("NS2000{NEOSTEAD,NS-2000}");
			this.m_lstTableconfig_bc2.Add("SPAS12{SPAS,SPAS-12}");
			this.m_lstTableconfig_bc2.Add("S20K{SAIGA}");
			this.m_lstTableconfig_bc2.Add("USAS12{USAS12,USAS-12,USAS}");
			this.m_lstTableconfig_bc2.Add("//SMG");
			this.m_lstTableconfig_bc2.Add("M1A1THOMPSON{THOMPSON,M1A1}");
			this.m_lstTableconfig_bc2.Add("//RIFLE");
			this.m_lstTableconfig_bc2.Add("MK14EBR{M14,M14EBR}");
			this.m_lstTableconfig_bc2.Add("G3");
			this.m_lstTableconfig_bc2.Add("GARAND{M1}");
			this.m_lstTableconfig_bc2.Add("//EXPLOSIVES");
			this.m_lstTableconfig_bc2.Add("DTN-4{C4,C-4}");
			this.m_lstTableconfig_bc2.Add("HG-2{GRANATE, HANDGRANATE,GRENADE,HANDGRENADE,HANDGND}");
			this.m_lstTableconfig_bc2.Add("//KNIFE");
			this.m_lstTableconfig_bc2.Add("KNV-1{KNIFE,MESSER,COMBATKNIFE,KAMPFMESSER}");
			this.m_lstTableconfig_bc2.Add("//VEHCLES");
			this.m_lstTableconfig_bc2.Add("ROADKILL");
			this.m_lstTableconfig_bc2.Add("ZU23{ANTIAIR}");
			this.m_lstTableconfig_bc2.Add("KORN");
			this.m_lstTableconfig_bc2.Add("TOW2{TOW}");
			this.m_lstTableconfig_bc2.Add("X312");
			this.m_lstTableconfig_bc2.Add("KORD");
			this.m_lstTableconfig_bc2.Add("VADS{MINIGUN,AAMINIGUN,AA-MINIGUN,VULCAN}");
			this.m_lstTableconfig_bc2.Add("X307");
			this.m_lstTableconfig_bc2.Add("PBLB{BOAT,BOOT,PATROLBOAT}");
			this.m_lstTableconfig_bc2.Add("//HELIS");
			this.m_lstTableconfig_bc2.Add("MI28{HAVOC,MI-28}");
			this.m_lstTableconfig_bc2.Add("MI24{HIND,MI-24}");
			this.m_lstTableconfig_bc2.Add("UAV1{UAV,DROHNE}");
			this.m_lstTableconfig_bc2.Add("AH60{BLACKHAWK,AH-60}");
			this.m_lstTableconfig_bc2.Add("AH64{APACHE,AH-64}");
			this.m_lstTableconfig_bc2.Add("KA52{ALLIGATOR,HOKUM-B,KA-52,HOKUM}");
			this.m_lstTableconfig_bc2.Add("//JEEPS QUADS");
			this.m_lstTableconfig_bc2.Add("COBR{COBRA}");
			this.m_lstTableconfig_bc2.Add("HUMV{HUMVEE,HMMWV}");
			this.m_lstTableconfig_bc2.Add("CAVJ");
			this.m_lstTableconfig_bc2.Add("VODN");
			this.m_lstTableconfig_bc2.Add("//TANKS");
			this.m_lstTableconfig_bc2.Add("T90R{T90,T90MBT}");
			this.m_lstTableconfig_bc2.Add("M1A2{ABRAMS}");
			this.m_lstTableconfig_bc2.Add("//APC");
			this.m_lstTableconfig_bc2.Add("BMD3");
			this.m_lstTableconfig_bc2.Add("M3A3{BRADLEY}");
			this.m_lstTableconfig_bc2.Add("//AA-Tanks");
			this.m_lstTableconfig_bc2.Add("BMDA");
			this.m_lstTableconfig_bc2.Add("ACV-S{ANTIAIRBRADLEY}");
			this.m_lstTableconfig_bc2.Add("//DESTRUCTION 2.0");
			this.m_lstTableconfig_bc2.Add("D2.0{DESTRUCTION,DESTRUCTION2.0}");
			this.m_lstTableconfig_bc2.Add("//UNKNOWN");
			this.m_lstTableconfig_bc2.Add("UNKNOWN");
			this.m_lstTableconfig_bc2.Add("COAXMG");
			this.m_lstTableconfig_bc2.Add("MP7");
			this.m_lstTableconfig_bc2.Add("QLZ8");
			this.m_lstTableschema_bc2 = new List<string>(this.m_lstTableconfig_bc2);
			
			//BFBC2 Vietnam
			this.m_lstTableconfig_bfv.Add("//ASSAULT");
			this.m_lstTableconfig_bfv.Add("M16A1V{M16A1}");
			this.m_lstTableconfig_bfv.Add("AK47V{AK47,AK-47}");
			this.m_lstTableconfig_bfv.Add("M79V{M79}");
			this.m_lstTableconfig_bfv.Add("M14V{M14}");
			this.m_lstTableconfig_bfv.Add("M2V{M2}");			
			this.m_lstTableconfig_bfv.Add("//ENGINEER");
			this.m_lstTableconfig_bfv.Add("MAC10V{MAC10,MAC-10}");
			this.m_lstTableconfig_bfv.Add("PPSHV{PPSH}");
			this.m_lstTableconfig_bfv.Add("UZIV{UZI}");
			this.m_lstTableconfig_bfv.Add("ATM-00{ATMINE,AT-MINE,ANTITANKMINE}");
			this.m_lstTableconfig_bfv.Add("RPG7{RPG-7}");
			this.m_lstTableconfig_bfv.Add("TORCHV{TORCH}");			
			this.m_lstTableconfig_bfv.Add("//MEDIC");
			this.m_lstTableconfig_bfv.Add("M60V{M60}");
			this.m_lstTableconfig_bfv.Add("RPKV{RPK}");
			this.m_lstTableconfig_bfv.Add("XM22V{XM22}");
			this.m_lstTableconfig_bfv.Add("SYRINGEV{SYRINGE}");			
			this.m_lstTableconfig_bfv.Add("//SNIPER");
			this.m_lstTableconfig_bfv.Add("M21V{M21}");
			this.m_lstTableconfig_bfv.Add("SVDV{SVD}");
			this.m_lstTableconfig_bfv.Add("M40V{M40}");
			this.m_lstTableconfig_bfv.Add("TNTV{TNT}");
			this.m_lstTableconfig_bfv.Add("MORTARV{MORTAR}");			
			this.m_lstTableconfig_bfv.Add("//PISTOLS");
			this.m_lstTableconfig_bfv.Add("M1911");
			this.m_lstTableconfig_bfv.Add("TT33V{T33V}");
			this.m_lstTableconfig_bfv.Add("//HANDGRANATE");
			this.m_lstTableconfig_bfv.Add("HG-2{GRANATE, HANDGRANATE,GRENADE,HANDGRENADE,HANDGND}");
			this.m_lstTableconfig_bfv.Add("//KNIFE");
			this.m_lstTableconfig_bfv.Add("KNV-1{KNIFE,MESSER,COMBATKNIFE,KAMPFMESSER}");			
			this.m_lstTableconfig_bfv.Add("//ALLKITS");
			this.m_lstTableconfig_bfv.Add("FLAMETHROWER");
			this.m_lstTableconfig_bfv.Add("GARAND{M1}");
			this.m_lstTableconfig_bfv.Add("//SHOTGUN");
			this.m_lstTableconfig_bfv.Add("870MCS");			
			this.m_lstTableconfig_bfv.Add("//SMG");
			this.m_lstTableconfig_bfv.Add("M1A1THOMPSON");			
			this.m_lstTableconfig_bfv.Add("//VEHCLES");
			this.m_lstTableconfig_bfv.Add("//LIGHT VEHCLES");
			this.m_lstTableconfig_bfv.Add("M151V{M151}");			
			this.m_lstTableconfig_bfv.Add("ROADKILL");
			this.m_lstTableconfig_bfv.Add("//HELIS");
			this.m_lstTableconfig_bfv.Add("HUEYV{UH-1,HUEY}");	
			this.m_lstTableconfig_bfv.Add("//TANKS");
			this.m_lstTableconfig_bfv.Add("T54V{T54}");
			this.m_lstTableconfig_bfv.Add("M48V{M48}");
			this.m_lstTableconfig_bfv.Add("//Boat");
			this.m_lstTableconfig_bfv.Add("PBRV{PATROLBOAT,BOAT}");
			this.m_lstTableconfig_bfv.Add("//UNKNOWN");
			this.m_lstTableconfig_bfv.Add("UNKNOWN");
			this.m_lstTableconfig_bfv.Add("G69V");
			this.m_lstTableconfig_bfv.Add("//DESTRUCTION 2.0");
			this.m_lstTableconfig_bfv.Add("D2.0{DESTRUCTION,DESTRUCTION2.0}");
			this.m_lstTableschema_bfv = new List<string>(this.m_lstTableconfig_bfv);
        }
        #endregion
        
		#region PluginSetup
        public string GetPluginName()
        {
            return "BFBC2 Chat, GUID, Stats and Map Logger";
        }

        public string GetPluginVersion()
        {
            return "3.0.2.2";
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
    <p>This plugin is used to log player chat, player GUID's und player Stats and Weaponstats and Mapstats.</p>
    <p>This inludes: Chat, PBGUID, EAGUID, IP, Stats, Weaponstats, Dogtags, Killstreaks, Country, ClanTag to be continued.. ;-)</p>
    
<h2>Requirements</h2>
	<p>It reqiues the use of a MySQL database with INNODB engine that allows remote connections.(MYSQL Version 5.1.x or higher is recommendend!!!)</p>
	<p>Also you should give INNODB some more Ram because the plugin mainly uses this engine if you need help ask me</p>
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
<p>Select the GameMod (BC2 / VIETNAM / Shared) Default is BC2 or let the plugin detect the GameMod </p>
<p>Enter your settings into Plugin Settings and THEN enable the plugin</p>
<p>Now the plugin should work if not request help in the <a href='http://phogue.net/forum/viewtopic.php?f=18&t=694' target='_blank'>Forum</a></p>

	
<h2>Things you have to know:</h2>
You need to drop your old tables or use the mysql script in the zip file<br>
Now you can have more than one server per database if you use the tableSuffix feature, if you dont want to use it keep this field blank.<br>
You can add additional Names for weapons in the Pluginsettings
But only use UpperCase, no Spaces and comma to seperate the words.<br>
Example: 40MMGL{NOOBTUBE} --> 40MMGL{NOOBTUBE,GL,TUBE}<br>
Dont change the first word!!! only these in brackets if there are no brackets you can add them.<br><br>



<h2>Ingame Commands</h2>
	<blockquote><h4>[@,#,!]stats</h4>Tells the Player their own Serverstats</blockquote>
	<blockquote><h4>[@,#,!]rank</h4>Tells the Player their own Serverstats</blockquote>
	<blockquote><h4>[@,#,!]session</h4>Tells the Player their own Sessiondata</blockquote>
	<blockquote><h4>[@,#,!]top10</h4>Tells the Player the Top10 players of the server</blockquote>
	<blockquote><h4>[@,#,!]stats WeaponName</h4>Tells the Player their own Weaponstats for the specific Weapon</blockquote>
	<blockquote><h4>[@,#,!]rank WeaponName</h4>Privately Tells the Player their own Weaponstats for the specific Weapon</blockquote>
	<blockquote><h4>[@,#,!]top10 WeaponName</h4>Privately Tells the Player the Top10 Player for the specific Weapon of the server</blockquote>
	<blockquote><h4>[@,#,!]dogtags WeaponName</h4>Privately Tells the Player his Dogtagstats </blockquote>

<h2>Replacement String for Playerstats</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%playerName%</td><td>Will be replaced by the player's name</td></tr>
	<tr><td>%playerScore%</td><td>Will be replaced by the player's totalscore on this server</td></tr>
	<tr><td>%SPM%</td><td>Will be replaced by the Player's score per minute on this server</td></tr>
	<tr><td>%playerKills%</td><td>Will be replaced by the player's totalkills on this server</td></tr>
	<tr><td>%playerHeadshots%</td><td>Will be replaced by the player's totalheadshots on this server</td></tr>
	<tr><td>%playerDeaths%</td><td>Will be replaced by the player's totaldeaths on this server</td></tr>
	<tr><td>%playerKDR%</td><td>Will be replaced by the player's kill death ratio on this server</td></tr>
	<tr><td>%playerSucide%</td><td>Will be replaced by the player's sucides on this server</td></tr>
	<tr><td>%playerPlaytime%</td><td>Will be replaced by the player's totalplaytime on this server in hh:mm:ss format</td></tr>
	<tr><td>%rounds%</td><td>Will be replaced by the player's totalrounds played on this server</td></tr>
	<tr><td>%playerRank%</td><td>Will be replaced by the player's concurrent serverrank</td></tr>
	<tr><td>%allRanks%</td><td>Will be replaced by the player's concurrent serverrank</td></tr>
	<tr><td>%killstreak%</td><td>Will be replaced by the player's best Killstreak</td></tr>
	<tr><td>%deathstreak%</td><td>Will be replaced by the player's worst Deathstreak</td></tr>
	</table>
	<br>
	
	<h2>Replacement String for Weaponstats</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%playerName%</td><td>Will be replaced by the player's name</td></tr>
	<tr><td>%playerKills%</td><td>Will be replaced by the player's Totalkills on this server with the specific Weapon</td></tr>
	<tr><td>%playerHeadshots%</td><td>Will be replaced by the player's Totalheadshotkills on this server the specific Weapon</td></tr>
	<tr><td>%playerDeaths%</td><td>Will be replaced by the player's totaldeaths on this server caused by this specific Weapon</td></tr>
	<tr><td>%playerKDH%</td><td>Will be replaced by the player's Headshotkill ratio on this server with the specific Weapon</td></tr>
	<tr><td>%playerKDR%</td><td>Will be replaced by the player's kill death ratio on this server with the specific Weapon</td></tr>
	<tr><td>%playerRank%</td><td>Will be replaced by the player's current Serverrank for the specific Weapon</td></tr>
	<tr><td>%allRanks%</td><td>Will be replaced by current Number of Player in Serverrank for the specific Weapon</td></tr>
	<tr><td>%killstreak%</td><td>Will be replaced by the player's best Killstreak</td></tr>
	<tr><td>%deathstreak%</td><td>Will be replaced by the player's worst Deathstreak</td></tr>
	</table>
	
	<h2>Replacement String for PlayerSession</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%playerName%</td><td>Will be replaced by the player's name</td></tr>
	<tr><td>%playerScore%</td><td>Will be replaced by the player's totalscore of the concurrent Session</td></tr>
	<tr><td>%playerKills%</td><td>Will be replaced by the player's totalkills of the concurrent Session</td></tr>
	<tr><td>%playerHeadshots%</td><td>Will be replaced by the player's totalheadshots of the concurrent Session</td></tr>
	<tr><td>%playerDeaths%</td><td>Will be replaced by the player's totaldeaths of the concurrent Session</td></tr>
	<tr><td>%playerKDR%</td><td>Will be replaced by the player's kill death ratio of the concurrent Session</td></tr>
	<tr><td>%playerSucide%</td><td>Will be replaced by the player's sucides of the concurrent Session</td></tr>
	<tr><td>%SessionDuration%</td><td>Will be replaced by the player's totalplaytime of the concurrent Session in Minutes</td></tr>
	<tr><td>%playerRank%</td><td>Will be replaced by the player's concurrent serverrank</td></tr>
	<tr><td>%RankDif%</td><td>Will be replaced by the player's rank change</td></tr>
	<tr><td>%SessionStarted%</td><td>Will be replaced by the player's start of the Session</td></tr>
	<tr><td>%killstreak%</td><td>Will be replaced by the player's best Killstreak of the Session</td></tr>
	<tr><td>%deathstreak%</td><td>Will be replaced by the player's worst Deathstreak of the Session</td></tr>
	</table>
	<br>
	
	<h3>NOTE:</h3>
		<p>Tracked stats are: Kills, Headshots, Deaths, All Weapons, TKs, Suicides, Score, Playtime, Rounds, MapStats, Dogtags </p>
		<p>The Rank is created dynamical from Query in  my opinion much better than write it back to database.</p>
		<p>The Stats are written to the Database at the end of the round</p>
		<p>Cheaterprotection uses Stats from BFBCS only!!!</p>
		<p>Cheaterprotection kicks in if both values are over the limit</p>
	
<h3>Known issues:</h3>
<p> If you reason is too long the Ban wont work(Eaguid Ban, Nameban)</p>
<p> Avoid using Special characters in the Reason Messages</p>
	
	
<h3>Changelog:</h3><br>
<b>3.0.2.2</b>
Fixed Bug that a player is not in the Database because he got kicked be the the cheaterprotection<br>
Improved BFBCS Statsfetch by packing multiply players in one query to bfbcs. The Plugin waits now until the minimum playercount is reached<br>
This optimization can be disabled by setting the the Request Packrate to 0. <br>
Recommend values are 2-3 or higher.<br><br>

<b>3.0.2.1</b>
Fixed Bug might cause false entry in tbl_bfbcs(overwritting existent entries)<br><br>

<b>3.0.2.0 </b><br>
Now able to pull Stats from BFBCS and able to warn/kick/ban Player with high kdr and high Score per Minute (Globalstats)<br>
The Stats of BFBCS will be stored in the DB to reduce stress on BFBCS api.
The Updateinterval (in hours) for the BFBCS Stats is variable and configurable (pls dont choose to low values)<br>
The plugin fetches up to 32 Players in one query from BFBCS and is able to repair JSON Objects containing Error messages from BFBCS API.<br>
The plugin tries to get the stats onetime only at the moment.<br>
Some Improvements in Code<br>
Cheater and Statspadder Protection based on Globalstats of BFBCS with configurable Limits and minimum values.<br>
Chatlog can be turned off now<br>
Option for Update empty PB-Guids(not recommend) <br>
Added Servertimeoffset feature<br><br>



<b>3.0.1.3 -->3.0.1.4 </b><br>
Added missing Weapon <br><br>

<b>3.0.1.2 -->3.0.1.3 </b><br>
Added missing Weapons thx to Sir_Duck for the report <br><br>

<b>3.0.1.1 -->3.0.1.2 </b><br>
Added missing Weapons thx to Hellokitty for the report <br>
Fixed Typo thx to HunterBfV <br><br>

<b>3.0.1.0 -->3.0.1.1 </b><br>
New Shared Mode -> BC2 and Vietnam in the same tables.<br>
Several Bugfixes<br>
Added Option to disable Weaponstatslogging on Request<br>

<b>3.0.0.7 -->3.0.1.0 </b><br>
Automatic detection of the Gamemod(BC2 / Vietnam)<br>
Added Support for BFBC2 Vietnam just select the right Gamemod in Pluginsettings or let it set by the Plugin automatically <br>
Tables of Vietnam are called (tbl_playerstats_bfv, tbl_weaponstats_bfv, tbl_chatlog_bfv, tbl_dogtags_bfv, tbl_mapstats_bfv) + your suffix<br>
BC2 and Vietnam share the same tbl_playerdata + you Suffix<br>
You will need a new Version of Webstats for Vietnam which i will provide later<br>
Serveral small fixes and improvements<br><br>

<b>3.0.0.6 -->3.0.0.7 </b><br>
Some Bugfixes<br>
New Feature: The plugin is now checking the the weapontable if it contains all needed fields(weapons)<br>
Dont worry about the weapons anymore.<br><br>

<b>3.0.0.5 -->3.0.0.6 </b><br>
Bugfixes<br><br>
Scorebug is not fully fixed atm

<b>3.0.0.4 -->3.0.0.5 </b><br>
Bugfixes in tablebuilder<br><br>

<b>3.0.0.3 -->3.0.0.4 </b><br>
Bugfixes<br><br>
<b>3.0.0.2 -->3.0.0.3 </b><br>
Bugfixes<br><br>
<b>3.0.0.1 -->3.0.0.2 /b><br>
Bugfix<br>
New Replacements<br><br>

<b>3.0.0.0 -->3.0.0.1 </b><br>
IMPORTANT BUGFIX<br><br>


<b>2.0.6.4 -->3.0.0.0 </b><br>
NEW TABLE DESIGN --> FASTER!!!<br>
Floodprotection<br>
Killstreak and Deathstreak Tracking<br><br>

<b>2.0.6.3 --> 2.0.6.4</b><br>
New @session Command Player now can call there Sessiondata.<br>
Some small improvements<br><br>
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion) {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerJoin","OnGlobalChat","OnTeamChat","OnSquadChat","OnPunkbusterMessage","OnPunkbusterPlayerInfo","OnServerInfo",
                                					 "OnPlayerKilled","OnPlayerLeft","OnRoundOverPlayers","OnPlayerSpawned","OnLoadingLevel","OnCommandStats","OnCommandTop10","OnCommandDogtags");
        }



        public void OnPluginEnable() {

            isStreaming = true;
            this.serverName = "";
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBF BC2 Chat, GUID and Stats Logger ^2Enabled");
            this.Spamprotection = new CSpamprotection(numberOfAllowedRequests);
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBF BC2 Chat, GUID and Stats Logger: ^2 Floodprotection set to "+ this.numberOfAllowedRequests.ToString() + " Request per Round for each Player");
            // Register Commands
            this.m_isPluginEnabled = true;
			this.prepareTablenames(this.m_strGameMod);
			this.setGameMod(this.m_strGameMod);
			this.MyDateTime = new myDateTime_W(this.m_dTimeOffset);
            this.RegisterAllCommands();  
        }

        public void OnPluginDisable()
        {
            isStreaming = false;
            if(OdbcCon != null)
            	if (OdbcCon.State == ConnectionState.Open)
            	{
               	 	OdbcCon.Close();
           		}
            if(OdbcConn != null)
           	 if (OdbcConn.State == ConnectionState.Open)
          	  {
                 OdbcConn.Close();
           	  }
            
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBF BC2 Chat, GUID and Stats Logger ^1Disabled");
            
            //Unregister Commands
            this.m_isPluginEnabled = false;
            this.UnregisterAllCommands();
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables() {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Server Details|Host", this.m_strHost.GetType(), this.m_strHost));
            lstReturn.Add(new CPluginVariable("Server Details|Port", this.m_strDBPort.GetType(), this.m_strDBPort));
            lstReturn.Add(new CPluginVariable("Server Details|Database Name", this.m_strDatabase.GetType(), this.m_strDatabase));
            lstReturn.Add(new CPluginVariable("Server Details|UserName", this.m_strUserName.GetType(), this.m_strUserName));
            lstReturn.Add(new CPluginVariable("Server Details|Password", this.m_strPassword.GetType(), this.m_strPassword));
            lstReturn.Add(new CPluginVariable("Chatlogging|Enable Chatlogging?", typeof(enumBoolYesNo), this.m_enChatloggingON));
            if(this.m_enChatloggingON == enumBoolYesNo.Yes)
			{
            	lstReturn.Add(new CPluginVariable("Chatlogging|Log ServerSPAM?", typeof(enumBoolYesNo), this.m_enNoServerMsg));
            	lstReturn.Add(new CPluginVariable("Chatlogging|Instant Logging of Chat Messages?", typeof(enumBoolYesNo), this.m_enInstantChatlogging));
            }
            lstReturn.Add(new CPluginVariable("Stats|Enable Statslogging?", typeof(enumBoolYesNo), this.m_enLogSTATS));
            lstReturn.Add(new CPluginVariable("Stats|Enable Weaponstats?", typeof(enumBoolYesNo), this.m_weaponstatsON));
            lstReturn.Add(new CPluginVariable("Stats|Update EA GUID?", typeof(enumBoolYesNo), this.m_UpdateEA_GUID));
            lstReturn.Add(new CPluginVariable("Stats|Update PB-GUID (NOT recommended!!!)?", typeof(enumBoolYesNo), this.m_UpdatePB_GUID));
            lstReturn.Add(new CPluginVariable("Stats|Ranking by Score?", typeof(enumBoolYesNo), this.m_enRankingByScore));
            lstReturn.Add(new CPluginVariable("Stats|Send Stats to all Players?", typeof(enumBoolYesNo), this.m_enSendStatsToAll));
			lstReturn.Add(new CPluginVariable("Stats|PlayerMessage", typeof(string[]), this.m_lstPlayerStatsMessage.ToArray()));
			lstReturn.Add(new CPluginVariable("Stats|Weaponstats Message", typeof(string[]), this.m_lstWeaponstatsMsg.ToArray()));
			lstReturn.Add(new CPluginVariable("WelcomeStats|Enable Welcomestats?", typeof(enumBoolYesNo), this.m_enWelcomeStats));
			lstReturn.Add(new CPluginVariable("WelcomeStats|Yell Welcome Message(not the stats)?", typeof(enumBoolYesNo), this.m_enYellWelcomeMSG));
			lstReturn.Add(new CPluginVariable("WelcomeStats|Welcome Message", this.m_strPlayerWelcomeMsg.GetType(), this.m_strPlayerWelcomeMsg));
			lstReturn.Add(new CPluginVariable("WelcomeStats|Welcome Message for new Player", this.m_strNewPlayerWelcomeMsg.GetType(), this.m_strNewPlayerWelcomeMsg));
			lstReturn.Add(new CPluginVariable("WelcomeStats|Welcomestats Delay", this.int_welcomeStatsDelay.GetType(), this.int_welcomeStatsDelay));
			lstReturn.Add(new CPluginVariable("Stats|Top10 ingame", this.m_enTop10ingame.GetType(), this.m_enTop10ingame));
			lstReturn.Add(new CPluginVariable("Debug|Debugmode on?", typeof(enumBoolYesNo), this.m_enDebugMode));
			lstReturn.Add(new CPluginVariable("Table|Keywordlist BC2", typeof(string[]), this.m_lstTableconfig_bc2.ToArray()));
			lstReturn.Add(new CPluginVariable("Table|Keywordlist Vietnam", typeof(string[]), this.m_lstTableconfig_bfv.ToArray()));
			lstReturn.Add(new CPluginVariable("Table|tableSuffix", this.tableSuffix.GetType(), this.tableSuffix));
			lstReturn.Add(new CPluginVariable("MapStats|MapStats ON?", typeof(enumBoolYesNo), this.m_mapstatsON));
			lstReturn.Add(new CPluginVariable("Session|Session ON?", typeof(enumBoolYesNo), this.m_sessionON));
			lstReturn.Add(new CPluginVariable("Session|SessionMessage", typeof(string[]), this.m_lstSessionMessage.ToArray()));
			lstReturn.Add(new CPluginVariable("FloodProtection|Playerrequests per Round", this.numberOfAllowedRequests.GetType(), this.numberOfAllowedRequests));
			lstReturn.Add(new CPluginVariable("TimeOffset|Servertime Offset", this.m_dTimeOffset.GetType(), this.m_dTimeOffset));
			lstReturn.Add(new CPluginVariable("Stats|Default GameMod", "enum.GameMods(BC2|VIETNAM|SHARED)", this.m_strGameMod));
			lstReturn.Add(new CPluginVariable("BFBCS|Fetch Stats from BFBCS", typeof(enumBoolYesNo), this.m_getStatsfromBFBCS));
			
			if(this.m_getStatsfromBFBCS == enumBoolYesNo.Yes)
			{
				lstReturn.Add(new CPluginVariable("BFBCS|Updateinterval (hours)", this.BFBCS_UpdateInterval.GetType(), this.BFBCS_UpdateInterval));
				lstReturn.Add(new CPluginVariable("BFBCS|Request Packrate", this.BFBCS_Min_Request.GetType(), this.BFBCS_Min_Request));
            	lstReturn.Add(new CPluginVariable("Cheaterprotection|Statsbased Protection", typeof(enumBoolYesNo), this.m_cheaterProtection));
			}
			if(this.m_cheaterProtection == enumBoolYesNo.Yes)
			{
				lstReturn.Add(new CPluginVariable("Cheaterprotection|Max. KDR:", this.m_dMaxAllowedKDR.GetType(), this.m_dMaxAllowedKDR));
				lstReturn.Add(new CPluginVariable("Cheaterprotection|Max. SPM:", this.m_dMaxScorePerMinute.GetType(), this.m_dMaxScorePerMinute));
				lstReturn.Add(new CPluginVariable("Cheaterprotection|Min. Playtime(hours):", this.m_dminimumPlaytime.GetType(), this.m_dminimumPlaytime));
				lstReturn.Add(new CPluginVariable("Cheaterprotection|Reason Message:", this.m_strReasonMsg.GetType(), this.m_strReasonMsg));
				lstReturn.Add(new CPluginVariable("Cheaterprotection|Perform Action", "enum.Actions(Warn|Kick|Nameban|EAGUIDBan|PBBan)", this.m_strRemoveMethode));
			}
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables() {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Host", this.m_strHost.GetType(), this.m_strHost));
            lstReturn.Add(new CPluginVariable("Port", this.m_strDBPort.GetType(), this.m_strDBPort));
            lstReturn.Add(new CPluginVariable("Database Name", this.m_strDatabase.GetType(), this.m_strDatabase));
            lstReturn.Add(new CPluginVariable("UserName", this.m_strUserName.GetType(), this.m_strUserName));
            lstReturn.Add(new CPluginVariable("Password", this.m_strPassword.GetType(), this.m_strPassword));
			// Switch for Stats Logging
			lstReturn.Add(new CPluginVariable("Enable Chatlogging?", typeof(enumBoolYesNo), this.m_enChatloggingON));
            if(this.m_enChatloggingON == enumBoolYesNo.Yes)
			{
				lstReturn.Add(new CPluginVariable("Log ServerSPAM?", typeof(enumBoolYesNo), this.m_enNoServerMsg));
				lstReturn.Add(new CPluginVariable("Instant Logging of Chat Messages?", typeof(enumBoolYesNo), this.m_enInstantChatlogging));
            }
            lstReturn.Add(new CPluginVariable("Enable Statslogging?", typeof(enumBoolYesNo), this.m_enLogSTATS));
            lstReturn.Add(new CPluginVariable("Enable Weaponstats?", typeof(enumBoolYesNo), this.m_weaponstatsON));
            lstReturn.Add(new CPluginVariable("Update EA GUID?", typeof(enumBoolYesNo), this.m_UpdateEA_GUID));
            lstReturn.Add(new CPluginVariable("Update PB-GUID (NOT recommended!!!)?", typeof(enumBoolYesNo), this.m_UpdatePB_GUID));
            lstReturn.Add(new CPluginVariable("Ranking by Score?", typeof(enumBoolYesNo), this.m_enRankingByScore));
            lstReturn.Add(new CPluginVariable("Send Stats to all Players?", typeof(enumBoolYesNo), this.m_enSendStatsToAll));
			lstReturn.Add(new CPluginVariable("PlayerMessage", typeof(string[]), this.m_lstPlayerStatsMessage.ToArray()));
			lstReturn.Add(new CPluginVariable("Weaponstats Message", typeof(string[]), this.m_lstWeaponstatsMsg.ToArray()));
			lstReturn.Add(new CPluginVariable("Enable Welcomestats?", typeof(enumBoolYesNo), this.m_enWelcomeStats));
			lstReturn.Add(new CPluginVariable("Yell Welcome Message(not the stats)?", typeof(enumBoolYesNo), this.m_enYellWelcomeMSG));
			lstReturn.Add(new CPluginVariable("Welcome Message", this.m_strPlayerWelcomeMsg.GetType(), this.m_strPlayerWelcomeMsg));
			lstReturn.Add(new CPluginVariable("Welcome Message for new Player", this.m_strNewPlayerWelcomeMsg.GetType(), this.m_strNewPlayerWelcomeMsg));
			lstReturn.Add(new CPluginVariable("Welcomestats Delay", this.int_welcomeStatsDelay.GetType(), this.int_welcomeStatsDelay));
			lstReturn.Add(new CPluginVariable("Top10 ingame", this.m_enTop10ingame.GetType(), this.m_enTop10ingame));
			lstReturn.Add(new CPluginVariable("Debugmode on?", typeof(enumBoolYesNo), this.m_enDebugMode));
			lstReturn.Add(new CPluginVariable("Keywordlist BC2", typeof(string[]), this.m_lstTableconfig_bc2.ToArray()));
			lstReturn.Add(new CPluginVariable("Keywordlist Vietnam", typeof(string[]), this.m_lstTableconfig_bfv.ToArray()));
			lstReturn.Add(new CPluginVariable("tableSuffix", this.tableSuffix.GetType(), this.tableSuffix));
			lstReturn.Add(new CPluginVariable("MapStats ON?", typeof(enumBoolYesNo), this.m_mapstatsON));
			lstReturn.Add(new CPluginVariable("Session ON?", typeof(enumBoolYesNo), this.m_sessionON));
			lstReturn.Add(new CPluginVariable("SessionMessage", typeof(string[]), this.m_lstSessionMessage.ToArray()));
			lstReturn.Add(new CPluginVariable("Playerrequests per Round", this.numberOfAllowedRequests.GetType(), this.numberOfAllowedRequests));
			lstReturn.Add(new CPluginVariable("Servertime Offset", this.m_dTimeOffset.GetType(), this.m_dTimeOffset));
			lstReturn.Add(new CPluginVariable("Default GameMod", "enum.GameMods(BC2|VIETNAM|SHARED)", this.m_strGameMod));
			lstReturn.Add(new CPluginVariable("Fetch Stats from BFBCS", typeof(enumBoolYesNo), this.m_getStatsfromBFBCS));
			if(this.m_getStatsfromBFBCS == enumBoolYesNo.Yes)
			{
				lstReturn.Add(new CPluginVariable("Updateinterval (hours)", this.BFBCS_UpdateInterval.GetType(), this.BFBCS_UpdateInterval));
				lstReturn.Add(new CPluginVariable("Request Packrate", this.BFBCS_Min_Request.GetType(), this.BFBCS_Min_Request));
	            lstReturn.Add(new CPluginVariable("Statsbased Protection", typeof(enumBoolYesNo), this.m_cheaterProtection));
			}
			if(this.m_cheaterProtection == enumBoolYesNo.Yes)
			{
				lstReturn.Add(new CPluginVariable("Max. KDR:", this.m_dMaxAllowedKDR.GetType(), this.m_dMaxAllowedKDR));
				lstReturn.Add(new CPluginVariable("Max. SPM:", this.m_dMaxScorePerMinute.GetType(), this.m_dMaxScorePerMinute));
				lstReturn.Add(new CPluginVariable("Min. Playtime(hours):", this.m_dminimumPlaytime.GetType(), this.m_dminimumPlaytime));
				lstReturn.Add(new CPluginVariable("Reason Message:", this.m_strReasonMsg.GetType(), this.m_strReasonMsg));
				lstReturn.Add(new CPluginVariable("Perform Action", "enum.Actions(Kick|Nameban|EAGUIDBan|PBBan)", this.m_strRemoveMethode));
			}
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
            else if (strVariable.CompareTo("Enable Chatlogging?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enChatloggingON = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Log ServerSPAM?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enNoServerMsg = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Instant Logging of Chat Messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enInstantChatlogging = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Enable Statslogging?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enLogSTATS = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Enable Weaponstats?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_weaponstatsON = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Update EA GUID?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_UpdateEA_GUID = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Update PB-GUID (NOT recommended!!!)?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_UpdatePB_GUID = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Ranking by Score?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enRankingByScore = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Send Stats to all Players?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enSendStatsToAll = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("PlayerMessage") == 0)
            {
                this.m_lstPlayerStatsMessage = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
			else if (strVariable.CompareTo("Weaponstats Message") == 0)
            {
                this.m_lstWeaponstatsMsg = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
			else if (strVariable.CompareTo("Enable Welcomestats?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enWelcomeStats = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("Yell Welcome Message(not the stats)?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enYellWelcomeMSG = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("Welcome Message") == 0)
            {
                this.m_strPlayerWelcomeMsg = strValue;
            }
			else if (strVariable.CompareTo("Welcome Message for new Player") == 0)
            {
                this.m_strNewPlayerWelcomeMsg = strValue;
            }
			else if (strVariable.CompareTo("Welcomestats Delay") == 0 && Int32.TryParse(strValue, out int_welcomeStatsDelay) == true)
            {
				this.int_welcomeStatsDelay = Convert.ToInt32(strValue);
            }
			else if (strVariable.CompareTo("Top10 ingame") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enTop10ingame = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("Debugmode on?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enDebugMode = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("Keywordlist BC2") == 0)
            {
                this.m_lstTableconfig_bc2 = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
			else if (strVariable.CompareTo("Keywordlist Vietnam") == 0)
            {
                this.m_lstTableconfig_bfv = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
			else if (strVariable.CompareTo("tableSuffix") == 0)
            {
                this.tableSuffix = strValue;
                if(this.m_enDebugMode == enumBoolYesNo.Yes)
				{
					this.m_enDebugMode = enumBoolYesNo.No;
					this.prepareTablenames(this.m_strGameMod);
					this.setGameMod(this.m_strGameMod);
					this.m_enDebugMode = enumBoolYesNo.Yes;
				}
				else
				{
					this.prepareTablenames(this.m_strGameMod);
					this.setGameMod(this.m_strGameMod);
				}
                this.boolTableEXISTS = false;
			}
			else if (strVariable.CompareTo("MapStats ON?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_mapstatsON = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("Session ON?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_sessionON = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
			else if (strVariable.CompareTo("SessionMessage") == 0)
            {
                this.m_lstSessionMessage = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
			else if (strVariable.CompareTo("Playerrequests per Round") == 0 && Int32.TryParse(strValue, out numberOfAllowedRequests) == true)
            {
				this.numberOfAllowedRequests = Convert.ToInt32(strValue);
            }
			else if (strVariable.CompareTo("Servertime Offset") == 0 && Double.TryParse(strValue, out m_dTimeOffset) == true)
            {
				this.m_dTimeOffset = Convert.ToDouble(strValue);
				this.MyDateTime = new myDateTime_W(this.m_dTimeOffset);
            }
			else if(strVariable.CompareTo("Default GameMod") == 0) 
			{
				this.m_strGameMod = strValue;
				
				if(this.m_enDebugMode == enumBoolYesNo.Yes)
				{
					this.m_enDebugMode = enumBoolYesNo.No;
					this.prepareTablenames(this.m_strGameMod);
					this.setGameMod(this.m_strGameMod);
					this.m_enDebugMode = enumBoolYesNo.Yes;
				}
				else
				{
					this.prepareTablenames(this.m_strGameMod);
					this.setGameMod(this.m_strGameMod);
				}
				this.boolTableEXISTS = false;
            }
			else if(strVariable.CompareTo("Fetch Stats from BFBCS") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true) 
			{
				this.m_getStatsfromBFBCS = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
			}
			else if(strVariable.CompareTo("Request Packrate") == 0 && Int32.TryParse(strValue, out this.BFBCS_Min_Request) == true) 
			{
				this.BFBCS_Min_Request = Convert.ToInt32(strValue);
			}
			else if(strVariable.CompareTo("Updateinterval (hours)") == 0 && Double.TryParse(strValue, out this.BFBCS_UpdateInterval) == true) 
			{
				this.BFBCS_UpdateInterval = Convert.ToDouble(strValue);
			}
			else if(strVariable.CompareTo("Statsbased Protection") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true) 
			{
				this.m_cheaterProtection =  (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo),strValue);
			}
			else if(strVariable.CompareTo("Max. KDR:") == 0 && Double.TryParse(strValue, out this.m_dMaxAllowedKDR) == true) 
			{
				this.m_dMaxAllowedKDR = Convert.ToDouble(strValue);
			}
			else if(strVariable.CompareTo("Max. SPM:") == 0 && Double.TryParse(strValue, out this.m_dMaxScorePerMinute) == true ) 
			{
				this.m_dMaxScorePerMinute = Convert.ToDouble(strValue);
			}
			else if(strVariable.CompareTo("Min. Playtime(hours):") == 0 && Double.TryParse(strValue, out this.m_dminimumPlaytime) == true) 
			{
				this.m_dminimumPlaytime = Convert.ToDouble(strValue);
			}
			else if(strVariable.CompareTo("Reason Message:") == 0) 
			{
				this.m_strReasonMsg = strValue;
			}
			else if(strVariable.CompareTo("Perform Action") == 0) 
			{
				this.m_strRemoveMethode = strValue;
			}
			this.RegisterAllCommands();
        }
       
        	
        private List<string> GetExcludedCommandStrings(string strAccountName) {

            List<string> lstReturnCommandStrings = new List<string>();

            List<MatchCommand> lstCommands = this.GetRegisteredCommands();

            CPrivileges privileges = this.GetAccountPrivileges(strAccountName);

            foreach (MatchCommand mtcCommand in lstCommands) {

                if (mtcCommand.Requirements.HasValidPermissions(privileges) == true && lstReturnCommandStrings.Contains(mtcCommand.Command) == false) {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }

            return lstReturnCommandStrings;
        }

        private List<string> GetCommandStrings() {

            List<string> lstReturnCommandStrings = new List<string>();

            List<MatchCommand> lstCommands = this.GetRegisteredCommands();

            foreach (MatchCommand mtcCommand in lstCommands) {

                if (lstReturnCommandStrings.Contains(mtcCommand.Command) == false) {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }

            return lstReturnCommandStrings;
        }

        private void UnregisterAllCommands() {
        	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandStats", this.Listify<string>("@", "!", "#"), "stats", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal serverstats"));
        	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandStats", this.Listify<string>("@", "!", "#"), "rank", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal serverstats"));
        	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandTop10", this.Listify<string>("@", "!", "#"), "top10", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player top10 Players"));
        	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandDogtags", this.Listify<string>("@", "!", "#"), "dogtags", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal serverstats"));
        	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandSession", this.Listify<string>("@", "!", "#"), "session", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal Session data"));
		}

        private void SetupHelpCommands() {
        
		}

        private void RegisterAllCommands() {
        	
        	 if (this.m_isPluginEnabled == true) {

                this.SetupHelpCommands();
               
                if(this.m_enLogSTATS == enumBoolYesNo.Yes){
                	this.RegisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandStats", this.Listify<string>("@", "!", "#"), "stats", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal serverstats"));
                }
                else{
                	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandStats", this.Listify<string>("@", "!", "#"), "stats", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal serverstats"));
                }
                
                if(this.m_enLogSTATS == enumBoolYesNo.Yes){
                	this.RegisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandStats", this.Listify<string>("@", "!", "#"), "rank", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal serverstats"));
                }
                else{
                	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandStats", this.Listify<string>("@", "!", "#"), "rank", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal serverstats"));
                }
                
                if(this.m_enLogSTATS == enumBoolYesNo.Yes){
                	this.RegisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandDogtags", this.Listify<string>("@", "!", "#"), "dogtags", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal serverstats"));
                }
                else{
                	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandDogtags", this.Listify<string>("@", "!", "#"), "dogtags", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal serverstats"));
                }
                
                if(this.m_enLogSTATS == enumBoolYesNo.Yes && this.m_enTop10ingame == enumBoolYesNo.Yes){
                	this.RegisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandTop10", this.Listify<string>("@", "!", "#"), "top10", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player top10 Players"));
                }
                else{
                	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandTop10", this.Listify<string>("@", "!", "#"), "top10", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player top10 Players"));
                }
                
                if(this.m_sessionON == enumBoolYesNo.Yes){
                	this.RegisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandSession", this.Listify<string>("@", "!", "#"), "session", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal Session data"));
                }
                else{
                	this.UnregisterCommand(new MatchCommand("CChatGUIDStatsLogger", "OnCommandSession", this.Listify<string>("@", "!", "#"), "session", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Provides a player his personal Session data"));
                }
        	}
        }
        #endregion
        
        #region IPRoConPluginInterface
/*=======ProCon Events========*/


        // Player events
        public override void OnPlayerJoin(string strSoldierName) 
        {
        	if(this.StatsTracker.ContainsKey(strSoldierName) == false)
        	{
        		CStats newEntry = new CStats("",0,0,0,0,0,0,0,this.m_dTimeOffset);
        		StatsTracker.Add(strSoldierName,newEntry);
        	}
        	this.CreateSession(strSoldierName);
        	if(bool_roundStarted == true && StatsTracker.ContainsKey(strSoldierName) == true)
        	{ 
        		if(StatsTracker[strSoldierName].PlayerOnServer == false)
        		{	
        			if(this.StatsTracker[strSoldierName].TimePlayerjoined == null)
        			{
        				this.StatsTracker[strSoldierName].TimePlayerjoined = MyDateTime.Now;
        			}
        			this.StatsTracker[strSoldierName].Playerjoined = MyDateTime.Now;
        			this.StatsTracker[strSoldierName].PlayerOnServer = true;
        		}
        	}
        	//Mapstatscounter for Player who joined the server
        	this.Mapstats.IntplayerjoinedServer++;
        	//Call of the Welcomstatsfunction
        	this.WelcomeStats(strSoldierName);		
        }


        // Will receive ALL chat global/team/squad in R3.
        public override void OnGlobalChat(string strSpeaker, string strMessage) 
        {	
        	this.LogChat(strSpeaker,strMessage,"Global");
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public override void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
        	this.LogChat(strSpeaker,strMessage,"Team");
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public override void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID) 
        {
        	this.LogChat(strSpeaker,strMessage,"Squad");
        }

        public override void OnPunkbusterMessage(string strPunkbusterMessage) 
      	{
        	try
        	{
        	// This piece of code gets the number of player out of Punkbustermessages
        		string playercount ="";
        		if(strPunkbusterMessage.Contains("End of Player List"))
        	   	{
        			playercount = strPunkbusterMessage.Remove(0,1+strPunkbusterMessage.LastIndexOf("("));
        			playercount = playercount.Replace(" ","");
        			playercount = playercount.Remove(playercount.LastIndexOf("P"),playercount.LastIndexOf(")"));
        	   		//this.DebugInfo("EoPl: "+playercount);
        	   		int players = Convert.ToInt32(playercount);
        	   			if( players >= 4 && bool_roundStarted == false)
        					{
        						bool_roundStarted = true;
        						Time_RankingStarted = MyDateTime.Now;
        						//Mapstats Roundstarted
        						this.Mapstats.MapStarted();
        					}
        	   			else if(players >= 4 && this.Mapstats.TimeMapStarted == DateTime.MinValue)
        	   			{
        	   				this.Mapstats.MapStarted();
        	   			}
        	   			//MapStats Playercount
        	   			this.Mapstats.ListADD(players);		
        	   	}
        	}
        	catch(Exception c)
            {
               	this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnPunkbusterMessage: " + c);
       		}
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer) 
        {
        	
        	this.RegisterAllCommands();
        	if (this.m_enLogSTATS == enumBoolYesNo.Yes)
        	{
            	try
            	{
            		this.AddPBInfoToStats(cpbiPlayer);
            		if(this.StatsTracker.ContainsKey(cpbiPlayer.SoldierName))
        			{
        				if(this.StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined == null)
        				{
        					this.StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined = MyDateTime.Now;
        				}
        				this.StatsTracker[cpbiPlayer.SoldierName].IP = cpbiPlayer.Ip;
        			}
            	}
            	catch (Exception c)
            	{
            		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnPunkbusterPlayerInfo: " + c);
            	}
        	}
        }

  
        // Query Events
        public override void OnServerInfo(CServerInfo csiServerInfo) 
        {
            this.serverName = csiServerInfo.ServerName;
            this.Mapstats.StrGamemode = csiServerInfo.GameMode;
            this.Mapstats.ListADD(csiServerInfo.PlayerCount);
            //Mapstats
            if(csiServerInfo.PlayerCount >= 4 && this.Mapstats.TimeMapStarted == DateTime.MinValue)
        	   {
        	   		this.Mapstats.MapStarted();
        	   }
            this.Mapstats.StrMapname = csiServerInfo.Map;
            this.Mapstats.IntRound = csiServerInfo.CurrentRound;
            this.Mapstats.IntNumberOfRounds = csiServerInfo.TotalRounds;
            this.Mapstats.IntServerplayermax = csiServerInfo.MaxPlayerCount;
            
            if(String.Equals(csiServerInfo.GameMod.ToString(),this.m_strGameMod) != true && String.Equals(csiServerInfo.GameMod.ToString(),"None") != true  && String.Equals(this.m_strGameMod, "SHARED") != true)
            {
            	this.DebugInfo("GameMod change detected to: " + csiServerInfo.GameMod.ToString());
            	this.setGameMod(csiServerInfo.GameMod.ToString()); 
            	this.prepareTablenames(csiServerInfo.GameMod.ToString());
            	this.m_strGameMod = csiServerInfo.GameMod.ToString();
            	this.boolTableEXISTS = false;
            }
            
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset) 
        {
        	int i = lstPlayers.Count;
        	List<CPlayerInfo> PlayerList = new List<CPlayerInfo>();
        	//Mapstats Add Playercount to list
        	this.Mapstats.ListADD(i);
        	if(bool_roundStarted == false)
        	{
        		if (i >= 4)
        		{
        			bool_roundStarted = true;
        			Time_RankingStarted = MyDateTime.Now;
        			this.DebugInfo("OLP: roundstarted");
        			//Mapstats Roundstarted
        			this.Mapstats.MapStarted();
        		}
        	}
        	if(i >= 4 && this.Mapstats.TimeMapStarted == DateTime.MinValue)
        	   			{
        	   				this.Mapstats.MapStarted();
        	   			}
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
                	
                	
                	//Timelogging
                	if(this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
                	{
                		if(this.StatsTracker[cpiPlayer.SoldierName].PlayerOnServer == false)
                		{
                			this.StatsTracker[cpiPlayer.SoldierName].Playerjoined = MyDateTime.Now;
                			this.StatsTracker[cpiPlayer.SoldierName].PlayerOnServer = true;
                		}
                		//EA-GUID, ClanTag, usw.
                		this.StatsTracker[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
                		this.StatsTracker[cpiPlayer.SoldierName].ClanTag = cpiPlayer.ClanTag;
                		if(cpiPlayer.Score != 0)
                		{
                			this.StatsTracker[cpiPlayer.SoldierName].Score = cpiPlayer.Score;
                		}
                		//BFBCS
                		if(String.Equals(this.StatsTracker[cpiPlayer.SoldierName].Guid,"") == false && String.Equals(this.StatsTracker[cpiPlayer.SoldierName].EAGuid,"") == false)
                		{
                			PlayerList.Add(cpiPlayer);
                		}
                		
                	}
                	//ID - Cache
                	if(this.m_ID_cache.ContainsKey(cpiPlayer.SoldierName))
                	   {
                			this.m_ID_cache[cpiPlayer.SoldierName].PlayeronServer = true;
                	   }
                	
                	this.CreateSession(cpiPlayer.SoldierName);
                	
                	//Session Score
                	if(this.m_dicSession.ContainsKey(cpiPlayer.SoldierName) && this.m_sessionON == enumBoolYesNo.Yes)
                	{
                		this.m_dicSession[cpiPlayer.SoldierName].AddScore(cpiPlayer.Score);
                	}
            	}
          		if(PlayerList.Count >= this.BFBCS_Min_Request == true)
          		{
          			this.getBFBCStats(PlayerList);
          		}
        	}
        	catch(Exception c)
        	{
        		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnListPlayers: " + c);
        	}
        }
      
		#endregion
		
        #region IPRoConPluginInterface2

        //
        // IPRoConPluginInterface2
        //
        public override void OnPlayerKilled(Kill kKillerVictimDetails) 
        {	
        	if(bool_roundStarted == true)
        	{
        		this.playerKilled(kKillerVictimDetails);
        	}
        }

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer) 
        {
        	
        	this.playerLeftServer(cpiPlayer);
        	this.RegisterAllCommands();
        }

       
        public override void OnRoundOverPlayers(List<CPlayerInfo> lstPlayers) 
        { 
			foreach(CPlayerInfo cpiPlayer in lstPlayers)
        	{
        		if(this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
                {
                	this.StatsTracker[cpiPlayer.SoldierName].Score = cpiPlayer.Score;
                	//EA-GUID, ClanTag, usw.
                	this.StatsTracker[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
                	this.StatsTracker[cpiPlayer.SoldierName].ClanTag = cpiPlayer.ClanTag;
                }
        		//Session Score
                if(this.m_dicSession.ContainsKey(cpiPlayer.SoldierName) && this.m_sessionON == enumBoolYesNo.Yes )
                {
                	this.m_dicSession[cpiPlayer.SoldierName].AddScore(cpiPlayer.Score);
                }
                this.m_dicSession[cpiPlayer.SoldierName].LastScore = 0;
        	}
			this.Mapstats.MapEnd();
        }


        public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory) 
        {
        	
        	if(bool_roundStarted == true && StatsTracker.ContainsKey(soldierName) == true)
        	{ 
        		if(StatsTracker[soldierName].PlayerOnServer == false)
        		{
        			this.StatsTracker[soldierName].Playerjoined = MyDateTime.Now;
        			this.StatsTracker[soldierName].PlayerOnServer = true;
        		}
        	}

        }

       

        #endregion

        #region IPRoConPluginInterface3

        //
        // IPRoConPluginInterface3
        //
        

        public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {
        	this.DebugInfo("OnLoadingLevel: " + mapFileName +" Round: " + roundsPlayed + "/" + roundsTotal);
        	this.DebugInfo("update sql server");
        	this.Nextmapinfo = new CMapstats(MyDateTime.Now,mapFileName,roundsPlayed,roundsTotal,this.m_dTimeOffset);
        	new Thread(StartStreaming).Start();
            m_dicPlayers.Clear();
            this.Spamprotection.Reset();
        	
        }


        #endregion
        
        #region In Game Commands
		public void OnCommandStats(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
		{
			if((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
			{
				string scope ="";
				string newText = strText.ToUpper();
				newText = newText.Replace("STATS","");
				newText = newText.Replace("RANK","");
				newText = newText.Replace("#","");
				if(newText.Contains("!")  == true)
				{
					if(this.m_enSendStatsToAll == enumBoolYesNo.Yes)
					{
						scope = "all";
					}
					else
					{
						scope = "player";
					}
				}
				else
				{
					scope = "player";
				}
				newText = newText.Replace("!","");
				newText = newText.Replace("@","");
				newText = newText.Replace(" ","");
				
				if(newText.Length > 0)
				{
					string weaponName = newText;
					this.GetWeaponStats(this.FindKeyword(weaponName),strSpeaker,scope);
				}
				else
				{
					this.GetPlayerStats(strSpeaker,0,scope);
				}
			}
		}
		
		public void OnCommandTop10(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
		{
			if((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
			{
				string scope ="";
				string newText = strText.ToUpper();
				newText = newText.Replace("TOP10","");
				newText = newText.Replace("#","");
				if(newText.Contains("!")  == true)
				{
					if(this.m_enSendStatsToAll == enumBoolYesNo.Yes)
					{
						scope = "all";
					}
					else
					{
						scope = "player";
					}
				}
				else
				{
					scope = "player";
				}
				newText = newText.Replace("!","");
				newText = newText.Replace("@","");
				newText = newText.Replace(" ","");

				if(newText.Length > 0)
				{
					string weaponName = newText;
					this.GetWeaponTop10(this.FindKeyword(weaponName), strSpeaker,2, scope);
				}
				else
				{
					this.GetTop10(strSpeaker,2,scope);
				}
			}
		}
		
		public void OnCommandDogtags(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
		{
			if((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
			{	
				string scope ="";
				string newText = strText.ToUpper();
				newText = newText.Replace("DOGTAGS","");
				newText = newText.Replace("#","");
				if(newText.Contains("!")  == true)
				{
					if(this.m_enSendStatsToAll == enumBoolYesNo.Yes)
					{
						scope = "all";
					}
					else
					{
						scope = "player";
					}
				}
				else
				{
					scope = "player";
				}
				this.GetDogtags(strSpeaker,1,scope);
			}
		}
		
		public void OnCommandSession(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
		{
			if((this.m_enLogSTATS == enumBoolYesNo.Yes) && (this.Spamprotection.isAllowed(strSpeaker) == true))
			{	
				string scope ="";
				string newText = strText.ToUpper();
				newText = newText.Replace("SESSION","");
				newText = newText.Replace("#","");
				if(newText.Contains("!")  == true)
				{
					if(this.m_enSendStatsToAll == enumBoolYesNo.Yes)
					{
						scope = "all";
					}
					else
					{
						scope = "player";
					}
				}
				else
				{
					scope = "player";
				}
				this.GetSession(strSpeaker,1,scope);
			}
		}

        #endregion
        
        #region CChatGUIDStatsLogger Methodes 
        
        private int GetPlayerTeamID(string strSoldierName)
        {

            int iTeamID = 0; // Neutral Team ID
 
            if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
            {
                iTeamID = this.m_dicPlayers[strSoldierName].TeamID;
            }
            return iTeamID;
        }
        
        public void playerLeftServer(CPlayerInfo cpiPlayer)
        {
        	if(this.StatsTracker.ContainsKey(cpiPlayer.SoldierName) == true)
        	{
        		this.StatsTracker[cpiPlayer.SoldierName].Score = cpiPlayer.Score;
        		this.StatsTracker[cpiPlayer.SoldierName].TimePlayerleft = MyDateTime.Now;
        		this.StatsTracker[cpiPlayer.SoldierName].playerleft();
        		//EA-GUID, ClanTag, usw.
        		this.StatsTracker[cpiPlayer.SoldierName].EAGuid = cpiPlayer.GUID;
               	this.StatsTracker[cpiPlayer.SoldierName].ClanTag = cpiPlayer.ClanTag;
        	}
        	//ID cache System
        	if(this.m_ID_cache.ContainsKey(cpiPlayer.SoldierName) == true)
        	{
        		this.m_ID_cache[cpiPlayer.SoldierName].PlayeronServer = false;
        	}
        	//Mapstats
        	this.Mapstats.IntplayerleftServer++;
        	
        	//Session
        	if(this.m_dicSession.ContainsKey(cpiPlayer.SoldierName) == true && this.m_sessionON == enumBoolYesNo.Yes)
        	{
        		this.m_dicSession.Remove(cpiPlayer.SoldierName);
        	}
        	
        }
        
        public void playerKilled(Kill kKillerVictimDetails)
        {
        	string strKillerSoldierName = kKillerVictimDetails.Killer.SoldierName;
        	string strVictimSoldierName = kKillerVictimDetails.Victim.SoldierName;
        	
        	//TEAMKILL OR SUICID
			if (String.Compare(strKillerSoldierName, strVictimSoldierName) == 0) {		//  A Suicide
        		this.AddSuicideToStats(strKillerSoldierName,kKillerVictimDetails.DamageType);
			}
			else
			{
				if (this.GetPlayerTeamID(strKillerSoldierName) == this.GetPlayerTeamID(strVictimSoldierName)) 
				{ 	//TeamKill
					this.AddTeamKillToStats(strKillerSoldierName);
					this.AddDeathToStats(strVictimSoldierName,kKillerVictimDetails.DamageType);
				}	
				else
				{	
					//Regular Kill: Player killed an Enemy
					this.AddKillToStats(strKillerSoldierName,kKillerVictimDetails.DamageType, kKillerVictimDetails.Headshot);
					this.AddDeathToStats(strVictimSoldierName,kKillerVictimDetails.DamageType);
					if(string.Equals(kKillerVictimDetails.DamageType,"KNV-1"))
					   {	//Dogtagstracking
					   		CKillerVictim KnifeKill = new CKillerVictim(strKillerSoldierName,strVictimSoldierName);
					   		if(m_dicKnifeKills.ContainsKey(KnifeKill) == true)
					   		{
					   			m_dicKnifeKills[KnifeKill]++;
					   		}
					   		else
					   		{
					   			m_dicKnifeKills.Add(KnifeKill,1);
					   		}
					   }
				}
			}
        }
        
        public List<string> SQLquery(string str_selectSQL, int sort)
		{	
        	this.tablebuilder();
			List<string> query = new List<string>();
			int int_counter = 0;
			double khr = 0;
			double kdr = 0;
			int rank = 0;
			string strRow = "";
			if ((m_strHost != null) || (m_strDatabase != null) || (m_strDBPort != null) || (m_strUserName != null) || (m_strPassword != null))
                {
                    	try
                        {
                    		this.ODBC_Connection_is_activ = true;
                    		this.OpenOdbcConnection(1);
                    		OdbcParameter param = new OdbcParameter();
                            if(OdbcCon.State == ConnectionState.Open)
                            {
								//Reader
								using(OdbcComm = new System.Data.Odbc.OdbcCommand(str_selectSQL,OdbcCon))
						    	{
									OdbcDR = OdbcComm.ExecuteReader();						
									
										// i known it is a work around got trouble return an array
										switch(sort)
										{
										case 1:
											while(OdbcDR.Read())
											{	
												//SELECT SoldierName,playerScore, playerKills, playerDeaths, playerSuicide, playerTKs, rank, allrank ,playerPlaytime, playerHeadshots, playerRounds, killstreak, deathstreak
												//			0			1				2			3				4			5		6		7			8				9				10           11		     12
												query = new List<string>(m_lstPlayerStatsMessage);
												query = this.ListReplace(query,"%playerName%",OdbcDR[0].ToString());
												query = this.ListReplace(query,"%playerScore%",OdbcDR[1].ToString());
												query = this.ListReplace(query,"%playerKills%",OdbcDR[2].ToString());
												query = this.ListReplace(query,"%playerDeaths%",OdbcDR[3].ToString());
												query = this.ListReplace(query,"%playerSuicide%",OdbcDR[4].ToString());
												query = this.ListReplace(query,"%playerTKs%",OdbcDR[5].ToString());                                                                                               
												query = this.ListReplace(query,"%playerRank%",OdbcDR[6].ToString());
												query = this.ListReplace(query,"%allRanks%",OdbcDR[7].ToString());
												query = this.ListReplace(query,"%playerHeadshots%",OdbcDR[9].ToString());
												query = this.ListReplace(query,"%rounds%",OdbcDR[10].ToString());
												query = this.ListReplace(query,"%killstreak%",OdbcDR[11].ToString());
												query = this.ListReplace(query,"%deathstreak%",OdbcDR[12].ToString());
												//KDR
												if(Convert.ToInt32(OdbcDR[3]) !=0)
												{
													kdr = Convert.ToDouble(OdbcDR[2])/Convert.ToDouble(OdbcDR[3]);
													kdr = Math.Round(kdr,2);
													query = this.ListReplace(query,"%playerKDR%",kdr.ToString());
												}
												else
												{
													kdr = Convert.ToDouble(OdbcDR[2]);
													query = this.ListReplace(query,"%playerKDR%",kdr.ToString());
												}
												//Playtime
												int playtime_sek = Convert.ToInt32(OdbcDR[8]) % 3600;
												int hours = (Convert.ToInt32(OdbcDR[8]) - playtime_sek)/ 3600;
												int sekunds = playtime_sek % 60;
												int minutes = (playtime_sek - sekunds) / 60;
												query = this.ListReplace(query,"%playerPlaytime%",hours.ToString() + ":"+ minutes.ToString() + ":"+ sekunds.ToString());
												//SPM
												double SPM;
												if(Convert.ToDouble(OdbcDR[8]) !=0)
												   {
														SPM =(Convert.ToDouble(OdbcDR[1])/(Convert.ToDouble(OdbcDR[8])/60));
														SPM = Math.Round(SPM,2);
														query = this.ListReplace(query,"%SPM%",SPM.ToString());
												   }
												   else
												   {
												   		query = this.ListReplace(query,"%SPM%","0");
												   }
											}
											break;
											
										case 2:
											query = new List<string>();
											if(m_enRankingByScore == enumBoolYesNo.Yes)
											{
												query.Add("Top 10 Player of the "+this.serverName+" Server");
											}
											else
											{
												query.Add("Top 10 Killers of the "+this.serverName+" Server");
											}
												double kdr1;
												while(OdbcDR.Read())
												{	
													if(Convert.ToDouble(OdbcDR[2]) != 0)
													{
														kdr1 = Convert.ToDouble(OdbcDR[2])/Convert.ToDouble(OdbcDR[3]);
														kdr1 = Math.Round(kdr1,2);
													}
													else
													{
														kdr1 = Convert.ToDouble(OdbcDR[2]);	
													}
													rank = rank + 1;
													if(m_enRankingByScore == enumBoolYesNo.Yes)
													{
														query.Add(rank.ToString()+".  "+OdbcDR[0]+"  Score: "+OdbcDR[1]+"  "+OdbcDR[2]+" Kills  "+OdbcDR[4]+" Headshots  "+OdbcDR[3]+" Deaths  KDR: "+kdr1.ToString());
													}
													else
													{
														query.Add(rank.ToString()+".  "+OdbcDR[0]+"  "+OdbcDR[2]+" Kills  "+OdbcDR[4]+" Headshots  "+OdbcDR[3]+" Deaths  KDR: "+kdr1.ToString());
													}
												}
											
										break;
											
										case 3:
											query = new List<string>();
											query.Add("0");
											while(OdbcDR.Read())
											{
												if(OdbcDR[0].ToString() != null)
												{
													query[0] = OdbcDR[0].ToString();
												}
												else
												{
													query[0] = "0";
												}
											}
										break;
										
										case 4:
											
											while(OdbcDR.Read())
										{
											query = new List<string>(this.m_lstWeaponstatsMsg);
											if(OdbcDR[0].ToString() != null || OdbcDR[1].ToString() != null || OdbcDR[2].ToString() != null)
											{
												query = this.ListReplace(query,"%playerKills%",OdbcDR[0].ToString());
												query = this.ListReplace(query,"%playerHeadshots%",OdbcDR[1].ToString());
												query = this.ListReplace(query,"%playerDeaths%",OdbcDR[2].ToString());
												query = this.ListReplace(query,"%playerRank%",OdbcDR[3].ToString());
												query = this.ListReplace(query,"%allRanks%",OdbcDR[4].ToString());
												                                                                                                                             
												if(Convert.ToDouble(OdbcDR[0]) != 0)
												{
													khr = Convert.ToDouble(OdbcDR[1])/Convert.ToDouble(OdbcDR[0]);
													khr = Math.Round(khr,2);
													khr = khr*100;
												}
												else
												{
													khr = 0;
												}
												if(Convert.ToDouble(OdbcDR[2]) != 0)
												{
													kdr = Convert.ToDouble(OdbcDR[0])/Convert.ToDouble(OdbcDR[2]);
													kdr = Math.Round(kdr,2);
												}
												else
												{
													kdr = Convert.ToDouble(OdbcDR[2]);	
												}
												
												query = this.ListReplace(query,"%playerKHR%",khr.ToString());
												query = this.ListReplace(query,"%playerKDR%",kdr.ToString());
											}
											else
											{
												query.Clear();
											}
										}
										break;
										case 5:
										query = new List<string>();
											query.Add("Top 10 Killers with %Weapon% of the "+this.serverName+" Server");
											
												while(OdbcDR.Read())
												{	
													if(Convert.ToDouble(OdbcDR[3]) != 0)
													{
														kdr = Convert.ToDouble(OdbcDR[1])/Convert.ToDouble(OdbcDR[3]);
														kdr = Math.Round(kdr,2);
													}
													else
													{
														kdr = Convert.ToDouble(OdbcDR[1]);	
													}
													if(Convert.ToDouble(OdbcDR[1]) != 0)
													{
														khr = Convert.ToDouble(OdbcDR[2])/Convert.ToDouble(OdbcDR[1]);
														khr = Math.Round(khr,4);
														khr = khr*100;
													}
													else
													{
														khr = 0;
													}
													rank = rank +1;
													query.Add(rank.ToString()+".  "+OdbcDR[0]+"  "+OdbcDR[1]+" Kills  "+OdbcDR[2]+" |Headshots  "+OdbcDR[3]+" killed by this Weapon  |Headshotrate: "+khr.ToString());
													
												}
										
										break;
										
										case 6:
										query = new List<string>();
											query.Add("Your favorite Victims:");
											
												while(OdbcDR.Read())
												{	
													query.Add(" "+OdbcDR[1]+"x  "+OdbcDR[0]);
												}
										
										break;
										
										case 7:
										query = new List<string>();
											query.Add("Your worst Enemies:");
											
												while(OdbcDR.Read())
												{	
													query.Add(" "+OdbcDR[1]+"x  "+OdbcDR[0]);
												}
										
										break;
										
										case 8:
										query = new List<string>();
										query.Add("0");
												while(OdbcDR.Read())
												{	
													query = new List<string>();
													query.Add(OdbcDR[0].ToString());
												}
										
										break;
										//Tablecheck
										case 9:
										query = new List<string>();
												while(OdbcDR.Read())
												{	
													query.Add(OdbcDR[0].ToString());
												}
										break;
										
										case 10:
										query = new List<string>();
										query.Add("0");
												while(OdbcDR.Read())
												{	
													query = new List<string>();
													query.Add(OdbcDR[0].ToString());
													query.Add(OdbcDR[1].ToString());
													query.Add(OdbcDR[2].ToString());
													query.Add(OdbcDR[3].ToString());
													query.Add(OdbcDR[4].ToString());
													query.Add(OdbcDR[5].ToString());
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
						
						catch(OdbcException oe)
                        {
							this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in SQLQuery in Case "+ sort +":  ");
                        	this.DisplayOdbcErrorCollection(oe);
                        }
						catch (Exception c)                    
						{
                        	this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in SQLQuery in Case "+ sort +":  " + c);
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
        	string CHECK ="SELECT `PlayerID` FROM " + this.tbl_playerdata + @" WHERE SoldierName ='"+ strSoldierName +"'";
        	int playerID = 0;
        	List<string> result;
        	try
        	{
        		if(this.m_ID_cache.ContainsKey(strSoldierName))
        	   	{
        			if(this.m_ID_cache[strSoldierName].Id >= 1)
        			{
        			
        	   			playerID = this.m_ID_cache[strSoldierName].Id;
        	   			//this.DebugInfo("Status ID-Cache: used ID from cache "+ playerID);
        			}
        			else
        			{
        				result = new List<string>(this.SQLquery(CHECK,3));
        				if(result != null)
        				{
        					foreach(string entry in result)
							{
								playerID = Convert.ToInt32(entry);
							}
        				}
        				else
        				{
        					playerID = -1;
        				}
        				
               			//this.DebugInfo("Received ID from Database ID: "+ playerID);
        			}
        	   	}
        	   	else
        	   	{
        	   		result = new List<string>(this.SQLquery(CHECK,3));
        	   		if(result != null)
        	   		{
               			playerID = Convert.ToInt32(result[0]);
               			if(playerID >= 1)
               			{
               				//this.DebugInfo("Received ID from Database ID: "+ playerID+ "Added to cache");
               				C_ID_Cache AddID = new C_ID_Cache(playerID,true);
               				this.m_ID_cache.Add(strSoldierName,AddID);
               			}
        	   		}
        	   		else
        	   		{
        	   			playerID = 0;
        	   		}
        	   	}
        	}
        	catch (Exception c)
        	{
        		this.ExecuteCommand("procon.protected.pluginconsole.write", "Error GetID: "+c);
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
        		
            if (isStreaming)
            {
                // Uploads chat logs and Stats for round to database
                if (ChatLog.Count > 0 || this.m_enLogSTATS == enumBoolYesNo.Yes)
                {
                	this.tablebuilder(); //Build the tables if not exists
                    if ((m_strHost != null) && (m_strDatabase != null) && (m_strDBPort != null) && (m_strUserName != null) && (m_strPassword != null))
                    {
                        try
                        {	
                        	this.ODBC_Connection_is_activ = true;
                            OdbcParameter param = new OdbcParameter();
                            this.OpenOdbcConnection(2);

                            if(ChatLog.Count > 0 && OdbcConn.State == ConnectionState.Open)
                            {
                            	string ChatSQL = @"INSERT INTO "+ this.tbl_chatlog + @" (logDate, logServer, logSubset, logSoldierName, logMessage) 
													VALUES ";
                            	lock(ChatLog)
                            	{
                            		foreach (CLogger log in ChatLog)
                            		{
                            			ChatSQL = string.Concat(ChatSQL,"(?,?,?,?,?),");
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
                            
                            if( this.m_mapstatsON == enumBoolYesNo.Yes && OdbcConn.State == ConnectionState.Open)
                            {
                            	this.Mapstats.calcMaxMinAvgPlayers();
                            	string MapSQL = @"INSERT INTO "+ tbl_mapstats + @" (TimeMapLoad, TimeRoundStarted, TimeRoundEnd, MapName, Gamemode, Roundcount, NumberofRounds, MinPlayers, AvgPlayers, MaxPlayers, PlayersJoinedServer, PlayersLeftServer)
													VALUES (?,?,?,?,?,?,?,?,?,?,?,?)";
                            		
                            			using (OdbcCommand OdbcCom = new OdbcCommand(MapSQL, OdbcConn))
                                		{
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.TimeMaploaded);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.TimeMapStarted);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.TimeRoundEnd);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.StrMapname);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.StrGamemode);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.IntRound);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.IntNumberOfRounds);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.IntMinPlayers);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.DoubleAvgPlayers);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.IntMaxPlayers);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.IntplayerjoinedServer);
                                    		OdbcCom.Parameters.AddWithValue("@pr", this.Mapstats.IntplayerleftServer);
                                    		OdbcCom.ExecuteNonQuery();
                                		}

                            }
                           	if (this.m_enLogSTATS == enumBoolYesNo.Yes && OdbcConn.State == ConnectionState.Open)	
                            {
                           		OdbcTrans = OdbcConn.BeginTransaction();	
                           		foreach(KeyValuePair<string, CStats> kvp in StatsTrackerCopy)
                           		{
                           			if(kvp.Key.Length > 0 && StatsTrackerCopy[kvp.Key].Guid.Length > 0)
                                	{
                           				int_id = GetID(kvp.Key);//Call of the ID Cache
                                		if(int_id >= 1)
                                		{
                                			string UpdatedataSQL ="";
                                			if(this.m_UpdateEA_GUID == enumBoolYesNo.Yes)
                                			{
                                				UpdatedataSQL = @"UPDATE " + tbl_playerdata + @" SET ClanTag = ?, EAGUID = ?, IP_Address = ?, CountryCode = ?  WHERE PlayerID = ?"; 
                                			}
                                			else
                                			{
                                				UpdatedataSQL = @"UPDATE " + tbl_playerdata + @" SET ClanTag = ?, IP_Address = ?, CountryCode = ? WHERE PlayerID = ?"; 
                                			}					
		                           			using (OdbcCommand OdbcCom = new OdbcCommand(UpdatedataSQL, OdbcConn, OdbcTrans))
	                                    	{
		                           				//Insert
		                           				OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].ClanTag);
		                           				if(this.m_UpdateEA_GUID == enumBoolYesNo.Yes)
                                				{
		                           					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].EAGuid);
		                           				}
		                           				OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].IP);
		                           				OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].PlayerCountryCode);
		                           				OdbcCom.Parameters.AddWithValue("@pr", int_id);
		                           				OdbcCom.ExecuteNonQuery();
		                           			}
		                           			if(this.m_UpdatePB_GUID == enumBoolYesNo.Yes)
		                           			{
		                           				UpdatedataSQL = @"UPDATE " + tbl_playerdata + @" SET GUID = ? WHERE PlayerID = ? AND GUID = NULL"; 
		                           				using (OdbcCommand OdbcCom = new OdbcCommand(UpdatedataSQL, OdbcConn, OdbcTrans))
	                                    		{
		                           					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Guid);
		                           					OdbcCom.Parameters.AddWithValue("@pr", int_id);
		                           					OdbcCom.ExecuteNonQuery();
		                           				}
		                           			}
                                		}
                                		else if(int_id != -1)
                                		{
		                           			string InsertdataSQL = @"INSERT INTO " + tbl_playerdata + @" (ClanTag, SoldierName, GUID, EAGUID, IP_Address, CountryCode) VALUES(?,?,?,?,?,?)"; 						
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
                           		
                           	//Start of the Transaction
                            	OdbcTrans = OdbcConn.BeginTransaction();	
                            	foreach(KeyValuePair<string, CStats> kvp in StatsTrackerCopy)
                            	{	
                            		Dictionary<string, CStats.CUsedWeapon> tempdic = new Dictionary<string, CStats.CUsedWeapon>();
                            		tempdic = StatsTrackerCopy[kvp.Key].getWeaponKills();
                            		
                            				int_id = GetID(kvp.Key);//Call of the ID Cache
                            				if(kvp.Key.Length > 0 && StatsTrackerCopy[kvp.Key].Guid.Length > 0 && int_id > 0)
                                			{
                            					string playerstatsSQL= @"INSERT INTO " + this.tbl_playerstats + @"(StatsID, playerScore, playerKills, playerHeadshots, playerDeaths, playerSuicide, playerTKs, playerPlaytime, playerRounds, FirstSeenOnServer, LastSeenOnServer, Killstreak, Deathstreak)
																			VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?) ON DUPLICATE KEY UPDATE playerScore = playerScore + ?,
																																    playerKills = playerKills + ?,
																																	playerHeadshots = playerHeadshots + ?,
																																	playerDeaths = playerDeaths + ?,
																																	playerSuicide = playerSuicide + ?,
																																	playerTKs = playerTKs + ?,
																																	playerPlaytime = playerPlaytime + ?,
																																	playerRounds = playerRounds + ?, 
																																	LastSeenOnServer = ?, 
                            																										Killstreak = GREATEST(Killstreak,?),
                            																										Deathstreak = GREATEST(Deathstreak, ?)";
                            				using (OdbcCommand OdbcCom = new OdbcCommand(playerstatsSQL, OdbcConn, OdbcTrans))
                                    				{	
                            							//Insert
                            							OdbcCom.Parameters.AddWithValue("@pr", int_id);
                            							OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].TotalScore);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Kills);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Headshots);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Deaths);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Suicides);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Teamkills);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].TotalPlaytime);
                                    					OdbcCom.Parameters.AddWithValue("@pr", 1);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].TimePlayerjoined);
                                    					if(StatsTrackerCopy[kvp.Key].TimePlayerleft != DateTime.MinValue)
                                    					{
                                    						OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].TimePlayerleft);
                                    					}
                                    					else
                                    					{
                                    						OdbcCom.Parameters.AddWithValue("@pr",MyDateTime.Now);
                                    					}
                            							OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Killstreak);
                            							OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Deathstreak);
                            							
                            							//Update
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].TotalScore);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Kills);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Headshots);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Deaths);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Suicides);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Teamkills);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].TotalPlaytime);
                                    					OdbcCom.Parameters.AddWithValue("@pr", 1);
                                    					if(StatsTrackerCopy[kvp.Key].TimePlayerleft != DateTime.MinValue)
                                    					{
                                    						OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].TimePlayerleft);
                                    					}
                                    					else
                                    					{
                                    						OdbcCom.Parameters.AddWithValue("@pr",MyDateTime.Now);
                                    					}
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Killstreak);
                            							OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].Deathstreak);
                                    					OdbcCom.ExecuteNonQuery();
                                    				}
                            					
                                				string InsertSQL = "INSERT INTO " + this.tbl_weaponstats + @" (WeaponStatsID";
                                				string ValuesSQL = "VALUES(?";
                                				string UpdateSQL= "ON DUPLICATE KEY UPDATE UNKNOWN_hs = UNKNOWN_hs + 0";
                                				if(this.m_weaponstatsON == enumBoolYesNo.Yes)
                                				{
                                				//Build Query for Weaponstats
                            						if(tempdic !=null)
                            						{
                            							foreach(KeyValuePair<string, CStats.CUsedWeapon> entry in tempdic)
                            							{
                            								
                            								if(tempdic[entry.Key].Kills != 0)
                            								{
                            									InsertSQL = String.Concat(InsertSQL,", `", entry.Key, "_kills`");
                            									ValuesSQL = String.Concat(ValuesSQL, ", " , tempdic[entry.Key].Kills);
                            								}
                            								if(tempdic[entry.Key].Headshots != 0)
                            								{
                            									InsertSQL = String.Concat(InsertSQL,", `", entry.Key, "_hs`");
                            									ValuesSQL = String.Concat(ValuesSQL, ", " , tempdic[entry.Key].Headshots);
                            								}
                            								if(tempdic[entry.Key].Deaths != 0)
                            								{
                            									InsertSQL = String.Concat(InsertSQL,", `", entry.Key, "_deaths`");
                            									ValuesSQL = String.Concat(ValuesSQL, ", " , tempdic[entry.Key].Deaths);
                            								}
                            							}
                            						}
                            					
                            						if(tempdic !=null)
                            						{
                            							foreach(KeyValuePair<string, CStats.CUsedWeapon> entry in tempdic)
                            							{		
                            								if(tempdic[entry.Key].Kills != 0)
                            								{
                            									UpdateSQL = String.Concat(UpdateSQL," `",entry.Key, "_kills` = `",entry.Key,"_kills` + ",tempdic[entry.Key].Kills," ,");
                            									UpdateSQL = UpdateSQL.Replace("UNKNOWN_hs = UNKNOWN_hs + 0","");
                            								}
                            								if(tempdic[entry.Key].Headshots != 0)
                            								{
                            									UpdateSQL = String.Concat(UpdateSQL," `",entry.Key, "_hs` = `",entry.Key,"_hs` + ",tempdic[entry.Key].Headshots," ,");
                            									UpdateSQL = UpdateSQL.Replace("UNKNOWN_hs = UNKNOWN_hs + 0","");
                            								}
                            								if(tempdic[entry.Key].Deaths != 0)
                            								{
                            									UpdateSQL = String.Concat(UpdateSQL," `",entry.Key, "_deaths` = `",entry.Key,"_deaths` + ",tempdic[entry.Key].Deaths," ,");
                            									UpdateSQL = UpdateSQL.Replace("UNKNOWN_hs = UNKNOWN_hs + 0","");
                            								}	
                            							}
                            						}
                            						int charindex = UpdateSQL.LastIndexOf(",");
                            						if(charindex > 0)
                            						{
                            							UpdateSQL = UpdateSQL.Remove(charindex);
                            						}
                            						InsertSQL = String.Concat(InsertSQL,") ",ValuesSQL,") ",UpdateSQL);
                            						using (OdbcCommand OdbcCom = new OdbcCommand(InsertSQL, OdbcConn, OdbcTrans))
                                    				{	
                                    					OdbcCom.Parameters.AddWithValue("@pr", int_id);
                                    					OdbcCom.ExecuteNonQuery();
                                    				}
                                				}
                                				string sqlBfbcs = "INSERT INTO " + tbl_bfbcs + @" (bfbcsID, Rank, Kills, Deaths, Score, Elo, Level, Time, LastUpdate) VALUES (?,?,?,?,?,?,?,?,?) 
  																	ON DUPLICATE KEY UPDATE Rank = ?, Kills = ?, Deaths = ?, Score = ?, Elo = ?, Level = ?, Time = ?, LastUpdate = ?";
                                				if(StatsTrackerCopy[kvp.Key].BFBCS_Stats.Rank > 0 && StatsTrackerCopy[kvp.Key].BFBCS_Stats.NoUpdate == false)
                                				using (OdbcCommand OdbcCom = new OdbcCommand(sqlBfbcs, OdbcConn, OdbcTrans))
                                    				{	
                                    					OdbcCom.Parameters.AddWithValue("@pr", int_id);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Rank);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Kills);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Deaths);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Score);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Elo);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Skilllevel);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Time);
                                    					OdbcCom.Parameters.AddWithValue("@pr", MyDateTime.Now);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Rank);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Kills);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Deaths);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Score);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Elo);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Skilllevel);
                                    					OdbcCom.Parameters.AddWithValue("@pr", StatsTrackerCopy[kvp.Key].BFBCS_Stats.Time);
                                    					OdbcCom.Parameters.AddWithValue("@pr", MyDateTime.Now);
                                    					OdbcCom.ExecuteNonQuery();
                                    				}
                            				}
                                		}
                            	string KnifeSQL = "";
                            	foreach(KeyValuePair<CKillerVictim, int> kvp in m_dicKnifeKills)
                            	{
                            		int killerID = this.GetID(kvp.Key.Killer);
                            		int victimID = this.GetID(kvp.Key.Victim);
                            		if( killerID > 0 && victimID > 0)
                            		{
                            			KnifeSQL = "INSERT INTO " + this.tbl_dogtags + @"( KillerID, VictimID, Count) VALUES(?,?,?)
                            						ON DUPLICATE KEY UPDATE Count = Count + ?";
                            			using (OdbcCommand OdbcCom = new OdbcCommand(KnifeSQL, OdbcConn, OdbcTrans))
                                   		{
                            				OdbcCom.Parameters.AddWithValue("@pr",killerID);
                            				OdbcCom.Parameters.AddWithValue("@pr",victimID);
                            				OdbcCom.Parameters.AddWithValue("@pr",m_dicKnifeKills[kvp.Key]);
                            				OdbcCom.Parameters.AddWithValue("@pr",m_dicKnifeKills[kvp.Key]);
                            				OdbcCom.ExecuteNonQuery();
                            			}
                            		}
                            	}
                            		
                            	    //Commit the Transaction for the Playerstats
                            		OdbcTrans.Commit();

                            		StatsTrackerCopy.Clear();
                            		this.m_dicKnifeKills.Clear();
                            		
									List<string> leftplayerlist = new List<string>();
									
                        				foreach(KeyValuePair<string,C_ID_Cache> kvp in this.m_ID_cache)
                            			{
                        					if(this.m_ID_cache[kvp.Key].PlayeronServer == false)
                            				{
                            					leftplayerlist.Add(kvp.Key);
                            				}
                            				// Because so playerleft event seems not been reported by the server
                            				this.m_ID_cache[kvp.Key].PlayeronServer = false;
                            				
                            			}
                        				foreach(string player in leftplayerlist)
                        				{
                        					
                        					this.m_ID_cache.Remove(player);
                        					//this.DebugInfo("Removed " + player);
                        				}
                        				
                            	
                            	this.DebugInfo("Status ID-Cache: "+m_ID_cache.Count+" ID's in cache");
                            	if(this.m_ID_cache.Count > 300)
                            	{
                            		this.ExecuteCommand("procon.protected.pluginconsole.write","Forced Cache clear due the Nummber of cached IDs reached over 300 entry(overflowProtection)");
                            	}
                            	
                            
                    		}
							else
							{	
                           		StatsTracker.Clear();
							}
							this.ODBC_Connection_is_activ = false;
							
                        }
                        catch(OdbcException oe)
                        {
                        	this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in Startstreaming: ");
                        	this.DisplayOdbcErrorCollection(oe);
                        }
                        catch (Exception c)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in Startstreaming: " + c);
                            OdbcTrans.Rollback();
                            this.m_ID_cache.Clear();
                            this.m_dicKnifeKills.Clear();
                        }
                        
                        finally
                        {
                        	this.Mapstats = this.Nextmapinfo;
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
            }
        }
        
        public void WelcomeStats(string strSpeaker)
        {
        	if(this.m_enWelcomeStats == enumBoolYesNo.Yes)
        	{
        		if (this.m_enLogSTATS == enumBoolYesNo.Yes)
        		{		
        			string SQL ="";
        			string strMSG ="";
        			int sort =0;
              		//Statsquery with KDR
					//Rankquery
					if(m_enRankingByScore == enumBoolYesNo.Yes)
					{
						SQL =@"SELECT a.SoldierName,y.playerScore, y.playerKills, y.playerDeaths, y.playerSuicide, y.playerTKs, y.rank, y.allrank ,y.playerPlaytime, y.playerHeadshots, y.playerRounds, y.Killstreak, y.Deathstreak 
									FROM (SELECT(@num := @num+1) rank, (SELECT count(*) 
									FROM "+ this.tbl_playerstats + @") allrank, StatsID, b.playerScore, b.playerKills, b.playerDeaths, b.playerSuicide, b.playerTKs, b.playerPlaytime, b.playerHeadshots, b.playerRounds, b.Killstreak, b.Deathstreak  
									FROM " + this.tbl_playerstats + @" b , (SELECT @num := 0) x 
                            		ORDER BY playerScore DESC) y 
									INNER JOIN " + tbl_playerdata + @" a ON a.PlayerID = y.StatsID
									WHERE SoldierName ='"+strSpeaker+"'";
					}
					else
					{
						SQL =@"SELECT a.SoldierName,y.playerScore, y.playerKills, y.playerDeaths, y.playerSuicide, y.playerTKs, y.rank, y.allrank ,y.playerPlaytime, y.playerHeadshots, y.playerRounds, y.Killstreak, y.Deathstreak 
									FROM (SELECT(@num := @num+1) rank, (SELECT count(*) 
									FROM " + this.tbl_playerstats + @") allrank, StatsID, b.playerScore, b.playerKills, b.playerDeaths, b.playerSuicide, b.playerTKs, b.playerPlaytime, b.playerHeadshots, b.playerRounds, b.Killstreak, b.Deathstreak  
									FROM " + this.tbl_playerstats + @" b , (SELECT @num := 0) x 
                            		ORDER BY playerKills DESC, playerDeaths ASC) y 
									INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = y.StatsID
									WHERE SoldierName ='"+strSpeaker+"'";
					}
					
					sort = 1; 
					List<string> result = new List<string>(this.SQLquery(SQL,sort));
					this.CloseOdbcConnection(1);
					if(result.Count > 0)
				 	{	
						strMSG = m_strPlayerWelcomeMsg;
						strMSG = strMSG.Replace("%serverName%",this.serverName);
						strMSG = strMSG.Replace("%playerName%",strSpeaker);
						
						this.CheckMessageLength(strMSG);
						if(m_enYellWelcomeMSG == enumBoolYesNo.Yes)
						{
							this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", int_welcomeStatsDelay.ToString(), "1", "1", "procon.protected.send", "admin.yell", strMSG,this.m_iDisplayTime.ToString(),"player", strSpeaker);
						}
						else
						{
							this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", int_welcomeStatsDelay.ToString(), "1", "1", "procon.protected.send", "admin.say", strMSG, "player", strSpeaker);
						}
						foreach(string line in result)
						{
							this.CheckMessageLength(line);
							this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", int_welcomeStatsDelay.ToString(), "1", "1", "procon.protected.send", "admin.say", line, "player", strSpeaker);
						}
				 	}
					else
					{	strMSG = m_strNewPlayerWelcomeMsg;
						strMSG = strMSG.Replace("%serverName%",this.serverName);
						strMSG = strMSG.Replace("%playerName%",strSpeaker);
						this.CheckMessageLength(strMSG);
						if(m_enYellWelcomeMSG == enumBoolYesNo.Yes)
						{
							this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", int_welcomeStatsDelay.ToString(), "1", "1", "procon.protected.send" , "admin.yell", strMSG, this.m_iDisplayTime.ToString(),"player", strSpeaker);
						}
						else
						{
							this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", int_welcomeStatsDelay.ToString(), "1", "1", "procon.protected.send", "admin.say", strMSG,"player", strSpeaker);
						}
					}
            	}
        		
        	}
        }
        
        public void GetPlayerStats(string strSpeaker,int delay, string scope)
        {
        	if (this.m_enLogSTATS == enumBoolYesNo.Yes)
        	{	
        		string SQL ="";
        		int sort =0;
              	//Statsquery with KDR
				//Rankquery
				if(m_enRankingByScore == enumBoolYesNo.Yes)
				{
					SQL =@"SELECT a.SoldierName,y.playerScore, y.playerKills, y.playerDeaths, y.playerSuicide, y.playerTKs, y.rank, y.allrank ,y.playerPlaytime, y.playerHeadshots, y.playerRounds, y.Killstreak, y.Deathstreak 
									FROM (SELECT(@num := @num+1) rank, (SELECT count(*) 
									FROM "+ this.tbl_playerstats + @") allrank, StatsID, b.playerScore, b.playerKills, b.playerDeaths, b.playerSuicide, b.playerTKs, b.playerPlaytime, b.playerHeadshots, b.playerRounds, b.Killstreak, b.Deathstreak  
									FROM " + this.tbl_playerstats+ @" b , (SELECT @num := 0) x 
                            		ORDER BY playerScore DESC) y 
									INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = y.StatsID
									WHERE SoldierName ='"+strSpeaker+"'";
				}
				else
				{
					SQL =@"SELECT a.SoldierName,y.playerScore, y.playerKills, y.playerDeaths, y.playerSuicide, y.playerTKs, y.rank, y.allrank ,y.playerPlaytime, y.playerHeadshots, y.playerRounds, y.Killstreak, y.Deathstreak 
									FROM (SELECT(@num := @num+1) rank, (SELECT count(*) 
									FROM " + this.tbl_playerstats + @") allrank, StatsID, b.playerScore, b.playerKills, b.playerDeaths, b.playerSuicide, b.playerTKs, b.playerPlaytime, b.playerHeadshots, b.playerRounds, b.Killstreak, b.Deathstreak  
									FROM " + this.tbl_playerstats + @" b , (SELECT @num := 0) x 
                            		ORDER BY playerKills DESC, playerDeaths ASC) y 
									INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = y.StatsID
									WHERE SoldierName ='"+strSpeaker+"'";
				}
					
				sort = 1; 
				List<string> result = new List<string>(this.SQLquery(SQL,sort));
				this.CloseOdbcConnection(1);
				//if(result[0].Equals("0") == false)
				if(result.Count != 0)
				{
					foreach(string line in result)
					{
						this.CheckMessageLength(line);
						if(String.Equals(scope,"all"))
						   {
								this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"all");
						   }
						   else
						   {
						   		this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"player",strSpeaker);
						   }
					}
				}
				else
        		{
        			this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say","No Stats are available yet! Please wait one Round!","player", strSpeaker);
        		}
				
            }
        	
        }
        
        public void GetTop10(string strSpeaker,int delay, string scope)
        {
        	if (this.m_enTop10ingame == enumBoolYesNo.Yes)
        	{	
        		int sort =0;
        		string SQL ="";
              	//Top10 Query
              	if(this.m_enRankingByScore == enumBoolYesNo.Yes)
              	{
              		SQL =@"SELECT SoldierName,playerScore, playerKills, playerDeaths, playerHeadshots 
              				FROM " + this.tbl_playerstats + @" b
							INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = b.StatsID
							ORDER BY playerScore DESC LIMIT 10";
              	}
              	else
              	{
              		SQL =@"SELECT SoldierName,playerScore, playerKills, playerDeaths, playerHeadshots 
              				FROM " + this.tbl_playerstats + @" b
							INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = b.StatsID
							ORDER BY playerKills DESC, playerDeaths ASC  LIMIT 10";
              	}
					
				sort = 2; 
				List<string> result = new List<string>(this.SQLquery(SQL,sort));
				this.CloseOdbcConnection(1);
				if(result[0].Equals("0") == false)
				 {
					int top10Delay = 0;
					foreach(string line in result)
					{
						this.CheckMessageLength(line);
						if(String.Equals(scope,"all"))
						   {
								this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", top10Delay.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"all");
						   }
						   else
						   {
						   		this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", top10Delay.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"player", strSpeaker);
						   }
						top10Delay += delay;
					}
				 }	
        	}
        }

        public void GetWeaponStats(string strWeapon, string strPlayer, string scope)
        {	
        	int delay = 0;
        	string query = "";
        	if(String.Equals(strWeapon,"") == false)
        	{	
        		query = @"SELECT `%Weapon%_kills`, `%Weapon%_hs`, `%Weapon%_deaths`, rank, allrank 
									FROM (select(@num := @num+1) rank, (SELECT count(*) 
									FROM " + this.tbl_weaponstats + @") allrank,WeaponStatsID, `%Weapon%_kills`, `%Weapon%_hs`, `%Weapon%_deaths`  
									FROM " + this.tbl_weaponstats + @", (select @num := 0) x 
									ORDER BY `%Weapon%_kills` DESC, `%Weapon%_hs` DESC) y 
									INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = y.WeaponStatsID
									WHERE a.SoldierName = '%Player%'";
        		
        		query = query.Replace("%Weapon%",strWeapon);
        		query = query.Replace("%Player%",strPlayer);
        		
        		List<string> result = new List<string>(this.SQLquery(query,4));
        		this.CloseOdbcConnection(1);
        		result = this.ListReplace(result,"%playerName%",strPlayer);
        		result = this.ListReplace(result,"%Weapon%",strWeapon);
        		if(result[0].Equals("0") == false)
				 {
					foreach(string line in result)
					{
						this.CheckMessageLength(line);
						if(String.Equals(scope,"all"))
						   {
								this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"all");
						   }
						   else
						   {
						   		this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"player", strPlayer);
						   }
					}
				 }
        		else
        		{
        			this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say","No Stats are available for this Weapon!!!","player", strPlayer);
        		}
        	}
        	else
        	{
        		this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say","Specifc Weapon not found!!","player", strPlayer);
        	}
        }
        
        public void GetWeaponTop10(string strWeapon, string strPlayer,int delay, string scope)
        {
        	int delaytop10 = 0;
        	if(String.Equals(strWeapon,"") == false)
        	{	
        		string query = @"SELECT `b`.`SoldierName`,`%Weapon%_kills`, `%Weapon%_hs`, `%Weapon%_deaths` 
									FROM " + this.tbl_weaponstats + @" a
									INNER JOIN " + this.tbl_playerdata + @" b ON b.PlayerID = a.WeaponstatsID
									ORDER BY `%Weapon%_kills` DESC, `%Weapon%_hs` DESC  
									LIMIT 10";
        		
        		query = query.Replace("%Weapon%",strWeapon);
        		List<string> result = new List<string>(this.SQLquery(query,5));
        		this.CloseOdbcConnection(1);
        		result = this.ListReplace(result,"%Player%",strPlayer);
        		result = this.ListReplace(result,"%Weapon%",strWeapon);
        		if(result[0].Equals("0") == false)
				 {
					foreach(string line in result)
					{
						this.CheckMessageLength(line);
						if(String.Equals(scope,"all"))
						   {
								this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delaytop10.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"all");
						   }
						   else
						   {
						   		this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delaytop10.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"player", strPlayer);
						   }
						delaytop10 = delaytop10 +delay;
					}
				 }
        		else
        		{
        			this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say","No Stats are available for this Weapon!!!","player", strPlayer);
        		}
        	}
        	else
        	{
        		this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say","Specifc Weapon not found!!","player", strPlayer);
        	}
        }
        
        public void GetDogtags(string strPlayer,int delay, string scope)
        {
        	int delaydogtags = 0;
        	
        		string query = @"SELECT `SoldierName`,`Count` from " + this.tbl_dogtags + @" d 
									INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = d.VictimID 
									WHERE `KillerID` =" +this.GetID(strPlayer)+ @" 
									ORDER BY `Count` DESC Limit 3";
        		
        		string query2 = @"SELECT `SoldierName`,`Count` FROM " + this.tbl_dogtags + @" d 
									INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = d.KillerID 
									WHERE `VictimID` = " +this.GetID(strPlayer)+@" 
									ORDER BY `Count` DESC Limit 3";
        		
        		
        		List<string> result = new List<string>(this.SQLquery(query,6));
        		List<string> result2 = new List<string>(this.SQLquery(query2,7));
        		this.CloseOdbcConnection(1);
        		result.AddRange(result2);
        		if(result[0].Equals("0") == false)
				 {
					foreach(string line in result)
					{
						this.CheckMessageLength(line);
						if(String.Equals(scope,"all"))
						   {
								this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delaydogtags.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"all");
						   }
						   else
						   {
						   		this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delaydogtags.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"player", strPlayer);
						   }
						delaydogtags = delaydogtags +delay;
					}
				 }
        		else
        		{
        			this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say","No Stats are available!!!","player", strPlayer);
        		}
        }
        
        public void AddKillToStats(string strPlayerName,string weapon, bool headshot )
        {
        	
        	if(StatsTracker.ContainsKey(strPlayerName))
        	{	
        		StatsTracker[strPlayerName].addKill(weapon,headshot);
        	}
        	else
        	{	
        		CStats newEntry = new CStats("",0,0,0,0,0,0,0,this.m_dTimeOffset);
        		StatsTracker.Add(strPlayerName,newEntry);
        		StatsTracker[strPlayerName].addKill(weapon,headshot);
        	}
        	
        	//Session
        	if(m_dicSession.ContainsKey(strPlayerName) && this.m_sessionON == enumBoolYesNo.Yes)
        	{	
        		m_dicSession[strPlayerName].addKill(weapon,headshot);
        	}

        }
        
        public void AddDeathToStats(string strPlayerName,string weapon)
        {	
        	if(StatsTracker.ContainsKey(strPlayerName))
        	{	
        		StatsTracker[strPlayerName].addDeath(weapon);
        	}
        	else
        	{	
        		CStats newEntry = new CStats("",0,0,0,0,0,0,0,this.m_dTimeOffset);
        		StatsTracker.Add(strPlayerName,newEntry);
        		StatsTracker[strPlayerName].addDeath(weapon);
        	}
        	
        	//Session
        	if(m_dicSession.ContainsKey(strPlayerName) && this.m_sessionON == enumBoolYesNo.Yes)
        	{	
        		m_dicSession[strPlayerName].addDeath(weapon);
        	}
        }
        
        public void AddSuicideToStats(string strPlayerName, string weapon)
        {
        	if(StatsTracker.ContainsKey(strPlayerName))
        	{		
        		StatsTracker[strPlayerName].addDeath(weapon);
        		StatsTracker[strPlayerName].Suicides ++;
        	}
        	else
        	{	
        		CStats newEntry = new CStats("",0,0,0,0,1,0,0,this.m_dTimeOffset);
        		StatsTracker.Add(strPlayerName,newEntry);
        		StatsTracker[strPlayerName].addDeath(weapon);
        	}
        	
        	//Session
        	if(m_dicSession.ContainsKey(strPlayerName) && this.m_sessionON == enumBoolYesNo.Yes)
        	{	
        		m_dicSession[strPlayerName].addDeath(weapon);
        		m_dicSession[strPlayerName].Suicides ++;
        	}
        }
        
        public void AddTeamKillToStats(string strPlayerName)
        {
        	if(StatsTracker.ContainsKey(strPlayerName))
        	{		
        		StatsTracker[strPlayerName].Teamkills ++;
        	}
        	else
        	{	
        		CStats newEntry = new CStats("",0,0,0,0,0,1,0,this.m_dTimeOffset);
        		StatsTracker.Add(strPlayerName,newEntry);
        	}
        	
        	//Session
        	if(m_dicSession.ContainsKey(strPlayerName) && this.m_sessionON == enumBoolYesNo.Yes)
        	{	
        		m_dicSession[strPlayerName].Teamkills ++;
        	}
        	
        }
        
        public void AddPBInfoToStats(CPunkbusterInfo cpbiPlayer)
        {
        	if(StatsTracker.ContainsKey(cpbiPlayer.SoldierName))
        	{		
        		StatsTracker[cpbiPlayer.SoldierName].Guid = cpbiPlayer.GUID;
        		StatsTracker[cpbiPlayer.SoldierName].PlayerCountryCode = cpbiPlayer.PlayerCountryCode;
        		if(StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined == null)
        			StatsTracker[cpbiPlayer.SoldierName].TimePlayerjoined = MyDateTime.Now;
        	}
        	else
        	{	
        		CStats newEntry = new CStats(cpbiPlayer.GUID,0,0,0,0,0,0,0,this.m_dTimeOffset);
        		StatsTracker.Add(cpbiPlayer.SoldierName,newEntry);
        		StatsTracker[cpbiPlayer.SoldierName].PlayerCountryCode = cpbiPlayer.PlayerCountryCode;
        	}
        	
        }
        
        public void OpenOdbcConnection(int type)
        {
        	try
        	{
        		switch(type)
        		{
        			//OdbcCon
        			case 1:
	        			if(OdbcCon == null)
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
	        			if(OdbcConn == null)
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
        	catch(OdbcException oe)
       		{
        		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OpenConnection:");
             	this.DisplayOdbcErrorCollection(oe);
            }
        	catch(Exception c)
        	{
        		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OpenConnection: " + c);
        	}
        }
        
        public void CloseOdbcConnection(int type)
        {
        	if(this.ODBC_Connection_is_activ == false)
        	{
		       try
		       {	
			       	switch(type)
			       	{
			       		case 1:
				  		//OdbcCon
				  		if(this.OdbcCon != null)
					        if(this.OdbcCon.State == ConnectionState.Open)
					        {
					        	this.OdbcCon.Close();
					        	this.DebugInfo("Connection OdbcCon closed");
					        }
				  		break;
				  		
				  		case 2:
					    //ODBCConn
					    if(this.OdbcConn != null)
					        if(this.OdbcConn.State == ConnectionState.Open)
					        {
					        	this.OdbcConn.Close();
					        	this.DebugInfo("Connection OdbcConn closed");
					        }
					    break;
					   default:
					    break;
			       }
				  
		       }
		       catch(OdbcException oe)
               {
		       		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in CloseOdbcConnection:");
               		this.DisplayOdbcErrorCollection(oe);
               }
		       catch(Exception c)
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
                            string SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_chatlog + @"` (
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
                            //MapStats Table
                            SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_mapstats + @"` (
  												`ID` INT NOT NULL AUTO_INCREMENT ,
  												`TimeMapLoad` DATETIME NULL ,
  												`TimeRoundStarted` DATETIME NULL ,
  												`TimeRoundEnd` DATETIME NULL ,
  												`MapName` VARCHAR(32) NULL ,
  												`Gamemode` VARCHAR(20) NULL ,
  												`Roundcount` INT NOT NULL DEFAULT 0 ,
  												`NumberofRounds` INT NOT NULL DEFAULT 0 ,
  												`MinPlayers` INT NOT NULL DEFAULT 0 ,
  												`AvgPlayers` double NOT NULL DEFAULT 0 ,
  												`MaxPlayers` INT NOT NULL DEFAULT 0 ,
  												`PlayersJoinedServer` INT NOT NULL DEFAULT 0 ,
  												`PlayersLeftServer` INT NOT NULL DEFAULT 0 ,
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
                            SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_playerdata + @"` (
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
                             using (OdbcCommand OdbcCom  = new OdbcCommand(SQLTable, OdbcCon, OdbcTrans))
                                		{
                                    		OdbcCom.ExecuteNonQuery();
                            			}
                             
                            //BFBCS Table
                            SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_bfbcs + @"` (
				  							  `bfbcsID` INT NOT NULL ,
											  `Rank` INT NOT NULL DEFAULT 0 ,
											  `Kills` INT NOT NULL DEFAULT 0 ,
											  `Deaths` INT NOT NULL DEFAULT 0 ,
											  `Score` INT NOT NULL DEFAULT 0 ,
											  `Elo` DOUBLE NOT NULL DEFAULT 0 ,
											  `Level` DOUBLE NOT NULL DEFAULT 0 ,
											  `Time` DOUBLE NOT NULL DEFAULT 0 ,
											  `LastUpdate` DATETIME NULL DEFAULT NULL ,
											  PRIMARY KEY (`bfbcsID`) )
											ENGINE = InnoDB";
                            if(this.m_getStatsfromBFBCS == enumBoolYesNo.Yes)
                             using (OdbcCommand OdbcCom  = new OdbcCommand(SQLTable, OdbcCon, OdbcTrans))
                                		{
                                    		OdbcCom.ExecuteNonQuery();
                            			}
                             
  												
                            //Stats Table
                            SQLTable = @"CREATE  TABLE IF NOT EXISTS `" + this.tbl_playerstats + @"` (
  												`StatsID` INT UNSIGNED NULL PRIMARY KEY ,
  												`playerScore` int(11) NOT NULL DEFAULT 0,
  												`playerKills` INT UNSIGNED NOT NULL DEFAULT 0 ,
  												`playerHeadshots` INT UNSIGNED NOT NULL DEFAULT 0 ,
  												`playerDeaths` INT UNSIGNED NOT NULL DEFAULT 0 ,
  												`playerSuicide` INT UNSIGNED NOT NULL DEFAULT 0 ,
  												`playerTKs` INT UNSIGNED NOT NULL DEFAULT 0 ,
  												`playerPlaytime` INT UNSIGNED NOT NULL DEFAULT 0 ,
  												`playerRounds` INT UNSIGNED NOT NULL DEFAULT 0 ,
  												`FirstSeenOnServer` DATETIME NULL DEFAULT NULL,
                          						`LastSeenOnServer` DATETIME NULL DEFAULT NULL ,
                          						`Killstreak` INT UNSIGNED NOT NULL DEFAULT 0,
                          						`Deathstreak`INT UNSIGNED NOT NULL DEFAULT 0)
                           						ENGINE = InnoDB DEFAULT CHARACTER SET = latin1";
                            
                             using (OdbcCommand OdbcCom  = new OdbcCommand(SQLTable, OdbcCon, OdbcTrans))
                                		{
                                    		OdbcCom.ExecuteNonQuery();
                            			}
                            
                            //Weapon Table
                            SQLTable = @"CREATE  TABLE IF NOT EXISTS `" + this.tbl_weaponstats + @"` (
  												`WeaponStatsID` INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY ";
                            List<string> columnlist = new List<string>();
 							foreach(string substring in m_lstTableschema)
                            {	
                            	int charindex = substring.IndexOf("{");
                            	string columnName = substring;
                            	if(charindex > 0)
                            		columnName = substring.Remove(charindex);
                            	if(substring.Contains("/") == false && substring.Length > 0)
                            	{
                            		if(columnlist.Contains(columnName) == false)
                            		{
                            			columnlist.Add(columnName);
                            		}
                            	}
                            }
 							foreach(string strcolumn in columnlist)
                            	{
                            		SQLTable = String.Concat(SQLTable,",`",strcolumn,"_kills` INT UNSIGNED NOT NULL DEFAULT 0, ");
                            		SQLTable = String.Concat(SQLTable,"`",strcolumn,"_hs` INT UNSIGNED NOT NULL DEFAULT 0, ");
                            		SQLTable = String.Concat(SQLTable,"`",strcolumn,"_deaths` INT UNSIGNED NOT NULL DEFAULT 0 ");
                            	}
                            SQLTable = String.Concat(SQLTable,")ENGINE = InnoDB DEFAULT CHARACTER SET = latin1");
                            
                            using (OdbcCommand OdbcCom  = new OdbcCommand(SQLTable, OdbcCon, OdbcTrans))
                                		{
                                    		OdbcCom.ExecuteNonQuery();
                            			}
                            //Dogtagstable
                            SQLTable = @"CREATE TABLE IF NOT EXISTS `" + this.tbl_dogtags + @"` (
										  `KillerID` INT(10) UNSIGNED NOT NULL ,
										  `VictimID` INT(10) UNSIGNED NOT NULL ,
										  `Count` INT UNSIGNED NOT NULL DEFAULT 0 ,
										  PRIMARY KEY (`KillerID`, `VictimID`) ,
										  INDEX `VictimID_Index` (`VictimID` ASC))
										  ENGINE = InnoDB
										  DEFAULT CHARACTER SET = latin1";
                            using (OdbcCommand OdbcCom  = new OdbcCommand(SQLTable, OdbcCon, OdbcTrans))
                                		{
                                    		OdbcCom.ExecuteNonQuery();
                            			}
                            
                            //Commit the Transaction
                            OdbcTrans.Commit();
                            this.boolTableEXISTS = true;
                           
                            //TableCheck
                            string sqlCheck ="DESC `" + this.tbl_weaponstats +"`";
				        	string sqlAltertable = "ALTER TABLE `" + this.tbl_weaponstats +"` ";
				        	List<string> result = new List<string>(this.SQLquery(sqlCheck,9));
				        	bool fieldMissing = false;
				        	
				        	foreach(string substring in columnlist)
                            {	
                            	string strField = substring;
					        		if(result.Contains(strField + "_kills") == false)
					        		{
					        			this.DebugInfo(strField + "_kills" +" is missing, Adding it to the table!");
					        			sqlAltertable = string.Concat(sqlAltertable,"ADD COLUMN `"+ strField + "_kills`" +" INT(10) UNSIGNED NOT NULL DEFAULT 0, ");
					        			fieldMissing = true;
					        		}
					        		if(result.Contains(strField + "_hs") == false)
					        		{
					        			this.DebugInfo(strField + "_hs" +" is missing, Adding it to the table!");
					        			sqlAltertable = string.Concat(sqlAltertable,"ADD COLUMN `"+ strField + "_hs`" +" INT(10) UNSIGNED NOT NULL DEFAULT 0, ");
					        			fieldMissing = true;
					        		}
					        		if(result.Contains(strField + "_deaths") == false)
					        		{
					        			this.DebugInfo(strField + "_deaths" +" is missing, Adding it to the table!");
					        			sqlAltertable = string.Concat(sqlAltertable,"ADD COLUMN `"+ strField + "_deaths`" +" INT(10) UNSIGNED NOT NULL DEFAULT 0, ");
					        			fieldMissing = true;
					        		}
                            	
				        	}
				        	if(fieldMissing == true)
				        	{
				        		OdbcTrans = OdbcCon.BeginTransaction();	
				        		SQLTable = "ALTER TABLE `" + this.tbl_weaponstats +"` ENGINE = MyISAM";
				        		using (OdbcCommand OdbcCom  = new OdbcCommand(SQLTable, OdbcCon, OdbcTrans))
                               		 {
                                		OdbcCom.ExecuteNonQuery();
                            		 }
								//Adding Columns
								int charindex = sqlAltertable.LastIndexOf(",");
                            	if(charindex > 0)
                            	{
                            		sqlAltertable = sqlAltertable.Remove(charindex);
                            	}
				        		using (OdbcCommand OdbcCom  = new OdbcCommand(sqlAltertable, OdbcCon, OdbcTrans))
                                	{
                               		 	OdbcCom.ExecuteNonQuery();
                            		}
				        		SQLTable = "ALTER TABLE `" + this.tbl_weaponstats +"` ENGINE = InnoDB";
				        		using (OdbcCommand OdbcCom  = new OdbcCommand(SQLTable, OdbcCon, OdbcTrans))
                                	{
                                		OdbcCom.ExecuteNonQuery();
                            		}
				        		OdbcTrans.Commit();
				        	}
				        	else
				        	{
				        		this.DebugInfo("Your Weapontable is containing all weapons known so far, if you got Error after this( missing Weapons) contact me pls");
				        	}

                    	}
                    
    					catch(OdbcException oe)
                        {
    						this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in Tablebuilder: ");
                        	this.DisplayOdbcErrorCollection(oe);
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
			if(this.m_enChatloggingON == enumBoolYesNo.No)
			{
				return;
			}
        	if(this.m_enNoServerMsg == enumBoolYesNo.No && strSpeaker.CompareTo("Server") == 0)
        	{
        		return;
            }
        	else if(m_enInstantChatlogging == enumBoolYesNo.Yes)
        	{
        		string query = "INSERT INTO " + this.tbl_chatlog + @" (logDate, logServer, logSubset, logSoldierName, logMessage) VALUES (?,?,?,?,?)";
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
                    			OdbcCom.Parameters.AddWithValue("@pr", MyDateTime.Now);
                            	OdbcCom.Parameters.AddWithValue("@pr", this.serverName);
                            	OdbcCom.Parameters.AddWithValue("@pr", strType);
                            	OdbcCom.Parameters.AddWithValue("@pr", strSpeaker);
                            	OdbcCom.Parameters.AddWithValue("@pr", strMessage);
                            	OdbcCom.ExecuteNonQuery();
                        	}
                    		this.CloseOdbcConnection(1);
                    	}

                    }
                    catch(OdbcException oe)
                    {
                    	this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in LogChat: ");
               		   	this.DisplayOdbcErrorCollection(oe);
                    }
                    catch(Exception c)
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
        			CLogger chat = new CLogger(MyDateTime.Now, strSpeaker, strMessage, strType);
        			ChatLog.Add(chat);
        		}
        }     
 
        public ArrayList TextFileReader(string textfile)
        {
        	StreamReader objReader = new StreamReader(textfile);
        	string sline ="";
        	ArrayList arrText = new ArrayList();
        	
        	while(sline != null)
        	{
        		sline = objReader.ReadLine();
        		if(sline != null)
        		{
        			arrText.Add(sline);
        		}
        		
        	}
        	objReader.Close();
        	return arrText;
        }        
        
        public void DebugInfo(string DebugMessage)
        {
        	if(m_enDebugMode == enumBoolYesNo.Yes)
        	{
        		this.ExecuteCommand("procon.protected.pluginconsole.write","^4" + DebugMessage);
        	}
        }
                       
        public void PrepareKeywordDic()
        {
        	this.m_dicKeywords.Clear();
        	if(boolKeywordDicReady == false)
        	{
        		try
        		{
        			string dicKey = "";
        			string dicValue = "";
        			int bracket1 = 0;
        			int bracket2 = 0;
/*
        			ArrayList arrText = this.TextFileReader("tableconfig.cfg");
        			arrText.Sort();
        			foreach(string line in arrText)
*/
					foreach(string line in m_lstTableconfig)
        			{
        				if(line.Contains("/") == false)
        				{
        					if(line.Contains("{") && line.Contains("}"))
        					{	
        						bracket1 = line.IndexOf("{");
        						dicKey = line.Remove(bracket1);
        						
        						dicValue = line.Replace("{",",");
        						dicValue = dicValue.Replace("}","");
        						string[] arrStrings = Regex.Split(dicValue,",");
        						if(this.m_dicKeywords.ContainsKey(dicKey) == false)
        						{
        							this.m_dicKeywords.Add(dicKey,new List<string>());
        						}
        						foreach(string substring in arrStrings)
        						{
        							this.m_dicKeywords[dicKey].Add(substring);
        						}
        						this.m_dicKeywords[dicKey].Sort();
        					}
        					else
        					{
        						dicKey = line.Replace(" ","");
        						bracket1 = dicKey.IndexOf("{");
        						if(bracket1 > 0)
        							dicKey = dicKey.Remove(bracket1);
        						if(this.m_dicKeywords.ContainsKey(dicKey) == false)
        						{
        							this.m_dicKeywords.Add(dicKey,new List<string>());
        							this.m_dicKeywords[dicKey].Add(dicKey);
        						}
        					}
        				}
        			}
        		}
        		catch(Exception c)
        		{
        			this.ExecuteCommand("procon.protected.pluginconsole.write", "^1PrepareKeywordDic: " + c);
        		}
        	}
        }
      
        public string FindKeyword(string strToFind)
        {
        	string foundKey ="";
        	string keyvalue = "";
        	strToFind = strToFind.ToUpper();
        	strToFind = strToFind.Replace(" ","");
        	foreach(KeyValuePair<string, List<string>> kvp in this.m_dicKeywords)
        	{	
        		if(this.m_dicKeywords[kvp.Key].Contains(strToFind))
        		{
        			foundKey = kvp.Key;
        			break;
        		}
        		
        	}
        	return foundKey;
        }
                
        public List<string> ListReplace(List<string> targetlist, string wordToReplace, string replacement)
        {
        	List<string> lstResult = new List<string>();
        	foreach(string substring in targetlist)
        	{
        		lstResult.Add(substring.Replace(wordToReplace,replacement));
        	}
        	return lstResult;
        }
        
        public void CheckMessageLength(string strMessage)
        {
        	if(strMessage.Length > 100)
        	{
        		//Send Warning
        		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Warning: "+ strMessage);
        		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Warning: This Ingamemessage is too long and wont sent to Server!!!" );
        		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Warning: The Message has a Length of "+ strMessage.Length.ToString() +" Chars, Allow are 100 Chars" );
        	}
        }
        
        public void CreateSession(string SoldierName)
        {
        	try
        	{
	        	if(this.m_sessionON == enumBoolYesNo.Yes)
	        	{
	        		//Session
	        		if(this.m_dicSession.ContainsKey(SoldierName) == false)
	        		{
	        			CStats Entry = new CStats("",0,0,0,0,0,0,0,this.m_dTimeOffset);
	        			this.m_dicSession.Add(SoldierName,Entry);
	        			this.m_dicSession[SoldierName].Rank = this.GetRank(SoldierName);
	        		}
	        	}
        	}
        	catch(Exception c)
        		{
        			this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in CreateSession: " + c);
        		}
        }
        
        public void RemoveSession(string SoldierName)
        {
        	try
        	{
	        	if(m_sessionON == enumBoolYesNo.Yes)
	        	{
	        		if(this.m_dicSession.ContainsKey(SoldierName) == true)
	        		{
	        			this.m_dicSession.Remove(SoldierName);
	        		}
	        	}
        	}
        	catch(Exception c)
        		{
        			this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in RemoveSession: " + c);
        		}
        	
        }
        
        public void GetSession(string SoldierName, int delay, string scope)
        {
        	try
        	{
	        	if(this.m_dicSession.ContainsKey(SoldierName) && this.m_sessionON == enumBoolYesNo.Yes)
	        	{
	        		List<string> result = new List<string>();
	        		result = m_lstSessionMessage;
	        		result = ListReplace(result, "%playerName%", SoldierName);
	        		result = ListReplace(result, "%playerScore%", this.m_dicSession[SoldierName].Score.ToString());
	        		result = ListReplace(result, "%playerKills%", this.m_dicSession[SoldierName].Kills.ToString());
	        		result = ListReplace(result, "%killstreak%", this.m_dicSession[SoldierName].Killstreak.ToString());
	        		result = ListReplace(result, "%playerDeaths%", this.m_dicSession[SoldierName].Deaths.ToString());
	        		result = ListReplace(result, "%deathstreak%", this.m_dicSession[SoldierName].Deathstreak.ToString());
	        		result = ListReplace(result, "%playerKDR%", this.m_dicSession[SoldierName].KDR().ToString());
	        		result = ListReplace(result, "%playerHeadshots%", this.m_dicSession[SoldierName].Headshots.ToString());
	        		result = ListReplace(result, "%playerSuicide%", this.m_dicSession[SoldierName].Suicides.ToString());
	        		result = ListReplace(result, "%playerTK%", this.m_dicSession[SoldierName].Teamkills.ToString());
	        		result = ListReplace(result, "%startRank%", this.m_dicSession[SoldierName].Rank.ToString());
	        		
	        		//Rankdiff
	        		int playerRank = this.GetRank(SoldierName);
	        		result = ListReplace(result, "%playerRank%", playerRank.ToString());
	        		int Rankdif = this.m_dicSession[SoldierName].Rank;
	        		Rankdif = Rankdif  - playerRank;
	        		if(Rankdif == 0)
	        		{
	        			result = ListReplace(result, "%RankDif%","0");
	        		}
	        		else if(Rankdif > 0)
	        		{
	        			result = ListReplace(result, "%RankDif%","+" + Rankdif.ToString());
	        		}
	        		else
	        		{
	        			result = ListReplace(result, "%RankDif%",Rankdif.ToString());
	        		}
	        		result = ListReplace(result, "%SessionStarted%",this.m_dicSession[SoldierName].TimePlayerjoined.ToString());
	        		TimeSpan duration = MyDateTime.Now - this.m_dicSession[SoldierName].TimePlayerjoined;
	        		result = ListReplace(result, "%SessionDuration%",Math.Round(duration.TotalMinutes,2).ToString());
	        		
	        		if(result.Count != 0)
					{
						foreach(string line in result)
						{
							this.CheckMessageLength(line);
							if(String.Equals(scope,"all"))
							   {
									this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"all");
							   }
							   else
							   {
							   		this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say",line,"player",SoldierName);
							   }
						}
					}
	        		else
	        		{
	        			this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger", delay.ToString(), "1", "1", "procon.protected.send", "admin.say","No Sessiondata are available!","player", SoldierName);
	        		}
	        	}
        	}
        	catch(Exception c)
        		{
        			this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in GetSession: " + c);
        		}
        }
        
        public int GetRank(string SoldierName)
        {
        	List<string> result = new List<string>();
        	int rank = 0;
        	try
        	{
        		string SQL = "";
        		this.tablebuilder();
        		if(m_enRankingByScore == enumBoolYesNo.Yes)
					{
						SQL = @"SELECT rank FROM(
                								  SELECT(@num := @num+1) rank, a.SoldierName  
												  FROM " + this.tbl_playerstats + @" b
                                                  INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = b.StatsID
                                                  , (select @num := 0) x 
									              ORDER BY playerScore DESC) y 
									WHERE SoldierName ='"+SoldierName+"'";
					}
					else
					{
						SQL = @"SELECT rank FROM(
                								  SELECT(@num := @num+1) rank, a.SoldierName  
												  FROM " + this.tbl_playerstats + @" b
                                                  INNER JOIN " + this.tbl_playerdata + @" a ON a.PlayerID = b.StatsID
                                                  , (select @num := 0) x
												  ORDER BY playerKills DESC, playerDeaths ASC) Y  
									WHERE SoldierName ='"+SoldierName+"'";
					}
				result = this.SQLquery(SQL,8);
				//this.CloseOdbcConnection(1);
				foreach(string entry in result)
				{
					rank = Convert.ToInt32(entry);
				}
        	}
        	catch(Exception c)
        	{
        		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in GetRank: " + c);
        	}
        	return rank;
        }
        
        public void PluginInfo(string strPlayer)
        {
        	//this.ExecuteCommand("procon.protected.tasks.add", "CChatGUIDStatsLogger","0", "1", "1", "procon.protected.send", "admin.say","This Server has the PRoCon plugin "+this.GetPluginName+" "+this.GetPluginVersion+"running by "+ this.GetPluginAuthor,"player", strPlayer);
        }
        
        public void DisplayOdbcErrorCollection(OdbcException myException) 
		{
		 	for (int i=0; i < myException.Errors.Count; i++)
			{
			   this.ExecuteCommand("procon.protected.pluginconsole.write","^1Index #" + i);
		 	   this.ExecuteCommand("procon.protected.pluginconsole.write","^1Message: " + myException.Errors[i].Message);
		 	   this.ExecuteCommand("procon.protected.pluginconsole.write","^1Native: " + myException.Errors[i].NativeError.ToString());
		 	   this.ExecuteCommand("procon.protected.pluginconsole.write","^1Source: " + myException.Errors[i].Source);
		 	   this.ExecuteCommand("procon.protected.pluginconsole.write","^1SQL: " + myException.Errors[i].SQLState );
			}
		}
        
        public void prepareTablenames(string gamemod)
        {
        	if(gamemod != this.m_strGameMod)
            {
        		this.boolTableEXISTS = false;
        	}
        	switch (gamemod) {
        		case "BC2":
        			this.tbl_playerdata = "tbl_playerdata" + this.tableSuffix;
        			this.tbl_playerstats = "tbl_playerstats" + this.tableSuffix;
        			this.tbl_weaponstats = "tbl_weaponstats" + this.tableSuffix;
        			this.tbl_dogtags = "tbl_dogtags" + this.tableSuffix;
			        this.tbl_mapstats = "tbl_mapstats" + this.tableSuffix;
			        this.tbl_chatlog = "tbl_chatlog" + this.tableSuffix;
			        this.tbl_bfbcs = "tbl_bfbcs" + this.tableSuffix;
			        this.DebugInfo("Gamemod Tableschema set to: BC2");
        			break;
        			
        		case "VIETNAM":
        			this.tbl_playerdata = "tbl_playerdata" + this.tableSuffix;
        			this.tbl_playerstats = "tbl_playerstats_bfv" + this.tableSuffix;
        			this.tbl_weaponstats = "tbl_weaponstats_bfv" + this.tableSuffix;
        			this.tbl_dogtags = "tbl_dogtags_bfv" + this.tableSuffix;
			        this.tbl_mapstats = "tbl_mapstats_bfv" + this.tableSuffix;
			        this.tbl_chatlog = "tbl_chatlog" + this.tableSuffix;
			        this.tbl_bfbcs = "tbl_bfbcs" + this.tableSuffix;
			        this.DebugInfo("Gamemod Tableschema set to: VIETNAM");
        			break;
        			
        		case "SHARED":
        			this.tbl_playerdata = "tbl_playerdata" + this.tableSuffix;
        			this.tbl_playerstats = "tbl_playerstats" + this.tableSuffix;
        			this.tbl_weaponstats = "tbl_weaponstats" + this.tableSuffix;
        			this.tbl_dogtags = "tbl_dogtags" + this.tableSuffix;
			        this.tbl_mapstats = "tbl_mapstats" + this.tableSuffix;
			        this.tbl_chatlog = "tbl_chatlog" + this.tableSuffix;
			        this.tbl_bfbcs = "tbl_bfbcs" + this.tableSuffix;
			        this.DebugInfo("Gamemod Tableschema set to: SHARED");
        			break;
        		default:
        			break;
        	}
        }
        
        public void setGameMod(string gamemod)
        {
           		switch (gamemod) 
            	{
            		case "BC2":
            			this.m_lstTableconfig = this.m_lstTableconfig_bc2;
            			this.m_lstTableschema = this.m_lstTableschema_bc2;
            			this.PrepareKeywordDic();
            			this.boolTableEXISTS = false;
            			this.DebugInfo("Gamemod Weaponlist set to: BC2");
            		break;
            		
            		case "VIETNAM":
            			this.m_lstTableconfig = this.m_lstTableconfig_bfv;
            			this.m_lstTableschema = this.m_lstTableschema_bfv;
            			this.PrepareKeywordDic();
            			this.boolTableEXISTS = false;
            			this.DebugInfo("Gamemod Weaponlist set to: VIETNAM");
            		break;
            		
            		case "SHARED":
            			this.m_lstTableconfig = this.m_lstTableconfig_bc2;
            			this.m_lstTableschema = this.m_lstTableschema_bc2;
            			this.m_lstTableconfig.AddRange(this.m_lstTableconfig_bfv);
            			this.m_lstTableschema.AddRange(this.m_lstTableschema_bfv);
            			this.PrepareKeywordDic();
            			this.boolTableEXISTS = false;
            			this.DebugInfo("Gamemod Weaponlist set to: SHARED");
            		break;
            		
            		default:	
            		break;
            	}

        }
        
        public void getBFBCStats(List<CPlayerInfo> lstPlayers)
        {
        	List<string> lstSoldierName = new List<string>();
        	string SoldierName = "";
        	try
        	{
        		foreach(CPlayerInfo Player in lstPlayers)
        		{
        			SoldierName = Player.SoldierName;    		
	        		DateTime lastUpdate = DateTime.MinValue;
	            	if (this.m_getStatsfromBFBCS == enumBoolYesNo.Yes && SoldierName != null &&  this.StatsTracker.ContainsKey(SoldierName) == true && this.StatsTracker[SoldierName].BFBCS_Stats.Updated == false && this.StatsTracker[SoldierName].BFBCS_Stats.Fetching == false )
	            	{
	            		string query = @"SELECT b.LastUpdate, b.Rank, b.Kills, b.Deaths, b.Score, b.Time
	  								 FROM "+ tbl_playerdata + @" a
	  								 INNER JOIN " + tbl_bfbcs + @" b ON a.PlayerID = b.bfbcsID
	               					 WHERE a.SoldierName = '"+ SoldierName + "'";
	                	
	               		List<string> result = new List<string>(this.SQLquery(query,10));
	               		if(result[0] != null)
	               		{
	               			//this.DebugInfo("Last Update: " + result[0].ToString());
	               			if(result[0] != "0")
	               			{
	               				lastUpdate = Convert.ToDateTime(result[0]);
	               			}
	               			TimeSpan TimeDifference = MyDateTime.Now.Subtract(lastUpdate);
	               			//this.DebugInfo(TimeDifference.TotalHours.ToString());
	               			if(TimeDifference.TotalHours >= this.BFBCS_UpdateInterval && this.StatsTracker[SoldierName].BFBCS_Stats.Fetching == false)
	               			{
	               				this.StatsTracker[SoldierName].BFBCS_Stats.Fetching = true;
	               				lstSoldierName.Add(SoldierName);
	               			} 
	               			else if( this.StatsTracker.ContainsKey(SoldierName) == true && this.StatsTracker[SoldierName].BFBCS_Stats.Fetching == true)
	               			{
	               				//Do nothing
	               			}
	               			else
	               			{
	               				if( this.StatsTracker.ContainsKey(SoldierName) == true)
	               				{
			               			//this.DebugInfo("No Update needed");
			               			this.StatsTracker[SoldierName].BFBCS_Stats.Updated = true;
			               			this.StatsTracker[SoldierName].BFBCS_Stats.Rank = Convert.ToInt32(result[1]);
			               			this.StatsTracker[SoldierName].BFBCS_Stats.Kills = Convert.ToInt32(result[2]);
			               			this.StatsTracker[SoldierName].BFBCS_Stats.Deaths = Convert.ToInt32(result[3]);
			               			this.StatsTracker[SoldierName].BFBCS_Stats.Score = Convert.ToInt32(result[4]);
			               			this.StatsTracker[SoldierName].BFBCS_Stats.Time = Convert.ToDouble(result[5]);
			               			this.StatsTracker[SoldierName].BFBCS_Stats.NoUpdate = true;
			               			this.checkPlayerStats(SoldierName,this.m_strReasonMsg);
	               				}
	               			}	
	               		}	
	            	}
        		}
        		if(lstSoldierName != null && lstSoldierName.Count > 0 && lstSoldierName.Count >= this.BFBCS_Min_Request)
        		{
        			//Start Fetching
        			specialArrayObject ListObject = new specialArrayObject(lstSoldierName);
	            	Thread newThread = new Thread(new ParameterizedThreadStart(this.DownloadBFBCS));
	            	newThread.Start(ListObject);
        		}
        		else
        		{
        			foreach(string player in lstSoldierName)
        			{
        				this.StatsTracker[player].BFBCS_Stats.Fetching = false;
        				this.StatsTracker[player].BFBCS_Stats.Updated = false;
        			}
        		}
        	}
        	catch (Exception c)
			{
				this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in getBFBCStats: " + c);
            }
        }
        
        public void DownloadBFBCS(object ListObject)
        {
        	specialArrayObject ListString = (specialArrayObject)ListObject;
        	List<string> lstSoldierName = new List<string>();
        	lstSoldierName = ListString.LstString;
        	
        	string ParameterString ="";
        	foreach(string SoldierName in lstSoldierName)
        	{
        		 if(this.StatsTracker[SoldierName].BFBCS_Stats.Updated == false)
			     {
        		 	ParameterString = String.Concat(ParameterString,SoldierName,",");
        		 	this.StatsTracker[SoldierName].BFBCS_Stats.Updated = true;
        		 }
        	}
        	ParameterString = ParameterString.Remove(ParameterString.LastIndexOf(","));
        	try
	        {
        		this.DebugInfo("Thread started and fetching Stats from BFBCS for Players: " + ParameterString);
				WebClient wc = new WebClient();
				string result = wc.DownloadString("http://api.bfbcs.com/api/pc?players=" + ParameterString + "&fields=basic");	
			    if(result == null || result.StartsWith("{") == false)
				{
					this.DebugInfo("the String returned by BFBCS was invalid");
					this.DebugInfo("Trying to repair the String...");
					if(result != null)
					{
						//result = result.Remove(result.IndexOf("<"),(result.LastIndexOf(">")+1));
						if(result.IndexOf("{") > 0)
						{
							result = result.Substring(result.IndexOf("{"));
						}
						if(result == null || result.StartsWith("{") == false)
						{
							this.DebugInfo("Repair failed!!!");
							return;
						}
						else
						{
							this.DebugInfo("Repair (might be) successful");
						}
					}
					else
					{
						this.DebugInfo("Empty String...");
						return;
					}
				}   	
			    //JSON DECODE
			    Hashtable jsonHash = (Hashtable)JSON.JsonDecode(result);
			    if(jsonHash["players"] != null)
			    {
			    	ArrayList jsonResults = (ArrayList)jsonHash["players"];
			    	//Player with Stats
			    	foreach(object objResult in jsonResults)
        			{
			    		string stringvalue ="";
						int intvalue = 0;
						double doublevalue = 0;
						Hashtable playerData = (Hashtable)objResult;
						if(playerData != null && lstSoldierName.Contains(playerData["name"].ToString()) == true)
					   	{
					   		stringvalue = playerData["name"].ToString();
					   		this.DebugInfo("Got BFBC2 stats for " + stringvalue);
						    int.TryParse(playerData["rank"].ToString(), out intvalue);
						    this.StatsTracker[stringvalue].BFBCS_Stats.Rank = intvalue;
						    int.TryParse(playerData["kills"].ToString(), out intvalue);
						    this.StatsTracker[stringvalue].BFBCS_Stats.Kills = intvalue;
						    int.TryParse(playerData["deaths"].ToString(), out intvalue);
						    this.StatsTracker[stringvalue].BFBCS_Stats.Deaths = intvalue;
						    int.TryParse(playerData["score"].ToString(), out intvalue);
						    this.StatsTracker[stringvalue].BFBCS_Stats.Score = intvalue;
						    double.TryParse(playerData["elo"].ToString(), out doublevalue);
						    this.StatsTracker[stringvalue].BFBCS_Stats.Elo = doublevalue;
						    double.TryParse(playerData["level"].ToString(), out doublevalue);
						    this.StatsTracker[stringvalue].BFBCS_Stats.Skilllevel = doublevalue;
						    double.TryParse(playerData["time"].ToString(), out doublevalue);
						    this.StatsTracker[stringvalue].BFBCS_Stats.Time = doublevalue;
						    this.StatsTracker[stringvalue].BFBCS_Stats.Updated = true;
						    // check Stats
						    if(this.m_cheaterProtection == enumBoolYesNo.Yes == true)
						    {
						    	this.checkPlayerStats(stringvalue, this.m_strReasonMsg);
						    }
					   }
					}
			    }
			    if(jsonHash["players_unknown"] != null)
			    {
			    	//Player without Stats
			    	ArrayList jsonResults_2 = (ArrayList)jsonHash["players_unknown"];
					foreach(object objResult in jsonResults_2)
        			{
			    		Hashtable playerData = (Hashtable)objResult;
			    		if(playerData != null && lstSoldierName.Contains(playerData["name"].ToString()) == true)
						{
			    			this.DebugInfo("No Stats found for Player: " + playerData["name"].ToString());
			    		}
					}
			    }
			}
			catch (Exception c)
			{
				this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in DownloadBFBCS: " + c);
				foreach(string SoldierName in lstSoldierName)
        		{
        			this.StatsTracker[SoldierName].BFBCS_Stats.Updated = false;
				}
            }
        }
        
        public void checkPlayerStats(string SoldierName, string Reason)
        {
        	try
        	{
	        	if(this.StatsTracker.ContainsKey(SoldierName) == true)
	        	{
	        		if((this.StatsTracker[SoldierName].BFBCS_Stats.KDR >= this.m_dMaxAllowedKDR && this.StatsTracker[SoldierName].BFBCS_Stats.SPM >= this.m_dMaxScorePerMinute) && (this.StatsTracker[SoldierName].BFBCS_Stats.Time/60)/60 >= this.m_dminimumPlaytime )
	        		//if(this.StatsTracker[SoldierName].BFBCS_Stats.KDR >= this.m_dMaxAllowedKDR)
	        		{
	        			this.RemovePlayerfromServer(SoldierName, Reason.Replace("%SoldierName%", SoldierName));
	        		}
	        	}
        	}
        	catch(Exception c)
        	{
        		this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in checkPlayerStats: " + c);
        	}
        }
        
        public void RemovePlayerfromServer(string targetSoldierName, string strReason)
        {
        	try
        	{
	        	if(targetSoldierName == string.Empty)
	        	{
	        		return;
	        	}
	        	switch (this.m_strRemoveMethode) {
	        		case "Kick" :
	        			this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", targetSoldierName, strReason);
	        			this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Kicked Player: " + targetSoldierName + " - " + strReason);
	        		break;
	        		
	        		case "PBBan" :
	        			this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", String.Format("pb_sv_ban \"{0}\" \"{1}\"", targetSoldierName, "BC2! " + strReason));
	        			this.ExecuteCommand("procon.protected.pluginconsole.write", "^1PB-Ban for Player: " + targetSoldierName + " - " + strReason);
	        		break;
	        		
	        		case "EAGUIDBan" :
	        			this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.StatsTracker[targetSoldierName].EAGuid, "perm", strReason);
	        			this.ExecuteCommand("procon.protected.send", "banList.save");
	        			this.ExecuteCommand("procon.protected.send", "banList.list");
	        			this.ExecuteCommand("procon.protected.pluginconsole.write", "^1EA-GUID Ban for Player: " + targetSoldierName + " - " + strReason);
	        		break;
	        		case "Nameban" :
	        			this.ExecuteCommand("procon.protected.send", "banList.add", "name", targetSoldierName, "perm", strReason);
	        			this.ExecuteCommand("procon.protected.send", "banList.save");
	        			this.ExecuteCommand("procon.protected.send", "banList.list");
	        			this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Nameban for Player: " + targetSoldierName + " - " + strReason);
	        		break;
	        		case "Warn" :
	        			this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Warning Player: " + targetSoldierName + " - " + strReason);
	        		break;
	        	}
        	}
        	catch (Exception c)
        	{
				this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in RemovePlayerfromServer: " + c);
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
        private string _Subset ="";
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
		private int	_Deaths = 0;
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
		//BFBCS
		private CBFBCS _BFBCS_Stats;
		private myDateTime MyDateTime = new myDateTime(0);
		
		public Dictionary<string, CUsedWeapon> dicWeap = new Dictionary<string, CUsedWeapon>();
		
		
		public string ClanTag {
			get { return _ClanTag; }
			set { _ClanTag = value; }
		}
		
		public string Guid {
			get { return _Guid; }
			set { _Guid = value; }
		}
		
		public string EAGuid {
			get { return _EAGuid; }
			set { _EAGuid = value; }
		}
		
		public string IP {
			get { return _IP; }
			set { _IP = value.Remove(value.IndexOf(":")); }
		}
		
		
		public string PlayerCountryCode {
			get { return _PlayerCountryCode; }
			set { _PlayerCountryCode = value; }
		}
		
		public int Score {
			get { return _Score; }
			set { _Score = value; }
		}
		
		public int LastScore {
			get { return _LastScore; }
			set { _LastScore = value; }
		}
		
		public int Kills {
			get { return _Kills; }
			set { _Kills = value; }
		}
		
		public int Headshots {
			get { return _Headshots; }
			set { _Headshots = value; }
		}
		
		public int Deaths {
			get { return _Deaths; }
			set { _Deaths = value; }
		}
		
		public int Suicides {
			get { return _Suicides; }
			set { _Suicides = value; }
		}
		
		public int Teamkills {
			get { return _Teamkills; }
			set { _Teamkills = value; }
		}
		
		public int Playtime {
			get { return _Playtime; }
			set { _Playtime = value; }
		}
		
		public DateTime Playerjoined {
			get { return _Playerjoined; }
			set { _Playerjoined = value; }
		}
		
		public DateTime TimePlayerleft {
			get { return _TimePlayerleft; }
			set { _TimePlayerleft = value; }
		}
		
		public DateTime TimePlayerjoined {
			get { return _TimePlayerjoined; }
			set { _TimePlayerjoined = value; }
		}
		
		public int PlayerleftServerScore {
			get { return _PlayerleftServerScore; }
			set { _PlayerleftServerScore = value; }
		}
		
		public bool PlayerOnServer {
			get { return _playerOnServer; }
			set { _playerOnServer = value; }
		}
		
		public int Rank {
			get { return _rank; }
			set { _rank = value; }
		}
		
		public int Killstreak {
			get { return _Killstreak; }
			set { _Killstreak = value; }
		}
		
		public int Deathstreak {
			get { return _Deathstreak; }
			set { _Deathstreak = value; }
		}
		
		//Methodes	
		public void AddScore(int intScore)
		{
			if(intScore != 0)
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
			if(this._Deaths != 0)
			{
				ratio = Math.Round(Convert.ToDouble(this._Kills)/Convert.ToDouble(this._Deaths),2);
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
			strweaponType = strweaponType.Replace(" ","");
			if((String.Equals(strweaponType,""))||(String.Equals(strweaponType," ")))
			{
				strweaponType = "UNKNOWN";
			}
			if(strweaponType.Contains("#"))
			   {
			   		int intindex = strweaponType.IndexOf("#");
			   		strweaponType = strweaponType.Remove(intindex);
			   }
			strweaponType = strweaponType.ToUpper();
			//End of the convert block
			
			if(this.dicWeap.ContainsKey(strweaponType))
			{
				if(blheadshot)
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
				if(blheadshot)
				{
					CUsedWeapon killinfo = new CUsedWeapon(1,1,0);
					this.dicWeap.Add(strweaponType,killinfo);
					this._Kills++;
					this._Headshots++;
				}
				else
				{
					CUsedWeapon killinfo = new CUsedWeapon(1,0,0);
					this.dicWeap.Add(strweaponType,killinfo);
					this._Kills++;
				}
			}
			//Killstreaks
			this._Killcount++;
			this._Deathcount = 0;
			if(this._Killcount > this._Killstreak)
			{
				this._Killstreak = this._Killcount;
			}
			
			
		}
		
		public void addDeath(string strweaponType)
		{	
			//Start of the convert block
			strweaponType = strweaponType.Replace(" ","");
			if((String.Equals(strweaponType,""))||(String.Equals(strweaponType," ")))
			{
				strweaponType = "UNKNOWN";
			}
			if(strweaponType.Contains("#"))
			   {
			   		int intindex = strweaponType.IndexOf("#");
			   		strweaponType = strweaponType.Remove(intindex);
			   }
			strweaponType = strweaponType.ToUpper();
			//End of the convert block
			
			if(this.dicWeap.ContainsKey(strweaponType))
			{
					this.dicWeap[strweaponType].Deaths++;
					this._Deaths++;
			}
			else
			{
					CUsedWeapon deathinfo = new CUsedWeapon(0,0,1);
					this.dicWeap.Add(strweaponType,deathinfo);
					this._Deaths++;
			}
			
			//Deathstreak
			this._Deathcount++;
			this._Killcount = 0;
			if(this._Deathcount > this._Deathstreak)
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
			TimeSpan duration = MyDateTime.Now - this._Playerjoined;
        	this._Playtime += Convert.ToInt32(duration.TotalSeconds);
        	this._playerOnServer = false;
		}
		
		
		
		public int TotalScore
		{
			get{ return (this._PlayerleftServerScore + this._Score);}
		}
		
		public int TotalPlaytime
		{
			get
			{
				if(this._playerOnServer)
				{
					TimeSpan duration = MyDateTime.Now - this._Playerjoined;
        			this._Playtime += Convert.ToInt32(duration.TotalSeconds);
				}
				return this._Playtime;
			}
		}
		
		public CStats.CBFBCS BFBCS_Stats {
			get { return _BFBCS_Stats; }
			set { _BFBCS_Stats = value; }
		}
		
		
		public class CUsedWeapon
		{	
			private int _Kills = 0;
			private int _Headshots = 0;
			private int _Deaths = 0;
		
			public int Kills {
				get { return _Kills; }
				set { _Kills = value; }
			}
		
			public int Headshots {
				get { return _Headshots; }
				set { _Headshots = value; }
			}
			
			public int Deaths {
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
		
		public class CBFBCS
		{
            private int _rank;
            private int _kills;
           	private int _deaths;
            private int _score;
            private double _skilllevel;
            private double _time;
            private double _elo;
            private bool _Updated;
            private bool _fetching;
            private bool _noUpdate;
            
			public int Rank {
				get { return _rank; }
				set { _rank = value; }
			}
			
			public int Kills {
				get { return _kills; }
				set { _kills = value; }
			}
			
			public int Deaths {
				get { return _deaths; }
				set { _deaths = value; }
			}
			
            public double KDR {
            	get{
            		double ratio = 0;
					if(this._deaths != 0)
					{
						ratio = Math.Round(Convert.ToDouble(this._kills)/Convert.ToDouble(this._deaths),2);
					}
					else
					{
						ratio = this._kills;
					}
					return ratio;
            	}
            }
            public double SPM {
            	get{
            		return Convert.ToDouble(this._score)/(this._time/60);
            	}
            }
            
            
			public int Score {
				get { return _score; }
				set { _score = value; }
			}
			
			public double Skilllevel {
				get { return _skilllevel; }
				set { _skilllevel = value; }
			}
			
			public double Time {
				get { return _time; }
				set { _time = value; }
			}
			
			public double Elo {
				get { return _elo; }
				set { _elo = value; }
			}
			
			public bool Updated {
				get { return _Updated; }
				set { _Updated = value; }
			}
            
			public bool Fetching {
				get { return _fetching; }
				set { _fetching = value; }
			}
            
			public bool NoUpdate {
				get { return _noUpdate; }
				set { _noUpdate = value; }
			}
            
            
            
			public CBFBCS()
			{
				this._rank = 0;
				this._kills = 0;
				this._deaths = 0;
				this._score = 0;
				this._skilllevel = 0;
				this._time = 0;
				this._elo = 0;
				this._Updated = false;
				this._fetching = false;
				this._noUpdate = false;
			}
            
		}
		
		public class myDateTime
		{
			private double _offset = 0;

			public DateTime Now {
				get { 
					DateTime dateValue  = DateTime.Now;
					return dateValue.AddHours(_offset); }
			}
			public myDateTime(double offset)
			{
				this._offset = offset;
			}
		}
				
		
	
		public CStats(string guid, int score, int kills, int headshots, int deaths, int suicides, int teamkills, int playtime, double timeoffset)
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
			this._PlayerCountryCode= String.Empty;
			this._TimePlayerjoined = MyDateTime.Now;
			this._TimePlayerleft = DateTime.MinValue;
			this._rank = 0;
			this._Killcount = 0;
			this._Killstreak = 0;
			this._Deathcount = 0;
			this._Deathstreak = 0;
			this.BFBCS_Stats = new CStats.CBFBCS();
			this.MyDateTime = new myDateTime(timeoffset);
		}
	
	}
	
	class C_ID_Cache
	{
		private int _Id;
		private bool _PlayeronServer;
		
		public int Id {
			get { return _Id; }
			set { _Id = value; }
		}
		
		public bool PlayeronServer {
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
	
		
		public string Killer {
			get { return _Killer; }
			set { _Killer = value; }
		}
		
		public string Victim {
			get { return _Victim; }
			set { _Victim = value; }
		}
		
		public CKillerVictim(string killer, string victim)
		{
			this._Killer = killer;
			this._Victim = victim;
		}
		
	}
	
	class CMapstats
	{
		private DateTime _timeMaploaded;
		private DateTime _timeMapStarted;
		private DateTime _timeRoundEnd;
		private string _strMapname;
		private string _strGamemode;
		private int _intRound;
		private int _intNumberOfRounds;
		private List<int> _lstPlayers;
		private int _intMinPlayers;
		private int _intMaxPlayers;
		private int _intServerplayermax;
		private double _doubleAvgPlayers;
		private int _intplayerleftServer;
		private int _intplayerjoinedServer;
		private myDateTime MyDateTime = new myDateTime(0);
		
		public DateTime TimeMaploaded {
			get { return _timeMaploaded; }
			set { _timeMaploaded = value; }
		}
		
		public DateTime TimeMapStarted {
			get { return _timeMapStarted; }
			set { _timeMapStarted = value; }
		}
		
		public DateTime TimeRoundEnd {
			get { return _timeRoundEnd; }
			set { _timeRoundEnd = value; }
		}
		
		public string StrMapname {
			get { return _strMapname; }
			set { _strMapname = value; }
		}
		
		public string StrGamemode {
			get { return _strGamemode; }
			set { _strGamemode = value; }
		}
		
		public int IntRound {
			get { return _intRound; }
			set { _intRound = value; }
		}
		
		public int IntNumberOfRounds {
			get { return _intNumberOfRounds; }
			set { _intNumberOfRounds = value; }
		}
		
		public List<int> LstPlayers {
			get { return _lstPlayers; }
			set { _lstPlayers = value; }
		}
		
		public int IntMinPlayers {
			get { return _intMinPlayers; }
			set { _intMinPlayers = value; }
		}
		
		public int IntMaxPlayers {
			get { return _intMaxPlayers; }
			set { _intMaxPlayers = value; }
		}
		
		public int IntServerplayermax {
			get { return _intServerplayermax; }
			set { _intServerplayermax = value; }
		}
		
		public double DoubleAvgPlayers {
			get { return _doubleAvgPlayers; }
			set { _doubleAvgPlayers = value; }
		}
		
		public int IntplayerleftServer {
			get { return _intplayerleftServer; }
			set { _intplayerleftServer = value; }
		}
		
		public int IntplayerjoinedServer {
			get { return _intplayerjoinedServer; }
			set { _intplayerjoinedServer = value; }
		}
		
		public void MapStarted()
		{
			this._timeMapStarted = MyDateTime.Now;
		}
		
		public void MapEnd()
		{
			this._timeRoundEnd = MyDateTime.Now;
		}
		
		public void ListADD(int entry)
		{
			this._lstPlayers.Add(entry);
		}
		
		public void calcMaxMinAvgPlayers()
		{
			this._intMaxPlayers = 0;
			this._intMinPlayers = _intServerplayermax;
			this._doubleAvgPlayers = 0;
			int entries = 0;
			foreach(int playercount in this._lstPlayers)
			{
				if( playercount >= this._intMaxPlayers)
					 this._intMaxPlayers = playercount;
				
				if( playercount <= this._intMinPlayers)
					this._intMinPlayers = playercount;
				
				this._doubleAvgPlayers = this._doubleAvgPlayers + playercount;
				entries = entries + 1;
				
			}
			if(entries != 0)
			{
				this._doubleAvgPlayers = this._doubleAvgPlayers/(Convert.ToDouble(entries));
				this._doubleAvgPlayers = Math.Round(this._doubleAvgPlayers,1);
			}
			else
			{
				this._doubleAvgPlayers = 0;
				this._intMaxPlayers = 0;
				this._intMinPlayers = 0;
			}
		}
		
		public class myDateTime
		{
			private double _offset = 0;
		
			public DateTime Now {
			get { 
				DateTime dateValue  = DateTime.Now;
				return dateValue.AddHours(_offset); }
			}
			public myDateTime(double offset)
			{
				this._offset = offset;
			}
		}
		
		public CMapstats(DateTime timeMaploaded, string strMapname, int intRound, int intNumberOfRounds, double timeoffset)
		{
			this._timeMaploaded = timeMaploaded;
			this._strMapname = strMapname;
			this._intRound = intRound;
			this._intNumberOfRounds = intNumberOfRounds;
			this._intMaxPlayers = 32;
			this._intServerplayermax= 32;
			this._intMinPlayers = 0;
			this._intplayerjoinedServer = 0;
			this._intplayerleftServer = 0;
			this._lstPlayers = new List<int>();
			this._timeMapStarted = DateTime.MinValue;
			this._timeRoundEnd = DateTime.MinValue;
			this._strGamemode = "";
			this.MyDateTime = new myDateTime(timeoffset);
		}
		
		
	}
	
	class CSpamprotection
	{
		private Dictionary<string, int> dicplayer;
		private int _allowedRequests;
		
		public CSpamprotection(int allowedRequests)
		{
			this._allowedRequests = allowedRequests;
			this.dicplayer = new Dictionary<string, int>();
		}
		
		public bool isAllowed(string strSpeaker)
		{
				bool result = false;
				if(this.dicplayer.ContainsKey(strSpeaker) == true)
				{
					int i = this.dicplayer[strSpeaker];
					if(0 >= i)
					{
						//Player is blocked
						result = false;
						this.dicplayer[strSpeaker]--;
					}
					else
					{
						//Player is not blocked
						result = true;
						this.dicplayer[strSpeaker] --;
					}				
				}
				else
				{
					this.dicplayer.Add(strSpeaker,this._allowedRequests);
					result = true;
					this.dicplayer[strSpeaker] --;
				}
				return result;
		}
		
		public void Reset()
		{
			this.dicplayer.Clear();
		}
		
	}
	
	class specialArrayObject
	{
		private List<string> lstString = new List<string>();
		
		public List<string> LstString {
			get { return lstString; }
			set { lstString = value; }
		}
		
		public specialArrayObject(List<string> LstString)
		{
			lstString = LstString;
		}	
	}
	
	class myDateTime_W
	{
		private double _offset = 0;
		
		public DateTime Now {
			get { 
				DateTime dateValue  = DateTime.Now;
				return dateValue.AddHours(_offset); }
			}
		public myDateTime_W(double offset)
		{
			this._offset = offset;
		}
	}
	
	
	
	
	
	
	#endregion
}
