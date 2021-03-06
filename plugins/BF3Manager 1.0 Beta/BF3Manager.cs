/*  
	Copyright 2012 DeadWalking  http://gibthis.com
	
	This file was created for PROCon. http://www.phogue.net/forumvb/forum.php
	
	This plugin is starting foundation of learning C# and coding plugins for PROCon.
 
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

    #region BF3Manager
    public class BF3Manager : PRoConPluginAPI, IPRoConPluginInterface
    {
        const int SrciptDebugVar = 0;
        private bool bf3m_blPluginEnabled;

        //ClassNames//
        private string bf3m_strPlugVarClassBF3Manager = "&.: BF3Manager|";
        private string bf3m_strSelections;

        private string bf3m_strPlugVarClassMapListGeneral = "&.: Map Lists - (General Settings)|";
        private string bf3m_strPlugVarClassMapListSettings = "&:: Map Lists - (Lists Settings)";
        private string bf3m_strPlugVarClassMapListTime = "&:: Map Lists - (Time Based List)|";
        private string bf3m_strPlugVarClassRTV = "&.: Rock the Vote - (Settings)|";
        private string bf3m_strPlugVarClassMessages = "&.: Messages - (InGame Admin)|";
        private string bf3m_strPlugVarClassMessagesPlug = "&.: Messages - (Plugin Console)|";
        private string bf3m_strPlugVarClassVarSettings = "&.: Variables - (Settings)|";
        private string bf3m_strPlugVarClassVarMaxPlayer = "&:: Variables - (Max Players)|";
        private string bf3m_strPlugVarClassVarMaps = "&~: Variables - (Map Lists)|";
        private string bf3m_strPlugVarClassCommands = "&.: InGame Commands - (Settings)|";

        //VarNames//
        private string bf3m_strPlugVarNameSelectInternalPlugin = "BF3Manager (Select Internal Plugin)";

        private string bf3m_strPlugVarNameNumberMapLists = "# of Map Lists";
        private string bf3m_strPlugVarNameNumberMapListsChangeTime = "Time between MapList changes";
        private string bf3m_strPlugVarNameOnOffRotateEmpty = "Use Empty Map Rotation";
        private string bf3m_strPlugVarNameTimeEmpty = "Empty Time in Minutes";
        private string bf3m_strPlugVarNameOnOffRotateRandom = "Use Random Map Rotation";
        private string bf3m_strPlugVarNameOnOffTimeBased = "Use Time Based Map Rotation";
        private string bf3m_strPlugVarNameTimeMapListList = "Time Based Map List";
        private string bf3m_strPlugVarNameTimeMapListStart = "Time Based Map List Start";
        private string bf3m_strPlugVarNameTimeMapListStop = "Time Based Map List Stop";

        private string bf3m_strPlugVarNameNumberMaxPlayerServer = "Max Players allowed on Server";
        private string bf3m_strPlugVarNameOnOffMaxPlayer = "Use Max Players by Maps List/GameType/MaxPlayerServer";
        private string bf3m_strPlugVarNameOnOffVars = "Use Variables by Map";
        private string bf3m_strPlugVarNameOnOffInfantry = "Use Infantry Only";
        private string bf3m_strPlugVarNameNumberInfantry = "I.O. Off @ Player Count";
        private string bf3m_strPlugVarNameNumberVehDelay = "I.O. Default Vehicle Spawn Delay";
        private string bf3m_strPlugVarNameStringMode = "I.O. Default Game Mode";

        private string bf3m_strPlugVarNameDefaultVars = "Default Vars Settings";
        private string bf3m_strPlugVarNameNumberModifyMapList = "Map List Variables to modify";

        private string bf3m_strPlugVarNameOnOffRTV = "Rock the Vote (On/Off)";
        private string bf3m_strPlugVarNameIncludeRTV = "Rock the Vote (Current Map Included)";
        private string bf3m_strPlugVarNameMultiRoundsRTV = "Rock the Vote (Allow RTV on Multi Rounds)";
        private string bf3m_strPlugVarNameNumMapsRTV = "Rock the Vote (# of maps to use in vote)";
        private string bf3m_strPlugVarNameChangeRTV = "Rock the Vote (Change After Vote)";
        private string bf3m_strPlugVarNameChangeTimeRTV = "Rock the Vote (Change After Vote Wait in Seconds)";
        private string bf3m_strPlugVarNameRoundWaitRTV = "Rock the Vote (Wait Minutes after LevelLoad)";
        private string bf3m_strPlugVarNameWaitRTV = "Rock the Vote (Wait Minutes between RTV's)";
        private string bf3m_strPlugVarNameDurationRTV = "Rock the Vote (Duration Minutes to collect votes)";
        private string bf3m_strPlugVarNamePercentageRTV = "Rock the Vote (% Players on Server)";

        private string bf3m_strPlugVarNameDepthDebug = "Debug Depth (How thorough debugging is)";
        private string bf3m_strPlugVarNameDepthVarDebug = "VarDebug Depth (How thorough debugging is)";
        private string bf3m_strPlugVarNameDepthMapDebug = "MapDebug Depth (How thorough debugging is)";
        private string bf3m_strPlugVarNameDepthAdminMess = "InGame Admin Messages (0=Off 1=On 2=VisualAid)";

        private string bf3m_strPlugVarClassCommandNextMap = "Next Map (Display Next Map)";
        private string bf3m_strPlugVarClassCommandRTVList = "Rock the Vote (List of Maps)";
        private string bf3m_strPlugVarClassCommandRTV = "Rock the Vote (Initiate RTV)";

        //Lists//
        private string bf3m_strMapListsNames;
        private string bf3m_strLstNameRot1;
        private string bf3m_strLstNameRot2;
        private string bf3m_strLstNameRot3;
        private string bf3m_strLstNameRot4;
        private string bf3m_strLstNameRot5;
        private string bf3m_strLstNameRot6;
        private string bf3m_strLstNameRot7;

        private int bf3m_iCurrentMapList;
        private int bf3m_iMapNamesListValue;
        private int bf3m_iMapListCount;
        private int bf3m_iMapListChangeTime;
        private int bf3m_iMapListTimeVar;
        private int bf3m_iMapListTimeVarSeconds;
        private enumBoolYesNo bf3m_enUseTimeBasedList;
        private string bf3m_strTimeMapListStart;
        private string bf3m_strTimeMapListStop;
        private DateTime bf3m_dtDateTimeNow;
        private bool bf3m_blTimeBasedChangeStart;
        private bool bf3m_blTimeBasedChangeStop;
        private bool bf3m_blTimeBasedLoaded;

        private int bf3m_iMinPlayersUseRot1;
        private int bf3m_iMinPlayersUseRot2;
        private int bf3m_iMinPlayersUseRot3;
        private int bf3m_iMinPlayersUseRot4;
        private int bf3m_iMinPlayersUseRot5;
        private int bf3m_iMinPlayersUseRot6;
        private int bf3m_iMinPlayersUseRot7;

        private List<CMap> bf3m_lstMapDefinesCheck = new List<CMap>();
        private List<MapListGenerator> bf3m_lstMapLists = new List<MapListGenerator>();
        private List<MapNamesModesRoundsIndex> bf3m_lstMapListsCheck = new List<MapNamesModesRoundsIndex>();

        //Variables//
        private enumBoolYesNo bf3m_enMapListVarsDefault;

        private string bf3m_strMaxPlayersByListOrMode;

        private int bf3m_iMaxPlayersServer;

        private int bf3m_iMaxPlayersRot1;
        private int bf3m_iMaxPlayersRot2;
        private int bf3m_iMaxPlayersRot3;
        private int bf3m_iMaxPlayersRot4;
        private int bf3m_iMaxPlayersRot5;
        private int bf3m_iMaxPlayersRot6;
        private int bf3m_iMaxPlayersRot7;
        private int bf3m_iMaxPlayersTimeBased;

        private int bf3m_iMaxPlayersSR;
        private int bf3m_iMaxPlayersSQDM;
        private int bf3m_iMaxPlayersTDM;
        private int bf3m_iMaxPlayersRUSH;
        private int bf3m_iMaxPlayersCQS0;
        private int bf3m_iMaxPlayersCQS1;
        private int bf3m_iMaxPlayersCQL;

        private List<MapSettings> bf3m_lstDefaultVarSettings = new List<MapSettings>();
        private List<MapSettingsGenerator> bf3m_lstMapSettings = new List<MapSettingsGenerator>();
        private List<MaxPlayersGameType> bf3m_lstGameTypeMaxPlayer = new List<MaxPlayersGameType>();

        private string[] bf3m_straValidVarsBool = { "vars.friendlyfire", "vars.killcam", "vars.minimap", "vars.hud", "vars.3dspotting", "vars.minimapspotting", "vars.nametag", "vars.3pcam", "vars.regeneratehealth", "vars.vehiclespawnallowed", "vars.onlysquadleaderspawn", "vars.crosshair" };
        private string[] bf3m_straValidVarsInt = { "vars.bulletdamage", "vars.playermandowntime", "vars.playerrespawntime", "vars.soldierhealth", "vars.gamemodecounter", "vars.vehiclespawndelay", "vars.idlebanrounds", "vars.idletimeout", "vars.teamkillvaluedecreasepersecond", "vars.teamkillvalueincrease", "vars.teamkillvalueforkick", "vars.teamkillcountforkick", "vars.maxplayers", "vars.roundrestartplayercount", "vars.roundstartplayercount" };

        //Infantry Only//
        private enumBoolYesNo bf3m_enUseInfantryOnly;
        private bool bf3m_blInfantryOnly;
        private string bf3m_strDefaultMode;
        private int bf3m_iMinPlayersInfantryOnly;
        private int bf3m_iDefaultVehDelay;
        private int bf3m_iMapVehDelay;

        //EmptyMap//
        private enumBoolYesNo bf3m_enEmptyRotate;
        private bool bf3m_blContinueEmptyCheck;
        private int bf3m_iEmptyRotateTime;
        private int bf3m_iEmptyRotateTimeInSeconds;
        private int bf3m_iEmptyRotateStoredTime;

        //NextMap//
        private enumBoolYesNo bf3m_enRandomMapIndex;
        private bool bf3m_blNextMapIsCurrentMap;
        private bool bf3m_blSetRandomMapIndex;
        private string bf3m_strNextMapFriendlyName;
        private string bf3m_strNextMapFriendlyMode;
        private string bf3m_strCurrentMapFriendlyName;
        private string bf3m_strCurrentMapFriendlyMode;
        private int bf3m_iMapIndexCurrent;
        private int bf3m_iMapIndexNext;

        //OnPlayerLimit//
        private int bf3m_iCurrentMaxPlayers;

        //OnLevelLoaded//
        private int bf3m_iLvlLRoundsPlayed;
        private int bf3m_iLvlLRoundsTotal;
        private bool bf3m_blLevelIsLoaded;

        //OnServerInfo//
        private string bf3m_strServerName;
        private int bf3m_iPlayerCount;
        private int bf3m_iMaxPlayerCount;
        private string bf3m_strGameMode;
        private string bf3m_strMap;
        private int bf3m_iCurrentRound;
        private int bf3m_iTotalRounds;
        private int bf3m_iRoundTime;
        private List<TeamScore> bf3m_lstTeamScore = new List<TeamScore>();
        private int bf3m_iWinningTeam;

        //RTV//
        private enumBoolOnOff bf3m_enOnOffRTV;
        private enumBoolYesNo bf3m_enRTVInclude;
        private enumBoolYesNo bf3m_enRTVMultiRounds;
        private enumBoolYesNo bf3m_enRTVChange;
        private bool bf3m_blRTVFired;
        private bool bf3m_blRTVStarted;
        private int bf3m_iRTVCount;
        private int bf3m_iRTVChangeTime;
        private int bf3m_iRTVDuration;
        private int bf3m_iRTVDurationInSeconds;
        private int bf3m_iRTVNumMaps;
        private int bf3m_iRTVRoundWait;
        private int bf3m_iRTVRoundWaitInSeconds;
        private int bf3m_iRTVStoredTime;
        private int bf3m_iRTVWait;
        private int bf3m_iRTVWaitInSeconds;
        private int bf3m_iRTVPercentage;

        private List<MapNamesModesRoundsIndex> bf3m_lstRTVMaps = new List<MapNamesModesRoundsIndex>();
        private List<VoterList> bf3m_lstRTVPlayerInfo = new List<VoterList>();

        //Messages//
        private int bf3m_iDebugLevel;
        private int bf3m_iVarDebugLevel;
        private int bf3m_iMapDebugLevel;
        private int bf3m_iAdminDebugLevel;

        //InGame Commands//
        private string bf3m_strNextMap;
        private string bf3m_strRTVList;
        private string bf3m_strRTV;

        public BF3Manager()
        {
            this.bf3m_blPluginEnabled = false;

            this.bf3m_strSelections = "Variables";

            //Lists//
            this.bf3m_strMapListsNames = "None";
            this.bf3m_strLstNameRot1 = "Rotation 1 Name 4 Admin Messages";
            this.bf3m_strLstNameRot2 = "Rotation 2 Name 4 Admin Messages";
            this.bf3m_strLstNameRot3 = "Rotation 3 Name 4 Admin Messages";
            this.bf3m_strLstNameRot4 = "Rotation 4 Name 4 Admin Messages";
            this.bf3m_strLstNameRot5 = "Rotation 5 Name 4 Admin Messages";
            this.bf3m_strLstNameRot6 = "Rotation 6 Name 4 Admin Messages";
            this.bf3m_strLstNameRot7 = "Rotation 7 Name 4 Admin Messages";
            this.bf3m_iCurrentMapList = 99;
            this.bf3m_iMapNamesListValue = 99;
            this.bf3m_iMapListCount = 0;
            this.bf3m_iMapListChangeTime = 9999;
            this.bf3m_iMapListTimeVar = 30;
            this.bf3m_iMapListTimeVarSeconds = 1;
            this.bf3m_strTimeMapListStart = "9:32 PM";
            this.bf3m_strTimeMapListStop = "4:15 AM";
            this.bf3m_enUseTimeBasedList = enumBoolYesNo.No;
            this.bf3m_blTimeBasedChangeStart = false;
            this.bf3m_blTimeBasedChangeStop = false;
            this.bf3m_blTimeBasedLoaded = false;

            //Variables//
            this.bf3m_enMapListVarsDefault = enumBoolYesNo.No;
            this.bf3m_strMaxPlayersByListOrMode = "Off";
            this.bf3m_iMaxPlayersServer = 0;

            //Infantry Only//
            this.bf3m_enUseInfantryOnly = enumBoolYesNo.No;
            this.bf3m_blInfantryOnly = false;
            this.bf3m_strDefaultMode = "Normal/Infantry Only";
            this.bf3m_iMinPlayersInfantryOnly = 0;
            this.bf3m_iDefaultVehDelay = 100;
            this.bf3m_iMapVehDelay = 100;

            //EmptyMap//
            this.bf3m_enEmptyRotate = enumBoolYesNo.No;
            this.bf3m_blContinueEmptyCheck = false;
            this.bf3m_iEmptyRotateTime = 30;
            this.bf3m_iEmptyRotateTimeInSeconds = 0;
            this.bf3m_iEmptyRotateStoredTime = 0;

            //NextMap//
            this.bf3m_enRandomMapIndex = enumBoolYesNo.No;
            this.bf3m_blNextMapIsCurrentMap = false;
            this.bf3m_blSetRandomMapIndex = false;
            this.bf3m_strNextMapFriendlyName = "Not";
            this.bf3m_strNextMapFriendlyMode = "Set";
            this.bf3m_strCurrentMapFriendlyName = "Not";
            this.bf3m_strCurrentMapFriendlyMode = "Set";
            this.bf3m_iMapIndexCurrent = 0;
            this.bf3m_iMapIndexNext = 0;

            //RTV//
            this.bf3m_enOnOffRTV = enumBoolOnOff.Off;
            this.bf3m_enRTVInclude = enumBoolYesNo.No;
            this.bf3m_enRTVMultiRounds = enumBoolYesNo.No;
            this.bf3m_enRTVChange = enumBoolYesNo.No;
            this.bf3m_blRTVFired = false;
            this.bf3m_blRTVStarted = false;
            this.bf3m_iRTVCount = 0;
            this.bf3m_iRTVChangeTime = 5;
            this.bf3m_iRTVDuration = 5;
            this.bf3m_iRTVDurationInSeconds = 0;
            this.bf3m_iRTVNumMaps = 3;
            this.bf3m_iRTVRoundWait = 5;
            this.bf3m_iRTVRoundWaitInSeconds = 0;
            this.bf3m_iRTVStoredTime = 0;
            this.bf3m_iRTVWait = 5;
            this.bf3m_iRTVWaitInSeconds = 0;
            this.bf3m_iRTVPercentage = 50;

            //Messages//
            this.bf3m_iDebugLevel = 0;
            this.bf3m_iVarDebugLevel = 0;
            this.bf3m_iMapDebugLevel = 0;
            this.bf3m_iAdminDebugLevel = 2;

            //InGame Commands//
            this.bf3m_strNextMap = "nextmap";
            this.bf3m_strRTVList = "rtvlist";
            this.bf3m_strRTV = "rtv";

            this.bf3m_iLvlLRoundsPlayed = 0;
            this.bf3m_iLvlLRoundsTotal = 0;
            this.bf3m_blLevelIsLoaded = true;
        }

        #region PluginInfo	
        public string GetPluginName()
        {
            return "BF3Manager";
        }

        public string GetPluginVersion()
        {
            return "1.0 Beta";
        }

        public string GetPluginAuthor()
        {
            return "DeadWalking";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net/forumvb/showthread.php?3640-BF3Manager-1.0-Beta-(15-02-2012)";
        }

        //In HTML format?
        public string GetPluginDescription()
        {
            return @"
					<blockquote>
						<h2>Prelude</h2>
							<p>This is my first plugin for PROCon and my first attempt at coding/scripting C#. So please keep that in mind if you notice some retarded stuff in the code.<br>
							All the not so retarded code I learned from reverse engineering other coders plugin features to determine how things work in C#.<br>
							<br>
							Many Thanks to all scripters on here who post on the forums answering questions. I found most of what I needed by using the search feature + google, and only had to ask 2 questions regarding how things work.<br>
							<br> 
							Special Thanks to Zaeed for explaining how some of the internal methods work. And micovery for answering on how to submit a plugin.<br>
							<br>
							This plugin is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.<br>
							<br>
							<b>I recommend creating a backup of the settings for this plugin once you have created a setup you are happy with, as there is a lot of stuff to go thru and reset should the settings get wiped from the servers PROCon config file.</b></p>
						<h2></h2>
					</blockquote>
					<blockquote>
						<h2>Description</h2>
							<ul>
								<li>Gives admins the ability to utilize and manage up to 7 different map lists dependent on current player count on the server.</li>
								<li>Time Based Map List provides the abillity to run 1 map list between set Time periods.</li>
								<li>Map lists are able to set variables on a per map/per list basis.</li>
								<li>Also allows the adjustment of in-game commands to suit your needs or to prevent a conflict with another plugin.</li>
								<li>Provides some internal plugins somewhat by enabling and disabling features.
									<ul>
										<li>Map Vote(Rock the Vote)</li>
										<li>Empty Map Rotate</li>
										<li>Random Map Rotation</li>
										<li>Dynamic Infantry Only for low player counts</li>
										<li>Variables by Map w/ a defaulting list of variables</li>
										<li>Max Players by MapList/GameType/Max Player allowed on Server.</li>
									</ul></li>
							</ul>
						<h2></h2>
					</blockquote>
					<blockquote>
						<h2>.: BF3Manager</h2>
							<h4>BF3Manager (Select Internal Plugin)</h4>
								<ul>
									<li>Allows the ability to select what area of the plugin you want to modify.</li>
									<li>This implementation was simply to alleviate clutter with to many settings to scroll through at the same time.</li>
								</ul>
						<h2></h2>
					</blockquote>
					<blockquote>
						<h2>.: Messages - (InGame Admin) & (Plugin Console)</h2>
							<h4>InGame Admin Messages (0=Off 1=On 2=VisualAid)</h4>
								<ul>
									<li>0: Will completely disable all in-game admin messages from this plugin, except for the nextmap command.</li>
									<li>1: Will show the most basic of info when producing in-game admin messages</li>
									<li>2: Will add a prefix line to each message. To provide a bit of a visual aid distinguishing the messages from regular chat.
										<ul>
											<li>*****Map Lists*****</li>
											<li>*****Next Map*****</li>
											<li>*****Rock the Vote*****</li>
										</ul></li>
								</ul>
							<h4>Debug Depth (How thorough debugging is)</h4>
								<ul>
									<li>99: Will provide every thing that can be dumped to PluginConsole</li>
									<li>Other allowed values are the same as VarDebug Depth and MapDebug Depth.</li>
								</ul>
							<h4>VarDebug Depth (How thorough debugging is)</h4>
							<h4>MapDebug Depth (How thorough debugging is)</h4>
								<ul>
									<li>0: Will completely disable all Plugin Console messages. Except for some variable limit messages.</li>
									<li>1: Will just provides basic info regarding changes to variables, so you know things are taking effect.</li>
									<li>2: Will provide a slight bump in info from 1.</li>
									<li>3: Will provide a slight bump in info from 2.</li>
								</ul>
						<h2></h2>
					</blockquote>
					<blockquote>
						<h2>.: InGame Commands - (Settings)</h2>
							<h4>!,@,#,/</h4>
								<ul>
									<li>are all acceptable prefixes for in-game commands</li>
								</ul>
							<h4>nextmap</h4>
								<ul>
									<li>Will show the nextmap decided by random or regular rotations, and Rock the Vote map winner.</li>
								</ul>
							<h4>rtvlist</h4>
								<ul>
									<li>If someone missed the maps available for voting it will send a new list to chat.</li>
								</ul>
							<h4>rtv</h4>
								<ul>
									<li>Informs PROCon that you are adding a request for a map vote.</li>
								</ul>
							<h4>rockthevote</h4>
								<ul>
									<li>Same as above left in so newcomers can see it and have a better idea of what !rtv is.</li>
								</ul>
							<h4>vote #</h4>
								<ul>
									<li>While Rock the Vote is active this will add your vote to the list for the map you select by a number value.</li>
								</ul>
                            <h4>stoprtv</h4>
								<ul>
									<li>Will stop and reset a Rock the Vote at any time.</li>
                                    <li><b>Only available to admins that can manipulate the map list in procon.</b></li>
								</ul>
						<h2></h2>
					</blockquote>
					<blockquote>
						<h2>.: Rock the Vote - (Settings)</h2>
							<h4>Rock the Vote (On/Off)</h4>
								<ul>
									<li>Enable and disable the use of the Rock the Vote feature.</li>
									<li>When !rtv goes thru and a map vote starts if 2 maps have equal votes or noone votes then it will randomly select a map from the list to set as the next map.</li>
								</ul>
							<h4>Rock the Vote (Current Map Included)</h4>
								<ul>
									<li>Determine if Rock the Vote should include the current map in its ""random"" selection.</li>
									<li>Does not mean it will always be a selectable map in the vote, just that it is considered when the random maps are chosen</li>
								</ul>
							<h4>Rock the Vote (Allow RTV on Multi Rounds)</h4>
								<ul>
									<li>Yes = RTV will work during the first round of maps that have more than 1 round.</li>
									<li>No = RTV only works if the server is on the last round for the map, from the MapList.txt.</li>
								</ul>
							<h4>Rock the Vote (Change After Vote)</h4>
								<ul>
									<li>Determine if you want maps to finish playing out or if they should be changed after a vote gooes through.</li>
								</ul>
							<h4>Rock the Vote (Change After Vote Wait in Seconds)</h4>
								<ul>
									<li>If Rock the Vote (Change After Vote) is enabled tehn this sets the amount of time to wait before changing the map.</li>
									<li>Value is in seconds and can not be more than 60 seconds.</li>
								</ul>
							<h4>Rock the Vote (# of maps to use in vote)</h4>
								<ul>
									<li>Set the number of maps that will be included in the !rtvlist</li>
									<li>Value can be between 2 - 5, more than that and it is difficult to see all the maps at the top of the list.</li>
									<li><b>If you set this value lower than your smallest map list maps count then it will produce results with the same maps multiple times in the !rtvlist when a vote is called.</b></li>
								</ul>
							<h4>Rock the Vote (Wait Minutes after LevelLoad)</h4>
								<ul>
									<li>How long after a level has loaded before !rtv is recognized.</li>
									<li>Value is in minutes and can not be more than 30 minutes.</li>
								</ul>
							<h4>Rock the Vote (Duration Minutes to collect votes)</h4>
								<ul>
									<li>How long to accept the !vote # command after !rtv has started.</li>
									<li>Value is in minutes and can not be more than 5 minutes.</li>
									<li>Considering upping the time to a larger value if it is asked for enough.</li>
								</ul>
							<h4>Rock the Vote (% Players on Server)</h4>
								<ul>
									<li>Percentage of the current player count on the server needed to type !rtv for a Rock the Vote to start.</li>
								</ul>
						<h2></h2>
					</blockquote>
					<blockquote>
						<h2>.: Map Lists - (General Settings)</h2>
							<h4># of Map Lists</h4>
								<ul>
									<li>Determine how many Map Lists are to be set and available.</li>
									<li>Will store settings for Map Lists if this value is changed to a lower number than previously set.</li>
								</ul>
							<h4>Use Empty Map Rotation</h4>
								<ul>
									<li>Turn Empty Rotate On/Off.</li>
									<li>While On this will rotate your maps every so often determined by the setting below, if there are no players connected.</li>
								</ul>
							<h4>Empty Time in Minutes</h4>
								<ul>
									<li>How long to wait before rotating an empty map if Use Empty Map Rotation is On.</li>
									<li>Value is in minutes and can not be less than 5 minutes or more than 30 minutes.</li>
								</ul>
							<h4>Use Random Map Rotation</h4>
								<ul>
									<li>Turn Random map rotation On/Off</li>
									<li>While On this will randomly select a map from the current map list as the nextmap after a level loads.</li>
								</ul>
							<h4>Use Time Based Map Rotation</h4>
								<ul>
									<li>Turn Time Based Map List On/Off</li>
									<li>While On this will modify the Server Map List at a specified time, no matter what the player count is.</li>
								</ul>
						<h2></h2>
						<br>
						<h2>:: Map Lists - (Lists Settings)</h2>
							List Settings will be created individually depending on how many lists are set to be created.<br>
							<br>
								<h4>#: Map List (Name NameOfList)</h4>
									<ul>
										<li>Gives the Map List a friendly in-game name for admin messages.</li>
										<li>Will also add teh name to all settings that apply to this list.</li>
									</ul>
								<h4>#: Map List Min Players 2 Use (NameOfList)</h4>
									<ul>
										<li>Minimum player count before this map rotation will be enabled, and loaded to the server.</li>
										<li>Keep in mind that by design Map List 1: is your largest player count Map List, and Map List 7: would be your lowest player count map list.</li>
										<li>An Empty Map List can be created by setting your smallest player count list to 0 players minimum and the next Map List to 2 or 1 players minimum.</li>
									</ul>
								<h4>#: Map List Max Players 2 Use (NameOfList)</h4>
									<ul>
										<li>This value is only a Visual Referance so that it is evident at what player counts the List will run at.</li>
										<li><b>YOU CAN NOT EDIT THIS VALUE</b></li>
									</ul>
								<h4>#: Map List (NameOfList)</h4>
									<ul>
										<li>This is the actual list of maps. It accepts maps exactly like the MapList.txt file does.</li>
										<li>XP1_001 SquadRush0 2</li>
										<li>XP1_001 SquadDeathMatch0 1</li>
										<li>XP1_001 TeamDeathMatch0 1</li>
										<li>XP1_001 RushLarge0 2</li>
										<li>XP1_001 ConquestSmall0 1</li>
										<li>XP1_001 ConquestSmall1 1</li>
										<li>XP1_001 ConquestLarge0 1</li>
									</ul>				
						<h2></h2>
						<h2>:: Map Lists - (Time Based List)</h2>
							Only visible if ""Use Time Based Map Rotation"" = ""Yes""<br>
							<br>
								<h4>Time Based Map List Start</h4>
									<ul>
										<li>The time that the Server should switch to the Time Based Map List.</li>
										<li>This value must be in a proper DateTime format i.e. 9:32 PM</li>
									</ul>
								<h4>Time Based Map List Stop</h4>
									<ul>
										<li>The time that the Server should switch back to running the regular Map Lists.</li>
										<li>This value must be in a proper DateTime format i.e. 4:15 AM</li>
									</ul>
								<h4>Time Based Map List</h4>
									<ul>
										<li>The list of maps that will be used for Time Based Map Lists.</li>
										<li>XP1_001 SquadRush0 2</li>
										<li>XP1_001 SquadDeathMatch0 1</li>
										<li>XP1_001 TeamDeathMatch0 1</li>
										<li>XP1_001 RushLarge0 2</li>
										<li>XP1_001 ConquestSmall0 1</li>
										<li>XP1_001 ConquestSmall1 1</li>
										<li>XP1_001 ConquestLarge0 1</li>
									</ul>				
						<h2></h2>
					</blockquote>
					<blockquote>
						<h2>.: Variables - (Settings)</h2>
							<h4>Max Players allowed on Server</h4>
								<ul>
									<li>Maximum players that the server can hold, generally determined by what you rent from your GSP(Game Server Provider).</li>
									<li>Is used for a lot of the plugins functions so make it a valid value.</li>
									<li>Value can not be less than 8 or more than 64.</li>
								</ul>
							<h4>Use Max Players by Maps List/GameType/MaxPlayerServer</h4>
								<ul>
									<li><b>If you use AdaptiveServerSize plugin or any other plugins that modify vars.maxPlayers then do not use this as the plugins will conflict a bit.</b></li>
									<li>Determine if the plugin should set the maxplayers dynamically for the server, and by what method.</li>
								</ul>
							<h4>Use Variables by Map</h4>
								<ul>
									<li><b>If you use CSettingChangeOnMap plugin or any other plugins that modify game variables then do not use this as the plugins will conflict a bit.</b></li>
									<li>Turn On/Off the ability to set variables on a per map/per list basis.</li>
									<li>Events are fired on round end.</li>
								</ul>
							<h4>Use Infantry Only</h4>
								<ul>
									<li><b>Due to issues with some maps spawning vehicles when vars.vehicleSpawnAllowed false is set before or at level load, then vars.vehicleSpawnAllowed true is set after the level has been loaded. I have made a work around with several options for I.O. to try and cater to all the available methods it can be setup and work.</b></li>
									<li>Turn dynamic Infantry Only On/Off.</li>
									<li>Tells PROCon to start performing I.O. checks and setting values according to how you have I.O. set up with the below settings.<br>
										<b>This can cause your Server to show as Custom in BattleLog if you run Hardcore Depending on how you set ""I.O. Default Game Mode"". If you Run Normal mdoe it will change between Normal and InfantryOnly</b></li>
								</ul>
							<h4>I.O. Off @ Player Count</h4>
								<ul>
									<li>Set the player count needed on the server to disable the Infantry only completely until the player count falls back below this value.</li>
									<li>Value can not be less than 0 or more than 64.</li>
								</ul>
							<h4>I.O. Default Vehicle Spawn Delay</h4>
								<ul>
									<li>Used only if you have ""I.O. Default Game Mode"" set to ""Retain Hardcore""</li>
									<li>When I.O. is fired on a hardcore server then it turns vars.vehicleSpawnAllowed false and vars.vehicleSpawnDelay 999999, once the level has loded it then sets vars.vehicleSpawnAllowed true. If I.O. is disabled it then sets vars.vehicleSpawnDelay to this default value.</li>
								</ul>
							<h4>I.O. Default Game Mode</h4>
								<ul>
									<li>Allows a server admin to determine what type of I.O. is to be used on the server.</li>
									<li>Normal/Infantry Only</li>
										<li>Used if the server is setup to run Normal mode. Will cause Battlelog to show as Infantry Only when it is fired on PROCon</li>
									<li>Retain Hardcore</li>
										<li>Used if the server is setup to run Hardcore mode. Will cause Battlelog to show as Hardcore when it is fired on PROCon.</li>
										<li>Will only completely work on some maps. SQDM maps always spawn a vehicle and do not respond to vars.vehicleSpawnDelay. Some CQ/Rush maps will spawn vehicles, but once destroyd the first time will not spawn until I.O is disabled again.</li>
									<li>Allow Custom</li>
										<li>If using Hardcore as a default this will ignore trying to retain Hardcore in Battlelog and just completly turn vehicle spawning off until I.O. is disabled for player count.</li>
								</ul>
						<h2></h2>
						<br>
						<h2>:: Variables - (Max Players)</h2>
							Provide a list of max player settings depending on what value is selected for Use Max Players by Maps List/GameType/MaxPlayerServer.<br>
							<br>
								<h4>Maps List</h4>
									<ul>
										<li>Provides a Max Player setting for each map list that has been created.</li>
									</ul>
								<h4>GameType</h4>
									<ul>
										<li>Provides a Max Player setting for each GameType supported by BF3.</li>
										<li>Squad Rush</li>
										<li>SQDM</li>
										<li>TDM</li>
										<li>Rush</li>
										<li>Conquest</li>
										<li>ConquestAssault</li>
										<li>Conquest 64</li>
									</ul>
								<h4>MaxPlayerServer</h4>
									<ul>
										<li>This will ensure that your server Max Players(vars.maxPlayers) never drops below the value set for Max Players allowed on Server.</li>
									</ul>			
						<h2></h2>
						<br>
						<h2>~: Variables - (Map Lists)</h2>
								Variables are acceptted exactly like they are in the StartUp.txt file on the server.
								<ul>
									<li>vars.gameModeCounter 100</li>
									<li>vars.autoBalance true</li>
								</ul>
								<br>
								<h4>Default Vars Settings</h4>
									<ul>
										<li>A list of default values for variables on the server.</li>
										<li>Any variables set for a specific map or maps should have a counter value for most of the other maps in a list. This setting allows for those defaults to be sved and set at round end for each map.</li>
									</ul>
								<h4>Map List Variables to modify</h4>
									<ul>
										<li>Provides drop down list of all the map lists, it will only provide maps to adjust settings for if that Map List has maps in it and if it is the selected value.</li>
									</ul>
								<h4>(NameOfMap)</h4>
									<ul>
										<li>If you have maps in the currently selected Map List Variables to modify, then there will be an input box for each map in that map list to set variables on a per map basis.</li>
										<li>Variables set for each map are only fired for that map if it is the next map in rotation at round end. This list is compared to the Default Vars Settings to ensure that if a variable exists in both that only the map setting will be applied.</li>
										<li>All variables from Default Vars Settings that are not in each maps settings will be fired on round end as well.</li>
									</ul>			
						<h2></h2>						
					</blockquote>
					<br>
					<blockquote>
					<h2>Version</h2>
						<h4>BF3Manager 1.0 Beta</h4>
						<ul>
							<li>Fixed variables by map in Random mode.</li>
							<ul>
								<li>Even though I fixed the variables by map in the last build it was only working on a regular rotation, if ""Use Random Rotation"" = ""Yes"" it was still setting the wrong variables.</li>
							</ul>						
						</ul>
						<h4>BF3Manager 0.9 Beta</h4>
						<ul>
							<li>Fixed a variables by map bug</li>
							<ul>
								<li>Plugin was only sending the last variable it read from the lists to the server.</li>
							</ul>
							<li>Changed tied or no votes for !RTV</li>
							<ul>
								<li>Plugin would choose a random map from the vote list if no one voted r votes were tied. Changed it to do nothing in both those cases.</li>
								<li>Also allowed for !RTV to be fired again in the case of this event occuring.</li>
							</ul>
						</ul>
						<h4>BF3Manager 0.8 Beta</h4>
						<ul>
							<li>Added a Time Based Map List</li>
							<ul>
								<li>Allows the server to run a specific set of maps between 2 set Times ""Time Based Map List Start"" and ""Time Based Map List Stop""</li>
							</ul>						
						</ul>
						<h4>BF3Manager 0.7 Beta</h4>
						<ul>
							<li>Added a check for time since the Server Map List has been modified</li>
							<ul>
								<li>To prevent the Map List changing to often when players leave the server.</li>
								<li>If the Map List has been modified in the last 30 minutes, and the Map List that should be loaded has a smaller player count then the current Map List, then BF3Manager does nothing.</li>
								<li>If the Map List has been modified in the last 30 minutes, and the Map List that should be loaded has a larger player count then the current Map List, then BF3Manager will still modify the Server Map List.</li>
								<li>If server operators find that they need to modify this value it can be added as a Plugin Setting.</li>
							</ul>						
						</ul>
						<h4>BF3Manager 0.6 Beta</h4>
						<ul>
							<li>Forced the end user to set Max Players allowed on Server</li>
							<ul>
								<li>Before being able to adjust any other settings the end user must set a proper value for Max Players allowed on Server, to help ensure that all the map list settings are accurate.</li>
							</ul>
                            <li>Fixed an issue for map lists and server restarts</li>
							<ul>
								<li>If the server happened to crash or be restarted there was a possibility that the plugin map list would not match the servers map list.</li>
								<li>Now when OnLevelLoad is fired the plugin checks the current plugin map list against the server maplist and changes it if they are diffrent.</li>
								<li>OnLevelLoad is is an event that is fired nearly immediately after PROCon reconnects to a game server.</li>
							</ul>	
							<li>Fixed a string issue with the Normal/Infantry Only setting.</li>
							<ul>
								<li>Plugin was loading with an incorrect string for I.O. Default Game Mode ""Normal"", should have been ""Normal/Infantry Only""</li>
							</ul>							
						</ul>
						<h4>BF3Manager 0.5 Beta</h4>
						<ul>
                            <li>Fixed small issue with map lists</li>
							<ul>
								<li>If you changed the # Map Lists to asmaller value then the plugin could get stuck in a loop changing between 2 map lists.</li>
							</ul>							
						</ul>
						<h4>BF3Manager 0.4 Beta</h4>
						<ul>
                            <li>Reworked RTV a little</li>
							<ul>
								<li>Had several issues prior that I was not able to create when testing in a low player count server.</li>
								<li>Thanks again to FurmanTheGerman for providing a server to test on and help in getting dialed in.</li>
							</ul>
							<li>Modified the Infantry Only feature</li>
							<ul>
								<li>Allows for an admin to decide if they are running Normal/InfantryOnly, Try and Retain Hardcore, or just allow the server to go cstom if running Hardcore.</li>
								<li>Due to isues with how vehicle spawns work across all the maps. Hardocre Infantry Only while retaining a Hardcore setting in Battlelog means that half the maps in SQDM,TDM,Rush, and CQ modes will always spawn vehicles once vars.vehicleSpawnAllowed is set to true at any time.</li>
							</ul>
							<li>Modified Variables per Map</li>
							<ul>
								<li>Was only pulling the Default vars correctly, and not grabbing the nextmap vars. Changed it to pull defaults first and then the map vars for nextmap.</li>
								<li>It will compare the default vars to the nextmap vars and only run vars from default that are not present in the nextmap vars, then run the nextmap vars.</li>
							</ul>
						</ul>
						<h4>BF3Manager 0.3 Beta</h4>
						<ul>
                            <li>Optimized code a little</li>
							<ul>
								<li>Moved the main code for BF3Manager into its own thread, most of the work occurs in this new thread.</li>
							</ul>
							<li>RTV was semi broken</li>
							<ul>
								<li>Found the issue with the help of FurmanTheGerman, and he provided a testing ground for the day to help with tracking down the issues.</li>
								<li>Seemed to work most of the time, but would not run if the plugin was not reloading properly when ProCon was loaded.</li>
								<li>Modified it to work properly added vars to OnPluginEnabled and OnPluginLoaded events to help prevent it as an issue.</li>
							</ul>
							<li>RTV added a couple settings</li>
							<ul>
								<li>Added a setting to allow RTV to work in any round on maps that have more than 1 round set.</li>
								<li>Added a setting to allow the # of maps available in !rtvlist to be set to a differnt value.</li>
							</ul>
                            <li>In-Game Commands added !rtvstop</li>
							<ul>
								<li>Will stop a Rock the Vote at any time. If the player issueing the command has rights to modify the maplist.</li>
							</ul>
						</ul>
						<h4>BF3Manager 0.2 Beta</h4>
						<ul>
							<li>Changed Infantry Only so that it will not retain Hardcore/Normal.</li>
							<ul>
								<li>Some maps in each mode will not spawn vehicles, and some will when ""vars.vehicleSpawnAllowed"" is set to ""false"" on level load then set back to ""true"" at any point.<br>
									This prevents this plugin from being able to truly remove vehicles and retain Hardcore/Normal mode.</li>
							</ul>
							<li>Tweaked SetNextMap</li>
							<ul>
								<li>It was selecting the wrong map when random rotation had been turned on then off again.</li>
							</ul>
							<li>Fixed a time till RTVWait was responding incorrectly.</li>
							<ul>
								<li>It was responding incorrectly with the incorrect amount of time. Causing RTV's not to fire.</li>
							</ul>
							<li>Removed old code</li>
							<ul>
								<li>Had some commented out code in the plugin and decided to remove it.</li>
							</ul>
						</ul>
						<h4>BF3Manager 0.1 Beta</h4>
						<ul>
							<li>Currently still a Beta build until I get some feedback on bugs or issues.</li>
						</ul>
					</blockquote>
					<br>
					";
        }
        #endregion

        #region PluginSetup	
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents
            (
                this.GetType().Name,
                "OnMaxPlayers",
                "OnMaplistList",
                "OnLevelLoaded",
                //"OnResponseError",
                "OnServerInfo",
                "OnMaplistList",
                "OnMaplistGetMapIndices",
                "OnRoundOver"
            );

            this.bf3m_lstMapDefinesCheck = this.GetMapDefines();

            this.bf3m_iCurrentMapList = 99;
            this.bf3m_blInfantryOnly = false;
            this.bf3m_iMapVehDelay = this.bf3m_iDefaultVehDelay;
            this.bf3m_blContinueEmptyCheck = false;
            this.bf3m_iEmptyRotateStoredTime = 0;
            this.bf3m_blNextMapIsCurrentMap = true;
            this.bf3m_blSetRandomMapIndex = true;
            this.bf3m_strNextMapFriendlyName = "Not";
            this.bf3m_strNextMapFriendlyMode = "Set";
            this.bf3m_strCurrentMapFriendlyName = "Not";
            this.bf3m_strCurrentMapFriendlyMode = "Set";
            this.bf3m_iMapIndexCurrent = 0;
            this.bf3m_iMapIndexNext = 0;
            this.bf3m_blRTVFired = false;
            this.bf3m_blRTVStarted = false;
            this.bf3m_iRTVCount = 0;
            this.bf3m_iRTVStoredTime = 0;
            this.bf3m_blLevelIsLoaded = true;
            this.bf3m_blTimeBasedLoaded = false;

            this.bf3m_lstTeamScore.Clear();
            this.bf3m_lstRTVPlayerInfo.Clear();
        }

        /*public void OnResponseError(List<string> requestWords, string error) {
			for (int i = 0; i < requestWords.Count; i++) {
				Write2PluginConsole(String.Format("{0} - {1}", requestWords[i], error), "OnResponseError", "Debug", 100);
			}
		}*/

        public void OnPluginEnable()
        {
            //Tasks to run when plugin loads
            this.bf3m_blPluginEnabled = true;

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^3[" + this.GetPluginName() + "] ^1Enabled");
            this.RegisterAllCommands();

            this.bf3m_iCurrentMapList = 99;
            this.bf3m_blInfantryOnly = false;
            this.bf3m_iMapVehDelay = this.bf3m_iDefaultVehDelay;
            this.bf3m_blContinueEmptyCheck = false;
            this.bf3m_iEmptyRotateStoredTime = 0;
            this.bf3m_blNextMapIsCurrentMap = false;
            this.bf3m_blSetRandomMapIndex = false;
            this.bf3m_strNextMapFriendlyName = "Not";
            this.bf3m_strNextMapFriendlyMode = "Set";
            this.bf3m_strCurrentMapFriendlyName = "Not";
            this.bf3m_strCurrentMapFriendlyMode = "Set";
            this.bf3m_iMapIndexCurrent = 0;
            this.bf3m_iMapIndexNext = 0;
            this.bf3m_blRTVFired = false;
            this.bf3m_blRTVStarted = false;
            this.bf3m_iRTVCount = 0;
            this.bf3m_iRTVStoredTime = 0;
            this.bf3m_blLevelIsLoaded = true;
            this.bf3m_blTimeBasedLoaded = false;

            this.bf3m_lstTeamScore.Clear();
            this.bf3m_lstRTVPlayerInfo.Clear();

            Thread bf3m_thrMainLoop = new Thread(new ThreadStart(delegate ()
            {
                while (this.bf3m_blPluginEnabled)
                {
                    Thread.Sleep(1000);
                    this.BF3MMainLoop();
                }
                Write2PluginConsole(String.Format("Stopping MainLoop"), "OnPluginEnable", "Debug", 100);
            }));
            bf3m_thrMainLoop.Start();
        }

        public void OnPluginDisable()
        {
            //Tasks to run when plugin stops
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^3[" + this.GetPluginName() + "] ^1Disabled");
            this.UnregisterAllCommands();

            this.bf3m_blPluginEnabled = false;
        }
        #endregion

        #region SetPluginVariables
        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iHelperInt = 999;
            DateTime dtHelper;

            if (strVariable.CompareTo("PlugVarNameValue1") == 0)
            {
                this.bf3m_strLstNameRot1 = strValue;
                MapListGenerator mapSetName = this.bf3m_lstMapLists[1];
                mapSetName.ListName = strValue;
            }
            else if (strVariable.CompareTo("PlugVarNameValue2") == 0)
            {
                this.bf3m_strLstNameRot2 = strValue;
                MapListGenerator mapSetName = this.bf3m_lstMapLists[2];
                mapSetName.ListName = strValue;
            }
            else if (strVariable.CompareTo("PlugVarNameValue3") == 0)
            {
                this.bf3m_strLstNameRot3 = strValue;
                MapListGenerator mapSetName = this.bf3m_lstMapLists[3];
                mapSetName.ListName = strValue;
            }
            else if (strVariable.CompareTo("PlugVarNameValue4") == 0)
            {
                this.bf3m_strLstNameRot4 = strValue;
                MapListGenerator mapSetName = this.bf3m_lstMapLists[4];
                mapSetName.ListName = strValue;
            }
            else if (strVariable.CompareTo("PlugVarNameValue5") == 0)
            {
                this.bf3m_strLstNameRot5 = strValue;
                MapListGenerator mapSetName = this.bf3m_lstMapLists[5];
                mapSetName.ListName = strValue;
            }
            else if (strVariable.CompareTo("PlugVarNameValue6") == 0)
            {
                this.bf3m_strLstNameRot6 = strValue;
                MapListGenerator mapSetName = this.bf3m_lstMapLists[6];
                mapSetName.ListName = strValue;
            }
            else if (strVariable.CompareTo("PlugVarNameValue7") == 0)
            {
                this.bf3m_strLstNameRot7 = strValue;
                MapListGenerator mapSetName = this.bf3m_lstMapLists[7];
                mapSetName.ListName = strValue;
            }

            //Internal Plugin Setting
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameSelectInternalPlugin + "") == 0)
            {
                this.bf3m_strSelections = strValue;
            }

            //MaxPlayersServer
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameNumberMaxPlayerServer + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if (iHelperInt <= 64 && iHelperInt >= 0)
                {
                    this.bf3m_iMaxPlayersServer = iHelperInt;
                    MapListGenerator mapListChangeMaxPlayer0 = this.bf3m_lstMapLists[0];
                    mapListChangeMaxPlayer0.ListMaxPlayers2Use = iHelperInt;
                    MapListGenerator mapListChangeMaxPlayer1 = this.bf3m_lstMapLists[1];
                    mapListChangeMaxPlayer1.ListMaxPlayers2Use = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 64)
                {
                    Write2PluginConsole(String.Format("Max Players on Server can not be less than 0 or mroe than 64"), "SetPluginVars", "Debug", 0);
                }
            }

            //MapLists + 2 Variable Settings
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameNumberMapLists + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if (iHelperInt >= 0 && iHelperInt <= 7)
                {
                    this.bf3m_iMapListCount = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 7)
                {
                    Write2PluginConsole(String.Format("Only allowed values are from 0 - 7 different map lists."), "SetPluginVars", "Debug", 0);
                }
                /*} else if (strVariable.CompareTo(""+this.bf3m_strPlugVarNameNumberMapListsChangeTime+"") == 0 && int.TryParse(strValue, out iHelperInt) == true) {
                    if (iHelperInt >= 0 && iHelperInt <= 60)	{
                        this.bf3m_iMapListTimeVar = iHelperInt;
                        this.bf3m_iMapListTimeVarSeconds = (this.bf3m_iMapListCount * 60);
                    } else if (iHelperInt < 0 || iHelperInt > 60) {
                        Write2PluginConsole(String.Format("Only allowed values are from 0 - 60 different map lists."), "SetPluginVars", "Debug", 0);
                    }*/
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameOnOffRotateEmpty + "") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.bf3m_enEmptyRotate = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.bf3m_enEmptyRotate == enumBoolYesNo.Yes)
                {
                    this.bf3m_blContinueEmptyCheck = true;
                }
                else if (this.bf3m_enEmptyRotate == enumBoolYesNo.No)
                {
                    this.bf3m_blContinueEmptyCheck = false;
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameTimeEmpty + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 30 && iHelperInt > 4) || SrciptDebugVar == 1)
                {
                    this.bf3m_iEmptyRotateTime = iHelperInt;
                }
                else if (iHelperInt <= 4)
                {
                    Write2PluginConsole(String.Format("Empty Rotate Time can not be set lower than 5 minutes so players can load into game before it fires."), "SetPluginVars", "Debug", 0);
                }
                this.bf3m_iEmptyRotateTimeInSeconds = this.bf3m_iEmptyRotateTime * 60;
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameOnOffRotateRandom + "") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.bf3m_enRandomMapIndex = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameOnOffTimeBased + "") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.bf3m_enUseTimeBasedList = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameTimeMapListList + "") == 0)
            {
                MapListGenerator mapListSetVar = this.bf3m_lstMapLists[0];
                List<string> lstValidateMaplist = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                lstValidateMaplist.RemoveAll(String.IsNullOrEmpty);
                this.GetMapNamesModesRounds(mapListSetVar.ListMapsList, lstValidateMaplist, mapListSetVar.ListName, 0);
                this.GetMaps4Settings(lstValidateMaplist, 0);
            }/* else if (strVariable.CompareTo(string.Format("Time Based: {0} {1}", this.bf3m_lstMapSettings[0].MapName, this.bf3m_lstMapSettings[0].MapMode)) == 0) {
				MapSettingsGenerator mapListSetVar = this.bf3m_lstMapSettings[0];
				List<string> lstValidatelist = new List<string>(CPluginVariable.DecodeStringArray(strValue));
				lstValidatelist.RemoveAll(String.IsNullOrEmpty);
				this.GetMapSettings(mapListSetVar.MapsSettings, lstValidatelist, 0, mapListSetVar.MapName, mapListSetVar.MapMode);
			}*/
            else if (strVariable.CompareTo("Time Based: Map List Max Players") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                MapListGenerator mapListSetVar = this.bf3m_lstMapLists[0];
                if (iHelperInt <= this.bf3m_iMaxPlayersServer && iHelperInt >= 0)
                {
                    mapListSetVar.ListMaxPlayers = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > this.bf3m_iMaxPlayersServer)
                {
                    Write2PluginConsole(String.Format("MaxPlayers by GameType can not be less than 0 or more than " + this.bf3m_strPlugVarNameNumberMaxPlayerServer + ""), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameTimeMapListStart + "") == 0 && DateTime.TryParse(strValue, out dtHelper))
            {
                try
                {
                    dtHelper = DateTime.Parse(strValue);
                    string format = "h:mm tt";
                    this.bf3m_strTimeMapListStart = dtHelper.ToString(format);
                }
                catch (FormatException)
                {
                    Write2PluginConsole(String.Format("Not a valid DateTime format i.e.- 9:32 PM"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameTimeMapListStop + "") == 0 && DateTime.TryParse(strValue, out dtHelper))
            {
                try
                {
                    dtHelper = DateTime.Parse(strValue);
                    string format = "h:mm tt";
                    this.bf3m_strTimeMapListStop = dtHelper.ToString(format);
                }
                catch (FormatException)
                {
                    Write2PluginConsole(String.Format("Not a valid DateTime format i.e.- 4:15 AM"), "SetPluginVars", "Debug", 0);
                }
            }

            int iCurrentID = 0;
            int iA = strVariable.IndexOf("|");
            int iB = strVariable.IndexOf(":");
            int iC = iB - iA;
            if (iC > 0)
            {
                int.TryParse(strVariable.Substring(strVariable.IndexOf("|") + 1, (strVariable.IndexOf(":") - strVariable.IndexOf("|") - 1)), out iHelperInt);
                iCurrentID = iHelperInt;

                if (this.bf3m_lstMapLists.Count >= iCurrentID)
                {
                    MapListGenerator mapListSetVar = this.bf3m_lstMapLists[iCurrentID];

                    if (strVariable.CompareTo(string.Format("{0}: Map List {1}", mapListSetVar.ListId, mapListSetVar.ListName)) == 0)
                    {
                        List<string> lstValidateMaplist = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                        lstValidateMaplist.RemoveAll(String.IsNullOrEmpty);
                        this.GetMapNamesModesRounds(mapListSetVar.ListMapsList, lstValidateMaplist, mapListSetVar.ListName, iCurrentID);
                        this.GetMaps4Settings(lstValidateMaplist, iCurrentID);
                    }
                    else if (strVariable.CompareTo(string.Format("{0}: Map List Name {1}", mapListSetVar.ListId, mapListSetVar.ListName)) == 0)
                    {
                        mapListSetVar.ListName = strValue;
                        if (iCurrentID == 1) { this.bf3m_strLstNameRot1 = strValue; }
                        if (iCurrentID == 2) { this.bf3m_strLstNameRot2 = strValue; }
                        if (iCurrentID == 3) { this.bf3m_strLstNameRot3 = strValue; }
                        if (iCurrentID == 4) { this.bf3m_strLstNameRot4 = strValue; }
                        if (iCurrentID == 5) { this.bf3m_strLstNameRot5 = strValue; }
                        if (iCurrentID == 6) { this.bf3m_strLstNameRot6 = strValue; }
                        if (iCurrentID == 7) { this.bf3m_strLstNameRot7 = strValue; }
                    }
                    else if (strVariable.CompareTo(string.Format("{0}: Map List Min Players 2 Use {1}", mapListSetVar.ListId, mapListSetVar.ListName)) == 0 && int.TryParse(strValue, out iHelperInt) == true)
                    {
                        if (iHelperInt <= mapListSetVar.ListMaxPlayers2Use && iHelperInt >= 0)
                        {
                            mapListSetVar.ListMinPlayers2Use = iHelperInt;
                            foreach (MapListGenerator mapListChangePlayerCount in this.bf3m_lstMapLists)
                            {
                                if (mapListSetVar.ListId == (iCurrentID + 1) && mapListSetVar.ListMaxPlayers2Use != (mapListSetVar.ListMinPlayers2Use - 1))
                                {
                                    mapListSetVar.ListMaxPlayers2Use = (mapListChangePlayerCount.ListMinPlayers2Use - 1);
                                }
                                else if (mapListChangePlayerCount.ListId == (iCurrentID - 1) && (iCurrentID - 1) != 0)
                                {
                                    mapListSetVar.ListMaxPlayers2Use = (mapListChangePlayerCount.ListMinPlayers2Use - 1);
                                }
                                else if (mapListChangePlayerCount.ListId > iCurrentID)
                                {
                                    if (mapListChangePlayerCount.ListMaxPlayers2Use == -1 && mapListSetVar.ListMinPlayers2Use >= 0)
                                    {
                                        mapListChangePlayerCount.ListMaxPlayers2Use = (mapListSetVar.ListMinPlayers2Use - 1);
                                    }
                                    if (mapListChangePlayerCount.ListMinPlayers2Use >= mapListSetVar.ListMinPlayers2Use)
                                    {
                                        mapListChangePlayerCount.ListMinPlayers2Use = (mapListSetVar.ListMinPlayers2Use - 1);
                                    }
                                    if (mapListChangePlayerCount.ListMaxPlayers2Use >= mapListSetVar.ListMinPlayers2Use)
                                    {
                                        mapListChangePlayerCount.ListMaxPlayers2Use = (mapListSetVar.ListMinPlayers2Use - 1);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Write2PluginConsole(String.Format("Map List Min Players can not be less than 0 or more than Max Players 2 Use"), "SetPluginVars", "Debug", 0);
                        }
                        foreach (MapListGenerator mapListChangePlayerSmaller in this.bf3m_lstMapLists)
                        {
                            MapListGenerator mapListChangePlayerLarger = this.bf3m_lstMapLists[mapListChangePlayerSmaller.ListId - 1];
                            if (mapListChangePlayerLarger.ListId == (iCurrentID + 1) && mapListSetVar.ListMaxPlayers2Use != (mapListSetVar.ListMinPlayers2Use - 1))
                            {
                                mapListChangePlayerLarger.ListMaxPlayers2Use = (mapListChangePlayerSmaller.ListMinPlayers2Use - 1);
                            }
                            if (mapListChangePlayerSmaller.ListId == (iCurrentID - 1) && (iCurrentID - 1) != 0)
                            {
                                mapListChangePlayerLarger.ListMaxPlayers2Use = (mapListChangePlayerSmaller.ListMinPlayers2Use - 1);
                            }
                            if (mapListChangePlayerLarger.ListId > iCurrentID)
                            {
                                if (mapListChangePlayerSmaller.ListMaxPlayers2Use == -1 && mapListChangePlayerLarger.ListMinPlayers2Use >= 0)
                                {
                                    mapListChangePlayerLarger.ListMaxPlayers2Use = (mapListChangePlayerLarger.ListMinPlayers2Use - 1);
                                }
                                if (mapListChangePlayerLarger.ListMinPlayers2Use >= mapListChangePlayerSmaller.ListMinPlayers2Use)
                                {
                                    mapListChangePlayerLarger.ListMinPlayers2Use = (mapListChangePlayerLarger.ListMinPlayers2Use - 1);
                                }
                                if (mapListChangePlayerLarger.ListMaxPlayers2Use >= mapListChangePlayerSmaller.ListMinPlayers2Use)
                                {
                                    mapListChangePlayerLarger.ListMaxPlayers2Use = (mapListChangePlayerLarger.ListMinPlayers2Use - 1);
                                }
                            }
                        }
                    }
                    else if (strVariable.CompareTo(string.Format("{0}: Map List Max Players {1}", mapListSetVar.ListId, mapListSetVar.ListName)) == 0 && int.TryParse(strValue, out iHelperInt) == true)
                    {
                        if (iHelperInt <= this.bf3m_iMaxPlayersServer && iHelperInt >= 0)
                        {
                            mapListSetVar.ListMaxPlayers = iHelperInt;
                        }
                        else if (iHelperInt < 0 || iHelperInt > this.bf3m_iMaxPlayersServer)
                        {
                            Write2PluginConsole(String.Format("MaxPlayers by GameType can not be less than 0 or more than " + this.bf3m_strPlugVarNameNumberMaxPlayerServer + ""), "SetPluginVars", "Debug", 0);
                        }
                    }
                    else
                    {
                        foreach (MapSettingsGenerator mapVarSettings in this.bf3m_lstMapSettings)
                        {
                            if (mapVarSettings.MapListId > 0)
                            {
                                if (strVariable.CompareTo(string.Format("{0}: {1} {2}", mapVarSettings.MapListId, mapVarSettings.MapName, mapVarSettings.MapMode)) == 0)
                                {
                                    List<string> lstValidatelist = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                                    lstValidatelist.RemoveAll(String.IsNullOrEmpty);
                                    this.GetMapSettings(mapVarSettings.MapsSettings, lstValidatelist, iCurrentID, mapVarSettings.MapName, mapVarSettings.MapMode);
                                }
                            }
                            else if (mapVarSettings.MapListId == 0)
                            {
                                if (strVariable.CompareTo(string.Format("Time Based: {0} {1}", mapVarSettings.MapName, mapVarSettings.MapMode)) == 0)
                                {
                                    List<string> lstValidatelist = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                                    lstValidatelist.RemoveAll(String.IsNullOrEmpty);
                                    this.GetMapSettings(mapVarSettings.MapsSettings, lstValidatelist, 0, mapVarSettings.MapName, mapVarSettings.MapMode);
                                }
                            }
                        }
                    }
                }
                else if (this.bf3m_lstGameTypeMaxPlayer.Count >= iCurrentID)
                {
                    MaxPlayersGameType strGameType = this.bf3m_lstGameTypeMaxPlayer[iCurrentID];
                    if (strVariable.CompareTo(String.Format("{0}:", strGameType.TypeName)) == 0 && int.TryParse(strValue, out iHelperInt) == true)
                    {
                        strGameType.MaxPlayers = iHelperInt;
                    }
                    else if (iHelperInt < 0 || iHelperInt > this.bf3m_iMaxPlayersServer)
                    {
                        Write2PluginConsole(String.Format("MaxPlayers by GameType can not be less than 0 or more than " + this.bf3m_strPlugVarNameNumberMaxPlayerServer + ""), "SetPluginVars", "Debug", 0);
                    }
                }
            }

            //Variables
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameOnOffMaxPlayer + "") == 0)
            {
                this.bf3m_strMaxPlayersByListOrMode = strValue;
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameOnOffVars + "") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.bf3m_enMapListVarsDefault = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameOnOffInfantry + "") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.bf3m_enUseInfantryOnly = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                //this.bf3m_blInfantryOnly = false;
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameNumberInfantry + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if (iHelperInt <= this.bf3m_iMaxPlayersServer && iHelperInt >= 0)
                {
                    this.bf3m_iMinPlayersInfantryOnly = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > this.bf3m_iMaxPlayersServer)
                {
                    Write2PluginConsole(String.Format("Infatry Only Off @ Player Count can not be set lower than 0 or more than 64."), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameNumberVehDelay + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if (iHelperInt >= 0)
                {
                    this.bf3m_iDefaultVehDelay = iHelperInt;
                }
                else if (iHelperInt < 0)
                {
                    Write2PluginConsole(String.Format("Vehicle Spawn Delay can not be set lower than 0."), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameStringMode + "") == 0)
            {
                this.bf3m_strDefaultMode = strValue;
            }

            //Var Settings
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameDefaultVars + "") == 0)
            {
                List<string> lstValidatelist = new List<string>(CPluginVariable.DecodeStringArray(strValue));
                lstValidatelist.RemoveAll(String.IsNullOrEmpty);
                this.GetMapSettings(this.bf3m_lstDefaultVarSettings, lstValidatelist, 0, "Default", "None");
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameNumberModifyMapList + "") == 0)
            {
                this.bf3m_strMapListsNames = strValue;
                foreach (MapListGenerator mapListName in this.bf3m_lstMapLists)
                {
                    if (mapListName.ListId.ToString() == this.bf3m_strMapListsNames)
                    {
                        this.bf3m_iMapNamesListValue = mapListName.ListId;
                        break;
                    }
                }
            }

            //RTV
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameOnOffRTV + "") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.bf3m_enOnOffRTV = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameIncludeRTV + "") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.bf3m_enRTVInclude = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameMultiRoundsRTV + "") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.bf3m_enRTVMultiRounds = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameChangeRTV + "") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.bf3m_enRTVChange = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameChangeTimeRTV + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3m_iRTVChangeTime = iHelperInt;
                }
                else if (iHelperInt < 0)
                {
                    Write2PluginConsole(String.Format("Rock the Vote (Change Immediately Time to Wait in Seconds) cannot be less than 0"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameNumMapsRTV + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt >= 2 && iHelperInt <= 5) || SrciptDebugVar == 1)
                {
                    this.bf3m_iRTVNumMaps = iHelperInt;
                }
                else if (iHelperInt < 2 || iHelperInt > 5)
                {
                    Write2PluginConsole(String.Format("" + this.bf3m_strPlugVarNameNumMapsRTV + " cannot be less than 2 or more than 5"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameRoundWaitRTV + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 30 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3m_iRTVRoundWait = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 30)
                {
                    Write2PluginConsole(String.Format("Rock the Vote (Wait Minutes after LevelLoad) cannot be less than 0 or more than 30 minutes"), "SetPluginVars", "Debug", 0);
                }
                this.bf3m_iRTVRoundWaitInSeconds = this.bf3m_iRTVRoundWait * 60;
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameWaitRTV + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 30 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3m_iRTVWait = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 30)
                {
                    Write2PluginConsole(String.Format("Rock the Vote (Wait Minutes between RTV's) cannot be less than 0 or more than 30 minutes"), "SetPluginVars", "Debug", 0);
                }
                this.bf3m_iRTVWaitInSeconds = this.bf3m_iRTVWait * 60;
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameDurationRTV + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 5 && iHelperInt >= 1) || SrciptDebugVar == 1)
                {
                    this.bf3m_iRTVDuration = iHelperInt;
                }
                else if (iHelperInt < 1 || iHelperInt > 5)
                {
                    Write2PluginConsole(String.Format("Rock the Vote (Duration Minutes to collect votes) cannot be less than 1 or more than 5 minutes"), "SetPluginVars", "Debug", 0);
                }
                this.bf3m_iRTVDurationInSeconds = this.bf3m_iRTVDuration * 60;
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNamePercentageRTV + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 100 && iHelperInt > 1) || SrciptDebugVar == 1)
                {
                    this.bf3m_iRTVPercentage = iHelperInt;
                }
                else if (iHelperInt <= 1 || iHelperInt > 100)
                {
                    Write2PluginConsole(String.Format("Rock the Vote (% Players on Server) cannot be less than 1% or more than 100%"), "SetPluginVars", "Debug", 0);
                }
            }

            //Messages
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameDepthDebug + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 3 && iHelperInt >= 0) || SrciptDebugVar == 1 || iHelperInt == 99)
                {
                    this.bf3m_iDebugLevel = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 3)
                {
                    Write2PluginConsole(String.Format("Debug Levels are from 0-3 or 99 all debug info"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameDepthVarDebug + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 3 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3m_iVarDebugLevel = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 3)
                {
                    Write2PluginConsole(String.Format("Var Debug Levels are from 0-3"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameDepthMapDebug + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 3 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3m_iMapDebugLevel = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 3)
                {
                    Write2PluginConsole(String.Format("Map Debug Levels are from 0-3"), "SetPluginVars", "Debug", 0);
                }
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarNameDepthAdminMess + "") == 0 && int.TryParse(strValue, out iHelperInt) == true)
            {
                if ((iHelperInt <= 2 && iHelperInt >= 0) || SrciptDebugVar == 1)
                {
                    this.bf3m_iAdminDebugLevel = iHelperInt;
                }
                else if (iHelperInt < 0 || iHelperInt > 3)
                {
                    Write2PluginConsole(String.Format("Admin Message Levels are from 0-2"), "SetPluginVars", "Debug", 0);
                }
            }

            //InGame Commands
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarClassCommandNextMap + "") == 0)
            {
                this.bf3m_strNextMap = strValue;
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarClassCommandRTVList + "") == 0)
            {
                this.bf3m_strRTVList = strValue;
            }
            else if (strVariable.CompareTo("" + this.bf3m_strPlugVarClassCommandRTV + "") == 0)
            {
                this.bf3m_strRTV = strValue;
            }
        }
        #endregion

        #region DisplayPluginVars
        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            //For Debugging
            /*lstReturn.Add(new CPluginVariable("~:Test|PlugVarNameValue1", typeof(string), this.bf3m_strLstNameRot1));
			lstReturn.Add(new CPluginVariable("~:Test|PlugVarNameValue2", typeof(string), this.bf3m_strLstNameRot2));
			lstReturn.Add(new CPluginVariable("~:Test|PlugVarNameValue3", typeof(string), this.bf3m_strLstNameRot3));
			lstReturn.Add(new CPluginVariable("~:Test|PlugVarNameValue4", typeof(string), this.bf3m_strLstNameRot4));
			lstReturn.Add(new CPluginVariable("~:Test|PlugVarNameValue5", typeof(string), this.bf3m_strLstNameRot5));
			lstReturn.Add(new CPluginVariable("~:Test|PlugVarNameValue6", typeof(string), this.bf3m_strLstNameRot6));
			lstReturn.Add(new CPluginVariable("~:Test|PlugVarNameValue7", typeof(string), this.bf3m_strLstNameRot7));*/
            //

            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassBF3Manager + "" + this.bf3m_strPlugVarNameSelectInternalPlugin + "", "enum.BF3ManagerSelections(Messages|Map Lists|Rock the Vote|Variables|InGame Commands)", this.bf3m_strSelections));

            if (this.bf3m_lstMapLists.Count <= this.bf3m_iMapListCount)
            {
                do
                {
                    Write2PluginConsole(String.Format("1st Run - bf3m_lstMapLists.Count {0} this.bf3m_iMapListCount {1}", bf3m_lstMapLists.Count, this.bf3m_iMapListCount), "GetDisplayVars", "Debug", 100);
                    string strTempName = "";
                    if ((bf3m_lstMapLists.Count) == 0) { strTempName = "Time Based List"; }
                    if ((bf3m_lstMapLists.Count) == 1) { strTempName = this.bf3m_strLstNameRot1; }
                    if ((bf3m_lstMapLists.Count) == 2) { strTempName = this.bf3m_strLstNameRot2; }
                    if ((bf3m_lstMapLists.Count) == 3) { strTempName = this.bf3m_strLstNameRot3; }
                    if ((bf3m_lstMapLists.Count) == 4) { strTempName = this.bf3m_strLstNameRot4; }
                    if ((bf3m_lstMapLists.Count) == 5) { strTempName = this.bf3m_strLstNameRot5; }
                    if ((bf3m_lstMapLists.Count) == 6) { strTempName = this.bf3m_strLstNameRot6; }
                    if ((bf3m_lstMapLists.Count) == 7) { strTempName = this.bf3m_strLstNameRot7; }

                    this.bf3m_lstMapLists.Add(new MapListGenerator(bf3m_lstMapLists.Count, strTempName));
                    Write2PluginConsole(String.Format("2nd Run - bf3m_lstMapLists.Count {0} this.bf3m_iMapListCount {1}", bf3m_lstMapLists.Count, this.bf3m_iMapListCount), "GetDisplayVars", "Debug", 100);
                }
                while (this.bf3m_lstMapLists.Count < this.bf3m_iMapListCount);
            }

            if (this.bf3m_iMaxPlayersServer == 0 && this.bf3m_strSelections == "Variables")
            {
                lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarSettings + "" + this.bf3m_strPlugVarNameNumberMaxPlayerServer + "", typeof(int), this.bf3m_iMaxPlayersServer));
            }
            else if (this.bf3m_iMaxPlayersServer != 0)
            {
                if (this.bf3m_strSelections == "Map Lists")
                {
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMapListGeneral + "" + this.bf3m_strPlugVarNameNumberMapLists + "", typeof(int), this.bf3m_iMapListCount));
                    //lstReturn.Add(new CPluginVariable(""+this.bf3m_strPlugVarClassMapListGeneral+""+this.bf3m_strPlugVarNameNumberMapListsChangeTime+"", typeof(int), this.bf3m_iMapListTimeVar));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMapListGeneral + "" + this.bf3m_strPlugVarNameOnOffRotateEmpty + "", typeof(enumBoolYesNo), this.bf3m_enEmptyRotate));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMapListGeneral + "" + this.bf3m_strPlugVarNameTimeEmpty + "", typeof(int), this.bf3m_iEmptyRotateTime));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMapListGeneral + "" + this.bf3m_strPlugVarNameOnOffRotateRandom + "", typeof(enumBoolYesNo), this.bf3m_enRandomMapIndex));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMapListGeneral + "" + this.bf3m_strPlugVarNameOnOffTimeBased + "", typeof(enumBoolYesNo), this.bf3m_enUseTimeBasedList));
                    if (this.bf3m_enUseTimeBasedList == enumBoolYesNo.Yes)
                    {
                        MapListGenerator timeBasedList = this.bf3m_lstMapLists[0];
                        lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMapListTime + "" + this.bf3m_strPlugVarNameTimeMapListStart + "", typeof(string), this.bf3m_strTimeMapListStart));
                        lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMapListTime + "" + this.bf3m_strPlugVarNameTimeMapListStop + "", typeof(string), this.bf3m_strTimeMapListStop));
                        lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMapListTime + "" + this.bf3m_strPlugVarNameTimeMapListList + "", typeof(string[]), this.StringifyMapList(timeBasedList.ListMapsList)));
                    }
                    foreach (MapListGenerator mapListGetDisVar in this.bf3m_lstMapLists)
                    {
                        if (mapListGetDisVar.ListId > 0 && mapListGetDisVar.ListId <= this.bf3m_iMapListCount)
                        {
                            lstReturn.Add(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassMapListSettings + "{0}|{0}: Map List Name {1}", mapListGetDisVar.ListId, mapListGetDisVar.ListName), typeof(string), mapListGetDisVar.ListName));
                            lstReturn.Add(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassMapListSettings + "{0}|{0}: Map List Min Players 2 Use {1}", mapListGetDisVar.ListId, mapListGetDisVar.ListName), typeof(int), mapListGetDisVar.ListMinPlayers2Use));
                            lstReturn.Add(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassMapListSettings + "{0}|{0}: Map List Max Players 2 Use {1}", mapListGetDisVar.ListId, mapListGetDisVar.ListName), typeof(int), mapListGetDisVar.ListMaxPlayers2Use));
                            lstReturn.Add(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassMapListSettings + "{0}|{0}: Map List {1}", mapListGetDisVar.ListId, mapListGetDisVar.ListName), typeof(string[]), this.StringifyMapList(mapListGetDisVar.ListMapsList)));
                        }
                    }
                }
                else if (this.bf3m_strSelections == "Variables")
                {
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarSettings + "" + this.bf3m_strPlugVarNameNumberMaxPlayerServer + "", typeof(int), this.bf3m_iMaxPlayersServer));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarSettings + "" + this.bf3m_strPlugVarNameOnOffMaxPlayer + "", "enum.BF3MapListManagerMaxPlayers(Off|Map List|GameType|Max Players allowed on Server)", this.bf3m_strMaxPlayersByListOrMode));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarSettings + "" + this.bf3m_strPlugVarNameOnOffVars + "", typeof(enumBoolYesNo), this.bf3m_enMapListVarsDefault));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarSettings + "" + this.bf3m_strPlugVarNameOnOffInfantry + "", typeof(enumBoolYesNo), this.bf3m_enUseInfantryOnly));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarSettings + "" + this.bf3m_strPlugVarNameNumberInfantry + "", typeof(int), this.bf3m_iMinPlayersInfantryOnly));
                    if (this.bf3m_strDefaultMode == "Retain Hardcore")
                    {
                        lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarSettings + "" + this.bf3m_strPlugVarNameNumberVehDelay + "", typeof(int), this.bf3m_iDefaultVehDelay));
                    }
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarSettings + "" + this.bf3m_strPlugVarNameStringMode + "", "enum.BF3MapListManagerDefaultMode(Normal/Infantry Only|Retain Hardcore|Allow Custom)", this.bf3m_strDefaultMode));
                    if (this.bf3m_strMaxPlayersByListOrMode != "Off")
                    {
                        if (this.bf3m_strMaxPlayersByListOrMode == "Map List")
                        {
                            foreach (MapListGenerator mapListGetDisVar in bf3m_lstMapLists)
                            {
                                if (mapListGetDisVar.ListId > 0)
                                {
                                    lstReturn.Add(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassVarMaxPlayer + "{0}: Map List Max Players {1}", mapListGetDisVar.ListId, mapListGetDisVar.ListName), typeof(int), mapListGetDisVar.ListMaxPlayers));
                                }
                                else if (mapListGetDisVar.ListId == 0 && this.bf3m_enUseTimeBasedList == enumBoolYesNo.Yes)
                                {
                                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarMaxPlayer + "Time Based: Map List Max Players", typeof(int), mapListGetDisVar.ListMaxPlayers));
                                }
                            }
                        }
                        else if (this.bf3m_strMaxPlayersByListOrMode == "GameType")
                        {
                            if (this.bf3m_lstGameTypeMaxPlayer.Count == 0)
                            {
                                this.bf3m_lstGameTypeMaxPlayer.Add(new MaxPlayersGameType(1, 8, "SquadRush0", "Squad Rush"));
                                this.bf3m_lstGameTypeMaxPlayer.Add(new MaxPlayersGameType(2, 16, "SquadDeathMatch0", "SQDM"));
                                this.bf3m_lstGameTypeMaxPlayer.Add(new MaxPlayersGameType(3, 24, "TeamDeathMatch0", "TDM"));
                                this.bf3m_lstGameTypeMaxPlayer.Add(new MaxPlayersGameType(4, 32, "RushLarge0", "Rush"));
                                this.bf3m_lstGameTypeMaxPlayer.Add(new MaxPlayersGameType(5, 32, "ConquestSmall0", "Conquest"));
                                this.bf3m_lstGameTypeMaxPlayer.Add(new MaxPlayersGameType(6, 32, "ConquestSmall1", "ConquestAssault"));
                                this.bf3m_lstGameTypeMaxPlayer.Add(new MaxPlayersGameType(7, 32, "ConquestLarge0", "Conquest 64"));
                            }
                            foreach (MaxPlayersGameType strGameType in this.bf3m_lstGameTypeMaxPlayer)
                            {
                                lstReturn.Add(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassVarMaxPlayer + "{0}:", strGameType.TypeName), typeof(int), strGameType.MaxPlayers));
                            }
                        }
                    }
                    if (this.bf3m_enMapListVarsDefault == enumBoolYesNo.Yes)
                    {
                        lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarMaps + "" + this.bf3m_strPlugVarNameDefaultVars + "", typeof(string[]), this.StringifyMapVars(this.bf3m_lstDefaultVarSettings)));
                        lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassVarMaps + "" + this.bf3m_strPlugVarNameNumberModifyMapList + "", "enum.BF3MapListManagerMapListNames(None|1|2|3|4|5|6|7|Time Based)", this.bf3m_strMapListsNames));
                        int iHelperInt = 999;
                        if (this.bf3m_strMapListsNames != "None" && (int.TryParse(this.bf3m_strMapListsNames, out iHelperInt) == true || this.bf3m_strMapListsNames == "Time Based"))
                        {
                            foreach (MapSettingsGenerator mapVarSettings in bf3m_lstMapSettings)
                            {
                                if (mapVarSettings.MapListId > 0)
                                {
                                    if (mapVarSettings.MapListId == this.bf3m_iMapNamesListValue && mapVarSettings.MapListId == iHelperInt)
                                    {
                                        lstReturn.Add(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassVarMaps + "{0}: {1} {2}", mapVarSettings.MapListId, mapVarSettings.MapName, mapVarSettings.MapMode), typeof(string[]), this.StringifyMapVars(mapVarSettings.MapsSettings)));
                                    }
                                    else
                                    {
                                        lstReturn.Remove(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassVarMaps + "{0}: {1} {2}", mapVarSettings.MapListId, mapVarSettings.MapName, mapVarSettings.MapMode), typeof(string[]), this.StringifyMapVars(mapVarSettings.MapsSettings)));
                                    }
                                }
                                else if (mapVarSettings.MapListId == 0)
                                {
                                    if (this.bf3m_enUseTimeBasedList == enumBoolYesNo.Yes && this.bf3m_strMapListsNames == "Time Based")
                                    {
                                        lstReturn.Add(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassVarMaps + "Time Based: {0} {1}", mapVarSettings.MapName, mapVarSettings.MapMode), typeof(string[]), this.StringifyMapVars(mapVarSettings.MapsSettings)));
                                    }
                                    else
                                    {
                                        lstReturn.Remove(new CPluginVariable(String.Format("" + this.bf3m_strPlugVarClassVarMaps + "Time Based: {0} {1}", mapVarSettings.MapName, mapVarSettings.MapMode), typeof(string[]), this.StringifyMapVars(mapVarSettings.MapsSettings)));
                                    }
                                }
                            }
                        }
                    }
                }
                else if (this.bf3m_strSelections == "Rock the Vote")
                {
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassRTV + "" + this.bf3m_strPlugVarNameOnOffRTV + "", typeof(enumBoolOnOff), this.bf3m_enOnOffRTV));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassRTV + "" + this.bf3m_strPlugVarNameIncludeRTV + "", typeof(enumBoolYesNo), this.bf3m_enRTVInclude));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassRTV + "" + this.bf3m_strPlugVarNameMultiRoundsRTV + "", typeof(enumBoolYesNo), this.bf3m_enRTVMultiRounds));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassRTV + "" + this.bf3m_strPlugVarNameChangeRTV + "", typeof(enumBoolYesNo), this.bf3m_enRTVChange));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassRTV + "" + this.bf3m_strPlugVarNameChangeTimeRTV + "", typeof(int), this.bf3m_iRTVChangeTime));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassRTV + "" + this.bf3m_strPlugVarNameNumMapsRTV + "", typeof(int), this.bf3m_iRTVNumMaps));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassRTV + "" + this.bf3m_strPlugVarNameRoundWaitRTV + "", typeof(int), this.bf3m_iRTVRoundWait));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassRTV + "" + this.bf3m_strPlugVarNameDurationRTV + "", typeof(int), this.bf3m_iRTVDuration));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassRTV + "" + this.bf3m_strPlugVarNamePercentageRTV + "", typeof(int), this.bf3m_iRTVPercentage));
                }
                else if (this.bf3m_strSelections == "Messages")
                {
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMessagesPlug + "" + this.bf3m_strPlugVarNameDepthDebug + "", typeof(int), this.bf3m_iDebugLevel));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMessagesPlug + "" + this.bf3m_strPlugVarNameDepthVarDebug + "", typeof(int), this.bf3m_iVarDebugLevel));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMessagesPlug + "" + this.bf3m_strPlugVarNameDepthMapDebug + "", typeof(int), this.bf3m_iMapDebugLevel));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassMessages + "" + this.bf3m_strPlugVarNameDepthAdminMess + "", typeof(int), this.bf3m_iAdminDebugLevel));
                }
                else if (this.bf3m_strSelections == "InGame Commands")
                {
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassCommands + "" + this.bf3m_strPlugVarClassCommandNextMap + "", typeof(string), this.bf3m_strNextMap));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassCommands + "" + this.bf3m_strPlugVarClassCommandRTVList + "", typeof(string), this.bf3m_strRTVList));
                    lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassCommands + "" + this.bf3m_strPlugVarClassCommandRTV + "", typeof(string), this.bf3m_strRTV));
                }
            }

            return lstReturn;
        }

        // Lists all of the plugins saved variables.
        public List<CPluginVariable> GetPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("PlugVarNameValue1", typeof(string), this.bf3m_strLstNameRot1));
            lstReturn.Add(new CPluginVariable("PlugVarNameValue2", typeof(string), this.bf3m_strLstNameRot2));
            lstReturn.Add(new CPluginVariable("PlugVarNameValue3", typeof(string), this.bf3m_strLstNameRot3));
            lstReturn.Add(new CPluginVariable("PlugVarNameValue4", typeof(string), this.bf3m_strLstNameRot4));
            lstReturn.Add(new CPluginVariable("PlugVarNameValue5", typeof(string), this.bf3m_strLstNameRot5));
            lstReturn.Add(new CPluginVariable("PlugVarNameValue6", typeof(string), this.bf3m_strLstNameRot6));
            lstReturn.Add(new CPluginVariable("PlugVarNameValue7", typeof(string), this.bf3m_strLstNameRot7));

            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameSelectInternalPlugin + "", "enum.BF3ManagerSelections(None|Map Lists|Rock the Vote|Messages|Variables)", this.bf3m_strSelections));

            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameNumberMapLists + "", typeof(int), this.bf3m_iMapListCount));
            //lstReturn.Add(new CPluginVariable(""+this.bf3m_strPlugVarNameNumberMapListsChangeTime+"", typeof(int), this.bf3m_iMapListTimeVar));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameOnOffRotateEmpty + "", typeof(enumBoolYesNo), this.bf3m_enEmptyRotate));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameTimeEmpty + "", typeof(int), this.bf3m_iEmptyRotateTime));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameOnOffRotateRandom + "", typeof(enumBoolYesNo), this.bf3m_enRandomMapIndex));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameOnOffTimeBased + "", typeof(enumBoolYesNo), this.bf3m_enUseTimeBasedList));
            MapListGenerator timeBasedList = this.bf3m_lstMapLists[0];
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameTimeMapListStart + "", typeof(string), this.bf3m_strTimeMapListStart));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameTimeMapListStop + "", typeof(string), this.bf3m_strTimeMapListStop));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameTimeMapListList + "", typeof(string[]), this.StringifyMapList(timeBasedList.ListMapsList)));
            foreach (MapListGenerator mapListGetVar in this.bf3m_lstMapLists)
            {
                if (mapListGetVar.ListId > 0)
                {
                    lstReturn.Add(new CPluginVariable(String.Format("{0}: Map List Name {1}", mapListGetVar.ListId, mapListGetVar.ListName), typeof(string), mapListGetVar.ListName));
                    lstReturn.Add(new CPluginVariable(String.Format("{0}: Map List Min Players 2 Use {1}", mapListGetVar.ListId, mapListGetVar.ListName), typeof(int), mapListGetVar.ListMinPlayers2Use));
                    lstReturn.Add(new CPluginVariable(String.Format("{0}: Map List Max Players 2 Use {1}", mapListGetVar.ListId, mapListGetVar.ListName), typeof(int), mapListGetVar.ListMaxPlayers2Use));
                    lstReturn.Add(new CPluginVariable(String.Format("{0}: Map List {1}", mapListGetVar.ListId, mapListGetVar.ListName), typeof(string[]), this.StringifyMapList(mapListGetVar.ListMapsList)));
                }
            }
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameNumberMaxPlayerServer + "", typeof(int), this.bf3m_iMaxPlayersServer));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameOnOffMaxPlayer + "", "enum.BF3MapListManagerMaxPlayers(Off|Map List|GameType|Max Players allowed on Server)", this.bf3m_strMaxPlayersByListOrMode));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameOnOffVars + "", typeof(enumBoolYesNo), this.bf3m_enMapListVarsDefault));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameOnOffInfantry + "", typeof(enumBoolYesNo), this.bf3m_enUseInfantryOnly));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameNumberInfantry + "", typeof(int), this.bf3m_iMinPlayersInfantryOnly));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameNumberVehDelay + "", typeof(int), this.bf3m_iDefaultVehDelay));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameStringMode + "", "enum.BF3MapListManagerDefaultMode(Normal/Infantry Only|Retain Hardcore|Allow Custom)", this.bf3m_strDefaultMode));
            foreach (MapListGenerator mapListGetVar in bf3m_lstMapLists)
            {
                if (mapListGetVar.ListId > 0)
                {
                    lstReturn.Add(new CPluginVariable(String.Format("{0}: Map List Max Players {1}", mapListGetVar.ListId, mapListGetVar.ListName), typeof(int), mapListGetVar.ListMaxPlayers));
                }
                else if (mapListGetVar.ListId == 0)
                {
                    lstReturn.Add(new CPluginVariable("Time Based: Map List Max Players", typeof(int), mapListGetVar.ListMaxPlayers));
                }
            }
            foreach (MaxPlayersGameType strGameType in this.bf3m_lstGameTypeMaxPlayer)
            {
                lstReturn.Add(new CPluginVariable(String.Format("{0}:", strGameType.TypeName), typeof(int), strGameType.MaxPlayers));
            }
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameDefaultVars + "", typeof(string[]), this.StringifyMapVars(this.bf3m_lstDefaultVarSettings)));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameNumberModifyMapList + "", "enum.BF3MapListManagerMapListNames(None|1|2|3|4|5|6|7|Time Based)", this.bf3m_strMapListsNames));
            foreach (MapSettingsGenerator mapVarSettings in this.bf3m_lstMapSettings)
            {
                if (mapVarSettings.MapListId > 0)
                {
                    lstReturn.Add(new CPluginVariable(String.Format("{0}: {1} {2}", mapVarSettings.MapListId, mapVarSettings.MapName, mapVarSettings.MapMode), typeof(string[]), this.StringifyMapVars(mapVarSettings.MapsSettings)));
                }
                else if (mapVarSettings.MapListId == 0)
                {
                    lstReturn.Add(new CPluginVariable(String.Format("Time Based: {0} {1}", mapVarSettings.MapName, mapVarSettings.MapMode), typeof(string[]), this.StringifyMapVars(mapVarSettings.MapsSettings)));
                }
            }

            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameOnOffRTV + "", typeof(enumBoolOnOff), this.bf3m_enOnOffRTV));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameIncludeRTV + "", typeof(enumBoolYesNo), this.bf3m_enRTVInclude));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameMultiRoundsRTV + "", typeof(enumBoolYesNo), this.bf3m_enRTVMultiRounds));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameChangeRTV + "", typeof(enumBoolYesNo), this.bf3m_enRTVChange));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameChangeTimeRTV + "", typeof(int), this.bf3m_iRTVChangeTime));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameNumMapsRTV + "", typeof(int), this.bf3m_iRTVNumMaps));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameRoundWaitRTV + "", typeof(int), this.bf3m_iRTVRoundWait));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameDurationRTV + "", typeof(int), this.bf3m_iRTVDuration));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNamePercentageRTV + "", typeof(int), this.bf3m_iRTVPercentage));

            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameDepthDebug + "", typeof(int), this.bf3m_iDebugLevel));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameDepthVarDebug + "", typeof(int), this.bf3m_iVarDebugLevel));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameDepthMapDebug + "", typeof(int), this.bf3m_iMapDebugLevel));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarNameDepthAdminMess + "", typeof(int), this.bf3m_iAdminDebugLevel));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassCommandNextMap + "", typeof(string), this.bf3m_strNextMap));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassCommandRTVList + "", typeof(string), this.bf3m_strRTVList));
            lstReturn.Add(new CPluginVariable("" + this.bf3m_strPlugVarClassCommandRTV + "", typeof(string), this.bf3m_strRTV));

            return lstReturn;
        }
        #endregion

        public void UnregisterAllCommands()
        {
            List<string> emptyList = new List<string>();

            this.UnregisterCommand(new MatchCommand(emptyList, this.bf3m_strNextMap, this.Listify<MatchArgumentFormat>()));
            this.UnregisterCommand(new MatchCommand(emptyList, this.bf3m_strRTVList, this.Listify<MatchArgumentFormat>()));
            this.UnregisterCommand(new MatchCommand(emptyList, "stoprtv", this.Listify<MatchArgumentFormat>()));
            this.UnregisterCommand(new MatchCommand(emptyList, this.bf3m_strRTV, this.Listify<MatchArgumentFormat>()));
            this.UnregisterCommand(new MatchCommand(emptyList, "rockthevote", this.Listify<MatchArgumentFormat>()));
            this.UnregisterCommand(new MatchCommand(emptyList, "vote", this.Listify<MatchArgumentFormat>()));
        }

        public void RegisterAllCommands()
        {
            this.RegisterCommand(new MatchCommand("BF3Manager", "OnCommandNextmap", this.Listify<string>("@", "!", "#", "/"), this.bf3m_strNextMap, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Display the NextMap in rotation."));
            this.RegisterCommand(new MatchCommand("BF3Manager", "OnCommandRTVList", this.Listify<string>("@", "!", "#", "/"), this.bf3m_strRTVList, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Display a list of available maps to vote for."));
            this.RegisterCommand(new MatchCommand("BF3Manager", "OnCommandRTVStop", this.Listify<string>("@", "!", "#", "/"), "stoprtv", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanUseMapFunctions, ""), "Cancel Rock the Vote."));
            //this.RegisterCommand(new MatchCommand("BF3Manager", "OnCommandRTVStop", this.Listify<string>("@", "!", "#", "/"), "stoprtv", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Cancel Rock the Vote."));
            this.RegisterCommand(new MatchCommand("BF3Manager", "OnCommandRockTheVote", this.Listify<string>("@", "!", "#", "/"), this.bf3m_strRTV, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Initiate Rock the Vote."));
            this.RegisterCommand(new MatchCommand("BF3Manager", "OnCommandRockTheVote", this.Listify<string>("@", "!", "#", "/"), "rockthevote", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Initiate Rock the Vote."));
            this.RegisterCommand(new MatchCommand("BF3Manager", "OnCommandVote", this.Listify<string>("@", "!", "#", "/"), "vote", this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("vote value", MatchArgumentFormatTypes.Regex, this.Listify<string>("[0-9]"))), new ExecutionRequirements(ExecutionScope.All), "Select Index 3 RTVMaps List."));
        }

        public void OnMaxPlayers(int limit)
        {
            this.bf3m_iCurrentMaxPlayers = limit;
        }

        #region OnLevelLoaded
        public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)
        {
            this.bf3m_strMap = mapFileName;
            this.bf3m_strGameMode = Gamemode;
            this.bf3m_iLvlLRoundsPlayed = (roundsPlayed + 1);
            this.bf3m_iLvlLRoundsTotal = roundsTotal;
            this.bf3m_iEmptyRotateStoredTime = 0;
            this.bf3m_blSetRandomMapIndex = true;
            this.bf3m_iRTVStoredTime = 0;
            this.bf3m_iRoundTime = 0;
            this.bf3m_blRTVFired = false;

            if (this.bf3m_iLvlLRoundsPlayed < this.bf3m_iLvlLRoundsTotal)
            {
                this.bf3m_blNextMapIsCurrentMap = true;
            }
            else if (this.bf3m_iLvlLRoundsPlayed >= this.bf3m_iLvlLRoundsTotal)
            {
                this.bf3m_blNextMapIsCurrentMap = false;
            }

            Write2PluginConsole(String.Format("Level Loaded = {0} {1} Rounds Played = {2} Rounds Total = {3}", this.bf3m_strMap, this.bf3m_strGameMode, this.bf3m_iLvlLRoundsPlayed, this.bf3m_iLvlLRoundsTotal), "OnLevelLoaded", "Debug", 1);

            foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
            {
                if (String.Compare(this.bf3m_strMap, cMapDefine.FileName, true) == 0 && String.Compare(this.bf3m_strGameMode, cMapDefine.PlayList, true) == 0)
                {
                    this.bf3m_strCurrentMapFriendlyName = cMapDefine.PublicLevelName;
                    this.bf3m_strCurrentMapFriendlyMode = cMapDefine.GameMode;
                    break;
                }
            }
            this.SetInfantryOnly();
            this.ChangeServerSize();

            this.ExecuteCommand("procon.protected.send", "mapList.list");

            this.bf3m_blLevelIsLoaded = true;
            Thread sleepLvlLoad = new Thread(new ThreadStart(delegate ()
            {
                Thread.Sleep(30000);
                this.ExecuteCommand("procon.protected.send", "maplist.getMapIndices");
            }));
            sleepLvlLoad.Start();
        }
        #endregion

        #region OnMaplistList
        public override void OnMaplistList(List<MaplistEntry> lstMaplist)
        {
            if (lstMaplist.Count == this.bf3m_lstMapListsCheck.Count)
            {
                Write2PluginConsole(String.Format("MapLists Same Length"), "OnMaplistList", "Debug", 100);
                bool blAreEqual = true;
                int iListCount = 0;
                foreach (MaplistEntry mapListList in lstMaplist)
                {
                    MapNamesModesRoundsIndex mapEntry = this.bf3m_lstMapListsCheck[iListCount];
                    Write2PluginConsole(String.Format("Checking Maps {0} {1} {2} - {3} {4} {5}", mapListList.MapFileName, mapListList.Gamemode, mapListList.Rounds, mapEntry.MapFileName, mapEntry.GameMode, mapEntry.Rounds), "OnMaplistList", "Debug", 100);
                    if (String.Compare(mapListList.MapFileName, mapEntry.MapFileName, true) == 0 && String.Compare(mapListList.Gamemode, mapEntry.GameMode, true) == 0 && mapListList.Rounds == mapEntry.Rounds)
                    {
                        blAreEqual = true;
                        iListCount = iListCount + 1;
                    }
                    else
                    {
                        blAreEqual = false;
                        break;
                    }
                }
                if (!blAreEqual)
                {
                    this.bf3m_iCurrentMapList = 99;
                }
            }
            else
            {
                Write2PluginConsole(String.Format("MapLists Different Length"), "OnMaplistList", "Debug", 100);
                this.bf3m_iCurrentMapList = 99;
            }
        }
        #endregion

        #region OnMaplistGetMapIndices
        public void OnMaplistGetMapIndices(int mapIndex, int nextIndex)
        {
            this.bf3m_blTimeBasedChangeStart = ParseDateTime.TimeBased(this.bf3m_dtDateTimeNow, this.bf3m_strTimeMapListStart);
            this.bf3m_blTimeBasedChangeStop = ParseDateTime.TimeBased(this.bf3m_dtDateTimeNow, this.bf3m_strTimeMapListStop);
            Write2PluginConsole(String.Format("ParseDateTime Start = {0}", this.bf3m_blTimeBasedChangeStart), "OnMaplistGetMapIndices", "Debug", 100);
            Write2PluginConsole(String.Format("ParseDateTime Stop = {0}", this.bf3m_blTimeBasedChangeStop), "OnMaplistGetMapIndices", "Debug", 100);
            if (this.bf3m_blSetRandomMapIndex)
            {
                this.SetNextMap(mapIndex, nextIndex);
            }
        }
        #endregion

        #region OnRoundOver
        public override void OnRoundOver(int iWinningTeamID)
        {
            Write2PluginConsole(String.Format("Winning Team Id = {0}", iWinningTeamID), "OnRoundOver", "Debug", 1);
            this.bf3m_iWinningTeam = iWinningTeamID;
            this.bf3m_blLevelIsLoaded = false;

            this.SetInfantryOnly();
            //this.ChangeServerSize();
            this.ExecuteCommand("procon.protected.send", "vars.maxPlayers", this.bf3m_iMaxPlayersServer.ToString());
            this.ChangeMapVars();
            this.bf3m_blRTVFired = false;
            this.bf3m_iRTVCount = 0;
            this.bf3m_iRTVStoredTime = 999999;
            this.bf3m_lstRTVPlayerInfo.Clear();
            this.bf3m_blRTVStarted = false;
        }
        #endregion

        #region ServerInfo
        public override void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.bf3m_strServerName = csiServerInfo.ServerName;
            this.bf3m_iPlayerCount = csiServerInfo.PlayerCount;
            this.bf3m_iMaxPlayerCount = csiServerInfo.MaxPlayerCount;
            this.bf3m_strGameMode = csiServerInfo.GameMode;
            this.bf3m_strMap = csiServerInfo.Map;
            this.bf3m_iCurrentRound = csiServerInfo.CurrentRound;
            this.bf3m_iTotalRounds = csiServerInfo.TotalRounds;
            if (!this.bf3m_blLevelIsLoaded)
            {
                this.bf3m_iRoundTime = csiServerInfo.RoundTime;
            }
            this.bf3m_lstTeamScore = csiServerInfo.TeamScores;
            this.bf3m_iWinningTeam = 2;

            Write2PluginConsole(String.Format("^2ServerName = {0}' ^1'Player Count = {1}' ^2'Max Players = {2}' ^1'Mode = {3}' ^2'Map = {4}' ^1'Current Round = {5}' ^2'Total Rounds = {6}' ^1'Round Time = {7}'", bf3m_strServerName, bf3m_iPlayerCount, bf3m_iMaxPlayerCount, bf3m_strGameMode, bf3m_strMap, bf3m_iCurrentRound, bf3m_iTotalRounds, bf3m_iRoundTime), "OnServerInfo", "Debug", 99);
        }
        #endregion

        #region BF3MMainLoop
        public void BF3MMainLoop()
        {
            //Write2PluginConsole(String.Format("IsConnected = {0}", FrostbiteConnection.IsConnected.get), "BF3MMainLoop", "Debug", 99);
            if (this.bf3m_blLevelIsLoaded)
            {
                this.bf3m_dtDateTimeNow = DateTime.Now;
                this.bf3m_iRoundTime = this.bf3m_iRoundTime + 1;
                this.bf3m_iMapListChangeTime = this.bf3m_iMapListChangeTime + 1;

                #region ChangeMapList
                int iMapListNext = -1;
                bool iContinueMapListChange = false;
                if (this.bf3m_blTimeBasedChangeStart && this.bf3m_enUseTimeBasedList == enumBoolYesNo.Yes && !this.bf3m_blTimeBasedLoaded)
                {
                    iMapListNext = 0;
                    iContinueMapListChange = true;
                    this.bf3m_blTimeBasedChangeStart = false;
                    this.bf3m_blTimeBasedLoaded = true;
                }
                else if ((this.bf3m_blTimeBasedChangeStop && this.bf3m_enUseTimeBasedList == enumBoolYesNo.Yes && this.bf3m_blTimeBasedLoaded) || this.bf3m_enUseTimeBasedList == enumBoolYesNo.No || !this.bf3m_blTimeBasedLoaded)
                {
                    this.bf3m_blTimeBasedChangeStop = false;
                    this.bf3m_blTimeBasedLoaded = false;
                    foreach (MapListGenerator mapListSelect in this.bf3m_lstMapLists)
                    {
                        if (mapListSelect.ListId <= this.bf3m_iMapListCount && this.bf3m_iCurrentMapList != mapListSelect.ListId && this.bf3m_iPlayerCount >= mapListSelect.ListMinPlayers2Use && this.bf3m_iPlayerCount <= mapListSelect.ListMaxPlayers2Use && !this.bf3m_blRTVStarted)
                        {
                            if ((this.bf3m_iMapListChangeTime >= this.bf3m_iMapListTimeVarSeconds && mapListSelect.ListId > this.bf3m_iCurrentMapList) || mapListSelect.ListId < this.bf3m_iCurrentMapList)
                            {
                                iMapListNext = mapListSelect.ListId;
                                iContinueMapListChange = true;
                            }
                            break;
                        }
                    }
                }
                if (iContinueMapListChange)
                {
                    MapListGenerator mapListChange = this.bf3m_lstMapLists[iMapListNext];
                    if (mapListChange.ListMapsList.Count == 0)
                    {
                        Write2PluginConsole(String.Format("Map List is empty. Halting Map File Changes"), "ChangeMapList", "Map", 1);
                        Write2PluginConsole(String.Format("Map List to check {0}", mapListChange.ListName), "ChangeMapList", "Map", 3);
                    }
                    else
                    {
                        this.bf3m_iCurrentMapList = mapListChange.ListId;
                        this.bf3m_lstMapListsCheck = mapListChange.ListMapsList;
                        Write2PluginConsole(String.Format("Map List to check {0}", mapListChange.ListName), "ChangeMapList", "Map", 3);
                        this.ExecuteCommand("procon.protected.send", "mapList.clear");
                        for (int i = 0; i < mapListChange.ListMapsList.Count; i++)
                        {
                            CMap cmMap2Add = this.GetMap(mapListChange.ListMapsList[i].MapFileName, mapListChange.ListMapsList[i].GameMode);
                            this.ExecuteCommand("procon.protected.send", "mapList.add", cmMap2Add.FileName, cmMap2Add.PlayList, mapListChange.ListMapsList[i].Rounds.ToString());
                            Write2PluginConsole(String.Format("Adding {0} {1} to Server Map File with {2} Rounds", cmMap2Add.FileName, cmMap2Add.PlayList, this.bf3m_lstMapListsCheck[i].Rounds), "ChangeMapList", "Map", 1);
                        }
                        this.ExecuteCommand("procon.protected.send", "mapList.save");
                        WriteAdminSay(String.Format("*****Map Lists*****"), "all", "", 2);
                        WriteAdminSay(String.Format("Server switching to {0} Map List on next map", mapListChange.ListName), "all", "", 1);
                        this.bf3m_blSetRandomMapIndex = true;
                        this.ExecuteCommand("procon.protected.send", "maplist.getMapIndices");
                        this.ChangeServerSize();
                        this.bf3m_iMapListChangeTime = 0;
                    }
                }
                #endregion

                this.SetInfantryOnly();

                #region RotateEmpty	
                if (this.bf3m_enEmptyRotate == enumBoolYesNo.Yes)
                {
                    if (this.bf3m_iPlayerCount == 0 && this.bf3m_blContinueEmptyCheck)
                    {
                        this.bf3m_iEmptyRotateStoredTime = this.bf3m_iRoundTime;
                        this.bf3m_blContinueEmptyCheck = false;
                    }
                    else if (this.bf3m_iPlayerCount > 0 && !this.bf3m_blContinueEmptyCheck)
                    {
                        this.bf3m_blContinueEmptyCheck = true;
                    }
                    if (this.bf3m_iPlayerCount == 0 && ((this.bf3m_iEmptyRotateStoredTime + this.bf3m_iEmptyRotateTimeInSeconds) < this.bf3m_iRoundTime))
                    {
                        Write2PluginConsole(String.Format("StoredTime + RotateTime ={0} RoundTime ={1}", (this.bf3m_iEmptyRotateStoredTime + this.bf3m_iEmptyRotateTimeInSeconds), this.bf3m_iRoundTime), "RotateEmpty", "Debug", 3);
                        if (this.bf3m_strGameMode == "SquadRush0" || this.bf3m_strGameMode == "RushLarge0")
                        {
                            this.bf3m_blLevelIsLoaded = false;
                            this.SetInfantryOnly();
                            this.ChangeServerSize();
                            this.ChangeMapVars();
                            this.ExecuteCommand("procon.protected.send", "maplist.runNextRound");
                            Write2PluginConsole(String.Format("Running Next Level"), "RotateEmpty", "Debug", 3);
                        }
                        else
                        {
                            if (this.bf3m_lstTeamScore.Count != 0)
                            {
                                for (int i = 0; i < this.bf3m_lstTeamScore.Count; i++)
                                {
                                    Write2PluginConsole(String.Format("Ticket Count: Team {0}: {1}/{2}", this.bf3m_lstTeamScore[i].TeamID.ToString(), this.bf3m_lstTeamScore[i].Score, this.bf3m_lstTeamScore[i].WinningScore.ToString()), "OnServerInfo", "Debug", 2);
                                }

                                if (this.bf3m_strGameMode == "ConquestSmall0" || this.bf3m_strGameMode == "ConquestSmall1" || this.bf3m_strGameMode == "ConquestLarge0" || this.bf3m_strGameMode == "TeamDeathMatch0")
                                {
                                    if (this.bf3m_lstTeamScore[0].Score > this.bf3m_lstTeamScore[1].Score)
                                    {
                                        this.bf3m_iWinningTeam = 1;
                                    }
                                    else if (this.bf3m_lstTeamScore[1].Score > this.bf3m_lstTeamScore[0].Score)
                                    {
                                        this.bf3m_iWinningTeam = 2;
                                    }
                                }
                                else if (this.bf3m_strGameMode == "SquadDeathMatch0")
                                {
                                    if (this.bf3m_lstTeamScore[0].Score > this.bf3m_lstTeamScore[1].Score && this.bf3m_lstTeamScore[0].Score > this.bf3m_lstTeamScore[2].Score && this.bf3m_lstTeamScore[0].Score > this.bf3m_lstTeamScore[3].Score)
                                    {
                                        this.bf3m_iWinningTeam = 1;
                                    }
                                    else if (this.bf3m_lstTeamScore[1].Score > this.bf3m_lstTeamScore[0].Score && this.bf3m_lstTeamScore[1].Score > this.bf3m_lstTeamScore[2].Score && this.bf3m_lstTeamScore[1].Score > this.bf3m_lstTeamScore[3].Score)
                                    {
                                        this.bf3m_iWinningTeam = 2;
                                    }
                                    else if (this.bf3m_lstTeamScore[2].Score > this.bf3m_lstTeamScore[0].Score && this.bf3m_lstTeamScore[2].Score > this.bf3m_lstTeamScore[1].Score && this.bf3m_lstTeamScore[2].Score > this.bf3m_lstTeamScore[3].Score)
                                    {
                                        this.bf3m_iWinningTeam = 3;
                                    }
                                    else if (this.bf3m_lstTeamScore[3].Score > this.bf3m_lstTeamScore[0].Score && this.bf3m_lstTeamScore[3].Score > this.bf3m_lstTeamScore[1].Score && this.bf3m_lstTeamScore[3].Score > this.bf3m_lstTeamScore[2].Score)
                                    {
                                        this.bf3m_iWinningTeam = 4;
                                    }
                                }
                                else if (this.bf3m_strGameMode == "SquadRush0" || this.bf3m_strGameMode == "RushLarge0" && this.bf3m_lstTeamScore.Count != 0)
                                {
                                    if (this.bf3m_lstTeamScore[0].Score > this.bf3m_lstTeamScore[1].Score)
                                    {
                                        this.bf3m_iWinningTeam = 1;
                                    }
                                    else if (this.bf3m_lstTeamScore[1].Score > this.bf3m_lstTeamScore[0].Score)
                                    {
                                        this.bf3m_iWinningTeam = 2;
                                    }
                                }
                            }
                            this.ExecuteCommand("procon.protected.send", "maplist.endRound", this.bf3m_iWinningTeam.ToString());
                        }
                    }
                }
                #endregion

                #region CheckTRV
                if (this.bf3m_blRTVStarted)
                {
                    Write2PluginConsole(String.Format("!Vote time left = {0}", (this.bf3m_iRTVDurationInSeconds + this.bf3m_iRTVStoredTime) - this.bf3m_iRoundTime), "CheckTRV", "Debug", 100);
                    this.RTVCheck();
                }
                #endregion
            }
        }
        #endregion

        #region SetInfantryOnly
        public void SetInfantryOnly()
        {
            //Write2PluginConsole(String.Format("Checking Infantry Only"), "SetInfantryOnly", "Var", 100);
            if (this.bf3m_strDefaultMode == "Normal/Infantry Only" || this.bf3m_strDefaultMode == "Allow Custom")
            {
                //Write2PluginConsole(String.Format("Default is Normal/Infantry Only or Allow Custom"), "SetInfantryOnly", "Var", 100);
                if (this.bf3m_strGameMode != "SquadRush0")
                {
                    //Write2PluginConsole(String.Format("Mode is Not Squad Rush"), "SetInfantryOnly", "Var", 100);
                    if (this.bf3m_iPlayerCount < this.bf3m_iMinPlayersInfantryOnly && this.bf3m_enUseInfantryOnly == enumBoolYesNo.Yes && !this.bf3m_blInfantryOnly)
                    {
                        Write2PluginConsole(String.Format("Changing to IO"), "SetInfantryOnly", "Var", 3);
                        if (this.bf3m_strDefaultMode == "Normal/Infantry Only")
                        {
                            this.ExecuteCommand("procon.protected.send", "vars.3pCam", "false");
                        }
                        this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "false");
                        Write2PluginConsole(String.Format("Infantry Only has been Enabled"), "SetInfantryOnly", "Var", 1);
                        this.bf3m_blInfantryOnly = true;
                    }
                    else if (this.bf3m_blInfantryOnly && (this.bf3m_enUseInfantryOnly == enumBoolYesNo.No || (this.bf3m_blInfantryOnly && this.bf3m_iPlayerCount >= this.bf3m_iMinPlayersInfantryOnly && this.bf3m_enUseInfantryOnly == enumBoolYesNo.Yes)))
                    {
                        if (this.bf3m_strDefaultMode == "Normal/Infantry Only")
                        {
                            this.ExecuteCommand("procon.protected.send", "vars.3pCam", "true");
                        }
                        this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "true");
                        Write2PluginConsole(String.Format("Infantry Only has been Disabled"), "SetInfantryOnly", "Var", 1);
                        this.bf3m_blInfantryOnly = false;
                    }
                }
                else if (this.bf3m_strGameMode == "SquadRush0" && this.bf3m_blInfantryOnly)
                {
                    if (this.bf3m_strDefaultMode == "Normal/Infantry Only")
                    {
                        this.ExecuteCommand("procon.protected.send", "vars.3pCam", "true");
                    }
                    this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "true");
                    this.bf3m_blInfantryOnly = false;
                }
            }
            else if (this.bf3m_strDefaultMode == "Retain Hardcore")
            {
                if (this.bf3m_strGameMode != "SquadRush0")
                {
                    if (this.bf3m_iPlayerCount < this.bf3m_iMinPlayersInfantryOnly && this.bf3m_enUseInfantryOnly == enumBoolYesNo.Yes && this.bf3m_blInfantryOnly)
                    {
                        this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "false");
                        Write2PluginConsole(String.Format("Infantry Only has been Enabled"), "SetInfantryOnly", "Var", 1);
                        this.bf3m_blInfantryOnly = true;
                        if (this.bf3m_strGameMode != "SquadDeathMatch0")
                        {
                            Thread sleepInfantry = new Thread(new ThreadStart(delegate ()
                            {
                                Thread.Sleep(5000);
                                this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnDelay", "999999");
                                Thread.Sleep(500);
                                this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "true");
                            }));
                            sleepInfantry.Start();
                        }
                    }
                    else if (this.bf3m_blInfantryOnly && (this.bf3m_enUseInfantryOnly == enumBoolYesNo.No || (this.bf3m_iPlayerCount >= this.bf3m_iMinPlayersInfantryOnly && this.bf3m_enUseInfantryOnly == enumBoolYesNo.Yes)))
                    {
                        this.GetMapVehDelay();
                        this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnDelay", this.bf3m_iMapVehDelay.ToString());
                        this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "true");
                        Write2PluginConsole(String.Format("Infantry Only has been Disabled"), "SetInfantryOnly", "Var", 1);
                        this.bf3m_blInfantryOnly = false;
                    }
                }
                else if (this.bf3m_strGameMode == "SquadRush0" && this.bf3m_blInfantryOnly)
                {
                    this.ExecuteCommand("procon.protected.send", "vars.vehicleSpawnAllowed", "true");
                    this.bf3m_blInfantryOnly = false;
                }
            }
        }
        #endregion

        #region ChangeServerSize
        public void ChangeServerSize()
        {
            if (this.bf3m_strMaxPlayersByListOrMode == "Map List")
            {
                MapListGenerator mapListGetMaxPlayer = this.bf3m_lstMapLists[bf3m_iCurrentMapList];
                //if (mapListGetMaxPlayer.ListMaxPlayers != this.bf3m_iCurrentMaxPlayers) {
                this.ExecuteCommand("procon.protected.send", "vars.maxPlayers", mapListGetMaxPlayer.ListMaxPlayers.ToString());
                Write2PluginConsole(String.Format("Max Players set to {0}", mapListGetMaxPlayer.ListMaxPlayers), "ChangeServerSize", "Var", 1);
                //}				
            }
            else if (this.bf3m_strMaxPlayersByListOrMode == "GameType")
            {
                foreach (MaxPlayersGameType mpgGameType in this.bf3m_lstGameTypeMaxPlayer)
                {
                    if (mpgGameType.GameType == this.bf3m_strGameMode/* && mpgGameType.MaxPlayers != this.bf3m_iCurrentMaxPlayers*/)
                    {
                        this.ExecuteCommand("procon.protected.send", "vars.maxPlayers", mpgGameType.MaxPlayers.ToString());
                        Write2PluginConsole(String.Format("Max Players set to {0}", mpgGameType.MaxPlayers), "ChangeServerSize", "Var", 1);
                        break;
                    }
                }
            }
            else if (this.bf3m_strMaxPlayersByListOrMode == "Max Players allowed on Server"/* && this.bf3m_iCurrentMaxPlayers != this.bf3m_iMaxPlayersServer*/)
            {
                this.ExecuteCommand("procon.protected.send", "vars.maxPlayers", this.bf3m_iMaxPlayersServer.ToString());
                Write2PluginConsole(String.Format("Max Players set to {0}", this.bf3m_iMaxPlayersServer), "ChangeServerSize", "Var", 1);
            }
        }
        #endregion

        #region ChangeMapVars
        public void ChangeMapVars()
        {
            if (this.bf3m_enMapListVarsDefault == enumBoolYesNo.Yes)
            {
                int iTempMapSettingsListID = 0;
                foreach (MapSettingsGenerator Map2check in this.bf3m_lstMapSettings)
                {
                    if (Map2check.MapListId == this.bf3m_iCurrentMapList && Map2check.MapName == this.bf3m_strNextMapFriendlyName && Map2check.MapMode == this.bf3m_strNextMapFriendlyMode)
                    {
                        Write2PluginConsole(String.Format("MapId={0} MapListId={1} MapName={2} MapMode={3}", Map2check.MapId, Map2check.MapListId, Map2check.MapName, Map2check.MapMode), "ChangeMapVars", "Debug", 99);
                        iTempMapSettingsListID = Map2check.MapId;
                        break;
                    }
                }
                List<MapSettings> tempList2Send = new List<MapSettings>();
                int iTempListPos = 0;
                foreach (MapSettings Settings2Check in this.bf3m_lstDefaultVarSettings)
                {
                    Write2PluginConsole(String.Format("Added Default Var = {0} {1}", Settings2Check.GameVarName, Settings2Check.GameVarValue), "ChangeMapVars", "Debug", 99);
                    tempList2Send.Add(new MapSettings(Settings2Check.GameVarName, Settings2Check.GameVarValue));
                    foreach (MapSettings SettingsNextMap in this.bf3m_lstMapSettings[iTempMapSettingsListID].MapsSettings)
                    {
                        if (Settings2Check.GameVarName == SettingsNextMap.GameVarName)
                        {
                            Write2PluginConsole(String.Format("Changing Default Var = {0} {1} ^1to NextMap Var ^0{2} {3}", Settings2Check.GameVarName, Settings2Check.GameVarValue, SettingsNextMap.GameVarName, SettingsNextMap.GameVarValue), "ChangeMapVars", "Debug", 99);
                            tempList2Send[iTempListPos].GameVarValue = SettingsNextMap.GameVarValue;
                        }
                    }
                    iTempListPos = iTempListPos + 1;
                }
                Thread sleepChangeVars = new Thread(new ThreadStart(delegate ()
                {
                    foreach (MapSettings Settings2Set in tempList2Send)
                    {
                        Thread.Sleep(500);
                        Write2PluginConsole(String.Format("Setting {0} {1}", Settings2Set.GameVarName, Settings2Set.GameVarValue), "ChangeMapVars", "Var", 1);
                        this.ExecuteCommand("procon.protected.send", Settings2Set.GameVarName, Settings2Set.GameVarValue);
                    }
                }));
                sleepChangeVars.Start();
                /*List<string> lstValidatelist = new List<string>(this.StringifyMapVars(tempList2Send));
				lstValidatelist.RemoveAll(String.IsNullOrEmpty);
				this.SetMapSettings(lstValidatelist);	*/
            }
        }
        #endregion

        #region SetNextMap
        public void SetNextMap(int mapIndex, int nextIndex)
        {
            this.bf3m_iMapIndexCurrent = mapIndex;
            this.bf3m_iMapIndexNext = nextIndex;
            if (this.bf3m_lstMapListsCheck.Count > 0)
            {
                if (this.bf3m_iMapIndexNext > (this.bf3m_lstMapListsCheck.Count - 1))
                {
                    this.bf3m_iMapIndexNext = 0;
                }
                if (this.bf3m_enRandomMapIndex == enumBoolYesNo.Yes)
                {
                    int iHelperInt = 0;
                    Random random = new Random();
                    int randomNumber = random.Next(0, (this.bf3m_lstMapListsCheck.Count - 1));
                    if (randomNumber != this.bf3m_iMapIndexCurrent)
                    {
                        iHelperInt = randomNumber;
                    }
                    this.bf3m_iMapIndexNext = iHelperInt;
                }
                if (this.bf3m_blNextMapIsCurrentMap)
                {
                    if (this.bf3m_iPlayerCount == 0 && this.bf3m_enEmptyRotate == enumBoolYesNo.Yes)
                    {
                        this.ExecuteCommand("procon.protected.send", "mapList.setNextMapIndex", this.bf3m_iMapIndexNext.ToString());
                    }
                    CMap cmCurrentIndex = this.GetMap(this.bf3m_lstMapListsCheck[this.bf3m_iMapIndexCurrent].MapFileName, this.bf3m_lstMapListsCheck[this.bf3m_iMapIndexCurrent].GameMode);
                    foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
                    {
                        if (String.Compare(cmCurrentIndex.FileName, cMapDefine.FileName, true) == 0 && String.Compare(cmCurrentIndex.PlayList, cMapDefine.PlayList, true) == 0)
                        {
                            this.bf3m_strNextMapFriendlyName = cMapDefine.PublicLevelName;
                            this.bf3m_strNextMapFriendlyMode = cMapDefine.GameMode;
                            break;
                        }
                    }
                    Write2PluginConsole(String.Format("Next Map is Current Map"), "SetNextMap", "Debug", 1);
                    WriteAdminSay(String.Format("*****Next Map*****"), "all", "", 2);
                    WriteAdminSay(String.Format("Currently on Round {0}/{1} On Map {2} {3}", this.bf3m_iLvlLRoundsPlayed, this.bf3m_iLvlLRoundsTotal, this.bf3m_strNextMapFriendlyName, this.bf3m_strNextMapFriendlyMode), "all", "", 1);
                }
                else
                {
                    Write2PluginConsole(String.Format("NextMap Index = {0}", this.bf3m_iMapIndexNext), "SetNextMap", "Debug", 3);
                    this.ExecuteCommand("procon.protected.send", "mapList.setNextMapIndex", this.bf3m_iMapIndexNext.ToString());
                    CMap cmNextIndex = this.GetMap(this.bf3m_lstMapListsCheck[this.bf3m_iMapIndexNext].MapFileName, this.bf3m_lstMapListsCheck[this.bf3m_iMapIndexNext].GameMode);
                    foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
                    {
                        if (String.Compare(cmNextIndex.FileName, cMapDefine.FileName, true) == 0 && String.Compare(cmNextIndex.PlayList, cMapDefine.PlayList, true) == 0)
                        {
                            this.bf3m_strNextMapFriendlyName = cMapDefine.PublicLevelName;
                            this.bf3m_strNextMapFriendlyMode = cMapDefine.GameMode;
                            break;
                        }
                    }
                    Write2PluginConsole(String.Format("Setting Next Map {0} {1} {2}", this.bf3m_iMapIndexNext, this.bf3m_strNextMapFriendlyName, this.bf3m_strNextMapFriendlyMode), "SetNextMap", "Debug", 1);
                    WriteAdminSay(String.Format("*****Next Map*****"), "all", "", 2);
                    WriteAdminSay(String.Format("Next Map {0} {1}", this.bf3m_strNextMapFriendlyName, this.bf3m_strNextMapFriendlyMode), "all", "", 1);
                }
                this.bf3m_blSetRandomMapIndex = false;
            }
        }
        #endregion

        #region RTVCheck
        public void RTVCheck()
        {
            if ((this.bf3m_iRTVDurationInSeconds + this.bf3m_iRTVStoredTime) < this.bf3m_iRoundTime)
            {
                Write2PluginConsole(String.Format("RTVTime Checked"), "RTVCheck", "Debug", 3);
                int iVote4Index = 99;
                int iVoted41 = 0;
                int iVoted42 = 0;
                int iVoted43 = 0;
                int iVoted44 = 0;
                int iVoted45 = 0;
                foreach (VoterList voter in this.bf3m_lstRTVPlayerInfo)
                {
                    Write2PluginConsole(String.Format("RTVMap voter.VoteId {0}", voter.VoteId), "RTVCheck", "Debug", 99);
                    if (voter.VoteId == 1) { iVoted41 = (iVoted41 + 1); }
                    if (voter.VoteId == 2) { iVoted42 = (iVoted42 + 1); }
                    if (voter.VoteId == 3) { iVoted43 = (iVoted43 + 1); }
                    if (voter.VoteId == 4) { iVoted44 = (iVoted44 + 1); }
                    if (voter.VoteId == 5) { iVoted45 = (iVoted45 + 1); }
                }
                if (iVoted41 > iVoted42 && iVoted41 > iVoted43 && iVoted41 > iVoted44 && iVoted41 > iVoted45)
                {
                    iVote4Index = 0;
                }
                else if (iVoted42 > iVoted41 && iVoted42 > iVoted43 && iVoted42 > iVoted44 && iVoted42 > iVoted45)
                {
                    iVote4Index = 1;
                }
                else if (iVoted43 > iVoted41 && iVoted43 > iVoted42 && iVoted43 > iVoted44 && iVoted43 > iVoted45)
                {
                    iVote4Index = 2;
                }
                else if (iVoted44 > iVoted41 && iVoted44 > iVoted42 && iVoted44 > iVoted43 && iVoted44 > iVoted45)
                {
                    iVote4Index = 3;
                }
                else if (iVoted45 > iVoted41 && iVoted45 > iVoted42 && iVoted45 > iVoted43 && iVoted45 > iVoted44)
                {
                    iVote4Index = 4;
                }
                else if (iVote4Index == 99/* || iVoted41 == iVoted42 || iVoted41 == iVoted43 || iVoted42 == iVoted43*/)
                {
                    //Random random = new Random();
                    //int randomNumber = random.Next(1, 5);
                    //iVote4Index = randomNumber;
                }
                if (iVote4Index == 99)
                {
                    WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                    WriteAdminSay(String.Format("No Map Won the Vote", this.bf3m_strNextMapFriendlyName, this.bf3m_strNextMapFriendlyMode), "all", "", 1);
                    this.bf3m_blRTVFired = false;
                }
                else if (iVote4Index != 99)
                {
                    MapNamesModesRoundsIndex RTVMap = this.bf3m_lstRTVMaps[iVote4Index];
                    this.bf3m_iMapIndexNext = RTVMap.Index;
                    Write2PluginConsole(String.Format("RTVMap.Index {0}", RTVMap.Index), "RTVCheck", "Debug", 99);
                    this.ExecuteCommand("procon.protected.send", "mapList.setNextMapIndex", RTVMap.Index.ToString());
                    foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
                    {
                        if (String.Compare(RTVMap.MapFileName, cMapDefine.FileName, true) == 0 && String.Compare(RTVMap.GameMode, cMapDefine.PlayList, true) == 0)
                        {
                            this.bf3m_strNextMapFriendlyName = cMapDefine.PublicLevelName;
                            this.bf3m_strNextMapFriendlyMode = cMapDefine.GameMode;
                            break;
                        }
                    }
                    WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                    WriteAdminSay(String.Format("{0} {1} Won the vote. Setting Next Map", this.bf3m_strNextMapFriendlyName, this.bf3m_strNextMapFriendlyMode), "all", "", 1);


                    if (this.bf3m_enRTVChange == enumBoolYesNo.Yes)
                    {
                        WriteAdminSay(String.Format("Changing maps in {0} seconds", this.bf3m_iRTVChangeTime), "all", "", 1);
                        Thread delayed = new Thread(new ThreadStart(delegate ()
                        {
                            Thread.Sleep(this.bf3m_iRTVChangeTime * 1000);
                            this.ExecuteCommand("procon.protected.send", "maplist.endRound", this.bf3m_iWinningTeam.ToString());
                        }));
                        delayed.Start();
                    }
                    this.bf3m_blRTVFired = true;
                }
                this.bf3m_iRTVCount = 0;
                this.bf3m_iRTVStoredTime = 999999;
                this.bf3m_lstRTVPlayerInfo.Clear();
                this.bf3m_blRTVStarted = false;
            }
        }
        #endregion

        #region StrigifyLists
        public string[] StringifyMapList(List<MapNamesModesRoundsIndex> lst2Stringify)
        {
            List<MapNamesModesRoundsIndex> lstStringifyMapList = lst2Stringify;
            string[] a_strReturn = new string[lstStringifyMapList.Count];
            for (int i = 0; i < lstStringifyMapList.Count; i++)
            {
                a_strReturn[i] = String.Format("{0} {1} {2}", lstStringifyMapList[i].MapFileName, lstStringifyMapList[i].GameMode, lstStringifyMapList[i].Rounds);
            }
            return a_strReturn;
        }

        public string[] StringifyMapVars(List<MapSettings> lst2Stringify)
        {
            List<MapSettings> lstStringifyMapVars = lst2Stringify;
            string[] a_strReturn = new string[lstStringifyMapVars.Count];
            for (int i = 0; i < lstStringifyMapVars.Count; i++)
            {
                a_strReturn[i] = String.Format("{0} {1}", lstStringifyMapVars[i].GameVarName, lstStringifyMapVars[i].GameVarValue);
            }
            return a_strReturn;
        }
        #endregion

        #region GetMapNamesModesRounds
        public void GetMapNamesModesRounds(List<MapNamesModesRoundsIndex> lstMapLists2Check, List<string> lstList2Validate, string strMapListsName, int iMapListsCurrent)
        {
            List<MapNamesModesRoundsIndex> lstMapListChecking = lstMapLists2Check;
            List<string> lstValidateMaplist = lstList2Validate;
            string strMapListNameChecking = strMapListsName;
            int iMapListCurrentChecking = iMapListsCurrent;
            if (lstMapListChecking.Count > 0)
            {
                Write2PluginConsole(String.Format("Clearing {0} Maplist..", strMapListNameChecking), "GetMapNamesModesRounds", "Map", 2);
                lstMapListChecking.Clear();
            }
            bool blShowEmptyMapAddError = true;
            foreach (string strMapFileNameRound in lstValidateMaplist)
            {
                string[] a_strMapInfo = strMapFileNameRound.Split(' ');
                string strMapFileName = "";
                string strGameMode = "";
                int iRounds = 0;
                if (a_strMapInfo.Length >= 1)
                {
                    strMapFileName = a_strMapInfo[0];
                    if (a_strMapInfo.Length >= 2)
                    {
                        strGameMode = a_strMapInfo[1];
                        if (a_strMapInfo.Length >= 3)
                        {
                            int.TryParse(a_strMapInfo[2], out iRounds);
                        }
                    }
                }
                if (String.IsNullOrEmpty(strMapFileName) == false)
                {
                    blShowEmptyMapAddError = true;
                    foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
                    {
                        if (String.Compare(strMapFileName, cMapDefine.FileName, true) == 0 && String.Compare(strGameMode, cMapDefine.PlayList, true) == 0)
                        {
                            lstMapListChecking.Add(new MapNamesModesRoundsIndex(cMapDefine.FileName.ToLower(), cMapDefine.PlayList.ToLower(), iRounds, (lstMapListChecking.Count)));
                            if (iRounds == 0)
                            {
                                Write2PluginConsole(String.Format("Adding ^4{0} ^5{1}^1 to the {2} Maplist with ^4default number of rounds..", cMapDefine.PublicLevelName, cMapDefine.GameMode, strMapListNameChecking), "GetMapNamesModesRounds", "Map", 1);
                            }
                            else if (iRounds != 0)
                            {
                                Write2PluginConsole(String.Format("Adding ^4{0} ^5{1}^1 to the {2} Maplist with ^4{3} round(s)..", cMapDefine.PublicLevelName, cMapDefine.GameMode, strMapListNameChecking, iRounds), "GetMapNamesModesRounds", "Map", 1);
                            }
                            blShowEmptyMapAddError = false;
                            break;
                        }
                    }
                    if (blShowEmptyMapAddError)
                    {
                        Write2PluginConsole(String.Format("{0} The map \"{1}\" is not a valid Map (or it's unknown to procon at the moment)", strMapListNameChecking, strMapFileName), "GetMapNamesModesRounds", "Map", 3);
                    }
                }
            }
        }
        #endregion

        #region GetMapSettings
        public void GetMaps4Settings(List<string> lstList2Validate, int iMapListsCurrent)
        {
            List<string> lstValidateMaplist = lstList2Validate;
            int iMapListCurrentChecking = iMapListsCurrent;
            foreach (string strMapFileNameRound in lstValidateMaplist)
            {
                string[] a_strMapInfo = strMapFileNameRound.Split(' ');
                string strMapFileName = "";
                string strGameMode = "";
                if (a_strMapInfo.Length >= 1)
                {
                    strMapFileName = a_strMapInfo[0];
                    if (a_strMapInfo.Length >= 2)
                    {
                        strGameMode = a_strMapInfo[1];
                    }
                }
                if (String.IsNullOrEmpty(strMapFileName) == false)
                {
                    foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
                    {
                        if (String.Compare(strMapFileName, cMapDefine.FileName, true) == 0 && String.Compare(strGameMode, cMapDefine.PlayList, true) == 0)
                        {
                            this.bf3m_lstMapSettings.Add(new MapSettingsGenerator(this.bf3m_lstMapSettings.Count, iMapListCurrentChecking, String.Format("{0}", cMapDefine.PublicLevelName), String.Format("{0}", cMapDefine.GameMode)));
                            //Write2PluginConsole(String.Format("Adding ^4{0} {1}^1 to the MapSettings Dictionary..", cMapDefine.PublicLevelName, cMapDefine.GameMode), "GetMaps4Settings", "Debug", 100);
                        }
                    }
                }
            }
        }

        public void GetMapSettings(List<MapSettings> lstLists2Check, List<string> lstList2Validate, int iMapListsCurrent, string strMapName, string strMapMode)
        {
            List<MapSettings> lstSettings2Check = lstLists2Check;
            List<string> lstValidatelist = lstList2Validate;
            int iMapListCurrentChecking = iMapListsCurrent;
            string strFriendlyName = strMapName;
            string strFriendlyMode = strMapMode;
            int iRandomInt = 99;
            if (lstSettings2Check.Count > 0)
            {
                Write2PluginConsole(String.Format("Clearing {0} {1} Settings List..", strFriendlyName, strFriendlyMode), "GetMapSettings", "Var", 2);
                lstSettings2Check.Clear();
            }
            foreach (string strVarNameValue in lstValidatelist)
            {
                string[] a_strVarInfo = strVarNameValue.Split(' ');
                string strVarName = "";
                string strVarValue = "";
                if (a_strVarInfo.Length >= 1)
                {
                    strVarName = a_strVarInfo[0];
                    if (a_strVarInfo.Length >= 2)
                    {
                        strVarValue = a_strVarInfo[1];
                    }
                }
                bool blContinueCheck = false;
                foreach (string var2checkBool in bf3m_straValidVarsBool)
                {
                    if (var2checkBool == strVarName.ToLower())
                    {
                        if (strVarValue.ToLower() == "true" || strVarValue.ToLower() == "false")
                        {
                            blContinueCheck = true;
                            break;
                        }
                    }
                }
                foreach (string var2checkInt in bf3m_straValidVarsInt)
                {
                    if (var2checkInt == strVarName.ToLower())
                    {
                        if (int.TryParse(strVarValue, out iRandomInt) == true)
                        {
                            blContinueCheck = true;
                            break;
                        }
                    }
                }
                if (blContinueCheck)
                {
                    if (strFriendlyName == "Default" && iMapListCurrentChecking == 0)
                    {
                        lstSettings2Check.Add(new MapSettings(strVarName.ToLower().ToLower(), strVarValue.ToLower().ToLower()));
                        Write2PluginConsole(String.Format("Adding ^4{0} {1}^1 to the Deafult Settings List", strVarName, strVarValue), "GetMapSettings", "Var", 1);
                    }
                    else
                    {
                        foreach (MapSettingsGenerator mapVarSettings in bf3m_lstMapSettings)
                        {
                            if (mapVarSettings.MapListId == iMapListCurrentChecking && mapVarSettings.MapName == strFriendlyName && mapVarSettings.MapMode == strFriendlyMode)
                            {
                                lstSettings2Check.Add(new MapSettings(strVarName.ToLower(), strVarValue.ToLower()));
                                Write2PluginConsole(String.Format("Adding ^4{0} {1}^1 to the MapSettings For ^4{2} ^5{3}", strVarName, strVarValue, mapVarSettings.MapName, mapVarSettings.MapMode), "GetMapSettings", "Var", 1);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Write2PluginConsole(String.Format("{0} {1}^1 is not a valid variable name or value", strVarName.ToLower(), strVarValue.ToLower()), "GetMapSettings", "Var", 3);
                }
            }
        }

        /*public void SetMapSettings(List<string> lstList2Validate) {		
			List<string> lstValidatelist = lstList2Validate;
			Thread sleepChangeVars = new Thread(new ThreadStart(delegate() {
				foreach (string strVarNameValue in lstValidatelist)	{
					string[] a_strVarInfo = strVarNameValue.Split(' ');
					string strVarName = "";
					string strVarValue = "";
					if (a_strVarInfo.Length >= 1) {
						strVarName = a_strVarInfo[0];
						if (a_strVarInfo.Length >= 2) {
							strVarValue = a_strVarInfo[1];
						}
					}
					Thread.Sleep(500);
					Write2PluginConsole(String.Format("Setting {0} {1}", strVarName, strVarValue), "SetMapSettings", "Debug", 99);
					this.ExecuteCommand("procon.protected.send", strVarName, strVarValue);	
				}
			}));
			sleepChangeVars.Start();
		}*/

        public void GetMapVehDelay()
        {
            this.bf3m_iMapVehDelay = this.bf3m_iDefaultVehDelay;
            if (this.bf3m_enMapListVarsDefault == enumBoolYesNo.Yes)
            {
                int iTempMapSettingsListID = 0;
                foreach (MapSettings DefaultSettings2Check in this.bf3m_lstMapSettings[0].MapsSettings)
                {
                    if (DefaultSettings2Check.GameVarName == "vars.vehiclespawnDelay")
                    {
                        if (int.TryParse(DefaultSettings2Check.GameVarValue, out this.bf3m_iMapVehDelay) == true)
                            break;
                    }
                }
                foreach (MapSettingsGenerator Map2check in this.bf3m_lstMapSettings)
                {
                    Write2PluginConsole(String.Format("MapId={0} MapListId={1} MapName={2} MapMode={3}", Map2check.MapId, Map2check.MapListId, Map2check.MapName, Map2check.MapMode), "GetMapVehDelay", "Debug", 99);
                    if (Map2check.MapListId == this.bf3m_iCurrentMapList && Map2check.MapName == this.bf3m_strCurrentMapFriendlyName && Map2check.MapMode == this.bf3m_strCurrentMapFriendlyMode)
                    {
                        iTempMapSettingsListID = Map2check.MapId;
                        break;
                    }
                }
                foreach (MapSettings MapSettings2Check in this.bf3m_lstMapSettings[iTempMapSettingsListID].MapsSettings)
                {
                    if (MapSettings2Check.GameVarName == "vars.vehiclespawnDelay")
                    {
                        if (int.TryParse(MapSettings2Check.GameVarValue, out this.bf3m_iMapVehDelay) == true)
                            break;
                    }
                }
            }
        }
        #endregion

        #region CMap
        public CMap GetMap(string strMapFilename, string strPlayList)
        {
            CMap cmReturn = null;
            foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
            {
                if (String.Compare(strMapFilename, cMapDefine.FileName.ToLower()) == 0 && String.Compare(strPlayList, cMapDefine.PlayList.ToLower()) == 0)
                {
                    cmReturn = cMapDefine;
                    break;
                }
            }
            return cmReturn;
        }
        #endregion

        #region ParseDateTime
        public class ParseDateTime
        {
            public static bool TimeBased(DateTime dtNow, string dtCheck)
            {
                bool tempBool;
                string strDateTime1 = "";
                string strDateTime2 = "";
                string strDateTime3 = "";

                string strCheckTime1 = "";
                string strCheckTime2 = "";
                string strCheckTime3 = "";

                DateTime dateCheckValue;

                string format = "h:mm tt";
                string format1 = "h:";
                string format2 = "mm";
                string format3 = "tt";

                strDateTime1 = dtNow.ToString(format1);
                strDateTime2 = dtNow.ToString(format2);
                strDateTime3 = dtNow.ToString(format3);

                dateCheckValue = DateTime.Parse(dtCheck);
                strCheckTime1 = dateCheckValue.ToString(format1);
                strCheckTime2 = dateCheckValue.ToString(format2);
                strCheckTime3 = dateCheckValue.ToString(format3);

                if (strDateTime1 == strCheckTime1)
                {
                    if (strDateTime2 == strCheckTime2)
                    {
                        if (strDateTime3 == strCheckTime3)
                        {
                            tempBool = true;
                        }
                        else
                        {
                            tempBool = false;
                        }
                    }
                    else
                    {
                        tempBool = false;
                    }
                }
                else
                {
                    tempBool = false;
                }

                return tempBool;
            }
        }
        #endregion

        #region MapNamesModesRoundsIndex
        public class MapNamesModesRoundsIndex
        {
            string strMapFileName;
            string strGameMode;
            int iRoundsInList;
            int iIndex;

            public MapNamesModesRoundsIndex(string strMapName, string strMapMode, int iRounds, int iListIndex)
            {
                strMapFileName = strMapName;
                strGameMode = strMapMode;
                iRoundsInList = iRounds;
                iIndex = iListIndex;
            }
            public string MapFileName
            {
                set { strMapFileName = value; }
                get { return strMapFileName; }
            }
            public string GameMode
            {
                set { strGameMode = value; }
                get { return strGameMode; }
            }
            public int Rounds
            {
                set { iRoundsInList = value; }
                get { return iRoundsInList; }
            }
            public int Index
            {
                set { iIndex = value; }
                get { return iIndex; }
            }
        }
        #endregion

        #region MapListGenerator
        public class MapListGenerator
        {
            int iMapListID;
            string strMapListName;
            int iMapListMinPlayers2Use;
            int iMapListMaxPlayers2Use;
            int iMapListMaxPlayers;
            List<MapNamesModesRoundsIndex> lstMapListList;

            public MapListGenerator(int id, string nameList)
            {
                iMapListID = id;
                strMapListName = nameList;
                iMapListMinPlayers2Use = 64;
                iMapListMaxPlayers2Use = 64;
                iMapListMaxPlayers = 64;
                lstMapListList = new List<MapNamesModesRoundsIndex>();
            }
            public int ListId
            {
                set { iMapListID = value; }
                get { return iMapListID; }
            }
            public string ListName
            {
                set { strMapListName = value; }
                get { return strMapListName; }
            }
            public int ListMinPlayers2Use
            {
                set { iMapListMinPlayers2Use = value; }
                get { return iMapListMinPlayers2Use; }
            }
            public int ListMaxPlayers2Use
            {
                set { iMapListMaxPlayers2Use = value; }
                get { return iMapListMaxPlayers2Use; }
            }
            public int ListMaxPlayers
            {
                set { iMapListMaxPlayers = value; }
                get { return iMapListMaxPlayers; }
            }
            public List<MapNamesModesRoundsIndex> ListMapsList
            {
                set { lstMapListList = value; }
                get { return lstMapListList; }
            }
        }
        #endregion

        #region MapSettings
        public class MapSettings
        {
            string m_strGameVarName;
            string m_strGameVarValue;

            public MapSettings(string strGameVarName, string strGameVarValue)
            {
                m_strGameVarName = strGameVarName;
                m_strGameVarValue = strGameVarValue;
            }
            public string GameVarName
            {
                set { m_strGameVarName = value; }
                get { return m_strGameVarName; }
            }
            public string GameVarValue
            {
                set { m_strGameVarValue = value; }
                get { return m_strGameVarValue; }
            }
        }
        #endregion

        #region MapSettingsGenerator
        public class MapSettingsGenerator
        {
            int iMapID;
            int iMapListID;
            string strMapName;
            string strMapMode;
            List<MapSettings> lstMapSettings;

            public MapSettingsGenerator(int id, int listId, string strMapListName, string strMapListMode)
            {
                iMapID = id;
                iMapListID = listId;
                strMapName = strMapListName;
                strMapMode = strMapListMode;
                lstMapSettings = new List<MapSettings>();
            }
            public int MapId
            {
                set { iMapID = value; }
                get { return iMapID; }
            }
            public int MapListId
            {
                set { iMapListID = value; }
                get { return iMapListID; }
            }
            public string MapName
            {
                set { strMapName = value; }
                get { return strMapName; }
            }
            public string MapMode
            {
                set { strMapMode = value; }
                get { return strMapMode; }
            }
            public List<MapSettings> MapsSettings
            {
                set { lstMapSettings = value; }
                get { return lstMapSettings; }
            }
        }
        #endregion

        #region MaxPlayersGameType
        public class MaxPlayersGameType
        {
            int iTypeID;
            int iMaxPlayers;
            string strGameType;
            string strGameTypeName;

            public MaxPlayersGameType(int id, int iMaximumPlayers, string strServerType, string strTypeName)
            {
                iTypeID = id;
                iMaxPlayers = iMaximumPlayers;
                strGameType = strServerType;
                strGameTypeName = strTypeName;
            }
            public int TypeId
            {
                set { iTypeID = value; }
                get { return iTypeID; }
            }
            public int MaxPlayers
            {
                set { iMaxPlayers = value; }
                get { return iMaxPlayers; }
            }
            public string GameType
            {
                set { strGameType = value; }
                get { return strGameType; }
            }
            public string TypeName
            {
                set { strGameTypeName = value; }
                get { return strGameTypeName; }
            }
        }
        #endregion

        #region VoterList
        public class VoterList
        {
            string strVoterName;
            int iVote;

            public VoterList(string strVoterId, int iVoteId)
            {
                strVoterName = strVoterId;
                iVote = iVoteId;
            }
            public string VoterName
            {
                set { strVoterName = value; }
                get { return strVoterName; }
            }
            public int VoteId
            {
                set { iVote = value; }
                get { return iVote; }
            }
        }
        #endregion

        #region In Game Commands
        public void OnCommandNextmap(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.bf3m_blNextMapIsCurrentMap)
            {
                WriteAdminSay(String.Format("*****Next Map*****"), "all", "", 2);
                WriteAdminSay(String.Format("Currently on Round {0}/{1} On Map {2} {3}", this.bf3m_iLvlLRoundsPlayed, this.bf3m_iLvlLRoundsTotal, this.bf3m_strNextMapFriendlyName, this.bf3m_strNextMapFriendlyMode), "all", "", 0);
            }
            else
            {
                WriteAdminSay(String.Format("*****Next Map*****"), "all", "", 2);
                WriteAdminSay(String.Format("Next Map {0} {1}", this.bf3m_strNextMapFriendlyName, this.bf3m_strNextMapFriendlyMode), "all", "", 0);
            }
        }

        public void OnCommandRTVStop(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            this.bf3m_iRTVCount = 0;
            this.bf3m_iRTVStoredTime = 999999;
            this.bf3m_lstRTVPlayerInfo.Clear();
            this.bf3m_blRTVStarted = false;
            Write2PluginConsole(String.Format("Stopping RTV"), "OnCommandRTVStop", "Debug", 3);
        }

        public void OnCommandRockTheVote(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!this.bf3m_blRTVStarted)
            {
                if (this.bf3m_enOnOffRTV == enumBoolOnOff.On)
                {
                    Write2PluginConsole(String.Format("RTV is Enabled"), "OnCommandRockTheVote", "Debug", 3);
                    if (!this.bf3m_blRTVFired)
                    {
                        Write2PluginConsole(String.Format("RTV not already Running"), "OnCommandRockTheVote", "Debug", 99);
                        if (this.bf3m_iRTVRoundWaitInSeconds < this.bf3m_iRoundTime)
                        {
                            Write2PluginConsole(String.Format("RTV round Wait time Checked"), "OnCommandRockTheVote", "Debug", 3);
                            if ((!this.bf3m_blNextMapIsCurrentMap && this.bf3m_enRTVMultiRounds == enumBoolYesNo.No) || this.bf3m_enRTVMultiRounds == enumBoolYesNo.Yes)
                            {
                                Write2PluginConsole(String.Format("RTV allowed Multi Rounds"), "OnCommandRockTheVote", "Debug", 99);
                                bool blTempVoteCheck = true;
                                foreach (VoterList voters in this.bf3m_lstRTVPlayerInfo)
                                {
                                    Write2PluginConsole(String.Format("RTVPlayerInfoAll {0} {1}", voters.VoterName, voters.VoteId), "OnCommandRockTheVote", "Debug", 3);
                                    if (voters.VoterName == strSpeaker)
                                    {
                                        WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                                        WriteAdminSay(String.Format("type !rtv or !rockthevote in chat"), "all", "", 1);
                                        blTempVoteCheck = false;
                                        break;
                                    }
                                }
                                if (blTempVoteCheck)
                                {
                                    this.bf3m_iRTVCount = (this.bf3m_iRTVCount + 1);
                                    Write2PluginConsole(String.Format("RTV Check Start"), "OnCommandRockTheVote", "Debug", 99);
                                    this.bf3m_lstRTVPlayerInfo.Add(new VoterList(strSpeaker, -1));
                                    VoterList votersInList = this.bf3m_lstRTVPlayerInfo[(this.bf3m_lstRTVPlayerInfo.Count - 1)];
                                    Write2PluginConsole(String.Format("RTVPlayerInfo {0} {1}", votersInList.VoterName, votersInList.VoteId), "OnCommandRockTheVote", "Debug", 99);
                                    if (this.bf3m_iRTVCount < ((this.bf3m_iPlayerCount * this.bf3m_iRTVPercentage) / 100))
                                    {
                                        WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                                        WriteAdminSay(String.Format("{0}/{1} RTV's needed to start map vote.", this.bf3m_iRTVCount, ((this.bf3m_iPlayerCount * this.bf3m_iRTVPercentage) / 100)), "all", "", 1);
                                    }
                                    this.bf3m_iRTVStoredTime = this.bf3m_iRoundTime;
                                    if (this.bf3m_iRTVCount >= ((this.bf3m_iPlayerCount * this.bf3m_iRTVPercentage) / 100))
                                    {
                                        Write2PluginConsole(String.Format("RTV's Needed {0} Recieved {1}", (this.bf3m_iPlayerCount * this.bf3m_iRTVPercentage) / 100, this.bf3m_iRTVCount), "OnCommandRockTheVote", "Debug", 3);
                                        this.bf3m_iRTVStoredTime = this.bf3m_iRoundTime;
                                        this.bf3m_lstRTVMaps.Clear();
                                        this.bf3m_blRTVStarted = true;
                                        this.bf3m_blSetRandomMapIndex = false;
                                        this.bf3m_lstRTVPlayerInfo.Clear();

                                        Random random = new Random();
                                        int randomNumber = random.Next(0, this.bf3m_lstMapListsCheck.Count);
                                        int randomSet1 = -1;
                                        int randomSet2 = -1;
                                        int randomSet3 = -1;
                                        int randomSet4 = -1;
                                        int randomSet5 = -1;
                                        WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                                        for (int i = 0; i < this.bf3m_iRTVNumMaps; i++)
                                        {
                                            randomNumber = random.Next(0, this.bf3m_lstMapListsCheck.Count);

                                            if (this.bf3m_enRTVInclude == enumBoolYesNo.Yes)
                                            {
                                            }
                                            else if ((this.bf3m_enRTVInclude == enumBoolYesNo.No && randomNumber == this.bf3m_iMapIndexCurrent) || randomSet1 == randomNumber || randomSet2 == randomNumber || randomSet3 == randomNumber || randomSet4 == randomNumber || randomSet5 == randomNumber)
                                            {
                                                i = (i - 1);
                                            }
                                            else
                                            {
                                                CMap cmMap2RTV = this.GetMap(this.bf3m_lstMapListsCheck[randomNumber].MapFileName, this.bf3m_lstMapListsCheck[randomNumber].GameMode);
                                                foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
                                                {
                                                    if (String.Compare(cmMap2RTV.FileName, cMapDefine.FileName, true) == 0 && String.Compare(cmMap2RTV.PlayList, cMapDefine.PlayList, true) == 0)
                                                    {
                                                        this.bf3m_lstRTVMaps.Add(new MapNamesModesRoundsIndex(cmMap2RTV.FileName, cmMap2RTV.PlayList, this.bf3m_lstMapListsCheck[randomNumber].Rounds, this.bf3m_lstMapListsCheck[randomNumber].Index));
                                                        WriteAdminSay(String.Format("!vote {0} {1} {2}", (i + 1), cMapDefine.PublicLevelName, cMapDefine.GameMode), "all", "", 1);
                                                        if (i == 0)
                                                        {
                                                            randomSet1 = randomNumber;
                                                        }
                                                        else if (i == 1)
                                                        {
                                                            randomSet2 = randomNumber;
                                                        }
                                                        else if (i == 2)
                                                        {
                                                            randomSet3 = randomNumber;
                                                        }
                                                        else if (i == 3)
                                                        {
                                                            randomSet4 = randomNumber;
                                                        }
                                                        else if (i == 4)
                                                        {
                                                            randomSet5 = randomNumber;
                                                        }
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                    }
                                }
                            }
                            else
                            {
                                WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                                WriteAdminSay(String.Format("Rock the Vote unavailable on the first round of a Map.", strSpeaker), "all", "", 1);
                            }
                        }
                        else
                        {
                            WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                            if ((this.bf3m_iRTVRoundWaitInSeconds - this.bf3m_iRoundTime) >= 120)
                            {
                                WriteAdminSay(String.Format("{0}: Rock the Vote can not be started for {1} more minute/s after Map Loads", strSpeaker, ((this.bf3m_iRTVRoundWaitInSeconds - this.bf3m_iRoundTime) / 60)), "all", "", 1);
                            }
                            else if ((this.bf3m_iRTVRoundWaitInSeconds - this.bf3m_iRoundTime) < 120)
                            {
                                WriteAdminSay(String.Format("{0}: Rock the Vote can not be started for {1} more second/s after Map Loads", strSpeaker, (this.bf3m_iRTVRoundWaitInSeconds - this.bf3m_iRoundTime)), "all", "", 1);
                            }
                        }
                    }
                    else
                    {
                        WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                        WriteAdminSay(String.Format("Rock the Vote Next Map has been decided already."), "all", "", 1);
                    }
                }
                else
                {
                    WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                    WriteAdminSay(String.Format("Rock the Vote currently Disabled"), "all", "", 1);
                }
            }
            else
            {
                WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                WriteAdminSay(String.Format("Rock the Vote Started, type !rtvlist to see available maps."), "all", "", 1);
            }
        }

        public void OnCommandRTVList(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            Write2PluginConsole(String.Format("OnCommandRTVList"), "OnCommandRTVList", "Map", 2);
            if (this.bf3m_blRTVStarted)
            {
                WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                int iTempInt = 1;
                foreach (MapNamesModesRoundsIndex cmMap2RTV in this.bf3m_lstRTVMaps)
                {
                    Write2PluginConsole(String.Format("OnCommandRTVList {0} {1}", cmMap2RTV.MapFileName, cmMap2RTV.GameMode), "OnCommandRTVList", "Debug", 3);
                    foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
                    {
                        if (String.Compare(cmMap2RTV.MapFileName, cMapDefine.FileName, true) == 0 && String.Compare(cmMap2RTV.GameMode, cMapDefine.PlayList, true) == 0)
                        {
                            WriteAdminSay(String.Format("!vote {0} {1} {2}", iTempInt, cMapDefine.PublicLevelName, cMapDefine.GameMode), "all", "", 1);
                            iTempInt = iTempInt + 1;
                            break;
                        }
                    }
                }
            }
            else
            {
                WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                WriteAdminSay(String.Format("type !rtv to start a vote."), "all", "", 1);
            }
        }

        public void OnCommandVote(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.bf3m_blRTVStarted)
            {
                int iValue = 0;
                Write2PluginConsole(String.Format("OnCommandVote {0}", capCommand.MatchedArguments[0].Argument), "OnCommandVote", "Debug", 3);
                if (int.TryParse(capCommand.MatchedArguments[0].Argument, out iValue) == true)
                {
                    this.bf3m_lstRTVPlayerInfo.Add(new VoterList(strSpeaker, iValue));
                    Write2PluginConsole(String.Format("OnCommandVote {0}", iValue), "OnCommandVote", "Debug", 3);
                    if (iValue >= 1 && iValue <= this.bf3m_iRTVNumMaps)
                    {
                        foreach (VoterList voter in this.bf3m_lstRTVPlayerInfo)
                        {
                            if (voter.VoterName == strSpeaker)
                            {
                                voter.VoteId = iValue;
                                Write2PluginConsole(String.Format("RTVPlayerInfo {0} {1}", voter.VoterName, voter.VoteId), "OnCommandVote", "Debug", 3);
                                MapNamesModesRoundsIndex cmMap2RTV = this.bf3m_lstRTVMaps[iValue - 1];
                                foreach (CMap cMapDefine in this.bf3m_lstMapDefinesCheck)
                                {
                                    if (String.Compare(cmMap2RTV.MapFileName, cMapDefine.FileName, true) == 0 && String.Compare(cmMap2RTV.GameMode, cMapDefine.PlayList, true) == 0)
                                    {
                                        WriteAdminSay(String.Format("{0}: Voted {1} {2}", strSpeaker, cMapDefine.PublicLevelName, cMapDefine.GameMode), "all", "", 1);
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                WriteAdminSay(String.Format("*****Rock the Vote*****"), "all", "", 2);
                WriteAdminSay(String.Format("type !rtv to start a vote."), "all", "", 1);
            }
        }
        #endregion

        #region Tools
        public void Write2PluginConsole(string message, string tag, string type, int level)
        {
            //Write2PluginConsole(String.Format(""), "Work", "Debug", 5);
            string line = "^b^3[" + this.GetPluginName() + "] ^1" + tag + ": ^0^n" + message;
            if ((this.bf3m_iDebugLevel >= level && "Debug" == type) || SrciptDebugVar == 1)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", line);
            }
            if ((this.bf3m_iVarDebugLevel >= level && "Var" == type) && SrciptDebugVar != 1)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", line);
            }
            if ((this.bf3m_iMapDebugLevel >= level && "Map" == type) && SrciptDebugVar != 1)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", line);
            }
        }
        public void WriteAdminSay(string message, string tag, string player, int level)
        {
            //WriteAdminSay(String.Format(""), "all", "", 5);
            if ("all" == tag && this.bf3m_iAdminDebugLevel >= level)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", message, tag);
            }
        }
        #endregion
    }
    #endregion
}