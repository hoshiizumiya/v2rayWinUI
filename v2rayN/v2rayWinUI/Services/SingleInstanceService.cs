using System;
using System.Threading;

namespace v2rayWinUI.Services;

public class SingleInstanceService : IDisposable
{
    private const string MutexName = "v2rayN_single_instance_mutex";
    private const string EventName = "v2rayN_single_instance_event";

    private Mutex? _mutex;
    private EventWaitHandle? _eventWaitHandle;

    /// <summary>
    /// Initialize single instance service. Returns true if this is the first instance.
    /// If not first instance, signals the existing instance to activate and returns false.
    /// </summary>
    public bool Initialize(Action? activateCallback)
    {
        bool created;
        try
        {
            _mutex = new Mutex(true, MutexName, out created);
        }
        catch
        {
            created = false;
        }

        if (created)
        {
            try
            {
                // Create named event and register a wait to call back when signaled
                _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
                ThreadPool.RegisterWaitForSingleObject(_eventWaitHandle, (state, timedOut) =>
                {
                    try
                    {
                        activateCallback?.Invoke();
                    }
                    catch { }
                }, null, -1, false);
            }
            catch { }

            return true;
        }

        try
        {
            // Signal existing instance to activate
            using EventWaitHandle existing = EventWaitHandle.OpenExisting(EventName);
            existing.Set();
        }
        catch { }

        return false;
    }

    public void Dispose()
    {
        try
        {
            _eventWaitHandle?.Close();
        }
        catch { }
        try
        {
            if (_mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch { }
                _mutex.Dispose();
            }
        }
        catch { }
    }
}
