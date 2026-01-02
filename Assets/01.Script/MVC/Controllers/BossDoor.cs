using UnityEngine;

public class BossDoor : MonoBehaviour
{
    public int doorHP;
    public bool isDestroyed = false;
    private Animator animator;

    private void Awake()
    {
        doorHP = 20;
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Bullet_Player"))
        {
            
            if(doorHP > 0) 
            {
                animator.SetTrigger("Hit");
                doorHP--;
            }
            if(doorHP <= 0 && !isDestroyed)
            {
                animator.SetTrigger("Destroy");
                isDestroyed = true;
            }
        }
    }

    public void DestroyDoor()
    {
        Destroy(gameObject);
    }
}
