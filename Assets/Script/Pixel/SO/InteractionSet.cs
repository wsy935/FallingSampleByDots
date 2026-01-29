using UnityEngine;

namespace Pixel
{
    [CreateAssetMenu(fileName = "InteractionSet", menuName = "SO/InteractionSet")]
    public class InteractionSet : ScriptableObject
    {
        public InteractionRule[] configs;
    }
}