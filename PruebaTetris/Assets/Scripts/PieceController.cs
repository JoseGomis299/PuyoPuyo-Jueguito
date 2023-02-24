using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PieceController : MonoBehaviour
{
    [Header("Grid stats")]
   [SerializeField] private Piece[] availablePieces; 
   [SerializeField] private float fallSpeed = 5;
   [SerializeField] private Vector3 initialPos;
   [SerializeField] private Vector2 gridSize = new Vector2(6, 14);
   [SerializeField] private float cellSize = 1;

   [Header("Key Binds")]
   [SerializeField] private KeyCode moveRight = KeyCode.D;
   [SerializeField] private KeyCode moveLeft = KeyCode.A;
   [SerializeField] private KeyCode moveDown = KeyCode.S;
   [SerializeField] private KeyCode rotateRight = KeyCode.E;
   [SerializeField] private KeyCode rotateLeft = KeyCode.Q;

   [Header("Controls")] 
   [SerializeField] private float moveCooldown = 0.1f;
   private float _lastMove;
   [SerializeField] private float rotationCooldown = 0.1f;
   private float _lastRotation;
   [SerializeField] private float fallSpeedBoost = 2f;

   private Block currentBlock;
   public int[] piecesNumbers;
   private int piecesNumber;
   private Grid<Piece> _grid;
   private bool stopPlacing;
   
   private void Awake()
   {
       piecesNumber = 0;
       piecesNumbers = new int[availablePieces.Length];
      _grid = new Grid<Piece>((int)gridSize.x, (int)gridSize.y, cellSize, initialPos);
      GenerateBlock();
   }

   public void GenerateBlock()
   {
      currentBlock = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
         Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);
      
      currentBlock.SetPosition(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
   }

   private void Update()
   {
      if(currentBlock.fallen && !stopPlacing) GenerateBlock();
      currentBlock.Fall(fallSpeed);

      ManageInput();

      //_fallSpeed += Time.deltaTime / 10f;
   }

   private void ManageInput()
   {
       if (Input.GetKey(moveLeft) && Time.time - _lastMove >= moveCooldown)
       {
           _lastMove = Time.time;
           currentBlock.Move(-1);
       }

       if (Input.GetKey(moveRight) && Time.time - _lastMove >= moveCooldown)
       {
           _lastMove = Time.time;
           currentBlock.Move(1);
       }

       if (Input.GetKey(rotateRight) && Time.time - _lastRotation >= rotationCooldown)
       {
           _lastRotation = Time.time;
           currentBlock.Rotate(90);
       }

       if (Input.GetKey(rotateLeft) && Time.time - _lastRotation >= rotationCooldown)
       {
           _lastRotation = Time.time;
           currentBlock.Rotate(-90);
       }

       if (Input.GetKeyDown(moveDown))
       {
           fallSpeed *= fallSpeedBoost;
       }

       if (Input.GetKeyUp(moveDown))
       {
           fallSpeed /= fallSpeedBoost;
       }
   }

   public void MakeAllFall()
   {
       for (int y = 0; y < _grid.GetHeight(); y++)
       {
           for (int x = 0; x < _grid.GetWidth(); x++)
           {
               if (_grid.GetValue(x,y) != null)
               {
                   if (!_grid.GetValue(x,y).FallCoroutine(ref _grid, fallSpeed, this))
                   {
                       _grid.SetValue(_grid.GetValue(x,y).transform.position, null);
                   }
               }
           }
       }
   }

   public void SetPiecesValue()
   {
       if(stopPlacing) return;
       bool greater = false;
       for (int i = 0; i < piecesNumbers.Length; i++)
       {
           if (piecesNumbers[i] >= 4)
           {
               greater = true;
               break;
           }
       }
       if(!greater) return;

       StartCoroutine(_SetPiecesValue());
   }
   
    private IEnumerator _SetPiecesValue()
    {
        stopPlacing = true;

        var pieces = GetActivePieces();

        for (int i = 0; i < pieces.Length; i++)
        {
            var list = new List<Piece>();
          
            //check neighbours
            if(pieces[i] != null) pieces[i].CheckNeighbours(ref _grid, ref list);
            //reset checks
            foreach (var piece in pieces)
            {
                if(piece != null) piece.check = false;
            }
            if (list.Count >= 4)
            {
                foreach (var p in list) {p.GetComponent<SpriteRenderer>().color = Color.white;}
                yield return new WaitForSeconds(0.1f);
                foreach (var p in list)
                {
                    AddToPieceNumber(p, -1);
                    p.Explode(ref _grid);
                }
                i = -1;
            }
            pieces = GetActivePieces();
        }

        MakeAllFall();
        //Wait until all have landed again
        yield return new WaitUntil(() =>
        {
            bool res = true;
            foreach (var piece in pieces)
            {
                if (piece != null && !piece.fallen) res = false;
            }

            return res;
        } );
        
        //reset checks
        // foreach (var piece in pieces)
        // {
        //     if(piece != null) piece.check = false;
        // }
        //If there it is a combo start again
        bool stop = false;
        
        foreach (var piece in pieces)
        {
            if (piece == null) continue;
            
            var list = new List<Piece>();
            piece.CheckNeighbours(ref _grid, ref list);
            
            if (list.Count < 4) continue;
            
            stop = true;
            break;
        }
        stopPlacing = stop;
        if (stop) StartCoroutine(_SetPiecesValue());
        yield return null;
    }

    private Piece[] GetActivePieces()
    {
        Piece[] pieces = new Piece[piecesNumber];
        int n = 0;

        for (int x = 0; x < _grid.GetWidth(); x++)
        {
            for (int y = 0; y < _grid.GetHeight(); y++)
            {
                if (_grid.GetValue(x, y) != null)
                {
                    pieces[n] = _grid.GetValue(x, y);
                    n++;
                }
            }
        }

        return pieces;
    }

    public void CleanStage()
    {
        for (int x = 0; x < _grid.GetWidth(); x++)
        {
            for (int y = 0; y < _grid.GetHeight(); y++)
            {
                if (_grid.GetValue(x, y) != null)
                {
                    GameObject.Destroy(_grid.GetValue(x, y).gameObject);
                    _grid.SetValue(x, y, null);
                }
            }
        }
        for (int i = 0; i < availablePieces.Length; i++)
        {
            piecesNumbers[i] = 0;
        }
        piecesNumber = 0;
    }
    public void AddToPieceNumber(Piece piece, int value)
    {
        for (int i = 0; i < availablePieces.Length; i++)
        {
            if (!availablePieces[i].Equals(piece)) continue;

            piecesNumbers[i] += value;
            break;
        }

        piecesNumber += value;
    }
}
