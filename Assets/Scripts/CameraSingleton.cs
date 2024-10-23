using UnityEngine;

public class CameraSingleton : MonoBehaviour
{
    public static Camera Instance;
    public static Vector3 OffsetPos;

    private void Awake()
    {
        Instance = GetComponent<Camera>();
        OffsetPos = transform.position;
    }
}
