// Slice.cs
using UnityEngine;
using UnityEngine.Events;
using System; 

public class Slice : MonoBehaviour
{
    public SliceOptions sliceOptions;
    public CallbackOptions callbackOptions;
    public Action<GameObject, GameObject> OnSliceFinished;

    private int currentSliceCount;

    public void SliceAtCenter(Vector3 sliceNormalWorld)
    {
        MeshRenderer renderer = this.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError("SliceAtCenter: Cannot slice without a MeshRenderer to find bounds.");
            return;
        }
        Vector3 sliceOriginWorld = renderer.bounds.center;
        Debug.Log($"SliceAtCenter: Slicing at origin {sliceOriginWorld} with normal {sliceNormalWorld}");
        ComputeSlice(sliceNormalWorld, sliceOriginWorld);
    }
    
    public void ComputeSlice(Vector3 sliceNormalWorld, Vector3 sliceOriginWorld)
    {
        var mesh = this.GetComponent<MeshFilter>().sharedMesh;

        if (mesh != null)
        {
            var sliceTemplate = CreateSliceTemplate();
            var sliceNormalLocal = this.transform.InverseTransformDirection(sliceNormalWorld);
            var sliceOriginLocal = this.transform.InverseTransformPoint(sliceOriginWorld);

            GameObject[] fragments = Fragmenter.Slice(this.gameObject,
                                     sliceNormalLocal,
                                     sliceOriginLocal,
                                     this.sliceOptions,
                                     sliceTemplate,
                                     this.transform.parent); 
                    
            GameObject.Destroy(sliceTemplate);

            this.gameObject.SetActive(false); 

            if (fragments != null && fragments.Length >= 2)
            {
                OnSliceFinished?.Invoke(fragments[0], fragments[1]);
            }
            else if (fragments != null && fragments.Length > 0)
            {
                Debug.LogWarning("Slice returned only one fragment.");
                OnSliceFinished?.Invoke(fragments[0], null);
            }
            else
            {
                Debug.LogError("Fragmenter.Slice did not return any fragments.");
            }

            if (callbackOptions.onCompleted != null)
            {
                callbackOptions.onCompleted.Invoke();
            }
        }
    }
    
    private GameObject CreateSliceTemplate()
    {
        GameObject obj = new GameObject();
        obj.name = "Slice";
        obj.tag = this.tag;

        obj.AddComponent<MeshFilter>();

        var meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = new Material[2] {
            this.GetComponent<MeshRenderer>().sharedMaterial,
            this.sliceOptions.insideMaterial
        };

        var thisCollider = this.GetComponent<Collider>();
        var fragmentCollider = obj.AddComponent<MeshCollider>();
        fragmentCollider.convex = true;
        
        if (thisCollider != null)
        {
            fragmentCollider.sharedMaterial = thisCollider.sharedMaterial;
            fragmentCollider.isTrigger = thisCollider.isTrigger;
        }
    
        if (this.sliceOptions.enableReslicing &&
           (this.currentSliceCount < this.sliceOptions.maxResliceCount))
        {
            CopySliceComponent(obj);
        }

        return obj;
    }
    
    private void CopySliceComponent(GameObject obj)
    {
        var sliceComponent = obj.AddComponent<Slice>();

        sliceComponent.sliceOptions = this.sliceOptions;
        sliceComponent.callbackOptions = this.callbackOptions;
        sliceComponent.currentSliceCount = this.currentSliceCount + 1;
    }
}
