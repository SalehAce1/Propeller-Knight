using System.Collections;
using UnityEngine;

namespace PropellerKnight
{
    public class BombControl : MonoBehaviour
    {
        bool start;
        Animator _anim;
        AudioSource _aud;
        public static bool bombExist;
        IEnumerator Start()
        {
            _anim = GetComponent<Animator>();
            _aud = gameObject.AddComponent<AudioSource>();
            PropFight.SetSoundSettings(_aud);
            bombExist = true;
            yield return new WaitForSeconds(0.5f);
            gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
            gameObject.AddComponent<DamageHero>().damageDealt = 1;
            gameObject.layer = 17;
            start = true;
        }

        void OnTriggerEnter2D(Collider2D c)
        {
            if (start && c.gameObject.layer == 8)
            {
                _aud.clip = ArenaFinder.audioClips["canon"];
                _aud.Play();
                GetComponent<Rigidbody2D>().gravityScale = 0f;
                GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
                GetComponent<Animator>().enabled = true;
                StartCoroutine(DestroyLater());
            }
        }

        IEnumerator DestroyLater()
        {
            yield return new WaitWhile(() => _anim.IsPlaying("bombExpl"));
            transform.localScale *= 1.4f;
            yield return new WaitWhile(() => _anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1);
            bombExist = false;
            Destroy(gameObject);
        }
    }
}