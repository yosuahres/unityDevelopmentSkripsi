//TESITNG PROGRAM
using UnityEngine;
using Unity.PolySpatial; 
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;    
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;  
using UnityEngine.InputSystem.LowLevel;
using System.Collections.Generic; 

public class ColorChangeOnInput : MonoBehaviour, ISpatialTouchable 
{
    public GameObject planeFragmentPrefab; 

    public static List<GameObject> currentCuttingPlanes { get; private set; } = new List<GameObject>();

    public static void ClearPlaneList()
    {
        currentCuttingPlanes.Clear();
    }

    void OnEnable() {
        EnhancedTouchSupport.Enable();
    }

    void Update() {
        Debug.Log("ColorChangeOnInput: Update running.");
        if (Touch.activeTouches.Count > 0) {
            Debug.Log($"ColorChangeOnInput: Active touches: {Touch.activeTouches.Count}");
            foreach (var touch in Touch.activeTouches) {
                if (touch.phase == TouchPhase.Began) 
                {
                    Debug.Log("ColorChangeOnInput: TouchPhase.Began detected.");
                    SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);
                    
                    Debug.Log($"ColorChangeOnInput: touchData.targetObject is {(touchData.targetObject != null ? touchData.targetObject.name : "null")}, this.gameObject is {this.gameObject.name}, touchData.Kind is {touchData.Kind}.");

                    if (touchData.targetObject != null && touchData.targetObject == this.gameObject && 
                        (touchData.Kind == SpatialPointerKind.Touch || touchData.Kind == SpatialPointerKind.IndirectPinch)) 
                    {
                        Debug.Log($"ColorChangeOnInput: Conditions met for OnSpatialTouch call with Kind: {touchData.Kind}.");
                        OnSpatialTouch(touchData.interactionPosition, touchData.inputDeviceRotation * UnityEngine.Vector3.forward);
                        break; 
                    }
                    else if (touchData.targetObject == null) {
                        Debug.LogWarning("ColorChangeOnInput: touchData.targetObject is null. Raycast might not be hitting a collider.");
                    }
                    else if (touchData.targetObject != this.gameObject) {
                        Debug.LogWarning($"ColorChangeOnInput: Touched object ({touchData.targetObject.name}) is not this GameObject ({this.gameObject.name}).");
                    }
                    else { 
                        Debug.LogWarning($"ColorChangeOnInput: Touch kind is {touchData.Kind}, expected SpatialPointerKind.Touch or IndirectPinch.");
                    }
                }
            }
        }   
    }

    public void OnSpatialTouch(Vector3 touchPosition, Vector3 touchNormal)
    {
        Debug.Log("ColorChangeOnInput: OnSpatialTouch called.");
        if (planeFragmentPrefab != null)
        {
            Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, touchNormal);
            GameObject spawnedPlane = Instantiate(planeFragmentPrefab, touchPosition, spawnRotation);
            
            currentCuttingPlanes.Add(spawnedPlane);

            Debug.Log($"ColorChangeOnInput: Spawned new plane ({currentCuttingPlanes.Count} total) at {touchPosition}");
        }
        else
        {
            Debug.LogWarning("ColorChangeOnInput: planeFragmentPrefab is not assigned, cannot spawn plane.");
        }
    }
}
