using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenPiece : Piece
{
    public override void Explode(ref Grid<Piece> grid)
    {
        grid.SetValue(transform.position, null);
        Destroy(gameObject);
    }

    public override bool Equals(Piece piece)
    {
        return piece is GreenPiece;
    }
}
