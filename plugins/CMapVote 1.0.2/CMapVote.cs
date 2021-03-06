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
using System.Timers;
using System.Windows.Forms;
using System.Net;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CMapVote : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private string m_strPreviousMessage;

        // Map Voting Options //
        private string m_strSelectedAllMaps;
        private string m_strSelectedConfirmList;
        private bool m_boolBeginningYell;
        private bool m_showMessage; 				// Couldn't think of a better way to prevent the spamming when the plugin is loaded
        private string m_yellEveryPrompt;
        private string m_strVoteCommand;
        private string m_strCancelVoteCommand;
        private double m_votingDuration;
        private int m_gametypeWinner;
        private int m_currentPlayerCount;
        private int m_conquestRounds;
        private int m_rushRounds;
        private int m_sqdmRounds;
        private int m_sqrushRounds;
        private int m_votingOption;
        private int m_votePrompts;
        private Dictionary<string, int> m_gametype = new Dictionary<string, int>();
        private Dictionary<string, int> m_maps = new Dictionary<string, int>();
        private Dictionary<int, int> currentVoting = new Dictionary<int, int>();
        private List<string> m_gametypeList = new List<string>();
        private List<string> m_conquestList = new List<string>();
        private List<string> m_rushList = new List<string>();
        private List<string> m_sqdmList = new List<string>();
        private List<string> m_sqrushList = new List<string>();
        private int m_currentRound;
        private int m_maxRound;
        private Thread t;
        private string m_strCurrentGame;
        private string m_strCurrentMap;

        private Dictionary<string, List<string>> acceptableEntries = new Dictionary<string, List<string>>();
        // Map Voting Options //

        private string m_strPrivatePrefix;
        private string m_strAdminsPrefix;
        private string m_strPublicPrefix;

        public CMapVote()
        {

            this.m_strSelectedAllMaps = "";
            this.m_strSelectedConfirmList = "";
            this.m_boolBeginningYell = true;
            this.m_strPreviousMessage = "";
            this.m_showMessage = false;

            this.m_yellEveryPrompt = "Every Prompt";
            this.m_strVoteCommand = "vote";
            this.m_strCancelVoteCommand = "cancelvote";
            this.t = new Thread(new ParameterizedThreadStart(manageVoting));
            this.m_votingDuration = 12;
            this.m_gametypeWinner = -1;
            this.m_currentPlayerCount = 0;
            this.m_conquestRounds = 1;
            this.m_rushRounds = 2;
            this.m_sqdmRounds = 1;
            this.m_sqrushRounds = 2;
            this.m_votingOption = 0;
            this.m_votePrompts = 3;
            this.m_gametypeList.Add("cq");
            this.m_conquestList.Add("levels/mp_001");
            this.m_rushList.Add("levels/mp_002");
            this.m_sqdmList.Add("levels/mp_004sdm");
            this.m_sqrushList.Add("levels/mp_005sr");

            this.m_strPrivatePrefix = "@";
            this.m_strAdminsPrefix = "#";
            this.m_strPublicPrefix = "!";
        }

        public string GetPluginName()
        {
            return "Map Voting";
        }

        public string GetPluginVersion()
        {
            return "1.0.2.0";
        }

        public string GetPluginAuthor()
        {
            return "[LAB]HeliMagnet";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            return @"
<p>For support or to post comments regarding this plugin please visit <a href=""http://phogue.net/forum/viewtopic.php?f=13&t=794"" target=""_blank"">Plugin Thread</a></p>

<p>This plugin works with PRoCon and falls under the GNU GPL, please read the included gpl.txt for details.
I have decided to work on PRoCon plugins without compensation. However, if you would like to donate to support the development of PRoCon, click the link below:
<a href=""http://phogue.net"" target=""_new""><img src=""http://i398.photobucket.com/albums/pp65/scoop27585/phoguenetdonate.png"" width=""482"" height=""84"" border=""0"" alt=""PRoCon Donation"" /></a></p>
<p>Toward the right side of the page, there is a location to enter the amount you would like to donate and whether you want the donation to be made public. Your donations are greatly appreciated and will be sent to Phogue (original creator of PRoCon).</p>
<h2>Description</h2>
<p>Allow players the opportunity to vote on the next gametype and map.</p>

<h2>Commands</h2>
		<blockquote><h4>Vote Command</h4>@< Vote Command > #.</blockquote>
        <blockquote><h4>Cancel Vote Command</h4>@< Cancel Command ></blockquote>

<h2>Settings</h2>
    <h3>Miscellaneous</h3>
        <blockquote><h4>Vote Command</h4>The string players on the server need to enter before their vote option (@<Vote Command> #).</blockquote>
        <blockquote><h4>Cancel Vote Command</h4>The string players on the server need to enter to cancel their vote.</blockquote>
        <blockquote><h4>Full Map List</h4>Select the gametype - map pair to add or remove (will automatically include the gametype and maps in the appropriate lists).</blockquote>
        <blockquote><h4>Add or Remove Map in List</h4>Sets or removes gametype - map pair.</blockquote>
        <blockquote><h4>Gametype List</h4>List of gametypes you want players to vote on.</blockquote>
        <blockquote><h4>Conquest Voting Possibilities</h4>List of Conquest maps you want players to vote on.</blockquote>
        <blockquote><h4>Rush Voting Possibilities</h4>List of Rush maps you want players to vote on.</blockquote>
        <blockquote><h4>Squad Deathmatch Voting Possibilities</h4>List of Squad Deathmatch maps you want players to vote on.</blockquote>
        <blockquote><h4>Squad Rush Voting Possibilities</h4>List of Squad Rush maps you want players to vote on.</blockquote>
        <blockquote><h4>Vote Duration</h4>How long (in minutes) each vote should take before final tally.</blockquote>
        <blockquote><h4>Number of Vote Prompts</h4>How many prompts should occur during voting.</blockquote>
        <blockquote><h4>Yell For Votes</h4>Should the prompt to vote be yelled at the beginning of the voting or every time a prompt occurs?</blockquote>
        <blockquote><h4>Number of Conquest Rounds</h4>If Conquest gametype wins, how many rounds each map should play.</blockquote>
        <blockquote><h4>Number of Rush Rounds</h4>If Rush gametype wins, how many rounds each map should play.</blockquote>
        <blockquote><h4>Number of Squad Deathmatch Rounds</h4>If Squad Deathmatch gametype wins, how many rounds each map should play.</blockquote>
        <blockquote><h4>Number of Squad Rush Rounds</h4>If Squad Rush gametype wins, how many rounds each map should play.</blockquote>
<h2>Updates / Change Log</h2>
<h3>Version 1.0.1.0 --> 1.0.2.0</h3>
		<h4><ul><li>Improved gametype and map checking in lists. External file is read - no need for software updates.</li></ul></h4>
<h3>Version 1.0.0.0 --> 1.0.1.0</h3>
		<h4><ul><li>Fixed problem with new maps not displaying properly.</li></ul></h4>
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
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMap Voting ^2Enabled!");
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            WebClient client = new WebClient();
            Stream strm = client.OpenRead("http://www.phogue.net/procon/developers/helimagnet/Map Vote/List Options.txt");
            StreamReader sr = new StreamReader(strm);
            string option = "";
            string line = " ";
            List<string> newList = new List<string>();
            do
            {
                line = sr.ReadLine();
                if (line.Equals("END"))
                {
                    break;
                }
                if (line.LastIndexOf("//") != -1)
                {                               // Option delimiter
                    option = line.Substring(line.LastIndexOf("//") + 2);
                }
                else
                {                                                           // Map name
                    if (this.acceptableEntries.ContainsKey(option) == false)
                    {   // New entry
                        newList = new List<string>();
                        newList.Add(line);
                        this.acceptableEntries.Add(option, newList);
                    }
                    else
                    {
                        newList = this.acceptableEntries[option];
                        newList.Add(line);
                        this.acceptableEntries[option] = newList;
                    }
                }
            } while (line != null);
            strm.Close();
            /*List<string> keys = new List<string>(this.acceptableEntries.Keys);
            foreach(string key in keys) {
            	this.ExecuteCommand("procon.protected.pluginconsole.write", "------------------------");
            	this.ExecuteCommand("procon.protected.pluginconsole.write", key + " values:");
            	foreach(string map in this.acceptableEntries[key]) {
            		this.ExecuteCommand("procon.protected.pluginconsole.write", map);
            	}
            }*/
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMap Voting ^1Disabled =(");
            this.m_votingOption = 0;
            this.m_gametype.Clear();
            this.m_maps.Clear();
            if (this.t.IsAlive)
            {
                try
                {
                    this.t.Abort();
                }
                catch (Exception)
                {
                    // "We don't care about this exception
                }
            }
        }

        private void promptUsers()
        {
            if (this.m_votingOption == 1)
            {
                foreach (string player in this.m_gametype.Keys)
                {
                    if (this.m_gametype[player] == 0)
                    {
                        if (this.m_yellEveryPrompt.Equals("Every Prompt") || this.m_boolBeginningYell == true)
                        {
                            this.m_boolBeginningYell = false;
                            this.ExecuteCommand("procon.protected.send", "admin.yell", "Please vote on the next gametype", "5000", "player", player);
                        }
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Please vote on the next gametype (/@" + this.m_strVoteCommand + " #):", "player", player);
                        for (int i = 0; i < this.m_gametypeList.Count; i++)
                        {
                            if ((i + 2) <= this.m_gametypeList.Count)
                            {           // There are at least two more options to display
                                this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(this.m_gametypeList[i]) + "   " + (i + 2).ToString() + ": " + translateMapName(this.m_gametypeList[i + 1]), "player", player);
                                i++;
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(this.m_gametypeList[i]), "player", player);
                            }
                        }
                    }
                }
            }
            else if (this.m_votingOption == 2)
            {
                foreach (string player in this.m_maps.Keys)
                {
                    if (this.m_maps[player] == 0)
                    {
                        if (this.m_yellEveryPrompt.Equals("Every Prompt") || this.m_boolBeginningYell == true)
                        {
                            this.m_boolBeginningYell = false;
                            this.ExecuteCommand("procon.protected.send", "admin.yell", "Please vote on the next map", "5000", "player", player);
                        }
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Please vote on the next map (/@" + this.m_strVoteCommand + " #):", "player", player);
                        List<string> mapList = new List<string>();
                        switch (this.m_gametypeWinner)
                        {
                            case 0:
                                mapList = this.m_conquestList;
                                break;
                            case 1:
                                mapList = this.m_rushList;
                                break;
                            case 2:
                                mapList = this.m_sqdmList;
                                break;
                            case 3:
                                mapList = this.m_sqrushList;
                                break;
                        }
                        for (int i = 0; i < mapList.Count; i++)
                        {
                            if ((i + 2) <= mapList.Count)
                            {           // There are at least two more options to display
                                this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(mapList[i]) + "   " + (i + 2).ToString() + ": " + translateMapName(mapList[i + 1]), "player", player);
                                i++;
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(mapList[i]), "player", player);
                            }
                        }
                    }
                }
            }
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Vote Command", typeof(string), this.m_strVoteCommand));
            lstReturn.Add(new CPluginVariable("Cancel Vote Command", typeof(string), this.m_strCancelVoteCommand));
            lstReturn.Add(this.GetMapListPluginVariable("Full Map List", "BasicEnumExampleMapList", this.m_strSelectedAllMaps, "{GameMode} - {PublicLevelName}"));
            lstReturn.Add(new CPluginVariable("Add or Remove Map in List", "enum.AddRemoveMapVote(Add|Remove)", this.m_strSelectedConfirmList));
            string[] temp = new string[this.m_gametypeList.Count];
            for (int i = 0; i < this.m_gametypeList.Count; i++)
            {
                temp[i] = this.m_gametypeList[i];
            }
            lstReturn.Add(new CPluginVariable("Gametype List", temp.GetType(), temp));
            temp = new string[this.m_conquestList.Count];
            for (int i = 0; i < this.m_conquestList.Count; i++)
            {
                temp[i] = this.m_conquestList[i];
            }
            lstReturn.Add(new CPluginVariable("Conquest Voting Possibilities", temp.GetType(), temp));
            temp = new string[this.m_rushList.Count];
            for (int i = 0; i < this.m_rushList.Count; i++)
            {
                temp[i] = this.m_rushList[i];
            }
            lstReturn.Add(new CPluginVariable("Rush Voting Possibilities", temp.GetType(), temp));
            temp = new string[this.m_sqdmList.Count];
            for (int i = 0; i < this.m_sqdmList.Count; i++)
            {
                temp[i] = this.m_sqdmList[i];
            }
            lstReturn.Add(new CPluginVariable("Squad Deathmatch Voting Possibilities", temp.GetType(), temp));
            temp = new string[this.m_sqrushList.Count];
            for (int i = 0; i < this.m_sqrushList.Count; i++)
            {
                temp[i] = this.m_sqrushList[i];
            }
            lstReturn.Add(new CPluginVariable("Squad Rush Voting Possibilities", temp.GetType(), temp));

            lstReturn.Add(new CPluginVariable("Vote Duration", typeof(double), this.m_votingDuration));
            lstReturn.Add(new CPluginVariable("Number of Vote Prompts", this.m_votePrompts.GetType(), this.m_votePrompts));
            lstReturn.Add(new CPluginVariable("Yell For Votes", "enum.YellVotingMapVote(Every Prompt|Beginning Only)", this.m_yellEveryPrompt));
            lstReturn.Add(new CPluginVariable("Number of Conquest Rounds", this.m_conquestRounds.GetType(), this.m_conquestRounds));
            lstReturn.Add(new CPluginVariable("Number of Rush Rounds", this.m_rushRounds.GetType(), this.m_rushRounds));
            lstReturn.Add(new CPluginVariable("Number of Squad Deathmatch Rounds", this.m_sqdmRounds.GetType(), this.m_sqdmRounds));
            lstReturn.Add(new CPluginVariable("Number of Squad Rush Rounds", this.m_sqrushRounds.GetType(), this.m_sqrushRounds));
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            return this.GetDisplayPluginVariables();
            /*List<CPluginVariable> lstReturn = new List<CPluginVariable>();
        	
        	lstReturn.Add(new CPluginVariable("Vote Command", typeof(string), this.m_strVoteCommand));
            lstReturn.Add(new CPluginVariable("Cancel Vote Command", typeof(string), this.m_strCancelVoteCommand));
            lstReturn.Add(new CPluginVariable("Vote Duration", typeof(double), this.m_votingDuration));
            //lstReturn.Add(new CPluginVariable("Interval between vote prompt (minutes)", typeof(int), this.m_votePromptDelay));
            string[] temp = new string[this.m_gametypeList.Count];
            for(int i=0; i<this.m_gametypeList.Count; i++) {
            	temp[i] = this.m_gametypeList[i];
            }
            lstReturn.Add(new CPluginVariable("Gametype List", temp.GetType(), temp));
            temp = new string[this.m_conquestList.Count];
            for(int i=0; i<this.m_conquestList.Count; i++) {
            	temp[i] = this.m_conquestList[i];
            }
            lstReturn.Add(new CPluginVariable("Conquest Voting Possibilities", temp.GetType(), temp));
            temp = new string[this.m_rushList.Count];
            for(int i=0; i<this.m_rushList.Count; i++) {
            	temp[i] = this.m_rushList[i];
            }
            lstReturn.Add(new CPluginVariable("Rush Voting Possibilities", temp.GetType(), temp));
            temp = new string[this.m_sqdmList.Count];
            for(int i=0; i<this.m_sqdmList.Count; i++) {
            	temp[i] = this.m_sqdmList[i];
            }
            lstReturn.Add(new CPluginVariable("Squad Deathmatch Voting Possibilities", temp.GetType(), temp));
            temp = new string[this.m_sqrushList.Count];
            for(int i=0; i<this.m_sqrushList.Count; i++) {
            	temp[i] = this.m_sqrushList[i];
            }
            lstReturn.Add(new CPluginVariable("Squad Rush Voting Possibilities", temp.GetType(), temp));
            
            lstReturn.Add(new CPluginVariable("Number of Vote Prompts", this.m_votePrompts.GetType(), this.m_votePrompts));
            lstReturn.Add(new CPluginVariable("Yell at Every Prompt (yes), beginning only (no)", this.m_yellEveryPrompt.GetType(), this.m_yellEveryPrompt));
            
            lstReturn.Add(new CPluginVariable("Number of Conquest Rounds", this.m_conquestRounds.GetType(), this.m_conquestRounds));
            lstReturn.Add(new CPluginVariable("Number of Rush Rounds", this.m_rushRounds.GetType(), this.m_rushRounds));
            lstReturn.Add(new CPluginVariable("Number of Squad Deathmatch Rounds", this.m_sqdmRounds.GetType(), this.m_sqdmRounds));
            lstReturn.Add(new CPluginVariable("Number of Squad Rush Rounds", this.m_sqrushRounds.GetType(), this.m_sqrushRounds));
			return lstReturn;*/
        }

        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {

            double dur = 15;
            int prompts = 3;
            //bool badEntry = false;
            List<string> oldEntries = new List<string>();
            List<string> newEntries = new List<string>();

            if (strVariable.CompareTo("Yell For Votes") == 0)
            {
                this.m_yellEveryPrompt = strValue;
            }
            else if (strVariable.CompareTo("Vote Duration") == 0 && double.TryParse(strValue, out dur) == true)
            {
                if (dur > 0 && dur <= 100)
                {
                    this.m_votingDuration = dur;
                    if (this.m_votePrompts.Equals(null) == false && this.m_votePrompts > 0 && this.m_showMessage == true)
                    {
                        double promptDur = this.m_votingDuration / (double)this.m_votePrompts;
                        MessageBox.Show("Given your voting duration and number prompts,\n a vote prompt will occur every " + promptDur.ToString() + " minutes", "Map Voting Information");
                    }
                }
            }
            else if (strVariable.CompareTo("Number of Vote Prompts") == 0 && int.TryParse(strValue, out prompts) == true)
            {
                if (prompts > 0)
                {
                    this.m_votePrompts = prompts;
                    if (this.m_votePrompts.Equals(null) == false && this.m_votePrompts > 0)
                    {
                        if (this.m_showMessage == true)
                        {
                            double promptDur = this.m_votingDuration / (double)this.m_votePrompts;
                            MessageBox.Show("Given your voting duration and number prompts,\n a vote prompt will occur every " + promptDur.ToString() + " minutes", "Map Voting Information");
                        }
                    }
                    this.m_showMessage = true;
                }
            }
            else if (strVariable.CompareTo("Number of Conquest Rounds") == 0 && int.TryParse(strValue, out prompts) == true)
            {
                if (prompts > 0)
                {
                    this.m_conquestRounds = prompts;
                }
            }
            else if (strVariable.CompareTo("Number of Rush Rounds") == 0 && int.TryParse(strValue, out prompts) == true)
            {
                if (prompts > 0)
                {
                    this.m_rushRounds = prompts;
                }
            }
            else if (strVariable.CompareTo("Number of Squad Deathmatch Rounds") == 0 && int.TryParse(strValue, out prompts) == true)
            {
                if (prompts > 0)
                {
                    this.m_sqdmRounds = prompts;
                }
            }
            else if (strVariable.CompareTo("Number of Squad Rush Rounds") == 0 && int.TryParse(strValue, out prompts) == true)
            {
                if (prompts > 0)
                {
                    this.m_sqrushRounds = prompts;
                }
            }
            else if (strVariable.CompareTo("Full Map List") == 0)
            {
                this.m_strSelectedAllMaps = strValue;
            }
            else if (strVariable.CompareTo("Add or Remove Map in List") == 0)
            {
                string[] options = Regex.Split(this.m_strSelectedAllMaps, " - ");
                string gametypeSelected = translateMapName(options[0]);
                string mapSelected = translateMapName(this.m_strSelectedAllMaps);
                if (strValue.Equals("Add"))
                {
                    if (this.m_gametypeList.Contains(gametypeSelected) == false)
                    {
                        this.m_gametypeList.Add(gametypeSelected);
                    }
                    switch (gametypeSelected)
                    {
                        case "cq":
                            if (this.m_conquestList.Contains(mapSelected) == false)
                            {
                                this.m_conquestList.Add(mapSelected);
                            }
                            break;
                        case "rush":
                            if (this.m_rushList.Contains(mapSelected) == false)
                            {
                                this.m_rushList.Add(mapSelected);
                            }
                            break;
                        case "sqdm":
                            if (this.m_sqdmList.Contains(mapSelected) == false)
                            {
                                this.m_sqdmList.Add(mapSelected);
                            }
                            break;
                        case "sqrush":
                            if (this.m_sqrushList.Contains(mapSelected) == false)
                            {
                                this.m_sqrushList.Add(mapSelected);
                            }
                            break;
                    }
                }
                else if (strValue.Equals("Remove"))
                {
                    switch (gametypeSelected)
                    {
                        case "cq":
                            if (this.m_gametypeList.Contains(gametypeSelected) == true && this.m_conquestList.Contains(mapSelected) == true && this.m_conquestList.Count == 1)
                            {
                                this.m_gametypeList.Remove(gametypeSelected);
                            }
                            if (this.m_conquestList.Contains(mapSelected) == true)
                            {
                                this.m_conquestList.Remove(mapSelected);
                            }
                            break;
                        case "rush":
                            if (this.m_gametypeList.Contains(gametypeSelected) == true && this.m_rushList.Contains(mapSelected) == true && this.m_rushList.Count == 1)
                            {
                                this.m_gametypeList.Remove(gametypeSelected);
                            }
                            if (this.m_rushList.Contains(mapSelected) == true)
                            {
                                this.m_rushList.Remove(mapSelected);
                            }
                            break;
                        case "sqdm":
                            if (this.m_gametypeList.Contains(gametypeSelected) == true && this.m_sqdmList.Contains(mapSelected) == true && this.m_sqdmList.Count == 1)
                            {
                                this.m_gametypeList.Remove(gametypeSelected);
                            }
                            if (this.m_sqdmList.Contains(mapSelected) == true)
                            {
                                this.m_sqdmList.Remove(mapSelected);
                            }
                            break;
                        case "sqrush":
                            if (this.m_gametypeList.Contains(gametypeSelected) == true && this.m_sqrushList.Contains(mapSelected) == true && this.m_sqrushList.Count == 1)
                            {
                                this.m_gametypeList.Remove(gametypeSelected);
                            }
                            if (this.m_sqrushList.Contains(mapSelected) == true)
                            {
                                this.m_sqrushList.Remove(mapSelected);
                            }
                            break;
                    }
                }
            }
            else if (strVariable.CompareTo("Gametype List") == 0)
            {
                string[] temp = CPluginVariable.DecodeStringArray(strValue.ToLower());
                oldEntries = this.m_gametypeList;
                if (temp.Length == 0 || temp[0].Equals(""))
                {
                    newEntries.Add("rush");
                }
                else
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        newEntries.Add(temp[i]);
                    }
                }
                for (int i = 0; i < newEntries.Count; i++)
                {
                    bool inList = false;
                    foreach (string gametype in this.acceptableEntries["gt"])
                    {
                        if (newEntries[i].Equals(gametype))
                        {
                            inList = true;
                            break;
                        }
                    }
                    if (inList == false)
                    {
                        string message = "You entered an incorrect gametype name.\nAvailable names:\t";
                        this.acceptableEntries["gt"].ForEach(delegate (string name)
                        {
                            message += name + "\n\t\t";
                        });
                        MessageBox.Show(message, "Input Error", MessageBoxButtons.OK);

                        if (newEntries.Count == oldEntries.Count)
                        {               // Edited an existing entry
                            newEntries[i] = oldEntries[i];
                        }
                        else
                        {                                                       // Added to the list, just add a default gametype
                            newEntries.RemoveAt(i);
                        }
                    }
                }
                this.m_gametypeList = removeDuplicates(newEntries);
            }
            else if (strVariable.CompareTo("Conquest Voting Possibilities") == 0)
            {
                string[] temp = CPluginVariable.DecodeStringArray(strValue.ToLower());
                oldEntries = this.m_conquestList;
                if (temp.Length == 0 || temp[0].Equals(""))
                {
                    newEntries.Add("levels/mp_007");
                }
                else
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        newEntries.Add(temp[i]);
                    }
                }
                for (int i = 0; i < newEntries.Count; i++)
                {
                    bool inList = false;
                    foreach (string gametype in this.acceptableEntries["cq"])
                    {
                        if (newEntries[i].Equals(gametype))
                        {
                            inList = true;
                            break;
                        }
                    }
                    if (inList == false)
                    {
                        string message = "You entered an incorrect map name.\nAvailable names:\t";
                        this.acceptableEntries["cq"].ForEach(delegate (string name)
                        {
                            message += name + "\n\t\t";
                        });
                        MessageBox.Show(message, "Input Error", MessageBoxButtons.OK);

                        if (newEntries.Count == oldEntries.Count)
                        {               // Edited an existing entry
                            newEntries[i] = oldEntries[i];
                        }
                        else
                        {                                                       // Added to the list, just add a default gametype
                            newEntries.RemoveAt(i);
                        }
                    }
                }
                this.m_conquestList = removeDuplicates(newEntries);
            }
            else if (strVariable.CompareTo("Rush Voting Possibilities") == 0)
            {
                string[] temp = CPluginVariable.DecodeStringArray(strValue.ToLower());
                oldEntries = this.m_rushList;
                if (temp.Length == 0 || temp[0].Equals(""))
                {
                    newEntries.Add("levels/mp_008");
                }
                else
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        newEntries.Add(temp[i]);
                    }
                }
                for (int i = 0; i < newEntries.Count; i++)
                {
                    bool inList = false;
                    foreach (string gametype in this.acceptableEntries["rush"])
                    {
                        if (newEntries[i].Equals(gametype))
                        {
                            inList = true;
                            break;
                        }
                    }
                    if (inList == false)
                    {
                        string message = "You entered an incorrect map name.\nAvailable names:\t";
                        this.acceptableEntries["rush"].ForEach(delegate (string name)
                        {
                            message += name + "\n\t\t";
                        });
                        MessageBox.Show(message, "Input Error", MessageBoxButtons.OK);

                        if (newEntries.Count == oldEntries.Count)
                        {               // Edited an existing entry
                            newEntries[i] = oldEntries[i];
                        }
                        else
                        {                                                       // Added to the list, just add a default gametype
                            newEntries.RemoveAt(i);
                        }
                    }
                }
                this.m_rushList = removeDuplicates(newEntries);
            }
            else if (strVariable.CompareTo("Squad Deathmatch Voting Possibilities") == 0)
            {
                string[] temp = CPluginVariable.DecodeStringArray(strValue.ToLower());
                oldEntries = this.m_sqdmList;
                if (temp.Length == 0 || temp[0].Equals(""))
                {
                    newEntries.Add("levels/mp_007sdm");
                }
                else
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        newEntries.Add(temp[i]);
                    }
                }
                for (int i = 0; i < newEntries.Count; i++)
                {
                    bool inList = false;
                    foreach (string gametype in this.acceptableEntries["sqdm"])
                    {
                        if (newEntries[i].Equals(gametype))
                        {
                            inList = true;
                            break;
                        }
                    }
                    if (inList == false)
                    {
                        string message = "You entered an incorrect map name.\nAvailable names:\t";
                        this.acceptableEntries["sqdm"].ForEach(delegate (string name)
                        {
                            message += name + "\n\t\t";
                        });
                        MessageBox.Show(message, "Input Error", MessageBoxButtons.OK);

                        if (newEntries.Count == oldEntries.Count)
                        {               // Edited an existing entry
                            newEntries[i] = oldEntries[i];
                        }
                        else
                        {                                                       // Added to the list, just add a default gametype
                            newEntries.RemoveAt(i);
                        }
                    }
                }
                this.m_sqdmList = removeDuplicates(newEntries);
            }
            else if (strVariable.CompareTo("Squad Rush Voting Possibilities") == 0)
            {
                string[] temp = CPluginVariable.DecodeStringArray(strValue.ToLower());
                oldEntries = this.m_sqrushList;
                if (temp.Length == 0 || temp[0].Equals(""))
                {
                    newEntries.Add("levels/mp_005sr");
                }
                else
                {
                    for (int i = 0; i < temp.Length; i++)
                    {
                        newEntries.Add(temp[i]);
                    }
                }
                for (int i = 0; i < newEntries.Count; i++)
                {
                    bool inList = false;
                    foreach (string gametype in this.acceptableEntries["sqrush"])
                    {
                        if (newEntries[i].Equals(gametype))
                        {
                            inList = true;
                            break;
                        }
                    }
                    if (inList == false)
                    {
                        string message = "You entered an incorrect map name.\nAvailable names:\t";
                        this.acceptableEntries["sqrush"].ForEach(delegate (string name)
                        {
                            message += name + "\n\t\t";
                        });
                        MessageBox.Show(message, "Input Error", MessageBoxButtons.OK);

                        if (newEntries.Count == oldEntries.Count)
                        {               // Edited an existing entry
                            newEntries[i] = oldEntries[i];
                        }
                        else
                        {                                                       // Added to the list, just add a default gametype
                            newEntries.RemoveAt(i);
                        }
                    }
                }
                this.m_sqrushList = removeDuplicates(newEntries);
            }
            else if (strVariable.CompareTo("Vote Command") == 0)
            {
                this.m_strVoteCommand = strValue;
            }
            else if (strVariable.CompareTo("Cancel Vote Command") == 0)
            {
                this.m_strCancelVoteCommand = strValue;
            }
        }

        static List<string> removeDuplicates(List<string> inputList)
        {
            Dictionary<string, int> uniqueStore = new Dictionary<string, int>();
            List<string> finalList = new List<string>();
            foreach (string currValue in inputList)
            {
                if (!uniqueStore.ContainsKey(currValue))
                {
                    uniqueStore.Add(currValue, 0);
                    finalList.Add(currValue);
                }
            }
            return finalList;
        }

        static string translateMapName(string levelString)
        {
            string mapName = " ";
            switch (levelString)
            {
                case "cq":
                    mapName = "Conquest";
                    break;
                case "rush":
                    mapName = "Rush";
                    break;
                case "sqdm":
                    mapName = "Squad Deathmatch";
                    break;
                case "sqrush":
                    mapName = "Squad Rush";
                    break;
                case "Conquest":
                    mapName = "cq";
                    break;
                case "Rush":
                    mapName = "rush";
                    break;
                case "Squad Deathmatch":
                    mapName = "sqdm";
                    break;
                case "Squadrush":
                    mapName = "sqrush";
                    break;
                case "Conquest - Panama Canal":
                    mapName = "levels/mp_001";
                    break;
                case "Conquest - Valparaíso":
                    mapName = "levels/mp_002cq";
                    break;
                case "Conquest - Laguna Alta":
                    mapName = "levels/mp_003";
                    break;
                case "Conquest - Isla Inocentes":
                    mapName = "levels/mp_004cq";
                    break;
                case "Conquest - Atacama Desert":
                    mapName = "levels/mp_005";
                    break;
                case "Conquest - Arica Harbor":
                    mapName = "levels/mp_006cq";
                    break;
                case "Conquest - White Pass":
                    mapName = "levels/mp_007";
                    break;
                case "Conquest - Nelson Bay":
                    mapName = "levels/mp_008cq";
                    break;
                case "Conquest - Laguna Presa":
                    mapName = "levels/mp_009cq";
                    break;
                case "Conquest - Port Valdez":
                    mapName = "levels/mp_012cq";
                    break;
                case "Rush - Panama Canal":
                    mapName = "levels/mp_001gr";
                    break;
                case "Rush - Valparaíso":
                    mapName = "levels/mp_002";
                    break;
                case "Rush - Laguna Alta":
                    mapName = "levels/mp_003gr";
                    break;
                case "Rush - Isla Inocentes":
                    mapName = "levels/mp_004";
                    break;
                case "Rush - Atacama Desert":
                    mapName = "levels/mp_005gr";
                    break;
                case "Rush - Arica Harbor":
                    mapName = "levels/mp_006";
                    break;
                case "Rush - White Pass":
                    mapName = "levels/mp_007gr";
                    break;
                case "Rush - Nelson Bay":
                    mapName = "levels/mp_008";
                    break;
                case "Rush - Laguna Presa":
                    mapName = "levels/mp_009gr";
                    break;
                case "Rush - Port Valdez":
                    mapName = "levels/mp_012gr";
                    break;
                case "Squadrush - Panama Canal":
                    mapName = "levels/mp_001sr";
                    break;
                case "Squadrush - Valparaíso":
                    mapName = "levels/mp_002sr";
                    break;
                case "Squadrush - Laguna Alta":
                    mapName = "levels/mp_003sr";
                    break;
                case "Squadrush - Isla Inocentes":
                    mapName = "levels/mp_004sr";
                    break;
                case "Squadrush - Atacama Desert":
                    mapName = "levels/mp_005sr";
                    break;
                case "Squadrush - Arica Harbor":
                    mapName = "levels/mp_006sr";
                    break;
                case "Squadrush - White Pass":
                    mapName = "levels/mp_007sr";
                    break;
                case "Squadrush - Nelson Bay":
                    mapName = "levels/mp_008sr";
                    break;
                case "Squadrush - Laguna Presa":
                    mapName = "levels/mp_009sr";
                    break;
                case "Squadrush - Port Valdez":
                    mapName = "levels/mp_012sr";
                    break;
                case "Squad Deathmatch - Panama Canal":
                    mapName = "levels/mp_001sdm";
                    break;
                case "Squad Deathmatch - Valparaíso":
                    mapName = "levels/mp_002sdm";
                    break;
                case "Squad Deathmatch - Laguna Alta":
                    mapName = "levels/mp_003sdm";
                    break;
                case "Squad Deathmatch - Isla Inocentes":
                    mapName = "levels/mp_004sdm";
                    break;
                case "Squad Deathmatch - Atacama Desert":
                    mapName = "levels/mp_005sdm";
                    break;
                case "Squad Deathmatch - Arica Harbor":
                    mapName = "levels/mp_006sdm";
                    break;
                case "Squad Deathmatch - White Pass":
                    mapName = "levels/mp_007sdm";
                    break;
                case "Squad Deathmatch - Nelson Bay":
                    mapName = "levels/mp_008sdm";
                    break;
                case "Squad Deathmatch - Laguna Presa":
                    mapName = "levels/mp_009sdm";
                    break;
                case "Squad Deathmatch - Port Valdez":
                    mapName = "levels/mp_012sdm";
                    break;
                case "levels/mp_001":
                case "levels/mp_001cq":
                case "levels/mp_001sdm":
                case "levels/mp_001gr":
                case "levels/mp_001sr":
                    mapName = "Panama Canal";
                    break;
                case "levels/mp_002":
                case "levels/mp_002cq":
                case "levels/mp_002sdm":
                case "levels/mp_002sr":
                case "levels/mp_002gr":
                    mapName = "Valparaiso";
                    break;
                case "levels/mp_003":
                case "levels/mp_003cq":
                case "levels/mp_003sdm":
                case "levels/mp_003gr":
                case "levels/mp_003sr":
                    mapName = "Laguna Alta";
                    break;
                case "levels/mp_004":
                case "levels/mp_004cq":
                case "levels/mp_004gr":
                case "levels/mp_004sr":
                case "levels/mp_004sdm":
                    mapName = "Isla Inocentes";
                    break;
                case "levels/mp_005":
                case "levels/mp_005cq":
                case "levels/mp_005sdm":
                case "levels/mp_005gr":
                case "levels/mp_005sr":
                    mapName = "Atacama Desert";
                    break;
                case "levels/mp_006":
                case "levels/mp_006gr":
                case "levels/mp_006cq":
                case "levels/mp_006sdm":
                case "levels/mp_006sr":
                    mapName = "Arica Harbor";
                    break;
                case "levels/mp_007":
                case "levels/mp_007cq":
                case "levels/mp_007gr":
                case "levels/mp_007sr":
                case "levels/mp_007sdm":
                    mapName = "White Pass";
                    break;
                case "levels/mp_008":
                case "levels/mp_008cq":
                case "levels/mp_008sdm":
                case "levels/mp_008gr":
                case "levels/mp_008sr":
                    mapName = "Nelson Bay";
                    break;
                case "levels/mp_009":
                case "levels/mp_009sr":
                case "levels/mp_009cq":
                case "levels/mp_009gr":
                case "levels/mp_009sdm":
                    mapName = "Laguna Presa";
                    break;
                case "levels/mp_012":
                case "levels/mp_012cq":
                case "levels/mp_012sdm":
                case "levels/mp_012gr":
                case "levels/mp_012sr":
                    mapName = "Port Valdez";
                    break;
            }
            return mapName;
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
            if (this.m_gametype.ContainsKey(strSoldierName) == false)
            {
                this.m_gametype.Add(strSoldierName, 0);
            }
            if (this.m_maps.ContainsKey(strSoldierName) == false)
            {
                this.m_maps.Add(strSoldierName, 0);
            }
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {
            if (this.m_gametype.ContainsKey(strSoldierName) == true)
            {
                int oldVote = this.m_gametype[strSoldierName];              // Save old vote to remove
                this.m_gametype.Remove(strSoldierName);                     // Clear player's vote
                if (this.currentVoting.ContainsKey(oldVote))
                {               // Check to make sure vote exists in voting list
                    if (this.currentVoting[oldVote] > 1)
                    {                   // More than one vote already?
                        this.currentVoting[oldVote]--;                      // Decrement vote count
                    }
                    else
                    {                                                   // Only one vote
                        this.currentVoting.Remove(oldVote);                 // Remove vote
                    }
                }
            }
            if (this.m_maps.ContainsKey(strSoldierName) == true)
            {
                int oldVote = this.m_maps[strSoldierName];                  // Save old vote to remove
                this.m_maps.Remove(strSoldierName);                         // Clear player's vote
                if (this.currentVoting.ContainsKey(oldVote))
                {               // Check to make sure vote exists in voting list
                    if (this.currentVoting[oldVote] > 1)
                    {                   // More than one vote already?
                        this.currentVoting[oldVote]--;                      // Decrement vote count
                    }
                    else
                    {                                                   // Only one vote
                        this.currentVoting.Remove(oldVote);                 // Remove vote
                    }
                }
            }
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

        }

        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            Match mMatch;

            mMatch = Regex.Match(strMessage, String.Format("(?<scope>{0})(?<command>{1})[ ]?(?<arguments>.*)", String.Format("{0}|{1}|{2}", this.m_strPrivatePrefix, this.m_strAdminsPrefix, this.m_strPublicPrefix), String.Format("{0}|{1}", this.m_strVoteCommand, this.m_strCancelVoteCommand)), RegexOptions.IgnoreCase);
            if (mMatch.Success == true)
            {
                if (String.Compare(mMatch.Groups["command"].Value, this.m_strVoteCommand, true) == 0)
                {
                    if (this.m_votingOption == 1)
                    {           // Gametype Voting right now
                        int voteLimit = this.m_gametypeList.Count;
                        int playerVote = 0;
                        if (int.TryParse(mMatch.Groups["arguments"].Value, out playerVote) == true)
                        {
                            if (playerVote > voteLimit || playerVote < 1)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", "Invalid Vote Option: Require 1 - " + voteLimit.ToString(), "player", strSpeaker);
                                for (int i = 0; i < this.m_gametypeList.Count; i++)
                                {
                                    if ((i + 2) <= this.m_gametypeList.Count)
                                    {           // There are at least two more options to display
                                        this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(this.m_gametypeList[i]) + "   " + (i + 2).ToString() + ": " + translateMapName(this.m_gametypeList[i + 1]), "player", strSpeaker);
                                        i++;
                                    }
                                    else
                                    {
                                        this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(this.m_gametypeList[i]), "player", strSpeaker);
                                    }
                                }
                            }
                            else if (this.m_gametype[strSpeaker] > 0)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", "You already voted for " + translateMapName(this.m_gametypeList[this.m_gametype[strSpeaker] - 1]) + ". Cancel your vote first (/@" + this.m_strCancelVoteCommand + ")", "player", strSpeaker);
                            }
                            else
                            {
                                this.m_gametype[strSpeaker] = playerVote;
                                this.ExecuteCommand("procon.protected.send", "admin.say", "You voted for " + translateMapName(this.m_gametypeList[playerVote - 1]) + ". Type /@" + this.m_strCancelVoteCommand + " to cancel vote", "player", strSpeaker);
                                int vote;
                                this.currentVoting = new Dictionary<int, int>();
                                foreach (string player in this.m_gametype.Keys)
                                {
                                    vote = this.m_gametype[player];
                                    if (vote > 0)
                                    {
                                        if (this.currentVoting.ContainsKey(vote) == false)
                                        {
                                            this.currentVoting.Add(vote, 1);
                                        }
                                        else
                                        {
                                            this.currentVoting[vote]++;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", "Invalid Vote Option: Require 1 - " + voteLimit.ToString(), "player", strSpeaker);
                            for (int i = 0; i < this.m_gametypeList.Count; i++)
                            {
                                if ((i + 2) <= this.m_gametypeList.Count)
                                {           // There are at least two more options to display
                                    this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(this.m_gametypeList[i]) + "   " + (i + 2).ToString() + ": " + translateMapName(this.m_gametypeList[i + 1]), "player", strSpeaker);
                                    i++;
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(this.m_gametypeList[i]), "player", strSpeaker);
                                }
                            }
                        }
                    }
                    else if (m_votingOption == 2)
                    {       // Map Voting right now
                        string gametypeWinner = this.m_gametypeList[this.m_gametypeWinner];
                        List<string> voteLimit;
                        switch (gametypeWinner)
                        {
                            case "cq":
                                voteLimit = this.m_conquestList;
                                break;
                            case "rush":
                                voteLimit = this.m_rushList;
                                break;
                            case "sqdm":
                                voteLimit = this.m_sqdmList;
                                break;
                            case "sqrush":
                                voteLimit = this.m_sqrushList;
                                break;
                            default:
                                voteLimit = this.m_rushList;
                                break;
                        }
                        int playerVote = 0;
                        if (int.TryParse(mMatch.Groups["arguments"].Value, out playerVote) == true)
                        {
                            if (playerVote > voteLimit.Count || playerVote < 1)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", "Invalid Vote Option: Require 1 - " + voteLimit.Count.ToString(), "player", strSpeaker);
                                for (int i = 0; i < voteLimit.Count; i++)
                                {
                                    if ((i + 2) <= voteLimit.Count)
                                    {           // There are at least two more options to display
                                        this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(voteLimit[i]) + "   " + (i + 2).ToString() + ": " + translateMapName(voteLimit[i + 1]), "player", strSpeaker);
                                        i++;
                                    }
                                    else
                                    {
                                        this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(voteLimit[i]), "player", strSpeaker);
                                    }
                                }
                            }
                            else if (this.m_maps[strSpeaker] > 0)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", "You already voted for " + translateMapName(voteLimit[this.m_maps[strSpeaker] - 1]) + ". Cancel your vote first (/@" + this.m_strCancelVoteCommand + ")", "player", strSpeaker);
                            }
                            else
                            {
                                this.m_maps[strSpeaker] = playerVote;
                                this.ExecuteCommand("procon.protected.send", "admin.say", "You voted for " + translateMapName(voteLimit[playerVote - 1]) + ". Type /@" + this.m_strCancelVoteCommand + " to cancel vote", "player", strSpeaker);
                                int vote;
                                this.currentVoting = new Dictionary<int, int>();
                                foreach (string player in this.m_maps.Keys)
                                {
                                    vote = this.m_maps[player];
                                    if (vote > 0)
                                    {
                                        if (this.currentVoting.ContainsKey(vote) == false)
                                        {
                                            this.currentVoting.Add(vote, 1);
                                        }
                                        else
                                        {
                                            this.currentVoting[vote]++;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            this.ExecuteCommand("procon.protected.send", "admin.say", "Invalid Vote Option: Require 1 - " + voteLimit.Count.ToString(), "player", strSpeaker);
                            for (int i = 0; i < voteLimit.Count; i++)
                            {
                                if ((i + 2) <= voteLimit.Count)
                                {           // There are at least two more options to display
                                    this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(voteLimit[i]) + "   " + (i + 2).ToString() + ": " + translateMapName(voteLimit[i + 1]), "player", strSpeaker);
                                    i++;
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(voteLimit[i]), "player", strSpeaker);
                                }
                            }
                        }
                    }
                    else if (this.m_votingOption == 0)
                    {       // No Voting right now
                        this.ExecuteCommand("procon.protected.send", "admin.say", "There is no vote right now", "player", strSpeaker);
                    }
                }
                else if (String.Compare(mMatch.Groups["command"].Value, this.m_strCancelVoteCommand, true) == 0)
                {
                    if (this.m_votingOption == 1)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Cancelling your gametype vote", "player", strSpeaker);
                        int oldVote = this.m_gametype[strSpeaker];              // Save old vote to remove
                        this.m_gametype[strSpeaker] = 0;                        // Clear player's vote
                        if (this.currentVoting.ContainsKey(oldVote))
                        {           // Check to make sure vote exists in voting list
                            if (this.currentVoting[oldVote] > 1)
                            {               // More than one vote already?
                                this.currentVoting[oldVote]--;                  // Decrement vote count
                            }
                            else
                            {                                               // Only one vote
                                this.currentVoting.Remove(oldVote);             // Remove vote
                            }
                        }
                        for (int i = 0; i < this.m_gametypeList.Count; i++)
                        {
                            if ((i + 2) <= this.m_gametypeList.Count)
                            {           // There are at least two more options to display
                                this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(this.m_gametypeList[i]) + "   " + (i + 2).ToString() + ": " + translateMapName(this.m_gametypeList[i + 1]), "player", strSpeaker);
                                i++;
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(this.m_gametypeList[i]), "player", strSpeaker);
                            }
                        }
                    }
                    else if (this.m_votingOption == 2)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Cancelling your map vote", "player", strSpeaker);
                        int oldVote = this.m_maps[strSpeaker];                  // Save old vote to remove
                        this.m_maps[strSpeaker] = 0;                            // Clear player's vote
                        if (this.currentVoting.ContainsKey(oldVote))
                        {           // Check to make sure vote exists in voting list
                            if (this.currentVoting[oldVote] > 1)
                            {               // More than one vote already?
                                this.currentVoting[oldVote]--;                  // Decrement vote count
                            }
                            else
                            {                                               // Only one vote
                                this.currentVoting.Remove(oldVote);             // Remove vote
                            }
                        }
                        string gametypeWinner = this.m_gametypeList[this.m_gametypeWinner];
                        List<string> voteLimit;
                        switch (gametypeWinner)
                        {
                            case "cq":
                                voteLimit = this.m_conquestList;
                                break;
                            case "rush":
                                voteLimit = this.m_rushList;
                                break;
                            case "sqdm":
                                voteLimit = this.m_sqdmList;
                                break;
                            case "sqrush":
                                voteLimit = this.m_sqrushList;
                                break;
                            default:
                                voteLimit = this.m_rushList;
                                break;
                        }
                        for (int i = 0; i < voteLimit.Count; i++)
                        {
                            if ((i + 2) <= voteLimit.Count)
                            {           // There are at least two more options to display
                                this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(voteLimit[i]) + "   " + (i + 2).ToString() + ": " + translateMapName(voteLimit[i + 1]), "player", strSpeaker);
                                i++;
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.say", (i + 1).ToString() + ": " + translateMapName(voteLimit[i]), "player", strSpeaker);
                            }
                        }
                    }
                    else if (this.m_votingOption == 0)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "There is no vote right now", "player", strSpeaker);
                    }
                }
            }
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        public void OnLoadingLevel(string strMapFileName)
        {

        }

        public void OnLevelStarted()
        {
            this.currentVoting.Clear();
            if (this.t.IsAlive)
            {
                try
                {
                    this.t.Abort();
                }
                catch (Exception)
                {
                    // "We don't care about this exception
                }
            }
            bool roundCount = false;
            while (roundCount == false)
            {
                this.m_currentRound = 0;
                this.m_maxRound = 0;
                this.ExecuteCommand("procon.protected.send", "serverInfo");
                Thread.Sleep(2000);
                if (this.m_currentRound.Equals(0) == false && this.m_maxRound.Equals(0) == false)
                {       // We have round numbers for both current and max, we can check if we vote this round now
                    roundCount = true;
                }
            }
            if (this.m_currentRound == this.m_maxRound)
            {
                this.m_votingOption = 1;
                this.m_gametypeWinner = -1;
                List<string> mapVotes = new List<string>(this.m_maps.Keys);
                foreach (string player in mapVotes)
                {
                    this.m_maps[player] = 0;
                }
                List<string> gameVotes = new List<string>(this.m_gametype.Keys);
                foreach (string player2 in gameVotes)
                {
                    this.m_gametype[player2] = 0;
                }
                this.t = new Thread(new ParameterizedThreadStart(manageVoting));
                this.t.Start(this.m_votingDuration);
            }
            else
            {
                this.m_votingOption = 0;
            }
        }

        private void manageVoting(object voteDur)
        {
            this.m_boolBeginningYell = true;
            Thread.Sleep(60000);                        // Let's wait 60 seconds, so everyone can see the first message
            double dur = (double)voteDur;
            double elapsed = 0;
            int voteCount = 0;
            int voteLeader = 0;
            bool tie = false;
            List<int> tieVote = new List<int>();
            double delta = dur / this.m_votePrompts;
            if (delta <= 2 / 3)
            {
                delta = 2 / 3;
            }
            List<string> voteLimit = new List<string>();
            List<int> votes = new List<int>(this.currentVoting.Keys);
            if (this.m_votingOption == 2)
            {
                string gametypeWinner = this.m_gametypeList[this.m_gametypeWinner];
                switch (gametypeWinner)
                {
                    case "cq":
                        voteLimit = this.m_conquestList;
                        break;
                    case "rush":
                        voteLimit = this.m_rushList;
                        break;
                    case "sqdm":
                        voteLimit = this.m_sqdmList;
                        break;
                    case "sqrush":
                        voteLimit = this.m_sqrushList;
                        break;
                    default:
                        voteLimit = this.m_rushList;
                        break;
                }
            }
            while (elapsed < dur)
            {
                voteCount = 0;
                voteLeader = 0;
                tie = false;
                tieVote.Clear();
                elapsed += delta;
                this.promptUsers();
                int roundedDuration = Convert.ToUInt16(delta);
                Thread.Sleep(roundedDuration * 1000 * 60);                              // Pause for 1/5 of the duration
                votes = new List<int>(this.currentVoting.Keys);
                if (votes.Count > 0)
                {
                    foreach (int curVote in votes)
                    {
                        if (this.currentVoting[curVote] > voteCount)
                        {
                            voteCount = this.currentVoting[curVote];
                            voteLeader = curVote;
                            tie = false;
                            tieVote.Clear();
                        }
                        else if (this.currentVoting[curVote] == voteCount)
                        {
                            if ((this.m_votingOption == 1 && this.m_gametypeList[curVote - 1].Equals(this.m_gametypeList[voteLeader - 1]) == false) || (this.m_votingOption == 2 && voteLimit[curVote - 1].Equals(voteLimit[voteLeader - 1]) == false))
                            {
                                tie = true;
                                tieVote.Add(curVote);
                            }
                        }
                    }
                    if (this.m_votingOption == 1)
                    {
                        if (tie == false)
                        {
                            if (voteCount == 1)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.yell", translateMapName(this.m_gametypeList[voteLeader - 1]) + " leads with " + voteCount.ToString() + " vote", "5000", "all");
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.yell", translateMapName(this.m_gametypeList[voteLeader - 1]) + " leads with " + voteCount.ToString() + " votes", "5000", "all");
                            }
                        }
                        else if (tie == true)
                        {
                            string tieNames = "";
                            foreach (int voteCast in tieVote)
                            {
                                tieNames += " and " + translateMapName(this.m_gametypeList[voteCast - 1]);
                            }
                            this.ExecuteCommand("procon.protected.send", "admin.yell", "There is a tie between " + translateMapName(this.m_gametypeList[voteLeader - 1]) + tieNames, "5000", "all");
                        }
                    }
                    else if (this.m_votingOption == 2)
                    {
                        if (tie == false)
                        {
                            if (voteCount == 1)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.yell", translateMapName(voteLimit[voteLeader - 1]) + " leads with " + voteCount.ToString() + " vote", "5000", "all");
                            }
                            else
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.yell", translateMapName(voteLimit[voteLeader - 1]) + " leads with " + voteCount.ToString() + " votes", "5000", "all");
                            }
                        }
                        else if (tie == true)
                        {
                            string tieNames = "";
                            foreach (int voteCast in tieVote)
                            {
                                tieNames += " and " + translateMapName(voteLimit[voteCast - 1]);
                            }
                            this.ExecuteCommand("procon.protected.send", "admin.yell", "There is a tie between " + translateMapName(voteLimit[voteLeader - 1]) + tieNames, "5000", "all");
                        }
                    }
                }
            }

            votes = new List<int>(this.currentVoting.Keys);
            if (votes.Count > 0)
            {
                if (tie == true)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.yell", "There was a tie, randomly picking winner", "5000", "all");
                    Random r = new Random();
                    int tieBreaker = r.Next(tieVote.Count);
                    voteLeader = tieVote[tieBreaker];
                }
                if (this.m_votingOption == 1)
                {
                    this.m_votingOption = 2;
                    this.m_gametypeWinner = voteLeader - 1;
                    Thread.Sleep(5000);
                    this.ExecuteCommand("procon.protected.send", "admin.yell", translateMapName(this.m_gametypeList[this.m_gametypeWinner]) + " wins!", "5000", "all");
                    this.ExecuteCommand("procon.protected.send", "mapList.clear");
                    List<string> mapList = new List<string>();
                    int rounds = 1;
                    switch (this.m_gametypeList[this.m_gametypeWinner])
                    {
                        case "cq":
                            this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "CONQUEST");
                            mapList = this.m_conquestList;
                            rounds = this.m_conquestRounds;
                            break;
                        case "rush":
                            this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "RUSH");
                            mapList = this.m_rushList;
                            rounds = this.m_rushRounds;
                            break;
                        case "sqdm":
                            this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "SQDM");
                            mapList = this.m_sqdmList;
                            rounds = this.m_sqdmRounds;
                            break;
                        case "sqrush":
                            this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "SQRUSH");
                            mapList = this.m_sqrushList;
                            rounds = this.m_sqrushRounds;
                            break;
                        default:
                            this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "RUSH");
                            mapList = this.m_rushList;
                            rounds = this.m_rushRounds;
                            break;
                    }
                    int mapLoc = mapList.IndexOf(this.m_strCurrentMap);
                    if (mapLoc.Equals(-1) || mapLoc.Equals(mapList.Count - 1))
                    {           // Current map NOT in list OR current map is last in the list
                        foreach (string level in mapList)
                        {
                            this.ExecuteCommand("procon.protected.send", "mapList.append", level, rounds.ToString());
                        }
                    }
                    else
                    {           // The current map is somewhere in the middle, rearrange the map list as it is appended
                        List<string> temp = new List<string>();
                        for (int j = mapLoc + 1; j < mapList.Count; j++)
                        {
                            temp.Add(mapList[j]);
                        }
                        for (int k = 0; k < mapLoc + 1; k++)
                        {
                            temp.Add(mapList[k]);
                        }
                        foreach (string level in temp)
                        {
                            this.ExecuteCommand("procon.protected.send", "mapList.append", level, rounds.ToString());
                        }
                    }
                    this.t = new Thread(new ParameterizedThreadStart(manageVoting));
                    this.t.Start(this.m_votingDuration);
                }
                else if (this.m_votingOption == 2)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.yell", translateMapName(voteLimit[voteLeader - 1]) + " wins!", "5000", "all");
                    this.m_votingOption = 0;
                    this.ExecuteCommand("procon.protected.send", "mapList.clear");
                    switch (this.m_gametypeList[this.m_gametypeWinner])
                    {
                        case "cq":
                            this.ExecuteCommand("procon.protected.send", "mapList.append", voteLimit[voteLeader - 1], this.m_conquestRounds.ToString());
                            break;
                        case "rush":
                            this.ExecuteCommand("procon.protected.send", "mapList.append", voteLimit[voteLeader - 1], this.m_rushRounds.ToString());
                            break;
                        case "sqdm":
                            this.ExecuteCommand("procon.protected.send", "mapList.append", voteLimit[voteLeader - 1], this.m_sqdmRounds.ToString());
                            break;
                        case "sqrush":
                            this.ExecuteCommand("procon.protected.send", "mapList.append", voteLimit[voteLeader - 1], this.m_sqrushRounds.ToString());
                            break;
                        default:
                            this.ExecuteCommand("procon.protected.send", "mapList.append", voteLimit[voteLeader - 1], this.m_rushRounds.ToString());
                            break;
                    }
                }
            }
            else if (this.m_votingOption == 1)
            {           // No votes on the gametype vote, randomly select a gametype and map
                this.ExecuteCommand("procon.protected.send", "serverInfo");
                Thread.Sleep(5000);
                Random r = new Random();
                int pick = 0;
                List<string> tempList = this.m_gametypeList;
                if (this.m_currentPlayerCount > 16)
                {       // We can pick from Conquest or Rush only
                    if (tempList.Contains("sqdm"))
                    {
                        tempList.Remove("sqdm");
                    }
                    if (tempList.Contains("sqrush"))
                    {
                        tempList.Remove("sqrush");
                    }
                }
                else if (this.m_currentPlayerCount > 8)
                {       // We can pick from Conquest, Rush, or Squad Deathmatch
                    if (tempList.Contains("sqrush"))
                    {
                        tempList.Remove("sqrush");
                    }
                }
                // If neither of the above is true (player count <= 8), we can pick from any gametype without distrupting the server
                pick = r.Next(tempList.Count);
                this.ExecuteCommand("procon.protected.send", "mapList.clear");
                switch (tempList[pick])
                {
                    case "cq":
                        this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "CONQUEST");
                        tempList = this.m_conquestList;
                        if (tempList.Contains(this.m_strCurrentMap))
                        {
                            tempList.Remove(this.m_strCurrentMap);
                        }
                        pick = r.Next(tempList.Count);
                        this.ExecuteCommand("procon.protected.send", "mapList.append", tempList[pick], this.m_conquestRounds.ToString());
                        break;
                    case "rush":
                        this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "RUSH");
                        tempList = this.m_rushList;
                        if (tempList.Contains(this.m_strCurrentMap))
                        {
                            tempList.Remove(this.m_strCurrentMap);
                        }
                        pick = r.Next(tempList.Count);
                        this.ExecuteCommand("procon.protected.send", "mapList.append", tempList[pick], this.m_rushRounds.ToString());
                        break;
                    case "sqdm":
                        this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "SQDM");
                        tempList = this.m_sqdmList;
                        if (tempList.Contains(this.m_strCurrentMap))
                        {
                            tempList.Remove(this.m_strCurrentMap);
                        }
                        pick = r.Next(tempList.Count);
                        this.ExecuteCommand("procon.protected.send", "mapList.append", tempList[pick], this.m_sqdmRounds.ToString());
                        break;
                    case "sqrush":
                        this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "SQRUSH");
                        tempList = this.m_sqrushList;
                        if (tempList.Contains(this.m_strCurrentMap))
                        {
                            tempList.Remove(this.m_strCurrentMap);
                        }
                        pick = r.Next(tempList.Count);
                        this.ExecuteCommand("procon.protected.send", "mapList.append", tempList[pick], this.m_sqrushRounds.ToString());
                        break;
                    default:
                        this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", "RUSH");
                        tempList = this.m_rushList;
                        if (tempList.Contains(this.m_strCurrentMap))
                        {
                            tempList.Remove(this.m_strCurrentMap);
                        }
                        pick = r.Next(tempList.Count);
                        this.ExecuteCommand("procon.protected.send", "mapList.append", tempList[pick], this.m_rushRounds.ToString());
                        break;
                }
            }
            this.currentVoting.Clear();
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
            this.m_currentRound = csiServerInfo.CurrentRound;
            this.m_maxRound = csiServerInfo.TotalRounds;
            this.m_currentPlayerCount = csiServerInfo.PlayerCount;
            this.m_strCurrentGame = csiServerInfo.GameMode.ToLower();
            this.m_strCurrentMap = csiServerInfo.Map.ToLower();
        }

        #region Blah
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
            foreach (CPlayerInfo cpiPlayer in lstPlayers)
            {
                if (this.m_gametype.ContainsKey(cpiPlayer.SoldierName) == false)
                {
                    this.m_gametype.Add(cpiPlayer.SoldierName, 0);
                }
                if (this.m_maps.ContainsKey(cpiPlayer.SoldierName) == false)
                {
                    this.m_maps.Add(cpiPlayer.SoldierName, 0);
                }
            }
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
    #endregion
}