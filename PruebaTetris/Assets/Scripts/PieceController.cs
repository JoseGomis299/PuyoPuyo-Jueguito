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
   [HideInInspector] public float fallSpeedDelta = 1;

   [Header("Grid dimensions and position")]
   [SerializeField] private Vector2 gridSize = new Vector2(6, 14);
   [SerializeField] private float cellSize = 1;
   
   private InputManager _inputManager;

   public Block currentBlock;
   public LinkedList<Piece> currentGarbage;
   private Block _holdBlock;
   private Transform holdTransform;
   public bool held { get; private set; }
   public Block[] nextBlocks;
   private Transform[] nextTransforms;

   public Grid<Piece> _grid  { get; private set; }
   private bool _stopPlacing;

   private LinkedList<Piece> _neighbours;
   private LinkedList<Piece> _garbage;
   private LinkedList<Piece> _pieces;

   private bool _isOnline;
   private bool _doNotGenerate;

   private int combo = 0;
   private int cantidadBasuraTirar = 0;
   private int cantidadBasuraRecibir = 0;
   private bool recibiendoBasura = false;

   private void Start()
   {
        currentGarbage = new LinkedList<Piece>();

       _isOnline = NetworkManager != null;
       _neighbours = new LinkedList<Piece>();
       _pieces = new LinkedList<Piece>();
       _garbage = new LinkedList<Piece>();
       nextBlocks = new Block[2]; 
       _inputManager = GetComponent<InputManager>();
       InitialPosition();
       _grid = new Grid<Piece>((int)gridSize.x, (int)gridSize.y, cellSize, transform.position);
       if (_isOnline && !IsOwner) { return; }
       
       if(!_isOnline)GenerateBlock();
       else GenerateBlockServerRpc(recibiendoBasura);

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
            StopAllCoroutines();
            StartCoroutine(recibirBasura());
        } 
   }

   private void Update()
   {
       if (_isOnline && !IsOwner) { return; }
       if(currentBlock == null) return;

       fallSpeed += (Time.deltaTime / 20)*fallSpeedDelta;
       currentBlock.Fall(fallSpeed);       
       if(_inputManager != null)_inputManager.ManageInput();
   }

   public void Hold()
   {
       if(currentBlock ==  null) return;
       if (_holdBlock == null)
       {
           _holdBlock = currentBlock;

           currentBlock = null;
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

       currentBlock.fallen = true;
       if (currentBlock.rotating)
       {
           currentBlock.GetPieces()[1].ForceRotation(_grid, currentBlock.rotation);
       }
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
       }

       SetPiecesValue();
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
       if(_stopPlacing || (_isOnline && !IsOwner)) return;
       
       StartCoroutine(_SetPiecesValue());
   }
   
    private IEnumerator _SetPiecesValue()
    {
        if (currentBlock != null)
        {
            foreach (var piece in currentBlock.GetPieces())
            {
                piece.SetValue(_grid, this);
            }
            
            GetAllPieces();
            MakeAllFall();

            //Wait until all have landed again
            yield return new WaitUntil(() =>
            {
                foreach (var piece in _pieces)
                {
                    if (piece == null) continue;
                    if (!piece.fallen)
                    {
                        return false;
                    }
                }

                return true;
            });
            currentBlock = null;
        }

        GetActivePieces();

        foreach (var piece in _pieces)
        {
            if (piece == null || piece.exploded) continue;
            _neighbours.Clear();
            _garbage.Clear();
            
            piece.justFallen = false;
            piece.CheckNeighbours(_grid, _neighbours, _garbage);

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
                    //int cantidadbasuratirar = 6 - cantidadbasura;
                    //int cantidadbasurarecuperar = 6 - cantidadbasuratirar;


                    /*if (cantidadbasurarecuperar < 0)
                    {
                        cantidadbasurarecuperar = 0;
                    }*/

                    /*if (cantidadbasura < 0)
                    {
                        cantidadbasura = 0;
                    }*/
                    cantidadBasuraTirar += 6;
                    //rival.cantidadbasura += cantidadbasuratirar;
                    Debug.Log("combo: " + combo + " - " + "basura: " + cantidadBasuraTirar);
                }
                else if (combo == 1)
                {
                    cantidadBasuraTirar += 1;
                    Debug.Log("combo: " + combo + " - " + "basura: " + cantidadBasuraTirar);
                }

                //sumar puntuación aquí, "_neighbours.count" es el número de piezas que van a explotar
                foreach (var p in _neighbours)
                {
                    p.Explode(_grid);
                }
                foreach (var p in _garbage)
                {
                    p.Explode(_grid);
                }
            }
            else
            {
                foreach (var p in _neighbours)
                {
                    p.check = false;
                }
                foreach (var p in _garbage)
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
        foreach (var piece in _pieces)
        {
            if (piece == null || piece.exploded) continue;
            
            _neighbours.Clear();
            piece.CheckNeighbours( _grid,  _neighbours, _garbage);

            foreach (var p in _neighbours)
            {
                p.check = false;
            }

            if (_neighbours.Count < 4) continue;
            startAgain = true;
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
                if(!_isOnline)GetComponent<AbilityController>().enemyPieceController.lanzarBasura(cantidadBasuraTirar);
                else EnemyThrowGarbageServerRpc(cantidadBasuraTirar);
                cantidadBasuraTirar = 0;
                combo = 0;
            }

        }
        yield return null;
    }

    [ServerRpc]
    private void EnemyThrowGarbageServerRpc(int garbageCount, ServerRpcParams serverRpcParams = default)
    {
        var id = serverRpcParams.Receive.SenderClientId;
        var enemy = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<AbilityController>().enemyPieceController;

        var enemyRef = new NetworkObjectReference(enemy.GetComponent<NetworkObject>());
        EnemyThrowGarbageClientRpc(garbageCount, enemyRef);
    }
    
    [ClientRpc]
    private void EnemyThrowGarbageClientRpc(int garbageCount, NetworkObjectReference reference)
    {
        reference.TryGet(out var e);
        e.GetComponent<PieceController>().lanzarBasura(garbageCount);
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

            if (!_isOnline)
            {
                Piece g = Instantiate(garbagePieces[0]);
                g.SetPositionInGrid(posX, _grid.GetHeight() + posY, _grid);
                currentGarbage.AddLast(g);
            }
            else GenerateGarbageServerRpc(posX, posY);
            posX++;
        }
        //Esperar a que se generen todas las piezas
        if(_isOnline)  yield return new WaitForSeconds(0.2f);
        
        foreach (Piece garbage in currentGarbage)
        {
            if (garbage == null) continue;
            garbage.FallCoroutine(_grid, fallSpeed, this);
        }
        
        //Esperar a que esten todas colocadas
        yield return new WaitUntil(()=> 
        {
            foreach (var gar in currentGarbage)
            {
                if(gar == null) continue;
                if (!gar.fallen)
                {
                    return false;
                }
            }
            return true;

        });
        yield return new WaitForSeconds(0.25f);
        //Continuar
        currentGarbage.Clear();
        recibiendoBasura = false;
        if(!_isOnline)GenerateBlock();
        else OnlineBlockGeneration();
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
        if(_isOnline && !IsOwner) return;
        for (int x = 0; x < _grid.GetWidth(); x++)
        {
            for (int y = 0; y < _grid.GetHeight(); y++)
            {
                if (_grid.GetValue(x, y) != null)
                {
                    if(!_isOnline)Destroy(_grid.GetValue(x, y).gameObject);
                    else _grid.GetValue(x,y).Despawn();
                    _grid.SetValue(x, y, null);
                }
            }
        }

        if (currentBlock != null)
        {
            foreach (var piece in currentBlock.GetPieces())
            {
                if (!_isOnline) Destroy(piece.gameObject);
                else piece.Despawn();
            }
        }

        if(!_isOnline)GenerateBlock();
        else OnlineBlockGeneration();
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
        if(_isOnline && !IsOwner) return;
        GenerateBlockServerRpc(recibiendoBasura);
        held = false;
    }

    [ServerRpc]
    private void GenerateBlockServerRpc(bool receivingGarbage, ServerRpcParams serverRpcParams = default)
    {
        var id = serverRpcParams.Receive.SenderClientId;

        if (!receivingGarbage)
        {
            if (nextBlocks[0] == null)
            {
                currentBlock = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                    Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);

                for (int i = 0; i < nextBlocks.Length; i++)
                {
                    nextBlocks[i] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                        Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), this._grid, this);

                    for (int j = 0; j < nextBlocks[i].GetPieces().Length; j++)
                        nextBlocks[i].GetPieces()[j].transform.GetComponent<NetworkObject>()
                            .SpawnWithOwnership(id, true);
                }

                for (int i = 0; i < currentBlock.GetPieces().Length; i++)
                    currentBlock.GetPieces()[i].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
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

                for (int i = 0; i < nextBlocks[^1].GetPieces().Length; i++)
                    nextBlocks[^1].GetPieces()[i].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
            }


            currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
            for (int i = 0; i < nextBlocks.Length; i++)
            {
                nextBlocks[i].SetPosition(nextTransforms[i].position, i == 0 ? 0.75f : 0.75f * 0.5f * i);
            }

            NetworkObjectReference[] currentPieces =
            {
                new(currentBlock.GetPieces()[0].GetComponent<NetworkObject>()),
                new(currentBlock.GetPieces()[1].GetComponent<NetworkObject>())
            };
            NetworkObjectReference[] nextPieces1 =
            {
                new(nextBlocks[0].GetPieces()[0].GetComponent<NetworkObject>()),
                new(nextBlocks[0].GetPieces()[1].GetComponent<NetworkObject>())
            };
            NetworkObjectReference[] nextPieces2 =
            {
                new(nextBlocks[1].GetPieces()[0].GetComponent<NetworkObject>()),
                new(nextBlocks[1].GetPieces()[1].GetComponent<NetworkObject>())
            };

            SendClientsBlocksClientRpc(currentPieces, nextPieces1, nextPieces2);
        }
        else
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{id}
                }
            };
            ThrowGarbageClientRpc(clientRpcParams);
        }
    }

    [ServerRpc (RequireOwnership = false)]
    private void GenerateGarbageServerRpc(int x, int y, ServerRpcParams serverRpcParams = default)
    {
        var id = serverRpcParams.Receive.SenderClientId;
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{id}
            }
        };
        
        Piece g = GameObject.Instantiate(garbagePieces[0], Vector3.right*30, quaternion.identity);
        g.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);

        NetworkObjectReference garbageReference = new NetworkObjectReference(g.GetComponent<NetworkObject>());
        SetGarbageClientRpc(garbageReference, x, y, clientRpcParams);
    }

    [ClientRpc]
    private void ThrowGarbageClientRpc(ClientRpcParams clientRpcParams)
    {
        StopAllCoroutines();
        StartCoroutine(recibirBasura());
    }

    [ClientRpc]
    private void SetGarbageClientRpc(NetworkObjectReference garbageRef, int posX, int posY, ClientRpcParams clientRpcParams)
    {
        garbageRef.TryGet(out var garbage);
        var g = garbage.GetComponent<Piece>();
        
        g.SetPositionInGrid(posX, _grid.GetHeight() + posY, _grid);
        currentGarbage.AddLast(g);
    }

    [ClientRpc]
    private  void SendClientsBlocksClientRpc(NetworkObjectReference[] currentPieces, NetworkObjectReference[] nextPieces1, NetworkObjectReference[] nextPieces2)
    {
        if (!IsOwnedByServer)
        {
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
