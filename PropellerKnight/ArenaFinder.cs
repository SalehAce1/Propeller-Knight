using System.Collections;
using System.Collections.Generic;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;
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
        public static int BossLevel = -1;
        public static List<Sprite> sprites;
        public static GameObject windPart;
        public static AnimationClip explodePart;
        private GameObject prop;
        public static bool foundBoss;


        //xH >= 244.3f && xH <= 252.7f && yH >= 6f && yH <= 7f
        private void Start()
        {
            PlayerData.instance.hornet1Defeated = true;

            audioClips = new Dictionary<string, AudioClip>();
            materials = new Dictionary<string, Material>();
            sprites = new List<Sprite>();
            animators = new Dictionary<string, RuntimeAnimatorController>();
            
            USceneManager.activeSceneChanged += SceneChanged;
            On.BossChallengeUI.LoadBoss_int_bool += BossChallengeUI_LoadBoss_int_bool;
            On.GameManager.BeginSceneTransition += GameManager_BeginSceneTransition;
            AssetBundle ab = null;
            foreach (var i in PropellerKnight.assetbundles)
            {
                if (i.Key.Contains("propk")) ab = i.Value;
            }

            if (ab == null)
            {
                Log("ERROR: Bundles did not load.");
                return;
            }
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

        private void SceneChanged(Scene arg0, Scene arg1) //Fungus1_04_boss
        {
            if (arg1.name == "GG_Workshop") SetStatue();
            if (BossLevel == -1) return;
            if (arg1.name != "GG_Mighty_Zote") BossLevel = -1;
            if (arg0.name == "GG_Mighty_Zote" && arg1.name == "GG_Workshop")
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                foreach (GameObject go in FindObjectsOfType<GameObject>().Where(x => !x.name.Contains(gameObject.name) && x.GetComponent<DamageHero>() != null))
                {
                    Destroy(go);
                }
                Destroy(prop.GetComponent<PropFight>());
                Destroy(prop.GetComponent<PropDeath>());
                Destroy(prop);
                PlayerData.instance.isInvincible = false;
            }

            if (arg1.name != "GG_Mighty_Zote") return;
            if (arg0.name != "GG_Workshop") return;
            StartCoroutine(AddComponent());
        }
        
        private void GameManager_BeginSceneTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            Log("PROP " + info.EntryGateName);
            Log("PROP " + BossLevel);
            if (info.SceneName == "GG_Workshop" && BossLevel != -1)
            {
                info.EntryGateName = "door_dreamReturnGGPropeller";
            }

            orig(self, info);
        }
        
        //For Godhome
        private void SetStatue()
        {
            //Used 56's pale prince code here
            GameObject statue = Instantiate(GameObject.Find("GG_Statue_ElderHu"));
            statue.transform.SetPosition2D(30f, statue.transform.GetPositionY());//6.5f); //248
            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Mighty_Zote";
            var bs = statue.GetComponent<BossStatue>();
            bs.bossScene = scene;
            bs.statueStatePD = "statueStateProp";
            foreach (Transform i in statue.transform)
            {
                if (i.name.Contains("door"))
                {
                    i.name = "door_dreamReturnGGPropeller";
                }
            }
            bs.SetPlaquesVisible(bs.StatueState.isUnlocked && bs.StatueState.hasBeenSeen);
            bs.StatueState = ((GlobalModSettings) PropellerKnight._settings).CompletionPropeller;
            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "PROP_NAME";
            details.descriptionKey = details.descriptionSheet = "PROP_DESC";
            bs.bossDetails = details;
            foreach (var i in bs.statueDisplay.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.sprite = PropellerKnight.Sprites[0];
                i.transform.localScale *= 1.6f;
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
            foreach (FieldInfo fi in typeof(HealthManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(x => x.Name.Contains("Prefab")))
            {
                fi.SetValue(_hm, fi.GetValue(hornHP));
            }
            ReflectionHelper.SetField(_hm, "enemyDamageAudio",ReflectionHelper.GetField<HealthManager, AudioEvent>(hornHP, "enemyDamageAudio"));

            var _sr = prop.GetComponent<SpriteRenderer>();
            _sr.material = materials["flash"];

            prop.AddComponent<PropFight>();
        }
        
        private void BossChallengeUI_LoadBoss_int_bool(On.BossChallengeUI.orig_LoadBoss_int_bool orig, BossChallengeUI self, int level, bool doHideAnim)
        {
            string bName = self.transform.Find("Panel").Find("BossName_Text").GetComponent<Text>().text;
            Log("GO " + bName);
            if (bName.Contains("Propeller"))
            {
                BossLevel = level;
            }
            orig(self, level, doHideAnim);
        }

        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
            On.GameManager.BeginSceneTransition -= GameManager_BeginSceneTransition;
            On.BossChallengeUI.LoadBoss_int_bool -= BossChallengeUI_LoadBoss_int_bool;
        }
        public static void Log(object o)
        {
            Logger.Log("[Lost Arena] " + o);
        }
    }
}