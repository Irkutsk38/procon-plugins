/*  DERIVED FROM CSpambot.cs:
 * 
 * Copyright 2010 Geoffrey 'Phogue' Green

    This file is part of BFBC2 PRoCon.

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
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class SimpleCommandRepeater : PRoConPluginAPI, IPRoConPluginInterface
    {

        public enum MessageType { Warning, Error, Exception, Normal, Debug };

        public int DebugLevel = 2;

        private List<string> m_lstCommands;
        //private int m_iDisplayTime;
        private int m_iIntervalBetweenCommands;

        // Status
        private string m_strServerGameType;
        private string m_strGameMod;
        private string m_strServerVersion;
        private string m_strPRoConVersion;

        //private int m_iYellDivider;
        //private bool m_blHasYellDuration;

        private bool m_blPluginEnabled = false;
        private bool m_blServerTypeChecked = false;

        //private enumBoolYesNo m_enYellResponses;

        public SimpleCommandRepeater()
        {
            this.m_lstCommands = new List<string>();
            this.m_lstCommands.Add("vars.preset \"Normal\"");

            this.m_iIntervalBetweenCommands = 600;

            this.m_strServerGameType = String.Empty;
        }

        public string GetPluginName()
        {
            return "Simple Command Repeater";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.2";
        }

        public string GetPluginAuthor()
        {
            return "PapaCharlie9";
        }

        public string GetPluginWebsite()
        {
            return "myrcon.com";
        }

        public string GetPluginDescription()
        {
            return @"
<h3>Repeats server commands at a set interval.</h3>
<p>Each command is executed at the specified interval in seconds.</p>
<h4>Commands</h4>
<p>A list of commands. Put each command on a separate line with its parameters. Separate the parameters with spaces.
Use double quotes around string arguments. For example:<br>
<pre>admin.say ""Welcome to the world's best server!"" all</pre></p>
<h4>Interval between commands (seconds)</h4>
<p>Specify the amount of time in seconds between execution of the list of commands. The entire list is
executed after the interval has elapsed. It is not possible to set separate intervals for each
command.</p>
<h4>Debug level</h4>
<p>Set to higher numbers for more debug logging.</p>
";
        }


        public void OnPluginLoadingEnv(List<string> lstPluginEnv)
        {
            foreach (String env in lstPluginEnv)
            {
                DebugWrite("^9OnPluginLoadingEnv: " + env, 8);
            }
            m_strServerGameType = lstPluginEnv[1];

            ConsoleWrite("^1Game Version = " + m_strServerGameType, 0);

        }



        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            // This is just so procon knows this plugin wants to override the default
            // "fire every event" setting when no events are registered.
            this.RegisterEvents(this.GetType().Name, "OnPluginLoaded");
        }

        public void OnPluginEnable()
        {
            this.m_blPluginEnabled = true;
            this.UpdateCommands();

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bSimpleCommandRepeater ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.m_blPluginEnabled = false;
            DebugWrite("Removing all tasks", 3);
            this.ExecuteCommand("procon.protected.tasks.remove", "SimpleCommandRepeater");

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bSimpleCommandRepeater ^1Disabled =(");
        }

        private void UpdateCommands()
        {
            this.ExecuteCommand("procon.protected.tasks.remove", "SimpleCommandRepeater");

            if (this.m_blPluginEnabled == true)
            {

                DebugWrite("Updating commands: removing old tasks first", 3);

                int iDelay = 1;
                foreach (string strCommand in this.m_lstCommands)
                {

                    // Parse command string into words
                    List<String> words = new List<String>();
                    int state = 0;
                    StringBuilder buf = new StringBuilder();

                    foreach (Char c in strCommand)
                    {
                        switch (state)
                        {
                            case 0: // normal whitespace separated words
                                if (c == ' ')
                                {
                                    if (buf.Length > 0)
                                    {
                                        words.Add(buf.ToString().Trim());
                                        buf = new StringBuilder();
                                    }
                                }
                                else if (c == '"')
                                {
                                    if (buf.Length > 0)
                                    {
                                        words.Add(buf.ToString().Trim());
                                        buf = new StringBuilder();
                                    }
                                    state = 1;
                                }
                                else
                                {
                                    buf.Append(c);
                                }
                                break;
                            case 1: // inside quoted string
                                if (c == '"')
                                {
                                    if (buf.Length > 0)
                                    {
                                        words.Add(buf.ToString());
                                        buf = new StringBuilder();
                                    }
                                    state = 0;
                                }
                                else
                                {
                                    buf.Append(c);
                                }
                                break;
                        }
                    }
                    if (buf.Length > 0)
                    {
                        words.Add(buf.ToString().Trim());
                        buf = new StringBuilder();
                    }

                    if (DebugLevel >= 4)
                    {
                        int w = 0;
                        foreach (String s in words)
                        {
                            DebugWrite("Word[" + w + "]: (" + s + ")", 4);
                            ++w;
                        }
                    }

                    DebugWrite("Adding task: " + strCommand, 3);

                    switch (words.Count)
                    {

                        case 0:
                            ConsoleWarn("Empty command!");
                            continue;

                        case 1:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0]
                            );
                            break;

                        case 2:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0],
                                words[1]
                            );
                            break;

                        case 3:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0],
                                words[1],
                                words[2]
                            );
                            break;

                        case 4:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0],
                                words[1],
                                words[2],
                                words[3]
                            );
                            break;

                        case 5:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0],
                                words[1],
                                words[2],
                                words[3],
                                words[4]
                            );
                            break;

                        case 6:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0],
                                words[1],
                                words[2],
                                words[3],
                                words[4],
                                words[5]
                            );
                            break;

                        case 7:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0],
                                words[1],
                                words[2],
                                words[3],
                                words[4],
                                words[5],
                                words[6]
                            );
                            break;

                        case 8:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0],
                                words[1],
                                words[2],
                                words[3],
                                words[4],
                                words[5],
                                words[6],
                                words[7]
                            );
                            break;

                        case 9:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0],
                                words[1],
                                words[2],
                                words[3],
                                words[4],
                                words[5],
                                words[6],
                                words[7],
                                words[8]
                            );
                            break;

                        case 10:
                            this.ExecuteCommand("procon.protected.tasks.add",
                                "SimpleCommandRepeater",
                                iDelay.ToString(),
                                this.m_iIntervalBetweenCommands.ToString(),
                                "-1",
                                "procon.protected.send",
                                words[0],
                                words[1],
                                words[2],
                                words[3],
                                words[4],
                                words[5],
                                words[6],
                                words[7],
                                words[8],
                                words[9]
                            );
                            break;

                        default:
                            ConsoleWarn("Command must be less than 10 words long!");
                            continue;
                    }


                    iDelay += 1;
                }
            }
        }

        // GetDisplayPluginVariables and GetPluginVariables

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Commands|Commands", typeof(string[]), this.m_lstCommands.ToArray()));

            lstReturn.Add(new CPluginVariable("Commands|Interval between commands (seconds)", this.m_iIntervalBetweenCommands.GetType(), this.m_iIntervalBetweenCommands));

            lstReturn.Add(new CPluginVariable("Commands|Debug level", this.DebugLevel.GetType(), this.DebugLevel));


            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Commands", typeof(string[]), this.m_lstCommands.ToArray()));

            lstReturn.Add(new CPluginVariable("Interval between commands (seconds)", this.m_iIntervalBetweenCommands.GetType(), this.m_iIntervalBetweenCommands));

            lstReturn.Add(new CPluginVariable("Debug level", this.DebugLevel.GetType(), this.DebugLevel));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iTimeSeconds = 8;
            int level;

            if (strVariable.CompareTo("Commands") == 0)
            {
                this.m_lstCommands = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Interval between commands (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iIntervalBetweenCommands = iTimeSeconds;
            }
            else if (strVariable.CompareTo("Debug level") == 0 && int.TryParse(strValue, out level) == true)
            {
                DebugLevel = level;
            }

            this.UpdateCommands();
        }

        private String FormatMessage(String msg, MessageType type, int level)
        {
            String prefix = "[^b" + GetPluginName() + "^n]:" + level + " ";

            if (Thread.CurrentThread.Name != null) prefix += "Thread(^b^5" + Thread.CurrentThread.Name + "^0^n): ";

            if (type.Equals(MessageType.Warning))
                prefix += "^1^bWARNING^0^n: ";
            else if (type.Equals(MessageType.Error))
                prefix += "^1^bERROR^0^n: ";
            else if (type.Equals(MessageType.Exception))
                prefix += "^1^bEXCEPTION^0^n: ";
            else if (type.Equals(MessageType.Debug))
                prefix += "^9^bDEBUG^n: ";

            return prefix + msg;
        }


        public void LogWrite(String msg)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
        }

        public void ConsoleWrite(String msg, MessageType type, int level)
        {
            LogWrite(FormatMessage(msg, type, level));
        }

        public void ConsoleWrite(String msg, int level)
        {
            ConsoleWrite(msg, MessageType.Normal, level);
        }

        public void ConsoleWarn(String msg)
        {
            ConsoleWrite(msg, MessageType.Warning, 1);
        }

        public void ConsoleError(String msg)
        {
            ConsoleWrite(msg, MessageType.Error, 0);
        }

        public void ConsoleException(Exception e)
        {
            if (DebugLevel >= 3) ConsoleWrite(e.ToString(), MessageType.Exception, 3);
        }

        public void DebugWrite(String msg, int level)
        {
            if (DebugLevel >= level) ConsoleWrite(msg, MessageType.Normal, level);
        }



        private void ServerCommand(params String[] args)
        {
            List<String> list = new List<String>();
            list.Add("procon.protected.send");
            list.AddRange(args);
            this.ExecuteCommand(list.ToArray());
        }

    }
}