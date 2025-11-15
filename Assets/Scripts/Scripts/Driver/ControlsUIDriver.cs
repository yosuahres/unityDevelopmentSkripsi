//ControlsUIDriver.cs
//mark 4 november

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

                if (!string.IsNullOrEmpty(command) && command.StartsWith("LoadModel:")) {
                    var modelName = command.Substring("LoadModel:".Length);
                    if (!string.IsNullOrEmpty(modelName)) {
                        var opl = UnityEngine.Object.FindObjectOfType<OsteotomyPlanLogic>();
                        if (opl != null) {
                            opl.LoadModelByName(modelName);
                        } else {
                            Debug.LogWarning($"OsteotomyPlanLogic not found to load model '{modelName}'");
                        }
                    }
                    return;
                }

                // list command from swift sided

                if (command == "TriggerSliceModel")
                {
                    
                }

                if (command == "TriggerHomeScene")
                {
                    self.TriggerHomeScene();
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