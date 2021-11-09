using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

/// <summary>
/// This project is mainly focused on the physics of the player
/// through the constant transformation of the Player's position.
/// There will be as much use of real life physics as it can get..
/// </summary>
public class PlayerController : MonoBehaviour
{
    private Vector3 closestPoint;
    private ContactPoint[] contacts;
    // Main components and major variables
    private ExternalCollider externalColliderScript;
    [SerializeField] private bool collided;
    private List<Vector3> collisionNormals;
    private Vector3 collisionNormal;
    private Vector3 collisionForward;
    private Vector3 collisionStop;

    private int terrainLayerMask;
    private int worldObjectLayerMask;
    private int loopFlagsLayerMask;

    private RaycastHit hitInfoCollision;
    private RaycastHit hitInfoGround;
    private RaycastHit hitInfoGroundSaved;

    [SerializeField] private float playerHeight;
    [SerializeField] private float heightPadding;
    private float distanceFromGround;

    private Vector3 velocityVector;
    private Vector3 previousPosition;
    private Vector3 currentPosition;

    ////////////////////////////////////
    /// Player Physics

    // XZ Direction Movement and Angles
    private Vector2 inputAxis;
    private Vector2 prevInputAxis;
    private bool noInput;

    [SerializeField] private float groundAccel;
    private float groundVelocity;
    [SerializeField] private float groundMaxVelocity;

    private float groundRotationAngle;
    [SerializeField] private float groundRotationSpeed;

    // Y Direction Movement and Angles
    [SerializeField] private bool grounded;
    [SerializeField] private float gravityCoeff;
    [SerializeField] private bool jumped;
    [SerializeField] private float jumpHeightMax;
    [SerializeField] private float jumpPower;
    [SerializeField] private float jumpSpeed;
    [SerializeField] private float jumpDeceleration;

    // Slope Direction and Angles
    private Vector3 playerForward;
    private Vector3 playerUp;
    private Vector3 playerRight;
    [SerializeField] private float slopeAngle;
    private bool enteredSlope;

    ////////////////////////////////////
    /// Player Actions

    // Jump action
    private bool actionJump;
    private bool actionHoming;
    private float homingInterval;

    // Spindash action
    private bool actionSpindashCharging;
    private float actionSpindashChargingInterval;
    private bool actionSpindashChargingReleased;
    private bool actionSpindash;
    private float actionSpindashSpeed;
    private float actionSpindashInterval;
    ////////////////////////////////////
    /// Debugging player
    [SerializeField] private bool debug;

    private Joystick joystick;
    private Joybutton joybutton;
    public bool android;

    public PathCreator pathCreator;
    public bool followPath;
    public float pathDistanceCovered;
    private Transform pathLoopBegin;
    private Transform pathLoopMid;
    private Transform pathLoopEnd;
    private int pathDirectivity;

    private void Start() {

        contacts = new ContactPoint[1];
        previousPosition = transform.position;

        externalColliderScript = transform.GetComponentInChildren<ExternalCollider>();
        collisionNormals = new List<Vector3>();

        terrainLayerMask = 1 << 6;
        worldObjectLayerMask = 1 << 7;
        loopFlagsLayerMask = 1 << 8;
        playerHeight = 2f;
        heightPadding = 0.3f;

        inputAxis = new Vector2(0f, 0f);
        prevInputAxis = new Vector2(0f, 0f);
        groundAccel = 8f;
        groundVelocity = 0f;
        groundMaxVelocity = 26f;
        groundRotationSpeed = 10f;

        gravityCoeff = 4f;
        jumpPower = 3f;
        jumpHeightMax = 5.5f;
        jumpSpeed = Mathf.Sqrt(2 * (9.81f * gravityCoeff) * jumpHeightMax);
        jumpDeceleration = 9.81f * gravityCoeff;

        // Instanciate the directional vectors
        playerForward = transform.forward;
        playerUp = transform.up;
        playerRight = transform.right;


        // Android Development joystick and buttons
        joystick = FindObjectOfType<Joystick>();
        joybutton = FindObjectOfType<Joybutton>();
    }

    private void Update() {

        // Direction axis
        inputAxis.x = Input.GetAxis("Horizontal") + joystick.Horizontal;
        inputAxis.y = Input.GetAxis("Vertical") + joystick.Vertical;

        // Jump Action Logic
        if (!android) {
            if ((Input.GetButtonDown("Jump")) && grounded) {
                grounded = false;
                jumped = true;
                actionJump = true;
                actionSpindash = false;
                actionSpindashCharging = false;
                actionSpindashChargingReleased = false;
                actionSpindashChargingInterval = 0f;
            }

            if ((Input.GetButtonUp("Jump")) && jumped) {
                jumpSpeed = 3f;
            }

            if (actionJump && !jumped) {
                if (Input.GetButtonDown("Jump"))
                    actionHoming = true;
            }
        }
        else {
            if (joybutton.pressed && grounded) {
                grounded = false;
                jumped = true;
                actionJump = true;
                actionSpindash = false;
                actionSpindashCharging = false;
                actionSpindashChargingReleased = false;
                actionSpindashChargingInterval = 0f;
            }

            if (!joybutton.pressed && jumped) {
                jumpSpeed = 3f;
            }

            if (actionJump && !jumped) {
                if (joybutton.pressed)
                    actionHoming = true;
            }
        }

        // Spindash Action logic
        if (Input.GetButtonDown("Spindash") && grounded) {
            actionSpindashCharging = true;
            actionSpindashChargingReleased = false;
            actionSpindashSpeed = 10f;
        }

        if (Input.GetButtonUp("Spindash") && actionSpindashCharging) {
            if (actionSpindashChargingInterval < .3f && groundVelocity > 10f) {
                if (actionSpindash) 
                    actionSpindash = false;
                else {
                    actionSpindashSpeed = groundVelocity;
                    actionSpindash = true;
                    actionSpindashChargingReleased = false;
                }
            }
            else {
                actionSpindashChargingReleased = true;
            }
            actionSpindashCharging = false;
            actionSpindashChargingInterval = 0f;
        }

        DebugUpdate();

        if (inputAxis == Vector2.zero) {
            // PlayerSlideOnSlope();
            CalculateNoInput();
            return;
        }
        
        noInput = false;
        
        // If input suddenly inverts then suddenly decelerate player
        if (prevInputAxis.x == -inputAxis.x && prevInputAxis.y == -inputAxis.y)
            groundVelocity = 0f;
        prevInputAxis = inputAxis;
        
        // PlayerRotate();

    }

    private void FixedUpdate() {

        currentPosition = transform.position;
        velocityVector = (currentPosition - previousPosition).normalized;
        previousPosition = currentPosition;

        CalculateRotationAngle();
        CalculateForward();
        CalculateUp();
        CalculateRight();
        CalculateCollision();
        CalculateGrounded();
        CalculateSlopeAngle();
        
        CalculateVelocity();
        CalculateSlopeVelocity();
       
        PlayerMove();
        PlayerGravity();
        ActionJump();
        ActionHoming();
        ActionSpindash();

        PlayerPathFollow();

        if (grounded)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(playerForward, playerUp), 4 * groundRotationSpeed * Time.deltaTime);
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(playerForward, Vector3.up), 4 * groundRotationSpeed * Time.deltaTime);

        if (noInput) {
            PlayerSlideOnSlope();
            // followPath = false;
            return;
        }
        
        PlayerRotate();
    }

    /// <summary>
    /// All the functions that calculate fundamental units
    /// for the player: positioning, angles, velocity etc
    /// </summary>

    // Calculate the rotation angle of the input axis
    private void CalculateRotationAngle() {
        groundRotationAngle = Mathf.Atan2(inputAxis.x, inputAxis.y);
        groundRotationAngle = Mathf.Rad2Deg * groundRotationAngle;
    }

    // Calculate if the player is grounded
    private void CalculateGrounded() {
        if (jumped) return;
        // If the angle of the slope is more than 90 degrees
        // and the player is travelling with less than the 3/4 of
        // its max speed then grounded = false
        if (slopeAngle > 90f && groundVelocity < 15f) {
            grounded = false;
            return;
        }

        grounded = Physics.Raycast(transform.position, -playerUp, out hitInfoGround, (playerHeight / 2) + heightPadding, terrainLayerMask | worldObjectLayerMask);
        
        if (!grounded)
            grounded = Physics.Raycast(transform.position, -Vector3.up, out hitInfoGround, (playerHeight / 2) + heightPadding, terrainLayerMask | worldObjectLayerMask);

        // if (!grounded)
        //     if (collided) {
        //         grounded = true;
        //         hitInfoGround.normal = collisionPointNormal;
        //         hitInfoGround.point = collisionPoint;
        //     }
        // if (Physics.CheckSphere(transform.position - 0.55f * transform.up, 0.5f, terrainLayerMask | worldObjectLayerMask)) {
        //     while (!Physics.Raycast(transform.position, -Vector3.up, out hitInfoGround, (playerHeight / 2) + heightPadding + 0.1f, terrainLayerMask | worldObjectLayerMask))
        //         /* Do nothing */;
        //     grounded = true;
        //     Debug.Log("It happened");
        // }
            

        // if (!grounded) {
        //     if (Physics.Raycast(transform.position + playerForward, -Vector3.up, out hitInfoGround, Mathf.Infinity, terrainLayerMask | worldObjectLayerMask)) {
        //         Debug.Log("Inside");
        //         Vector3 point = hitInfoGround.point;
        //         grounded = (Vector3.Distance(transform.position - playerUp * playerHeight, point) < 1f);
        //     }
        // }

        if (grounded) {
            if (slopeAngle < 90f) {
                distanceFromGround = (transform.position - (playerHeight / 2) * playerUp).y  - hitInfoGround.point.y;
                if ((transform.position - (playerHeight / 2) * playerUp).y  < hitInfoGround.point.y) {
                    float difference = hitInfoGround.point.y - (transform.position - (playerHeight / 2) * playerUp).y;
                    transform.position += difference * playerUp;
                }
            }
            else {
                distanceFromGround = hitInfoGround.point.y - (transform.position - (playerHeight / 2) * playerUp).y;
                if ((transform.position - (playerHeight / 2) * playerUp).y  > hitInfoGround.point.y) {
                    float difference = (transform.position - (playerHeight / 2) * playerUp).y - hitInfoGround.point.y;
                    transform.position += difference * playerUp;
                }
            }

            actionJump = false;
            actionHoming = false;
            homingInterval = 0f;
        }

        // if (!grounded) {
        //     if (Physics.OverlapSphere(transform.position - 0.5f * playerUp, 1f, terrainLayerMask).Length > 0) {
        //         Debug.Log("RESCUE >> terrainDetection");
        //         grounded = true;
        //     }
        // }
    }

        private void CalculateCollision() {

        // Collider[] colliders = Physics.OverlapSphere(transform.position + 0.5f * playerUp, 1f, worldObjectLayerMask);
        // if (colliders.Length > 0) {
        //     foreach (var col in colliders) {
        //         Vector3 direction = (col.ClosestPointOnBounds(transform.position) - transform.position).normalized;
        //         collided = true;
        //         collisionNormal = direction;
        //     }
        // } else collided = false;

        // collisionNormals = new List<Vector3>();
        // List<ContactPoint> collisionContactPoints = externalColliderScript.collisionContactPoints;
        // if (collisionContactPoints.Count > 0) {
            
        //     collided = true;

        //     // ContactPoint farthestCollisionPoint = collisionContactPoints[0];
            
        //     // foreach (ContactPoint cPoint in collisionContactPoints)
        //     //     if (Vector3.Distance(transform.position, cPoint.point) > Vector3.Distance(transform.position, farthestCollisionPoint.point))
        //     //         farthestCollisionPoint = cPoint;

        //     foreach (ContactPoint cPoint in collisionContactPoints) {
        //         var collisionNormal = Vector3.ProjectOnPlane(cPoint.point - transform.position, playerUp);
        //         collisionNormals.Add(-collisionNormal);
        //         Debug.DrawRay(cPoint.point, -collisionNormal, Color.red);
        //     }
        //     // collisionPointNormal = farthestCollisionPoint.normal;
        //     // collisionPoint = farthestCollisionPoint.point;
        //     // collisionNormal = (collisionPoint - transform.position);
        // }
        // else collided = false;

        List<ContactPoint> collisionContactPoints = externalColliderScript.collisionContactPoints;
        if (collisionContactPoints.Count > 0) {

            collided = true;

            Vector3 collisionNormal1 = Vector3.ProjectOnPlane(collisionContactPoints[0].point - transform.position, playerUp);
            Vector3 smallestProjectedForward = Vector3.ProjectOnPlane(playerForward, collisionNormal1);
            collisionStop = Vector3.Project(playerForward, collisionNormal1);

            foreach (ContactPoint cPoint in collisionContactPoints) {

                Debug.DrawRay(cPoint.point, cPoint.normal, Color.green);
                // Debug.DrawRay(cPoint.point, Vector3.ProjectOnPlane(cPoint.normal, playerUp), Color.red);

                collisionNormal1 = Vector3.ProjectOnPlane(cPoint.point - transform.position, playerUp);
                
                if (Vector3.ProjectOnPlane(playerForward, collisionNormal1).magnitude < smallestProjectedForward.magnitude) {
                    smallestProjectedForward = Vector3.ProjectOnPlane(playerForward, collisionNormal1);
                    // collisionStop = Vector3.Project(playerForward, collisionNormal1);
                    collisionStop = Vector3.ProjectOnPlane(cPoint.normal, playerUp);
                    // collisionNormal = collisionNormal1;
                    collisionNormal = Vector3.ProjectOnPlane(cPoint.normal, playerUp);
                }
            }

            collisionForward = smallestProjectedForward;
            Debug.DrawRay(transform.position, collisionForward, Color.green);
            Debug.DrawRay(transform.position, collisionStop, Color.red);

        } else collided = false;


        // collided = Physics.Raycast(transform.position, playerForward, out hitInfoCollision, 0.65f, worldObjectLayerMask);
        // if (collided) {
        //     collisionNormal = hitInfoCollision.normal;
        //     return;
        // }
        // if (!collided) {
        //     collided = Physics.Raycast(transform.position, playerForward + playerRight, out hitInfoCollision, 0.65f, worldObjectLayerMask);
        //     collisionNormal = hitInfoCollision.normal;
        //     return;
        // }

        // if (!collided) {
        //     collided = Physics.Raycast(transform.position, playerForward - playerRight, out hitInfoCollision, 0.65f, worldObjectLayerMask);
        //     collisionNormal = hitInfoCollision.normal;
        //     return;
        // }

    }

    // Calculate player's forward vector
    private void CalculateForward() {
        if (grounded)
            playerForward = Vector3.Cross(transform.right, hitInfoGround.normal).normalized;
        else
            // Smooth change when not grounded
            playerForward = Vector3.Lerp(playerForward, transform.forward, 2 * groundRotationSpeed * Time.deltaTime);
    }

    // Calculate player's up vector
    private void CalculateUp() {
        if (grounded)
            playerUp = hitInfoGround.normal;
        else
            // Restart vector's direction linearly if not grounded
            playerUp = Vector3.Lerp(playerUp, transform.up, 2 * groundRotationSpeed * Time.deltaTime);
    }

    // Calculate player's right vector
    private void CalculateRight() {
        if (grounded)
            playerRight = Vector3.Cross(playerUp, playerForward).normalized;
        else
            // Restart vector's direction linearly if player is not grounded
            playerRight = Vector3.Lerp(playerRight, transform.right, 2 * groundRotationSpeed * Time.deltaTime);
    }

    // Calculate the angle of the slope the player ascends or descends
    private void CalculateSlopeAngle() {
        // if (Physics.Raycast(transform.position + playerForward, -playerUp, out hitInfoGround, (playerHeight / 2) + heightPadding, terrainLayerMask)) {
        //     float forwardHeight = hitInfoGround.point.y;
        //     if (Physics.Raycast(transform.position - playerForward, -playerUp, out hitInfoGround, (playerHeight / 2) + heightPadding, terrainLayerMask)) {
        //         float backwardsHeight = hitInfoGround.point.y;
        //         slopeAngle = Mathf.Atan2(Mathf.Abs(forwardHeight - backwardsHeight), 2);
        //         slopeAngle = Mathf.Rad2Deg * slopeAngle;
        //     }
        // }
        // else slopeAngle = 0f;
        if (grounded)
            slopeAngle = Vector3.Angle(Vector3.up, playerUp);
        else
            slopeAngle = 0f;
    }

    // Calculate player's velocity by adding acceleration
    private float CalculateVelocity() {

        if (groundMaxVelocity > 50f)
            groundMaxVelocity = 50f;

        if (!noInput)
            groundVelocity += groundAccel * Time.deltaTime;
        if (groundVelocity > groundMaxVelocity)
            StartCoroutine("CoSmoothVelocityReduction");
            
        return groundVelocity;
    }

    private IEnumerator CoSmoothVelocityReduction() {
        while (groundVelocity > groundMaxVelocity)
            yield return groundVelocity -= groundAccel * Time.deltaTime / 20f;
    }

    // Calculate velocity and acceleration on slope
    private void CalculateSlopeVelocity() {
        if (slopeAngle >= 20f && slopeAngle < 45f) {
            enteredSlope = true;
            Vector3 slopeDirectionProjected = Vector3.ProjectOnPlane(hitInfoGround.normal, Vector3.up).normalized;
            float angle = Vector3.Angle(slopeDirectionProjected, playerForward);
            
            if (angle < 85f || angle > 95f) {
                // If slope angle helps player's velocity
                if (Vector3.Dot(slopeDirectionProjected, playerForward) > 0) {
                    groundAccel = 12f;
                    float slopeVelocity = 0.5f * 9.81f * Mathf.Abs(Mathf.Sin(slopeAngle));
                    if (actionSpindash)
                        slopeVelocity = 9.81f * Mathf.Abs(Mathf.Sin(slopeAngle));
                    if ((groundMaxVelocity < 26f + slopeVelocity) && (groundMaxVelocity < 40f)) 
                        groundMaxVelocity = 26f + slopeVelocity;
                }

                // If slope angle is against player's velocity
                if (Vector3.Dot(slopeDirectionProjected, playerForward) < 0) {
                    groundAccel = 4f;
                    float slopeVelocity = 9.81f * Mathf.Abs(Mathf.Sin(slopeAngle));
                    if (groundMaxVelocity > 26f - slopeVelocity)
                        groundMaxVelocity = 26f - slopeVelocity;
                }
            }
        }

        if (slopeAngle >= 45f && slopeAngle < 90f) {
            enteredSlope = true;
            Vector3 slopeDirectionProjected = Vector3.ProjectOnPlane(hitInfoGround.normal, Vector3.up).normalized;
            float angle = Vector3.Angle(slopeDirectionProjected, playerForward);
            
            if (angle < 85f || angle > 95f) {
                // If slope angle helps player's velocity
                if (Vector3.Dot(slopeDirectionProjected, playerForward) > 0) {
                    groundAccel = 12f;
                    float slopeVelocity = 2f * 9.81f * Mathf.Abs(Mathf.Sin(slopeAngle));
                    if (actionSpindash)
                        slopeVelocity = 3f * 9.81f * Mathf.Abs(Mathf.Sin(slopeAngle));
                    if ((groundMaxVelocity < 26f + slopeVelocity) && (groundMaxVelocity < 40f)) 
                        groundMaxVelocity = 26f + slopeVelocity;
                }

                // If slope angle is against player's velocity
                if (Vector3.Dot(slopeDirectionProjected, playerForward) < 0) {
                    groundAccel = 4f;
                    float slopeVelocity = 4f * 9.81f * Mathf.Abs(Mathf.Sin(slopeAngle));
                    if (groundMaxVelocity > 26f - slopeVelocity)
                        groundMaxVelocity = 26f - slopeVelocity;
                }
            }
        }


        // If slope's angle doesn't affect player's velocity
        if ((slopeAngle >= 0f && slopeAngle < 20f) ||  (slopeAngle >= 90f && slopeAngle < 180f)) {
            // If exited the slope then the last reached velocity is the max
            if (enteredSlope) {
                groundMaxVelocity = groundVelocity;
                enteredSlope = false;
            }

            groundAccel = 8f;
            if (groundMaxVelocity < 26f)
                groundMaxVelocity = 26f;
        }
    }

    private void CalculateNoInput() {
        // Set no input flag to true so that 
        // CalculateVelocity doesn't increase velocity
        noInput = true;
        // Decrease velocity
        groundVelocity -= 7 * groundAccel * Time.deltaTime;
        // Don't let the player go backwards
        if (groundVelocity < 0)
            groundVelocity = 0f;
        // Reset maximum velocity on ground
        groundMaxVelocity = 26f;
    }


    /// <summary>
    /// Player actions and movements
    /// </summary>

    // Add gravity to the player's physics
    private void PlayerGravity() {
        if (!grounded && !jumped)
            transform.position -= 9.81f * Vector3.up * gravityCoeff * Time.deltaTime;
    }

    // Movement of the player
    private void PlayerMove() {
        if (!collided)
            transform.position += playerForward * CalculateVelocity() * Time.deltaTime;
        else {
            // if (hitInfoCollision.transform.tag != "Loop") {
                // If player collided on object then move sideways from the object
                // transform.position = Vector3.Lerp(transform.position, transform.position-Vector3.ProjectOnPlane(collisionNormal, Vector3.up), 2 * Time.deltaTime);
            // if (collisionNormal == Vector3.zero) {
            //     Debug.Log("RESCUE >> Collision Trap");
            //     groundVelocity = 0f;
            //     // RESCUE: Push the player outside the object
            //     Vector3 point;
            //     Physics.Raycast(transform.position, transform.forward, out hitInfoCollision, Mathf.Infinity, worldObjectLayerMask);
            //     point = hitInfoCollision.point;
            //     Physics.Raycast(transform.position, -transform.forward, out hitInfoCollision, Mathf.Infinity, worldObjectLayerMask);
            //     if (Vector3.Distance(transform.position, hitInfoCollision.point) < Vector3.Distance(transform.position, point)) {
            //         point = hitInfoCollision.point;
            //         transform.position = Vector3.Lerp(transform.position, point - transform.forward + (playerHeight / 2) * transform.up, groundRotationSpeed * Time.deltaTime);
            //         return;
            //     }
            //     transform.position = Vector3.Lerp(transform.position, point + transform.forward + (playerHeight / 2) * transform.up, groundRotationSpeed * Time.deltaTime);
            //     return;
            // }

            // Vector3 directionality;
            // if (velocityVector == Vector3.zero)
            //     directionality = playerForward;
            // else
            //     directionality = velocityVector;
            
            // foreach (Vector3 collisionNormal in collisionNormals) {
            //     Vector3 collisionNormalProjected = Vector3.Project(collisionNormal, playerForward);

            //     Debug.DrawRay(transform.position, Vector3.Project(playerForward, collisionNormal), Color.green);
            //     Debug.DrawRay(transform.position, Vector3.ProjectOnPlane(playerForward, collisionNormal), Color.green);

                if (Vector3.Dot(collisionNormal, playerForward) > 0)
                    transform.position += playerForward * CalculateVelocity() * Time.deltaTime;
                else {
                    // transform.position = previousPosition;
                    // groundVelocity = 0f;
                    // transform.position += -Vector3.Project(playerForward, collisionStop).normalized * velocityVector.magnitude;
                    transform.position = previousPosition + Vector3.ProjectOnPlane(playerForward, collisionNormal) * CalculateVelocity() * Time.deltaTime;
                }

                // if (Vector3.Dot(collisionNormal, playerForward) > 0)
                //     transform.position += playerForward * CalculateVelocity() * Time.deltaTime;
                // if (Vector3.Dot(collisionNormal, playerForward) > -0.3f && Vector3.Dot(collisionNormal, playerForward) <= 0) {
                //     // transform.position = previousPosition;
                //     // transform.position = Vector3.Lerp(transform.position, transform.position + collisionNormalProjected, 2f * Time.deltaTime);
                //     transform.position += Vector3.ProjectOnPlane(playerForward, collisionNormal) * CalculateVelocity() * Time.deltaTime;
                // }
                // if (Vector3.Dot(collisionNormal, playerForward) >= -1f && Vector3.Dot(collisionNormal, playerForward) <= -0.3f)
                //     transform.position = previousPosition;
                    // transform.position = Vector3.Lerp(transform.position, transform.position + collisionNormal, CalculateVelocity() * Time.deltaTime);
                    // if (groundVelocity < 2f)
                    //     transform.position = Vector3.Lerp(transform.position, transform.position + 0.3f * collisionNormal, 2f * Time.deltaTime);
                    // else
            // }
            // }
            // else
            //     transform.position += playerForward * CalculateVelocity() * Time.deltaTime;
        }
    }

    // Rotate the player to the forward's direction
    private void PlayerRotate() {
        if (slopeAngle < 140f)
        // if (!actionJump)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.rotation.x, groundRotationAngle, transform.rotation.z), groundRotationSpeed * Time.deltaTime);
        // else
        //     transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.rotation.x, groundRotationAngle, transform.rotation.z), 4 * groundRotationSpeed * Time.deltaTime);


        else
            // When the player is heading to an upside down view then make rotation more responsive and rotate counterwise
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(transform.rotation.x, -groundRotationAngle, transform.rotation.z), groundRotationSpeed * Time.deltaTime);

    }

    // If the player is on a slope slide him downwards
    private void PlayerSlideOnSlope() {
        // If the angle of the slope is greater than 36 degress slide down
        if (slopeAngle > 36f && slopeAngle < 90f && grounded) {
            Vector3 slideDirectionOnXZPlane = Vector3.ProjectOnPlane(hitInfoGround.normal, Vector3.up).normalized;
            Vector3 slideDirection = Vector3.ProjectOnPlane(slideDirectionOnXZPlane, hitInfoGround.normal).normalized;
            
            transform.position += slideDirection * (2 * (9.81f * gravityCoeff * 10f) * Mathf.Abs(Mathf.Sin(slopeAngle))) * Time.deltaTime * Time.deltaTime;
        }
    }

    private void ActionJump() {
        if (jumped) {
            
            Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f, terrainLayerMask | worldObjectLayerMask);
            if (colliders.Length > 0) {
                if (colliders[0].TryGetComponent<MeshCollider>(out MeshCollider comp))
                    return;
                Vector3 point = colliders[0].ClosestPoint(transform.position);
                Vector3 normal = (transform.position - point).normalized;
                Debug.Log(normal);
                playerForward = Vector3.ProjectOnPlane(playerForward, normal);
            }
            
            jumpSpeed -= jumpDeceleration * Time.deltaTime;
            transform.position += Vector3.up * jumpPower * jumpSpeed * Time.deltaTime; // TRY: transform.up <-- playerUp

            if (jumpSpeed < 0f) {
                jumped = false;
                jumpSpeed = Mathf.Sqrt(2 * (9.81f * gravityCoeff) * jumpHeightMax);
            }
        }
    }

    private void ActionHoming() {
        if (!actionHoming) return;

        if (jumped) {
            // groundVelocity = 20f;
            jumped = false;
            jumpSpeed = Mathf.Sqrt(2 * (9.81f * gravityCoeff) * jumpHeightMax);
        }

        homingInterval += Time.deltaTime;
        if (homingInterval > .22f) {
            actionHoming = false;
            groundVelocity = groundMaxVelocity;
            return;
        }
        // groundVelocity = 35f;
        Vector3 homingTowards = playerForward * 100 * Time.deltaTime + 6f * Vector3.up * gravityCoeff * Time.deltaTime;
        Collider[] colliders = Physics.OverlapSphere(transform.position - 0.5f * playerUp, 0.5f, terrainLayerMask | worldObjectLayerMask);
        if (colliders.Length > 0) {
            if (colliders[0].TryGetComponent<MeshCollider>(out MeshCollider comp))
                return;
            Vector3 point = colliders[0].ClosestPoint(transform.position);
            Vector3 normal = (transform.position - point).normalized;
            Debug.Log(normal);
            homingTowards = Vector3.ProjectOnPlane(homingTowards, normal);
        }

        transform.position += homingTowards;
    }

    private void ActionSpindash() {

        if (actionSpindashCharging) {

            actionSpindashChargingInterval += Time.deltaTime;
            actionSpindashInterval = 0f;

            if (actionSpindashChargingInterval > .3f) {
                actionSpindash = false;
                groundVelocity = 0f;
                actionSpindashSpeed += 3.5f * groundAccel * Time.deltaTime;
                if (actionSpindashSpeed > 45f)
                    actionSpindashSpeed = 45f;
            }
        }

        if (actionSpindashChargingReleased) {        
            groundVelocity = actionSpindashSpeed;
            actionSpindashCharging = false;
            actionSpindashChargingReleased = false;
            actionSpindash = true;
        }

        if (actionSpindash) {
            actionSpindashInterval += Time.deltaTime;

            if (actionSpindashInterval < 2f) {
                
                if (groundVelocity < actionSpindashSpeed)
                    groundVelocity = actionSpindashSpeed;

                if (groundMaxVelocity > 50f)
                    groundMaxVelocity = 50f;

            }

            if (actionSpindashInterval > 10f || groundVelocity < 10f) {
                actionSpindash = false;
                actionSpindashChargingInterval = 0f;
            }
        }
    }

    private void PlayerPathFollow() {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1f, loopFlagsLayerMask);
        
        if (colliders.Length > 0) {
            
            // Get all the paths controller cubes
            pathLoopBegin = colliders[0].transform.parent.GetChild(0);
            pathLoopMid = colliders[0].transform.parent.GetChild(1);
            pathLoopEnd = colliders[0].transform.parent.GetChild(2);
       
            if (colliders[0].tag == "LoopBegin") {
                
                if (Vector3.Dot(colliders[0].transform.forward, playerForward) < 0) {
                    pathCreator = colliders[0].transform.GetComponentInParent<PathCreator>();
                    if (colliders[0].transform == pathLoopBegin)
                        pathDirectivity = +1;
                    if (colliders[0].transform == pathLoopEnd)
                        pathDirectivity = -1;

                    followPath = true;
                }
                
            }

            if (colliders[0].tag == "LoopMid") {
                if (pathLoopBegin.tag == "LoopBegin")
                    pathLoopBegin.tag = "LoopEnd";

                if (pathLoopEnd.tag == "LoopBegin")
                    pathLoopEnd.tag = "LoopEnd";
                followPath = true;
            }

            if (colliders[0].tag == "LoopEnd") {
                followPath = false;
                colliders[0].tag = "LoopBegin";
            }

        }
        
        if (followPath) {

            if (noInput || jumped || !grounded) {
                followPath = false;
                return;
            }

            // colliders = Physics.OverlapSphere(transform.position, 1f, loopFlagsLayerMask);
            // if (colliders.Length > 0)
            //     followPath = !(colliders[0].tag == "LoopEnd");

            pathDistanceCovered += pathDirectivity * groundVelocity * Time.deltaTime;
            var pos = pathCreator.path.GetPointAtDistance(pathDistanceCovered);
            playerForward = pos - transform.position;
            // transform.rotation = pathCreator.path.GetRotationAtDistance(pathDistanceCovered);
        } 
        else {
            pathDistanceCovered = 0f;
        }
    }

    // private void OnCollisionStay(Collision other) {
    //     // other.GetContacts(contacts);
        
    //     closestPoint = other.articulationBody.GetClosestPoint(transform.position);
    // }

    
    /// <summary>
    /// Debug settings: logging, printing, etc
    /// </summary>
    private void DebugUpdate() {
        if (debug) {
            DebugLogging();
            DebugDrawing();
        }
    }

    private void DebugLogging() {
        // return;
        // Debug.Log(capsuleCollider.ClosestPoint(transform.position));
        // Debug.Log(Quaternion.());
    }

    private void DebugDrawing() {
        // Draw playerForward vector
        Debug.DrawLine(transform.position, transform.position + playerForward * 4f, Color.blue);

        // Draw playerUp vector
        Debug.DrawLine(transform.position, transform.position + playerUp * 4f, Color.green);

        // Draw playerRight vector
        Debug.DrawLine(transform.position, transform.position + playerRight * 4f, Color.red);

    }

    private float sphereRadius;
    private void OnDrawGizmos() {
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, velocityVector);

        Gizmos.color = Color.red;
        
        if (actionJump) {
            if (sphereRadius < 12f)
                sphereRadius += 0.00000000000000000000001f * Time.deltaTime;
            else
                sphereRadius = 12f;
            
            Gizmos.DrawWireSphere(transform.position, sphereRadius++);
        }
        else sphereRadius = 0f;
        // Gizmos.color = Color.red;
        // Gizmos.DrawSphere(closestPoint, 0.5f);
        // try {
        //     foreach (ContactPoint contact in contacts)
        //         Gizmos.DrawSphere(contact.point, 1f);
        // }
        // catch {

        // }
        // Gizmos.DrawLine(transform.position, hitInfoCollision.point);
        // Gizmos.DrawWireSphere(transform.position + 0.5f * transform.up, 0.6f);
        // Gizmos.DrawWireSphere(transform.position + transform.up, 0.6f);
        // Gizmos.color = Color.yellow;
        // Gizmos.DrawRay(transform.position, capsuleCollider.ClosestPointOnBounds(transform.position) - transform.position);
        // capsuleCollider.
    }

    /// <summary>
    /// Getters for character models
    /// </summary>

    public float GetVelocity() {
        return groundVelocity;
    }

    public bool GetIsGrounded() {
        return grounded;
    }

    public bool GetActionJump() {
        return actionJump;
    }

    public bool GetActionSpindashCharge() {
        return actionSpindashCharging;
    }

    public bool GetActionSpindash() {
        return (actionSpindashChargingReleased || actionSpindash);
    }

    public float GetSpindashSpeed() {
        return actionSpindashSpeed;
    }

    public Vector3 GetPlayerForward() {
        return playerForward;
    }
    
    public Vector3 GetPlayerUp() {
        return playerUp;
    }

    public float GetDistanceFromGround() {
        return distanceFromGround;
    }

    public float GetSlopAngle() {
        return slopeAngle;
    }

}
