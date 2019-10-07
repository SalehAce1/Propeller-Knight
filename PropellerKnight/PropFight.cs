using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using Modding;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Logger = Modding.Logger;

namespace PropellerKnight
{
    internal class PropFight : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private DamageHero _dmg;
        private BoxCollider2D _bc;
        private SpriteRenderer _sr;
        private Recoil _rc;
        private HeroController _target;
        private HealthManager _hm;
        private AudioSource _aud;
        private GameObject musicControl;
        private Animator _anim;
        private Text title;
        private GameObject canvas;
        private bool soundOverride;
        private bool flashing;
        private float origHeight;
        private bool introTextDone;
        private bool allIntroDone;
        private bool grounded;
        private bool heroDmg;
        private bool propDmg;
        private bool doNextAttack;
        private float idleTime;
        private bool aboveHead;
        private const float GROUND_Y = 5.25f;
        private const float RIGHT_X = 115.5f;
        private const float LEFT_X = 88.5f;
        private const int HP_MAX = 225;
        private const int HP_PHASE2 = 150;

        private void Awake()
        {
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _dmg = gameObject.AddComponent<DamageHero>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _rc = gameObject.AddComponent<Recoil>();
            _hm = gameObject.GetComponent<HealthManager>();
            _aud = gameObject.AddComponent<AudioSource>();
            _anim = gameObject.GetComponent<Animator>();
            _target = HeroController.instance;
        }

        private void Start()
        {
            PropDeath.isDying = false;

            ModHooks.Instance.TakeHealthHook += Instance_TakeHealthHook;
            On.HealthManager.TakeDamage += HealthManager_TakeDamage;
            _hm.OnDeath += _hm_OnDeath;

            SetSoundSettings(_aud);
            gameObject.layer = 11;
            gameObject.transform.SetPosition3D(HeroController.instance.transform.position.x + 10f, HeroController.instance.transform.position.y + 6f, 0f);
            gameObject.transform.localScale = new Vector2(gameObject.transform.localScale.x * -1f, gameObject.transform.localScale.y);

            _hm.hp = HP_MAX;
            _bc.isTrigger = true;
            _rc.SetRecoilSpeed(15f);
            _rb.gravityScale = 0f;
            origHeight = gameObject.transform.position.y;
            idleTime = 0.3f;
            for (float i = 88.5f; i < 116.5f; i += 3.3f)
            {
                SpawnPlatform(0, i, 21f, 0f, 180f);
                var spk = SpawnPlatform(1, i - 0.1f, 21f - 1.7f, 0.5f, 180f);
                spk.transform.localScale *= 1.2f;
                spk.transform.localScale = new Vector2(spk.transform.localScale.x * 1.4f, spk.transform.localScale.y);
                spk.AddComponent<SpikeCollider>();
            }

            for (float i = 5f; i < 22f; i += 2)
            {
                GameObject spk = SpawnPlatform(1, 87.2f, i, 0.5f, 270f);
                spk.AddComponent<SpikeCollider>();
            }

            for (float i = 5f; i < 22f; i += 2f)
            {
                GameObject spk = SpawnPlatform(1, 118.8f, i, 0.5f, 90f);
                spk.AddComponent<SpikeCollider>();
            }
            _anim.Play("fly");
            StartCoroutine(AttackChoice());
        }

        private int Instance_TakeHealthHook(int damage)
        {
            heroDmg = true;
            HeroController.instance.INVUL_TIME = 1.3f;
            return damage;
        }

        private void _hm_OnDeath()
        {
            gameObject.AddComponent<PropDeath>();
            _aud.Stop();
            musicControl.GetComponent<AudioSource>().Stop();
            Destroy(gameObject.GetComponent<BoxCollider2D>());
            StopAllCoroutines();
            Destroy(gameObject.GetComponent<PropFight>());
        }

        private void HealthManager_TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            if (self.name.Contains("prop"))
            {
                propDmg = true;
                if (!flashing)
                {
                    flashing = true;
                    StartCoroutine(FlashWhite());
                }
            }
            orig(self, hitInstance);
        }

        private bool transition;
        IEnumerator AttackChoice()
        {
            StartCoroutine(IntroText());
            StartCoroutine(IntroAnim());
            yield return new WaitWhile(() => !allIntroDone);
            StartCoroutine(MusicControl());
            Coroutine lastC = null;
            while (_hm.hp > HP_PHASE2)
            {
                int rand = Random.Range(0, 2);
                if (aboveHead) lastC = StartCoroutine(PushUp());
                else if (rand == 0) lastC = StartCoroutine(AirAtt());
                else lastC = StartCoroutine(Dash());
                doNextAttack = false;
                yield return new WaitWhile(() => !doNextAttack);
                yield return new WaitForSeconds(idleTime);
            }
            if (lastC != null) StopCoroutine(lastC);
            StartCoroutine(FlySweep());
            yield return new WaitWhile(() => !transition);
            transition = false;

            while (true)
            {
                int rand = Random.Range(0, 2);
                int rand2 = Random.Range(0, 10);
                if (rand2==0)
                {
                    StartCoroutine(FlySweep());
                    yield return new WaitWhile(() => !transition);
                    transition = false;
                }
                if (aboveHead) lastC = StartCoroutine(PushUp());
                else if (rand == 0 && !BombControl.bombExist) lastC = StartCoroutine(AirAtt());
                else lastC = StartCoroutine(Dash());
                doNextAttack = false;
                yield return new WaitWhile(() => !doNextAttack);
                yield return new WaitForSeconds(idleTime);
            }


        }

        IEnumerator IntroText() //Say Made by Yacht Club Games and ripped by Daxar
        {
            yield return new WaitForSeconds(1f);
            if (!ArenaFinder.foundBoss)
            {
                GameObject text = Instantiate(GameObject.Find("DialogueManager"));
                var txtfsm = text.LocateMyFSM("Box Open");
                txtfsm.SendEvent("BOX UP");
                yield return null;
                GameManager.instance.playerData.disablePause = true;
                _target.RelinquishControl();
                _target.StopAnimationControl();
                _target.gameObject.GetComponent<tk2dSpriteAnimator>().Play("LookUp");
                _target.transform.localScale = new Vector2(-1f * Mathf.Abs(_target.transform.localScale.x), _target.transform.localScale.y);
                GameObject sec = text.transform.Find("Text").gameObject;
                sec.GetComponent<DialogueBox>().StartConversation("testee", "testudo");
                yield return new WaitWhile(() => sec.GetComponent<DialogueBox>().currentPage <= 3);
                txtfsm.SendEvent("BOX DOWN");
                text.SetActive(false);
                _target.RegainControl();
                GameManager.instance.playerData.disablePause = false;
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
            StartCoroutine(EndingTextFade());
            _rb.gravityScale = 0.5f;
            yield return new WaitWhile(() => !grounded);
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f, 0f);
            _anim.Play("laugh");
            yield return new WaitForSeconds(1f);
            allIntroDone = true;
        }

        IEnumerator AirAtt()
        {
            _rb.velocity = new Vector2(0f, 0f);
            float dir = FaceHero();
            _anim.Play("flair");
            _anim.speed *= 1.3f;
            yield return new WaitForSeconds(0.05f);
            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return new WaitForSeconds(0.05f);
            _anim.speed /= 1.3f;
            _anim.Play("summon");
            _rb.velocity = new Vector2(dir * 15f, 15f);
            yield return new WaitForSeconds(0.5f);
            _rb.velocity = new Vector2(0f, 0f);
            _anim.Play("fly");
            _rb.gravityScale = 4f;
            yield return new WaitWhile(() => !grounded);
            _rb.velocity = new Vector2(0f, 0f);
            _rb.gravityScale = 0f;
            _anim.Play("idle");

            SpawnWave(true);
            SpawnWave(false);

            yield return new WaitForSeconds(0.9f);
            doNextAttack = true;

        }

        private void SpawnWave(bool faceRight)
        {
            PlayMakerFSM lord = PropellerKnight.preloadedGO["wave"].LocateMyFSM("Mage Lord");
            GameObject go = Instantiate(lord.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value);
            go.transform.localScale = new Vector2(1.5f, 1f);
            PlayMakerFSM shock = go.LocateMyFSM("shockwave");
            shock.FsmVariables.FindFsmBool("Facing Right").Value = faceRight;
            shock.FsmVariables.FindFsmFloat("Speed").Value = 22f;
            go.SetActive(true);
            go.transform.SetPosition2D(gameObject.transform.position.x, gameObject.transform.position.y-0.2f);
        }

        IEnumerator Dash()
        {
            float dir = FaceHero();
            _anim.Play("dashAntic");
            yield return new WaitForSeconds(0.35f);
            _anim.Play("dash");
            while (_anim.GetCurrentFrame() < 1)
            {
                if (aboveHead)
                {
                    StartCoroutine(PushUp());
                    yield break;
                }
                yield return new WaitForEndOfFrame();
            }
            _aud.clip = ArenaFinder.audioClips["dash"];
            _aud.Play();
            _rb.velocity = new Vector2(dir * 30f, 0f);
            while (_anim.GetCurrentFrame() < 3)
            {
                if (aboveHead)
                {
                    StartCoroutine(PushUp());
                    yield break;
                }
                yield return new WaitForEndOfFrame();
            }
            _anim.TogglePause();
            yield return new WaitForSeconds(0.05f);
            _anim.TogglePause();
            yield return new WaitWhile(() => _anim.IsPlaying());
            FaceHero();
            _rb.velocity = new Vector2(0f, 0f);
            if (_hm.hp > HP_PHASE2) yield return new WaitForSeconds(0.05f);
            else yield return new WaitForSeconds(0.25f);
            doNextAttack = true;
        }

        IEnumerator PushUp()
        {
            FaceHero();
            _rb.velocity = new Vector2(0f, 0f);
            _anim.Play("upWind");
            GameObject particle = Instantiate(ArenaFinder.windPart);
            particle.transform.SetPosition2D(gameObject.transform.position.x + 0.1f, gameObject.transform.position.y + 0.4f);
            particle.SetActive(true);
            _aud.clip = ArenaFinder.audioClips["spin"];
            _aud.Play();
            Rigidbody2D tmp = _target.GetComponent<Rigidbody2D>();
            float speed = 5f;
            bool upCut = false;
            while (aboveHead)
            {
                tmp.velocity = new Vector2(0f, speed);
                speed += 2f;
                if (_target.transform.GetPositionY() > 18f) upCut = true;
                yield return new WaitForEndOfFrame();
            }
            particle.GetComponent<ParticleSystem>().Stop();
            if (upCut)
            {
                HeroController.instance.INVUL_TIME = 0f;
                propDmg = false;
                _anim.Play("upAttAntic");
                yield return new WaitWhile(() => _anim.IsPlaying());
                _anim.Play("upAtt");
                while (_target.transform.GetPositionY() > 7f)
                {
                    FaceHero();
                    Vector2 special = new Vector2(_target.transform.GetPositionX(), gameObject.transform.GetPositionY());
                    gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, special, Time.deltaTime * 20f);
                    yield return new WaitForEndOfFrame();
                    if (propDmg)
                    {
                        float side = FaceHero();
                        propDmg = false;
                        _rb.gravityScale = 1f;
                        _rb.velocity = new Vector2(Mathf.Abs(_target.transform.GetPositionX() - gameObject.transform.GetPositionX()) * side, 23f);
                        yield return new WaitForSeconds(0.2f);
                        yield return new WaitWhile(() => !grounded);
                        _rb.gravityScale = 0f;
                        _rb.velocity = new Vector2(0f, 0f);
                        break;
                    }
                }
                HeroController.instance.INVUL_TIME = 1.3f;
            }
            _anim.Play("flipSide");
            _aud.clip = ArenaFinder.audioClips["flip"];
            _aud.Play();
            yield return new WaitWhile(() => _anim.IsPlaying());

            doNextAttack = true;

        }

        private GameObject ship;
        IEnumerator FlySweep()
        {
            Vector2 origPos = new Vector2(103f, 4f);
            Vector2 pos1 = origPos + new Vector2(-14.26f, 12f);
            Vector2 pos2 = origPos + new Vector2(-6.5f, -2.4f);
            Vector2 pos2_5 = origPos + new Vector2(6.5f, -2.4f);
            Vector2 pos3 = origPos + new Vector2(14.15f, 12f);

            _anim.Play("fly");
            _rb.gravityScale = 0f;

            float te = 1f;
            gameObject.transform.localScale = new Vector2(Mathf.Abs(gameObject.transform.localScale.x) * -1f, gameObject.transform.localScale.y);
            while (!FastApproximately(transform.position.x, pos1.x, te) && !FastApproximately(transform.position.y, pos1.y, te))
            {
                transform.position = Vector2.Lerp(transform.position, pos1, te * Time.deltaTime);
                yield return new WaitForEndOfFrame();
            }
            _anim.Play("summon");
            float ti = 2f;
            if (!ship)
            {
                ship = SpawnShip();
            }
            else
            {
                ti = 0.5f;
                ship.GetComponent<ShipControl>().sendLargeBalls = true;
            }
            FaceHero();
            yield return new WaitForSeconds(ti);
            float offset = 1.5f;
            int count = 0;
            _anim.Play("fly");
            while (count++ < 2)
            {
                float total = 0f;
                FaceHero();
                yield return new WaitForSeconds(0.5f);
                _aud.clip = ArenaFinder.audioClips["spin"];
                _aud.Play();
                Vector2 currPos = transform.position;
                while (transform.position.x < pos3.x - 1.5f || transform.position.y < pos3.y - 1.5f)
                {
                    Vector2 pos = Bezier3(currPos, pos2, pos2_5, pos3, total);
                    transform.position = pos;
                    yield return new WaitForEndOfFrame();
                    total += Time.deltaTime / offset;
                }

                total = 0f;
                FaceHero();
                yield return new WaitForSeconds(0.5f);
                _aud.clip = ArenaFinder.audioClips["spin"];
                _aud.Play();
                currPos = transform.position;
                while (transform.position.x > pos1.x + 1.5f || transform.position.y < pos1.y - 1.5f)
                {
                    Vector2 pos = Bezier3(currPos, pos2_5, pos2, pos1, total);
                    transform.position = pos;
                    yield return new WaitForEndOfFrame();
                    total += Time.deltaTime / offset;
                }
            }
            _rb.gravityScale = 0.5f;
            FaceHero();
            yield return new WaitWhile(() => !grounded);
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f, 0f);
            _anim.Play("laugh");
            yield return new WaitForSeconds(1f);
            _anim.Play("flair");
            yield return new WaitWhile(() => _anim.IsPlaying());
            //ship.GetComponent<ShipControl>().idleTime = 3.5f;
            ship.GetComponent<ShipControl>().sendLargeBalls = false;
            transition = true;
        }

        void Update()
        {
            Vector2 posH = _target.transform.position;
            Vector2 posP = gameObject.transform.position;
            Vector2 divisor = new Vector2(2f, 1.15f);
            _bc.offset = new Vector2(0.1f, 0.25f);
            _bc.size = new Vector3(Mathf.Abs(_sr.bounds.size.x / transform.lossyScale.x / divisor.x),
                                         Mathf.Abs(_sr.bounds.size.y / transform.lossyScale.y / divisor.y),
                                         Mathf.Abs(_sr.bounds.size.z / transform.lossyScale.z));
            /*if (posP.y < GROUND_Y)
            {
                grounded = true;
            }
            else
            {
                grounded = false;
            }*/
            if (gameObject.transform.GetPositionY() <= GROUND_Y)
            {
                grounded = true;
            }
            else
            {
                grounded = false;
            }
            if (posH.y > posP.y && FastApproximately(posH.x, posP.x, _sr.size.x * 4f)) aboveHead = true;
            else aboveHead = false;

            if (posP.x <= LEFT_X && _rb.velocity.x < 0) _rb.velocity = new Vector2(0f, _rb.velocity.y);
            else if (posP.x >= RIGHT_X && _rb.velocity.x > 0) _rb.velocity = new Vector2(0f, _rb.velocity.y);
        }

        void OnTriggerEnter2D(Collider2D c)
        {
            if (c.gameObject.layer == 8)
            {
                grounded = true;
                _rb.gravityScale = 0f;
                _rb.velocity = new Vector2(_rb.velocity.x, 0f);
                gameObject.transform.position = new Vector2(gameObject.transform.GetPositionX(), GROUND_Y);
            }
        }
        void OnTriggerExit2D(Collider2D c)
        {
            if (c.gameObject.layer == 8)
            {
                grounded = false;
            }
        }


        IEnumerator EndingTextFade()
        {
            CanvasUtil.CreateFonts();
            canvas = CanvasUtil.CreateCanvas(RenderMode.ScreenSpaceOverlay, new Vector2(1920f, 1080f));//1536f, 864f));
            title = CanvasUtil.CreateTextPanel(canvas, "Propeller Knight by Yacht Club Games", 40, TextAnchor.MiddleCenter, new CanvasUtil.RectData(new Vector2(1000, 1500), new Vector2(0f, 65f), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f)), false).GetComponent<Text>();
            title.color = new Color(1f, 1f, 1f, 0f);
            title.font = CanvasUtil.TrajanBold;//CanvasUtil.GetFont("Perpetua");
            for (float i = 0f; i <= 1f; i += 0.05f)
            {
                title.color = new Color(1f, 1f, 1f, i);
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(1.7f);
            for (float i = 1; i >= 0f; i -= 0.05f)
            {
                title.color = new Color(1f, 1f, 1f, i);
                yield return new WaitForEndOfFrame();
            }
            title.text = "";
        }

        private GameObject SpawnShip()
        {
            GameObject ship = Instantiate(PropellerKnight.preloadedGO["ship"]);
            ship.SetActive(true);
            ship.transform.localScale /= 1.3f;
            ship.transform.position = new Vector3(80f, 17f, ship.transform.position.z + 5f);
            ship.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            ship.AddComponent<ShipControl>();
            return ship;
        }

        Vector2 Bezier3(Vector2 s, Vector2 st, Vector2 et, Vector2 e, float t)
        {
            return (((-s + 3 * (st - et) + e) * t + (3 * (s + et) - 6 * st)) * t + 3 * (st - s)) * t + s;
        }

        private float FaceHero()
        {
            var heroSignX = Mathf.Sign(_target.transform.GetPositionX() - gameObject.transform.GetPositionX());
            var pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector2(Mathf.Abs(pScale.x) * heroSignX, pScale.y);
            return heroSignX;
        }

        IEnumerator MusicControl()
        {
            yield return null;
            musicControl = new GameObject("music");
            musicControl.transform.SetPosition2D(HeroController.instance.transform.position);
            musicControl.SetActive(true);
            AudioSource comp = musicControl.AddComponent<AudioSource>();
            SetSoundSettings(comp);
            comp.loop = true;
            comp.clip = ArenaFinder.audioClips["music"];
            comp.Play();
            StartCoroutine(MusPauseHand(comp));
            StartCoroutine(MusicVol(comp));
        }

        public static void SetSoundSettings(AudioSource aud)
        {
            aud.enabled = true;
            aud.volume = 1f;
            aud.bypassEffects = true;
            aud.bypassReverbZones = true;
            aud.bypassListenerEffects = true;
        }

        IEnumerator MusicVol(AudioSource one)
        {
            while (true)
            {
                if (!soundOverride) one.volume = GameManager.instance.gameSettings.musicVolume / 10f;
                yield return new WaitForEndOfFrame();
            }
        }

        IEnumerator MusPauseHand(AudioSource one)
        {
            while (true)
            {
                if (heroDmg && !GameManager.instance.isPaused)
                {
                    soundOverride = true;
                    heroDmg = false;
                    for (float i = 1f; i > 0.2f; i -= 0.1f)
                    {
                        one.volume = i;
                        yield return new WaitForEndOfFrame();
                    }
                    yield return new WaitForSeconds(1.5f);
                    for (float i = 0.2f; i <= 1f; i += 0.1f)
                    {
                        one.volume = i;
                        yield return new WaitForEndOfFrame();
                    }
                    soundOverride = false;
                    yield return new WaitForSeconds(0.5f);
                }
                if (GameManager.instance.isPaused)
                {
                    soundOverride = true;
                    one.volume = 0.2f;
                    yield return new WaitWhile(() => GameManager.instance.isPaused);
                    one.volume = 1f;
                    soundOverride = false;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        GameObject SpawnPlatform(int type, float x, float y, float z, float rotation = 0f)
        {
            string transition = "PLAT EXPAND";
            GameObject plat = Instantiate(PropellerKnight.preloadedGO["plat"]); ;
            if (type == 1)
            {
                plat = Instantiate(PropellerKnight.preloadedGO["spike"]);
                transition = "EXPAND";
            }
            plat.SetActive(true);
            plat.transform.SetPosition3D(x, y, z);
            plat.transform.SetRotation2D(rotation);
            if (type == 1)
            {
                plat.LocateMyFSM("Control").GetAction<Wait>("Antic", 2).time = 0.8f;
                plat.AddComponent<SpikeCollider>();
            }
            plat.LocateMyFSM("Control").SetState("Init");
            plat.LocateMyFSM("Control").SendEvent(transition);
            if (type == 0)
            {
                plat.GetComponent<BoxCollider2D>().size = new Vector2(3.4f, 1f);
                plat.GetComponent<BoxCollider2D>().offset = new Vector2(0f, 0.7f);
            }
            return plat;
        }

        IEnumerator FlashWhite()
        {
            _sr.material.SetFloat("_FlashAmount", 1f);
            yield return null;
            for (float i = 1f; i >= 0f; i -= 0.05f)
            {
                _sr.material.SetFloat("_FlashAmount", i);
                yield return null;
            }
            yield return null;
            flashing = false;
        }

        private void OnDestroy()
        {
            ModHooks.Instance.TakeHealthHook -= Instance_TakeHealthHook;
            On.HealthManager.TakeDamage -= HealthManager_TakeDamage;
            _hm.OnDeath -= _hm_OnDeath;
        }

        public static bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }

        private static void Log(object obj)
        {
            Logger.Log("[Propeller Knight] " + obj);
        }
    }
}
