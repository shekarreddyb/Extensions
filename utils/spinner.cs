public class ConsoleSpinner
{
    private int _counter;
    private readonly string[] _sequence = { "/", "-", "\\", "|" };
    private readonly int _delay;
    private bool _active;
    private Thread? _thread;

    public ConsoleSpinner(int delay = 100)
    {
        _delay = delay;
    }

    public void Start(string message = "Loading")
    {
        _active = true;
        _thread = new Thread(() =>
        {
            while (_active)
            {
                Turn(message);
                Thread.Sleep(_delay);
            }
        });
        _thread.Start();
    }

    public void Stop()
    {
        _active = false;
        if (_thread != null)
            _thread.Join();
        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r"); // clear line
    }

    private void Turn(string message)
    {
        var symbol = _sequence[_counter % _sequence.Length];
        Console.Write($"\r{message} {symbol}");
        _counter++;
    }
}