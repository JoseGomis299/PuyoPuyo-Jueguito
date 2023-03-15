using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YellowPiece : Piece
{
    public override void Explode(Grid<Piece> grid)
    {
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

    public override bool Equals(Piece piece)
    {
        return piece is YellowPiece;
    }
}
