using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YellowPiece : Piece
{
    public override bool Equals(Piece piece)
    {
        return piece is YellowPiece;
    }
}
