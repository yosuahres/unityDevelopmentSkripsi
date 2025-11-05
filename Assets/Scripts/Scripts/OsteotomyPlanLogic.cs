using System.IO;
using UnityEngine;
using Unity.PolySpatial; 

namespace Assets.Scripts.Scripts
{
    public class OsteotomyPlanLogic : MonoBehaviour
    {
        public Vector3 modelScale = new Vector3(0.001f, 0.001f, 0.001f);
        public float spawnDistance = 2.0f;

        public void LoadModelByName(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                Debug.LogError("LoadModelByName called with empty name.");
                return;
            }

            string cleanName = Path.GetFileNameWithoutExtension(modelName);
            Debug.Log($"OsteotomyPlanLogic: Attempting to load resource '{cleanName}' from Resources.");

            var prefab = Resources.Load<GameObject>(cleanName);
            if (prefab != null)
            {
                Camera mainCamera = Camera.main;
                var volumeCamera = Object.FindObjectOfType<VolumeCamera>();

                if (volumeCamera != null)
                {
                    GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    
                    SetupModelCollider(instance);
                    
                    instance.transform.SetParent(volumeCamera.transform, false); 
                    instance.transform.localPosition = Vector3.forward * spawnDistance;
                    instance.transform.localScale = modelScale;

                    if (mainCamera != null)
                    {
                        instance.transform.LookAt(mainCamera.transform, mainCamera.transform.up);
                    }
                    Debug.Log($"OsteotomyPlanLogic: Instantiated '{cleanName}' as child of VolumeCamera.");
                }
                else
                {
                    Vector3 spawnPosition = Vector3.zero;
                    if (mainCamera != null)
                    {
                        spawnPosition = mainCamera.transform.TransformPoint(Vector3.forward * spawnDistance);
                    }
                    else
                    {
                        Debug.LogWarning("Main Camera not found. Spawning at origin.");
                    }

                    GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity);

                    SetupModelCollider(instance);

                    instance.transform.localScale = modelScale;

                    if (mainCamera != null)
                    {
                        instance.transform.LookAt(mainCamera.transform, mainCamera.transform.up);
                    }
                    Debug.Log($"OsteotomyPlanLogic: Successfully instantiated '{cleanName}' at {spawnPosition}.");
                }
            }
            else
            {
                Debug.LogError($"OsteotomyPlanLogic: Could not find resource '{cleanName}' in Resources.");
            }
        }
        
        private void SetupModelCollider(GameObject modelInstance)
        {
            if (modelInstance == null) return;

            // MeshCollider
            MeshCollider collider = modelInstance.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = modelInstance.AddComponent<MeshCollider>();
            }
            collider.convex = false; 

            Rigidbody rb = modelInstance.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = modelInstance.AddComponent<Rigidbody>();
            }
            rb.useGravity = false;
            rb.isKinematic = true; 
        }
    }
}