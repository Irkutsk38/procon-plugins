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
using System.Threading;
using System.Net;
using System.Net.Mail;

//Procon includes
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;


namespace PRoConEvents
{
    public class CEmailNotifier : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Variables and Constructor
        //Proconvariables
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;
        private bool m_isPluginEnabled;

        //Logging
        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();

        //Other
        private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();   //Players

        //Various Variables
        private string serverName;
        private CServerInfo ServerInfo;
        private List<CPlayerInfo> lstPlayerList;

        //Ingame Commands
        private string m_strPrivatePrefix;
        private string m_strAdminsPrefix;
        private string m_strPublicPrefix;
        //Command Strings
        private string m_strAdminCommand;
        private string m_strReportCommand;
        private string m_strConfirmCommand;
        private string m_strCancelCommand;

        //Email Variables
        private enumBoolYesNo m_enSSL;
        private string str_smtpserver;
        private int intSmtpPort;
        private string str_senderEmailadress;
        private string str_receiverEmailadress;
        private string str_emailuser;
        private string str_emailpassword;

        //StatusMail
        private enumBoolYesNo m_enStatusMail;
        private int StatusMailInterval; // How often a Statusmail will sent in minutes
        private string strStatusMailSubject;
        private List<string> m_lstStatusMailBody = new List<string>();

        //Reportmail
        private enumBoolYesNo m_enReportMail;
        private string strReportMailSubject;
        private List<string> m_lstReportMailBody = new List<string>();

        //Admin Mail
        private enumBoolYesNo m_enAdminMail;
        private string strAdminMailSubject;
        private List<string> m_lstAdminMailBody = new List<string>();

        //Spamprotection
        private int numberOfAllowedRequests;
        private CSpamprotection Spamprotection;

        //Reciepient
        private List<string> m_mail_TO = new List<string>();
        private List<string> m_mail_CC = new List<string>();
        private List<string> m_mail_BCC = new List<string>();



        public CEmailNotifier()
        {
            //SMTP-Server
            this.str_smtpserver = "";
            this.intSmtpPort = 25;
            this.str_senderEmailadress = "";
            this.str_emailuser = "";
            this.str_emailpassword = "";
            this.m_enSSL = enumBoolYesNo.No;
            this.m_strHostName = "";
            this.m_strPort = "";
            this.m_strPRoConVersion = "";

            //Reciepient
            this.m_mail_TO = new List<string>();
            this.m_mail_CC = new List<string>();
            this.m_mail_BCC = new List<string>();

            //Statusmail
            this.m_enStatusMail = enumBoolYesNo.No;
            this.StatusMailInterval = 30;
            this.strStatusMailSubject = "Serverstatus for %ServerName% Time: %Now%";
            //Body
            this.m_lstStatusMailBody = new List<string>();
            this.m_lstStatusMailBody.Add("<H3>Summary for %ServerName%</H3>");
            this.m_lstStatusMailBody.Add("Connection State: %ConnectionState%<br>");
            this.m_lstStatusMailBody.Add("Player: %PlayerCount% / %MaxPlayerCount%<br>");
            this.m_lstStatusMailBody.Add("Map: %Map% <br>");
            this.m_lstStatusMailBody.Add("Gamemode: %Gamemode%<br>");
            this.m_lstStatusMailBody.Add("%Playertable%");

            //Reportmail
            this.m_enReportMail = enumBoolYesNo.No;
            this.strReportMailSubject = "A Player has been reported on Server: %ServerName% Time: %Now%";
            //Body
            this.m_lstReportMailBody = new List<string>();
            this.m_lstReportMailBody.Add("%Player% has report %ReportedPlayer% <br>");
            this.m_lstReportMailBody.Add("Reason: %reason%<br> ");
            this.m_lstReportMailBody.Add("PB_GUID: %R_PBGUID%<br>");
            this.m_lstReportMailBody.Add("EA_GUID: %R_EAGUID%<br>");
            this.m_lstReportMailBody.Add("<br> %Playertable%");

            //AdminMail
            this.m_enAdminMail = enumBoolYesNo.No;
            this.strAdminMailSubject = "Your Server needs attention: %ServerName% Time: %Now%";
            //Body
            this.m_lstAdminMailBody = new List<string>();
            this.m_lstAdminMailBody.Add("%Player% requests an Admin<br>");
            this.m_lstAdminMailBody.Add("Reason: %reason%<br>");
            this.m_lstAdminMailBody.Add("<br> %Playertable%");


            this.m_isPluginEnabled = false;
            this.lstPlayerList = new List<CPlayerInfo>();
            //Scope
            this.m_strPrivatePrefix = "@";
            this.m_strAdminsPrefix = "#";
            this.m_strPublicPrefix = "!";
            //Command
            this.m_strReportCommand = "report";
            this.m_strAdminCommand = "admin";
            this.m_strConfirmCommand = "yes";
            this.m_strCancelCommand = "cancel";
            //Spamprotection
            numberOfAllowedRequests = 1; //Number of allowed Requests per Round
            this.Spamprotection = new CSpamprotection(numberOfAllowedRequests);


        }
        #endregion

        #region PluginSetup
        public string GetPluginName()
        {
            return "PRoCon Email Notifier";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.3";
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
<p><form action='https://www.paypal.com/cgi-bin/webscr' target='_blank' method='post'>
<input type='hidden' name='cmd' value='_s-xclick'>
<input type='hidden' name='hosted_button_id' value='3B2FEDDHHWUW8'>
<input type='image' src='https://www.paypal.com/en_US/i/btn/btn_donate_SM.gif' border='0' name='submit' alt='PayPal - The safer, easier way to pay online!'>
<img alt='' border='0' src='https://www.paypal.com/de_DE/i/scr/pixel.gif' width='1' height='1'>
</form></p>


   
<h2>Description</h2>
    <p>This plugin is used to sent Email to various Events.</p>
    <p>This plugin is capable to sent a Email to a list of adresses defined in the TO:, CC: or BCC: lists</p>
    
    
<h2>Requirements</h2>
	<p>PRoCon Sandbox must be disabled</p>
	<p>For SSL/TLS right Port must be selected(Example: Port 587 for SSL) if SSL is off the Port is standard SMTP Port: 25</p>
	<p>Pls Give FEEDBACK !!!</p>

<h2>Installation</h2>
<p>Download and install this plugin</p>
<p>Turn the Procon Sandbox off</p>
<p>Enter your credentials and setup the email adresses</p>

<h2>Ingame Commands</h2>
	<blockquote><h4>[@,#,!]report PlayerName [Reason] Send an EMail to an Admin with the Reported Player and who has report him.</h4></blockquote>
	<blockquote><h4>[@,#,!]admin[Reason] Send an EMail to an Admin.</h4></blockquote>

<h2>Replacement Strings for Statusmail</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%ServerName%</td><td>Will be replaced with the Server Name.</td></tr>
	<tr><td>%Now%</td><td>Will be replaced with the Time the Email sent in hh:mm:ss format.</td></tr>
	<tr><td>%ConnectionState%</td><td>Will be replaced with the Connectionstats of the Server.</td></tr>
	<tr><td>%PlayerCount%</td><td>Will be replaced with the Server concurrent PlayerCount.</td></tr>
	<tr><td>%MaxPlayerCount%</td><td>Will be replaced with the Server concurrent MaxPlayerCount.</td></tr>
	<tr><td>%Map%</td><td>Will be replaced with the Servers concurrent Map.</td></tr>
	<tr><td>%Gamemode%</td><td>Will be replaced with the Servers concurrent Gamemode.</td></tr>
	<tr><td>%Playertable%</td><td>Will be replaced with a table of all Players on the Server.</td></tr>
	</table>
	<br>
	
	<h2>Replacement Strings for Reportmail</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%ServerName%</td><td>Will be replaced with the Server Name.</td></tr>
	<tr><td>%Now%</td><td>Will be replaced with the Time the Email sent in hh:mm:ss format.</td></tr>
	<tr><td>%ConnectionState%</td><td>Will be replaced with the Connectionstats of the Server.</td></tr>
	<tr><td>%PlayerCount%</td><td>Will be replaced with the Server concurrent PlayerCount.</td></tr>
	<tr><td>%MaxPlayerCount%</td><td>Will be replaced with the Server concurrent MaxPlayerCount.</td></tr>
	<tr><td>%Map%</td><td>Will be replaced with the Servers concurrent Map.</td></tr>
	<tr><td>%Gamemode%</td><td>Will be replaced with the Servers concurrent Gamemode.</td></tr>
	<tr><td>%PlayerName%</td><td>Will be replaced with Name of the Player who sent the request.</td></tr>
	<tr><td>%ReportedPlayer%</td><td>Will be replaced with the Reported PlayerName.</td></tr>
	<tr><td>%R_PBGUID%</td><td>Will be replaced with the ReportedPlayer's PBGUID.</td></tr>
	<tr><td>%R_EAGUID%</td><td>Will be replaced with the ReportedPlayer's EAGUID.</td></tr>
	<tr><td>%reason%</td><td>Will be replaced with the Reason given by the Player.</td></tr>
	<tr><td>%Playertable%</td><td>Will be replaced with a table of all Players on the Server.</td></tr>
	</table>
	<br>
	
	<h2>Replacement Strings for Adminmail</h2>
	
	<table border ='1'>
	<tr><th>Replacement String</th><th>Effect</th></tr>
	<tr><td>%ServerName%</td><td>Will be replaced with the Server Name.</td></tr>
	<tr><td>%Now%</td><td>Will be replaced with the Time the Email sent in hh:mm:ss format.</td></tr>
	<tr><td>%PlayerCount%</td><td>Will be replaced with the Server concurrent PlayerCount.</td></tr>
	<tr><td>%MaxPlayerCount%</td><td>Will be replaced with the Server concurrent MaxPlayerCount.</td></tr>
	<tr><td>%Map%</td><td>Will be replaced with the Servers concurrent Map.</td></tr>
	<tr><td>%Gamemode%</td><td>Will be replaced with the Servers concurrent Gamemode.</td></tr>
	<tr><td>%PlayerName%</td><td>Will be replaced with Name of the Player who sent the request.</td></tr>
	<tr><td>%^reason%</td><td>Will be replaced with the Reason given by the Player.</td></tr>
	<tr><td>%Playertable%</td><td>Will be replaced with a table of all Players on the Server.</td></tr>
	</table>
	<br>
<h3>Known issues:</h3>
<p> -none</p>

<h3>Plans for future release:</h3>
<p>more Ingame Commands</p>
		
<h3>Changelog:</h3><br>
1.0.0.3<br>
Added multiply Receiver options: TO, CC and BCC <br><br>

1.0.0.2<br>
InGame Commands

1.0.0.1<br>
Implemented SSL/TLS<br><br>

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
                                                     "OnPlayerLeft", "OnRoundOverPlayers", "OnCommandReport", "OnCommandAdmin");
        }



        public void OnPluginEnable()
        {

            this.serverName = "";
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon E-Mail Notifier ^2Enabled");
            this.ExecuteCommand("procon.protected.tasks.add", "CEmailNotifier", "30", (StatusMailInterval * 60).ToString(), "-1", "procon.protected.plugins.call", "CEmailNotifier", "ServerstatusEmail");
            // Register Commands
            this.m_isPluginEnabled = true;
            this.Spamprotection = new CSpamprotection(numberOfAllowedRequests);
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon E-Mail Notifier: ^2 Spamprotection set to " + this.numberOfAllowedRequests.ToString() + " Request per Round for each Player");
            this.RegisterAllCommands();
        }

        public void OnPluginDisable()
        {

            //this.ExecuteCommand("procon.protected.tasks.remove", "CEmailNotifier");
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPRoCon E-Mail Notifier ^1Disabled");
            //Unregister Commands
            this.m_isPluginEnabled = false;
            this.UnregisterAllCommands();
            this.ExecuteCommand("procon.protected.tasks.remove", "CEmailNotifier");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("SMTP-Server Details|Hostaddress", this.str_smtpserver.GetType(), this.str_smtpserver));
            lstReturn.Add(new CPluginVariable("SMTP-Server Details|Port", this.str_smtpserver.GetType(), this.intSmtpPort));
            lstReturn.Add(new CPluginVariable("SMTP-Server Details|Enable SSL/TLS?", typeof(enumBoolYesNo), this.m_enSSL));
            lstReturn.Add(new CPluginVariable("SMTP-Server Details|Username", this.str_emailuser.GetType(), this.str_emailuser));
            lstReturn.Add(new CPluginVariable("SMTP-Server Details|Password", this.str_emailpassword.GetType(), this.str_emailpassword));
            lstReturn.Add(new CPluginVariable("SMTP-Server Details|Emailadress", this.str_senderEmailadress.GetType(), this.str_senderEmailadress));
            lstReturn.Add(new CPluginVariable("Receiver Details|TO:", typeof(string[]), this.m_mail_TO.ToArray()));
            lstReturn.Add(new CPluginVariable("Receiver Details|CC:", typeof(string[]), this.m_mail_CC.ToArray()));
            lstReturn.Add(new CPluginVariable("Receiver Details|BCC:", typeof(string[]), this.m_mail_BCC.ToArray()));
            //Statusmail
            lstReturn.Add(new CPluginVariable("Statusmail|Enable Serverstatus Emails?", typeof(enumBoolYesNo), this.m_enStatusMail));
            if (this.m_enStatusMail == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Statusmail|Subject", this.strStatusMailSubject.GetType(), this.strStatusMailSubject));
                lstReturn.Add(new CPluginVariable("Statusmail|Mailbody", typeof(string[]), this.m_lstStatusMailBody.ToArray()));
                lstReturn.Add(new CPluginVariable("Statusmail|Interval", this.StatusMailInterval.GetType(), this.StatusMailInterval));
            }
            //Reportmail
            lstReturn.Add(new CPluginVariable("Reportmail|Enable Report Emails?", typeof(enumBoolYesNo), this.m_enReportMail));
            if (this.m_enReportMail == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Reportmail|Report Subject", this.strReportMailSubject.GetType(), this.strReportMailSubject));
                lstReturn.Add(new CPluginVariable("Reportmail|Report Mailbody", typeof(string[]), this.m_lstReportMailBody.ToArray()));
            }
            //Adminmail
            lstReturn.Add(new CPluginVariable("Adminmail|Enable Admin Emails?", typeof(enumBoolYesNo), this.m_enAdminMail));
            if (this.m_enAdminMail == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Adminmail|Admin Subject", this.strAdminMailSubject.GetType(), this.strAdminMailSubject));
                lstReturn.Add(new CPluginVariable("Adminmail|Admin Mailbody", typeof(string[]), this.m_lstAdminMailBody.ToArray()));
            }
            //Spamprotection
            lstReturn.Add(new CPluginVariable("Spamprotection|Playerrequests per Round", this.numberOfAllowedRequests.GetType(), this.numberOfAllowedRequests));
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Hostaddress", this.str_smtpserver.GetType(), this.str_smtpserver));
            lstReturn.Add(new CPluginVariable("Port", this.str_smtpserver.GetType(), this.intSmtpPort));
            lstReturn.Add(new CPluginVariable("Enable SSL/TLS?", typeof(enumBoolYesNo), this.m_enSSL));
            lstReturn.Add(new CPluginVariable("Username", this.str_emailuser.GetType(), this.str_emailuser));
            lstReturn.Add(new CPluginVariable("Password", this.str_emailpassword.GetType(), this.str_emailpassword));
            lstReturn.Add(new CPluginVariable("Emailadress", this.str_senderEmailadress.GetType(), this.str_senderEmailadress));
            lstReturn.Add(new CPluginVariable("TO:", typeof(string[]), this.m_mail_TO.ToArray()));
            lstReturn.Add(new CPluginVariable("CC:", typeof(string[]), this.m_mail_CC.ToArray()));
            lstReturn.Add(new CPluginVariable("BCC:", typeof(string[]), this.m_mail_BCC.ToArray()));
            //Statusmail
            lstReturn.Add(new CPluginVariable("Enable Serverstatus Emails?", typeof(enumBoolYesNo), this.m_enStatusMail));
            if (this.m_enStatusMail == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Subject", this.strStatusMailSubject.GetType(), this.strStatusMailSubject));
                lstReturn.Add(new CPluginVariable("Mailbody", typeof(string[]), this.m_lstStatusMailBody.ToArray()));
                lstReturn.Add(new CPluginVariable("Interval", this.StatusMailInterval.GetType(), this.StatusMailInterval));
            }
            //Reportmail
            lstReturn.Add(new CPluginVariable("Enable Report Emails?", typeof(enumBoolYesNo), this.m_enReportMail));
            if (this.m_enReportMail == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Report Subject", this.strStatusMailSubject.GetType(), this.strReportMailSubject));
                lstReturn.Add(new CPluginVariable("Report Mailbody", typeof(string[]), this.m_lstReportMailBody.ToArray()));
            }
            //Adminmail
            lstReturn.Add(new CPluginVariable("Enable Admin Emails?", typeof(enumBoolYesNo), this.m_enAdminMail));
            if (this.m_enAdminMail == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Admin Subject", this.strAdminMailSubject.GetType(), this.strAdminMailSubject));
                lstReturn.Add(new CPluginVariable("Admin Mailbody", typeof(string[]), this.m_lstAdminMailBody.ToArray()));
            }
            lstReturn.Add(new CPluginVariable("Playerrequests per Round", this.numberOfAllowedRequests.GetType(), this.numberOfAllowedRequests));
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            //SMTP-Server
            if (strVariable.CompareTo("Hostaddress") == 0)
            {
                this.str_smtpserver = strValue;
            }
            else if (strVariable.CompareTo("Port") == 0 && Int32.TryParse(strValue, out this.intSmtpPort) == true)
            {
                this.intSmtpPort = Convert.ToInt32(strValue);
            }
            else if (strVariable.CompareTo("Enable SSL/TLS?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enSSL = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Username") == 0)
            {
                this.str_emailuser = strValue;
            }
            else if (strVariable.CompareTo("Password") == 0)
            {
                this.str_emailpassword = strValue;
            }
            else if (strVariable.CompareTo("Emailadress") == 0)
            {
                this.str_senderEmailadress = strValue;
            }
            else if (strVariable.CompareTo("TO:") == 0)
            {
                this.m_mail_TO = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("CC:") == 0)
            {
                this.m_mail_CC = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("BCC:") == 0)
            {
                this.m_mail_BCC = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }

            //Statusmail
            else if (strVariable.CompareTo("Enable Serverstatus Emails?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enStatusMail = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Subject") == 0)
            {
                this.strStatusMailSubject = strValue;
            }
            else if (strVariable.CompareTo("Mailbody") == 0)
            {
                this.m_lstStatusMailBody = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Interval") == 0 && Int32.TryParse(strValue, out this.StatusMailInterval) == true)
            {
                this.StatusMailInterval = Convert.ToInt32(strValue);
            }
            //Reportmail
            else if (strVariable.CompareTo("Enable Report Emails?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enReportMail = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Report Subject") == 0)
            {
                this.strReportMailSubject = strValue;
            }
            else if (strVariable.CompareTo("Report Mailbody") == 0)
            {
                this.m_lstReportMailBody = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            //Adminmail
            else if (strVariable.CompareTo("Enable Admin Emails?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enAdminMail = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Admin Subject") == 0)
            {
                this.strAdminMailSubject = strValue;
            }
            else if (strVariable.CompareTo("Admin Mailbody") == 0)
            {
                this.m_lstAdminMailBody = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Playerrequests per Round") == 0 && Int32.TryParse(strValue, out numberOfAllowedRequests) == true)
            {
                this.numberOfAllowedRequests = Convert.ToInt32(strValue);
                this.Spamprotection = new CSpamprotection(numberOfAllowedRequests);
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
                    this.m_strReportCommand,
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

                MatchCommand confirmationCommand = new MatchCommand(scopes, this.m_strConfirmCommand, this.Listify<MatchArgumentFormat>());
                /*               
                if(true){
                	this.RegisterCommand(new MatchCommand("CEmailNotifier", "OnCommandEmail", this.Listify<string>("@", "!", "#"), "email", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Sends a Test-Email"));
                }
                else{
                	this.UnregisterCommand(new MatchCommand("CEmailNotifier", "OnCommandEmail", this.Listify<string>("@", "!", "#"), "email", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Sends a Test-Email"));
                }
                */
                this.RegisterCommand(
                    new MatchCommand(
                        "CEmailNotifier",
                        "OnCommandReport",
                        scopes,
                        this.m_strReportCommand,
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
                            "You do not have enough privileges to report players"
                           ),
                        "Reports a player with an optional reason via Email"
                    )
                );
                //Adminmail
                this.RegisterCommand(
                     new MatchCommand(
                         "CEmailNotifier",
                         "OnCommandAdmin",
                         scopes,
                         this.m_strAdminCommand,
                         this.Listify<MatchArgumentFormat>(
                             new MatchArgumentFormat(
                                 "optional: reason",
                                 emptyList)
                         ),
                         new ExecutionRequirements(
                             ExecutionScope.All,
                             2,
                             confirmationCommand,
                             "You do not have enough privileges to report players"
                            ),
                         "Will send an Email to an Admin"
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

        #region IPRoConPluginInterface3

        //
        #endregion


        #region In Game Commands
        public void OnCommandEmail(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            // none
        }

        public void OnCommandReport(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.Spamprotection.isAllowed(strSpeaker) == false)
            {
                //Player is blocked
                return;
            }
            string reason = "";
            try
            {
                if (this.m_enReportMail == enumBoolYesNo.Yes && capCommand.MatchedArguments[0] != null)
                {

                    reason = strText;
                    string reportedPlayer = capCommand.MatchedArguments[0].Argument;
                    //this.DebugInfo(reportedPlayer);
                    string subject = this.strReportMailSubject;
                    subject = subject.Replace("%ServerName%", ServerInfo.ServerName);
                    subject = subject.Replace("%Now%", DateTime.Now.ToString());
                    subject = subject.Replace("%Player%", strSpeaker);
                    subject = subject.Replace("%ReportedPlayer%", reportedPlayer);
                    subject = subject.Replace("%ConnectionState%", ServerInfo.ConnectionState);
                    subject = subject.Replace("%PlayerCount%", ServerInfo.PlayerCount.ToString());
                    subject = subject.Replace("%MaxPlayerCount%", ServerInfo.MaxPlayerCount.ToString());
                    subject = subject.Replace("%Map%", ServerInfo.Map);
                    subject = subject.Replace("%Gamemode%", ServerInfo.GameMode);
                    subject = subject.Replace("%reason%", reason);
                    if (this.m_dicPlayers.ContainsKey(reportedPlayer) == true && this.m_dicPbInfo.ContainsKey(reportedPlayer) == true)
                    {
                        subject = subject.Replace("%R_PBGUID%", this.m_dicPbInfo[reportedPlayer].GUID);
                        subject = subject.Replace("%R_EAGUID%", this.m_dicPlayers[reportedPlayer].GUID);
                    }
                    string body = "";
                    foreach (string entry in this.m_lstReportMailBody)
                    {
                        body = string.Concat(body, entry);
                    }
                    body = body.Replace("%ServerName%", ServerInfo.ServerName);
                    body = body.Replace("%Now%", DateTime.Now.ToString());
                    body = body.Replace("%Player%", strSpeaker);
                    body = body.Replace("%ReportedPlayer%", reportedPlayer);
                    body = body.Replace("%ConnectionState%", ServerInfo.ConnectionState);
                    body = body.Replace("%PlayerCount%", ServerInfo.PlayerCount.ToString());
                    body = body.Replace("%MaxPlayerCount%", ServerInfo.MaxPlayerCount.ToString());
                    body = body.Replace("%Map%", ServerInfo.Map);
                    body = body.Replace("%Gamemode%", ServerInfo.GameMode);
                    body = body.Replace("%reason%", reason);
                    if (this.m_dicPlayers.ContainsKey(reportedPlayer) == true && this.m_dicPbInfo.ContainsKey(reportedPlayer) == true)
                    {
                        body = body.Replace("%R_PBGUID%", this.m_dicPbInfo[reportedPlayer].GUID);
                        body = body.Replace("%R_EAGUID%", this.m_dicPlayers[reportedPlayer].GUID);
                    }
                    string playertable = "";
                    playertable = string.Concat(playertable, "<table border='1'> <tr><th>ClanTag</th><th>Playername</th><th>Score</th><th>Kills</th><th>Deaths</th><th>KDR</th><th>Ping</th></tr>");
                    foreach (CPlayerInfo playerinfo in lstPlayerList)
                    {
                        playertable = string.Concat(playertable, "<tr><td>" + playerinfo.ClanTag + "</td><td>" + playerinfo.SoldierName + "</td><td>" + playerinfo.Score + "</td><td>" + playerinfo.Kills + "</td><td>" + playerinfo.Deaths + "</td><td>" + playerinfo.Kdr + "</td><td>" + playerinfo.Ping + "</td></tr>");
                    }
                    playertable = string.Concat(playertable, "</table>");

                    body = body.Replace("%Playertable%", playertable);
                    this.Emailsender(subject, body);
                    this.ExecuteCommand("procon.protected.send", "admin.say", "The Player has been reported to an Admin", "player", strSpeaker);
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnCommandReport: " + c);
            }
        }

        public void OnCommandAdmin(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.Spamprotection.isAllowed(strSpeaker) == false)
            {
                //Player is blocked
                return;
            }
            try
            {
                string reason = "";
                if (this.m_enAdminMail == enumBoolYesNo.Yes)
                {
                    reason = strText;
                    string subject = this.strAdminMailSubject;
                    subject = subject.Replace("%ServerName%", ServerInfo.ServerName);
                    subject = subject.Replace("%Now%", DateTime.Now.ToString());
                    subject = subject.Replace("%Player%", strSpeaker);
                    subject = subject.Replace("%reason%", reason);
                    subject = subject.Replace("%ConnectionState%", ServerInfo.ConnectionState);
                    subject = subject.Replace("%PlayerCount%", ServerInfo.PlayerCount.ToString());
                    subject = subject.Replace("%MaxPlayerCount%", ServerInfo.MaxPlayerCount.ToString());
                    subject = subject.Replace("%Map%", ServerInfo.Map);
                    subject = subject.Replace("%Gamemode%", ServerInfo.GameMode);
                    string body = "";
                    foreach (string entry in this.m_lstAdminMailBody)
                    {
                        body = string.Concat(body, entry);
                    }
                    body = body.Replace("%ServerName%", ServerInfo.ServerName);
                    body = body.Replace("%Now%", DateTime.Now.ToString());
                    body = body.Replace("%Player%", strSpeaker);
                    body = body.Replace("%reason%", reason);
                    body = body.Replace("%ConnectionState%", ServerInfo.ConnectionState);
                    body = body.Replace("%PlayerCount%", ServerInfo.PlayerCount.ToString());
                    body = body.Replace("%MaxPlayerCount%", ServerInfo.MaxPlayerCount.ToString());
                    body = body.Replace("%Map%", ServerInfo.Map);
                    body = body.Replace("%Gamemode%", ServerInfo.GameMode);
                    string playertable = "";
                    playertable = string.Concat(playertable, "<table border='1'> <tr><th>ClanTag</th><th>Playername</th><th>Score</th><th>Kills</th><th>Deaths</th><th>KDR</th><th>Ping</th></tr>");
                    foreach (CPlayerInfo playerinfo in lstPlayerList)
                    {
                        playertable = string.Concat(playertable, "<tr><td>" + playerinfo.ClanTag + "</td><td>" + playerinfo.SoldierName + "</td><td>" + playerinfo.Score + "</td><td>" + playerinfo.Kills + "</td><td>" + playerinfo.Deaths + "</td><td>" + playerinfo.Kdr + "</td><td>" + playerinfo.Ping + "</td></tr>");
                    }
                    playertable = string.Concat(playertable, "</table>");
                    body = body.Replace("%Playertable%", playertable);
                    this.Emailsender(subject, body);
                    this.ExecuteCommand("procon.protected.send", "admin.say", "Your Request has been sent to an Admin", "player", strSpeaker);
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in OnCommandAdmin: " + c);
            }
        }

        #endregion

        #region  Methodes 

        public void ServerstatusEmail()
        {
            if (this.m_enStatusMail == enumBoolYesNo.Yes)
            {
                string body = "";
                string subject = this.strStatusMailSubject;

                //Subject
                subject = subject.Replace("%ServerName%", ServerInfo.ServerName);
                subject = subject.Replace("%Now%", DateTime.Now.ToString());
                subject = subject.Replace("%ConnectionState%", ServerInfo.ConnectionState);
                subject = subject.Replace("%PlayerCount%", ServerInfo.PlayerCount.ToString());
                subject = subject.Replace("%MaxPlayerCount%", ServerInfo.MaxPlayerCount.ToString());
                subject = subject.Replace("%Map%", ServerInfo.Map);
                subject = subject.Replace("%Gamemode%", ServerInfo.GameMode);
                //End of Subject

                //Body
                foreach (string entry in m_lstStatusMailBody)
                {
                    body = string.Concat(body, entry);
                }
                body = body.Replace("%ServerName%", ServerInfo.ServerName);
                body = body.Replace("%Now%", DateTime.Now.ToString());
                body = body.Replace("%ConnectionState%", ServerInfo.ConnectionState);
                body = body.Replace("%PlayerCount%", ServerInfo.PlayerCount.ToString());
                body = body.Replace("%MaxPlayerCount%", ServerInfo.MaxPlayerCount.ToString());
                body = body.Replace("%Map%", ServerInfo.Map);
                body = body.Replace("%Gamemode%", ServerInfo.GameMode);

                string playertable = "";
                playertable = string.Concat(playertable, "<table border='1'> <tr><th>ClanTag</th><th>Playername</th><th>Score</th><th>Kills</th><th>Deaths</th><th>KDR</th><th>Ping</th></tr>");
                foreach (CPlayerInfo playerinfo in lstPlayerList)
                {
                    playertable = string.Concat(playertable, "<tr><td>" + playerinfo.ClanTag + "</td><td>" + playerinfo.SoldierName + "</td><td>" + playerinfo.Score + "</td><td>" + playerinfo.Kills + "</td><td>" + playerinfo.Deaths + "</td><td>" + playerinfo.Kdr + "</td><td>" + playerinfo.Ping + "</td></tr>");
                }
                playertable = string.Concat(playertable, "</table>");
                body = body.Replace("%Playertable%", playertable);
                //End of Body

                this.Emailsender(subject, body);
            }
        }

        public void Emailsender(string Subject, string Emailbody)
        {
            try
            {
                //create the mail message
                using (MailMessage mail = new MailMessage())
                {
                    //set the addresses
                    if (str_senderEmailadress == null || String.Equals(str_senderEmailadress, "") == true)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in Emailsender: Sender is empty!");
                        return;
                    }
                    mail.From = new MailAddress(str_senderEmailadress);
                    if (this.m_mail_TO.Count > 0)
                    {
                        foreach (string TO in this.m_mail_TO)
                        {
                            if (TO.Contains("@"))
                            {
                                mail.To.Add(TO);
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in Emailsender: The TO-list contains a invaild entry: " + TO);
                                return;
                            }
                        }
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in Emailsender: The TO-list is empty!");
                        return;
                    }
                    if (this.m_mail_CC.Count > 0)
                    {
                        foreach (string CC in this.m_mail_CC)
                        {
                            if (CC.Contains("@"))
                                mail.CC.Add(CC);
                        }
                    }
                    if (this.m_mail_BCC.Count > 0)
                    {
                        foreach (string BCC in this.m_mail_BCC)
                        {
                            if (BCC.Contains("@"))
                                mail.Bcc.Add(BCC);
                        }
                    }

                    //set the content
                    mail.Subject = Subject;
                    mail.Body = Emailbody;
                    mail.IsBodyHtml = true; //Set to true for html Email

                    //send the message
                    SmtpClient smtp = new SmtpClient(this.str_smtpserver, this.intSmtpPort);
                    if (this.m_enSSL == enumBoolYesNo.Yes)
                    {
                        smtp.EnableSsl = true;
                    }
                    else
                    {
                        smtp.EnableSsl = false;
                    }
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    //to authenticate we set the username and password properites on the SmtpClient
                    smtp.Credentials = new NetworkCredential(this.str_emailuser, this.str_emailpassword);
                    smtp.Send(mail);
                    //this.DebugInfo("Email sent");
                }
            }
            catch (Exception c)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^1Error in Emailsender(Procon plugin sandbox turned off??, check your credentials): " + c);
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
            if (true)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", DebugMessage);
            }
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

    #endregion
}