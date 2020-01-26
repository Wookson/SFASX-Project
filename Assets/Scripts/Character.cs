using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public float SingleNodeMoveTime = 0.5f;

    public Animator animator;

    public Game game { get; set; }
    public HUD Hud;

    public int Health { get; set; }
    public int intent { get; set; }
    public int AP;
    public Environment CurrentArea { get; set; }

    public EnvironmentTile CurrentPosition { get; set; }

    void Start()
    {
        animator = GetComponent<Animator>();
        Reset();
    }

    public void Reset()
    {
        for(int i = 0; i < Hud.Hearts.Count; i++)
        {
            Destroy(Hud.Hearts[i].gameObject);
        }
        Hud.Hearts.Clear();
        Hud.UpdateHealth(3);
        Health = 3;
        intent = 0;
        AP = 4;
        Hud.UpdateAPAvailibilty(AP);
    }

    private void Update()
    {
        //Animation
        if (game.CoroutineRunning)
        {
            animator.SetBool("Walk", true);
        }
        else
        {
            animator.SetBool("Walk", false);
        }
    }

    public virtual void Die()
    {
        Destroy(this.gameObject);
    }


    public void UseAbility(List<EnvironmentTile> AoE)
    {
        Debug.Log(CurrentArea.Enemies.Count);
        for (int i = 0; i < CurrentArea.Enemies.Count; i++)
        {
            for (int j = 0; j < AoE.Count; j++)
            {
                if (CurrentArea.Enemies[i] != null)
                {
                    if (CurrentArea.Enemies[i].CurrentPosition == AoE[j])
                    {
                        CurrentArea.Enemies[i].Die();
                    }
                }
            }
        }
    }

    public void TakeDamage(int Damage)
    {
        Health -= Damage;
        Hud.UpdateHealth(-Damage);
        Hud.NextWave.text = "Game Over";
        if(Health <= 0)
        {
            Die();
        }
    }

    private IEnumerator DoMove(Vector3 position, Vector3 destination)
    {
        // Move between the two specified positions over the specified amount of time
        if (position != destination)
        {
            transform.rotation = Quaternion.LookRotation(destination - position, Vector3.up);

            Vector3 p = transform.position;
            float t = 0.0f;

            while (t < SingleNodeMoveTime)
            {
                t += Time.deltaTime;
                p = Vector3.Lerp(position, destination, t / SingleNodeMoveTime);
                transform.position = p;
                yield return null;
            }
        }
    }

    private IEnumerator DoGoTo(List<EnvironmentTile> route)
    {
        // Move through each tile in the given route
        if (route != null)
        {
            Vector3 position = CurrentPosition.Position;
            for (int count = 0; count < route.Count; ++count)
            {
                Vector3 next = route[count].Position;
                yield return DoMove(position, next);
                CurrentPosition = route[count];
                position = next;
            } 
        }
        game.CoroutineRunning = false;
    }

    public void GoTo(List<EnvironmentTile> route)
    {
        // Clear all coroutines before starting the new route so 
        // that clicks can interupt any current route animation
        StopAllCoroutines();
        StartCoroutine(DoGoTo(route));
    }
}
