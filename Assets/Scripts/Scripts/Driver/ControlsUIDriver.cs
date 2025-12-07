// controlsuidriver.cs 
using System;
using System.Collections.Generic;
using AOT;
using PolySpatial.Samples;
using Unity.PolySpatial;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

#if UNITY_VISIONOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Assets.Scripts.Scripts
{
    public class ControlsUIDriver : MonoBehaviour
    {
        bool m_ControlUIWindowOpen = false;

        void OnEnable()
        {
            SetNativeCallback(CallbackFromNative);

            OpenControlsUIWindow("ControlView");
            m_ControlUIWindowOpen = true;
        }

        void OnDisable()
        {
            SetNativeCallback(null);
            CloseControlsUIWindow("ControlView");
        }

        public void ForceCloseWindow()
        {
            CloseControlsUIWindow("ControlView");
            m_ControlUIWindowOpen = false;
        }

        delegate void CallbackDelegate(string command, int value);
        [MonoPInvokeCallback(typeof(CallbackDelegate))]
        static void CallbackFromNative(string command, int value)
        {
            try
            {
                Debug.Log($"Callback from native: {command} {value}");

                var self = FindFirstObjectByType<ControlsUIDriver>();

                // list command from swift sided

                if (command == "TriggerSliceModel")
                {
                    var osteotomyPlanLogic = FindFirstObjectByType<OsteotomyPlanLogic>();
                    osteotomyPlanLogic.PerformOsteotomySlice();
                }
                else if(command == "RevertToUncutModel")
                {
                    var osteotomyPlanLogic = FindFirstObjectByType<OsteotomyPlanLogic>();
                    osteotomyPlanLogic.RevertToUncutModel();
                }
                else

                if (command == "TriggerHomeScene")
                {
                    self.TriggerHomeScene();
                }
                else if (command == "SetLockPosition")
                {
                    DataManager.Instance.IsPositionLocked = (value == 1);
                    Debug.Log($"[ControlsUIDriver] Received command 'SetLockPosition' with value: {value}. IsPositionLocked set to: {DataManager.Instance.IsPositionLocked}");
                }
                else if (command == "SetPlaneScale")
                {
                    float planeScale = value / 100.0f;
                    TouchInput.SetPlaneScale(planeScale);
                    Debug.Log($"[ControlsUIDriver] Received command 'SetPlaneScale' with converted float value: {planeScale}");
                }
                else if (command == "SetPlaneVisibility")
                {
                    bool isVisible = (value == 1);
                    TouchInput.SetPlaneVisibility(isVisible);
                    Debug.Log($"[ControlsUIDriver] Received command 'SetPlaneVisibility' with value: {value}. Planes visibility set to: {isVisible}");
                }
                else if (command == "SetRulerVisibility")
                {
                    bool isVisible = (value == 1);
                    TouchInput.SetRulerVisibility(isVisible);
                    Debug.Log($"[ControlsUIDriver] Received command 'SetRulerVisibility' with value: {value}. Rulers visibility set to: {isVisible}");
                }
                else if (command == "SetGizmoVisibility")
                {
                    bool isVisible = (value == 1);
                    var gizmoVisualizer = FindFirstObjectByType<GizmoVisualizer>();   
                    if (gizmoVisualizer != null)
                    {
                        gizmoVisualizer.SetGizmoVisibility(isVisible);
                    }
                    else
                    {
                        Debug.LogWarning("[ControlsUIDriver] GizmoVisualizer not found in scene.");
                    }
                }
                else if (command == "SetMaxPlane")
                {
                    int maxPlanes = value;
                    TouchInput.maxCuttingPlanes = maxPlanes;
                    Debug.Log($"[ControlsUIDriver] Received command 'SetMaxPlane' with value: {value}. Max cutting planes set to: {maxPlanes}");
                    TouchInput.CheckAndEnforceMaxPlanes();
                }

            }
            catch (Exception e)
            {
                Debug.LogError($"Exception in CallbackFromNative: {e}");
            }
        }

#if UNITY_VISIONOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void SetNativeCallback(CallbackDelegate callback);

        [DllImport("__Internal")]
        static extern void OpenControlsUIWindow(string name);

        [DllImport("__Internal")]
        static extern void CloseControlsUIWindow(string name);

#else
        static void SetNativeCallback(CallbackDelegate callback) { }

        static void OpenControlsUIWindow(string name) { }

        static void CloseControlsUIWindow(string name) { }

#endif
        public void TriggerHomeScene()
        {
            Debug.Log("Triggering Home Scene called from swift!");

            SceneManager.LoadScene("WindowedListScene");
        }
    }
}