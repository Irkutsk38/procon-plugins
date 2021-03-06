using System;
using System.Collections.Generic;
using PRoCon.Core.Plugin;
using PRoCon.Core;
using System.Text.RegularExpressions;
using PRoCon.Core.Players.Items;
using System.Threading;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class WeaponLimiter : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Basic methods

        bool IsEnabled;

        public string GetPluginName()
        {
            return "WeaponLimiter Beta";
        }

        public string GetPluginVersion()
        {
            return "0.4.2";
        }

        public string GetPluginAuthor()
        {
            return "t-master and Myriades";
        }

        public string GetPluginWebsite()
        {
            return "";
        }

        public string GetPluginDescription()
        {
            string[] weapons = new string[weaponDefines.Count];
            for (int i = 0; i < weaponDefines.Count; i++)
            {
                weapons[i] = weaponDefines[i].Name;
            }

            string[] specs = new string[specializationDefines.Count];
            for (int i = 0; i < specializationDefines.Count; i++)
            {
                specs[i] = specializationDefines[i].Name;
            }

            string desc = String.Format(@"Please be aware that you don't break the BFBC2 RoC (http://forums.electronicarts.co.uk/battlefield-announcements/1167691-bfbc2-rules-conduct.html) when you
are using this plugin! Before you are able to use this plugin you have to read them and accept them by setting the" + "\"" + "RoC read and accepted" + "\"" + @" value to Yes.
The settings should be self-explaining now, you add another ruleset by increasing the number in RuleSetCount");

            return desc;
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            weaponDefines = GetWeaponDefines();
            specializationDefines = GetSpecializationDefines();
            commandScopeList.Add(Commands.Count, CommandScopes.Spawn);
            commandScopeList.Add(Commands.Equipped, CommandScopes.Spawn);
            commandScopeList.Add(Commands.Kill_Percentage, CommandScopes.Kill);
            commandScopeList.Add(Commands.OnKill, CommandScopes.Kill);

            defaultCommandSettings.Add(Commands.None, new List<object>());
            defaultCommandSettings.Add(Commands.Equipped, new List<object>());

            List<object> tmp2 = new List<object>();
            tmp2.Add(0);
            defaultCommandSettings.Add(Commands.Kill_Percentage, tmp2);
            defaultCommandSettings.Add(Commands.OnKill, tmp2);
            defaultCommandSettings.Add(Commands.Count, tmp2);

            ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public void OnPluginEnable()
        {
            IsEnabled = true;
            //ParseRules();
        }

        public void OnPluginDisable()
        {
            IsEnabled = false;
        }

        #endregion

        #region Vars

        WeaponDictionary weaponDefines;
        SpecializationDictionary specializationDefines;

        Dictionary<object, Dictionary<Commands, object>> watchedObjects = new Dictionary<object, Dictionary<Commands, object>>();

        Dictionary<string, string> clantagList = new Dictionary<string, string>();

        string whiteListClanTag = "";

        PlayerList players = new PlayerList();

        int count;
        int timeTillKill = 5000;
        enumBoolYesNo rocRead = enumBoolYesNo.No;

        Notifications notification = Notifications.Both;

        SlotReservationMode slotReservationMode = SlotReservationMode.FirstComeFirstServed;

        List<RuleSet> RuleSets = new List<RuleSet>();
        Dictionary<Commands, CommandScopes> commandScopeList = new Dictionary<Commands, CommandScopes>(Enum.GetNames(typeof(Commands)).Length);
        Dictionary<Commands, List<object>> defaultCommandSettings = new Dictionary<Commands, List<object>>();

        List<string> reservedSlots = new List<string>();

        WhiteListSettings whiteListSettings = WhiteListSettings.None;

        enum Notifications
        {
            Yell,
            Say,
            Both
        }

        [Flags]
        enum CommandScopes
        {
            Kill = 0x1,
            Spawn = 0x2
        }

        enum Punishments
        {
            DebugMode,
            Warn,
            Kill,
            Kick
        }

        enum Commands
        {
            None,
            Count,
            Equipped,
            Kill_Percentage,
            OnKill
        }

        enum WhiteListSettings
        {
            None,
            UseReservedSlotList,
            UseClanTag
        }


        enum LimitType
        {
            None,
            LowerLimit,
            UpperLimit
        }

        enum RuleObjectiveType
        {
            None,
            Weapon,
            Specialization,
            Kit,
            WeaponType
        }

        enum SlotReservationMode
        {
            FirstComeFirstServed,
            ReserveSlotsTillRespawn
        }

        class Rule
        {
            public object DisplayRuleObjective;
            public object RuleObjective;
            public RuleObjectiveType RuleObjectiveType;
            public Commands Command = Commands.None;
            public List<object> CustomSettings = new List<object>();
            public int PlayerLimit = 0;
            public LimitType PlayerLimitType = LimitType.None;
        }

        class RuleSet
        {
            public List<Rule> Rules = new List<Rule>();
            public int Count = 0;
            public string Message;
            public Punishments Punishement = Punishments.DebugMode;
            public bool IsEnabled = false;
        }

        class PlayerList
        {
            public PlayerList()
            {
                TeamA = new List<string>(16);
                TeamB = new List<string>(16);
            }

            public List<string> TeamA;
            public List<string> TeamB;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            try
            {
                strValue = CPluginVariable.Decode(strValue);
                switch (strVariable)
                {
                    default:
                        Match m = new Regex(@"^Message #(?<id>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            RuleSets[Int32.Parse(m.Groups["id"].Value)].Message = strValue;
                            return;
                        }
                        m = new Regex(@"^Punishment #(?<id>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            RuleSets[Int32.Parse(m.Groups["id"].Value)].Punishement = (Punishments)Enum.Parse(typeof(Punishments), strValue);
                            return;
                        }
                        m = new Regex(@"^RuleCount #(?<id>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            RuleSet ruleSet = RuleSets[Int32.Parse(m.Groups["id"].Value)];
                            int rulecount = Int32.Parse(strValue);
                            if (rulecount < ruleSet.Count)
                                ruleSet.Rules.RemoveRange(rulecount, RuleSets.Count - count);
                            else if (rulecount > ruleSet.Count)
                            {
                                int diff = rulecount - ruleSet.Count;
                                for (int i = 0; i < diff; i++)
                                {
                                    ruleSet.Rules.Add(new Rule());
                                }
                            }
                            ruleSet.Count = Int32.Parse(strValue);
                            return;
                        }
                        m = new Regex(@"^IsEnabled #(?<id>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            RuleSets[Int32.Parse(m.Groups["id"].Value)].IsEnabled = Convert.ToBoolean(strValue);
                            return;
                        }
                        m = new Regex(@"^RuleCommand #(?<id>\d+)-(?<id2>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            Rule rule = RuleSets[Int32.Parse(m.Groups["id"].Value)].Rules[Int32.Parse(m.Groups["id2"].Value)];
                            Commands cmd = (Commands)Enum.Parse(typeof(Commands), strValue);
                            if (rule.Command != cmd)
                                rule.CustomSettings.Clear();
                            foreach (object defaultValue in defaultCommandSettings[cmd])
                            {
                                rule.CustomSettings.Add(defaultValue);
                            }
                            rule.Command = cmd;
                            return;
                        }
                        m = new Regex(@"^RuleObjectiveType #(?<id>\d+)-(?<id2>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            Rule rule = RuleSets[Int32.Parse(m.Groups["id"].Value)].Rules[Int32.Parse(m.Groups["id2"].Value)];
                            RuleObjectiveType type = (RuleObjectiveType)Enum.Parse(typeof(RuleObjectiveType), strValue);
                            if (rule.RuleObjectiveType != type)
                            {
                                rule.DisplayRuleObjective = null;
                                rule.RuleObjective = null;
                            }
                            rule.RuleObjectiveType = type;
                            return;
                        }
                        m = new Regex(@"^RuleObjective #(?<id>\d+)-(?<id2>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            Rule rule = RuleSets[Int32.Parse(m.Groups["id"].Value)].Rules[Int32.Parse(m.Groups["id2"].Value)];
                            object oldObjective = rule.RuleObjective;
                            rule.DisplayRuleObjective = strValue;
                            object newObjective = null;
                            switch (rule.RuleObjectiveType)
                            {
                                case RuleObjectiveType.Weapon:
                                    newObjective = GetWeaponByLocalizedName(strValue);
                                    break;
                                case RuleObjectiveType.Kit:
                                    newObjective = (Kits)Enum.Parse(typeof(Kits), strValue);
                                    break;
                                case RuleObjectiveType.WeaponType:
                                    newObjective = (DamageTypes)Enum.Parse(typeof(DamageTypes), strValue);
                                    break;
                                case RuleObjectiveType.Specialization:
                                    newObjective = this.GetSpecializationByLocalizedName(strValue);
                                    break;
                            }
                            rule.RuleObjective = newObjective;
                            if (oldObjective != newObjective)
                                MaintainRuleObjectiveList(oldObjective, newObjective, rule);
                            return;
                        }
                        m = new Regex(@"^PlayerLimitType #(?<id>\d+)-(?<id2>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            RuleSets[Int32.Parse(m.Groups["id"].Value)].Rules[Int32.Parse(m.Groups["id2"].Value)].PlayerLimitType = (LimitType)Enum.Parse(typeof(LimitType), strValue);
                            return;
                        }
                        m = new Regex(@"^PlayerLimit #(?<id>\d+)-(?<id2>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            RuleSets[Int32.Parse(m.Groups["id"].Value)].Rules[Int32.Parse(m.Groups["id2"].Value)].PlayerLimit = Int32.Parse(strValue);
                            return;
                        }

                        //Count Command
                        m = new Regex(@"^Count Limit #(?<id>\d+)-(?<id2>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            RuleSets[Int32.Parse(m.Groups["id"].Value)].Rules[Int32.Parse(m.Groups["id2"].Value)].CustomSettings[0] = Convert.ToInt32(strValue);
                            return;
                        }

                        //KillPercentage
                        m = new Regex(@"^KillPercentage Percentage #(?<id>\d+)-(?<id2>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            RuleSets[Int32.Parse(m.Groups["id"].Value)].Rules[Int32.Parse(m.Groups["id2"].Value)].CustomSettings[0] = Int32.Parse(strValue);
                            return;
                        }

                        //Kill
                        m = new Regex(@"^Kill Count #(?<id>\d+)-(?<id2>\d+)").Match(strVariable);
                        if (m.Success)
                        {
                            RuleSets[Int32.Parse(m.Groups["id"].Value)].Rules[Int32.Parse(m.Groups["id2"].Value)].CustomSettings[0] = Int32.Parse(strValue);
                            return;
                        }

                        break;
                    case "RuleSetCount":
                        count = Int32.Parse(strValue);
                        if (count < RuleSets.Count)
                            RuleSets.RemoveRange(count, RuleSets.Count - count);
                        else if (count > RuleSets.Count)
                        {
                            int diff = count - RuleSets.Count;
                            for (int i = 0; i < diff; i++)
                            {
                                RuleSets.Add(new RuleSet());
                            }
                        }
                        //ParseRules();
                        break;
                    case "WhiteListSettings":
                        whiteListSettings = (WhiteListSettings)Enum.Parse(typeof(WhiteListSettings), strValue);
                        break;
                    case "WhiteListClanTag":
                        whiteListClanTag = strValue;
                        break;
                    case "TimeTillKill":
                        timeTillKill = Int32.Parse(strValue);
                        break;
                    case "Notification":
                        notification = (Notifications)Enum.Parse(typeof(Notifications), strValue);
                        break;
                    case "SlotReservationMode":
                        slotReservationMode = (SlotReservationMode)Enum.Parse(typeof(SlotReservationMode), strValue);
                        break;
                    case "RoC read and accepted":
                        rocRead = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                        break;
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private void MaintainRuleObjectiveList(object oldObjective, object newObjective, Rule rule)
        {
            List<Rule> sameObjectiveRules = new List<Rule>();
            List<Rule> sameObjectiveANDCommandRules = new List<Rule>();
            if (oldObjective != null)
            {
                if (watchedObjects.ContainsKey(oldObjective))
                {
                    foreach (RuleSet ruleSet in this.RuleSets)
                    {
                        foreach (Rule ruleSetRule in ruleSet.Rules)
                        {
                            if (ruleSetRule.RuleObjective == oldObjective)
                            {
                                sameObjectiveRules.Add(ruleSetRule);
                                if (ruleSetRule.Command == rule.Command)
                                    sameObjectiveANDCommandRules.Add(rule);
                            }
                        }
                    }
                    if (sameObjectiveRules.Count == 0)
                        watchedObjects.Remove(oldObjective);
                    else if (sameObjectiveANDCommandRules.Count == 0)
                        watchedObjects[oldObjective].Remove(rule.Command);
                }
            }
            if (newObjective != null)
            {
                if (!watchedObjects.ContainsKey(newObjective))
                    watchedObjects.Add(newObjective, new Dictionary<Commands, object>());
            }
        }



        public List<PRoCon.Core.CPluginVariable> GetDisplayPluginVariables()
        {
            return GetInternalCombinedVariables(true);
        }
        public List<PRoCon.Core.CPluginVariable> GetInternalCombinedVariables(bool displayVariables)
        {
            try
            {
                List<CPluginVariable> lst = new List<CPluginVariable>();
                lst.Add(new CPluginVariable(GetPluginVariableName("General|", "RuleSetCount", displayVariables), typeof(int), count));
                lst.Add(new CPluginVariable(GetPluginVariableName("General|", "TimeTillKill", displayVariables), typeof(int), timeTillKill));
                lst.Add(new CPluginVariable(GetPluginVariableName("General|", "Notification", displayVariables), CreateEnumString(typeof(Notifications), false), Convert.ToString(notification)));
                lst.Add(new CPluginVariable(GetPluginVariableName("General|", "SlotReservationMode", displayVariables), CreateEnumString(typeof(SlotReservationMode), false), Convert.ToString(slotReservationMode)));
                lst.Add(new CPluginVariable(GetPluginVariableName("General|", "RoC read and accepted", displayVariables), typeof(enumBoolYesNo), rocRead));
                lst.Add(new CPluginVariable(GetPluginVariableName("General|", "WhiteListSettings", displayVariables), CreateEnumString(typeof(WhiteListSettings), false), Convert.ToString(whiteListSettings)));
                if (whiteListSettings == WhiteListSettings.UseClanTag)
                    lst.Add(new CPluginVariable(GetPluginVariableName("General|", "Whitelisted Clantag", displayVariables), typeof(string), whiteListClanTag));
                for (int i = 0; i < count; i++)
                {
                    RuleSet ruleSet = RuleSets[i];
                    lst.Add(new CPluginVariable(string.Format(GetPluginVariableName("RuleSet #{0}|", "IsEnabled #{0}", displayVariables), i), typeof(bool), ruleSet.IsEnabled));
                    lst.Add(new CPluginVariable(string.Format(GetPluginVariableName("RuleSet #{0}|", "Message #{0}", displayVariables), i), typeof(string), ruleSet.Message));
                    lst.Add(new CPluginVariable(string.Format(GetPluginVariableName("RuleSet #{0}|", "Punishment #{0}", displayVariables), i), CreateEnumString(typeof(Punishments), false), Convert.ToString(ruleSet.Punishement)));
                    lst.Add(new CPluginVariable(string.Format(GetPluginVariableName("RuleSet #{0}|", "RuleCount #{0}", displayVariables), i), typeof(int), Convert.ToString(ruleSet.Count)));
                    for (int j = 0; j < ruleSet.Count; j++)
                    {
                        if (ruleSet.Rules.Count == j)
                            ruleSet.Rules.Add(new Rule());
                        Rule rule = ruleSet.Rules[j];
                        string rulePrefix = "RuleSet #{0} - Rule #{1}|";
                        lst.Add(new CPluginVariable(string.Format(GetPluginVariableName(rulePrefix, "RuleCommand #{0}-{1}", displayVariables), i, j), CreateEnumString(typeof(Commands), false), Convert.ToString(rule.Command)));
                        lst.Add(new CPluginVariable(string.Format(GetPluginVariableName(rulePrefix, "RuleObjectiveType #{0}-{1}", displayVariables), i, j), CreateEnumString(typeof(RuleObjectiveType), false), Convert.ToString(rule.RuleObjectiveType)));
                        if (rule.RuleObjectiveType != RuleObjectiveType.None)
                        {
                            string varname = string.Format(GetPluginVariableName(rulePrefix, "RuleObjective #{0}-{1}", displayVariables), i, j);
                            switch (rule.RuleObjectiveType)
                            {
                                case RuleObjectiveType.Kit:
                                    {
                                        lst.Add(new CPluginVariable(varname, CreateEnumString(typeof(Kits), true), this.CheckForDefaultValue(rule.DisplayRuleObjective, "None").ToString()));
                                    }
                                    break;
                                case RuleObjectiveType.Specialization:
                                    {
                                        CPluginVariable variable = this.GetSpecializationListPluginVariable(varname, "WeaponLimiter_Specializations", "", SpecializationSlots.None);
                                        lst.Add(new CPluginVariable(variable.Name, AddNoneOptionToEnumString(variable.Type), CheckForDefaultValue(rule.DisplayRuleObjective, "None").ToString()));
                                    }
                                    break;
                                case RuleObjectiveType.Weapon:
                                    {
                                        CPluginVariable variable = this.GetWeaponListPluginVariable(varname, "WeaponLimiter_Weapons", Convert.ToString(rule.DisplayRuleObjective), DamageTypes.None);
                                        lst.Add(new CPluginVariable(variable.Name, AddNoneOptionToEnumString(variable.Type), CheckForDefaultValue(rule.DisplayRuleObjective, "None").ToString()));
                                    }
                                    break;
                                case RuleObjectiveType.WeaponType:
                                    {
                                        lst.Add(new CPluginVariable(varname, CreateEnumString(typeof(DamageTypes)), CheckForDefaultValue(rule.DisplayRuleObjective, "None").ToString()));
                                    }
                                    break;
                                default:
                                    throw new NotImplementedException(Convert.ToString(rule.RuleObjectiveType) + " is not implemented properly!");
                            }
                        }
                        if (rule.Command != Commands.None)
                        {
                            switch (rule.Command)
                            {
                                case Commands.Count:
                                    {
                                        lst.Add(new CPluginVariable(string.Format(GetPluginVariableName(rulePrefix, "Count Limit #{0}-{1}", displayVariables), i, j), typeof(int), Convert.ToString(rule.CustomSettings[0])));
                                    }
                                    break;
                                case Commands.Kill_Percentage:
                                    {
                                        lst.Add(new CPluginVariable(string.Format(GetPluginVariableName(rulePrefix, "KillPercentage Percentage #{0}-{1}", displayVariables), i, j), typeof(double), Convert.ToDouble(rule.CustomSettings[0])));
                                    }
                                    break;
                                case Commands.OnKill:
                                    lst.Add(new CPluginVariable(string.Format(GetPluginVariableName(rulePrefix, "Kill Count #{0}-{1}", displayVariables), i, j), typeof(int), Convert.ToInt32(rule.CustomSettings[0])));
                                    break;
                            }
                        }
                        lst.Add(new CPluginVariable(string.Format(GetPluginVariableName(rulePrefix, "PlayerLimitType #{0}-{1}", displayVariables), i, j), CreateEnumString(typeof(LimitType), false), rule.PlayerLimitType.ToString()));
                        if (rule.PlayerLimitType != LimitType.None)
                        {
                            lst.Add(new CPluginVariable(string.Format(GetPluginVariableName(rulePrefix, "PlayerLimit #{0}-{1}", displayVariables), i, j), typeof(int), rule.PlayerLimit));
                        }
                    }
                }
                return lst;
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return new List<CPluginVariable>();
            }
        }


        public List<PRoCon.Core.CPluginVariable> GetPluginVariables()
        {
            return GetInternalCombinedVariables(false);
        }

        #endregion

        #region Logic

        //TODO: test with punishment = kick if player gets removed from all lists (since he disconnects) before all rulechecks ended

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {
            //Assigning joined and switched players to a team
            if (iTeamID == 1)
            {
                players.TeamB.Remove(strSoldierName);
                players.TeamA.Add(strSoldierName);
            }
            else if (iTeamID == 2)
            {
                players.TeamA.Remove(strSoldierName);
                players.TeamB.Add(strSoldierName);
            }
            foreach (Dictionary<Commands, object> value in this.watchedObjects.Values)
            {
                foreach (KeyValuePair<Commands, object> keyval in value)
                {
                    switch (keyval.Key)
                    {
                        case Commands.Count:
                            {
                                PlayerList lst = (PlayerList)keyval.Value;
                                if (iTeamID == 2)
                                    lst.TeamA.Remove(strSoldierName);
                                else if (iTeamID == 1)
                                    lst.TeamB.Remove(strSoldierName);
                            }
                            break;
                        case Commands.Kill_Percentage:
                            {
                                Dictionary<string, int[]> dict = (Dictionary<string, int[]>)keyval.Value;
                                dict.Remove(strSoldierName);
                            }
                            break;
                        case Commands.Equipped:
                            continue;
                        case Commands.OnKill:
                            {
                                Dictionary<string, int> dict = (Dictionary<string, int>)keyval.Value;
                                dict.Remove(strSoldierName);
                            }
                            break;
                        default:
                            throw new NotImplementedException(string.Format("It seems, command {0} has not been implemented properly", keyval.Key.ToString()));
                    }
                }
            }
        }

        public void OnPlayerLeft(string strSoldierName)
        {
            //Removes left player from all lists
            players.TeamA.Remove(strSoldierName);
            players.TeamB.Remove(strSoldierName);
            clantagList.Remove(strSoldierName);
            foreach (Dictionary<Commands, object> value in this.watchedObjects.Values)
            {
                foreach (KeyValuePair<Commands, object> keyval in value)
                {
                    switch (keyval.Key)
                    {
                        case Commands.Count:
                            {
                                PlayerList lst = (PlayerList)keyval.Value;
                                lst.TeamA.Remove(strSoldierName);
                                lst.TeamB.Remove(strSoldierName);
                            }
                            break;
                        case Commands.Kill_Percentage:
                            {
                                Dictionary<string, int[]> dict = (Dictionary<string, int[]>)keyval.Value;
                                dict.Remove(strSoldierName);
                            }
                            break;
                        case Commands.OnKill:
                            {
                                Dictionary<string, int> dict = (Dictionary<string, int>)keyval.Value;
                                dict.Remove(strSoldierName);
                            }
                            break;
                        case Commands.Equipped:
                            continue;
                        default:
                            throw new NotImplementedException(string.Format("It seems, command {0} has not been implemented properly", keyval.Key.ToString()));
                    }
                }
            }
        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            //Sort players into teams
            players.TeamA.Clear();
            players.TeamB.Clear();
            foreach (CPlayerInfo info in lstPlayers)
            {
                if (info.TeamID == 1)
                    players.TeamA.Add(info.SoldierName);
                else if (info.TeamID == 2)
                    players.TeamB.Add(info.SoldierName);
                if (!clantagList.ContainsKey(info.SoldierName))
                    clantagList.Add(info.SoldierName, info.ClanTag);
            }
        }

        public void OnReservedSlotsList(List<string> lstSoldierNames)
        {
            this.reservedSlots = lstSoldierNames;
        }

        public void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            //Check if the player carries any of the things we're looking for
            try
            {
                foreach (object obj in watchedObjects.Keys)
                {
                    if (obj is Weapon)
                    {
                        Weapon tmp = obj as Weapon;
                        foreach (Weapon weapon in spawnedInventory.Weapons)
                        {
                            if (weapon.Name == tmp.Name)
                            {
                                CheckRulesForPlayer(soldierName, spawnedInventory, CommandScopes.Spawn);
                                return;
                            }
                        }
                    }
                    else if (obj is Kits)
                    {
                        if (spawnedInventory.Kit == (Kits)obj)
                        {
                            CheckRulesForPlayer(soldierName, spawnedInventory, CommandScopes.Spawn);
                            return;
                        }
                    }
                    else if (obj is DamageTypes)
                    {
                        DamageTypes wtype = (DamageTypes)obj;
                        foreach (Weapon weapon in spawnedInventory.Weapons)
                        {
                            if (weapon.Damage == wtype)
                            {
                                CheckRulesForPlayer(soldierName, spawnedInventory, CommandScopes.Spawn);
                                return;
                            }
                        }
                    }
                    else if (obj is Specialization)
                    {
                        Specialization spec = obj as Specialization;
                        foreach (Specialization spec2 in spawnedInventory.Specializations)
                        {
                            if (spec2.Name == spec.Name)
                            {
                                CheckRulesForPlayer(soldierName, spawnedInventory, CommandScopes.Spawn);
                                return;
                            }
                        }
                    }
                    else
                        throw new NotSupportedException(string.Format("Object {0} is not supported!", obj.ToString()));
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            string strSoldierName = kKillerVictimDetails.Victim.SoldierName;
            foreach (Dictionary<Commands, object> value in this.watchedObjects.Values)
            {
                foreach (KeyValuePair<Commands, object> keyval in value)
                {
                    switch (keyval.Key)
                    {
                        case Commands.Count:
                            if (slotReservationMode == SlotReservationMode.FirstComeFirstServed)
                            {
                                PlayerList lst = (PlayerList)keyval.Value;
                                lst.TeamA.Remove(strSoldierName);
                                lst.TeamB.Remove(strSoldierName);
                            }
                            break;
                        case Commands.Equipped:
                            continue;
                        case Commands.Kill_Percentage:

                            break;
                        case Commands.OnKill:

                            break;
                        default:
                            throw new NotImplementedException(string.Format("It seems, command {0} has not been implemented properly", keyval.Key.ToString()));
                    }
                }
            }
            /*foreach(object obj in watchedObjects.Keys)
            {
                if (obj is Weapon)
                {
                    if ((obj as Weapon).Name == kKillerVictimDetails.DamageType)*/
            CheckRulesForPlayer(kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails, CommandScopes.Kill);
            /*  }
              else if (obj is Specialization)
              {
                  if ((obj as Specialization).Name == kKillerVictimDetails.DamageType)
                      CheckRulesForPlayer(kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails, CommandScopes.Kill);
              }
              else if (obj is DamageTypes)
              {
                  if (this.GetWeaponList((DamageTypes)obj).Contains(kKillerVictimDetails.DamageType))
                      CheckRulesForPlayer(kKillerVictimDetails.Killer.SoldierName, kKillerVictimDetails, CommandScopes.Kill);
              }
          }*/

        }

        private void CheckRulesForPlayer(string soldierName, object parameter, CommandScopes commandScope)
        {
            try
            {
                if (whiteListSettings == WhiteListSettings.UseReservedSlotList && reservedSlots.Contains(soldierName))
                {
                    return;
                }
                if (whiteListSettings == WhiteListSettings.UseClanTag && clantagList[soldierName] == whiteListClanTag)
                    return;
                int teamID = players.TeamA.Contains(soldierName) ? 1 : 2;
                List<RuleSet> trueRuleSets = new List<RuleSet>();
                foreach (RuleSet rules in RuleSets)
                {
                    if (!rules.IsEnabled)
                        continue;
                    if (CheckRuleSet(soldierName, parameter, teamID, rules, commandScope))
                    {
                        trueRuleSets.Add(rules);
                    }
                }
                Punishments highestPunishment = Punishments.DebugMode;
                RuleSet highestRuleSet = null;
                foreach (RuleSet rules in trueRuleSets)
                {
                    int priority = (int)rules.Punishement;
                    if (priority > (int)highestPunishment)
                    {
                        highestPunishment = rules.Punishement;
                        highestRuleSet = rules;
                    }
                    if (rules.Punishement == Punishments.DebugMode)
                        ThreadPool.QueueUserWorkItem(PunishPlayer, new object[] { rules.Message, soldierName, RuleSets.IndexOf(rules) });
                }
                if (highestRuleSet != null && highestRuleSet.Punishement != Punishments.DebugMode)
                    ThreadPool.QueueUserWorkItem(PunishPlayer, new object[] { highestRuleSet.Message, soldierName, RuleSets.IndexOf(highestRuleSet) });
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private bool CheckRuleSet(string soldierName, object parameter, int teamID, RuleSet rules, CommandScopes commandScope)
        {
            List<bool> rulesFulFilled = new List<bool>();
            foreach (Rule rule in rules.Rules)
            {
                if (rule.Command == Commands.None)
                    continue;
                if ((this.commandScopeList[rule.Command] & commandScope) != commandScope)
                {
                    rulesFulFilled.Add(false);
                    continue;
                }
                bool playerLimitFulfilled = CheckPlayerLimit(rule.PlayerLimitType, rule.PlayerLimit, teamID);
                if (!playerLimitFulfilled)
                {
                    rulesFulFilled.Add(false);
                    continue;
                }
                rulesFulFilled.Add(CheckRule(soldierName, parameter, teamID, rule));
            }
            if (rules.Rules.Count > 0 && rulesFulFilled.Count > 0)
                return rulesFulFilled.TrueForAll(IsValueTrue);
            else
                return false;
        }

        private bool CheckPlayerLimit(LimitType type, int playerLimit, int teamID)
        {
            if (type == LimitType.LowerLimit)
            {
                if (teamID == 1)
                {
                    return players.TeamA.Count >= playerLimit;
                }
                else if (teamID == 2)
                {
                    return players.TeamA.Count >= playerLimit;
                }
            }
            else if (type == LimitType.UpperLimit)
            {
                if (teamID == 1)
                {
                    return players.TeamA.Count <= playerLimit;
                }
                else if (teamID == 2)
                {
                    return players.TeamB.Count <= playerLimit;
                }
            }
            else if (type == LimitType.None)
                return true;
            throw new InvalidOperationException();
        }

        private bool CheckRule(string soldierName, object parameter, int teamID, Rule rule)
        {
            if (rule.Command == Commands.Count)
            {
                Inventory spawnedInventory = (Inventory)parameter;
                int count = 0;
                PlayerList playerLst;
                if (watchedObjects[rule.RuleObjective].ContainsKey(Commands.Count))
                    playerLst = (PlayerList)watchedObjects[rule.RuleObjective][Commands.Count];
                else
                {
                    playerLst = new PlayerList();
                    watchedObjects[rule.RuleObjective].Add(Commands.Count, playerLst);
                }
                if (!IsEquipped(spawnedInventory, rule.RuleObjectiveType, rule.RuleObjective))
                {
                    playerLst.TeamA.Remove(soldierName);
                    playerLst.TeamB.Remove(soldierName);
                    return false;
                }
                if (teamID == 1)
                {
                    if (playerLst.TeamA.Contains(soldierName))
                        return false;
                    count = playerLst.TeamA.Count + 1;
                }
                else if (teamID == 2)
                {
                    if (playerLst.TeamB.Contains(soldierName))
                        return false;
                    count = playerLst.TeamB.Count + 1;
                }
                if (count <= (int)rule.CustomSettings[0])
                {
                    if (teamID == 1)
                        playerLst.TeamA.Add(soldierName);
                    else if (teamID == 2)
                        playerLst.TeamB.Add(soldierName);
                    return false;
                }
                else return true;
            }
            else if (rule.Command == Commands.Equipped)
            {
                return IsEquipped((Inventory)parameter, rule.RuleObjectiveType, rule.RuleObjective);
            }
            else if (rule.Command == Commands.Kill_Percentage)
            {
                if (!watchedObjects[rule.RuleObjective].ContainsKey(Commands.Kill_Percentage))
                    watchedObjects[rule.RuleObjective].Add(Commands.Kill_Percentage, new Dictionary<string, int[]>());
                Dictionary<string, int[]> playerDict = watchedObjects[rule.RuleObjective][Commands.Kill_Percentage] as Dictionary<string, int[]>;
                if (!playerDict.ContainsKey(soldierName))
                    playerDict.Add(soldierName, new int[2]);
                Kill kill = parameter as Kill;
                if (kill.IsSuicide)
                    return false;
                playerDict[soldierName][0] += 1;
                string ruleObjectiveString = String.Empty;
                bool killedWithRuleObjective = false;
                switch (rule.RuleObjectiveType)
                {
                    case RuleObjectiveType.None:
                        return false;
                    case RuleObjectiveType.Kit:
                        return false;
                    case RuleObjectiveType.Specialization:
                        if (kill.DamageType == (rule.RuleObjective as Specialization).Name)
                        {
                            playerDict[soldierName][1] += 1;
                            killedWithRuleObjective = true;
                        }
                        break;
                    case RuleObjectiveType.Weapon:
                        if (kill.DamageType == (rule.RuleObjective as Weapon).Name)
                        {
                            playerDict[soldierName][1] += 1;
                            killedWithRuleObjective = true;
                        }
                        break;
                    case RuleObjectiveType.WeaponType:
                        Weapon wep = this.weaponDefines[kill.DamageType];
                        if (wep.Damage == (DamageTypes)rule.RuleObjective)
                        {
                            playerDict[soldierName][1] += 1;
                            killedWithRuleObjective = true;
                        }
                        break;
                }
                if (playerDict[soldierName][0] >= 7)
                {
                    double percentage = ((double)playerDict[soldierName][1]) / ((double)playerDict[soldierName][0]) * 100;
                    if (percentage >= Convert.ToInt64(rule.CustomSettings[0]) && killedWithRuleObjective)
                        return true;
                    else
                        return false;
                }
                return false;
            }
            else if (rule.Command == Commands.OnKill)
            {
                Kill kill = (Kill)parameter;
                Dictionary<string, int> playerDict = watchedObjects[rule.RuleObjective][Commands.OnKill] as Dictionary<string, int>;
                if (kill.IsSuicide)
                    return false;
                if (!playerDict.ContainsKey(soldierName))
                    playerDict.Add(soldierName, 0);
                bool killedWithRuleObjective = false;
                switch (rule.RuleObjectiveType)
                {
                    case RuleObjectiveType.None:
                        return false;
                    case RuleObjectiveType.Kit:
                        return false;
                    case RuleObjectiveType.Specialization:
                        if (kill.DamageType == (rule.RuleObjective as Specialization).Name)
                        {
                            playerDict[soldierName] += 1;
                            killedWithRuleObjective = true;
                        }
                        break;
                    case RuleObjectiveType.Weapon:
                        if (kill.DamageType == (rule.RuleObjective as Weapon).Name)
                        {
                            playerDict[soldierName] += 1;
                            killedWithRuleObjective = true;
                        }
                        break;
                    case RuleObjectiveType.WeaponType:
                        Weapon wep = this.weaponDefines[kill.DamageType];
                        if (wep.Damage == (DamageTypes)rule.RuleObjective)
                        {
                            playerDict[soldierName] += 1;
                            killedWithRuleObjective = true;
                        }
                        break;
                }
                if (playerDict[soldierName] >= Convert.ToInt32(rule.CustomSettings[0]) && killedWithRuleObjective)
                    return true;
                else return false;
            }
            throw new NotImplementedException();
        }

        private static bool IsEquipped(Inventory spawnedInventory, RuleObjectiveType ruleObjectType, object ruleObject)
        {
            if (ruleObjectType == RuleObjectiveType.Kit)
            {
                return spawnedInventory.Kit == (Kits)ruleObject;
            }
            else if (ruleObjectType == RuleObjectiveType.Specialization)
            {
                Specialization spec = (Specialization)ruleObject;
                foreach (Specialization spec2 in spawnedInventory.Specializations)
                {
                    if (spec.Name == spec2.Name)
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (ruleObjectType == RuleObjectiveType.Weapon)
            {
                Weapon wep = (Weapon)ruleObject;
                foreach (Weapon wep2 in spawnedInventory.Weapons)
                {
                    if (wep.Name == wep2.Name)
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (ruleObjectType == RuleObjectiveType.WeaponType)
            {
                DamageTypes dType = (DamageTypes)ruleObject;
                foreach (Weapon wep2 in spawnedInventory.Weapons)
                {
                    if (wep2.Damage == dType)
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        void PunishPlayer(object state)
        {
            object[] parameters = (object[])state;
            PunishPlayerThread(parameters[0].ToString(), parameters[1].ToString(), (int)parameters[2]);
        }

        void PunishPlayerThread(string Message, string soldierName, int ruleSetIndex)
        {
            Punishments punishment = RuleSets[ruleSetIndex].Punishement;
            if (punishment == Punishments.Kill || punishment == Punishments.Warn)
            {
                if (notification == Notifications.Yell)
                    ExecuteCommand("procon.protected.send", "admin.yell", Message, timeTillKill.ToString(), "player", soldierName);
                else if (notification == Notifications.Say)
                    ExecuteCommand("procon.protected.send", "admin.say", Message, "player", soldierName);
                else
                {
                    ExecuteCommand("procon.protected.send", "admin.yell", Message, timeTillKill.ToString(), "player", soldierName);
                    ExecuteCommand("procon.protected.send", "admin.say", Message, "player", soldierName);
                }
                if (punishment == Punishments.Kill)
                {
                    Thread.Sleep(timeTillKill);
                    ExecuteCommand("procon.protected.send", "admin.killPlayer", soldierName);
                    WriteDebugInfo(string.Format("{0} killed because of ruleset #{1} ({2})", soldierName, ruleSetIndex, Message));
                }
                else
                    WriteDebugInfo(string.Format("{0} warned because of ruleset #{1} ({2})", soldierName, ruleSetIndex, Message));
            }
            else if (punishment == Punishments.Kick)
            {
                ExecuteCommand("procon.protected.send", "admin.kickPlayer", soldierName, Message);
                WriteDebugInfo(string.Format("{0} kicked because of ruleset #{1} ({2})", soldierName, ruleSetIndex, Message));
            }
            else if (punishment == Punishments.DebugMode)
            {
                WriteDebugInfo(string.Format("{0} would have been punished because of ruleset #{1} ({2})", soldierName, ruleSetIndex, Message));
            }
        }

        public void OnRoundOver(int iWinningTeamID)
        {
            foreach (Dictionary<Commands, object> val in watchedObjects.Values)
            {
                foreach (Commands key in val.Keys)
                {
                    switch (key)
                    {
                        case Commands.Count:
                            val[Commands.Count] = new PlayerList();
                            break;
                        case Commands.Equipped:
                            continue;
                        case Commands.None:

                            break;
                        case Commands.Kill_Percentage:
                            val[Commands.Count] = new Dictionary<string, int[]>();
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        #endregion

        #region Utilites

        public string CreateEnumString(string Name, string[] valueList, bool addEmptyEntry)
        {
            if (!addEmptyEntry)
                return string.Format("enum.{0}_{1}({2})", GetType().Name, Name, string.Join("|", valueList));
            else
                return AddNoneOptionToEnumString(string.Format("enum.{0}_{1}({2})", GetType().Name, Name, string.Join("|", valueList)));
        }
        public string CreateEnumString(Type enumeration, bool addEmptyEntry)
        {
            return CreateEnumString(enumeration.Name, Enum.GetNames(enumeration), addEmptyEntry);
        }

        public string CreateEnumString(Type enumeration)
        {
            return CreateEnumString(enumeration.Name, Enum.GetNames(enumeration), false);
        }

        public string AddNoneOptionToEnumString(string input)
        {
            Match m = Regex.Match(input, @"enum.(?<enumname>.*?)\((?<literals>.*)\)");
            if (!m.Success)
                return input;
            else
            {
                return input.Replace(string.Format("enum.{0}(", m.Groups["enumname"].Value), string.Format("enum.{0}(None|", m.Groups["enumname"].Value));
            }
        }

        public void PrintException(Exception ex)
        {
            WriteDebugInfo(ex.ToString());
        }

        private void WriteDebugInfo(string message)
        {
            ExecuteCommand("procon.protected.pluginconsole.write", message);
        }

        bool IsValueTrue(bool b)
        {
            return b;
        }

        private string GetPluginVariableName(string prefix, string name, bool usePrefix)
        {
            if (usePrefix)
                return string.Concat(prefix, name);
            else
                return name;
        }

        public object CheckForDefaultValue(object input, object defaultValue)
        {
            if (input == null)
                return defaultValue;
            else if (input is string && string.IsNullOrEmpty(input.ToString()))
                return defaultValue;
            else
                return input;
        }


        #endregion

        #region Unused Methods

        public void OnAccountCreated(string strUsername)
        {

        }

        public void OnEndRound(int iWinningTeamID)
        {

        }
        public void OnAccountDeleted(string strUsername)
        {

        }

        public void OnAccountPrivilegesUpdate(string strUsername, CPrivileges spPrivs)
        {

        }

        public void OnReceiveProconVariable(string strVariableName, string strValue)
        {

        }

        public void OnConnectionClosed()
        {

        }

        public void OnPlayerJoin(string strSoldierName)
        {

        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

        }

        public void OnGlobalChat(string strSpeaker, string strMessage)
        {

        }

        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {

        }

        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
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

        public void OnResponseError(List<string> lstRequestWords, string strError)
        {

        }

        public void OnHelp(List<string> lstCommands)
        {

        }

        public void OnVersion(string strServerType, string strVersion)
        {

        }

        public void OnLogin()
        {

        }

        public void OnLogout()
        {

        }

        public void OnQuit()
        {

        }

        public void OnRunScript(string strScriptFileName)
        {

        }

        public void OnRunScriptError(string strScriptFileName, int iLineError, string strErrorDescription)
        {

        }

        public void OnServerInfo(CServerInfo csiServerInfo)
        {

        }

        public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset)
        {

        }

        public void OnSaying(string strMessage, CPlayerSubset cpsSubset)
        {

        }

        public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps)
        {

        }

        public void OnPlaylistSet(string strPlaylist)
        {

        }

        public void OnListPlaylists(List<string> lstPlaylists)
        {

        }

        public void OnPlayerKicked(string strSoldierName, string strReason)
        {

        }

        public void OnPlayerSquadChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

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

        public void OnMaplistCleared()
        {

        }

        public void OnMaplistList(List<string> lstMapFileNames)
        {

        }

        public void OnMaplistNextLevelIndex(int iMapIndex)
        {

        }

        public void OnMaplistMapRemoved(int iMapIndex)
        {

        }

        public void OnMaplistMapInserted(int iMapIndex, string strMapFileName)
        {

        }

        public void OnRunNextLevel()
        {

        }

        public void OnCurrentLevel(string strCurrentLevel)
        {

        }

        public void OnRestartLevel()
        {

        }

        public void OnLoadingLevel(string strMapFileName)
        {

        }

        public void OnLevelStarted()
        {

        }

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


        public void OnRoundOverTeamScores(List<TeamScore> lstTeamScores)
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
    }
}
