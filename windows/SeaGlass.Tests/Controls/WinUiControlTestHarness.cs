using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeaGlass.Tests.Controls;

public sealed class TestApplication : Microsoft.UI.Xaml.Application
{
}

/// <summary>
/// Execution harness for WinUI 3 / Windows App SDK tests requiring an STA thread apartment and XamlCore.
/// </summary>
public static class WinUiControlTestHarness
{
    private static bool _bootstrapInitialized;
    private static Thread? _xamlThread;
    private static Microsoft.UI.Dispatching.DispatcherQueue? _xamlQueue;
    private static readonly ManualResetEventSlim _xamlStarted = new(false);
    private static Exception? _xamlStartError;
    private static readonly object _initLock = new();

    public static void EnsureBootstrap()
    {
        if (_bootstrapInitialized) return;
        var minVersion = new Microsoft.Windows.ApplicationModel.DynamicDependency.PackageVersion(0x0002000500010000);
        bool success = Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.TryInitialize(
            0x00020005, "", minVersion,
            Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.InitializeOptions.None,
            out int hr);
        if (!success)
        {
            throw new InvalidOperationException($"Bootstrap.TryInitialize failed with hr=0x{hr:X8}");
        }
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        _bootstrapInitialized = true;
    }

    public static void EnsureApplication()
    {
        if (_xamlQueue != null) return;
        lock (_initLock)
        {
            if (_xamlQueue != null) return;
            EnsureBootstrap();
            _xamlThread = new Thread(() =>
            {
                try
                {
                    Microsoft.UI.Xaml.Application.Start(p =>
                    {
                        try
                        {
                            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                            SynchronizationContext.SetSynchronizationContext(context);
                            _xamlQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                            _ = new TestApplication();
                            _xamlStarted.Set();
                        }
                        catch (Exception ex)
                        {
                            _xamlStartError = ex;
                            _xamlStarted.Set();
                        }
                    });
                }
                catch (Exception ex)
                {
                    _xamlStartError = ex;
                    _xamlStarted.Set();
                }
            });
            _xamlThread.SetApartmentState(ApartmentState.STA);
            _xamlThread.IsBackground = true;
            _xamlThread.Start();

            if (!_xamlStarted.Wait(10000))
            {
                throw new TimeoutException("Timed out waiting for WinUI 3 Application.Start to initialize");
            }
            if (_xamlStartError != null)
            {
                throw new InvalidOperationException("Failed to initialize WinUI 3 Application.Start", _xamlStartError);
            }
        }
    }

    public static void RunOnStaThread(Action action)
    {
        EnsureApplication();
        if (Thread.CurrentThread == _xamlThread)
        {
            action();
            return;
        }

        Exception? error = null;
        using var done = new ManualResetEventSlim(false);
        bool enqueued = _xamlQueue!.TryEnqueue(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException comEx)
                {
                    error = new Exception($"COMException HResult=0x{comEx.HResult:X8}: {comEx.Message}", ex);
                }
                else
                {
                    error = ex;
                }
            }
            finally
            {
                done.Set();
            }
        });

        if (!enqueued)
        {
            throw new InvalidOperationException("Failed to enqueue action to WinUI 3 DispatcherQueue");
        }

        done.Wait();
        if (error != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    public static async Task<T> RunOnStaThreadAsync<T>(Func<Task<T>> asyncFunc)
    {
        EnsureApplication();
        var tcs = new TaskCompletionSource<T>();
        _xamlQueue!.TryEnqueue(async () =>
        {
            try
            {
                T result = await asyncFunc();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return await tcs.Task;
    }
}
