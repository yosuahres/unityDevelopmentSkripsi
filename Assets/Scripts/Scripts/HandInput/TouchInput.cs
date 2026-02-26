//TouchInput.cs
using UnityEngine;
using Unity.PolySpatial;
using Unity.PolySpatial.InputDevices;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.InputSystem.LowLevel;
using System.Collections.Generic;
using Assets.Scripts.Scripts;

public class TouchInput : MonoBehaviour
{
    [Header("Settings")]
    public static readonly string SPAWNABLE_TAG = "SPAWNABLE";
    public static float currentPlaneScale = 0.1f;
    public static float fibulaPlaneScale = 0.05f;
    public static readonly float minPlaneScale = 0.1f;
    public static readonly float maxPlaneScale = 0.5f;
    public static int maxCuttingPlanes = 2;

    [Header("Prefabs")]
    [Tooltip("Prefab used for the primary mandible cutting planes.")]
    public GameObject planeFragmentPrefab;
    [Tooltip("Prefab used specifically for the fibula reconstruction planes.")]
    public GameObject fibulaPlanePrefab;
    public GameObject rulerPrefab;

    // State Tracking
    public static List<GameObject> currentCuttingPlanes { get; private set; } = new List<GameObject>();
    public static List<GameObject> fibulaPlanes { get; private set; } = new List<GameObject>();
    private List<GameObject> activeRulers = new List<GameObject>();

    public static TouchInput Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable() => EnhancedTouchSupport.Enable();

    #region Visibility and Scaling
    public static void SetPlaneVisibility(bool isVisible)
    {
        foreach (var plane in currentCuttingPlanes) if (plane != null) plane.SetActive(isVisible);
        foreach (var plane in fibulaPlanes) if (plane != null) plane.SetActive(isVisible);
    }

    public static void SetRulerVisibility(bool isVisible)
    {
        if (Instance == null) return;
        foreach (var ruler in Instance.activeRulers) if (ruler != null) ruler.SetActive(isVisible);
    }

    public static void SetPlaneScale(float scale)
    {
        currentPlaneScale = Mathf.Clamp(scale, minPlaneScale, maxPlaneScale);
        Vector3 newScale = new Vector3(currentPlaneScale, 0.0002f, currentPlaneScale);

        foreach (var plane in currentCuttingPlanes) if (plane != null) plane.transform.localScale = newScale;
        foreach (var plane in fibulaPlanes) if (plane != null) plane.transform.localScale = newScale;
    }
    #endregion

    #region Cleanup
    public static void ClearPlaneList()
    {
        foreach (var plane in currentCuttingPlanes) Destroy(plane);
        foreach (var plane in fibulaPlanes) Destroy(plane);
        currentCuttingPlanes.Clear();
        fibulaPlanes.Clear();

        if (Instance != null)
        {
            foreach (var ruler in Instance.activeRulers) Destroy(ruler);
            Instance.activeRulers.Clear();
        }
    }

    public static void CheckAndEnforceMaxPlanes()
    {
        while (currentCuttingPlanes.Count > maxCuttingPlanes)
        {
            int lastIdx = currentCuttingPlanes.Count - 1;
            Destroy(currentCuttingPlanes[lastIdx]);
            currentCuttingPlanes.RemoveAt(lastIdx);

            if (Instance != null && Instance.activeRulers.Count > 0)
            {
                int lastRulerIdx = Instance.activeRulers.Count - 1;
                Destroy(Instance.activeRulers[lastRulerIdx]);
                Instance.activeRulers.RemoveAt(lastRulerIdx);
            }
        }
    }
    #endregion

    #region Spawning Logic
    private void Update()
    {
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase == TouchPhase.Began) HandleTouchBegan(touch);
        }
    }

    private void HandleTouchBegan(Touch touch)
    {
        if (OsteotomyPlanLogic.HasPerformedSlice) return;

        SpatialPointerState touchData = EnhancedSpatialPointerSupport.GetPointerState(touch);

        //|| touchData.Kind == SpatialPointerKind.IndirectPinch
        if (touchData.targetObject != null && (touchData.Kind == SpatialPointerKind.Touch || touchData.Kind == SpatialPointerKind.IndirectPinch) )
        {
            ISpatialTouchable touchable = touchData.targetObject.GetComponent<ISpatialTouchable>();
            bool isSpawnable = touchData.targetObject.CompareTag(SPAWNABLE_TAG);

            if (touchable != null && isSpawnable)
            {
                Vector3 spawnPosition = touchData.interactionPosition;
                Vector3 touchNormal = touchData.inputDeviceRotation * Vector3.forward;
                Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, touchNormal) * Quaternion.Euler(0, 100f, 0);

                SpawnMandiblePlane(spawnPosition, spawnRotation);
                touchable.OnSpatialTouch(spawnPosition, touchNormal);
            }
        }
    }

    private void SpawnMandiblePlane(Vector3 position, Quaternion rotation)
    {
        if (currentCuttingPlanes.Count >= maxCuttingPlanes || planeFragmentPrefab == null) return;

        GameObject plane = Instantiate(planeFragmentPrefab, position, rotation);
        plane.tag = "PLANE";
        plane.transform.localScale = new Vector3(currentPlaneScale, 0.0002f, currentPlaneScale);
        currentCuttingPlanes.Add(plane);

        var logic = Object.FindFirstObjectByType<OsteotomyPlanLogic>();
        if (logic != null) logic.SyncAllPlanesToFibula();

        if (currentCuttingPlanes.Count >= 2) 
            CreateRulerBetween(currentCuttingPlanes[currentCuttingPlanes.Count - 2].transform, plane.transform);
    }

    public void SpawnPlaneExternal(Vector3 position, Quaternion rotation, bool ignoreLimit = false)
    {
        // Use fibula-specific prefab, fallback to fragment prefab if null
        GameObject prefabToUse = fibulaPlanePrefab != null ? fibulaPlanePrefab : planeFragmentPrefab;
        if (prefabToUse == null) return;

        GameObject plane = Instantiate(prefabToUse, position, rotation);
        
        var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
        if (volumeCamera != null) plane.transform.SetParent(volumeCamera.transform, true);

        plane.tag = "PLANE_FIBULA";
        plane.transform.localScale = new Vector3(fibulaPlaneScale, 0.0002f, fibulaPlaneScale);
        fibulaPlanes.Add(plane);

        if (fibulaPlanes.Count >= 2) 
            CreateRulerBetween(fibulaPlanes[fibulaPlanes.Count - 2].transform, plane.transform);
    }

    private void CreateRulerBetween(Transform p1, Transform p2)
    {
        if (rulerPrefab == null) return;

        Vector3 rulerPosition = (p1.position + p2.position) / 2f;
        GameObject newRuler = Instantiate(rulerPrefab, rulerPosition, Quaternion.identity);
        activeRulers.Add(newRuler);

        if (newRuler.TryGetComponent<RulerVisualizer>(out var rv)) rv.SetPoints(p1, p2);
    }
    #endregion
}