using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;

public class GestureInput : MonoBehaviour
{
    private GameObject selectedObject;
    private Quaternion lastInputDeviceRotation; // Only needed for Rotation tracking

    [SerializeField]
    private float rotationSpeed = 5f;
    
    // Note: The two-finger logic is retained but placed in the 'else if' block
    private float lastTwoFingerAngle; 
    
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
                    lastInputDeviceRotation = touchData.inputDeviceRotation; 
                }
                else if (touch.phase == TouchPhase.Moved && selectedObject != null)
                {
                    // --- 1. INDIRECT PINCH (Translation/Movement) ---
                    if (touchData.Kind == SpatialPointerKind.IndirectPinch)
                    {
                        // Use the built-in delta for reliable movement tracking
                        UnityEngine.Vector3 deltaPosition = touchData.deltaInteractionPosition;

                        if (!selectedObject.CompareTag(ROTATION_ONLY_TAG) && !DataManager.Instance.IsPositionLocked)
                        {
                            selectedObject.transform.position += deltaPosition;
                            Debug.Log($"[GestureInput] Object moved (Indirect): {selectedObject.name}, Delta: {deltaPosition}");
                        }
                    }
                    
                    // --- 2. DIRECT PINCH (Rotation) ---
                    else if (touchData.Kind == SpatialPointerKind.DirectPinch)
                    {
                        // Calculate the rotation change from the input device's orientation (wrist roll, pitch, yaw)
                        Quaternion deltaRotation = Quaternion.Inverse(lastInputDeviceRotation) * touchData.inputDeviceRotation;
                        
                        // Apply the full 3D rotation change
                        selectedObject.transform.localRotation *= deltaRotation;
                        
                        Debug.Log($"[GestureInput] Object rotated (Direct): {selectedObject.name}");

                        // Update the last rotation for the next frame's delta calculation
                        lastInputDeviceRotation = touchData.inputDeviceRotation;
                    }
                }
            }
        }
        
        // --- 3. TWO-FINGER INPUT (Scaling or dedicated rotation/translation) ---
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
                    // This uses screen space rotation (e.g., rotating fingers on the surface)
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