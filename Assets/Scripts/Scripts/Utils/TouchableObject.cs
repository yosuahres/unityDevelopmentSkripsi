using UnityEngine;

public class TouchableObject : MonoBehaviour, ISpatialTouchable
{
    public void OnSpatialTouch(Vector3 touchPosition, Vector3 touchNormal)
    {
        Debug.Log($"Touched {gameObject.name} at {touchPosition} with normal {touchNormal}");
    }
}
