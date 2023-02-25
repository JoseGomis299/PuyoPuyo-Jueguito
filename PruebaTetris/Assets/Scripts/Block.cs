using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block
{
    private Piece[] pieceList;
    private Grid<Piece> _grid;
    
    private int _rotation;
    public bool fallen { get; private set; }
    private PieceController _pieceController;
    
    public Block(Piece piece1, Piece piece2, Grid<Piece> grid, PieceController pieceController)
    {
        _pieceController = pieceController;
        _grid = grid;
        _rotation = 0;
        pieceList = new Piece[] { piece1, piece2 };
    }

    public void Rotate(int rotation)
    {
        if(pieceList[0] == null || pieceList[1] == null) return;
        if(pieceList[0].fallen || pieceList[1].fallen) return;
        
        var newRotation = _rotation + rotation;
        if (newRotation >= 360) newRotation -= 360;
        else if (newRotation < 0) newRotation += 360;

        var xFactor = (int) Mathf.Sin(newRotation*Mathf.Deg2Rad)*pieceList[0].transform.localScale.x;
        var yFactor = (int) Mathf.Cos(newRotation*Mathf.Deg2Rad)*pieceList[0].transform.localScale.y;

        var rotatedPosition = new Vector3(pieceList[0].transform.position.x + xFactor, pieceList[0].transform.position.y + yFactor);
        _grid.GetXY(rotatedPosition, out var x, out var y);
        
        if (!_grid.IsInBounds(x,y) || _grid.GetValue(x,y) != null) return;
        
        _rotation = newRotation;
        pieceList[1].transform.position = rotatedPosition;
    }

    public void Fall(float fallSpeed)
    {
        if(fallen) return;

        fallen = true;
        foreach (var piece in pieceList)
        {
            if (piece != null && !piece.Fall(ref _grid, fallSpeed, _pieceController)) fallen = false;
        }
        
        if(fallen) _pieceController.SetPiecesValue();
    }

    public void Move(int direction)
    {
        bool move = true;
        foreach (var piece in pieceList)
        {
            if(piece == null || piece.fallen) continue;
            
            var pieceBottom = new Vector3(piece.transform.position.x, piece.transform.position.y - piece.transform.localScale.y / 2,piece.transform.position.z);
            _grid.GetXY(pieceBottom, out var x, out var y);
            if (!_grid.IsInBoundsNoHeight(_grid.GetCellCenter(x + direction, y)) ||
                _grid.GetValue(x + direction, y) != null) move = false;
        }
        if(!move)return;
        foreach (var piece in pieceList)
        {
            if(piece == null || piece.fallen) continue;
            
            _grid.GetXY(piece.transform.position, out var x, out var y);
            piece.transform.position = new Vector3(_grid.GetCellCenter(x+direction, y).x, piece.transform.position.y);
        }

    }
    public void SetPosition(int x, int y)
    {
        for (int i = 0; i < pieceList.Length; i++)
        {
            pieceList[i].transform.position = _grid.GetCellCenter(x, y + i);
        }
    }

    public Piece[] GetPieces()
    {
        return pieceList;
    }
}
