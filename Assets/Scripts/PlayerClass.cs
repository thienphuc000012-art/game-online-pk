using Fusion;
using UnityEngine;

public enum PlayerClass
{
    None,
    Mage,
    Archer,
    Barbarian,
    Elf
}


[System.Serializable]
public struct PlayerClassInfo
{
    public string Name;
    public PlayerClass Class;
}

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public bool Attack;   // J - hit
    public bool Jump;
    public bool Block;    // K - kick / block
    public bool SuperHit; // I
    public bool Shoot;    // U
}