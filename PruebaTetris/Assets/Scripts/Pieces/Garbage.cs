using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Class <c>Garbage</c>: A special type of Piece that doesn't combine with others and only explodes by exploding the adjacent pieces</summary>
///
public class Garbage : Piece
{
    private int health;
    private float generationTime;
    private bool explodesWithTime;

    private PieceController myController;
    public override void Explode(Grid<Piece> grid)
    {
        if(--health > 0) return;
        exploded = true;
        grid.SetValue(transform.position, null);
        StartCoroutine(Explosion(grid));
    }

    public void SetValues(int health, bool explodesWithTime,PieceController pieceController)
    {
        this.health = health;
        this.explodesWithTime = explodesWithTime;
        if (explodesWithTime) generationTime = Time.time;
        myController = pieceController;
    }
    
    public override bool Equals(Piece piece)
    {
        return piece is Garbage;
    }

    private void Update()
    {
        if (explodesWithTime && Time.time - 10 > generationTime)
        {
            explodesWithTime = false;
            Explode(myController.grid);
            Timer.Instance.WaitForAction(() =>
            {
                myController.AddHealth(-0.33f);
                myController.SetPiecesValue(true);
            }, 0.25f);
        }
    }
}
