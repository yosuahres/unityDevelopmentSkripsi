using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Prefracture : MonoBehaviour
{
    public TriggerOptions triggerOptions;
    public FractureOptions fractureOptions;
    public CallbackOptions callbackOptions;
    public PrefractureOptions prefractureOptions;

    /// <summary>
    /// Collector object that stores the produced fragments
    /// </summary>
    private GameObject fragmentRoot;

    void OnValidate()
    {
        if (this.transform.parent != null)
        {
            var scale = this.transform.parent.localScale;
            if ((scale.x != scale.y) || (scale.x != scale.z) || (scale.y != scale.z))
            {
                Debug.LogWarning($"Warning: Parent transform of fractured object must be uniformly scaled in all axes or fragments will not render correctly.", this.transform);
            }
        }
    }

    /// <summary>
    /// Compute the fracture and create the fragments
    /// </summary>
    /// <returns></returns>
    [ExecuteInEditMode] 
    [ContextMenu("Prefracture")]
    public void ComputeFracture()
    {
        if (!Application.isEditor || Application.isPlaying) return;

        var mesh = this.GetComponent<MeshFilter>().sharedMesh;

        if (mesh != null)
        {
            if (this.fragmentRoot == null)
            {
                this.fragmentRoot = new GameObject($"{this.name}Fragments");
                this.fragmentRoot.transform.SetParent(this.transform.parent);
                this.fragmentRoot.transform.position = this.transform.position;
                this.fragmentRoot.transform.rotation = this.transform.rotation;
                this.fragmentRoot.transform.localScale = Vector3.one;
            }            

            var fragmentTemplate = CreateFragmentTemplate();
            
            Fragmenter.Fracture(this.gameObject,
                                this.fractureOptions,
                                fragmentTemplate,
                                this.fragmentRoot.transform,
                                prefractureOptions.saveFragmentsToDisk,
                                prefractureOptions.saveLocation);
                                        
            // Done with template, destroy it. Since we're in editor, use DestroyImmediate
            GameObject.DestroyImmediate(fragmentTemplate);
                
            // Deactivate the original object
            this.gameObject.SetActive(false);

            // Fire the completion callback
            if (callbackOptions.onCompleted != null)
            {
                callbackOptions.onCompleted.Invoke();
            }
        }
    }

    /// <summary>
    /// Creates a template object which each fragment will derive from
    /// </summary>
    /// <returns></returns>
    private GameObject CreateFragmentTemplate()
    {
        GameObject obj = new GameObject();
        obj.name = "Fragment";
        obj.tag = this.tag;

        obj.AddComponent<MeshFilter>();

        var meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = new Material[2] {
            this.GetComponent<MeshRenderer>().sharedMaterial,
            this.fractureOptions.insideMaterial
        };

        var thisCollider = this.GetComponent<Collider>();
        
        var fragmentCollider = obj.AddComponent<MeshCollider>();
        fragmentCollider.convex = true;
        
        fragmentCollider.sharedMaterial = thisCollider.sharedMaterial;
        fragmentCollider.isTrigger = thisCollider.isTrigger;

        var rigidBody = obj.AddComponent<Rigidbody>();
        rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        rigidBody.linearDamping = this.GetComponent<Rigidbody>().linearDamping;
        rigidBody.angularDamping = this.GetComponent<Rigidbody>().angularDamping;
        rigidBody.useGravity = this.GetComponent<Rigidbody>().useGravity;

        var unfreeze = obj.AddComponent<UnfreezeFragment>();
        unfreeze.unfreezeAll = prefractureOptions.unfreezeAll;
        unfreeze.triggerOptions = this.triggerOptions;
        unfreeze.onFractureCompleted = callbackOptions.onCompleted;
        
        return obj;
    }
}