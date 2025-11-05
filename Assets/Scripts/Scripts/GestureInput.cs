//handinput.cs
//handle moving the target object by dragging
using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;
// using System.Numerics;

public class GestureInput : MonoBehaviour
{
    private GameObject selectedObject;
    private Vector3 lastPosition;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            foreach (var touch in Touch.activeTouches)
            {
                SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);

                if (touchData.targetObject != null && touchData.Kind != SpatialPointerKind.Touch)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        selectedObject = touchData.targetObject;
                        lastPosition = touchData.interactionPosition;
                    }
                    else if (touch.phase == TouchPhase.Moved && selectedObject != null)
                    {
                        Vector3 deltaPosition = touchData.interactionPosition - lastPosition;
                        selectedObject.transform.position += deltaPosition;
                        lastPosition = touchData.interactionPosition;
                    }
                }
            }
        }
        
        if (Touch.activeTouches.Count == 0)
        {
            selectedObject = null;
        }   
    }
}