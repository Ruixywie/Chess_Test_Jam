using UnityEngine;
using TacticsGame.Core;

namespace TacticsGame.Player
{
    public class PlayerUnit : UnitBase
    {
        protected override void Awake()
        {
            base.Awake();
            isPlayerUnit = true;
        }

        public override void OnTurnStart()
        {
            ResetAP();
            Debug.Log($"[Player] Turn started. AP: {currentAP}");
        }

        public override void OnTurnEnd()
        {
            Debug.Log("[Player] Turn ended.");
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            Debug.Log("[Player] Player has died!");
        }
    }
}
