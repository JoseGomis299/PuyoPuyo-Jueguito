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
      _right.OnValueChanged = (value, newValue) => { transform.GetChild(3).gameObject.SetActive(true); };
      _left.OnValueChanged = (value, newValue) => { transform.GetChild(1).gameObject.SetActive(true); };
      _up.OnValueChanged = (value, newValue) => { transform.GetChild(0).gameObject.SetActive(true); };
      _down.OnValueChanged = (value, newValue) => { transform.GetChild(2).gameObject.SetActive(true); };
   }
   private void Update()
   {
      
      if (IsOwner)
      {
         _netState.Value = new PieceNetworkData()
         {
            Position = transform.position,
            Scale = transform.localScale
         };
      }
      else
      {
         if(_netState.Value.Scale == Vector3.zero) return;
         transform.position = new Vector3(_netState.Value.Position.x+12, _netState.Value.Position.y, _netState.Value.Position.z);
         transform.localScale = _netState.Value.Scale;
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
      private float _xS, _yS, _zS;

      internal Vector3 Position
      {
         get => new Vector3(_x, _y, 0);
         set
         {
            _x =  value.x;
            _y = value.y;
         }
      }
      
      internal Vector3 Scale
      {
         get => new Vector3(_xS, _yS, _zS);
         set
         {
            _xS =  value.x;
            _yS = value.y;
            _zS = value.z;
         }
      }
      public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
      {
         serializer.SerializeValue(ref _x);
         serializer.SerializeValue(ref _y);
         
         serializer.SerializeValue(ref _xS);
         serializer.SerializeValue(ref _yS);
         serializer.SerializeValue(ref _zS);
      }
   }
}
