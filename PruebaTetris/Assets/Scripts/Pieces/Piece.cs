using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    public bool check;
    public bool fallen { get; private set; }
    public bool justFallen;
    public bool exploded { get; protected set; }

    private Block _block;
    private bool _advisedFromFalling;
    public bool doNotSetTime;

    public bool rotating{ get; protected set; }

    public void SetBlockReference(Block block)
    {
        _block = block;
    }
    
    public void SetAdvice(bool value)
    {
        _advisedFromFalling = value;
    }
    
    public void CheckNeighbours(Grid<Piece> grid, LinkedList<Piece> list)
    {
        grid.GetXY(transform.position, out var x, out var y);
        check = true;
        
        Piece right = grid.GetValue(x + 1, y);
        Piece left = grid.GetValue(x - 1, y);
        Piece up = grid.GetValue(x, y+1);
        Piece down = grid.GetValue(x, y-1);
        
        //RIGHT
        if (right != null && !right.exploded && right.Equals(this) && !right.check)
        {
            grid.GetValue(x + 1, y).CheckNeighbours(grid, list);
        } 
        //LEFT
        if (left != null && !left.exploded && left.Equals(this) && !left.check)   
        {
            grid.GetValue(x - 1, y).CheckNeighbours(grid, list);
        } 
        //UP
        if (up != null && !up.exploded && up.Equals(this) && !up.check)    
        {
            grid.GetValue(x, y+1).CheckNeighbours(grid, list);
        }
        //DOWN
        if (down != null && !down.exploded && down.Equals(this) && !down.check)    
        {
            grid.GetValue(x , y-1).CheckNeighbours(grid, list);
        }
        list.AddLast(this);
    }

    public bool FallCoroutine(Grid<Piece> grid, float fallSpeed, PieceController pieceController)
    {
        var movedPiecePosition = new Vector3(transform.position.x, transform.position.y+ transform.position.z) + Vector3.down * (fallSpeed * Time.deltaTime);
        check = false;

        grid.GetXY(transform.position, out var x, out var y);

        if ((grid.GetValue(x,y-1) == null || grid.GetValue(x,y-1) != null && !grid.GetValue(x,y-1).fallen) && grid.IsInBoundsNoHeight(x,y-1))
        {
            fallen = false;
            justFallen = true;
            _advisedFromFalling = true;
            pieceController.AddToPieceNumber(this, -1);
            StartCoroutine(DoFall(grid, fallSpeed, pieceController));
            return false;
        }
        
        return true;
    }

    private IEnumerator DoFall(Grid<Piece> grid, float fallSpeed, PieceController pieceController)
    {
        yield return null;
        while (!Fall(grid, fallSpeed*3, pieceController))
        {
            yield return null;
        }
    }

    public bool Fall(Grid<Piece> grid, float fallSpeed, PieceController pieceController)
    {
        if (fallen || _block.stopFalling) return true;
        
        grid.GetXY(transform.position, out var x, out var y);

        if ((grid.GetValue(x,y-1) == null || grid.GetValue(x,y-1) != null && !grid.GetValue(x,y-1).fallen) && grid.IsInBoundsNoHeight(x,y-1))
        {
            transform.Translate(Vector3.down * (fallSpeed * Time.deltaTime));
            return false;
        }

        if (!_advisedFromFalling)
        {
            _block.stopFalling = true;
            if(!doNotSetTime)_block.lastFallenTime = Time.time;
            doNotSetTime = true;
            foreach (var piece in _block.GetPieces())
            {
                if(piece == null) continue;
                piece.SetAdvice(true);
            }

            return false;
        }

        justFallen = true;
        fallen = true;

        if (!grid.IsInBoundsNoHeight(x, y) || grid.GetValue(x,y) != null) y++;
        transform.position = grid.GetCellCenter(x, y);
        
        if (y >= grid.GetHeight())
        {
            pieceController.CleanStage();
            Destroy(gameObject);
            return true;
        }
        
        grid.SetValue(x,y, this);
        pieceController.AddToPieceNumber(this, 1);
        return true;
    }

    public void Rotate(Grid<Piece> grid, float finalRotation, float rotation)
    {
        if(!rotating)StartCoroutine(DoRotation(grid, finalRotation, rotation));
    }

    public void ForceRotation(Grid<Piece> grid, float finalRotation)
    {
        StopAllCoroutines();
        finalRotation *=  Mathf.Deg2Rad;

        var targetX = Mathf.Sin(finalRotation);
        var targetY = Mathf.Cos(finalRotation);
        
        transform.position = _block.GetPieces()[0].transform.position + new Vector3(targetX, targetY);
        
        if (!grid.IsInBoundsNoHeight(transform.position) || grid.GetValue(transform.position) != null)
        {
            _block.Move(new Vector2(-targetX, -targetY));
        }
    }

    private IEnumerator DoRotation(Grid<Piece> grid, float finalRotation, float rotation)
    {
        rotating = true;
        
        var currentRotation = finalRotation-rotation;
        currentRotation *= Mathf.Deg2Rad;
        finalRotation *=  Mathf.Deg2Rad;

        var x = Mathf.Sin(currentRotation);
        var y = Mathf.Cos(currentRotation);

        var targetX = Mathf.Sin(finalRotation);
        var targetY = Mathf.Cos(finalRotation);
        
        while (Math.Abs(x - targetX) > 0.01f && Math.Abs(y - targetY) > 0.01f)
        {
            Vector3 center = _block.GetPieces()[0].transform.position;
            currentRotation = Mathf.Lerp(currentRotation, finalRotation, Time.deltaTime * 15);
            x = Mathf.Sin(currentRotation);
            y = Mathf.Cos(currentRotation);
            var finalPos = center;
            finalPos.x += x;
            finalPos.y += y;
            transform.position = finalPos;
            yield return null;
        }

        if (fallen) transform.position = grid.GetCellCenter(transform.position);
        else
        {
            transform.position = _block.GetPieces()[0].transform.position + new Vector3(targetX, targetY);
        }
        
        if (!grid.IsInBoundsNoHeight(transform.position) || grid.GetValue(transform.position) != null)
        {
            _block.Move(new Vector2(-targetX, -targetY));
        }
        rotating = false;
    }
    public abstract void Explode(Grid<Piece> grid);
    public abstract IEnumerator Explosion(Grid<Piece> grid);
    public abstract bool Equals(Piece piece);
    
}
