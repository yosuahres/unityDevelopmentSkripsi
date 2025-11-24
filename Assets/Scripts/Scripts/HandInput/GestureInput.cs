using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;

public class GestureInput : MonoBehaviour
{
    private GameObject selectedObject;
    private UnityEngine.Vector3 lastPosition;
    private float lastTwoFingerAngle; 

    [SerializeField]
    private float rotationSpeed = 5f;
    
    private const string ROTATION_ONLY_TAG = "ROTATEONLY";

    void OnEnable() 
    {
        EnhancedTouchSupport.Enable();
    }

    void Update()
    {
        if (Touch.activeTouches.Count == 1)
        {
            var touch = Touch.activeTouches[0];
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
                    if (!selectedObject.CompareTag(ROTATION_ONLY_TAG) && !DataManager.Instance.IsPositionLocked)
                    {
                        UnityEngine.Vector3 deltaPosition = touchData.interactionPosition - lastPosition;
                        selectedObject.transform.position += deltaPosition;
                        Debug.Log($"[GestureInput] Object moved: {selectedObject.name}, New Position: {selectedObject.transform.position}");
                    }
                    else if (DataManager.Instance.IsPositionLocked)
                    {
                        Debug.Log($"[GestureInput] Object movement blocked for {selectedObject.name} because IsPositionLocked is true.");
                    }
                    lastPosition = touchData.interactionPosition;
                }
            }
        }
        
        else if (Touch.activeTouches.Count == 2)
        {
            var touch1 = Touch.activeTouches[0];
            var touch2 = Touch.activeTouches[1];

            SpatialPointerState touchData1 = EnhancedSpatialPointerSupport.GetPointerState(touch1);
            if (touchData1.targetObject != null && touchData1.Kind != SpatialPointerKind.Touch)
            {
                UnityEngine.Vector2 currentVector = touch2.screenPosition - touch1.screenPosition;
                float currentAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;

                if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
                {
                    selectedObject = touchData1.targetObject;
                    lastTwoFingerAngle = currentAngle;
                }
                else if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) && selectedObject != null)
                {
                    float deltaAngle = Mathf.DeltaAngle(lastTwoFingerAngle, currentAngle);
                    selectedObject.transform.Rotate(UnityEngine.Vector3.up, -deltaAngle * rotationSpeed, Space.World);
                    lastTwoFingerAngle = currentAngle;
                }
            }
        }
        else
        {
            selectedObject = null;
        }
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }
}
