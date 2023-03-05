using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block
{
    private readonly Piece[] pieceList;
    public Grid<Piece> grid { get; private set; }
    
    public int rotation { get; private set; }
    public bool fallen { get; private set; }
    public bool anyPieceHasFallen { get; private set; }
    private PieceController _pieceController;
    
    public float lastFallenTime;
    public bool stopFalling;
    public Block(Piece piece1, Piece piece2, Grid<Piece> grid, PieceController pieceController)
    {
        _pieceController = pieceController;
        this.grid = grid;
        rotation = 0;
        pieceList = new Piece[] { piece1, piece2 };

        for (int i = 0; i < pieceList.Length; i++)
        {
            pieceList[i].SetBlockReference(this, i);
        }
    }

    public void Rotate(int rotation)
    {
        if(pieceList[0] == null || pieceList[1] == null) return;
        if(anyPieceHasFallen || pieceList[1].rotating) return;
        
        var newRotation = this.rotation + rotation;
        if (newRotation >= 360) newRotation -= 360;
        else if (newRotation < 0) newRotation += 360;

        var xFactor = (int) Mathf.Sin(newRotation*Mathf.Deg2Rad)*grid.GetCellSize();
        var yFactor = (int) Mathf.Cos(newRotation*Mathf.Deg2Rad)*grid.GetCellSize();

        var rotatedPosition = new Vector3(pieceList[0].transform.position.x + xFactor, pieceList[0].transform.position.y + yFactor);
        grid.GetXY(rotatedPosition, out var x, out var y);
        
        if (!grid.IsInBoundsNoHeight(x, y) || grid.GetValue(x, y) != null)
        {
            if(!MoveForRotation(new Vector2(-xFactor, -yFactor))) return;
            rotatedPosition = new Vector3(pieceList[0].transform.position.x + xFactor, pieceList[0].transform.position.y + yFactor);
        }

        if (stopFalling && (grid.IsInBoundsNoHeight(grid.GetCellCenter(x, y-1)) && grid.GetValue(x, y-1) == null))
        {
            stopFalling = false;
            ResetPiecesAdvice(false);
        }
        this.rotation = newRotation;
        //pieceList[1].transform.position = rotatedPosition;
         pieceList[1].Rotate(grid,newRotation, rotation);
    }
    
    public void Fall(float fallSpeed)
    {
        if(fallen) return;
        fallen = true;
        if (stopFalling && Time.time - lastFallenTime >= 0.55f)
        {
            stopFalling = false;
        }
        if (stopFalling)
        {
            fallen = false;
            return;
        }

        var anyHasFallen = false;
        foreach (var piece in pieceList)
        {
            if (piece == null) continue;
            if (piece.fallen)
            {
                fallSpeed *= 3;
                anyHasFallen = true;
                break;
            }
        }
        
        anyPieceHasFallen = anyHasFallen;
        
        foreach (var piece in pieceList)
        {
            if(piece == null) continue;
            if (!piece.Fall(grid, fallSpeed, _pieceController)) fallen = false;
        }

        //Set pieces positions 
        if (stopFalling)
        {
            foreach (var piece in pieceList)
            {
                if(piece == null) continue;
                piece.transform.position = grid.GetCellCenter(piece.transform.position);
            }
            return;
        }

        if(fallen) _pieceController.SetPiecesValue();
    }

    //MOVES THE PIECE, IF IT FALLS AGAIN RESETS THE TIME TO STAY WITHOUT CHECKING
    public void Move(Vector2 direction)
    {
        if(pieceList[0] == null || pieceList[1] == null) return;
        if(anyPieceHasFallen) return;
        
        bool move = true;
        bool canMoveDown = true;
        foreach (var piece in pieceList)
        {
            var pieceBottom = new Vector3(piece.transform.position.x, piece.transform.position.y - piece.transform.localScale.y / 2,piece.transform.position.z);
            grid.GetXY(pieceBottom, out var x, out var y);
            if (!grid.IsInBoundsNoHeight(grid.GetCellCenter(x + (int)direction.x, y + (int)direction.y)) ||
                grid.GetValue(x + (int)direction.x, y + (int)direction.y) != null) move = false;
        }
        if(!move) return;
        foreach (var piece in pieceList)
        {
            grid.GetXY(piece.transform.position, out var x, out var y);
            piece.transform.position = grid.GetCellCenter(x+(int)direction.x, y + (int)direction.y);
            
            if (!grid.IsInBoundsNoHeight(grid.GetCellCenter(x + (int)direction.x, y + (int)direction.y-1)) ||
                grid.GetValue(x + (int)direction.x, y + (int)direction.y-1) != null) canMoveDown = false;
        }

        if (stopFalling && canMoveDown)
        {
            stopFalling = false;
            ResetPiecesAdvice(true);
        }
        return;
    }
    
    //MOVES THE PIECE WITHOUT CHANGING THE TIME TO STAY WITHOUT CHECKING AND RETURNS IF IT CAN BE MOVED
    private bool MoveForRotation(Vector2 direction)
    {
        if(pieceList[0] == null || pieceList[1] == null) return false;
        if(pieceList[0].fallen || pieceList[1].fallen) return false;
        
        bool move = true;
        foreach (var piece in pieceList)
        {
            var pieceBottom = new Vector3(piece.transform.position.x, piece.transform.position.y - piece.transform.localScale.y / 2,piece.transform.position.z);
            grid.GetXY(pieceBottom, out var x, out var y);
            if (!grid.IsInBoundsNoHeight(grid.GetCellCenter(x + (int)direction.x, y + (int)direction.y)) ||
                grid.GetValue(x + (int)direction.x, y + (int)direction.y) != null) move = false;
        }
        if(!move) return false;
        foreach (var piece in pieceList)
        {
            grid.GetXY(piece.transform.position, out var x, out var y);
            piece.transform.position = grid.GetCellCenter(x+(int)direction.x, y + (int)direction.y);
        }
        return true;
    }
    public void SetPositionInGrid(int x, int y)
    {
        for (int i = 0; i < pieceList.Length; i++)
        {   if(pieceList[i] == null) return;
            pieceList[i].transform.position = grid.GetCellCenter(x, y + i);
            pieceList[i].transform.localScale = Vector3.one;
        }
    }
    
    public void SetPosition(Vector3 position, float scaleMultiplier)
    {
        for (int i = 0; i < pieceList.Length; i++)
        {
            if(pieceList[i] == null) return;
            pieceList[i].transform.localScale = Vector3.one*scaleMultiplier;
            position.y += pieceList[i].transform.localScale.y*i;
            pieceList[i].transform.position = position;
        }
    }

    public Piece[] GetPieces()
    {
        return pieceList;
    }
    
    
    private void ResetPiecesAdvice(bool setTime)
    {
        foreach (var piece in pieceList)
        {
            if (piece == null) continue;
            piece.SetAdvice(false);
            piece.doNotSetTime = !setTime;
        }
    }
}
