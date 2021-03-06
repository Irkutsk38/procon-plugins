/*  
	Copyright 2012 DeadWalking  http://gibthis.com
	
	This file was created for PROCon. http://www.phogue.net/forumvb/forum.php
	
	This is my second delve into coding C# and plugins for PROCon. 
 
    This file is part of DeadWalking's Plugins for PROCon Admin RCon tool.

    DeadWalking's Plugins for PROCon are free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
	
	This plugin is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with PROCon.  If not, see <http://www.gnu.org/licenses/>.
 */


//Some of these includes are not used and I need to go thru and remove what is unused.
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

//Some of these includes are not used and I need to go thru and remove what is unused.
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;
using PRoCon.Core.HttpServer;
using PRoCon.Core.Remote;
using PRoCon.Core.Logging;
using PRoCon.Core.Lists;

namespace PRoConEvents
{

    #region BF3AntiSpawnCamp
    public class BF3AntiSpawnCamp : PRoConPluginAPI, IPRoConPluginInterface
    {
        const int SrciptDebugVar = 0;
        private bool iFirstRun = false;

        private bool bf3asc_blPluginEnabled;

        private List<PlayerInfos> bf3asc_lstPlayerInfos = new List<PlayerInfos>();

        private enumBoolOnOff bf3asc_enAdminDebugHeader = enumBoolOnOff.Off;
        private enumBoolOnOff bf3asc_enAdminSaySlayAlive = enumBoolOnOff.Off;
        //private enumBoolOnOff bf3asc_enAdminSaySlayDead = enumBoolOnOff.Off;
        private enumBoolOnOff bf3asc_enAdminSayComitted = enumBoolOnOff.Off;
        private enumBoolOnOff bf3asc_enAdminSayPunish = enumBoolOnOff.On;

        private int bf3asc_iRoundTime = 0;
        private int bf3asc_iTimeSpawn2Death = 3;
        private int bf3asc_iMaxInfractions = 5;
        private int bf3asc_iMaxPunishInfractions = 3;
        private int bf3asc_iInfractionsCoolDown = 60;
        private int bf3asc_iPunishTime = 10;

        private int bf3asc_iDebugLevel = 0;

        private string bf3asc_strActionType = "Auto";
        private string bf3asc_strPunishCommand = "punish";
        private string bf3asc_strAdminHeader = "*****Anti Spawn Kill*****";
        private string bf3asc_strAdminSaySlayAlive = "Slaying {0} for Spawn Killing";
        //private string bf3asc_strAdminSaySlayDead = "{1} Defeated the Spawn Killer Formerly know as {0} after {2} of {3} Spawn Kills";
        private string bf3asc_strAdminSayComitted = "{0}: has committed {2} of {3} allowed Spawn Kills";
        private string bf3asc_strAdminSayPunish = "{1}: has {2}second(s) to type !{3} to punish {0}";

        public BF3AntiSpawnCamp()
        {

        }

        #region PluginInfo	
        public string GetPluginName()
        {
            return "BF3AntiSpawnCamp";
        }

        public string GetPluginVersion()
        {
            return "0.5 Beta";
        }

        public string GetPluginAuthor()
        {
            return "DeadWalking";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net/forumvb/showthread.php?3790-BF3AntiSpawnCamp-0-6-Beta-(07-02-2012)";
        }

        //In HTML format?
        public string GetPluginDescription()
        {
            return @"
					<blockquote>
						<h2>Prelude</h2>
							<p>A small plugin to monitor and punish, if necessary, players that seem to be repeatedly spawn killing/camping.<br>
							Plugin can Automatically Punish or allow Spawn Killed players to Punish, depending on if the maximum infractions have been reached.<br></p>
							<h2></h2>
					</blockquote>
					<blockquote>
						<h2>AntiSpawnCamp Debug</h2>
							<h4>Debug Depth</h4>
							<ul>
								<li>0 - provides no debug info and keeps the plugin console free of clutter from <b>this</b> plugin.</li>
								<li>1 - will provide some minimal info regarding players, and is a good way to make sure the plugin is actually working.</li>
								<li>2 - is more for me, but can provide some extra debug info if you think that the plugin is acting oddly. This info can possibly help me to fix an issue in the future.</li>
							</ul>
						<h2></h2>
						<h2>AntiSpawnCamp Settings</h2>
							<h4># of seconds between Spawn and Death</h4>
							<ul>
								<li>Determine how long you want to monitor a player after spawning.</li>
								<li>Maximum amount of time is 10 seconds, there really is no need to track after that.</li>
								<li><b>Keep in mind that a player does not have control over their soldier for about 2 seconds after the server registers them spawning into game.</b></li>
							</ul>
							<h4>Infractions cool down time in seconds</h4>
							<ul>
								<li>For every x number of seconds, without committing a spawn kill(according to the plugin), the offending player will have an infraction removed.</li>
								<li>Maximum value is 1800 seconds(i.e. 30 minutes)</li>
							</ul>
							<h4>Take Action Auto/By Player/Both</h4>
							<ul>
								<li>Choose to Auto Kill, Kill By Player Punish, or utlize Both</li>
								<li><b>When using Both and ""Max # of Infractions before Auto Slay Action is taken"" is smaller than ""Max # of Infractions before Player Punish"" then Player Punish will never fire.</b></li>
							</ul>
							<h4>Max # of Infractions before Auto Slay Action is taken</h4>
							<ul>
								<li>Set how many times a player can commit spawn kills(according to the plugin), before action is taken against the offending player.</li>
								<li>Maximum allowed value is 20.</li>
							</ul>
							<h4>Max # of Infractions before Player Punish</h4>
							<ul>
								<li>Set how many times a player can commit spawn kills(according to the plugin), before the last victim of the spawn killer can punish for it.</li>
								<li>Maximum allowed value is 20.</li>
							</ul>
							<h4>Time a player has to punish his killer(seconds)</h4>
							<ul>
								<li><b>Only available if ""Take Action Auto/By Player"" = ""By Player""</b></li>
								<li>Number of seconds the last Spawn Killed player has to punish or the punishment choice will pass onto the next player Spawn Killed by the Killer.</li>
								<li>If the Victim player does not Punish then the next Infraction will be larger than the needed Infractions, I left it like this so that other players can see that it is getting out of hand, and hopefully are more likely to punish if they get Spawn Killed.</li>
							</ul>
							<h4>In Game Command to Punish</h4>
							<ul>
								<li><b>Only available if ""Take Action Auto/By Player"" = ""By Player""</b></li>
								<li>Ability to change the In Game Command so that it does not conflict with other plugins.</li>
							</ul>
						<h2></h2>
						<h2>In Game Messages AntiSpawnCamp</h2>
							<h4>Admin Messages show Header</h4>
							<ul>
								<li>Turn ""Admin Message Header"" ""On/Off""</li>
								<li>Determine if the plugin should add a Header to each In Game message.</li>
							</ul>
							<h4>Admin Message Header</h4>
							<ul>
								<li>Provides the ability to customize a Header for Admin Messages.</li>
							</ul>
							<h4>Admin Messages show Slay Player</h4>
							<ul>
								<li>Turn ""Admin Message Slay Player"" ""On/Off""</li>
							</ul>
							<h4>Admin Message Slay Player</h4>
							<ul>
								<li>Message that is displayed In Game to everyone on the server when a Spawn Killer is alive killed by BF3AntiSpawnCamp.</li>
								<br><b>String Replacement Variables</b>
								<ul>	
									<li><b>{0} = Killer</b></li>
									<li><b>{1} = Victim</b></li>
									<li><b>{2} = Killer's Current Infractions</b></li>
									<li><b>{3} = ""Max # of Infractions before Action is taken""</b></li>
								</ul>
							</ul>
							<h4>Admin Messages show Committed Spawn Kill</h4>
							<ul>
								<li>Turn ""Admin Message Committed Spawn Kill"" ""On/Off""</li>
							</ul>
							<h4>Admin Message Committed Spawn Kill</h4>
							<ul>
								<li>Message displayed everytime a Spawn Kill occurs.</li>
								<br><b>String Replacement Variables</b>
								<ul>
									<li><b>{0} = Killer</b></li>
									<li><b>{1} = Victim</b></li>
									<li><b>{2} = Killer's Current Infractions</b></li>
									<li><b>{3} = ""Max # of Infractions before Action is taken""</b></li>
								</ul>
							</ul>
							<h4>Admin Message Victim Punish</h4>
							<ul>
								<li><b>Only available if ""Take Action Auto/By Player"" = ""By Player""</b></li>
								<li>Message displayed to a player/squad that was the last victim of a Spawn Killer reaching the ""Max # of Infractions before Action is taken""</li>
								<li>Informs the Victim how to take action against the Spawn Killer.</li>
								<br><b>String Replacement Variables</b>
								<ul>
									<li><b>{0} = Killer</b></li>
									<li><b>{1} = Victim</b></li>
									<li><b>{2} = ""Time a player has to punish his killer(seconds)""</b></li>
									<li><b>{3} = ""In Game Command to Punish""</b></li>
								</ul>
							</ul>
						<h2></h2>
						<h2>Versions</h2>
							<h4>BF3AntiSpawnCamp 0.6 Beta</h4>
							<ul>
								<li>Fixed: Punish Message was being broacast to all players, not the squad of the Victim/Punisher.</li>
							</ul>
							<h4>BF3AntiSpawnCamp 0.5 Beta</h4>
							<ul>
								<li>Modified: ""Take Action Auto/By Player"" - changed to ""Take Action Auto/By Player/Both"" allows the server to enforce both methods of killing a spawn killer.</li>								
								<li>Added: ""Max # of Infractions before Player Punish"" - Infractions count at which players recieve notification to be able to puish a spawn killer</li>
								<li>Removed: ""Admin Messages show Slay Player Dead"" and ""Admin Messages Slay Player Dead"" - With the information of to much chat box clutter and being a somewhat redundant feature I removed it.</li>
								<li>Fixed: ""Admin Messages show Slay Killer Alive"" and ""Admin Messages Slay Killer Alive"" - It was worded improperly and I removed the Slay Player Dead messages. So it is now ""Admin Messages show Slay Player"" and ""Admin Messages Slay Player"".</li>
								<li>Modified: Moved each In Game Message to it own setting section to make it easier modify them.</li>
								<li>Fixed: Example messages were firing upon plugin loaded, change them to only show if they are modified.</li>
							</ul>
							<h4>BF3AntiSpawnCamp 0.4 Beta</h4>
							<ul>
								<li>Removed: ""Take Action when Max Infractions reached"" - removing the extra chat feature to prevent excessive chat spam.</li>
								<li>Removed: ""Admin Messages Take No Action"" - removing the extra chat feature to prevent excessive chat spam.</li>
								<li>Modified: Punish methods.</li>
								<li>Reworked: Admin Messages</li>
							</ul>
							<h4>BF3AntiSpawnCamp 0.3 Beta</h4>
							<ul>
								<li>Fixed: Retardedly I was calculating every kill not just the ones that counted as a Spawn Kill. Ahh the dumb things you overlook testing by yourself.</li>
								<li>Added: Ability to allow players to choose to punish or have PROCon Auto Punish.</li>
								<li>Added: Time limit a player has to punish their Killer.</li>
								<li>Added: In Game command for the Player Punish feature.</li>
							</ul>
							<h4>BF3AntiSpawnCamp 0.2 Beta</h4>
							<ul>
								<li>Fixed: had an issue with not setting TeamID's correctly unless it was a connecting player. Affected players that were on the server when the plugin was loaded.</li>
							</ul>
							<h4>BF3AntiSpawnCamp 0.1 Beta</h4>
							<ul>
								<li>Initial release of the plugin to see waht others may help me to find for issues, or additions.</li>
							</ul>
						<h2></h2>
					</blockquote>
					<br>";
        }
        #endregion

        #region PluginSetup	
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents
            (
                this.GetType().Name,
                //"OnResponseError",
                "OnListPlayers",
                "OnPlayerJoin",
                "OnPlayerLeft",
                "OnPlayerSpawned",
                "OnPlayerKilled",
                "OnLevelLoaded",
                "OnServerInfo"
            );
        }

        public void OnResponseError(List<string> requestWords, string error)
        {
            for (int i = 0; i < requestWords.Count; i++)
            {
                Write2PluginConsole(String.Format("{0} - {1}", requestWords[i], error), "OnResponseError", "Debug", 100);
            }
        }

        public void OnPluginEnable()
        {
            //Tasks to run when plugin Starts
            this.bf3asc_blPluginEnabled = true;

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^3[" + this.GetPluginName() + "] ^1Enabled");
            this.RegisterAllCommands();

            this.bf3asc_iRoundTime = 0;

            Thread bf3asc_thrMainLoop = new Thread(new ThreadStart(delegate ()
            {
                Write2PluginConsole(String.Format("Starting MainLoop"), "OnPluginEnable", "Debug", 100);
                while (this.bf3asc_blPluginEnabled)
                {
                    Thread.Sleep(1000);
                    this.BF3ASCMainLoop();
                }
                Write2PluginConsole(String.Format("Stopping MainLoop"), "OnPluginEnable", "Debug", 100);
            }));
            bf3asc_thrMainLoop.Start();
        }

        public void OnPluginDisable()
        {
            //Tasks to run when plugin stops
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^3[" + this.GetPluginName() + "] ^1Disabled");
            this.UnregisterAllCommands();

            this.bf3asc_blPluginEnabled = false;
        }
        #endregion

        #region SetPluginVariables
        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iHelperInt = 999;

            if (strVariable.CompareTo("# of seconds between Spawn and Death") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 10 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3asc_iTimeSpawn2Death = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 10)
                {
                    Write2PluginConsole(String.Format("Anti Spawn Camp can not be less than 0 or more than 10"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("Max # of Infractions before Auto Slay Action is taken") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 20 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3asc_iMaxInfractions = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 20)
                {
                    Write2PluginConsole(String.Format("Max Infractions can not be less than 0 or more than 20"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("Max # of Infractions before Player Punish") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 20 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3asc_iMaxPunishInfractions = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 20)
                {
                    Write2PluginConsole(String.Format("Max Infractions can not be less than 0 or more than 20"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("Infractions cool down time in seconds") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 1800 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3asc_iInfractionsCoolDown = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 1800)
                {
                    Write2PluginConsole(String.Format("Infractions cool down can not be less than 0 or more than 1800(30 Minutes)"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("Take Action Auto/By Player/Both") == 0)
            {
                this.bf3asc_strActionType = strValue;
            }
            else if (strVariable.CompareTo("Time a player has to punish his killer(seconds)") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= this.bf3asc_iInfractionsCoolDown && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3asc_iPunishTime = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > this.bf3asc_iInfractionsCoolDown)
                {
                    Write2PluginConsole(String.Format("Punish Time can not be less than 0 or more than 'Infractions cool down time in seconds' = {0}", this.bf3asc_iInfractionsCoolDown), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("In Game Command to Punish") == 0)
            {
                this.bf3asc_strPunishCommand = strValue;
            }
            else if (strVariable.CompareTo("Debug Depth") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 2 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3asc_iDebugLevel = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 2)
                {
                    Write2PluginConsole(String.Format("Debug Levels are 0 - 2"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("Admin Messages show Header") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.bf3asc_enAdminDebugHeader = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Admin Message Header") == 0)
            {
                this.bf3asc_strAdminHeader = strValue;
            }
            else if (strVariable.CompareTo("Admin Messages show Slay Player") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.bf3asc_enAdminSaySlayAlive = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Admin Message Slay Player") == 0)
            {
                this.bf3asc_strAdminSaySlayAlive = strValue;
                if (this.bf3asc_enAdminSaySlayAlive == enumBoolOnOff.On && this.bf3asc_blPluginEnabled && this.iFirstRun)
                {
                    Write2PluginConsole(String.Format("" + strValue + "", "'KillerName'", "'VictimName'", "#SpawnKills#", "#MaxSpawnKills#"), "Example Slay Player Alive", "Debug", 0);
                }
            }/* else if (strVariable.CompareTo("Admin Messages show Slay Player Dead") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true) {
				this.bf3asc_enAdminSaySlayDead = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
			} else if (strVariable.CompareTo("Admin Message Slay Player Dead") == 0) {
				this.bf3asc_strAdminSaySlayDead = strValue;	
				if (this.bf3asc_enAdminSaySlayDead == enumBoolOnOff.On && this.bf3asc_blPluginEnabled && this.iFirstRun) {
					Write2PluginConsole(String.Format(""+strValue+"", "'KillerName'", "'VictimName'", "#SpawnKills#", "#MaxSpawnKills#"), "Example Slay Player Dead", "Debug", 0);
				}
			}*/
            else if (strVariable.CompareTo("Admin Messages show Committed Spawn Kill") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.bf3asc_enAdminSayComitted = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Admin Message Committed Spawn Kill") == 0)
            {
                this.bf3asc_strAdminSayComitted = strValue;
                if (this.bf3asc_enAdminSayComitted == enumBoolOnOff.On && this.bf3asc_blPluginEnabled && this.iFirstRun)
                {
                    Write2PluginConsole(String.Format("" + strValue + "", "'KillerName'", "'VictimName'", "#SpawnKills#", "#MaxSpawnKills#"), "Example Committed Spawn Kill", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("Admin Messages show Victim Punish") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.bf3asc_enAdminSayPunish = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Admin Message Victim Punish") == 0)
            {
                this.bf3asc_strAdminSayPunish = strValue;
                if (this.bf3asc_enAdminSayPunish == enumBoolOnOff.On && this.bf3asc_blPluginEnabled && this.iFirstRun)
                {
                    Write2PluginConsole(String.Format("" + strValue + "", "'KillerName'", "'VictimName'", "#VicPunishTime#", "'PunishCommand'"), "Example Victim Punish", "Debug", 0);
                }
            }
        }
        #endregion

        #region DisplayPluginVars
        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("AntiSpawnCamp Debug |Debug Depth", typeof(int), this.bf3asc_iDebugLevel));
            lstReturn.Add(new CPluginVariable("AntiSpawnCamp Settings|# of seconds between Spawn and Death", typeof(int), this.bf3asc_iTimeSpawn2Death));
            lstReturn.Add(new CPluginVariable("AntiSpawnCamp Settings|Take Action Auto/By Player/Both", "enum.BF3ASCPunishType(Auto|By Player|Both)", this.bf3asc_strActionType));
            lstReturn.Add(new CPluginVariable("AntiSpawnCamp Settings|Infractions cool down time in seconds", typeof(int), this.bf3asc_iInfractionsCoolDown));
            if (this.bf3asc_strActionType == "Auto" || this.bf3asc_strActionType == "Both")
            {
                lstReturn.Add(new CPluginVariable("AntiSpawnCamp Settings|Max # of Infractions before Auto Slay Action is taken", typeof(int), this.bf3asc_iMaxInfractions));
            }
            if (this.bf3asc_strActionType == "By Player" || this.bf3asc_strActionType == "Both")
            {
                lstReturn.Add(new CPluginVariable("AntiSpawnCamp Settings|Max # of Infractions before Player Punish", typeof(int), this.bf3asc_iMaxPunishInfractions));
                lstReturn.Add(new CPluginVariable("AntiSpawnCamp Settings|Time a player has to punish his killer(seconds)", typeof(int), this.bf3asc_iPunishTime));
                lstReturn.Add(new CPluginVariable("AntiSpawnCamp Settings|In Game Command to Punish", typeof(string), this.bf3asc_strPunishCommand));
            }
            lstReturn.Add(new CPluginVariable("In Game Messages &Header|Admin Messages show Header", typeof(enumBoolOnOff), this.bf3asc_enAdminDebugHeader));
            if (this.bf3asc_enAdminDebugHeader == enumBoolOnOff.On)
            {
                lstReturn.Add(new CPluginVariable("In Game Messages &Header|Admin Message Header", typeof(string), this.bf3asc_strAdminHeader));
            }
            lstReturn.Add(new CPluginVariable("In Game Messages &Slay Player|Admin Messages show Slay Player", typeof(enumBoolOnOff), this.bf3asc_enAdminSaySlayAlive));
            if (this.bf3asc_enAdminSaySlayAlive == enumBoolOnOff.On)
            {
                lstReturn.Add(new CPluginVariable("In Game Messages &Slay Player|Admin Message Slay Player", typeof(string), this.bf3asc_strAdminSaySlayAlive));
            }
            /*lstReturn.Add(new CPluginVariable("In Game Messages &Slay Player Dead|Admin Messages show Slay Player Dead", typeof(enumBoolOnOff), this.bf3asc_enAdminSaySlayDead));
			if (this.bf3asc_enAdminSaySlayDead == enumBoolOnOff.On) {
				lstReturn.Add(new CPluginVariable("In Game Messages &Slay Player Dead|Admin Message Slay Player Dead", typeof(string), this.bf3asc_strAdminSaySlayDead));
			}*/
            lstReturn.Add(new CPluginVariable("In Game Messages Comitted Spawn Kill|Admin Messages show Committed Spawn Kill", typeof(enumBoolOnOff), this.bf3asc_enAdminSayComitted));
            if (this.bf3asc_enAdminSayComitted == enumBoolOnOff.On)
            {
                lstReturn.Add(new CPluginVariable("In Game Messages Comitted Spawn Kill|Admin Message Committed Spawn Kill", typeof(string), this.bf3asc_strAdminSayComitted));
            }
            //lstReturn.Add(new CPluginVariable("In Game Messages Victim Punish|Admin Messages show Victim Punish", typeof(enumBoolOnOff), this.bf3asc_enAdminSayPunish));
            if (this.bf3asc_strActionType == "By Player" || this.bf3asc_strActionType == "Both"/*this.bf3asc_enAdminSayPunish == enumBoolOnOff.On*/)
            {
                lstReturn.Add(new CPluginVariable("In Game Messages Victim Punish|Admin Message Victim Punish", typeof(string), this.bf3asc_strAdminSayPunish));
            }

            return lstReturn;
        }

        // Lists all of the plugins saved variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Debug Depth", typeof(int), this.bf3asc_iDebugLevel));
            lstReturn.Add(new CPluginVariable("# of seconds between Spawn and Death", typeof(int), this.bf3asc_iTimeSpawn2Death));
            lstReturn.Add(new CPluginVariable("Take Action Auto/By Player/Both", "enum.BF3ASCPunishType(Auto|By Player|Both)", this.bf3asc_strActionType));
            lstReturn.Add(new CPluginVariable("Max # of Infractions before Auto Slay Action is taken", typeof(int), this.bf3asc_iMaxInfractions));
            lstReturn.Add(new CPluginVariable("Max # of Infractions before Player Punish", typeof(int), this.bf3asc_iMaxPunishInfractions));
            lstReturn.Add(new CPluginVariable("Infractions cool down time in seconds", typeof(int), this.bf3asc_iInfractionsCoolDown));
            lstReturn.Add(new CPluginVariable("Time a player has to punish his killer(seconds)", typeof(int), this.bf3asc_iPunishTime));
            lstReturn.Add(new CPluginVariable("In Game Command to Punish", typeof(string), this.bf3asc_strPunishCommand));
            lstReturn.Add(new CPluginVariable("Admin Messages show Header", typeof(enumBoolOnOff), this.bf3asc_enAdminDebugHeader));
            lstReturn.Add(new CPluginVariable("Admin Message Header", typeof(string), this.bf3asc_strAdminHeader));
            lstReturn.Add(new CPluginVariable("Admin Messages show Slay Player", typeof(enumBoolOnOff), this.bf3asc_enAdminSaySlayAlive));
            lstReturn.Add(new CPluginVariable("Admin Message Slay Player", typeof(string), this.bf3asc_strAdminSaySlayAlive));
            //lstReturn.Add(new CPluginVariable("Admin Messages show Slay Player Dead", typeof(enumBoolOnOff), this.bf3asc_enAdminSaySlayDead));
            //lstReturn.Add(new CPluginVariable("Admin Message Slay Player Dead", typeof(string), this.bf3asc_strAdminSaySlayDead));
            lstReturn.Add(new CPluginVariable("Admin Messages show Committed Spawn Kill", typeof(enumBoolOnOff), this.bf3asc_enAdminSayComitted));
            lstReturn.Add(new CPluginVariable("Admin Message Committed Spawn Kill", typeof(string), this.bf3asc_strAdminSayComitted));
            lstReturn.Add(new CPluginVariable("Admin Messages show Victim Punish", typeof(enumBoolOnOff), this.bf3asc_enAdminSayPunish));
            lstReturn.Add(new CPluginVariable("Admin Message Victim Punish", typeof(string), this.bf3asc_strAdminSayPunish));

            return lstReturn;
        }
        #endregion

        #region PROConPluginAPI
        public void UnregisterAllCommands()
        {
            List<string> emptyList = new List<string>();

            this.UnregisterCommand(new MatchCommand(emptyList, this.bf3asc_strPunishCommand, this.Listify<MatchArgumentFormat>()));
        }

        public void RegisterAllCommands()
        {
            this.RegisterCommand(new MatchCommand("" + this.GetPluginName() + "", "OnCommandPunishASC", this.Listify<string>("@", "!", "#", "/"), this.bf3asc_strPunishCommand, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Punish a player that has committed repeated Spawn Killing"));
        }

        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {
            foreach (CPlayerInfo allPlayers in players)
            {
                bool blFoundPlayer = false;
                foreach (PlayerInfos thisPlayer in this.bf3asc_lstPlayerInfos)
                {
                    if (allPlayers.SoldierName == thisPlayer.PlayerName)
                    {
                        blFoundPlayer = true;
                        if (thisPlayer.TeamID == 0)
                        {
                            thisPlayer.TeamID = allPlayers.TeamID;
                            thisPlayer.SquadID = allPlayers.SquadID;
                            Write2PluginConsole(String.Format("Setting {0} TeamID {1} SquadID {2}", thisPlayer.PlayerName, allPlayers.TeamID, allPlayers.SquadID), "OnListPlayers", "Debug", 2);
                        }
                        break;
                    }
                }
                if (!blFoundPlayer)
                {
                    this.bf3asc_lstPlayerInfos.Add(new PlayerInfos(allPlayers.SoldierName, this.bf3asc_iRoundTime, 0, this.bf3asc_iRoundTime));
                    Write2PluginConsole(String.Format("Adding {0} to the Players List", allPlayers.SoldierName), "OnListPlayers", "Debug", 1);
                    PlayerInfos thisPlayer = this.bf3asc_lstPlayerInfos[(this.bf3asc_lstPlayerInfos.Count - 1)];
                    if (thisPlayer.TeamID == 0)
                    {
                        thisPlayer.TeamID = allPlayers.TeamID;
                        thisPlayer.SquadID = allPlayers.SquadID;
                        Write2PluginConsole(String.Format("Setting {0} TeamID {1} SquadID {2}", allPlayers.SoldierName, allPlayers.TeamID, allPlayers.SquadID), "OnListPlayers", "Debug", 2);
                    }
                }
            }
            this.iFirstRun = true;
        }

        public override void OnPlayerJoin(string soldierName)
        {
            Write2PluginConsole(String.Format("{0} Joined the Server", soldierName), "OnPlayerJoin", "Debug", 100);
            Write2PluginConsole(String.Format("Adding {0} to the Players List", soldierName), "OnPlayerJoin", "Debug", 1);
            this.bf3asc_lstPlayerInfos.Add(new PlayerInfos(soldierName, this.bf3asc_iRoundTime, 0, this.bf3asc_iRoundTime));
        }

        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            Write2PluginConsole(String.Format("{0}{7} Left the Server from Team {9} Squad {8}", playerInfo.ClanTag, playerInfo.Deaths, playerInfo.GUID, playerInfo.Kdr, playerInfo.Kills, playerInfo.Ping, playerInfo.Score, playerInfo.SoldierName, playerInfo.SquadID, playerInfo.TeamID), "OnPlayerLeft", "Debug", 100);
            int iPlayerInfosIndex = 0;
            foreach (PlayerInfos thisPlayer in this.bf3asc_lstPlayerInfos)
            {
                if (thisPlayer.PlayerName == playerInfo.SoldierName)
                {
                    this.bf3asc_lstPlayerInfos.RemoveAt(iPlayerInfosIndex);
                    Write2PluginConsole(String.Format("Removed {0} from the Players List", playerInfo.SoldierName), "OnPlayerLeft", "Debug", 1);
                    break;
                }
                else
                {
                    iPlayerInfosIndex = iPlayerInfosIndex + 1;
                }
            }
        }

        public override void OnPlayerSquadChange(string soldierName, int teamId, int squadId)
        {
            foreach (PlayerInfos thisPlayer in this.bf3asc_lstPlayerInfos)
            {
                if (thisPlayer.PlayerName == soldierName)
                {
                    thisPlayer.TeamID = teamId;
                    thisPlayer.SquadID = squadId;
                    Write2PluginConsole(String.Format("Setting {0} TeamID {1} SquadID {2}", thisPlayer.PlayerName, thisPlayer.TeamID, thisPlayer.SquadID), "OnPlayerSquadChange", "Debug", 2);
                    break;
                }
            }
        }

        public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId)
        {
            foreach (PlayerInfos thisPlayer in this.bf3asc_lstPlayerInfos)
            {
                if (thisPlayer.PlayerName == soldierName)
                {
                    thisPlayer.TeamID = teamId;
                    thisPlayer.SquadID = squadId;
                    Write2PluginConsole(String.Format("Setting {0} TeamID {1} SquadID {2}", thisPlayer.PlayerName, thisPlayer.TeamID, thisPlayer.SquadID), "OnPlayerTeamChange", "Debug", 2);
                    break;
                }
            }
        }

        public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            Write2PluginConsole(String.Format("{0} Spawned @RoundTime {1}", soldierName, this.bf3asc_iRoundTime), "OnPlayerSpawned", "Debug", 100);
            foreach (PlayerInfos thisPlayer in this.bf3asc_lstPlayerInfos)
            {
                if (thisPlayer.PlayerName == soldierName)
                {
                    thisPlayer.SpawnTime = this.bf3asc_iRoundTime;
                    thisPlayer.AliveDead = 1;
                    Write2PluginConsole(String.Format("{0}'s Spawn Time set @RoundTime {1}", soldierName, this.bf3asc_iRoundTime), "OnPlayerSpawned", "Debug", 2);
                    break;
                }
            }
        }

        public override void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            Write2PluginConsole(String.Format("{0} Killed {1} HeadShot={2}", kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails.Victim.SoldierName, kKillerVictimDetails.Headshot), "OnPlayerKilled", "Debug", 1);
            int iKillersTeamID = 0;
            int iKillersIndex = 0;
            int iVictimsTeamID = 0;
            int iVictimsIndex = 0;
            int iTimeOfDeath = this.bf3asc_iRoundTime;
            int iTimeOfSpawn = 0;
            int iKillersInfractions = 0;
            foreach (PlayerInfos thisPlayer in this.bf3asc_lstPlayerInfos)
            {
                if (thisPlayer.PlayerName == kKillerVictimDetails.Killer.SoldierName)
                {
                    iKillersTeamID = thisPlayer.TeamID;
                    iKillersInfractions = thisPlayer.PlayerInfractions;
                    break;
                }
                else
                {
                    iKillersIndex = iKillersIndex + 1;
                }
            }
            foreach (PlayerInfos thisPlayer in this.bf3asc_lstPlayerInfos)
            {
                if (thisPlayer.PlayerName == kKillerVictimDetails.Victim.SoldierName)
                {
                    iVictimsTeamID = thisPlayer.TeamID;
                    iTimeOfSpawn = thisPlayer.SpawnTime;
                    thisPlayer.AliveDead = 0;
                    Write2PluginConsole(String.Format("{0} Spawn Time is {1}.", kKillerVictimDetails.Victim.SoldierName, iTimeOfSpawn), "OnPlayerKilled", "Debug", 2);
                    break;
                }
                else
                {
                    iVictimsIndex = iVictimsIndex + 1;
                }
            }
            if (iKillersTeamID != iVictimsTeamID || SrciptDebugVar == 1)
            {
                Write2PluginConsole(String.Format("Spawn time 2 Death time difference = {0} this.bf3asc_iRoundTime = {1} iTimeOfDeath = {2} iTimeOfSpawn = {3}", (iTimeOfDeath - iTimeOfSpawn), this.bf3asc_iRoundTime, iTimeOfDeath, iTimeOfSpawn), "OnPlayerKilled", "Debug", 2);
                PlayerInfos playerKiller = this.bf3asc_lstPlayerInfos[iKillersIndex];
                if ((iTimeOfDeath - iTimeOfSpawn) <= this.bf3asc_iTimeSpawn2Death)
                {
                    playerKiller.PlayerInfractions = playerKiller.PlayerInfractions + 1;
                    playerKiller.LastInfractionTime = this.bf3asc_iRoundTime;
                    Write2PluginConsole(String.Format("{0} received a Spawn Kill Infraction.", kKillerVictimDetails.Killer.SoldierName), "OnPlayerKilled", "Debug", 1);
                    if (this.bf3asc_enAdminSayComitted == enumBoolOnOff.On)
                    {
                        WriteAdminHeader(String.Format("{0}", this.bf3asc_strAdminHeader), "all", "");
                    }
                    WriteAdminSay(String.Format("" + this.bf3asc_strAdminSayComitted + "", kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails.Victim.SoldierName, playerKiller.PlayerInfractions, this.bf3asc_iMaxInfractions), "Comitted", "", playerKiller.TeamID, playerKiller.SquadID);
                    if ((this.bf3asc_strActionType == "Auto" || this.bf3asc_strActionType == "Both") && playerKiller.PlayerInfractions >= this.bf3asc_iMaxInfractions)
                    {
                        Write2PluginConsole(String.Format("{0} has reached Max Spawn Kill Infractions.", kKillerVictimDetails.Killer.SoldierName), "OnPlayerKilled", "Debug", 1);
                        Write2PluginConsole(String.Format("Killing {0} for Spawn Kills", kKillerVictimDetails.Killer.SoldierName), "OnPlayerKilled", "Debug", 1);
                        Thread bf3asc_thrPunishWait = new Thread(new ThreadStart(delegate ()
                        {
                            Thread.Sleep(1000);
                            if (playerKiller.AliveDead == 1)
                            {
                                Write2PluginConsole(String.Format("Informing {0} why they were killed", kKillerVictimDetails.Killer.SoldierName), "OnPlayerKilled", "Debug", 1);
                                if (this.bf3asc_enAdminSaySlayAlive == enumBoolOnOff.On)
                                {
                                    WriteAdminHeader(String.Format("{0}", this.bf3asc_strAdminHeader), "all", "");
                                }
                                WriteAdminSay(String.Format("" + this.bf3asc_strAdminSaySlayAlive + "", kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails.Victim.SoldierName, playerKiller.PlayerInfractions, this.bf3asc_iMaxInfractions), "SlayAlive", "", playerKiller.TeamID, playerKiller.SquadID);
                                this.ExecuteCommand("procon.protected.send", "admin.killPlayer", kKillerVictimDetails.Killer.SoldierName);
                                //playerKiller.PlayerInfractions = 0;
                            }
                            else
                            {
                                //if (this.bf3asc_enAdminSaySlayDead == enumBoolOnOff.On) {
                                //	WriteAdminHeader(String.Format("{0}", this.bf3asc_strAdminHeader), "all", "");
                                //}
                                //WriteAdminSay(String.Format(""+this.bf3asc_strAdminSaySlayDead+"", kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails.Victim.SoldierName, playerKiller.PlayerInfractions, this.bf3asc_iMaxInfractions), "SlayDead", "");
                                //playerKiller.PlayerInfractions = 0;
                            }
                            playerKiller.PlayerInfractions = 0;
                        }));
                        bf3asc_thrPunishWait.Start();
                    }
                    else if ((this.bf3asc_strActionType == "By Player" || this.bf3asc_strActionType == "Both") && playerKiller.PlayerInfractions >= this.bf3asc_iMaxPunishInfractions)
                    {
                        PlayerInfos playerVictim = this.bf3asc_lstPlayerInfos[iVictimsIndex];
                        playerVictim.LastKillerName = kKillerVictimDetails.Killer.SoldierName;
                        Thread bf3asc_thrPunishWait = new Thread(new ThreadStart(delegate ()
                        {
                            Thread.Sleep(1000);
                            Write2PluginConsole(String.Format("Giving Player Option to punish"), "OnPlayerKilled", "Debug", 1);
                            if (this.bf3asc_enAdminSayPunish == enumBoolOnOff.On)
                            {
                                WriteAdminHeader(String.Format("{0}", this.bf3asc_strAdminHeader), "player", kKillerVictimDetails.Victim.SoldierName);
                            }
                            WriteAdminSay(String.Format("" + this.bf3asc_strAdminSayPunish + "", kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails.Victim.SoldierName, this.bf3asc_iPunishTime, this.bf3asc_strPunishCommand), "Punish", kKillerVictimDetails.Victim.SoldierName, playerVictim.TeamID, playerVictim.SquadID);
                            Thread.Sleep(this.bf3asc_iPunishTime * 1000);
                            playerVictim.LastKillerName = "";
                        }));
                        bf3asc_thrPunishWait.Start();
                    }
                }
            }
        }

        public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)
        {
            foreach (PlayerInfos allPlayers in this.bf3asc_lstPlayerInfos)
            {
                allPlayers.PlayerInfractions = 0;
                allPlayers.LastInfractionTime = 999999999;
                allPlayers.LastKillerName = "";
            }
        }

        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            //Write2PluginConsole(String.Format("this.bf3asc_iRoundTime = {0} csiServerInfo.RoundTime = {1}", this.bf3asc_iRoundTime, csiServerInfo.RoundTime), "OnServerInfo", "Debug", 100);
            this.bf3asc_iRoundTime = csiServerInfo.RoundTime;
        }
        #endregion

        #region CustomClasses
        public class PlayerInfos
        {
            string strPlayerName;
            int iTimeSpawn;
            int iInfractions;
            int iLastInfraction;
            int iTeamID;
            int iSquadID;
            int iAliveDead;
            string strLastKilledBy;

            public PlayerInfos(string strSoldierName, int iTimeNow, int iCurrentInfractions, int iInfractionTime)
            {
                strPlayerName = strSoldierName;
                iTimeSpawn = iTimeNow;
                iInfractions = iCurrentInfractions;
                iLastInfraction = iInfractionTime;
                iTeamID = 0;
                iSquadID = 0;
                iAliveDead = 0;
                strLastKilledBy = "";
            }
            public string PlayerName
            {
                set { strPlayerName = value; }
                get { return strPlayerName; }
            }
            public int SpawnTime
            {
                set { iTimeSpawn = value; }
                get { return iTimeSpawn; }
            }
            public int PlayerInfractions
            {
                set { iInfractions = value; }
                get { return iInfractions; }
            }
            public int LastInfractionTime
            {
                set { iLastInfraction = value; }
                get { return iLastInfraction; }
            }
            public int TeamID
            {
                set { iTeamID = value; }
                get { return iTeamID; }
            }
            public int SquadID
            {
                set { iSquadID = value; }
                get { return iSquadID; }
            }
            public int AliveDead
            {
                set { iAliveDead = value; }
                get { return iAliveDead; }
            }
            public string LastKillerName
            {
                set { strLastKilledBy = value; }
                get { return strLastKilledBy; }
            }
        }
        #endregion

        #region In Game Commands
        public void OnCommandPunishASC(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            string strKillerName = "";
            bool blKillingPlayer = false;
            foreach (PlayerInfos playerPunish in this.bf3asc_lstPlayerInfos)
            {
                if (playerPunish.PlayerName == strSpeaker && playerPunish.LastKillerName != "")
                {
                    strKillerName = playerPunish.LastKillerName;
                    playerPunish.LastKillerName = "";
                    blKillingPlayer = true;
                }
            }
            if (blKillingPlayer)
            {
                foreach (PlayerInfos killPlayer in this.bf3asc_lstPlayerInfos)
                {
                    if (killPlayer.PlayerName == strKillerName)
                    {
                        if (killPlayer.AliveDead == 1)
                        {
                            Thread bf3asc_thrPunishWait = new Thread(new ThreadStart(delegate ()
                            {
                                Thread.Sleep(1000);
                                Write2PluginConsole(String.Format("Informing {0} why they were killed", strKillerName), "OnCommandPunishASC", "Debug", 1);
                                if (this.bf3asc_enAdminSayPunish == enumBoolOnOff.On)
                                {
                                    WriteAdminHeader(String.Format("{0}", this.bf3asc_strAdminHeader), "all", "");
                                }
                                WriteAdminSay(String.Format("" + this.bf3asc_strAdminSaySlayAlive + "", strKillerName, strSpeaker, killPlayer.PlayerInfractions, this.bf3asc_iMaxInfractions), "SlayAlive", "", killPlayer.TeamID, killPlayer.SquadID);
                                Thread.Sleep(1000);
                                this.ExecuteCommand("procon.protected.send", "admin.killPlayer", strKillerName);
                                killPlayer.PlayerInfractions = 0;
                            }));
                            bf3asc_thrPunishWait.Start();
                        }
                        else
                        {
                            //if (this.bf3asc_enAdminSaySlayDead == enumBoolOnOff.On) {
                            //	WriteAdminHeader(String.Format("{0}", this.bf3asc_strAdminHeader), "all", "");
                            //}
                            //WriteAdminSay(String.Format(""+this.bf3asc_strAdminSaySlayDead+"", strKillerName, strSpeaker, killPlayer.PlayerInfractions, this.bf3asc_iMaxInfractions), "SlayDead", "");
                        }
                    }
                }
            }
        }
        #endregion

        #region Tools
        public void BF3ASCMainLoop()
        {
            this.bf3asc_iRoundTime = this.bf3asc_iRoundTime + 1;

            foreach (PlayerInfos thisPlayer in this.bf3asc_lstPlayerInfos)
            {
                if (thisPlayer.PlayerInfractions > 0)
                {
                    //Write2PluginConsole(String.Format("{0} Infractions {1}", thisPlayer.PlayerName, thisPlayer.PlayerInfractions), "BF3ASCMainLoop", "Debug", 100);
                }
                if (thisPlayer.PlayerInfractions > 0 && (thisPlayer.LastInfractionTime + this.bf3asc_iInfractionsCoolDown) < this.bf3asc_iRoundTime)
                {
                    thisPlayer.PlayerInfractions = thisPlayer.PlayerInfractions - 1;
                    Write2PluginConsole(String.Format("{0} Infractions {1} RemoveInfractionTime={2} RoundTime={3}", thisPlayer.PlayerName, thisPlayer.PlayerInfractions, (thisPlayer.LastInfractionTime + this.bf3asc_iInfractionsCoolDown), this.bf3asc_iRoundTime), "BF3ASCMainLoop", "Debug", 1);
                    thisPlayer.LastInfractionTime = this.bf3asc_iRoundTime;
                    break;
                }
            }
        }

        public void Write2PluginConsole(string text, string tag, string type, int level)
        {
            string message = "^b^3[" + this.GetPluginName() + "] ^1" + tag + ": ^0^n" + text;
            if ((this.bf3asc_iDebugLevel >= level && "Debug" == type) || SrciptDebugVar == 1)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", message);
            }
        }

        public void WriteAdminSay(string message, string tag, string player, int teamId, int squadId)
        {
            if ("SlayAlive" == tag && this.bf3asc_enAdminSaySlayAlive == enumBoolOnOff.On)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "all");
            }
            //if ("SlayDead" == tag && this.bf3asc_enAdminSaySlayDead == enumBoolOnOff.On) {
            //	this.ExecuteCommand("procon.protected.send", "admin.say", message, "all");
            //}
            if ("Comitted" == tag && this.bf3asc_enAdminSayComitted == enumBoolOnOff.On)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "all");
            }
            if ("Punish" == tag && this.bf3asc_enAdminSayPunish == enumBoolOnOff.On)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", message, "squad", teamId.ToString(), squadId.ToString());
            }
        }

        public void WriteAdminHeader(string message, string tag, string player)
        {
            if ("all" == tag && this.bf3asc_enAdminDebugHeader == enumBoolOnOff.On)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", message, tag);
            }
            if ("player" == tag && this.bf3asc_enAdminDebugHeader == enumBoolOnOff.On)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", message, tag, player);
            }
        }
        #endregion
    }
    #endregion
}