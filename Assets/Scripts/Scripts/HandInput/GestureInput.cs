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
    private float lastTwoFingerAngle; 
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
                        Quaternion deltaRotation = Quaternion.Inverse(lastInputDeviceRotation) * touchData.inputDeviceRotation;
                        selectedObject.transform.localRotation *= deltaRotation;
                        lastInputDeviceRotation = touchData.inputDeviceRotation;
                    }
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
                    
                    
                    
                    UnityEngine.Vector3 rotationAxis = UnityEngine.Vector3.up; 

                    
                    selectedObject.transform.RotateAround(rotationCenter, rotationAxis, -deltaAngle * rotationSpeed);
                    
                    
                    
                    
                    float deltaCenterY = currentCenterY - lastTwoFingerCenterY;
                    
                    const float verticalSensitivityMultiplier = 3f; 
                    float verticalRotationAmount = deltaCenterY * rotationSpeed * verticalSensitivityMultiplier; 
                    
                    
                    
                    selectedObject.transform.Rotate(UnityEngine.Vector3.right, verticalRotationAmount, Space.Self);
                    
                    lastTwoFingerAngle = currentAngle;
                    lastTwoFingerCenterY = currentCenterY;
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