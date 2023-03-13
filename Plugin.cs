using BepInEx;
using HarmonyLib;
using Assets.Scripts.Objects.Items;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using System.Reflection.Emit;
using System;
using Assets.Scripts.Inventory;

namespace StationeersTest
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string pluginGuid = "io.inp.stationeers.caninhand";
        private const string pluginName = "Can in hand";
        private const string pluginVersion = "1.0.0";
        private static Plugin instance;
        public static void Log(object line) {
            instance.Logger.LogInfo(line);
        }
        private void Awake()
        {
            instance = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginName} is loaded!");

            var harmony = new Harmony(pluginGuid);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(CannedFood), nameof(CannedFood.DestroyItemAtZero))]
    class CanPatch {
        public static DynamicThing CreateCan(DynamicThing thing, Slot slot) {
            if (slot.IsHandSlot) {
                Slot otherHand = InventoryManager.Instance.InactiveHand.Slot;
                var newthing = Thing.Create<DynamicThing>(thing, slot.Location.position, slot.Location.rotation);
                if (otherHand.Occupant != null) {
                    if (Slot.CanMerge(newthing, otherHand)) {
                        if (newthing is Stackable src) {
                            if (otherHand.Occupant is Stackable dest) {
                                OnServer.Merge(dest, src);
                            }
                        }
                    }
                }
                OnServer.MoveToSlotOrWorld(newthing, otherHand);
                return newthing;
            }
            return OnServer.CreateOld(thing, slot);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(OnServer), nameof(OnServer.CreateOld), new Type[] {typeof(DynamicThing), typeof(Slot)})))
                .SetOperandAndAdvance(AccessTools.Method(typeof(CanPatch), nameof(CanPatch.CreateCan)))
                .InstructionEnumeration();
        }
    }
}
