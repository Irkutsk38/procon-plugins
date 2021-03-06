using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;

using PRoCon.Plugin;

namespace PRoConEvents
{
    public class CKillrateChecker : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();                              //Dictionary for maintaining players
        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo;

        private Dictionary<string, int> m_dicPlayersKillsAdvPlayers = new Dictionary<string, int>();
        private Dictionary<string, DateTime> m_dicPlayersPastTimeStampsAdvPlayers = new Dictionary<string, DateTime>();

        private Dictionary<string, int> m_dicPlayersKillsAdvPlayersAlt = new Dictionary<string, int>();
        private Dictionary<string, DateTime> m_dicPlayersPastTimeStampsAdvPlayersAlt = new Dictionary<string, DateTime>();

        private Dictionary<string, int> m_dicPlayersKillsCheaters = new Dictionary<string, int>();
        private Dictionary<string, DateTime> m_dicPlayersPastTimeStampsCheaters = new Dictionary<string, DateTime>();

        private Dictionary<string, int> m_dicPlayersKillsCheatersAlt = new Dictionary<string, int>();
        private Dictionary<string, DateTime> m_dicPlayersPastTimeStampsCheatersAlt = new Dictionary<string, DateTime>();

        private Dictionary<string, int> m_dicSuspiciousPlayersDetectionCountersAdvPlayers = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicSuspiciousPlayersDetectionCountersCheaters = new Dictionary<string, int>();

        private List<string> m_lstPrefixes;
        private List<string> m_lstPlayerNames;

        private string m_KickMessagePrivate;        //Ban message (Private)
        private string m_KickMessagePublic;         //Ban message (Public)
        private string m_BanMessagePrivate;         //Ban message (Private)
        private string m_BanMessagePublic;          //Ban message (Public)
        private enumBoolYesNo m_enKick;             //Kicking Cheaters en- or disbled
        private enumBoolYesNo m_enBan;              //Banning Cheaters en- or disbled
        private enumBoolYesNo m_enBanByName;
        private enumBoolYesNo m_enBanByEA;
        private enumBoolYesNo m_enBanByPB;
        private enumBoolYesNo m_enPrefix;
        private enumBoolYesNo m_enPlayerNames;
        private enumBoolYesNo m_enPBSS;				//enables PB screenshots for cheaters
        public int m_iPeriod;                       //sets Period in Minutes to analyse Players Killrate
        public int m_iKillsPerPeriodCheater;        //sets Kills per Period for Cheaters
        public int m_iKillsPerPeriodAdvPlayer;      //sets Kills per Period for too advanced Players
        public int m_maxKillsPerRoundCheater;       //sets Maximum allowed Kills per Round for Cheaters
        public int m_maxKillsPerRoundAdvPlayer;     //sets Maximum allowed Kills per Round for advanced Players
        public int m_maxDetections;                 //sets Maximum allowed Detections of an suspicious player per Round before he gets kicked / banned


        public CKillrateChecker()
        {
            this.m_iKillsPerPeriodAdvPlayer = 6;
            this.m_iKillsPerPeriodCheater = 10;
            this.m_iPeriod = 1;  // fixed value, since plugin version 0.1.5.1
            this.m_maxDetections = 5;
            this.m_maxKillsPerRoundCheater = 70;
            this.m_maxKillsPerRoundAdvPlayer = 45;
            this.m_KickMessagePrivate = "Sorry, this Server is for Scrubs only!";
            this.m_KickMessagePublic = "%pk% is no Scrub and was kicked!";
            this.m_BanMessagePrivate = "You are probably a Cheater %pk%  !";
            this.m_BanMessagePublic = "%pk% is probably a Cheater and was banned!";
            this.m_enBan = enumBoolYesNo.Yes;
            this.m_enPBSS = enumBoolYesNo.No;
            this.m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
            this.m_enBanByName = enumBoolYesNo.Yes;
            this.m_enBanByEA = enumBoolYesNo.No;
            this.m_enBanByPB = enumBoolYesNo.No;
            this.m_enKick = enumBoolYesNo.No;
            this.m_enPrefix = enumBoolYesNo.No;
            this.m_enPlayerNames = enumBoolYesNo.No;
            this.m_lstPrefixes = new List<string>();
            this.m_lstPlayerNames = new List<string>();
        }

        public string GetPluginName()
        {
            return "Killrate Checker";
        }

        public string GetPluginVersion()
        {
            return "0.1.7.0";
        }

        public string GetPluginAuthor()
        {
            return "[KMN] DaBIGfisH";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>
This plugin checks the absolute amount of Playerkills per round AND the PlayerKillSpeed to ban Cheaters and/or kick Advanced Players.
<blockquote></blockquote>
        <li>- Advanced Players can only be kicked.</li>
        <blockquote></blockquote>
        <li>- Cheaters can only be permanent banned.</li>
        <blockquote></blockquote>
        <li>- <b>Please use this Plugin very carefully !!! Think twice before you set Values!</b></li>
        <blockquote></blockquote>
        <li>- For Timeban after X Kicks use the Plugin: Kick Tracker with Temp Ban.</li>
        <blockquote></blockquote>
For detailed information about this plugin visit   www.phogue.net,  go to the Plugins - Forum and search for Killrate Checker. 
        <blockquote></blockquote>
Regards & nice fragging!<blockquote></blockquote><blockquote></blockquote>[KMN] DaBIGfisH
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
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bKillrate Checker Plugin ^2Enabled!");
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bKillrate Checker Plugin ^1Disabled =(");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("1) Killrate Options|max Kills per Minute for Advanced Players", typeof(int), this.m_iKillsPerPeriodAdvPlayer));
            lstReturn.Add(new CPluginVariable("1) Killrate Options|max Kills per Minute for Cheaters", typeof(int), this.m_iKillsPerPeriodCheater));
            //lstReturn.Add(new CPluginVariable("1) Killrate Options|Period in Minutes", typeof(int), this.m_iPeriod));
            lstReturn.Add(new CPluginVariable("1) Killrate Options|max suspicious Detections per Round", typeof(int), this.m_maxDetections));
            lstReturn.Add(new CPluginVariable("1) Killrate Options|max Kills per Round for Advanced Players", typeof(int), this.m_maxKillsPerRoundAdvPlayer));
            lstReturn.Add(new CPluginVariable("1) Killrate Options|max Kills per Round for Cheaters", typeof(int), this.m_maxKillsPerRoundCheater));
            lstReturn.Add(new CPluginVariable("2) Kick Options|Kick Advanced Players", typeof(enumBoolYesNo), this.m_enKick));
            if (this.m_enKick == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("2) Kick Options|Kick Message Private", typeof(string), this.m_KickMessagePrivate));
                lstReturn.Add(new CPluginVariable("2) Kick Options|Kick Message Public", typeof(string), this.m_KickMessagePublic));
            }
            lstReturn.Add(new CPluginVariable("3) Ban Options|Ban Cheaters", typeof(enumBoolYesNo), this.m_enBan));
            if (this.m_enBan == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("3) Ban Options|Do PB Screenshots on suspicious Detections", typeof(enumBoolYesNo), this.m_enPBSS));
                lstReturn.Add(new CPluginVariable("3) Ban Options|Ban by Name", typeof(enumBoolYesNo), this.m_enBanByName));
                lstReturn.Add(new CPluginVariable("3) Ban Options|Ban by EA GUID", typeof(enumBoolYesNo), this.m_enBanByEA));
                lstReturn.Add(new CPluginVariable("3) Ban Options|Ban by PB GUID", typeof(enumBoolYesNo), this.m_enBanByPB));
                lstReturn.Add(new CPluginVariable("3) Ban Options|Ban Message Private", typeof(string), this.m_BanMessagePrivate));
                lstReturn.Add(new CPluginVariable("3) Ban Options|Ban Message Public", typeof(string), this.m_BanMessagePublic));
            }
            lstReturn.Add(new CPluginVariable("4) Ignore specific Clan Tags|Do not kick / ban Players with specific Clan Tag", typeof(enumBoolYesNo), this.m_enPrefix));
            if (this.m_enPrefix == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("4) Ignore specific Clan Tags|List of Clan Tags to be ignored", typeof(string[]), this.m_lstPrefixes.ToArray()));
            }
            lstReturn.Add(new CPluginVariable("5) Ignore specific Playernames|Do not kick / ban Players if their Names are in List", typeof(enumBoolYesNo), this.m_enPlayerNames));
            if (this.m_enPlayerNames == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("5) Ignore specific Playernames|List of Playernames to be ignored", typeof(string[]), this.m_lstPlayerNames.ToArray()));
            }
            return lstReturn;
        }

        //Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("max Kills per Minute for Advanced Players", typeof(int), this.m_iKillsPerPeriodAdvPlayer));
            lstReturn.Add(new CPluginVariable("max Kills per Minute for Cheaters", typeof(int), this.m_iKillsPerPeriodCheater));
            //lstReturn.Add(new CPluginVariable("Period in Minutes", typeof(int), this.m_iPeriod));
            lstReturn.Add(new CPluginVariable("max suspicious Detections per Round", typeof(int), this.m_maxDetections));
            lstReturn.Add(new CPluginVariable("max Kills per Round for Advanced Players", typeof(int), this.m_maxKillsPerRoundAdvPlayer));
            lstReturn.Add(new CPluginVariable("max Kills per Round for Cheaters", typeof(int), this.m_maxKillsPerRoundCheater));

            lstReturn.Add(new CPluginVariable("Kick Advanced Players", typeof(enumBoolYesNo), this.m_enKick));
            lstReturn.Add(new CPluginVariable("Kick Message Private", typeof(string), this.m_KickMessagePrivate));
            lstReturn.Add(new CPluginVariable("Kick Message Public", typeof(string), this.m_KickMessagePublic));

            lstReturn.Add(new CPluginVariable("Ban Cheaters", typeof(enumBoolYesNo), this.m_enBan));
            lstReturn.Add(new CPluginVariable("Do PB Screenshots on suspicious Detections", typeof(enumBoolYesNo), this.m_enPBSS));
            lstReturn.Add(new CPluginVariable("Ban by Name", typeof(enumBoolYesNo), this.m_enBanByName));
            lstReturn.Add(new CPluginVariable("Ban by EA GUID", typeof(enumBoolYesNo), this.m_enBanByEA));
            lstReturn.Add(new CPluginVariable("Ban by PB GUID", typeof(enumBoolYesNo), this.m_enBanByPB));
            lstReturn.Add(new CPluginVariable("Ban Message Private", typeof(string), this.m_BanMessagePrivate));
            lstReturn.Add(new CPluginVariable("Ban Message Public", typeof(string), this.m_BanMessagePublic));

            lstReturn.Add(new CPluginVariable("Do not kick / ban Players with specific Clan Tag", typeof(enumBoolYesNo), this.m_enPrefix));
            lstReturn.Add(new CPluginVariable("List of Clan Tags to be ignored", typeof(string[]), this.m_lstPrefixes.ToArray()));

            lstReturn.Add(new CPluginVariable("Do not kick / ban Players if their Names are in List", typeof(enumBoolYesNo), this.m_enPlayerNames));
            lstReturn.Add(new CPluginVariable("List of Playernames to be ignored", typeof(string[]), this.m_lstPlayerNames.ToArray()));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {

            int KPPC;
            int KPPAP;
            int P;
            int D;
            int maxKPPAP;
            int maxKPPC;

            if (strVariable.CompareTo("max Kills per Minute for Advanced Players") == 0 && int.TryParse(strValue, out KPPAP) == true)
            {
                if (KPPAP < 1)
                {
                    this.m_iKillsPerPeriodAdvPlayer = 1;
                }
                else
                {
                    this.m_iKillsPerPeriodAdvPlayer = KPPAP;
                }
            }
            else if (strVariable.CompareTo("max Kills per Minute for Cheaters") == 0 && int.TryParse(strValue, out KPPC) == true)
            {
                if (KPPC < 1)
                {
                    this.m_iKillsPerPeriodCheater = 1;
                }
                else
                {
                    this.m_iKillsPerPeriodCheater = KPPC;
                }
            }
            /*else if (strVariable.CompareTo("Period in Minutes") == 0 && int.TryParse(strValue, out P) == true)
            {
                if (P < 0)
                {
                    this.m_iPeriod = 1;
                }
                else
                {
                    this.m_iPeriod = P;
					if (this.m_maxDetections < this.m_iPeriod) { this.m_maxDetections = P;}
                }
            }*/
            else if (strVariable.CompareTo("max suspicious Detections per Round") == 0 && int.TryParse(strValue, out D) == true)
            {
                if (D < 1)
                {
                    this.m_maxDetections = 1;
                }
                else
                {
                    this.m_maxDetections = D;
                }
            }
            else if (strVariable.CompareTo("max Kills per Round for Advanced Players") == 0 && int.TryParse(strValue, out maxKPPAP) == true)
            {
                if (maxKPPAP < 0)
                {
                    this.m_maxKillsPerRoundAdvPlayer = 1;
                }
                else
                {
                    this.m_maxKillsPerRoundAdvPlayer = maxKPPAP;
                }
            }
            else if (strVariable.CompareTo("max Kills per Round for Cheaters") == 0 && int.TryParse(strValue, out maxKPPC) == true)
            {
                if (maxKPPC < 0)
                {
                    this.m_maxKillsPerRoundCheater = 1;
                }
                else
                {
                    this.m_maxKillsPerRoundCheater = maxKPPC;
                }
            }
            else if (strVariable.CompareTo("Kick Advanced Players") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enKick = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Kick Message Private") == 0)
            {
                this.m_KickMessagePrivate = strValue.Substring(0, strValue.Length > 64 ? 64 : strValue.Length);
            }
            else if (strVariable.CompareTo("Kick Message Public") == 0)
            {
                this.m_KickMessagePublic = strValue.Substring(0, strValue.Length > 64 ? 64 : strValue.Length);
            }
            else if (strVariable.CompareTo("Ban Cheaters") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enBan = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_enBan == enumBoolYesNo.No)
                {
                    this.m_enPBSS = enumBoolYesNo.No;
                }
            }
            else if (strVariable.CompareTo("Do PB Screenshots on suspicious Detections") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enPBSS = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Ban by Name") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enBanByName = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_enBanByName == enumBoolYesNo.Yes)
                {
                    this.m_enBanByEA = enumBoolYesNo.No;
                    this.m_enBanByPB = enumBoolYesNo.No;
                }
                else
                {
                    if (this.m_enBanByName == enumBoolYesNo.No && this.m_enBanByEA == enumBoolYesNo.No && this.m_enBanByPB == enumBoolYesNo.No)
                    {
                        this.m_enBanByName = enumBoolYesNo.Yes;
                    }
                }
            }
            else if (strVariable.CompareTo("Ban by EA GUID") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enBanByEA = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_enBanByEA == enumBoolYesNo.Yes)
                {
                    this.m_enBanByName = enumBoolYesNo.No;
                    this.m_enBanByPB = enumBoolYesNo.No;
                }
                else
                {
                    if (this.m_enBanByName == enumBoolYesNo.No && this.m_enBanByEA == enumBoolYesNo.No && this.m_enBanByPB == enumBoolYesNo.No)
                    {
                        this.m_enBanByEA = enumBoolYesNo.Yes;
                    }
                }
            }
            else if (strVariable.CompareTo("Ban by PB GUID") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enBanByPB = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_enBanByPB == enumBoolYesNo.Yes)
                {
                    this.m_enBanByName = enumBoolYesNo.No;
                    this.m_enBanByEA = enumBoolYesNo.No;
                }
                else
                {
                    if (this.m_enBanByName == enumBoolYesNo.No && this.m_enBanByEA == enumBoolYesNo.No && this.m_enBanByPB == enumBoolYesNo.No)
                    {
                        this.m_enBanByPB = enumBoolYesNo.Yes;
                    }
                }
            }
            else if (this.m_enBanByName == enumBoolYesNo.No && this.m_enBanByEA == enumBoolYesNo.No && this.m_enBanByPB == enumBoolYesNo.No)
            {
                this.m_enBanByName = enumBoolYesNo.Yes;
            }
            else if (strVariable.CompareTo("Ban Message Private") == 0)
            {
                this.m_BanMessagePrivate = strValue.Substring(0, strValue.Length > 64 ? 64 : strValue.Length);
            }
            else if (strVariable.CompareTo("Ban Message Public") == 0)
            {
                this.m_BanMessagePublic = strValue.Substring(0, strValue.Length > 64 ? 64 : strValue.Length);
            }
            else if (strVariable.CompareTo("Do not kick / ban Players with specific Clan Tag") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enPrefix = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("List of Clan Tags to be ignored") == 0)
            {
                this.m_lstPrefixes = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Do not kick / ban Players if their Names are in List") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enPlayerNames = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("List of Playernames to be ignored") == 0)
            {
                this.m_lstPlayerNames = new List<string>(CPluginVariable.DecodeStringArray(strValue));
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

            //this.ExecuteCommand("procon.protected.pluginconsole.write", "^2player joined . . .");
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {

            RemovePlayerFromDictionaries(strSoldierName);
        }




        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {
            int flagkickedAdvPlayer = 0;
            int flagkickedAdvPlayerAlt = 0;
            int flagbannedCheater = 0;
            int flagbannedCheaterAlt = 0;
            int ignore = 0;

            if (this.m_enPrefix == enumBoolYesNo.Yes)
            {
                foreach (string prfx in this.m_lstPrefixes)
                {
                    if (String.Compare(this.m_dicPlayers[strKillerSoldierName].ClanTag, prfx) == 0)
                    {
                        ignore = 1;
                    }
                }
            }

            if (this.m_enPlayerNames == enumBoolYesNo.Yes)
            {
                foreach (string pn in this.m_lstPlayerNames)
                {
                    if (String.Compare(this.m_dicPlayers[strKillerSoldierName].SoldierName, pn) == 0)
                    {
                        ignore = 1;
                    }
                }
            }

            //if (String.Compare(strKillerSoldierName, strVictimSoldierName) == 0 && String.Compare(strKillerSoldierName, "Stinkerfisch") == 0 && ignore == 0)  // for testing the plugin
            if (String.Compare(strKillerSoldierName, strVictimSoldierName) != 0 && ignore == 0)
            {
                // background calculation to identify advanced players
                if (this.m_dicPlayersKillsAdvPlayers.ContainsKey(strKillerSoldierName) == false)
                {
                    this.m_dicPlayersKillsAdvPlayers.Add(strKillerSoldierName, 1);
                    this.m_dicPlayersPastTimeStampsAdvPlayers.Add(strKillerSoldierName, DateTime.Now);
                }
                else
                {
                    this.m_dicPlayersKillsAdvPlayers[strKillerSoldierName] += 1;

                    if (this.m_dicPlayersKillsAdvPlayers[strKillerSoldierName] >= (this.m_iKillsPerPeriodAdvPlayer * this.m_maxDetections))
                    {
                        double elapsedTimeAdvPlayer = ((int)(DateTime.Now - this.m_dicPlayersPastTimeStampsAdvPlayers[strKillerSoldierName]).TotalSeconds) / 60.0;
                        double killSpeedAdvPlayer = this.m_dicPlayersKillsAdvPlayers[strKillerSoldierName] / elapsedTimeAdvPlayer;

                        if (killSpeedAdvPlayer >= (this.m_iKillsPerPeriodAdvPlayer * this.m_maxDetections) / (this.m_iPeriod * this.m_maxDetections))
                        {
                            if (this.m_enKick == enumBoolYesNo.Yes)
                            {
                                KickPlayer(strKillerSoldierName);
                                flagkickedAdvPlayer = 1;
                            }
                        }

                        if (flagkickedAdvPlayer == 0)
                        {
                            this.m_dicPlayersKillsAdvPlayers[strKillerSoldierName] = 0;
                            this.m_dicPlayersPastTimeStampsAdvPlayers[strKillerSoldierName] = DateTime.Now;
                        }
                    }
                }

                // main calculation to identify advanced players
                if (this.m_dicPlayersKillsAdvPlayersAlt.ContainsKey(strKillerSoldierName) == false)
                {
                    this.m_dicPlayersKillsAdvPlayersAlt.Add(strKillerSoldierName, 1);
                    this.m_dicPlayersPastTimeStampsAdvPlayersAlt.Add(strKillerSoldierName, DateTime.Now);
                    this.m_dicSuspiciousPlayersDetectionCountersAdvPlayers.Add(strKillerSoldierName, 0);
                }
                else
                {
                    this.m_dicPlayersKillsAdvPlayersAlt[strKillerSoldierName] += 1;

                    if (this.m_dicPlayersKillsAdvPlayersAlt[strKillerSoldierName] >= (this.m_iKillsPerPeriodAdvPlayer / this.m_iPeriod))
                    {
                        double elapsedTimeAdvPlayerAlt = ((int)(DateTime.Now - this.m_dicPlayersPastTimeStampsAdvPlayersAlt[strKillerSoldierName]).TotalSeconds) / 60.0;
                        double killSpeedAdvPlayerAlt = this.m_dicPlayersKillsAdvPlayersAlt[strKillerSoldierName] / elapsedTimeAdvPlayerAlt;

                        if (killSpeedAdvPlayerAlt >= (this.m_iKillsPerPeriodAdvPlayer / this.m_iPeriod))
                        {
                            this.m_dicSuspiciousPlayersDetectionCountersAdvPlayers[strKillerSoldierName] += 1;
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^3Killrate Checker: advanced player detected:  {0}   K/D:  {1}/{2}  KDR:  {3}", strKillerSoldierName, this.m_dicPlayers[strKillerSoldierName].Kills, this.m_dicPlayers[strKillerSoldierName].Deaths, this.m_dicPlayers[strKillerSoldierName].Kdr));

                            if (this.m_dicSuspiciousPlayersDetectionCountersAdvPlayers[strKillerSoldierName] >= this.m_maxDetections) // when player gets x times detected, kick him
                            {
                                if (this.m_enKick == enumBoolYesNo.Yes)
                                {
                                    KickPlayer(strKillerSoldierName);
                                    flagkickedAdvPlayerAlt = 1;
                                }
                            }
                        }

                        if (flagkickedAdvPlayerAlt == 0)
                        {
                            this.m_dicPlayersKillsAdvPlayersAlt[strKillerSoldierName] = 0;
                            this.m_dicPlayersPastTimeStampsAdvPlayersAlt[strKillerSoldierName] = DateTime.Now;
                        }
                    }
                }

                //background calculation to identify cheaters
                if (this.m_dicPlayersKillsCheaters.ContainsKey(strKillerSoldierName) == false)
                {
                    this.m_dicPlayersKillsCheaters.Add(strKillerSoldierName, 1);
                    this.m_dicPlayersPastTimeStampsCheaters.Add(strKillerSoldierName, DateTime.Now);
                }
                else
                {
                    this.m_dicPlayersKillsCheaters[strKillerSoldierName] += 1;

                    if (this.m_dicPlayersKillsCheaters[strKillerSoldierName] >= (this.m_iKillsPerPeriodCheater * this.m_maxDetections))
                    {
                        double elapsedTimeCheater = ((int)(DateTime.Now - this.m_dicPlayersPastTimeStampsCheaters[strKillerSoldierName]).TotalSeconds) / 60.0;
                        double killSpeedCheater = this.m_dicPlayersKillsCheaters[strKillerSoldierName] / elapsedTimeCheater;

                        if (killSpeedCheater >= (this.m_iKillsPerPeriodCheater * this.m_maxDetections) / (this.m_iPeriod * this.m_maxDetections))
                        {
                            if (this.m_enBan == enumBoolYesNo.Yes)
                            {
                                BanPlayer(strKillerSoldierName);
                                flagbannedCheater = 1;
                            }
                        }

                        if (flagbannedCheater == 0)
                        {
                            this.m_dicPlayersKillsCheaters[strKillerSoldierName] = 0;
                            this.m_dicPlayersPastTimeStampsCheaters[strKillerSoldierName] = DateTime.Now;
                            //this.ExecuteCommand("procon.protected.pluginconsole.write", "^2no cheater detected");

                        }
                    }
                }

                // alternative calculation to identify cheaters
                if (this.m_dicPlayersKillsCheatersAlt.ContainsKey(strKillerSoldierName) == false)
                {
                    this.m_dicPlayersKillsCheatersAlt.Add(strKillerSoldierName, 1);
                    this.m_dicPlayersPastTimeStampsCheatersAlt.Add(strKillerSoldierName, DateTime.Now);
                    this.m_dicSuspiciousPlayersDetectionCountersCheaters.Add(strKillerSoldierName, 0);
                }
                else
                {
                    this.m_dicPlayersKillsCheatersAlt[strKillerSoldierName] += 1;

                    if (this.m_dicPlayersKillsCheatersAlt[strKillerSoldierName] >= (this.m_iKillsPerPeriodCheater / this.m_iPeriod))
                    {
                        double elapsedTimeCheaterAlt = ((int)(DateTime.Now - this.m_dicPlayersPastTimeStampsCheatersAlt[strKillerSoldierName]).TotalSeconds) / 60.0;
                        double killspeedCheaterAlt = this.m_dicPlayersKillsCheatersAlt[strKillerSoldierName] / elapsedTimeCheaterAlt;

                        if (killspeedCheaterAlt >= (this.m_iKillsPerPeriodCheater / this.m_iPeriod))
                        {
                            this.m_dicSuspiciousPlayersDetectionCountersCheaters[strKillerSoldierName] += 1;
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^3Killrate Checker: suspicious player detected:  {0}   K/D:  {1}/{2}  KDR:  {3}", strKillerSoldierName, this.m_dicPlayers[strKillerSoldierName].Kills, this.m_dicPlayers[strKillerSoldierName].Deaths, this.m_dicPlayers[strKillerSoldierName].Kdr));

                            if (this.m_enBan == enumBoolYesNo.Yes && this.m_enPBSS == enumBoolYesNo.Yes)
                            {
                                //this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_getss " + this.m_dicPbInfo[strKillerSoldierName].SlotID);
                                this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_getss \"" + strKillerSoldierName + "\"");
                            }

                            if (this.m_dicSuspiciousPlayersDetectionCountersCheaters[strKillerSoldierName] >= this.m_maxDetections) // when player gets x times detected, ban him
                            {
                                if (this.m_enBan == enumBoolYesNo.Yes)
                                {
                                    BanPlayer(strKillerSoldierName);
                                    flagbannedCheaterAlt = 1;
                                }
                            }
                        }

                        if (flagbannedCheaterAlt == 0)
                        {
                            this.m_dicPlayersKillsCheatersAlt[strKillerSoldierName] = 0;
                            this.m_dicPlayersPastTimeStampsCheatersAlt[strKillerSoldierName] = DateTime.Now;
                            //this.ExecuteCommand("procon.protected.pluginconsole.write", "^2no cheater detected");
                        }
                    }
                }

                // if absolute killcount to high kick advanced player
                if (this.m_dicPlayersKillsAdvPlayersAlt[strKillerSoldierName] >= this.m_maxKillsPerRoundAdvPlayer)
                {
                    if (this.m_enKick == enumBoolYesNo.Yes)
                    {
                        KickPlayer(strKillerSoldierName);
                        //this.ExecuteCommand("procon.protected.pluginconsole.write", "^2absKillCnt: kicking . . .");
                    }
                }

                // if absolute killcount to high ban cheater
                if (this.m_dicPlayersKillsCheatersAlt[strKillerSoldierName] >= this.m_maxKillsPerRoundCheater)
                {
                    if (this.m_enBan == enumBoolYesNo.Yes)
                    {
                        BanPlayer(strKillerSoldierName);
                        //this.ExecuteCommand("procon.protected.pluginconsole.write", "^2absKillCnt: banning . . .");
                    }
                }
            }
        }


        private int KickPlayer(string strKillerSoldierName)
        {
            int f = 1;

            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", strKillerSoldierName, this.m_KickMessagePrivate);
            this.ExecuteCommand("procon.protected.tasks.add", "KillrateChecker", "0", "1", "1", "procon.protected.send", "admin.say", this.m_KickMessagePublic.Replace("%pk%", strKillerSoldierName), "all");
            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^1Killrate Checker: an advanced player was kicked:  {0}   K/D:  {1}/{2}  KDR:  {3}", strKillerSoldierName, this.m_dicPlayers[strKillerSoldierName].Kills, this.m_dicPlayers[strKillerSoldierName].Deaths, this.m_dicPlayers[strKillerSoldierName].Kdr));

            RemovePlayerFromDictionaries(strKillerSoldierName);

            return f;
        }



        private int BanPlayer(string strKillerSoldierName)
        {
            int f = 1;

            if (this.m_enBanByName == enumBoolYesNo.Yes)
            {
                this.ExecuteCommand("procon.protected.send", "banList.add", "name", strKillerSoldierName, "perm", this.m_BanMessagePrivate.Replace("%pk%", String.Format("{0}   K/D:  {1}/{2}  KDR:  {3}", strKillerSoldierName, this.m_dicPlayers[strKillerSoldierName].Kills, this.m_dicPlayers[strKillerSoldierName].Deaths, this.m_dicPlayers[strKillerSoldierName].Kdr)));
                this.ExecuteCommand("procon.protected.tasks.add", "KillrateChecker", "0", "1", "1", "procon.protected.send", "admin.say", this.m_BanMessagePublic.Replace("%pk%", strKillerSoldierName), "all");
                this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^1Killrate Checker: a player was banned:  {0}   K/D:  {1}/{2}  KDR:  {3}", strKillerSoldierName, this.m_dicPlayers[strKillerSoldierName].Kills, this.m_dicPlayers[strKillerSoldierName].Deaths, this.m_dicPlayers[strKillerSoldierName].Kdr));
            }

            if (this.m_enBanByEA == enumBoolYesNo.Yes)
            {
                this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_dicPlayers[strKillerSoldierName].GUID, "perm", this.m_BanMessagePrivate.Replace("%pk%", String.Format("{0}   K/D:  {1}/{2}  KDR:  {3}", strKillerSoldierName, this.m_dicPlayers[strKillerSoldierName].Kills, this.m_dicPlayers[strKillerSoldierName].Deaths, this.m_dicPlayers[strKillerSoldierName].Kdr)));
                this.ExecuteCommand("procon.protected.tasks.add", "KillrateChecker", "0", "1", "1", "procon.protected.send", "admin.say", this.m_BanMessagePublic.Replace("%pk%", strKillerSoldierName), "all");
                this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^1Killrate Checker: a player was banned:  {0}   K/D:  {1}/{2}  KDR:  {3}", strKillerSoldierName, this.m_dicPlayers[strKillerSoldierName].Kills, this.m_dicPlayers[strKillerSoldierName].Deaths, this.m_dicPlayers[strKillerSoldierName].Kdr));
            }

            if (this.m_enBanByPB == enumBoolYesNo.Yes)
            {
                this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", String.Format("pb_sv_ban \"{0}\" \"{1}\"", strKillerSoldierName, "BC2! " + this.m_BanMessagePrivate.Replace("%pk%", String.Format("{0}   K/D:  {1}/{2}  KDR:  {3}", strKillerSoldierName, this.m_dicPlayers[strKillerSoldierName].Kills, this.m_dicPlayers[strKillerSoldierName].Deaths, this.m_dicPlayers[strKillerSoldierName].Kdr))));
                this.ExecuteCommand("procon.protected.tasks.add", "KillrateChecker", "0", "1", "1", "procon.protected.send", "admin.say", this.m_BanMessagePublic.Replace("%pk%", strKillerSoldierName), "all");
                this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^1Killrate Checker: a player was banned:  {0}   K/D:  {1}/{2}  KDR:  {3}", strKillerSoldierName, this.m_dicPlayers[strKillerSoldierName].Kills, this.m_dicPlayers[strKillerSoldierName].Deaths, this.m_dicPlayers[strKillerSoldierName].Kdr));
            }

            RemovePlayerFromDictionaries(strKillerSoldierName);

            return f;
        }


        private int RemovePlayerFromDictionaries(string strSoldierName)
        {
            int f = 1;

            //this.ExecuteCommand("procon.protected.pluginconsole.write", "^2removing player from dics . . .");

            //Remove the player from dictionaries
            if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayers.Remove(strSoldierName);
            }


            if (this.m_dicPlayersKillsAdvPlayers.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayersKillsAdvPlayers.Remove(strSoldierName);
            }

            if (this.m_dicPlayersKillsAdvPlayersAlt.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayersKillsAdvPlayersAlt.Remove(strSoldierName);
            }


            if (this.m_dicPlayersKillsCheaters.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayersKillsCheaters.Remove(strSoldierName);
            }

            if (this.m_dicPlayersKillsCheatersAlt.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayersKillsCheatersAlt.Remove(strSoldierName);
            }


            if (this.m_dicPlayersPastTimeStampsAdvPlayers.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayersPastTimeStampsAdvPlayers.Remove(strSoldierName);
            }

            if (this.m_dicPlayersPastTimeStampsAdvPlayersAlt.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayersPastTimeStampsAdvPlayersAlt.Remove(strSoldierName);
            }


            if (this.m_dicPlayersPastTimeStampsCheaters.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayersPastTimeStampsCheaters.Remove(strSoldierName);
            }

            if (this.m_dicPlayersPastTimeStampsCheatersAlt.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayersPastTimeStampsCheatersAlt.Remove(strSoldierName);
            }


            if (this.m_dicSuspiciousPlayersDetectionCountersAdvPlayers.ContainsKey(strSoldierName) == true)
            {
                this.m_dicSuspiciousPlayersDetectionCountersAdvPlayers.Remove(strSoldierName);
            }

            if (this.m_dicSuspiciousPlayersDetectionCountersCheaters.ContainsKey(strSoldierName) == true)
            {
                this.m_dicSuspiciousPlayersDetectionCountersCheaters.Remove(strSoldierName);
            }
            return f;
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

            int f = 0;

            // Keep the player dictionary up to date.
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

                // reset all dics on new round
                // on new round all players got no kills, if so f=0 and reset all dics, if first kill resetting is stopped
                if (this.m_dicPlayers[cpiPlayer.SoldierName].Kills > 0)
                {
                    f = 1;
                    //this.ExecuteCommand("procon.protected.pluginconsole.write", "^2some one was killed. . .");
                }
            }

            if (f == 0) //no kill yet
            {

                //this.ExecuteCommand("procon.protected.pluginconsole.write", "^2level started, clearing dics . . .");

                this.m_dicPlayersKillsAdvPlayers.Clear();
                this.m_dicPlayersKillsAdvPlayersAlt.Clear();

                this.m_dicPlayersKillsCheaters.Clear();
                this.m_dicPlayersKillsCheatersAlt.Clear();

                this.m_dicPlayersPastTimeStampsAdvPlayers.Clear();
                this.m_dicPlayersPastTimeStampsAdvPlayersAlt.Clear();

                this.m_dicPlayersPastTimeStampsCheaters.Clear();
                this.m_dicPlayersPastTimeStampsCheatersAlt.Clear();

                this.m_dicSuspiciousPlayersDetectionCountersAdvPlayers.Clear();
                this.m_dicSuspiciousPlayersDetectionCountersCheaters.Clear();
            }
        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {
            // Keep the player dictionary up to date when players change teams, this ensures teamid is knowen even before a listplayers is done.
            if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayers[strSoldierName].TeamID = iTeamID;
            }
        }

        // function to get a players team ID from the players dictionary. Returns the teamID.
        // private int GetPlayerTeamID(string strSoldierName)
        // {
        // int iTeamID = 0; // Neutral Team ID

        // if (this.m_dicPlayers.ContainsKey(strSoldierName) == true)
        // {
        // iTeamID = this.m_dicPlayers[strSoldierName].TeamID;
        // }

        // return iTeamID;
        // }



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