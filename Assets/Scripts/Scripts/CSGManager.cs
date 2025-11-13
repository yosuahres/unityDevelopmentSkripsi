// CSGManager.cs
using UnityEngine;
using Parabox.CSG;
using Unity.PolySpatial; 
using Unity.PolySpatial.InputDevices; 

public class CSGManager : MonoBehaviour
{
    public static CSGManager Instance { get; private set; }

    public GameObject MandibleModel { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void RegisterMandible(GameObject model)
    {
        MandibleModel = model;
        Debug.Log("CSGManager: Mandible model registered.");
    }

    public void RegisterAndCut(GameObject cuttingPlane)
    {
        if (MandibleModel == null)
        {
            Debug.LogError("CSG ERROR: Mandible model is not registered.");
            Destroy(cuttingPlane);
            return;
        }

        Component originalTouchable = MandibleModel.GetComponent<ISpatialTouchable>() as Component;

        if (originalTouchable == null)
        {
             Debug.LogError("CSG ERROR: The original Mandible Model does not have a component that implements ISpatialTouchable.");
        }

        Debug.Log("CSGManager: Received cutting plane. Performing subtraction...");

        Model resultModel = CSG.Subtract(MandibleModel, cuttingPlane);
        GameObject newMandible = new GameObject(MandibleModel.name + "_cut");
        newMandible.transform.position = MandibleModel.transform.position;
        newMandible.transform.rotation = MandibleModel.transform.rotation;
        newMandible.transform.localScale = MandibleModel.transform.localScale;
        newMandible.transform.SetParent(MandibleModel.transform.parent, true);

        newMandible.AddComponent<MeshFilter>().sharedMesh = resultModel.mesh;
        newMandible.AddComponent<MeshRenderer>().sharedMaterials = resultModel.materials.ToArray();

        SetupModelCollider(newMandible); 

        if (originalTouchable != null)
        {
            newMandible.AddComponent(originalTouchable.GetType());
        }

        Destroy(MandibleModel); 
        Destroy(cuttingPlane);  

        MandibleModel = newMandible;

        Debug.Log("CSG Subtraction complete.");
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
}