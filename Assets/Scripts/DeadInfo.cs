using System.Collections.Generic;
using UnityEngine;

public class DeadInfo : MonoBehaviour
{
    private static DeadInfo _instance;

    public static DeadInfo Instance
    {
        get { return _instance; }
    }

    [SerializeField]
    public List<DeadInfoScriptableObject> _deadInfoScriptableObjects = new List<DeadInfoScriptableObject>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    
}