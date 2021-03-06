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

    #region Variable Declaration
    public class CChatFilterBF3 : PRoConPluginAPI, IPRoConPluginInterface
    {

        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;
        private Dictionary<string, int> m_dicHOffenders = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicDOffenders = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicHROffenders = new Dictionary<string, int>();
        private Dictionary<string, int> m_dicDROffenders = new Dictionary<string, int>();
        private Dictionary<string, CPlayerInfo> m_dicPlayerInfo = new Dictionary<string, CPlayerInfo>();
        private Dictionary<string, CPunkbusterInfo> m_dicPbInfo = new Dictionary<string, CPunkbusterInfo>();
        private List<string> ImmunePeople = new List<string>();
        private string[] BWords;		//The list of bad words
        private string[] HWords;
        private string[] DWords;
        private string[] DWordsforWord;
        private string[] HWhitelist;
        private string[] DWhitelist;
        private string[] ProtectedNames;
        private string[] ProtectedTags;
        private string[] CommandKeys = new string[] { "@admin", "@report" };
        private string m_strBanMessage;	//The message sent as an extra argument to the kicked player
        private string m_strBadWarn;
        private string m_strKickMessage;
        private string m_strHackerWarn;
        private string m_strHackerKick;
        private string m_strHServerSay;
        private string m_strDServerSay;
        private string m_strHRBan;
        private string m_strHRPBan;
        private string m_strHBan;
        private string m_strHPBan;
        private string m_strDRBan;
        private string m_strDRPBan;
        private string m_strDBan;
        private string m_strDPBan;
        private enumBoolYesNo bBan;     //This boolean determines kick or ban punishment 	
        private int iBanTime;			// Ban time for bans
        private int minsBanTime;
        private int iHbeforewarn;
        private int iDbeforewarn;
        private int iDbeforesay;
        private int iHbeforesay;
        private int iHbeforekick;
        private int iDbeforekick;
        private int iDYellTime;
        private int iHYellTime;
        private enumBoolYesNo blHackWarn;
        private int iBadYellTime;
        private int iHackYellTime;
        private enumBoolYesNo blDbeforesay;
        private enumBoolYesNo blDbeforewarn;
        private enumBoolYesNo blHbeforesay;
        private enumBoolYesNo blHbeforewarn;
        private enumBoolYesNo blTempBan;
        private enumBoolYesNo blDBanRepeat;
        private enumBoolYesNo blDbeforekick;
        private enumBoolYesNo blHBanRepeat;
        private enumBoolYesNo blHbeforekick;
        private enumBoolYesNo blProtectAdmins;
        private enumBoolYesNo blClearOnImmune = enumBoolYesNo.No;
        private int iHRepeatbeforeban;
        private int iDRepeatbeforeban;
        private int iHbeforeban;
        private int iDbeforeban;
        private int iHRBanTime;
        private int iDRBanTime;
        private int iHBanTime;
        private int iDBanTime;
        private enumBoolYesNo blHRepeatTempBan;
        private string strDBanOrKick;
        private string strHBanOrKick;
        private string strTempBan;
        private string strDTempBan;
        private string strHTempBan;
        private string strDRTempBan;
        private string strHRTempBan;
        private string m_strPBanMessage;
        private enumBoolYesNo blHDrop;
        private enumBoolYesNo blDDrop;
        private enumBoolYesNo blHDropKick;
        private enumBoolYesNo blDDropKick;
        private int HRminsBanTime;
        private int DRminsBanTime;
        private int HminsBanTime;
        private int DminsBanTime;
        private string m_strDClearCommand;
        private string m_strHClearCommand;
        private enumBoolYesNo blDMute;
        private int iDbeforemute;
        private string strDMuteChoice;
        private int iDMuteTime;
        private int iDMuteRounds;
        private string m_strDMuteMessage;
        private string m_strDClearMuteCommand;
        private enumBoolYesNo blHMute;
        private int iHbeforemute;
        private string strHMuteChoice;
        private int iHMuteTime;
        private int iHMuteRounds;
        private string m_strHMuteMessage;
        private string m_strHClearMuteCommand;
        private enumBoolYesNo blDMuteClear;
        private enumBoolYesNo blHMuteClear;
        private int iDMinMute;
        private int iHMinMute;
        private enumBoolYesNo blDMuteLeaveClear;
        private enumBoolYesNo blHMuteLeaveClear;
        private string strBanMethod;
        private string strHRBanMethod;
        private string strDRBanMethod;
        private string strHBanMethod;
        private string strDBanMethod;
        private string strTBanUnits;
        private string strHRTBanUnits;
        private string strDRTBanUnits;
        private string strHTBanUnits;
        private string strDTBanUnits;
        private enumBoolYesNo blBanAnnounce = enumBoolYesNo.No;
        private string strBanAnnounceType = "Say";
        private string strTDBanAnnounce = "%pn% was banned for %bt% hours for bad language.";
        private string strDBanAnnounce = "%pn% was banned for bad language.";
        private string strTHBanAnnounce = "%pn% was banned for %bt% hours for whining.";
        private string strHBanAnnounce = "%pn% was banned for whining.";
        private string strTBanAnnounce = "%pn% was banned for %bt% hours for using racial slurs.";
        private string strBanAnnounce = "%pn% was banned for using racial slurs.";
        private enumBoolYesNo blGiveImmunity = enumBoolYesNo.No;
        private enumBoolYesNo blKickAnnounce = enumBoolYesNo.No;
        private enumBoolYesNo blMuteAnnounce = enumBoolYesNo.No;
        private string strDKickAnnounce = "%pn% was kicked for bad language.";
        private string strHKickAnnounce = "%pn% was kicked for whining.";
        private string strDMuteAnnounce = "%pn% was muted for bad language.";
        private string strHMuteAnnounce = "%pn% was muted for whining.";
        private string strKickAnnounceType = "Say";
        private string strMuteAnnounceType = "Say";
        private int iBAYellTime = 8;
        private int iKAYellTime = 8;
        private int iMAYellTime = 8;




        public CChatFilterBF3()
        {
            this.strBanMethod = "Name";
            this.strHRBanMethod = "Name";
            this.strDRBanMethod = "Name";
            this.strHBanMethod = "Name";
            this.strDBanMethod = "Name";
            this.strTBanUnits = "Hours";
            this.strHRTBanUnits = "Hours";
            this.strDRTBanUnits = "Hours";
            this.strHTBanUnits = "Hours";
            this.strDTBanUnits = "Hours";
            this.blDMute = enumBoolYesNo.No;
            this.blHMute = enumBoolYesNo.No;
            this.iDbeforemute = 5;
            this.iHbeforemute = 5;
            this.strDMuteChoice = "Time";
            this.strHMuteChoice = "Time";
            this.iDMuteTime = 30;
            this.iHMuteTime = 30;
            this.iDMuteRounds = 3;
            this.iHMuteRounds = 3;
            this.m_strDMuteMessage = "You've been muted for bad language.";
            this.m_strHMuteMessage = "You've been muted for whining.";
            this.m_strDClearMuteCommand = "languageunmute";
            this.m_strHClearMuteCommand = "whineunmute";
            this.blDMuteClear = enumBoolYesNo.No;
            this.blHMuteClear = enumBoolYesNo.No;
            this.blDMuteLeaveClear = enumBoolYesNo.No;
            this.blHMuteLeaveClear = enumBoolYesNo.No;
            this.BWords = new string[] {
"nigger",
"niggers",
"negro",
"negros",
"nigga",
"niggas",
"n1gga",
"n1ggas",
"nigg4",
"nigg4s",
"n1gger",
"n1ggers",
"n1gg3r",
"n1gg3rs",
"n!gger",
"n!ggers",
"nigg3r",
"nigg3rs",
"niga",
"nigas",
"n3gro",
"n3gros",
"negr0",
"negr0s",
"nehgger",
"nehggers",
"nihgger",
"nihggers",
"nlgger",
"nlggers",
"niger",
"nigers",
"ni66er",
"ni66ers"};

            this.HWords = new string[] { "hack", "cheat", "aimbot", "hax", "hak" };
            this.DWords = new string[] { "f4g", "sh!t", "ph4g", "tw4t", "a55", "puta", "puto", "foder" };
            this.DWordsforWord = new string[] { "a$$", "a$$hole", "a$$holes", "a$$es", "a$$e$", "a$$ed", "a$$h0le", "a$$h0les", "a$$h0l3", "a$$h0l3s", "fdp", "vsf" };
            //this.BWhitelist = new string[] {"niger"};
            this.DWhitelist = new string[] { "assin" };
            this.HWhitelist = new string[] {"admin",
"report",
"whack",
"shack",
"shak",
"whak",
"aku"};
            this.ProtectedNames = new string[] { "Brazin" };
            this.ProtectedTags = new string[] { "MIA" };
            this.m_strHackerWarn = "Stop whining and ask for an admin's assitance. %warnings% out of %totalwarnings% warnings";
            this.m_strHackerKick = "Whining too much about hackers. Ask for an admin and they will take care of it.";
            this.m_strBadWarn = "Do not dodge the language filter. This is warning %warnings% out of %totalwarnings%";
            this.m_strKickMessage = "Do not dodge the language filter in our servers.";
            this.m_strBanMessage = "Banned for %bt% hours for using offensive language";
            this.m_strHServerSay = "Automated Message: Request an admin if hackers are in the server.";
            this.m_strDServerSay = "Automated Message: Do not dodge the language filter.";
            this.m_strPBanMessage = "Banned for using offensive language.";
            this.m_strHRBan = "Banned for %bt% hours for repeatedly being kicked for whining about hackers.";
            this.m_strHRPBan = "Banned for repeatedly being kicked for whining too much about hackers.";
            this.m_strHBan = "Banned for %bt% hours for whining too much about hackers";
            this.m_strHPBan = "Banned for whining too much about hackers";
            this.m_strDRBan = "Banned for %bt% hours for repeatedly being kicked for dodging the language filter.";
            this.m_strDRPBan = "Banned for repeatedly being kicked for dodging the language filter.";
            this.m_strDBan = "Banned for %bt% hours for bad language.";
            this.m_strDPBan = "Banned for bad language.";
            this.iDbeforekick = 5;
            this.iDbeforewarn = 1;
            this.iHbeforekick = 6;
            this.iHbeforewarn = 3;
            this.iDbeforesay = 1;
            this.iHbeforesay = 1;
            this.iDbeforeban = 6;
            this.iHbeforeban = 6;
            this.iHYellTime = 10;
            this.iDYellTime = 10;
            this.iHRepeatbeforeban = 3;
            this.iDRepeatbeforeban = 3;
            this.bBan = enumBoolYesNo.No;
            this.iBanTime = 24;
            this.iHRBanTime = 24;
            this.iDRBanTime = 24;
            this.iHBanTime = 24;
            this.iDBanTime = 24;
            this.blHackWarn = enumBoolYesNo.Yes;
            this.blDbeforesay = enumBoolYesNo.No;
            this.blDbeforewarn = enumBoolYesNo.No;
            this.blHbeforesay = enumBoolYesNo.No;
            this.blHbeforewarn = enumBoolYesNo.No;
            this.blTempBan = enumBoolYesNo.No;
            this.blDBanRepeat = enumBoolYesNo.No;
            this.blDbeforekick = enumBoolYesNo.No;
            this.blHBanRepeat = enumBoolYesNo.No;
            this.blHbeforekick = enumBoolYesNo.No;
            this.blHRepeatTempBan = enumBoolYesNo.Yes;
            this.blProtectAdmins = enumBoolYesNo.Yes;
            this.strDBanOrKick = "None";
            this.strHBanOrKick = "None";
            this.strTempBan = "Temporary";
            this.strHTempBan = "Temporary";
            this.strDTempBan = "Temporary";
            this.strDRTempBan = "Temporary";
            this.strHRTempBan = "Temporary";
            this.blHDrop = enumBoolYesNo.Yes;
            this.blDDrop = enumBoolYesNo.Yes;
            this.blHDropKick = enumBoolYesNo.Yes;
            this.blDDropKick = enumBoolYesNo.Yes;
            this.m_strDClearCommand = "clearinfractions";
            this.m_strHClearCommand = "clearwhines";
        }

        public string GetPluginName()
        {
            return "Chat Filter BF3";
        }

        public string GetPluginVersion()
        {
            return "1.0.0.1";
        }

        public string GetPluginAuthor()
        {
            return "DFC-NightMare[NL], Based on Brazin's V1.3.3.3";
        }

        public string GetPluginWebsite()
        {
            return "www.DutchFightClub.nl";
        }

        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>
    <p>This plugin is designed to check all messages sent by players in your server for words flagged by the administrator. There are three categories for bad words: ban words, bad language and hack whining. Ban words will be followed by an immediate ban of the offender. For bad language and hacker whining, you are given a number of options. You may choose what actions are made for flagged words from say warnings up to kicking and banning. You can also choose how many times players are flagged before actions are taken, and there is also a repeat offender banning option that will ban players who have been kicked too many times for bad language or whining.</p>
    <p>Thanks to MonkeyFiend of sneakymonkeys.com for making the original Profanity Filter plugin. I have borrowed some of his code and utilized it within this plugin and I mean the highest respects for him.</p>
    <p>This plugin also incorporates a method very similar to HeliMagnet's banning of repeat offenders, and it draws inspiration from other authors as well. Many thanks to Zaeed for helping me develop the mute feature, as I used his Player Muter plugin as a reference. Being a novice at developing plugins myself I have only been able to learn from observation. I would not have been able to write this plugin without help (both direct and indirect) from this great community and certainly not without Phogue. Thanks guys!</p>
    
<h2>Fields</h2>	
<p></p>
<h3>All Warning Messages</h3>
	<p>You can use %warnings% and %warningstotal% in say and yell warnings to represent the number of current warnings and the number of total warnings respectively. They are calculated based on the number of infractions required to earn the respective warning and the number of infractions required for the plugin to take a final action (kick or ban). These two strings will only be replaced by their respective numbers if the Final Action taken is not set to None.</p>
<h3>All Temporary Ban Messages</h3>
	<p>You can use %bt% to represent the number of hours a player will be banned in their temp ban message.</p>
<h3>Announcements</h3>
    <p>Announcements are handled for ban, kick and mute events. These announcements will be triggered in the event that somebody is kicked/banned/muted on your server by this plugin depending on the settings you provide. The messages and and settings are fairly straightforward to enable and set. You will have the option to set announcements for temp bans and perm bans as well as messages regarding users of bad language for every type of punishment. %pn% is used to indicate the offender's name and %bt% will represent the ban time where appropriate.</p>
<h3>Global Options</h3>
	<p>This section contains options for protected names and clan tags, as well as the option to protect admins from this plugin's actions. The plugin will not check messages from any of these players.
	<blockquote><h4>Protected Names</h4>A list of names of players that will not have their messages checked on your server. These names much match the names of the players you want to protect letter for letter, but they are not case sensitive.</blockquote>
	<blockquote><h4>Protected Clan Tags</h4>A list of clan tags that are protected from this plugin. Anybody with clantags that exactly match those on this list will have their messages ignored by this plugin. Not case sensitive</blockquote>
	<blockquote><h4>Protect Account Holders?</h4>This option determines whether the plugin will ignore messages from account holders on your PRoCon Client</blockquote>
<h3>Bannable Offense</h3>
	<p>This section handles words that will ban players on the spot for using certain words for a specified number of hours or even permanently. Remember that these words are taken literally, which means you must provide all permutations of the word within the list (plural, past tense, etc.). This is done in order to prevent players from accidentally being banned and also negates the need for a whitelist</p>
	<blockquote><h4>Ban Words</h4>Words that will get players banned</blockquote>
	<blockquote><h4>Ban Type</h4>Choose Ban Type
		<blockquote><h4>Temporary</h4>
			<blockquote><h4>Temporary Ban: Time Units</h4>Establishes the units of time that the Ban Time will be based off of</blockquote>
			<blockquote><h4>Temporary Ban: Ban Time</h4>How long a player will be banned if temp banned</blockquote>
			<blockquote><h4>Temporary Ban: Ban Message</h4>The message the player will receive when he is temp banned</blockquote></blockquote>
		<blockquote><h4>Permanent</h4>
			<blockquote><h4>Ban Message</h4>The message the player will receive when he is temp banned</blockquote></blockquote></blockquote>
<h3>Word Warnings</h3>
	<p>This section handles normal bad words. If these words are detected, offenders will accumulate infractions. All actions are based off of the number of infractions a player has, but you can set the infraction thresholds yourself.</p>
	<blockquote><h4>Bad Words</h4>Words that players will be warned for saying. Any variation of these words will be detected as long as the characters in the bad word are contained within the message sent by the player</blockquote>
	<blockquote><h4>Bad Words (Literal)</h4>These bad words will be treated literally, like the ban words, and will accumulate infractions just like normal Bad Words do.</blockquote>
	<blockquote><h4>Drop Infractions When Leaving?</h4>This setting determines whether a player's accumulated bad language infractions will be reset to 0 whenever they leave the server.</blockquote>
	<blockquote><h4>Infraction Clear Command</h4>An admin command that can be used to manually clear a player's existing infractions. Uses all admin command prefixes , and requires players to be present in the server to function properly(ex. @clearinfractions Brazin)</blockquote>
	<blockquote><h4>Say Warning: Enable?</h4>Enables say warnings for bad word infractions</blockquote>
	<blockquote><h4>Say Warning: Infractions Before Say</h4>The number of infractions before the player will be warned through a say command.</blockquote>
	<blockquote><h4>Say Warning: Say Message</h4>The message that will be sent to the warned player</blockquote>
	<blockquote><h4>Yell Warning: Enable?</h4>Enables yell warnings for bad word infractions</blockquote>
	<blockquote><h4>Bad Word Final Action</h4>Choose between None, Kick or Ban. If None is selected, the plugin will not take any final action towards offenders.
		<blockquote><h4>Kick</h4>
			<blockquote><h4>Kicking: Infractions Before Kick</h4>The number of infractions before the player will be kicked</blockquote>
			<blockquote><h4>Kicking: Kick Message</h4>The message the kicked player will receive</blockquote>
			<blockquote><h4>Kicking: Drop Infractions When Kicked?</h4>This setting determines whether a player's infractions will be set to 0 whenever they are kicked from the server by the plugin for bad language.</blockquote>
			<blockquote><h4>Repeat Offender: Enable?</h4>Enable banning of repeat offenders</blockquote>
			<blockquote><h4>Repeat Offender: Kicks Before Ban</h4>The number of offender kicks required before a player is banned.</blockquote>
			<blockquote><h4>Repear Offender: Ban Method</h4>Choose your preference of ban method. You can ban players by Name, EA GUID or PB GUID</blockquote>
			<blockquote><h4>Repeat Offender: Ban Type</h5>
				<blockquote><h4>Temporary</h4>
					<blockquote><h4>Repeat Offender: Temporary Ban: Time Units</h4>Establishes the units of time that the Ban Time will be based off of</blockquote>
					<blockquote><h4>Repeat Offender: Temporary Ban: Ban Time</h4>How long a player will be banned if temp banned</blockquote>
					<blockquote><h4>Repeat Offender: Temporary Ban: Ban Message</h4>The message the player will receive when he is temp banned</blockquote></blockquote>
				<blockquote><h4>Permanent</h4>
					<blockquote><h4>Repeat Offender: Ban Message</h4>The message the player will receive when he is temp banned</blockquote></blockquote></blockquote></blockquote>
		<blockquote><h4>Ban</h4>
			<blockquote><h4>Bannings: Infractions Before Ban</h4>The number of detected infractions before a player is banned.</blockquote>
			<blockquote><h4>Bannings: Ban Type</h4>
				<blockquote><h4>Temporary</h4>
					<blockquote><h4>Bannings: Temporary Ban: Time Units</h4>Establishes the units of time that the Ban Time will be based off of</blockquote>
					<blockquote><h4>Bannings: Temporary Ban: Ban Time</h4>How long a player will be banned if temp banned</blockquote>
					<blockquote><h4>Bannings: Temporary Ban: Ban Message</h4>The message the player will receive when he is temp banned</blockquote></blockquote>
				<blockquote><h4>Permanent</h4>
					<blockquote><h4>Bannings: Ban Message</h4>The message the player will receive when he is permanently banned</blockquote></blockquote></blockquote></blockquote></blockquote></blockquote>
<h3>Hack Whiners</h3>
	<p>This section handles whiners. If these words are detected, offenders will accumulate whines. All actions are based off of the number of whines a player has, but you can set the whine thresholds yourself. You could also use this section as a custom list for different words you don't want to hear in your server. It doesn't just have to be reserved for hacker whining. I just originally intended it as a sepparate list handler for people who don't stop whining about hackers.</p>
	<blockquote><h4>Enable Hack Whine Filter</h4>Enables the hack whine filtering feature</blockquote>
		<blockquote><h4>Words</h4>Similar to Bad Words, but detects words for actions against whiners like hack, hax and aimbot</blockquote>
		<blockquote><h4>Whitelist</h4>These whitelisted words will not apply infractions to the whining player if they are detected. For instance, I don't consider it whining if they are trying to let an admin know about the problem, so I have admin as a whitelisted word. If admin is detected within the player's message at the same time as a flagged word, then that player will not accumulate offenses.</blockquote>
        <blockquote><h4>Enable Report Command immunity?</h4>This enables a second whitelist that will actually make players immune to punishment for the rest of the round. This is useful if you have commands players can use to report hackers to admins and don't want to punish players that use these commands.
        <blockquote><h4>Clear Whines on Report Command?</h4>If set to 'Yes' this option will clear a players accumulated whines whenever a Report Command is used</blockquote>
        <blockquote><h4>Report Commands</h4>These are where you may place commands or keywords that will give players immunity for the remainder of the round when spoken. It is advisable that you not allow players to become aware of this feature in order to avoid having it abused</blockquote></blockquote>
		<blockquote><h4>Drop Whines When Leaving?</h4>This setting determines whether a player's accumulated hack whines will be reset to 0 whenever they leave the server.</blockquote>
		<blockquote><h4>Whine Clear Command</h4>An admin command that can be used to manually clear a player's existing whines. Uses all admin command prefixes, and requires players to be present in the server to function properly (ex. @clearwhines Brazin)</blockquote>
		<blockquote><h4>Say Warnings: Enable?</h4>Enables the say warnings for whiners</blockquote>
		<blockquote><h4>Say Warnings: Whines Before Say</h4>The number of detected whines before say messages will be sent to the whiner</blockquote>
		<blockquote><h4>Say Warnings: Say Message</h4>The say message that will be sent to the whiner.</blockquote>
		<blockquote><h4>Whine Final Action</h4>Choose between None, Kick or Ban. If None is selected, the plugin will not take any final action towards whiners.
		<blockquote><h4>Kick</h4>
			<blockquote><h4>Kicking: Whines Before Kick</h4>The number of detected whines before the player will be kicked for whining.</blockquote>
			<blockquote><h4>Kicking: Message</h4>The message the whiner will receive when kicked.</blockquote>
			<blockquote><h4>Kicking: Drop Whines When Kicked?</h4>This setting determines whether a player's whines will be set to 0 whenever they are kicked from the server by the plugin for whining.</blockquote>
			<blockquote><h4>Repeat Offenders: Enable?</h4>Enable banning of repeat offenders</blockquote>
			<blockquote><h4>Repeat Offenders: Kicks Before Ban</h4>The number of whine kicks required before a player is banned.</blockquote>
			<blockquote><h4>Repear Offenders: Ban Method</h4>Choose your preference of ban method. You can ban players by Name, EA GUID or PB GUID</blockquote>
			<blockquote><h4>Repeat Offenders: Ban Type</h4>
				<blockquote><h4>Temporary</h4>
					<blockquote><h4>Repeat Offenders: Temporary Ban: Time Units</h4>Establishes the units of time that the Ban Time will be based off of</blockquote>
					<blockquote><h4>Repeat Offenders: Temporary Ban: Ban Time</h4>How long a player will be banned if temp banned</blockquote>
					<blockquote><h4>Repeat Offenders: Temporary Ban: Ban Message</h4>The message the player will receive when he is temp banned</blockquote></blockquote>
				<blockquote><h4>Permanent</h4>
					<blockquote><h4>Repeat Offenders: Ban Message</h4>The message the player will receive when he is perm banned</blockquote></blockquote></blockquote></blockquote>
		<blockquote><h4>Ban</h4>
			<blockquote><h4>Banning: Whines Before Ban</h4> The number of detected whines before a player is banned.</blockquote>
			<blockquote><h4>Banning: Ban Type</h4>
				<blockquote><h4>Temporary</h4>
					<blockquote><h4>Banning: Temporary Ban: Time Units</h4>Establishes the units of time that the Ban Time will be based off of</blockquote>
					<blockquote><h4>Banning: Temporary Ban: Ban Time</h4>How long a player will be banned in if temp banned</blockquote>
					<blockquote><h4>Banning: Temporary Ban: Ban Message</h4>The message the player will receive when he is temp banned</blockquote></blockquote>
				<blockquote><h4>Permanent</h4>
					<blockquote><h4>Banning: Ban Message</h4>The message the player will receive when he is perm banned</blockquote></blockquote></blockquote></blockquote></blockquote>";
        }
        #endregion

        #region PluginSetup

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bChat Filter BF3 ^2Enabled!");
            //this.ExecuteCommand("procon.protected.send", "punkBuster.pb_sv_command", "pb_sv_banList BC2!");
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerJoin", "OnPlayerLeft", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnPunkbusterPlayerInfo", "OnLoadingLevel", "OnCommandDClear", "OnCommandHClear", "OnCommandDMuteClear", "OnCommandHMuteClear");
            this.RegisterAllCommands();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bChat Filter BF3 ^1Disabled =(");
            this.UnregisterAllCommands();
        }

        // Lists only variables you want shown.. for instance enabling one option might hide another option
        // It's the best I got until I implement a way for plugins to display their own small interfaces.
        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("0. Global Options|Protected Names", this.ProtectedNames.GetType(), this.ProtectedNames));
            lstReturn.Add(new CPluginVariable("0. Global Options|Protected Clan Tags", this.ProtectedTags.GetType(), this.ProtectedTags));
            lstReturn.Add(new CPluginVariable("0. Global Options|Protect Account Holders?", typeof(enumBoolYesNo), this.blProtectAdmins));
            lstReturn.Add(new CPluginVariable("1. Bannable Offense|Ban Words", this.BWords.GetType(), this.BWords));
            lstReturn.Add(new CPluginVariable("1. Bannable Offense|Ban Type", "enum.BanType(Temporary|Permanent)", this.strTempBan));
            if (this.strTempBan.Equals("Temporary") == true)
            {
                lstReturn.Add(new CPluginVariable("1. Bannable Offense|Temporary Ban: Time Units", "enum.TBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strTBanUnits));
                lstReturn.Add(new CPluginVariable("1. Bannable Offense|Temporary Ban: Ban Time", this.iBanTime.GetType(), this.iBanTime));
                lstReturn.Add(new CPluginVariable("1. Bannable Offense|Temporary Ban: Ban Message", this.m_strBanMessage.GetType(), this.m_strBanMessage));
            }
            else if (this.strTempBan.Equals("Permanent") == true)
            {
                lstReturn.Add(new CPluginVariable("1. Bannable Offense|Ban Message", this.m_strPBanMessage.GetType(), this.m_strPBanMessage));
            }
            lstReturn.Add(new CPluginVariable("2. Word Warnings|Bad Words", this.DWords.GetType(), this.DWords));
            lstReturn.Add(new CPluginVariable("2. Word Warnings|Bad Words (Literal)", this.DWordsforWord.GetType(), this.DWordsforWord));
            lstReturn.Add(new CPluginVariable("2. Word Warnings|Drop Infractions When Leaving?", typeof(enumBoolYesNo), this.blDDrop));
            lstReturn.Add(new CPluginVariable("2. Word Warnings|Infraction Clear Command", this.m_strDClearCommand.GetType(), this.m_strDClearCommand));
            lstReturn.Add(new CPluginVariable("2. Word Warnings|Say Warning: Enable?", typeof(enumBoolYesNo), this.blDbeforesay));
            if (this.blDbeforesay == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("2. Word Warnings|Say Warning: Infractions Before Say", this.iDbeforesay.GetType(), this.iDbeforesay));
                lstReturn.Add(new CPluginVariable("2. Word Warnings|Say Warning: Say Message", this.m_strDServerSay.GetType(), this.m_strDServerSay));
            }
            lstReturn.Add(new CPluginVariable("2. Word Warnings|Bad Word Final Action", "enum.BWordChooseFinalAction(None|Kick|Ban)", this.strDBanOrKick));
            if (this.strDBanOrKick.Equals("Kick") == true)
            {
                lstReturn.Add(new CPluginVariable("2. Word Warnings|   Kicking: Infractions Before Kick", this.iDbeforekick.GetType(), this.iDbeforekick));
                lstReturn.Add(new CPluginVariable("2. Word Warnings|   Kicking: Kick Message", this.m_strKickMessage.GetType(), this.m_strKickMessage));
                lstReturn.Add(new CPluginVariable("2. Word Warnings|   Kicking: Drop Infractions When Kicked?", typeof(enumBoolYesNo), this.blDDropKick));
                lstReturn.Add(new CPluginVariable("2. Word Warnings|   Repeat Offender: Enable?", typeof(enumBoolYesNo), this.blDBanRepeat));
                if (this.blDBanRepeat == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("2. Word Warnings|   Repeat Offender: Kicks Before Ban?", this.iDRepeatbeforeban.GetType(), this.iDRepeatbeforeban));
                    lstReturn.Add(new CPluginVariable("2. Word Warnings|   Repeat Offender: Ban Method", "enum.DRBanMethod(Name|PB GUID|EA GUID)", this.strDRBanMethod));
                    lstReturn.Add(new CPluginVariable("2. Word Warnings|   Repeat Offender: Ban Type", "enum.DRBanType(Temporary|Permanent)", this.strDRTempBan));
                    if (this.strDRTempBan.Equals("Temporary") == true)
                    {
                        lstReturn.Add(new CPluginVariable("2. Word Warnings|   Repeat Offender: Temporary Ban: Time Units", "enum.DRTBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strDRTBanUnits));
                        lstReturn.Add(new CPluginVariable("2. Word Warnings|   Repeat Offender: Temporary Ban: Ban Time", this.iDRBanTime.GetType(), this.iDRBanTime));
                        lstReturn.Add(new CPluginVariable("2. Word Warnings|   Repeat Offender: Temporary Ban: Ban Message", this.m_strDRBan.GetType(), this.m_strDRBan));
                    }
                    else if (this.strDRTempBan.Equals("Permanent") == true)
                    {
                        lstReturn.Add(new CPluginVariable("2. Word Warnings|   Repeat Offender: Ban Message", this.m_strDRPBan.GetType(), this.m_strDRPBan));
                    }
                }
            }
            if (this.strDBanOrKick.Equals("Ban") == true)
            {
                lstReturn.Add(new CPluginVariable("2. Word Warnings|   Bannings: Infractions Before Ban", this.iDbeforeban.GetType(), this.iDbeforeban));
                lstReturn.Add(new CPluginVariable("2. Word Warnings|   Bannings: Ban Method", "enum.DRBanMethod(Name|PB GUID|EA GUID)", this.strDBanMethod));
                lstReturn.Add(new CPluginVariable("2. Word Warnings|   Bannings: Ban Type", "enum.DBanBanType(Temporary|Permanent)", this.strDTempBan));
                if (this.strDTempBan.Equals("Temporary") == true)
                {
                    lstReturn.Add(new CPluginVariable("2. Word Warnings|   Bannings: Temporary Ban: Time Units", "enum.DTBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strDTBanUnits));
                    lstReturn.Add(new CPluginVariable("2. Word Warnings|   Bannings: Temporary Ban: Ban Time", this.iDBanTime.GetType(), this.iDBanTime));
                    lstReturn.Add(new CPluginVariable("2. Word Warnings|   Bannings: Temporary Ban: Ban Message", this.m_strDBan.GetType(), this.m_strDBan));
                }
                else if (this.strDTempBan.Equals("Permanent") == true)
                {
                    lstReturn.Add(new CPluginVariable("2. Word Warnings|   Bannings: Ban Message", this.m_strDPBan.GetType(), this.m_strDPBan));
                }
            }
            lstReturn.Add(new CPluginVariable("3. Hack Whiners|Enable Hack Whine Filter", typeof(enumBoolYesNo), this.blHackWarn));
            if (this.blHackWarn == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("3. Hack Whiners|Words", this.HWords.GetType(), this.HWords));
                lstReturn.Add(new CPluginVariable("3. Hack Whiners|Whitelist", this.HWhitelist.GetType(), this.HWhitelist));
                lstReturn.Add(new CPluginVariable("3. Hack Whiners|Enable Report Command immunity?", typeof(enumBoolYesNo), this.blGiveImmunity));
                if (blGiveImmunity == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|Clear Whines on Report Command?", typeof(enumBoolYesNo), this.blClearOnImmune));
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|Report Commands", this.CommandKeys.GetType(), this.CommandKeys));
                }
                lstReturn.Add(new CPluginVariable("3. Hack Whiners|Drop Whines When Leaving?", typeof(enumBoolYesNo), this.blHDrop));
                lstReturn.Add(new CPluginVariable("3. Hack Whiners|Whine Clear Command", this.m_strHClearCommand.GetType(), this.m_strHClearCommand));
                lstReturn.Add(new CPluginVariable("3. Hack Whiners|Say Warnings: Enable?", typeof(enumBoolYesNo), this.blHbeforesay));
                if (this.blHbeforesay == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|Say Warnings: Whines Before Say", this.iHbeforesay.GetType(), this.iHbeforesay));
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|Say Warnings: Say Message", this.m_strHServerSay.GetType(), this.m_strHServerSay));
                }
                lstReturn.Add(new CPluginVariable("3. Hack Whiners|Whine Final Action", "enum.WhineChooseFinalAction(None|Kick|Ban)", this.strHBanOrKick));
                if (this.strHBanOrKick.Equals("Kick") == true) //(this.blHbeforekick == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Kicking: Whines Before Kick", this.iHbeforekick.GetType(), this.iHbeforekick));
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Kicking: Message", this.m_strHackerKick.GetType(), this.m_strHackerKick));
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Kicking: Drop Whines When Kicked?", typeof(enumBoolYesNo), this.blHDropKick));
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Repeat Offenders: Enable?", typeof(enumBoolYesNo), this.blHBanRepeat));
                    if (this.blHBanRepeat == enumBoolYesNo.Yes)
                    {
                        lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Repeat Offenders: Kicks Before Ban?", this.iHRepeatbeforeban.GetType(), this.iHRepeatbeforeban));
                        lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Repeat Offenders: Ban Method", "enum.DRBanMethod(Name|PB GUID|EA GUID)", this.strHRBanMethod));
                        lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Repeat Offenders: Ban Type", "enum.HRBanType(Temporary|Permanent)", this.strHRTempBan));
                        if (this.strHRTempBan.Equals("Temporary") == true)
                        {
                            lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Repeat Offenders: Temporary Ban: Time Units", "enum.HRTBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strHRTBanUnits));
                            lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Repeat Offenders: Temporary Ban: Ban Time", this.iHRBanTime.GetType(), this.iHRBanTime));
                            lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Repeat Offenders: Temporary Ban: Ban Message", this.m_strHRBan.GetType(), this.m_strHRBan));
                        }
                        else if (this.strHRTempBan.Equals("Permanent") == true)
                        {
                            lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Repeat Offenders: Ban Message", this.m_strHRPBan.GetType(), this.m_strHRPBan));
                        }
                    }
                }
                if (this.strHBanOrKick.Equals("Ban") == true)
                {
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Banning: Whines Before Ban", this.iHbeforeban.GetType(), this.iHbeforeban));
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Banning: Ban Method", "enum.DRBanMethod(Name|PB GUID|EA GUID)", this.strHBanMethod));
                    lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Banning: Ban Type", "enum.HBanBanType(Temporary|Permanent)", this.strHTempBan));
                    if (this.strHTempBan.Equals("Temporary") == true)
                    {
                        lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Banning: Temporary Ban: Time Units", "enum.HTBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strHTBanUnits));
                        lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Banning: Temporary Ban: Ban Time", this.iHBanTime.GetType(), this.iHBanTime));
                        lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Banning: Temporary Ban: Ban Message", this.m_strHBan.GetType(), this.m_strHBan));
                    }
                    else if (this.strHTempBan.Equals("Permanent") == true)
                    {
                        lstReturn.Add(new CPluginVariable("3. Hack Whiners|   Banning: Ban Message", this.m_strHPBan.GetType(), this.m_strHPBan));
                    }
                }
            }
            lstReturn.Add(new CPluginVariable("4. Announcements|Announce Bans to all players?", typeof(enumBoolYesNo), this.blBanAnnounce));
            if (this.blBanAnnounce == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("4. Announcements|Ban Announcement Type", "enum.BanAnnounce(Say|Yell|Both)", this.strBanAnnounceType));
                if (this.strBanAnnounceType.Equals("Yell") || this.strBanAnnounceType.Equals("Both"))
                {
                    lstReturn.Add(new CPluginVariable("4. Announcements|Ban Yell Announce Duration (seconds)", this.iBAYellTime.GetType(), this.iBAYellTime));
                }

                if (this.strTempBan.Equals("Temporary") == true)
                {
                    lstReturn.Add(new CPluginVariable("4. Announcements|Ban Words: Temp Ban Announcement", this.strTBanAnnounce.GetType(), this.strTBanAnnounce));
                }
                else if (this.strTempBan.Equals("Permanent") == true)
                {
                    lstReturn.Add(new CPluginVariable("4. Announcements|Ban Words: Ban Announcement", this.strBanAnnounce.GetType(), this.strBanAnnounce));
                }

                if (strDBanOrKick.Equals("Ban") || (blDBanRepeat == enumBoolYesNo.Yes && strDBanOrKick.Equals("Kick")))
                {
                    if ((this.strDTempBan.Equals("Temporary") && this.strDBanOrKick.Equals("Ban")) || (this.strDRTempBan.Equals("Temporary") && blDBanRepeat == enumBoolYesNo.Yes && strDBanOrKick.Equals("Kick")))
                    {
                        lstReturn.Add(new CPluginVariable("4. Announcements|Bad Language: Temp Ban Announcement", this.strTDBanAnnounce.GetType(), this.strTDBanAnnounce));
                    }
                    else if ((this.strDTempBan.Equals("Permanent") && this.strDBanOrKick.Equals("Ban")) || (this.strDRTempBan.Equals("Permanent") && blDBanRepeat == enumBoolYesNo.Yes && strDBanOrKick.Equals("Kick")))
                    {
                        lstReturn.Add(new CPluginVariable("4. Announcements|Bad Language: Ban Announcement", this.strDBanAnnounce.GetType(), this.strDBanAnnounce));
                    }
                }

                if (blHackWarn == enumBoolYesNo.Yes && (this.strHBanOrKick.Equals("Ban") || (this.strHBanOrKick.Equals("Kick") && this.blHBanRepeat == enumBoolYesNo.Yes)))
                {
                    if ((this.strHTempBan.Equals("Temporary") && this.strHBanOrKick.Equals("Ban")) || (this.strHRTempBan.Equals("Temporary") && this.strHBanOrKick.Equals("Kick") && this.blHBanRepeat == enumBoolYesNo.Yes))
                    {
                        lstReturn.Add(new CPluginVariable("4. Announcements|Whiners: Temp Ban Announcement", this.strTHBanAnnounce.GetType(), this.strTHBanAnnounce));
                    }
                    if ((this.strHTempBan.Equals("Permanent") && this.strHBanOrKick.Equals("Ban")) || (this.strHRTempBan.Equals("Permanent") && this.strHBanOrKick.Equals("Kick") && this.blHBanRepeat == enumBoolYesNo.Yes))
                    {
                        lstReturn.Add(new CPluginVariable("4. Announcements|Whiners: Ban Announcement", this.strHBanAnnounce.GetType(), this.strHBanAnnounce));
                    }
                }
            }
            if ((this.strHBanOrKick.Equals("Kick") == true && blHackWarn == enumBoolYesNo.Yes) || this.strDBanOrKick.Equals("Kick") == true)
            {
                lstReturn.Add(new CPluginVariable("4. Announcements|Announce Kicks to all players?", typeof(enumBoolYesNo), this.blKickAnnounce));
                if (this.blKickAnnounce == enumBoolYesNo.Yes)
                {
                    lstReturn.Add(new CPluginVariable("4. Announcements|Kick Announcement Type", "enum.KickAnnounce(Say|Yell|Both)", this.strKickAnnounceType));
                    if (this.strKickAnnounceType.Equals("Yell") || this.strKickAnnounceType.Equals("Both"))
                    {
                        lstReturn.Add(new CPluginVariable("4. Announcements|Kick Yell Announce Duration (seconds)", this.iKAYellTime.GetType(), this.iKAYellTime));
                    }
                    if (this.strDBanOrKick.Equals("Kick"))
                    {
                        lstReturn.Add(new CPluginVariable("4. Announcements|Bad Language: Kick Announcement", this.strDKickAnnounce.GetType(), this.strDKickAnnounce));
                    }
                    if (this.strHBanOrKick.Equals("Kick") && blHackWarn == enumBoolYesNo.Yes)
                    {
                        lstReturn.Add(new CPluginVariable("4. Announcements|Whiners: Kick Announcement", this.strHKickAnnounce.GetType(), this.strHKickAnnounce));
                    }
                }
            }
            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Protected Names", this.ProtectedNames.GetType(), this.ProtectedNames));
            lstReturn.Add(new CPluginVariable("Protected Clan Tags", this.ProtectedTags.GetType(), this.ProtectedTags));
            lstReturn.Add(new CPluginVariable("Protect Account Holders?", typeof(enumBoolYesNo), this.blProtectAdmins));
            lstReturn.Add(new CPluginVariable("Ban Words", this.BWords.GetType(), this.BWords));
            lstReturn.Add(new CPluginVariable("Ban Type", "enum.BanType(Temporary|Permanent)", this.strTempBan));
            lstReturn.Add(new CPluginVariable("Temporary Ban: Time Units", "enum.TBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strTBanUnits));
            lstReturn.Add(new CPluginVariable("Temporary Ban: Ban Time", this.iBanTime.GetType(), this.iBanTime));
            lstReturn.Add(new CPluginVariable("Temporary Ban: Ban Message", this.m_strBanMessage.GetType(), this.m_strBanMessage));
            lstReturn.Add(new CPluginVariable("Ban Message", this.m_strPBanMessage.GetType(), this.m_strPBanMessage));
            lstReturn.Add(new CPluginVariable("Bad Words", this.DWords.GetType(), this.DWords));
            lstReturn.Add(new CPluginVariable("Bad Words (Literal)", this.DWordsforWord.GetType(), this.DWordsforWord));
            lstReturn.Add(new CPluginVariable("Drop Infractions When Leaving?", typeof(enumBoolYesNo), this.blDDrop));
            lstReturn.Add(new CPluginVariable("Infraction Clear Command", this.m_strDClearCommand.GetType(), this.m_strDClearCommand));
            lstReturn.Add(new CPluginVariable("Say Warning: Enable?", typeof(enumBoolYesNo), this.blDbeforesay));
            lstReturn.Add(new CPluginVariable("Say Warning: Infractions Before Say", this.iDbeforesay.GetType(), this.iDbeforesay));
            lstReturn.Add(new CPluginVariable("Say Warning: Say Message", this.m_strDServerSay.GetType(), this.m_strDServerSay));
            lstReturn.Add(new CPluginVariable("Bad Word Final Action", "enum.BWordChooseFinalAction(Kick)", this.strDBanOrKick));
            lstReturn.Add(new CPluginVariable("Bad Word Final Action", "enum.BWordChooseFinalAction(None|Kick|Ban)", this.strDBanOrKick));
            lstReturn.Add(new CPluginVariable("   Kicking: Infractions Before Kick", this.iDbeforekick.GetType(), this.iDbeforekick));
            lstReturn.Add(new CPluginVariable("   Kicking: Kick Message", this.m_strKickMessage.GetType(), this.m_strKickMessage));
            lstReturn.Add(new CPluginVariable("   Kicking: Drop Infractions When Kicked?", typeof(enumBoolYesNo), this.blDDropKick));
            lstReturn.Add(new CPluginVariable("   Repeat Offender: Enable?", typeof(enumBoolYesNo), this.blDBanRepeat));
            lstReturn.Add(new CPluginVariable("   Repeat Offender: Kicks Before Ban?", this.iDRepeatbeforeban.GetType(), this.iDRepeatbeforeban));
            lstReturn.Add(new CPluginVariable("   Repeat Offender: Ban Method", "enum.DRBanMethod(Name|PB GUID|EA GUID)", this.strDRBanMethod));
            lstReturn.Add(new CPluginVariable("   Repeat Offender: Ban Type", "enum.DRBanType(Temporary|Permanent)", this.strDRTempBan));
            lstReturn.Add(new CPluginVariable("   Repeat Offender: Temporary Ban: Time Units", "enum.DRTBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strDRTBanUnits));
            lstReturn.Add(new CPluginVariable("   Repeat Offender: Temporary Ban: Ban Time", this.iDRBanTime.GetType(), this.iDRBanTime));
            lstReturn.Add(new CPluginVariable("   Repeat Offender: Temporary Ban: Ban Message", this.m_strDRBan.GetType(), this.m_strDRBan));
            lstReturn.Add(new CPluginVariable("   Repeat Offender: Ban Message", this.m_strDRPBan.GetType(), this.m_strDRPBan));
            lstReturn.Add(new CPluginVariable("   Bannings: Infractions Before Ban", this.iDbeforeban.GetType(), this.iDbeforeban));
            lstReturn.Add(new CPluginVariable("   Bannings: Ban Method", "enum.DRBanMethod(Name|PB GUID|EA GUID)", this.strDBanMethod));
            lstReturn.Add(new CPluginVariable("   Bannings: Ban Type", "enum.DBanBanType(Temporary|Permanent)", this.strDTempBan));
            lstReturn.Add(new CPluginVariable("   Bannings: Temporary Ban: Time Units", "enum.DTBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strDTBanUnits));
            lstReturn.Add(new CPluginVariable("   Bannings: Temporary Ban: Ban Time", this.iDBanTime.GetType(), this.iDBanTime));
            lstReturn.Add(new CPluginVariable("   Bannings: Temporary Ban: Ban Message", this.m_strDBan.GetType(), this.m_strDBan));
            lstReturn.Add(new CPluginVariable("   Bannings: Ban Message", this.m_strDPBan.GetType(), this.m_strDPBan));
            lstReturn.Add(new CPluginVariable("Enable Hack Whine Filter", typeof(enumBoolYesNo), this.blHackWarn));
            lstReturn.Add(new CPluginVariable("Words", this.HWords.GetType(), this.HWords));
            lstReturn.Add(new CPluginVariable("Whitelist", this.HWhitelist.GetType(), this.HWhitelist));
            lstReturn.Add(new CPluginVariable("Drop Whines When Leaving?", typeof(enumBoolYesNo), this.blHDrop));
            lstReturn.Add(new CPluginVariable("Whine Clear Command", this.m_strHClearCommand.GetType(), this.m_strHClearCommand));
            lstReturn.Add(new CPluginVariable("Say Warnings: Enable?", typeof(enumBoolYesNo), this.blHbeforesay));
            lstReturn.Add(new CPluginVariable("Say Warnings: Whines Before Say", this.iHbeforesay.GetType(), this.iHbeforesay));
            lstReturn.Add(new CPluginVariable("Say Warnings: Say Message", this.m_strHServerSay.GetType(), this.m_strHServerSay));
            lstReturn.Add(new CPluginVariable("Whine Final Action", "enum.WhineChooseFinalAction(None|Kick|Ban)", this.strHBanOrKick));
            lstReturn.Add(new CPluginVariable("   Kicking: Whines Before Kick", this.iHbeforekick.GetType(), this.iHbeforekick));
            lstReturn.Add(new CPluginVariable("   Kicking: Message", this.m_strHackerKick.GetType(), this.m_strHackerKick));
            lstReturn.Add(new CPluginVariable("   Kicking: Drop Whines When Kicked?", typeof(enumBoolYesNo), this.blHDropKick));
            lstReturn.Add(new CPluginVariable("   Repeat Offenders: Enable?", typeof(enumBoolYesNo), this.blHBanRepeat));
            lstReturn.Add(new CPluginVariable("   Repeat Offenders: Kicks Before Ban?", this.iHRepeatbeforeban.GetType(), this.iHRepeatbeforeban));
            lstReturn.Add(new CPluginVariable("   Repeat Offenders: Ban Method", "enum.DRBanMethod(Name|PB GUID|EA GUID)", this.strHRBanMethod));
            lstReturn.Add(new CPluginVariable("   Repeat Offenders: Ban Type", "enum.HRBanType(Temporary|Permanent)", this.strHRTempBan));
            lstReturn.Add(new CPluginVariable("   Repeat Offenders: Temporary Ban: Time Units", "enum.HRTBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strHRTBanUnits));
            lstReturn.Add(new CPluginVariable("   Repeat Offenders: Temporary Ban: Ban Time", this.iHRBanTime.GetType(), this.iHRBanTime));
            lstReturn.Add(new CPluginVariable("   Repeat Offenders: Temporary Ban: Ban Message", this.m_strHRBan.GetType(), this.m_strHRBan));
            lstReturn.Add(new CPluginVariable("   Repeat Offenders: Ban Message", this.m_strHRPBan.GetType(), this.m_strHRPBan));
            lstReturn.Add(new CPluginVariable("   Banning: Whines Before Ban", this.iHbeforeban.GetType(), this.iHbeforeban));
            lstReturn.Add(new CPluginVariable("   Banning: Ban Method", "enum.DRBanMethod(Name|PB GUID|EA GUID)", this.strHBanMethod));
            lstReturn.Add(new CPluginVariable("   Banning: Ban Type", "enum.HBanBanType(Temporary|Permanent)", this.strHTempBan));
            lstReturn.Add(new CPluginVariable("   Banning: Temporary Ban: Time Units", "enum.HTBanUnits(Minutes|Hours|Days|Weeks|Months)", this.strHTBanUnits));
            lstReturn.Add(new CPluginVariable("   Banning: Temporary Ban: Ban Time", this.iHBanTime.GetType(), this.iHBanTime));
            lstReturn.Add(new CPluginVariable("   Banning: Temporary Ban: Ban Message", this.m_strHBan.GetType(), this.m_strHBan));
            lstReturn.Add(new CPluginVariable("   Banning: Ban Message", this.m_strHPBan.GetType(), this.m_strHPBan));
            lstReturn.Add(new CPluginVariable("Announce Bans to all players?", typeof(enumBoolYesNo), this.blBanAnnounce));
            lstReturn.Add(new CPluginVariable("Ban Announcement Type", "enum.TBanUnits(Say|Yell)", this.strBanAnnounceType));
            lstReturn.Add(new CPluginVariable("Bad Language: Temp Ban Announcement", this.strTDBanAnnounce.GetType(), this.strTDBanAnnounce));
            lstReturn.Add(new CPluginVariable("Bad Language: Ban Announcement", this.strDBanAnnounce.GetType(), this.strDBanAnnounce));
            lstReturn.Add(new CPluginVariable("Whiners: Temp Ban Announcement", this.strTHBanAnnounce.GetType(), this.strTHBanAnnounce));
            lstReturn.Add(new CPluginVariable("Whiners: Ban Announcement", this.strHBanAnnounce.GetType(), this.strHBanAnnounce));
            lstReturn.Add(new CPluginVariable("Announce Kicks to all players?", typeof(enumBoolYesNo), this.blKickAnnounce));
            lstReturn.Add(new CPluginVariable("Kick Announcement Type", "enum.TBanUnits(Say|Yell)", this.strKickAnnounceType));
            lstReturn.Add(new CPluginVariable("Bad Language: Kick Announcement", this.strDKickAnnounce.GetType(), this.strDKickAnnounce));
            lstReturn.Add(new CPluginVariable("Whiners: Kick Announcement", this.strHKickAnnounce.GetType(), this.strHKickAnnounce));
            lstReturn.Add(new CPluginVariable("Bad Language: Mute Announcement", this.strDMuteAnnounce.GetType(), this.strDMuteAnnounce));
            lstReturn.Add(new CPluginVariable("Report Commands", this.CommandKeys.GetType(), this.CommandKeys));
            lstReturn.Add(new CPluginVariable("Enable Report Command immunity?", typeof(enumBoolYesNo), this.blGiveImmunity));
            lstReturn.Add(new CPluginVariable("Clear Whines on Report Command?", typeof(enumBoolYesNo), this.blClearOnImmune));
            lstReturn.Add(new CPluginVariable("Ban Words: Ban Announcement", this.strBanAnnounce.GetType(), this.strBanAnnounce));
            lstReturn.Add(new CPluginVariable("Ban Words: Temp Ban Announcement", this.strTBanAnnounce.GetType(), this.strTBanAnnounce));

            return lstReturn;
        }

        // Allways be suspicious of strValue's actual value.  A command in the console can
        // by the user can put any kind of data it wants in strValue.
        // use type.TryParse
        public void SetPluginVariable(string strVariable, string strValue)
        {

            //int iDelayOverride = 30;
            int intOut = 0;
            if (strVariable.Equals("Protected Names") == true)
            {
                this.ProtectedNames = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable.Equals("Ban Words: Temp Ban Announcement") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.strTBanAnnounce = strValue;
            }
            else if (strVariable.Equals("Ban Words: Ban Announcement") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.strBanAnnounce = strValue;
            }
            else if (strVariable.Equals("Clear Whines on Report Command?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blClearOnImmune = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Enable Report Command immunity?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blGiveImmunity = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Report Commands") == true)
            {
                this.CommandKeys = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable.Equals("Whiners: Kick Announcement") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.strHKickAnnounce = strValue;
            }
            else if (strVariable.Equals("Bad Language: Kick Announcement") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.strDKickAnnounce = strValue;
            }
            else if (strVariable.Equals("Announce Kicks to all players?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blKickAnnounce = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Kick Announcement Type") == true)
            {
                this.strKickAnnounceType = strValue;
            }
            else if (strVariable.Equals("Whiners: Ban Announcement") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.strHBanAnnounce = strValue;
            }
            else if (strVariable.Equals("Whiners: Temp Ban Announcement") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.strTHBanAnnounce = strValue;
            }
            else if (strVariable.Equals("Bad Language: Ban Announcement") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.strDBanAnnounce = strValue;
            }
            else if (strVariable.Equals("Announce Bans to all players?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blBanAnnounce = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Ban Announcement Type") == true)
            {
                this.strBanAnnounceType = strValue;
            }
            else if (strVariable.Equals("Bad Language: Temp Ban Announcement") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.strTDBanAnnounce = strValue;
            }
            else if (strVariable.Equals("Protected Clan Tags") == true)
            {
                this.ProtectedTags = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable.Equals("Protect Account Holders?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blProtectAdmins = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Ban Words") == true)
            {
                this.BWords = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable.Equals("Ban Method") == true)
            {
                this.strBanMethod = "Name";
            }
            else if (strVariable.Equals("Ban Type") == true)
            {
                this.strTempBan = strValue;
            }
            else if (strVariable.Equals("Temporary Ban: Time Units") == true)
            {
                this.strTBanUnits = strValue;
            }
            else if (strVariable.Equals("Temporary Ban: Ban Time") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iBanTime = int.Parse(strValue);
            }
            else if (strVariable.Equals("Temporary Ban: Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strBanMethod.Equals("PB GUID") == true))
            {
                this.m_strBanMessage = strValue;
            }
            else if (strVariable.Equals("Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strBanMethod.Equals("PB GUID") == true))
            {
                this.m_strPBanMessage = strValue;
            }
            else if (strVariable.Equals("Bad Words") == true)
            {
                this.DWords = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable.Equals("Bad Words (Literal)") == true)
            {
                this.DWordsforWord = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable.Equals("Drop Infractions When Leaving?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blDDrop = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Infraction Clear Command") == true)
            {
                this.m_strDClearCommand = strValue;
            }
            else if (strVariable.Equals("Say Warning: Enable?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blDbeforesay = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Say Warning: Infractions Before Say") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iDbeforesay = int.Parse(strValue);
            }
            else if (strVariable.Equals("Say Warning: Say Message") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.m_strDServerSay = strValue;
            }
            else if (strVariable.Equals("Bad Word Final Action") == true)
            {
                this.strDBanOrKick = strValue;
            }
            else if (strVariable.Equals("   Kicking: Infractions Before Kick") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iDbeforekick = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Kicking: Kick Message") == true)
            {
                this.m_strKickMessage = strValue;
            }
            else if (strVariable.Equals("   Kicking: Drop Infractions When Kicked?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blDDropKick = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("   Repeat Offender: Enable?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blDBanRepeat = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("   Repeat Offender: Kicks Before Ban?") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iDRepeatbeforeban = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Repeat Offender: Ban Method") == true)
            {
                this.strDRBanMethod = strValue;
            }
            else if (strVariable.Equals("   Repeat Offender: Ban Type") == true)
            {
                this.strDRTempBan = strValue;
            }
            else if (strVariable.Equals("   Repeat Offender: Temporary Ban: Time Units") == true)
            {
                this.strDRTBanUnits = strValue;
            }
            else if (strVariable.Equals("   Repeat Offender: Temporary Ban: Ban Time") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iDRBanTime = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Repeat Offender: Temporary Ban: Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strDRBanMethod.Equals("PB GUID") == true))
            {
                this.m_strDRBan = strValue;
            }
            else if (strVariable.Equals("   Repeat Offender: Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strDRBanMethod.Equals("PB GUID") == true))
            {
                this.m_strDRPBan = strValue;
            }
            else if (strVariable.Equals("   Bannings: Infractions Before Ban") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iDbeforeban = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Bannings: Ban Method") == true)
            {
                this.strDBanMethod = "Name";
            }
            else if (strVariable.Equals("   Bannings: Ban Type") == true)
            {
                this.strDTempBan = strValue;
            }
            else if (strVariable.Equals("   Bannings: Temporary Ban: Time Units") == true)
            {
                this.strDTBanUnits = strValue;
            }
            else if (strVariable.Equals("   Bannings: Temporary Ban: Ban Time") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iDBanTime = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Bannings: Temporary Ban: Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strDBanMethod.Equals("PB GUID") == true))
            {
                this.m_strDBan = strValue;
            }
            else if (strVariable.Equals("   Bannings: Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strDBanMethod.Equals("PB GUID") == true))
            {
                this.m_strDPBan = strValue;
            }
            else if (strVariable.Equals("Enable Hack Whine Filter") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blHackWarn = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Words") == true)
            {
                this.HWords = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable.Equals("Whitelist") == true)
            {
                this.HWhitelist = CPluginVariable.DecodeStringArray(strValue);
            }
            else if (strVariable.Equals("Drop Whines When Leaving?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blHDrop = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Whine Clear Command") == true)
            {
                this.m_strHClearCommand = strValue;
            }
            else if (strVariable.Equals("Say Warnings: Enable?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blHbeforesay = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("Say Warnings: Whines Before Say") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iHbeforesay = int.Parse(strValue);
            }
            else if (strVariable.Equals("Say Warnings: Say Message") == true && AdminCCheck("Speech", strValue) == true)
            {
                this.m_strHServerSay = strValue;
            }
            else if (strVariable.Equals("Whines Before Kick") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iHbeforekick = int.Parse(strValue);
            }
            else if (strVariable.Equals("Kicking Message") == true)
            {
                this.m_strHackerKick = strValue;
            }
            else if (strVariable.Equals("Whine Final Action") == true)
            {
                this.strHBanOrKick = strValue;
            }
            else if (strVariable.Equals("   Kicking: Whines Before Kick") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iHbeforekick = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Kicking: Message") == true)
            {
                this.m_strHackerKick = strValue;
            }
            else if (strVariable.Equals("   Kicking: Drop Whines When Kicked?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blHDropKick = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("   Repeat Offenders: Enable?") == true && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.blHBanRepeat = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.Equals("   Repeat Offenders: Kicks Before Ban?") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iHRepeatbeforeban = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Repeat Offenders: Ban Method") == true)
            {
                this.strHRBanMethod = strValue;
            }
            else if (strVariable.Equals("   Repeat Offenders: Ban Type") == true)
            {
                this.strHRTempBan = strValue;
            }
            else if (strVariable.Equals("   Repeat Offenders: Temporary Ban: Time Units") == true)
            {
                this.strHRTBanUnits = strValue;
            }
            else if (strVariable.Equals("   Repeat Offenders: Temporary Ban: Ban Time") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iHRBanTime = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Repeat Offenders: Temporary Ban: Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strHRBanMethod.Equals("PB GUID") == true))
            {
                this.m_strHRBan = strValue;
            }
            else if (strVariable.Equals("   Repeat Offenders: Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strHRBanMethod.Equals("PB GUID") == true))
            {
                this.m_strHRPBan = strValue;
            }
            else if (strVariable.Equals("   Banning: Whines Before Ban") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iHbeforeban = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Banning: Ban Method") == true)
            {
                this.strHBanMethod = "Name";
            }
            else if (strVariable.Equals("   Banning: Ban Type") == true)
            {
                this.strHTempBan = strValue;
            }
            else if (strVariable.Equals("   Banning: Temporary Ban: Time Units") == true)
            {
                this.strHTBanUnits = strValue;
            }
            else if (strVariable.Equals("   Banning: Temporary Ban: Ban Time") == true && int.TryParse(strValue, out intOut) == true)
            {
                this.iHBanTime = int.Parse(strValue);
            }
            else if (strVariable.Equals("   Banning: Temporary Ban: Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strHBanMethod.Equals("PB GUID") == true))
            {
                this.m_strHBan = strValue;
            }
            else if (strVariable.Equals("   Banning: Ban Message") == true && (AdminCCheck("Removal", strValue) == true || strHBanMethod.Equals("PB GUID") == true))
            {
                this.m_strHPBan = strValue;
            }

            this.iBadYellTime = iDYellTime * 1000;
            this.iHackYellTime = iHYellTime * 1000;
            this.iDMinMute = iDMuteTime * 60;
            this.iHMinMute = iHMuteTime * 60;
            this.TBanUnitHandler();
            this.RegisterAllCommands();

        }

        public bool AdminCCheck(string strType, string strValue)
        {
            if (strType.Equals("Speech") == true && (strValue.Replace("%warnings%", "0").Replace("%totalwarnings%", "0").Replace("%pn%", "1111111111").Replace("%bt", "11")).Length <= 100)
            {
                return true;
            }
            else if (strType.Equals("Removal") == true && strValue.Length <= 80)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region AnnounceHandler
        public void AnnounceHandler(string SoldierName, string Type1, string Type2)
        {
            if (Type1.Equals("Ban") && blBanAnnounce == enumBoolYesNo.Yes)
            {
                if (Type2.Equals("TempBL"))
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", strTDBanAnnounce.Replace("%pn%", SoldierName).Replace("%bt%", iBanTime.ToString()), "all");
                }
                else if (Type2.Equals("TempHW"))
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", strTHBanAnnounce.Replace("%pn%", SoldierName).Replace("%bt%", iHBanTime.ToString()), "all");
                }
                else if (Type2.Equals("BL"))
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", strDBanAnnounce.Replace("%pn%", SoldierName), "all");
                }
                else if (Type2.Equals("HW"))
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", strHBanAnnounce.Replace("%pn%", SoldierName), "all");
                }
                else if (Type2.Equals("TempDBL"))
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", strTDBanAnnounce.Replace("%pn%", SoldierName).Replace("%bt%", iDBanTime.ToString()), "all");
                }
                else if (Type2.Equals("TempB"))
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", strTBanAnnounce.Replace("%pn%", SoldierName).Replace("%bt%", iBanTime.ToString()), "all");
                }
                else if (Type2.Equals("B"))
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", strBanAnnounce.Replace("%pn%", SoldierName), "all");
                }
            }
            else if (Type1.Equals("Kick") && blKickAnnounce == enumBoolYesNo.Yes)
            {
                if (Type2.Equals("BL"))
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", strDKickAnnounce.Replace("%pn%", SoldierName), "all");
                }
                else if (Type2.Equals("HW"))
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", strHKickAnnounce.Replace("%pn%", SoldierName), "all");
                }
            }
        }
        #endregion

        #region RunProfanityFilter
        public void RunProfanityFilter(string strSpeaker, string strMessage)
        {
            bool blProtected = false;
            CPrivileges CPSpeaker = this.GetAccountPrivileges(strSpeaker);

            foreach (string pname in ProtectedNames)
            {
                if ((strSpeaker.ToLower()).Equals((pname.ToLower())) == true && string.IsNullOrEmpty(pname) == false)
                {
                    blProtected = true;
                }
            }
            foreach (string ptag in ProtectedTags)
            {
                if (string.IsNullOrEmpty(ptag) == false && (ptag.ToLower()).Equals((m_dicPlayerInfo[strSpeaker].ClanTag.ToLower())) == true)
                {
                    blProtected = true;
                }
            }
            if (this.blProtectAdmins == enumBoolYesNo.Yes && CPSpeaker != null && CPSpeaker.PrivilegesFlags >= 8328)
            {
                blProtected = true;
            }

            if (blProtected == false)
            {
                RunLiteralScan(strSpeaker, strMessage);
                RunBadWordScan(strSpeaker, strMessage);

                if (blHackWarn == enumBoolYesNo.Yes)
                {
                    RunHackWhineScan(strSpeaker, strMessage);
                }
            }
        }
        #endregion

        #region RunLiteralScan
        public void RunLiteralScan(string strSpeaker, string strMessage)
        {
            string[] messageWords = strMessage.Split(new Char[] { ' ' });

            bool Bloopbreak = false;
            bool Dloopbreak = false;


            foreach (string words in messageWords)
            {
                foreach (string match in BWords)
                {
                    if (string.IsNullOrEmpty(match) == false)
                    {
                        if ((match.ToLower()).Equals((words.ToLower())) == true)
                        {
                            Bloopbreak = true;
                            if (this.strTempBan.Equals("Temporary") == true)
                            {
                                this.BanHandler(strSpeaker, "Name", strTempBan, m_strBanMessage, iBanTime, minsBanTime);
                                //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "seconds", (this.minsBanTime).ToString(), m_strBanMessage.Replace("%bt%", iBanTime.ToString()));
                                this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Temp Banned for Offensive Language! (" + match + ")").Replace("%pn%", strSpeaker));
                                this.ExecuteCommand("procon.protected.pluginconsole.write", strSpeaker + ": " + strMessage);
                                AnnounceHandler(strSpeaker, "Ban", "TempB");
                            }
                            else if (this.strTempBan.Equals("Permanent") == true)
                            {
                                this.BanHandler(strSpeaker, "Name", strTempBan, m_strPBanMessage, iBanTime, minsBanTime);
                                //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "perm", m_strPBanMessage);
                                this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Banned for Offensive Language! (" + match + ")").Replace("%pn%", strSpeaker));
                                this.ExecuteCommand("procon.protected.pluginconsole.write", strSpeaker + ": " + strMessage);
                                AnnounceHandler(strSpeaker, "Ban", "B");
                            }
                            RunListRemover(strSpeaker);
                            break;
                        }
                    }
                }
                if (Bloopbreak == true)
                {
                    break;
                }

                foreach (string Dword in DWordsforWord)
                {

                    if (string.IsNullOrEmpty(Dword) == false)
                    {
                        if ((Dword.ToLower()).Equals((words.ToLower())) == true)
                        {
                            RunInfractionCheck(strSpeaker, "BadWord", Dword);
                            break;
                        }
                    }

                    if (Dloopbreak == true)
                    {
                        break;
                    }
                }
            }
        }
        #endregion

        #region RunBadWordScan
        public void RunBadWordScan(string strSpeaker, string strMessage)
        {
            foreach (string Dword in DWords)
            {
                if (string.IsNullOrEmpty(Dword) == false)
                {
                    Match DwordMatch;
                    DwordMatch = Regex.Match(strMessage, Dword, RegexOptions.IgnoreCase);
                    if (DwordMatch.Success == true) //((Dword.ToLower()).CompareTo((words.ToLower())) == 0)
                    {
                        RunInfractionCheck(strSpeaker, "BadWord", Dword);
                    }
                }
            }
        }
        #endregion

        #region RunHackWhineScan
        public void RunHackWhineScan(string strSpeaker, string strMessage)
        {
            if (blGiveImmunity == enumBoolYesNo.Yes)
            {
                foreach (string Com in CommandKeys)
                {
                    Match WordMatch = Regex.Match(strMessage, Com, RegexOptions.IgnoreCase);

                    if (WordMatch.Success == true)
                    {
                        ImmunePeople.Add(strSpeaker);

                        if (m_dicHOffenders.ContainsKey(strSpeaker) && blClearOnImmune == enumBoolYesNo.Yes)
                        {
                            m_dicHOffenders.Remove(strSpeaker);
                        }
                    }
                }
            }

            if (ImmunePeople.Contains(strSpeaker) == false || blGiveImmunity == enumBoolYesNo.No)
            {
                bool Hwhitewordfound = false;

                foreach (string Hword in HWords)
                {
                    if (string.IsNullOrEmpty(Hword) == false)
                    {
                        Match HMatch;
                        HMatch = Regex.Match(strMessage, Hword, RegexOptions.IgnoreCase);
                        if (HMatch.Success == true)//((Hmatch.ToLower()).CompareTo((words.ToLower())) == 0)
                        {
                            foreach (string whiteword in HWhitelist)
                            {
                                if (string.IsNullOrEmpty(whiteword) == false)
                                {
                                    Match WMatch;
                                    WMatch = Regex.Match(strMessage, whiteword, RegexOptions.IgnoreCase);
                                    if (WMatch.Success == true)  //if ((whiteword.ToLower()).CompareTo((words.ToLower())) == 0)    						
                                    {
                                        Hwhitewordfound = true;
                                    }
                                }
                            }
                            if (Hwhitewordfound != true)
                            {
                                RunInfractionCheck(strSpeaker, "Whining", Hword);
                                break;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region RunInfractionCheck
        public void RunInfractionCheck(string strSpeaker, string strReason, string strBadWord)
        {
            if (strReason.Equals("BadWord") == true)
            {
                if (this.m_dicDOffenders.ContainsKey(strSpeaker) == true)
                {
                    this.m_dicDOffenders[strSpeaker] += 1;
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "Player " + strSpeaker + " flagged for infraction on word '" + strBadWord + ".' No. of infractions: " + m_dicDOffenders[strSpeaker]);
                }
                else
                {
                    this.m_dicDOffenders.Add(strSpeaker, 1);
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "Player " + strSpeaker + " added to infractions list on word '" + strBadWord + ".' No. of infractions: " + m_dicDOffenders[strSpeaker]);
                }
                RunOffenderHandler(strSpeaker);
            }
            else if (strReason.Equals("Whining") == true)
            {
                if (this.m_dicHOffenders.ContainsKey(strSpeaker) == true)
                {
                    this.m_dicHOffenders[strSpeaker] += 1;
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "Player " + strSpeaker + " flagged for whining on word '" + strBadWord + ".' No. of whines: " + m_dicHOffenders[strSpeaker]);
                }
                else
                {
                    this.m_dicHOffenders.Add(strSpeaker, 1);
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "Player " + strSpeaker + " added to whiners list on word '" + strBadWord + ".' No. of whines: " + m_dicHOffenders[strSpeaker]);
                }
                RunWhinerHandler(strSpeaker);
            }
        }
        #endregion

        #region RunOffenderHandler
        public void RunOffenderHandler(string strSpeaker)
        {
            if (this.m_dicDOffenders[strSpeaker] >= iDbeforekick && strDBanOrKick.Equals("Kick") == true)
            {
                if (this.m_dicDROffenders.ContainsKey(strSpeaker) == false)
                {
                    this.m_dicDROffenders.Add(strSpeaker, 0);
                }

                if (blDBanRepeat == enumBoolYesNo.Yes && m_dicDROffenders[strSpeaker] >= iDRepeatbeforeban)
                {
                    if (this.strDRTempBan.Equals("Temporary"))
                    {
                        AnnounceHandler(strSpeaker, "Ban", "TempDBL");
                        this.BanHandler(strSpeaker, "Name", strDRTempBan, m_strDRBan, iDRBanTime, DRminsBanTime);
                        //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "seconds", (this.DRminsBanTime).ToString(), m_strDRBan.Replace("%bt%", iDRBanTime.ToString()));
                        this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Temp Banned for repeated offenses of bad language!").Replace("%pn%", strSpeaker));
                    }
                    else if (this.strDRTempBan.Equals("Permanent"))
                    {
                        AnnounceHandler(strSpeaker, "Ban", "BL");
                        this.BanHandler(strSpeaker, "Name", strDRTempBan, m_strDRPBan, iDRBanTime, DRminsBanTime);
                        //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "perm", m_strDRPBan);
                        this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Banned for repeated offenses of bad language!").Replace("%pn%", strSpeaker));
                    }
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", strSpeaker, this.m_strKickMessage);

                    this.m_dicDROffenders[strSpeaker] += 1;

                    if (this.blDBanRepeat == enumBoolYesNo.No)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", strSpeaker + " has been kicked for bad language.");
                    }
                    else
                    {
                        if (this.m_dicDROffenders[strSpeaker] > 1)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "Player " + strSpeaker + " was kicked again for bad language. No. of kicks: " + m_dicDROffenders[strSpeaker]);
                        }
                        else
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "Player " + strSpeaker + " was kicked for bad language and added to repeat offenders list.");
                        }
                    }

                    AnnounceHandler(strSpeaker, "Kick", "BL");
                }
                RunListRemover(strSpeaker);
            }
            else if (this.m_dicDOffenders[strSpeaker] >= iDbeforeban && strDBanOrKick.Equals("Ban") == true)
            {
                if (this.strDTempBan.Equals("Temporary"))
                {
                    AnnounceHandler(strSpeaker, "Ban", "TempDBL");
                    this.BanHandler(strSpeaker, "Name", strDTempBan, m_strDBan, iDBanTime, DminsBanTime);
                    //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "seconds", (this.DminsBanTime).ToString(), m_strDBan.Replace("%bt%", iDBanTime.ToString()));
                    this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Temp Banned for bad language!").Replace("%pn%", strSpeaker));
                }
                else if (this.strDTempBan.Equals("Permanent"))
                {
                    AnnounceHandler(strSpeaker, "Ban", "BL");
                    this.BanHandler(strSpeaker, "Name", strDTempBan, m_strDPBan, iDBanTime, DminsBanTime);
                    //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "perm", m_strDPBan);
                    this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Banned for bad language!").Replace("%pn%", strSpeaker));
                }
                RunListRemover(strSpeaker);
            }
            if (this.blDbeforesay == enumBoolYesNo.Yes && this.m_dicDOffenders[strSpeaker] >= iDbeforesay)
            {
                if (this.strDBanOrKick.Equals("Kick") == true)
                {
                    int iDBK = iDbeforekick - iDbeforesay;
                    int iwarns = m_dicDOffenders[strSpeaker] - iDbeforesay + 1;
                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strDServerSay.Replace("%warnings%", iwarns.ToString()).Replace("%totalwarnings%", iDBK.ToString()), "player", strSpeaker);
                }
                else if (this.strDBanOrKick.Equals("Ban") == true)
                {
                    int iDBK = iDbeforeban - iDbeforesay;
                    int iwarns = m_dicDOffenders[strSpeaker] - iDbeforesay + 1;
                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strDServerSay.Replace("%warnings%", iwarns.ToString()).Replace("%totalwarnings%", iDBK.ToString()), "player", strSpeaker);
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strDServerSay, "player", strSpeaker);
                }
                this.ExecuteCommand("procon.protected.pluginconsole.write", strSpeaker + " has been say warned for bad language.");
            }

            if (this.blDbeforewarn == enumBoolYesNo.Yes && this.m_dicDOffenders[strSpeaker] >= iDbeforewarn)
            {
                if (this.strDBanOrKick.Equals("Kick") == true)
                {
                    int iDBK = iDbeforekick - iDbeforewarn;
                    int iwarns = m_dicDOffenders[strSpeaker] - iDbeforewarn + 1;
                    this.ExecuteCommand("procon.protected.send", "admin.yell", this.m_strBadWarn.Replace("%warnings%", iwarns.ToString()).Replace("%totalwarnings%", iDBK.ToString()), iBadYellTime.ToString(), "player", strSpeaker);
                }
                else if (this.strDBanOrKick.Equals("Ban") == true)
                {
                    int iDBK = iDbeforeban - iDbeforewarn;
                    int iwarns = m_dicDOffenders[strSpeaker] - iDbeforewarn + 1;
                    this.ExecuteCommand("procon.protected.send", "admin.yell", this.m_strBadWarn.Replace("%warnings%", iwarns.ToString()).Replace("%totalwarnings%", iDBK.ToString()), iBadYellTime.ToString(), "player", strSpeaker);
                }
                else if (this.strDBanOrKick.Equals("None") == true)
                {
                    if (this.blDMute == enumBoolYesNo.Yes)
                    {
                        int iDBK = iDbeforemute - iDbeforewarn;
                        int iwarns = m_dicDOffenders[strSpeaker] - iDbeforewarn + 1;
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strBadWarn.Replace("%warnings%", iwarns.ToString()).Replace("%totalwarnings%", iDBK.ToString()), "player", strSpeaker);
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strBadWarn, "player", strSpeaker);
                    }
                }
                this.ExecuteCommand("procon.protected.pluginconsole.write", strSpeaker + " has been yell warned for bad language.");
            }

        }
        #endregion
        #region RunWhinerHandler
        public void RunWhinerHandler(string strSpeaker)
        {
            if (this.m_dicHOffenders[strSpeaker] >= iHbeforekick && strHBanOrKick.Equals("Kick") == true)
            {
                if (this.m_dicHROffenders.ContainsKey(strSpeaker) == false)
                {
                    this.m_dicHROffenders.Add(strSpeaker, 0);
                }

                if (blHBanRepeat == enumBoolYesNo.Yes && m_dicHROffenders[strSpeaker] >= iHRepeatbeforeban)
                {
                    if (this.strHRTempBan.Equals("Temporary"))
                    {
                        AnnounceHandler(strSpeaker, "Ban", "TempHW");
                        this.BanHandler(strSpeaker, "Name", strHRTempBan, m_strHRBan, iHRBanTime, HRminsBanTime);
                        //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "seconds", (this.HRminsBanTime).ToString(), m_strHRBan.Replace("%bt%", iHRBanTime.ToString()));
                        this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Temp Banned for repeated offenses of whining!").Replace("%pn%", strSpeaker));
                    }
                    else if (this.strHRTempBan.Equals("Permanent"))
                    {
                        AnnounceHandler(strSpeaker, "Ban", "HW");
                        this.BanHandler(strSpeaker, "Name", strHRTempBan, m_strHRPBan, iHRBanTime, HRminsBanTime);
                        //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "perm", m_strDRPBan);
                        this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Banned for repeated offenses of whining!").Replace("%pn%", strSpeaker));
                    }
                }
                else
                {
                    this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", strSpeaker, this.m_strHackerKick);

                    this.m_dicHROffenders[strSpeaker] += 1;

                    if (this.blHBanRepeat == enumBoolYesNo.No)
                    {
                        this.ExecuteCommand("procon.protected.pluginconsole.write", strSpeaker + " has been kicked for whining about hackers.");
                    }
                    else
                    {
                        if (this.m_dicHROffenders[strSpeaker] > 1)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "Player " + strSpeaker + " was kicked again for whining. No. of kicks: " + m_dicHROffenders[strSpeaker]);
                        }
                        else
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", "Player " + strSpeaker + " was kicked for whining and added to repeat offenders list.");
                        }
                    }

                    AnnounceHandler(strSpeaker, "Kick", "HW");
                }
                RunListRemover(strSpeaker);
            }
            else if (this.m_dicHOffenders[strSpeaker] >= iHbeforeban && strHBanOrKick.Equals("Ban") == true)
            {
                if (this.strHTempBan.Equals("Temporary"))
                {
                    AnnounceHandler(strSpeaker, "Ban", "TempHW");
                    this.BanHandler(strSpeaker, strHBanMethod, strHTempBan, m_strHBan, iHBanTime, HminsBanTime);
                    //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "seconds", (this.HminsBanTime).ToString(), m_strHBan.Replace("%bt%", iHBanTime.ToString()));
                    this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Temp Banned for whining!").Replace("%pn%", strSpeaker));
                }
                else if (this.strHTempBan.Equals("Permanent"))
                {
                    AnnounceHandler(strSpeaker, "Ban", "HW");
                    this.BanHandler(strSpeaker, strHBanMethod, strHTempBan, m_strHPBan, iHBanTime, HminsBanTime);
                    //this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSpeaker, "perm", m_strHPBan);
                    this.ExecuteCommand("procon.protected.pluginconsole.write", ("%pn% has been Banned for whining!").Replace("%pn%", strSpeaker));
                }

                RunListRemover(strSpeaker);
            }
            if (this.blHbeforesay == enumBoolYesNo.Yes && this.m_dicHOffenders[strSpeaker] >= iHbeforesay)
            {
                if (this.strHBanOrKick.Equals("Kick") == true)
                {
                    int iDBK = iHbeforekick - iHbeforesay;
                    int iwarns = m_dicHOffenders[strSpeaker] - iHbeforesay + 1;
                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strHServerSay.Replace("%warnings%", iwarns.ToString()).Replace("%totalwarnings%", iDBK.ToString()), "player", strSpeaker);
                }
                else if (this.strHBanOrKick.Equals("Ban") == true)
                {
                    int iDBK = iHbeforeban - iHbeforesay;
                    int iwarns = m_dicHOffenders[strSpeaker] - iHbeforesay + 1;
                    this.ExecuteCommand("procon.protected.send", "admin.say", m_strHServerSay.Replace("%warnings%", iwarns.ToString()).Replace("%totalwarnings%", iDBK.ToString()), "player", strSpeaker);
                }
                else
                {
                    if (this.blHMute == enumBoolYesNo.Yes)
                    {
                        int iDBK = iHbeforemute - iHbeforesay;
                        int iwarns = m_dicHOffenders[strSpeaker] - iHbeforesay + 1;
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strHServerSay.Replace("%warnings%", iwarns.ToString()).Replace("%totalwarnings%", iDBK.ToString()), "player", strSpeaker);
                    }
                    else
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", m_strHServerSay, "player", strSpeaker);
                    }
                }
                this.ExecuteCommand("procon.protected.pluginconsole.write", strSpeaker + " has been say warned for whining about hackers.");
            }



        }
        #endregion

        #region RunListRemover
        public void RunListRemover(string strSpeaker)
        {
            if (this.m_dicDOffenders.ContainsKey(strSpeaker) == true)
            {
                if ((blDDropKick == enumBoolYesNo.No && this.m_dicDROffenders.ContainsKey(strSpeaker) == false && blDDrop == enumBoolYesNo.Yes) || (blDDropKick == enumBoolYesNo.Yes && blDDrop == enumBoolYesNo.Yes))
                {
                    this.m_dicDOffenders.Remove(strSpeaker);
                }
            }

            if (this.m_dicHOffenders.ContainsKey(strSpeaker) == true)
            {
                if ((blHDropKick == enumBoolYesNo.No && this.m_dicHROffenders.ContainsKey(strSpeaker) == false && blHDrop == enumBoolYesNo.Yes) || (blHDropKick == enumBoolYesNo.Yes && blHDrop == enumBoolYesNo.Yes))
                {
                    this.m_dicHOffenders.Remove(strSpeaker);
                }
            }


        }
        #endregion

        #region Ban Handler
        public void BanHandler(string strSoldierName, string strMethod, string strTempOrPerm, string strReason, int BanTime, int BanTimeReal)
        {
            if (strTempOrPerm.Equals("Temporary") == true)
            {
                this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSoldierName, "seconds", BanTimeReal.ToString(), (strReason.Replace("%bt%", BanTime.ToString())).Replace("%pn%", strSoldierName));
                this.ExecuteCommand("procon.protected.tasks.add", "CChatFilterRefresh", "10", "1", "1", "procon.protected.send", "banList.list");
            }

            else if (strTempOrPerm.Equals("Permanent") == true)
            {
                this.ExecuteCommand("procon.protected.send", "banList.add", "name", strSoldierName, "perm", strReason.Replace("%pn%", strSoldierName));
                this.ExecuteCommand("procon.protected.tasks.add", "CChatFilterRefresh", "10", "1", "1", "procon.protected.send", "banList.list");
            }
        }
        #endregion

        #region TBanUnitHandler
        public void TBanUnitHandler()
        {
            if (strTBanUnits.Equals("Minutes") == true)
            {
                this.minsBanTime = iBanTime * 60;
            }
            else if (strTBanUnits.Equals("Hours") == true)
            {
                this.minsBanTime = iBanTime * 3600;
            }
            else if (strTBanUnits.Equals("Days") == true)
            {
                this.minsBanTime = iBanTime * 86400;
            }
            else if (strTBanUnits.Equals("Weeks") == true)
            {
                this.minsBanTime = iBanTime * 604800;
            }
            else if (strTBanUnits.Equals("Months") == true)
            {
                this.minsBanTime = iBanTime * 2592000;
            }

            if (strDTBanUnits.Equals("Minutes") == true)
            {
                this.DminsBanTime = iDBanTime * 60;
            }
            else if (strDTBanUnits.Equals("Hours") == true)
            {
                this.DminsBanTime = iDBanTime * 3600;
            }
            else if (strDTBanUnits.Equals("Days") == true)
            {
                this.DminsBanTime = iDBanTime * 86400;
            }
            else if (strDTBanUnits.Equals("Weeks") == true)
            {
                this.DminsBanTime = iDBanTime * 604800;
            }
            else if (strDTBanUnits.Equals("Months") == true)
            {
                this.DminsBanTime = iDBanTime * 2592000;
            }

            if (strHTBanUnits.Equals("Minutes") == true)
            {
                this.HminsBanTime = iHBanTime * 60;
            }
            else if (strHTBanUnits.Equals("Hours") == true)
            {
                this.HminsBanTime = iHBanTime * 3600;
            }
            else if (strHTBanUnits.Equals("Days") == true)
            {
                this.HminsBanTime = iHBanTime * 86400;
            }
            else if (strHTBanUnits.Equals("Weeks") == true)
            {
                this.HminsBanTime = iHBanTime * 604800;
            }
            else if (strHTBanUnits.Equals("Months") == true)
            {
                this.HminsBanTime = iHBanTime * 2592000;
            }

            if (strDRTBanUnits.Equals("Minutes") == true)
            {
                this.DRminsBanTime = iDRBanTime * 60;
            }
            else if (strDRTBanUnits.Equals("Hours") == true)
            {
                this.DRminsBanTime = iDRBanTime * 3600;
            }
            else if (strDRTBanUnits.Equals("Days") == true)
            {
                this.DRminsBanTime = iDRBanTime * 86400;
            }
            else if (strDRTBanUnits.Equals("Weeks") == true)
            {
                this.DRminsBanTime = iDRBanTime * 604800;
            }
            else if (strDRTBanUnits.Equals("Months") == true)
            {
                this.DRminsBanTime = iDRBanTime * 2592000;
            }

            if (strHRTBanUnits.Equals("Minutes") == true)
            {
                this.HRminsBanTime = iHRBanTime * 60;
            }
            else if (strHRTBanUnits.Equals("Hours") == true)
            {
                this.HRminsBanTime = iHRBanTime * 3600;
            }
            else if (strHRTBanUnits.Equals("Days") == true)
            {
                this.HRminsBanTime = iHRBanTime * 86400;
            }
            else if (strHRTBanUnits.Equals("Weeks") == true)
            {
                this.HRminsBanTime = iHRBanTime * 604800;
            }
            else if (strHRTBanUnits.Equals("Months") == true)
            {
                this.HRminsBanTime = iHRBanTime * 2592000;
            }
        }
        #endregion

        public void PluginConsole(string strMessage, string strLevel)
        {

        }

        #region Command Related Stuff
        private void UnregisterAllCommands()
        {
            MatchCommand ConfirmCommand = new MatchCommand(this.Listify<string>("@", "!", "#", "/"), "yes", this.Listify<MatchArgumentFormat>());

            this.UnregisterCommand(new MatchCommand("CChatFilter", "OnCommandDClear", this.Listify<string>("@", "!", "#", "/"), this.m_strDClearCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, ConfirmCommand, "You do not have the required permissions for this command."), "Clears an offender's infractions"));
            this.UnregisterCommand(new MatchCommand("CChatFilter", "OnCommandHClear", this.Listify<string>("@", "!", "#", "/"), this.m_strHClearCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, ConfirmCommand, "You do not have the required permissions for this command."), "Clears an offender's whines"));
            this.UnregisterCommand(new MatchCommand("CChatFilter", "OnCommandDMuteClear", this.Listify<string>("@", "!", "#", "/"), this.m_strDClearMuteCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, ConfirmCommand, "You do not have the required permissions for this command."), "Clears a bad language offender's mute"));
            this.UnregisterCommand(new MatchCommand("CChatFilter", "OnCommandHMuteClear", this.Listify<string>("@", "!", "#", "/"), this.m_strHClearMuteCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, ConfirmCommand, "You do not have the required permissions for this command."), "Clears a bad language offender's mute"));
        }

        private void RegisterAllCommands()
        {
            MatchCommand ConfirmCommand = new MatchCommand(this.Listify<string>("@", "!", "#", "/"), "yes", this.Listify<MatchArgumentFormat>());

            this.RegisterCommand(new MatchCommand("CChatFilter", "OnCommandDClear", this.Listify<string>("@", "!", "#", "/"), this.m_strDClearCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, ConfirmCommand, "You do not have the required permissions for this command."), "Clears an offender's infractions"));
            this.RegisterCommand(new MatchCommand("CChatFilter", "OnCommandHClear", this.Listify<string>("@", "!", "#", "/"), this.m_strHClearCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, ConfirmCommand, "You do not have the required permissions for this command."), "Clears an offender's whines"));
            this.RegisterCommand(new MatchCommand("CChatFilter", "OnCommandDMuteClear", this.Listify<string>("@", "!", "#", "/"), this.m_strDClearMuteCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, ConfirmCommand, "You do not have the required permissions for this command."), "Clears a bad language offender's mute"));
            this.RegisterCommand(new MatchCommand("CChatFilter", "OnCommandHMuteClear", this.Listify<string>("@", "!", "#", "/"), this.m_strHClearMuteCommand, this.Listify<MatchArgumentFormat>(new MatchArgumentFormat("playername", new List<string>(this.m_dicPlayerInfo.Keys))), new ExecutionRequirements(ExecutionScope.Account, 1, ConfirmCommand, "You do not have the required permissions for this command."), "Clears a bad language offender's mute"));
        }

        public void OnCommandDClear(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.m_dicPbInfo.ContainsKey(capCommand.MatchedArguments[0].Argument) == true)
            {
                if (this.m_dicDOffenders.ContainsKey(this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName) == true)
                {
                    this.CommandResponse(capCommand.ResposeScope, strSpeaker, "Clearing " + this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + " of infractions.");
                    this.ExecuteCommand("procon.protected.pluginconsole.write", strSpeaker + " cleared " + this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + "'s infractions.");
                    m_dicDOffenders.Remove(this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                }
                else
                {
                    this.CommandResponse(capCommand.ResposeScope, strSpeaker, this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + " has no infractions.");
                }
            }
            else
            {
                this.CommandResponse(capCommand.ResposeScope, strSpeaker, "Command failed. No PB info");
            }
        }

        public void OnCommandHClear(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (this.m_dicPbInfo.ContainsKey(capCommand.MatchedArguments[0].Argument) == true)
            {
                if (this.m_dicHOffenders.ContainsKey(this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName) == true)
                {
                    this.CommandResponse(capCommand.ResposeScope, strSpeaker, "Clearing " + this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + " of whines.");
                    this.ExecuteCommand("procon.protected.pluginconsole.write", strSpeaker + " cleared " + this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + "'s whines.");
                    m_dicHOffenders.Remove(this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName);
                }
                else
                {
                    this.CommandResponse(capCommand.ResposeScope, strSpeaker, this.m_dicPbInfo[capCommand.MatchedArguments[0].Argument].SoldierName + " has no whines.");
                }
            }
            else
            {
                this.CommandResponse(capCommand.ResposeScope, strSpeaker, "Command failed. No PB info");
            }
        }
        public void OnCommandDMuteClear(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
        }

        public void OnCommandHMuteClear(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
        }

        private void CommandResponse(string strScope, string strSpeaker, string strMessage)
        {
            if (strScope.Equals("!") == true)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", strMessage, "all");
            }
            else if (strScope.Equals("@") == true || strScope.Equals("/") == true || strScope.Equals("#") == true)
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", strMessage, "player", strSpeaker);
            }
        }
        #endregion

        #region Procon Events
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
            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName) == false)
            {
                this.m_dicPlayerInfo.Add(strSoldierName, new CPlayerInfo(strSoldierName, "", 0, 24));
            }
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {
            RunListRemover(strSoldierName);

            if (this.m_dicPlayerInfo.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPlayerInfo.Remove(strSoldierName);
            }

            if (this.m_dicPbInfo.ContainsKey(strSoldierName) == true)
            {
                this.m_dicPbInfo.Remove(strSoldierName);

            }
            RegisterAllCommands();
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

        }

        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {

            if ((strSpeaker).Equals("Server") == false) //(wMatch.Success != true)
            {
                this.RunProfanityFilter(strSpeaker, strMessage);
            }
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {

            if ((strSpeaker).Equals("Server") == false) //(wMatch.Success != true)
            {
                this.RunProfanityFilter(strSpeaker, strMessage);
            }
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {

            if ((strSpeaker).Equals("Server") == false) //(wMatch.Success != true)
            {
                this.RunProfanityFilter(strSpeaker, strMessage);
            }
        }

        public void OnLoadingLevel(string strMapFileName)
        {
            ImmunePeople.Clear();

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
        #endregion

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

        #region IPRoConPluginInterface4

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage, object trespassState) { }

        public void RegisterZoneTags(params string[] tags) { }

        public void UnregisterZoneTags(params string[] tags) { }

        #endregion

        #region IPRoConPluginInterface5

        public void OnRoundOverPlayers(List<CPlayerInfo> lstPlayers) { }

        #endregion

        #region IPRoConPluginInterface6

        public HttpWebServerResponseData OnHttpRequest(HttpWebServerRequestData data)
        {

            if (data.Query.Get("echo") != null)
            {
                return new HttpWebServerResponseData(data.Query.Get("echo"));
            }
            else
            {
                return new HttpWebServerResponseData("Hello World!");
            }
        }

        #endregion


    }
}