using UnityEngine;

namespace PropellerKnight
{
    internal class SpikeCollider : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D col)
        {
            if (col.name == "HeroBox")
            {
                HeroController.instance.TakeDamage(HeroController.instance.gameObject, GlobalEnums.CollisionSide.other, 1, 0);
            }
        }
    }
}