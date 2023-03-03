using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class InputManager : NetworkBehaviour
{
    [Header("Controls")] 
    private PlayerInput _myInput;
    public bool playerTwo { get; private set; }
    [SerializeField] private float moveCooldown = 0.1f;
    private float _lastMove;
    [SerializeField] private float rotationCooldown = 0.1f;
    private float _lastRotation;
    [SerializeField] private float fallSpeedBoost = 2f;

    private Vector2 _moveDirection;
    private int _rotation;

    private PieceController _pieceController;
    private void Start()
    {
        _myInput = gameObject.GetComponent<PlayerInput>();
        _pieceController = gameObject.GetComponent<PieceController>();

        
        if (!(IsClient || IsHost) && SpawnController.Instance.playerCount > 1)
        {
            playerTwo = SpawnController.Instance.SetPlayerID() == 1;
            _myInput.SwitchCurrentActionMap("TwoPlayers");
        }
        else
        {
            _myInput.SwitchCurrentActionMap("SinglePlayer");
        }
    }

    #region player1

    public void OnMove(InputAction.CallbackContext context)
    {
        if(playerTwo) return;
        _moveDirection = context.ReadValue<Vector2>();
    }

    public void OnDown(InputAction.CallbackContext context)
    {
        if(playerTwo) return;
        if (context.started)
        {
            _pieceController.fallSpeed *= fallSpeedBoost;
        }
        else if (context.canceled)
        {
            _pieceController.fallSpeed /= fallSpeedBoost;
        }
    }
    
    public void OnRotateRight(InputAction.CallbackContext context)
    {
        if(playerTwo) return;
        if (context.started)
        {
            _rotation = 90;
        }
        else if (context.canceled)
        {
            _rotation = 0;
        }    
    } 
    
    public void OnRotateLeft(InputAction.CallbackContext context)
    {
        if(playerTwo) return;
        if (context.started)
        {
            _rotation = -90;
        }
        else if (context.canceled)
        {
            _rotation = 0;
        }    
    }
    
    public void OnHold(InputAction.CallbackContext context)
    {
        if(playerTwo || _pieceController.currentBlock == null) return;
        if (context.started && !_pieceController.currentBlock.fallen && !_pieceController.held)
        {
            _pieceController.Hold();
        }
    }
    
    public void OnInstantDown(InputAction.CallbackContext context)
    {
        if(playerTwo) return;
        if (context.started)
        {
            _pieceController.InstantDown();
        }
    }

    #endregion
    
    #region Player2

    public void OnMove1(InputAction.CallbackContext context)
    {
        if(!playerTwo) return;
        _moveDirection = context.ReadValue<Vector2>();
    }

    public void OnDown1(InputAction.CallbackContext context)
    {
        if(!playerTwo) return;
        if (context.started)
        {
            _pieceController.fallSpeed *= fallSpeedBoost;
        }
        else if (context.canceled)
        {
            _pieceController.fallSpeed /= fallSpeedBoost;
        }
    }
    
    public void OnRotateRight1(InputAction.CallbackContext context)
    {
        if(!playerTwo) return;
        if (context.started)
        {
            _rotation = 90;

        }
        else if (context.canceled)
        {
            _rotation = 0;
        }    
    } 
    
    public void OnRotateLeft1(InputAction.CallbackContext context)
    {
        if(!playerTwo) return;
        if (context.started)
        {
            _rotation = -90;
        }
        else if (context.canceled)
        {
            _rotation = 0;
        }    
    }
    
    public void OnHold1(InputAction.CallbackContext context)
    {
        if(!playerTwo) return;
        if (context.started && !_pieceController.currentBlock.fallen && !_pieceController.held)
        {
            _pieceController.Hold();
        }
    }
    
    public void OnInstantDown1(InputAction.CallbackContext context)
    {
        if(!playerTwo) return;
        if (context.started)
        {
            _pieceController.InstantDown();
        }
    }

    #endregion
    public void ManageInput()
    {
        if (_moveDirection.magnitude > 0 && Time.time - _lastMove >= moveCooldown)
        {
            _lastMove = Time.time;
            _pieceController.currentBlock.Move(_moveDirection);
        }
   
        if (_rotation != 0 && Time.time - _lastRotation >= rotationCooldown)
        {
            _lastRotation = Time.time;
            _pieceController.currentBlock.Rotate(_rotation);
        }
    }
}
