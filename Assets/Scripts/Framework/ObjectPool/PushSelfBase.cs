using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushSelfBase : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;
    private float timer;
    private bool isPushed;

    private void InitPushTimer()
    {
        timer = 0f;
        isPushed = false;
    }

    void Update()
    {
        if (isPushed) return; // 如果已经被推送，则直接返回

        timer += Time.deltaTime;

        if (timer > lifeTime)
        {
            isPushed = true;
            PushObj();
        }
    }

    private void PushObj()
    {
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.PushObject(gameObject);
        }
        else
        {
            Debug.LogError("ObjectPool 实例为 null，无法推送对象。");
        }
    }
}