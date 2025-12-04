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
                fragment.transform.Rotate(0, 90, -60, Space.Self);
            else if (fragment.name.Contains("Right"))
                fragment.transform.Rotate(0, -90, 60, Space.Self);
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

            // --- START OF ROBUST FILTERING LOGIC ---
            if (m_ActiveFragments.Count > 0 && currentPlanes.Count > 1)
            {
                
                // 1. Establish the reference plane and the filtering axis (Normal)
                GameObject referencePlane = currentPlanes[0];
                Vector3 filterNormal = referencePlane.transform.up;
                Vector3 originPosition = referencePlane.transform.position;

                // 2. Calculate the minimum and maximum projection distances of all planes
                float minProjectionDistance = 0f; // Distance from the first plane to itself is 0
                float maxProjectionDistance = 0f; 

                foreach (GameObject plane in currentPlanes)
                {
                    // Calculate the distance of the plane from the reference plane along the filterNormal
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

                    // 3. Calculate the fragment's projection distance (D)
                    Vector3 fragmentCenter = mr.bounds.center;
                    float fragmentProjectionDistance = Vector3.Dot(fragmentCenter - originPosition, filterNormal);

                    // 4. Robust Filter: Keep the fragment if its projection is OUTSIDE the range
                    if (fragmentProjectionDistance < minProjectionDistance || fragmentProjectionDistance > maxProjectionDistance)
                    {
                        kept.Add(frag); // Kept if it's on the 'outside'
                        frag.SetActive(true);
                    }
                    else
                    {
                        frag.SetActive(false); // Discarded if it's in the 'middle'
                    }
                }

                m_ActiveFragments = kept;
                Debug.Log($"OsteotomyPlanLogic: Robustly filtered fragments. Kept {m_ActiveFragments.Count} pieces.");
            }
            // --- END OF ROBUST FILTERING LOGIC ---
            else
            {
                // If only one plane was used, or if the list is empty, just ensure everything is active
                foreach (GameObject frag in m_ActiveFragments)
                    if (frag != null) frag.SetActive(true);
            }

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