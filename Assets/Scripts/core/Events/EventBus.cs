using System;
using System.Collections.Generic;

public static class EventBus<T> where T : struct
{
    private static readonly Dictionary<T, Action> _listeners = new();

    public static void Subscribe(T eventType, Action listener)
    {
        if (!_listeners.ContainsKey(eventType)) _listeners[eventType] = null;
        _listeners[eventType] += listener;
    }

    public static void Unsubscribe(T eventType, Action listener)
    {
        if (_listeners.ContainsKey(eventType)) _listeners[eventType] -= listener;
    }

    public static void Publish(T eventType) => _listeners.GetValueOrDefault(eventType)?.Invoke();
}