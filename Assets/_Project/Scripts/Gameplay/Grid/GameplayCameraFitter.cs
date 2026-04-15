using UnityEngine;

public sealed class GameplayCameraFitter : MonoBehaviour
{
    [SerializeField, Tooltip("Camera to reposition.")]
    private Camera targetCamera;

    [SerializeField, Range(0.4f, 0.9f), Tooltip("Fraction of screen width the grid should occupy. Lower = more margin.")]
    private float boardWidthFraction = 0.70f;

    [SerializeField, Range(0f, 0.3f), Tooltip("Fraction of screen height reserved for HUD at the top.")]
    private float hudScreenFraction = 0.12f;

    public Camera TargetCamera => targetCamera;

    public void Fit(GridSize gridSize, CellSpace cellSpace)
    {
        if (targetCamera == null || cellSpace == null) return;

        float cs = cellSpace.CellSize;
        float aspect = Mathf.Max(0.0001f, targetCamera.aspect);

        float gridWorldWidth  = gridSize.width  * cs;
        float gridWorldHeight = gridSize.height * cs;

        float centerX = (gridSize.width  - 1) * 0.5f * cs;
        float centerZ = (gridSize.height - 1) * 0.5f * cs;

        if (targetCamera.orthographic)
        {
            float orthoByWidth = gridWorldWidth / (boardWidthFraction * 2f * aspect);
            float orthoByHeight = gridWorldHeight / (boardWidthFraction * 2f);
            float orthoSize = Mathf.Max(orthoByWidth, orthoByHeight);
            targetCamera.orthographicSize = orthoSize;

            // Shift down so the grid centers in the playable area below the HUD.
            float hudOffset = orthoSize * hudScreenFraction;
            var center = new Vector3(centerX, 0f, centerZ - hudOffset);
            targetCamera.transform.position = center + Vector3.up * 20f;
            targetCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            // Perspective, looking straight down:
            //   visible width at distance d = 2 * d * tan(fov/2) * aspect
            //   gridWorldWidth = boardWidthFraction * visibleWidth
            //   d = gridWorldWidth / (boardWidthFraction * 2 * tan(fov/2) * aspect)
            float fovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
            float halfTan = Mathf.Tan(fovRad * 0.5f);

            float heightByWidth  = gridWorldWidth  / (boardWidthFraction * 2f * halfTan * aspect);
            float heightByHeight = gridWorldHeight / (boardWidthFraction * 2f * halfTan);
            float camHeight = Mathf.Max(heightByWidth, heightByHeight);

            float visibleHeight = 2f * camHeight * halfTan;
            float hudOffset = visibleHeight * hudScreenFraction * 0.5f;

            var center = new Vector3(centerX, 0f, centerZ - hudOffset);
            targetCamera.transform.position = center + Vector3.up * camHeight;
            targetCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
