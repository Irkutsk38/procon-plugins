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
using System.Windows.Forms;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CAdminLogger : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private string m_strPrivatePrefix;
        private string m_strAdminsPrefix;
        private string m_strPublicPrefix;

        private Dictionary<string, CPlayerInfo> m_dicVanillaInfo;
        private Dictionary<string, string> m_dicTeamNames;
        private Dictionary<string, int> m_dicSquadNames;
        private string m_strCurrentMapFileName;
        private List<CMap> m_lstMaps;

        // Admin Logging //
        private string logDir;
        private Dictionary<string, string[]> adminCommands = new Dictionary<string, string[]>();
        private Dictionary<string, int> commandCounter = new Dictionary<string, int>();
        private enumBoolYesNo m_logCommands;
        private enumBoolYesNo adjustLogDir;
        private int m_commandLimit;
        // Admin Logging //

        private string m_strKickCommand;
        private string m_strKillCommand;
        private string m_strNukeCommand;
        private string m_strMoveCommand;
        private string m_strForceMoveCommand;
        private string m_strTemporaryBanCommand;
        private string m_strPermanentBanCommand;
        private string m_strSayCommand;
        private string m_strPlayerSayCommand;
        private string m_strYellCommand;
        private string m_strPlayerYellCommand;
        private string m_strPlayerWarnCommand;
        private string m_strRestartLevelCommand;
        private string m_strNextLevelCommand;
        private string m_strExecuteConfigCommand;
        private string m_strConfirmCommand;
        private string m_strCancelCommand;
        private string m_strReservedCommand;


        public CAdminLogger()
        {

            this.m_strPrivatePrefix = "@";
            this.m_strAdminsPrefix = "#";
            this.m_strPublicPrefix = "!";

            // Admin Logging //
            this.m_commandLimit = 100;
            this.m_logCommands = enumBoolYesNo.Yes;
            this.adjustLogDir = enumBoolYesNo.No;
            this.logDir = @Directory.GetCurrentDirectory() + "\\Plugins\\Admin Logs";
            this.commandCounter.Add("Total Admin Log", 0);
            this.adminCommands.Add("Total Admin Log", new string[this.m_commandLimit]);
            this.adminCommands["Total Admin Log"].SetValue("", this.commandCounter["Total Admin Log"]);
            this.commandCounter["Total Admin Log"]++;
            this.adminCommands["Total Admin Log"].SetValue("Admin Commands Starting at: " + DateTime.Now.ToString(), this.commandCounter["Total Admin Log"]);
            this.commandCounter["Total Admin Log"]++;
            // Admin Logging //

            this.m_dicVanillaInfo = new Dictionary<string, CPlayerInfo>();
            this.m_dicTeamNames = new Dictionary<string, string>();
            this.m_dicSquadNames = new Dictionary<string, int>();
            this.m_strCurrentMapFileName = "";

            this.m_strKickCommand = "kick";
            this.m_strKillCommand = "kill";
            this.m_strNukeCommand = "nuke";
            this.m_strMoveCommand = "move";
            this.m_strForceMoveCommand = "fmove";
            this.m_strTemporaryBanCommand = "tban";
            this.m_strPermanentBanCommand = "ban";
            this.m_strSayCommand = "say";
            this.m_strPlayerSayCommand = "psay";
            this.m_strYellCommand = "yell";
            this.m_strPlayerYellCommand = "pyell";
            this.m_strPlayerWarnCommand = "warn";
            this.m_strRestartLevelCommand = "restart";
            this.m_strNextLevelCommand = "nextlevel";
            this.m_strConfirmCommand = "yes";
            this.m_strCancelCommand = "cancel";
            this.m_strExecuteConfigCommand = "exec";
            this.m_strReservedCommand = "reserve";
        }

        public string GetPluginName()
        {
            return "Admin Logger";
        }

        public string GetPluginVersion()
        {
            return "1.2";
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
            return @"Logs admin commands that are SUCCESSFULLY sent the server. Please update the commands your server uses in the plugin settings to ensure all commands are logged.
Commands
========================
Kick command
Move command
Force Move command
Kill command
Nuke command
Permanent Ban command
Temporary Ban command
Say command
Player say command
Yell command
Player yell command
Restart level command
Next level command
Execute Config
Reserve Player
Confirm command
Cancel command

Version 1.1: Allow admin to pick folder to place log files
Version 1.2: Audit other admin controls (map list, ban list, etc. other stuff not easily doable through in game chat)";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bIn-Game Admin Logger ^2Enabled!");
            if (Directory.Exists(this.logDir) == false)
            {
                Directory.CreateDirectory(this.logDir);
            }
            if (File.Exists(this.logDir + "\\Total Admin Log.txt") == false)
            {
                File.Create(this.logDir + "\\Total Admin Log.txt");
            }

            this.m_lstMaps = this.GetMapDefines();

            this.m_dicTeamNames.Clear();
            this.m_dicTeamNames.Add("global.conquest.neutral", "Neutral");
            this.m_dicTeamNames.Add("global.conquest.us", "U.S Army");
            this.m_dicTeamNames.Add("global.conquest.ru", "Russian Army");
            this.m_dicTeamNames.Add("global.rush.neutral", "Neutral");
            this.m_dicTeamNames.Add("global.rush.defenders", "Defenders");
            this.m_dicTeamNames.Add("global.rush.attackers", "Attackers");
            this.m_dicTeamNames.Add("global.Squad1", "Alpha");
            this.m_dicTeamNames.Add("global.Squad2", "Bravo");
            this.m_dicTeamNames.Add("global.Squad3", "Charlie");
            this.m_dicTeamNames.Add("global.Squad4", "Delta");
            this.m_dicTeamNames.Add("global.Squad5", "Echo");
            this.m_dicTeamNames.Add("global.Squad6", "Foxtrot");
            this.m_dicTeamNames.Add("global.Squad7", "Golf");
            this.m_dicTeamNames.Add("global.Squad8", "Hotel");

            this.m_dicSquadNames.Clear();
            this.m_dicSquadNames.Add("None", 0);
            this.m_dicSquadNames.Add("Alpha", 1);
            this.m_dicSquadNames.Add("Bravo", 2);
            this.m_dicSquadNames.Add("Charlie", 3);
            this.m_dicSquadNames.Add("Delta", 4);
            this.m_dicSquadNames.Add("Echo", 5);
            this.m_dicSquadNames.Add("Foxtrot", 6);
            this.m_dicSquadNames.Add("Golf", 7);
            this.m_dicSquadNames.Add("Hotel", 8);
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bIn-Game Admin Logger ^1Disabled =(");
            writeCommandsToFile("all");
            //this.adminCommands["Total Admin Log"].Close();
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Command Logging|Log Commands?", this.m_logCommands.GetType(), this.m_logCommands));

            lstReturn.Add(new CPluginVariable("Commands|Kick", this.m_strKickCommand.GetType(), this.m_strKickCommand));
            lstReturn.Add(new CPluginVariable("Commands|Move", this.m_strMoveCommand.GetType(), this.m_strMoveCommand));
            lstReturn.Add(new CPluginVariable("Commands|Force Move", this.m_strForceMoveCommand.GetType(), this.m_strForceMoveCommand));
            lstReturn.Add(new CPluginVariable("Commands|Nuke", this.m_strNukeCommand.GetType(), this.m_strNukeCommand));
            lstReturn.Add(new CPluginVariable("Commands|Kill", this.m_strKillCommand.GetType(), this.m_strKillCommand));
            lstReturn.Add(new CPluginVariable("Commands|Temporary Ban", this.m_strTemporaryBanCommand.GetType(), this.m_strTemporaryBanCommand));
            lstReturn.Add(new CPluginVariable("Commands|Permanent Ban", this.m_strPermanentBanCommand.GetType(), this.m_strPermanentBanCommand));
            lstReturn.Add(new CPluginVariable("Commands|Say", this.m_strSayCommand.GetType(), this.m_strSayCommand));
            lstReturn.Add(new CPluginVariable("Commands|Player Say", this.m_strPlayerSayCommand.GetType(), this.m_strPlayerSayCommand));
            lstReturn.Add(new CPluginVariable("Commands|Yell", this.m_strYellCommand.GetType(), this.m_strYellCommand));
            lstReturn.Add(new CPluginVariable("Commands|Player Yell", this.m_strPlayerYellCommand.GetType(), this.m_strPlayerYellCommand));
            lstReturn.Add(new CPluginVariable("Commands|Restart Map", this.m_strRestartLevelCommand.GetType(), this.m_strRestartLevelCommand));
            lstReturn.Add(new CPluginVariable("Commands|Next Map", this.m_strNextLevelCommand.GetType(), this.m_strNextLevelCommand));
            lstReturn.Add(new CPluginVariable("Commands|Confirm Selection", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));
            lstReturn.Add(new CPluginVariable("Commands|Cancel command", this.m_strCancelCommand.GetType(), this.m_strCancelCommand));
            lstReturn.Add(new CPluginVariable("Commands|Execute config command", this.m_strExecuteConfigCommand.GetType(), this.m_strExecuteConfigCommand));
            lstReturn.Add(new CPluginVariable("Commands|Reserve Player command", this.m_strReservedCommand.GetType(), this.m_strReservedCommand));

            lstReturn.Add(new CPluginVariable("Response Scope|Private Prefix", this.m_strPrivatePrefix.GetType(), this.m_strPrivatePrefix));
            lstReturn.Add(new CPluginVariable("Response Scope|Admins Prefix", this.m_strAdminsPrefix.GetType(), this.m_strAdminsPrefix));
            lstReturn.Add(new CPluginVariable("Response Scope|Public Prefix", this.m_strPublicPrefix.GetType(), this.m_strPublicPrefix));

            lstReturn.Add(new CPluginVariable("Commands Before Write to File", this.m_commandLimit.GetType(), this.m_commandLimit));

            lstReturn.Add(new CPluginVariable("Manually Pick Directory?", this.adjustLogDir.GetType(), this.adjustLogDir));
            lstReturn.Add(new CPluginVariable("Log Files Directory", this.logDir.GetType(), this.logDir));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Log Commands?", this.m_logCommands.GetType(), this.m_logCommands));

            lstReturn.Add(new CPluginVariable("Private Prefix", this.m_strPrivatePrefix.GetType(), this.m_strPrivatePrefix));
            lstReturn.Add(new CPluginVariable("Admins Prefix", this.m_strAdminsPrefix.GetType(), this.m_strAdminsPrefix));
            lstReturn.Add(new CPluginVariable("Public Prefix", this.m_strPublicPrefix.GetType(), this.m_strPublicPrefix));

            lstReturn.Add(new CPluginVariable("Kick", this.m_strKickCommand.GetType(), this.m_strKickCommand));
            lstReturn.Add(new CPluginVariable("Nuke", this.m_strNukeCommand.GetType(), this.m_strNukeCommand));
            lstReturn.Add(new CPluginVariable("Kill", this.m_strKillCommand.GetType(), this.m_strKillCommand));
            lstReturn.Add(new CPluginVariable("Move", this.m_strMoveCommand.GetType(), this.m_strMoveCommand));
            lstReturn.Add(new CPluginVariable("Force Move", this.m_strForceMoveCommand.GetType(), this.m_strForceMoveCommand));
            lstReturn.Add(new CPluginVariable("Temporary Ban", this.m_strTemporaryBanCommand.GetType(), this.m_strTemporaryBanCommand));
            lstReturn.Add(new CPluginVariable("Permanent Ban", this.m_strPermanentBanCommand.GetType(), this.m_strPermanentBanCommand));
            lstReturn.Add(new CPluginVariable("Say", this.m_strSayCommand.GetType(), this.m_strSayCommand));
            lstReturn.Add(new CPluginVariable("Player Say", this.m_strPlayerSayCommand.GetType(), this.m_strPlayerSayCommand));
            lstReturn.Add(new CPluginVariable("Yell", this.m_strYellCommand.GetType(), this.m_strYellCommand));
            lstReturn.Add(new CPluginVariable("Player Yell", this.m_strPlayerYellCommand.GetType(), this.m_strPlayerYellCommand));
            lstReturn.Add(new CPluginVariable("Restart Map", this.m_strRestartLevelCommand.GetType(), this.m_strRestartLevelCommand));
            lstReturn.Add(new CPluginVariable("Next Map", this.m_strNextLevelCommand.GetType(), this.m_strNextLevelCommand));
            lstReturn.Add(new CPluginVariable("Confirm Selection", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));
            lstReturn.Add(new CPluginVariable("Cancel command", this.m_strCancelCommand.GetType(), this.m_strCancelCommand));
            lstReturn.Add(new CPluginVariable("Execute config command", this.m_strExecuteConfigCommand.GetType(), this.m_strExecuteConfigCommand));
            lstReturn.Add(new CPluginVariable("Reserve Player command", this.m_strReservedCommand.GetType(), this.m_strReservedCommand));

            lstReturn.Add(new CPluginVariable("Commands Before Write to File", this.m_commandLimit.GetType(), this.m_commandLimit));

            lstReturn.Add(new CPluginVariable("Manually Pick Directory?", this.adjustLogDir.GetType(), this.adjustLogDir));
            lstReturn.Add(new CPluginVariable("Log Files Directory", this.logDir.GetType(), this.logDir));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {

            int commands = 100;

            if (strVariable.CompareTo("Manually Pick Directory?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                if (strValue.Equals("Yes"))
                {
                    FolderBrowserDialog dialog = new FolderBrowserDialog();
                    dialog.ShowDialog();
                    if (dialog.SelectedPath.Equals("") == false)
                    {           // User presses cancel, do not update
                        this.logDir = dialog.SelectedPath;
                        if (Directory.Exists(this.logDir) == false)
                        {
                            Directory.CreateDirectory(this.logDir);
                        }
                        if (File.Exists(this.logDir + "\\Total Admin Log.txt") == false)
                        {
                            File.Create(this.logDir + "\\Total Admin Log.txt");
                        }
                    }
                }
            }
            else if (strVariable.CompareTo("Log Commands?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_logCommands = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Commands Before Write to File") == 0 && int.TryParse(strValue, out commands) == true)
            {
                if (commands > 0)
                {
                    this.m_commandLimit = commands;
                }
            }
            else if (strVariable.CompareTo("Private Prefix") == 0)
            {
                this.m_strPrivatePrefix = strValue;
            }
            else if (strVariable.CompareTo("Admins Prefix") == 0)
            {
                this.m_strAdminsPrefix = strValue;
            }
            else if (strVariable.CompareTo("Public Prefix") == 0)
            {
                this.m_strPublicPrefix = strValue;
            }
            else if (strVariable.CompareTo("Kick") == 0)
            {
                this.m_strKickCommand = strValue;
            }
            else if (strVariable.CompareTo("Kill") == 0)
            {
                this.m_strKillCommand = strValue;
            }
            else if (strVariable.CompareTo("Nuke") == 0)
            {
                this.m_strNukeCommand = strValue;
            }
            else if (strVariable.CompareTo("Move") == 0)
            {
                this.m_strMoveCommand = strValue;
            }
            else if (strVariable.CompareTo("Force Move") == 0)
            {
                this.m_strForceMoveCommand = strValue;
            }
            else if (strVariable.CompareTo("Temporary Ban") == 0)
            {
                this.m_strTemporaryBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Permanent Ban") == 0)
            {
                this.m_strPermanentBanCommand = strValue;
            }
            else if (strVariable.CompareTo("Say") == 0)
            {
                this.m_strSayCommand = strValue;
            }
            else if (strVariable.CompareTo("Player Say") == 0)
            {
                this.m_strPlayerSayCommand = strValue;
            }
            else if (strVariable.CompareTo("Yell") == 0)
            {
                this.m_strYellCommand = strValue;
            }
            else if (strVariable.CompareTo("Player Yell") == 0)
            {
                this.m_strPlayerYellCommand = strValue;
            }
            else if (strVariable.CompareTo("Restart Map") == 0)
            {
                this.m_strRestartLevelCommand = strValue;
            }
            else if (strVariable.CompareTo("Next Map") == 0)
            {
                this.m_strNextLevelCommand = strValue;
            }
            else if (strVariable.CompareTo("Confirm Selection") == 0)
            {
                this.m_strConfirmCommand = strValue;
            }
            else if (strVariable.CompareTo("Cancel command") == 0)
            {
                this.m_strCancelCommand = strValue;
            }
            else if (strVariable.CompareTo("Execute config command") == 0)
            {
                this.m_strExecuteConfigCommand = strValue;
            }
            else if (strVariable.CompareTo("Reserve Player command") == 0)
            {
                this.m_strReservedCommand = strValue;
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
            if (this.m_dicVanillaInfo.ContainsKey(strSoldierName.ToLower()) != true)
            {
                this.m_dicVanillaInfo.Add(strSoldierName.ToLower(), new CPlayerInfo(strSoldierName, "", 0, 24));
            }

            // Admin Logging //
            CPrivileges cpSpeakerPrivs = this.GetAccountPrivileges(strSoldierName);
            if (cpSpeakerPrivs != null && cpSpeakerPrivs.PrivilegesFlags > 0)
            {
                if (this.adminCommands.ContainsKey(strSoldierName) == false)
                {
                    this.adminCommands.Add(strSoldierName, new string[this.m_commandLimit]);
                    this.commandCounter.Add(strSoldierName, 0);
                    this.adminCommands[strSoldierName].SetValue("", this.commandCounter[strSoldierName]);
                    this.commandCounter[strSoldierName]++;
                    this.adminCommands[strSoldierName].SetValue(strSoldierName + " Commands Starting at: " + DateTime.Now.ToString(), this.commandCounter[strSoldierName]);
                    this.commandCounter[strSoldierName]++;
                }
            }
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {
            // Admin Logging //
            string[] commands;
            if (this.m_dicVanillaInfo.ContainsKey(strSoldierName.ToLower()) == true)
            {
                this.m_dicVanillaInfo.Remove(strSoldierName.ToLower());
            }
            if (this.adminCommands.ContainsKey(strSoldierName) == true)
            {
                StreamWriter tw = File.AppendText(this.logDir + "\\" + strSoldierName + ".txt");
                commands = this.adminCommands[strSoldierName];
                for (int i = 0; i < this.commandCounter[strSoldierName]; i++)
                {
                    tw.WriteLine(commands[i]);
                }
                tw.Close();
                this.adminCommands.Remove(strSoldierName);
                this.commandCounter.Remove(strSoldierName);
            }
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {
        }

        // Thanks Dr. Levenshtein and Sam Allen @ http://dotnetperls.com/levenshtein
        public static int Compute(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public class CMatchedDictionaryKey : IComparable<CMatchedDictionaryKey>
        {
            private string m_strLowerCaseMatchedText;
            private int m_iMatchedScoreCharacters;
            private int m_iMatchedScore;

            public CMatchedDictionaryKey(string strMatchedText)
            {
                this.m_strLowerCaseMatchedText = strMatchedText;
                this.m_iMatchedScoreCharacters = 0;
                this.m_iMatchedScore = int.MaxValue;
            }

            public int CompareTo(CMatchedDictionaryKey other)
            {
                return MatchedScore.CompareTo(other.MatchedScore);
            }

            public string LowerCaseMatchedText
            {
                get
                {
                    return this.m_strLowerCaseMatchedText;
                }
            }

            public int MatchedScoreCharacters
            {
                get
                {
                    return this.m_iMatchedScoreCharacters;
                }
                set
                {
                    this.m_iMatchedScoreCharacters = value;
                }
            }

            public int MatchedScore
            {
                get
                {
                    return this.m_iMatchedScore;
                }
                set
                {
                    this.m_iMatchedScore = value;
                }
            }
        }

        private int GetClosestMatch(string strArguments, List<string> lstDictionary, out string strSoldierName, out string strRemainderArguments)
        {
            int iSimilarity = int.MaxValue;
            int iScore = 0;

            strRemainderArguments = "";
            strSoldierName = "";

            if (lstDictionary.Count >= 1)
            {

                int iLargestDictionaryKey = 0;

                // Build array of default matches from the dictionary to store a rank for each match.
                // (it's designed to work on smaller dictionaries with say.. 32 player names in it =)
                List<CMatchedDictionaryKey> lstMatches = new List<CMatchedDictionaryKey>();
                foreach (string strDictionaryKey in lstDictionary)
                {
                    lstMatches.Add(new CMatchedDictionaryKey(strDictionaryKey.ToLower()));

                    if (strDictionaryKey.Length > iLargestDictionaryKey)
                    {
                        iLargestDictionaryKey = strDictionaryKey.Length;
                    }
                }

                // Rank each match, find the remaining characters for a match (arguements)
                for (int x = 1; x <= Math.Min(strArguments.Length, iLargestDictionaryKey); x++)
                {
                    // Skip it if it's a space (a space breaks a name and moves onto arguement.
                    // but the space could also be included in the dictionarykey, which will be checked
                    // on the next loop.
                    if (x + 1 < strArguments.Length && strArguments[x] != ' ')
                        continue;

                    for (int i = 0; i < lstMatches.Count; i++)
                    {
                        iScore = CAdminLogger.Compute(strArguments.Substring(0, x).ToLower(), lstMatches[i].LowerCaseMatchedText);

                        if (iScore < lstMatches[i].MatchedScore)
                        {
                            lstMatches[i].MatchedScore = iScore;
                            lstMatches[i].MatchedScoreCharacters = x;
                        }
                    }
                }

                // Sort the matches
                lstMatches.Sort();

                int iBestCharactersMatched = lstMatches[0].MatchedScoreCharacters;
                iSimilarity = lstMatches[0].MatchedScore;
                strSoldierName = lstMatches[0].LowerCaseMatchedText;

                // Now though we want to loop through from start to end and see if a subset of what we entered is found.
                // if so then this will find the highest ranked item with a subset of what was entered and favour that instead.
                string strBestCharsSubstringLower = strArguments.Substring(0, iBestCharactersMatched).ToLower();
                for (int i = 0; i < lstMatches.Count; i++)
                {
                    if (lstMatches[i].LowerCaseMatchedText.Contains(strBestCharsSubstringLower) == true)
                    {
                        iSimilarity = lstMatches[i].MatchedScore;
                        strSoldierName = lstMatches[i].LowerCaseMatchedText;
                        iBestCharactersMatched = lstMatches[i].MatchedScoreCharacters;

                        break;
                    }
                }

                if (iBestCharactersMatched < strArguments.Length)
                {
                    strRemainderArguments = strArguments.Substring(iBestCharactersMatched + 1);
                }
                else
                {
                    strRemainderArguments = strArguments.Substring(iBestCharactersMatched);
                }

            }

            return iSimilarity;
        }

        private struct SCommand
        {
            public string m_strResponseScope;
            public string m_strCommand;
            public string[] ma_strArguments;
            public string m_strExtraArguments;
        }

        private SCommand GetCommand(string strResponseScope, string strCommand, string strArguments, params string[] a_strSoldierName)
        {
            SCommand scReturnCommand = new SCommand();

            scReturnCommand.m_strResponseScope = strResponseScope;
            scReturnCommand.m_strCommand = strCommand;
            scReturnCommand.m_strExtraArguments = strArguments;
            scReturnCommand.ma_strArguments = a_strSoldierName;

            return scReturnCommand;
        }

        private void QueueResponse(string strScope, string strAccountName, string strMessage, string strTaskName, int iDelay, int iInterval, int iRepeat)
        {
        }

        private void QueueYellingResponse(string strScope, string strAccountName, string strMessage, string strTaskName, int iDelay, int iInterval, int iRepeat)
        {
        }

        private void SendYellingResponse(string strScope, string strAccountName, string strMessage)
        {
        }

        private void SendResponse(string strScope, string strAccountName, string strMessage)
        {
        }

        private string ShiftString(string strText, int iShift)
        {

            string strReturn = strText;

            if (strText.Length > 0)
            {
                if (iShift < 0)
                {
                    strReturn = String.Format("{0}{1}", strText.Substring(strText.Length - Math.Abs(iShift) % strText.Length), strText.Substring(0, strText.Length - Math.Abs(iShift) % strText.Length));
                }
                else if (iShift > 0)
                {
                    strReturn = String.Format("{0}{1}", strText.Substring(iShift % strText.Length), strText.Substring(0, iShift % strText.Length));
                }
                // else == 0 return the text as is.
            }
            return strReturn;
        }

        private CMap GetMap(string strMapFileName)
        {
            CMap cmReturn = null;

            if (this.m_lstMaps != null)
            {
                foreach (CMap cmMap in this.m_lstMaps)
                {
                    if (String.Compare(cmMap.FileName, strMapFileName, true) == 0)
                    {
                        cmReturn = cmMap;
                        break;
                    }
                }
            }

            return cmReturn;
        }

        private int GetTeamsForMap()
        {
            int iTeamsCount = 0;

            CMap cmCurrentMap = null;
            if ((cmCurrentMap = this.GetMap(this.m_strCurrentMapFileName)) != null)
            {
                iTeamsCount = cmCurrentMap.TeamNames.Count;
            }

            return iTeamsCount;
        }

        private bool TryGetTeamID(string strLocalizationKey, out int iTeamID)
        {

            bool blValidTeamForMap = false;
            iTeamID = 0;

            CMap cmCurrentMap = null;
            if ((cmCurrentMap = this.GetMap(this.m_strCurrentMapFileName)) != null)
            {
                foreach (CTeamName ctnTeam in cmCurrentMap.TeamNames)
                {
                    if (String.Compare(ctnTeam.LocalizationKey, strLocalizationKey, true) == 0)
                    {
                        iTeamID = ctnTeam.TeamID;
                        blValidTeamForMap = true;
                        break;
                    }
                }
            }

            return blValidTeamForMap;
        }

        private int GetSquadID(string strSquadName)
        {

            int iReturnSquadID = -1;

            if (this.m_dicSquadNames.ContainsKey(strSquadName) == true)
            {
                iReturnSquadID = this.m_dicSquadNames[strSquadName];
            }

            return iReturnSquadID;
        }

        private string GetTeamLocalizationKey(string strTeamName)
        {

            string strReturnLocalizationKey = "";

            foreach (KeyValuePair<string, string> kvpTeamName in this.m_dicTeamNames)
            {
                if (String.Compare(kvpTeamName.Value, strTeamName, true) == 0)
                {
                    strReturnLocalizationKey = kvpTeamName.Key;
                    break;
                }
            }

            return strReturnLocalizationKey;
        }

        private string CapatalizeFirstLetter(string strText)
        {
            return char.ToUpper(strText[0]) + strText.Substring(1).ToLower();
        }

        private Dictionary<string, SCommand> m_dicRequestConfirmationCommands = new Dictionary<string, SCommand>();
        private string AddConfirmationCommand(string strAccountName, SCommand scCommand)
        {
            if (this.m_dicRequestConfirmationCommands.ContainsKey(strAccountName) == true)
            {
                this.m_dicRequestConfirmationCommands[strAccountName] = scCommand;
            }
            else
            {
                this.m_dicRequestConfirmationCommands.Add(strAccountName, scCommand);
            }

            string strDidYouMeanCommand = "";

            if (String.Compare(scCommand.m_strCommand, this.m_strMoveCommand, true) == 0 || String.Compare(scCommand.m_strCommand, this.m_strForceMoveCommand, true) == 0)
            {
                if (scCommand.ma_strArguments.Length == 1)
                {
                    strDidYouMeanCommand = String.Format("{0}{1} {2}", scCommand.m_strResponseScope, scCommand.m_strCommand, this.m_dicVanillaInfo[scCommand.ma_strArguments[0].ToLower()].SoldierName);
                }
                else if (scCommand.ma_strArguments.Length == 2)
                {
                    strDidYouMeanCommand = String.Format("{0}{1} {2} {3}", scCommand.m_strResponseScope, scCommand.m_strCommand, this.m_dicVanillaInfo[scCommand.ma_strArguments[0].ToLower()].SoldierName, this.CapatalizeFirstLetter(scCommand.ma_strArguments[1]));
                }
                else if (scCommand.ma_strArguments.Length == 3)
                {
                    strDidYouMeanCommand = String.Format("{0}{1} {2} {3} {4}", scCommand.m_strResponseScope, scCommand.m_strCommand, this.m_dicVanillaInfo[scCommand.ma_strArguments[0].ToLower()].SoldierName, this.CapatalizeFirstLetter(scCommand.ma_strArguments[1]), this.CapatalizeFirstLetter(scCommand.ma_strArguments[2]));
                }
            }
            else if (String.Compare(scCommand.m_strCommand, this.m_strNukeCommand, true) == 0)
            {
                strDidYouMeanCommand = String.Format("{0}{1} {2}", scCommand.m_strResponseScope, scCommand.m_strCommand, this.CapatalizeFirstLetter(scCommand.ma_strArguments[0]));
            }
            else if (this.m_dicVanillaInfo.ContainsKey(scCommand.ma_strArguments[0].ToLower()) == true)
            {
                strDidYouMeanCommand = String.Format("{0}{1} {2}", scCommand.m_strResponseScope, scCommand.m_strCommand, this.m_dicVanillaInfo[scCommand.ma_strArguments[0].ToLower()].SoldierName);
            }

            return String.Format("Did you mean {0}?", strDidYouMeanCommand);
        }

        private DateTime m_dtCountdownBlocker = DateTime.Now;

        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
            Match mtcCommand = Regex.Match(strMessage, String.Format("(?<scope>{0})(?<command>{1})[ ]?(?<arguments>.*)", String.Format("{0}|{1}|{2}", this.m_strPrivatePrefix, this.m_strAdminsPrefix, this.m_strPublicPrefix), String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}|{15}|{16}|{17}", this.m_strKickCommand, this.m_strPermanentBanCommand, this.m_strTemporaryBanCommand, this.m_strSayCommand, this.m_strPlayerSayCommand, this.m_strRestartLevelCommand, this.m_strNextLevelCommand, this.m_strConfirmCommand, this.m_strCancelCommand, this.m_strPlayerWarnCommand, this.m_strExecuteConfigCommand, this.m_strYellCommand, this.m_strPlayerYellCommand, this.m_strKillCommand, this.m_strForceMoveCommand, this.m_strMoveCommand, this.m_strNukeCommand, this.m_strReservedCommand)), RegexOptions.IgnoreCase);
            try
            {
                CPrivileges cpSpeakerPrivs = this.GetAccountPrivileges(strSpeaker);
                if (cpSpeakerPrivs != null && cpSpeakerPrivs.PrivilegesFlags > 0)
                {
                    mtcCommand = Regex.Match(strMessage, String.Format("(?<scope>{0})(?<command>{1})[ ]?(?<arguments>.*)", String.Format("{0}|{1}|{2}", this.m_strPrivatePrefix, this.m_strAdminsPrefix, this.m_strPublicPrefix), String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}|{13}|{14}|{15}|{16}|{17}", this.m_strKickCommand, this.m_strPermanentBanCommand, this.m_strTemporaryBanCommand, this.m_strSayCommand, this.m_strPlayerSayCommand, this.m_strRestartLevelCommand, this.m_strNextLevelCommand, this.m_strConfirmCommand, this.m_strCancelCommand, this.m_strPlayerWarnCommand, this.m_strExecuteConfigCommand, this.m_strYellCommand, this.m_strPlayerYellCommand, this.m_strKillCommand, this.m_strForceMoveCommand, this.m_strMoveCommand, this.m_strNukeCommand, this.m_strReservedCommand)), RegexOptions.IgnoreCase);

                    if (mtcCommand.Success == true)
                    {
                        string response = "";
                        bool displayCommand = true;             // In case we get to situations where it isn't successful, but doesn't throw a "Did you mean"
                        string strTargetSoldierName = "";
                        string strRemainderArguements = "";
                        if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strKickCommand, true) == 0)
                        {
                            if (cpSpeakerPrivs.CanKickPlayers == true)
                            {
                                if (this.GetClosestMatch(mtcCommand.Groups["arguments"].Value, new List<string>(this.m_dicVanillaInfo.Keys), out strTargetSoldierName, out strRemainderArguements) == 0)
                                {
                                }
                                else
                                {
                                    // Issue Did you mean thingo..
                                    response = this.AddConfirmationCommand(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                                }
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strPermanentBanCommand, true) == 0)
                        {
                            if (cpSpeakerPrivs.CanTemporaryBanPlayers == true)
                            {
                                if (this.GetClosestMatch(mtcCommand.Groups["arguments"].Value, new List<string>(this.m_dicVanillaInfo.Keys), out strTargetSoldierName, out strRemainderArguements) == 0)
                                {
                                    //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                                }
                                else
                                {
                                    // Issue Did you mean thingo..
                                    response = this.AddConfirmationCommand(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                                }
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strReservedCommand, true) == 0)
                        {
                            if (cpSpeakerPrivs.CanEditReservedSlotsList == true)
                            {
                                string[] args = mtcCommand.Groups["arguments"].Value.Split(' ');
                                double dur;
                                if (args.Length < 2)
                                {
                                    displayCommand = false;
                                }
                                else if (double.TryParse(args[args.Length - 1], out dur) == false)
                                {
                                    displayCommand = false;
                                }
                                else
                                {

                                }
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strKillCommand, true) == 0)
                        {

                            if (cpSpeakerPrivs.CanKillPlayers == true)
                            {

                                if (this.GetClosestMatch(mtcCommand.Groups["arguments"].Value, new List<string>(this.m_dicVanillaInfo.Keys), out strTargetSoldierName, out strRemainderArguements) == 0)
                                {
                                    // Issue kill command.
                                    //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                                }
                                else
                                {
                                    // Issue Did you mean thingo..
                                    response = this.AddConfirmationCommand(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                                }
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strNukeCommand, true) == 0)
                        {

                            if (cpSpeakerPrivs.CanKillPlayers == true)
                            {

                                if (this.GetClosestMatch(mtcCommand.Groups["arguments"].Value, new List<string>(this.m_dicTeamNames.Values), out strTargetSoldierName, out strRemainderArguements) == 0)
                                {
                                    // Issue nuke command.
                                    //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                                }
                                else
                                {
                                    // Issue Did you mean thingo..
                                    response = this.AddConfirmationCommand(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                                }
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strMoveCommand, true) == 0 || String.Compare(mtcCommand.Groups["command"].Value, this.m_strForceMoveCommand, true) == 0)
                        {

                            // !move Phogue - Moves Phogue to the opposite team
                            // !move Phogue Defenders - Moves Phogue to Defenders with no squad
                            // !move Phogue Alpha - Moves Phogue to Alpha squad on the same team or to Team Alpha in SQDM
                            // !move Phogue Defenders Alpha - Move Phogue to Defenders on squad Alpha
                            if (cpSpeakerPrivs.CanMovePlayers == true)
                            {

                                List<string> lstArguments = new List<string>();
                                bool blExactMatch = true;

                                string strClosestNameMatch = "";
                                string strClosestTeamMatch = "";
                                string strClosestSquadMatch = "";

                                int iNameMatch = GetClosestMatch(mtcCommand.Groups["arguments"].Value, new List<string>(this.m_dicVanillaInfo.Keys), out strClosestNameMatch, out strRemainderArguements);
                                int iTeamMatch = GetClosestMatch(strRemainderArguements, new List<string>(this.m_dicTeamNames.Values), out strClosestTeamMatch, out strRemainderArguements);
                                int iSquadMatch = GetClosestMatch(strRemainderArguements, new List<string>(this.m_dicSquadNames.Keys), out strClosestSquadMatch, out strRemainderArguements);

                                if (strClosestNameMatch.Length > 0) { lstArguments.Add(strClosestNameMatch); }
                                if (strClosestTeamMatch.Length > 0 && iTeamMatch != int.MaxValue) { lstArguments.Add(strClosestTeamMatch); }
                                if (strClosestSquadMatch.Length > 0 && iSquadMatch != int.MaxValue) { lstArguments.Add(strClosestSquadMatch); }

                                blExactMatch = iNameMatch != int.MaxValue ? (iNameMatch == 0 ? blExactMatch : false) : blExactMatch;
                                blExactMatch = iTeamMatch != int.MaxValue ? (iTeamMatch == 0 ? blExactMatch : false) : blExactMatch;
                                blExactMatch = iSquadMatch != int.MaxValue ? (iSquadMatch == 0 ? blExactMatch : false) : blExactMatch;

                                if (blExactMatch == true)
                                {
                                    // Issue kill command.
                                    //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, lstArguments.ToArray()));
                                }
                                else
                                {
                                    // Issue Did you mean thingo..
                                    response = this.AddConfirmationCommand(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, lstArguments.ToArray()));
                                }
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strTemporaryBanCommand, true) == 0)
                        {

                            if (cpSpeakerPrivs.CanTemporaryBanPlayers == true)
                            {

                                if (this.GetClosestMatch(mtcCommand.Groups["arguments"].Value, new List<string>(this.m_dicVanillaInfo.Keys), out strTargetSoldierName, out strRemainderArguements) == 0)
                                {
                                    // Issue ban command.
                                    //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                                }
                                else
                                {
                                    // Issue Did you mean thingo..
                                    response = this.AddConfirmationCommand(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                                }
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strConfirmCommand, true) == 0)
                        {
                            if (this.m_dicRequestConfirmationCommands.ContainsKey(strSpeaker) == true)
                            {
                                //this.logger(strSpeaker, this.m_dicRequestConfirmationCommands[strSpeaker]);
                                this.m_dicRequestConfirmationCommands.Remove(strSpeaker);
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strCancelCommand, true) == 0)
                        {
                            response = "Cancelled Command";
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strSayCommand, true) == 0)
                        {
                            //this.logger("procon.protected.send", "admin.say", mtcCommand.Groups["arguments"].Value, "all");
                            //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, mtcCommand.Groups["arguments"].Value, "all"));
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strYellCommand, true) == 0)
                        {
                            //this.logger("procon.protected.send", "admin.yell", mtcCommand.Groups["arguments"].Value, this.m_strShowMessageLength, "all");
                            //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, mtcCommand.Groups["arguments"].Value, "all"));
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strRestartLevelCommand, true) == 0)
                        {
                            if (cpSpeakerPrivs.CanUseMapFunctions == true)
                            {

                                if (this.m_dtCountdownBlocker < DateTime.Now)
                                {

                                    int iTimeout = 0;

                                    if (mtcCommand.Groups["arguments"].Value.Length > 0)
                                    {
                                        if (int.TryParse(mtcCommand.Groups["arguments"].Value, out iTimeout) == true)
                                        {
                                        }
                                        else
                                        {
                                            displayCommand = false;
                                        }
                                    }
                                    else
                                    {
                                    }
                                }
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strNextLevelCommand, true) == 0)
                        {
                            // This is identical to above really, I'll split it off into a method later.

                            if (cpSpeakerPrivs.CanUseMapFunctions == true)
                            {

                                if (this.m_dtCountdownBlocker < DateTime.Now)
                                {

                                    int iTimeout = 0;

                                    if (mtcCommand.Groups["arguments"].Value.Length > 0)
                                    {
                                        if (int.TryParse(mtcCommand.Groups["arguments"].Value, out iTimeout) == true)
                                        {
                                        }
                                        else
                                        {
                                            displayCommand = false;
                                        }
                                    }
                                    else
                                    {
                                    }
                                }
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strPlayerWarnCommand, true) == 0)
                        {
                            if (this.GetClosestMatch(mtcCommand.Groups["arguments"].Value, new List<string>(this.m_dicVanillaInfo.Keys), out strTargetSoldierName, out strRemainderArguements) == 0)
                            {
                                //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                            }
                            else
                            {
                                // Issue Did you mean thingo..
                                response = this.AddConfirmationCommand(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strPlayerYellCommand, true) == 0 || String.Compare(mtcCommand.Groups["command"].Value, this.m_strPlayerSayCommand, true) == 0)
                        {
                            if (this.GetClosestMatch(mtcCommand.Groups["arguments"].Value, new List<string>(this.m_dicVanillaInfo.Keys), out strTargetSoldierName, out strRemainderArguements) == 0)
                            {
                                //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                            }
                            else
                            {
                                // Issue Did you mean thingo..
                                response = this.AddConfirmationCommand(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                            }
                        }
                        else if (String.Compare(mtcCommand.Groups["command"].Value, this.m_strExecuteConfigCommand, true) == 0)
                        {
                            //this.logger(strSpeaker, this.GetCommand(mtcCommand.Groups["scope"].Value, mtcCommand.Groups["command"].Value, strRemainderArguements, strTargetSoldierName));
                            //if(File.Exists(@Directory.GetCurrentDirectory() + "\\Configs"))
                        }
                        if (this.m_logCommands.Equals(enumBoolYesNo.Yes))
                        {
                            if (response.Equals("") == true && displayCommand == true)
                            {       // Successful command
                                if (strMessage.IndexOf("/") == 0)
                                {           // Admin is hiding the command to the rest of the server
                                    strMessage = strMessage.Remove(0, 1);
                                }
                                if (strMessage.IndexOf(this.m_strPrivatePrefix) == 0 || strMessage.IndexOf(this.m_strAdminsPrefix) == 0 || strMessage.IndexOf(this.m_strPublicPrefix) == 0)
                                {
                                    strMessage = strMessage.Remove(0, 1);
                                }
                                DateTime rightNow = DateTime.Now;
                                if (this.adminCommands.ContainsKey(strSpeaker) == false)
                                {
                                    OnPlayerJoin(strSpeaker);
                                }
                                this.adminCommands[strSpeaker].SetValue(rightNow.ToString() + ": " + strSpeaker + " --> " + strMessage, this.commandCounter[strSpeaker]);
                                this.commandCounter[strSpeaker]++;
                                this.adminCommands["Total Admin Log"].SetValue(rightNow.ToString() + ": " + strSpeaker + " --> " + strMessage, this.commandCounter["Total Admin Log"]);
                                this.commandCounter["Total Admin Log"]++;
                            }
                            else if (response.Equals("") == false && displayCommand == true)
                            {                           // Message sent back to player
                                if (response.IndexOf("/") == 0)
                                {           // Admin is hiding the command to the rest of the server
                                    response = response.Remove(0, 1);
                                }
                                if (response.IndexOf(this.m_strPrivatePrefix) == 0 || response.IndexOf(this.m_strAdminsPrefix) == 0 || response.IndexOf(this.m_strPublicPrefix) == 0)
                                {
                                    response = response.Remove(0, 1);
                                }
                                DateTime rightNow = DateTime.Now;
                                if (this.adminCommands.ContainsKey(strSpeaker) == false)
                                {
                                    OnPlayerJoin(strSpeaker);
                                }
                                this.adminCommands[strSpeaker].SetValue(rightNow.ToString() + ": " + strSpeaker + " <-- " + "(server) " + response, this.commandCounter[strSpeaker]);
                                this.commandCounter[strSpeaker]++;
                                this.adminCommands["Total Admin Log"].SetValue(rightNow.ToString() + ": " + strSpeaker + " <-- " + "(server) " + response, this.commandCounter["Total Admin Log"]);
                                this.commandCounter["Total Admin Log"]++;
                            }
                            if (this.commandCounter["Total Admin Log"] == this.m_commandLimit)
                            {
                                writeCommandsToFile("Total Admin Log");
                            }
                            if (this.commandCounter[strSpeaker] == this.m_commandLimit)
                            {
                                writeCommandsToFile(strSpeaker);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", e.InnerException + " " + e.Message);
            }

        }

        public void InterfaceChangeLogging(string logMessage)
        {
            string strSpeaker = "changesWithinInterface";
            if (this.m_logCommands.Equals(enumBoolYesNo.Yes))
            {
                DateTime rightNow = DateTime.Now;
                if (this.adminCommands.ContainsKey(strSpeaker) == false)
                {
                    this.adminCommands.Add(strSpeaker, new string[this.m_commandLimit]);
                    this.commandCounter.Add(strSpeaker, 0);
                    this.adminCommands[strSpeaker].SetValue("", this.commandCounter[strSpeaker]);
                    this.commandCounter[strSpeaker]++;
                    this.adminCommands[strSpeaker].SetValue("Server Changes Starting at: " + DateTime.Now.ToString(), this.commandCounter[strSpeaker]);
                    this.commandCounter[strSpeaker]++;
                }
                this.adminCommands[strSpeaker].SetValue(rightNow.ToString() + ": " + logMessage, this.commandCounter[strSpeaker]);
                this.commandCounter[strSpeaker]++;
                this.adminCommands["Total Admin Log"].SetValue(rightNow.ToString() + ": " + logMessage, this.commandCounter["Total Admin Log"]);
                this.commandCounter["Total Admin Log"]++;
            }
            if (this.commandCounter["Total Admin Log"] >= this.m_commandLimit)
            {
                writeCommandsToFile("Total Admin Log");
            }
            if (this.commandCounter[strSpeaker] >= this.m_commandLimit)
            {
                writeCommandsToFile(strSpeaker);
            }
        }

        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
            this.OnGlobalChat(strSpeaker, strMessage);
        }

        public void OnLoadingLevel(string strMapFileName)
        {
            this.m_strCurrentMapFileName = strMapFileName;
            writeCommandsToFile("all");
        }

        private void writeCommandsToFile(string subset)
        {
            string[] commands;
            if (subset.Equals("all") || subset.Equals("Total Admin Log"))
            {
                if (this.adminCommands.ContainsKey("Total Admin Log") == true)
                {
                    StreamWriter tw = File.AppendText(this.logDir + "\\Total Admin Log.txt");
                    commands = this.adminCommands["Total Admin Log"];
                    for (int i = 0; i < this.commandCounter["Total Admin Log"]; i++)
                    {
                        tw.WriteLine(commands[i]);
                    }
                    tw.Close();
                    this.adminCommands["Total Admin Log"] = new string[this.m_commandLimit];
                    this.commandCounter["Total Admin Log"] = 0;
                }
            }
            if (subset.Equals("all"))
            {
                List<string> admins = new List<string>(this.adminCommands.Keys);
                foreach (string player in admins)
                {
                    StreamWriter tw = File.AppendText(this.logDir + "\\" + player + ".txt");
                    commands = this.adminCommands[player];
                    for (int i = 0; i < this.commandCounter[player]; i++)
                    {
                        tw.WriteLine(commands[i]);
                    }
                    tw.Close();
                    this.adminCommands[player] = new string[this.m_commandLimit];
                    this.commandCounter[player] = 0;
                }
            }
            else
            {                           // Write to file a specific admin
                string player = subset;
                StreamWriter tw = File.AppendText(this.logDir + "\\" + player + ".txt");
                commands = this.adminCommands[player];
                for (int i = 0; i < this.commandCounter[player]; i++)
                {
                    tw.WriteLine(commands[i]);
                }
                tw.Close();
                this.adminCommands[player] = new string[this.m_commandLimit];
                this.commandCounter[player] = 0;
            }
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

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {
        }

        public void OnPlayerSquadChange(string strSoldierName, int iTeamID, int iSquadID)
        {
        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (this.m_dicVanillaInfo.ContainsKey(cpiPlayer.SoldierName.ToLower()) == true)
                    {
                        this.m_dicVanillaInfo[cpiPlayer.SoldierName.ToLower()] = cpiPlayer;
                    }
                    else
                    {
                        this.m_dicVanillaInfo.Add(cpiPlayer.SoldierName.ToLower(), cpiPlayer);
                    }
                }
            }
        }

        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {

        }

        public void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {

        }

        public void OnServerName(string strServerName)
        {

        }

        // Banning and Banlist Events
        public void OnBanList(List<CBanInfo> lstBans)
        {

        }

        public void OnBanAdded(CBanInfo cbiBan)
        {
            string message = "Someone added: ";
            try
            {
                message = message + cbiBan.SoldierName;
            }
            catch
            {
                try
                {
                    message = message + cbiBan.Guid;
                }
                catch
                {
                    try
                    {
                        message = message + cbiBan.IpAddress;
                    }
                    catch
                    {
                        message = message + "<something weird happened here>";
                    }
                }
            }
            message = message + " to the Ban List. Reason: " + cbiBan.Reason;
            InterfaceChangeLogging(message);
        }

        public void OnBanRemoved(CBanInfo cbiUnban)
        {
            string message = "Someone removed: ";
            try
            {
                message = message + cbiUnban.SoldierName;
            }
            catch
            {
                try
                {
                    message = message + cbiUnban.Guid;
                }
                catch
                {
                    try
                    {
                        message = message + cbiUnban.IpAddress;
                    }
                    catch
                    {
                        message = message + "<something weird happened here>";
                    }
                }
            }
            message = message + " from the Ban List";
            InterfaceChangeLogging(message);
        }

        public void OnBanListClear()
        {
            string message = "Someone cleared the Ban List";
            InterfaceChangeLogging(message);
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
            string message = "Someone added: " + strSoldierName + " to the Reserved Slots List";
            InterfaceChangeLogging(message);
        }

        public void OnReservedSlotsPlayerRemoved(string strSoldierName)
        {
            string message = "Someone removed: " + strSoldierName + " from the Reserved Slots List";
            InterfaceChangeLogging(message);
        }

        public void OnReservedSlotsCleared()
        {
            string message = "Someone cleared the Reserved Slots List";
            InterfaceChangeLogging(message);
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
            string message = "Someone appended the Map List with: " + strMapFileName;
            InterfaceChangeLogging(message);
        }

        public void OnMaplistMapRemoved(int iMapIndex)
        {
            string message = "Someone removed the map at index: " + iMapIndex.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnMaplistCleared()
        {
            string message = "Someone cleared the Map List";
            InterfaceChangeLogging(message);
        }

        public void OnMaplistList(List<string> lstMapFileNames)
        {

        }

        public void OnMaplistNextLevelIndex(int iMapIndex)
        {

        }

        public void OnMaplistMapInserted(int iMapIndex, string strMapFileName)
        {
            string message = "Someone inserted the map: " + strMapFileName + " into index: " + iMapIndex.ToString();
            InterfaceChangeLogging(message);
        }

        // Vars
        public void OnGamePassword(string strGamePassword)
        {
            string message = "Someone adjusted the Game Password to: " + strGamePassword;
            InterfaceChangeLogging(message);
        }

        public void OnPunkbuster(bool blEnabled)
        {
            string message = "Someone adjusted if the Server Uses PunkBuster to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnHardcore(bool blEnabled)
        {
            string message = "Someone adjusted if the Server is Hardcore to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnRanked(bool blEnabled)
        {
            string message = "Someone adjusted if the Server is Ranked to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnRankLimit(int iRankLimit)
        {
            string message = "Someone adjusted Rank Limit to: " + iRankLimit.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnTeamBalance(bool blEnabled)
        {
            string message = "Someone adjusted Team Balance to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnFriendlyFire(bool blEnabled)
        {
            string message = "Someone adjusted Friendly Fire to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnMaxPlayerLimit(int iMaxPlayerLimit)
        {
            string message = "Someone adjusted the Max Player Limit to: " + iMaxPlayerLimit.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnCurrentPlayerLimit(int iCurrentPlayerLimit)
        {
            string message = "Someone adjusted the Current Player Limit to: " + iCurrentPlayerLimit.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnPlayerLimit(int iPlayerLimit)
        {
            string message = "Someone adjusted the Player Limit to: " + iPlayerLimit.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnBannerURL(string strURL)
        {
            string message = "Someone adjusted the Banner URL to: " + strURL;
            InterfaceChangeLogging(message);
        }

        public void OnServerDescription(string strServerDescription)
        {
            string message = "Someone adjusted the Server Description to: " + strServerDescription;
            InterfaceChangeLogging(message);
        }

        public void OnKillCam(bool blEnabled)
        {
            string message = "Someone adjusted Kill Cam to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnMiniMap(bool blEnabled)
        {
            string message = "Someone adjusted Mini Map to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnCrossHair(bool blEnabled)
        {
            string message = "Someone adjusted CrossHair to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void On3dSpotting(bool blEnabled)
        {
            string message = "Someone adjusted 3D Spotting to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnMiniMapSpotting(bool blEnabled)
        {
            string message = "Someone adjusted Mini Map Spotting to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnThirdPersonVehicleCameras(bool blEnabled)
        {
            string message = "Someone adjusted the Third Person Vehicle Cameras to: " + blEnabled.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnTeamKillCountForKick(int iLimit)
        {
            string message = "Someone adjusted the Team Kill Count For Kick to: " + iLimit.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnTeamKillValueIncrease(int iLimit)
        {
            string message = "Someone adjusted the Team Kill Value to: " + iLimit.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnTeamKillValueDecreasePerSecond(int iLimit)
        {
            string message = "Someone adjusted the Team Kill Decrease Per Second to: " + iLimit.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnTeamKillValueForKick(int iLimit)
        {
            string message = "Someone adjusted the Team Kill Value For Kick to: " + iLimit.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnIdleTimeout(int iLimit)
        {
            string message = "Someone adjusted the Idle Timeout to: " + iLimit.ToString();
            InterfaceChangeLogging(message);
        }

        public void OnProfanityFilter(bool isEnabled)
        {
            string message = "Someone adjusted the Profanity Filter to: " + isEnabled.ToString();
            InterfaceChangeLogging(message);
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
            string message = "Someone cleared the level variables";
            InterfaceChangeLogging(message);
        }

        public void OnLevelVariablesSet(LevelVariable lvRequestedContext)
        {
            string message = "Someone adjusted a level varible";
            InterfaceChangeLogging(message);
        }

        public void OnLevelVariablesGet(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue)
        {

        }
    }
}