// ISpatialTouchable.cs
// mark 6 november
// interface for spatial touchable objects

using UnityEngine;

public interface ISpatialTouchable
{
    // call on TouchInput.cs
    void OnSpatialTouch(Vector3 touchPosition, Vector3 touchNormal);
}