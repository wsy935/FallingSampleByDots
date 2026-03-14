using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "PixelSet", menuName = "SO/PixelSet")]
    public class PixelSet : ScriptableObject
    {
        public PixelConfigSO[] configs;
    }

    [Serializable]
    public class PixelConfigSO
    {
        public PixelConfig config;
        public List<ReactionRule> reactionRules;
    }
}
