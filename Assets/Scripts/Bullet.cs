using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Bullet hit " + collision.gameObject.name);
        if (collision.gameObject.layer != LayerMask.NameToLayer("Bullets"))
        {
            StartCoroutine(DestroyBullet());
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy") )
        {
            Destroy(collision.gameObject);
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Children"))
        {
            GameObject.Find("Player").GetComponent<PlayerCharacter>().DeadChildren(collision.gameObject);
            Destroy(collision.gameObject);
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            collision.gameObject.GetComponent<PlayerCharacter>().GotHit();
        }
    }
    //delayed to give the other object time to react to the collision
    IEnumerator DestroyBullet()
    {
        yield return new WaitForEndOfFrame();
        Destroy(gameObject);
    }
}
