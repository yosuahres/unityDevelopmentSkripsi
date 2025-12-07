//osteotomyplanlogic.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.PolySpatial; 

namespace Assets.Scripts.Scripts
{
    public class OsteotomyPlanLogic : MonoBehaviour
    {
        public static bool HasPerformedSlice { get; private set; } = false;

        [Header("Model Setup")]
        public Vector3 fragmentModelScale = new Vector3(0.001f, 0.001f, 0.001f); 
        public Vector3 wholeModelScale = new Vector3(0.01f, 0.01f, 0.01f); 
        public float spawnDistance = 2.0f;
        public float spawnHeight = 1.5f; 

        [Header("Slicing Setup")]
        public Material osteotomySliceCapMaterial;
        public SliceOptions osteotomySliceOptions;
        public CallbackOptions osteotomyCallbackOptions; 

        // === GIZMO FIELDS MODIFIED FOR COMPLETELY INDEPENDENT OFFSETS ===
        [Header("Gizmo Setup (Z-Axis Ring)")]
        public GameObject gizmoPrefabZ; 
        // MODIFIED: Lowered opacity to 50% (Alpha = 0.5f)
        public Color gizmoColorZ = new Color(1.0f, 0f, 0f, 0.5f); 
        public float gizmoZRotation = 90f; 

        [Header("Gizmo Setup (X-Axis Ring)")]
        public GameObject gizmoPrefabX; 
        // MODIFIED: Lowered opacity to 50% (Alpha = 0.5f)
        public Color gizmoColorX = new Color(0f, 1.0f, 0f, 0.5f); 
        public float gizmoXRotation = 0f; 

        [Header("Common Gizmo Settings")]
        public float gizmoScaleFactor = 100f; 
        
        [Header("Z-Axis Ring Specific Offsets")]
        public float gizmoViewOffsetX_ZAxis = 0f; // NEW: X-offset for Z-ring
        public float gizmoViewOffsetY_ZAxis = 0f; // NEW: Y-offset for Z-ring
        public float gizmoViewOffsetZ_ZAxis = 0f; // NEW: Z-offset for Z-ring

        [Header("X-Axis Ring Specific Offsets")]
        public float gizmoViewOffsetX_XAxis = 0f; // NEW: X-offset for X-ring
        public float gizmoViewOffsetY_XAxis = 0f; // NEW: Y-offset for X-ring
        public float gizmoViewOffsetZ_XAxis = 0f; // NEW: Z-offset for X-ring

        private GameObject m_LoadedGizmoZ; 
        private GameObject m_LoadedGizmoX; 
        // ============================================

        private GameObject m_LoadedFragment;
        private GameObject m_WholeModel; 
        private GameObject m_InitialFragmentPrefab;
        private List<GameObject> m_ActiveFragments = new List<GameObject>();

        void Start()
        {
            HasPerformedSlice = false;

            if (DataManager.Instance == null)
            {
                Debug.LogError("OsteotomyPlanLogic: DataManager.Instance is null! Cannot load models.");
                return;
            }

            if (DataManager.Instance.SelectedFragment == null)
            {
                Debug.LogError("OsteotomyPlanLogic: DataManager.Instance.SelectedFragment is null! Cannot load models.");
                return;
            }

            GameObject sourceFragment = DataManager.Instance.SelectedFragment;
            DataManager.Instance.SelectedFragment = null; 

            string originalModelName = sourceFragment.name.Replace("(Clone)", "").Replace("_Left", "").Replace("_Right", "");
            var wholeModelPrefab = Resources.Load<GameObject>(originalModelName);

            
            
            m_InitialFragmentPrefab = Instantiate(sourceFragment);
            m_InitialFragmentPrefab.name = sourceFragment.name + "_InitialCopy";
            m_InitialFragmentPrefab.SetActive(false); 
            DontDestroyOnLoad(m_InitialFragmentPrefab); 
            
            
            if (m_InitialFragmentPrefab.GetComponent<TouchableObject>() == null)
                m_InitialFragmentPrefab.AddComponent<TouchableObject>();
            m_InitialFragmentPrefab.tag = TouchInput.SPAWNABLE_TAG; 

            var initialHoverEffect = m_InitialFragmentPrefab.GetComponent<VisionOSHoverEffect>();
            if (initialHoverEffect == null) 
                initialHoverEffect = m_InitialFragmentPrefab.AddComponent<VisionOSHoverEffect>();
            
            initialHoverEffect.Type = VisionOSHoverEffect.EffectType.Highlight;
            initialHoverEffect.Color = Color.white;
            initialHoverEffect.IntensityMultiplier = 0.5f;
            
            
            StartCoroutine(SafeSetupCollider(m_InitialFragmentPrefab)); 
            

            m_LoadedFragment = sourceFragment;
            PositionFragment(m_LoadedFragment);
            if (m_LoadedFragment.GetComponent<TouchableObject>() == null)
                m_LoadedFragment.AddComponent<TouchableObject>();
            m_LoadedFragment.tag = TouchInput.SPAWNABLE_TAG; 

            StartCoroutine(SafeSetupCollider(m_LoadedFragment));
            m_LoadedFragment.SetActive(true);

            var hoverEffect = m_LoadedFragment.GetComponent<VisionOSHoverEffect>();
            if (hoverEffect == null) 
                hoverEffect = m_LoadedFragment.AddComponent<VisionOSHoverEffect>();
            else
                hoverEffect.enabled = true; 

            
            hoverEffect.Type = VisionOSHoverEffect.EffectType.Highlight;
            hoverEffect.Color = Color.white;
            hoverEffect.IntensityMultiplier = 0.5f;

            m_ActiveFragments.Add(m_LoadedFragment);
            
            // ======================================
            // CENTER AND ROTATE BOTH GIZMOS ON START
            // ======================================
            SetupAndCenterGizmo(m_LoadedFragment);
            // ======================================
            
            
            if (wholeModelPrefab != null)
            {
                m_WholeModel = Instantiate(wholeModelPrefab);
                DontDestroyOnLoad(m_WholeModel); 
                m_WholeModel.tag = "ROTATEONLY"; 
                PositionWholeModel(m_WholeModel, m_LoadedFragment.transform.position);
                if (m_WholeModel.GetComponent<TouchableObject>() == null)
                    m_WholeModel.AddComponent<TouchableObject>();
                StartCoroutine(SafeSetupCollider(m_WholeModel));
                m_WholeModel.SetActive(true);
            }
            else
            {
                Debug.LogError($"OsteotomyPlanLogic: Could not find original model '{originalModelName}' in Resources to load whole model.");
            }
        }

        /// <summary>
        /// Instantiates, centers, parents, colors, and rotates a single gizmo instance.
        /// </summary>
        private void InitializeAndPositionGizmo(GameObject gizmoPrefab, ref GameObject loadedGizmoInstance, GameObject fragment, Color color, float rotationAngle, string rotationAxis, float offsetX, float offsetY, float offsetZ)
        {
            if (gizmoPrefab == null)
            {
                Debug.LogWarning($"Gizmo Prefab for {rotationAxis}-Axis is not assigned. Skipping gizmo setup.");
                return;
            }

            if (loadedGizmoInstance == null)
            {
                loadedGizmoInstance = Instantiate(gizmoPrefab);
            }

            // --- Apply Color for URP compatibility ---
            MeshRenderer gizmoRenderer = loadedGizmoInstance.GetComponentInChildren<MeshRenderer>();
            if (gizmoRenderer != null && gizmoRenderer.material != null)
            {
                Material mat = gizmoRenderer.material;
                if (mat.HasProperty("_BaseColor"))
                {
                    // This sets the color, including the 0.5f alpha value defined in the field
                    mat.SetColor("_BaseColor", color); 
                }
                else if (mat.HasProperty("_Color"))
                {
                    // This sets the color, including the 0.5f alpha value defined in the field
                    mat.SetColor("_Color", color); 
                }
                else
                {
                    Debug.LogWarning("Gizmo material does not have a recognized color property (_BaseColor or _Color). Color change may fail.");
                }
            }
            else
            {
                Debug.LogWarning("Gizmo model is missing a MeshRenderer or Material. Cannot set color.");
            }
            // --- End Apply Color ---
            
            MeshRenderer fragmentRenderer = fragment.GetComponent<MeshRenderer>();
            Vector3 finalLocalPosition;

            if (fragmentRenderer != null)
            {
                // 1. Calculate the fragment's center in WORLD space
                Vector3 worldCenter = fragmentRenderer.bounds.center;
                
                // 2. Convert that WORLD center to LOCAL space, relative to the fragment (the parent)
                Vector3 localCenterOffset = fragment.transform.InverseTransformPoint(worldCenter);
                
                // 3. APPLY THE NEW X, Y, and Z VIEW OFFSETS 
                finalLocalPosition = localCenterOffset + new Vector3(offsetX, offsetY, offsetZ);
            }
            else
            {
                // Fallback: Use pivot (Vector3.zero) + X, Y, Z Offsets
                finalLocalPosition = new Vector3(offsetX, offsetY, offsetZ);
                Debug.LogError("Fragment model is missing a MeshRenderer. Gizmo using pivot fallback.");
            }

            // 4. Parent, position, and scale (common to both)
            loadedGizmoInstance.transform.SetParent(fragment.transform, false); 
            loadedGizmoInstance.transform.localPosition = finalLocalPosition; 
            loadedGizmoInstance.transform.localScale = Vector3.one * gizmoScaleFactor; 
            
            // 5. Apply the required local rotation based on the axis
            loadedGizmoInstance.transform.localRotation = Quaternion.identity;
            
            switch (rotationAxis.ToUpper())
            {
                case "Z":
                    loadedGizmoInstance.transform.Rotate(0, 0, rotationAngle, Space.Self); 
                    break;
                case "X":
                    loadedGizmoInstance.transform.Rotate(rotationAngle, 0, 0, Space.Self); 
                    break;
                default:
                    break;
            }

            loadedGizmoInstance.SetActive(true);
        }


        /// <summary>
        /// Centers ALL gizmos on the fragment's visible mesh bounds, adds offset, and applies rotation.
        /// </summary>
        /// <param name="fragment">The fragment model to parent the gizmo to.</param>
        private void SetupAndCenterGizmo(GameObject fragment)
        {
            // Setup Z-Axis Gizmo (using independent X, Y, Z offsets)
            InitializeAndPositionGizmo(
                gizmoPrefabZ, 
                ref m_LoadedGizmoZ, 
                fragment, 
                gizmoColorZ, 
                gizmoZRotation, 
                "Z",
                gizmoViewOffsetX_ZAxis,
                gizmoViewOffsetY_ZAxis, // INDEPENDENT Y
                gizmoViewOffsetZ_ZAxis
            );

            // Setup X-Axis Gizmo (using independent X, Y, Z offsets)
            InitializeAndPositionGizmo(
                gizmoPrefabX, 
                ref m_LoadedGizmoX, 
                fragment, 
                gizmoColorX, 
                gizmoXRotation, 
                "X",
                gizmoViewOffsetX_XAxis,
                gizmoViewOffsetY_XAxis, // INDEPENDENT Y
                gizmoViewOffsetZ_XAxis
            );
        }

        private void PositionFragment(GameObject fragment)
        {
            Camera mainCamera = Camera.main;
            
            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();

            
            fragment.transform.SetParent(volumeCamera.transform, false); 
            fragment.transform.localPosition = new Vector3(0, spawnHeight, spawnDistance); 
            fragment.transform.localScale = fragmentModelScale;

            if (mainCamera != null)
                fragment.transform.LookAt(mainCamera.transform, mainCamera.transform.up);

            if (fragment.name.Contains("Left"))
                fragment.transform.Rotate(0, 90, 0, Space.Self); //-60
            else if (fragment.name.Contains("Right"))
                fragment.transform.Rotate(0, -90, 0, Space.Self); //60
        }

        private void PositionWholeModel(GameObject wholeModel, Vector3 referencePosition)
        {
            Camera mainCamera = Camera.main;
            
            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();

            wholeModel.transform.SetParent(volumeCamera.transform, false);

            wholeModel.transform.localPosition = referencePosition + new Vector3(1.0f, 1.0f, 0f); 
            wholeModel.transform.localScale = wholeModelScale; 

            if (mainCamera != null)
                wholeModel.transform.LookAt(mainCamera.transform, mainCamera.transform.up);
        }


        private IEnumerator SafeSetupCollider(GameObject model)
        {
            yield return StartCoroutine(ForceConvexMeshCollider(model));
            yield return new WaitForEndOfFrame();
            yield return StartCoroutine(ForceConvexMeshCollider(model));
        }

        private IEnumerator ForceConvexMeshCollider(GameObject model)
        {
            if (model == null) yield break;

            MeshFilter mf = model.GetComponent<MeshFilter>();
            while (mf == null || mf.sharedMesh == null)
            {
                yield return null;
                mf = model.GetComponent<MeshFilter>();
            }

            MeshCollider col = model.GetComponent<MeshCollider>();
            if (col == null)
                col = model.AddComponent<MeshCollider>();

            const int attempts = 5;

            for (int i = 0; i < attempts; i++)
            {
                col.sharedMesh = null;
                yield return null;

                col.sharedMesh = mf.sharedMesh;
                col.convex = true;

                if (col.sharedMesh != null && col.convex)
                {
                    Debug.Log($"OsteotomyPlanLogic: Convex MeshCollider OK after {i + 1} tries.");
                    yield break;
                }

                Debug.LogWarning($"OsteotomyPlanLogic: Convex retry {i + 1} failed.");
                yield return null;
            }

            Debug.LogError("OsteotomyPlanLogic: Convex MeshCollider FAILED after all retries.");
        }

        /// <summary>
        /// Toggles the active state of the loaded gizmo GameObjects.
        /// </summary>
        /// <param name="isVisible">True to show gizmos, false to hide them.</param>
        [UnityEngine.Scripting.Preserve]
        public void SetGizmoVisibility(bool isVisible)
        {
            if (m_LoadedGizmoZ != null)
            {
                m_LoadedGizmoZ.SetActive(isVisible);
                Debug.Log($"OsteotomyPlanLogic: SetGizmoVisibility Z set to {isVisible}");
            }
            if (m_LoadedGizmoX != null)
            {
                m_LoadedGizmoX.SetActive(isVisible);
                Debug.Log($"OsteotomyPlanLogic: SetGizmoVisibility X set to {isVisible}");
            }
        }


        [UnityEngine.Scripting.Preserve]
        public void PerformOsteotomySlice()
        {
            if (m_ActiveFragments == null || m_ActiveFragments.Count == 0)
            {
                Debug.LogWarning("OsteotomyPlanLogic: No active fragments to slice.");
                return;
            }

            List<GameObject> currentPlanes = TouchInput.currentCuttingPlanes;

            foreach (var plane in currentPlanes)
            {
            if (plane == null) continue;

            var hoverPlane = plane.GetComponent<VisionOSHoverEffect>();
            if (hoverPlane == null)
                hoverPlane = plane.AddComponent<VisionOSHoverEffect>();

            
            
            hoverPlane.Type = VisionOSHoverEffect.EffectType.Highlight;
            hoverPlane.Color = Color.white;
            hoverPlane.IntensityMultiplier = 0.5f;
            }

            if (currentPlanes == null || currentPlanes.Count == 0)
            {
                Debug.LogWarning("OsteotomyPlanLogic: No cutting planes found in TouchInput.");
                return;
            }
            
            HasPerformedSlice = true; 
            Debug.Log("OsteotomyPlanLogic: Slice performed. Plane spawning is now disabled.");

            List<GameObject> currentSetOfFragments = new List<GameObject>(m_ActiveFragments);
            m_ActiveFragments.Clear(); 

            
            Dictionary<GameObject, (Vector3 position, Quaternion rotation)> initialTransforms = new Dictionary<GameObject, (Vector3 position, Quaternion rotation)>();
            foreach (GameObject frag in currentSetOfFragments)
            {
                if (frag != null)
                {
                    initialTransforms[frag] = (frag.transform.position, frag.transform.rotation);
                }
            }
            

            foreach (GameObject plane in currentPlanes)
            {
                List<GameObject> nextSet = new List<GameObject>();
                foreach (GameObject frag in currentSetOfFragments)
                {
                    if (frag == null || !frag.activeSelf) continue;

                    Vector3 sliceOrigin = plane.transform.position;
                    Vector3 sliceNormal = plane.transform.up;

                    
                    (Vector3 position, Quaternion rotation) capturedTransform = initialTransforms.ContainsKey(frag) 
                                                                               ? initialTransforms[frag] 
                                                                               : (frag.transform.position, frag.transform.rotation);
                                                                               
                    AddSliceComponents(frag, sliceOrigin, sliceNormal, capturedTransform, (fragA, fragB) =>
                    {
                        if (fragA != null) nextSet.Add(fragA);
                        if (fragB != null) nextSet.Add(fragB);
                    });
                }
                currentSetOfFragments = nextSet;
            }

            m_ActiveFragments = currentSetOfFragments;
            if (m_ActiveFragments.Count > 0 && currentPlanes.Count > 1)
            {
                // Calculate the Average Filtering Normal (the "ruler" direction)
                Vector3 sumNormal = Vector3.zero;
                foreach (GameObject plane in currentPlanes)
                {
                    sumNormal += plane.transform.up;
                }
                Vector3 filterNormal = sumNormal.normalized;
                
                GameObject referencePlane = currentPlanes[0];
                Vector3 originPosition = referencePlane.transform.position;
                
                // Calculate the minimum and maximum projection distances of all planes
                // This defines the boundary of the 'discard' zone along the filterNormal axis.
                float minProjectionDistance = 0f; 
                float maxProjectionDistance = 0f; 

                foreach (GameObject plane in currentPlanes)
                {
                    // Projection: Vector3.Dot(Vector that goes from origin to plane, Filter Normal)
                    float currentProjectionDistance = Vector3.Dot(plane.transform.position - originPosition, filterNormal);
                    
                    minProjectionDistance = Mathf.Min(minProjectionDistance, currentProjectionDistance);
                    maxProjectionDistance = Mathf.Max(maxProjectionDistance, currentProjectionDistance);
                }

                List<GameObject> kept = new List<GameObject>();
                foreach (GameObject frag in m_ActiveFragments)
                {
                    if (frag == null) continue;
                    MeshRenderer mr = frag.GetComponent<MeshRenderer>();

                    if (mr == null) 
                    { 
                        kept.Add(frag); 
                        frag.SetActive(true);
                        continue; 
                    }

                    // Calculate the fragment's projection distance (D)
                    Vector3 fragmentCenter = mr.bounds.center;
                    float fragmentProjectionDistance = Vector3.Dot(fragmentCenter - originPosition, filterNormal);

                    // Keep the fragment if its projection is OUTSIDE the range
                    // D < Min or D > Max means it's one of the end pieces.
                    if (fragmentProjectionDistance < minProjectionDistance || fragmentProjectionDistance > maxProjectionDistance)
                    {
                        kept.Add(frag);
                        frag.SetActive(true);
                    }
                    else
                    {
                        frag.SetActive(false); 
                    }
                }

                m_ActiveFragments = kept;
                Debug.Log($"OsteotomyPlanLogic: Robustly filtered fragments using Average Normal. Kept {m_ActiveFragments.Count} pieces.");
            }
            else
            {
                foreach (GameObject frag in m_ActiveFragments)
                    if (frag != null) frag.SetActive(true);
            }

            // ======================================
            // RE-CENTER BOTH GIZMOS ON NEW ACTIVE FRAGMENT
            // ======================================
            if (m_ActiveFragments.Count > 0)
            {
                // We assume the first fragment in the kept list is the one to follow
                SetupAndCenterGizmo(m_ActiveFragments[0]);
            }
            else 
            {
                if (m_LoadedGizmoZ != null) m_LoadedGizmoZ.SetActive(false);
                if (m_LoadedGizmoX != null) m_LoadedGizmoX.SetActive(false); 
            }
            // ======================================

            TouchInput.SetPlaneVisibility(false);
            TouchInput.SetRulerVisibility(false);
        }


        [UnityEngine.Scripting.Preserve]
        public void RevertToUncutModel()
        {
            Debug.Log("OsteotomyPlanLogic: Reverting to uncut model.");

            
            Vector3 lastFragmentPosition = Vector3.zero;
            Quaternion lastFragmentRotation = Quaternion.identity;

            
            GameObject fragmentToCapture = m_LoadedFragment;
            if (fragmentToCapture == null && m_ActiveFragments.Count > 0)
            {
                fragmentToCapture = m_ActiveFragments[0];
            }

            if (fragmentToCapture != null)
            {
                lastFragmentPosition = fragmentToCapture.transform.position;
                lastFragmentRotation = fragmentToCapture.transform.rotation;
                Debug.Log($"OsteotomyPlanLogic: Captured last position: {lastFragmentPosition}, rotation: {lastFragmentRotation}");
            }


            
            foreach (GameObject frag in m_ActiveFragments)
            {
                if (frag != null)
                    Destroy(frag);
            }
            m_ActiveFragments.Clear();
            
            if (m_LoadedFragment != null)
            {
                Destroy(m_LoadedFragment);
                m_LoadedFragment = null;
            }

            
            if (m_InitialFragmentPrefab == null)
            {
                Debug.LogError("OsteotomyPlanLogic: Cannot revert. Initial fragment prefab is missing.");
                return;
            }

            
            GameObject newFragment = Instantiate(m_InitialFragmentPrefab);
            newFragment.name = m_InitialFragmentPrefab.name.Replace("_InitialCopy", ""); 

            
            if (fragmentToCapture != null)
            {
                
                var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
                if (volumeCamera != null)
                {
                    
                    newFragment.transform.SetParent(volumeCamera.transform, true); 
                }
                else
                {
                    
                    newFragment.transform.SetParent(null);
                }
                
                
                newFragment.transform.position = lastFragmentPosition;
                newFragment.transform.rotation = lastFragmentRotation;
                
                
                newFragment.transform.localScale = fragmentModelScale; 
            }
            else
            {
                
                PositionFragment(newFragment); 
                Debug.LogWarning("OsteotomyPlanLogic: Reverting to initial position (no last fragment found).");
            }

            newFragment.SetActive(true);
            
            
            m_LoadedFragment = newFragment;
            m_ActiveFragments.Add(m_LoadedFragment);
            HasPerformedSlice = false; 

            // ======================================
            // RE-CENTER BOTH GIZMOS ON REVERTED FRAGMENT
            // ======================================
            SetupAndCenterGizmo(m_LoadedFragment);
            // ======================================
            
            TouchInput.SetPlaneVisibility(true); 
            TouchInput.SetRulerVisibility(true);
            
            Debug.Log($"OsteotomyPlanLogic: Successfully reverted to uncut model: {m_LoadedFragment.name}.");
        }


        private void AddSliceComponents(GameObject target, Vector3 sliceOriginWorld, Vector3 sliceNormalWorld, 
                                        (Vector3 position, Quaternion rotation) capturedTransform, 
                                        System.Action<GameObject, GameObject> onFinished)
        {
            if (target == null) return;

            if (target.GetComponent<MeshFilter>() == null)
                target.AddComponent<MeshFilter>();

            MeshRenderer rend = target.GetComponent<MeshRenderer>();
            if (rend == null)
                rend = target.AddComponent<MeshRenderer>();

            if (rend.sharedMaterial == null)
                rend.sharedMaterial = osteotomySliceCapMaterial;

            StartCoroutine(ForceConvexMeshCollider(target));

            Slice slice = target.GetComponent<Slice>();
            if (slice != null) Destroy(slice);
            slice = target.AddComponent<Slice>();

            if (osteotomySliceOptions == null)
                osteotomySliceOptions = new SliceOptions();

            slice.sliceOptions = osteotomySliceOptions;
            slice.sliceOptions.insideMaterial = osteotomySliceCapMaterial;

            if (osteotomyCallbackOptions == null)
                osteotomyCallbackOptions = new CallbackOptions();

            slice.callbackOptions = osteotomyCallbackOptions;

            slice.OnSliceFinished = (fragA, fragB) =>
            {
                
                HandleNewFragment(fragA, target.name + "_A", capturedTransform); 
                HandleNewFragment(fragB, target.name + "_B", capturedTransform); 
                onFinished?.Invoke(fragA, fragB);

                if (fragA != null || fragB != null)
                    target.SetActive(false);
            };

            slice.ComputeSlice(sliceNormalWorld, sliceOriginWorld);
        }

        private void HandleNewFragment(GameObject fragment, string name, (Vector3 position, Quaternion rotation) capturedTransform) 
        {
            if (fragment == null) return;

            fragment.name = name;

            
            
            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
            if (volumeCamera != null)
            {
                
                fragment.transform.SetParent(volumeCamera.transform, true);
            }
            
            
            fragment.transform.position = capturedTransform.position;
            fragment.transform.rotation = capturedTransform.rotation;
            fragment.transform.localScale = fragmentModelScale; 
            

            if (fragment.GetComponent<TouchableObject>() == null)
                fragment.AddComponent<TouchableObject>();

            StartCoroutine(ForceConvexMeshCollider(fragment));

            var hoverEffect = fragment.GetComponent<VisionOSHoverEffect>();
            if (hoverEffect == null) 
                hoverEffect = fragment.AddComponent<VisionOSHoverEffect>();
            else
                hoverEffect.enabled = true; 

            
            hoverEffect.Type = VisionOSHoverEffect.EffectType.Highlight;
            hoverEffect.Color = Color.white;
            hoverEffect.IntensityMultiplier = 0.5f;

            fragment.SetActive(true);
        }
    }
}