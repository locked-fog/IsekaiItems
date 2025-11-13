using HarmonyLib;
using ItemStatsSystem.Data;
using System;

namespace IsekaiItems
{

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public Harmony harmony;

        protected void OnAfterSetup()
        {
            
        }

        private void OnEnable()
        {
            HarmonyLoad.Load0Harmony();
        }

        private void Start()
        {
            harmony = new Harmony("isekaiitems");
            harmony.PatchAll();
        }

        private void OnDisable()
        {
            harmony.UnpatchAll("isekaiitems");
        }

        public struct ItemConfig
        {
            public string LocalizationDesc
            {
                get
                {
                    return this.LocalizationKey + "_Desc";
                }
            }

            public int OriginalItemId;
            public int NewItemId;
            public string DisplayName;
            public string LocalizationKey;
            public string LocalizationDescValue;
            public float Weight;
            public int Value;
            public string RequireTagName;
            public int SlotCount;
            public string EmbeddedSpritePath;
            public int Quality;
            public string[] Tags;
            public int DecomMoney;


        }
    }

    
}