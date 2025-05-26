public class ConsoleSpinner
{
    private readonly string[] _sequence = { "/", "-", "\\", "|" };
    private int _counter = 0;
    private bool _active;
    private Thread? _thread;
    private string _label = "";
    private readonly Stopwatch _stopwatch = new();

    public string StatusText { get; set; } = "";

    public void Start(string label)
    {
        _label = label;
        _stopwatch.Restart();
        _active = true;

        _thread = new Thread(() =>
        {
            while (_active)
            {
                Draw();
                Thread.Sleep(100);
            }
        });

        _thread.Start();
    }

    public void Stop(string doneMessage = "Done")
    {
        _active = false;
        _thread?.Join();
        _stopwatch.Stop();

        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r"); // clear line
        Console.WriteLine($"[✓] {_label}: {doneMessage} in {_stopwatch.Elapsed.TotalSeconds:F1}s");
    }

    private void Draw()
    {
        var symbol = _sequence[_counter++ % _sequence.Length];
        var time = $"{_stopwatch.Elapsed.TotalSeconds:F1}s";

        Console.Write($"\r[{symbol}] {_label}... {StatusText} ({time})");
    }
}