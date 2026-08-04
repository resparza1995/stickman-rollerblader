using UnityEngine;

namespace TricksSystem
{
    public enum TrickType
    {
        Air,
        Grind
    }

    [CreateAssetMenu(fileName = "NewTrickData", menuName = "Stickman Rollerblader/Trick Data")]
    public class TrickData : ScriptableObject
    {
        [Header("General Settings")]
        public string trickName = "Standard Trick";
        public int scorePoints = 100;
        public TrickType trickType = TrickType.Air;
        public string animationTrigger = "Trick";

        [Header("Input Setup")]
        public string inputBinding = "Fire1";
    }
}
