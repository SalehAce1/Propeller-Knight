using System.Collections;
using System.Linq;
using UnityEngine;
using Logger = Modding.Logger;

namespace PropellerKnight
{
    public class ShipControl : MonoBehaviour
    {
        Rigidbody2D _rb;
        Animator _anim;
        public float idleTime = 2f;
        public bool sendLargeBalls = true;
        // Use this for initialization
        IEnumerator Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            _anim = GetComponent<Animator>();
            _rb.velocity = new Vector2(30f, 0f);
            yield return new WaitWhile(() => transform.position.x < 103f);
            _rb.velocity = new Vector2(0f, 0f);
            _anim.Play("shipAtt");
            yield return new WaitForSeconds(0.1f);
            StartCoroutine(EndingCheck());
            while (true)
            {
                int currentFrame = _anim.GetCurrentFrame();
                if (currentFrame >= 6 && currentFrame <= 9)
                {
                    int ballN = currentFrame - 6;
                    GameObject origBall = transform.Find("ballSmall" + ballN).gameObject;
                    GameObject newBall = Instantiate(origBall);
                    newBall.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                    newBall.transform.parent = transform;
                    newBall.SetActive(true);
                    newBall.transform.localScale = origBall.transform.localScale;
                    newBall.transform.position = origBall.transform.position;
                    newBall.name += "destroy";
                    newBall.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 15f);
                }
                else if (currentFrame >= 11)
                {
                    _anim.Play("shipIdle");
                    yield return new WaitForSeconds(idleTime);
                    foreach (var i in FindObjectsOfType<GameObject>().Where(x => x.name.Contains("destroy"))) Destroy(i);
                    StartCoroutine(SendItem());
                    _anim.Play("shipAtt");
                }

                yield return new WaitWhile(() => _anim.GetCurrentFrame() == currentFrame);
            }
        }

        IEnumerator EndingCheck()
        {
            yield return new WaitWhile(() => !PropDeath.isDying);
            StopAllCoroutines();
            _rb.velocity = new Vector2(30f, 0f);
            yield return new WaitWhile(() => transform.GetPositionX() < 122f);
            Destroy(gameObject);
        }

        IEnumerator SendItem()
        {
            //90 96 102 108 114
            if (sendLargeBalls)
            {
                for (float i = 92.3f; i <= 113; i += 6.8f)
                {
                    var go = Instantiate(PropellerKnight.preloadedGO["ball"]);
                    go.transform.SetPosition2D(i, 23f);
                    go.SetActive(true);
                    go.GetComponent<SpriteRenderer>().enabled = true;
                    go.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                    go.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, -15f);
                    go.GetComponent<Rigidbody2D>().isKinematic = true;
                    if (FastApproximately(92.3f, i, 1f) || FastApproximately(105.9f, i, 1f)) go.AddComponent<BallControl>().faceRight = true;
                    else go.AddComponent<BallControl>().faceRight = false;
                    go.name += "destroy";
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                for (float i = 92.3f; i <= 113; i += 6.8f)
                {
                    var go = Instantiate(PropellerKnight.preloadedGO["bomb"]);
                    go.transform.SetPosition2D(i, 23f);
                    go.SetActive(true);
                    go.GetComponent<SpriteRenderer>().enabled = true;
                    go.GetComponent<SpriteRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                    go.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, -20f);
                    go.GetComponent<Rigidbody2D>().isKinematic = true;
                    go.AddComponent<BombControl>();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        public static bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }

        // Update is called once per frame
        private void Log(object o)
        {
            Logger.Log("[Ship Control] " + o);
        }
    }
}