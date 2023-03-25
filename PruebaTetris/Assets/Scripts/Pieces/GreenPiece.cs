using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenPiece : Piece
{
    public override bool Equals(Piece piece)
    {
        return piece is GreenPiece;
    }
}
