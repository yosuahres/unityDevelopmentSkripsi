// GUISlicing.cs
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.PolySpatial; 

namespace Assets.Scripts.Scripts
{
    public class GUISlicing : MonoBehaviour
    {
        public GameObject loadedInstance; 

        [Header("Slice Results")]
        public GameObject leftFragment;
        public GameObject rightFragment;
        
        public void LoadModelByName(string modelName)
        {
            string cleanName = Path.GetFileNameWithoutExtension(modelName);
            var prefab = Resources.Load<GameObject>(cleanName);
            
            if (prefab != null)
            {
                StartCoroutine(LoadAndSliceModel(prefab));
            }
            else
            {
                Debug.LogError($"GUISlicing: Could not find resource '{cleanName}' in Resources.");
            }
        }

        private IEnumerator LoadAndSliceModel(GameObject prefab)
        {
            loadedInstance = Instantiate(prefab);
            Debug.Log($"GUISlicing: Successfully instantiated '{prefab.name}'.");

            yield return StartCoroutine(SetupComponents(loadedInstance));
            
            yield return StartCoroutine(SliceModelAtCenter(loadedInstance));
            
            Debug.Log($"GUISlicing: Model setup and slicing complete for '{loadedInstance.name}'.");
        }

        private IEnumerator SetupComponents(GameObject modelInstance)
        {
            AddRequiredComponents(modelInstance);
            yield return new WaitForEndOfFrame();
            AddRequiredComponents(modelInstance);
            
            Debug.Log($"GUISlicing: Components successfully added to '{modelInstance.name}'.");
        }

        private IEnumerator SliceModelAtCenter(GameObject modelInstance)
        {
            yield return new WaitForEndOfFrame();

            Slice sliceComponent = modelInstance.GetComponent<Slice>();
            if (sliceComponent != null)
            {
                Vector3 sliceNormal = modelInstance.transform.right;
                Vector3 sliceOrigin = modelInstance.GetComponent<MeshRenderer>().bounds.center;

                sliceComponent.OnSliceFinished = (fragA, fragB) =>
                {
                    if (fragA == null || fragB == null)
                    {
                        Debug.LogError("Slice event did not return two fragments.");
                        return;
                    }

                    Vector3 dirToA = fragA.GetComponent<MeshRenderer>().bounds.center - sliceOrigin;
                    
                    if (Vector3.Dot(dirToA, sliceNormal) > 0)
                    {
                        rightFragment = fragA;
                        leftFragment = fragB;
                    }
                    else
                    {
                        leftFragment = fragA;
                        rightFragment = fragB;
                    }

                    leftFragment.name = $"{modelInstance.name}_Left";
                    rightFragment.name = $"{modelInstance.name}_Right";
                    
                    Debug.Log($"GUISlicing: Captured fragments. Left: {leftFragment.name}, Right: {rightFragment.name}");
                };
                
                sliceComponent.SliceAtCenter(sliceNormal);
                Debug.Log($"GUISlicing: Model slice initiated at center for '{modelInstance.name}'.");
            }
            else
            {
                Debug.LogError($"GUISlicing: Could not find Slice component on '{modelInstance.name}'! Slicing failed.");
            }
        }

        private void AddRequiredComponents(GameObject modelInstance)
        {
            if (modelInstance == null) return;
            if (modelInstance.GetComponent<MeshFilter>() == null)
                modelInstance.AddComponent<MeshFilter>();
            if (modelInstance.GetComponent<MeshRenderer>() == null)
                modelInstance.AddComponent<MeshRenderer>();
            MeshCollider collider = modelInstance.GetComponent<MeshCollider>();
            if (collider == null)
                collider = modelInstance.AddComponent<MeshCollider>();
            collider.convex = false;  
            if (modelInstance.GetComponent<Slice>() == null)
                modelInstance.AddComponent<Slice>();
        }

        public GameObject GetRightFragment()
        {
            if (rightFragment == null)
            {
                Debug.LogWarning("GetRightFragment() was called, but rightFragment is null.");
            }
            return rightFragment;
        }

        public GameObject GetLeftFragment()
        {
            if (leftFragment == null)
            {
                Debug.LogWarning("GetLeftFragment() was called, but leftFragment is null.");
            }
            return leftFragment;
        }
    }
}