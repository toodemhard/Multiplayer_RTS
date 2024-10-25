using UnityEngine;

public class Statics : MonoBehaviour
{
    public static Config Config;

    public void Awake()
    {
        Config = GetComponentInChildren<Config>();
    }

}