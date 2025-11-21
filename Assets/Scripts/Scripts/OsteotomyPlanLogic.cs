using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using Unity.PolySpatial; 

namespace Assets.Scripts.Scripts
{
    public class OsteotomyPlanLogic : MonoBehaviour
    {
        [Header("Model Setup")]
        public Vector3 modelScale = new Vector3(0.001f, 0.001f, 0.001f);
        public float spawnDistance = 2.0f;
        public float spawnHeight = 1.5f; 

        [Header("Slicing Setup")]
        public Material osteotomySliceCapMaterial;
        public SliceOptions osteotomySliceOptions;
        public CallbackOptions osteotomyCallbackOptions; 

        private GameObject m_LoadedFragment;
        private List<GameObject> m_ActiveFragments = new List<GameObject>();

        void Start()
        {
            if (DataManager.Instance == null || DataManager.Instance.SelectedFragment == null)
            {
                Debug.LogError("OsteotomyPlanLogic: DataManager or SelectedFragment not found! Cannot load model.");
                return;
            }

            m_LoadedFragment = DataManager.Instance.SelectedFragment;
            DataManager.Instance.SelectedFragment = null;
            Debug.Log($"OsteotomyPlanLogic: Successfully retrieved fragment '{m_LoadedFragment.name}'.");

            PositionFragment(m_LoadedFragment);
            
            if (m_LoadedFragment.GetComponent<TouchableObject>() == null)
            {
                m_LoadedFragment.AddComponent<TouchableObject>();
                Debug.Log($"OsteotomyPlanLogic: Added TouchableObject component to '{m_LoadedFragment.name}'.");
            }

            StartCoroutine(SetupColliderTwice(m_LoadedFragment));
            m_LoadedFragment.SetActive(true);
            
            m_ActiveFragments.Add(m_LoadedFragment); 
        }

        private void PositionFragment(GameObject fragment)
        {
            Camera mainCamera = Camera.main;
            var volumeCamera = Object.FindObjectOfType<VolumeCamera>();

            if (volumeCamera != null)
            {
                fragment.transform.SetParent(volumeCamera.transform, false); 
                fragment.transform.localPosition = new Vector3(0, spawnHeight, spawnDistance); 
                fragment.transform.localScale = modelScale;
                
                if (mainCamera != null)
                {
                    fragment.transform.LookAt(mainCamera.transform, mainCamera.transform.up);
                }
                
                if (fragment.name.Contains("Left"))
                {
                    fragment.transform.Rotate(0, 90, 0, Space.Self);
                }
                else if (fragment.name.Contains("Right"))
                {
                    fragment.transform.Rotate(0, -90, 0, Space.Self);
                }
            }
            else
            {
                Vector3 spawnPosition = Vector3.zero;
                if (mainCamera != null)
                {
                    Vector3 localSpawnOffset = new Vector3(0, spawnHeight, spawnDistance);
                    spawnPosition = mainCamera.transform.TransformPoint(localSpawnOffset);
                }
                else
                {
                    Debug.LogWarning("Main Camera not found. Spawning at world origin + offset.");
                    spawnPosition = new Vector3(0, spawnHeight, spawnDistance);
                }

                fragment.transform.SetParent(null); 
                fragment.transform.position = spawnPosition;
                fragment.transform.localScale = modelScale;
                
                if (mainCamera != null)
                {
                    fragment.transform.LookAt(mainCamera.transform, mainCamera.transform.up);
                }

                if (fragment.name.Contains("Left"))
                {
                    fragment.transform.Rotate(0, 90, 0, Space.Self);
                }
                else if (fragment.name.Contains("Right"))
                {
                    fragment.transform.Rotate(0, -90, 0, Space.Self);
                }
            }
        }

        private IEnumerator SetupColliderTwice(GameObject modelInstance)
        {
            SetupModelCollider(modelInstance);
            yield return new WaitForEndOfFrame();
            SetupModelCollider(modelInstance);
        }
        
        private void SetupModelCollider(GameObject modelInstance)
        {
            if (modelInstance == null) return;

            BoxCollider collider = modelInstance.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = modelInstance.AddComponent<BoxCollider>();
            }
            Debug.Log($"OsteotomyPlanLogic: Set Box Collider on '{modelInstance.name}'.");
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
            if (currentPlanes == null || currentPlanes.Count == 0)
            {
                Debug.LogWarning("OsteotomyPlanLogic: No cutting planes found in TouchInput.");
                return;
            }

            List<GameObject> currentSetOfFragments = new List<GameObject>(m_ActiveFragments);
            
            foreach (GameObject plane in currentPlanes)
            {
                List<GameObject> nextSetOfFragments = new List<GameObject>();
                foreach (GameObject fragmentToSlice in currentSetOfFragments)
                {
                    if (fragmentToSlice == null || !fragmentToSlice.activeSelf) continue;

                    Debug.Log($"OsteotomyPlanLogic: Slicing fragment '{fragmentToSlice.name}' with plane from TouchInput.");
                    
                    GameObject[] results = new GameObject[2];
                    
                    AddSliceComponents(fragmentToSlice, plane.transform.position, plane.transform.right, (fragA, fragB) =>
                    {
                        Vector3 sliceOrigin = plane.transform.position; 
                        Vector3 sliceNormal = plane.transform.right; 

                        GameObject keptFragment = null;

                        MeshRenderer rendererA = fragA?.GetComponent<MeshRenderer>();
                        MeshRenderer rendererB = fragB?.GetComponent<MeshRenderer>();

                        if (fragA != null && rendererA != null)
                        {
                            Vector3 dirToA = rendererA.bounds.center - sliceOrigin;
                            if (Vector3.Dot(dirToA, sliceNormal) > 0) 
                            {
                                keptFragment = fragA;
                            }
                        }
                        
                        if (keptFragment == null && fragB != null && rendererB != null)
                        {
                            Vector3 dirToB = rendererB.bounds.center - sliceOrigin;
                            if (Vector3.Dot(dirToB, sliceNormal) > 0) 
                            {
                                keptFragment = fragB;
                            }
                        }

                        if (fragA != null) nextSetOfFragments.Add(fragA);
                        if (fragB != null) nextSetOfFragments.Add(fragB);
                    });
                }
                currentSetOfFragments = new List<GameObject>(nextSetOfFragments);
            }
            m_ActiveFragments = currentSetOfFragments;

            if (m_ActiveFragments.Count > 0 && currentPlanes.Count > 1) 
            {
                float minPlaneX = float.MaxValue;
                float maxPlaneX = float.MinValue;
                foreach (GameObject plane in currentPlanes)
                {
                    minPlaneX = Mathf.Min(minPlaneX, plane.transform.position.x);
                    maxPlaneX = Mathf.Max(maxPlaneX, plane.transform.position.x);
                }

                List<GameObject> finalKeptFragments = new List<GameObject>();
                foreach (GameObject fragment in m_ActiveFragments)
                {
                    if (fragment == null) continue;

                    MeshRenderer fragmentRenderer = fragment.GetComponent<MeshRenderer>();
                    if (fragmentRenderer == null)
                    {
                        Debug.LogWarning($"OsteotomyPlanLogic: Fragment '{fragment.name}' has no MeshRenderer. Cannot determine bounds for filtering.");
                        finalKeptFragments.Add(fragment); 
                        continue;
                    }

                    if (fragmentRenderer.bounds.center.x < minPlaneX || fragmentRenderer.bounds.center.x > maxPlaneX)
                    {
                        finalKeptFragments.Add(fragment);
                        fragment.SetActive(true);
                    }
                    else
                    {
                        fragment.SetActive(false);
                        Debug.Log($"OsteotomyPlanLogic: Deactivating fragment '{fragment.name}' (between planes).");
                    }
                }
                m_ActiveFragments = finalKeptFragments;
            } else {
                foreach (GameObject fragment in m_ActiveFragments)
                {
                    if (fragment != null) fragment.SetActive(true);
                }
            }

            Debug.Log($"OsteotomyPlanLogic: Slicing complete. Total fragments remaining: {m_ActiveFragments.Count}");
            TouchInput.ClearPlaneList();
        }

        private void AddSliceComponents(GameObject targetModel, Vector3 sliceOriginWorld, Vector3 sliceNormalWorld, System.Action<GameObject, GameObject> onSliceFinishedCallback)
        {
            if (targetModel == null) return;
            
            if (targetModel.GetComponent<MeshFilter>() == null)
                targetModel.AddComponent<MeshFilter>();
            
            MeshRenderer renderer = targetModel.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = targetModel.AddComponent<MeshRenderer>();
            
            if (renderer.sharedMaterial == null)
            {
                renderer.sharedMaterial = osteotomySliceCapMaterial; 
            }

            BoxCollider collider = targetModel.GetComponent<BoxCollider>();
            if (collider == null)
                collider = targetModel.AddComponent<BoxCollider>();
            
            Slice sliceComponent = targetModel.GetComponent<Slice>();
            if (sliceComponent != null) 
            {
                Destroy(sliceComponent);
            }
            sliceComponent = targetModel.AddComponent<Slice>(); 
            if (osteotomySliceOptions == null)
            {
                osteotomySliceOptions = new SliceOptions();
                Debug.LogWarning("OsteotomyPlanLogic: osteotomySliceOptions was null, creating default.");
            }
            sliceComponent.sliceOptions = osteotomySliceOptions;
            
            if (osteotomySliceCapMaterial == null)
            {
                Debug.LogWarning("OsteotomyPlanLogic: osteotomySliceCapMaterial is not assigned. Slice cap will be default.");
            }
            sliceComponent.sliceOptions.insideMaterial = osteotomySliceCapMaterial; 
            
            if (osteotomyCallbackOptions == null)
            {
                osteotomyCallbackOptions = new CallbackOptions();
                Debug.LogWarning("OsteotomyPlanLogic: osteotomyCallbackOptions was null, creating default.");
            }
            sliceComponent.callbackOptions = osteotomyCallbackOptions;

            sliceComponent.OnSliceFinished = (fragA, fragB) =>
            {
                Debug.Log($"OsteotomyPlanLogic: Slice finished for '{targetModel.name}'. Fragments A: {(fragA != null ? fragA.name : "null")}, B: {(fragB != null ? fragB.name : "null")}");
                
                HandleNewFragment(fragA, targetModel.name + "_Ost_A");
                HandleNewFragment(fragB, targetModel.name + "_Ost_B");
                
                onSliceFinishedCallback?.Invoke(fragA, fragB);
            };
            
            // Perform the slice
            sliceComponent.ComputeSlice(sliceNormalWorld, sliceOriginWorld);
            targetModel.SetActive(false); 
        }

        private void HandleNewFragment(GameObject fragment, string name)
        {
            if (fragment == null) return;

            fragment.name = name;
            
            if (fragment.GetComponent<TouchableObject>() == null)
            {
                fragment.AddComponent<TouchableObject>();
            }
            
            BoxCollider collider = fragment.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = fragment.AddComponent<BoxCollider>();
            }
            
            fragment.SetActive(true);
        }
    }
}
