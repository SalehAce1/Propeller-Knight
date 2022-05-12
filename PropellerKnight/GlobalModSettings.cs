using System;
using Modding;
using UnityEngine;

namespace PropellerKnight
{
    [Serializable]
    public class GlobalModSettings
    {
        public BossStatue.Completion CompletionPropeller = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
    }
}