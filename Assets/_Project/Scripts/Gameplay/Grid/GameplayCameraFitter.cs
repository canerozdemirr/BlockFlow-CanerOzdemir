using UnityEngine;

/// <summary>
/// Positions and sizes the gameplay camera so the entire grid plus a padding
/// margin fits the screen, regardless of aspect ratio. Works for both
/// orthographic and (loosely) perspective cameras; orthographic is the
/// intended mode for this project.
///
/// Call <see cref="Fit"/> once after the <see cref="GridModel"/> is built.
/// Re-call it on device rotation or aspect changes if those ever become
/// supported.
/// </summary>
public sealed class GameplayCameraFitter : MonoBehaviour
{
    [SerializeField, Tooltip("Camera to reposition. Usually the main gameplay camera.")]
    private Camera targetCamera;

    [SerializeField, Min(0f), Tooltip("Extra cells of empty space kept around the grid.")]
    private float padding = 0.75f;

    [SerializeField, Tooltip("World-space height the camera sits at while looking straight down.")]
    private float cameraHeight = 12f;

    public Camera TargetCamera => targetCamera;

    public void Fit(GridSize gridSize, CellSpace cellSpace)
    {
        if (targetCamera == null || cellSpace == null) return;

        float cs = cellSpace.CellSize;

        // Frame the grid centered at its bounding-box midpoint.
        float centerX = (gridSize.width  - 1) * 0.5f * cs;
        float centerZ = (gridSize.height - 1) * 0.5f * cs;
        var center = new Vector3(centerX, 0f, centerZ);

        var camTransform = targetCamera.transform;
        camTransform.position = center + new Vector3(0f, cameraHeight, 0f);
        camTransform.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (targetCamera.orthographic)
        {
            float aspect = Mathf.Max(0.0001f, targetCamera.aspect);
            float halfHeightByRows = (gridSize.height + padding * 2f) * cs * 0.5f;
            float halfHeightByCols = (gridSize.width  + padding * 2f) * cs * 0.5f / aspect;
            targetCamera.orthographicSize = Mathf.Max(halfHeightByRows, halfHeightByCols);
        }
    }
}
