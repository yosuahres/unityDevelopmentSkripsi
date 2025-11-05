//touchInput.cs
//mark 6 november (Corrected)
// handle the fragment spawn on target touch object

using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;

public class TouchInput : MonoBehaviour {
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
                    
                    if (touchData.targetObject != null && touchData.Kind == SpatialPointerKind.Direct)
                    {
                        ISpatialTouchable touchable = touchData.targetObject.GetComponent<ISpatialTouchable>();
                        if (touchable != null)
                        {
                            touchable.OnSpatialTouch(touchData.interactionPosition, touchData.interactionNormal);
                        }
                        break;
                    }
                }
            }
        }
    }
}