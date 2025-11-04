using UnityEngine;

public class AutoRotate : MonoBehaviour
{
    public float speed = 30.0f;

    void Update()
    {
        transform.Rotate(Vector3.up, speed * Time.deltaTime);
    }
}