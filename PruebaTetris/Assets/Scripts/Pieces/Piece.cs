using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece : MonoBehaviour
{
    public bool check;
    public bool fallen { get; private set; }
    
    public void CheckNeighbours(ref Grid<Piece> grid, ref List<Piece> list)
    {
        grid.GetXY(transform.position, out var x, out var y);
        check = true;
        
        //RIGHT
        if (grid.GetValue(x + 1, y) != null && grid.GetValue(x + 1, y).Equals(this) && !grid.GetValue(x + 1, y).check)
        {
            grid.GetValue(x + 1, y).CheckNeighbours(ref grid, ref list);
        } 
        //LEFT
        if (grid.GetValue(x - 1, y) != null && grid.GetValue(x - 1, y).Equals(this) && !grid.GetValue(x - 1, y).check)   
        {
            grid.GetValue(x - 1, y).CheckNeighbours(ref grid, ref list);
        } 
        //UP
        if (grid.GetValue(x , y+1) != null && grid.GetValue(x, y+1).Equals(this) && !grid.GetValue(x , y+1).check)    
        {
            grid.GetValue(x, y+1).CheckNeighbours( ref grid, ref list);
        }
        //DOWN
        if (grid.GetValue(x, y-1) != null && grid.GetValue(x, y-1).Equals(this) && !grid.GetValue(x, y-1).check)    
        {
            grid.GetValue(x , y-1).CheckNeighbours(ref grid,ref list);
        }
        list.Add(this);
    }

    public bool FallCoroutine(ref Grid<Piece> grid, float fallSpeed, PieceController pieceController)
    {
        var movedPiecePosition = new Vector3(transform.position.x, transform.position.y+ transform.position.z) + Vector3.down * (fallSpeed * Time.deltaTime);
        check = false;

        grid.GetXY(transform.position, out var x, out var y);

        if ((grid.GetValue(x,y-1) == null || grid.GetValue(x,y-1) != null && !grid.GetValue(x,y-1).fallen) && grid.IsInBoundsNoHeight(x,y-1))
        {
            fallen = false;
            pieceController.AddToPieceNumber(this, -1);
            StartCoroutine(DoFall(grid, fallSpeed, pieceController));
            return false;
        }
        
        return true;
    }

    private IEnumerator DoFall(Grid<Piece> grid, float fallSpeed, PieceController pieceController)
    {
        yield return null;
        while (!Fall(ref grid, fallSpeed, pieceController))
        {
            yield return null;
        }
    }

    public bool Fall(ref Grid<Piece> grid, float fallSpeed, PieceController pieceController)
    {
        if (fallen) return true;
        
        var pieceTop = new Vector3(transform.position.x, transform.position.y + (transform.localScale.y / 2), transform.position.z); 
        var movedPiecePosition = pieceTop + Vector3.down * (fallSpeed * Time.deltaTime);
        
        grid.GetXY(transform.position, out var x, out var y);

        if ((grid.GetValue(x,y-1) == null || grid.GetValue(x,y-1) != null && !grid.GetValue(x,y-1).fallen) && grid.IsInBoundsNoHeight(x,y-1))
        {
            transform.Translate(Vector3.down * (fallSpeed * Time.deltaTime));
            return false;
        }
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

    public Vector3 GetRealPos()
    {
        return transform.position - transform.localScale / 2;
    }
    
    public abstract void Explode(ref Grid<Piece> grid);
    public abstract bool Equals(Piece piece);
    
}
