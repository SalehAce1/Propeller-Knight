using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using Logger = Modding.Logger;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;
using ModCommon.Util;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace PropellerKnight
{
    internal class ArenaFinder : MonoBehaviour
    {
        GameObject prop;
        public static bool foundBoss = true;


        //xH >= 244.3f && xH <= 252.7f && yH >= 6f && yH <= 7f
        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg0.name == "GG_Mighty_Zote" && arg1.name == "GG_Workshop")
            {
                GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                Destroy(prop.GetComponent<PropFight>());
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
            //if (tier1) bs.StatueState = 
            var gg = new BossStatue.Completion
            {
                completedTier1 = true,
                seenTier3Unlock = true,
                completedTier2 = true,
                completedTier3 = true,
                isUnlocked = true,
                hasBeenSeen = true,
                usingAltVersion = false
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
            yield return null;
            yield return new WaitForSeconds(0.5f);
            PropellerKnight.preloadedGO["prop"] = new GameObject("prop knight");
            prop = PropellerKnight.preloadedGO["prop"];
            prop.SetActive(true);
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            prop.transform.SetPosition2D(xH + 10f, yH + 5f);
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