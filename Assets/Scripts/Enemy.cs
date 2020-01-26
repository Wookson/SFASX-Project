using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    void Start()
    {
        animator = GetComponent<Animator>();
        SingleNodeMoveTime = 2f;
        AP = 2;
    }

    public override void Die()
    {
        base.Die();
        CurrentArea.Enemies.Remove(this);
        game.Score += 100;
    }
}
