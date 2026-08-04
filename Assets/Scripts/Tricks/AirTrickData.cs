using UnityEngine;

namespace TricksSystem
{
    [CreateAssetMenu(fileName = "NewAirTrick", menuName = "Stickman Rollerblader/Air Trick")]
    public class AirTrickData : TrickData
    {
        [Header("Air Trick Specifics")]
        public float requiredAirTime = 0.4f;
        public float spinDegrees = 360f;
        public bool isGrab = false;

        private void OnEnable()
        {
            trickType = TrickType.Air;
        }
    }
}
