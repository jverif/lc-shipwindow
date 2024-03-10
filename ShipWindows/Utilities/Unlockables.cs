using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipWindows.Utilities
{
    internal class Unlockables
    {

        public static Dictionary<int, string> windowNames = new()
        {
            [1] = "Right Window",
            [2] = "Left Window",
            [3] = "Floor Window"
        };

        public static Dictionary<int, string> windowInfo = new()
        {
            [1] = "\nAdds a window to the right of the ship's control panel, behind the terminal.\n\n",
            [2] = "\nAdds a window to the left of the ship's control panel.\n\n",
            [3] = "\nAdds a window to the floor of the ship.\n\n"
        };

        class WindowUnlockable
        {
            public string name;
            public int price;
        }

        private static Dictionary<int, WindowUnlockable> unlocks = new();

        [HarmonyPrefix, HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        static void Patch_TextPostProcess(ref string modifiedDisplayText, TerminalNode node)
        {
            try
            {
                if (modifiedDisplayText.Contains("[buyableItemsList]") && modifiedDisplayText.Contains("[unlockablesSelectionList]")) {
                    int index = modifiedDisplayText.IndexOf(@":");

                    foreach (var unlock in unlocks.Reverse())
                    {
                        string upgradeLine = $"\n* {unlock.Value.name}    //    Price: ${unlock.Value.price}";

                        modifiedDisplayText = modifiedDisplayText.Insert(index + 1, upgradeLine);
                    }
                }

            } catch(Exception e)
            {
                ShipWindowPlugin.Log.LogError(e);
            }
            
        }

        private static TerminalKeyword CreateKeyword(string word, TerminalKeyword defaultVerb)
        {
            TerminalKeyword keyword = ScriptableObject.CreateInstance<TerminalKeyword>();
            keyword.name = word;
            keyword.word = word;
            keyword.isVerb = false;
            keyword.accessTerminalObjects = false;
            keyword.defaultVerb = defaultVerb;

            return keyword;
        }

        public static int AddWindowToUnlockables(Terminal terminal, ShipWindowDef def)
        {
            string name;
            if (!windowNames.TryGetValue(def.ID, out name))
                name = $"Window {def.ID}";
            
            ShipWindowPlugin.Log.LogInfo($"Adding {name} to unlockables...");

            int windowUnlockableID = -1;

            UnlockablesList unlockablesList = StartOfRound.Instance.unlockablesList;

            int index = unlockablesList.unlockables.FindIndex(unlockable => unlockable.unlockableName == name);

            if (!unlocks.ContainsKey(def.ID))
            {
                unlocks.Add(def.ID, new WindowUnlockable()
                {
                    name = name,
                    price = def.BaseCost
                });
            }

            if (index == -1)
            {

                var buyKeyword = terminal.terminalNodes.allKeywords.First(keyword => keyword.word == "buy");
                var cancelPurchaseNode = buyKeyword.compatibleNouns[0].result.terminalOptions[1].result;
                var infoKeyword = terminal.terminalNodes.allKeywords.First(keyword => keyword.word == "info");

                var keyword = CreateKeyword($"{name.ToLowerInvariant().Replace(" ", "")}", buyKeyword);

                UnlockableItem sw = new UnlockableItem();
                sw.unlockableName = name;
                sw.spawnPrefab = true;
                sw.alwaysInStock = true;
                sw.prefabObject = def.Prefab;
                sw.unlockableType = 1;
                sw.IsPlaceable = false;
                sw.maxNumber = 1;
                sw.canBeStored = false;
                sw.alreadyUnlocked = false;

                unlockablesList.unlockables.Capacity++;
                unlockablesList.unlockables.Add(sw);
                windowUnlockableID = unlockablesList.unlockables.FindIndex(unlockable => unlockable.unlockableName == name);

                ShipWindowPlugin.Log.LogInfo($"{name} added to unlockable list at index {windowUnlockableID}");

                TerminalNode buyNode2 = ScriptableObject.CreateInstance<TerminalNode>();
                buyNode2.name = $"{name.Replace(" ", "-")}BuyNode2";
                buyNode2.displayText = $"Ordered {name}! Your new balance is [playerCredits].\n\nPlease clean the windows at the end of your contract.\n\n";
                buyNode2.clearPreviousText = true;
                buyNode2.maxCharactersToType = 15;
                buyNode2.buyItemIndex = -1;
                buyNode2.shipUnlockableID = windowUnlockableID;
                buyNode2.buyUnlockable = true;
                buyNode2.creatureName = name;
                buyNode2.isConfirmationNode = false;
                buyNode2.itemCost = def.BaseCost;

                TerminalNode buyNode1 = ScriptableObject.CreateInstance<TerminalNode>();
                buyNode1.name = $"{name.Replace(" ", "-")}BuyNode1";
                buyNode1.displayText = $"You have requested to order {name}.\nTotal cost of item: [totalCost].\n\nPlease CONFIRM or DENY.\n\n";
                buyNode1.clearPreviousText = true;
                buyNode1.maxCharactersToType = 15;
                buyNode1.shipUnlockableID = windowUnlockableID;
                buyNode1.itemCost = def.BaseCost;
                buyNode1.creatureName = name;
                buyNode1.overrideOptions = true;
                buyNode1.terminalOptions =
                [
                    new CompatibleNoun()
                    {
                        noun = terminal.terminalNodes.allKeywords.First(keyword2 => keyword2.word == "confirm"),
                        result = buyNode2
                    },
                    new CompatibleNoun()
                    {
                        noun = terminal.terminalNodes.allKeywords.First(keyword2 => keyword2.word == "deny"),
                        result = cancelPurchaseNode
                    }
                ];

                string infoText;
                if (!windowInfo.TryGetValue(def.ID, out infoText))
                    infoText = $"[No information about this object was found.]\n";

                TerminalNode itemInfo = ScriptableObject.CreateInstance<TerminalNode>();
                itemInfo.name = $"{name.Replace(" ", "-")}InfoNode";
                itemInfo.displayText = infoText;
                itemInfo.clearPreviousText = true;
                itemInfo.maxCharactersToType = 25;

                var allKeywords = terminal.terminalNodes.allKeywords.ToList();
                allKeywords.Add(keyword);
                terminal.terminalNodes.allKeywords = allKeywords.ToArray();

                var nouns = buyKeyword.compatibleNouns.ToList();
                nouns.Add(new CompatibleNoun()
                {
                    noun = keyword,
                    result = buyNode1
                });
                buyKeyword.compatibleNouns = nouns.ToArray();

                var itemInfoNouns = infoKeyword.compatibleNouns.ToList();
                itemInfoNouns.Add(new CompatibleNoun()
                {
                    noun = keyword,
                    result = itemInfo
                });
                infoKeyword.compatibleNouns = itemInfoNouns.ToArray();

                ShipWindowPlugin.Log.LogInfo($"Registered terminal nodes for {name}");

            } else
            {
                windowUnlockableID = index;
            }

            return windowUnlockableID;
        }

        public static int AddSwitchToUnlockables()
        {

            int switchUnlockableID = -1;

            string name = "Shutter Switch";
            UnlockablesList unlockablesList = StartOfRound.Instance.unlockablesList;

            // When running in unity editor this function permanently edits the unlockables list.
            // To keep from duplicating a ton, check if the unlockable is already there and use it's ID instead.

            int index = unlockablesList.unlockables.FindIndex(unlockable => unlockable.unlockableName == name);

            if (index == -1)
            {
                UnlockableItem sw = new UnlockableItem();
                sw.unlockableName = name;
                sw.spawnPrefab = false;
                sw.unlockableType = 1;
                sw.IsPlaceable = true;
                sw.maxNumber = 1;
                sw.canBeStored = false;
                sw.alreadyUnlocked = true;

                unlockablesList.unlockables.Capacity++;
                unlockablesList.unlockables.Add(sw);
                switchUnlockableID = unlockablesList.unlockables.FindIndex(unlockable => unlockable.unlockableName == name);
            } else
            {
                switchUnlockableID = index;
            }

            ShipWindowPlugin.Log.LogInfo($"Added shutter switch to unlockables list at ID {switchUnlockableID}");

            return switchUnlockableID;
        }
    }
}
