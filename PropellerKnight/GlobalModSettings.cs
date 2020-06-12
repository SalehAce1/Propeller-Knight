using System;
using Modding;
using UnityEngine;

namespace PropellerKnight
{
    [Serializable]
    public class GlobalModSettings : ModSettings
    {
        public BossStatue.Completion CompletionPropeller = new BossStatue.Completion
        {
            isUnlocked = true,
            hasBeenSeen = true
        };
        
        public void OnBeforeSerialize()
        {
            StringValues["CompletionPropeller"] = JsonUtility.ToJson(CompletionPropeller);
        }

        public void OnAfterDeserialize()
        {
            StringValues.TryGetValue("CompletionPropeller", out string @out2);
            if (string.IsNullOrEmpty(@out2)) return;
            CompletionPropeller = JsonUtility.FromJson<BossStatue.Completion>(@out2);
        }
    }
}