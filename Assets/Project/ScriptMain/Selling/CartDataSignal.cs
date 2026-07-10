using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/CartDataSignal")]
public class CartDataSignal : ScriptableObject
{
    public SharedCartData Current { get; private set; }
    public event Action<SharedCartData> OnCartReady;

    public void Register(SharedCartData cart)
    {
        Current = cart;
        OnCartReady?.Invoke(cart);
    }

    public void Unregister()
    {
        Current = null;
    }
}
