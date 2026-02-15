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
        public Vector3 fragmentModelScale = new Vector3(0.001f, 0.001f, 0.01f);
        public Vector3 wholeModelScale = new Vector3(0.01f, 0.01f, 0.01f);
        public Vector3 fibulaModelScale = new Vector3(0.001f, 0.001f, 0.01f); // Added for Fibula
        public float spawnDistance = 2.0f;
        public float spawnHeight = 1.5f;

        [Header("Slicing Setup")]
        public Material osteotomySliceCapMaterial;
        public SliceOptions osteotomySliceOptions;
        public CallbackOptions osteotomyCallbackOptions;

        [Header("Gizmo Component")]
        public GizmoVisualizer gizmoVisualizer;

        private GameObject m_LoadedFragment;
        private GameObject m_WholeModel;
        private GameObject m_FibulaModel; // Reference for Fibula
        private GameObject m_InitialFragmentPrefab;
        private List<GameObject> m_ActiveFragments = new List<GameObject>();

        #region Initialization

        void Start()
        {
            if (!ValidateDependencies()) return;
            InitializeOsteotomySystem();
        }

        private bool ValidateDependencies()
        {
            HasPerformedSlice = false;
            if (gizmoVisualizer == null) gizmoVisualizer = GetComponent<GizmoVisualizer>();

            if (gizmoVisualizer == null)
            {
                Debug.LogError("OsteotomyPlanLogic: GizmoVisualizer missing!");
                return false;
            }

            if (DataManager.Instance == null || DataManager.Instance.SelectedFragment == null)
            {
                Debug.LogError("OsteotomyPlanLogic: DataManager or SelectedFragment is null!");
                return false;
            }
            return true;
        }

        private void InitializeOsteotomySystem()
        {
            // 1. Extract and Clean Data
            GameObject sourceFragment = DataManager.Instance.SelectedFragment;
            DataManager.Instance.SelectedFragment = null; 

            string originalModelName = sourceFragment.name
                .Replace("(Clone)", "")
                .Replace("_Left", "")
                .Replace("_Right", "");

            // 2. Setup Reference Prefab
            m_InitialFragmentPrefab = Instantiate(sourceFragment);
            m_InitialFragmentPrefab.name = sourceFragment.name + "_InitialCopy";
            m_InitialFragmentPrefab.SetActive(false);
            DontDestroyOnLoad(m_InitialFragmentPrefab);
            SetupModelCommonRequirements(m_InitialFragmentPrefab, TouchInput.SPAWNABLE_TAG);

            // 3. Setup Active Fragment
            m_LoadedFragment = sourceFragment;
            SetupModelCommonRequirements(m_LoadedFragment, TouchInput.SPAWNABLE_TAG);
            PositionFragment(m_LoadedFragment);
            
            m_ActiveFragments.Add(m_LoadedFragment);
            m_LoadedFragment.SetActive(true);

            // 4. Gizmo Setup
            gizmoVisualizer.SetupAndCenterGizmo(m_LoadedFragment);
            gizmoVisualizer.SetGizmoVisibility(false);

            // 5. Load Secondary Models
            LoadWholeModel(originalModelName);
            LoadFibulaModel("Fibula"); // Added Fibula loader call
        }

        private void LoadWholeModel(string modelName)
        {
            GameObject prefab = Resources.Load<GameObject>(modelName);
            if (prefab != null)
            {
                m_WholeModel = Instantiate(prefab);
                DontDestroyOnLoad(m_WholeModel);
                SetupModelCommonRequirements(m_WholeModel, "ROTATEONLY");
                PositionWholeModel(m_WholeModel, m_LoadedFragment.transform.position);
                m_WholeModel.SetActive(true);
            }
        }

        private void LoadFibulaModel(string modelName)
        {
            GameObject prefab = Resources.Load<GameObject>(modelName);
            if (prefab != null)
            {
                m_FibulaModel = Instantiate(prefab);
                DontDestroyOnLoad(m_FibulaModel);
                
                // Fibula doesn't necessarily need to be spawnable/sliceable, 
                // but we keep common requirements for visual consistency.
                SetupModelCommonRequirements(m_FibulaModel, "ROTATEONLY"); 
                
                PositionFibulaModel(m_FibulaModel, m_LoadedFragment.transform.position);
                m_FibulaModel.SetActive(true);
                Debug.Log("OsteotomyPlanLogic: Fibula model loaded successfully.");
            }
            else
            {
                Debug.LogWarning($"OsteotomyPlanLogic: Could not find '{modelName}' in Resources.");
            }
        }

        #endregion

        #region Core Functionality (Slicing & Reverting)

        [UnityEngine.Scripting.Preserve]
        public void PerformOsteotomySlice()
        {
            if (m_ActiveFragments.Count == 0) return;

            List<GameObject> currentPlanes = TouchInput.currentCuttingPlanes;
            if (currentPlanes == null || currentPlanes.Count == 0) return;

            HasPerformedSlice = true;
            List<GameObject> currentSetOfFragments = new List<GameObject>(m_ActiveFragments);
            m_ActiveFragments.Clear();

            Dictionary<GameObject, (Vector3 pos, Quaternion rot)> initialTransforms = new Dictionary<GameObject, (Vector3, Quaternion)>();
            foreach (var f in currentSetOfFragments) 
                if (f != null) initialTransforms[f] = (f.transform.position, f.transform.rotation);

            foreach (GameObject plane in currentPlanes)
            {
                List<GameObject> nextSet = new List<GameObject>();
                foreach (GameObject frag in currentSetOfFragments)
                {
                    if (frag == null || !frag.activeSelf) continue;

                    var captured = initialTransforms.ContainsKey(frag) ? initialTransforms[frag] : (frag.transform.position, frag.transform.rotation);

                    AddSliceComponents(frag, plane.transform.position, plane.transform.up, captured, (fragA, fragB) =>
                    {
                        if (fragA != null) nextSet.Add(fragA);
                        if (fragB != null) nextSet.Add(fragB);
                    });
                }
                currentSetOfFragments = nextSet;
            }

            // Apply Visibility Filter for wedge removal
            if (currentPlanes.Count > 1)
            {
                m_ActiveFragments = FilterFragmentsBetweenPlanes(currentSetOfFragments, currentPlanes);
            }
            else
            {
                m_ActiveFragments = currentSetOfFragments;
                foreach (var frag in m_ActiveFragments) if (frag != null) frag.SetActive(true);
            }

            FinalizeSliceUI();
        }

        private List<GameObject> FilterFragmentsBetweenPlanes(List<GameObject> fragments, List<GameObject> planes)
        {
            Vector3 sumNormal = Vector3.zero;
            foreach (var plane in planes) sumNormal += plane.transform.up;
            Vector3 filterNormal = sumNormal.normalized;

            Vector3 originPosition = planes[0].transform.position;
            float minProj = 0f;
            float maxProj = 0f;

            foreach (var plane in planes)
            {
                float proj = Vector3.Dot(plane.transform.position - originPosition, filterNormal);
                minProj = Mathf.Min(minProj, proj);
                maxProj = Mathf.Max(maxProj, proj);
            }

            List<GameObject> kept = new List<GameObject>();
            foreach (var frag in fragments)
            {
                if (frag == null) continue;
                MeshRenderer mr = frag.GetComponent<MeshRenderer>();
                
                if (mr == null)
                {
                    kept.Add(frag);
                    frag.SetActive(true);
                    continue;
                }

                float fragProj = Vector3.Dot(mr.bounds.center - originPosition, filterNormal);

                if (fragProj < minProj || fragProj > maxProj)
                {
                    kept.Add(frag);
                    frag.SetActive(true);
                }
                else
                {
                    frag.SetActive(false);
                }
            }
            return kept;
        }

        [UnityEngine.Scripting.Preserve]
        public void RevertToUncutModel()
        {
            Vector3 lastPos = m_LoadedFragment ? m_LoadedFragment.transform.position : Vector3.zero;
            Quaternion lastRot = m_LoadedFragment ? m_LoadedFragment.transform.rotation : Quaternion.identity;

            foreach (GameObject frag in m_ActiveFragments) if (frag != null) Destroy(frag);
            m_ActiveFragments.Clear();
            if (m_LoadedFragment != null) Destroy(m_LoadedFragment);

            if (m_InitialFragmentPrefab == null) return;

            GameObject newFragment = Instantiate(m_InitialFragmentPrefab);
            newFragment.name = m_InitialFragmentPrefab.name.Replace("_InitialCopy", "");
            
            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
            newFragment.transform.SetParent(volumeCamera?.transform, true);
            newFragment.transform.position = lastPos;
            newFragment.transform.rotation = lastRot;
            newFragment.transform.localScale = fragmentModelScale;

            SetupModelCommonRequirements(newFragment, TouchInput.SPAWNABLE_TAG);
            newFragment.SetActive(true);

            m_LoadedFragment = newFragment;
            m_ActiveFragments.Add(m_LoadedFragment);
            HasPerformedSlice = false;

            gizmoVisualizer.SetupAndCenterGizmo(m_LoadedFragment);
            gizmoVisualizer.SetGizmoVisibility(false);

            TouchInput.SetPlaneVisibility(true);
            TouchInput.SetRulerVisibility(true);
        }

        #endregion

        #region Helpers & Positioners

        private void SetupModelCommonRequirements(GameObject obj, string tag)
        {
            obj.tag = tag;
            if (obj.GetComponent<TouchableObject>() == null) obj.AddComponent<TouchableObject>();

            var hover = obj.GetComponent<VisionOSHoverEffect>() ?? obj.AddComponent<VisionOSHoverEffect>();
            hover.Type = VisionOSHoverEffect.EffectType.Highlight;
            hover.Color = Color.white;
            hover.IntensityMultiplier = 0.5f;

            StartCoroutine(SafeSetupCollider(obj));
        }

        private void PositionFragment(GameObject fragment)
        {
            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
            fragment.transform.SetParent(volumeCamera?.transform, false);
            fragment.transform.localPosition = new Vector3(0, spawnHeight, spawnDistance);
            fragment.transform.localScale = fragmentModelScale;

            ApplyLookRotation(fragment);
            if (fragment.name.Contains("Left")) fragment.transform.Rotate(0, 90, 0, Space.Self);
            else if (fragment.name.Contains("Right")) fragment.transform.Rotate(0, -90, 0, Space.Self);
        }

        private void PositionWholeModel(GameObject wholeModel, Vector3 referencePosition)
        {
            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
            wholeModel.transform.SetParent(volumeCamera?.transform, false);

            wholeModel.transform.localPosition = referencePosition + new Vector3(-1.0f, 1.0f, 0f);
            wholeModel.transform.localScale = wholeModelScale;
            ApplyLookRotation(wholeModel);
        }

        private void PositionFibulaModel(GameObject fibula, Vector3 referencePosition)
        {
            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
            
            fibula.transform.SetParent(null);
            fibula.transform.localScale = Vector3.one;
            fibula.transform.SetParent(volumeCamera?.transform, false);
            
            fibula.transform.localPosition = referencePosition + new Vector3(0.5f, 0f, 0f); 
            fibula.transform.localScale = fibulaModelScale;
            
            ApplyLookRotation(fibula);

            var mesh = fibula.GetComponentInChildren<MeshFilter>()?.sharedMesh;
            if (mesh != null) {
                Debug.Log($"Fibula Mesh Bounds: {mesh.bounds.size}");
            }
        }

        private void ApplyLookRotation(GameObject obj)
        {
            Camera main = Camera.main;
            if (main != null) obj.transform.LookAt(main.transform, main.transform.up);
        }

        private void AddSliceComponents(GameObject target, Vector3 sliceOrigin, Vector3 sliceNormal,
                                        (Vector3 pos, Quaternion rot) captured, System.Action<GameObject, GameObject> onFinished)
        {
            if (target == null) return;
            
            var slice = target.GetComponent<Slice>() ?? target.AddComponent<Slice>();
            slice.sliceOptions = osteotomySliceOptions ?? new SliceOptions();
            slice.sliceOptions.insideMaterial = osteotomySliceCapMaterial;
            slice.callbackOptions = osteotomyCallbackOptions ?? new CallbackOptions();

            slice.OnSliceFinished = (fragA, fragB) =>
            {
                HandleNewFragment(fragA, target.name + "_A", captured);
                HandleNewFragment(fragB, target.name + "_B", captured);
                onFinished?.Invoke(fragA, fragB);
                target.SetActive(false);
            };

            slice.ComputeSlice(sliceNormal, sliceOrigin);
        }

        private void HandleNewFragment(GameObject fragment, string name, (Vector3 pos, Quaternion rot) captured)
        {
            if (fragment == null) return;
            fragment.name = name;
            
            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
            fragment.transform.SetParent(volumeCamera?.transform, true);
            fragment.transform.position = captured.pos;
            fragment.transform.rotation = captured.rot;
            fragment.transform.localScale = fragmentModelScale;

            SetupModelCommonRequirements(fragment, TouchInput.SPAWNABLE_TAG);
            fragment.SetActive(true);
        }

        private void FinalizeSliceUI()
        {
            if (m_ActiveFragments.Count > 0 && gizmoVisualizer != null)
            {
                gizmoVisualizer.SetupAndCenterGizmo(m_ActiveFragments[0]);
                gizmoVisualizer.SetGizmoVisibility(false);
            }
            TouchInput.SetPlaneVisibility(false);
            TouchInput.SetRulerVisibility(false);
        }

        private IEnumerator SafeSetupCollider(GameObject model)
        {
            if (model == null) yield break;
            yield return StartCoroutine(ForceConvexMeshCollider(model));
            yield return new WaitForEndOfFrame();
            if (model != null) yield return StartCoroutine(ForceConvexMeshCollider(model));
        }

        private IEnumerator ForceConvexMeshCollider(GameObject model)
        {
            if (model == null) yield break;
            MeshFilter mf = model.GetComponent<MeshFilter>();
            
            float timer = 0;
            while ((mf == null || mf.sharedMesh == null) && timer < 2f)
            {
                yield return null;
                timer += Time.deltaTime;
                if (model == null) yield break;
                mf = model.GetComponent<MeshFilter>();
            }

            MeshCollider col = model.GetComponent<MeshCollider>() ?? model.AddComponent<MeshCollider>();
            
            col.sharedMesh = null;
            yield return new WaitForEndOfFrame(); 

            if (model != null)
            {
                col.sharedMesh = mf.sharedMesh;
                col.convex = true;
                // Force a sync with PolySpatial
                col.enabled = false;
                col.enabled = true;
            }
        }
        #endregion
    }
}