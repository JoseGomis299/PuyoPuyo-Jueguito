using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public class PieceController : NetworkBehaviour
{
   [Header("Grid stats")]
   [SerializeField] private Piece[] availablePieces; 
   [SerializeField] private Piece[] garbagePieces; 
   public float fallSpeed = 5;
   [HideInInspector] public int[] piecesNumbers;

   [Header("Grid dimensions and position")]
   [SerializeField] private Vector2 gridSize = new Vector2(6, 14);
   [SerializeField] private float cellSize = 1;

   [Header("Grid Rival")]
   [SerializeField] private PieceController rival;


    private InputManager _inputManager;

   public Block currentBlock;
   public List<Block> currentGarbage;
   private Block _holdBlock;
   private Transform holdTransform;
   public bool held { get; private set; }
   public Block[] nextBlocks;
   private Transform[] nextTransforms;

   public Grid<Piece> _grid  { get; private set; }
   private bool _stopPlacing;

   private LinkedList<Piece> _neighbours;
   private LinkedList<Piece> _pieces;

   private bool _isOnline;
   private bool _doNotGenerate;

   private int combo = 0;
   private int cantidadBasuraTirar = 0;
   private int cantidadBasuraRecibir = 0;
   private bool recibiendoBasura = false;
  
   private void Start()
   {
        currentGarbage = new List<Block>();

       _isOnline = IsClient || IsHost;
       _neighbours = new LinkedList<Piece>();
       _pieces = new LinkedList<Piece>();
       nextBlocks = new Block[2]; 
       piecesNumbers = new int[availablePieces.Length];
       _inputManager = GetComponent<InputManager>();
       InitialPosition();
       _grid = new Grid<Piece>((int)gridSize.x, (int)gridSize.y, cellSize, transform.position);
       if (_isOnline && !IsOwner) { return; }
       
       if(!_isOnline)GenerateBlock();
       else GenerateBlockServerRpc();

   }

   public void GenerateBlock()
   {
        if (!recibiendoBasura)
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
                for (int i = 0; i < nextBlocks.Length - 1; i++)
                {
                    nextBlocks[i] = nextBlocks[i + 1];
                }
                nextBlocks[^1] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                    Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);
            }

            currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
            for (int i = 0; i < nextBlocks.Length; i++)
            {
                nextBlocks[i].SetPosition(nextTransforms[i].position, i == 0 ? 0.75f : 0.75f * 0.5f * i);
            }
            held = false;
        }
        else
        {
            StartCoroutine(recibirBasura());
        } 
   }

   private void Update()
   {
       if (_isOnline && !IsOwner) { return; }
       if(currentBlock == null) return;
      
       currentBlock.Fall(fallSpeed);
        foreach (Block garbage in currentGarbage)
        {
            garbage.Fall(fallSpeed);
        }
       
       if(_inputManager != null)_inputManager.ManageInput();
   }

   public void Hold()
   {
       if(currentBlock ==  null) return;
       if (_holdBlock == null)
       {
           _holdBlock = currentBlock;
           if(!_isOnline)GenerateBlock();
           else OnlineBlockGeneration();
       }
       else
       {
           (_holdBlock, currentBlock) = (currentBlock, _holdBlock);
           currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
       }
       _holdBlock.SetPosition(holdTransform.position, 0.75f);
       held = true;
   }

   public void InstantDown()
   {
       if(currentBlock == null) return;
       if(!_grid.IsInBounds(currentBlock.GetPieces()[1].transform.position) || _grid.GetValue(currentBlock.GetPieces()[0].transform.position)) return;

       if (currentBlock.GetPieces()[1].rotating)
       {
           currentBlock.GetPieces()[1].ForceRotation(_grid, currentBlock.rotation);
       }
       for (int i = 0; i < currentBlock.GetPieces().Length; i++)
       {
           if(currentBlock.GetPieces()[i].fallen) return;
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
        StartCoroutine(_SetPiecesValue()); //Cambio de linea

        if (_stopPlacing) return;
       bool greater = false;
       for (int i = 0; i < piecesNumbers.Length; i++)
       {
           if (piecesNumbers[i] >= 4)
           {
               greater = true;
               break;
           }
       }

       if (!greater)
       {
           if(!_isOnline)GenerateBlock();
           else OnlineBlockGeneration();
           return;
       }

       //StartCoroutine(_SetPiecesValue());
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
                //Combos y Basura
                if (combo == 1)
                {
                    cantidadBasuraTirar -= 1;
                }

                combo++;

                cantidadBasuraTirar += _neighbours.Count - 4;

                if (combo > 1)
                {
                    //int cantidadBasuraTirar = 6 - cantidadBasura;
                    //int cantidadBasuraRecuperar = 6 - cantidadBasuraTirar;


                    /*if (cantidadBasuraRecuperar < 0)
                    {
                        cantidadBasuraRecuperar = 0;
                    }*/

                    /*if (cantidadBasura < 0)
                    {
                        cantidadBasura = 0;
                    }*/
                    cantidadBasuraTirar += 6;
                    //rival.cantidadBasura += cantidadBasuraTirar;
                    Debug.Log("Combo: " + combo + " - " + "Basura: " + cantidadBasuraTirar);
                }
                else if (combo == 1)
                {
                    cantidadBasuraTirar += 1;
                    Debug.Log("Combo: " + combo + " - " + "Basura: " + cantidadBasuraTirar);
                }

                //sumar puntuación aquí, "_neighbours.Count" es el número de piezas que van a explotar
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
        else
        {
            //hacer lo que tenga que hacer el combo
            if(!_isOnline)GenerateBlock();
            else OnlineBlockGeneration();
            if (combo > 0)
            {
                rival.lanzarBasura(cantidadBasuraTirar);
                cantidadBasuraTirar = 0;
                combo = 0;
            }

        }
        yield return null;
    }

    private void lanzarBasura(int numBasura)
    {
        cantidadBasuraRecibir = numBasura;
        recibiendoBasura = true;
    }

    private IEnumerator recibirBasura()
    {
        yield return new WaitForSeconds(0.25f);
        //Recibir Basura
        int posY = 0;
        int posX = 0;
        for (int i = 0; i < cantidadBasuraRecibir; i++)
        {
            if (posX >= 6)
            {
                posX = 0;
                posY++;
            }


            Block g = new Block(Instantiate(garbagePieces[0]), _grid, this);
            g.SetPositionInGrid(posX, _grid.GetHeight() + posY);
            currentGarbage.Add(g);
            posX++;
        }
        Debug.Log("Recibiendo Basura");

        
        //Esperar a que esten todas colocadas
        yield return new WaitForSeconds(3);

        //Continuar
        recibiendoBasura = false;
        GenerateBlock();
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
                    Destroy(_grid.GetValue(x, y).gameObject);
                    _grid.SetValue(x, y, null);
                }
            }
        }

        foreach (var piece in currentBlock.GetPieces())
        {
            Destroy(piece.gameObject);
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

    private void InitialPosition()
    {
        if (!_isOnline)
        {
            if (_inputManager.playerTwo)
            {
                transform.position = Vector3.right * 3;
                holdTransform = GameObject.Find("HoldPosR").transform;
                nextTransforms = new[]
                    { GameObject.Find("NextPosR").transform, GameObject.Find("NextPos2R").transform };
            }
            else
            {
                transform.position = Vector3.right * -9;
                holdTransform = GameObject.Find("HoldPosL").transform;
                nextTransforms = new[]
                    { GameObject.Find("NextPosL").transform, GameObject.Find("NextPos2L").transform };
            }
        }
        else
        {
            if (IsOwner)
            {
                transform.position = Vector3.right*-9;
                holdTransform = GameObject.Find("HoldPosL").transform;
                nextTransforms = new []{GameObject.Find("NextPosL").transform, GameObject.Find("NextPos2L").transform};
            }
            else
            {
                 transform.position = Vector3.right*3;
                 holdTransform = GameObject.Find("HoldPosR").transform;
                 nextTransforms = new []{GameObject.Find("NextPosR").transform, GameObject.Find("NextPos2R").transform};
            }
        }
    }
    
    private void OnlineBlockGeneration()
    {
        GenerateBlockServerRpc();
        held = false;
    }

    [ServerRpc]
    private void GenerateBlockServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var id = serverRpcParams.Receive.SenderClientId;
        if (!NetworkManager.ConnectedClients.ContainsKey(id)) return;
        var pieceController = NetworkManager.ConnectedClients[id].PlayerObject.gameObject.GetComponent<PieceController>();
        pieceController.gameObject.name = "Grid " + id;

        if (currentBlock == null)
        {
            currentBlock = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);
                
            for (int i = 0; i < nextBlocks.Length; i++)
            {
                nextBlocks[i] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                    Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), this._grid, this);
                
                nextBlocks[i].GetPieces()[0].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
                nextBlocks[i].GetPieces()[1].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
            }
            currentBlock.GetPieces()[0].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
            currentBlock.GetPieces()[1].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
        }
        else
        {
            currentBlock = nextBlocks[0];
    
            for (int i = 0; i < nextBlocks.Length-1; i++)
            {
                nextBlocks[i] = nextBlocks[i + 1];
            }
            nextBlocks[^1] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), this._grid, this);

            nextBlocks[^1].GetPieces()[0].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
            nextBlocks[^1].GetPieces()[1].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
        }

        pieceController.currentBlock = currentBlock;
        pieceController.nextBlocks = nextBlocks;
        
        currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
        pieceController.currentBlock.SetPositionInGrid(Random.Range(0, pieceController._grid.GetWidth()), pieceController._grid.GetHeight());
        for (int i = 0; i < nextBlocks.Length; i++)
        {
            nextBlocks[i].SetPosition(nextTransforms[i].position, i== 0 ? 0.75f : 0.75f*0.5f*i);
            pieceController.nextBlocks[i].SetPosition(pieceController.nextTransforms[i].position, i== 0 ? 0.75f : 0.75f*0.5f*i);
        }
        
        NetworkObjectReference[] currentPieces = {
            new(currentBlock.GetPieces()[0].GetComponent<NetworkObject>()),
            new(currentBlock.GetPieces()[1].GetComponent<NetworkObject>())
        };
        NetworkObjectReference[] nextPieces1 = {
            new(nextBlocks[0].GetPieces()[0].GetComponent<NetworkObject>()),
            new(nextBlocks[0].GetPieces()[1].GetComponent<NetworkObject>())
        };
        NetworkObjectReference[] nextPieces2 = {
            new(nextBlocks[1].GetPieces()[0].GetComponent<NetworkObject>()),
            new(nextBlocks[1].GetPieces()[1].GetComponent<NetworkObject>())
        };
        Debug.Log(id);
        SendClientsBlocksClientRpc(id, currentPieces, nextPieces1, nextPieces2);
    }

    [ClientRpc]
    private  void SendClientsBlocksClientRpc(ulong id, NetworkObjectReference[] currentPieces, NetworkObjectReference[] nextPieces1, NetworkObjectReference[] nextPieces2)
    {
        if (!IsOwnedByServer)
        {
            gameObject.name = "GridClient " + id;
            currentPieces[0].TryGet(out var piece0);
            currentPieces[1].TryGet(out var piece1);
            currentBlock = new Block(piece0.GetComponent<Piece>(), piece1.GetComponent<Piece>(), _grid, this); 
            nextPieces1[0].TryGet(out  piece0);
            nextPieces1[1].TryGet(out  piece1);
            nextBlocks[0] = new Block(piece0.GetComponent<Piece>(), piece1.GetComponent<Piece>(), _grid, this); 
            nextPieces2[0].TryGet(out  piece0);
            nextPieces2[1].TryGet(out  piece1);
            nextBlocks[1] = new Block(piece0.GetComponent<Piece>(), piece1.GetComponent<Piece>(), _grid, this);
            currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
            for (int i = 0; i < nextBlocks.Length; i++)
            {
                nextBlocks[i].SetPosition(nextTransforms[i].position, i== 0 ? 0.75f : 0.75f*0.5f*i);
            }
        }
    }
    
}
