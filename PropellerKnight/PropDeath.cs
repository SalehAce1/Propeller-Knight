using System.Collections;
using UnityEngine;
using Logger = Modding.Logger;

namespace PropellerKnight
{
    internal class PropDeath : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Animator _anim;
        private Rigidbody2D _rb;
        private HeroController _target;
        public static bool isDying;

        private void Awake()
        {
            _sr = gameObject.GetComponent<SpriteRenderer>();
            _anim = gameObject.GetComponent<Animator>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _target = HeroController.instance;
        }

        private void Start()
        {
            isDying = true;
            _sr.material.SetFloat("_FlashAmount", 0f);
            _rb.velocity = new Vector2(0f, 0f);
            StartCoroutine(DeathSentence());
        }

        IEnumerator DeathSentence()
        {
            FaceHero();
            _anim.Play("deadAir");
            PlayerData.instance.isInvincible = true;
            Time.timeScale = 0.25f;
            _rb.gravityScale = 1f;
            float dir = Mathf.Sign(transform.GetPositionX() - HeroController.instance.transform.GetPositionX());
            _rb.velocity = new Vector2(10f * dir, 15f);
            yield return new WaitForSeconds(0.1f);
            yield return new WaitWhile(() => gameObject.transform.GetPositionY() >= 5.05f);
            Time.timeScale = 1f;
            _rb.gravityScale = 0f;
            _rb.velocity = new Vector2(0f, 0f);
            FaceHero();
            _anim.Play("deadGnd");
            yield return new WaitWhile(() => _anim.IsPlaying());

            GameObject text = Instantiate(GameObject.Find("DialogueManager"));
            text.SetActive(true);
            yield return null;
            var txtfsm = text.LocateMyFSM("Box Open");
            txtfsm.SendEvent("BOX UP");
            GameManager.instance.playerData.SetBool("disablePause", true);
            _target.RelinquishControl();
            _target.StopAnimationControl();
            GameObject sec = text.transform.Find("Text").gameObject;
            sec.GetComponent<DialogueBox>().StartConversation("PROP_END", "testudo");
            yield return new WaitWhile(() => sec.GetComponent<DialogueBox>().currentPage <= 1);
            txtfsm.SendEvent("BOX DOWN");
            text.SetActive(false);
            _target.RegainControl();
            GameManager.instance.playerData.SetBool("disablePause", false);
            _target.StartAnimationControl();
            yield return new WaitForSeconds(0.5f);
            _anim.Play("deadEnd");
            yield return new WaitWhile(() => _anim.IsPlaying());
            yield return new WaitForSeconds(1f);
            var endCtrl = GameObject.Find("Boss Scene Controller").LocateMyFSM("Dream Return");
            endCtrl.SendEvent("DREAM RETURN");
        }

        private void FixedUpdate()
        {
            if (transform.GetPositionX() > 117f || transform.GetPositionX() < 87f) _rb.velocity = new Vector2(0f, _rb.velocity.y);
        }
        private float FaceHero()
        {
            var heroSignX = Mathf.Sign(_target.transform.GetPositionX() - gameObject.transform.GetPositionX());
            var pScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector2(Mathf.Abs(pScale.x) * heroSignX, pScale.y);
            if (heroSignX > 0) _target.FaceLeft();
            else _target.FaceRight();
            return heroSignX;
        }

        private static void Log(object obj)
        {
            Logger.Log("[Death] " + obj);
        }
    }
}

