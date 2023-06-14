using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using EFT.InventoryLogic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BleedingDelay
{
    [BepInPlugin("com.MarsyApp.BleedingDelay", "MarsyApp-BleedingDelay", "1.0.0")]
    public class BleedingDelay : BaseUnityPlugin
    {
        private void Awake()
        {
            Patcher.PatchAll();
            Logger.LogInfo($"Plugin BleedingDelay is loaded!");
        }
        
        private void OnDestroy()
        {
            Patcher.UnpatchAll();
            Logger.LogInfo($"Plugin BleedingDelay is unloaded!");
        }
    }
}
