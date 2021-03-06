/*  Copyright 2010 [GWC]XpKillerhx

    This plugin file is part of PRoCon.

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
using System.Configuration;
using System.ComponentModel;
using System.Text.RegularExpressions;

//Procon includes
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;


namespace PRoConEvents
{
    public class CPlayerVote : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Variables and Constructor
        //Proconvariables
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;
        private bool m_isPluginEnabled;

        private CVote Voteobj = new CVote(false);

        //Logging for Banning/ kicking
        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
        private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();   //Players

        //Various Variables
        private string serverName;
        private CServerInfo ServerInfo;
        private List<CPlayerInfo> lstPlayerList;
        private enumBoolYesNo m_debugmessages;

        //Ingame Commands
        private string m_strPrivatePrefix;
        private string m_strAdminsPrefix;
        private string m_strPublicPrefix;

        //Command Strings
        private string m_strVoteKickCommand;
        private string m_strVoteBanCommand;
        private string m_strVoteCommand;
        private string m_strConfirmCommand;
        private string m_strCancelCommand;

        //Votekick
        private enumBoolYesNo m_Votekick;
        private int m_Votekick_minplayers;
        private int m_Votekick_reqVotes;
        private int m_Votekick_voteduration;
        private double m_Votekick_percent;

        //Voteban
        private enumBoolYesNo m_Voteban;
        private int m_Voteban_minplayers;
        private int m_Voteban_reqVotes;
        private int m_Voteban_voteduration;
        private double m_Voteban_percent;

        //Vote Messages
        private List<string> msg_StartVote = new List<string>();
        private List<string> msg_VoteFailed = new List<string>();
        private List<string> msg_VoteSuccessful = new List<string>();
        private List<string> msg_VoteResults = new List<string>();
        private string m_Reason;

        //Spamprotection
        private int numberOfAllowedRequests;
        private CSpamprotection Spamprotection;

        //Whitelist
        private List<string> m_playerwhitelist = new List<string>();
        private List<string> m_clanwhitelist = new List<string>();
        private enumBoolYesNo m_performAction;


        public CPlayerVote()
        {
            this.m_strHostName = "";
            this.m_strPort = "";
            this.m_strPRoConVersion = "";
            this.m_debugmessages = enumBoolYesNo.No;

            this.m_isPluginEnabled = false;
            this.lstPlayerList = new List<CPlayerInfo>();
            //Scope
            this.m_strPrivatePrefix = "@";
            this.m_strAdminsPrefix = "#";
            this.m_strPublicPrefix = "!";
            //Command
            this.m_strVoteKickCommand = "votekick";
            this.m_strVoteBanCommand = "voteban";
            this.m_strVoteCommand = "vote";
            this.m_strConfirmCommand = "yes";
            this.m_strCancelCommand = "cancel";
            //Spamprotection
            numberOfAllowedRequests = 1; //Number of allowed Requests per Round
            this.Spamprotection = new CSpamprotection(numberOfAllowedRequests);

            //Vote
            this.m_Voteban = enumBoolYesNo.No;
            this.m_Votekick_minplayers = 6;
            this.m_Votekick_reqVotes = 16;
            this.m_Votekick_voteduration = 1;
            this.m_Votekick_percent = 60;

            this.m_Votekick = enumBoolYesNo.No;
            this.m_Voteban_minplayers = 6;
            this.m_Voteban_reqVotes = 16;
            this.m_Voteban_voteduration = 1;
            this.m_Voteban_percent = 75;


            //Vote Messages
            this.msg_StartVote = new List<string>();
            this.msg_StartVote.Add("[Yell]A %votetype% has been started against Player %target% by %starter%");
            this.msg_StartVote.Add("[Yell]Use @vote yes or @vote no to vote - %req% Votes required");
            this.msg_VoteFailed = new List<string>();
            this.msg_VoteFailed.Add("[Yell]The %votetype% failed, Player %target% stays");
            this.msg_VoteSuccessful = new List<string>();
            this.msg_VoteSuccessful.Add("[Yell]The %votetype% was successful, Player %target% will be removed");
            this.msg_VoteResults = new List<string>();
            this.msg_VoteResults.Add("[Say]%yes% Players vote for YES  and %no% for NO - Total: %total% votes");
            this.m_Reason = "You have been kicked or banned due to a result of a %votetype%!";

            //Whitelist
            this.m_playerwhitelist = new List<string>();
            this.m_clanwhitelist = new List<string>();
            this.m_performAction = enumBoolYesNo.No;

        }
        #endregion

        #region PluginSetup
        public string GetPluginName()
        {
            return "PRoCon Voteban and Votekick";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.1";
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
    <p>This plugin allows Players to start a Vote against an other Player.</p>
    <p>You are to choose which vote types are activ, also you can set parameter like vote duration or how many player needed to start a vote.</p>
  	<p>If a vote is active no other Vote can be started.</p>
  	<p>When the vote ends the desired action will be execute if enough player vote for it.</p>
  	<p>ATM the voteban uses Namebans only this will maybe change in later version or on request.</p>
  	<p>All Players will be informed about the Vote status.</p>
    
<h2>Requirements</h2>
	<p>Adjust the pluginsetting for your needs and Serversize.</p>
	<p>Pls Give FEEDBACK !!!</p>

<h2>Installation</h2>
<p>Download and install this plugin</p>

<h2>Ingame Commands</h2>
	<blockquote><h4>[@,#,!]voteban PlayerName [Reason] Starts a Voteban.</h4></blockquote>
	<blockquote><h4>[@,#,!]votekick PlayerName [Reason] Starts a Votekick.</h4></blockquote>
	<blockquote><h4>[@,#,!]vote [yes,no] To vote for or against a Player.</h4></blockquote>
	<blockquote><h4>[@,#,!]vote [cancel] To cancel a you started (Only the player who started the vote can cancel his vote).</h4></blockquote>

<h3>Known issues:</h3>
<p> -none yet but i expect some ;-)</p>

<h3>Plans for future release:</h3>
Feature Requests ;-)

Changelog:<br>
1.0.0.1<br>
Added Whitelist<br><br>

1.0.0.0<br>
First release
";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerJoin", "OnPunkbusterPlayerInfo", "OnServerInfo",
                                                     "OnPlayerLeft", "OnRoundOverPlayers", "OnCommandVotekick", "OnCommandVoteban",
                                                     "OnCommandVote");
        }

        public void OnPluginEnable()
        {
            this.serverName = "";
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon Voteban and Votekick ^2Enabled");
            //Register Commands
            this.m_isPluginEnabled = true;
            this.Spamprotection = new CSpamprotection(numberOfAllowedRequests);
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon Voteban and Votekick: ^2 Number of votes set to " + this.numberOfAllowedRequests.ToString() + " per Round for each Player");
            this.RegisterAllCommands();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon Voteban and Votekick ^1Disabled");
            //Unregister Commands
            this.m_isPluginEnabled = false;
            this.UnregisterAllCommands();
            this.ExecuteCommand("procon.protected.tasks.remove", "CPlayerVote");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            //Spamprotection
            lstReturn.Add(new CPluginVariable("Requests|Playerrequests per Round", this.numberOfAllowedRequests.GetType(), this.numberOfAllowedRequests));
            //Voteban
            lstReturn.Add(new CPluginVariable("Voteban|Enable Voteban?", typeof(enumBoolYesNo), this.m_Voteban));
            if (this.m_Voteban == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Voteban|Minimum Playercount to start a Voteban?", this.m_Voteban_minplayers.GetType(), this.m_Voteban_minplayers));
                lstReturn.Add(new CPluginVariable("Voteban|Percent to Ban", this.m_Voteban_percent.GetType(), this.m_Voteban_percent));
                lstReturn.Add(new CPluginVariable("Voteban|Voteban Duration", this.m_Voteban_voteduration.GetType(), this.m_Voteban_voteduration));
                lstReturn.Add(new CPluginVariable("Voteban|Voteban Command", this.m_strVoteBanCommand.GetType(), this.m_strVoteBanCommand));
            }
            //Votekick
            lstReturn.Add(new CPluginVariable("Votekick|Enable Votekick?", typeof(enumBoolYesNo), this.m_Votekick));
            if (this.m_Votekick == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Votekick|Minimum Playercount to start a Votekick?", this.m_Votekick_minplayers.GetType(), this.m_Votekick_minplayers));
                lstReturn.Add(new CPluginVariable("Votekick|Percent to Kick", this.m_Votekick_percent.GetType(), this.m_Votekick_percent));
                lstReturn.Add(new CPluginVariable("Votekick|Votekick Duration", this.m_Votekick_voteduration.GetType(), this.m_Votekick_voteduration));
                lstReturn.Add(new CPluginVariable("Votekick|Votekick Command", this.m_strVoteKickCommand.GetType(), this.m_strVoteKickCommand));
            }
            //Whitelists
            lstReturn.Add(new CPluginVariable("Whitelist|Protected Playerlist", typeof(string[]), this.m_playerwhitelist.ToArray()));
            lstReturn.Add(new CPluginVariable("Whitelist|Protected Clantags", typeof(string[]), this.m_clanwhitelist.ToArray()));
            lstReturn.Add(new CPluginVariable("Whitelist|Kick for trying?", typeof(enumBoolYesNo), this.m_performAction));
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Playerrequests per Round", this.numberOfAllowedRequests.GetType(), this.numberOfAllowedRequests));
            //Voteban
            lstReturn.Add(new CPluginVariable("Enable Voteban?", typeof(enumBoolYesNo), this.m_Voteban));
            if (this.m_Voteban == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Minimum Playercount to start a Voteban?", this.m_Voteban_minplayers.GetType(), this.m_Voteban_minplayers));
                lstReturn.Add(new CPluginVariable("Percent to Ban", this.m_Voteban_percent.GetType(), this.m_Voteban_percent));
                lstReturn.Add(new CPluginVariable("Voteban Duration", this.m_Voteban_voteduration.GetType(), this.m_Voteban_voteduration));
                lstReturn.Add(new CPluginVariable("Voteban Command", this.m_strVoteBanCommand.GetType(), this.m_strVoteBanCommand));
            }
            //Votekick
            lstReturn.Add(new CPluginVariable("Enable Votekick?", typeof(enumBoolYesNo), this.m_Votekick));
            if (this.m_Votekick == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Minimum Playercount to start a Votekick?", this.m_Votekick_minplayers.GetType(), this.m_Votekick_minplayers));
                lstReturn.Add(new CPluginVariable("Percent to Kick", this.m_Votekick_percent.GetType(), this.m_Votekick_percent));
                lstReturn.Add(new CPluginVariable("Votekick Duration", this.m_Votekick_voteduration.GetType(), this.m_Votekick_voteduration));
                lstReturn.Add(new CPluginVariable("Votekick Command", this.m_strVoteKickCommand.GetType(), this.m_strVoteKickCommand));
            }
            //Whitelists
            lstReturn.Add(new CPluginVariable("Protected Playerlist", typeof(string[]), this.m_playerwhitelist.ToArray()));
            lstReturn.Add(new CPluginVariable("Protected Clantags", typeof(string[]), this.m_clanwhitelist.ToArray()));
            lstReturn.Add(new CPluginVariable("Kick for trying?", typeof(enumBoolYesNo), this.m_performAction));
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            if (strVariable.CompareTo("Playerrequests per Round") == 0 && Int32.TryParse(strValue, out numberOfAllowedRequests) == true)
            {
                this.numberOfAllowedRequests = Convert.ToInt32(strValue);
                this.Spamprotection = new CSpamprotection(numberOfAllowedRequests);
            }
            //Voteban
            else if (strVariable.CompareTo("Enable Voteban?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_Voteban = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Minimum Playercount to start a Voteban?") == 0 && Int32.TryParse(strValue, out this.m_Voteban_minplayers) == true)
            {
                this.m_Voteban_minplayers = Convert.ToInt32(strValue);
            }
            else if (strVariable.CompareTo("Percent to Ban") == 0 && Double.TryParse(strValue, out this.m_Voteban_percent) == true)
            {
                this.m_Voteban_percent = Convert.ToDouble(strValue);
            }
            else if (strVariable.CompareTo("Voteban Duration") == 0 && Int32.TryParse(strValue, out this.m_Voteban_voteduration) == true)
            {
                this.m_Voteban_voteduration = Convert.ToInt32(strValue);
            }
            else if (strVariable.CompareTo("Voteban Command") == 0)
            {
                this.m_strVoteBanCommand = strValue;
            }
            //Votekick
            else if (strVariable.CompareTo("Enable Votekick?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_Votekick = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Minimum Playercount to start a Votekick?") == 0 && Int32.TryParse(strValue, out this.m_Votekick_minplayers) == true)
            {
                this.m_Votekick_minplayers = Convert.ToInt32(strValue);
            }
            else if (strVariable.CompareTo("Percent to Kick") == 0 && Double.TryParse(strValue, out this.m_Votekick_percent) == true)
            {
                this.m_Votekick_percent = Convert.ToDouble(strValue);
            }
            else if (strVariable.CompareTo("Votekick Duration") == 0 && Int32.TryParse(strValue, out this.m_Votekick_voteduration) == true)
            {
                this.m_Votekick_voteduration = Convert.ToInt32(strValue);
            }
            else if (strVariable.CompareTo("Votekick Command") == 0)
            {
                this.m_strVoteKickCommand = strValue;
            }
            //Whitelists
            else if (strVariable.CompareTo("Protected Playerlist") == 0)
            {
                this.m_playerwhitelist = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Protected Clantags") == 0)
            {
                this.m_clanwhitelist = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Kick for trying?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_performAction = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            this.RegisterAllCommands();

        }

        private void UnregisterAllCommands()
        {
            List<string> emptyList = new List<string>();
            /*
        	this.UnregisterCommand(
        		new MatchCommand(
        			"CEmailNotifier",
        			"OnCommandEmail",
        			this.Listify<string>("@", "!", "#"),
        				"email", 
        			this.Listify<MatchArgumentFormat>(),
        				new ExecutionRequirements(ExecutionScope.All),
        					"Sends a Test-Email"
        		)
        	);*/

            this.UnregisterCommand(
                new MatchCommand(
                    emptyList,
                    this.m_strVoteBanCommand,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "playername",
                            emptyList
                        ),
                        new MatchArgumentFormat(
                            "optional: reason",
                            emptyList
                        )
                    )
                )
            );

        }

        private void RegisterAllCommands()
        {

            if (this.m_isPluginEnabled == true)
            {
                List<string> scopes = this.Listify<string>(this.m_strPrivatePrefix, this.m_strAdminsPrefix, this.m_strPublicPrefix);
                List<string> emptyList = new List<string>();
                List<string> Arguments = new List<string>();
                Arguments.Add("Yes");
                Arguments.Add("No");
                Arguments.Add("Cancel");

                MatchCommand confirmationCommand = new MatchCommand(scopes, this.m_strConfirmCommand, this.Listify<MatchArgumentFormat>());

                this.RegisterCommand(
                    new MatchCommand(
                        "CPlayerVote",
                        "OnCommandVoteban",
                        scopes,
                        this.m_strVoteBanCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "playername",
                                this.GetSoldierNameKeys(this.m_dicPlayers)
                            ),
                            new MatchArgumentFormat(
                                "optional: reason",
                                emptyList)
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges"
                           ),
                        "Starts a Voteban against a Player"
                    )
                );
                //Votekick
                this.RegisterCommand(
                     new MatchCommand(
                         "CPlayerVote",
                         "OnCommandVotekick",
                         scopes,
                         this.m_strVoteKickCommand,
                         this.Listify<MatchArgumentFormat>(
                             new MatchArgumentFormat(
                                 "playername",
                                 this.GetSoldierNameKeys(this.m_dicPlayers)
                             ),
                             new MatchArgumentFormat(
                                 "optional: reason",
                                 emptyList)
                         ),
                         new ExecutionRequirements(
                             ExecutionScope.All,
                             2,
                             confirmationCommand,
                             "You do not have enough privileges"
                            ),
                         "Starts a Voteban against a Player"
                     )
                 );

                //Vote
                this.RegisterCommand(
                    new MatchCommand("CPlayerVote", "OnCommandVote",
                        scopes,
                        this.m_strVoteCommand,
                        this.Listify<MatchArgumentFormat>(
                            new MatchArgumentFormat(
                                "decision",
                                Arguments
                            )
                        ),
                        new ExecutionRequirements(
                            ExecutionScope.All,
                            2,
                            confirmationCommand,
                            "You do not have enough privileges"
                           ),
                        "Vote Yes or No"
                    )
                );

            }
        }
        #endregion

        #region IPRoConPluginInterface
        /*=======ProCon Events========*/

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {
            this.RegisterAllCommands();
            if (this.m_dicPbInfo.ContainsKey(cpbiPlayer.SoldierName) == false)
            {
                this.m_dicPbInfo.Add(cpbiPlayer.SoldierName, cpbiPlayer);
            }
        }

        // Query Events
        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.serverName = csiServerInfo.ServerName;
            this.ServerInfo = csiServerInfo;
            this.m_Voteban_reqVotes = this.calcReqVotes(this.m_Voteban_percent, csiServerInfo.PlayerCount);
            this.DebugInfo("Playervotes to Ban: " + this.m_Voteban_reqVotes);
            this.m_Votekick_reqVotes = this.calcReqVotes(this.m_Votekick_percent, csiServerInfo.PlayerCount);
            this.DebugInfo("Playervotes to Kick: " + this.m_Votekick_reqVotes);
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
                }
                this.lstPlayerList = lstPlayers;
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnListPlayers: " + c);
            }
        }

        #endregion

        #region IPRoConPluginInterface2

        //
        // IPRoConPluginInterface2
        //

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {
            this.RegisterAllCommands();
        }


        public override void OnRoundOverPlayers(List<CPlayerInfo> lstPlayers)
        {
            this.Spamprotection.Reset();
        }

        #endregion

        #region In Game Commands

        public void OnCommandVoteban(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            try
            {
                List<string> lstMsg = new List<string>();
                this.DebugInfo("OnCommandVoteban");
                if (this.Voteobj.Voteactiv == true || capCommand.MatchedArguments[0] == null || this.lstPlayerList.Count < this.m_Voteban_minplayers || (this.m_Voteban == enumBoolYesNo.No) == true)
                {
                    if (this.Voteobj.Voteactiv == true)
                    {
                        lstMsg.Add("You can not start a vote while a vote is in process");
                        this.ServerMessagesToPlayer(lstMsg, strSpeaker);
                    }
                    return;
                }
                if (this.m_playerwhitelist.Contains(capCommand.MatchedArguments[0].Argument) == true || this.m_clanwhitelist.Contains(this.m_dicPlayers[capCommand.MatchedArguments[0].Argument].ClanTag) == true)
                {
                    lstMsg.Add("You can not start a vote against this Player ([" + this.m_dicPlayers[capCommand.MatchedArguments[0].Argument].ClanTag + "] " + capCommand.MatchedArguments[0].Argument + ")");
                    this.ServerMessagesToPlayer(lstMsg, strSpeaker);
                    if (this.m_performAction == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", strSpeaker, "You tried to start a vote against a protected player or clantag!");
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Kicked Player: " + strSpeaker + " - " + "You tried to start a vote against a protected player or clantag!");
                    }
                    return;
                }
                if (this.Spamprotection.isAllowed(strSpeaker) == false)
                {
                    //Player is blocked
                    return;
                }
                else
                {
                    //Start a Vote
                    this.Voteobj = new CVote("Voteban", capCommand.MatchedArguments[0].Argument, strSpeaker, this.m_Voteban_reqVotes);
                    //You started a vote against Player ...
                    lstMsg = this.msg_StartVote;
                    lstMsg = ListReplace(lstMsg, "%target%", capCommand.MatchedArguments[0].Argument);
                    lstMsg = ListReplace(lstMsg, "%starter%", strSpeaker);
                    lstMsg = ListReplace(lstMsg, "%req%", this.m_Voteban_reqVotes.ToString());
                    lstMsg = ListReplace(lstMsg, "%votetype%", "Voteban");
                    this.ServerMessagesToAll(lstMsg);
                    this.ExecuteCommand("procon.protected.tasks.add", "CPlayerVote", (this.m_Voteban_voteduration * 60).ToString(), "1", "1", "procon.protected.plugins.call", "CPlayerVote", "getFinalResult");
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnCommandVoteban: " + c);
            }
        }

        public void OnCommandVotekick(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            try
            {
                List<string> lstMsg = new List<string>();
                this.DebugInfo("OnCommandVotekick");
                if (this.Voteobj.Voteactiv == true || capCommand.MatchedArguments[0] == null || this.lstPlayerList.Count < this.m_Votekick_minplayers || (this.m_Votekick == enumBoolYesNo.No) == true)
                {
                    if (this.Voteobj.Voteactiv == true)
                    {
                        lstMsg.Add("You can not start a vote while a vote is in process");
                        this.ServerMessagesToPlayer(lstMsg, strSpeaker);
                    }
                    return;
                }
                if (this.m_playerwhitelist.Contains(capCommand.MatchedArguments[0].Argument) == true || this.m_clanwhitelist.Contains(this.m_dicPlayers[capCommand.MatchedArguments[0].Argument].ClanTag) == true)
                {
                    lstMsg.Add("You can not start a vote against this Player ([" + this.m_dicPlayers[capCommand.MatchedArguments[0].Argument].ClanTag + "] " + capCommand.MatchedArguments[0].Argument + ")");
                    this.ServerMessagesToPlayer(lstMsg, strSpeaker);
                    if (this.m_performAction == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", strSpeaker, "You tried to start a vote against a protected player or clantag!");
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Kicked Player: " + strSpeaker + " - " + "You tried to start a vote against a protected player or clantag!");
                    }
                    return;
                }
                if (this.Spamprotection.isAllowed(strSpeaker) == false)
                {
                    //Player is blocked
                    return;
                }
                else
                {
                    //Start a Vote
                    this.Voteobj = new CVote("Votekick", capCommand.MatchedArguments[0].Argument, strSpeaker, this.m_Votekick_reqVotes);
                    //You started a vote against Player ...
                    lstMsg = this.msg_StartVote;
                    lstMsg = ListReplace(lstMsg, "%target%", capCommand.MatchedArguments[0].Argument);
                    lstMsg = ListReplace(lstMsg, "%starter%", strSpeaker);
                    lstMsg = ListReplace(lstMsg, "%req%", this.m_Votekick_reqVotes.ToString());
                    lstMsg = ListReplace(lstMsg, "%votetype%", "Votekick");
                    this.ServerMessagesToAll(lstMsg);
                    this.ExecuteCommand("procon.protected.tasks.add", "CPlayerVote", (this.m_Votekick_voteduration * 60).ToString(), "1", "1", "procon.protected.plugins.call", "CPlayerVote", "getFinalResult");
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnCommandVotekick: " + c);
            }
        }

        public void OnCommandVote(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            try
            {
                List<string> Playermsg = new List<string>();
                this.DebugInfo("OnCommandVote");
                if (this.Voteobj.Voteactiv == true && capCommand.MatchedArguments[0] != null && this.Voteobj.VoteResults.ContainsKey(strSpeaker) == false)
                {
                    string vote = capCommand.MatchedArguments[0].Argument;
                    bool successful = false;
                    switch (vote.ToUpper())
                    {
                        case "NO":
                            successful = this.Voteobj.playerVote(strSpeaker, false);
                            break;

                        case "YES":
                            successful = this.Voteobj.playerVote(strSpeaker, true);
                            break;

                        case "CANCEL":
                            if (String.Equals(strSpeaker, this.Voteobj.Starter) == true)
                            {
                                //Cancel the vote
                                Playermsg.Add("[SAY]The Vote has been canceled!");
                                this.ServerMessagesToAll(Playermsg);
                                this.ExecuteCommand("procon.protected.tasks.remove", "CPlayerVote");
                            }
                            break;

                        default:
                            break;
                    }
                    if (successful)
                    {
                        //Send Text to player
                        Playermsg.Add("[SAY]Your vote has been accepted!");
                        this.ServerMessagesToPlayer(Playermsg, strSpeaker);
                    }
                    else
                    {
                        Playermsg.Add("[SAY]You have already voted!");
                        this.ServerMessagesToPlayer(Playermsg, strSpeaker);
                    }
                    this.getIntermediateResult();
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnCommandVote: " + c);
            }
        }

        #endregion

        #region  Methodes 

        public List<string> ListReplace(List<string> targetlist, string wordToReplace, string replacement)
        {
            List<string> lstResult = new List<string>();
            foreach (string substring in targetlist)
            {
                lstResult.Add(substring.Replace(wordToReplace, replacement));
            }
            return lstResult;
        }

        public List<string> GetSoldierNameKeys(Dictionary<string, CPlayerInfo> playerdic)
        {
            List<string> soldierNames = new List<string>();
            foreach (KeyValuePair<string, CPlayerInfo> player in playerdic)
            {
                soldierNames.Add(player.Key);
            }
            return soldierNames;
        }

        public void DebugInfo(string DebugMessage)
        {
            if (this.m_debugmessages == enumBoolYesNo.Yes)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", DebugMessage);
            }
        }

        public void ServerMessagesToAll(List<string> lstMessage)
        {
            int delay = 1;
            foreach (string line in lstMessage)
            {
                if (line.Contains("[Yell]") == true)
                {
                    this.DebugInfo("Yell" + line);
                    this.ExecuteCommand("procon.protected.tasks.add", "CPlayerVote", delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", line.Replace("[Yell]", ""), "5000", "all");
                    delay = delay + 5;
                }
                else if (line.Contains("[Say]") == true)
                {
                    this.DebugInfo("Say" + line);
                    this.ExecuteCommand("procon.protected.send", "admin.say", line.Replace("[Say]", ""), "all");
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", line, "all");
                }
            }
        }

        public void ServerMessagesToPlayer(List<string> lstMessage, string strSoldierName)
        {
            int delay = 1;
            foreach (string line in lstMessage)
            {
                if (line.Contains("[Yell]") == true)
                {
                    this.DebugInfo("Yell" + line);
                    this.ExecuteCommand("procon.protected.tasks.add", "CPlayerVote", delay.ToString(), "1", "1", "procon.protected.send", "admin.yell", line.Replace("[Yell]", ""), "5000", "player", strSoldierName);
                    delay = delay + 5;
                }
                else if (line.Contains("[Say]") == true)
                {
                    this.DebugInfo("Say" + line);
                    this.ExecuteCommand("procon.protected.send", "admin.say", line.Replace("[Say]", ""), "player", strSoldierName);
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", line, "player", strSoldierName);
                }
            }
        }

        public void RemovePlayerfromServer(string targetSoldierName, string strReason)
        {
            try
            {
                if (targetSoldierName == string.Empty)
                {
                    return;
                }
                string strRemoveMethode = "";
                if (string.Equals(this.Voteobj.VoteType, "Voteban"))
                {
                    strRemoveMethode = "Nameban";
                }
                else
                {
                    strRemoveMethode = "Kick";
                }
                switch (strRemoveMethode)
                {
                    case "Kick":
                        this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", targetSoldierName, strReason);
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Kicked Player: " + targetSoldierName + " - " + strReason);
                        break;
                    /*
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
	        		*/
                    case "Nameban":
                        this.ExecuteCommand("procon.protected.send", "banList.add", "name", targetSoldierName, "perm", strReason);
                        this.ExecuteCommand("procon.protected.send", "banList.save");
                        this.ExecuteCommand("procon.protected.send", "banList.list");
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Nameban for Player: " + targetSoldierName + " - " + strReason);
                        break;
                    case "Warn":
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Warning Player: " + targetSoldierName + " - " + strReason);
                        break;
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in RemovePlayerfromServer: " + c);
            }
        }

        public void getIntermediateResult()
        {
            int[] resultarray = new int[2];
            resultarray = this.Voteobj.getVoteResult();
            if (resultarray == null)
            {
                return;
            }
            List<string> lstMsg = new List<string>();
            lstMsg = this.msg_VoteResults;
            lstMsg = ListReplace(lstMsg, "%yes%", resultarray[1].ToString());
            lstMsg = ListReplace(lstMsg, "%no%", resultarray[0].ToString());
            lstMsg = ListReplace(lstMsg, "%total%", (resultarray[0] + resultarray[1]).ToString());
            this.ServerMessagesToAll(lstMsg);
        }

        public void getFinalResult()
        {
            int[] resultarray = new int[3];
            resultarray = this.Voteobj.getFinalResult();
            if (resultarray == null)
            {
                return;
            }
            List<string> lstMsg = new List<string>();
            lstMsg = this.msg_VoteResults;
            lstMsg = ListReplace(lstMsg, "%yes%", resultarray[1].ToString());
            lstMsg = ListReplace(lstMsg, "%no%", resultarray[0].ToString());
            lstMsg = ListReplace(lstMsg, "%total%", (resultarray[0] + resultarray[1]).ToString());
            this.ServerMessagesToAll(lstMsg);
            if (resultarray[2] == 1)
            {
                //Vote Successful
                lstMsg = this.msg_VoteSuccessful;
                this.RemovePlayerfromServer(this.Voteobj.TargetPlayer, this.m_Reason.Replace("%votetype%", this.Voteobj.VoteType));
            }
            else
            {
                //Vote Failed
                lstMsg = this.msg_VoteFailed;
            }
            lstMsg = ListReplace(lstMsg, "%votetype%", this.Voteobj.VoteType);
            lstMsg = ListReplace(lstMsg, "%target%", this.Voteobj.TargetPlayer);
            this.ServerMessagesToAll(lstMsg);
            this.Voteobj.Voteactiv = false;
        }

        public int calcReqVotes(double percent, int playercount)
        {
            int result = playercount;
            result = Convert.ToInt32(Math.Round(Convert.ToDouble(playercount) * (percent / 100)));
            return result;
        }
    }
    #endregion

    #region Classes
    /*==========Classes========*/

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
            if (this.dicplayer.ContainsKey(strSpeaker) == true)
            {
                int i = this.dicplayer[strSpeaker];
                if (0 >= i)
                {
                    //Player is blocked
                    result = false;
                    this.dicplayer[strSpeaker]--;
                }
                else
                {
                    //Player is not blocked
                    result = true;
                    this.dicplayer[strSpeaker]--;
                }
            }
            else
            {
                this.dicplayer.Add(strSpeaker, this._allowedRequests);
                result = true;
                this.dicplayer[strSpeaker]--;
            }
            return result;
        }

        public void Reset()
        {
            this.dicplayer.Clear();
        }
    }

    class CVote
    {
        private bool _voteactiv;
        private bool _voteOver;
        private Dictionary<string, bool> _VoteResults = new Dictionary<string, bool>();
        private string _targetPlayer;
        private string _starter;
        private int _reqVotes;
        private int _minVoters;
        private TimeSpan _voteDuration;
        private string _voteType;

        public bool Voteactiv
        {
            get { return _voteactiv; }
            set { _voteactiv = value; }
        }

        public string Starter
        {
            get { return _starter; }
        }

        public string VoteType
        {
            get { return _voteType; }
        }

        public string TargetPlayer
        {
            get { return _targetPlayer; }
        }

        public Dictionary<string, bool> VoteResults
        {
            get { return _VoteResults; }
            set { _VoteResults = value; }
        }

        public CVote(bool voteactiv)
        {
            this._voteactiv = voteactiv;
            this._starter = "";
            this._reqVotes = 0;
            this._minVoters = 0;
            this._voteOver = true;
            this._voteType = "";
        }


        public CVote(string VoteType, string targetPlayer, string starter, int reqVotes)
        {
            this._targetPlayer = targetPlayer;
            this._starter = starter;
            this._reqVotes = reqVotes;
            this._voteactiv = true;
            this._voteOver = false;
            _VoteResults = new Dictionary<string, bool>();
            _VoteResults.Add(starter, true);
            this._voteType = VoteType;
        }


        public bool playerVote(string SoldierName, bool theirVote)
        {
            bool boolResult = false;
            if (this._VoteResults.ContainsKey(SoldierName) == true)
            {
                boolResult = false;
            }
            else
            {
                this._VoteResults.Add(SoldierName, theirVote);
                boolResult = true;
            }
            return boolResult;
        }
        public int[] getVoteResult()
        {
            int[] result = new int[2];
            lock (this)
            {
                result[0] = 0; //voted No
                result[1] = 0; //voted Yes
                foreach (KeyValuePair<string, bool> kvp in this._VoteResults)
                {
                    if (this._VoteResults[kvp.Key])
                    {
                        result[1]++;    //voted Yes
                    }
                    else
                    {
                        result[0]++;    //voted No
                    }
                }
            }
            return result;
        }

        public int[] getFinalResult()
        {
            int[] result = new int[3];
            lock (this)
            {
                result[0] = 0;  //voted No
                result[1] = 0;  //voted Yes
                foreach (KeyValuePair<string, bool> kvp in this._VoteResults)
                {
                    if (this._VoteResults[kvp.Key])
                    {
                        result[1]++;    //voted Yes
                    }
                    else
                    {
                        result[0]++;    //voted No
                    }
                }
                this._voteOver = false;
            }
            if (this._VoteResults.Count >= this._minVoters && result[1] >= this._reqVotes)
            {
                result[2] = 1; // Vote Successful Player get kicked or banned
            }
            else
            {
                result[2] = 0; // Vote unsuccessful -> Not engough player voted or the req. amount of votes has not been reached 
            }
            return result;
        }
    }

    #endregion
}