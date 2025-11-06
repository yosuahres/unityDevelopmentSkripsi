//main logic
using System.IO;
using UnityEngine;
using Unity.PolySpatial;

namespace Assets.Scripts.Scripts
{
    public class OsteotomyPlanLogic : MonoBehaviour, ISpatialTouchable
    {
        [Header("Model Settings")]
        public Vector3 modelScale = new Vector3(0.001f, 0.001f, 0.001f);
        public float spawnDistance = 1.5f;

        [Tooltip("The height above the floor (Y=0) to spawn the model.")]
        public float spawnHeight = 2.2f;

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
                //spawn logic
                Camera mainCamera = Camera.main;
                Vector3 camPos = mainCamera.transform.position;
                Vector3 camPosOnFloor = new Vector3(camPos.x, 0, camPos.z);

                Vector3 camForward = mainCamera.transform.forward;
                camForward.y = 0;
                camForward.Normalize();

                Vector3 spawnPosition = camPosOnFloor + camForward * spawnDistance;
                spawnPosition.y = spawnHeight;
                GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity);

                //setup collider - Removed call, assuming prefab already has components.
                // SetupModelCollider(instance);

                // set scale
                instance.transform.localScale = modelScale;

                // make it face the camera
                Vector3 lookAtPosition = new Vector3(mainCamera.transform.position.x, instance.transform.position.y, mainCamera.transform.position.z);
                instance.transform.LookAt(lookAtPosition);

                //if you want to flip the model
                // instance.transform.Rotate(0, 180, 0, Space.Self);

                Debug.Log($"OsteotomyPlanLogic: Successfully instantiated '{cleanName}' at {spawnPosition}.");


                // slice logic
                // Component requirements (MeshFilter, Rigidbody, etc.)
                // are now assumed to be set up on the prefab itself.


            }
            else
            {
                Debug.LogError($"OsteotomyPlanLogic: Could not find resource '{cleanName}' in Resources.");
            }
        }

        /* --- Removed SetupModelCollider method ---
           Assuming the prefab/model GameObject already has the
           necessary MeshCollider and Rigidbody components configured.

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
        */

        public void OnSpatialTouch(Vector3 touchPosition, Vector3 touchNormal)
        {
            Debug.Log("OsteotomyPlanLogic.OnSpatialTouch received (spawning handled by TouchInput).");
        }
    }
}