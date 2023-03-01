using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public class PieceController : MonoBehaviour
{
    [Header("Grid stats")]
   [SerializeField] private Piece[] availablePieces; 
   public float fallSpeed = 5;
   [HideInInspector] public int[] piecesNumbers;

   [Header("Grid dimensions and position")]
   [SerializeField] private Vector2 gridSize = new Vector2(6, 14);
   [SerializeField] private float cellSize = 1;
   [SerializeField] private Vector3 initialPos;

   private InputManager _inputManager;

   public Block _currentBlock { get; private set; }
   private Block _holdBlock;
   [SerializeField] private Transform holdTransform;
   public bool held{ get; private set; }
   private Block[] _nextBlocks;
   [SerializeField] private Transform[] nextTransforms;

   private Grid<Piece> _grid;
   private bool _stopPlacing;

   private LinkedList<Piece> _neighbours;
   private LinkedList<Piece> _pieces;

   private void Awake()
   {
       _neighbours = new LinkedList<Piece>();
       _inputManager = GetComponent<InputManager>();
       _pieces = new LinkedList<Piece>();
       _nextBlocks = new Block[2];
       piecesNumbers = new int[availablePieces.Length];
      _grid = new Grid<Piece>((int)gridSize.x, (int)gridSize.y, cellSize, initialPos);
      GenerateBlock();
   }
   public void GenerateBlock()
   {
       if (_nextBlocks[0] == null)
       {
           _currentBlock = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
               Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);

           for (int i = 0; i < _nextBlocks.Length; i++)
           {
               _nextBlocks[i] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                   Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);
           }
       }
       else
       {
           _currentBlock = _nextBlocks[0];
           for (int i = 0; i < _nextBlocks.Length-1; i++)
           {
               _nextBlocks[i] = _nextBlocks[i + 1];
           }
           _nextBlocks[^1] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
               Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);
       }
       
       _currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
      for (int i = 0; i < _nextBlocks.Length; i++)
      {
          _nextBlocks[i].SetPosition(nextTransforms[i].position, i== 0 ? 0.75f : 0.75f*0.5f*i);
      }
      held = false;
   }

   private void Update()
   {
      if(_currentBlock.fallen && !_stopPlacing) GenerateBlock();
      _currentBlock.Fall(fallSpeed);

      _inputManager.ManageInput();
   }

   public void Hold()
   {
       if (_holdBlock == null)
       {
           _holdBlock = _currentBlock;
           GenerateBlock();
       }
       else
       {
           (_holdBlock, _currentBlock) = (_currentBlock, _holdBlock);
           _currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
       }
       _holdBlock.SetPosition(holdTransform.position, 0.75f);
       held = true;
   }

   public void InstantDown()
   {
       if (_currentBlock.GetPieces()[1].rotating)
       {
           _currentBlock.GetPieces()[1].ForceRotation(_grid, _currentBlock.rotation);
       }
       for (int i = 0; i < _currentBlock.GetPieces().Length; i++)
       {
           if(_currentBlock.GetPieces()[i].fallen) return;
           var position = CalculateFinalPiecePosition(_currentBlock.GetPieces()[i].transform.position);
           switch (_currentBlock.rotation)
           {
               case 0: _currentBlock.GetPieces()[i].transform.position = _grid.GetCellCenter((int)position.x,(int)position.y+i);
                   break;
               case 180: _currentBlock.GetPieces()[i].transform.position = _grid.GetCellCenter((int)position.x,(int)position.y+(_currentBlock.GetPieces().Length-1)-i);
                   break;
               default: _currentBlock.GetPieces()[i].transform.position = _grid.GetCellCenter((int)position.x,(int)position.y);
                   break;
           }
           _currentBlock.GetPieces()[i].SetAdvice(true);
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
       if(_stopPlacing) return;
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
        _stopPlacing = true;
        GetActivePieces();

        foreach (var piece in _pieces)
        {
            if (piece == null || piece.exploded) continue;
            _neighbours.Clear();
            
            piece.justFallen = false;
            piece.CheckNeighbours(_grid, _neighbours);

            if (_neighbours.Count >= 4)
            {
                foreach (var p in _neighbours)
                {
                    AddToPieceNumber(p, -1);
                    p.Explode(_grid);
                }
            }
            else
            {
                foreach (var p in _neighbours)
                {
                    p.check = false;
                }
            }
        }
        
        //Wait until all have exploded
        yield return new WaitUntil(() =>
        {
            bool allExploded = true;
            foreach (var piece in _pieces)
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
            foreach (var piece in _pieces)
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
        foreach (var piece in _pieces)
        {
            if (piece == null || piece.exploded) continue;
            
            _neighbours.Clear();
            piece.CheckNeighbours( _grid,  _neighbours);

            foreach (var p in _neighbours)
            {
                p.check = false;
            }
            
            if (_neighbours.Count < 4) continue;
            
            startAgain = true;
            break;
        }

        _stopPlacing = startAgain;
        if (startAgain) StartCoroutine(_SetPiecesValue());
        yield return null;
    }

    private void GetActivePieces()
    {
        _pieces.Clear();

        for (int x = 0; x < _grid.GetWidth(); x++)
        {
            for (int y = 0; y < _grid.GetHeight(); y++)
            {
                if (_grid.GetValue(x, y) != null && !_grid.GetValue(x, y).exploded && _grid.GetValue(x, y).justFallen)
                {
                    _pieces.AddLast(_grid.GetValue(x, y));
                }
            }
        }
    }
    private void GetAllPieces()
    { 
        _pieces.Clear();
        
        for (int x = 0; x < _grid.GetWidth(); x++)
        {
            for (int y = 0; y < _grid.GetHeight(); y++)
            {
                if (_grid.GetValue(x, y) != null && !_grid.GetValue(x, y).exploded)
                {
                    _pieces.AddLast(_grid.GetValue(x, y));
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
