/*  Copyright 2010 Zaeed (Matt Green)

    http://aussieunderdogs.com

    This file is part of Zaeed's Plugins for BFBC2 PRoCon.

    Zaeed's Plugins for BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Zaeed's Plugins for BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Zaeed's Plugins for BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
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
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;
using PRoCon.Core.HttpServer;

namespace PRoConEvents
{
    public class CPlayerMuter : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo;
        private Dictionary<string, CPlayerInfo> m_dicPlayerInfo;

        private Dictionary<string, string> m_dicPlayerMutes;
        private List<string> m_lstVoice;
        private List<string> m_lstAdmin;

        private string m_strConfirmCommand;
        private string m_strServerFreeMode;
        private string m_strServerModeratedMode;
        private string m_strServerMutedMode;
        private string m_strPlayerMute;
        private string m_strPlayerNormal;
        private string m_strPlayerVoice;
        private string m_strPlayerAdmin;
        private string m_removeMuteAfter;
        private int m_strMuteTime;

        private bool m_isPluginEnabled;

        public CPlayerMuter()
        {

            this.m_strConfirmCommand = "yes";
            this.m_strServerFreeMode = "freechat";
            this.m_strServerModeratedMode = "moderatedchat";
            this.m_strServerMutedMode = "adminchat";
            this.m_strPlayerMute = "mute";
            this.m_strPlayerNormal = "unmute";
            this.m_strPlayerVoice = "grantvoice";
            this.m_strPlayerAdmin = "grantadmin";
            this.m_removeMuteAfter = "Round change";
            this.m_strMuteTime = 10;

            this.m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
            this.m_dicPlayerInfo = new Dictionary<string, CPlayerInfo>();
            this.m_dicPlayerMutes = new Dictionary<string, string>();
            this.m_lstVoice = new List<string>();
            this.m_lstAdmin = new List<string>();

            this.m_isPluginEnabled = false;
        }

        public string GetPluginName()
        {
            return "Player Muter";
        }

        public string GetPluginVersion()
        {
            return "1.2.0.0";
        }

        public string GetPluginAuthor()
        {
            return "Zaeed";
        }

        public string GetPluginWebsite()
        {
            return "aussieunderdogs.com";
        }

        public string GetPluginDescription()
        {
            return @"<p>If you find my plugins useful, please feel free to donate</p>
<blockquote>
<form action=""https://www.paypal.com/cgi-bin/webscr/"" method=""POST"" target=""_blank"">
<input type=""hidden"" name=""cmd"" value=""_s-xclick"">
<input type=""hidden"" name=""encrypted"" value=""-----BEGIN PKCS7-----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-----END PKCS7-----"">
<input type=""image"" src=""https://www.paypal.com/en_AU/i/btn/btn_donate_LG.gif"" border=""0"" name=""submit"" alt=""PayPal - The safer, easier way to pay online."">
<img alt="""" border=""0"" src=""https://www.paypal.com/en_AU/i/scr/pixel.gif"" width=""1"" height=""1"">
</form>
</blockquote>

<h2>Description</h2>
    <p>A very basic interface to mute players and control the chat mode</p>

<h2>Player Commands</h2>
    <blockquote>
   	  <h4>Mute player</h4>
        This mutes the player completely.  The player will be unmuted depending on the the 'Remove mute after' option
        </blockquote>
        <blockquote>
   	  <h4>Mute player &lt;time&gt;</h4>
        This mutes a player for the period defined by the admin. Time is in minutes.</blockquote>
    <blockquote>
	  <h4>Normal player</h4>
    	This allows the player to chat in Free mode.  Normal is the default setting for players.
</blockquote>
    <blockquote>
		<h4>Voice player</h4>
		Allows the player to chat in Free and Moderated modes.
</blockquote>
    <blockquote>
		<h4>Admin player</h4>
        Allows the player to talk in all moderation
	</blockquote>
    
<h2>Server Commands</h2>
        <blockquote>
          <h4>Free chat</h4>
          This is the default mode, and allows everyone to chat, except for muted players</blockquote>
        <blockquote>
          <h4>Moderated chat</h4>
          This allows for only people with Voice and Admin chat rights to talk.</blockquote>
        <blockquote>
          <h4>Muted chat</h4>
          Only players with Admin chat rights can talk.</blockquote>";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerJoin", "OnLoadingLevel", "OnPlayerLeft", "OnPunkbusterPlayerInfo");
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPlayer Muter ^2Enabled!");

            this.m_isPluginEnabled = true;
            this.RegisterAllCommands();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bPlayer Muter ^1Disabled =(");

            this.m_isPluginEnabled = false;
            this.UnregisterAllCommands();
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("~Setup|Confirmation command", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));

            lstReturn.Add(new CPluginVariable("Server Moderation|Free Chat Command", this.m_strServerFreeMode.GetType(), this.m_strServerFreeMode));
            lstReturn.Add(new CPluginVariable("Server Moderation|Moderated Chat Command", this.m_strServerModeratedMode.GetType(), this.m_strServerModeratedMode));
            lstReturn.Add(new CPluginVariable("Server Moderation|Muted Chat Command", this.m_strServerMutedMode.GetType(), this.m_strServerMutedMode));

            lstReturn.Add(new CPluginVariable("Player Moderation|Mute Players Chat Command", this.m_strPlayerMute.GetType(), this.m_strPlayerMute));
            lstReturn.Add(new CPluginVariable("Player Moderation|Normal Player Chat Command (unmute)", this.m_strPlayerNormal.GetType(), this.m_strPlayerNormal));
            lstReturn.Add(new CPluginVariable("Player Moderation|Voice Chat Privilege Command", this.m_strPlayerVoice.GetType(), this.m_strPlayerVoice));
            lstReturn.Add(new CPluginVariable("Player Moderation|Admin Chat Privilege Command", this.m_strPlayerAdmin.GetType(), this.m_strPlayerAdmin));

            lstReturn.Add(new CPluginVariable("Player Moderation|Remove mute after", "enum.proconCPlayerMuterUnmuteChoice(Round change|Time limit|Leave server|Never)", this.m_removeMuteAfter));
            if (String.Compare(this.m_removeMuteAfter, "Time limit") == 0)
            {
                lstReturn.Add(new CPluginVariable("Player Moderation|Mute time", typeof(int), this.m_strMuteTime));
            }

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Confirmation command", this.m_strConfirmCommand.GetType(), this.m_strConfirmCommand));

            lstReturn.Add(new CPluginVariable("Free Chat Command", this.m_strServerFreeMode.GetType(), this.m_strServerFreeMode));
            lstReturn.Add(new CPluginVariable("Moderated Chat Command", this.m_strServerModeratedMode.GetType(), this.m_strServerModeratedMode));
            lstReturn.Add(new CPluginVariable("Muted Chat Command", this.m_strServerMutedMode.GetType(), this.m_strServerMutedMode));

            lstReturn.Add(new CPluginVariable("Mute Players Chat Command", this.m_strPlayerMute.GetType(), this.m_strPlayerMute));
            lstReturn.Add(new CPluginVariable("Normal Player Chat Command (unmute)", this.m_strPlayerNormal.GetType(), this.m_strPlayerNormal));
            lstReturn.Add(new CPluginVariable("Voice Chat Privilege Command", this.m_strPlayerVoice.GetType(), this.m_strPlayerVoice));
            lstReturn.Add(new CPluginVariable("Admin Chat Privilege Command", this.m_strPlayerAdmin.GetType(), this.m_strPlayerAdmin));
            lstReturn.Add(new CPluginVariable("Remove mute after", "enum.proconCPlayerMuterUnmuteChoice(Round change|Time limit|Leave server|Never)", this.m_removeMuteAfter));
            lstReturn.Add(new CPluginVariable("Mute time", typeof(int), this.m_strMuteTime));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int intOut;
            if (strVariable.CompareTo("Confirmation command") == 0)
            {
                this.m_strConfirmCommand = strValue;
            }
            else if (strVariable.CompareTo("Free Mode Command") == 0)
            {
                this.m_strServerFreeMode = strValue;
            }
            else if (strVariable.CompareTo("Moderated Mode Command") == 0)
            {
                this.m_strServerModeratedMode = strValue;
            }
            else if (strVariable.CompareTo("Muted Mode Command") == 0)
            {
                this.m_strServerMutedMode = strValue;
            }

            else if (strVariable.CompareTo("Mute Players Chat Command") == 0)
            {
                this.m_strPlayerMute = strValue;
            }
            else if (strVariable.CompareTo("Normal Player Chat Command (unmute)") == 0)
            {
                this.m_strPlayerNormal = strValue;
            }
            else if (strVariable.CompareTo("Voice Chat Privilege Command") == 0)
            {
                this.m_strPlayerVoice = strValue;
            }
            else if (strVariable.CompareTo("Admin Chat Privilege Command") == 0)
            {
                this.m_strPlayerAdmin = strValue;
            }
            else if (strVariable.CompareTo("Remove mute after") == 0)
            {
                this.m_removeMuteAfter = strValue;
            }
            else if (strVariable.CompareTo("Mute time") == 0 && int.TryParse(strValue, out intOut) == true)
            {
                this.m_strMuteTime = intOut * 60;
            }

            this.RegisterAllCommands();
        }

        private void UnregisterAllCommands()
        {
            MatchCommand confirmationCommand = new MatchCommand(this.Listify<string>("@", "!", "#", "/"), this.m_strConfirmCommand, this.Listify<MatchArgumentFormat>());

            this.UnregisterCommand(
                new MatchCommand(
                    "CPlayerMuter",
                    "OnCommandPlayerMute",
                    this.Listify<string>("@", "!", "#", "/"),
                    this.m_strPlayerMute,
                    this.Listify<MatchArgumentFormat>(
                        new MatchArgumentFormat(
                            "playername",
                            new List<string>(this.m_dicPlayerInfo.Keys))),
                            new ExecutionRequirements(ExecutionScope.Account, 1, confirmationCommand, "You do not have the required permissions for this command."), "Player can never chat"));

            this.UnregisterCommand(new MatchCommand("CPlayerMuter", "OnCommandPlayerNormal", this.Listify<string>("@", "!", "#", "/"), this.m_strPlayerNormal, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, confirmationCommand, "You do not have the required permissions for this command."), "Player can chat when moderation is Free"));

            this.UnregisterCommand(new MatchCommand("CPlayerMuter", "OnCommandPlayerVoice", this.Listify<string>("@", "!", "#", "/"), this.m_strPlayerVoice, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanShutdownServer, 1, confirmationCommand, "You do not have the required permissions for this command."), "Player can chat when moderation is Free/Moderated"));

            this.UnregisterCommand(new MatchCommand("CPlayerMuter", "OnCommandPlayerAdmin", this.Listify<string>("@", "!", "#", "/"), this.m_strPlayerAdmin, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanShutdownServer, 1, confirmationCommand, "You do not have the required permissions for this command."), "Player can always chat"));


            this.UnregisterCommand(new MatchCommand("CPlayerMuter", "OnCommandSetFreeMode", this.Listify<string>("@", "!", "#", "/"), this.m_strServerFreeMode, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanShutdownServer, 1, confirmationCommand, "You do not have the required permissions for this command."), "Normal, Voice, and Admins players can chat"));

            this.UnregisterCommand(new MatchCommand("CPlayerMuter", "OnCommandSetModeratedMode", this.Listify<string>("@", "!", "#", "/"), this.m_strServerModeratedMode, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanShutdownServer, 1, confirmationCommand, "You do not have the required permissions for this command."), "Voice, and Admin players can chat"));

            this.UnregisterCommand(new MatchCommand("CPlayerMuter", "OnCommandSetAdminMode", this.Listify<string>("@", "!", "#", "/"), this.m_strServerMutedMode, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanShutdownServer, 1, confirmationCommand, "You do not have the required permissions for this command."), "Only Admin players can chat"));
        }

        private void RegisterAllCommands()
        {

            if (this.m_isPluginEnabled == true)
            {
                MatchCommand confirmationCommand = new MatchCommand(this.Listify<string>("@", "!", "#", "/"), this.m_strConfirmCommand, this.Listify<MatchArgumentFormat>());

                this.RegisterCommand(new MatchCommand("CPlayerMuter", "OnCommandPlayerMute", this.Listify<string>("@", "!", "#", "/"), this.m_strPlayerMute, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanEditTextChatModerationList, 1, confirmationCommand, "You do not have the required permissions for this command."), "Player can never chat"));

                this.RegisterCommand(new MatchCommand("CPlayerMuter", "OnCommandPlayerNormal", this.Listify<string>("@", "!", "#", "/"), this.m_strPlayerNormal, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanEditTextChatModerationList, 1, confirmationCommand, "You do not have the required permissions for this command."), "Player can chat when moderation is Free"));

                this.RegisterCommand(new MatchCommand("CPlayerMuter", "OnCommandPlayerVoice", this.Listify<string>("@", "!", "#", "/"), this.m_strPlayerVoice, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanAlterServerSettings, 1, confirmationCommand, "You do not have the required permissions for this command."), "Player can chat when moderation is Free/Moderated"));

                this.RegisterCommand(new MatchCommand("CPlayerMuter", "OnCommandPlayerAdmin", this.Listify<string>("@", "!", "#", "/"), this.m_strPlayerAdmin, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanAlterServerSettings, 1, confirmationCommand, "You do not have the required permissions for this command."), "Player can always chat"));


                this.RegisterCommand(new MatchCommand("CPlayerMuter", "OnCommandSetFreeMode", this.Listify<string>("@", "!", "#", "/"), this.m_strServerFreeMode, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanAlterServerSettings, 1, confirmationCommand, "You do not have the required permissions for this command."), "Normal, Voice, and Admins players can chat"));

                this.RegisterCommand(new MatchCommand("CPlayerMuter", "OnCommandSetModeratedMode", this.Listify<string>("@", "!", "#", "/"), this.m_strServerModeratedMode, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanAlterServerSettings, 1, confirmationCommand, "You do not have the required permissions for this command."), "Voice, and Admin players can chat"));

                this.RegisterCommand(new MatchCommand("CPlayerMuter", "OnCommandSetAdminMode", this.Listify<string>("@", "!", "#", "/"), this.m_strServerMutedMode, this.Listify<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.Privileges, Privileges.CanAlterServerSettings, 1, confirmationCommand, "You do not have the required permissions for this command."), "Only Admin players can chat"));

            }
        }

        public void RemoveMute(string soldierName)
        {
            this.ExecuteCommand("procon.protected.send", "textChatModerationList.addPlayer", "normal", soldierName);
            this.ExecuteCommand("procon.protected.send", "textChatModerationList.save");
            this.ExecuteCommand("procon.protected.send", "admin.say", "You have been unmuted", "player", soldierName);
        }

        #region general stuff

        public override void OnPlayerJoin(string strSoldierName)
        {

            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName) == false)
            {
                this.m_dicPlayerInfo.Add(strSoldierName, new CPlayerInfo(strSoldierName, "", 0, 24));
            }

        }

        public override void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {
            if (this.m_dicPbInfo.ContainsKey(cpiPlayer.SoldierName) == true)
            {
                this.m_dicPbInfo.Remove(cpiPlayer.SoldierName);

            }

            if (this.m_dicPlayerInfo.ContainsKey(cpiPlayer.SoldierName) == true)
            {
                this.m_dicPlayerInfo.Remove(cpiPlayer.SoldierName);
            }


            foreach (KeyValuePair<string, string> kvp in this.m_dicPlayerMutes)
            {
                if ((kvp.Key.CompareTo(cpiPlayer.SoldierName) == 0) && (kvp.Value.CompareTo("Leave server") == 0))
                {
                    RemoveMute(cpiPlayer.SoldierName);
                    this.m_dicPlayerMutes.Remove(cpiPlayer.SoldierName);
                }
            }

            this.RegisterAllCommands();
        }

        public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {
            foreach (KeyValuePair<string, string> kvp in this.m_dicPlayerMutes)
            {
                if (kvp.Value.CompareTo("Round change") == 0)
                {
                    RemoveMute(kvp.Key);
                    this.m_dicPlayerMutes.Remove(kvp.Key);
                }
            }
        }

        public override void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {

            if (cpbiPlayer != null)
            {
                if (this.m_dicPbInfo.ContainsKey(cpbiPlayer.SoldierName) == false)
                {
                    this.m_dicPbInfo.Add(cpbiPlayer.SoldierName, cpbiPlayer);
                }
                else
                {
                    this.m_dicPbInfo[cpbiPlayer.SoldierName] = cpbiPlayer;
                }

                this.RegisterAllCommands();
            }
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {

            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo cpiPlayer in lstPlayers)
                {
                    if (this.m_dicPlayerInfo.ContainsKey(cpiPlayer.SoldierName) == true)
                    {
                        this.m_dicPlayerInfo[cpiPlayer.SoldierName] = cpiPlayer;
                    }
                    else
                    {
                        this.m_dicPlayerInfo.Add(cpiPlayer.SoldierName, cpiPlayer);
                    }
                }
            }

        }

        #endregion

        #region Player Modulation Levels

        public void OnCommandPlayerMute(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.m_dicPbInfo.ContainsKey(capCommand.MatchedArguments[0].Argument) == true)
            {
                int myInt;
                this.ExecuteCommand("procon.protected.send", "textChatModerationList.addPlayer", "muted", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);

                if (capCommand.ExtraArguments.Length > 0)
                {
                    try
                    {
                        myInt = Int32.Parse(capCommand.ExtraArguments);

                        this.ExecuteCommand("procon.protected.send", "admin.say", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + " now is muted for " + myInt.ToString() + " minutes", "player", strSpeaker);
                        this.ExecuteCommand("procon.protected.send", "admin.say", "You have been muted for " + myInt.ToString() + " minutes", "player", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);

                        myInt = myInt * 60;
                        this.ExecuteCommand("procon.protected.tasks.add", "CPlayerMuterUnMute", myInt.ToString(), "1", "1", "procon.protected.plugins.call", "CPlayerMuter", "RemoveMute", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                    }
                    catch
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Invalid mute time, player not muted", "player", strSpeaker);
                    }
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + " has been muted.", "player", strSpeaker);
                    this.ExecuteCommand("procon.protected.send", "admin.say", "You have been muted", "player", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                    if (this.m_removeMuteAfter.CompareTo("Time limit") == 0)
                    {
                        this.ExecuteCommand("procon.protected.tasks.add", "CPlayerMuterUnMute", this.m_strMuteTime.ToString(), "1", "1", "procon.protected.plugins.call", "CPlayerMuter", "RemoveMute", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                    }
                    else
                    {
                        this.m_dicPlayerMutes.Add(this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName, this.m_removeMuteAfter);
                    }
                }

                this.ExecuteCommand("procon.protected.send", "textChatModerationList.save");
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "Player not found", "player", strSpeaker);
            }
        }

        public void OnCommandPlayerNormal(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

            if (this.m_dicPbInfo.ContainsKey(capCommand.MatchedArguments[0].Argument) == true)
            {
                this.ExecuteCommand("procon.protected.send", "textChatModerationList.addPlayer", "normal", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                this.ExecuteCommand("procon.protected.send", "admin.say", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + " now has normal chat privileges.", "player", strSpeaker);
                this.ExecuteCommand("procon.protected.send", "admin.say", "You have been granted nomral chat privleges", "player", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                this.ExecuteCommand("procon.protected.send", "textChatModerationList.save");
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "Player not found", "player", strSpeaker);
            }
        }

        public void OnCommandPlayerVoice(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

            if (this.m_dicPbInfo.ContainsKey(capCommand.MatchedArguments[0].Argument) == true)
            {
                this.ExecuteCommand("procon.protected.send", "textChatModerationList.addPlayer", "voice", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                this.ExecuteCommand("procon.protected.send", "admin.say", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + " now has voice chat privileges.", "player", strSpeaker);
                this.ExecuteCommand("procon.protected.send", "admin.say", "You have been granted moderator chat privleges", "player", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                this.ExecuteCommand("procon.protected.send", "textChatModerationList.save");
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "Player not found", "player", strSpeaker);
            }
        }

        public void OnCommandPlayerAdmin(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

            if (this.m_dicPbInfo.ContainsKey(capCommand.MatchedArguments[0].Argument) == true)
            {
                this.ExecuteCommand("procon.protected.send", "textChatModerationList.addPlayer", "admin", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                this.ExecuteCommand("procon.protected.send", "admin.say", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + " now has admin chat privileges.", "player", strSpeaker);
                this.ExecuteCommand("procon.protected.send", "admin.say", "You have been granted admin chat privleges", "player", this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                this.ExecuteCommand("procon.protected.send", "textChatModerationList.save");
            }
            else
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", "Player not found", "player", strSpeaker);
            }
        }


        #endregion

        #region Server Moderation Modes

        public void OnCommandSetFreeMode(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

            this.ExecuteCommand("procon.protected.send", "vars.textChatModerationMode", "free");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Free chat mode has been activated", "all");

        }

        public void OnCommandSetModeratedMode(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

            this.ExecuteCommand("procon.protected.send", "vars.textChatModerationMode", "moderated");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Moderator only chat mode has been activated", "all");

        }

        public void OnCommandSetAdminMode(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

            this.ExecuteCommand("procon.protected.send", "vars.textChatModerationMode", "muted");
            this.ExecuteCommand("procon.protected.send", "admin.say", "Admin only chat mode has been activated", "all");

        }

        #endregion
    }
}