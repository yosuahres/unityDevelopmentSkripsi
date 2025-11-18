using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.PolySpatial; 

namespace Assets.Scripts.Scripts
{
    public class GUISlicing : MonoBehaviour
    {
        public Material sliceCapMaterial;

        public GameObject loadedInstance; 

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

            AddRequiredComponents(loadedInstance);
            yield return StartCoroutine(SliceModelAtCenter(loadedInstance));
            Debug.Log($"GUISlicing: Model setup and slicing coroutine complete for '{loadedInstance.name}'.");
        }

        private IEnumerator SliceModelAtCenter(GameObject modelInstance)
        {
            yield return new WaitForEndOfFrame();
            Slice sliceComponent = modelInstance.GetComponent<Slice>();
            MeshRenderer renderer = modelInstance.GetComponent<MeshRenderer>();

            try
            {
                Vector3 sliceNormal = modelInstance.transform.right;
                Vector3 sliceOrigin = renderer.bounds.center;

                sliceComponent.OnSliceFinished = (fragA, fragB) =>
                {
                    if (fragA == null || fragB == null)
                    {
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
                };
                
                sliceComponent.SliceAtCenter(sliceNormal);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception: {e.Message}");
                Debug.LogException(e);
            }
        }

        private void AddRequiredComponents(GameObject modelInstance)
        {
            if (modelInstance == null) return;
            
            if (modelInstance.GetComponent<MeshFilter>() == null)
                modelInstance.AddComponent<MeshFilter>();
            
            MeshRenderer renderer = modelInstance.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = modelInstance.AddComponent<MeshRenderer>();
            
            if (renderer.sharedMaterial == null)
            {
                renderer.sharedMaterial = sliceCapMaterial; 
            }

            BoxCollider collider = modelInstance.GetComponent<BoxCollider>();
            if (collider == null)
                collider = modelInstance.AddComponent<BoxCollider>();
            
            Slice sliceComponent = modelInstance.GetComponent<Slice>();
            if (sliceComponent == null)
                sliceComponent = modelInstance.AddComponent<Slice>();

            if (sliceComponent.sliceOptions == null)
            {
                sliceComponent.sliceOptions = new SliceOptions();
                sliceComponent.sliceOptions.detectFloatingFragments = false; 
            }
            
            if (sliceCapMaterial == null)
            {
                return;
            }

            sliceComponent.sliceOptions.insideMaterial = sliceCapMaterial; 
        }

        public GameObject GetRightFragment()
        {
            if (rightFragment == null)
            {
                Debug.LogWarning("RightFragment is null.");
            }
            return rightFragment;
        }

        public GameObject GetLeftFragment()
        {
            if (leftFragment == null)
            {
                Debug.LogWarning("LeftFragment is null.");
            }
            return leftFragment;
        }
    }
}
