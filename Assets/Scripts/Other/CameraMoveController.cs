using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraMoveController : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private bool onlyHorizontal = false;
    
    [Header("Limits (X, Z)")]
    [SerializeField] private Vector2 minLimit = new Vector2(-20, -20);
    [SerializeField] private Vector2 maxLimit = new Vector2(20, 20);

    private void Update()
    {
        Vector3 moveDir = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed && !onlyHorizontal) moveDir.z += 1;
            if (Keyboard.current.sKey.isPressed && !onlyHorizontal) moveDir.z -= 1;
            if (Keyboard.current.aKey.isPressed) moveDir.x -= 1;
            if (Keyboard.current.dKey.isPressed) moveDir.x += 1;
        }
#else
        float h = Input.GetAxisRaw("Horizontal");
        float v = onlyHorizontal ? 0 : Input.GetAxisRaw("Vertical");
        moveDir = new Vector3(h, 0, v);
#endif

        if (moveDir.sqrMagnitude > 0)
        {
            moveDir = moveDir.normalized;
            Vector3 newPos = transform.position + moveDir * speed * Time.deltaTime;

            newPos.x = Mathf.Clamp(newPos.x, minLimit.x, maxLimit.x);
            
            if (!onlyHorizontal)
            {
                newPos.z = Mathf.Clamp(newPos.z, minLimit.y, maxLimit.y);
            }

            transform.position = newPos;
        }
    }
}
