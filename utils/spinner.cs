public class ConsoleSpinner
{
    private readonly string[] _sequence = { "/", "-", "\\", "|" };
    private int _counter = 0;
    private bool _active;
    private Thread? _thread;
    private readonly Stopwatch _stopwatch = new();
    private readonly int _consoleLine;

    private string _label = "";
    public string StatusText { get; set; } = "";

    public ConsoleSpinner(int consoleLine)
    {
        _consoleLine = consoleLine;
    }

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

        lock (Console.Out)
        {
            Console.SetCursorPosition(0, _consoleLine);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, _consoleLine);
            Console.WriteLine($"[✓] {_label}: {doneMessage} in {_stopwatch.Elapsed.TotalSeconds:F1}s");
        }
    }

    private void Draw()
    {
        var symbol = _sequence[_counter++ % _sequence.Length];
        var time = $"{_stopwatch.Elapsed.TotalSeconds:F1}s";

        lock (Console.Out)
        {
            Console.SetCursorPosition(0, _consoleLine);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, _consoleLine);
            Console.Write($"[{symbol}] {_label}... {StatusText} ({time})");
        }
    }
}