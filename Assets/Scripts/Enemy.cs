using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private bool hasSeenPlayer = false;
    private bool isAttacking = false;
    private bool isStrafingVertical = false;
    
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float visionWidth = 3f;
    [SerializeField] private float visionRange = 8f;

    [SerializeField] private float engageDistance = 5f;
    [SerializeField] private float disengageDistance = 7f;
    [SerializeField] private float strafeWidth = 4f;
    [SerializeField] private float PatrolDistance = 3.5f;

    [SerializeField] private GameObject bulletPrefab;
    private GameObject player;
    private int lastFireTime = 0;
    [SerializeField] private int fireRate = 30;
    [SerializeField] private float bulletSpeed = 3f;
    [SerializeField] private float firingWindowRadius = 0.75f;

    private float facingZ = 0;
    private Vector3 movementFromPosition;

    private int LastTurned = 0;
    private int numHorizontalStrafes = 0;
    private int TurnRate = 30;

    // if the enemy has seen the player, 
       // check if they are within distance to engage.
       // if they are not, move towards them.
         // if they are, strafe back and forth while shooting at them.
    void Update() {
        if (hasSeenPlayer) {
            if (isAttacking) {
                if (DisengageCheck()) {
                    Debug.Log("Disengaging");
                    isAttacking = false;
                } else {
                    StrafeBehaviour();
                }
            } else {
                if (EngageCheck()) {
                    Debug.Log("Engaging");
                    isAttacking = true;
                    StrafeBehaviour(true);
                } else {
                    SeekBehaviour();
                }
            }
        } else {
            if (VisionCheck()) {
                Debug.Log("Seeking!");
                hasSeenPlayer = true;
            } else {
                PatrolBehaviour();
            }
        }
    }

    void Awake() {
        movementFromPosition = transform.position;
        facingZ = transform.rotation.eulerAngles.z;
        player = GameObject.Find("Player");
    }

    bool VisionCheck() {
        //check if the player is within the vision range
        //if they are, check if they are within the vision width.
        //if they are, do a raycast to check if there is a wall between the enemy and the player
        (bool isVertical, float distMult) = AxisForDirection(facingZ);
        if (isVertical) {
            float relativeDist = (player.transform.position.y - transform.position.y) *distMult;
            if (relativeDist < visionRange && relativeDist > 0) {
                if (Mathf.Abs(player.transform.position.x - transform.position.x) < visionWidth) {
                    return !CheckForWallBlockingVision();
                }
            }
        } else {
            float relativeDist = (player.transform.position.x - transform.position.x) *distMult;
            if (relativeDist < visionRange && relativeDist > 0) {
                if (Mathf.Abs(player.transform.position.y - transform.position.y) < visionWidth) {
                    return !CheckForWallBlockingVision();
                }
            }
        }

        return false;
    } 

    bool EngageCheck() {
        //check if the player is within the engage distance.
        return SingleDistanceToPlayer() < engageDistance;
    }

    bool DisengageCheck() {
        //check if the player is within the disengage distance

        return SingleDistanceToPlayer() > disengageDistance;
    }

    // if the player is further away on the axis we are strafing along than the axis we are firing on, turn to face the player.
    // if we are further away from the player on our strafe axis than the strafe width, change direction.
    // if the player is in front of us and we have not fired a bullet within the fire rate, fire a bullet.
    void StrafeBehaviour() {
        if (LastTurned + (TurnRate*2) >  Time.frameCount) {
            return;
        }
        bool shouldBeVert = CloserOnX();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        (float xDist, float yDist) = AxisDistanceToPlayer();
        float strafeDist;
        float fireDist;
        if (shouldBeVert) {
            strafeDist = yDist;
            fireDist = xDist;
        } else {
            strafeDist = xDist;
            fireDist = yDist;
        }
        // if the player got behind us.
        if (GetFacingTowardsPlayer(isStrafingVertical) != facingZ) {
            Debug.Log("Player behind us");
            StrafeBehaviour(true);
        } else
        // if we've passed the length of our strafe.
        if (Vector3.Distance(transform.position, movementFromPosition) > strafeWidth) {
            Debug.Log("Strafe width reached");
            StrafeBehaviour(true);
        } else 
        // if we're further away from the player than our total strafe width
        if (strafeDist > strafeWidth) {
            Debug.Log("Player too far away");
            StrafeBehaviour(true);
        } else 
        // if we're walking into a wall
        if (Physics2D.CircleCast(transform.position,0.5f, rb.velocity, 0.5f, LayerMask.GetMask("Walls", "Children")).collider != null) {
            Debug.Log("Hit a wall");
            StrafeBehaviour(true);
        } 
        // then we can fire.
        if (Physics2D.CircleCast(transform.position, firingWindowRadius, LaunchDirectionForFacing(facingZ), disengageDistance, LayerMask.GetMask("Player")).collider != null) {
            if (lastFireTime + fireRate < Time.frameCount) {
                lastFireTime = Time.frameCount;
                FireBullet();
            }
        }
    }

    
    void StrafeBehaviour(bool setupStrafe) {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        isStrafingVertical = CloserOnX();
        (float xDist, float yDist) = AxisDistanceToPlayer(true);
        if (xDist < 0.5f) {
            isStrafingVertical = true;
        } else if (yDist < 0.5f) {
            isStrafingVertical = false;
        }
        if (isStrafingVertical) {
            numHorizontalStrafes = 0;
        } else {
            numHorizontalStrafes++;
        }
        if (numHorizontalStrafes > 3) {
            isStrafingVertical = true;
        }
        
        TurnToFacePlayer(isStrafingVertical);
        movementFromPosition = transform.position;
        Vector3 playerDirection = MovementDirectionToPlayer();
        if (isStrafingVertical) {
            rb.velocity = new Vector2(0, moveSpeed * playerDirection.y);
        } else {
            rb.velocity = new Vector2(moveSpeed * playerDirection.x, 0);
        }
    }

    // if the player is not yet at their patrol distance, move in the direction they are facing.
    // if they have reached the patrol distance, turn them randomly, reset their movementFromPosition, and move them in the new direction.
    void PatrolBehaviour() {
        float movedDistance = Vector3.Distance(transform.position, movementFromPosition);
        Debug.Log("Patrolling: " + movedDistance + " / " + PatrolDistance);       
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        RaycastHit2D hit = Physics2D.CircleCast(transform.position,0.5f, rb.velocity, 0.5f, LayerMask.GetMask("Walls", "Children"));
        Debug.DrawRay(transform.position, rb.velocity, Color.red, Time.deltaTime);
        if (movedDistance > PatrolDistance || hit.collider != null || rb.velocity.magnitude == 0) {
            movementFromPosition = transform.position;
            Turn(RandomDirection());
            (bool isVertical, float distMult) = AxisForDirection(facingZ);
            Debug.Log("Turning: " + facingZ);
            if (isVertical) {
                rb.velocity = new Vector2(0, moveSpeed * distMult);
            } else {
                rb.velocity = new Vector2(moveSpeed * distMult, 0);
            }
        }
    }

    
    void SeekBehaviour() {
        if (LastTurned + TurnRate >  Time.frameCount) {
            return;
        }
        Vector3 directionToPlayer = player.transform.position - transform.position;
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        (float vertRad, float horRad) = CircleRadiusForDirection(facingZ);
        RaycastHit2D vertHit = Physics2D.CircleCast(transform.position, vertRad, new Vector3(0, directionToPlayer.y, 0), 1f, LayerMask.GetMask("Walls", "Children"));
        //Debug.DrawLine(transform.position, new Vector3(0, directionToPlayer.y/Mathf.Abs(directionToPlayer.y), 0) + transform.position, Color.red, Time.deltaTime);
        RaycastHit2D horzHit = Physics2D.CircleCast(transform.position, horRad, new Vector3(directionToPlayer.x, 0, 0), 1f, LayerMask.GetMask("Walls", "Children"));
        //Debug.DrawLine(transform.position, new Vector3(directionToPlayer.x/Mathf.Abs(directionToPlayer.x), 0, 0) + transform.position, Color.red, Time.deltaTime);
        if (vertHit.collider != null && horzHit.collider != null) {
            Debug.LogError("The enemy is stuck in a corner; design the map better, I don't want to code around this.");
        } else if (vertHit.collider != null && rb.velocity.x == 0) {
            directionToPlayer = new Vector3((directionToPlayer.x/Mathf.Abs(directionToPlayer.x)) * moveSpeed, 0, 0);
            rb.velocity = directionToPlayer;
           Turn(directionToPlayer);
        } else if (horzHit.collider != null && rb.velocity.y == 0) {
            directionToPlayer = new Vector3(0, moveSpeed*(directionToPlayer.y/Mathf.Abs(directionToPlayer.y)), 0); 
           rb.velocity = directionToPlayer;
            Turn(directionToPlayer);
        } else {
            if (Mathf.Abs(directionToPlayer.x) > Mathf.Abs(directionToPlayer.y)) {
                directionToPlayer = new Vector3((directionToPlayer.x/Mathf.Abs(directionToPlayer.x)) * moveSpeed, 0, 0);
                rb.velocity = directionToPlayer;
                Turn(directionToPlayer);
            } else {
                directionToPlayer = new Vector3(0, (directionToPlayer.y/Mathf.Abs(directionToPlayer.y)) * moveSpeed, 0);
                rb.velocity = directionToPlayer;
                Turn(directionToPlayer);
            }
        }
        CheckIfWalkingIntoWallAndTurn();
    }

    void FireBullet() {
        Vector3 posOffset = LaunchDirectionForFacing(facingZ);
        GameObject bullet = Instantiate(bulletPrefab, transform.position + posOffset, transform.rotation);
        bullet.gameObject.GetComponent<Rigidbody2D>().velocity = posOffset * bulletSpeed;
    }


    bool CloserOnX() {
        float xDistance = Mathf.Abs(transform.position.x - player.transform.position.x);
        float yDistance = Mathf.Abs(transform.position.y - player.transform.position.y);
        return xDistance < yDistance;
    } 

    float SingleDistanceToPlayer() {
        return Vector3.Distance(transform.position, player.transform.position);
    }

    (float, float) AxisDistanceToPlayer() {
        float xDistance = transform.position.x - player.transform.position.x;
        float yDistance = transform.position.y - player.transform.position.y;
        return (xDistance, yDistance);
    }

    (float, float) AxisDistanceToPlayer(bool abs) {
        if (abs) {
        (float xDistance, float yDistance) = AxisDistanceToPlayer();
        return (Mathf.Abs(xDistance), Mathf.Abs(yDistance));
        }   else return AxisDistanceToPlayer();
    }

    bool CheckForWallBlockingVision() {
        int layerMask = LayerMask.GetMask("Walls");
        return Physics2D.Raycast(transform.position, player.transform.position - transform.position, SingleDistanceToPlayer(), layerMask).collider != null;
    }


    void CheckIfWalkingIntoWallAndTurn() {
        Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, rb.velocity, 0.25f, LayerMask.GetMask("Walls", "Children"));
        Debug.DrawRay(transform.position, rb.velocity, Color.red, Time.deltaTime);
        if (hit.collider != null) {
            TurnToFacePlayer();
            rb.velocity = new Vector3(rb.velocity.y, rb.velocity.x, 0);
        }
    }

    Quaternion RandomDirection() {
        int direction = Random.Range(0, 4);
        if (direction == 0) {
            return Quaternion.Euler(0, 0, 0);
        } else if (direction == 1) {
            return Quaternion.Euler(0, 0, 90);
        } else if (direction == 2) {
            return Quaternion.Euler(0, 0, 180);
        } else {
            return Quaternion.Euler(0, 0, 270);
        }
    }

    Quaternion RandomDirection(float currentZ) {
        while (true) {
            Quaternion newDirection = RandomDirection();
            if (newDirection.eulerAngles.z != currentZ) {
                return newDirection;
            }
        }
    }

    int RandomSign() {
        return Random.Range(0, 1) == 0 ? -1 : 1;
    }

    // retvals: 
    // true if y axis, false if x axis
    // float multiplier for distance calculations
    (bool, float) AxisForDirection(float directionZ) {
        if (directionZ == 0) {
            return (true, 1);
        } else if (directionZ == 90) {
            return (false, -1);
        } else if (directionZ == 180) {
            return (true, -1);
        } else {
            return (false, 1);
        }
    }

    Vector3 LaunchDirectionForFacing(float directionZ) {
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
        return posOffset;
    }

    (float, float) CircleRadiusForDirection(float directionZ) {
        Collider2D collider = gameObject.GetComponent<Collider2D>();
        float wid = collider.bounds.size.x/2;
        float len = collider.bounds.size.y/2;
        if (directionZ == 0) {
            return (wid, len);
        } else if (directionZ == 90) {
            return (len, wid);
        } else if (directionZ == 180) {
            return (wid, len);
        } else {
            return (len, wid);
        }
    }

    void Turn(float direction) {
        facingZ = direction;
        transform.rotation = Quaternion.Euler(0, 0, facingZ);
        LastTurned = Time.frameCount;
    }

    void Turn(Quaternion direction) {
        Turn(direction.eulerAngles.z);
    }

    void Turn(Vector3 direction) {
        if (direction.y > 0) {
            Turn(0);
        } else if (direction.y < 0) {
            Turn(180);
        } else if (direction.x > 0) {
            Turn(270);
        } else {
            Turn(90);
        }
    }

    float GetFacingTowardsPlayer() {
        (float xDistance, float yDistance) = AxisDistanceToPlayer();
        if (Mathf.Abs(xDistance) > Mathf.Abs(yDistance)) {
            return xDistance > 0 ? 90 : 270;
        } else {
            return yDistance < 0 ? 0 : 180;
        }
    }

    float GetFacingTowardsPlayer(bool onlyHorizontal) {
        (float xDistance, float yDistance) = AxisDistanceToPlayer();
        if (onlyHorizontal) {
            return xDistance > 0 ? 90 : 270;
        } else {
            return yDistance < 0 ? 0 : 180;
        }
    }

    void TurnToFacePlayer() {
        Turn(GetFacingTowardsPlayer());
    }

    void TurnToFacePlayer(bool onlyHorizontal) {
        Turn(GetFacingTowardsPlayer(onlyHorizontal));
    }


    Vector3 MovementDirectionToPlayer() {
        Vector3 directionToPlayer = player.transform.position - transform.position;
        return new Vector3(directionToPlayer.x/Mathf.Abs(directionToPlayer.x), directionToPlayer.y/Mathf.Abs(directionToPlayer.y), 0);
    }
}
