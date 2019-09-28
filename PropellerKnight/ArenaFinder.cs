using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Modding.Logger;
using UObject = UnityEngine.Object;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace PropellerKnight
{
    internal class ArenaFinder : MonoBehaviour
    {
        public static Dictionary<string, AudioClip> audioClips;
        public static Dictionary<string, Material> materials;
        public static Dictionary<string, RuntimeAnimatorController> animators;
        public static bool tier1;
        public static bool tier2;
        public static bool tier3;
        public static List<Sprite> sprites;
        public static GameObject windPart;
        public static AnimationClip explodePart;
        private GameObject prop;
        public static bool foundBoss;


        //xH >= 244.3f && xH <= 252.7f && yH >= 6f && yH <= 7f
        private void Start()
        {
            audioClips = new Dictionary<string, AudioClip>();
            materials = new Dictionary<string, Material>();
            sprites = new List<Sprite>();
            animators = new Dictionary<string, RuntimeAnimatorController>();

            string path = "";
            switch(SystemInfo.operatingSystemFamily)
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
            USceneManager.activeSceneChanged += SceneChanged;
            AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, path));
            UObject[] assets = ab.LoadAllAssets();
            PropellerKnight.preloadedGO["prop"] = ab.LoadAsset("prop asset") as GameObject;
            PropellerKnight.preloadedGO["ship"] = ab.LoadAsset("ship") as GameObject;
            PropellerKnight.preloadedGO["ball"] = ab.LoadAsset<GameObject>("ballBig");
            PropellerKnight.preloadedGO["bomb"] = ab.LoadAsset<GameObject>("bomb");
            explodePart = ab.LoadAsset<AnimationClip>("explode");
            animators["prop"] = ab.LoadAsset("propController") as RuntimeAnimatorController;
            audioClips["canon"] = ab.LoadAsset("canonS") as AudioClip;
            audioClips["dash"] = ab.LoadAsset("dashS") as AudioClip;
            audioClips["flip"] = ab.LoadAsset("flipS") as AudioClip;
            audioClips["floor"] = ab.LoadAsset("floorS") as AudioClip;
            audioClips["music"] = ab.LoadAsset("musicS") as AudioClip;
            audioClips["spin"] = ab.LoadAsset("spinS") as AudioClip;
            materials["flash"] = ab.LoadAsset("Material") as Material;
            windPart = ab.LoadAsset("windPart") as GameObject;
            var mod2 = windPart.GetComponent<ParticleSystemRenderer>();
            mod2.renderMode = ParticleSystemRenderMode.Billboard;
            mod2.material = new Material(Shader.Find("Sprites/Default"));
            //for (int i = 28; i < 84; i++) sprites.Add(assets[i] as Sprite);
            //ab.Unload(false);

        }
        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg0.name == "GG_Mighty_Zote" && arg1.name == "GG_Workshop")
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                Destroy(prop.GetComponent<PropFight>());
                Destroy(prop);
                PlayerData.instance.isInvincible = false;
            }

            if (arg1.name == "GG_Workshop") SetStatue();

            if (arg1.name != "GG_Mighty_Zote") return;
            if (arg0.name != "GG_Workshop") return;
            StartCoroutine(AddComponent());
        }

        private void SetStatue()
        {

            //Used 56's pale prince code here
            GameObject statue = Instantiate(GameObject.Find("GG_Statue_ElderHu"));
            statue.transform.SetPosition2D(25.4f, statue.transform.GetPositionY());//6.5f); //248
            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Mighty_Zote";
            var bs = statue.GetComponent<BossStatue>();
            bs.bossScene = scene;
            bs.statueStatePD = "PropKnightArena";
            //if () bs.StatueState = 
            var gg = new BossStatue.Completion
            {
                completedTier1 = true,
                seenTier3Unlock = true,
                completedTier2 = true,
                completedTier3 = true,
                isUnlocked = true,
                hasBeenSeen = true,
                usingAltVersion = false,
            };
            bs.StatueState = gg;
            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "PROP_NAME";
            details.descriptionKey = details.descriptionSheet = "PROP_DESC";
            bs.bossDetails = details;
            foreach (var i in bs.statueDisplay.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.sprite = new Sprite();
            }

        }

        private IEnumerator AddComponent()
        {
            yield return null;
            Destroy(GameObject.Find("Battle Control"));
            Destroy(GameObject.Find("Zote Boss"));
            //Changed dict prop to local prop
            prop = Instantiate(PropellerKnight.preloadedGO["prop"]);
            prop.SetActive(true);
            var _hm = prop.AddComponent<HealthManager>();
            HealthManager hornHP = PropellerKnight.preloadedGO["hornet"].GetComponent<HealthManager>();
            GameObject hornetFab = Modding.ReflectionHelper.GetAttr<HealthManager, GameObject>(hornHP, "strikeNailPrefab");
            GameObject hornetFab2 = Modding.ReflectionHelper.GetAttr<HealthManager, GameObject>(hornHP, "slashImpactPrefab");
            GameObject hornetFab3 = Modding.ReflectionHelper.GetAttr<HealthManager, GameObject>(hornHP, "blockHitPrefab");
            GameObject hornetFab4 = Modding.ReflectionHelper.GetAttr<HealthManager, GameObject>(hornHP, "fireballHitPrefab");
            GameObject hornetFab5 = Modding.ReflectionHelper.GetAttr<HealthManager, GameObject>(hornHP, "sharpShadowImpactPrefab");
            Modding.ReflectionHelper.SetAttr(_hm, "strikeNailPrefab", hornetFab);
            Modding.ReflectionHelper.SetAttr(_hm, "slashImpactPrefab", hornetFab2);
            Modding.ReflectionHelper.SetAttr(_hm, "blockHitPrefab", hornetFab3);
            Modding.ReflectionHelper.SetAttr(_hm, "fireballHitPrefab", hornetFab4);
            Modding.ReflectionHelper.SetAttr(_hm, "sharpShadowImpactPrefab", hornetFab5);
            var _sr = prop.GetComponent<SpriteRenderer>();
            _sr.material = materials["flash"];

            prop.AddComponent<PropFight>();
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
        }
        public static void Log(object o)
        {
            Logger.Log("[Lost Arena] " + o);
        }
    }
}