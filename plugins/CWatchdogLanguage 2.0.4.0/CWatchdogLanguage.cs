using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Data;

using PRoCon.Plugin;

namespace PRoConEvents
{
    public class CWatchdogLanguage : PRoConPluginAPI, IPRoConPluginInterface
    {
        /// <summary>
        /// Watchdog Langage, surveille et prends les mesures adequates. --- BAF
        /// </summary>
        ///        

        private bool m_blPluginEnabled = false;

        private enumBoolYesNo m_enYellKicks;
        private enumBoolYesNo m_enYellWarnBefNextAct;
        private enumBoolYesNo m_enSendWarnPrivately;
        private enumBoolYesNo m_enSendKillPrivately;
        private enumBoolYesNo m_enSendKicksPrivately;
        private enumBoolYesNo m_enSendTBanPrivately;
        private enumBoolYesNo m_enSendBanPrivately;
        private enumBoolYesNo m_enProtectedPlayer;
        private enumBoolYesNo m_enLinearRslt;
        //private enumBoolYesNo m_enWatchdogChatGlobal;
        //private enumBoolYesNo m_enWatchdogChatTeam;
        //private enumBoolYesNo m_enWatchdogChatSquad;
        private enumBoolYesNo m_enBanByGuid;
        private enumBoolYesNo m_enSendMsgBeforeLastAction;
        private enumBoolYesNo m_enUseActionListByWord;

        private string m_strPrivateMessagePlayerWarn;
        private string m_strPrivateMessagePlayerKill;
        private string m_strPrivateMessagePlayerKick;
        private string m_strPrivateMessagePlayerTBan;
        private string m_strPrivateMessagePlayerBan;
        private string m_strPublicMessagePlayerWarn;
        private string m_strPublicMessagePlayerKill;
        private string m_strPublicMessagePlayerKick;
        private string m_strPublicMessagePlayerTBan;
        private string m_strPublicMessagePlayerBan;
        private string strNameUserList1, strNameUserList2, strNameUserList3, strNameUserList4, strNameUserList5, strNameUserList6;

        List<string> lstUserList1 = new List<string>();
        List<string> lstUserList2 = new List<string>();
        List<string> lstUserList3 = new List<string>();
        List<string> lstUserList4 = new List<string>();
        List<string> lstUserList5 = new List<string>();
        List<string> lstUserList6 = new List<string>();
        List<string> lstProtectedPlayers = new List<string>();

        private int maxWarnUserList1, maxWarnUserList2, maxWarnUserList3, maxWarnUserList4, maxWarnUserList5, maxWarnUserList6;
        private int maxKillUserList1, maxKillUserList2, maxKillUserList3, maxKillUserList4, maxKillUserList5, maxKillUserList6;
        private int maxKickUserList1, maxKickUserList2, maxKickUserList3, maxKickUserList4, maxKickUserList5, maxKickUserList6;
        private int maxTBanUserList1, maxTBanUserList2, maxTBanUserList3, maxTBanUserList4, maxTBanUserList5, maxTBanUserList6;
        private int maxBanUserList1, maxBanUserList2, maxBanUserList3, maxBanUserList4, maxBanUserList5, maxBanUserList6;

        private int m_DayBeforeReset;
        private int m_RoundBeforeReset;
        private int m_iDelayBetweenMessageAndKick;
        private int m_iTimeOfTBan;
        private int m_iTimeOfMsgYell;

        private Dictionary<string, CPlayerInfo> m_dicPlayerInfo = new Dictionary<string, CPlayerInfo>();
        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();

        private Dictionary<string, Dictionary<string, int>> m_dicNumbreWarn = new Dictionary<string, Dictionary<string, int>>();// strSpeaker, string mot, int nbWarn
        private Dictionary<string, Dictionary<string, int>> m_dicNumbreKill = new Dictionary<string, Dictionary<string, int>>();// strSpeaker, string mot, int nbKill
        private Dictionary<string, Dictionary<string, int>> m_dicNumbreKick = new Dictionary<string, Dictionary<string, int>>();// strSpeaker, string mot, int nbKick
        private Dictionary<string, Dictionary<string, int>> m_dicNumbreTBan = new Dictionary<string, Dictionary<string, int>>();// strSpeaker, string mot, int nbTBan

        private Dictionary<string, int> m_dicNumbreWarnBySpeaker = new Dictionary<string, int>();  // strSpeaker, nbWarn
        private Dictionary<string, int> m_dicNumbreKillBySpeaker = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicNumbreKickBySpeaker = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicNumbreTBanBySpeaker = new Dictionary<string, int>();
        private Dictionary<string, string> m_dicLastDateOffencedPlayer = new Dictionary<string, string>(); //strSpeaker, dateTime.ToString("d")
        private Dictionary<string, int> m_dicNbRoundWithoutOffencedPlayer = new Dictionary<string, int>(); //strSpeaker, nbRound

        public enum MethTtype { Warn = 1, Kill, Kick, TBan, Ban };

        public CWatchdogLanguage()
        {
            /*this.lstUserList3 = new string[] { ""}; 
            this.lstUserList4 = new string[] { "" };
            this.lstUserList5 = new string[]{""};
            this.lstUserList6 = new string[]{""};
            this.lstProtectedPlayers = new string[] { "" };*/

            this.m_enYellKicks = enumBoolYesNo.No;
            this.m_enYellWarnBefNextAct = enumBoolYesNo.No;
            this.m_enSendWarnPrivately = enumBoolYesNo.No;
            this.m_enSendKicksPrivately = enumBoolYesNo.No;
            this.m_enSendKillPrivately = enumBoolYesNo.No;
            this.m_enSendTBanPrivately = enumBoolYesNo.No;
            this.m_enSendBanPrivately = enumBoolYesNo.No;
            this.m_enProtectedPlayer = enumBoolYesNo.Yes;
            this.m_enLinearRslt = enumBoolYesNo.No;
            //this.m_enWatchdogChatGlobal = enumBoolYesNo.Yes;
            //this.m_enWatchdogChatTeam = enumBoolYesNo.Yes;
            //this.m_enWatchdogChatSquad = enumBoolYesNo.No;
            this.m_enBanByGuid = enumBoolYesNo.Yes;
            this.m_enSendMsgBeforeLastAction = enumBoolYesNo.Yes;
            this.m_enUseActionListByWord = enumBoolYesNo.No;

            this.m_strPrivateMessagePlayerWarn = @"Your are warned for your language";
            this.m_strPrivateMessagePlayerKill = @"Your are killed for your language";
            this.m_strPrivateMessagePlayerKick = @"Your are kicked for your language";
            this.m_strPrivateMessagePlayerTBan = @"Your are temp banned (%Time%) for your language";
            this.m_strPrivateMessagePlayerBan = @"Your are banned for your language";
            this.m_strPublicMessagePlayerWarn = @"Watchdog: %PN% was warned for language!";
            this.m_strPublicMessagePlayerKill = @"Watchdog: %PN% was killed for language!";
            this.m_strPublicMessagePlayerKick = @"Watchdog: %PN% was kicked for language!";
            this.m_strPublicMessagePlayerTBan = @"Watchdog: %PN% was temp banned (%Time%) for Language! .";
            this.m_strPublicMessagePlayerBan = @"Watchdog: %PN% was auto banned for language!";
            this.strNameUserList1 = @"UserList1";
            this.strNameUserList2 = @"UserList2";
            this.strNameUserList3 = @"UserList3";
            this.strNameUserList4 = @"UserList4";
            this.strNameUserList5 = @"UserList5";
            this.strNameUserList6 = @"UserList6";

            this.maxWarnUserList1 = 1; this.maxWarnUserList2 = 1; this.maxWarnUserList3 = 1; this.maxWarnUserList4 = 1; this.maxWarnUserList5 = 1; this.maxWarnUserList6 = 1;
            this.maxKillUserList1 = 2; maxKillUserList2 = 2; maxKillUserList3 = 2; maxKillUserList4 = 2; maxKillUserList5 = 2; maxKillUserList6 = 2;
            this.maxKickUserList1 = 1; maxKickUserList2 = 1; maxKickUserList3 = 1; maxKickUserList4 = 1; maxKickUserList5 = 1; maxKickUserList6 = 1;
            this.maxTBanUserList1 = 5; maxTBanUserList2 = 5; maxTBanUserList3 = 5; maxTBanUserList4 = 5; maxTBanUserList5 = 5; maxTBanUserList6 = 5;
            this.maxBanUserList1 = 1; maxBanUserList2 = 1; maxBanUserList3 = 1; maxBanUserList4 = 1; maxBanUserList5 = 1; maxBanUserList6 = 1;
            this.m_DayBeforeReset = 2;
            this.m_RoundBeforeReset = 20;
            this.m_iDelayBetweenMessageAndKick = 5;
            this.m_iTimeOfTBan = 120;
            this.m_iTimeOfMsgYell = 0;

        }

        public string GetPluginName()
        {
            return "Watchdog Language";
        }

        public string GetPluginVersion()
        {
            return "2.0.4.0";
        }

        public string GetPluginAuthor()
        {
            return "Sparda - www.la-baf.eu - FR";
        }

        public string GetPluginWebsite()
        {
            return "www.la-baf.eu";
        }

        public string GetPluginDescription()
        {
            return @"
            <h2>Description</h2>
             <p>Warn/Kill/kick/Tban/Ban player when using bad word</p>
            
                <p>List a forbidden word categories and the watchdog does the rest.<br/>
                You can give increasing punishments.</p>
    <h2>Setting</h2>
        <h3>Communication</h3>
        <blockquote><h4>Yell Message</h4>Option between yelling (the big text in the middle of the screen) or saying (chat box in the top left corner)</blockquote>
		<blockquote><h4>Yell Message for :Warn before next action: </h4>Option between yelling (the big text in the middle of the screen) or saying (chat box in the top left corner)</blockquote>
        <blockquote><h4>Send privately warn to player</h4>This will send the warn message directly to the player or shout the warn to all players</blockquote>
        <blockquote><h4>Send privately kill to player</h4>This will send the kill message directly to the player or shout the kill to all players</blockquote>
        <blockquote><h4>Send privately kick to player</h4>This will send the kick message directly to the player or shout the kick to all players</blockquote>
        <blockquote><h4>Send privately TBan to player</h4>This will send the TBan message directly to the player or shout the TBan to all players</blockquote>
        <blockquote><h4>Send privately Ban to player</h4>This will send the Ban message directly to the player or shout the Ban to all players</blockquote>

        <blockquote><h4>Private warn message</h4>The message to use when privately telling the player they are about to be warned as well as the reason displayed in the warn message</blockquote>
        <blockquote><h4>Public warn message</h4>The message to use when shouting a warn message to the whole server</blockquote>
        <blockquote><h4>Private kill message</h4>The message to use when privately telling the player they are about to be warned as well as the reason displayed in the warn message</blockquote>
        <blockquote><h4>Public kill message</h4>The message to use when shouting a kill message to the whole server</blockquote>
        <blockquote><h4>Private kick message</h4>The message to use when privately telling the player they are about to be kicked as well as the reason displayed in the kick message</blockquote>
        <blockquote><h4>Public kick message</h4>The message to use when shouting a kick message to the whole server</blockquote>
        <blockquote><h4>Private Tban message</h4>The message to use when privately telling the player they are about to be Tbanned as well as the reason displayed in the Tban message</blockquote>
        <blockquote><h4>Public Tban message</h4>The message to use when shouting a Tban message to the whole server</blockquote>
        <blockquote><h4>Private ban message</h4>The message to use when privately telling the player they are about to be banned as well as the reason displayed in the ban message</blockquote>
        <blockquote><h4>Public ban message</h4>The message to use when shouting a ban message to the whole server</blockquote>

        <h3>Communication - Additional Options message context replacements</h2>
        <blockquote><h4>%PN%</h4>Show Playername for public message only</blockquote>
        <blockquote><h4>%Time%</h4>Show Time in seconde for TBan methode only</blockquote>

        <h3>Watchdog</h3>
		<blockquote><h4>User List 1</h4>List 1 of word that triggers the automatics action (with incremental Action). For use regex you need to start word by R/ </blockquote>
        <blockquote><h4>User List 2</h4>List 2 of word that triggers the automatics action (with incremental Action). For use regex you need to start word by R/ </blockquote>
        <blockquote><h4>User List 3</h4>List 3 of word that triggers the automatics action (with incremental Action). For use regex you need to start word by R/ </blockquote>
        <blockquote><h4>User List 4</h4>List 4 of word that triggers the automatics action (with incremental Action). For use regex you need to start word by R/ </blockquote>
        <blockquote><h4>User List 5</h4>List 5 of word that triggers the automatics action (with incremental Action). For use regex you need to start word by R/ </blockquote>
        <blockquote><h4>User List 6</h4>List 6 of word that triggers the automatics action (with incremental Action). For use regex you need to start word by R/ </blockquote>
		<blockquote><h4>User List name 1</h4>Name of User List 1</blockquote>
        <blockquote><h4>User List name 2</h4>Name of User List 2</blockquote>
        <blockquote><h4>User List name 3</h4>Name of User List 3</blockquote>
        <blockquote><h4>User List name 4</h4>Name of User List 4</blockquote>
        <blockquote><h4>User List name 5</h4>Name of User List 5</blockquote>
        <blockquote><h4>User List name 6</h4>Name of User List 6</blockquote>
		
        <h3>Watchdog Param</h3>
        <blockquote><h4>Linear methode</h4>Methode of kill</blockquote>
            <p>if yes, after a kick, the program doesn't restart from the kill <br/></p>
            <p>exemple if no: kill,kill,kick,kill,kill,kick,kill,kill,Tban <br/></p>
            <p>exemple if yes: kill,kill,kick,kick,TBan <br/></p>
        <blockquote><h4>Max warn user List x </h4>Maximum warn before next action, 0 for never, for list User x</blockquote>
        <blockquote><h4>Max kill user List x </h4>Maximum kill before next action, 0 for never, for list User x</blockquote>
        <blockquote><h4>Max kick user List x </h4>Maximum kick before next action, 0 for never, for list User x</blockquote>
        <blockquote><h4>Max Tban user List x </h4>Maximum TBan before next action, 0 for never, for list User x</blockquote>
		<blockquote><h4>Max ban user List x </h4>Maximum Ban before next action, 0 for never, for list User x</blockquote>
        <blockquote><h4>Day before Reset</h4>Number of day without offence before resetting counters (Tban & ban are never removed), 0 for never</blockquote>
        <blockquote><h4>Round before Reset</h4>Number of round without offence before resetting counters (Tban & ban are never removed), 0 for never</blockquote>
        <blockquote><h4>Time Ban in second</h4>Time of TBan in seconde</blockquote>
		<blockquote><h4>Time Yell Message in second</h4>Time of Yell Message in seconde, 0=auto</blockquote>
        <blockquote><h4>Delay between message and kill/kick/Tban/Ban</h4>Delay in telling the player/server the player is about to be kicked and the actual kick</blockquote>
        <blockquote><h4>Ban by Guid ? (no = by name)</h4>Methode for ban = guid</blockquote>
        <blockquote><h4>Warn before next action</h4>Send message to player, warning a higher action will be taken to the next action </blockquote>
		<blockquote><h4>Use Action List By Word</h4>Each word has its own action list, ex: </blockquote>
			<p> If Use Action List By Word = yes  <br/>
				soab -> 1st warn <br/>
				asshole -> 1st warn  <br/>
				soab -> 2nd warn <br/>
				soab -> kick <br/>
				asshole -> 2nd warn <br/>
			</p>
            <p> If Use Action List By Word = No  <br/>
				soab -> 1st warn <br/>
				asshole -> 2nd warn <br/>
				soab -> kick <br/>
			</p>	
        <h3>White list option</h3>
        <blockquote><h4>Protected player</h4>Using the white list</blockquote>
        <blockquote><h4>List of protected players</h4>List of protected players</blockquote>

    <h2>Using Regex</h2>        
        <p>exemple: kill this word > noob and all forms > nooooooob</p>
        <p>Add this R/n[o0]{2,}b{1,}[^a-z]{1,}[ .]*</p>

        <p>1) not kill/kick/... : xxxWordxxx</p>
            <p>use : R/[^a-z]{1}Word[^a-z]{1,}[ .]* </p>
            <p>Word = ass</p>
            <p>Result : glass, assignment are authorized; ass is not authorized</p>

       <p>2) not kill/kick/... : Wordxxx</p>
            <p>use : R/Word[^a-z]{1,}[ .]* </p>
            <p>Word = ass</p>
            <p>Result : assignment is authorized, glass and ass are not authorized</p>

        <p>3) not kill/kick/... : xxxWord</p>
            <p>use : R/[^a-z]{1}</p>
            <p>Word = ass</p>
            <p>Result : glass is authorized, assignment and ass are not authorized </p>

            ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
        }

        public void OnPluginEnable()
        {
            this.m_blPluginEnabled = true;
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bWatchdog ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.m_blPluginEnabled = false;
            this.ExecuteCommand("procon.protected.tasks.remove", "CWatchdogLanguage");

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bWatchdog ^1Disabled =(");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Communication|Yell Message", typeof(enumBoolYesNo), this.m_enYellKicks));
            lstReturn.Add(new CPluginVariable("Communication|Yell Message for Warn before next action", typeof(enumBoolYesNo), this.m_enYellWarnBefNextAct));
            lstReturn.Add(new CPluginVariable("Communication|Send privately warn to player", typeof(enumBoolYesNo), this.m_enSendWarnPrivately));
            lstReturn.Add(new CPluginVariable("Communication|Send privately kill to player", typeof(enumBoolYesNo), this.m_enSendKillPrivately));
            lstReturn.Add(new CPluginVariable("Communication|Send privately kick to player", typeof(enumBoolYesNo), this.m_enSendKicksPrivately));
            lstReturn.Add(new CPluginVariable("Communication|Send privately TBan to player", typeof(enumBoolYesNo), this.m_enSendTBanPrivately));
            lstReturn.Add(new CPluginVariable("Communication|Send privately Ban to player", typeof(enumBoolYesNo), this.m_enSendBanPrivately));
            lstReturn.Add(new CPluginVariable("Whitelist options|Protected Player", typeof(enumBoolYesNo), this.m_enProtectedPlayer));
            //lstReturn.Add(new CPluginVariable("WatchDog options|Watchdog global chat", typeof(enumBoolYesNo), this.m_enWatchdogChatGlobal));
            //lstReturn.Add(new CPluginVariable("WatchDog options|Watchdog team chat", typeof(enumBoolYesNo), this.m_enWatchdogChatTeam));
            //lstReturn.Add(new CPluginVariable("WatchDog options|Watchdog squad chat", typeof(enumBoolYesNo), this.m_enWatchdogChatSquad));



            lstReturn.Add(new CPluginVariable("Communication|Private warn message", this.m_strPrivateMessagePlayerWarn.GetType(), this.m_strPrivateMessagePlayerWarn));
            lstReturn.Add(new CPluginVariable("Communication|Public warn message", this.m_strPublicMessagePlayerWarn.GetType(), this.m_strPublicMessagePlayerWarn));
            //if (this.m_enSendKillPrivately == enumBoolYesNo.Yes)
            lstReturn.Add(new CPluginVariable("Communication|Private kill message", this.m_strPrivateMessagePlayerKill.GetType(), this.m_strPrivateMessagePlayerKill));
            //else
            lstReturn.Add(new CPluginVariable("Communication|Public kill message", this.m_strPublicMessagePlayerKill.GetType(), this.m_strPublicMessagePlayerKill));

            //if (this.m_enSendKicksPrivately == enumBoolYesNo.Yes)
            lstReturn.Add(new CPluginVariable("Communication|Private kick message", this.m_strPrivateMessagePlayerKick.GetType(), this.m_strPrivateMessagePlayerKick));
            //else
            lstReturn.Add(new CPluginVariable("Communication|Public kick message", this.m_strPublicMessagePlayerKick.GetType(), this.m_strPublicMessagePlayerKick));

            //if (this.m_enSendTBanPrivately == enumBoolYesNo.Yes)
            lstReturn.Add(new CPluginVariable("Communication|Private TBan message", this.m_strPrivateMessagePlayerTBan.GetType(), this.m_strPrivateMessagePlayerTBan));
            //else
            lstReturn.Add(new CPluginVariable("Communication|Public TBan message", this.m_strPublicMessagePlayerTBan.GetType(), this.m_strPublicMessagePlayerTBan));

            //if (this.m_enSendBanPrivately == enumBoolYesNo.Yes)
            lstReturn.Add(new CPluginVariable("Communication|Private ban message", this.m_strPrivateMessagePlayerBan.GetType(), this.m_strPrivateMessagePlayerBan));
            //else
            lstReturn.Add(new CPluginVariable("Communication|Public ban message", this.m_strPublicMessagePlayerBan.GetType(), this.m_strPublicMessagePlayerBan));


            lstReturn.Add(new CPluginVariable("Whitelist options|List of Protected Players", typeof(string[]), this.lstProtectedPlayers.ToArray()));
            //lstReturn.Add(new CPluginVariable("Watchdog|Kill Words", typeof(string[]), this.lstUserList3));
            lstReturn.Add(new CPluginVariable("Watchdog|User List 1", typeof(string[]), this.lstUserList1.ToArray()));
            lstReturn.Add(new CPluginVariable("Watchdog|Name User List 1", this.strNameUserList1.GetType(), this.strNameUserList1));
            lstReturn.Add(new CPluginVariable("Watchdog|User List 2", typeof(string[]), this.lstUserList2.ToArray()));
            lstReturn.Add(new CPluginVariable("Watchdog|Name User List 2", this.strNameUserList2.GetType(), this.strNameUserList2));
            lstReturn.Add(new CPluginVariable("Watchdog|User List 3", typeof(string[]), this.lstUserList3.ToArray()));
            lstReturn.Add(new CPluginVariable("Watchdog|Name User List 3", this.strNameUserList3.GetType(), this.strNameUserList3));
            lstReturn.Add(new CPluginVariable("Watchdog|User List 4", typeof(string[]), this.lstUserList4.ToArray()));
            lstReturn.Add(new CPluginVariable("Watchdog|Name User List 4", this.strNameUserList4.GetType(), this.strNameUserList4));
            lstReturn.Add(new CPluginVariable("Watchdog|User List 5", typeof(string[]), this.lstUserList5.ToArray()));
            lstReturn.Add(new CPluginVariable("Watchdog|Name User List 5", this.strNameUserList5.GetType(), this.strNameUserList5));
            lstReturn.Add(new CPluginVariable("Watchdog|User List 6", typeof(string[]), this.lstUserList6.ToArray()));
            lstReturn.Add(new CPluginVariable("Watchdog|Name User List 6", this.strNameUserList6.GetType(), this.strNameUserList6));

            lstReturn.Add(new CPluginVariable("Watchdog Param|Linear methode >", typeof(enumBoolYesNo), this.m_enLinearRslt));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList1|Max warn User List 1>", typeof(int), this.maxWarnUserList1));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList1|Max kill User List 1>", typeof(int), this.maxKillUserList1));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList1|Max kick User List 1>", typeof(int), this.maxKickUserList1));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList1|Max TBan User List 1>", typeof(int), this.maxTBanUserList1));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList1|Max Ban User List 1>", typeof(int), this.maxBanUserList1));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList2|Max warn User List 2>", typeof(int), this.maxWarnUserList2));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList2|Max kill User List 2>", typeof(int), this.maxKillUserList2));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList2|Max kick User List 2>", typeof(int), this.maxKickUserList2));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList2|Max TBan User List 2>", typeof(int), this.maxTBanUserList2));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList2|Max Ban User List 2>", typeof(int), this.maxBanUserList2));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList3|Max warn User List 3>", typeof(int), this.maxWarnUserList3));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList3|Max kill User List 3>", typeof(int), this.maxKillUserList3));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList3|Max kick User List 3>", typeof(int), this.maxKickUserList3));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList3|Max TBan User List 3>", typeof(int), this.maxTBanUserList3));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList3|Max Ban User List 3>", typeof(int), this.maxBanUserList3));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList4|Max warn User List 4>", typeof(int), this.maxWarnUserList4));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList4|Max kill User List 4>", typeof(int), this.maxKillUserList4));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList4|Max kick User List 4>", typeof(int), this.maxKickUserList4));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList4|Max TBan User List 4>", typeof(int), this.maxTBanUserList4));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList4|Max Ban User List 4>", typeof(int), this.maxBanUserList4));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList5|Max warn User List 5>", typeof(int), this.maxWarnUserList5));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList5|Max kill User List 5>", typeof(int), this.maxKillUserList5));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList5|Max kick User List 5>", typeof(int), this.maxKickUserList5));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList5|Max TBan User List 5>", typeof(int), this.maxTBanUserList5));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList5|Max Ban User List 5>", typeof(int), this.maxBanUserList5));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList6|Max warn User List 6>", typeof(int), this.maxWarnUserList6));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList6|Max kill User List 6>", typeof(int), this.maxKillUserList6));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList6|Max kick User List 6>", typeof(int), this.maxKickUserList6));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList6|Max TBan User List 6>", typeof(int), this.maxTBanUserList6));
            lstReturn.Add(new CPluginVariable("Watchdog Param userList6|Max Ban User List 6>", typeof(int), this.maxBanUserList6));
            lstReturn.Add(new CPluginVariable("Watchdog Param|Day before reset >", typeof(int), this.m_DayBeforeReset));
            lstReturn.Add(new CPluginVariable("Watchdog Param|Round before reset >", typeof(int), this.m_RoundBeforeReset));
            lstReturn.Add(new CPluginVariable("Watchdog Param|Time Ban in second >", typeof(int), this.m_iTimeOfTBan));
            lstReturn.Add(new CPluginVariable("Watchdog Param|Time Yell message in second >", typeof(int), this.m_iTimeOfMsgYell));
            lstReturn.Add(new CPluginVariable("Watchdog Param|Delay between message and kick", typeof(int), this.m_iDelayBetweenMessageAndKick));
            lstReturn.Add(new CPluginVariable("Watchdog Param|Ban by guid", typeof(enumBoolYesNo), this.m_enBanByGuid));
            lstReturn.Add(new CPluginVariable("Watchdog Param|Warn before next action", typeof(enumBoolYesNo), this.m_enSendMsgBeforeLastAction));
            lstReturn.Add(new CPluginVariable("Watchdog Param|Use Action List By Word", typeof(enumBoolYesNo), this.m_enUseActionListByWord));
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Yell Message", typeof(enumBoolYesNo), this.m_enYellKicks));
            lstReturn.Add(new CPluginVariable("Yell Message for Warn before next action", typeof(enumBoolYesNo), this.m_enYellWarnBefNextAct));

            lstReturn.Add(new CPluginVariable("Send privately warn to player", typeof(enumBoolYesNo), this.m_enSendWarnPrivately));
            lstReturn.Add(new CPluginVariable("Send privately kill to player", typeof(enumBoolYesNo), this.m_enSendKillPrivately));
            lstReturn.Add(new CPluginVariable("Send privately kick to player", typeof(enumBoolYesNo), this.m_enSendKicksPrivately));
            lstReturn.Add(new CPluginVariable("Send privately TBan to player", typeof(enumBoolYesNo), this.m_enSendTBanPrivately));
            lstReturn.Add(new CPluginVariable("Send privately Ban to player", typeof(enumBoolYesNo), this.m_enSendBanPrivately));
            lstReturn.Add(new CPluginVariable("Protected Player", typeof(enumBoolYesNo), this.m_enProtectedPlayer));
            //lstReturn.Add(new CPluginVariable("Watchdog global chat", typeof(enumBoolYesNo), this.m_enWatchdogChatGlobal));
            //lstReturn.Add(new CPluginVariable("Watchdog team chat", typeof(enumBoolYesNo), this.m_enWatchdogChatTeam));
            //lstReturn.Add(new CPluginVariable("Watchdog squad chat", typeof(enumBoolYesNo), this.m_enWatchdogChatSquad));

            lstReturn.Add(new CPluginVariable("Private warn message", this.m_strPrivateMessagePlayerWarn.GetType(), this.m_strPrivateMessagePlayerWarn));
            lstReturn.Add(new CPluginVariable("Public warn message", this.m_strPublicMessagePlayerWarn.GetType(), this.m_strPublicMessagePlayerWarn));
            lstReturn.Add(new CPluginVariable("Private kill message", this.m_strPrivateMessagePlayerKill.GetType(), this.m_strPrivateMessagePlayerKill));
            lstReturn.Add(new CPluginVariable("Public kill message", this.m_strPublicMessagePlayerKill.GetType(), this.m_strPublicMessagePlayerKill));
            lstReturn.Add(new CPluginVariable("Private kick message", this.m_strPrivateMessagePlayerKick.GetType(), this.m_strPrivateMessagePlayerKick));
            lstReturn.Add(new CPluginVariable("Public kick message", this.m_strPublicMessagePlayerKick.GetType(), this.m_strPublicMessagePlayerKick));
            lstReturn.Add(new CPluginVariable("Private TBan message", this.m_strPrivateMessagePlayerTBan.GetType(), this.m_strPrivateMessagePlayerTBan));
            lstReturn.Add(new CPluginVariable("Public TBan message", this.m_strPublicMessagePlayerTBan.GetType(), this.m_strPublicMessagePlayerTBan));
            lstReturn.Add(new CPluginVariable("Private ban message", this.m_strPrivateMessagePlayerBan.GetType(), this.m_strPrivateMessagePlayerBan));
            lstReturn.Add(new CPluginVariable("Public ban message", this.m_strPublicMessagePlayerBan.GetType(), this.m_strPublicMessagePlayerBan));

            lstReturn.Add(new CPluginVariable("List of Protected Players", typeof(string[]), this.lstProtectedPlayers.ToArray()));
            lstReturn.Add(new CPluginVariable("User List 1", typeof(string[]), this.lstUserList1.ToArray()));
            lstReturn.Add(new CPluginVariable("User List 2", typeof(string[]), this.lstUserList2.ToArray()));
            lstReturn.Add(new CPluginVariable("User List 3", typeof(string[]), this.lstUserList3.ToArray()));
            lstReturn.Add(new CPluginVariable("User List 4", typeof(string[]), this.lstUserList4.ToArray()));
            lstReturn.Add(new CPluginVariable("User List 5", typeof(string[]), this.lstUserList5.ToArray()));
            lstReturn.Add(new CPluginVariable("User List 6", typeof(string[]), this.lstUserList6.ToArray()));
            lstReturn.Add(new CPluginVariable("Name User List 1", this.strNameUserList1.GetType(), this.strNameUserList1));
            lstReturn.Add(new CPluginVariable("Name User List 2", this.strNameUserList2.GetType(), this.strNameUserList2));
            lstReturn.Add(new CPluginVariable("Name User List 3", this.strNameUserList3.GetType(), this.strNameUserList3));
            lstReturn.Add(new CPluginVariable("Name User List 4", this.strNameUserList4.GetType(), this.strNameUserList4));
            lstReturn.Add(new CPluginVariable("Name User List 5", this.strNameUserList5.GetType(), this.strNameUserList5));
            lstReturn.Add(new CPluginVariable("Name User List 6", this.strNameUserList6.GetType(), this.strNameUserList6));

            lstReturn.Add(new CPluginVariable("Linear methode >", typeof(enumBoolYesNo), this.m_enLinearRslt));
            lstReturn.Add(new CPluginVariable("Max warn User List 1>", typeof(int), this.maxWarnUserList1));
            lstReturn.Add(new CPluginVariable("Max kill User List 1>", typeof(int), this.maxKillUserList1));
            lstReturn.Add(new CPluginVariable("Max kick User List 1>", typeof(int), this.maxKickUserList1));
            lstReturn.Add(new CPluginVariable("Max TBan User List 1>", typeof(int), this.maxTBanUserList1));
            lstReturn.Add(new CPluginVariable("Max Ban User List 1>", typeof(int), this.maxBanUserList1));
            lstReturn.Add(new CPluginVariable("Max warn User List 2>", typeof(int), this.maxWarnUserList2));
            lstReturn.Add(new CPluginVariable("Max kill User List 2>", typeof(int), this.maxKillUserList2));
            lstReturn.Add(new CPluginVariable("Max kick User List 2>", typeof(int), this.maxKickUserList2));
            lstReturn.Add(new CPluginVariable("Max TBan User List 2>", typeof(int), this.maxTBanUserList2));
            lstReturn.Add(new CPluginVariable("Max Ban User List 2>", typeof(int), this.maxBanUserList2));
            lstReturn.Add(new CPluginVariable("Max warn User List 3>", typeof(int), this.maxWarnUserList3));
            lstReturn.Add(new CPluginVariable("Max kill User List 3>", typeof(int), this.maxKillUserList3));
            lstReturn.Add(new CPluginVariable("Max kick User List 3>", typeof(int), this.maxKickUserList3));
            lstReturn.Add(new CPluginVariable("Max TBan User List 3>", typeof(int), this.maxTBanUserList3));
            lstReturn.Add(new CPluginVariable("Max Ban User List 3>", typeof(int), this.maxBanUserList3));
            lstReturn.Add(new CPluginVariable("Max warn User List 4>", typeof(int), this.maxWarnUserList4));
            lstReturn.Add(new CPluginVariable("Max kill User List 4>", typeof(int), this.maxKillUserList4));
            lstReturn.Add(new CPluginVariable("Max kick User List 4>", typeof(int), this.maxKickUserList4));
            lstReturn.Add(new CPluginVariable("Max TBan User List 4>", typeof(int), this.maxTBanUserList4));
            lstReturn.Add(new CPluginVariable("Max Ban User List 4>", typeof(int), this.maxBanUserList4));
            lstReturn.Add(new CPluginVariable("Max warn User List 5>", typeof(int), this.maxWarnUserList5));
            lstReturn.Add(new CPluginVariable("Max kill User List 5>", typeof(int), this.maxKillUserList5));
            lstReturn.Add(new CPluginVariable("Max kick User List 5>", typeof(int), this.maxKickUserList5));
            lstReturn.Add(new CPluginVariable("Max TBan User List 5>", typeof(int), this.maxTBanUserList5));
            lstReturn.Add(new CPluginVariable("Max Ban User List 5>", typeof(int), this.maxBanUserList5));
            lstReturn.Add(new CPluginVariable("Max warn User List 6>", typeof(int), this.maxWarnUserList6));
            lstReturn.Add(new CPluginVariable("Max kill User List 6>", typeof(int), this.maxKillUserList6));
            lstReturn.Add(new CPluginVariable("Max kick User List 6>", typeof(int), this.maxKickUserList6));
            lstReturn.Add(new CPluginVariable("Max TBan User List 6>", typeof(int), this.maxTBanUserList6));
            lstReturn.Add(new CPluginVariable("Max Ban User List 6>", typeof(int), this.maxBanUserList6));

            lstReturn.Add(new CPluginVariable("Day before reset >", typeof(int), this.m_DayBeforeReset));
            lstReturn.Add(new CPluginVariable("Round before reset >", typeof(int), this.m_RoundBeforeReset));
            lstReturn.Add(new CPluginVariable("Time Ban in second >", typeof(int), this.m_iTimeOfTBan));
            lstReturn.Add(new CPluginVariable("Time Yell message in second >", typeof(int), this.m_iTimeOfMsgYell));
            lstReturn.Add(new CPluginVariable("Delay between message and kick >", typeof(int), this.m_iDelayBetweenMessageAndKick));
            lstReturn.Add(new CPluginVariable("Ban by guid", typeof(enumBoolYesNo), this.m_enBanByGuid));
            lstReturn.Add(new CPluginVariable("Warn before next action", typeof(enumBoolYesNo), this.m_enSendMsgBeforeLastAction));
            lstReturn.Add(new CPluginVariable("Use Action List By Word", typeof(enumBoolYesNo), this.m_enUseActionListByWord));
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iTimeSeconds;
            strValue = CPluginVariable.Decode(strValue);

            if (strVariable.CompareTo("Yell Message") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enYellKicks = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            if (strVariable.CompareTo("Yell Message for Warn before next action") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enYellWarnBefNextAct = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            else if (strVariable.CompareTo("Send privately warn to player") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enSendWarnPrivately = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            else if (strVariable.CompareTo("Send privately kill to player") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enSendKillPrivately = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            else if (strVariable.CompareTo("Send privately kick to player") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enSendKicksPrivately = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            else if (strVariable.CompareTo("Send privately TBan to player") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enSendTBanPrivately = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            else if (strVariable.CompareTo("Send privately Ban to player") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enSendBanPrivately = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            else if (strVariable.CompareTo("Protected Player") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enProtectedPlayer = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            //else if (strVariable.CompareTo("Watchdog global chat") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)            
            //    this.m_enWatchdogChatGlobal = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);   
            //else if (strVariable.CompareTo("Watchdog team chat") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)            
            //    this.m_enWatchdogChatTeam = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);  
            //else if (strVariable.CompareTo("Watchdog squad chat") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)            
            //    this.m_enWatchdogChatSquad = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);  

            else if (strVariable.CompareTo("Private warn message") == 0)
                this.m_strPrivateMessagePlayerWarn = strValue;
            else if (strVariable.CompareTo("Public warn message") == 0)
                this.m_strPublicMessagePlayerWarn = strValue;
            else if (strVariable.CompareTo("Private kill message") == 0)
                this.m_strPrivateMessagePlayerKill = strValue;
            else if (strVariable.CompareTo("Public kill message") == 0)
                this.m_strPublicMessagePlayerKill = strValue;
            else if (strVariable.CompareTo("Private kick message") == 0)
                this.m_strPrivateMessagePlayerKick = strValue;
            else if (strVariable.CompareTo("Public kick message") == 0)
                this.m_strPublicMessagePlayerKick = strValue;
            else if (strVariable.CompareTo("Private TBan message") == 0)
                this.m_strPrivateMessagePlayerTBan = strValue;
            else if (strVariable.CompareTo("Public TBan message") == 0)
                this.m_strPublicMessagePlayerTBan = strValue;
            else if (strVariable.CompareTo("Private ban message") == 0)
                this.m_strPrivateMessagePlayerBan = strValue;
            else if (strVariable.CompareTo("Public ban message") == 0)
                this.m_strPublicMessagePlayerBan = strValue;

            else if (strVariable.CompareTo("List of Protected Players") == 0)
                this.lstProtectedPlayers = new List<string>(strValue.Split(new char[] { '|' }));
            else if (strVariable.CompareTo("User List 1") == 0)
                this.lstUserList1 = new List<string>(strValue.Split(new char[] { '|' }));
            else if (strVariable.CompareTo("User List 2") == 0)
                this.lstUserList2 = new List<string>(strValue.Split(new char[] { '|' }));
            else if (strVariable.CompareTo("User List 3") == 0)
                this.lstUserList3 = new List<string>(strValue.Split(new char[] { '|' }));//CPluginVariable.DecodeStringArray(strValue));   
            else if (strVariable.CompareTo("User List 4") == 0)
                this.lstUserList4 = new List<string>(strValue.Split(new char[] { '|' }));
            else if (strVariable.CompareTo("User List 5") == 0)
                this.lstUserList5 = new List<string>(strValue.Split(new char[] { '|' }));
            else if (strVariable.CompareTo("User List 6") == 0)
                this.lstUserList6 = new List<string>(strValue.Split(new char[] { '|' }));
            else if (strVariable.CompareTo("Name User List 1") == 0)
                this.strNameUserList1 = strValue;
            else if (strVariable.CompareTo("Name User List 2") == 0)
                this.strNameUserList2 = strValue;
            else if (strVariable.CompareTo("Name User List 3") == 0)
                this.strNameUserList3 = strValue;
            else if (strVariable.CompareTo("Name User List 4") == 0)
                this.strNameUserList4 = strValue;
            else if (strVariable.CompareTo("Name User List 5") == 0)
                this.strNameUserList5 = strValue;
            else if (strVariable.CompareTo("Name User List 6") == 0)
                this.strNameUserList6 = strValue;

            else if (strVariable.CompareTo("Linear methode >") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enLinearRslt = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            else if (strVariable.CompareTo("Max warn User List 1>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxWarnUserList1 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kill User List 1>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKillUserList1 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kick User List 1>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKickUserList1 = iTimeSeconds;
            else if (strVariable.CompareTo("Max TBan User List 1>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxTBanUserList1 = iTimeSeconds;
            else if (strVariable.CompareTo("Max Ban User List 1>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxBanUserList1 = iTimeSeconds;
            else if (strVariable.CompareTo("Max warn User List 2>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxWarnUserList2 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kill User List 2>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKillUserList2 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kick User List 2>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKickUserList2 = iTimeSeconds;
            else if (strVariable.CompareTo("Max TBan User List 2>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxTBanUserList2 = iTimeSeconds;
            else if (strVariable.CompareTo("Max Ban User List 2>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxBanUserList2 = iTimeSeconds;
            else if (strVariable.CompareTo("Max warn User List 3>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxWarnUserList3 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kill User List 3>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKillUserList3 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kick User List 3>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKickUserList3 = iTimeSeconds;
            else if (strVariable.CompareTo("Max TBan User List 3>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxTBanUserList3 = iTimeSeconds;
            else if (strVariable.CompareTo("Max Ban User List 3>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxBanUserList3 = iTimeSeconds;
            else if (strVariable.CompareTo("Max warn User List 4>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxWarnUserList4 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kill User List 4>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKillUserList4 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kick User List 4>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKickUserList4 = iTimeSeconds;
            else if (strVariable.CompareTo("Max TBan User List 4>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxTBanUserList4 = iTimeSeconds;
            else if (strVariable.CompareTo("Max Ban User List 4>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxBanUserList4 = iTimeSeconds;
            else if (strVariable.CompareTo("Max warn User List 5>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxWarnUserList5 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kill User List 5>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKillUserList5 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kick User List 5>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKickUserList5 = iTimeSeconds;
            else if (strVariable.CompareTo("Max TBan User List 5>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxTBanUserList5 = iTimeSeconds;
            else if (strVariable.CompareTo("Max Ban User List 5>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxBanUserList5 = iTimeSeconds;
            else if (strVariable.CompareTo("Max warn User List 6>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxWarnUserList6 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kill User List 6>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKillUserList6 = iTimeSeconds;
            else if (strVariable.CompareTo("Max kick User List 6>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxKickUserList6 = iTimeSeconds;
            else if (strVariable.CompareTo("Max TBan User List 6>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxTBanUserList6 = iTimeSeconds;
            else if (strVariable.CompareTo("Max Ban User List 6>") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.maxBanUserList6 = iTimeSeconds;



            else if (strVariable.CompareTo("Day before reset >") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.m_DayBeforeReset = iTimeSeconds;
            else if (strVariable.CompareTo("Round before reset >") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.m_RoundBeforeReset = iTimeSeconds;
            else if (strVariable.CompareTo("Time Ban in second >") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.m_iTimeOfTBan = iTimeSeconds;
            else if (strVariable.CompareTo("Time Yell message in second >") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.m_iTimeOfMsgYell = iTimeSeconds;
            else if (strVariable.CompareTo("Delay between message and kick >") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
                this.m_iDelayBetweenMessageAndKick = iTimeSeconds;
            else if (strVariable.CompareTo("Ban by guid") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enBanByGuid = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            else if (strVariable.CompareTo("Warn before next action") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enSendMsgBeforeLastAction = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            else if (strVariable.CompareTo("Use Action List By Word") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
                this.m_enUseActionListByWord = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);

        }

        // Player events
        public void OnPlayerJoin(string strSoldierName)
        {
            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName) == false)
                this.m_dicPlayerInfo.Add(strSoldierName, new CPlayerInfo(strSoldierName, "", 0, 24));
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {
            if (cpbiPlayer != null)
            {
                if (this.m_dicPbInfo.ContainsKey(cpbiPlayer.SoldierName) == false)
                    this.m_dicPbInfo.Add(cpbiPlayer.SoldierName, cpbiPlayer);
                else
                    this.m_dicPbInfo[cpbiPlayer.SoldierName] = cpbiPlayer;
            }
        }

        public void OnPlayerLeft(string strSoldierName)
        {
            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName))
                this.m_dicPlayerInfo.Remove(strSoldierName);

            if (this.m_dicPbInfo.ContainsKey(strSoldierName))
                this.m_dicPbInfo.Remove(strSoldierName);

            //supprime les données si le joueur n'a pas fait de mauvaise action dans le temps
            foreach (KeyValuePair<string, string> lastDate in m_dicLastDateOffencedPlayer)
            {
                DateTime MyLastDateTime = new DateTime();
                MyLastDateTime = DateTime.ParseExact(lastDate.Value, "yyyy-MM-dd HH:mm tt", null);
                DateTime dateToday = DateTime.Now;
                TimeSpan dif = dateToday - MyLastDateTime;
                if ((int)dif.TotalDays > m_DayBeforeReset && m_DayBeforeReset != 0)
                {
                    if (this.m_dicLastDateOffencedPlayer.ContainsKey(lastDate.Key))
                        this.m_dicLastDateOffencedPlayer.Remove(lastDate.Key);
                    if (this.m_dicNbRoundWithoutOffencedPlayer.ContainsKey(strSoldierName))
                        this.m_dicNbRoundWithoutOffencedPlayer.Remove(strSoldierName);
                    if (this.m_dicNumbreWarn.ContainsKey(strSoldierName))
                        this.m_dicNumbreWarn.Remove(strSoldierName);
                    if (this.m_dicNumbreKill.ContainsKey(strSoldierName))
                        this.m_dicNumbreKill.Remove(strSoldierName);
                    if (this.m_dicNumbreKick.ContainsKey(strSoldierName))
                        this.m_dicNumbreKick.Remove(strSoldierName);
                    if (this.m_dicNumbreTBan.ContainsKey(strSoldierName))
                        this.m_dicNumbreTBan.Remove(strSoldierName);
                    if (this.m_dicNumbreWarnBySpeaker.ContainsKey(strSoldierName))
                        this.m_dicNumbreWarnBySpeaker.Remove(strSoldierName);
                    if (this.m_dicNumbreKillBySpeaker.ContainsKey(strSoldierName))
                        this.m_dicNumbreKillBySpeaker.Remove(strSoldierName);
                    if (this.m_dicNumbreKickBySpeaker.ContainsKey(strSoldierName))
                        this.m_dicNumbreKickBySpeaker.Remove(strSoldierName);
                    if (this.m_dicNumbreTBanBySpeaker.ContainsKey(strSoldierName))
                        this.m_dicNumbreTBanBySpeaker.Remove(strSoldierName);


                }
            }
        }

        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            if ((strSpeaker).Equals("Server") == false)// && this.m_enWatchdogChatGlobal == enumBoolYesNo.Yes) 
            {
                WatchdogGuard(strSpeaker, strMessage);
                //this.ExecuteCommand("procon.protected.pluginconsole.write", "onglobchat=on\r\n");
            }
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            if ((strSpeaker).Equals("Server") == false)// && this.m_enWatchdogChatTeam == enumBoolYesNo.Yes) 
            {
                WatchdogGuard(strSpeaker, strMessage);
                //this.ExecuteCommand("procon.protected.pluginconsole.write", "onteamchat=on\r\n");
            }
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            if ((strSpeaker).Equals("Server") == false)// && this.m_enWatchdogChatSquad == enumBoolYesNo.Yes) 
            {
                WatchdogGuard(strSpeaker, strMessage);
                //this.ExecuteCommand("procon.protected.pluginconsole.write", "onsquadchat=on\r\n");
            }
        }

        public void OnRoundOver(int winningTeamId)
        {
            string strSoldierName;
            foreach (KeyValuePair<string, int> AllOffencedPlayer in m_dicNbRoundWithoutOffencedPlayer)
            {
                strSoldierName = AllOffencedPlayer.Key;
                this.m_dicNbRoundWithoutOffencedPlayer[strSoldierName] += 1;

                if (this.m_dicNbRoundWithoutOffencedPlayer[strSoldierName] >= m_RoundBeforeReset && m_RoundBeforeReset != 0) // reset
                {
                    if (this.m_dicLastDateOffencedPlayer.ContainsKey(strSoldierName))
                        this.m_dicLastDateOffencedPlayer.Remove(strSoldierName);
                    if (this.m_dicNbRoundWithoutOffencedPlayer.ContainsKey(strSoldierName))
                        this.m_dicNbRoundWithoutOffencedPlayer.Remove(strSoldierName);
                    if (this.m_dicNumbreWarn.ContainsKey(strSoldierName))
                        this.m_dicNumbreWarn.Remove(strSoldierName);
                    if (this.m_dicNumbreKill.ContainsKey(strSoldierName))
                        this.m_dicNumbreKill.Remove(strSoldierName);
                    if (this.m_dicNumbreKick.ContainsKey(strSoldierName))
                        this.m_dicNumbreKick.Remove(strSoldierName);
                    if (this.m_dicNumbreTBan.ContainsKey(strSoldierName))
                        this.m_dicNumbreTBan.Remove(strSoldierName);
                    if (this.m_dicNumbreWarnBySpeaker.ContainsKey(strSoldierName))
                        this.m_dicNumbreWarnBySpeaker.Remove(strSoldierName);
                    if (this.m_dicNumbreKillBySpeaker.ContainsKey(strSoldierName))
                        this.m_dicNumbreKillBySpeaker.Remove(strSoldierName);
                    if (this.m_dicNumbreKickBySpeaker.ContainsKey(strSoldierName))
                        this.m_dicNumbreKickBySpeaker.Remove(strSoldierName);
                    if (this.m_dicNumbreTBanBySpeaker.ContainsKey(strSoldierName))
                        this.m_dicNumbreTBanBySpeaker.Remove(strSoldierName);
                }
            }
        }

        //Main routine
        public void WatchdogGuard(string strSpeaker, string strMessage)
        {
            bool whiteList = false;
            if (this.m_enProtectedPlayer == enumBoolYesNo.Yes)
            {
                foreach (string name in lstProtectedPlayers)
                {
                    if (strSpeaker.ToLower().Equals(name.ToLower()) && !string.IsNullOrEmpty(name))
                    {
                        whiteList = true;
                        break;
                    }
                }
            }
            if (!whiteList)
            {
                DateTime dateTime = DateTime.Now;
                bool blRetContBW = false;
                int WordNumber = 0, valReturn;

                List<string> lstOfWords;

                for (int userList = 1; userList < 7; userList++)
                {
                    if (userList == 1)
                        lstOfWords = new List<string>(lstUserList1);
                    else if (userList == 2)
                        lstOfWords = new List<string>(lstUserList2);
                    else if (userList == 3)
                        lstOfWords = new List<string>(lstUserList3);
                    else if (userList == 4)
                        lstOfWords = new List<string>(lstUserList4);
                    else if (userList == 5)
                        lstOfWords = new List<string>(lstUserList5);
                    else
                        lstOfWords = new List<string>(lstUserList6);

                    WordNumber = 0;
                    foreach (string words in lstOfWords)
                    {
                        blRetContBW = ContainsBadWord(words, strMessage);
                        if (!words.StartsWith(";"))
                            WordNumber++;
                        //this.ExecuteCommand("procon.protected.pluginconsole.write", "kill lstUserList3=\r\n" + words);
                        if (blRetContBW && !string.IsNullOrEmpty(words))
                        {
                            //this.ExecuteCommand("procon.protected.pluginconsole.write", "kill strMessage.Contains(words)\r\n");

                            // Ajout de l'action sur le joueur
                            valReturn = TypeMethode(strSpeaker, words, userList);

                            switch (valReturn)
                            {
                                case 1: //Warn
                                    SetIncPlayerParam(strSpeaker, words, (int)MethTtype.Warn, 1);
                                    WarnMethode(strSpeaker, WordNumber, userList);
                                    break;
                                case 2: //kill
                                    SetIncPlayerParam(strSpeaker, words, (int)MethTtype.Kill, 1);
                                    KillMethode(strSpeaker, WordNumber, userList);
                                    break;
                                case 3: //kick
                                    SetIncPlayerParam(strSpeaker, words, (int)MethTtype.Kick, 1);
                                    KickMethode(strSpeaker, WordNumber, userList);
                                    // Reset les kill si la méthode est non lineaire 							
                                    if (this.m_enLinearRslt == enumBoolYesNo.No)
                                    {
                                        if (this.m_dicNumbreWarn.ContainsKey(strSpeaker))
                                            this.m_dicNumbreWarn.Remove(strSpeaker);
                                        if (this.m_dicNumbreWarnBySpeaker.ContainsKey(strSpeaker))
                                            this.m_dicNumbreWarnBySpeaker.Remove(strSpeaker);
                                        if (this.m_dicNumbreKill.ContainsKey(strSpeaker))
                                            this.m_dicNumbreKill.Remove(strSpeaker);
                                        if (this.m_dicNumbreKillBySpeaker.ContainsKey(strSpeaker))
                                            this.m_dicNumbreKillBySpeaker.Remove(strSpeaker);
                                    }
                                    break;
                                case 4: //TBan
                                    SetIncPlayerParam(strSpeaker, words, (int)MethTtype.TBan, 1);
                                    TBanMethode(strSpeaker, WordNumber, userList);
                                    // Reset les kill et kick si la méthode est non lineaire 							
                                    if (this.m_enLinearRslt == enumBoolYesNo.No)
                                    {
                                        if (this.m_dicNumbreWarn.ContainsKey(strSpeaker))
                                            this.m_dicNumbreWarn.Remove(strSpeaker);
                                        if (this.m_dicNumbreKill.ContainsKey(strSpeaker))
                                            this.m_dicNumbreKill.Remove(strSpeaker);
                                        if (this.m_dicNumbreKick.ContainsKey(strSpeaker))
                                            this.m_dicNumbreKick.Remove(strSpeaker);
                                        if (this.m_dicNumbreWarnBySpeaker.ContainsKey(strSpeaker))
                                            this.m_dicNumbreWarnBySpeaker.Remove(strSpeaker);
                                        if (this.m_dicNumbreKillBySpeaker.ContainsKey(strSpeaker))
                                            this.m_dicNumbreKillBySpeaker.Remove(strSpeaker);
                                        if (this.m_dicNumbreKickBySpeaker.ContainsKey(strSpeaker))
                                            this.m_dicNumbreKickBySpeaker.Remove(strSpeaker);
                                    }
                                    break;
                                case 5: //Ban
                                    BanMethode(strSpeaker, WordNumber, userList);
                                    break;
                            }
                            // Ajout de la date du dernier evenement
                            if (m_DayBeforeReset > 0)
                            {
                                if (this.m_dicLastDateOffencedPlayer.ContainsKey(strSpeaker))
                                    this.m_dicLastDateOffencedPlayer[strSpeaker] = dateTime.ToString("d");
                                else

                                    this.m_dicLastDateOffencedPlayer.Add(strSpeaker, dateTime.ToString("d"));
                            }

                            if (m_RoundBeforeReset > 0)
                            {
                                if (this.m_dicNbRoundWithoutOffencedPlayer.ContainsKey(strSpeaker))
                                    this.m_dicNbRoundWithoutOffencedPlayer[strSpeaker] = 0;
                                else
                                    this.m_dicNbRoundWithoutOffencedPlayer.Add(strSpeaker, 0);
                            }

                            // Message dernier avertissement avant action sup.
                            if (this.m_enSendMsgBeforeLastAction == enumBoolYesNo.Yes)
                                MsgBeforeNextAction(strSpeaker, words, userList);
                            break;
                        }
                    }
                }
            }
        }

        public int TypeMethode(string strSpeaker, string words, int userList)
        {
            int nbWarn = 0, nbKill = 0, nbKick = 0, nbTBan = 0, maxWarn, maxKill, maxKick, maxTBan, maxBan;

            if (this.m_enUseActionListByWord == enumBoolYesNo.Yes)
            {
                nbWarn = GetNbAction(strSpeaker, words, (int)MethTtype.Warn);
                nbKill = GetNbAction(strSpeaker, words, (int)MethTtype.Kill);
                nbKick = GetNbAction(strSpeaker, words, (int)MethTtype.Kick);
                nbTBan = GetNbAction(strSpeaker, words, (int)MethTtype.TBan);
            }
            else
            {
                if (this.m_dicNumbreWarnBySpeaker.TryGetValue(strSpeaker, out nbWarn)) { }
                if (this.m_dicNumbreKillBySpeaker.TryGetValue(strSpeaker, out nbKill)) { }
                if (this.m_dicNumbreKickBySpeaker.TryGetValue(strSpeaker, out nbKick)) { }
                if (this.m_dicNumbreTBanBySpeaker.TryGetValue(strSpeaker, out nbTBan)) { }
            }

            if (userList == 1)
            {
                maxWarn = maxWarnUserList1;
                maxKill = maxKillUserList1;
                maxKick = maxKickUserList1;
                maxTBan = maxTBanUserList1;
                maxBan = maxBanUserList1;
            }
            else if (userList == 2)
            {
                maxWarn = maxWarnUserList2;
                maxKill = maxKillUserList2;
                maxKick = maxKickUserList2;
                maxTBan = maxTBanUserList2;
                maxBan = maxBanUserList2;
            }
            else if (userList == 3)
            {
                maxWarn = maxWarnUserList3;
                maxKill = maxKillUserList3;
                maxKick = maxKickUserList3;
                maxTBan = maxTBanUserList3;
                maxBan = maxBanUserList3;
            }
            else if (userList == 4)
            {
                maxWarn = maxWarnUserList4;
                maxKill = maxKillUserList4;
                maxKick = maxKickUserList4;
                maxTBan = maxTBanUserList4;
                maxBan = maxBanUserList4;
            }
            else if (userList == 5)
            {
                maxWarn = maxWarnUserList5;
                maxKill = maxKillUserList5;
                maxKick = maxKickUserList5;
                maxTBan = maxTBanUserList5;
                maxBan = maxBanUserList5;
            }
            else
            {
                maxWarn = maxWarnUserList6;
                maxKill = maxKillUserList6;
                maxKick = maxKickUserList6;
                maxTBan = maxTBanUserList6;
                maxBan = maxBanUserList6;
            }

            int ValCmd = 1;
            if (nbWarn >= maxWarn) { ValCmd = 2; }
            if (nbWarn >= maxWarn && nbKill >= maxKill) { ValCmd = 3; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && nbKick >= maxKick) { ValCmd = 4; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && nbKick >= maxKick && nbTBan >= maxTBan) { ValCmd = 5; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && nbKick >= maxKick && nbTBan >= maxTBan && maxBan == 0) { ValCmd = 4; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && nbKick >= maxKick && maxTBan == 0) { ValCmd = 5; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && nbKick >= maxKick && maxTBan == 0) { ValCmd = 5; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && nbKick >= maxKick && maxTBan == 0 && maxBan == 0) { ValCmd = 3; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && maxKick == 0) { ValCmd = 4; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && maxKick == 0) { ValCmd = 4; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && maxKick == 0 && nbTBan >= maxTBan) { ValCmd = 5; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && maxKick == 0 && nbTBan >= maxTBan && maxBan == 0) { ValCmd = 4; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (nbWarn >= maxWarn && nbKill >= maxKill && maxKick == 0 && maxTBan == 0 && maxBan == 0) { ValCmd = 2; }
            if (nbWarn >= maxWarn && maxKill == 0) { ValCmd = 3; }
            if (nbWarn >= maxWarn && maxKill == 0 && nbKick >= maxKick) { ValCmd = 4; }
            if (nbWarn >= maxWarn && maxKill == 0 && nbKick >= maxKick && nbTBan >= maxTBan) { ValCmd = 5; }
            if (nbWarn >= maxWarn && maxKill == 0 && nbKick >= maxKick && nbTBan >= maxTBan && maxBan == 0) { ValCmd = 4; }
            if (nbWarn >= maxWarn && maxKill == 0 && nbKick >= maxKick && maxTBan == 0) { ValCmd = 5; }
            if (nbWarn >= maxWarn && maxKill == 0 && nbKick >= maxKick && maxTBan == 0) { ValCmd = 5; }
            if (nbWarn >= maxWarn && maxKill == 0 && nbKick >= maxKick && maxTBan == 0 && maxBan == 0) { ValCmd = 3; }
            if (nbWarn >= maxWarn && maxKill == 0 && maxKick == 0) { ValCmd = 4; }
            if (nbWarn >= maxWarn && maxKill == 0 && maxKick == 0 && nbTBan >= maxTBan) { ValCmd = 5; }
            if (nbWarn >= maxWarn && maxKill == 0 && maxKick == 0 && nbTBan >= maxTBan && maxBan == 0) { ValCmd = 4; }
            if (nbWarn >= maxWarn && maxKill == 0 && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (nbWarn >= maxWarn && maxKill == 0 && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (nbWarn >= maxWarn && maxKill == 0 && maxKick == 0 && maxTBan == 0 && maxBan == 0) { ValCmd = 1; }
            if (maxWarn == 0) { ValCmd = 2; }
            if (maxWarn == 0 && nbKill >= maxKill) { ValCmd = 3; }
            if (maxWarn == 0 && nbKill >= maxKill && nbKick >= maxKick) { ValCmd = 4; }
            if (maxWarn == 0 && nbKill >= maxKill && nbKick >= maxKick && nbTBan >= maxTBan) { ValCmd = 5; }
            if (maxWarn == 0 && nbKill >= maxKill && nbKick >= maxKick && nbTBan >= maxTBan && maxBan == 0) { ValCmd = 4; }
            if (maxWarn == 0 && nbKill >= maxKill && nbKick >= maxKick && maxTBan == 0) { ValCmd = 5; }
            if (maxWarn == 0 && nbKill >= maxKill && nbKick >= maxKick && maxTBan == 0) { ValCmd = 5; }
            if (maxWarn == 0 && nbKill >= maxKill && nbKick >= maxKick && maxTBan == 0 && maxBan == 0) { ValCmd = 3; }
            if (maxWarn == 0 && nbKill >= maxKill && maxKick == 0) { ValCmd = 4; }
            if (maxWarn == 0 && nbKill >= maxKill && maxKick == 0 && nbTBan >= maxTBan) { ValCmd = 5; }
            if (maxWarn == 0 && nbKill >= maxKill && maxKick == 0 && nbTBan >= maxTBan && maxBan == 0) { ValCmd = 4; }
            if (maxWarn == 0 && nbKill >= maxKill && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (maxWarn == 0 && nbKill >= maxKill && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (maxWarn == 0 && nbKill >= maxKill && maxKick == 0 && maxTBan == 0 && maxBan == 0) { ValCmd = 2; }
            if (maxWarn == 0 && maxKill == 0) { ValCmd = 3; }
            if (maxWarn == 0 && maxKill == 0 && nbKick >= maxKick) { ValCmd = 4; }
            if (maxWarn == 0 && maxKill == 0 && nbKick >= maxKick && nbTBan >= maxTBan) { ValCmd = 5; }
            if (maxWarn == 0 && maxKill == 0 && nbKick >= maxKick && nbTBan >= maxTBan && maxBan == 0) { ValCmd = 4; }
            if (maxWarn == 0 && maxKill == 0 && nbKick >= maxKick && maxTBan == 0) { ValCmd = 5; }
            if (maxWarn == 0 && maxKill == 0 && nbKick >= maxKick && maxTBan == 0) { ValCmd = 5; }
            if (maxWarn == 0 && maxKill == 0 && nbKick >= maxKick && maxTBan == 0 && maxBan == 0) { ValCmd = 3; }
            if (maxWarn == 0 && maxKill == 0 && maxKick == 0) { ValCmd = 4; }
            if (maxWarn == 0 && maxKill == 0 && maxKick == 0 && nbTBan >= maxTBan) { ValCmd = 5; }
            if (maxWarn == 0 && maxKill == 0 && maxKick == 0 && nbTBan >= maxTBan && maxBan == 0) { ValCmd = 4; }
            if (maxWarn == 0 && maxKill == 0 && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (maxWarn == 0 && maxKill == 0 && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (maxWarn == 0 && maxKill == 0 && maxKick == 0 && maxTBan == 0 && maxBan == 0) { ValCmd = 6; }

            return ValCmd;
        }

        // 0=rien, 1=warn, 2=Kill, 3=Kick, 4=TBan, 5=Ban
        // Retourne le nombre d'action associé au mot banni pour chaque type.
        public int GetNbAction(string strSpeaker, string words, int type)
        {
            int ValueReturn = 0, nbAction;
            Dictionary<string, int> TmpDicoNbX = new Dictionary<string, int>();

            switch (type)
            {
                case 1: //warn					
                    if (m_dicNumbreWarn.TryGetValue(strSpeaker, out TmpDicoNbX))
                    {
                        if (TmpDicoNbX.TryGetValue(words, out nbAction))
                        {
                            ValueReturn = nbAction;
                        }
                        else
                        {
                            ValueReturn = 0;
                        }
                    }
                    else
                    {
                        ValueReturn = 0;
                    }
                    break;
                case 2: //kill					
                    if (this.m_dicNumbreKill.TryGetValue(strSpeaker, out TmpDicoNbX))
                    {
                        if (TmpDicoNbX.TryGetValue(words, out nbAction)) { ValueReturn = nbAction; }
                        else
                        {
                            ValueReturn = 0;
                        }
                    }
                    else
                    {
                        ValueReturn = 0;
                    }
                    break;
                case 3: //kick
                    if (this.m_dicNumbreKick.TryGetValue(strSpeaker, out TmpDicoNbX))
                    {
                        if (TmpDicoNbX.TryGetValue(words, out nbAction)) { ValueReturn = nbAction; }
                        else
                        {
                            ValueReturn = 0;
                        }
                    }
                    else
                    {
                        ValueReturn = 0;
                    }
                    break;
                case 4: //TBan
                    if (this.m_dicNumbreTBan.TryGetValue(strSpeaker, out TmpDicoNbX))
                    {
                        if (TmpDicoNbX.TryGetValue(words, out nbAction)) { ValueReturn = nbAction; }
                        else
                        {
                            ValueReturn = 0;
                        }
                    }
                    else
                    {
                        ValueReturn = 0;
                    }
                    break;
            }
            return ValueReturn;
        }
        // 0=rien, 1=warn, 2=Kill, 3=Kick, 4=TBan, 5=Ban
        public void MsgBeforeNextAction(string strSpeaker, string words, int userList)
        {
            int nbWarn = 0, nbKill = 0, nbKick = 0, nbTBan = 0, maxWarn, maxKill, maxKick, maxTBan, maxBan, ValCmd = 0;

            if (this.m_enUseActionListByWord == enumBoolYesNo.Yes)
            {
                nbWarn = GetNbAction(strSpeaker, words, (int)MethTtype.Warn);
                nbKill = GetNbAction(strSpeaker, words, (int)MethTtype.Kill);
                nbKick = GetNbAction(strSpeaker, words, (int)MethTtype.Kick);
                nbTBan = GetNbAction(strSpeaker, words, (int)MethTtype.TBan);
            }
            else
            {
                if (this.m_dicNumbreWarnBySpeaker.TryGetValue(strSpeaker, out nbWarn)) { }
                if (this.m_dicNumbreKillBySpeaker.TryGetValue(strSpeaker, out nbKill)) { }
                if (this.m_dicNumbreKickBySpeaker.TryGetValue(strSpeaker, out nbKick)) { }
                if (this.m_dicNumbreTBanBySpeaker.TryGetValue(strSpeaker, out nbTBan)) { }
            }

            if (userList == 1)
            {
                maxWarn = maxWarnUserList1;
                maxKill = maxKillUserList1;
                maxKick = maxKickUserList1;
                maxTBan = maxTBanUserList1;
                maxBan = maxBanUserList1;
            }
            else if (userList == 2)
            {
                maxWarn = maxWarnUserList2;
                maxKill = maxKillUserList2;
                maxKick = maxKickUserList2;
                maxTBan = maxTBanUserList2;
                maxBan = maxBanUserList2;
            }
            else if (userList == 3)
            {
                maxWarn = maxWarnUserList3;
                maxKill = maxKillUserList3;
                maxKick = maxKickUserList3;
                maxTBan = maxTBanUserList3;
                maxBan = maxBanUserList3;
            }
            else if (userList == 4)
            {
                maxWarn = maxWarnUserList4;
                maxKill = maxKillUserList4;
                maxKick = maxKickUserList4;
                maxTBan = maxTBanUserList4;
                maxBan = maxBanUserList4;
            }
            else if (userList == 5)
            {
                maxWarn = maxWarnUserList5;
                maxKill = maxKillUserList5;
                maxKick = maxKickUserList5;
                maxTBan = maxTBanUserList5;
                maxBan = maxBanUserList5;
            }
            else
            {
                maxWarn = maxWarnUserList6;
                maxKill = maxKillUserList6;
                maxKick = maxKickUserList6;
                maxTBan = maxTBanUserList6;
                maxBan = maxBanUserList6;
            }
            // warn 2, kill 0, kick 1, tban 1

            if (nbWarn == maxWarn && maxWarn != 0) { ValCmd = 2; }
            if (nbWarn == maxWarn && maxWarn != 0 && maxKill == 0) { ValCmd = 3; }
            if (nbWarn == maxWarn && maxWarn != 0 && maxKill == 0 && nbKick == maxKick) { ValCmd = 4; }
            if (nbWarn == maxWarn && maxWarn != 0 && maxKill == 0 && nbTBan == maxTBan) { ValCmd = 5; }
            if (nbWarn == maxWarn && maxWarn != 0 && maxKill == 0 && maxKick == 0) { ValCmd = 4; }
            if (nbWarn == maxWarn && maxWarn != 0 && maxKill == 0 && maxKick == 0 && nbTBan == maxTBan) { ValCmd = 5; }
            if (nbWarn == maxWarn && maxWarn != 0 && maxKill == 0 && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (nbWarn == maxWarn && maxWarn != 0 && maxKill == 0 && maxKick == 0 && maxTBan == 0 && maxBan == 0) { ValCmd = 0; }
            if (nbKill == maxKill && maxKill != 0) { ValCmd = 3; }
            if (nbKill == maxKill && maxKill != 0 && maxKick == 0) { ValCmd = 4; }
            if (nbKill == maxKill && maxKill != 0 && maxKick == 0 && nbTBan == maxTBan) { ValCmd = 5; }
            if (nbKill == maxKill && maxKill != 0 && maxKick == 0 && maxTBan == 0) { ValCmd = 5; }
            if (nbKill == maxKill && maxKill != 0 && maxKick == 0 && maxTBan == 0 && maxBan == 0) { ValCmd = 0; }
            if (nbKick == maxKick && maxKick != 0) { ValCmd = 4; }
            if (nbKick == maxKick && maxKick != 0 && maxTBan == 0) { ValCmd = 5; }
            if (nbKick == maxKick && maxKick != 0 && maxTBan == 0 && maxBan == 0) { ValCmd = 0; }
            if (nbTBan == maxTBan && maxTBan != 0) { ValCmd = 5; }
            if (nbTBan == maxTBan && maxTBan != 0 && maxBan == 0) { ValCmd = 0; }

            switch (ValCmd)
            {
                case 2: //kill
                    if (this.m_enYellWarnBefNextAct == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.send", "admin.yell", "Warning: " + strSpeaker + " Last warning before Kill, watch your language", this.m_iTimeOfMsgYell.ToString(), "all");
                    else
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Warning: " + strSpeaker + " Last warning before Kill, watch your language", "all");
                    SetIncPlayerParam(strSpeaker, words, (int)MethTtype.Warn, 1);  // pour empecher que le message ne se répète
                    break;
                case 3: //kick
                    if (this.m_enYellWarnBefNextAct == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.send", "admin.yell", "Warning: " + strSpeaker + " Last warning before Kick, watch your language", this.m_iTimeOfMsgYell.ToString(), "all");
                    else
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Warning: " + strSpeaker + " Last warning before Kick, watch your language", "all");
                    SetIncPlayerParam(strSpeaker, words, (int)MethTtype.Kill, 1);   // pour empecher que le message ne se répète
                    break;
                case 4: //TBan
                    if (this.m_enYellWarnBefNextAct == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.send", "admin.yell", "Warning: " + strSpeaker + " Last warning before TBan, watch your language", this.m_iTimeOfMsgYell.ToString(), "all");
                    else
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Warning: " + strSpeaker + " Last warning before TBan, watch your language", "all");
                    SetIncPlayerParam(strSpeaker, words, (int)MethTtype.Kick, 1);   // pour empecher que le message ne se répète
                    break;
                case 5: //Ban
                    if (this.m_enYellWarnBefNextAct == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.send", "admin.yell", "Warning: " + strSpeaker + " Last warning before Ban, watch your language", this.m_iTimeOfMsgYell.ToString(), "all");
                    else
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Warning: " + strSpeaker + " Last warning before Ban, watch your language", "all");
                    SetIncPlayerParam(strSpeaker, words, (int)MethTtype.TBan, 1);   // pour empecher que le message ne se répète
                    break;
            }
        }

        // 1=warn, 2=Kill, 3=Kick, 4=TBan, 5=Ban
        public void SetIncPlayerParam(string strSpeaker, string words, int NumParam, int value)
        {
            int nbWarn, nbKill, nbKick, nbTBan, nbBan;
            Dictionary<string, int> TmpDicoNbXPlayer = new Dictionary<string, int>();
            Dictionary<string, int> TmpDicoNbX;

            switch (NumParam)
            {
                case 1: //Warn	
                    if (this.m_enUseActionListByWord == enumBoolYesNo.Yes)
                    {
                        if (this.m_dicNumbreWarn.TryGetValue(strSpeaker, out TmpDicoNbXPlayer))
                        {
                            if (TmpDicoNbXPlayer.TryGetValue(words, out nbWarn)) // mot deja present dans la liste du joueur	
                            {
                                this.m_dicNumbreWarn[strSpeaker][words] += 1;//this.m_dicNumbreWarn[strSpeaker] += value;
                            }
                            else // mot non présent das la liste du joueur
                            {
                                TmpDicoNbX = new Dictionary<string, int>(this.m_dicNumbreWarn[strSpeaker]);
                                TmpDicoNbX.Add(words, 1);
                                this.m_dicNumbreWarn[strSpeaker] = TmpDicoNbX;
                            }
                        }
                        else // joueur non enregistré
                        {
                            TmpDicoNbX = new Dictionary<string, int>();
                            TmpDicoNbX.Add(words, 1);
                            this.m_dicNumbreWarn.Add(strSpeaker, TmpDicoNbX);
                        }
                    }
                    else
                    {
                        if (this.m_dicNumbreWarnBySpeaker.TryGetValue(strSpeaker, out nbWarn))
                            this.m_dicNumbreWarnBySpeaker[strSpeaker] += value;
                        else
                            this.m_dicNumbreWarnBySpeaker.Add(strSpeaker, value);
                    }
                    break;

                case 2: //kill
                    if (this.m_enUseActionListByWord == enumBoolYesNo.Yes)
                    {
                        if (this.m_dicNumbreKill.TryGetValue(strSpeaker, out TmpDicoNbXPlayer))
                        {
                            if (TmpDicoNbXPlayer.TryGetValue(words, out nbWarn)) // mot deja present dans la liste du joueur	
                            {
                                this.m_dicNumbreKill[strSpeaker][words] += 1;//this.m_dicNumbreWarn[strSpeaker] += value;
                            }
                            else // mot non présent das la liste du joueur
                            {
                                TmpDicoNbX = new Dictionary<string, int>(this.m_dicNumbreKill[strSpeaker]);
                                TmpDicoNbX.Add(words, 1);
                                this.m_dicNumbreKill[strSpeaker] = TmpDicoNbX;
                            }
                        }
                        else // joueur non enregistré
                        {
                            TmpDicoNbX = new Dictionary<string, int>();
                            TmpDicoNbX.Add(words, 1);
                            this.m_dicNumbreKill.Add(strSpeaker, TmpDicoNbX);
                        }
                    }
                    else
                    {
                        if (this.m_dicNumbreKillBySpeaker.TryGetValue(strSpeaker, out nbWarn))
                            this.m_dicNumbreKillBySpeaker[strSpeaker] += value;
                        else
                            this.m_dicNumbreKillBySpeaker.Add(strSpeaker, value);
                    }
                    break;

                case 3: //Kick
                    if (this.m_enUseActionListByWord == enumBoolYesNo.Yes)
                    {
                        if (this.m_dicNumbreKick.TryGetValue(strSpeaker, out TmpDicoNbXPlayer))
                        {
                            if (TmpDicoNbXPlayer.TryGetValue(words, out nbWarn)) // mot deja present dans la liste du joueur	
                            {
                                this.m_dicNumbreKick[strSpeaker][words] += 1;//this.m_dicNumbreWarn[strSpeaker] += value;
                            }
                            else // mot non présent das la liste du joueur
                            {
                                TmpDicoNbX = new Dictionary<string, int>(this.m_dicNumbreKick[strSpeaker]);
                                TmpDicoNbX.Add(words, 1);
                                this.m_dicNumbreKick[strSpeaker] = TmpDicoNbX;
                            }
                        }
                        else // joueur non enregistré
                        {
                            TmpDicoNbX = new Dictionary<string, int>();
                            TmpDicoNbX.Add(words, 1);
                            this.m_dicNumbreKick.Add(strSpeaker, TmpDicoNbX);
                        }
                    }
                    else
                    {
                        if (this.m_dicNumbreKickBySpeaker.TryGetValue(strSpeaker, out nbWarn))
                            this.m_dicNumbreKickBySpeaker[strSpeaker] += value;
                        else
                            this.m_dicNumbreKickBySpeaker.Add(strSpeaker, value);
                    }
                    break;

                case 4: //TBan
                    if (this.m_enUseActionListByWord == enumBoolYesNo.Yes)
                    {
                        if (this.m_dicNumbreTBan.TryGetValue(strSpeaker, out TmpDicoNbXPlayer))
                        {
                            if (TmpDicoNbXPlayer.TryGetValue(words, out nbWarn)) // mot deja present dans la liste du joueur	
                            {
                                this.m_dicNumbreTBan[strSpeaker][words] += 1;//this.m_dicNumbreWarn[strSpeaker] += value;
                            }
                            else // mot non présent das la liste du joueur
                            {
                                TmpDicoNbX = new Dictionary<string, int>(this.m_dicNumbreTBan[strSpeaker]);
                                TmpDicoNbX.Add(words, 1);
                                this.m_dicNumbreTBan[strSpeaker] = TmpDicoNbX;
                            }
                        }
                        else // joueur non enregistré
                        {
                            TmpDicoNbX = new Dictionary<string, int>();
                            TmpDicoNbX.Add(words, 1);
                            this.m_dicNumbreTBan.Add(strSpeaker, TmpDicoNbX);
                        }
                    }
                    else
                    {
                        if (this.m_dicNumbreTBanBySpeaker.TryGetValue(strSpeaker, out nbWarn))
                            this.m_dicNumbreTBanBySpeaker[strSpeaker] += value;
                        else
                            this.m_dicNumbreTBanBySpeaker.Add(strSpeaker, value);
                    }
                    break;
            }
        }

        public void ConsoleWrite(string message)
        {
            this.ExecuteCommand("procon.protected.tasks.add", "CWatchdogLanguage", "0", "1", "1", "procon.protected.pluginconsole.write", message);
        }

        public string GetNameUserList(int userList)
        {
            string myList = "rien";
            if (userList == 1)
                myList = this.strNameUserList1;
            else if (userList == 2)
                myList = this.strNameUserList2;
            else if (userList == 3)
                myList = this.strNameUserList3;
            else if (userList == 4)
                myList = this.strNameUserList4;
            else if (userList == 5)
                myList = this.strNameUserList5;
            else
                myList = this.strNameUserList6;

            return myList;
        }

        public bool ContainsBadWord(string words, string strMessage)
        {
            //this.ExecuteCommand("procon.protected.pluginconsole.write", "words="+words.ToString()+"\r\n");

            bool blCont = false, blRetGex = false;
            if (!words.StartsWith(";"))
            {
                if (words.StartsWith("R/") || words.StartsWith("r/"))
                {
                    words = words.Remove(0, 2); //suppression R/
                    Regex myReg = new Regex(words.ToString());
                    blRetGex = myReg.IsMatch(" " + strMessage.ToLower() + " ");
                }
                else
                {
                    blCont = strMessage.ToLower().Contains(words);
                }
            }
            if (blRetGex || blCont)
                return true;
            else
                return false;
        }

        //ConsoleWrite("^3^bWatchdog: ^0Val=0^1Val=1^2Val=2^3Val=3^4Val=4^5Val=5^6Val=6^7Val=7^8Val=8^9Val=9^nVal=n");
        public void WarnMethode(string strSpeaker, int words, int userList)
        {
            //this.ExecuteCommand("procon.protected.pluginconsole.write", "WarnMethode\r\n");
            string strPublicMessage = strPublicMessage = this.m_strPublicMessagePlayerWarn.Replace("%PN%", strSpeaker);
            string strPrivateMessage = strPrivateMessage = this.m_strPrivateMessagePlayerWarn.Replace("%PN%", strSpeaker);
            if (this.m_enSendWarnPrivately == enumBoolYesNo.Yes)
            {
                SendMsgPrivately(strPrivateMessage, strSpeaker);
            }
            else
            {
                SendMsgPublic(strPublicMessage);
            }
            ConsoleWrite("^5^bWatchdog: ^0Warning=^2" + strSpeaker + "^n^0 for Language, word number:^9 " + words.ToString() + "^n^0 in Userlist:^9 " + GetNameUserList(userList));
        }

        public void KillMethode(string strSpeaker, int words, int userList)
        {
            string strPublicMessage = strPublicMessage = this.m_strPublicMessagePlayerKill.Replace("%PN%", strSpeaker);
            string strPrivateMessage = strPrivateMessage = this.m_strPrivateMessagePlayerKill.Replace("%PN%", strSpeaker);
            if (this.m_enSendKicksPrivately == enumBoolYesNo.Yes)
            {
                SendMsgPrivately(strPrivateMessage, strSpeaker);
            }
            else
            {
                SendMsgPublic(strPublicMessage);
            }
            ConsoleWrite("^5^bWatchdog: ^9Killing=^2" + strSpeaker + "^n^0 for Language, word number: ^9" + words.ToString() + "^n^0 in Userlist:^9 " + GetNameUserList(userList));
            this.ExecuteCommand("procon.protected.send", "admin.killPlayer", strSpeaker);
        }

        public void KickMethode(string strSpeaker, int words, int userList)
        {
            string strPublicMessage = strPublicMessage = this.m_strPublicMessagePlayerKick.Replace("%PN%", strSpeaker);
            string strPrivateMessage = strPrivateMessage = this.m_strPrivateMessagePlayerKick.Replace("%PN%", strSpeaker);
            if (this.m_enSendKicksPrivately == enumBoolYesNo.Yes)
            {
                SendMsgPrivately(strPrivateMessage, strSpeaker);
            }
            else
            {
                SendMsgPublic(strPublicMessage);
            }
            ConsoleWrite("^5^bWatchdog: ^1Kicking=^2" + strSpeaker + "^n^0 for Language, word number: ^9" + words.ToString() + "^n^0 in Userlist:^9 " + GetNameUserList(userList));
            this.ExecuteCommand("procon.protected.tasks.add", "CWatchdogLanguage", this.m_iDelayBetweenMessageAndKick.ToString(), "1", "1", "procon.protected.send", "admin.kickPlayer", strSpeaker, strPrivateMessage);
        }

        public void TBanMethode(string strSpeaker, int words, int userList)
        {
            string strPublicMessage = strPublicMessage = this.m_strPublicMessagePlayerTBan.Replace("%PN%", strSpeaker).Replace("%Time%", m_iTimeOfTBan.ToString());
            string strPrivateMessage = strPrivateMessage = this.m_strPrivateMessagePlayerTBan.Replace("%PN%", strSpeaker).Replace("%Time%", m_iTimeOfTBan.ToString());
            if (this.m_enSendKicksPrivately == enumBoolYesNo.Yes)
            {
                SendMsgPrivately(strPrivateMessage, strSpeaker);
            }
            else
            {
                SendMsgPublic(strPublicMessage);
            }
            ConsoleWrite("^5^bWatchdog: ^7TBan=^2" + strSpeaker + "^n^0 for Language, word number: ^9" + words.ToString() + "^n^0 in Userlist:^9 " + GetNameUserList(userList));

            /*if (this.m_enBanByGuid == enumBoolYesNo.Yes)
            {
                int timeOfBan= m_iTimeOfTBan / 60;
                //this.ExecuteCommand("procon.protected.tasks.add", "CWatchdogLanguage", this.m_iDelayBetweenMessageAndKick.ToString(), "1", "1", "procon.protected.send", "banList.add", "guid", this.m_dicPbInfo[strSpeaker].GUID, "perm", strPrivateMessage);
                this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", String.Format("pb_sv_kick \"{0}\" {1} \"{2}\"", strSpeaker, timeOfBan.ToString(), strPrivateMessage));
            }
            else*/
            /*this.ExecuteCommand("procon.protected.tasks.add", "CWatchdogLanguage", this.m_iDelayBetweenMessageAndKick.ToString(), "1", "1", "procon.protected.send", "banList.add", "name", strSpeaker, "seconds", m_iTimeOfTBan.ToString(),"BC2! "+ strPrivateMessage);
            this.ExecuteCommand("procon.protected.tasks.add", "CWatchdogLanguage", "10", "1", "1", "procon.protected.send", "banList.list");
            this.ExecuteCommand("procon.protected.send", "banList.save");	
			*/
            this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "seconds", m_iTimeOfTBan.ToString(), "BC2! " + strPrivateMessage);
            this.ExecuteCommand("procon.protected.send", "banList.save");
            this.ExecuteCommand("procon.protected.send", "banList.list");

        }

        public void BanMethode(string strSpeaker, int words, int userList)
        {
            string strPublicMessage = strPublicMessage = this.m_strPublicMessagePlayerBan.Replace("%PN%", strSpeaker);
            string strPrivateMessage = strPrivateMessage = this.m_strPrivateMessagePlayerBan.Replace("%PN%", strSpeaker);
            if (this.m_enSendKicksPrivately == enumBoolYesNo.Yes)
            {
                SendMsgPrivately(strPrivateMessage, strSpeaker);
            }
            else
            {
                SendMsgPublic(strPublicMessage);
            }
            ConsoleWrite("^5^bWatchdog: ^8Ban=^2" + strSpeaker + "^n^0 for Language, word number: ^9" + words.ToString() + "^n^0 in Userlist:^9 " + GetNameUserList(userList));

            if (this.m_enBanByGuid == enumBoolYesNo.Yes && this.m_dicPbInfo.ContainsKey(strSpeaker))
            {
                ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", String.Format("pb_sv_banguid \"{0}\" \"{1}\" \"{2}\" \"{3}\"", this.m_dicPbInfo[strSpeaker].GUID, strSpeaker, this.m_dicPbInfo[strSpeaker].Ip, "BC2! " + strPrivateMessage));
                //this.ExecuteCommand("procon.protected.tasks.add", "CWatchdogLanguage", this.m_iDelayBetweenMessageAndKick.ToString(), "1", "1", "procon.protected.send", "banList.add", "guid", this.m_dicPbInfo[strSpeaker].GUID, "perm", strPrivateMessage);
            }
            else
                this.ExecuteCommand("procon.protected.tasks.add", "CWatchdogLanguage", this.m_iDelayBetweenMessageAndKick.ToString(), "1", "1", "procon.protected.send", "banList.add", "name", strSpeaker, "perm", "BC2! " + strPrivateMessage);

            this.ExecuteCommand("procon.protected.send", "banList.save");
            //this.ExecuteCommand("procon.protected.tasks.add", "CWatchdogLanguage", "10", "1", "1", "procon.protected.send", "banList.list");
            this.ExecuteCommand("procon.protected.send", "banList.list");


            //delete information
            if (this.m_dicLastDateOffencedPlayer.ContainsKey(strSpeaker))
                this.m_dicLastDateOffencedPlayer.Remove(strSpeaker);
            if (this.m_dicNbRoundWithoutOffencedPlayer.ContainsKey(strSpeaker))
                this.m_dicNbRoundWithoutOffencedPlayer.Remove(strSpeaker);
            if (this.m_dicNumbreWarn.ContainsKey(strSpeaker))
                this.m_dicNumbreWarn.Remove(strSpeaker);
            if (this.m_dicNumbreKill.ContainsKey(strSpeaker))
                this.m_dicNumbreKill.Remove(strSpeaker);
            if (this.m_dicNumbreKick.ContainsKey(strSpeaker))
                this.m_dicNumbreKick.Remove(strSpeaker);
            if (this.m_dicNumbreTBan.ContainsKey(strSpeaker))
                this.m_dicNumbreTBan.Remove(strSpeaker);
            if (this.m_dicNumbreWarnBySpeaker.ContainsKey(strSpeaker))
                this.m_dicNumbreWarnBySpeaker.Remove(strSpeaker);
            if (this.m_dicNumbreKillBySpeaker.ContainsKey(strSpeaker))
                this.m_dicNumbreKillBySpeaker.Remove(strSpeaker);
            if (this.m_dicNumbreKickBySpeaker.ContainsKey(strSpeaker))
                this.m_dicNumbreKickBySpeaker.Remove(strSpeaker);
            if (this.m_dicNumbreTBanBySpeaker.ContainsKey(strSpeaker))
                this.m_dicNumbreTBanBySpeaker.Remove(strSpeaker);
        }

        public void SendMsgPrivately(string strPrivateMessage, string strSpeaker)
        {
            if (this.m_enYellKicks == enumBoolYesNo.Yes)
            {//this.ExecuteCommand("procon.protected.tasks.add", "CWatchdogLanguage", "0", "1", "1", "procon.protected.send", "admin.yell", strPrivateMessage, this.m_iTimeOfMsgYell.ToString(), "player", strSpeaker);
                this.ExecuteCommand("procon.protected.send", "admin.yell", strPrivateMessage, this.m_iTimeOfMsgYell.ToString(), "player", strSpeaker);
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", strPrivateMessage, "player", strSpeaker);
            }
        }
        public void SendMsgPublic(string strPublicMessage)
        {
            if (this.m_enYellKicks == enumBoolYesNo.Yes)
            {
                this.ExecuteCommand("procon.protected.send", "admin.yell", strPublicMessage, this.m_iTimeOfMsgYell.ToString(), "all");
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", strPublicMessage, "all");
            }
        }
    }
}