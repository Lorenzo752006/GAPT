using UnityEngine;

public class DungeonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float zOffset = -10f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, zOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Center the camera on the grid.
    /// </summary>
    public void CenterOnGrid()
    {
        if (GridManager.Instance == null) return;
        float cx = (GridManager.Instance.Width * GridManager.Instance.CellSize) / 2f;
        float cy = (GridManager.Instance.Height * GridManager.Instance.CellSize) / 2f;
        transform.position = new Vector3(cx, cy, zOffset);
    }
}