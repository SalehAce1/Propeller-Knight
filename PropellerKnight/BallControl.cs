using HutongGames.PlayMaker.Actions;
using Satchel;
using System.Collections;
using UnityEngine;

namespace PropellerKnight
{
    public class BallControl : MonoBehaviour
    {
        bool start;
        public bool faceRight;
        AudioSource _aud;
        IEnumerator Start()
        {
            Destroy(GetComponent<CircleCollider2D>());
            gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            gameObject.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            gameObject.AddComponent<DamageHero>();
            gameObject.layer = 17;
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
                PlayMakerFSM lord = PropellerKnight.preloadedGO["wave"].LocateMyFSM("Mage Lord");
                for (int i = -1; i < 2; i += 2)
                {
                    GameObject go = Instantiate(lord.GetAction<SpawnObjectFromGlobalPool>("Quake Waves", 0).gameObject.Value);
                    go.transform.localScale = new Vector2(0.6f, 1f);
                    PlayMakerFSM shock = go.LocateMyFSM("shockwave");
                    shock.FsmVariables.FindFsmBool("Facing Right").Value = faceRight;
                    shock.FsmVariables.FindFsmFloat("Speed").Value = 22f;
                    go.SetActive(true);
                    go.transform.SetPosition2D(gameObject.transform.position);
                }
            }
        }
    }
}