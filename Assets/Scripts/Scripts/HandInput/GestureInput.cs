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
    
    // Perhatikan: Konstantra ini harus sama dengan yang didefinisikan di OsteotomyPlanLogic.cs
    private const string ROTATION_ONLY_TAG = "ROTATEONLY"; 
    private const string SPAWNABLE_TAG = "SPAWNABLE"; 

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
                    if (touchData.Kind == SpatialPointerKind.IndirectPinch)
                    {
                        UnityEngine.Vector3 deltaPosition = touchData.deltaInteractionPosition;
                        // Diasumsikan DataManager.Instance ada dan memiliki properti IsPositionLocked
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
                    selectedObject = touchData1.targetObject;
                    lastTwoFingerAngle = currentAngle;
                    lastTwoFingerCenterY = currentCenterY; 
                }
                else if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) && selectedObject != null)
                {
                    // twist motion (Rotasi Y - Yaw)
                    float deltaAngle = Mathf.DeltaAngle(lastTwoFingerAngle, currentAngle);
                    
                    // --- MODIFIKASI TERAKHIR UNTUK MENGHILANGKAN ROTASI ORBITAL ---
                    
                    // 1. Tentukan Pusat Rotasi (Gunakan Bounding Box Center)
                    UnityEngine.Vector3 rotationCenter;
                    Renderer rend = selectedObject.GetComponent<Renderer>();
                    
                    if (rend != null)
                    {
                        rotationCenter = rend.bounds.center;
                    }
                    else
                    {
                        // Fallback ke posisi pivot jika tidak ada Renderer
                        rotationCenter = selectedObject.transform.position;
                    }
                    
                    // 2. Tentukan Sumbu Rotasi (Gunakan Sumbu Y DUNIA/World Up)
                    // Ini memastikan rotasi selalu tegak lurus, terlepas dari orientasi objek (LookAt)
                    UnityEngine.Vector3 rotationAxis = UnityEngine.Vector3.up; 

                    // Melakukan rotasi di tempat (self-rotation)
                    selectedObject.transform.RotateAround(rotationCenter, rotationAxis, -deltaAngle * rotationSpeed);
                    
                    // -------------------------------------------------------------------
                    
                    // Rotasi Vertikal (Rotasi X - Pitch)
                    float deltaCenterY = currentCenterY - lastTwoFingerCenterY;
                    
                    const float verticalSensitivityMultiplier = 3f; 
                    float verticalRotationAmount = deltaCenterY * rotationSpeed * verticalSensitivityMultiplier; 
                    
                    // upward gesture rotates up, downward gesture rotates motion.
                    // Ini tetap menggunakan Space.Self, yang benar untuk Pitch.
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