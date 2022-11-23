using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCamera : MonoBehaviour
{
    // Start is called before the first frame update
    // lock to fps

    [SerializeField] private int fps = 30;
    [SerializeField] private GameObject player;

    void Awake () {
        QualitySettings.vSyncCount = 0;  // VSync must be disabled
        Application.targetFrameRate = fps;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //follow the player on Y axis
        transform.position = new Vector3(transform.position.x, Mathf.Min(0, player.transform.position.y), transform.position.z);
        
    }
}
