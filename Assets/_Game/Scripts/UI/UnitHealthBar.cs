using UnityEngine;
using UnityEngine.UI;
using TacticsGame.Core;

namespace TacticsGame.UI
{
    /// <summary>
    /// World-space health bar that follows a unit.
    /// </summary>
    public class UnitHealthBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private float heightOffset = 2.2f;

        private UnitBase owner;
        private Camera mainCamera;
        private Canvas canvas;

        public void Initialize(UnitBase unit)
        {
            owner = unit;
            mainCamera = Camera.main;
            canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.worldCamera = mainCamera;
            }
        }

        private void LateUpdate()
        {
            if (owner == null || owner.IsDead)
            {
                gameObject.SetActive(false);
                return;
            }

            // Follow unit position
            transform.position = owner.transform.position + Vector3.up * heightOffset;

            // Face camera
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }

            // Update fill
            if (fillImage != null)
            {
                float ratio = owner.currentHealth / owner.stats.maxHealth;
                fillImage.fillAmount = ratio;
                fillImage.color = Color.Lerp(Color.red, Color.green, ratio);
            }
        }
    }
}
