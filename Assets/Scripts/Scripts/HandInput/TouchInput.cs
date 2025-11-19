//touchInput.cs
//mark 6 november (Corrected)
// handle the fragment spawn on target touch object

using UnityEngine;
using Unity.PolySpatial;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;
using System.Collections.Generic; 

public class TouchInput : MonoBehaviour {
    public GameObject planeFragmentPrefab;

    public static List<GameObject> currentCuttingPlanes { get; private set; } = new List<GameObject>();

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    public static void ClearPlaneList()
    {
        currentCuttingPlanes.Clear();
    }

    void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    HandleTouchBegan(touch);
                }
            }
        }
    }

    private void HandleTouchBegan(Touch touch)
    {
        SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);

        if (touchData.targetObject != null && 
            (touchData.Kind == SpatialPointerKind.Touch || touchData.Kind == SpatialPointerKind.IndirectPinch))
        {
            ISpatialTouchable touchable = touchData.targetObject.GetComponent<ISpatialTouchable>();

            if (touchable != null)
            {
                Vector3 spawnPosition = touchData.interactionPosition;
                Vector3 touchNormal = touchData.inputDeviceRotation * UnityEngine.Vector3.forward;
                Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, touchNormal);

                SpawnPlaneFragment(spawnPosition, spawnRotation);

                touchable.OnSpatialTouch(spawnPosition, touchNormal);
            }
            else
            {
                Debug.LogWarning($"TouchInput: Touched object '{touchData.targetObject.name}' does not have an ISpatialTouchable component.");
            }
        }
        else if (touchData.targetObject == null)
        {
            Debug.LogWarning("TouchInput: No target object found for touch. Raycast might not be hitting a collider.");
        }
        else
        {
            Debug.LogWarning($"TouchInput: Touch kind '{touchData.Kind}' is not handled for target object '{touchData.targetObject.name}'.");
        }
    }

    private void SpawnPlaneFragment(Vector3 position, Quaternion rotation)
    {
        if (planeFragmentPrefab != null)
        {
            GameObject spawnedPlane = Instantiate(planeFragmentPrefab, position, rotation);
            currentCuttingPlanes.Add(spawnedPlane);
            Debug.Log($"TouchInput: Spawned new plane ({currentCuttingPlanes.Count} total) at {position}");
        }
        else
        {
            Debug.LogError("TouchInput: planeFragmentPrefab is not assigned. Cannot spawn plane.");
        }
    }
}
