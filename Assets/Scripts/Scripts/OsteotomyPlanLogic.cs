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
        private List<GameObject> m_ActiveFragments = new List<GameObject>();
        private GameObject m_OriginalModelPrefab; 

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

            m_OriginalModelPrefab = Instantiate(sourceFragment);
            m_OriginalModelPrefab.name = "Original_Uncut_Fragment";
            DontDestroyOnLoad(m_OriginalModelPrefab); 
            m_OriginalModelPrefab.SetActive(false); 

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
            hoverEffect.IntensityMultiplier = 2.0f;

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
            hoverPlane.IntensityMultiplier = 2.0f;
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

            foreach (GameObject plane in currentPlanes)
            {
                List<GameObject> nextSet = new List<GameObject>();
                foreach (GameObject frag in currentSetOfFragments)
                {
                    if (frag == null || !frag.activeSelf) continue;

                    Vector3 sliceOrigin = plane.transform.position;
                    Vector3 sliceNormal = plane.transform.up;

                    AddSliceComponents(frag, sliceOrigin, sliceNormal, (fragA, fragB) =>
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
                float minX = float.MaxValue;
                float maxX = float.MinValue;

                foreach (GameObject plane in currentPlanes)
                {
                    minX = Mathf.Min(minX, plane.transform.position.x);
                    maxX = Mathf.Max(maxX, plane.transform.position.x);
                }

                List<GameObject> kept = new List<GameObject>();
                foreach (GameObject frag in m_ActiveFragments)
                {
                    if (frag == null) continue;
                    MeshRenderer mr = frag.GetComponent<MeshRenderer>();

                    if (mr == null) { kept.Add(frag); continue; }

                    float x = mr.bounds.center.x;

                    if (x < minX || x > maxX)
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
            }
            else
            {
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
            
            foreach (GameObject frag in m_ActiveFragments)
            {
                if (frag != null)
                {
                    Destroy(frag);
                }
            }
            m_ActiveFragments.Clear();

            if (m_LoadedFragment != null)
            {
                Destroy(m_LoadedFragment);
                m_LoadedFragment = null;
            }

            if (m_OriginalModelPrefab != null)
            {
                m_LoadedFragment = Instantiate(m_OriginalModelPrefab);
                m_LoadedFragment.name = m_OriginalModelPrefab.name.Replace("Original_Uncut_", "");
                
                PositionFragment(m_LoadedFragment);
                if (m_LoadedFragment.GetComponent<TouchableObject>() == null)
                    m_LoadedFragment.AddComponent<TouchableObject>();
                m_LoadedFragment.tag = TouchInput.SPAWNABLE_TAG; 
                
                StartCoroutine(SafeSetupCollider(m_LoadedFragment));

                m_LoadedFragment.SetActive(true);
                m_ActiveFragments.Add(m_LoadedFragment);
            }
            else
            {
                Debug.LogError("OsteotomyPlanLogic: Original reference fragment (m_OriginalModelPrefab) is null. Cannot revert.");
            }
            HasPerformedSlice = false;
            TouchInput.SetPlaneVisibility(true);
            TouchInput.SetRulerVisibility(true);
            
            Debug.Log("OsteotomyPlanLogic: Reversion complete. Ready for adjustment.");
        }


        private void AddSliceComponents(GameObject target, Vector3 sliceOriginWorld, Vector3 sliceNormalWorld, System.Action<GameObject, GameObject> onFinished)
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
                HandleNewFragment(fragA, target.name + "_A");
                HandleNewFragment(fragB, target.name + "_B");
                onFinished?.Invoke(fragA, fragB);

                if (fragA != null || fragB != null)
                    target.SetActive(false);
            };

            slice.ComputeSlice(sliceNormalWorld, sliceOriginWorld);
        }

        private void HandleNewFragment(GameObject fragment, string name)
        {
            if (fragment == null) return;

            fragment.name = name;

            PositionFragment(fragment);

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
            hoverEffect.IntensityMultiplier = 1.0f;

            fragment.SetActive(true);
        }
    }
}