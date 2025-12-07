using UnityEngine;
namespace Assets.Scripts.Scripts
{
    public class GizmoVisualizer : MonoBehaviour
    {
        
        [Header("Gizmo Setup (Z-Axis Ring)")]
        public GameObject gizmoPrefabZ;
        
        public Color gizmoColorZ = new Color(1.0f, 0f, 0f, 0.5f);
        public float gizmoZRotation = 90f;

        [Header("Gizmo Setup (X-Axis Ring)")]
        public GameObject gizmoPrefabX;
        
        public Color gizmoColorX = new Color(0f, 1.0f, 0f, 0.5f);
        public float gizmoXRotation = 0f;

        [Header("Common Gizmo Settings")]
        public float gizmoScaleFactor = 100f;

        [Header("Z-Axis Ring Specific Offsets")]
        public float gizmoViewOffsetX_ZAxis = 0f; 
        public float gizmoViewOffsetY_ZAxis = 0f; 
        public float gizmoViewOffsetZ_ZAxis = 0f; 

        [Header("X-Axis Ring Specific Offsets")]
        public float gizmoViewOffsetX_XAxis = 0f; 
        public float gizmoViewOffsetY_XAxis = 0f; 
        public float gizmoViewOffsetZ_XAxis = 0f; 

        private GameObject m_LoadedGizmoZ;
        private GameObject m_LoadedGizmoX;
        
        
        private void InitializeAndPositionGizmo(GameObject gizmoPrefab, ref GameObject loadedGizmoInstance, GameObject fragment, Color color, float rotationAngle, string rotationAxis, float offsetX, float offsetY, float offsetZ)
        {
            if (gizmoPrefab == null)
            {
                Debug.LogWarning($"Gizmo Prefab for {rotationAxis}-Axis is not assigned. Skipping gizmo setup.");
                return;
            }

            if (loadedGizmoInstance == null)
            {
                loadedGizmoInstance = Instantiate(gizmoPrefab);
            }

            
            MeshRenderer gizmoRenderer = loadedGizmoInstance.GetComponentInChildren<MeshRenderer>();
            if (gizmoRenderer != null && gizmoRenderer.material != null)
            {
                Material mat = gizmoRenderer.material;
                if (mat.HasProperty("_BaseColor"))
                {
                    
                    mat.SetColor("_BaseColor", color);
                }
                else if (mat.HasProperty("_Color"))
                {
                    
                    mat.SetColor("_Color", color);
                }
                else
                {
                    Debug.LogWarning("Gizmo material does not have a recognized color property (_BaseColor or _Color). Color change may fail.");
                }
            }
            else
            {
                Debug.LogWarning("Gizmo model is missing a MeshRenderer or Material. Cannot set color.");
            }
            

            MeshRenderer fragmentRenderer = fragment.GetComponent<MeshRenderer>();
            Vector3 finalLocalPosition;

            if (fragmentRenderer != null)
            {
                
                Vector3 worldCenter = fragmentRenderer.bounds.center;

                
                Vector3 localCenterOffset = fragment.transform.InverseTransformPoint(worldCenter);

                
                finalLocalPosition = localCenterOffset + new Vector3(offsetX, offsetY, offsetZ);
            }
            else
            {
                
                finalLocalPosition = new Vector3(offsetX, offsetY, offsetZ);
                Debug.LogError("Fragment model is missing a MeshRenderer. Gizmo using pivot fallback.");
            }

            
            loadedGizmoInstance.transform.SetParent(fragment.transform, false);
            loadedGizmoInstance.transform.localPosition = finalLocalPosition;
            loadedGizmoInstance.transform.localScale = Vector3.one * gizmoScaleFactor;

            
            loadedGizmoInstance.transform.localRotation = Quaternion.identity;

            switch (rotationAxis.ToUpper())
            {
                case "Z":
                    loadedGizmoInstance.transform.Rotate(0, 0, rotationAngle, Space.Self);
                    break;
                case "X":
                    loadedGizmoInstance.transform.Rotate(rotationAngle, 0, 0, Space.Self);
                    break;
                default:
                    break;
            }

            loadedGizmoInstance.SetActive(true);
        }
        
        public void SetupAndCenterGizmo(GameObject fragment)
        {
            if (fragment == null)
            {
                Debug.LogError("GizmoVisualizer: Fragment model is null. Cannot center gizmos.");
                return;
            }

            
            InitializeAndPositionGizmo(
                gizmoPrefabZ,
                ref m_LoadedGizmoZ,
                fragment,
                gizmoColorZ,
                gizmoZRotation,
                "Z",
                gizmoViewOffsetX_ZAxis,
                gizmoViewOffsetY_ZAxis, 
                gizmoViewOffsetZ_ZAxis
            );

            
            InitializeAndPositionGizmo(
                gizmoPrefabX,
                ref m_LoadedGizmoX,
                fragment,
                gizmoColorX,
                gizmoXRotation,
                "X",
                gizmoViewOffsetX_XAxis,
                gizmoViewOffsetY_XAxis, 
                gizmoViewOffsetZ_XAxis
            );
        }

        
        
        
        
        [UnityEngine.Scripting.Preserve]
        public void SetGizmoVisibility(bool isVisible)
        {
            if (m_LoadedGizmoZ != null)
            {
                m_LoadedGizmoZ.SetActive(isVisible);
                Debug.Log($"GizmoVisualizer: SetGizmoVisibility Z set to {isVisible}");
            }
            if (m_LoadedGizmoX != null)
            {
                m_LoadedGizmoX.SetActive(isVisible);
                Debug.Log($"GizmoVisualizer: SetGizmoVisibility X set to {isVisible}");
            }
        }
    }
}