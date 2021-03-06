using System;
using System.IO;
using System.Globalization;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Settings;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CIdleKicker : PRoConPluginAPI, IPRoConPluginInterface
    {
        //	public class CIdleKicker : CPRoConMarshalByRefObject, IPRoConPluginInterface4 {

        #region Plugin VARS & INIT
        private string S_strHostName;
        private string S_strPort;
        private string S_strPRoConVersion;

        //	White lists vars
        private List<string> L_ClanTagWL;   //	Clan tag white List
        private List<string> L_VIPWL;   //	VIP white List
        private List<string> L_PlayerWL;    //	Player white List
        private enumBoolYesNo E_UsesWL;
        private enumBoolYesNo E_PreserveCTWL;
        private enumBoolYesNo E_PreserveVIPWL;
        private enumBoolYesNo E_PreservePlayerWL;
        private enumBoolYesNo E_CTWLEn;
        private enumBoolYesNo E_VIPWLEn;
        private enumBoolYesNo E_PlayerWLEn;
        private enumBoolYesNo E_KeepCTWLoffD;
        private enumBoolYesNo E_KeepVIPWLoffD;
        private enumBoolYesNo E_KeepPlWLoffD;

        //	Internal var
        private Dictionary<string, PlayerStatus> D_PlayerStatus = new Dictionary<string, PlayerStatus>();   //	string : playername
        private bool B_TaskIsActive;
        private int I_NbPMaxOnServer;
        private bool B_DPSTaskIsActive;
        private string S_PlugVer;
        private bool B_CanBeDistribute;

        //	Conditionnal parameters
        private int I_TresholdTimeDisplay;  //	minutes
        private long Lo_TresholdTime;   //	0.1µs
        private int I_MinPlayerOnServer;
        //	Plugin settings
        private int I_CheckAFKEvery;
        private int MaxTimeCheck;
        private enumBoolYesNo E_CD_AFK;
        private enumBoolYesNo E_CD_KICK;
        private enumBoolYesNo E_CD_MOVES;
        private enumBoolYesNo E_ActionUnder;
        //	Global communication vars
        private enumBoolYesNo E_DPlayerSpam;
        private int I_DPlayerDelai;
        private int I_DPlayerStartSpam;
        private enumBoolYesNo E_DPlayerTMinus;
        private string S_DPlayerMSG;
        private string S_KickMSG;

        private class PlayerStatus
        {
            public PlayerStatus()
            {
                this.I_TimeOfDeath = 0;
                this.I_TimeOfSpawn = 0;
                this.I_TimePlayerEvent = RefreshedTime();   //	player join or new round
                this.B_PlayerIsDead = false;
                this.S_ClanTag = "";
                this.I_TeamID = 0;
                this.I_PlayerScore = 0;
                this.B_IsAFK = false;
            }
            public void PlayerEvent()
            {
                this.B_IsAFK = false;
                this.I_TimePlayerEvent = RefreshedTime();
            }
            public void PlayerKilled()
            {
                if (this.I_TimeOfSpawn == 0)
                    this.PlayerSpawned();
                else
                    this.PlayerEvent();
            }
            public void PlayerSpawned()
            {
                this.B_PlayerIsDead = false;
                this.PlayerEvent();
                this.I_TimeOfSpawn = this.I_TimePlayerEvent;
            }
            public void PlayerDied()
            {
                this.B_PlayerIsDead = true;
                this.PlayerEvent();
                this.I_TimeOfDeath = this.I_TimePlayerEvent;
            }
            public long I_TimeOfSpawn;
            public long I_TimeOfDeath;
            public long I_TimePlayerEvent; // spawn, chat, kill, change team, change squad
            public bool B_PlayerIsDead;
            public string S_ClanTag;
            public int I_TeamID;
            public int I_PlayerScore;
            public bool B_IsAFK;    //	when player reached end of time (essential for player distribution)
        }

        public CIdleKicker()
        {
            this.S_PlugVer = "1.4.3.12x";
            //	White lists
            this.L_ClanTagWL = new List<string>();
            this.L_VIPWL = new List<string>();
            this.L_PlayerWL = new List<string>();
            this.E_UsesWL = enumBoolYesNo.No;
            this.E_PreserveCTWL = enumBoolYesNo.No;
            this.E_PreserveVIPWL = enumBoolYesNo.No;
            this.E_PreservePlayerWL = enumBoolYesNo.No;
            this.E_CTWLEn = enumBoolYesNo.No;
            this.E_VIPWLEn = enumBoolYesNo.No;
            this.E_PlayerWLEn = enumBoolYesNo.No;
            this.E_KeepCTWLoffD = enumBoolYesNo.No;
            this.E_KeepVIPWLoffD = enumBoolYesNo.No;
            this.E_KeepPlWLoffD = enumBoolYesNo.No;
            //	oups :D
            this.D_PlayerStatus.Clear();
            this.I_TresholdTimeDisplay = 10;
            this.Lo_TresholdTime = (long)this.I_TresholdTimeDisplay * 600000000;
            this.I_MinPlayerOnServer = 32;
            this.B_TaskIsActive = false;
            this.I_CheckAFKEvery = 1;
            this.MaxTimeCheck = 60 * this.I_TresholdTimeDisplay * 3 / 10;   //	30% of "Elapse time before kick"
            this.E_CD_AFK = enumBoolYesNo.No;
            this.E_CD_KICK = enumBoolYesNo.Yes;
            this.E_CD_MOVES = enumBoolYesNo.Yes;
            this.E_ActionUnder = enumBoolYesNo.No;
            this.E_DPlayerSpam = enumBoolYesNo.Yes;
            this.I_DPlayerStartSpam = 60 * this.I_TresholdTimeDisplay / 4;  //	25% of "Elapse time before kick"
            this.I_DPlayerDelai = 5;
            this.E_DPlayerTMinus = enumBoolYesNo.No;
            this.S_DPlayerMSG = "%pn%, you are AFK! You will be kicked in %ttk% seconds";
            this.S_KickMSG = "%pn%, you were AFK for more than %At% minute";
            this.I_NbPMaxOnServer = 32;
            this.B_DPSTaskIsActive = false;
            this.B_CanBeDistribute = false;
        }
        #endregion

        #region Plugin details
        public string GetPluginName()
        {
            return "Idle Kicker";
        }

        public string GetPluginVersion()
        {
            return this.S_PlugVer;
        }

        public string GetPluginAuthor()
        {
            return "Myriades";
        }

        public string GetPluginWebsite()
        {
            return "phogue.net/forum/viewtopic.php?f=18&t=861";
        }

        // A note to plugin authors: DO NOT change how a tag works, instead make a whole new tag.
        public string GetPluginDescription()
        {
            return @"<h2>Description</h2>
	This plugin checks the AFK players.<br>
	They may be distributed (option) or kicked.<br>
	It includes many options such as white lists, and so on.<br>
	Keep in mind that this plugin kicks AFK players :
		<ul>
			<li>that has never spawned</li>
			<li>that are dead</li>
		</ul>
<h2>Settings</h2>
	<h3>1 : Plugin settings</h3>
		<blockquote>
			<h4>Check for AFK every :</h4>
			set this variable from 1 second to 10% of &quot;Elapse time before kick&quot; var<br>
			Unit : seconds<br>
			Remarks :
			<ul>
				<li>This setting has nothing to do with the player activity detection.</li>
				<li>This is just an interval time for checking AFK players.</li>
				<li>So, the lowest value you set, more precise will be the checks.</li>
			</ul>
		</blockquote>
		<blockquote>
			<h4>Display AFK in plugin console :</h4>
			Displays (or not) each player name and the time he is AFK.<br>
			It depends on &quot;Check for AFK every&quot; time value.
		</blockquote>
		<blockquote>
			<h4>Display MOVES in plugin console :</h4>
			Displays (or not) each player name when he is moved to the other team (with reason).<br>
			The action is performed every kill, if necessary :).
		</blockquote>
		<blockquote>
			<h4>Display KICK in plugin console :</h4>
			Displays (or not) each player name when he is kicked from the server (with reason).<br>
			It depends on &quot;Check for AFK every&quot; time value.
		</blockquote>
	<h3>2 : Conditional parameters kick</h3>
		<blockquote>
			<h4>Elapse time before kick :</h4>
			time till kicking the player.<br>
			Limited from 1 to 10 minutes<br>
			Unit : minutes
		</blockquote>
		<blockquote>
				<h4>Minimum players on server :</h4>
				value to start checking for kick<br>
				Limited from 1 to Max players on server
		</blockquote>
		<blockquote>
			<h4>Distribute players if minimum not reached :</h4>
			This option provides you the ability to move AFK players in the other team, in case of &quot;unbalanced&quot; server.<br>
			Also, this option does balance active players to keep a real balanced server.<br>
			Remarks :
			<ul>
				<li>Check is performed each time a kill is done.</li>
				<li><b><i>The active player balance needs to be improved (Will be done later).</i></b></li>
				<li>ATM, It doesn't take care about clans, squads and so on.</li>
				<li>The only one criteria that have been considered is the player score.</li>
				<li>So, the highest and lowest scores won't be moved.</li>
				<li>The mean score and deviation are used to evaluate players that can (or not) be moved.</li>
			</ul>
		</blockquote>
	<h3>3 : Communications</h3>
		<blockquote>
			<h4>Spam player :</h4>
				nothing to say
		</blockquote>
		<blockquote>
			<h4>Messages are displayed from the :</h4>
				Value to start spamming the player.<br>
				Limited from 25% to 75% of &quot;Elapse time before kick&quot;<br>
				Unit : seconds
		</blockquote>
		<blockquote>
			<h4>Delay between messages :</h4>
				Message display interval<br>
				Unit : seconds
		</blockquote>
		<blockquote>
			<h4>Uses T-minus countdown :</h4>
				spam the the player.<br>
				Depends on &quot;Check for AFK every&quot; time value
		</blockquote>
		<blockquote>
			<h4>Message :</h4>
				the displayed message<br>
				2 optionnal parameters :
				<ul>
					<li>%pn% : player name</li>
					<li>%ttk% : time till kick</li>
				</ul>
		</blockquote>
		<blockquote>
			<h4>Kick message :</h4>
				the message displayed to the player that has been kicked<br>
				2 optionnal parameters :
				<ul>
					<li>%pn% : player name</li>
					<li>%At% : AFK time</li>
				</ul>
		</blockquote>
	<h3>4 : White Lists</h3>
		<blockquote>
			<h4>Use white lists :</h4>
			quick en/disable of white lists (all)</blockquote>
		<blockquote>
			<h4>Uses [Clan tags | VIPs | PLayers] :</h4>
			quick en/disable of the current white list</blockquote>
		<blockquote>
			<h4>[Clan tags | VIPs | PLayers] :</h4>
			Remark :
			<ul>
				<li>Does match exactly</li>
			</ul>
		</blockquote>
		<blockquote>
			<h4>Preserving [Clan tags | VIPs | PLayers] if server is full</h4>
			This option offers to you the ability (or not) to kick players when the server is full
		</blockquote>
		<blockquote>
			<h4>Keep [Clan tags | VIPs | PLayers] off distribution</h4>
			This option offers to you the ability (or not) to switch players (AFK or not) to keep the server balanced.
		</blockquote>
<h2>Change log</h2>
	<h3>v" + this.S_PlugVer + @"</h3>
		<ul>
			<li>Fixed : works now with ProCon 1.4.0.9 and higher (SamTyler)</li>
		</ul>
	<h3>v1.3.3.12</h3>	
		<ul>
			<li>Fixed a bug : wrong player distribution when activating the plugin</li>
			<li>Fixed a bug : wrong player distribution on empty server</li>
			<li>Communications</li>
			<ul>
				<li>Added kick message</li>
			</ul>
			<li>White lists</li>
			<ul>
				<li>Added an option to preserve white lists from distribution</li>
			</ul>
		</ul>
	<h3>v1.3.3.11</h3>
		<ul>
			<li>Re-write plugin 2.0 details</li>
			<li>Conditionnal parameters</li>
			<ul>
				<li>Added players distribution</li>
			</ul>
			<li>Plugin settings</li>
			<ul>
				<li>Added an option to display (or not) MOVES in plugin console</li>
			</ul>
		</ul>
	<h3>v1.2.3.11</h3>
		<ul>
			<li>This is a major update</li>
			<li>Re-numbered the plugin version</li>
			<li>Numbered plugin options to get a logical ordering (as far as PRoCon will still sort by names)</li>
			<li>Improve player activity detection</li>
			<li>All settings are now saved</li>
			<li>White lists</li>
			<ul>
				<li>Added VIP white list</li>
				<li>Added individual white list activation</li>
				<li>Added for each list ""on server full option""</li>
			</ul>
			<li>Plugin settings</li>
			<ul>
				<li>Added an option to display (or not) KICK in plugin console</li>
				<li>Added an option to display (or not) AFK in plugin console</li>
			</ul>
			<li>Added Communications with dead players</li>
			<ul>
				<li>Added &quot;Spam player&quot;</li>
				<li>Added &quot;Messages are displayed from the&quot;</li>
				<li>Added &quot;Delay between messages&quot;</li>
				<li>Added &quot;Uses T-minus countdown&quot;</li>
				<li>Added &quot;Message&quot;</li>
			</ul>
		</ul>
	<h3>v0.4.1</h3>
		<ul>
			<li>Fixed a bug</li>
		</ul>
	<h3>v0.4</h3>
		<ul>
			<li>Some code improvement</li>
			<li>Fixed refresh rate bug</li>
		</ul>
	<h3>v0.3</h3>
		<ul>
			<li>Added Check as a system task.</li>
			<li>Fixed a bug that does mass kick, sorry for the disturbs :(</li>
		</ul>
	<h3>v0.2</h3>
		<ul>
			<li>Added Details 2.0</li>
			<li>Displays AFK player list in console(only)</li>
			<li>Added clan tag white list</li>
			<li>Added player white list</li>
		</ul>
	<h3>v0.1</h3>
		<ul>
			<li>initial release</li>
		</ul>
<h2>Comments</h2>
	<p>This plugin has ended his beta version.</p>
	<p>Some more things are on the todo list. Stay tuned :D<br>
		<ul>
			<li>Giving priority to &quot;White lists&quot;</li>
			<li>Adding &quot;Distribute AFK players&quot;</li>
		</ul>
	</p>
<h2>Credits</h2>
	<p>
		Phogue and the developper staff<br>
		Flyswamper for the ideas ;)
	</p>";
        }
        #endregion

        #region Plug load/enable/disable
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.S_strHostName = strHostName;
            this.S_strPort = strPort;
            this.S_strPRoConVersion = strPRoConVersion;
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bIdle Kicker ^2Enabled!");
            this.D_PlayerStatus.Clear();
            this.B_DPSTaskIsActive = false;
            this.B_TaskIsActive = false;
            this.ExecuteCommand("procon.protected.tasks.remove", "CIdleKicker_CheckForAFK");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bIdle Kicker ^1Disabled =(");
            this.D_PlayerStatus.Clear();
            this.B_DPSTaskIsActive = false;
            this.B_TaskIsActive = false;
            this.B_CanBeDistribute = false;
            this.ExecuteCommand("procon.protected.tasks.remove", "CIdleKicker_CheckForAFK");
        }
        #endregion

        #region plugin graphic controls
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            //	Plugin settings
            lstReturn.Add(new CPluginVariable("1 : Plugin settings|Check for AFK every", typeof(int), this.I_CheckAFKEvery));
            lstReturn.Add(new CPluginVariable("1 : Plugin settings|Display AFK in plugin console", typeof(enumBoolYesNo), this.E_CD_AFK));
            lstReturn.Add(new CPluginVariable("1 : Plugin settings|Display MOVES in plugin console", typeof(enumBoolYesNo), this.E_CD_MOVES));
            lstReturn.Add(new CPluginVariable("1 : Plugin settings|Display KICK in plugin console", typeof(enumBoolYesNo), this.E_CD_KICK));
            //	Conditionnal parameters
            lstReturn.Add(new CPluginVariable("2 : Conditional parameters|Elapse time before kick", typeof(int), this.I_TresholdTimeDisplay));
            lstReturn.Add(new CPluginVariable("2 : Conditional parameters|Minimum players on server", typeof(int), this.I_MinPlayerOnServer));
            lstReturn.Add(new CPluginVariable("2 : Conditional parameters|Distribute players if minimum not reached", typeof(enumBoolYesNo), this.E_ActionUnder));
            //	Communications with dead players
            lstReturn.Add(new CPluginVariable("3 : Communications|Spam player", typeof(enumBoolYesNo), this.E_DPlayerSpam));
            lstReturn.Add(new CPluginVariable("3 : Communications|Messages are displayed from the", typeof(int), this.I_DPlayerStartSpam));
            lstReturn.Add(new CPluginVariable("3 : Communications|Delay between messages", typeof(int), this.I_DPlayerDelai));
            lstReturn.Add(new CPluginVariable("3 : Communications|Uses T-minus countdown", typeof(enumBoolYesNo), this.E_DPlayerTMinus));
            lstReturn.Add(new CPluginVariable("3 : Communications|Message", typeof(string), this.S_DPlayerMSG));
            lstReturn.Add(new CPluginVariable("3 : Communications|Kick message", typeof(string), this.S_KickMSG));
            //	White lists
            lstReturn.Add(new CPluginVariable("4 : White lists|Use white lists", typeof(enumBoolYesNo), this.E_UsesWL));
            if (this.E_UsesWL == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("4.1 : White list :: Clan tag|Uses Clan Tags", typeof(enumBoolYesNo), this.E_CTWLEn));
                if (this.E_CTWLEn == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("4.1 : White list :: Clan tag|Clan Tags", typeof(string[]), this.L_ClanTagWL.ToArray()));
                    lstReturn.Add(new CPluginVariable("4.1 : White list :: Clan tag|Preserving clan tag's if server is full", typeof(enumBoolYesNo), this.E_PreserveCTWL));
                    if (this.E_ActionUnder == enumBoolYesNo.Yes)
                        lstReturn.Add(new CPluginVariable("4.1 : White list :: Clan tag|Keep clan tag's off Distribution", typeof(enumBoolYesNo), this.E_KeepCTWLoffD));
                }
                lstReturn.Add(new CPluginVariable("4.2 : White list :: VIP|Uses VIPs", typeof(enumBoolYesNo), this.E_VIPWLEn));
                if (this.E_VIPWLEn == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("4.2 : White list :: VIP|VIPs", typeof(string[]), this.L_VIPWL.ToArray()));
                    lstReturn.Add(new CPluginVariable("4.2 : White list :: VIP|Preserving VIP's if server is full", typeof(enumBoolYesNo), this.E_PreserveVIPWL));
                    if (this.E_ActionUnder == enumBoolYesNo.Yes)
                        lstReturn.Add(new CPluginVariable("4.2 : White list :: VIP|Keep VIP's off Distribution", typeof(enumBoolYesNo), this.E_KeepVIPWLoffD));
                }
                lstReturn.Add(new CPluginVariable("4.3 : White list :: Player|Uses Players", typeof(enumBoolYesNo), this.E_PlayerWLEn));
                if (this.E_PlayerWLEn == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("4.3 : White list :: Player|Players", typeof(string[]), this.L_PlayerWL.ToArray()));
                    lstReturn.Add(new CPluginVariable("4.3 : White list :: Player|Preserving player's if server is full", typeof(enumBoolYesNo), this.E_PreservePlayerWL));
                    if (this.E_ActionUnder == enumBoolYesNo.Yes)
                        lstReturn.Add(new CPluginVariable("4.3 : White list :: Player|Keep player's off Distribution", typeof(enumBoolYesNo), this.E_KeepPlWLoffD));
                }
            }

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            //	Plugin settings
            lstReturn.Add(new CPluginVariable("Check for AFK every", typeof(int), this.I_CheckAFKEvery));
            lstReturn.Add(new CPluginVariable("Display AFK in plugin console", typeof(enumBoolYesNo), this.E_CD_AFK));
            lstReturn.Add(new CPluginVariable("Display KICK in plugin console", typeof(enumBoolYesNo), this.E_CD_KICK));
            lstReturn.Add(new CPluginVariable("Display KICK in plugin console", typeof(enumBoolYesNo), this.E_CD_MOVES));
            //	Communications with dead players
            lstReturn.Add(new CPluginVariable("Spam player", typeof(enumBoolYesNo), this.E_DPlayerSpam));
            lstReturn.Add(new CPluginVariable("Messages are displayed from the", typeof(int), this.I_DPlayerStartSpam));
            lstReturn.Add(new CPluginVariable("Delay between messages", typeof(int), this.I_DPlayerDelai));
            lstReturn.Add(new CPluginVariable("Uses T-minus countdown", typeof(enumBoolYesNo), this.E_DPlayerTMinus));
            lstReturn.Add(new CPluginVariable("Message", typeof(string), this.S_DPlayerMSG));
            lstReturn.Add(new CPluginVariable("Kick message", typeof(string), this.S_KickMSG));
            //	Conditionnal parameters
            lstReturn.Add(new CPluginVariable("Elapse time before kick", typeof(int), this.I_TresholdTimeDisplay));
            lstReturn.Add(new CPluginVariable("Minimum players on server", typeof(int), this.I_MinPlayerOnServer));
            lstReturn.Add(new CPluginVariable("Distribute players if minimum not reached", typeof(enumBoolYesNo), this.E_ActionUnder));
            //	White lists
            lstReturn.Add(new CPluginVariable("Use white lists", typeof(enumBoolYesNo), this.E_UsesWL));
            lstReturn.Add(new CPluginVariable("Uses Clan Tags", typeof(enumBoolYesNo), this.E_CTWLEn));
            lstReturn.Add(new CPluginVariable("Clan Tags", typeof(string[]), this.L_ClanTagWL.ToArray()));
            lstReturn.Add(new CPluginVariable("Preserving clan tag's if server is full", typeof(enumBoolYesNo), this.E_PreserveCTWL));
            lstReturn.Add(new CPluginVariable("Uses VIPs", typeof(enumBoolYesNo), this.E_VIPWLEn));
            lstReturn.Add(new CPluginVariable("VIPs", typeof(string[]), this.L_VIPWL.ToArray()));
            lstReturn.Add(new CPluginVariable("Preserving VIP's if server is full", typeof(enumBoolYesNo), this.E_PreserveVIPWL));
            lstReturn.Add(new CPluginVariable("Uses Players", typeof(enumBoolYesNo), this.E_PlayerWLEn));
            lstReturn.Add(new CPluginVariable("Players", typeof(string[]), this.L_PlayerWL.ToArray()));
            lstReturn.Add(new CPluginVariable("Preserving player's if server is full", typeof(enumBoolYesNo), this.E_PreservePlayerWL));
            lstReturn.Add(new CPluginVariable("Keep clan tag's off Distribution", typeof(enumBoolYesNo), this.E_KeepCTWLoffD));
            lstReturn.Add(new CPluginVariable("Keep VIP's off Distribution", typeof(enumBoolYesNo), this.E_KeepVIPWLoffD));
            lstReturn.Add(new CPluginVariable("Keep player's off Distribution", typeof(enumBoolYesNo), this.E_KeepPlWLoffD));
            //	Server info
            lstReturn.Add(new CPluginVariable("I_NbPMaxOnServer", typeof(int), this.I_NbPMaxOnServer));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int TmpInt_1 = 0;
            switch (strVariable)
            {
                case "Check for AFK every":
                    if (int.TryParse(strValue, NumberStyles.Integer, null, out TmpInt_1))
                    {
                        this.I_CheckAFKEvery = this.IntLimiter(TmpInt_1, 1, this.MaxTimeCheck);
                        this.TaskRefreshRateUpdated();
                    }
                    break;
                case "Display AFK in plugin console":
                    this.E_CD_AFK = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Display MOVES in plugin console":
                    this.E_CD_MOVES = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Display KICK in plugin console":
                    this.E_CD_KICK = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Use white lists":
                    this.E_UsesWL = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Clan Tags":
                    this.L_ClanTagWL = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                    break;
                case "VIPs":
                    this.L_VIPWL = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                    break;
                case "Players":
                    this.L_PlayerWL = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                    break;
                case "Elapse time before kick":
                    if (int.TryParse(strValue, NumberStyles.Integer, null, out TmpInt_1))
                    {
                        //	evaluate new Message start at value
                        this.I_DPlayerStartSpam = TmpInt_1 * this.I_DPlayerStartSpam / this.I_TresholdTimeDisplay;
                        this.I_TresholdTimeDisplay = this.IntLimiter(TmpInt_1, 1, 10);
                        this.Lo_TresholdTime = (long)this.I_TresholdTimeDisplay * 600000000;
                        //	evaluate MaxCheckTime
                        this.MaxTimeCheck = 60 * this.I_TresholdTimeDisplay * 3 / 10;
                        this.I_CheckAFKEvery = this.IntLimiter(this.I_CheckAFKEvery, 1, this.MaxTimeCheck);
                        this.TaskRefreshRateUpdated();
                    }
                    break;
                case "Messages are displayed from the":
                    if (int.TryParse(strValue, NumberStyles.Integer, null, out TmpInt_1))
                        this.I_DPlayerStartSpam = this.IntLimiter(TmpInt_1, 60 * this.I_TresholdTimeDisplay / 4, 60 * this.I_TresholdTimeDisplay * 3 / 4);  //	From 25% to 75% of Elapse time before kick
                    break;
                case "Minimum players on server":
                    if (int.TryParse(strValue, NumberStyles.Integer, null, out TmpInt_1))
                    {
                        this.I_MinPlayerOnServer = this.IntLimiter(TmpInt_1, 1, this.I_NbPMaxOnServer);
                    }
                    break;
                case "Distribute players if minimum not reached":
                    this.E_ActionUnder = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Spam player":
                    this.E_DPlayerSpam = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Delay between messages":
                    if (int.TryParse(strValue, NumberStyles.Integer, null, out TmpInt_1))
                        this.I_DPlayerDelai = this.IntLimiter(TmpInt_1, 5, this.MaxTimeCheck);
                    break;
                case "Uses T-minus countdown":
                    this.E_DPlayerTMinus = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Message":
                    this.S_DPlayerMSG = strValue;
                    break;
                case "Kick message":
                    this.S_KickMSG = strValue;
                    break;
                case "Preserving clan tag's if server is full":
                    this.E_PreserveCTWL = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Preserving VIP's if server is full":
                    this.E_PreserveVIPWL = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Preserving player's if server is full":
                    this.E_PreservePlayerWL = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Uses Clan Tags":
                    this.E_CTWLEn = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Uses VIPs":
                    this.E_VIPWLEn = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Uses Players":
                    this.E_PlayerWLEn = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Keep clan tag's off Distribution":
                    this.E_KeepCTWLoffD = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Keep VIP's off Distribution":
                    this.E_KeepVIPWLoffD = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
                case "Keep player's off Distribution":
                    this.E_KeepPlWLoffD = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                    break;
            }
            this.CheckTask();
        }

        #endregion

        #region Account created
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
        #endregion

        #region Player events
        public void OnPlayerJoin(string strSoldierName)
        {
            this.D_PlayerStatus.Add(strSoldierName, new PlayerStatus());
            this.CheckTask();
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {
        }

        public void OnPlayerLeft(string strSoldierName)
        {
            this.D_PlayerStatus.Remove(strSoldierName);
            this.CheckTask();
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {
        }
        #endregion

        #region (un peu de tout)
        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            if (strSpeaker != "Server")
                this.D_PlayerStatus[strSpeaker].PlayerEvent();
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            if (strSpeaker != "Server")
                this.D_PlayerStatus[strSpeaker].PlayerEvent();
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            if (strSpeaker != "Server")
                this.D_PlayerStatus[strSpeaker].PlayerEvent();
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
            this.I_NbPMaxOnServer = csiServerInfo.MaxPlayerCount;
            this.I_MinPlayerOnServer = this.IntLimiter(this.I_MinPlayerOnServer, 1, this.I_NbPMaxOnServer);
        }
        #endregion

        #region Communication Events
        public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset)
        {
        }

        public void OnSaying(string strMessage, CPlayerSubset cpsSubset)
        {
            //	player killed by an admin
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.Player && strMessage.StartsWith("You have been killed by an admin"))
            {
                this.D_PlayerStatus[cpsSubset.SoldierName].PlayerDied();
            }
        }
        #endregion

        #region Level Events
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
        #endregion

        #region Does not work in R3, never called for now.
        public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps)
        {
        }

        public void OnPlaylistSet(string strPlaylist)
        {

        }

        public void OnListPlaylists(List<string> lstPlaylists)
        {

        }
        #endregion

        #region Player Kick/List Events
        public void OnPlayerKicked(string strSoldierName, string strReason)
        {
        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {
            if (!this.D_PlayerStatus[strSoldierName].B_IsAFK)
            {
                this.D_PlayerStatus[strSoldierName].I_TeamID = iTeamID;
                this.D_PlayerStatus[strSoldierName].PlayerDied();
            }
        }

        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {
            if (!this.D_PlayerStatus[strSpeaker].B_IsAFK)
            {
                this.D_PlayerStatus[strSpeaker].I_TeamID = iTeamID;
                this.D_PlayerStatus[strSpeaker].PlayerEvent();
            }
        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            foreach (CPlayerInfo playerInfos in lstPlayers)
            {
                if (this.D_PlayerStatus.ContainsKey(playerInfos.SoldierName))
                {
                    //	Check player score
                    if (playerInfos.Score != this.D_PlayerStatus[playerInfos.SoldierName].I_PlayerScore)
                    {
                        this.D_PlayerStatus[playerInfos.SoldierName].I_PlayerScore = playerInfos.Score;
                        this.D_PlayerStatus[playerInfos.SoldierName].PlayerEvent();
                    }
                    //	search for clan tag
                    if (this.D_PlayerStatus[playerInfos.SoldierName].S_ClanTag != playerInfos.ClanTag)
                        this.D_PlayerStatus[playerInfos.SoldierName].S_ClanTag = playerInfos.ClanTag;
                    //	search for teamID
                    if (this.D_PlayerStatus[playerInfos.SoldierName].I_TeamID != playerInfos.TeamID)
                        this.D_PlayerStatus[playerInfos.SoldierName].I_TeamID = playerInfos.TeamID;
                    //	skip end of loop
                    continue;
                }
                PlayerStatus playerstatus = new PlayerStatus();
                this.D_PlayerStatus.Add(playerInfos.SoldierName, playerstatus);
            }
            this.B_CanBeDistribute = true;
            this.CheckTask();
        }
        #endregion

        #region Banning and Banlist Events
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
        #endregion

        #region Reserved Slots Events
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
        #endregion

        #region Maplist Events
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
        #endregion

        #region Game Vars
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
        #endregion

        #region IPRoConPluginInterface2

        //
        // IPRoConPluginInterface2
        //
        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            //	the one killed
            this.D_PlayerStatus[kKillerVictimDetails.Killer.SoldierName].PlayerKilled();
            //	the one died
            this.D_PlayerStatus[kKillerVictimDetails.Victim.SoldierName].PlayerDied();
            //	try to make some player distribution
            if (this.B_CanBeDistribute && !this.B_DPSTaskIsActive && this.D_PlayerStatus.Count < this.I_MinPlayerOnServer && this.E_ActionUnder == enumBoolYesNo.Yes)
            {
                this.B_DPSTaskIsActive = true;
                this.DistributePlayers(kKillerVictimDetails.Victim.SoldierName);
            }
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
            this.D_PlayerStatus[soldierName].PlayerSpawned();
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

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage)
        {

        }

        public void OnRegisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnUnregisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {
            //	Reset all players
            foreach (KeyValuePair<string, PlayerStatus> playerDatas in this.D_PlayerStatus)
                this.D_PlayerStatus[playerDatas.Key] = new PlayerStatus();
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public void OnMaplistList(List<MaplistEntry> lstMaplist)
        {

        }

        #endregion

        #region IPRoConPluginInterface4

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage, object trespassState)
        {

        }
        #endregion

        #region MY functions
        private static long RefreshedTime()
        {
            DateTime tmpDateTime;
            tmpDateTime = DateTime.Now;
            return tmpDateTime.ToFileTimeUtc();
        }

        public void CheckForAFK()
        {
            bool B_kick_player;
            long death;
            long LastEvent;
            long alive;
            long AFKTime;
            long TimeGoesOn;
            long TimeTillKick;
            bool b_enoughPlayer;
            foreach (KeyValuePair<string, PlayerStatus> playerDatas in this.D_PlayerStatus)
            {
                b_enoughPlayer = this.D_PlayerStatus.Count >= this.I_NbPMaxOnServer;
                if (this.E_UsesWL == enumBoolYesNo.Yes)
                {
                    //	Check for Player white List
                    if (this.L_PlayerWL.Contains(playerDatas.Key) && !(b_enoughPlayer && this.E_PreservePlayerWL == enumBoolYesNo.No) && this.E_PlayerWLEn == enumBoolYesNo.Yes)
                    {
                        if (E_KeepPlWLoffD == enumBoolYesNo.Yes)
                            this.D_PlayerStatus[playerDatas.Key].PlayerEvent();
                        continue;
                    }
                    //	Check for VIP white List
                    if (this.L_VIPWL.Contains(playerDatas.Key) && !(b_enoughPlayer && this.E_PreserveVIPWL == enumBoolYesNo.No) && this.E_VIPWLEn == enumBoolYesNo.Yes)
                    {
                        if (E_KeepVIPWLoffD == enumBoolYesNo.Yes)
                            this.D_PlayerStatus[playerDatas.Key].PlayerEvent();
                        continue;
                    }
                    //	Check for clan tag white List
                    if (this.L_ClanTagWL.Contains(this.D_PlayerStatus[playerDatas.Key].S_ClanTag) && !(b_enoughPlayer && this.E_PreserveCTWL == enumBoolYesNo.No) && this.E_CTWLEn == enumBoolYesNo.Yes)
                    {
                        if (E_KeepCTWLoffD == enumBoolYesNo.Yes)
                            this.D_PlayerStatus[playerDatas.Key].PlayerEvent();
                        continue;
                    }
                }
                //	evaluate the current player
                TimeGoesOn = RefreshedTime();
                B_kick_player = false;
                death = playerDatas.Value.I_TimeOfDeath;
                LastEvent = playerDatas.Value.I_TimePlayerEvent;
                alive = playerDatas.Value.I_TimeOfSpawn;
                AFKTime = (TimeGoesOn - LastEvent) / 10000000;
                TimeTillKick = (int)((LastEvent + this.Lo_TresholdTime - TimeGoesOn) / 10000000);
                //	The player has never spawned or never died
                if (alive == 0 && death == 0 && !playerDatas.Value.B_PlayerIsDead || playerDatas.Value.B_PlayerIsDead)
                {
                    //	Displays AFK players in Console
                    if (this.E_CD_AFK == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "Player : ^b" + this.D_PlayerStatus[playerDatas.Key].S_ClanTag + " " + playerDatas.Key + "^n is AFK for : ^b" + AFKTime.ToString() + " second(s)");
                    //	Spam dead player in case where minimum player on server has been reached. Message can only be said.
                    if (this.E_DPlayerSpam == enumBoolYesNo.Yes && AFKTime >= this.I_DPlayerStartSpam && this.D_PlayerStatus.Count >= this.I_MinPlayerOnServer)
                    {
                        int ModDelai = (int)TimeTillKick % I_DPlayerDelai;
                        string TheMSG = this.S_DPlayerMSG.Replace("%ttk%", TimeTillKick.ToString());
                        TheMSG = TheMSG.Replace("%pn%", playerDatas.Key);
                        if (ModDelai == 0 && TimeTillKick >= this.I_DPlayerDelai)
                            this.ExecuteCommand("procon.protected.send", "admin.say", TheMSG, "player", playerDatas.Key);
                        else if (this.E_DPlayerTMinus == enumBoolYesNo.Yes && TimeTillKick < this.I_DPlayerDelai)   //	T-minus task
                            this.ExecuteCommand("procon.protected.send", "admin.say", TheMSG, "player", playerDatas.Key);
                    }
                    //	how long this guy is dead?
                    if (TimeTillKick <= 0)
                    {
                        if (this.D_PlayerStatus.Count >= this.I_MinPlayerOnServer)
                            B_kick_player = true;
                        else if (this.E_ActionUnder == enumBoolYesNo.Yes)
                            this.D_PlayerStatus[playerDatas.Key].B_IsAFK = true;
                    }
                }
                //	Must be dead
                if (B_kick_player)
                {
                    if (this.E_CD_KICK == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "Player : ^b" + this.D_PlayerStatus[playerDatas.Key].S_ClanTag + " " + playerDatas.Key + "^n has been kicked. Reason : AFK");
                    string TheMSG = this.S_KickMSG.Replace("%pn%", playerDatas.Key);
                    TheMSG = TheMSG.Replace("%At%", this.I_TresholdTimeDisplay.ToString());
                    this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", playerDatas.Key, TheMSG);
                    this.D_PlayerStatus.Remove(playerDatas.Key);
                }
            }
        }

        private int IntLimiter(int ValueToCheck, int LowerLimit, int UpperLimit)
        {
            if (ValueToCheck < LowerLimit)
                ValueToCheck = LowerLimit;
            else if (ValueToCheck > UpperLimit)
                ValueToCheck = UpperLimit;
            return ValueToCheck;
        }

        private void TaskRefreshRateUpdated()
        {
            if (this.B_TaskIsActive)
            {
                this.ExecuteCommand("procon.protected.tasks.remove", "CIdleKicker_CheckForAFK");
                this.ExecuteCommand("procon.protected.tasks.add", "CIdleKicker_CheckForAFK", "0", this.I_CheckAFKEvery.ToString(), "-1", "procon.protected.plugins.call", "CIdleKicker", "CheckForAFK");
            }
        }

        private void CheckTask()
        {
            if ((this.D_PlayerStatus.Count >= this.I_MinPlayerOnServer || this.E_ActionUnder == enumBoolYesNo.Yes) && !this.B_TaskIsActive)
            {
                this.ExecuteCommand("procon.protected.tasks.add", "CIdleKicker_CheckForAFK", "0", this.I_CheckAFKEvery.ToString(), "-1", "procon.protected.plugins.call", "CIdleKicker", "CheckForAFK");
                this.B_TaskIsActive = true;
            }
            else if (this.D_PlayerStatus.Count < this.I_MinPlayerOnServer && !(this.E_ActionUnder == enumBoolYesNo.Yes))
            {
                this.ExecuteCommand("procon.protected.tasks.remove", "CIdleKicker_CheckForAFK");
                this.B_TaskIsActive = false;
            }
        }

        private void DistributePlayers(string strSoldierName)
        {
            /*
			 *	Ne peut pas fonctionner  en mode SQRUSH et SQDM
			 */
            int I_NbPlayerActivOnTeamA = 0;
            List<string> L_SoldierNameInactivOnTeamA = new List<string>();
            int I_NbPlayerActivOnTeamB = 0;
            List<string> L_SoldierNameInactivOnTeamB = new List<string>();
            int I_TeamBValue = 0;
            int i = 0;
            List<int> L_ScoreTeamA = new List<int>();
            int I_CurPlayerTeam;
            int I_CurPlayerScore;
            lock (this.D_PlayerStatus)
            {
                I_CurPlayerTeam = this.D_PlayerStatus[strSoldierName].I_TeamID;
                I_CurPlayerScore = this.D_PlayerStatus[strSoldierName].I_PlayerScore;
                //	Count active and enum inactive players on each team
                foreach (KeyValuePair<string, PlayerStatus> playerDatas in this.D_PlayerStatus)
                {
                    if (playerDatas.Value.I_TeamID == I_CurPlayerTeam)
                    {   //	TeamA
                        if (playerDatas.Value.B_IsAFK)
                            L_SoldierNameInactivOnTeamA.Add(playerDatas.Key);
                        else
                        {
                            L_ScoreTeamA.Add(playerDatas.Value.I_PlayerScore);
                            I_NbPlayerActivOnTeamA++;
                        }
                    }
                    else
                    {   //	TeamB
                        I_TeamBValue = this.D_PlayerStatus[playerDatas.Key].I_TeamID;
                        if (playerDatas.Value.B_IsAFK)
                            L_SoldierNameInactivOnTeamB.Add(playerDatas.Key);
                        else
                            I_NbPlayerActivOnTeamB++;
                    }
                }
            }
            int limit_inf = Bornes(L_ScoreTeamA, "min");
            int limit_sup = Bornes(L_ScoreTeamA, "max");
            //	There is more active players on TeamA than TeamB + 2, so we move the died player from TeamA to TeamB (nothing to do with clan, friends; done later, btw it's the OnDeathBalance plugin)
            // this.ExecuteCommand("procon.protected.pluginconsole.write", "Stage 1 : " + I_NbPlayerActivOnTeamA.ToString() + "|" + L_SoldierNameInactivOnTeamA.Count.ToString() + "|" + I_NbPlayerActivOnTeamB.ToString() + "|" + L_SoldierNameInactivOnTeamB.Count.ToString());
            if (I_NbPlayerActivOnTeamA >= I_NbPlayerActivOnTeamB + 2 && I_CurPlayerScore >= limit_inf && I_CurPlayerScore <= limit_sup)
            {
                if (I_TeamBValue > 0)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.movePlayer", strSoldierName, I_TeamBValue.ToString(), "0", "true");
                    //	Update vars
                    I_NbPlayerActivOnTeamA--;
                    I_NbPlayerActivOnTeamB++;
                    //	Notify player
                    this.ExecuteCommand("procon.protected.send", "admin.say", "You have been moved to the other team to keep the server balanced", "player", strSoldierName);
                    //	Dislpay plugin console message
                    if (this.E_CD_MOVES == enumBoolYesNo.Yes)
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b" + strSoldierName + "^n has been moved to the other team to keep the server balanced");
                }
            }
            //	Now we checked AFK team A to get a "balanced" server (move from A to B)
            // this.ExecuteCommand("procon.protected.pluginconsole.write", "Stage 2 : " + I_NbPlayerActivOnTeamA.ToString() + "|" + L_SoldierNameInactivOnTeamA.Count.ToString() + "|" + I_NbPlayerActivOnTeamB.ToString() + "|" + L_SoldierNameInactivOnTeamB.Count.ToString());
            i = L_SoldierNameInactivOnTeamA.Count;
            while (I_NbPlayerActivOnTeamA + i >= I_NbPlayerActivOnTeamB + L_SoldierNameInactivOnTeamB.Count + 2)
            {
                //	Move the player in game
                this.ExecuteCommand("procon.protected.send", "admin.movePlayer", L_SoldierNameInactivOnTeamA[i - 1], I_TeamBValue.ToString(), "0", "true");
                //	Notify player
                this.ExecuteCommand("procon.protected.send", "admin.say", "You have been moved to the other team", "player", L_SoldierNameInactivOnTeamA[i - 1]);
                //	Dislpay plugin console message
                if (this.E_CD_MOVES == enumBoolYesNo.Yes)
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^b" + L_SoldierNameInactivOnTeamA[i - 1] + "^n has been moved to the other team : AFK");
                //	Add the player to AFK TeamB
                L_SoldierNameInactivOnTeamB.Add(L_SoldierNameInactivOnTeamA[i - 1]);
                //	Remove player from AFK TeamA
                L_SoldierNameInactivOnTeamA.Remove(L_SoldierNameInactivOnTeamA[i - 1]);
                i = L_SoldierNameInactivOnTeamA.Count;
            }
            //	Now we checked AFK team B to get a "balanced" server (move from B to A)
            // this.ExecuteCommand("procon.protected.pluginconsole.write", "Stage 3 : " + I_NbPlayerActivOnTeamA.ToString() + "|" + L_SoldierNameInactivOnTeamA.Count.ToString() + "|" + I_NbPlayerActivOnTeamB.ToString() + "|" + L_SoldierNameInactivOnTeamB.Count.ToString());
            i = L_SoldierNameInactivOnTeamB.Count;
            while (I_NbPlayerActivOnTeamB + i >= I_NbPlayerActivOnTeamA + L_SoldierNameInactivOnTeamA.Count + 2)
            {
                //	Move the player in game
                this.ExecuteCommand("procon.protected.send", "admin.movePlayer", L_SoldierNameInactivOnTeamB[i - 1], I_CurPlayerTeam.ToString(), "0", "true");
                //	Notify player
                this.ExecuteCommand("procon.protected.send", "admin.say", "You have been moved to the other team", "player", L_SoldierNameInactivOnTeamB[i - 1]);
                //	Dislpay plugin console message
                if (this.E_CD_MOVES == enumBoolYesNo.Yes)
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^b" + L_SoldierNameInactivOnTeamB[i - 1] + "^n has been moved to the other team : AFK");
                //	Add the player to AFK TeamA
                L_SoldierNameInactivOnTeamA.Add(L_SoldierNameInactivOnTeamB[i - 1]);
                //	Remove player from AFK TeamB
                L_SoldierNameInactivOnTeamB.Remove(L_SoldierNameInactivOnTeamB[i - 1]);
                i = L_SoldierNameInactivOnTeamB.Count;
            }
            this.B_DPSTaskIsActive = false;
        }

        private int Bornes(List<int> maliste, string QueFaire)
        {
            int i = maliste.Count;
            if (i == 0)
                return 0;
            else
            {
                double moyenne = 0;
                double e_type = 0;
                double somme1 = 0;
                double somme2 = 0;
                //	mean
                foreach (double x in maliste)
                {
                    somme1 += x;
                }
                moyenne = somme1 / i;
                //	populated deviance (correted)
                somme1 = 0;
                foreach (double x in maliste)
                {
                    somme1 += Math.Pow(x - moyenne, 2);
                    somme2 += x - moyenne;
                }
                somme2 = Math.Pow(somme2, 2) / i;
                e_type = Math.Sqrt((somme1 - somme2) / (i - 1));
                //	defines the limits
                switch (QueFaire)
                {
                    case "min":
                        return (int)Math.Round(moyenne - e_type);
                        break;
                    case "max":
                        return (int)Math.Round(moyenne + e_type);
                        break;
                    default:
                        return 0;
                        break;
                }
            }
        }
        #endregion
    }
}