/*/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
//								
//		Copyright 2010 ScHrAnZ DiNgEnS
//      http://www.promillestube.de
//
//		This file is part of BFBC2 PRoCon.
//
//		BFBC2 PRoCon is free software: you can redistribute it and/or modify
//		it under the terms of the GNU General Public License as published by
//		the Free Software Foundation, either version 3 of the License, or
//		(at your option) any later version.
//
//  	BFBC2 PRoCon is distributed in the hope that it will be useful,
//   	but WITHOUT ANY WARRANTY; without even the implied warranty of
//   	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   	GNU General Public License for more details.
//
//   	You should have received a copy of the GNU General Public License
//   	along with BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
//
//
//		Credits:
//		Phogue - For programming BFBC2:ProCon
//		Phogue - For basecode of this plugin
//		Hamma & Xp3r7 & powerbits - Ideas of emptymap features
//      DICE - For fixing roundbug if i force a mapchange on round 1 of 2
//	
//
//		Changelog:
//		
//		2.2 
//		- Add. ability to change to a spesific map if server is empty
//	
//		2.3 
//		- Fix. bug with currentplayers
//
//		3.0
//		- Initial Relase
//		- Change Emptymap Staytime default to 4
//		- REM. cleanup code
//      	
//      3.1
//      - Add. say: @nextmap to display the next map or played rounds on current map
//      - Fix. bug with the first map in maplist postet by W.[ ].R.R.e.L. (http://phogue.net/forum/viewtopic.php?f=18&t=516#p3150)
//
//      3.1.1 First testversion for 3.2
//      - Add. Emptymap Maplist to fill up the server
//      - Add. vars for Emptymap Naplist: "Emptymap Maplist Maxplayers" and "Use Emptymap Maplist for specific Emptymap"
//      - Change. the next map will displayed to all players if a player type @nextmap, so we can make it public to other players
//	
//      3.2
//      - Add. Playerbased Gamemodes
//      - Add. Seperate maplist for emptyMaplist/playerbased Gamemodes
//      - Add. Var that specific how much players are needet to switch to normal cycle
//      - Fix. Rounds not longer change if a specific map is running
//      - Rew. Change code from nextmap command, now using the method from IPRoConPluginInterface3
//      - Add. print the nextmap a few seconds before level is ending
//      - Add. Find a way to set the next map near to end of a level
//      - Add. Plugin.console messages to display configurationerrors
//
//      3.2.1
//      -Fix. Remove Delaymultiplicator and replace with a task wich can be set with EmptymapStaytime (int/minutes)
//      -Rem. Some parts of code wich are not used.
//      -Change. regular maplist for emptymap if Emptymap Maplist is not filled up with maps
//      -Fix. After mapchange all rounds start on round 1 (thanks to DICE for fix this)
//
//      3.2.2
//      -Add. Remove "CMixedGamodes_v3_CheckForEmptymap" task if ServerInfo was called twice ore more on one time
//
//      3.3
//      -Change. Pluginconsolemessages are now only print if plugin is enabled
//      -Change. Nextmap is now print to chat on the end of a round because yell did not work
//
//      3.3.1
//      -Fix. Map changed twice because round change to the last round even u set playlist with a different gamemode
//
//      3.3.2
//      -Fix. Playerbased Gamemodes did not work correct if players drop
//      -Add. Option to set the next map on Roundend (Set Map on Roundend)
//
//      3.3.3
//      -Fix. Correct some codeparts from pluginversion 3.3.2
//  
//      3.4
//      -Addvar. 'Supported Gamemodes' to choose gamemodes if 'Playerbased Gamemodes' is enabled 
//      -Add.   Nextmap check on mapend to be shure the right map for playercount is played
//      -Rem.   Task to change gamemode if 'Playerbased Gamemodes' is enabled, now the check is on mapend
//      -Fix.   Map should change correct if a map is played with only 1 round 
//      -Add.   Support for Details 2.0, new clear and hopefully understandable discription	
//
//      3.5
//      -Fix. Remove bug in mapcheck on level end
//
//      3.6.0.0 
//      -Fix. Next map is not displayed in the chat on levelend
//      -Add. Var added to disable/enable pluginconsole output
// 
//      3.6.1.0 Testversion for 3.7 (stable)
//      -Fix. Remove Mapstucking
//
//      3.6.2.0 Testversion for 3.7 
//      -Change. Maplist/EmptyMaplist keep old positions after the cycle change
//
//      3.6.3.0 
//      -Fix. Next map is displayed wrong in the chat.
//      -Add. You can add a map twice or more in a cycle now, but not twice in succession! 
//
//
//      Support: http://phogue.net/forum/viewtopic.php?f=18&t=516
//		
//
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/


using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CMixedGamemodes_v3 : PRoConPluginAPI, IPRoConPluginInterface
    {

        const int SQDM_LIMIT = 14;
        const int SQRUSH_LIMIT = 6;

        private int m_iCurrentEmptymapIndex = 0;
        private int m_iCurrentIndex = 0;
        private int m_iDeathmatchIndex = 0;
        private int m_iRushIndex = 0;
        private bool m_bStaytaskAktive; //Check task
        private int m_iCurrentplayers;
        private int m_iNextmapType;

        private List<CMap> m_lstMapDefines;
        private List<CMap> m_lstEmptyMapDefines;

        //for internal use if playerbased Mods is enabled
        private List<CMap> m_lstSQDMMapDefines;
        private List<CMap> m_lstSQRUSHMapDefines;

        private string m_strPreviousMessage; //need to say nextmap
        //private string m_strSelectedAllMaps; //select Emptymap

        // Variables
        private List<CMapNameRounds> m_lstMaplist;
        private List<CMapNameRounds> m_lstEmptyMaplist;

        //for internal use if playerbased mods is enabled
        private List<CMapNameRounds> m_lstSQDMMaplist;
        private List<CMapNameRounds> m_lstSQRUSHMaplist;

        private int m_iEmptymapStaytime;	// Specific how long we should stay on a map if server is empty (int/minutes)
        private int m_iEmptyMapPosition; //need to set the emptymap
        private int m_iEmptyMapMaxplayers;

        private enumBoolYesNo m_enPlayerbasedModeEnabled;
        private enumBoolYesNo m_enEmptymapEnabled; // Enable/disable emptymap feature
        private enumBoolYesNo m_enPluginconsoleMessage;

        private string m_strSelectedSupportedGamemodes;

        private bool m_bPluginEnabled;

        private class CMapNameRounds
        {
            private string m_strMapFileName;
            private int m_iRoundsPlayed;

            public CMapNameRounds(string strMapFileName, int iRoundsPlayed)
            {
                this.m_strMapFileName = strMapFileName;
                this.m_iRoundsPlayed = iRoundsPlayed;
            }

            public string MapFileName
            {
                get
                {
                    return this.m_strMapFileName;
                }
            }

            public int Rounds
            {
                get
                {
                    return this.m_iRoundsPlayed;
                }
            }
        }

        public CMixedGamemodes_v3()
        {

            this.m_lstMaplist = new List<CMapNameRounds>();
            this.m_bPluginEnabled = false;
            this.m_strPreviousMessage = "";

            //needed for emptymap
            this.m_lstEmptyMaplist = new List<CMapNameRounds>();
            this.m_iCurrentplayers = 0;
            this.m_iEmptyMapMaxplayers = 12; //playerlimit for emptymap maplist
            this.m_bStaytaskAktive = false;  //check if task is set
            this.m_iEmptymapStaytime = 3; //minimum 1 (default 3)	
            this.m_iEmptyMapPosition = -1; //default is -1 to switch between all maps
            this.m_enPlayerbasedModeEnabled = enumBoolYesNo.No;
            this.m_enEmptymapEnabled = enumBoolYesNo.No; //default is NO to disable this feature
            this.m_enPluginconsoleMessage = enumBoolYesNo.No;
            this.m_iNextmapType = 0; // 0 = Next map is not set yet / 2 = is a map from emptymap maplist / 1 = is a map from normal maplist
            //need to display nextmap
            this.m_lstSQDMMaplist = new List<CMapNameRounds>();
            this.m_lstSQRUSHMaplist = new List<CMapNameRounds>();

            //VARS selected
            //this.m_strSelectedAllMaps = ""; //Select Emptymap
            this.m_strSelectedSupportedGamemodes = "SQRUSH & SQDM & RUSH/CONQUEST";


        }

        public string GetPluginName()
        {
            return "Mixed Gamemodes v3";
        }

        public string GetPluginVersion()
        {
            return "3.6.3 Testing";
        }

        public string GetPluginAuthor()
        {
            return "ScHrAnZ DiNgEnS";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {
            List<string> mapList = this.GetMapList("<tr><td><b>{FileName}</b></td><td>{GameMode}</td><td>{PublicLevelName}</td></tr>", "CONQUEST", "RUSH", "SQRUSH", "SQDM");

            return @"
<p>For support or to post comments regarding this plugin please visit <a href=""http://phogue.net/forum/viewtopic.php?f=18&t=516"" target=""_blank"">Plugin Thread</a></p>

<p>This plugin works with PRoCon and falls under the GNU GPL, please click <a href=""http://www.gnu.org/licenses/gpl.html""target=""_blank"">here</a> for details.
If you would like to donate to support the development of PRoCon, click the link below:<br />
<br />
<a href=""http://phogue.net"" target=""_new""><img src=""http://109.169.40.197/procon/developers/schranzdingens/phogue_donate.png"" width=""400"" height=""120"" border=""0"" alt=""PRoCon Donation"" /></a></p>
<p>Toward the right side of the page, there is a location to enter the amount you would like to donate and whether you want the donation to be made public. Your donations are greatly appreciated and will be sent to Phogue (original creator of PRoCon).</p>
<br />
<h2>Description</h2>
<p>Nullifies any maplist control by the client and cycles through a set map list, changing gamemodes if need be.
There are a lot of feature to control maps and gamemodes to easy fill up your server if the server is empty. 
The plugin display the next map on the end of a map and have a command to display the nextmap instantly</p>
<br />

<h2>Commands</h2>
    <blockquote><h4>[prefix]nextmap</h4>
        <ul>
	        <li>Display the next map instantly to chat</li>
            <li>Prefixes that can be used are !, @, #, and /</li>
        </ul>
    </blockquote>
<br />

<h2>Settings</h2>
    <h3>Emptymap Settings</h3>
        <blockquote><h4>Emptymap enabled</h4>
        Choose 'Yes' to enable Emptymap with all other emptymap features like 'Emptymap Maplist' and 'Playerbased Gamemodes' or No to disable.
        </blockquote>

        <blockquote><h4>Emptymap Staytime</h4>
        Choose the time in minutes how long the last map should stay before the server switch to a specific emptymap.
        </blockquote>

        <blockquote><h4>Position of Emptymap</h4>
        Specify the position of the map from Maplist wich should played if there are no players on the server to fill the server up.
        You can also set the value to '-1' to rotate between all maps in the Maplist.
        <h4>NOTE: Mapindex begins with 0 and if you have maps in 'Emptymap Maplist' the index of 'Emptymap Maplist' is used!
        If u enable 'PLayerbased Gamemodes' u have to choose a Squadrushmap or if Squadrush is disabled a Squaddeathmatch map!</h4>         
        </blockquote>


    <h3>Playerbased Gamemodes</h3>
    <ul>If u want to play all Gamemodes on your server this feature will make u happy!

	    <li> 0-8  Players -> Squadrush will be played (or Squaddeathmatch if u disable Squadrush)</li>
        <li> 9-16 Players -> Squaddeathmatch will be played</li>
        <li>17-32 Players -> Rush or Conquest will be played</li>
    </ul>

        <blockquote><h4>Use Playerbased Gamemodes</h4>
        choose Yes to enable 'Playerbased Gamemodes' or No to disable.
        </blockquote>

        <blockquote><h4>Supported Gamemodes</h4>
        You can disable Squadrush here if u choose 'SQDM & RUSH/CONQUEST.
        </blockquote>


    <h3>Emptymap Maplist</h3>
        <blockquote><h4>Emptymap Maplist</h4>
            This Maplist is used if there are not many players on the server, you can specify the amount of players with 'Emptymap Maplist Maxplayers'      
            Set up the cycle with only 1 map per line and add the most played maps to fill up your server.
            If you enabled 'Playerbased Gamemodes' you should add Squadrush and Squaddeathmatch maps to this cycle.
            you can specify the number of rounds after the map e.g:
            <ul>
	            <li>levels/mp_002 1</li>
	            <li>levels/mp_002 5</li>
	            <li>levels/mp_002 3</li>
            </ul>
            .. or leave blank to play the default rounds for that game type (usually 2)
            <ul>
	            <li>levels/mp_002</li>
            </ul>
        </blockquote>


        <blockquote><h4>Emptymap Maplist Maxplayers</h4>
        You can specify the amount of players to use Emptymap Maplist here.
        <h4>NOTE: This has no effect if u enable 'Playerbased Gamemodes' or disable Emptymap!</h4>
        </blockquote>


    <h3>Regular Maplist</h3>
        <blockquote><h4>Maplist</h4>
            This Maplist is used if Emptymap is disabled or if there are more players than 'Emptymap Maplist Maxplayers' on the server. If you want to use PLayerbased Gamemodes you have to fill up this Maplist with Rush/Conquest maps.      
            Set up the cycle with only 1 map per line, you can specify the number of rounds after the map e.g:
            <ul>
	            <li>levels/mp_002 1</li>
	            <li>levels/mp_002 5</li>
	            <li>levels/mp_002 3</li>
            </ul>
            .. or leave blank to play the default rounds for that game type (usually 2)
            <ul>
	            <li>levels/mp_002</li>
            </ul>
        </blockquote>
    <br />

<h2>Additional Information</h2>
    <ul>
        <li>CAUTION, enabling will immediately alter your maplist.  You cannot have the same map played twice in a row =(</li>
    </ul>
<br />

<h2>Available Maps</h2>
    <table style=""padding-left: 30px;"">
        " + String.Join("", mapList.ToArray()) + @"
    </table>
    <br />

<h2>Credits</h2>
    <h3>Credits goes to:</h3>
        
            <ul>
	            <li><h4>Phogue</h4>For developing ProCon</li>
	            <li><h4>Phogue</h4>For the basepart of this plugin</li>
	            <li><h4>Hamma & Xp3r7 & powerbits</h4>Ideas of emptymap features</li>
	            <li><h4>DICE</h4>For fixing roundbug if we force a mapchange on round 1 of 2</li>
            </ul>
        <br />
    <br />
";


        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            // Reload debugging.
            this.m_lstMaplist = new List<CMapNameRounds>();
            this.m_lstEmptyMaplist = new List<CMapNameRounds>();
            this.m_lstSQDMMaplist = new List<CMapNameRounds>();
            this.m_lstSQRUSHMaplist = new List<CMapNameRounds>();

            this.m_lstMapDefines = this.GetMapDefines();
            this.m_lstEmptyMapDefines = this.GetMapDefines();
            this.m_lstSQDMMapDefines = this.GetMapDefines();
            this.m_lstSQRUSHMapDefines = this.GetMapDefines();
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Multiple Gamemodes ^2Enabled!");
            this.m_bPluginEnabled = true;
            this.RegisterAllCommands();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Multiple Gamemodes ^1Disabled =(");
            this.m_bPluginEnabled = false;
            this.UnregisterAllCommands();
            if (this.m_bStaytaskAktive == true)
            {
                this.ExecuteCommand("procon.protected.tasks.remove", "CMixedGamodes_v3_CheckForEmptymap");
                this.m_bStaytaskAktive = false;
            }

            if (this.m_enPlayerbasedModeEnabled == enumBoolYesNo.Yes && this.m_blFinalRoundSet == true)
            {
                this.ExecuteCommand("procon.protected.tasks.remove", "CMixedGamodes_v3_CheckNextMap");
            }
        }

        private string[] StringifyMaplist()
        {
            string[] a_strReturn = new string[this.m_lstMaplist.Count];

            for (int i = 0; i < this.m_lstMaplist.Count; i++)
            {
                //foreach (CMapNameRounds cMapNameRound in this.m_lstMaplist) {
                a_strReturn[i] = String.Format("{0} {1}", this.m_lstMaplist[i].MapFileName, this.m_lstMaplist[i].Rounds);
            }

            return a_strReturn;
        }

        private string[] StringifyEmptyMaplist()
        {
            string[] a_strReturn = new string[this.m_lstEmptyMaplist.Count];

            for (int i = 0; i < this.m_lstEmptyMaplist.Count; i++)
            {

                a_strReturn[i] = String.Format("{0} {1}", this.m_lstEmptyMaplist[i].MapFileName, this.m_lstEmptyMaplist[i].Rounds);
            }

            return a_strReturn;
        }


        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Regular Maplist|Maplist", typeof(string[]), this.StringifyMaplist()));
            lstReturn.Add(new CPluginVariable("Emptymap Maplist|Emptymap Maplist", typeof(string[]), this.StringifyEmptyMaplist()));
            lstReturn.Add(new CPluginVariable("Emptymap|Emptymap enabled", typeof(enumBoolYesNo), this.m_enEmptymapEnabled));
            lstReturn.Add(new CPluginVariable("Plugin Console|Show Pluginmessages", typeof(enumBoolYesNo), this.m_enPluginconsoleMessage));
            lstReturn.Add(new CPluginVariable("Playerbased Gamemodes|Use playerbased Gamemodes", typeof(enumBoolYesNo), this.m_enPlayerbasedModeEnabled));
            lstReturn.Add(new CPluginVariable("Playerbased Gamemodes|Supported Gamemodes", "enum.CTimebasedGameplaySupportedGamemodes(SQRUSH & SQDM & RUSH/CONQUEST|SQDM & RUSH/CONQUEST)", this.m_strSelectedSupportedGamemodes));
            lstReturn.Add(new CPluginVariable("Emptymap|Emptymap Staytime", typeof(int), this.m_iEmptymapStaytime));
            lstReturn.Add(new CPluginVariable("Emptymap|Position of Emptymap", typeof(int), this.m_iEmptyMapPosition));
            lstReturn.Add(new CPluginVariable("Emptymap Maplist|Emptymap Maplist Maxplayers", typeof(int), this.m_iEmptyMapMaxplayers));
            //lstReturn.Add(this.GetMapListPluginVariable("Emptymap|Choose Emptymap", "CMixedGamemodes_v3MapList", this.m_strSelectedAllMaps, "{GameMode} - {PublicLevelName}"));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Maplist", typeof(string[]), this.StringifyMaplist()));
            lstReturn.Add(new CPluginVariable("Emptymap Maplist", typeof(string[]), this.StringifyEmptyMaplist()));
            lstReturn.Add(new CPluginVariable("Emptymap enabled", typeof(enumBoolYesNo), this.m_enEmptymapEnabled));
            lstReturn.Add(new CPluginVariable("Show Pluginmessages", typeof(enumBoolYesNo), this.m_enPluginconsoleMessage));
            lstReturn.Add(new CPluginVariable("Use playerbased Gamemodes", typeof(enumBoolYesNo), this.m_enPlayerbasedModeEnabled));
            lstReturn.Add(new CPluginVariable("Supported Gamemodes", "enum.CTimebasedGameplaySupportedGamemodes(SQRUSH & SQDM & RUSH/CONQUEST|SQDM & RUSH/CONQUEST)", this.m_strSelectedSupportedGamemodes));
            lstReturn.Add(new CPluginVariable("Emptymap Staytime", typeof(int), this.m_iEmptymapStaytime));
            lstReturn.Add(new CPluginVariable("Position of Emptymap", typeof(int), this.m_iEmptyMapPosition));
            lstReturn.Add(new CPluginVariable("Emptymap Maplist Maxplayers", typeof(int), this.m_iEmptyMapMaxplayers));
            //lstReturn.Add(this.GetMapListPluginVariable("Choose Emptymap", "CMixedGamemodes_v3MapList", this.m_strSelectedAllMaps, "{GameMode} - {PublicLevelName}"));

            return lstReturn;

        }

        public void SetPluginVariable(string strVariable, string strValue)
        {

            int iTimeSeconds = 15;

            if (strVariable.CompareTo("Maplist") == 0)
            {

                this.m_lstMapDefines = this.GetMapDefines();
                List<string> lstValidateMaplist = new List<string>(CPluginVariable.DecodeStringArray(strValue));

                // Gets rid of the error message when no array is set.
                lstValidateMaplist.RemoveAll(String.IsNullOrEmpty);

                if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^3MixedGamemodes_v3: Loading Maprotation.."));
                }

                if (this.m_lstMaplist.Count > 0)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^3MixedGamemodes_v3: Clearing Maplist.."));
                    }
                    this.m_lstMaplist.Clear();
                }

                string strPreviouslyAddedMapFilename = "";
                bool blShowMapAdditionError = true;
                foreach (string strMapFileNameRound in lstValidateMaplist)
                {

                    string[] a_strMapFileNameRound = strMapFileNameRound.Split(' ');

                    string strMapFileName = "";
                    int iRounds = 0;

                    if (a_strMapFileNameRound.Length >= 1)
                    {
                        strMapFileName = a_strMapFileNameRound[0];

                        if (a_strMapFileNameRound.Length >= 2)
                        {
                            int.TryParse(a_strMapFileNameRound[1], out iRounds);
                        }
                    }

                    if (String.IsNullOrEmpty(strMapFileName) == false)
                    {

                        blShowMapAdditionError = true;

                        foreach (CMap cMapDefine in this.m_lstMapDefines)
                        {
                            if (String.Compare(strMapFileName, cMapDefine.FileName, true) == 0)
                            {
                                if (String.Compare(strPreviouslyAddedMapFilename, strMapFileName, true) != 0)
                                {
                                    this.m_lstMaplist.Add(new CMapNameRounds(cMapDefine.FileName.ToLower(), iRounds));
                                    strPreviouslyAddedMapFilename = cMapDefine.FileName;

                                    if (iRounds == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: Adding ^4{0} ^5{1}^2 to the rotation with default number of rounds..", cMapDefine.GameMode, cMapDefine.PublicLevelName));
                                    }
                                    else if (iRounds != 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: Adding ^4{0} ^5{1}^2 to the rotation with {2} round(s)..", cMapDefine.GameMode, cMapDefine.PublicLevelName, iRounds));
                                    }

                                }
                                else
                                {
                                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1MixedGamemodes_v3: Removing consecutive map \"{0}\" from the list.  Can't run the same map twice in a row =(", strMapFileName));
                                    }
                                }

                                blShowMapAdditionError = false;
                                break;
                            }
                        }

                        if (blShowMapAdditionError == true && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1MixedGamemodes_v3: [ERROR] The map \"{0}\" is not a valid map (or it's unknown to procon at the moment)", strMapFileName));
                        }
                    }
                }

                if (this.m_iCurrentIndex > this.m_lstMaplist.Count)
                {
                    this.m_iCurrentIndex = 0;
                }

                if (this.m_lstMaplist.Count == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] No maps in Maplist found...");
                }
            }

            else if (strVariable.CompareTo("Show Pluginmessages") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enPluginconsoleMessage = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("Emptymap enabled") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enEmptymapEnabled = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_enEmptymapEnabled == enumBoolYesNo.Yes)
                {
                    if (this.m_lstMaplist.Count == 0 && this.m_lstEmptyMaplist.Count == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] No maps in regular Maplist and Emptymaplist found...");
                    }
                }
            }
            else if (strVariable.CompareTo("Use playerbased Gamemodes") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_enPlayerbasedModeEnabled = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.m_enPlayerbasedModeEnabled == enumBoolYesNo.Yes)
                {
                    if (this.m_lstSQRUSHMaplist.Count == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] No 'SQRUSH' maps in Emptymaplist found...");
                    }

                    if (this.m_lstSQDMMaplist.Count == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] No 'SQDM' maps in Emptymaplist found...");
                    }
                }
            }
            else if (strVariable.CompareTo("Supported Gamemodes") == 0)
            {
                this.m_strSelectedSupportedGamemodes = strValue;
            }
            ////////////////Emptymaplist//////////////
            else if (strVariable.CompareTo("Emptymap Maplist") == 0)
            {
                this.m_lstEmptyMapDefines = this.GetMapDefines();
                List<string> lstValidateMaplist = new List<string>(CPluginVariable.DecodeStringArray(strValue));

                // Gets rid of the error message when no array is set.
                lstValidateMaplist.RemoveAll(String.IsNullOrEmpty);

                if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^3MixedGamemodes_v3: LOADING Emptymap Maplist.."));
                }
                if (this.m_lstEmptyMaplist.Count > 0)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^3MixedGamemodes_v3: Clearing Emptymap Maplist.."));
                    }
                    this.m_lstEmptyMaplist.Clear();
                }

                this.m_lstSQRUSHMapDefines = this.GetMapDefines();
                this.m_lstSQDMMapDefines = this.GetMapDefines();

                if (this.m_lstSQRUSHMaplist.Count > 0)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^3MixedGamemodes_v3: Clearing SQRUSH Maplist.."));
                    }
                    this.m_lstSQRUSHMaplist.Clear();
                }

                if (this.m_lstSQDMMaplist.Count > 0)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^3MixedGamemodes_v3: Clearing SQDM Maplist.."));
                    }
                    this.m_lstSQDMMaplist.Clear();
                }

                string strPreviouslyAddedMapFilename = "";
                bool blShowEmptyMapAdditionError = true;
                foreach (string strMapFileNameRound in lstValidateMaplist)
                {

                    string[] a_strMapFileNameRound = strMapFileNameRound.Split(' ');

                    string strMapFileName = "";
                    int iRounds = 0;

                    if (a_strMapFileNameRound.Length >= 1)
                    {
                        strMapFileName = a_strMapFileNameRound[0];

                        if (a_strMapFileNameRound.Length >= 2)
                        {
                            int.TryParse(a_strMapFileNameRound[1], out iRounds);
                        }
                    }

                    if (String.IsNullOrEmpty(strMapFileName) == false)
                    {

                        blShowEmptyMapAdditionError = true;

                        foreach (CMap cMapDefine in this.m_lstEmptyMapDefines)
                        {
                            if (String.Compare(strMapFileName, cMapDefine.FileName, true) == 0)
                            {
                                if (String.Compare(strPreviouslyAddedMapFilename, strMapFileName, true) != 0)
                                {
                                    this.m_lstEmptyMaplist.Add(new CMapNameRounds(cMapDefine.FileName.ToLower(), iRounds));

                                    if (iRounds == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: Adding ^4{0} ^5{1}^2 to the Emptymap Maplist with default number of rounds..", cMapDefine.GameMode, cMapDefine.PublicLevelName));
                                    }
                                    else if (iRounds != 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: Adding ^4{0} ^5{1}^2 to the Emptymap Maplist with {2} round(s)..", cMapDefine.GameMode, cMapDefine.PublicLevelName, iRounds));
                                    }

                                    if (cMapDefine.GameMode == "Squad Deathmatch")
                                    {
                                        this.m_lstSQDMMaplist.Add(new CMapNameRounds(cMapDefine.FileName.ToLower(), iRounds));

                                        if (iRounds == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                        {
                                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: Adding ^4{0} ^5{1}^2 to the ^1SQDM Maplist ^2with default number of rounds..", cMapDefine.GameMode, cMapDefine.PublicLevelName));
                                        }
                                        else if (iRounds != 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                        {
                                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: Adding ^4{0} ^5{1}^2 to the ^1SQDM Maplist ^2with {2} round(s)..", cMapDefine.GameMode, cMapDefine.PublicLevelName, iRounds));
                                        }

                                    }
                                    else if (cMapDefine.GameMode == "Squadrush")
                                    {
                                        this.m_lstSQRUSHMaplist.Add(new CMapNameRounds(cMapDefine.FileName.ToLower(), iRounds));

                                        if (iRounds == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                        {
                                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: Adding ^4{0} ^5{1}^2 to the ^1SQRUSH Maplist ^2with default number of rounds..", cMapDefine.GameMode, cMapDefine.PublicLevelName));
                                        }
                                        else if (iRounds != 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                        {
                                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^MixedGamemodes_v3: 2Adding ^4{0} ^5{1}^2 to the ^1SQRUSH Maplist ^2with {2} round(s)..", cMapDefine.GameMode, cMapDefine.PublicLevelName, iRounds));
                                        }
                                    }

                                    strPreviouslyAddedMapFilename = cMapDefine.FileName;
                                }
                                else
                                {
                                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1MixedGamemodes_v3: Removing consecutive Map \"{0}\" from Emptymap Maplist.  Can't run the same EmptyMap twice in a row =(", strMapFileName));
                                    }
                                }

                                blShowEmptyMapAdditionError = false;
                                break;
                            }
                        }

                        if (blShowEmptyMapAdditionError == true && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1MixedGamemodes_v3: [ERROR] The EmptyMap \"{0}\" is not a valid Map (or it's unknown to procon at the moment)", strMapFileName));
                        }
                    }
                }

                if (this.m_iCurrentEmptymapIndex > this.m_lstEmptyMaplist.Count)
                {
                    this.m_iCurrentEmptymapIndex = 0;
                }

                if (this.m_enEmptymapEnabled == enumBoolYesNo.Yes)
                {
                    if (this.m_lstEmptyMaplist.Count == 0 && this.m_lstMaplist.Count == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] No maps in Emptymaplist/playerbased Maplist found...");
                    }
                }
                else
                {
                    if (this.m_lstEmptyMaplist.Count == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^3MixedGamemodes_v3: No maps found...");
                    }
                }

                if (this.m_enPlayerbasedModeEnabled == enumBoolYesNo.Yes)
                {
                    if (this.m_lstSQRUSHMaplist.Count == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] No 'SQRUSH' maps in Emptymaplist found...");
                    }

                    if (this.m_lstSQDMMaplist.Count == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] No 'SQDM' maps in Emptymaplist found...");
                    }
                }
            }

            else if (strVariable.CompareTo("Emptymap Staytime") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iEmptymapStaytime = iTimeSeconds;
            }

            else if (strVariable.CompareTo("Position of Emptymap") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iEmptyMapPosition = iTimeSeconds;
                if (this.m_enEmptymapEnabled == enumBoolYesNo.Yes)
                {
                    if (this.m_lstMaplist.Count == 0 && this.m_lstEmptyMaplist.Count == 0 && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] No maps in regular Maplist and Emptymaplist found, 'Position of Emptymap' has no effect now...");
                    }
                    else if (this.m_lstEmptyMaplist.Count == 0 && this.m_iEmptyMapPosition >= this.m_lstMaplist.Count && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] 'Position of emptymap' can't be higher than " + (this.m_lstMaplist.Count - 1));
                    }
                    else if (this.m_lstEmptyMaplist.Count >= 1 && this.m_iEmptyMapPosition >= this.m_lstMaplist.Count && this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] 'Position of emptymap' can't be higher than " + (this.m_lstEmptyMaplist.Count - 1));
                    }
                }
            }

            else if (strVariable.CompareTo("Emptymap Maplist Maxplayers") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                this.m_iEmptyMapMaxplayers = iTimeSeconds;
            }

            /*if (strVariable.CompareTo("Choose Emptymap") == 0)
            {
                this.m_strSelectedAllMaps = strValue;

                CMap selectedMap = this.GetMapByFormattedName("{GameMode} - {PublicLevelName}", this.m_strSelectedAllMaps);

                if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^3MixedGamemodes_v3: Selected Emptymap: " + selectedMap.PublicLevelName + " - " + selectedMap.PlayList);
                }
            }*/

            if (this.m_bStaytaskAktive == true)
            {
                this.ExecuteCommand("procon.protected.tasks.remove", "CMixedGamodes_v3_CheckForEmptymap");
                this.m_bStaytaskAktive = false;
            }
        }

        private void UnregisterAllCommands()
        {
            this.UnregisterCommand(new MatchCommand("CMixedGamemodes_v3", "OnCommandNextmap", this.Listify<string>("@", "!", "#", "/"), "nextmap", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Show the next map or number of rounds played"));
        }

        private void RegisterAllCommands()
        {
            if (this.m_bPluginEnabled == true)
            {
                this.RegisterCommand(new MatchCommand("CMixedGamemodes_v3", "OnCommandNextmap", this.Listify<string>("@", "!", "#", "/"), "nextmap", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Show the next map or number of rounds played"));
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

        public void ShowNextMap()
        {
            if (this.m_blFinalRoundSet == false)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "Nextmap will be the current map ( " + this.m_csiLatestServerInfo.CurrentRound + " of " + this.m_csiLatestServerInfo.TotalRounds + " rounds played )", "all");
            }
            else
            {
                if (this.m_iNextmapType == 1)
                {
                    CMap cmNextMap = this.GetMap(this.m_lstMaplist[this.m_iCurrentIndex - 1].MapFileName);

                    if (cmNextMap != null)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Nextmap will be: " + cmNextMap.PublicLevelName + " ( " + cmNextMap.PlayList + " )", "all");
                    }
                }

                else if (this.m_iNextmapType == 2)
                {
                    CMap cmNextMap = this.GetMap(this.m_lstEmptyMaplist[this.m_iCurrentEmptymapIndex - 1].MapFileName);

                    if (cmNextMap != null)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Nextmap will be: " + cmNextMap.PublicLevelName + " ( " + cmNextMap.PlayList + " )", "all");
                    }
                }

                else if (this.m_iNextmapType == 3)
                {
                    CMap cmNextMap = this.GetMap(this.m_lstSQRUSHMaplist[this.m_iRushIndex - 1].MapFileName);

                    if (cmNextMap != null)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Nextmap will be: " + cmNextMap.PublicLevelName + " ( " + cmNextMap.PlayList + " )", "all");
                    }
                }

                else if (this.m_iNextmapType == 4)
                {
                    CMap cmNextMap = this.GetMap(this.m_lstSQDMMaplist[this.m_iDeathmatchIndex - 1].MapFileName);

                    if (cmNextMap != null)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Nextmap will be: " + cmNextMap.PublicLevelName + " ( " + cmNextMap.PlayList + " )", "all");
                    }
                }

                else if (this.m_iNextmapType == 0)
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", "The nextmap is not set yet", "all");
                }
            }
        }

        private string m_strCurrentMapFilename = "";
        public void OnLoadingLevel(string strMapFileName)
        {
            this.m_strCurrentMapFilename = strMapFileName;
        }

        public void OnLevelStarted()
        {
            this.m_iNextmapType = 0;
            this.m_blFinalRoundSet = false;
            this.ExecuteCommand("procon.protected.send", "serverInfo");
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

        private int m_iSkippedErrorMaps = 0;

        // Global or misc..
        public void OnResponseError(List<string> lstRequestWords, string strError)
        {

            if (lstRequestWords.Count >= 2 && String.Compare(lstRequestWords[0], "mapList.append") == 0 && String.Compare(strError, "InvalidMapName") == 0)
            {

                CMap cmErrorMap = this.GetMap(lstRequestWords[1]);

                if (cmErrorMap != null)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1MixedGamemodes_v3: [ERROR] appending map \"{0}\":\"{1} - {2}\".  The server does not recognize it as a valid map.  Removing from mixed gamemode maplist.", cmErrorMap.FileName, cmErrorMap.GameMode, cmErrorMap.PublicLevelName));
                    }
                    for (int i = 0; i < this.m_lstMaplist.Count; i++)
                    {
                        if (String.Compare(this.m_lstMaplist[i].MapFileName, cmErrorMap.FileName) == 0)
                        {
                            this.m_lstMaplist.RemoveAt(i);
                        }
                    }

                    this.m_iSkippedErrorMaps++;
                    this.SetNextMap();
                }
            }

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

        private CMap GetMap(string strMapFilename)
        {

            CMap cmReturn = null;

            if (this.m_iNextmapType == 1)
            {
                foreach (CMap cMapDefine in this.m_lstMapDefines)
                {
                    if (String.Compare(strMapFilename, cMapDefine.FileName) == 0)
                    {
                        cmReturn = cMapDefine;
                        break;
                    }
                }
            }
            else if (this.m_iNextmapType == 2)
            {
                foreach (CMap cMapDefine in this.m_lstEmptyMapDefines)
                {
                    if (String.Compare(strMapFilename, cMapDefine.FileName) == 0)
                    {
                        cmReturn = cMapDefine;
                        break;
                    }
                }
            }
            else if (this.m_iNextmapType == 3)
            {
                foreach (CMap cMapDefine in this.m_lstSQRUSHMapDefines)
                {
                    if (String.Compare(strMapFilename, cMapDefine.FileName) == 0)
                    {
                        cmReturn = cMapDefine;
                        break;
                    }
                }
            }
            else if (this.m_iNextmapType == 4)
            {
                foreach (CMap cMapDefine in this.m_lstSQDMMapDefines)
                {
                    if (String.Compare(strMapFilename, cMapDefine.FileName) == 0)
                    {
                        cmReturn = cMapDefine;
                        break;
                    }
                }
            }

            return cmReturn;
        }

        public void CheckNextMap()
        {
            if (this.m_enPlayerbasedModeEnabled == enumBoolYesNo.Yes)
            {
                if (this.m_iNextmapType == 1 && this.m_iCurrentplayers < SQDM_LIMIT)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: [GAMEMODE CHANGED] Not enough players for RUSH/CONQUEST"));
                    }
                    this.SetNextMap();
                }

                else if (this.m_iNextmapType == 3 && this.m_iCurrentplayers >= SQRUSH_LIMIT)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: [GAMEMODE CHANGED] Too much players for SQRUSH"));
                    }
                    this.SetNextMap();
                }

                else if (this.m_iNextmapType == 4 && this.m_iCurrentplayers >= SQDM_LIMIT)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: [GAMEMODE CHANGED] Too much players for SQDM"));
                    }
                    this.SetNextMap();
                }

                else if (this.m_iNextmapType == 4 && this.m_iCurrentplayers < SQRUSH_LIMIT && this.m_strSelectedSupportedGamemodes == "SQRUSH & SQDM & RUSH/CONQUEST")
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: [GAMEMODE CHANGED] Not enough players for SQDM"));
                    }
                    this.SetNextMap();
                }
            }
            else
            {
                if (this.m_iNextmapType == 1 && this.m_iCurrentplayers <= this.m_iEmptyMapMaxplayers)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: [EMPTYMAP PANIC] Not enough players for regular Maplist, choosing map from Emptymap Maplist now!"));
                    }
                    this.SetNextMap();
                }

                else if (this.m_iNextmapType == 2 && this.m_iCurrentplayers > this.m_iEmptyMapMaxplayers)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2MixedGamemodes_v3: [EMPTYMAP PANIC] Too much players for Emptymap Maplist, choosing map from normal cycle now!"));
                    }
                    this.SetNextMap();
                }
            }
        }

        public void SetNextMap()
        {
            if (this.m_iCurrentplayers > this.m_iEmptyMapMaxplayers || this.m_iCurrentplayers >= SQDM_LIMIT && this.m_enPlayerbasedModeEnabled == enumBoolYesNo.Yes || this.m_enEmptymapEnabled != enumBoolYesNo.Yes || this.m_lstEmptyMaplist.Count == 0)
            {
                this.m_iNextmapType = 1;

                // If the entire maplist is has been removed due to errors..
                // (They've only added the two new maps and the server does not know them yet..)
                if (this.m_lstMaplist.Count == 0)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1MixedGamemodes_v3: [MApLIST PANIC]"));
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1The server does not know any of the maps in the Mixed Gamemode Maplist.  Setting to default RUSH list."));
                    }
                    this.SetPluginVariable("Maplist", "levels/mp_002|levels/mp_004|levels/mp_006|levels/mp_008|levels/mp_009gr|levels/mp_012gr");

                    this.m_iSkippedErrorMaps = 0;
                }

                if (this.m_iCurrentIndex == this.m_lstMaplist.Count)
                    this.m_iCurrentIndex = 0;

                if (this.m_lstMaplist[this.m_iCurrentIndex].MapFileName.ToLower() == this.m_strCurrentMapFilename.ToLower())
                {
                    this.m_iCurrentIndex++;
                    if (this.m_iCurrentIndex == this.m_lstMaplist.Count)
                        this.m_iCurrentIndex = 0;
                }

                CMap cmNextMap = this.GetMap(this.m_lstMaplist[this.m_iCurrentIndex].MapFileName);

                if (cmNextMap != null)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Setting playlist to: " + cmNextMap.PlayList);
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Clearing the maplist");
                    }
                    this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", cmNextMap.PlayList);
                    this.ExecuteCommand("procon.protected.send", "mapList.clear");

                    if (this.m_lstMaplist[this.m_iCurrentIndex].Rounds == 0)
                    {
                        if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Adding " + cmNextMap.FileName + " to the maplist");
                        }
                        this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName);
                    }
                    else if (this.m_lstMaplist[this.m_iCurrentIndex].Rounds > 0)
                    {
                        if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^bMixedGamemodes_v3: Adding {0} to the maplist with {1} rounds", cmNextMap.FileName, this.m_lstMaplist[this.m_iCurrentIndex].Rounds));
                        }
                        this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName, this.m_lstMaplist[this.m_iCurrentIndex].Rounds.ToString());
                    }
                }
                else if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] pulling map information for " + this.m_lstMaplist[this.m_iCurrentIndex]);
                }
                this.m_iCurrentIndex++;

            }
            else if (this.m_iCurrentplayers <= this.m_iEmptyMapMaxplayers && this.m_enEmptymapEnabled == enumBoolYesNo.Yes && this.m_enPlayerbasedModeEnabled != enumBoolYesNo.Yes)
            {
                this.m_iNextmapType = 2;

                // If the entire maplist is has been removed due to errors..
                // (They've only added the two new maps and the server does not know them yet..)
                if (this.m_lstEmptyMaplist.Count == 0)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1MixedGamemodes_v3: [EMPTYMAPLIST PANIC]"));
                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1The server does not know any of the maps in the Emptymap Maplist.  Setting to default RUSH list."));
                    }
                    this.SetPluginVariable("Maplist", "levels/mp_002|levels/mp_004|levels/mp_006|levels/mp_008|levels/mp_009gr|levels/mp_012gr");

                    this.m_iSkippedErrorMaps = 0;
                }

                if (this.m_iCurrentEmptymapIndex == this.m_lstEmptyMaplist.Count)
                    this.m_iCurrentEmptymapIndex = 0;

                if (this.m_lstEmptyMaplist[this.m_iCurrentEmptymapIndex].MapFileName.ToLower() == this.m_strCurrentMapFilename.ToLower())
                {
                    this.m_iCurrentEmptymapIndex++;
                    if (this.m_iCurrentEmptymapIndex == this.m_lstEmptyMaplist.Count)
                        this.m_iCurrentEmptymapIndex = 0;
                }

                CMap cmNextMap = this.GetMap(this.m_lstEmptyMaplist[this.m_iCurrentEmptymapIndex].MapFileName);

                if (cmNextMap != null)
                {
                    if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Setting playlist to: " + cmNextMap.PlayList);
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Clearing the maplist");
                    }
                    this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", cmNextMap.PlayList);
                    this.ExecuteCommand("procon.protected.send", "mapList.clear");

                    if (this.m_lstEmptyMaplist[this.m_iCurrentEmptymapIndex].Rounds == 0)
                    {
                        if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Adding " + cmNextMap.FileName + " to the maplist");
                        }
                        this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName);
                    }
                    else if (this.m_lstEmptyMaplist[this.m_iCurrentEmptymapIndex].Rounds > 0)
                    {
                        if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^bMixedGamemodes_v3: Adding {0} to the maplist with {1} rounds", cmNextMap.FileName, this.m_lstEmptyMaplist[this.m_iCurrentEmptymapIndex].Rounds));
                        }
                        this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName, this.m_lstEmptyMaplist[this.m_iCurrentEmptymapIndex].Rounds.ToString());
                    }
                }
                else if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] pulling map information for " + this.m_lstEmptyMaplist[m_iCurrentEmptymapIndex]);
                }
                this.m_iCurrentEmptymapIndex++;

            }
            else if (this.m_enPlayerbasedModeEnabled == enumBoolYesNo.Yes && this.m_iCurrentplayers < SQDM_LIMIT)
            {

                if (this.m_iCurrentplayers < SQRUSH_LIMIT && this.m_strSelectedSupportedGamemodes == "SQRUSH & SQDM & RUSH/CONQUEST")
                {
                    this.m_iNextmapType = 3;

                    if (this.m_lstSQRUSHMaplist.Count == 0)
                    {
                        if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1MixedGamemodes_v3: [EMPTYMAP PANIC]"));
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1No SQRUSH Maps in Emptymaplist. Setting to default SQRUSH list."));
                        }
                        this.SetPluginVariable("Emptymap Maplist", "levels/mp_001sr|levels/mp_002sr|levels/mp_005sr|levels/mp_009sr|levels/mp_012sr|levels/mp_003sr|levels/bc1_harvest_day_sr|levels/bc1_oasis_sr|levels/mp_sp_002sr");
                        this.m_iSkippedErrorMaps = 0;
                    }

                    if (this.m_iRushIndex == this.m_lstSQRUSHMaplist.Count)
                        this.m_iRushIndex = 0;

                    CMap cmNextMap = this.GetMap(this.m_lstSQRUSHMaplist[this.m_iRushIndex].MapFileName);

                    if (cmNextMap != null)
                    {
                        if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Setting playlist to: " + cmNextMap.PlayList);
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Clearing the maplist");
                        }
                        this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", cmNextMap.PlayList);
                        this.ExecuteCommand("procon.protected.send", "mapList.clear");

                        if (this.m_lstSQRUSHMaplist[this.m_iRushIndex].Rounds == 0)
                        {
                            if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Adding " + cmNextMap.FileName + " to the maplist");
                            }
                            this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName);
                        }
                        else if (this.m_lstSQRUSHMaplist[this.m_iRushIndex].Rounds > 0)
                        {
                            if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^bMixedGamemodes_v3: Adding {0} to the maplist with {1} rounds", cmNextMap.FileName, this.m_lstSQRUSHMaplist[this.m_iRushIndex].Rounds));
                            }
                            this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName, this.m_lstSQRUSHMaplist[this.m_iRushIndex].Rounds.ToString());
                        }
                    }
                    else if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] pulling map information for " + this.m_lstSQRUSHMaplist[this.m_iRushIndex]);
                    }

                    this.m_iRushIndex++;
                }

                else
                {
                    this.m_iNextmapType = 4;

                    if (this.m_lstSQDMMaplist.Count == 0)
                    {
                        if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1MixedGamemodes_v3: [EMPTYMAP PANIC]"));
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1No SQDM Maps in Emptymaplist. Setting to default SQDM list."));
                        }
                        this.SetPluginVariable("Emptymap Maplist", "levels/mp_001sdm|levels/mp_004sdm|levels/mp_006sdm|levels/mp_007sdm|levels/mp_009sdm|levels/mp_008sdm|levels/bc1_harvest_day_sdm|levels/bc1_oasis_sdm|levels/mp_sp_002sdm|levels/mp_sp_005sdm");
                        this.m_iSkippedErrorMaps = 0;
                    }

                    if (this.m_iDeathmatchIndex == this.m_lstSQDMMaplist.Count)
                        this.m_iDeathmatchIndex = 0;

                    CMap cmNextMap = this.GetMap(this.m_lstSQDMMaplist[this.m_iDeathmatchIndex].MapFileName);

                    if (cmNextMap != null)
                    {
                        if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Setting playlist to: " + cmNextMap.PlayList);
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Clearing the maplist");
                        }
                        this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", cmNextMap.PlayList);
                        this.ExecuteCommand("procon.protected.send", "mapList.clear");

                        if (this.m_lstSQDMMaplist[this.m_iDeathmatchIndex].Rounds == 0)
                        {
                            if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Adding " + cmNextMap.FileName + " to the maplist");
                            }
                            this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName);
                        }
                        else if (this.m_lstSQDMMaplist[this.m_iDeathmatchIndex].Rounds > 0)
                        {
                            if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^bMixedGamemodes_v3: Adding {0} to the maplist with {1} rounds", cmNextMap.FileName, this.m_lstSQDMMaplist[this.m_iDeathmatchIndex].Rounds));
                            }
                            this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName, this.m_lstSQDMMaplist[this.m_iDeathmatchIndex].Rounds.ToString());
                        }
                    }
                    else if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] pulling map information for " + this.m_lstSQDMMaplist[this.m_iDeathmatchIndex]);
                    }

                    this.m_iDeathmatchIndex++;
                }
            }
        }

        CServerInfo m_csiLatestServerInfo = null;
        private bool m_blFinalRoundSet = false;
        private bool m_blMapchangeSet = false;
        public void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.m_csiLatestServerInfo = csiServerInfo;

            this.m_strCurrentMapFilename = csiServerInfo.Map;
            this.m_iCurrentplayers = csiServerInfo.PlayerCount;

            if (this.m_csiLatestServerInfo.CurrentRound == this.m_csiLatestServerInfo.TotalRounds)
            {

                if (this.m_blFinalRoundSet == false && this.m_blMapchangeSet == false)
                {

                    this.m_blFinalRoundSet = true;
                    this.SetNextMap();
                }
                else if (this.m_blMapchangeSet == true)
                {
                    this.m_blMapchangeSet = false;
                }
            }

            //Set a task to check for emptymap


            if (this.m_iEmptymapStaytime <= 0)
            {
                this.m_iEmptymapStaytime = 1;
            }

            if (this.m_iCurrentplayers == 0 && this.m_enEmptymapEnabled == enumBoolYesNo.Yes)
            {
                int iSeconds = this.m_iEmptymapStaytime * 60;

                if (this.m_iEmptyMapPosition == -1 && this.m_bStaytaskAktive == false)
                {
                    this.m_bStaytaskAktive = true;
                    this.ExecuteCommand("procon.protected.tasks.add", "CMixedGamodes_v3_CheckForEmptymap", iSeconds.ToString(), iSeconds.ToString(), "-1", "procon.protected.plugins.call", "CMixedGamemodes_v3", "CheckForEmptyMap");
                }
                else
                {
                    if (this.m_lstEmptyMaplist.Count == 0)
                    {
                        if (this.m_bStaytaskAktive == false && this.m_lstMaplist[this.m_iEmptyMapPosition].MapFileName.ToLower() != this.m_strCurrentMapFilename.ToLower())
                        {
                            this.m_bStaytaskAktive = true;
                            this.ExecuteCommand("procon.protected.tasks.add", "CMixedGamodes_v3_CheckForEmptymap", iSeconds.ToString(), iSeconds.ToString(), "-1", "procon.protected.plugins.call", "CMixedGamemodes_v3", "CheckForEmptyMap");
                        }
                        else if (this.m_lstMaplist[this.m_iEmptyMapPosition].MapFileName.ToLower() == this.m_strCurrentMapFilename.ToLower() && this.m_bStaytaskAktive == true)
                        {
                            this.ExecuteCommand("procon.protected.tasks.remove", "CMixedGamodes_v3_CheckForEmptymap");
                            this.m_bStaytaskAktive = false;
                        }
                    }
                    if (this.m_lstEmptyMaplist.Count >= 1)
                    {
                        if (this.m_bStaytaskAktive == false && this.m_lstEmptyMaplist[this.m_iEmptyMapPosition].MapFileName.ToLower() != this.m_strCurrentMapFilename.ToLower())
                        {
                            this.m_bStaytaskAktive = true;
                            this.ExecuteCommand("procon.protected.tasks.add", "CMixedGamodes_v3_CheckForEmptymap", iSeconds.ToString(), iSeconds.ToString(), "-1", "procon.protected.plugins.call", "CMixedGamemodes_v3", "CheckForEmptyMap");
                        }
                        else if (this.m_lstEmptyMaplist[this.m_iEmptyMapPosition].MapFileName.ToLower() == this.m_strCurrentMapFilename.ToLower() && this.m_bStaytaskAktive == true)
                        {
                            this.ExecuteCommand("procon.protected.tasks.remove", "CMixedGamodes_v3_CheckForEmptymap");
                            this.m_bStaytaskAktive = false;
                        }
                    }
                }
            }
            else if (this.m_iCurrentplayers >= 1 && this.m_bStaytaskAktive == true || this.m_enEmptymapEnabled == enumBoolYesNo.No && this.m_bStaytaskAktive == true)
            {
                this.ExecuteCommand("procon.protected.tasks.remove", "CMixedGamodes_v3_CheckForEmptymap");
                this.m_bStaytaskAktive = false;
            }
        }

        public void CheckForEmptyMap()
        {
            if (this.m_iCurrentplayers >= 1)
            {
                //Remove task
                this.ExecuteCommand("procon.protected.tasks.remove", "CMixedGamodes_v3_CheckForEmptymap");
                this.m_bStaytaskAktive = false;
            }
            if (this.m_iCurrentplayers == 0)
            {
                if (this.m_lstEmptyMaplist.Count == 0)
                {
                    this.m_iNextmapType = 1;

                    if (this.m_iEmptyMapPosition >= 0)
                    {
                        CMap cmNextMap = this.GetMap(this.m_lstMaplist[this.m_iEmptyMapPosition].MapFileName);
                        if (cmNextMap != null)
                        {
                            if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Setting playlist to: " + cmNextMap.PlayList);
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Clearing the maplist");
                            }
                            this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", cmNextMap.PlayList);
                            this.ExecuteCommand("procon.protected.send", "mapList.clear");

                            if (this.m_lstMaplist[this.m_iEmptyMapPosition].Rounds == 0)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Adding " + cmNextMap.FileName + " to the maplist");
                                this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName);
                            }
                            else if (this.m_lstMaplist[this.m_iEmptyMapPosition].Rounds > 0)
                            {
                                if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^bMixedGamemodes_v3: Adding {0} to the maplist with {1} rounds", cmNextMap.FileName, this.m_lstMaplist[this.m_iEmptyMapPosition].Rounds));
                                }
                                this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName, this.m_lstMaplist[this.m_iEmptyMapPosition].Rounds.ToString());
                            }
                            this.ExecuteCommand("procon.protected.send", "admin.runNextLevel");
                        }
                        else if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] pulling map information for " + this.m_lstMaplist[this.m_iEmptyMapPosition]);
                        }
                        if (this.m_bStaytaskAktive == false)
                        {
                            //There should no task exist and this should never called, but if ServerInfo is called twice and make 2 or more tasks on 1 time this will remove double tasks 
                            this.ExecuteCommand("procon.protected.tasks.remove", "CMixedGamodes_v3_CheckForEmptymap");
                        }
                    }
                    else if (this.m_iEmptyMapPosition == -1)
                    {
                        if (this.m_blFinalRoundSet == false)
                        {
                            this.m_blMapchangeSet = true;
                            this.SetNextMap();
                        }
                        this.ExecuteCommand("procon.protected.tasks.add", "Changelevel", "1", "1", "1", "procon.protected.send", "admin.runNextLevel");
                    }
                }
                if (this.m_lstEmptyMaplist.Count >= 1)
                {
                    this.m_iNextmapType = 2;

                    if (this.m_iEmptyMapPosition >= 0)
                    {
                        CMap cmNextMap = this.GetMap(this.m_lstEmptyMaplist[this.m_iEmptyMapPosition].MapFileName);

                        if (cmNextMap != null)
                        {
                            if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Setting playlist to: " + cmNextMap.PlayList);
                                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Clearing the maplist");
                            }
                            this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", cmNextMap.PlayList);
                            this.ExecuteCommand("procon.protected.send", "mapList.clear");

                            if (this.m_lstEmptyMaplist[this.m_iEmptyMapPosition].Rounds == 0)
                            {
                                if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMixedGamemodes_v3: Adding " + cmNextMap.FileName + " to the maplist");
                                }
                                this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName);
                            }
                            else if (this.m_lstEmptyMaplist[this.m_iEmptyMapPosition].Rounds > 0)
                            {
                                if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^bMixedGamemodes_v3: Adding {0} to the maplist with {1} rounds", cmNextMap.FileName, this.m_lstMaplist[this.m_iEmptyMapPosition].Rounds));
                                }
                                this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName, this.m_lstEmptyMaplist[this.m_iEmptyMapPosition].Rounds.ToString());
                            }
                            this.ExecuteCommand("procon.protected.send", "admin.runNextLevel");
                        }
                        else if (this.m_enPluginconsoleMessage == enumBoolYesNo.Yes)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1MixedGamemodes_v3: [ERROR] pulling map information for " + this.m_lstEmptyMaplist[this.m_iEmptyMapPosition]);
                        }
                        if (this.m_bStaytaskAktive == false)
                        {
                            //There should no task exist and this should never called, but if ServerInfo is called twice and make 2 or more tasks on 1 time this will remove double second tasks 
                            this.ExecuteCommand("procon.protected.tasks.remove", "CMixedGamodes_v3_CheckForEmptymap");
                        }
                    }
                    else if (this.m_iEmptyMapPosition <= -1)
                    {

                        if (this.m_blFinalRoundSet == false)
                        {
                            this.m_blMapchangeSet = true;
                            this.SetNextMap();
                        }
                        this.ExecuteCommand("procon.protected.tasks.add", "Changelevel", "1", "1", "1", "procon.protected.send", "admin.runNextLevel");
                    }
                }
            }
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

        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
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
            // Map successfully added, cancel the skipped map errors.
            this.m_iSkippedErrorMaps = 0;
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
        #region IPRoConPluginInterface2

        //
        // IPRoConPluginInterface2
        //
        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {

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
            if (this.m_blFinalRoundSet == true && this.m_enEmptymapEnabled == enumBoolYesNo.Yes && this.m_lstEmptyMaplist.Count >= 1)
            {
                this.CheckNextMap();
            }
            this.ExecuteCommand("procon.protected.tasks.add", "CMixedGamodes_v3_ShowNextMap", "1", "1", "1", "procon.protected.plugins.call", "CMixedGamemodes_v3", "ShowNextMap");
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

        public void OnRegisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnUnregisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage)
        {

        }

        public void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {

        }

        public void OnMaplistList(List<MaplistEntry> lstMaplist)
        {

        }

        #endregion


        #region In Game Commands

        public void OnCommandNextmap(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.ShowNextMap();
        }

        #endregion
    }
}