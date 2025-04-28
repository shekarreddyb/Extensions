using Microsoft.Win32;
using StackExchange.Redis;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace RedisTodoApp
{
    public partial class MainWindow : Window
    {
        private ConnectionMultiplexer redis;
        private IDatabase db;
        private const string RedisKeyPrefix = "todo:";
        private string pfxFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectPfx_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PFX files (*.pfx)|*.pfx"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                pfxFilePath = openFileDialog.FileName;
                PfxPathInput.Text = pfxFilePath;
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cert = new X509Certificate2(pfxFilePath, PfxPasswordInput.Password);
                var options = new ConfigurationOptions
                {
                    EndPoints = { $"{HostInput.Text}:{PortInput.Text}" },
                    Ssl = true,
                    AllowAdmin = true,
                    ClientCertificates = { cert }
                };

                redis = await ConnectionMultiplexer.ConnectAsync(options);
                db = redis.GetDatabase();
                StatusText.Text = "Status: Connected to Redis.";
                LoadTodos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Status: Not connected.";
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TodoInput.Text)) return;

            var todoItem = TodoInput.Text.Trim();
            await db.StringSetAsync($"{RedisKeyPrefix}{Guid.NewGuid()}", todoItem);

            TodoInput.Clear();
            LoadTodos();
        }

        private async void LoadTodos()
        {
            try
            {
                var server = redis.GetServer(redis.GetEndPoints()[0]);
                var keys = server.Keys(pattern: $"{RedisKeyPrefix}*");

                TodoList.Items.Clear();

                foreach (var key in keys)
                {
                    var value = await db.StringGetAsync(key);
                    TodoList.Items.Add(new TodoItem { Key = key, Value = value });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading items: {ex.Message}");
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TodoList.SelectedItem is TodoItem selectedItem)
            {
                await db.KeyDeleteAsync(selectedItem.Key);
                LoadTodos();
            }
        }

        private async void PingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pong = await db.ExecuteAsync("PING");
                MessageBox.Show($"Redis says: {pong}", "Ping", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ping failed: {ex.Message}");
            }
        }
    }

    public class TodoItem
    {
        public RedisKey Key { get; set; }
        public RedisValue Value { get; set; }

        public override string ToString() => Value;
    }
}