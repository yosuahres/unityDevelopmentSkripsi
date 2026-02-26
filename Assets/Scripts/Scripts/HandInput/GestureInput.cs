// GestureInput.cs
using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;
using Assets.Scripts.Scripts;

public class GestureInput : MonoBehaviour
{
    private GameObject selectedObject;
    private Quaternion lastInputDeviceRotation;
    private float lastTwoFingerAngle;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 0.005f;

    private const string ROTATION_ONLY_TAG = "ROTATEONLY";
    private const string SPAWNABLE_TAG = "SPAWNABLE";
    private const string PLANE_TAG = "PLANE";
    private const string CYLINDER_TAG = "CYLINDER";

    private OsteotomyPlanLogic _planLogic;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        _planLogic = Object.FindFirstObjectByType<OsteotomyPlanLogic>();
    }

    void Update()
    {
        int touchCount = Touch.activeTouches.Count;

        if (touchCount == 1)
        {
            HandleSingleTouch(Touch.activeTouches[0]);
        }
        else if (touchCount == 2)
        {
            HandleTwoFingerRotation(Touch.activeTouches[0], Touch.activeTouches[1]);
        }
        else
        {
            selectedObject = null;
        }

        if (touchCount > 0 && selectedObject != null)
        {
            SyncFibulaSystems();
        }
    }

    private void HandleSingleTouch(Touch touch)
    {
        SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);

        if (touchData.targetObject == null || touchData.Kind == SpatialPointerKind.Touch) return;

        if (touch.phase == TouchPhase.Began)
        {
            if (touchData.targetObject.CompareTag(CYLINDER_TAG))
            {
                selectedObject = null;
                return;
            }

            selectedObject = touchData.targetObject;
            lastInputDeviceRotation = touchData.inputDeviceRotation;
        }
        else if (touch.phase == TouchPhase.Moved && selectedObject != null)
        {
            if (touchData.Kind == SpatialPointerKind.IndirectPinch)
            {
                ApplyTranslation(touchData);
            }
            else if (touchData.Kind == SpatialPointerKind.DirectPinch)
            {
                ApplyDirectRotation(touchData);
            }
        }
    }

    private void ApplyTranslation(SpatialPointerState touchData)
    {
        Vector3 deltaPosition = touchData.deltaInteractionPosition;
        bool isPositionLocked = selectedObject.CompareTag(SPAWNABLE_TAG) && DataManager.Instance.IsPositionLocked;

        // Don't move if rotation-only or if the user locked the fragment position
        if (!selectedObject.CompareTag(ROTATION_ONLY_TAG) && !isPositionLocked)
        {
            selectedObject.transform.position += deltaPosition;
        }
    }

    private void ApplyDirectRotation(SpatialPointerState touchData)
    {
        Quaternion deltaRotation = Quaternion.Inverse(lastInputDeviceRotation) * touchData.inputDeviceRotation;
        selectedObject.transform.localRotation *= deltaRotation;
        lastInputDeviceRotation = touchData.inputDeviceRotation;
    }

    private void HandleTwoFingerRotation(Touch t1, Touch t2)
    {
        SpatialPointerState touchData1 = EnhancedSpatialPointerSupport.GetPointerState(t1);
        if (touchData1.targetObject == null || touchData1.targetObject.CompareTag(CYLINDER_TAG)) return;

        Vector2 currentVector = t2.screenPosition - t1.screenPosition;
        float currentAngle = Mathf.Atan2(currentVector.y, currentVector.x) * Mathf.Rad2Deg;

        if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
        {
            selectedObject = touchData1.targetObject;
            lastTwoFingerAngle = currentAngle;
        }
        else if ((t1.phase == TouchPhase.Moved || t2.phase == TouchPhase.Moved) && selectedObject != null)
        {
            float deltaAngle = Mathf.DeltaAngle(lastTwoFingerAngle, currentAngle);
            
            Vector3 rotationCenter = GetObjectCenter(selectedObject);
            selectedObject.transform.RotateAround(rotationCenter, Vector3.up, -deltaAngle * rotationSpeed);
            
            lastTwoFingerAngle = currentAngle;
        }
    }

    private Vector3 GetObjectCenter(GameObject obj)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        return (rend != null) ? rend.bounds.center : obj.transform.position;
    }

    private void SyncFibulaSystems()
    {
        if (selectedObject.CompareTag(SPAWNABLE_TAG) || selectedObject.CompareTag(PLANE_TAG))
        {
            if (_planLogic != null)
            {
                _planLogic.SyncAllPlanesToFibula();
            }
        }
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }
}