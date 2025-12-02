//guislicing.cs
using System.Collections;
using System.IO;
using UnityEngine;
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
            Debug.Log($"GUISlicing: Instantiated '{prefab.name}'.");
            DontDestroyOnLoad(loadedInstance); 
            Debug.Log($"GUISlicing: Called DontDestroyOnLoad for '{loadedInstance.name}'.");

            if (DataManager.Instance != null)
            {
                DataManager.Instance.WholeModel = loadedInstance;
                Debug.Log($"GUISlicing: Set DataManager.Instance.WholeModel to '{loadedInstance.name}'.");
            }

            yield return StartCoroutine(AddRequiredComponents(loadedInstance));
            yield return StartCoroutine(ForceConvexMeshCollider(loadedInstance));
            yield return StartCoroutine(SliceModelAtCenter(loadedInstance));

            Debug.Log($"GUISlicing: Model ready and sliced.");
        }

        private IEnumerator ForceConvexMeshCollider(GameObject model)
        {
            MeshFilter mf = model.GetComponent<MeshFilter>();

            while (mf == null || mf.sharedMesh == null)
            {
                yield return null;
                mf = model.GetComponent<MeshFilter>();
            }

            MeshCollider col = model.GetComponent<MeshCollider>();
            if (col == null)
                col = model.AddComponent<MeshCollider>();

            const int maxAttempts = 5;

            for (int i = 0; i < maxAttempts; i++)
            {
                col.sharedMesh = null;
                yield return null;

                col.sharedMesh = mf.sharedMesh;
                col.convex = true;

                if (col.sharedMesh != null && col.convex)
                {
                    Debug.Log($"Convex MeshCollider SUCCESS after {i + 1} attempts.");
                    yield break;
                }

                Debug.LogWarning($"Convex MeshCollider attempt {i + 1} failed. Retrying...");
                yield return null;
            }

            Debug.LogError("Convex MeshCollider FAILED after all retries.");
        }

        private IEnumerator AddRequiredComponents(GameObject modelInstance)
        {
            if (modelInstance == null) yield break;

            MeshFilter mf = modelInstance.GetComponent<MeshFilter>();
            if (mf == null)
                mf = modelInstance.AddComponent<MeshFilter>();

            MeshRenderer renderer = modelInstance.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = modelInstance.AddComponent<MeshRenderer>();

            if (renderer.sharedMaterial == null && sliceCapMaterial != null)
                renderer.sharedMaterial = sliceCapMaterial;

            Slice slice = modelInstance.GetComponent<Slice>();
            if (slice == null)
                slice = modelInstance.AddComponent<Slice>();

            if (slice.sliceOptions == null)
                slice.sliceOptions = new SliceOptions();

            slice.sliceOptions.detectFloatingFragments = false;

            if (sliceCapMaterial != null)
                slice.sliceOptions.insideMaterial = sliceCapMaterial;

            yield return null;
        }

        private IEnumerator SliceModelAtCenter(GameObject modelInstance)
        {
            yield return new WaitForEndOfFrame();

            Slice sliceComponent = modelInstance.GetComponent<Slice>();
            MeshRenderer renderer = modelInstance.GetComponent<MeshRenderer>();

            Vector3 sliceNormal = modelInstance.transform.right;
            Vector3 sliceOrigin = renderer.bounds.center;

            sliceComponent.OnSliceFinished = (fragA, fragB) =>
            {
                if (fragA == null || fragB == null)
                    return;

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
                leftFragment.tag = "SPAWNABLE";
                rightFragment.tag = "SPAWNABLE";
                Debug.Log($"GUISlicing: Tagged Left Fragment '{leftFragment.name}' as '{leftFragment.tag}'");
                Debug.Log($"GUISlicing: Tagged Right Fragment '{rightFragment.name}' as '{rightFragment.tag}'");
            };

            sliceComponent.SliceAtCenter(sliceNormal);
        }

        public GameObject GetRightFragment()
        {
            if (rightFragment == null)
                Debug.LogWarning("RightFragment is null.");
            return rightFragment;
        }

        public GameObject GetLeftFragment()
        {
            if (leftFragment == null)
                Debug.LogWarning("LeftFragment is null.");
            return leftFragment;
        }
    }
}
