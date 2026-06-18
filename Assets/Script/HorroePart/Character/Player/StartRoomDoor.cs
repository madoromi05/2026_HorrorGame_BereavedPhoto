using HorrorGame.Interaction;
using System;
using UnityEngine;

/// <summary>
/// スタートルームの出口に動的生成されるドア。
/// IInteractable を実装しており、プレイヤーがインタラクトすると
/// OnOpened を通知してから自身を無効化する。
/// </summary>
[RequireComponent(typeof(Collider))]
public class StartRoomDoor : MonoBehaviour, IInteractable
{
    public event Action OnOpened;

    public bool   CanInteract => true;
    public string HintText    => "【E】ドアを開ける";

    public void OnInteract()
    {
        OnOpened?.Invoke();
        gameObject.SetActive(false);
    }
}
