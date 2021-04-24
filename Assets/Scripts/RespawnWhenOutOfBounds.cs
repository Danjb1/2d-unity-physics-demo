using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Causes an entity to teleport back to its spawn location after falling out of
 * bounds.
 */
public class RespawnWhenOutOfBounds : MonoBehaviour
{

  // Hardcode this, for now
  private const float MinY = -6;

  new private Rigidbody2D rigidbody;

  private Vector2 spawnPos;

  void Start()
  {
    rigidbody = GetComponent<Rigidbody2D>();
    spawnPos = transform.position;
  }

  void LateUpdate()
  {
    Respawn();
  }

  private void Respawn()
  {
    if (transform.position.y < MinY)
    {
      transform.position = spawnPos;
      rigidbody.velocity = Vector2.zero;
    }
  }

}
