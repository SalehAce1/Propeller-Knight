using System;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using UnityEngine;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;

namespace PropellerKnight
{
    [UsedImplicitly]
    public class PropellerKnight : Mod, ITogglableMod,IGlobalSettings<GlobalModSettings>
    {
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();
        public static PropellerKnight Instance;
        public static readonly List<Sprite> SPRITES = new List<Sprite>();
        public static Dictionary<string, AssetBundle> assetbundles = new Dictionary<string, AssetBundle>();
        public static readonly List<Sprite> Sprites = new List<Sprite>();
       
        public static GlobalModSettings _settings = new GlobalModSettings();
        public void OnLoadGlobal(GlobalModSettings s)=>_settings = s;
        public GlobalModSettings OnSaveGlobal() => _settings;

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
            On.HeroController.Start += AddCP;
            ModHooks.LanguageGetHook += LangGet;
            ModHooks.SetPlayerVariableHook += SetVariableHook;
            ModHooks.GetPlayerVariableHook += GetVariableHook;
            
            string path = "";
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                    path = "propkWin";
                    break;
                case OperatingSystemFamily.Linux:
                    path = "propkLin";
                    break;
                case OperatingSystemFamily.MacOSX:
                    path = "propkMC";
                    break;
                default:
                    Log("ERROR UNSUPPORTED SYSTEM: " + SystemInfo.operatingSystemFamily);
                    return;
            }
            
            Assembly asm = Assembly.GetExecutingAssembly();
            int ind = 0;
            foreach (string res in asm.GetManifestResourceNames())
            {
                using (Stream s = asm.GetManifestResourceStream(res))
                {
                    if (s == null) continue;
                    byte[] buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Dispose();
                    if (res.EndsWith(".png"))
                    {
                        // Create texture from bytes
                        var tex = new Texture2D(1, 1);
                        tex.LoadImage(buffer, true);
                        // Create sprite from texture
                        Sprites.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
                        Log("Created sprite from embedded image: " + res + " at ind " + ++ind);
                    }
                    else
                    {
                        string bundleName = Path.GetExtension(res).Substring(1);
                        if (bundleName != path) continue;
                        assetbundles[bundleName] = AssetBundle.LoadFromMemory(buffer);   
                    }
                }
            }
        }

        private void AddCP(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);
            if( GameManager.instance.gameObject.GetComponent<ArenaFinder>()==null)
            {
                AddComponent();
            }
        }

        private string LangGet(string key, string sheettitle,string orig)
        {
            switch (key)
            {
                case "PROP_NAME": return "Propeller Knight";
                case "PROP_DESC": return "Airborne god of a distant land.";
                case "testee": return "Huh...what a strange place this is. A world full of preposterous curves and dreams! And who might you be little knight?<page>A silent one you are, makes me almost miss my fabulous blue friend from across land and time.<page>In a different world I might have given you a tour of my magnificent ship first but time runs short. En Garde!<page>";
                case "PROP_END": return "You show great strength tiny knight, but your prowess will do little against what is to come...<page>";
                default:return orig;
            }
        }
        
        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "statueStateProp")
                _settings.CompletionPropeller = (BossStatue.Completion)obj;
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            if (key == "statueStateProp")
                return _settings.CompletionPropeller;
            return orig;
        }

        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<ArenaFinder>();
        }

        public void Unload()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            ModHooks.LanguageGetHook -= LangGet;
            On.HeroController.Start -= AddCP;
            ModHooks.SetPlayerVariableHook -= SetVariableHook;
            ModHooks.GetPlayerVariableHook -= GetVariableHook;

            // ReSharper disable once Unity.NoNullPropogation
            var x = GameManager.instance?.gameObject.GetComponent<ArenaFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}