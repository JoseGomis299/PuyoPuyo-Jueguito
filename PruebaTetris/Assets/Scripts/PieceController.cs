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
using TMPro;

/// <summary>Class <c>PieceController</c> the controller for a Player's Grid</summary>
///
public class PieceController : NetworkBehaviour
{ 
    //******************START GLOBAL VARIABLES REGION********************
    #region Global Variables
    
   [Header("Grid stats")]
   [SerializeField] private Piece[] availablePieces; 
   [SerializeField] private Piece[] garbagePieces; 
   public float fallSpeed = 5;
   [HideInInspector] public float fallSpeedDelta = 1;

   [Header("Grid dimensions and position")]
   [SerializeField] private Vector2 gridSize = new Vector2(6, 14);
   [SerializeField] private float cellSize = 1;

    [Header("Garbage Data")]
    [SerializeField] private int garbageCombo = 6;
    [SerializeField] private int garbageSimple = 1;

    private TMP_Text _garbageIndicator;
    private PieceController _rival;
   
   //************REFERENCES**************
   
   private InputManager _inputManager;
   
   //THE BLOCK THAT THE PLAYER IS CONTROLLING AT THIS MOMENT
   public Block currentBlock;
   
   //THE BLOCK THAT THE PLAYER IS HOLDING AT THIS MOMENT
   private Block _holdBlock;
   private Transform _holdTransform;
   
   //THE BLOCKS THAT ARE COMING NEXT
   private Block[] _nextBlocks;
   private Transform[] _nextTransforms;
   
   //THE GRID WHERE THE PIECES ARE PLACED
   private Grid<Piece> _grid;

   //************LISTS**************
   
   //LIST FOR STORING THE NEIGHBOUR PIECES, INCLUDING THIS
   private LinkedList<Piece> _neighbours;
   
   //LIST FOR STORING GARBAGE THAT IS GOING TO EXPLODE
   private LinkedList<Piece> _garbage;
   
   //LIST FOR STORING GARBAGE THAT IS GOING TO BE GENERATED
   private LinkedList<Piece> _currentGarbage;
   
   //LIST FOR STORING THE PIECES IN THE GRID
   private LinkedList<Piece> _pieces;
   
   //************CONTROLLER PARAMETERS**************
   
   public bool held { get; private set; }
   private bool _stopPlacing;
   private bool _doNotGenerate;

   private bool _isOnline;

   //************SCORE PARAMETERS**************
   
   private int combo = 0;
   
   //************GARBAGE CONTROL**************
   
   private int _garbageQuantityThrow;
   private int _garbageQuantityReceive;
   private bool _receivingGarbage;

   #endregion
   //******************END GLOBAL VARIABLES REGION********************
   
   //------------------------------------------------------------------------------------------------------------------------------------------------------------------------

   private void Start()
   {
       // INITIALISE ALL VARIABLES 
       _isOnline = NetworkManager != null;

       _rival = GetComponent<AbilityController>().enemyPieceController;
       _currentGarbage = new LinkedList<Piece>();
        _neighbours = new LinkedList<Piece>();
        _pieces = new LinkedList<Piece>();
        _garbage = new LinkedList<Piece>();
        
       _nextBlocks = new Block[2]; 
       _inputManager = GetComponent<InputManager>();
       
       // SET POSITION ON THE SCREEN + CREATE GRID
       InitialPosition();
       _grid = new Grid<Piece>((int)gridSize.x, (int)gridSize.y, cellSize, transform.position);
       
       if (_isOnline && !IsOwner) { return; }
       
       // GENERATE FIRST BLOCK
       if(!_isOnline)GenerateBlock();
       else GenerateBlockServerRpc(_receivingGarbage);

   }
   
   private void Update()
   {
       if (_isOnline && !IsOwner) { return; }
       if(currentBlock == null) return;

       //INCREASE fallSpeed WITH TIME AND MAKE currentBlock FALL
       fallSpeed += (Time.deltaTime / 20)*fallSpeedDelta;
       currentBlock.Fall(fallSpeed);
       
       //MANAGE THE INPUT FOR THE currentBlock
       if(_inputManager != null)_inputManager.ManageInput();
   }

   //------------------------------------------------------------------------------------------------------------------------------------------------------------------------

   //******************START BLOCK GENERATION REGION********************
   #region Block Generation
   
   /// <summary>
   /// <para>Instantiates a <see cref="Block"/> locally.</para>
   /// <para> <c>currentBlock</c> is set to the new <c>Block</c> and
   /// <c>_nextBlocks[]</c> is also changed</para>
   /// </summary>
   ///
   public void GenerateBlock()
   {
       if (!_receivingGarbage)
       {
           //IF _nextBlocks[0] == null GENERATES EVERY BLOCK SINCE THERE IS NOT IN THE GRID YET
           if (_nextBlocks[0] == null)
           {
               currentBlock = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                   Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);

               for (int i = 0; i < _nextBlocks.Length; i++)
               {
                   _nextBlocks[i] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                       Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);
               }
           }
           
           //IF _nextBlocks[0] != null JUST GENERATES THE NEW _nextBlocks[^1] AND CHANGES THE REFERENCE
           //FOR THE OTHER BLOCKS SINCE THEY ARE ALREADY GENERATED
           else
           {
               currentBlock = _nextBlocks[0];
               for (int i = 0; i < _nextBlocks.Length - 1; i++)
               {
                   _nextBlocks[i] = _nextBlocks[i + 1];
               }
               _nextBlocks[^1] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                   Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);
           }
           
           //SETS EVERY BLOCK TO ITS CORRESPONDENT POSITION
           currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
           for (int i = 0; i < _nextBlocks.Length; i++)
           {
               _nextBlocks[i].SetPosition(_nextTransforms[i].position, i == 0 ? 0.75f : 0.75f * 0.5f * i);
           }
           held = false;
       }
       else
       {
           //IF IS _receivingGarbage GENERATES THE GARBAGE
           StopAllCoroutines();
           StartCoroutine(ReceiveGarbage());
       } 
   }
   
   /// <summary>
   /// <para>Calls the server to Instantiates a <see cref="Block"/> online.</para>
   /// </summary>
   ///
   private void OnlineBlockGeneration()
    {
        if(_isOnline && !IsOwner) return;
        GenerateBlockServerRpc(_receivingGarbage);
        held = false;
    }
   
   /// <summary>
   /// <para>Instantiates a <see cref="Block"/> online.</para>
   /// <para> <c>currentBlock</c> is set to the new <c>Block</c> and                
   /// <c>_nextBlocks[]</c> is also changed</para>
   /// <param name="receivingGarbage"> If it is receiving Garbage.</param>
   /// </summary>
   ///
   [ServerRpc]
    private void GenerateBlockServerRpc(bool receivingGarbage, ServerRpcParams serverRpcParams = default)
    {
        var id = serverRpcParams.Receive.SenderClientId;

        if (!receivingGarbage)
        {
            //IF _nextBlocks[0] == null GENERATES EVERY BLOCK SINCE THERE IS NOT IN THE GRID YET
            if (_nextBlocks[0] == null)
            {
                currentBlock = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                    Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);

                for (int i = 0; i < _nextBlocks.Length; i++)
                {
                    _nextBlocks[i] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                        Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), this._grid, this);

                    for (int j = 0; j < _nextBlocks[i].GetPieces().Length; j++)
                        _nextBlocks[i].GetPieces()[j].transform.GetComponent<NetworkObject>()
                            .SpawnWithOwnership(id, true);
                }

                for (int i = 0; i < currentBlock.GetPieces().Length; i++)
                    currentBlock.GetPieces()[i].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
            }
            
            //IF _nextBlocks[0] != null JUST GENERATES THE NEW _nextBlocks[^1] AND CHANGES THE REFERENCE
            //FOR THE OTHER BLOCKS SINCE THEY ARE ALREADY GENERATED
            else
            {
                currentBlock = _nextBlocks[0];

                for (int i = 0; i < _nextBlocks.Length - 1; i++)
                {
                    _nextBlocks[i] = _nextBlocks[i + 1];
                }

                _nextBlocks[^1] = new Block(Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]),
                    Instantiate(availablePieces[Random.Range(0, availablePieces.Length)]), _grid, this);

                for (int i = 0; i < _nextBlocks[^1].GetPieces().Length; i++)
                    _nextBlocks[^1].GetPieces()[i].transform.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);
            }


            //SETS EVERY BLOCK TO ITS CORRESPONDENT POSITION IN THE SERVER
            currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
            for (int i = 0; i < _nextBlocks.Length; i++)
            {
                _nextBlocks[i].SetPosition(_nextTransforms[i].position, i == 0 ? 0.75f : 0.75f * 0.5f * i);
            }

            //SETS EVERY BLOCK NetworkObjectReference FOR CHANGING ITS REFERENCE IN THE CLIENTS
            NetworkObjectReference[] currentPieces =
            {
                new(currentBlock.GetPieces()[0].GetComponent<NetworkObject>()),
                new(currentBlock.GetPieces()[1].GetComponent<NetworkObject>())
            };
            NetworkObjectReference[] nextPieces1 =
            {
                new(_nextBlocks[0].GetPieces()[0].GetComponent<NetworkObject>()),
                new(_nextBlocks[0].GetPieces()[1].GetComponent<NetworkObject>())
            };
            NetworkObjectReference[] nextPieces2 =
            {
                new(_nextBlocks[1].GetPieces()[0].GetComponent<NetworkObject>()),
                new(_nextBlocks[1].GetPieces()[1].GetComponent<NetworkObject>())
            };

            //SETS EVERY BLOCK REFERENCE AND POSITION IN THE CLIENTS IN THE CLIENTS
            SendClientsBlocksClientRpc(currentPieces, nextPieces1, nextPieces2);
        }
        else
        {
            //IF IS _receivingGarbage GENERATES THE GARBAGE
            //IT IS ONLY SENT TO THE CLIENT THAT HAS INVOKED THIS METHOD
            
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
   
   /// <summary>
   /// <para>Sets every <see cref="Block"/> reference in the clients</para>
   /// <param name="currentPieces[]"> <c>NetworkObjectReferences</c> for the pieces in <c>currentBlock</c></param>
   /// <param name="nextPieces1[]"> <c>NetworkObjectReferences</c> for the pieces in <c>_nextBlocks[0]</c></param>
   /// <param name="nextPieces2[]"> <c>NetworkObjectReferences</c> for the pieces in <c>_nextBlocks[1]</c></param>
   /// </summary>
   ///
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
            _nextBlocks[0] = new Block(piece0.GetComponent<Piece>(), piece1.GetComponent<Piece>(), _grid, this); 
            nextPieces2[0].TryGet(out  piece0);
            nextPieces2[1].TryGet(out  piece1);
            _nextBlocks[1] = new Block(piece0.GetComponent<Piece>(), piece1.GetComponent<Piece>(), _grid, this);
            currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
            for (int i = 0; i < _nextBlocks.Length; i++)
            {
                _nextBlocks[i].SetPosition(_nextTransforms[i].position, i== 0 ? 0.75f : 0.75f*0.5f*i);
            }

        }
    }
    
   #endregion
   //******************END BLOCK GENERATION REGION********************

   //------------------------------------------------------------------------------------------------------------------------------------------------------------------------
   
   //******************START PIECE UTILITIES REGION********************
   #region Piece Utilities

   /// <summary>
   /// <para>Stores a <see cref="Block"/> to be used later, and places it in the <c>_holdTransform.position</c>.</para>
   /// </summary>
   ///
   public void Hold()
   {
       if(currentBlock ==  null) return;
       
       //IF _holdBlock == null GENERATES A NEW BLOCK AND CHANGES THE REFERENCES
       if (_holdBlock == null)
       {
           _holdBlock = currentBlock;

           currentBlock = null;
           if(!_isOnline)GenerateBlock();
           else OnlineBlockGeneration();
       }
       
       //IF _holdBlock != null JUST CHANGES THE REFERENCES
       else
       {
           (_holdBlock, currentBlock) = (currentBlock, _holdBlock);
           currentBlock.SetPositionInGrid(Random.Range(0, _grid.GetWidth()), _grid.GetHeight());
       }
       
       //PLACES THE BLOCK IN _holdTransform.position
       _holdBlock.SetPosition(_holdTransform.position, 0.75f);
       
       held = true;
   }

   /// <summary>
   /// <para>Makes the <c>currentBlock</c> fall instantly, making it also set its value in the grid</para>
   /// </summary>
   ///
   public void InstantDown()
   {
       if(currentBlock == null) return;
       if(!_grid.IsInBounds(currentBlock.GetPieces()[1].transform.position) || _grid.GetValue(currentBlock.GetPieces()[0].transform.position)) return;

       currentBlock.fallen = true;
       
       //IF currentBlock IS ROTATING FORCES IT TO FINISH ITS ROTATION
       if (currentBlock.rotating)
       {
           currentBlock.GetPieces()[1].ForceRotation(_grid, currentBlock.rotation);
       }
       
       //SETS EVERY currentBlock's PIECE POSITION
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

       //SETS THE PIECES VALUE IN THE GRID
       SetPiecesValue();
   }

   /// <summary>
   /// <para>Returns the final position of a <see cref="Piece"/></para>
   /// <param name="origin"> The current position of the Piece</param>
   /// </summary>
   ///
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
   
   /// <summary>
   /// <para>Resets the grid value and makes every <see cref="Piece"/> that was in it fall </para>
   /// <param name="origin"> The current position of the Piece</param>
   /// </summary>
   ///
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

   #endregion
   //******************END PIECE UTILITIES REGION********************

   //------------------------------------------------------------------------------------------------------------------------------------------------------------------------

   //******************START GRID UTILITIES REGION********************
   #region Grid Utilities
   
   /// <summary>
   /// <para>Sets every <see cref="Piece"/> value in the grid.</para>
   /// <para>If there is a combination, then makes them explode and fall.
   /// This process is repeated until there are no more combinations</para>
   /// </summary>
   ///
   public void SetPiecesValue()
   {
       if(_stopPlacing || (_isOnline && !IsOwner)) return;
       
       StartCoroutine(_SetPiecesValue());
   }
   
   private IEnumerator _SetPiecesValue()
    {
        //IF currentBlock != null MAKES THE CURRENT BLOCK PIECES FALL BEFORE MAKING THEM EXPLODE
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

        //CHECK IF THERE ARE COMBINATIONS, IF SO, MAKE THAT PIECES EXPLODE
        foreach (var piece in _pieces)
        {
            if (piece == null || piece.exploded) continue;
            
            //CHECKS THE PIECE NEIGHBOURS
            _neighbours.Clear();
            _garbage.Clear();
            
            piece.justFallen = false;
            piece.CheckNeighbours(_grid, _neighbours, _garbage);

            //IF _neighbours.Count >= 4 IT MEANS THAT THERE IS A COMBINATION
            if (_neighbours.Count >= 4)
            {
                //Combos and garbage

                //Si se hace mas de una linea, deja de ser combo simple
                if (combo == 1)
                {
                    _garbageQuantityThrow -= garbageSimple;
                }

                combo++;

                int cantidadbasuratirar = 0;

                //Por cada pieza "extra" tiramos uno mas de basura
                cantidadbasuratirar += _neighbours.Count - 4;

                //Combo normal
                if (combo > 1)
                {
                    cantidadbasuratirar += garbageCombo;
                } //Combo Simple
                else if (combo == 1)
                {
                    cantidadbasuratirar += garbageSimple;
                }

                //Recuperamos basura
                if (cantidadbasuratirar >= _garbageQuantityReceive)
                {
                    cantidadbasuratirar = cantidadbasuratirar - _garbageQuantityReceive;
                    _garbageQuantityReceive = 0;
                }
                else
                {
                    _garbageQuantityReceive = _garbageQuantityReceive - cantidadbasuratirar;
                    cantidadbasuratirar = 0;
                }

                _garbageQuantityThrow += cantidadbasuratirar;

                //Indicadores
                _rival._garbageIndicator.text = (_rival._garbageQuantityReceive + _garbageQuantityThrow).ToString();
                _garbageIndicator.text = (_garbageQuantityReceive + _rival._garbageQuantityThrow).ToString();

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
            
            //IF THERE IS NOT A COMBINATION RESET THE PIECES CHECK TO AVOID IGNORING THEM LATER
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
        
        //NOW THAT EVERY PIECE HAS EXPLODED IF NEEDED, WE HAVE TO MAKE THE REMAINING PIECES FALL
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
        
        //CHECK IF THERE ARE MORE COMBINATIONS
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

        //IF THERE ARE MORE COMBINATIONS START AGAIN
        if (startAgain) StartCoroutine(_SetPiecesValue());
        
        //ELSE GENERATE A NEW BLOCK AND THROW GARBAGE IF COMBO > 0
        else
        {
            if(!_isOnline)GenerateBlock();
            else OnlineBlockGeneration();
            
            if (combo > 0)
            {
                if(!_isOnline)_rival.ThrowGarbage(_garbageQuantityThrow);
                else EnemyThrowGarbageServerRpc(_garbageQuantityThrow);
                _garbageQuantityThrow = 0;
                combo = 0;
            }
        }
        yield return null;
    }
   
   /// <summary>
   /// <para>Sets <c>_pieces[]</c> to all the pieces that has just fallen.</para>
   /// </summary>
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
   
   /// <summary>
   /// <para>Sets <c>_pieces[]</c> to all the pieces that are in the grid.</para>
   /// </summary>
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
   
   /// <summary>
   /// <para>Deletes every <see cref="Piece"/> in the grid</para>
   /// </summary>
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

   /// <summary>
   /// <para>Sets the initial position of the player's grid</para>
   /// </summary>
   private void InitialPosition()
   {
       if (!_isOnline)
       {
           if (_inputManager.playerTwo)
           {
               transform.position = Vector3.right * 3;
               _holdTransform = GameObject.Find("HoldPosR").transform;
               _nextTransforms = new[]
                   { GameObject.Find("NextPosR").transform, GameObject.Find("NextPos2R").transform };
               _garbageIndicator = GameObject.Find("GarbageR").GetComponent<TMP_Text>();
           }
           else
           {
               transform.position = Vector3.right * -9;
               _holdTransform = GameObject.Find("HoldPosL").transform;
               _nextTransforms = new[]
                   { GameObject.Find("NextPosL").transform, GameObject.Find("NextPos2L").transform };
               _garbageIndicator = GameObject.Find("GarbageL").GetComponent<TMP_Text>();
           }
       }
       else
       {
           if (IsOwner)
           {
               transform.position = Vector3.right * -9;
               _holdTransform = GameObject.Find("HoldPosL").transform;
               _nextTransforms = new[]
                   { GameObject.Find("NextPosL").transform, GameObject.Find("NextPos2L").transform };
               _garbageIndicator = GameObject.Find("GarbageL").GetComponent<TMP_Text>();
           }
           else
           {
               transform.position = Vector3.right * 3;
               _holdTransform = GameObject.Find("HoldPosR").transform;
               _nextTransforms = new[]
                   { GameObject.Find("NextPosR").transform, GameObject.Find("NextPos2R").transform };
               _garbageIndicator = GameObject.Find("GarbageR").GetComponent<TMP_Text>();
           }
       }
   }

   #endregion
   //******************END GRID UTILITIES REGION********************

    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    //******************START GARBAGE REGION********************
    #region Garbage

       /// <summary>
       /// <para>Stops all coroutines and calls <c>ReceiveGarbage</c> on the client that has invoked the previous ServerRpc method</para>
       /// </summary>
       [ClientRpc]
       private void ThrowGarbageClientRpc(ClientRpcParams clientRpcParams)
       {
           StopAllCoroutines();
           StartCoroutine(ReceiveGarbage());
       }

       /// <summary>
       /// <para>Sets <c>_garbageQuantityReceive</c> to the number of <see cref="Garbage"/> that the enemy has thrown to you and tells the generator to generate it</para>
       /// <param name="garbageNum">The number of garbage that is going to be generated</param>
       /// </summary>
       private void ThrowGarbage(int garbageNum)
       {
           _garbageQuantityReceive = garbageNum;
           _receivingGarbage = true;
       }

       /// <summary>
       /// <para>Gets the <c>enemyPieceController</c> reference and tells the client to throw garbage to him</para>
       /// <param name="garbageNum">The number of garbage that is going to be generated</param>
       /// </summary>
       [ServerRpc]
       private void EnemyThrowGarbageServerRpc(int garbageNum, ServerRpcParams serverRpcParams = default)
       {
           var id = serverRpcParams.Receive.SenderClientId;
           var enemy = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<AbilityController>()
               .enemyPieceController;

           var enemyRef = new NetworkObjectReference(enemy.GetComponent<NetworkObject>());
           EnemyThrowGarbageClientRpc(garbageNum, enemyRef);
       }

       [ClientRpc]
       private void EnemyThrowGarbageClientRpc(int garbageCount, NetworkObjectReference reference)
       {
           reference.TryGet(out var e);
           e.GetComponent<PieceController>().ThrowGarbage(garbageCount);
       }

       /// <summary>
       /// <para>Generates the number of <see cref="Garbage"/> set on <c>_garbageQuantityReceive</c></para>
       /// </summary>
       private IEnumerator ReceiveGarbage()
       {
           yield return new WaitForSeconds(0.25f);
        _garbageIndicator.text = "0";

        int posY = 0;
           int posX = 0;

           //GENERATES THE NEEDED GARBAGE AND ADDS THEM TO _currentGarbage
           for (int i = 0; i < _garbageQuantityReceive; i++)
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
                   _currentGarbage.AddLast(g);
               }
               else GenerateGarbageServerRpc(posX, posY);

               posX++;
           }

           //SINCE IT IS A COROUTINE AND ONLINE IS NOT INSTANT, WE HAVE TO WAIT FOR EVERY PIECE TO BE GENERATED
           if (_isOnline) yield return new WaitUntil(() => _currentGarbage.Count == _garbageQuantityReceive);

           //MAKE EVERY PIECE FALL
           foreach (Piece garbage in _currentGarbage)
           {
               if (garbage == null) continue;
               garbage.FallCoroutine(_grid, fallSpeed, this);
           }

           //WAIT UNTIL EVERY PIECE HAS FALLEN
           yield return new WaitUntil(() =>
           {
               foreach (var gar in _currentGarbage)
               {
                   if (gar == null) continue;
                   if (!gar.fallen)
                   {
                       return false;
                   }
               }

               return true;

           });
           yield return new WaitForSeconds(0.25f);

        //CONTINUE
        _garbageQuantityReceive = 0;
        _currentGarbage.Clear();
           _receivingGarbage = false;
           if (!_isOnline) GenerateBlock();
           else OnlineBlockGeneration();
       }

       /// <summary>
       /// <para>Generates a piece of <see cref="Garbage"/> on the client that has invoked this method</para>
       /// <param name="x">The X position where it is going to be generated</param>
       /// <param name="y">The Y position where it is going to be generated</param>
       /// </summary>
       [ServerRpc(RequireOwnership = false)]
       private void GenerateGarbageServerRpc(int x, int y, ServerRpcParams serverRpcParams = default)
       {
           var id = serverRpcParams.Receive.SenderClientId;
           ClientRpcParams clientRpcParams = new ClientRpcParams
           {
               Send = new ClientRpcSendParams
               {
                   TargetClientIds = new ulong[] { id }
               }
           };

           Piece g = GameObject.Instantiate(garbagePieces[0], Vector3.right * 30, quaternion.identity);
           g.GetComponent<NetworkObject>().SpawnWithOwnership(id, true);

           NetworkObjectReference garbageReference = new NetworkObjectReference(g.GetComponent<NetworkObject>());
           SetGarbageClientRpc(garbageReference, x, y, clientRpcParams);
       }

       /// <summary>
       /// <para>Places the generated <see cref="Garbage"/> and adds it to <c>_currentGarbage</c></para>
       /// <param name="garbageRef">The reference of the Garbage that has been generated</param>
       /// <param name="posX">The X position where it is going to be placed</param>
       /// <param name="posY">The Y position where it is going to be placed</param>
       /// </summary>
       [ClientRpc]
       private void SetGarbageClientRpc(NetworkObjectReference garbageRef, int posX, int posY,
           ClientRpcParams clientRpcParams)
       {
           garbageRef.TryGet(out var garbage);
           var g = garbage.GetComponent<Piece>();

           g.SetPositionInGrid(posX, _grid.GetHeight() + posY, _grid);
           _currentGarbage.AddLast(g);
       }

       #endregion
    //******************END GARBAGE REGION********************

    //------------------------------------------------------------------------------------------------------------------------------------------------------------------------

}


   

    

    

    

