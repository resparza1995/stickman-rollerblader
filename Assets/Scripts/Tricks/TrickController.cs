using System;
using System.Collections.Generic;
using UnityEngine;

namespace TricksSystem
{
    public class TrickController : MonoBehaviour
    {
        [Header("Available Tricks")]
        public List<TrickData> availableTricks = new List<TrickData>();

        public event Action<TrickData> OnTrickExecuted;

        public bool TryExecuteTrick(string inputName, TrickType currentContext)
        {
            foreach (var trick in availableTricks)
            {
                if (trick != null && trick.trickType == currentContext && trick.inputBinding.Equals(inputName, StringComparison.OrdinalIgnoreCase))
                {
                    ExecuteTrick(trick);
                    return true;
                }
            }
            return false;
        }

        private void ExecuteTrick(TrickData trick)
        {
            Debug.Log($"Executed Trick: {trick.trickName} (+{trick.scorePoints} pts)");
            OnTrickExecuted?.Invoke(trick);
        }
    }
}
