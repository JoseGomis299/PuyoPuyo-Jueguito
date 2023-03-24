using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>Class <c>Piece</c> a single Piece</summary>
///
public abstract class Piece : NetworkBehaviour
{
    [field: SerializeReference] public AudioClip explosionSound { get; private set; }
    [HideInInspector] public bool check;
    [HideInInspector] public bool fallen;
    [HideInInspector] public bool justFallen;
    public NetworkVariable<bool> networkDontMove = new (writePerm: NetworkVariableWritePermission.Owner);
    public bool exploded { get; protected set; }
    public Block block{ get; private set; }
    
    public void SetBlockReference(Block block)
    {
        this.block = block;
    }

    public void SetDontMove(bool value)
    {
        if(!IsOwner) return;
        networkDontMove.Value = value;
    }

    public void CheckNeighbours(Grid<Piece> grid, LinkedList<Piece> list, LinkedList<Piece> garbageList)
    {
        //Debug.Log("CheckingNeighbours");
        //Si soy basura return
        if (this is Garbage) return;

        grid.GetXY(transform.position, out var x, out var y);
        check = true;

        Piece[] cardinalPieces =
        {
            grid.GetValue(x + 1, y), //RIGHT
            grid.GetValue(x - 1, y), //LEFT
            grid.GetValue(x, y + 1), //UP
            grid.GetValue(x, y - 1) //DOWN
        };
        
        //Desactivar los prefabs de unión
          foreach (Transform childTrans in transform.GetComponentInChildren<Transform>())
        {
            childTrans.gameObject.SetActive(false);
        }
        if(NetworkManager != null) GetComponent<PieceNetwork>().SetJoints(4, false);


        for (int i = 0; i < cardinalPieces.Length; i++)
        {
            if (cardinalPieces[i] != null && !cardinalPieces[i].exploded)
            {
                //si es la pieza buscada añadirla a la lista de vecinos
                if (cardinalPieces[i].Equals(this))
                {
                     switch (i)
                     {
                         case 0: transform.GetChild(3).gameObject.SetActive(true); //ACTIVAR PREFAB DERECHA
                                if(NetworkManager != null) GetComponent<PieceNetwork>().SetJoints(0, true);
                             break;
                         case 1: transform.GetChild(1).gameObject.SetActive(true); //ACTIVAR PREFAB IZQUIERDA
                                 if(NetworkManager != null) GetComponent<PieceNetwork>().SetJoints(1, true);
                             break;
                         case 2: transform.GetChild(0).gameObject.SetActive(true); //ACTIVAR PREFAB ARRIBA
                                if(NetworkManager != null) GetComponent<PieceNetwork>().SetJoints(2, true);
                             break;
                         case 3:transform.GetChild(2).gameObject.SetActive(true); //ACTIVAR PREFAB ABAJO
                                if(NetworkManager != null) GetComponent<PieceNetwork>().SetJoints(3, true);
                             break;
                     }
                    if(!cardinalPieces[i].check)cardinalPieces[i].CheckNeighbours(grid, list, garbageList);
                }
                else if (cardinalPieces[i] is Garbage)
                {
                    //si es basura y no está en la lista de basura añadirla
                    cardinalPieces[i].check = true;
                    garbageList.AddLast(cardinalPieces[i]);
                }
            }
        }

        list.AddLast(this);
    }

    public bool FallCoroutine(Grid<Piece> grid, float fallSpeed, PieceController pieceController)
    {
        check = false;

        grid.GetXY(transform.position, out var x, out var y);

        if (grid.GetValue(x,y-1) == null && grid.IsInBoundsNoHeight(x, y-1))
        {
            fallen = false;
            justFallen = true;
            StartCoroutine(DoFall(grid, fallSpeed, pieceController));
            return false;
        }
        
        if (y >= grid.GetHeight())
        {
            pieceController.CleanStage();
            if (NetworkManager != null) Despawn();
            else Destroy(gameObject);
        }

        fallen = true;
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
        if (fallen) return true;
        
        grid.GetXY(new Vector3(transform.position.x, transform.position.y-grid.GetCellSize()/2f-(fallSpeed * Time.deltaTime), transform.position.z), out var x, out var y);
        var nextPos = transform.position + Vector3.down * (fallSpeed * Time.deltaTime);

        if (grid.GetValue(x,y) == null && grid.IsInBoundsNoHeight(x,y))
        {
            transform.position = nextPos;
           return false;
        }

        while (!grid.IsInBoundsNoHeight(x, y) || grid.GetValue(x,y) != null) y++;
        transform.position = grid.GetCellCenter(x, y);

        justFallen = true;
        fallen = true;
        
        SetValue(grid, pieceController);
        return true;
    }

    public void SetValue(Grid<Piece> grid, PieceController pieceController)
    {
        grid.SetValue(transform.position, this);
        justFallen = true;
        fallen = true;
    }


    public void Rotate(Grid<Piece> grid, float finalRotation, float rotation)
    {
        StartCoroutine(DoRotation(grid, finalRotation, rotation));
    }

    public void ForceRotation(Grid<Piece> grid, float finalRotation)
    {
        StopAllCoroutines();
        finalRotation *=  Mathf.Deg2Rad;
        grid.GetXY(new Vector3(transform.position.x, transform.position.y+grid.GetCellSize()/2.5f, transform.position.z), out var x, out var y);

        var targetX = Mathf.Sin(finalRotation);
        var targetY = Mathf.Cos(finalRotation);
        
        transform.position = block.GetPieces()[0].transform.position + new Vector3(targetX, targetY);
        
        if (!grid.IsInBoundsNoHeight(x, y) || grid.GetValue(x, y) != null)
        {
            block.Move(new Vector2(-targetX, -targetY));
        }
        block.rotating = false;
    }

    private IEnumerator DoRotation(Grid<Piece> grid, float finalRotation, float rotation)
    {
        var currentRotation = finalRotation-rotation;
        currentRotation *= Mathf.Deg2Rad;
        finalRotation *=  Mathf.Deg2Rad;

        var x = Mathf.Sin(currentRotation);
        var y = Mathf.Cos(currentRotation);

        var targetX = Mathf.Sin(finalRotation);
        var targetY = Mathf.Cos(finalRotation);
        
        while (Math.Abs(x - targetX) > 0.1f && Math.Abs(y - targetY) > 0.1f)
        {
            if(block.GetPieces()[0] == null) {break;} 
            Vector3 center = block.GetPieces()[0].transform.position;
            currentRotation = Mathf.Lerp(currentRotation, finalRotation, Time.deltaTime * 12.5f);
            x = Mathf.Sin(currentRotation);
            y = Mathf.Cos(currentRotation);
            var finalPos = center;
            finalPos.x += x;
            finalPos.y += y;
            transform.position = finalPos;
            yield return null;
        }

        if (!grid.IsInBoundsNoHeight(transform.position) || grid.GetValue(transform.position) != null)
        {
            block.Move(new Vector2(-targetX, -targetY));
        }
        
        if(block.GetPieces()[0] != null) transform.position = block.GetPieces()[0].transform.position + new Vector3(targetX, targetY);
        
        block.rotating = false;
    }
    
    public void GoToFinalPosition(Grid<Piece> grid)
    {
        grid.GetXY(transform.position, out var x, out var y);
        
        for (int i = 0; i < grid.GetHeight(); i++)
        {
            if (grid.GetValue(x,i) != null) continue;
            y = i;
            break;
        }

        transform.position = grid.GetCellCenter(x, y);
    }
    public virtual void Explode(Grid<Piece> grid)
    {
        exploded = true;
        grid.SetValue(transform.position, null);
        StartCoroutine(Explosion(grid));
    }

    public override void OnDestroy()
    {
        AudioManager.Instance.PlaySound(explosionSound);
    }

    public IEnumerator Explosion(Grid<Piece> grid)
    {
        GetComponent<SpriteRenderer>().color = Color.white;
        yield return new WaitForSeconds(0.25f);
        if (IsHost || IsClient) DespawnPieceServerRpc();
        else Destroy(gameObject);
    }
    public abstract bool Equals(Piece piece);

    public void Despawn()
    {
        DespawnPieceServerRpc();
    }

    [ServerRpc]
    protected void DespawnPieceServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }

    internal void SetPositionInGrid(int posX, int posY, Grid<Piece> grid)
    {
       transform.position = grid.GetCellCenter(posX, posY);   
    }
}
