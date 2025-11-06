    using System.IO;
    using UnityEngine;
    using Unity.PolySpatial; 

    namespace Assets.Scripts.Scripts
    {
        public class OsteotomyPlanLogic : MonoBehaviour, ISpatialTouchable
    {
            //MODIFY THIS ON THE INSPECTOR IDIOTS
            [Header("Model Settings")]
            public Vector3 modelScale = new Vector3(0.001f, 0.001f, 0.001f);
            public float spawnDistance = 1.5f; 
            
            [Tooltip("The height above the floor (Y=0) to spawn the model.")]
            public float spawnHeight = 1.6f; 

            [Header("Osteotomy Settings")]
            public GameObject planeFragmentPrefab;

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

                    Vector3 camPos = mainCamera.transform.position;
                    Vector3 camPosOnFloor = new Vector3(camPos.x, 0, camPos.z);

                    Vector3 camForward = mainCamera.transform.forward;
                    camForward.y = 0; 
                    camForward.Normalize();

                    Vector3 spawnPosition = camPosOnFloor + camForward * spawnDistance;
                    
                    spawnPosition.y = spawnHeight; 

                    GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity);
                    SetupModelCollider(instance);

                    instance.transform.localScale = modelScale;

                    Vector3 lookAtPosition = new Vector3(mainCamera.transform.position.x, instance.transform.position.y, mainCamera.transform.position.z);
                    instance.transform.LookAt(lookAtPosition);
                    
                    //if you want to flip the model
                    // instance.transform.Rotate(0, 180, 0, Space.Self);
                    
                    Debug.Log($"OsteotomyPlanLogic: Successfully instantiated '{cleanName}' at {spawnPosition}.");
                }
                else
                {
                    Debug.LogError($"OsteotomyPlanLogic: Could not find resource '{cleanName}' in Resources.");
                }
            }
            
            private void SetupModelCollider(GameObject modelInstance)
            {
                if (modelInstance == null) return;

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

            // spawn plane based on TouchIInput.cs data
            public void OnSpatialTouch(Vector3 touchPosition, Vector3 touchNormal)
            {
                Vector3 spawnPosition = touchPosition;
                Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, touchNormal);

                Instantiate(planeFragmentPrefab, spawnPosition, spawnRotation);
                Debug.Log($"Spawned plane at {spawnPosition}");
            }
        }
    }