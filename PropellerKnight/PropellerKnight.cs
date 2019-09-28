using JetBrains.Annotations;
using Modding;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace PropellerKnight
{
    [UsedImplicitly]
    public class PropellerKnight : Mod, ITogglableMod
    {
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();
        public static PropellerKnight Instance;
        public static readonly List<Sprite> SPRITES = new List<Sprite>();

        public override string GetVersion()
        {
            return "0.0.0.0";
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Hornet_2","Boss Holder/Hornet Boss 2"),
                ("Room_Colosseum_Bronze", "Colosseum Manager/Waves/Arena 8/Colosseum Platform (1)"),
                ("Room_Colosseum_Bronze", "Colosseum Manager/Ground Spikes/Colosseum Spike"),
                ("Ruins1_24_boss", "Mage Lord")
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GOs");
            preloadedGO.Add("hornet", preloadedObjects["GG_Hornet_2"]["Boss Holder/Hornet Boss 2"]);
            preloadedGO.Add("plat", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Waves/Arena 8/Colosseum Platform (1)"]);
            preloadedGO.Add("spike", preloadedObjects["Room_Colosseum_Bronze"]["Colosseum Manager/Ground Spikes/Colosseum Spike"]);
            preloadedGO.Add("wave", preloadedObjects["Ruins1_24_boss"]["Mage Lord"]);
            preloadedGO.Add("prop", null);
            preloadedGO.Add("ship", null);
            preloadedGO.Add("ball", null);
            preloadedGO.Add("bomb", null);
            Instance = this;
            Log("Initalizing.");

            Unload();
            ModHooks.Instance.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += LangGet;
        }

        private string LangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "PROP_NAME": return "Propeller Knight";
                case "PROP_DESC": return "Airborne god of a distant land.";
                case "testee": return "Huh...what a strange place this is. A world full of preposterous curves and dreams! And who might you be little knight?<page>A silent one you are, makes me almost miss my fabulous blue friend from across land and time.<page>In a different world I might have given you a tour of my magnificent ship first but time runs short. En Garde!<page>";
                case "PROP_END": return "You show great strength tiny knight, but your prowess will do little against what is to come...<page>";
                default: return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private void AfterSaveGameLoad(SaveGameData data) => AddComponent();

        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<ArenaFinder>();
        }

        public void Unload()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            ModHooks.Instance.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= LangGet;

            // ReSharper disable once Unity.NoNullPropogation
            var x = GameManager.instance?.gameObject.GetComponent<ArenaFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}