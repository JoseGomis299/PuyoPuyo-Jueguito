using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Class <c>Garbage</c>: A special type of Piece that doesn't combine with others and only explodes by exploding the adjacent pieces</summary>
///
public class Garbage : Piece
{
    private int health;
    public override void Explode(Grid<Piece> grid)
    {
        if(--health > 0) return;
        exploded = true;
        grid.SetValue(transform.position, null);
        StartCoroutine(Explosion(grid));
    }

    public override IEnumerator Explosion(Grid<Piece> grid)
    {
        GetComponent<SpriteRenderer>().color = Color.white;
        yield return new WaitForSeconds(0.25f);
        if(IsHost || IsClient) DespawnPieceServerRpc();
        else Destroy(gameObject);
    }

    public void SetHealth(int value)
    {
        health = value;
    }

    public override bool Equals(Piece piece)
    {
        return piece is Garbage;
    }
}
