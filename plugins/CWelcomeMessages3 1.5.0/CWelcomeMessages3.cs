/*  Copyright 2010 [LAB]HeliMagnet (Gerry Wohlrab)

    http://www.luckyatbingo.net

    This file is part of [LAB]HeliMagnet's Plugins for BFBC2 PRoCon.

    [LAB]HeliMagnet's Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    [LAB]HeliMagnet's Plugins for BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with [LAB]HeliMagnet's Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CWelcomeMessages3 : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private List<string> m_strMessage;                              // Message list for visitors
        private List<string> m_strMessageRegulars;                      // Message list for regulars
        private List<string> m_regularMembers;                          // List of regulars on your server
        private List<string> m_strMessageClan;                          // Message list for clan members
        private List<string> m_adminTagList;                            // List of tags for admins
        private List<string> m_clanTagList;                             // List of tags for clan members
        private List<string> m_visitTagList;                            // List of tags for visitors
        private List<string> m_clanMembers;                             // List of clan members
        private List<string> m_strMessageAdmin;                         // Message list for admins
        private List<string> m_strPersonalMessages;                     // Message for a particular player

        private Dictionary<string, int> returnVisitors;
        private int timesBeforeRegular;

        private string m_strPlayer = "";                                // Get string of current player joining
        private string m_strPlayerTags = "";                            // Get tags of current player joining

        private int m_iDisplayTime;
        private int m_iDelayTime;
        private int m_iDelayBetweenMessages;

        private enumBoolYesNo m_displayConsole;                         // Display player joins in console?
        private enumBoolYesNo m_displayClanWithAdmin;                   // Display both clan and admin messages to admins?
        private enumBoolYesNo m_enYellResponses;

        public CWelcomeMessages3()
        {
            this.m_strMessage = new List<string>();
            this.m_strMessage.Add("Welcome to our server %pn%!");

            this.m_strMessageRegulars = new List<string>();
            this.m_strMessageRegulars.Add("Welcome back %pn%!");

            this.m_regularMembers = new List<string>();
            this.m_regularMembers.Add("HeliMagnet");

            this.m_strMessageClan = new List<string>();
            this.m_strMessageClan.Add("Did you vote on the new rotation yet?");
            this.m_strMessageClan.Add("Should we let Jim in the clan?");

            this.m_adminTagList = new List<string>();
            this.m_adminTagList.Add("!LAB!");
            this.m_clanTagList = new List<string>();
            this.m_clanTagList.Add("LAB");
            this.m_visitTagList = new List<string>();
            this.m_visitTagList.Add("LA");
            this.m_clanMembers = new List<string>();
            this.m_clanMembers.Add("HeliMagnet");

            this.m_strMessageAdmin = new List<string>();
            this.m_strMessageAdmin.Add("Please warn before kicking!");

            this.m_iDisplayTime = 8000;
            this.m_iDelayTime = 60;

            this.m_displayConsole = enumBoolYesNo.No;
            this.m_displayClanWithAdmin = enumBoolYesNo.No;
            this.m_enYellResponses = enumBoolYesNo.No;

            this.m_iDelayBetweenMessages = 5;

            this.m_strPersonalMessages = new List<string>();
            this.m_strPersonalMessages.Add("HeliMagnet//This message is for the one and only HeliMagnet!");

            this.returnVisitors = new Dictionary<string, int>();
            this.timesBeforeRegular = 10;
        }

        public string GetPluginName()
        {
            return "Player Based Welcoming";
        }

        public string GetPluginVersion()
        {
            return "1.5.0.0";
        }

        public string GetPluginAuthor()
        {
            return "HeliMagnet";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            return @"
<p>For support or to post comments regarding this plugin please visit <a href=""http://phogue.net/forum/viewtopic.php?f=13&t=367"" target=""_blank"">Plugin Thread</a></p>

<p>This plugin works with PRoCon and falls under the GNU GPL, please read the included gpl.txt for details.
I have decided to work on PRoCon plugins without compensation. However, if you would like to donate to support the development of PRoCon, click the link below:
<a href=""http://phogue.net"" target=""_new""><img src=""http://i398.photobucket.com/albums/pp65/scoop27585/phoguenetdonate.png"" width=""482"" height=""84"" border=""0"" alt=""PRoCon Donation"" /></a></p>
<p>Toward the right side of the page, there is a location to enter the amount you would like to donate and whether you want the donation to be made public. Your donations are greatly appreciated and will be sent to Phogue (original creator of PRoCon).</p>
<h2>Description</h2>
<p>Shows a list of messages to a player after they have joined. Has five message options: one for admins, one for clan members, one for regulars that are not clan members, one for visitors (not in 'Regulars' list, 'Clan Members' list, and are not admins), and personalized messages for individual players. Looks at admin list, clan member list, regulars list, and personalized list in ProCon to decide which message to give the player. You can separate multiple messages which are displayed one after the other by putting them on a new line.
Version 1.5.0.0 added personal player messages that overwrite admin/clan/regular messages.</p>

<h2>Commands</h2>
<p>There are no commands needed for this plugin</p>

<h2>Settings</h2>
    <h3>Communication</h3>
		<blockquote><h4>Yell Welcome Messages?</h4>Setting to true will yell to all players (center flashing text) or setting to false will display in chat.</blockquote>
		<blockquote><h4>Show Message (seconds)</h4>If yell is set to true, the number of seconds the yell will display.</blockquote>
	<h3>Console</h3>
		<blockquote><h4>Display Joining Players?</h4>Display joining players and their class (admin, clan member, regular, visitor, personal message) in the plugin console on connect.</blockquote>
	<h3>Message</h3>
		<blockquote><h4>Message to New Visitors</h4>Specific message(s) displayed to visitors.</blockquote>
		<blockquote><h4>Message to Regulars</h4>Specific message(s) displayed to regulars.</blockquote>
		<blockquote><h4>Regulars List</h4>List of player names that are regulars to the server.</blockquote>
		<blockquote><h4>Regular Tags</h4>List of tags that are considered regulars and will display regulars messages.</blockquote>
		<blockquote><h4>Message to Clan Members</h4>Specific message(s) displayed to clan members.</blockquote>
		<blockquote><h4>Clan Members</h4>List of player names that are clan members to the server.</blockquote>
		<blockquote><h4>Clan Tags</h4>List of tags that are considered clan members and will display clan member messages.</blockquote>
		<blockquote><h4>Message to Admins</h4>Specific message(s) displayed to admins.</blockquote>
		<blockquote><h4>Admin Tags</h4>List of tags that are considered admins and will display admin messages.</blockquote>
		<blockquote><h4>Personal Player Messages</h4>Holds specific player name and personalized message only for that player.</blockquote>
		<blockquote><h4>Admin and Clan Messages to Admins?</h4>Display both admin and clan messages to admins connecting? Yes will, no doesn't.</blockquote>
	<h3>Timing</h3>
		<blockquote><h4>Delay Before Welcome (seconds)</h4>Time in seconds before the first message is displayed to the connecting player (make sure it is long enough for slower loading computers).</blockquote>
		<blockquote><h4>Delay Between Messages (seconds)</h4>Time in seconds in between successive messages (if there is more than one).</blockquote>
	<h3>Return Visitors</h3>
		<blockquote><h4>Times Before Visitor Becomes Regular</h4>Number of times a visitor must connect before they are considered a regular.</blockquote>
<h2>Updates / Change Log</h2>
<h3>Version 1.4.0.0 --> 1.5.0.0</h3>
	<h4><ul><li>Added personalized messages with syntax: player name // personal message</li>
		<li>Added counter and value to check if connecting player has visited the server enough to be considered a regular</li></ul></h4>
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
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPlayer Based Welcome Messages ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPlayer Based Welcome Messages ^1Disabled =(");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Communication| Yell Welcome Messages?", typeof(enumBoolYesNo), this.m_enYellResponses));
            if (this.m_enYellResponses == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Communication|Show Message (seconds)", this.m_iDisplayTime.GetType(), this.m_iDisplayTime / 1000));
            }

            lstReturn.Add(new CPluginVariable("Console|Display Joining Players?", typeof(enumBoolYesNo), this.m_displayConsole));
            lstReturn.Add(new CPluginVariable("Message|Message to New Visitors", typeof(string[]), this.m_strMessage.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Message to Regulars", typeof(string[]), this.m_strMessageRegulars.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Regulars List", typeof(string[]), this.m_regularMembers.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Regular Tags", typeof(string[]), this.m_visitTagList.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Message to Clan Members", typeof(string[]), this.m_strMessageClan.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Clan Members", typeof(string[]), this.m_clanMembers.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Clan Tags", typeof(string[]), this.m_clanTagList.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Message to Admins", typeof(string[]), this.m_strMessageAdmin.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Admin Tags", typeof(string[]), this.m_adminTagList.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Personal Player Messages", typeof(string[]), this.m_strPersonalMessages.ToArray()));
            lstReturn.Add(new CPluginVariable("Message|Admin and Clan Messages to Admins?", typeof(enumBoolYesNo), this.m_displayClanWithAdmin));

            lstReturn.Add(new CPluginVariable("Timing|Delay Before Welcome (seconds)", this.m_iDelayTime.GetType(), this.m_iDelayTime));
            lstReturn.Add(new CPluginVariable("Timing|Delay Between Messages (seconds)", this.m_iDelayBetweenMessages.GetType(), this.m_iDelayBetweenMessages));

            lstReturn.Add(new CPluginVariable("Return Visitors|Times Before Visitor Becomes Regular", this.timesBeforeRegular.GetType(), this.timesBeforeRegular.ToString()));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Display Joining Players?", typeof(enumBoolYesNo), this.m_displayConsole));
            lstReturn.Add(new CPluginVariable("Message to New Visitors", typeof(string[]), this.m_strMessage.ToArray()));
            lstReturn.Add(new CPluginVariable("Message to Regulars", typeof(string[]), this.m_strMessageRegulars.ToArray()));
            lstReturn.Add(new CPluginVariable("Regulars List", typeof(string[]), this.m_regularMembers.ToArray()));
            lstReturn.Add(new CPluginVariable("Message to Clan Members", typeof(string[]), this.m_strMessageClan.ToArray()));
            lstReturn.Add(new CPluginVariable("Admin Tags", typeof(string[]), this.m_adminTagList.ToArray()));
            lstReturn.Add(new CPluginVariable("Clan Tags", typeof(string[]), this.m_clanTagList.ToArray()));
            lstReturn.Add(new CPluginVariable("Regular Tags", typeof(string[]), this.m_visitTagList.ToArray()));
            lstReturn.Add(new CPluginVariable("Clan Members", typeof(string[]), this.m_clanMembers.ToArray()));
            lstReturn.Add(new CPluginVariable("Message to Admins", typeof(string[]), this.m_strMessageAdmin.ToArray()));
            lstReturn.Add(new CPluginVariable("Personal Player Messages", typeof(string[]), this.m_strPersonalMessages.ToArray()));
            lstReturn.Add(new CPluginVariable("Admin and Clan Messages to Admins?", typeof(enumBoolYesNo), this.m_displayClanWithAdmin));
            lstReturn.Add(new CPluginVariable(" Yell Welcome Messages?", typeof(enumBoolYesNo), this.m_enYellResponses));
            lstReturn.Add(new CPluginVariable("Show Message (seconds)", this.m_iDisplayTime.GetType(), this.m_iDisplayTime / 1000));
            lstReturn.Add(new CPluginVariable("Delay Before Welcome (seconds)", this.m_iDelayTime.GetType(), this.m_iDelayTime));
            lstReturn.Add(new CPluginVariable("Delay Between Messages (seconds)", this.m_iDelayBetweenMessages.GetType(), this.m_iDelayBetweenMessages));
            lstReturn.Add(new CPluginVariable("Times Before Visitor Becomes Regular", this.timesBeforeRegular.GetType(), this.timesBeforeRegular.ToString()));
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iTimeSeconds = 8;

            if (strVariable.CompareTo("Message to New Visitors") == 0)
            {
                this.m_strMessage = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Display Joining Players?") == 0)
            {
                this.m_displayConsole = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Message to Regulars") == 0)
            {
                this.m_strMessageRegulars = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Regulars List") == 0)
            {
                this.m_regularMembers = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Message to Clan Members") == 0)
            {
                this.m_strMessageClan = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Admin Tags") == 0)
            {
                this.m_adminTagList = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Clan Tags") == 0)
            {
                this.m_clanTagList = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Regular Tags") == 0)
            {
                this.m_visitTagList = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Clan Members") == 0)
            {
                this.m_clanMembers = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Message to Admins") == 0)
            {
                this.m_strMessageAdmin = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Personal Player Messages") == 0)
            {
                this.m_strPersonalMessages = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Admin and Clan Messages to Admins?") == 0)
            {
                this.m_displayClanWithAdmin = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Delay Before Welcome (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iDelayTime = iTimeSeconds;
            }
            else if (strVariable.CompareTo(" Yell Welcome Messages?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enYellResponses = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Delay Between Messages (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iDelayBetweenMessages = iTimeSeconds;
            }
            else if (strVariable.CompareTo("Show Message (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iDisplayTime = iTimeSeconds * 1000;

                if (iTimeSeconds <= 0)
                {
                    this.m_iDisplayTime = 1000;
                }
                else if (iTimeSeconds > 60)
                {
                    this.m_iDisplayTime = 59999;
                }
            }
            else if (strVariable.CompareTo("Times Before Visitor Becomes Regular") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                if (iTimeSeconds > 0)
                {
                    this.timesBeforeRegular = iTimeSeconds;
                }
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
            CPrivileges cpSpeakerPrivs = this.GetAccountPrivileges(strSoldierName);
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "player", strSoldierName);
            Thread.Sleep(3000);
            List<string> lstMessages = new List<string>();
            string[] parsedMessages;
            bool personal = false;
            foreach (string personalMessage in this.m_strPersonalMessages)
            {
                parsedMessages = Regex.Split(personalMessage, "//");
                if (parsedMessages[0].Equals(strSoldierName))
                {
                    for (int i = 1; i < parsedMessages.Length; i++)
                    {
                        lstMessages.Add(parsedMessages[i]);
                    }
                    personal = true;
                    if (this.m_displayConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^5^bPlayer Joining: " + strSoldierName + " has a personal message");
                    }
                    break;
                }
            }
            if (personal == false)
            {
                bool adminMember = false;
                if (this.m_adminTagList.Count > 0)
                {
                    foreach (string tag in this.m_adminTagList)
                    {
                        if (tag.CompareTo(this.m_strPlayerTags) == 0)                           // Player joining has same tags as in list
                        {
                            adminMember = true;
                            break;
                        }
                    }
                }
                if ((cpSpeakerPrivs != null && cpSpeakerPrivs.PrivilegesFlags >= 8328) || adminMember == true)               // Admin
                {
                    lstMessages = new List<string>(this.m_strMessageAdmin);                         // Admin messages
                    if (this.m_displayClanWithAdmin == enumBoolYesNo.Yes)                           // Append admin list with clan list
                    {
                        List<string> lstClan = new List<string>(this.m_strMessageClan);             // Get messages to clan members
                        foreach (string clanMsg in lstClan)                                         // Iterate through all clan member messages
                        {
                            lstMessages.Add(clanMsg);                                               // Add to displayed message list
                        }
                    }
                    if (this.m_displayConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^5^bPlayer Joining: " + strSoldierName + " is an admin");
                    }
                }
                else
                {                                                                               // Not an admin, check if Clan Member
                    bool clanMember = false;
                    if (this.m_displayConsole == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^5^bPlayer's tags: " + this.m_strPlayerTags);
                    }
                    if (this.m_clanTagList.Count > 0)
                    {
                        foreach (string tag in this.m_clanTagList)
                        {
                            if (tag.CompareTo(this.m_strPlayerTags) == 0)                           // Player joining has same tags as in list
                            {
                                clanMember = true;
                                break;
                            }
                        }
                    }
                    if (clanMember == false)
                    {                                                   // Player's clan tags doesn't match, need to look in member name list
                        if (this.m_clanMembers.Count > 0)
                        {
                            foreach (string player in this.m_clanMembers)
                            {
                                if (player.CompareTo(strSoldierName) == 0)                      // Player joining is in clan member list
                                {
                                    clanMember = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (clanMember == true)
                    {
                        lstMessages = new List<string>(this.m_strMessageClan);
                        if (this.m_displayConsole == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^5^bPlayer Joining: " + strSoldierName + " is a clan member");
                        }
                    }
                    else
                    {                                                                          // Not a Clan Member, check if Regular
                        bool regular = false;
                        if (this.m_visitTagList.Count > 0)
                        {
                            foreach (string tag in this.m_visitTagList)
                            {
                                if (tag.CompareTo(this.m_strPlayerTags) == 0)                           // Player joining has same tags as in list
                                {
                                    regular = true;
                                    break;
                                }
                            }
                        }
                        if (regular == false)
                        {
                            if (this.m_regularMembers.Count > 0)
                            {
                                foreach (string player in this.m_regularMembers)
                                {
                                    if (player.CompareTo(strSoldierName) == 0)                      // Player joining is in regulars list
                                    {
                                        regular = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (regular == true)
                        {
                            lstMessages = new List<string>(this.m_strMessageRegulars);
                            if (this.m_displayConsole == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^5^bPlayer Joining: " + strSoldierName + " is a regular");
                            }
                        }
                        else
                        {
                            lstMessages = new List<string>(this.m_strMessage);
                            if (this.returnVisitors.ContainsKey(strSoldierName) == false)
                            {
                                this.returnVisitors.Add(strSoldierName, 1);         // Add them to the list that checks how many times they have visited the server
                            }
                            else
                            {
                                this.returnVisitors[strSoldierName]++;
                            }
                            if (this.returnVisitors[strSoldierName] >= this.timesBeforeRegular)
                            {               // Make the player a regular
                                this.m_regularMembers.Add(strSoldierName);
                                this.returnVisitors.Remove(strSoldierName);
                            }
                            if (this.m_displayConsole == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^5^bPlayer Joining: " + strSoldierName + " is a visitor");
                            }
                        }
                    }
                }
            }

            lstMessages.RemoveAll(String.IsNullOrEmpty);

            if (lstMessages.Count > 0)
            {
                int iDelay = this.m_iDelayTime;
                foreach (string strMessage in lstMessages)
                {
                    if (this.m_enYellResponses == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.tasks.add", "CWelcomeMessages3", iDelay.ToString(), "1", "1", "procon.protected.send", "admin.yell", strMessage.Replace("%pn%", strSoldierName), this.m_iDisplayTime.ToString(), "player", strSoldierName);
                        iDelay += (this.m_iDisplayTime / 1000) + this.m_iDelayBetweenMessages;
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.tasks.add", "CWelcomeMessages3", iDelay.ToString(), "1", "1", "procon.protected.send", "admin.say", strMessage.Replace("%pn%", strSoldierName), "player", strSoldierName);
                        iDelay += this.m_iDelayBetweenMessages;
                    }
                }
            }
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {

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
            if (cpsSubset.Subset.Equals(PRoCon.Core.CPlayerSubset.PlayerSubsetType.Player) == true)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (cpiPlayer.SoldierName.Equals(cpsSubset.SoldierName))
                    {
                        this.m_strPlayerTags = cpiPlayer.ClanTag;
                    }
                }
            }
        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

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