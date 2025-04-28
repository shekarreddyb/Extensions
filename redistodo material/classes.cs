using System;
using System.Globalization;
using System.Windows.Data;

namespace RedisTodoApp
{
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value?.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using StackExchange.Redis;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace RedisTodoApp.Services
{
    public class RedisService : IDisposable
    {
        private ConnectionMultiplexer _redis;
        private readonly string _host;
        private readonly int _port;
        private readonly string _pfxPath;
        private readonly string _pfxPassword;

        public RedisService(string host, int port, string pfxPath, string pfxPassword)
        {
            _host = host;
            _port = port;
            _pfxPath = pfxPath;
            _pfxPassword = pfxPassword;
        }

        public async Task ConnectAsync()
        {
            var cert = new X509Certificate2(File.ReadAllBytes(_pfxPath), _pfxPassword);
            var config = new ConfigurationOptions
            {
                EndPoints = { $"{_host}:{_port}" },
                Ssl = true,
                SslClientAuthenticationOptions = _ => new SslClientAuthenticationOptions
                {
                    ClientCertificates = new X509CertificateCollection { cert }
                }
            };

            _redis = await ConnectionMultiplexer.ConnectAsync(config);
        }

        public async Task<string> PingAsync()
        {
            if (_redis == null) throw new InvalidOperationException("Not connected to Redis.");
            var db = _redis.GetDatabase();
            return await db.PingAsync().ConfigureAwait(false) > TimeSpan.Zero ? "PONG" : "ERROR";
        }

        public async Task AddTodoAsync(string todo)
        {
            var db = _redis.GetDatabase();
            await db.ListRightPushAsync("todos", todo);
        }

        public async Task<string[]> GetTodosAsync()
        {
            var db = _redis.GetDatabase();
            var todos = await db.ListRangeAsync("todos");
            return todos.ToStringArray();
        }

        public async Task DeleteTodoAsync(long index)
        {
            var db = _redis.GetDatabase();
            await db.ListRemoveAsync("todos", await db.ListGetByIndexAsync("todos", index));
        }

        public void Dispose()
        {
            _redis?.Dispose();
        }
    }
}


using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using RedisTodoApp.Models;
using RedisTodoApp.Services;

namespace RedisTodoApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly RedisService _redisService;
        private string _host = "localhost";
        private int _port = 6379;
        private string _pfxPath;
        private string _pfxPassword;
        private string _pingResult;
        private string _newTodo;
        private ObservableCollection<TodoItem> _todos = new ObservableCollection<TodoItem>();
        private bool _isConnected;

        public string Host { get => _host; set { _host = value; OnPropertyChanged(); } }
        public int Port { get => _port; set { _port = value; OnPropertyChanged(); } }
        public string PfxPath { get => _pfxPath; set { _pfxPath = value; OnPropertyChanged(); } }
        public string PfxPassword { get => _pfxPassword; set { _pfxPassword = value; OnPropertyChanged(); } }
        public string PingResult { get => _pingResult; set { _pingResult = value; OnPropertyChanged(); } }
        public string NewTodo { get => _newTodo; set { _newTodo = value; OnPropertyChanged(); } }
        public ObservableCollection<TodoItem> Todos { get => _todos; set { _todos = value; OnPropertyChanged(); } }
        public bool IsConnected { get => _isConnected; set { _isConnected = value; OnPropertyChanged(); } }

        public ICommand UploadPfxCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand PingCommand { get; }
        public ICommand AddTodoCommand { get; }
        public ICommand DeleteTodoCommand { get; }

        public MainViewModel()
        {
            UploadPfxCommand = new RelayCommand(UploadPfx);
            ConnectCommand = new RelayCommand(async () => await ConnectAsync());
            PingCommand = new RelayCommand(async () => await PingAsync());
            AddTodoCommand = new RelayCommand(async () => await AddTodoAsync());
            DeleteTodoCommand = new RelayCommand(async (param) => await DeleteTodoAsync(param));

            _redisService = new RedisService(Host, Port, PfxPath, PfxPassword);
        }

        private void UploadPfx()
        {
            var dialog = new OpenFileDialog { Filter = "PFX Files (*.pfx)|*.pfx" };
            if (dialog.ShowDialog() == true)
            {
                PfxPath = dialog.FileName;
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                _redisService.Dispose(); // Dispose previous connection
                _redisService = new RedisService(Host, Port, PfxPath, PfxPassword);
                await _redisService.ConnectAsync();
                IsConnected = true;
                MessageBox.Show("Connected to Redis!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadTodosAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", MessageBoxButton.ggOK, MessageBoxImage.Error);
                IsConnected = false;
            }
        }

        private async Task PingAsync()
        {
            try
            {
                PingResult = await _redisService.PingAsync();
            }
            catch (Exception ex)
            {
                PingResult = $"Error: {ex.Message}";
            }
        }

        private async Task AddTodoAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTodo)) return;
            try
            {
                await _redisService.AddTodoAsync(NewTodo);
                NewTodo = string.Empty;
                await LoadTodosAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add todo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteTodoAsync(object parameter)
        {
            if (parameter is TodoItem todo)
            {
                try
                {
                    await _redisService.DeleteTodoAsync(todo.Index);
                    await LoadTodosAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete todo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task LoadTodosAsync()
        {
            try
            {
                var todos = await _redisService.GetTodosAsync();
                Todos.Clear();
                for (int i = 0; i < todos.Length; i++)
                {
                    Todos.Add(new TodoItem { Description = todos[i], Index = i });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load todos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class BaseViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Action<object> _executeWithParam;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute, Func<bool> canExecute = null)
        {
            _executeWithParam = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter)
        {
            if (_execute != null) _execute();
            else _executeWithParam?.Invoke(parameter);
        }
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}

