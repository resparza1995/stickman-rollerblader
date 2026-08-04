using UnityEngine;

namespace TricksSystem
{
    [CreateAssetMenu(fileName = "NewGrindTrick", menuName = "Stickman Rollerblader/Grind Trick")]
    public class GrindTrickData : TrickData
    {
        [Header("Grind Trick Specifics")]
        public float balanceMultiplier = 1.0f;
        public string stanceName = "Soul Grind";

        private void OnEnable()
        {
            trickType = TrickType.Grind;
        }
    }
}
