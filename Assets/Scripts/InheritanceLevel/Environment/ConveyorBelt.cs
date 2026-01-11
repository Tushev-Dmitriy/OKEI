using UnityEngine;
using System.Collections.Generic;

public class ConveyorBelt : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private Vector3 direction = Vector3.forward;

    private List<Rigidbody> objectsOnBelt = new List<Rigidbody>();

    private void FixedUpdate()
    {
        for (int i = 0; i < objectsOnBelt.Count; i++)
        {
            if (objectsOnBelt[i] != null)
            {
                objectsOnBelt[i].MovePosition(objectsOnBelt[i].position + direction * speed * Time.fixedDeltaTime);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody != null)
        {
            objectsOnBelt.Add(collision.rigidbody);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody != null)
        {
            objectsOnBelt.Remove(collision.rigidbody);
        }
    }
}
