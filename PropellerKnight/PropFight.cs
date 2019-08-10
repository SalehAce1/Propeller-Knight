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
using GlobalEnums;

namespace PropellerKnight
{
    internal class PropFight : MonoBehaviour
    {
        private CustomAnimator _anim;
        private Rigidbody2D _rb;
        private DamageHero _dmg;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
        private Recoil _rc;
        private HeroController _target;
        private HealthManager _hm;
        private SpriteFlash _sf;
        private float origHeight;
        private List<Sprite> pkSprites = new List<Sprite>();
        private bool introTextDone;
        private bool allIntroDone;
        private bool dashing;
        private bool grounded;
        private void Awake()
        {
            _anim = gameObject.AddComponent<CustomAnimator>();
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _dmg = gameObject.AddComponent<DamageHero>();
            _bc = gameObject.AddComponent<BoxCollider2D>();
            _rc = gameObject.AddComponent<Recoil>();
            _hm = gameObject.AddComponent<HealthManager>();
            gameObject.AddComponent<EnemyHitEffectsUninfected>();
            _sf = gameObject.AddComponent<SpriteFlash>();
            _target = HeroController.instance;
        }
        private void Start()
        {
            _hm.hp = 1000;
            _rc.SetRecoilSpeed(15f);
            gameObject.layer = 11;
            _dmg.damageDealt = 1;
            gameObject.transform.localScale *= 7.5f;
            gameObject.transform.SetPosition2D(_target.transform.position.x + 10f, _target.transform.position.y + 6f);
            _rb.gravityScale = 0f;
            gameObject.transform.localScale = new Vector2(gameObject.transform.localScale.x * -1f, gameObject.transform.localScale.y);
            origHeight = gameObject.transform.position.y;
            AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, "propk"));
            UnityEngine.Object[] go = ab.LoadAllAssets();
            for (int i = 1; i < 56; i++) pkSprites.Add(go[i] as Sprite);
            ab.Unload(false);

            _anim.animations.Add("fly", pkSprites.GetRange(24, 4));
            _anim.animations.Add("flair", pkSprites.GetRange(8, 5));
            _anim.animations.Add("dash", pkSprites.GetRange(15, 4));
            _anim.animations.Add("forWindAntic", pkSprites.GetRange(0, 3));
            _anim.animations.Add("forWind", pkSprites.GetRange(3, 4));
            _anim.animations.Add("forWindEnd", pkSprites.GetRange(7, 3));
            _anim.animations.Add("flipSide", pkSprites.GetRange(33, 6));
            _anim.animations.Add("laugh", pkSprites.GetRange(13, 2));
            _anim.animations.Add("upAttackAntic", pkSprites.GetRange(28, 2));
            _anim.animations.Add("upAttack", pkSprites.GetRange(30, 2));
            _anim.animations.Add("upWindAntic", pkSprites.GetRange(19, 1));
            _anim.animations.Add("upWind", pkSprites.GetRange(20, 4));

            StartCoroutine(_anim.Play("fly", true, 0.08f));
            StartCoroutine(AttackChoice());
        }

        IEnumerator AttackChoice()
        {
            StartCoroutine(IntroText());
            StartCoroutine(IntroAnim());
            yield return new WaitWhile(() => !allIntroDone);
            while (true)
            {
                if (!dashing)
                {
                    yield return new WaitForSeconds(0.3f);
                    StartCoroutine(Dash());
                    yield return new WaitForSeconds(0.3f);
                }
                yield return null;
            }
        }

        IEnumerator IntroText()
        {
            yield return new WaitForSeconds(1f);
            if (!ArenaFinder.foundBoss)
            {
                GameObject text = GameObject.Find("DialogueManager");
                var txtfsm = text.LocateMyFSM("Box Open");
                txtfsm.SendEvent("BOX UP");
                yield return null;
                GameManager.instance.playerData.SetBool("disablePause", true);
                _target.RelinquishControl();
                _target.StopAnimationControl();
                _target.gameObject.GetComponent<tk2dSpriteAnimator>().Play("LookUp");
                _target.transform.localScale = new Vector2(-1f*Mathf.Abs(_target.transform.localScale.x), _target.transform.localScale.y);
                GameObject sec = text.transform.Find("Text").gameObject;
                sec.GetComponent<DialogueBox>().StartConversation("testee", "testudo");
                yield return new WaitWhile(() => sec.GetComponent<DialogueBox>().currentPage <= 3);
                txtfsm.SendEvent("BOX DOWN");
                text.SetActive(false);
                _target.RegainControl();
                GameManager.instance.playerData.SetBool("disablePause", false);
                _target.StartAnimationControl();
                ArenaFinder.foundBoss = true;
                yield return new WaitForSeconds(0.5f);
            }
            introTextDone = true;
        }

        IEnumerator IntroAnim()
        {
            while (!introTextDone)
            {
                Vector2 pos = transform.position;
                float newY = Mathf.Sin(Time.time * 2.5f) * 0.5f;
                transform.position = new Vector2(pos.x, newY + origHeight);
                yield return null;
            }
            _rb.gravityScale = 0.5f;
            yield return new WaitWhile(() => !grounded);
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f, 0f);
            StartCoroutine(_anim.Play("laugh", true, 0.1f));
            yield return new WaitForSeconds(1f);
            allIntroDone = true;
        }

        IEnumerator Dash()
        {
            dashing = true;
            float dir = FaceHero();
            StartCoroutine(_anim.Play("dash", false, 0.1f));
            yield return new WaitWhile(() => !_anim.animCurr.Contains("dash"));
            yield return new WaitForSeconds(0.12f);
            _rb.velocity = new Vector2(dir * 35f, 0f);
            yield return new WaitWhile(() => _anim.playing);
            FaceHero();
            _rb.velocity = new Vector2(0f, 0f);
            StartCoroutine(_anim.Play("flair", false, 0.08f));
            dashing = false;
        }

        private float FaceHero()
        {
            var heroSignX = Mathf.Sign(_target.transform.GetPositionX()-gameObject.transform.GetPositionX());
            var pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector2(Mathf.Abs(pScale.x) * heroSignX, pScale.y);
            return heroSignX;
        }

        void Update()
        {
            Log(_hm.hp);
            Vector2 divisor = new Vector2(2f,1.1f);
            _bc.offset = new Vector2(0, 0);
            _bc.size = new Vector3(Mathf.Abs(_sr.bounds.size.x / transform.lossyScale.x/divisor.x),
                                         Mathf.Abs(_sr.bounds.size.y / transform.lossyScale.y/divisor.y),
                                         Mathf.Abs(_sr.bounds.size.z / transform.lossyScale.z));
            if (gameObject.transform.GetPositionY() < 6.6f) grounded = true;
            else grounded = false;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
        }

        private static void Log(object obj)
        {
            Logger.Log("[Propeller Knight] " + obj);
        }
    }
}


/*IEnumerator AnimPlay() //Replace all hk bosses with new bosses
        {
            yield return new WaitForSeconds(1f);
            while (true)
            {
                List<string> names = new List<string>(_anim.animations.Keys);
                bool left;
                bool right;
                for (int i = 0; i < names.Count; i++)
                {
                    left = false;
                    right = false;
                    string name = names[i];
                    while (!left && !right)
                    {
                        Log("ready");
                        if (Input.GetKey(KeyCode.Mouse2))
                        {
                            StartCoroutine(_anim.Play(name, false, 0.08f));
                            yield return null;
                            yield return new WaitWhile(() => _anim.name == name);
                        }
                        if (Input.GetKey(KeyCode.Mouse0)) left = true;
                        if (Input.GetKey(KeyCode.Mouse1)) right = true;
                        yield return null;
                    }
                    if (left && i > 0) i -= 2;
                    yield return null;
                    yield return new WaitForSeconds(1f);
                }
            }
            Log("left");
        }*/

