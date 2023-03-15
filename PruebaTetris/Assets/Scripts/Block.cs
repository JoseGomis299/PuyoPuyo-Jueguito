using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block
{
    private readonly Piece[] pieceList;
    public Grid<Piece> grid { get; private set; }
    
    public int rotation { get; private set; }
    public bool fallen { get; set; }
    private PieceController _pieceController;
    
    public float lastFallenTime;
    public bool stopFalling;
    private bool _advisedFromFalling;
    public bool rotating;
    private bool _resetTime;

    public Block(Piece piece1, Piece piece2, Grid<Piece> grid, PieceController pieceController)
    {
        _pieceController = pieceController;
        this.grid = grid;
        rotation = 0;
        pieceList = new Piece[] { piece1, piece2 };
        _resetTime = true;
        for (int i = 0; i < pieceList.Length; i++)
        {
            pieceList[i].SetBlockReference(this);
        }
    }

    public Block(Piece piece1, Grid<Piece> grid, PieceController pieceController)
    {
        _pieceController = pieceController;
        this.grid = grid;
        rotation = 0;
        pieceList = new Piece[] { piece1 };

        for (int i = 0; i < pieceList.Length; i++)
        {
            pieceList[i].SetBlockReference(this, i);
        }
    }

    public void Rotate(int rotation)
    {
        if(pieceList[0] == null || pieceList[1] == null) return;
        if(fallen || rotating) return;
        rotating = true;
        
        var newRotation = this.rotation + rotation;
        if (newRotation >= 360) newRotation -= 360;
        else if (newRotation < 0) newRotation += 360;

        int xFactor = (int)Mathf.Sin(newRotation * Mathf.Deg2Rad);
        int yFactor = (int) Mathf.Cos(newRotation*Mathf.Deg2Rad);

        var rotatedPosition = new Vector3(pieceList[0].transform.position.x + xFactor, pieceList[0].transform.position.y + yFactor);
        grid.GetXY(rotatedPosition, out var x, out var y);
        
        while (!grid.IsInBoundsNoHeight(x, y) || grid.GetValue(x, y) != null)
        {
            if (!MoveForRotation(new Vector2(-xFactor, -yFactor)))
            {
                rotating = false;
                return;
            }
            rotatedPosition = new Vector3(pieceList[0].transform.position.x + xFactor, pieceList[0].transform.position.y + yFactor);
            grid.GetXY(rotatedPosition, out x, out y);
        }
        
        this.rotation = newRotation;
        stopFalling = false;
        _advisedFromFalling = false;

        pieceList[1].Rotate(grid,newRotation, rotation);
    }

    public void Fall(float fallSpeed)
    {
        if (fallen) return;
        if (stopFalling)
        {
            HandleExtraTime();
            return;
        }

        int fallenPiece = -1;
        for (int i = 0; i < pieceList.Length; i++)
        {
            grid.GetXY(new Vector3(pieceList[i].transform.position.x, pieceList[i].transform.position.y-grid.GetCellSize()/2f-fallSpeed * Time.deltaTime), out var x, out var y);
            if (grid.GetValue(x,y) != null || !grid.IsInBoundsNoHeight(x,y))
            {
                fallenPiece = i;
                break;
            }
        }

        if (fallenPiece != -1 && !rotating)
        {
            //Set "y" to fallenPiece's "y"
            grid.GetXY(new Vector3(pieceList[fallenPiece].transform.position.x, pieceList[fallenPiece].transform.position.y-grid.GetCellSize()/2f-fallSpeed * Time.deltaTime), out _, out var y);
            y++;

            //Fix a position for the pieces
            for (int i = 0; i < pieceList.Length; i++)
            {
                grid.GetXY(new Vector3(pieceList[i].transform.position.x, 0), out var x, out _);
                while (!grid.IsInBoundsNoHeight(x, y) || grid.GetValue(x,y) != null) y++;
                if ((i == 0 && rotation == 180) || (i == 1 && rotation == 0)) pieceList[i].transform.position = grid.GetCellCenter(x, y+1);
                else pieceList[i].transform.position = grid.GetCellCenter(x, y);
                
                //Check if any piece is out of bounds (Loose condition)
                grid.GetXY(pieceList[i].transform.position, out _, out var y1);
                if (y1 >= grid.GetHeight())
                {
                    fallen = true;
                    _pieceController.CleanStage();
                }
            }
            
            //If haven't fallen activate extra time (it resets every time the block falls)
            if (!_advisedFromFalling || _resetTime)
            {
                _advisedFromFalling = true;
                stopFalling = true;
                if (_resetTime)
                {
                    _resetTime = false;
                    lastFallenTime = Time.time;
                }
                return;
            }
            
            //Set values in the grid and make the necessary pieces explode or fall
            fallen = true;
            _pieceController.SetPiecesValue();
            return;
        }

        if (fallenPiece == -1)
        {
            foreach (var piece in pieceList)
            {
                var nextPos = piece.transform.position + Vector3.down * (fallSpeed * Time.deltaTime);
                piece.transform.position = nextPos;
            }
        }
    }

    private void HandleExtraTime()
    {
        if (Time.time - lastFallenTime >= 0.6f)
        {
            stopFalling = false;
        }
    }

    //MOVES THE PIECE, IF IT FALLS AGAIN RESETS THE TIME TO STAY WITHOUT CHECKING
    public void Move(Vector2 direction)
    {
        if(pieceList[0] == null || pieceList[1] == null) return;
        if(fallen) return;
        
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
            if(direction.y != 0) piece.transform.position = grid.GetCellCenter(x+(int)direction.x, y + (int)direction.y);
            else piece.transform.position = new Vector3(grid.GetCellCenter(x+(int)direction.x, 0).x, piece.transform.position.y);
            
            if (!grid.IsInBoundsNoHeight(grid.GetCellCenter(x + (int)direction.x, y + (int)direction.y-1)) ||
                grid.GetValue(x + (int)direction.x, y + (int)direction.y-1) != null) canMoveDown = false;
        }

        if (stopFalling && canMoveDown)
        {
            stopFalling = false;
            _resetTime = true;
        }
    }
    
    //MOVES THE PIECE WITHOUT CHANGING THE TIME TO STAY WITHOUT CHECKING AND RETURNS IF IT CAN BE MOVED
    private bool MoveForRotation(Vector2 direction)
    {
        if(pieceList[0] == null || pieceList[1] == null) return false;
        if(fallen) return false;
        
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
        _advisedFromFalling = false;
        _resetTime = true;
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
            if (rotating && i>0)
            {
                rotation = 0;
                pieceList[i].ForceRotation(grid, 0);
            }
            pieceList[i].transform.localScale = Vector3.one*scaleMultiplier;
            position.y += pieceList[i].transform.localScale.y*i;
            pieceList[i].transform.position = position;
        }
    }

    public Piece[] GetPieces()
    {
        return pieceList;
    }
    
}
