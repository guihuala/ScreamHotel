using UnityEngine;

public class SingletonPersistent<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance => instance;

    protected virtual void Awake()
    {

        if (instance)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this as T;
        }
        DontDestroyOnLoad(gameObject);
    }
}
