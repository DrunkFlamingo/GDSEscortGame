using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCharacter : MonoBehaviour
{

    [SerializeField] private GameObject bulletPrefab;

    public Quaternion facingDirection = Quaternion.Euler(0, 0, 0);
    private float directionZ = 0;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float bulletSpeed = 3f;
    private   bool ignoreLeft = false;
    private  bool ignoreRight = false;
    private  bool ignoreUp = false;
    private   bool ignoreDown = false; 

    private int lastFireTime = 0;
    [SerializeField] private int fireRate = 15;

    private List<Vector3> followPostions = new List<Vector3>();
    private List<GameObject> followers = new List<GameObject>();
    private List<GameObject> childrenNearby = new List<GameObject>();
    [SerializeField] private float followDistance = 0.5f;
    private Vector3 lastFollowPosition;

    private int numFollowers = 0;

    [SerializeField] private int healthMax = 3;
    private int health = 3;

    // Update is called once per frame
    void Update()
    {
        updateMovement();
        shootGun();
        updateFollowers();
    }

    void Awake() {
        lastFollowPosition = transform.position;
        GameObject.Find("GameOverCanvas").GetComponent<Canvas>().enabled = false;
        health = healthMax;
    }

    (bool, bool, bool, bool) Face(bool isUpPressed, bool isDownPressed, bool isLeftPressed, bool isRightPressed) {
        if (isUpPressed) {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            directionZ = 0;
            return (false, true, false, false);
        } else if (isDownPressed) {
            transform.rotation = Quaternion.Euler(0, 0, 180);
            directionZ = 180;
            return (true, false, false, false);
        } else if (isLeftPressed) {
            transform.rotation = Quaternion.Euler(0, 0, 90);
            directionZ = 90;
            return (false, false, false, true);
        } else if (isRightPressed) {
            transform.rotation = Quaternion.Euler(0, 0, 270);
            directionZ = 270;
            return (false, false, true, false);
        }
        return (false, false, false, false);
    }

    //check each directional key to see if they were pressed this frame
    //if they were, turn the player to face that direction and set the facing direction variable
    //check each directional key to see if they are being held down
    //if they are, move the player in that direction
    // if the player is no longer moving in a direction which they are facing, turn them to face the direction they are moving in
    void updateMovement() {

        //pressed this frame.
        (ignoreUp, ignoreDown, ignoreLeft, ignoreRight) = Face(
            Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W),
            Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S),
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A),
            Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)
        );
        // being held
        bool isUpPressed = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool isDownPressed = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
        bool isLeftPressed = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool isRightPressed = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);

        Vector3 velocity = new Vector3(0, 0, 0);
        if (isUpPressed && !ignoreUp) {
            velocity += Vector3.up *  moveSpeed;
        } else if (directionZ == 0) {
            Face(isUpPressed, isDownPressed, isLeftPressed, isRightPressed);
        }
        if (isDownPressed && !ignoreDown) {
           velocity += Vector3.down *  moveSpeed;
        }  else if (directionZ == 180) {
            Face(isUpPressed, isDownPressed, isLeftPressed, isRightPressed);
        }
        if (isLeftPressed && !ignoreLeft) {
            velocity += Vector3.left *  moveSpeed;
        }  else if (directionZ == 90) {
            Face(isUpPressed, isDownPressed, isLeftPressed, isRightPressed);
        }
        if (isRightPressed && !ignoreRight) {
            velocity += Vector3.right *  moveSpeed;
        } else if (directionZ == 270) {
            Face(isUpPressed, isDownPressed, isLeftPressed, isRightPressed);
        }
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.velocity = velocity;
    }

    //instantiate a bullet prefab and set its direction to the player's facing direction
    void FireBullet() {
        Vector3 posOffset = new Vector3(0, 0, 0);
            if (directionZ == 0) {
                posOffset = Vector3.up;
            } else if (directionZ == 180) {
                posOffset = Vector3.down;
            } else if (directionZ == 90) {
                posOffset = Vector3.left;
            } else if (directionZ == 270) {
                posOffset = Vector3.right;
            }
        GameObject bullet = Instantiate(bulletPrefab, transform.position + posOffset, transform.rotation);
        bullet.gameObject.GetComponent<Rigidbody2D>().velocity = posOffset * bulletSpeed;
    }

    
    void shootGun() {
        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Mouse0)) {
            if (lastFireTime + fireRate < Time.frameCount) {
                lastFireTime = Time.frameCount;
                FireBullet();
            }
        }
    }

    void updateFollowers() {
        if (Vector3.Distance(transform.position, lastFollowPosition) > followDistance) {
            followPostions.Insert(0, lastFollowPosition);
            lastFollowPosition = transform.position;
        }
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Insert)) {
            Debug.Log("Dropping "+ followers.Count + " Children");
            foreach (GameObject child in followers) {
                Rigidbody2D rb = child.GetComponent<Rigidbody2D>();
                rb.velocity = new Vector3(0, 0, 0);
            }
            followers.Clear();
        }
        if (followPostions.Count > followers.Count + 1) {
            followPostions.RemoveAt(followPostions.Count - 1);
        }
        foreach (Vector3 followerPos in followPostions) {
            // move the rigidbody of each follower to the position in the followerpositions list.
            // if it is close enough to the position, remove its velocity so it stops moving.
            
            if (followers.Count > followPostions.IndexOf(followerPos)) {
                GameObject child = followers[followPostions.IndexOf(followerPos)];
                Rigidbody2D rb = child.GetComponent<Rigidbody2D>();
                if (Vector3.Distance(child.transform.position, followerPos) > 0.1f) {
                    rb.velocity = (followerPos - child.transform.position).normalized * moveSpeed;
                } else {
                    rb.velocity = Vector3.zero;
                }
            }
        }
    }

    public void DeadChildren(GameObject child) {
        followers.Remove(child);
        GameOver();
    }

    void OnTriggerStay2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Children")) {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.LeftControl)) {
                if (followers.Contains(other.gameObject)) {
                    return;
                }  
                Debug.Log("Picking Up Children");
                followers.Add(other.gameObject);
                Debug.Log("Now holding " + followers.Count + " children");
            }
        }
    }

    public void GotHit() {
        health--;
        if (health <= 0) {
            GameOver();
        }
    }


    void GameOver() {
        Debug.Log("Game Over");
        GameObject.Find("GameOverCanvas").GetComponent<Canvas>().enabled = true;
        GameObject.Find("Background").GetComponent<SpriteRenderer>().enabled = false;
        GameObject.Find("UICanvas").GetComponent<Canvas>().enabled = false;

        StartCoroutine(GameOverCoroutine());
    }

    IEnumerator GameOverCoroutine() {
        yield return new WaitForSeconds(5);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);

        while (!asyncLoad.isDone) {
            yield return null;
        }
    }

    void OnGUI() {
        GameObject.Find("HealthMeter").GetComponent<TMPro.TextMeshProUGUI>().text = "Health: " + health;
    }
}
