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
        public Vector3 fibulaModelScale = new Vector3(0.001f, 0.001f, 0.001f); 
        public float spawnDistance = 2.0f;
        public float spawnHeight = 1.5f;

        [Header("Fibula Offset")]
        [Tooltip("Offset (in meters) from the fibula tip for the first cutting plane.")]
        public float fibulaStartOffsetCm = 0.05f; // 5 cm

        [Header("Slicing Setup")]
        public Material osteotomySliceCapMaterial;
        public SliceOptions osteotomySliceOptions;
        public CallbackOptions osteotomyCallbackOptions;

        [Header("Gizmo Component")]
        public GizmoVisualizer gizmoVisualizer;

        private GameObject m_LoadedFragment;
        private GameObject m_WholeModel;
        private GameObject m_FibulaModel; 
        private GameObject m_InitialFragmentPrefab;
        private GameObject m_InitialFibulaPrefab;
        private List<GameObject> m_FibulaPlanes = new List<GameObject>();
        private List<GameObject> m_ActiveFragments = new List<GameObject>();
        private List<GameObject> m_ActiveFibulaFragments = new List<GameObject>();

        #region Initialization

        void Start()
        {
            if (!ValidateDependencies()) return;
            InitializeOsteotomySystem();
        }

        void Update()
        {
            if (TouchInput.currentCuttingPlanes != null && TouchInput.currentCuttingPlanes.Count > 0)
            {
                SyncAllPlanesToFibula();
            }
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
            GameObject sourceFragment = DataManager.Instance.SelectedFragment;
            DataManager.Instance.SelectedFragment = null; 

            string originalModelName = sourceFragment.name
                .Replace("(Clone)", "")
                .Replace("_Left", "")
                .Replace("_Right", "");

            m_InitialFragmentPrefab = Instantiate(sourceFragment);
            m_InitialFragmentPrefab.name = sourceFragment.name + "_InitialCopy";
            m_InitialFragmentPrefab.SetActive(false);
            DontDestroyOnLoad(m_InitialFragmentPrefab);
            SetupModelCommonRequirements(m_InitialFragmentPrefab, TouchInput.SPAWNABLE_TAG);

            m_LoadedFragment = sourceFragment;
            SetupModelCommonRequirements(m_LoadedFragment, TouchInput.SPAWNABLE_TAG);
            PositionFragment(m_LoadedFragment);
            
            m_ActiveFragments.Add(m_LoadedFragment);
            m_LoadedFragment.SetActive(true);

            gizmoVisualizer.SetupAndCenterGizmo(m_LoadedFragment);
            gizmoVisualizer.SetGizmoVisibility(false);

            LoadWholeModel(originalModelName);
            LoadFibulaModel("Fibula"); 
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

                m_InitialFibulaPrefab = Instantiate(prefab);
                m_InitialFibulaPrefab.name = modelName + "_InitialCopy";
                m_InitialFibulaPrefab.SetActive(false);
                DontDestroyOnLoad(m_InitialFibulaPrefab);
                
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

        public void UpdateFibulaBridgeOrientations()
        {
            
            
        }
        
        private void TrimFibulaPlanes(int requiredCount)
        {
            while (TouchInput.fibulaPlanes.Count > requiredCount)
            {
                int lastIdx = TouchInput.fibulaPlanes.Count - 1;
                GameObject excessPlane = TouchInput.fibulaPlanes[lastIdx];
                if (excessPlane != null) Destroy(excessPlane);
                TouchInput.fibulaPlanes.RemoveAt(lastIdx);
            }
        }

        public void SyncAllPlanesToFibula()
        {
            List<GameObject> mPlanes = TouchInput.currentCuttingPlanes;
            if (mPlanes.Count < 2 || m_FibulaModel == null || m_LoadedFragment == null)
            {
                
                TrimFibulaPlanes(0);
                return;
            }

            int requiredFibulaPlanes = mPlanes.Count <= 2 ? mPlanes.Count : (mPlanes.Count - 2) * 2 + 2;

            
            TrimFibulaPlanes(requiredFibulaPlanes);

            while (TouchInput.fibulaPlanes.Count < requiredFibulaPlanes)
            {
                TouchInput.Instance.SpawnPlaneExternal(Vector3.zero, Quaternion.identity);
            }

            List<GameObject> fPlanes = TouchInput.fibulaPlanes;
            MeshRenderer fibulaRenderer = m_FibulaModel.GetComponentInChildren<MeshRenderer>();

            
            Vector3 fibulaBoneAxis = m_FibulaModel.transform.right;

            // Project the AABB extent onto the actual bone axis to find the correct half-length
            Vector3 ext = fibulaRenderer.bounds.extents;
            float projectedExtent = Mathf.Abs(fibulaBoneAxis.x) * ext.x
                                  + Mathf.Abs(fibulaBoneAxis.y) * ext.y
                                  + Mathf.Abs(fibulaBoneAxis.z) * ext.z;
            Vector3 fibulaStart = fibulaRenderer.bounds.center + (fibulaBoneAxis * projectedExtent)
                                  - (fibulaBoneAxis * fibulaStartOffsetCm); // apply start offset

            Vector3 mandibleBoneAxis = (mPlanes[mPlanes.Count - 1].transform.position - mPlanes[0].transform.position).normalized;
            Quaternion boneMapping = Quaternion.FromToRotation(mandibleBoneAxis, -fibulaBoneAxis);

            // Pre-compute cumulative distances along the mandible path (segment-by-segment)
            float[] cumulativeDist = new float[mPlanes.Count];
            cumulativeDist[0] = 0f;
            for (int i = 1; i < mPlanes.Count; i++)
            {
                cumulativeDist[i] = cumulativeDist[i - 1]
                    + Vector3.Distance(mPlanes[i - 1].transform.position, mPlanes[i].transform.position);
            }

            int fibIndex = 0;
            for (int i = 0; i < mPlanes.Count; i++)
            {
                float worldDist = cumulativeDist[i];
                Vector3 targetBasePos = fibulaStart - (fibulaBoneAxis * worldDist);

                
                Quaternion targetRotation = boneMapping * mPlanes[i].transform.rotation;

                if (i == 0 || i == mPlanes.Count - 1)
                {
                    fPlanes[fibIndex].transform.position = targetBasePos;
                    fPlanes[fibIndex].transform.rotation = targetRotation;
                    fibIndex++;
                }
                else
                {
                    float wedgeGap = 0.001f;

                    
                    Vector3 segBefore = (mPlanes[i].transform.position - mPlanes[i - 1].transform.position).normalized;
                    Vector3 segAfter  = (mPlanes[i + 1].transform.position - mPlanes[i].transform.position).normalized;
                    float wedgeHalfAngle = Vector3.Angle(segBefore, segAfter) * 0.5f;

                    
                    
                    Vector3 bendCross = Vector3.Cross(segBefore, segAfter);
                    Vector3 fibulaBendAxis;
                    if (bendCross.sqrMagnitude > 0.0001f)
                        fibulaBendAxis = (boneMapping * bendCross.normalized).normalized;
                    else
                        fibulaBendAxis = m_FibulaModel.transform.up; 

                    fPlanes[fibIndex].transform.position     = targetBasePos + (fibulaBoneAxis * wedgeGap);
                    fPlanes[fibIndex + 1].transform.position = targetBasePos - (fibulaBoneAxis * wedgeGap);

                    
                    fPlanes[fibIndex].transform.rotation     = Quaternion.AngleAxis( wedgeHalfAngle, fibulaBendAxis) * targetRotation;
                    fPlanes[fibIndex + 1].transform.rotation = Quaternion.AngleAxis(-wedgeHalfAngle, fibulaBendAxis) * targetRotation;
                    fibIndex += 2;
                }
            }
        }
        
        #endregion

        #region slicing & reverting

        [UnityEngine.Scripting.Preserve]

        public void PerformFibulaSlice()
        {
            if (m_FibulaModel == null) return;

            List<GameObject> mPlanes = TouchInput.currentCuttingPlanes;
            if (mPlanes == null || mPlanes.Count < 2) return;

            
            int requiredFibulaPlanes = mPlanes.Count <= 2 ? mPlanes.Count : (mPlanes.Count - 2) * 2 + 2;

            
            List<GameObject> allFibulaPlanes = TouchInput.fibulaPlanes;
            if (allFibulaPlanes == null || allFibulaPlanes.Count < 2) return;
            int planesToUse = Mathf.Min(allFibulaPlanes.Count, requiredFibulaPlanes);
            List<GameObject> fPlanes = allFibulaPlanes.GetRange(0, planesToUse);

            Debug.Log($"PerformFibulaSlice: {mPlanes.Count} mandible planes → {planesToUse} fibula planes (of {allFibulaPlanes.Count} total)");

            
            Vector3 fibulaBoneAxis = m_FibulaModel.transform.right;
            Vector3 fibulaPos = m_FibulaModel.transform.position;
            Quaternion fibulaRot = m_FibulaModel.transform.rotation;

            
            int expectedSegments = planesToUse + 1;
            
            int expectedGrafts = mPlanes.Count - 1;

            
            List<GameObject> fibulaSegments = new List<GameObject> { m_FibulaModel };

            foreach (GameObject plane in fPlanes)
            {
                if (plane == null) continue;
                List<GameObject> nextSegments = new List<GameObject>();
                foreach (GameObject segment in fibulaSegments)
                {
                    if (segment == null || !segment.activeSelf) continue;

                    AddSliceComponents(segment, plane.transform.position, plane.transform.up,
                        (fibulaPos, fibulaRot), (fragA, fragB) =>
                    {
                        if (fragA != null) nextSegments.Add(fragA);
                        if (fragB != null) nextSegments.Add(fragB);
                    });
                }
                fibulaSegments = nextSegments;
            }

            Debug.Log($"PerformFibulaSlice: Produced {fibulaSegments.Count} segments (expected {expectedSegments})");

            if (fibulaSegments.Count < 3)
            {
                Debug.LogWarning("PerformFibulaSlice: Not enough segments produced.");
                return;
            }

            
            fibulaSegments.Sort((a, b) =>
            {
                float projA = ProjectOntoAxis(a, fibulaPos, fibulaBoneAxis);
                float projB = ProjectOntoAxis(b, fibulaPos, fibulaBoneAxis);
                return projB.CompareTo(projA);
            });

            m_ActiveFibulaFragments.Clear();

            
            fibulaSegments[0].SetActive(false);
            fibulaSegments[fibulaSegments.Count - 1].SetActive(false);

            
            List<GameObject> interiorSegments = new List<GameObject>();
            for (int i = 1; i < fibulaSegments.Count - 1; i++)
            {
                if (fibulaSegments[i] != null)
                    interiorSegments.Add(fibulaSegments[i]);
            }

            
            interiorSegments.Sort((a, b) =>
            {
                float sizeA = GetSegmentBoundsSize(a);
                float sizeB = GetSegmentBoundsSize(b);
                return sizeB.CompareTo(sizeA);
            });

            
            int graftsToTake = Mathf.Min(expectedGrafts, interiorSegments.Count);
            for (int i = 0; i < graftsToTake; i++)
            {
                m_ActiveFibulaFragments.Add(interiorSegments[i]);
            }

            
            for (int i = graftsToTake; i < interiorSegments.Count; i++)
            {
                interiorSegments[i].SetActive(false);
            }

            
            m_ActiveFibulaFragments.Sort((a, b) =>
            {
                float projA = ProjectOntoAxis(a, fibulaPos, fibulaBoneAxis);
                float projB = ProjectOntoAxis(b, fibulaPos, fibulaBoneAxis);
                return projB.CompareTo(projA);
            });

            Debug.Log($"PerformFibulaSlice: {interiorSegments.Count} interior segments, selected {m_ActiveFibulaFragments.Count} grafts (expected {expectedGrafts})");

            
            MapFibulaGraftsToMandible(mPlanes, fibulaBoneAxis, fibulaRot);

            Debug.Log($"Fibula sliced: {fibulaSegments.Count} total, {m_ActiveFibulaFragments.Count} grafts mapped to mandible (expected {expectedGrafts}).");
        }

        public void PerformOsteotomySlice()
        {
            if (m_ActiveFragments.Count == 0 || m_FibulaModel == null) return;

            List<GameObject> currentPlanes = TouchInput.currentCuttingPlanes;
            List<GameObject> currentFibulaPlanes = TouchInput.fibulaPlanes;

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

            
            foreach (var graft in m_ActiveFibulaFragments) if (graft != null) Destroy(graft);
            m_ActiveFibulaFragments.Clear();
            if (m_FibulaModel != null) { Destroy(m_FibulaModel); m_FibulaModel = null; }

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

            
            if (m_InitialFibulaPrefab != null)
            {
                m_FibulaModel = Instantiate(m_InitialFibulaPrefab);
                m_FibulaModel.name = "Fibula";
                DontDestroyOnLoad(m_FibulaModel);
                SetupModelCommonRequirements(m_FibulaModel, "ROTATEONLY");
                PositionFibulaModel(m_FibulaModel, m_LoadedFragment.transform.position);
                m_FibulaModel.SetActive(true);
            }

            gizmoVisualizer.SetupAndCenterGizmo(m_LoadedFragment);
            gizmoVisualizer.SetGizmoVisibility(false);

            TouchInput.SetPlaneVisibility(true);
            TouchInput.SetRulerVisibility(true);
        }

        #endregion

        #region utils & positioning

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
            
            fibula.transform.localPosition = referencePosition + new Vector3(0.5f, -0.5f, 0f); 
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

            if (name.Contains("Fibula")) {
                fragment.transform.localScale = fibulaModelScale;
                SetupModelCommonRequirements(fragment, "ROTATEONLY");
            } else {
                fragment.transform.localScale = fragmentModelScale;
                SetupModelCommonRequirements(fragment, TouchInput.SPAWNABLE_TAG);
            }
            
            fragment.SetActive(true);
        }

        private float ProjectOntoAxis(GameObject obj, Vector3 origin, Vector3 axis)
        {
            MeshRenderer mr = obj.GetComponentInChildren<MeshRenderer>();
            Vector3 center = (mr != null) ? mr.bounds.center : obj.transform.position;
            return Vector3.Dot(center - origin, axis);
        }
        
        private float GetSegmentBoundsSize(GameObject obj)
        {
            MeshRenderer mr = obj.GetComponentInChildren<MeshRenderer>();
            return (mr != null) ? mr.bounds.size.magnitude : 0f;
        }
        
        private void MapFibulaGraftsToMandible(List<GameObject> mPlanes, Vector3 fibulaBoneAxis, Quaternion fibulaOriginalRot)
        {
            if (m_ActiveFibulaFragments.Count == 0 || mPlanes.Count < 2) return;

            var volumeCamera = Object.FindFirstObjectByType<VolumeCamera>();
            int numGrafts = mPlanes.Count - 1;
            int graftsToMap = Mathf.Min(m_ActiveFibulaFragments.Count, numGrafts);

            for (int i = 0; i < graftsToMap; i++)
            {
                GameObject graft = m_ActiveFibulaFragments[i];
                if (graft == null) continue;

                
                Vector3 segStart = mPlanes[i].transform.position;
                Vector3 segEnd   = mPlanes[i + 1].transform.position;
                Vector3 segCenter = (segStart + segEnd) / 2f;
                Vector3 segDir = (segEnd - segStart).normalized;

                
                
                
                Quaternion alignRot = Quaternion.FromToRotation(-fibulaBoneAxis, segDir);
                graft.transform.rotation = alignRot * fibulaOriginalRot;
                graft.transform.localScale = fibulaModelScale;

                
                MeshRenderer mr = graft.GetComponentInChildren<MeshRenderer>();
                if (mr != null)
                {
                    Vector3 boundsOffset = mr.bounds.center - graft.transform.position;
                    graft.transform.position = segCenter - boundsOffset;
                }
                else
                {
                    graft.transform.position = segCenter;
                }

                graft.transform.SetParent(volumeCamera?.transform, true);
                graft.tag = TouchInput.SPAWNABLE_TAG;
                graft.SetActive(true);

                Debug.Log($"Graft {i}: mapped to mandible segment [{i}→{i+1}], pos={graft.transform.position}");
            }
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
                col.enabled = false;
                col.enabled = true;
            }
        }
        #endregion
    }
}