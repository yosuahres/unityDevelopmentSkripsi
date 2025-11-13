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
                    SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);
                    
                    if (touchData.targetObject != null && touchData.Kind == SpatialPointerKind.Touch)
                    {
                        ISpatialTouchable touchable = touchData.targetObject.GetComponent<ISpatialTouchable>();
                        if (touchable != null)
                        {
                            Vector3 spawnPosition = touchData.interactionPosition;
                            Vector3 touchNormal = touchData.inputDeviceRotation * UnityEngine.Vector3.forward;
                            Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, touchNormal);

                            if (planeFragmentPrefab != null)
                            {
                                GameObject spawnedPlane = Instantiate(planeFragmentPrefab, spawnPosition, spawnRotation);
                                
                                currentCuttingPlanes.Add(spawnedPlane);

                                Debug.Log($"Spawned new plane ({currentCuttingPlanes.Count} total) at {spawnPosition}");
                            }

                            touchable.OnSpatialTouch(spawnPosition, touchNormal);
                        }
                        break;
                    }
                }
            }
        }
    }
}