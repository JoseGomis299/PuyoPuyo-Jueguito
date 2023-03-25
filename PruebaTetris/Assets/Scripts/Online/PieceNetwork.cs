using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PieceNetwork : NetworkBehaviour
{
   private NetworkVariable<PieceNetworkData> _netState = new(writePerm: NetworkVariableWritePermission.Owner);
   private NetworkVariable<bool> _right = new(writePerm: NetworkVariableWritePermission.Owner); 
   private NetworkVariable<bool> _left = new(writePerm: NetworkVariableWritePermission.Owner); 
   private NetworkVariable<bool> _up = new(writePerm: NetworkVariableWritePermission.Owner); 
   private NetworkVariable<bool> _down = new(writePerm: NetworkVariableWritePermission.Owner); 
   private Piece piece;

   private void Start()
   {
      if (NetworkManager == null)
      {
         enabled = false;
         return;
      }
      
      piece = GetComponent<Piece>();
      _right.OnValueChanged = (value, newValue) => { transform.GetChild(3).gameObject.SetActive(newValue); };
      _left.OnValueChanged = (value, newValue) => { transform.GetChild(1).gameObject.SetActive(newValue); };
      _up.OnValueChanged = (value, newValue) => { transform.GetChild(0).gameObject.SetActive(newValue); };
      _down.OnValueChanged = (value, newValue) => { transform.GetChild(2).gameObject.SetActive(newValue); };
   }
   private void Update()
   {
      if (IsOwner)
      {
         _netState.Value = new PieceNetworkData()
         {
            Position = transform.position,
         };
      }
      else
      {
         if(_netState.Value.Position == Vector3.zero) return;
         if(!piece.networkDontMove.Value) transform.position = new Vector3(_netState.Value.Position.x+12, _netState.Value.Position.y);
      }
   }

   public void SetJoints(int i, bool value)
   {
      switch (i)
      {
         case 0: _right.Value = value;
            break;
         case 1: _left.Value = value;
            break;
         case 2: _up.Value = value;
            break;
         case 3: _down.Value = value;
            break;
         default: 
            _right.Value = value; 
            _left.Value = value; 
            _up.Value = value; 
            _down.Value = value;
            break;
      }
   }

   struct PieceNetworkData : INetworkSerializable
   {
      private float _x, _y;
      internal Vector3 Position
      {
         get => new Vector3(_x, _y);
         set
         {
            _x =  value.x;
            _y = value.y;
         }
      }
      
      public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
      {
         serializer.SerializeValue(ref _x);
         serializer.SerializeValue(ref _y);
      }
   }
}
