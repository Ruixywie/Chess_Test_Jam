using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TacticsGame.Core;

namespace TacticsGame.Movement
{
    public class FreeMovementSystem : MonoBehaviour
    {
        [Header("Path Preview")]
        [SerializeField] private Color pathPreviewColor = new Color(0.2f, 0.8f, 1f, 0.7f);
        [SerializeField] private Color pathInvalidColor = new Color(1f, 0.3f, 0.3f, 0.7f);
        [SerializeField] private float pathWidth = 0.1f;

        [Header("Move Range")]
        [SerializeField] private Color rangeColor = new Color(0.2f, 0.6f, 1f, 0.2f);

        private LineRenderer pathPreviewLine;
        private GameObject rangeIndicator;
        private NavMeshPath previewPath;
        private float previewAPCost;
        private bool isMoving;

        public bool IsMoving => isMoving;
        public float PreviewAPCost => previewAPCost;

        private void Awake()
        {
            // Path preview line
            GameObject lineObj = new GameObject("PathPreview");
            lineObj.transform.SetParent(transform);
            pathPreviewLine = lineObj.AddComponent<LineRenderer>();
            pathPreviewLine.startWidth = pathWidth;
            pathPreviewLine.endWidth = pathWidth;
            pathPreviewLine.material = new Material(Shader.Find("Sprites/Default"));
            pathPreviewLine.positionCount = 0;
            pathPreviewLine.useWorldSpace = true;
            lineObj.SetActive(false);

            previewPath = new NavMeshPath();
        }

        public void ShowMoveRange(UnitBase unit)
        {
            float maxDist = unit.GetMaxMoveDistance();
            if (rangeIndicator == null)
            {
                rangeIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rangeIndicator.name = "MoveRangeIndicator";
                var col = rangeIndicator.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var renderer = rangeIndicator.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = rangeColor;
            }

            rangeIndicator.transform.position = unit.transform.position + Vector3.up * 0.02f;
            rangeIndicator.transform.localScale = new Vector3(maxDist * 2f, 0.01f, maxDist * 2f);
            rangeIndicator.SetActive(true);
        }

        public void HideMoveRange()
        {
            if (rangeIndicator != null)
                rangeIndicator.SetActive(false);
        }

        public bool PreviewPath(UnitBase unit, Vector3 targetPos)
        {
            NavMesh.CalculatePath(unit.transform.position, targetPos, NavMesh.AllAreas, previewPath);

            if (previewPath.status == NavMeshPathStatus.PathInvalid)
            {
                HidePathPreview();
                return false;
            }

            float pathDist = NavMeshUtils.CalculatePathLength(previewPath);
            previewAPCost = unit.GetMovementAPCost(pathDist);
            bool canAfford = unit.HasEnoughAP(previewAPCost);

            // Show path
            pathPreviewLine.gameObject.SetActive(true);
            pathPreviewLine.positionCount = previewPath.corners.Length;
            for (int i = 0; i < previewPath.corners.Length; i++)
            {
                pathPreviewLine.SetPosition(i, previewPath.corners[i] + Vector3.up * 0.15f);
            }

            Color color = canAfford ? pathPreviewColor : pathInvalidColor;
            pathPreviewLine.startColor = color;
            pathPreviewLine.endColor = color;

            return canAfford;
        }

        public void HidePathPreview()
        {
            pathPreviewLine.gameObject.SetActive(false);
            pathPreviewLine.positionCount = 0;
        }

        public IEnumerator MoveUnit(UnitBase unit, Vector3 targetPos)
        {
            NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
            if (agent == null) yield break;

            NavMesh.CalculatePath(unit.transform.position, targetPos, NavMesh.AllAreas, previewPath);
            float pathDist = NavMeshUtils.CalculatePathLength(previewPath);
            float apCost = unit.GetMovementAPCost(pathDist);

            if (!unit.HasEnoughAP(apCost)) yield break;

            isMoving = true;
            HidePathPreview();
            HideMoveRange();

            agent.isStopped = false;
            agent.SetDestination(targetPos);

            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.1f)
            {
                yield return null;
            }

            agent.isStopped = true;
            unit.ConsumeAP(apCost);
            isMoving = false;
        }

        public void Cleanup()
        {
            HidePathPreview();
            HideMoveRange();
        }

        private void OnDestroy()
        {
            if (rangeIndicator != null) Destroy(rangeIndicator);
        }
    }
}
