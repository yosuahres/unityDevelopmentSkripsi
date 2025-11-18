using System.Collections;
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

        private GameObject m_LoadedFragment;

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

            StartCoroutine(SetupColliderTwice(m_LoadedFragment));
            m_LoadedFragment.SetActive(true);
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
            Debug.Log($"OsteotomyPlanLogic: Set convex mesh collider on '{modelInstance.name}'.");
        }
    }
}
