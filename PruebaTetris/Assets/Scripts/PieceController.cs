using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public class PieceController : MonoBehaviour
{
    [Header("Grid stats")]
   [SerializeField] private Piece[] availablePieces; 
   [SerializeField] private float fallSpeed = 5;
   [HideInInspector] public int[] piecesNumbers;

   [Header("Grid dimensions and position")]
   [SerializeField] private Vector2 gridSize = new Vector2(6, 14);
   [SerializeField] private float cellSize = 1;
   [SerializeField] private Vector3 initialPos;

   [Header("Key Binds")]
   [SerializeField] private KeyCode moveRight = KeyCode.D;
   [SerializeField] private KeyCode moveLeft = KeyCode.A;
   [SerializeField] private KeyCode moveDown = KeyCode.S;
   [SerializeField] private KeyCode rotateRight = KeyCode.W;
   [SerializeField] private KeyCode rotateLeft = KeyCode.Q;
   [SerializeField] private KeyCode hold = KeyCode.F;
   [SerializeField] private KeyCode instantDown = KeyCode.Space;

   [Header("Controls")] 
   [SerializeField] private float moveCooldown = 0.1f;
   private float _lastMove;
   [SerializeField] private float rotationCooldown = 0.1f;
   private float _lastRotation;
   [SerializeField] private float fallSpeedBoost = 2f;

   private Block currentBlock;
   private Block holdBlock;
   [SerializeField] private Transform holdTransform;
   private bool _held;
   private Block[] nextBlocks;
   [SerializeField] private Transform[] nextTransforms;

   private Grid<Piece> _grid;
   private bool stopPlacing;

   private LinkedList<Piece> neighbours;
   private LinkedList<Piece> pieces;

   private void Awake()
   {
       neighbours = new LinkedList<Piece>();
       pieces = new LinkedList<Piece>();
       nextBlocks = new Block[2];
       piecesNumbers = new int[availablePieces.Length];
      _grid = new Grid<Piece>((int)gridSize.x, (int)gridSize.y, cellSize, initialPos);
      GenerateBlock();
   }

   public void GenerateBlock()
   {
       if (nextBlocks[0] == null)
       {
           currentBlock = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
               Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);

           for (int i = 0; i < nextBlocks.Length; i++)
           {
               nextBlocks[i] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                   Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);
           }
       }
       else
       {
           currentBlock = nextBlocks[0];
           for (int i = 0; i < nextBlocks.Length-1; i++)
           {
               nextBlocks[i] = nextBlocks[i + 1];
           }
           nextBlocks[^1] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
               Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);
       }
       
       currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
      for (int i = 0; i < nextBlocks.Length; i++)
      {
          nextBlocks[i].SetPosition(nextTransforms[i].position, i== 0 ? 0.75f : 0.75f*0.5f*i);
      }
      _held = false;
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
           currentBlock.Move(new Vector2(-1, 0));
       }

       if (Input.GetKey(moveRight) && Time.time - _lastMove >= moveCooldown)
       {
           _lastMove = Time.time;
           currentBlock.Move(new Vector2(1, 0));
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

       if (Input.GetKeyDown(hold) && !currentBlock.fallen && !_held)
       {
           if (holdBlock == null)
           {
               holdBlock = currentBlock;
               GenerateBlock();
           }
           else
           {
               (holdBlock, currentBlock) = (currentBlock, holdBlock);
               currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
           }
           holdBlock.SetPosition(holdTransform.position, 0.75f);
           _held = true;
       }

       if (Input.GetKeyDown(instantDown))
       {
           for (int i = 0; i < currentBlock.GetPieces().Length; i++)
           {
               var position = CalculateFinalPiecePosition(currentBlock.GetPieces()[i].transform.position);
               switch (currentBlock.rotation)
               {
                   case 0: currentBlock.GetPieces()[i].transform.position = _grid.GetCellCenter((int)position.x,(int)position.y+i);
                       break;
                   case 180: currentBlock.GetPieces()[i].transform.position = _grid.GetCellCenter((int)position.x,(int)position.y+(currentBlock.GetPieces().Length-1)-i);
                       break;
                   default: currentBlock.GetPieces()[i].transform.position = _grid.GetCellCenter((int)position.x,(int)position.y);
                       break;
               }
               currentBlock.GetPieces()[i].SetAdvice(true);
           }
       }
   }

   private Vector2 CalculateFinalPiecePosition(Vector3 origin)
   {
       _grid.GetXY(origin, out var x, out var y);
       for (int i = 0; i < _grid.GetHeight(); i++)
       {
           if (_grid.GetValue(x,i) != null) continue;
           return new Vector2(x, i);
       }
       return Vector2.zero;
   }
   private void MakeAllFall()
   {
       for (int y = 0; y < _grid.GetHeight(); y++)
       {
           for (int x = 0; x < _grid.GetWidth(); x++)
           {
               if (_grid.GetValue(x,y) != null)
               {
                   if (!_grid.GetValue(x,y).FallCoroutine(_grid, fallSpeed, this))
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
        GetActivePieces();

        foreach (var piece in pieces)
        {
            if (piece == null || piece.exploded) continue;
            neighbours.Clear();
            
            piece.justFallen = false;
            piece.CheckNeighbours(_grid, neighbours);

            if (neighbours.Count >= 4)
            {
                foreach (var p in neighbours)
                {
                    AddToPieceNumber(p, -1);
                    p.Explode(_grid);
                }
            }
            else
            {
                foreach (var p in neighbours)
                {
                    p.check = false;
                }
            }
        }
        
        //Wait until all have exploded
        yield return new WaitUntil(() =>
        {
            bool allExploded = true;
            foreach (var piece in pieces)
            {
                if(piece == null) continue; 
                if(piece.exploded) allExploded = false;
            }
            return allExploded;
        } );
        
        GetAllPieces();
        MakeAllFall();
        
        //Wait until all have landed again
        yield return new WaitUntil(() =>
        {
            foreach (var piece in pieces)
            {
                if(piece == null) continue; 
                if (!piece.fallen)
                {
                    return false;
                }
            }
            return true;
        } );
        
        //If there it is a combo start again
        bool startAgain = false;
        GetActivePieces();
        foreach (var piece in pieces)
        {
            if (piece == null || piece.exploded) continue;
            
            neighbours.Clear();
            piece.CheckNeighbours( _grid,  neighbours);

            foreach (var p in neighbours)
            {
                p.check = false;
            }
            
            if (neighbours.Count < 4) continue;
            
            startAgain = true;
            break;
        }

        stopPlacing = startAgain;
        if (startAgain) StartCoroutine(_SetPiecesValue());
        yield return null;
    }

    private void GetActivePieces()
    {
        pieces.Clear();

        for (int x = 0; x < _grid.GetWidth(); x++)
        {
            for (int y = 0; y < _grid.GetHeight(); y++)
            {
                if (_grid.GetValue(x, y) != null && !_grid.GetValue(x, y).exploded && _grid.GetValue(x, y).justFallen)
                {
                    pieces.AddLast(_grid.GetValue(x, y));
                }
            }
        }
    }
    private void GetAllPieces()
    { 
        pieces.Clear();
        
        for (int x = 0; x < _grid.GetWidth(); x++)
        {
            for (int y = 0; y < _grid.GetHeight(); y++)
            {
                if (_grid.GetValue(x, y) != null && !_grid.GetValue(x, y).exploded)
                {
                    pieces.AddLast(_grid.GetValue(x, y));
                }
            }
        }
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
    }
    public void AddToPieceNumber(Piece piece, int value)
    {
        for (int i = 0; i < availablePieces.Length; i++)
        {
            if (!availablePieces[i].Equals(piece)) continue;

            piecesNumbers[i] += value;
            break;
        }
    }
}
