using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using ModCommon.Util;
using ModCommon;
using Modding;
using UnityEngine;
using UnityEngine.UI;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using System.IO;
using System;
using On;

namespace PropellerKnight
{
    internal class CustomAnimator : MonoBehaviour
    {
        public Dictionary<string, List<Sprite>> animations = new Dictionary<string, List<Sprite>>();
        public bool playing;
        public string animCurr;
        public bool looping;
        private bool queue;
        public IEnumerator Play(string name, bool loop = false, float delay = 0.1f)
        {
            if (playing)
            {
                queue = true;
                yield return new WaitWhile(() => playing);
                queue = false;
            }
            Log(name);
            animCurr = name;
            playing = true;
            looping = loop;
            do
            {
                foreach (var i in animations[name])
                {
                    gameObject.GetComponent<SpriteRenderer>().sprite = i;
                    yield return new WaitForSeconds(delay);
                }
                yield return null;
            }
            while (loop && !queue);
            animCurr = "";
            playing = false;
        }
        private static void Log(object obj)
        {
            Logger.Log("[Custom Animator] " + obj);
        }
    }
}