/*  Copyright 2010 Brazin

    www.MIAClan.net

    This file is part of Brazin's Plugins for BFBC2 PRoCon.

    Brazin's Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Brazin's Plugins for BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Brazin's Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
*/


using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class CBadNameKicker : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private string[] ma_Names;		//The list of bad names
        private string[] m_aNamesLiteral;
        private string[] m_ClanTagsOnly;
        private string[] ma_Whitelist;	//The whitelist for names that are immune to bad name kicking
        private string[] m_aStealth;
        private string m_strNameMessage;	//The message sent as an extra argument to the player kick to the kicked player   
        private string m_strClanTagMessage;
        private string m_strTagMessage;

        private Dictionary<string, string> lstNames = new Dictionary<string, string>();

        private enumBoolYesNo blDebug;  //This boolean variable is used to determine whether action will be taken or not    								

        public CBadNameKicker()
        {
            this.ma_Names = new string[] { "fuck", "fuk", "c0ck", "bitch", "queef", "n!g", "nigr", "fag", "phag", "neggar", "negger", "fux", "B==", "==B", "8==", "==8", };
            this.m_aNamesLiteral = new string[] { "ngr", "f.a.g", "niger" };
            this.m_aStealth = new string[] { "" };
            this.ma_Whitelist = new string[] { "Brazin" };
            this.m_ClanTagsOnly = new string[] { "=wck=", "k21c" };
            this.m_strNameMessage = "Your name contains '%bn%.'";
            this.m_strClanTagMessage = "Your clan tag contains '%bn%.'";
            this.m_strTagMessage = "Your clan is not welcome here.";
            this.blDebug = enumBoolYesNo.No;
        }

        public string GetPluginName()
        {
            return "Bad Name Kicker";
        }

        public string GetPluginVersion()
        {
            return "2.0.0.0";
        }

        public string GetPluginAuthor()
        {
            return "Brazin";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net or www.MIAClan.net";
        }

        public string GetPluginDescription()
        {
            return @"<h2>Description</h2>
    <p>This plugin checks the names of all players that enter server against several lists of bad names. It utilizies both literal matching and character matching to detect unwanted names and issues a kick against those players as soon as they are detected.</p>
<h3>Fields</h3>
    <blockquote><h4>Lists</h4>
        <blockquote><h4>Bad Names List</h4>This list uses the default name-checking method (Regular Expression match). For each bad name in the list it will search the player's name and clan tag, and if it finds a match of the bad name anywhere within the player's name or clan tag it will then move on to check the Whitelist. If no match is found in the Whitelist, that player will be kicked. This is the only list that runs through the whitelist before kicking because all other lists are run based on a literal match rather than this list's character match method.</blockquote>
        <blockquote><h4>Bad Names Literal List</h4> This list checks players' names and clan tags for a literal match of each bad name in the list. That is, if a player's name or clan tag does not exactly equal the bad name, then the plugin will do nothing. The only exception is that the bad names are not case sensitive. This list is great for checking for bad names or acronyms that are very short and are often found within normal names. It is also good for bad names that have characters that normally act more like wild cards in the normal bad names list (like periods and dollar signs).</blockquote>
        <blockquote><h4>Clan Tags List</h4>This list is reserved for literal checks on clan tags. It's good for removing members of clans you don't want in your server. I may actually add a function to allow people detected on this list to be automatically banned from your server so that they can't just remove the clan tag and rejoin.</blockquote>
        <blockquote><h4>Stealth List</h4>This list checks names only for literal matches. This list will kick players without giving them a reason. It's good strategy for keeping hackers out of your server who keep coming back under the same names despite how many times you ban them because they'll just think that you are kicking them out manually.</blockquote>
        <blockquote><h4>Whitelist</h4>When a bad name from the Bad Names List is detected within a player's name, their name is checked for entries in the whitelist. The whitelist is useful for preventing innocent players from being removed for having an innocent name that might contain a bad name. (Ex. Bad Names List Contains 'dick'. If somebody named 'Dickson' joins the server, the plugin will remove them because their name contains 'dick'. You can manually add the entry 'dickson'/'dickso'/'ickso' to the whitelist and that player will nolonger be kicked anymore.</blockquote></blockquote>
    <blockquote><h4>Kick Messages</h4>
    <blockquote><h4>Name Kick Message</h4>This message will be received by players kicked from both the Bad Names List and the Bad Names Literal List if their name contains a blacklisted word. Use %bn% to let players know what bad name string they were kicked for.</blockquote>
    <blockquote><h4>Clan Tag Kick Message</h4>This message will be received by players kicked from both the Bad Names List and the Bad Names Literal List if their clan tag contains a blacklisted word. Use %bn% to let players know what bad name string they were kicked for.</blockquote>
    <blockquote><h4>Clan Kick Message</h4>This message will be received by players kicked based on detection in only the Clag Tags List. Use %bn% to let players know what bad name string they were kicked for.</blockquote></blockquote>";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBad Name Kicker ^2Enabled!");
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerJoin", "OnPlayerLeft", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnPunkbusterPlayerInfo", "OnLoadingLevel");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bBad Name Kicker ^1Disabled =(");
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Lists|Bad Names List", this.ma_Names.GetType(), this.ma_Names));
            lstReturn.Add(new CPluginVariable("Lists|Bad Names Literal List", this.m_aNamesLiteral.GetType(), this.m_aNamesLiteral));
            lstReturn.Add(new CPluginVariable("Lists|Clan Tags List", this.m_ClanTagsOnly.GetType(), this.m_ClanTagsOnly));
            lstReturn.Add(new CPluginVariable("Lists|Stealth List", this.m_aStealth.GetType(), this.m_aStealth));
            lstReturn.Add(new CPluginVariable("Lists|Whitelist", this.ma_Whitelist.GetType(), this.ma_Whitelist));
            lstReturn.Add(new CPluginVariable("Messages|Name Kick Message", this.m_strNameMessage.GetType(), this.m_strNameMessage));
            lstReturn.Add(new CPluginVariable("Messages|Clan Tag Kick Message", this.m_strClanTagMessage.GetType(), this.m_strClanTagMessage));
            lstReturn.Add(new CPluginVariable("Messages|Clan Kick Message", this.m_strTagMessage.GetType(), this.m_strTagMessage));
            //lstReturn.Add(new CPluginVariable("Enable Debug Mode?", typeof(enumBoolYesNo), this.blDebug));
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Bad Names List", this.ma_Names.GetType(), this.ma_Names));
            lstReturn.Add(new CPluginVariable("Bad Names Literal List", this.m_aNamesLiteral.GetType(), this.m_aNamesLiteral));
            lstReturn.Add(new CPluginVariable("Clan Tags List", this.m_ClanTagsOnly.GetType(), this.m_ClanTagsOnly));
            lstReturn.Add(new CPluginVariable("Stealth List", this.m_aStealth.GetType(), this.m_aStealth));
            lstReturn.Add(new CPluginVariable("Whitelist", this.ma_Whitelist.GetType(), this.ma_Whitelist));
            lstReturn.Add(new CPluginVariable("Name Kick Message", this.m_strNameMessage.GetType(), this.m_strNameMessage));
            lstReturn.Add(new CPluginVariable("Clan Tag Kick Message", this.m_strClanTagMessage.GetType(), this.m_strClanTagMessage));
            lstReturn.Add(new CPluginVariable("Clan Kick Message", this.m_strTagMessage.GetType(), this.m_strTagMessage));
            lstReturn.Add(new CPluginVariable("Enable Debug Mode?", typeof(enumBoolYesNo), this.blDebug));
            return lstReturn; ;
        }

        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {


            if (strVariable.CompareTo("Bad Names List") == 0)
            {
                this.ma_Names = CPluginVariable.DecodeStringArray(strValue);
                lstNames.Clear();
            }
            else if (strVariable.CompareTo("Bad Names Literal List") == 0)
            {
                this.m_aNamesLiteral = CPluginVariable.DecodeStringArray(strValue);
                lstNames.Clear();
            }
            else if (strVariable.CompareTo("Clan Tags List") == 0)
            {
                this.m_ClanTagsOnly = CPluginVariable.DecodeStringArray(strValue);
                lstNames.Clear();
            }
            else if (strVariable.CompareTo("Stealth List") == 0)
            {
                this.m_aStealth = CPluginVariable.DecodeStringArray(strValue);
                lstNames.Clear();
            }
            else if (strVariable.CompareTo("Whitelist") == 0)
            {
                this.ma_Whitelist = CPluginVariable.DecodeStringArray(strValue);
                lstNames.Clear();
            }
            else if (strVariable.CompareTo("Enable Debug Mode?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blDebug = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Name Kick Message") == 0)
            {
                this.m_strNameMessage = strValue;
            }
            else if (strVariable.CompareTo("Clan Tag Kick Message") == 0)
            {
                this.m_strClanTagMessage = strValue;
            }
            else if (strVariable.CompareTo("Clan Kick Message") == 0)
            {
                this.m_strTagMessage = strValue;
            }
        }

        public void CheckPlayer(string ClanTag, string SoldierName)
        {
            Match NMatch;
            Match TMatch;

            foreach (string Stealth in m_aStealth)
            {
                if (string.IsNullOrEmpty(Stealth) == false)
                {
                    if ((SoldierName.ToLower()).Equals((Stealth.ToLower())) == true && string.IsNullOrEmpty(SoldierName) == false)
                    {
                        if (blDebug == enumBoolYesNo.Yes)
                        {
                            ExecuteCommand("procon.protected.pluginconsole.write", "^bStealth Match on [" + ClanTag + "] " + SoldierName + " found for '" + Stealth + ".'");
                        }
                        else
                        {
                            string Reason = "Stealth";
                            RunKickProcess(ClanTag, SoldierName, Stealth, Reason);
                        }
                        break;
                    }
                }
            }

            //Check for literal matches
            foreach (string NameLiteral in m_aNamesLiteral)
            {
                if (string.IsNullOrEmpty(NameLiteral) == false)
                {
                    if ((SoldierName.ToLower()).Equals((NameLiteral.ToLower())) == true && string.IsNullOrEmpty(SoldierName) == false)
                    {
                        if (blDebug == enumBoolYesNo.Yes)
                        {
                            ExecuteCommand("procon.protected.pluginconsole.write", "^bLiteral Match on [" + ClanTag + "] " + SoldierName + " found for '" + NameLiteral + ".'");
                        }
                        else
                        {
                            string Reason = "Name";
                            RunKickProcess(ClanTag, SoldierName, NameLiteral, Reason); //Literal matches have no whitelist
                        }
                        break;
                    }
                    else if ((ClanTag.ToLower()).Equals((NameLiteral.ToLower())) == true && string.IsNullOrEmpty(ClanTag) == false)
                    {
                        if (blDebug == enumBoolYesNo.Yes)
                        {
                            ExecuteCommand("procon.protected.pluginconsole.write", "^bLiteral Match on [" + ClanTag + "] " + SoldierName + " found for '" + NameLiteral + ".'");
                        }
                        else
                        {
                            string Reason = "ClanTag";
                            RunKickProcess(ClanTag, SoldierName, NameLiteral, Reason);
                        }
                        break;
                    }
                }
            }

            //Check for matches of the general list on both clan tags and names
            foreach (string Name in ma_Names)
            {
                if (string.IsNullOrEmpty(Name) == false)
                {
                    NMatch = Regex.Match(SoldierName, Name, RegexOptions.IgnoreCase);
                    TMatch = Regex.Match(ClanTag, Name, RegexOptions.IgnoreCase);

                    if (NMatch.Success == true && string.IsNullOrEmpty(SoldierName) == false)
                    {
                        if (blDebug == enumBoolYesNo.Yes)
                        {
                            ExecuteCommand("procon.protected.pluginconsole.write", "^bMatch on [" + ClanTag + "] " + SoldierName + " found for '" + Name + ".'");
                        }
                        string Reason = "Name";
                        bool NameOrTag = true;
                        CheckWhitelist(SoldierName, ClanTag, NameOrTag, Name, Reason);
                        break;
                    }
                    else if (TMatch.Success == true && string.IsNullOrEmpty(ClanTag) == false)
                    {
                        if (blDebug == enumBoolYesNo.Yes)
                        {
                            ExecuteCommand("procon.protected.pluginconsole.write", "^bMatch on [" + ClanTag + "] " + SoldierName + " found for '" + Name + ".'");
                        }
                        string Reason = "ClanTag";
                        bool NameOrTag = false;
                        CheckWhitelist(SoldierName, ClanTag, NameOrTag, Name, Reason);
                        break;
                    }
                }
            }

            //Check for banned clan tags
            foreach (string Tag in m_ClanTagsOnly)
            {

                if (string.IsNullOrEmpty(Tag) == false)
                {
                    if ((ClanTag.ToLower()).Equals((Tag.ToLower())) == true && string.IsNullOrEmpty(ClanTag) == false)
                    {
                        if (blDebug == enumBoolYesNo.Yes)
                        {
                            ExecuteCommand("procon.protected.pluginconsole.write", "^bClan Match on [" + ClanTag + "] " + SoldierName + " found for '" + Tag + ".'");
                        }
                        else
                        {
                            string Reason = "Tag";
                            RunKickProcess(ClanTag, SoldierName, Tag, Reason);
                            break;
                        }
                    }
                }
            }
        }

        public void RunKickProcess(string ClanTag, string SoldierName, string BadName, string Reason)
        {
            if (Reason.Equals("Name"))
            {
                ExecuteCommand("procon.protected.send", "admin.kickPlayer", SoldierName, m_strNameMessage.Replace("%bn%", BadName));
                ExecuteCommand("procon.protected.pluginconsole.write", "^b[" + ClanTag + "] " + SoldierName + " was removed because name contained '" + BadName + ".'");
            }
            else if (Reason.Equals("ClanTag"))
            {
                ExecuteCommand("procon.protected.send", "admin.kickPlayer", SoldierName, m_strClanTagMessage.Replace("%bn%", BadName));
                ExecuteCommand("procon.protected.pluginconsole.write", "^b[" + ClanTag + "] " + SoldierName + " was removed because name contained '" + BadName + ".'");
            }
            else if (Reason.Equals("Tag"))
            {
                ExecuteCommand("procon.protected.send", "admin.kickPlayer", SoldierName, m_strTagMessage.Replace("%bn%", BadName));
                ExecuteCommand("procon.protected.pluginconsole.write", "^b[" + ClanTag + "] " + SoldierName + " was removed because name contained '" + BadName + ".' (ClanTag Only)");
            }
            else if (Reason.Equals("Stealth"))
            {
                ExecuteCommand("procon.protected.send", "admin.kickPlayer", SoldierName);
                ExecuteCommand("procon.protected.pluginconsole.write", "^b[" + ClanTag + "] " + SoldierName + " was removed because name contained '" + BadName + ".' (Stealth)");
            }

        }

        public void CheckWhitelist(string SoldierName, string ClanTag, bool NameOrTag, string BadName, string Reason)
        {
            Match NMatch;
            Match TMatch;
            bool whitenamefound = false;

            foreach (string whitename in ma_Whitelist)
            {
                NMatch = Regex.Match(SoldierName, whitename, RegexOptions.IgnoreCase);
                TMatch = Regex.Match(ClanTag, whitename, RegexOptions.IgnoreCase);

                if (NameOrTag == true && NMatch.Success == true && string.IsNullOrEmpty(whitename) == false)
                {
                    if (blDebug == enumBoolYesNo.Yes)
                    {
                        ExecuteCommand("procon.protected.pluginconsole.write", "^bWhitelist Match on [" + ClanTag + "] " + SoldierName + " found for '" + whitename + ".' Kick Aborted");
                    }
                    whitenamefound = true;
                    break;
                }
                else if (NameOrTag == false && TMatch.Success == true && string.IsNullOrEmpty(whitename) == false)
                {
                    if (blDebug == enumBoolYesNo.Yes)
                    {
                        ExecuteCommand("procon.protected.pluginconsole.write", "^bWhitelist Match on [" + ClanTag + "] " + SoldierName + " found for '" + whitename + ".' Kick Aborted");
                    }
                    whitenamefound = true;
                    break;
                }
            }

            //Was a match for the whitelist found?        	
            if (whitenamefound == false)
            {
                if (blDebug == enumBoolYesNo.Yes)
                {
                    ExecuteCommand("procon.protected.pluginconsole.write", "^bNo Whitelist Match on [" + ClanTag + "] " + SoldierName + " found. Debug Mode enabled, kick aborted.");
                }
                else
                {
                    RunKickProcess(ClanTag, SoldierName, BadName, Reason); //Run the kick script
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

        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {
            if (this.lstNames.ContainsKey(strSoldierName) == true)
            {
                this.lstNames.Remove(strSoldierName);
                if (blDebug == enumBoolYesNo.Yes)
                {
                    ExecuteCommand("procon.protected.pluginconsole.write", "Removing " + strSoldierName);
                }
            }
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
            this.lstNames.Clear();
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

        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            foreach (CPlayerInfo Player in lstPlayers)
            {
                if (this.lstNames.ContainsKey(Player.SoldierName) == false)
                {
                    if (blDebug == enumBoolYesNo.Yes)
                    {
                        ExecuteCommand("procon.protected.pluginconsole.write", "Adding " + Player.SoldierName);
                    }
                    this.lstNames.Add(Player.SoldierName, Player.SoldierName);
                    CheckPlayer(Player.ClanTag, Player.SoldierName);
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
}