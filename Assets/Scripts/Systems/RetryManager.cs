using System;
using UnityEngine;

public class RetryManager : MonoBehaviour
{
    public static event Action<int, int, bool> OnAttemptsUpdated; // current, max, canRetry
    public static event Action OnMaxAttemptsReached;

    [SerializeField] private int maxFailuresPerModule = 3;
    private int _currentFailures = 0;
    private int _currentModule = 0;

    public void RecordAttempt(int moduleId, bool isCorrect)
    {
        if (moduleId != _currentModule) 
        { 
            _currentModule = moduleId; 
            _currentFailures = 0; 
        }
        
        if (!isCorrect)
        {
            _currentFailures++;
            bool canRetry = _currentFailures < maxFailuresPerModule;
            OnAttemptsUpdated?.Invoke(_currentFailures, maxFailuresPerModule, canRetry);
            
            if (!canRetry) 
                OnMaxAttemptsReached?.Invoke(); // ✅ Invocación correcta de evento estático
        }
        else
        {
            _currentFailures = 0; // Reset on success
            OnAttemptsUpdated?.Invoke(0, maxFailuresPerModule, true);
        }
    }

    public void ResetForNewModule() 
    { 
        _currentFailures = 0; 
        _currentModule++; 
    }
}