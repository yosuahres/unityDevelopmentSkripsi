//swiftuidriver.cs
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
    // This is a driver MonoBehaviour that connects to SwiftUISamplePlugin.swift via
    // C# DllImport. See SwiftUISamplePlugin.swift for more information.
    public class SwiftUIDriver : MonoBehaviour
    {
        bool m_SwiftUIWindowOpen = false;

        void OnEnable()
        {
            SetNativeCallback(CallbackFromNative);

            OpenSwiftUIWindow("HomeView");
            m_SwiftUIWindowOpen = true;
        }

        void OnDisable()
        {
            SetNativeCallback(null);
            CloseSwiftUIWindow("HomeView");
        }

        public void ForceCloseWindow()
        {
            CloseSwiftUIWindow("HomeView");
            m_SwiftUIWindowOpen = false;
        }

        delegate void CallbackDelegate(string command, int value);

        // This attribute is required for methods that are going to be called from native code
        // via a function pointer.
        [MonoPInvokeCallback(typeof(CallbackDelegate))]
        static void CallbackFromNative(string command, int value)
        {
            // MonoPInvokeCallback methods will leak exceptions and cause crashes; always use a try/catch in these methods
            try
            {
                Debug.Log($"Callback from native: {command} {value}");

                // This could be stored in a static field or a singleton.
                // If you need to deal with multiple windows and need to distinguish between them,
                // you could add an ID to this callback and use that to distinguish windows.
                var self = FindFirstObjectByType<SwiftUIDriver>();

                if (command == "closed") {
                    self.m_SwiftUIWindowOpen = false;
                    return;
                }

                if(command == "TriggerImmersiveScene")
                {
                    self.TriggerImmersiveScene();
                    CloseSwiftUIWindow("HomeView");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

#if UNITY_VISIONOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void SetNativeCallback(CallbackDelegate callback);

        [DllImport("__Internal")]
        static extern void SendCommandToSwift(string command, int value);

        [DllImport("__Internal")]
        static extern void OpenSwiftUIWindow(string name);

        [DllImport("__Internal")]
        static extern void CloseSwiftUIWindow(string name);

#else
        static void SetNativeCallback(CallbackDelegate callback) {}
        static void SendCommandToSwift(string command, int value) {}

        static void OpenSwiftUIWindow(string name) {}
        static void CloseSwiftUIWindow(string name) {}
#endif
        public void LoadObj(string path)
        {
            SendCommandToSwift($"load:{path}", 0);
        }
        
        public void TriggerImmersiveScene()
        
        {
            Debug.Log("TriggerImmersiveScene called from Swift!");
            // switch to immersive scene
            SceneManager.LoadScene("FullImmersiveScene");
        }
    }
}
