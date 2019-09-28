using System.Collections;
using UnityEngine;

namespace PropellerKnight
{
    public class BombControl : MonoBehaviour
    {
        bool start;
        Animator _anim;
        AudioSource _aud;
        IEnumerator Start()
        {
            _anim = GetComponent<Animator>();
            _aud = gameObject.AddComponent<AudioSource>();
            PropFight.SetSoundSettings(_aud);
            yield return new WaitForSeconds(0.5f);
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
            gameObject.AddComponent<BoxCollider2D>();
            gameObject.AddComponent<DamageHero>().damageDealt = 1;
            gameObject.layer = 17;
            yield return new WaitWhile(() => _anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1);
            Destroy(gameObject);
        }
    }
}