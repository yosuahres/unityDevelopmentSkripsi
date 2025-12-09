//fingervisualizer.cs
//mark 5 november
// NOT USED TEMPORARILY
using UnityEngine;
using UnityEngine.XR.Hands;

public class FingerVisualizer : MonoBehaviour
{
    public GameObject leftIndexSphere;
    public GameObject rightIndexSphere;

    public float colliderRadius = 0.02f; 
    void Start()
    {
        //sphere collider
        SetupCollider(leftIndexSphere, "LeftIndexSphere");
        SetupCollider(rightIndexSphere, "RightIndexSphere");
    }

    private void SetupCollider(GameObject sphere, string sphereName)
    {
        if (sphere != null)
        {
            bool wasActive = sphere.activeSelf;
            if (!wasActive)
            {
                sphere.SetActive(true);
            }

            SphereCollider collider = sphere.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = sphere.AddComponent<SphereCollider>();
            }

            collider.isTrigger = true;
            collider.radius = colliderRadius;

            if (!wasActive)
            {
                sphere.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning($"{sphereName} is not assigned in the Inspector.", this);
        }
    }


    public void UpdateRightHand(XRHandJointsUpdatedEventArgs eventArgs)
    {
        if (rightIndexSphere == null) return;

        XRHand hand = eventArgs.hand;
        XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);

        if (indexTip.TryGetPose(out Pose pose))
        {
            if (!rightIndexSphere.activeSelf)
                rightIndexSphere.SetActive(true);

            rightIndexSphere.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }

    public void HideRightHand()
    {
        if (rightIndexSphere != null && rightIndexSphere.activeSelf)
            rightIndexSphere.SetActive(false);
    }


    public void UpdateLeftHand(XRHandJointsUpdatedEventArgs eventArgs)
    {
        if (leftIndexSphere == null) return;

        XRHand hand = eventArgs.hand;
        XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);

        if (indexTip.TryGetPose(out Pose pose))
        {
            if (!leftIndexSphere.activeSelf)
                leftIndexSphere.SetActive(true);

            leftIndexSphere.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }

    public void HideLeftHand()
    {
        if (leftIndexSphere != null && leftIndexSphere.activeSelf)
            leftIndexSphere.SetActive(false);
    }
}