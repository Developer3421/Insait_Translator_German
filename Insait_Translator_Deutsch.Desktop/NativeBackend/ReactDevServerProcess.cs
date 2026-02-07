using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Insait_Translator_Deutsch.Desktop.NativeBackend;

internal sealed class ReactDevServerProcess : IAsyncDisposable
{
    private readonly string _workingDirectory;
    private Process? _process;
    private readonly int _port;
    private readonly string _backendUrl;

    public ReactDevServerProcess(string workingDirectory, int port = 4200, string backendUrl = "http://localhost:4200")
    {
        _workingDirectory = workingDirectory;
        _port = port;
        _backendUrl = backendUrl;
    }

    public bool IsRunning => _process is { HasExited: false };

    public void Start()
    {
        if (IsRunning) return;

        if (!Directory.Exists(_workingDirectory))
            throw new DirectoryNotFoundException($"React working directory not found: {_workingDirectory}");

        // Check if node_modules exists, if not run npm install first
        var nodeModulesPath = Path.Combine(_workingDirectory, "node_modules");
        if (!Directory.Exists(nodeModulesPath))
        {
            Debug.WriteLine("[React] node_modules not found, running npm install...");
            RunNpmInstall();
        }

        // On Windows you typically need npm.cmd for ProcessStartInfo (npm is a cmd shim).
        var npmExe = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";

        var psi = new ProcessStartInfo
        {
            FileName = npmExe,
            Arguments = "run dev",
            WorkingDirectory = _workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        // Set environment variables for Vite
        psi.Environment["INSAIT_UI_PORT"] = _port.ToString();
        psi.Environment["INSAIT_UI_HOST"] = "127.0.0.1";
        psi.Environment["INSAIT_BACKEND_URL"] = _backendUrl;

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        _process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Debug.WriteLine($"[React] {e.Data}");
        };
        _process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Debug.WriteLine($"[React][ERR] {e.Data}");
        };

        Debug.WriteLine($"[React] Starting dev server on port {_port}...");
        
        if (!_process.Start())
            throw new InvalidOperationException("Failed to start React dev server process.");

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        
        Debug.WriteLine($"[React] Dev server process started (PID: {_process.Id})");
    }
    
    private void RunNpmInstall()
    {
        var npmExe = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
        
        var psi = new ProcessStartInfo
        {
            FileName = npmExe,
            Arguments = "install",
            WorkingDirectory = _workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        using var process = new Process { StartInfo = psi };
        
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Debug.WriteLine($"[npm install] {e.Data}");
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                Debug.WriteLine($"[npm install][ERR] {e.Data}");
        };
        
        if (process.Start())
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit(120000); // 2 minutes timeout for npm install
            Debug.WriteLine($"[npm install] Completed with exit code: {process.ExitCode}");
        }
    }

    public async Task<bool> WaitUntilReadyAsync(Uri baseAddress, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            if (_process != null && _process.HasExited)
                return false;

            try
            {
                using var resp = await http.GetAsync(baseAddress, ct).ConfigureAwait(false);
                if ((int)resp.StatusCode is >= 200 and < 500)
                    return true;
            }
            catch
            {
                // ignore during warmup
            }

            await Task.Delay(250, ct).ConfigureAwait(false);
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        Debug.WriteLine("[React] Disposing React dev server...");
        
        try
        {
            if (_process != null && !_process.HasExited)
            {
                Debug.WriteLine($"[React] Killing process tree (PID: {_process.Id})...");
                try { _process.Kill(entireProcessTree: true); } catch { }
                
                // Wait a bit for the process to exit
                try 
                { 
                    await Task.Run(() => _process.WaitForExit(3000)); 
                }
                catch { }
            }

            _process?.Dispose();
            _process = null;

            // On Windows, also kill any remaining node processes on the port
            if (OperatingSystem.IsWindows())
            {
                await KillProcessOnPortAsync(_port);
            }
            
            Debug.WriteLine("[React] React dev server disposed.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[React] Error during dispose: {ex.Message}");
        }
    }

    private static async Task KillProcessOnPortAsync(int port)
    {
        try
        {
            // Find PID using netstat and kill it
            var findPsi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c netstat -ano | findstr :{port} | findstr LISTENING",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var findProcess = Process.Start(findPsi);
            if (findProcess == null) return;

            var output = await findProcess.StandardOutput.ReadToEndAsync();
            await Task.Run(() => findProcess.WaitForExit(5000));

            if (string.IsNullOrWhiteSpace(output)) return;

            // Parse PID from netstat output (last column)
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && int.TryParse(parts[^1].Trim(), out var pid) && pid > 0)
                {
                    Debug.WriteLine($"[React] Killing process on port {port} (PID: {pid})...");
                    try
                    {
                        using var proc = Process.GetProcessById(pid);
                        proc.Kill(entireProcessTree: true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[React] Could not kill PID {pid}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[React] Error killing process on port: {ex.Message}");
        }
    }
}
