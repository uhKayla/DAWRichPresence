using System.Diagnostics;
using System.IO.Pipes;
using NPlug;

namespace DAWRichPresence;

public static class Logger
{
    private static readonly string logFilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DRPC",
            "DAWRichPresence.log");

    public static void Log(string message)
    {
        try
        {
            using (var writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }
        catch (Exception ex)
        {
            // If logging fails, there's not much we can do.
            Debug.WriteLine($"Logging failed: {ex.Message}");
        }
    }
}

public class DRPProcessor : AudioProcessor<DRPModel>
{
    // Buffer Variables
    private float[] _bufferLeft;
    private float[] _bufferRight;
    private int _bufferPosition;

    // Named Pipe Client
    private NamedPipeClientStream pipeClient;
    private StreamWriter writer;

    // Host Application Name
    private string hostApplicationName;

    // Processor GUID
    public static readonly Guid ClassId = new("7a130e07-004a-408d-a1d8-97b671f36ca2");

    // Processor Constructor
    public DRPProcessor() : base(AudioSampleSizeSupport.Float32)
    {
        _bufferLeft = Array.Empty<float>();
        _bufferRight = Array.Empty<float>();
    }

    // GUID Override
    public override Guid ControllerClassId => DRPController.ClassId;

    protected override bool Initialize(AudioHostApplication host)
    {
        AddAudioInput("AudioInput", SpeakerArrangement.SpeakerStereo);
        AddAudioOutput("AudioOutput", SpeakerArrangement.SpeakerStereo);

        hostApplicationName = host.Name;
        Logger.Log($"Host application name: {hostApplicationName}");

        StartDiscordServiceIfNotRunning();
        TryConnectToDiscordService();

        SendPresenceUpdate("Initialized VST Plugin", $"Ready in {hostApplicationName}");

        return true;
    }

    private void StartDiscordServiceIfNotRunning()
    {
        try
        {
            var isServiceRunning = false;

            if (!isServiceRunning)
            {
                var servicePath =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DRPC",
                        "DAWRichPresence.Service.exe");
                if (!File.Exists(servicePath))
                {
                    Logger.Log($"Service executable not found at {servicePath}");
                    return;
                }

                Logger.Log($"Host application name: {hostApplicationName}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = servicePath,
                    Arguments = $"\"{hostApplicationName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                var process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                Logger.Log($"Process ID: {process.Id}, {process.ProcessName}");

                if (process.Id == null || process.Id == 0) Logger.Log("Process seems to not exist...");
            }
            else
            {
                Logger.Log("Discord Service is already running.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Could not start Discord Service: " + ex.Message);
            Logger.Log(ex.StackTrace);
        }
    }

    private void TryConnectToDiscordService()
    {
        try
        {
            pipeClient = new NamedPipeClientStream(".", "DiscordPipe", PipeDirection.Out);
            pipeClient.Connect(5000); // Timeout after 5 seconds
            writer = new StreamWriter(pipeClient) { AutoFlush = true };
            Logger.Log("Connected to Discord Service.");
        }
        catch (Exception ex)
        {
            Logger.Log("Could not connect to Discord Service: " + ex.Message);
            Logger.Log(ex.StackTrace);
        }
    }

    protected override void OnActivate(bool isActive)
    {
        if (isActive)
        {
            var delayInSamples = (int)(ProcessSetupData.SampleRate * sizeof(float) + 0.5);
            _bufferLeft = GC.AllocateArray<float>(delayInSamples, true);
            _bufferRight = GC.AllocateArray<float>(delayInSamples, true);
            _bufferPosition = 0;

            SendPresenceUpdate("Processing Audio", $"Active in {hostApplicationName}");
        }
        else
        {
            _bufferLeft = Array.Empty<float>();
            _bufferRight = Array.Empty<float>();
            _bufferPosition = 0;

            SendPresenceUpdate("Initialized VST Plugin", $"Inactive in {hostApplicationName}");
        }
    }

    // Processor main function (we do not process anything here)
    protected override void ProcessMain(in AudioProcessData data)
    {
        // Optional: Update Discord presence based on audio processing if needed
    }

    private void SendPresenceUpdate(string details, string state)
    {
        if (writer != null)
        {
            writer.WriteLine($"{details};{state}");
            Logger.Log($"Sent presence update: Details = {details}, State = {state}");
        }
        else
        {
            Logger.Log("Writer is null, presence update not sent.");
        }
    }

    protected override void Terminate()
    {
        writer?.Dispose();
        pipeClient?.Dispose();
        Logger.Log("Disposed of writer and pipe client.");
        base.Terminate();
    }
}