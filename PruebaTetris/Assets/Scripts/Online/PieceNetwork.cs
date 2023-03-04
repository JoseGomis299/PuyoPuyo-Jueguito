using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PieceNetwork : NetworkBehaviour
{
   private NetworkVariable<PieceNetworkData> _netState = new(writePerm: NetworkVariableWritePermission.Owner);

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
