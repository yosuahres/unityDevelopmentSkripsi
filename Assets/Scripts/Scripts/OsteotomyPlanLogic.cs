using System.IO;
using UnityEngine;

namespace Assets.Scripts.Scripts
{
    public class OsteotomyPlanLogic : MonoBehaviour
    {
        public Vector3 modelScale = new Vector3(0.001f, 0.001f, 0.001f);
        public float spawnDistance = 2.0f;
        // ---------------------

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
                Vector3 spawnPosition = Vector3.zero; 

                if (mainCamera != null)
                {
                    spawnPosition = mainCamera.transform.position + mainCamera.transform.forward * spawnDistance;
                }
                else
                {
                    Debug.LogWarning("Main Camera not found (is it tagged 'MainCamera'?). Spawning at (0,0,0).");
                }

                GameObject instance = Instantiate(prefab, spawnPosition, Quaternion.identity);
                instance.transform.localScale = modelScale;

                Debug.Log($"OsteotomyPlanLogic: Successfully instantiated '{cleanName}' at {spawnPosition} with scale {modelScale}.");
                
                // ---------------------
            }
            else
            {
                Debug.LogError($"OsteotomyPlanLogic: Could not find resource '{cleanName}' in Resources.");
            }
        }
    }
}