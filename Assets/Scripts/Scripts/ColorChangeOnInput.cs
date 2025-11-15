//TESITNG PROGRAM
using UnityEngine;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;    
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;  
using UnityEngine.InputSystem.LowLevel;

public class ColorChangeOnInput : MonoBehaviour 
{
    void OnEnable() {
        EnhancedTouchSupport.Enable();
    }

    void Update() {
        if (Touch.activeTouches.Count > 0) {
            foreach (var touch in Touch.activeTouches) {
                if (touch.phase == TouchPhase.Began) 
                {
                    SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);
                    if (touchData.targetObject != null && touchData.Kind != SpatialPointerKind.Touch) {
                        ChangeObjectColor(touchData.targetObject);
                        break;
                    }
                }
            }
        }   
    }

    void ChangeObjectColor(GameObject obj) 
    {
        Renderer objRenderer = obj.GetComponent<Renderer>();
        if (objRenderer != null) 
        {
            objRenderer.material.color = new Color(Random.value, Random.value, Random.value);
        }
    }
}
