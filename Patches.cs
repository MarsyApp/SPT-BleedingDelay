using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT.InventoryLogic;

namespace BleedingDelay
{
    class Patcher
    {
        public static void PatchAll()
        {
            new PatchManager().RunPatches();
        }

        public static void UnpatchAll()
        {
            new PatchManager().RunUnpatches();
        }
    }

    public class PatchManager
    {
        public PatchManager()
        {
            this._patches = new List<ModulePatch>
            {
                new ItemViewPatches.ResiduePath(),
                new ItemViewPatches.Method18Path(),
            };
        }

        public void RunPatches()
        {
            foreach (ModulePatch patch in this._patches)
            {
                patch.Enable();
            }
        }

        public void RunUnpatches()
        {
            foreach (ModulePatch patch in this._patches)
            {
                patch.Disable();
            }
        }

        private readonly List<ModulePatch> _patches;
    }

    public static class ItemViewPatches
    {
        public static string GetCallingMethodName(int frame = 1)
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame callingFrame = stackTrace.GetFrame(frame);

            if (callingFrame != null)
            {
                MethodBase callingMethod = callingFrame.GetMethod();
                return callingMethod.Name;
            }

            return string.Empty;
        }

        private static int lightBleedingDamageEffectType = 0;
        private static int heavyBleedingDamageEffectType = 0;
        private static int currentCheckLoop = 0;

        private static float lightBleedingDelay;
        private static float heavyBleedingDelay;

        public class ResiduePath : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                Type medEffectType =
                    typeof(ActiveHealthControllerClass).GetNestedType("MedEffect", BindingFlags.NonPublic);
                MethodInfo methodInfo =
                    medEffectType.GetMethod("Residue", BindingFlags.Instance | BindingFlags.NonPublic);
                return methodInfo;
            }

            [PatchPrefix]
            private static void PatchPrefix(object __instance)
            {
                FieldInfo healthEffectsComponent0 = __instance.GetType().GetField("healthEffectsComponent_0",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                HealthEffectsComponent healthEffectsComponent =
                    (HealthEffectsComponent)healthEffectsComponent0.GetValue(__instance);

                bool hasHeavyBleeding =
                    healthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.HeavyBleeding);
                bool hasLightBleeding =
                    healthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.LightBleeding);

                heavyBleedingDamageEffectType = -1;
                lightBleedingDamageEffectType = -1;
                currentCheckLoop = 0;

                if (hasHeavyBleeding)
                {
                    heavyBleedingDamageEffectType += 1;
                    lightBleedingDamageEffectType += 1;
                    heavyBleedingDelay = healthEffectsComponent.DamageEffects[EDamageEffectType.HeavyBleeding].Delay;
                }

                if (hasLightBleeding)
                {
                    lightBleedingDamageEffectType += 1;
                    lightBleedingDelay = healthEffectsComponent.DamageEffects[EDamageEffectType.LightBleeding].Delay;
                }
            }
        }

        public class Method18Path : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(ActiveHealthControllerClass)
                    .GetMethod("method_18", BindingFlags.Instance | BindingFlags.NonPublic)
                    .MakeGenericMethod(typeof(ActiveHealthControllerClass.GClass2103));
            }

            [PatchPrefix]
            private static bool PatchPrefix(ActiveHealthControllerClass __instance, ref EBodyPart bodyPart,
                ref ActiveHealthControllerClass.GClass2103 __result)
            {
                try
                {
                    bool flag = true;

                    if (heavyBleedingDamageEffectType == currentCheckLoop &&
                        GetCallingMethodName(3).Contains("MedEffect::Residue"))
                    {
                        heavyBleedingDamageEffectType = -1;

                        Type heavyBleedingType =
                            typeof(ActiveHealthControllerClass).GetNestedType("HeavyBleeding", BindingFlags.NonPublic);
                        MethodInfo findActiveEffectMethod =
                            typeof(ActiveHealthControllerClass).GetMethod("FindActiveEffect");
                        MethodInfo specializedMethod = findActiveEffectMethod.MakeGenericMethod(heavyBleedingType);

                        object[] parameters = new object[] { bodyPart };
                        var val = specializedMethod.Invoke(__instance, parameters);

                        flag = false;
                        __result = null;

                        if (val != null)
                        {
                            if (heavyBleedingType != null && heavyBleedingType.IsAssignableFrom(val.GetType()))
                            {
                                ActiveHealthControllerClass.GClass2103 val2 =
                                    (ActiveHealthControllerClass.GClass2103)val;
                                if (val2.TimeLeft > heavyBleedingDelay)
                                {
                                    val2.AddWorkTime(heavyBleedingDelay, true);
                                }

                                __result = val2;
                            }
                        }
                    }

                    if (lightBleedingDamageEffectType == currentCheckLoop &&
                        GetCallingMethodName(3).Contains("MedEffect::Residue"))
                    {
                        lightBleedingDamageEffectType = -1;

                        Type lightBleedingType =
                            typeof(ActiveHealthControllerClass).GetNestedType("LightBleeding", BindingFlags.NonPublic);
                        MethodInfo findActiveEffectMethod =
                            typeof(ActiveHealthControllerClass).GetMethod("FindActiveEffect");
                        MethodInfo specializedMethod = findActiveEffectMethod.MakeGenericMethod(lightBleedingType);

                        object[] parameters = new object[] { bodyPart };
                        var val = specializedMethod.Invoke(__instance, parameters);

                        flag = false;
                        __result = null;

                        if (val != null)
                        {
                            if (lightBleedingType != null && lightBleedingType.IsAssignableFrom(val.GetType()))
                            {
                                ActiveHealthControllerClass.GClass2103 val2 =
                                    (ActiveHealthControllerClass.GClass2103)val;
                                if (val2.TimeLeft > lightBleedingDelay)
                                {
                                    val2.AddWorkTime(lightBleedingDelay, true);
                                    __result = val2;
                                }
                            }
                        }
                    }

                    currentCheckLoop += 1;
                    return flag;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    currentCheckLoop += 1;
                    return true;
                }
            }
        }
    }
}