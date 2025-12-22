using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;

public class GestureInput : MonoBehaviour
{
    private GameObject selectedObject;
    
    private Quaternion lastInputDeviceRotation; 

    [SerializeField]
    private float rotationSpeed = 0.005f;
    
    // This variable will still be updated, but only used for Y-axis rotation (left/right)
    private float lastTwoFingerAngle; 
    
    // This variable is no longer needed for rotation logic, but retained for completeness if required elsewhere.
    private float lastTwoFingerCenterY; 
    
    
    private const string ROTATION_ONLY_TAG = "ROTATEONLY"; 
    private const string SPAWNABLE_TAG = "SPAWNABLE";
    
    private const string CYLINDER_TAG = "CYLINDER"; 

    void OnEnable() 
    {
        EnhancedTouchSupport.Enable();
    }

    void Update()
    {
        // --- 1. SINGLE TOUCH HANDLING (Movement/Direct Rotation) ---
        if (Touch.activeTouches.Count == 1)
        {
            var touch = Touch.activeTouches[0];
            SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);

            if (touchData.targetObject != null && touchData.Kind != SpatialPointerKind.Touch)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    
                    if (touchData.targetObject.CompareTag(CYLINDER_TAG))
                    {
                        Debug.Log($"[GestureInput] Cannot select {CYLINDER_TAG} object for movement/rotation.");
                        selectedObject = null; 
                        return; 
                    }
                    
                    selectedObject = touchData.targetObject;
                    lastInputDeviceRotation = touchData.inputDeviceRotation; 
                }
                else if (touch.phase == TouchPhase.Moved && selectedObject != null)
                {
                    
                    
                    if (selectedObject.CompareTag(CYLINDER_TAG))
                    {
                         return; 
                    }
                    
                    if (touchData.Kind == SpatialPointerKind.IndirectPinch)
                    {
                        // Handle Indirect Pinch (Translation/Movement)
                        UnityEngine.Vector3 deltaPosition = touchData.deltaInteractionPosition;
                        
                        bool isPositionLockedForThisObject = selectedObject.CompareTag(SPAWNABLE_TAG) && DataManager.Instance.IsPositionLocked; 
                        if (!selectedObject.CompareTag(ROTATION_ONLY_TAG) && !isPositionLockedForThisObject)
                        {
                            selectedObject.transform.position += deltaPosition;
                            Debug.Log($"[GestureInput] Object moved (Indirect): {selectedObject.name}, Delta: {deltaPosition}. Position Lock Active: {isPositionLockedForThisObject}");
                        }
                        else if (isPositionLockedForThisObject)
                        {
                            Debug.Log($"[GestureInput] SPAWNABLE object {selectedObject.name} position locked by user.");
                        }
                        
                    }
                    
                    else if (touchData.Kind == SpatialPointerKind.DirectPinch)
                    {
                        // Handle Direct Pinch (Rotation based on device/hand orientation)
                        Quaternion deltaRotation = Quaternion.Inverse(lastInputDeviceRotation) * touchData.inputDeviceRotation;
                        selectedObject.transform.localRotation *= deltaRotation;
                        lastInputDeviceRotation = touchData.inputDeviceRotation;
                    }
                }
            }
        }
        
        // --- 2. TWO TOUCH HANDLING (2D Screen Rotation - Only Yaw/Y-axis) ---
        else if (Touch.activeTouches.Count == 2)
        {
            var touch1 = Touch.activeTouches[0];
            var touch2 = Touch.activeTouches[1];

            SpatialPointerState touchData1 = EnhancedSpatialPointerSupport.GetPointerState(touch1);
            
            if (touchData1.targetObject != null && touchData1.Kind != SpatialPointerKind.Touch)
            {
                UnityEngine.Vector2 currentVector = touch2.screenPosition - touch1.screenPosition;
                float currentAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;
                float currentCenterY = (touch1.screenPosition.y + touch2.screenPosition.y) / 2f;


                if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
                {
                    
                    if (touchData1.targetObject.CompareTag(CYLINDER_TAG))
                    {
                        selectedObject = null; 
                        return; 
                    }
                    
                    selectedObject = touchData1.targetObject;
                    lastTwoFingerAngle = currentAngle;
                    lastTwoFingerCenterY = currentCenterY; 
                }
                else if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) && selectedObject != null)
                {
                    if (selectedObject.CompareTag(CYLINDER_TAG))
                    {
                        return;
                    }
                    
                    
                    float deltaAngle = Mathf.DeltaAngle(lastTwoFingerAngle, currentAngle);
                    
                    
                    
                    // Determine rotation center
                    UnityEngine.Vector3 rotationCenter;
                    Renderer rend = selectedObject.GetComponent<Renderer>();
                    
                    if (rend != null)
                    {
                        rotationCenter = rend.bounds.center;
                    }
                    else
                    {
                        
                        rotationCenter = selectedObject.transform.position;
                    }
                    
                    
                    // Perform rotation around the Y-axis (Up)
                    UnityEngine.Vector3 rotationAxis = UnityEngine.Vector3.up; 

                    
                    selectedObject.transform.RotateAround(rotationCenter, rotationAxis, -deltaAngle * rotationSpeed);
                    
                    
                    // --- REMOVED UP/DOWN ROTATION LOGIC ---
                    // The original code rotated based on deltaCenterY here. This has been removed.
                    
                    
                    lastTwoFingerAngle = currentAngle;
                    lastTwoFingerCenterY = currentCenterY;
                }
            }
        }
        
        // --- 3. NO TOUCH / RELEASED HANDLING ---
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