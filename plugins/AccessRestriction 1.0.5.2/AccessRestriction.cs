/*  Copyright 2013 MorpheusX(AUT)

    https://myrcon.com

    This file is part of MorpheusX(AUT)'s Plugins for BFBC2 PRoCon.

    MorpheusX(AUT)'s Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MorpheusX(AUT)'s Plugins for BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MorpheusX(AUT)'s Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class AccessRestriction : PRoConPluginAPI, IPRoConPluginInterface
    {

        #region Variables and Constructor

        // Server & Plugin-Info
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;
        private bool m_IsPluginEnabled;
        private string serverName;
        private CServerInfo ServerInfo;
        private List<CPlayerInfo> lstPlayerList;

        // Dictionaries, Hashtables & Lists to store data
        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
        private Dictionary<string, CPlayerInfo> m_dicPlayers = new Dictionary<string, CPlayerInfo>();
        private Dictionary<string, int> m_PlayerKicks = new Dictionary<string, int>();
        private Dictionary<string, string> m_PlayerGUIDs = new Dictionary<string, string>();
        //private Hashtable m_LastUpdated;
        private List<string> m_WhiteList;
        private List<string> m_WhiteListClanTags;
        private List<string> m_AllowedPlayers;
        private List<string> m_AllowedClanTags;
        private List<string> m_ModeratedClanTags;
        private List<string> m_AllowedClanPlayers;
        private List<string> m_DisallowedClanTags;

        // Plugin-Variables
        private enumBoolYesNo m_RoCAccepted;
        private enumBoolYesNo m_BanGUID;
        private int m_RankLimit;
        private enumBoolYesNo m_TempBanPlayers;
        private enumBoolYesNo m_PermBanPlayers;
        private int m_TempBanTime;
        private enumBoolYesNo m_BanAfterKicks;
        private enumBoolYesNo m_ClanTagWhiteList;
        private int m_KicksBeforeBan;
        private string m_KickReason;
        private string m_BanReason;
        private string m_PermBanReason;
        private enumBoolOnOff m_RankKickerOnOff;
        private enumBoolYesNo m_ConsoleLog;
        private enumBoolYesNo m_ClearAll;
        private enumBoolOnOff m_RankKicker;
        private enumBoolOnOff m_AccessLimitation;
        private enumBoolYesNo m_AccessLimitationPlayers;
        private enumBoolYesNo m_AccessLimitationTags;
        private enumBoolYesNo m_AccessBanPlayers;
        private string m_AccessKickReason;
        private string m_AccessPermBanReason;
        private enumBoolOnOff m_ClanTagModeration;
        private enumBoolYesNo m_ClanTagBanPlayers;
        private string m_ClanTagKickReason;
        private string m_ClanTagPermBanReason;
        private enumBoolOnOff m_DisallowedTags;
        private enumBoolYesNo m_DisallowedTagsBan;
        private string m_DisallowedTagsKickReason;
        private string m_DisallowedTagsBanReason;
        private enumBoolYesNo m_AutomaticallyClearCache;
        private int m_CacheClearRounds;
        private int m_RoundsCacheClear;

        public AccessRestriction()
        {
            this.lstPlayerList = new List<CPlayerInfo>();
            //this.m_LastUpdated = null;
            this.m_RoCAccepted = enumBoolYesNo.No;
            this.m_BanGUID = enumBoolYesNo.No;
            this.m_RankKicker = enumBoolOnOff.Off;
            this.m_AccessLimitation = enumBoolOnOff.Off;
            this.m_RankLimit = 50;
            this.m_TempBanPlayers = enumBoolYesNo.No;
            this.m_PermBanPlayers = enumBoolYesNo.No;
            this.m_TempBanTime = 60;
            this.m_BanAfterKicks = enumBoolYesNo.No;
            this.m_KicksBeforeBan = 3;
            this.m_KickReason = "You got kicked due to your Player Rank being too high!";
            this.m_BanReason = "You  got banned for %bt% minutes due to your Player Rank being too high!";
            this.m_PermBanReason = "You permanently got banned due to your Player Rank being too high!";
            this.m_IsPluginEnabled = false;
            this.m_RankKickerOnOff = enumBoolOnOff.On;
            this.m_ConsoleLog = enumBoolYesNo.Yes;
            this.m_ClearAll = enumBoolYesNo.No;
            this.m_WhiteList = new List<string>();
            this.m_ClanTagWhiteList = enumBoolYesNo.No;
            this.m_WhiteListClanTags = new List<string>();
            this.m_AllowedClanTags = new List<string>();
            this.m_AccessLimitationPlayers = enumBoolYesNo.No;
            this.m_AccessLimitationTags = enumBoolYesNo.No;
            this.m_AllowedPlayers = new List<string>();
            this.m_AccessBanPlayers = enumBoolYesNo.No;
            this.m_AccessKickReason = "Sorry for the kick, dude! This is a private match!";
            this.m_AccessPermBanReason = "Sorry for the ban, dude! This is a private match!";
            this.m_ModeratedClanTags = new List<string>();
            this.m_AllowedClanPlayers = new List<string>();
            this.m_ClanTagModeration = enumBoolOnOff.Off;
            this.m_ClanTagBanPlayers = enumBoolYesNo.No;
            this.m_ClanTagKickReason = "You got kicked for using a clantag by unfair means!";
            this.m_ClanTagPermBanReason = "You got banned for using a clantag by unfair means!";
            this.m_DisallowedClanTags = new List<string>();
            this.m_DisallowedTags = enumBoolOnOff.Off;
            this.m_DisallowedTagsBan = enumBoolYesNo.No;
            this.m_DisallowedTagsKickReason = "Your clan isn't welcome on this server! You got kicked!";
            this.m_DisallowedTagsBanReason = "Your clan isn't welcome on this server! You got banned permanently!";
            this.m_AutomaticallyClearCache = enumBoolYesNo.Yes;
            this.m_CacheClearRounds = 6;
        }

        #endregion

        #region Stats

        private struct PlayerStats
        {
            public double rank;
            public void reset()
            {
                rank = 0;
            }
        }

        private PlayerStats? getBFBCStats(string name)
        {
            try
            {
                /* Getting BFBC2 stats */
                WebClient wc = new WebClient();
                string address = "http://api.bfbcs.com/api/pc?players=" + name + "&fields=smallinfo";
                string result = wc.DownloadString(address);

                Hashtable data = (Hashtable)JSON.JsonDecode(result);

                double found;
                if (!(data.Contains("found") && Double.TryParse(data["found"].ToString(), out found) == true && found == 1))
                    /* could not find stats */
                    return null;

                /* interpret the results from BFBC stats */
                Hashtable playerData = (Hashtable)((ArrayList)data["players"])[0];

                PlayerStats stats = new PlayerStats();
                double.TryParse(playerData["rank"].ToString(), out stats.rank);

                return stats;
            }
            catch (Exception e)
            {
                /* exception occurred while requesting BFBC2 stats */
                return null;
            }
        }

        #endregion

        #region Plugin Setup

        public string GetPluginName()
        {
            return "Access Restriction";
        }

        public string GetPluginVersion()
        {
            return "1.0.5.2";
        }

        public string GetPluginAuthor()
        {
            return "MorpheusX(AUT)";
        }

        public string GetPluginWebsite()
        {
            return "https://forum.myrcon.com/showthread.php?1446-Access-Restriction-(v1-0-5-1-01-01-2011)-BC2";
        }

        public string GetPluginDescription()
        {
            return @"
                <p>This Plugin was written by MorpheusX(AUT).<br>
                <b>eMail</b>: procon(at)morpheusx(dot)at<br>
                <b>Twitter</b>: MorpheusXAUT<br>
                <b>Thanks to</b>: micovery for helping me out with the rank-fetching code, 1ApRiL for providing me with a stats-API, blactionhero for some Clantag-Snippet<br>
                <br></p>

                <h2>Description</h2>
                <p>Please be aware that the use of this plugin might break the BFBC2 Rules of Conduct
                (found here: http://forums.electronicarts.co.uk/battlefield-announcements/1167691-bfbc2-rules-conduct.html). Please ensure
                you have read and understood those rules before using the plugin!<br>
                <b>Access Restriction</b> shall bring an automatic system,
                which allows a server admin to set limitations in server-access.
                The plugin contains four mechanisms:</p>
                <h3>RankKicker</h3>
                <p>RankKicker kicks players, whose rank is higher than the
                one defined by the server admin.
                Thus, providing a 'noob only'-Server shall get much
                easier than it was before.</p>
                <h3>AccessLimitation</h3>
                <p>AccessLimitation lets the admin set a list of allowed players
                and/or Clantags. Any other player, who joins the server and is
                not in one of those lists (if both lists are activated,
                a player just needs to have either a valid Clantag or a valid name)
                , will get kicked or banned.</p>
                <h3>ClanTagModeration</h3>
                <p>ClanTagModeration can be used to prevent the abuse of a Clantag.
                Once a Clantag is added to the list of moderated Clantags, each
                Player allowed to wear the tag must be added to a seperate list.
                If a player joins the server, wearing a specific tag, but not being
                in the list of allowed clan players, he will get kicked or banned.
                </p>
                <h3>DisallowedClanTags</h3>
                <p>DisallowedClanTags provides the feature to keep players wearing a specific
                Clantag off your server. The administrator can choose whether to kick or ban
                those players, and thus saves a lot of time adding new players to his banlist.</p>

                <h2>Setup & Configuration</h2>
                <ul>
                <li><b>RoC read and accepted</b>: makes sure the admin has read the EA RoC. The plugin can't be used without this variable being set to 'Yes'</li>
                <li><b>Rank Kicker</b>: activates/deactivates the RankKicker</li>
                <li><b>Access Limitation</b>: activates/deactivates the AccessLimitation</li>
                <li><b>Clan Tag Moderation</b>: activates/deactivates the ClanTagModeration</li>
                <li><b>Disallowed Clan Tags</b>: activates/deactivates the DisallowedClanTags</li>
                <li><b>Ban Player's GUID?</b>: ban a player using his GUID, which prevents him from joining with another soldier. This setting is used for all mechanisms of Access Restriction</li>
                <li><b>Show actions in plugin console?</b>: when turned off, Access Restriction won't display its actions in the plugin console</li>
                <li><b>Clear Cache automatically? (Recommended!)</b>: toggle whether Access Restriction should delete the stored data automatically. This is recommended to reduce lags when using RankKicker, since all kicks and GUIDs - as well as the unused stats - get deleted and thus, the needed memory is reduced.</li>
                <li><b>Rounds before Cache-Clearing</b>: number of ingame-rounds before data will be removed</li>
                <li><b>Clear data now?</b>: forces a clearing of data. This can be used if you experience lags within a short period of time and don't want to wait for the         automatic cleanup. Please note that the variable's value won't change when clicking 'Yes'. Access Restriction will just verify the clearing of the data inside the plugin console.</li>
                </ul>
                <h3>RankKicker</h3>
                <p>
                Before activating RankKicker, please make sure your PRoCon is configured correctly to allow stats-fetching.
                This can either be done by disabling the sandbox-mode for plugins<br>(Tools->Options->Plugins->Plugin security->'Run plugins with no restrictions')<br>
                or adding 'http://api.bfbcs.com:80' to the list of trusted hosts/domains<br>(...->'Run plugins in a sandbox (recommendend)'->Trusted host/domain->'http://api.bfbcs.com'->Port->'80')<br>
                Please note that PRoCon must be restarted to change those settings.
                <br>
                If you own an hosted PRoCon-Layer, you must contact your hoster and ask for these changes.</p>
                <ul>
                <li><b>Plugin active? (Ingame-Command)</b>: used to turn the plugin on/off via ingame chat (see ingame-commands)</li>
                <li><b>Rank Limit</b>: if a player's rank is equal or greater than the set value, he will get kicked/banned</li>
                <li><b>White List</b>: ingame names of players, who are excluded from the RankKicker</li>
                <li><b>Allowed certain Clan Tags?</b>: allow certain clan tags to pass the RankKicker-check</li>
                <li><b>White List Clan Tags</b>: clan tags, which are excluded from the RankKicker</li>
                <li><b>Ban Players?</b>: toggle whether player should be kicked or banned</li>
                <li><b>Permanently Ban Players?</b>: toggle whether player should be temporarely or permanently banned</li>
                <li><b>Player Ban Time</b>: set the minutes for a timeban</li>
                <li><b>Ban Player after X Kicks?</b>: player gets permamently banned after being kicked for X times</li>
                <li><b>Number of Kicks before Ban</b>: number of kicks executed before a player will be permanently banned</li>
                <li><b>Kick Reason</b>: message displayed to a player when he is kicked by RankKicker</li>
                <li><b>Ban Reason</b>: message displayed to a player when he is temporarely banned by RankKicker</li>
                <li><b>Perm Ban Reason</b>: message displayed to a player when he is banned permamently by RankKicker</li>
                </ul>
                <h3>AccessLimitation</h3>
                <ul>
                <li><b>Allow Access via Playername?</b>: toggles the scanning for allowed playernames</li>
                <li><b>Allowed Access via Clantag?</b>: toggles the scanning for allowed clantags</li>
                <li><b>Ban disallowed Players?</b>: if 'No' is chosen, disallowed players will just be kicked. if 'Yes' is chosen, disallowed players will be banned permanently</li>
                <li><b>Allowed Players</b>: ingame name of players, who are allowed to play on the server</li>
                <li><b>Allowed Clantags</b>: clantags, which are allowed on the server</li>
                <li><b>Access Kick Reason</b>: message displayed to a player when he is kicked by AccessLimitation</li>
                <li><b>Access Ban Reason</b>: message displayed to a player when he is banned permanently by AccessLimitation</li>
                </ul>
                <h3>ClanTagModeration</h3>
                <ul>
                <li><b>Moderated Clan Tags</b>: list of clantags, which should be checked</li>
                <li><b>Allowed Clan Players</b>: list of players, who are allowed to wear one of the clantags</li>
                <li><b>Ban disallowed Clan Players?</b>: if 'No' is chosen, disallowed Clan players will just be kicked. if 'Yes' is chosen, disallowed Clan players will be banned permanently</li>
                <li><b>Clan Tag Kick Reason</b>: message displayed to a player when he is kicked by ClanTagModeration</li>
                <li><b>Clan Tag Ban Reason</b>: message displayed to a player when he is banned permanently by ClanTagModeration</li>
                </ul>
                <h3>DisallowedClanTags</h3>
                <ul>
                <li><b>Ban disallowed Clan Players?</b>: toggle whether player should be kicked or banned</li>
                <li><b>Blacklist</b>: list of clantags, which are not allowed on this server</li>
                <li><b>Disallowed Clan Kick Reason</b>: message displayed to a player when he is kicked by DisallowedClanTags</li>
                <li><b>Disallowed Clan Ban Reason</b>: message displayed to a player when he is banned permanently by DisallowedClanTags</li>
                </ul>

                <h2>Ingame-commands</h2>
                <br>
                <h3>RankKicker</h3>
                <p>
                Use '@rk' to interact with the (running) RankKicker plugin. You can find the list of available commands below.
                <br>
                Please remember that RankKicker won't react to your ingame-commands when disabled in your PRoCon, I recommend
                you turn it off by toggling the 'Plugin active? (Ingame-Command)'-Variable or via use of ingame-chat. Note that the variable shown
                in the PRoCon-window won't change when you turn off RankKicker via ingame-chat, the plugin's functionality will be turned off anyways.
                The safest way to prevent mistakes is to toggle the variable in PRoCon too.</p>
                <h4>Available commands</h4>
                <ul>
                <li><b>@rk on/1</b>: activates RankKicker's functionality. You can either use '@rk on' or '@rk 1'</li>
                <li><b>@rk off/0</b>: deactivates RankKicker's functionality. You can either use '@rk off' or '@rk 0'</li>
                <li><b>@rk clear</b>: clears the number of kicks per player, which is stored by the plugin</li>
                <li><b>@rk check</b>: forces RankKicker to check all players</li>
                </ul>

                <h2>TODO-List</h2>
                <ul>
                <li>add mySQL-Support to store data</li>
                <li>further ingamecommands (alter plugin variables)</li>
                <li>check player stats for last update</li>
                </ul>";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;

            this.RegisterEvents(this.GetType().Name, "OnPlayerJoin", "OnPlayerAuthenticated", "OnServerInfo", "OnListPlayers", "OnLevelStarted");
        }

        public void OnPluginEnable()
        {
            this.serverName = "";
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAccessRestriction ^2Enabled!");
            this.m_IsPluginEnabled = true;
            this.m_RoundsCacheClear = 0;
            this.RegisterAllCommands();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAccessRestriction ^1Disabled =(");
            this.m_IsPluginEnabled = false;
            this.UnregisterAllCommands();
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("*Access Restriction*|RoC read and accepted", typeof(enumBoolYesNo), this.m_RoCAccepted));
            if (this.m_RoCAccepted == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("*Access Restriction*|RankKicker", typeof(enumBoolOnOff), this.m_RankKicker));
                lstReturn.Add(new CPluginVariable("*Access Restriction*|Access Limitation", typeof(enumBoolOnOff), this.m_AccessLimitation));
                lstReturn.Add(new CPluginVariable("*Access Restriction*|Clan Tag Moderation", typeof(enumBoolOnOff), this.m_ClanTagModeration));
                lstReturn.Add(new CPluginVariable("*Access Restriction*|Disallowed Clan Tags", typeof(enumBoolOnOff), this.m_DisallowedTags));
                lstReturn.Add(new CPluginVariable("*Access Restriction*|Ban Player's GUID?", typeof(enumBoolYesNo), this.m_BanGUID));
                lstReturn.Add(new CPluginVariable("*Access Restriction*|Show actions in plugin console?", typeof(enumBoolYesNo), this.m_ConsoleLog));
                lstReturn.Add(new CPluginVariable("*Access Restriction*|Clear Cache automatically? (Recommended!)", typeof(enumBoolYesNo), this.m_AutomaticallyClearCache));
                lstReturn.Add(new CPluginVariable("*Access Restriction*|Clear data now?", typeof(enumBoolYesNo), this.m_ClearAll));
                if (this.m_AutomaticallyClearCache == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("*Access Restriction*|Rounds before Cache-Clearing", typeof(int), this.m_CacheClearRounds));
                }
                if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On)
                {
                    lstReturn.Add(new CPluginVariable("RankKicker Settings|RankKicker active? (Ingame-Command)", typeof(enumBoolOnOff), this.m_RankKickerOnOff));
                    if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On && this.m_RankKickerOnOff == enumBoolOnOff.On)
                    {
                        lstReturn.Add(new CPluginVariable("RankKicker Settings|Rank Limit", typeof(int), this.m_RankLimit));
                        lstReturn.Add(new CPluginVariable("RankKicker Settings|Ban Players?", typeof(enumBoolYesNo), this.m_TempBanPlayers));
                        lstReturn.Add(new CPluginVariable("RankKicker Settings|White List", typeof(string[]), this.m_WhiteList.ToArray()));
                        lstReturn.Add(new CPluginVariable("RankKicker Settings|Allowed certain Clan Tags?", typeof(enumBoolYesNo), this.m_ClanTagWhiteList));
                        if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On && this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_ClanTagWhiteList == enumBoolYesNo.Yes)
                        {
                            lstReturn.Add(new CPluginVariable("RankKicker Settings|White List Clan Tags", typeof(string[]), this.m_WhiteListClanTags.ToArray()));
                        }
                        if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On && this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_TempBanPlayers == enumBoolYesNo.Yes)
                        {
                            lstReturn.Add(new CPluginVariable("RankKicker Settings|Permanently Ban Players?", typeof(enumBoolYesNo), this.m_PermBanPlayers));
                        }
                        if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On && this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_TempBanPlayers == enumBoolYesNo.Yes && this.m_PermBanPlayers == enumBoolYesNo.No)
                        {
                            lstReturn.Add(new CPluginVariable("RankKicker Settings|Player Ban Time (minutes)", this.m_TempBanTime.GetType(), this.m_TempBanTime));
                        }
                        if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On && this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_TempBanPlayers == enumBoolYesNo.No && this.m_PermBanPlayers == enumBoolYesNo.No)
                        {
                            lstReturn.Add(new CPluginVariable("RankKicker Settings|Ban Player after X Kicks?", typeof(enumBoolYesNo), this.m_BanAfterKicks));
                        }
                        if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On && this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_BanAfterKicks == enumBoolYesNo.Yes && this.m_TempBanPlayers == enumBoolYesNo.No && this.m_PermBanPlayers == enumBoolYesNo.No)
                        {
                            lstReturn.Add(new CPluginVariable("RankKicker Settings|Number of Kicks before Ban", typeof(int), this.m_KicksBeforeBan));
                        }
                        if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On && this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_TempBanPlayers == enumBoolYesNo.No && this.m_PermBanPlayers == enumBoolYesNo.No)
                        {
                            lstReturn.Add(new CPluginVariable("RankKicker Settings|Kick Reason", typeof(string), this.m_KickReason));
                        }
                        else if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On && this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_TempBanPlayers == enumBoolYesNo.Yes && this.m_PermBanPlayers == enumBoolYesNo.No)
                        {
                            lstReturn.Add(new CPluginVariable("RankKicker Settings|Ban Reason", typeof(string), this.m_BanReason));
                        }
                        else if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_RankKicker == enumBoolOnOff.On && this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_TempBanPlayers == enumBoolYesNo.Yes && this.m_PermBanPlayers == enumBoolYesNo.Yes)
                        {
                            lstReturn.Add(new CPluginVariable("RankKicker Settings|Perm Ban Reason", typeof(string), this.m_PermBanReason));
                        }
                    }
                }
                if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_AccessLimitation == enumBoolOnOff.On)
                {
                    lstReturn.Add(new CPluginVariable("Access Limitation|Allow Access via Playername?", typeof(enumBoolYesNo), this.m_AccessLimitationPlayers));
                    lstReturn.Add(new CPluginVariable("Access Limitation|Allow Access via Clantag?", typeof(enumBoolYesNo), this.m_AccessLimitationTags));
                    if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_AccessLimitation == enumBoolOnOff.On && this.m_AccessLimitationPlayers == enumBoolYesNo.Yes)
                    {
                        lstReturn.Add(new CPluginVariable("Access Limitation|Allowed Players", typeof(string[]), this.m_AllowedPlayers.ToArray()));
                    }
                    if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_AccessLimitation == enumBoolOnOff.On && this.m_AccessLimitationTags == enumBoolYesNo.Yes)
                    {
                        lstReturn.Add(new CPluginVariable("Access Limitation|Allowed Clantags", typeof(string[]), this.m_AllowedClanTags.ToArray()));
                    }
                    lstReturn.Add(new CPluginVariable("Access Limitation|Ban disallowed Players?", typeof(enumBoolYesNo), this.m_AccessBanPlayers));
                    if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_AccessLimitation == enumBoolOnOff.On && this.m_AccessBanPlayers == enumBoolYesNo.No)
                    {
                        lstReturn.Add(new CPluginVariable("Access Limitation|Access Kick Reason", typeof(string), this.m_AccessKickReason));
                    }
                    else if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_AccessLimitation == enumBoolOnOff.On && this.m_AccessBanPlayers == enumBoolYesNo.Yes)
                    {
                        lstReturn.Add(new CPluginVariable("Access Limitation|Access Ban Reason", typeof(string), this.m_AccessPermBanReason));
                    }
                }
                if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_ClanTagModeration == enumBoolOnOff.On)
                {
                    lstReturn.Add(new CPluginVariable("Clan Tag Moderation|Moderated Clan Tags", typeof(string[]), this.m_ModeratedClanTags.ToArray()));
                    lstReturn.Add(new CPluginVariable("Clan Tag Moderation|Allowed Clan Players", typeof(string[]), this.m_AllowedClanPlayers.ToArray()));
                    lstReturn.Add(new CPluginVariable("Clan Tag Moderation|Ban disallowed Clan Players?", typeof(enumBoolYesNo), this.m_ClanTagBanPlayers));
                    if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_ClanTagModeration == enumBoolOnOff.On && this.m_ClanTagBanPlayers == enumBoolYesNo.No)
                    {
                        lstReturn.Add(new CPluginVariable("Clan Tag Moderation|Clan Tag Kick Reason", typeof(string), this.m_ClanTagKickReason));
                    }
                    else if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_ClanTagModeration == enumBoolOnOff.On && this.m_ClanTagBanPlayers == enumBoolYesNo.Yes)
                    {
                        lstReturn.Add(new CPluginVariable("Clan Tag Moderation|Clan Tag Ban Reason", typeof(string), this.m_ClanTagPermBanReason));
                    }
                }
                if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_DisallowedTags == enumBoolOnOff.On)
                {
                    lstReturn.Add(new CPluginVariable("Disallowed Clan Tags|Ban disallowed Clan Players?", typeof(enumBoolYesNo), this.m_DisallowedTagsBan));
                    lstReturn.Add(new CPluginVariable("Disallowed Clan Tags|Black List", typeof(string[]), this.m_DisallowedClanTags.ToArray()));
                    if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_DisallowedTags == enumBoolOnOff.On && this.m_DisallowedTagsBan == enumBoolYesNo.No)
                    {
                        lstReturn.Add(new CPluginVariable("Disallowed Clan Tags|Disallowed Clan Kick Reason", typeof(string), this.m_DisallowedTagsKickReason));
                    }
                    else if (this.m_RoCAccepted == enumBoolYesNo.Yes && this.m_DisallowedTags == enumBoolOnOff.On && this.m_DisallowedTagsBan == enumBoolYesNo.Yes)
                    {
                        lstReturn.Add(new CPluginVariable("Disallowed Clan Tags|Disallowed Clan Ban Reason", typeof(string), this.m_DisallowedTagsBanReason));
                    }
                }
            }

            return lstReturn;
        }


        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("RoC read and accepted", typeof(enumBoolYesNo), this.m_RoCAccepted));
            lstReturn.Add(new CPluginVariable("Ban Player's GUID?", typeof(enumBoolYesNo), this.m_BanGUID));
            lstReturn.Add(new CPluginVariable("RankKicker", typeof(enumBoolOnOff), this.m_RankKicker));
            lstReturn.Add(new CPluginVariable("Access Limitation", typeof(enumBoolOnOff), this.m_AccessLimitation));
            lstReturn.Add(new CPluginVariable("Clan Tag Moderation", typeof(enumBoolOnOff), this.m_ClanTagModeration));
            lstReturn.Add(new CPluginVariable("Disallowed Clan Tags", typeof(enumBoolOnOff), this.m_DisallowedTags));
            lstReturn.Add(new CPluginVariable("RankKicker active? (Ingame-Command)", typeof(enumBoolOnOff), this.m_RankKickerOnOff));
            lstReturn.Add(new CPluginVariable("Show actions in plugin console?", typeof(enumBoolYesNo), this.m_ConsoleLog));
            lstReturn.Add(new CPluginVariable("Rank Limit", typeof(int), this.m_RankLimit));
            lstReturn.Add(new CPluginVariable("Ban Players?", typeof(enumBoolYesNo), this.m_TempBanPlayers));
            lstReturn.Add(new CPluginVariable("White List", typeof(string[]), this.m_WhiteList.ToArray()));
            lstReturn.Add(new CPluginVariable("Allowed certain Clan Tags?", typeof(enumBoolYesNo), this.m_ClanTagWhiteList));
            lstReturn.Add(new CPluginVariable("White List Clan Tags", typeof(string[]), this.m_WhiteListClanTags.ToArray()));
            lstReturn.Add(new CPluginVariable("Clear data now?", typeof(enumBoolYesNo), this.m_ClearAll));
            lstReturn.Add(new CPluginVariable("Permanently Ban Players?", typeof(enumBoolYesNo), this.m_PermBanPlayers));
            lstReturn.Add(new CPluginVariable("Player Ban Time (minutes)", this.m_TempBanTime.GetType(), this.m_TempBanTime));
            lstReturn.Add(new CPluginVariable("Ban Player after X Kicks?", typeof(enumBoolYesNo), this.m_BanAfterKicks));
            lstReturn.Add(new CPluginVariable("Number of Kicks before Ban", typeof(int), this.m_KicksBeforeBan));
            lstReturn.Add(new CPluginVariable("Kick Reason", typeof(string), this.m_KickReason));
            lstReturn.Add(new CPluginVariable("Ban Reason", typeof(string), this.m_BanReason));
            lstReturn.Add(new CPluginVariable("Permanent Ban Reason", typeof(string), this.m_PermBanReason));
            lstReturn.Add(new CPluginVariable("Allow Access via Playername?", typeof(enumBoolYesNo), this.m_AccessLimitationPlayers));
            lstReturn.Add(new CPluginVariable("Allow Access via Clantag?", typeof(enumBoolYesNo), this.m_AccessLimitationTags));
            lstReturn.Add(new CPluginVariable("Allowed Players", typeof(string[]), this.m_AllowedPlayers.ToArray()));
            lstReturn.Add(new CPluginVariable("Allowed Clantags", typeof(string[]), this.m_AllowedClanTags.ToArray()));
            lstReturn.Add(new CPluginVariable("Ban disallowed Players?", typeof(enumBoolYesNo), this.m_AccessBanPlayers));
            lstReturn.Add(new CPluginVariable("Access Kick Reason", typeof(string), this.m_AccessKickReason));
            lstReturn.Add(new CPluginVariable("Access Ban Reason", typeof(string), this.m_AccessPermBanReason));
            lstReturn.Add(new CPluginVariable("Moderated Clan Tags", typeof(string[]), this.m_ModeratedClanTags.ToArray()));
            lstReturn.Add(new CPluginVariable("Allowed Clan Players", typeof(string[]), this.m_AllowedClanPlayers.ToArray()));
            lstReturn.Add(new CPluginVariable("Ban disallowed Clan Players?", typeof(enumBoolYesNo), this.m_ClanTagBanPlayers));
            lstReturn.Add(new CPluginVariable("Clan Tag Kick Reason", typeof(string), this.m_ClanTagKickReason));
            lstReturn.Add(new CPluginVariable("Clan Tag Ban Reason", typeof(string), this.m_ClanTagPermBanReason));
            lstReturn.Add(new CPluginVariable("Ban disallowed Clan Players?", typeof(enumBoolYesNo), this.m_DisallowedTagsBan));
            lstReturn.Add(new CPluginVariable("Black List", typeof(string[]), this.m_DisallowedClanTags.ToArray()));
            lstReturn.Add(new CPluginVariable("Disallowed Clan Kick Reason", typeof(string), this.m_DisallowedTagsKickReason));
            lstReturn.Add(new CPluginVariable("Disallowed Clan Ban Reason", typeof(string), this.m_DisallowedTagsBanReason));
            lstReturn.Add(new CPluginVariable("Clear Cache automatically? (Recommended!)", typeof(enumBoolYesNo), this.m_AutomaticallyClearCache));
            lstReturn.Add(new CPluginVariable("Rounds before Cache-Clearing", typeof(int), this.m_CacheClearRounds));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int MaxRank = 0;
            int iTimeMinutes = 0;
            int KickBanValue = 0;
            int Rounds = 0;
            enumBoolYesNo clear = enumBoolYesNo.No;

            if (strVariable.CompareTo("RoC read and accepted") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_RoCAccepted = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Ban Player's GUID?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_BanGUID = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("RankKicker") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.m_RankKicker = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Access Limitation") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.m_AccessLimitation = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Clan Tag Moderation") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.m_ClanTagModeration = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Disallowed Clan Tags") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.m_DisallowedTags = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("RankKicker active? (Ingame-Command)") == 0 && Enum.IsDefined(typeof(enumBoolOnOff), strValue) == true)
            {
                this.m_RankKickerOnOff = (enumBoolOnOff)Enum.Parse(typeof(enumBoolOnOff), strValue);
            }
            else if (strVariable.CompareTo("Show actions in plugin console?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ConsoleLog = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Rank Limit") == 0 && int.TryParse(strValue, out MaxRank) == true)
            {
                this.m_RankLimit = int.Parse(strValue);
            }
            else if (strVariable.CompareTo("White List") == 0)
            {
                this.m_WhiteList = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Allowed certain Clan Tags?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ClanTagWhiteList = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("White List Clan Tags") == 0)
            {
                this.m_WhiteListClanTags = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Clear data now?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                clear = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (clear == enumBoolYesNo.Yes)
                {
                    Clear(1);
                    this.m_ClearAll = enumBoolYesNo.No;
                }
            }
            else if (strVariable.CompareTo("Ban Players?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_TempBanPlayers = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Permanently Ban Players?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_PermBanPlayers = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Player Ban Time (minutes)") == 0 && int.TryParse(strValue, out iTimeMinutes) == true)
            {
                if (iTimeMinutes > 0)
                {
                    this.m_TempBanTime = iTimeMinutes;
                }
            }
            else if (strVariable.CompareTo("Ban Player after X Kicks?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_BanAfterKicks = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Number of Kicks before Ban") == 0 && int.TryParse(strValue, out KickBanValue) == true)
            {
                this.m_KicksBeforeBan = KickBanValue;
            }
            else if (strVariable.CompareTo("Kick Reason") == 0)
            {
                this.m_KickReason = strValue;
            }
            else if (strVariable.CompareTo("Ban Reason") == 0)
            {
                this.m_BanReason = strValue;
            }
            else if (strVariable.CompareTo("Permanent Ban Reason") == 0)
            {
                this.m_PermBanReason = strValue;
            }
            else if (strVariable.CompareTo("Allow Access via Playername?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_AccessLimitationPlayers = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Allow Access via Clantag?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_AccessLimitationTags = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Allowed Players") == 0)
            {
                this.m_AllowedPlayers = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Allowed Clantags") == 0)
            {
                this.m_AllowedClanTags = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Ban disallowed Players?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_AccessBanPlayers = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Access Kick Reason") == 0)
            {
                this.m_AccessKickReason = strValue;
            }
            else if (strVariable.CompareTo("Access Ban Reason") == 0)
            {
                this.m_AccessPermBanReason = strValue;
            }
            else if (strVariable.CompareTo("Moderated Clan Tags") == 0)
            {
                this.m_ModeratedClanTags = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Allowed Clan Players") == 0)
            {
                this.m_AllowedClanPlayers = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Ban dísallowed Clan Players?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_ClanTagBanPlayers = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Clan Tag Kick Reason") == 0)
            {
                this.m_ClanTagKickReason = strValue;
            }
            else if (strVariable.CompareTo("Clan Tag Ban Reason") == 0)
            {
                this.m_ClanTagPermBanReason = strValue;
            }
            else if (strVariable.CompareTo("Ban disallowed Clan Players?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_DisallowedTagsBan = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Black List") == 0)
            {
                this.m_DisallowedClanTags = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            else if (strVariable.CompareTo("Disallowed Clan Kick Reason") == 0)
            {
                this.m_DisallowedTagsKickReason = strValue;
            }
            else if (strVariable.CompareTo("Disallowed Clan Ban Reason") == 0)
            {
                this.m_DisallowedTagsBanReason = strValue;
            }
            else if (strVariable.CompareTo("Clear Cache automatically? (Recommended!)") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.m_AutomaticallyClearCache = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Rounds before Cache-Clearing") == 0 && int.TryParse(strValue, out Rounds) == true)
            {
                this.m_CacheClearRounds = int.Parse(strValue);
            }

            if (this.m_ConsoleLog == enumBoolYesNo.Yes && clear == enumBoolYesNo.No)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0Plugin variable '{0}' set to '{1}'", strVariable, strValue));
            }
        }

        private List<string> GetExcludedCommandStrings(string strAccountName)
        {
            List<string> lstReturnCommandStrings = new List<string>();

            List<MatchCommand> lstCommands = this.GetRegisteredCommands();

            CPrivileges privileges = this.GetAccountPrivileges(strAccountName);

            foreach (MatchCommand mtcCommand in lstCommands)
            {
                if (mtcCommand.Requirements.HasValidPermissions(privileges) == true && lstReturnCommandStrings.Contains(mtcCommand.Command) == false)
                {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }

            return lstReturnCommandStrings;
        }

        private List<string> GetCommandStrings()
        {
            List<string> lstReturnCommandStrings = new List<string>();

            List<MatchCommand> lstCommands = this.GetRegisteredCommands();

            foreach (MatchCommand mtcCommand in lstCommands)
            {
                if (lstReturnCommandStrings.Contains(mtcCommand.Command) == false)
                {
                    lstReturnCommandStrings.Add(mtcCommand.Command);
                }
            }

            return lstReturnCommandStrings;
        }

        private void UnregisterAllCommands()
        {
            this.UnregisterCommand(new MatchCommand("AccessRestriction", "OnCommandToggle", this.Listify<string>("@", "!", "#"), "rk", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Account, "You do not have enough privileges to toggle AccessRestriction"), "Toggle AccessRestriction settings"));
        }

        private void SetupHelpCommands()
        {

        }

        private void RegisterAllCommands()
        {
            if (this.m_IsPluginEnabled == true)
            {
                this.SetupHelpCommands();

                if (true)
                {
                    this.RegisterCommand(new MatchCommand("AccessRestriction", "OnCommandToggle", this.Listify<string>("@", "!", "#"), "rk", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Account, "You do not have enough privileges to toggle AccessRestriction"), "Toggle AccessRestriction settings"));
                }
                else
                {
                    this.UnregisterCommand(new MatchCommand("AccessRestriction", "OnCommandToggle", this.Listify<string>("@", "!", "#"), "rk", this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Account, "You do not have enough privileges to toggle AccessRestriction"), "Toggle AccessRestriction settings"));
                }
            }
        }

        #endregion

        #region Used Interfaces

        public void OnPlayerJoin(string strSoldierName)
        {
            if (this.m_RoCAccepted == enumBoolYesNo.Yes)
            {
                if (this.m_RankKicker == enumBoolOnOff.On)
                {
                    if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_ClanTagWhiteList == enumBoolYesNo.No)
                    {
                        if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_PlayerKicks.ContainsKey(strSoldierName) == false && this.m_WhiteList.Contains(strSoldierName) == false)
                        {
                            this.m_PlayerKicks.Add(strSoldierName, 0);
                        }
                        else if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_PlayerKicks.ContainsKey(strSoldierName) == true && this.m_WhiteList.Contains(strSoldierName) == false)
                        {
                            if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_PlayerKicks[strSoldierName] > 0 && this.m_PlayerKicks[strSoldierName] < this.m_KicksBeforeBan && this.m_BanAfterKicks == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", strSoldierName, this.m_KickReason);
                                this.m_PlayerKicks[strSoldierName]++;
                                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' kicked due to a too high rank!", strSoldierName));
                                if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0RankKicker-Player '{0}' kicked. Number of kicks: {1}", strSoldierName, this.m_PlayerKicks[strSoldierName]));
                                }
                            }
                            else if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_PlayerKicks[strSoldierName] > 0 && this.m_PlayerKicks[strSoldierName] >= this.m_KicksBeforeBan && this.m_BanAfterKicks == enumBoolYesNo.Yes)
                            {
                                if (this.m_BanGUID == enumBoolYesNo.No || this.m_PlayerGUIDs.ContainsKey(strSoldierName) == false)
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSoldierName, "perm", this.m_PermBanReason);
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_PlayerGUIDs[strSoldierName], "perm", this.m_PermBanReason);
                                }
                                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' banned permanently due to a too high rank!", strSoldierName));
                                if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0RankKicker-Player '{0}' banned permanently.", strSoldierName));
                                }
                            }
                        }
                        // TO-DO: check stats for last update
                        /*if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_LastUpdated.ContainsKey(strSoldierName) == false && this.m_WhiteList.Contains(strSoldierName) == false)
                        {
                            this.m_LastUpdated.Add(strSoldierName, 0);
                        }*/
                    }
                }

                if (this.m_AccessLimitation == enumBoolOnOff.On)
                {
                    if (this.m_AccessLimitationPlayers == enumBoolYesNo.Yes && this.m_AccessLimitationPlayers == enumBoolYesNo.No)
                    {
                        if (this.m_AllowedPlayers.Contains(strSoldierName) != true)
                        {
                            if (this.m_AccessBanPlayers == enumBoolYesNo.No)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", strSoldierName, this.m_AccessKickReason);
                                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' kicked for not being on the list of allowed players!", strSoldierName));
                                if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0AccessLimitation-Player '{0}' kicked.", strSoldierName));
                                }
                            }
                            else if (this.m_AccessBanPlayers == enumBoolYesNo.Yes)
                            {
                                this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSoldierName, "perm", this.m_AccessPermBanReason);
                                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' banned permanently for not being on the list of allowed players!", strSoldierName));
                                if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0AccessLimitation-Player '{0}' banned permanently.", strSoldierName));
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {
            if (this.m_RoCAccepted == enumBoolYesNo.Yes)
            {
                if (this.m_PlayerGUIDs.ContainsKey(strSoldierName) == false)
                {
                    this.m_PlayerGUIDs.Add(strSoldierName, strGuid);
                }
            }
        }

        public void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.serverName = csiServerInfo.ServerName;
            this.ServerInfo = csiServerInfo;
        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            this.lstPlayerList = lstPlayers;
            if (this.m_RoCAccepted == enumBoolYesNo.Yes)
            {
                foreach (CPlayerInfo info in lstPlayers)
                {
                    if (this.m_RankKicker == enumBoolOnOff.On)
                    {
                        if (this.m_RankKickerOnOff == enumBoolOnOff.On)
                        {
                            if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_PlayerKicks.ContainsKey(info.SoldierName) == false && this.m_WhiteList.Contains(info.SoldierName) == false && this.m_WhiteListClanTags.Contains(info.ClanTag) == false)
                            {
                                this.m_PlayerKicks.Add(info.SoldierName, 0);
                            }
                            else if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_PlayerKicks.ContainsKey(info.SoldierName) == true && this.m_WhiteList.Contains(info.SoldierName) == false)
                            {
                                if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_PlayerKicks[info.SoldierName] > 0 && this.m_PlayerKicks[info.SoldierName] < this.m_KicksBeforeBan && this.m_BanAfterKicks == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", info.SoldierName, this.m_KickReason);
                                    this.m_PlayerKicks[info.SoldierName]++;
                                    this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' kicked due to a too high rank!", info.SoldierName));
                                    if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0RankKicker-Player '{0}' kicked. Number of kicks: {1}", info.SoldierName, this.m_PlayerKicks[info.SoldierName]));
                                    }
                                }
                                else if (this.m_RankKickerOnOff == enumBoolOnOff.On && this.m_PlayerKicks[info.SoldierName] > 0 && this.m_PlayerKicks[info.SoldierName] >= this.m_KicksBeforeBan && this.m_BanAfterKicks == enumBoolYesNo.Yes)
                                {
                                    if (this.m_BanGUID == enumBoolYesNo.No || this.m_PlayerGUIDs.ContainsKey(info.SoldierName) == false)
                                    {
                                        this.ExecuteCommand("procon.protected.send", "banList.add", "name", info.SoldierName, "perm", this.m_PermBanReason);
                                    }
                                    else
                                    {
                                        this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_PlayerGUIDs[info.SoldierName], "perm", this.m_PermBanReason);
                                    }
                                    this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' banned permanently due to a too high rank!", info.SoldierName));
                                    if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0RankKicker-Player '{0}' banned permanently.", info.SoldierName));
                                    }
                                }
                            }

                            PlayerStats? stats = getBFBCStats(info.SoldierName);
                            if (stats == null)
                                continue;

                            if (this.m_RankKickerOnOff == enumBoolOnOff.On && stats.Value.rank >= this.m_RankLimit && this.m_TempBanPlayers == enumBoolYesNo.No && this.m_PermBanPlayers == enumBoolYesNo.No && this.m_WhiteList.Contains(info.SoldierName) == false)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", info.SoldierName, this.m_KickReason);
                                this.m_PlayerKicks[info.SoldierName]++;
                                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' kicked due to a too high rank!", info.SoldierName));
                                if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0RankKicker-Player '{0}' kicked. Number of kicks: {1}", info.SoldierName, this.m_PlayerKicks[info.SoldierName]));
                                }
                            }
                            else if (this.m_RankKickerOnOff == enumBoolOnOff.On && stats.Value.rank >= this.m_RankLimit && this.m_TempBanPlayers == enumBoolYesNo.Yes && this.m_PermBanPlayers == enumBoolYesNo.No && this.m_WhiteList.Contains(info.SoldierName) == false)
                            {
                                if (this.m_BanGUID == enumBoolYesNo.No || this.m_PlayerGUIDs.ContainsKey(info.SoldierName) == false)
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "name", info.SoldierName, "seconds", (this.m_TempBanTime * 60).ToString(), this.m_BanReason);
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_PlayerGUIDs[info.SoldierName], "seconds", (this.m_TempBanTime * 60).ToString(), this.m_BanReason);
                                }
                                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' banned for {1} minutes due to a too high rank!", info.SoldierName, this.m_TempBanTime));
                                if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0RankKicker-Player '{0}' banned for {1} minutes.", info.SoldierName, this.m_TempBanTime));
                                }
                            }
                            else if (this.m_RankKickerOnOff == enumBoolOnOff.On && stats.Value.rank >= this.m_RankLimit && this.m_TempBanPlayers == enumBoolYesNo.Yes && this.m_PermBanPlayers == enumBoolYesNo.Yes && this.m_WhiteList.Contains(info.SoldierName) == false)
                            {
                                if (this.m_BanGUID == enumBoolYesNo.No || this.m_PlayerGUIDs.ContainsKey(info.SoldierName) == false)
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "name", info.SoldierName, "perm", this.m_PermBanReason);
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_PlayerGUIDs[info.SoldierName], "perm", this.m_PermBanReason);
                                }
                                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' banned permanently due to a too high rank!", info.SoldierName));
                                if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0RankKicker-Player '{0}' banned permanently.", info.SoldierName));
                                }
                            }
                        }
                    }

                    if (this.m_AccessLimitation == enumBoolOnOff.On)
                    {
                        if (this.m_AccessLimitationPlayers == enumBoolYesNo.Yes || this.m_AccessLimitationTags == enumBoolYesNo.Yes)
                        {
                            bool validPlayer;
                            if (this.m_AllowedPlayers.Contains(info.SoldierName) == true || this.m_AllowedClanTags.Contains(info.ClanTag) == true)
                            {
                                validPlayer = true;
                            }
                            else
                            {
                                validPlayer = false;
                            }
                            if (validPlayer != true)
                            {
                                if (this.m_AccessBanPlayers == enumBoolYesNo.No)
                                {
                                    this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", info.SoldierName, this.m_AccessKickReason);
                                    this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' kicked for not being on the list of allowed players!", info.SoldierName));
                                    if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0AccessLimitation-Player '{0}' kicked.", info.SoldierName));
                                    }
                                }
                                else if (this.m_AccessBanPlayers == enumBoolYesNo.Yes)
                                {
                                    if (this.m_BanGUID == enumBoolYesNo.No || this.m_PlayerGUIDs.ContainsKey(info.SoldierName) == false)
                                    {
                                        this.ExecuteCommand("procon.protected.send", "banList.add", "name", info.SoldierName, "perm", this.m_AccessPermBanReason);
                                    }
                                    else
                                    {
                                        this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_PlayerGUIDs[info.SoldierName], "perm", this.m_AccessPermBanReason);
                                    }
                                    this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' banned permanently for not being on the list of allowed players!", info.SoldierName));
                                    if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0AccessLimitation-Player '{0}' banned permanently.", info.SoldierName));
                                    }
                                }
                            }
                        }
                    }

                    if (this.m_ClanTagModeration == enumBoolOnOff.On)
                    {
                        if (this.m_ModeratedClanTags.Contains(info.ClanTag) == true)
                        {
                            if (this.m_AllowedClanPlayers.Contains(info.SoldierName) != true)
                            {
                                if (this.m_ClanTagBanPlayers == enumBoolYesNo.No)
                                {
                                    this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", info.SoldierName, this.m_ClanTagKickReason);
                                    this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' kicked for using a clantag by unfair means!", info.SoldierName));
                                    if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0ClanTagModeration-Player '{0}' kicked.", info.SoldierName));
                                    }
                                }
                                else if (this.m_ClanTagBanPlayers == enumBoolYesNo.Yes)
                                {
                                    if (this.m_BanGUID == enumBoolYesNo.No || this.m_PlayerGUIDs.ContainsKey(info.SoldierName) == false)
                                    {
                                        this.ExecuteCommand("procon.protected.send", "banList.add", "name", info.SoldierName, "perm", this.m_ClanTagPermBanReason);
                                    }
                                    else
                                    {
                                        this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_PlayerGUIDs[info.SoldierName], "perm", this.m_ClanTagPermBanReason);
                                    }
                                    this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' banned permanently for using a clantag by unfair means!", info.SoldierName));
                                    if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0ClanTagModeration-Player '{0}' banned permanently.", info.SoldierName));
                                    }
                                }
                            }
                        }
                    }

                    if (this.m_DisallowedTags == enumBoolOnOff.On)
                    {
                        if (this.m_DisallowedClanTags.Contains(info.ClanTag) == true)
                        {
                            if (this.m_DisallowedTagsBan == enumBoolYesNo.No)
                            {
                                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", info.SoldierName, this.m_DisallowedTagsKickReason);
                                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' kicked due to his clan not being welcome here!", info.SoldierName));
                                if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0DisallowedClanTags-Player '{0}' kicked.", info.SoldierName));
                                }
                            }
                            else if (this.m_DisallowedTagsBan == enumBoolYesNo.Yes)
                            {
                                if (this.m_BanGUID == enumBoolYesNo.No || this.m_PlayerGUIDs.ContainsKey(info.SoldierName) == false)
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "name", info.SoldierName, "perm", this.m_DisallowedTagsBanReason);
                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.send", "banList.add", "guid", this.m_PlayerGUIDs[info.SoldierName], "perm", this.m_DisallowedTagsBanReason);
                                }
                                this.ExecuteCommand("procon.protected.send", "admin.say", String.Format("Player '{0}' banned permanently due to his clan not being welcome here!", info.SoldierName));
                                if (this.m_ConsoleLog == enumBoolYesNo.Yes)
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^4AccessRestriction: ^0DisallowedClanTags-Player '{0}' banned permanently.", info.SoldierName));
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnLevelStarted()
        {
            if (this.m_AutomaticallyClearCache == enumBoolYesNo.Yes)
            {
                if (this.m_RoundsCacheClear < this.m_CacheClearRounds)
                {
                    this.m_RoundsCacheClear++;
                }
                else
                {
                    Clear(2);
                    this.m_RoundsCacheClear = 0;
                }
            }
        }

        #endregion

        #region In Game Commands

        public void OnCommandToggle(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (capCommand.ExtraArguments == "0" || capCommand.ExtraArguments == "off")
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "RankKicker disabled!", "all");
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^4AccessRestriction: ^0RankKicker disabled ingame!");
                this.m_RankKickerOnOff = enumBoolOnOff.Off;
            }
            else if (capCommand.ExtraArguments == "1" || capCommand.ExtraArguments == "on")
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "RankKicker enabled!", "all");
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^4AccessRestriction: ^0RankKicker enabled ingame!");
                this.m_RankKickerOnOff = enumBoolOnOff.On;
            }
            else if (capCommand.ExtraArguments == "clear")
            {
                Clear(0);
            }
            else if (capCommand.ExtraArguments == "check")
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "RankKicker checking all players!", "all");
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^4AccessRestriction: ^0RankKicker checking all players!");
                this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "Wrong arguments. Use '0'/'off', '1'/'on', 'clear' or 'check'!", "all");
            }
        }

        #endregion

        #region Methodes

        public void Clear(int x)
        {
            this.m_PlayerKicks.Clear();
            this.m_PlayerGUIDs.Clear();
            this.m_dicPbInfo.Clear();
            this.m_dicPlayers.Clear();
            System.GC.Collect();
            if (x == 0)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "AccessRestriction-Cache cleared!", "all");
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^4AccessRestriction: ^0Cache cleared ingame!");
            }
            else if (x == 1)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^4AccessRestriction: ^0Cache cleared!");
            }
            else
            {

            }
        }

        #endregion

    }
}