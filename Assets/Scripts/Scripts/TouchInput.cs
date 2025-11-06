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

public class TouchInput : MonoBehaviour {
    // assign this in the inspector
    public GameObject planeFragmentPrefab;
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
                                Instantiate(planeFragmentPrefab, spawnPosition, spawnRotation);
                                Debug.Log($"Spawned plane at {spawnPosition}");
                            }

                            // notify the touched component if it needs the event for other purposes
                            touchable.OnSpatialTouch(spawnPosition, touchNormal);
                        }
                        break;
                    }
                }
            }
        }
    }
}