using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Applies physics to the parent GameObject.
 *
 * This includes gravity, acceleration, friction and collisions with level
 * geometry.
 */
public class EntityPhysics : MonoBehaviour
{

  /**
   * The smallest possible distance that matters, as far as our physics are
   * concerned.
   *
   * Distances smaller than this can safely be rounded to zero.
   */
  public const float SmallestDistance = 0.0001f;

  /**
   * Change in vertical velocity to apply each tick.
   */
  private const float Gravity = -1f;

  /**
   * Absolute change in horizontal velocity to apply each tick as result of
   * friction, when airborne.
   */
  private const float AirFriction = 0.65f;

  /**
   * Absolute change in horizontal velocity to apply each tick as result of
   * friction, when grounded.
   */
  private const float GroundFriction = 1.5f;

  /**
   * Minimum y-component of the normal of a slope in order for it to be
   * traversable.
   *
   * Steeper slopes become traversable as this approaches zero.
   */
  private const float SlopeTolerance = 0.75f;

  /**
   * Maximum magnitude of an entity's x-velocity (m/s).
   *
   * If this is set too high, entities may pass through thin level geometry.
   */
  private const float MaxSpeedX = 12f;

  /**
   * Maximum fall speed that can be attained by gravity alone (m/s).
   *
   * If this is set too high, entities may pass through thin level geometry.
   */
  private const float TerminalVelocity = -14f;

  /**
   * How far a "stuck" entity should move, in the hope of becoming unstuck.
   */
  private const float UnstuckDistance = 0.15f;

  /**
   * Input that governs this entity's movement.
   */
  private EntityInput input;

  /**
   * Rigidbody used to move the entity with collision detection.
   */
  new private Rigidbody2D rigidbody;

  /**
   * Whether the entity is currently on the ground.
   *
   * This flag is cleared at the end of `FixedUpdate`, and set again by any
   * collisions that follow.
   */
  private bool grounded;

  /**
   * Position at the end of the previous frame.
   */
  private Vector2 prevPosition;

  /**
   * Points used to check for collisions, relative to the entity's center.
   *
   * For small entities, using the corners of the hitbox should be sufficient to
   * detect all collisions. For larger entities - or if the level geometry
   * contains very thin spikes - additional collision nodes may be necessary.
   *
   * The more collision nodes, the more accurate our collision detection will
   * be, but also the more expensive it becomes, as each node entails a raycast.
   *
   * If the entity changes size, this array must be regenerated.
   */
  private Vector2[] collisionNodes;

  /**
   * SpriteRenderer used to retrieve the entity bounds.
   */
  private SpriteRenderer spriteRenderer;

  void Start()
  {
    input = GetComponent<EntityInput>();
    rigidbody = GetComponent<Rigidbody2D>();
    spriteRenderer = GetComponent<SpriteRenderer>();

    // For now, all entities use the same collision nodes, but later we could
    // optimise this by taking into account the size of the entity.
    collisionNodes = CreateCollisionNodes();
  }

  private Vector2[] CreateCollisionNodes()
  {
    return new Vector2[] {
      // Center-left
      new Vector2(-spriteRenderer.bounds.extents.x, 0),
      // Center-right
      new Vector2(spriteRenderer.bounds.extents.x, 0),
      // Top-left
      new Vector2(
          -spriteRenderer.bounds.extents.x,
          spriteRenderer.bounds.extents.y),
      // Top-right
      new Vector2(
          spriteRenderer.bounds.extents.x,
          spriteRenderer.bounds.extents.y),
      // Bottom-right
      new Vector2(
          spriteRenderer.bounds.extents.x,
          -spriteRenderer.bounds.extents.y),
      // Bottom-left
      new Vector2(
          -spriteRenderer.bounds.extents.x,
          -spriteRenderer.bounds.extents.y)
    };
  }

  void FixedUpdate()
  {
    ApplyAcceleration();
    ApplyJump();
    ApplyGravity();
    ApplyFriction();
    ScheduleMove();
  }

  /**
   * Increases the horizontal velocity according to the entity input.
   */
  private void ApplyAcceleration()
  {
    rigidbody.velocity = VectorUtils.SetX(
      rigidbody.velocity,
      Mathf.Clamp(rigidbody.velocity.x
          + input.acceleration * input.DirX.GetMultiplier(),
          -MaxSpeedX, MaxSpeedX)
    );
  }

  /**
   * Initiates or continues a jump based on entity input.
   */
  private void ApplyJump()
  {
    if (grounded && input.JumpHeld)
    {
      // Simple jump, for now
      rigidbody.velocity = VectorUtils.SetY(
          rigidbody.velocity,
          rigidbody.velocity.y + 15
      );
      grounded = false;
    }
  }

  /**
   * Reduces horizontal velocity according to friction.
   */
  private void ApplyFriction()
  {
    if (!ShouldApplyFriction())
    {
      return;
    }

    float friction = grounded ? GroundFriction : AirFriction;

    // Apply friction as a drag force in the opposite direction of movement
    if (rigidbody.velocity.x < 0)
    {
      rigidbody.velocity = VectorUtils.SetX(
          rigidbody.velocity,
          Mathf.Min(rigidbody.velocity.x + friction, 0)
      );
    }
    else if (rigidbody.velocity.x > 0)
    {
      rigidbody.velocity = VectorUtils.SetX(
          rigidbody.velocity,
          Mathf.Max(rigidbody.velocity.x - friction, 0)
      );
    }
  }

  private bool ShouldApplyFriction()
  {
    // Only apply friction if no direction is held...
    return rigidbody.velocity.x == 0 ||
        // ... or we are trying to change direction
        Mathf.Sign(rigidbody.velocity.x) != input.DirX.GetMultiplier();
  }

  /**
   * Changes the velocity by applying gravity.
   */
  private void ApplyGravity()
  {
    rigidbody.velocity = VectorUtils.SetY(
        rigidbody.velocity,
        Mathf.Max(rigidbody.velocity.y + Gravity, TerminalVelocity)
    );
  }

  /**
   * Tells the Rigidbody how it should move after FixedUpdate has finished.
   *
   * This movement will result in the OnTrigger* callbacks below, should we
   * collide with any trigger colliders.
   */
  private void ScheduleMove()
  {
    // Remember our position before any movement is applied
    prevPosition = transform.position;

    // Reset this flag to prepare for imminent collisions
    grounded = false;

    rigidbody.MovePosition((Vector2)transform.position
        + rigidbody.velocity * Time.fixedDeltaTime);
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Solid"))
    {
      CollideWithSolid(other);
    }
  }

  private void OnTriggerStay2D(Collider2D other)
  {
    if (other.gameObject.CompareTag("Solid"))
    {
      CollideWithSolid(other);
    }
  }

  /**
   * Resolves a collision with the given collider.
   *
   * In hindsight, it may have been easier to look for collisions BEFORE
   * attempting the movement, but I am too far down the rabbit hole to change it
   * now.
   *
   * That approach would allow us to consider all collisions ahead of time and
   * then decide how to resolve them, whereas this approach means that we have
   * to resolve collisions as they occur, without knowing what other collisions
   * may have occurred simultaneously.
   */
  private void CollideWithSolid(Collider2D other)
  {
    float distanceTravelled = (rigidbody.position - prevPosition).magnitude;

    // We know that a collision has occurred but we don't know where exactly.
    // To find the position of the collision, we re-enact the movement that's
    // just occurred using raycasts from each of our collision nodes.
    foreach (Vector2 node in collisionNodes)
    {
      Vector2 prev = prevPosition + node;
      Vector2 current = rigidbody.position + node;
      RaycastHit2D hit = Physics2D.Raycast(
          prev, current - prev, distanceTravelled, LayerMask.GetMask("Level"));

      // We only care about collisions with the collider that caused this
      // callback; other colliders can take care of their own collisions.
      if (hit.collider == other && CollisionUtils.CanCollide(
          node, hit, spriteRenderer.bounds.extents, SlopeTolerance))
      {
        ProcessCollision(hit);
        return;
      }
    }
  }

  /**
   * Moves the entity away from the given collision until it is no longer
   * colliding, and changes the velocity as appropriate.
   */
  private void ProcessCollision(RaycastHit2D hit)
  {
    if (hit.distance == 0)
    {
      // We are already inside the solid, so we can't rely on the collision
      // normal. If this happens, something has gone wrong. For now, just try
      // moving up and "backwards" and hope that we escape the collision.
      rigidbody.transform.position += (Vector3)Vector2.up * UnstuckDistance
          + (Vector3)hit.normal * UnstuckDistance;
      rigidbody.velocity = VectorUtils.SetY(rigidbody.velocity, 0);
      return;
    }

    // Figure out how far we need to move to escape the solid in which we are
    // embedded.
    Vector2 escapeVector = CollisionUtils.CalculateCollisionEscapeVector(
        rigidbody.position, prevPosition, hit, SlopeTolerance);

    // Move away from the collision
    rigidbody.transform.position += (Vector3)escapeVector;

    // Adjust velocity
    if (CollisionUtils.IsCollisionWithFloor(hit, SlopeTolerance))
    {
      rigidbody.velocity = VectorUtils.SetY(rigidbody.velocity, 0);
      grounded = true;
    }
    else if (CollisionUtils.IsCollisionWithCeiling(hit))
    {
      rigidbody.velocity = VectorUtils.SetY(rigidbody.velocity, 0);
    }
    else if (CollisionUtils.IsCollisionWithWall(hit, SlopeTolerance))
    {
      rigidbody.velocity = VectorUtils.SetX(rigidbody.velocity, 0);
    }
  }

}
