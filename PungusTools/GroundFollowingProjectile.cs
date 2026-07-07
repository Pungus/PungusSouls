using UnityEngine;

public class GroundFollowingProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float heightAboveGround = 1f;
    public float terrainCheckHeight = 50f;
    public LayerMask terrainMask;

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        Vector3 rayStart = transform.position + Vector3.up * terrainCheckHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, terrainMask))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y + heightAboveGround;
            transform.position = pos;
        }
    }
}