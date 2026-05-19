using UnityEngine;

public class Vibration : MonoBehaviour
{
    public static Vibration instance;

    void Awake()
    {
        instance = this;
    }

    public void Vibrate(long milliseconds)
    {
        Handheld.Vibrate();
    }
}