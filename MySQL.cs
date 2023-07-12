using System;
using MySql.Data.MySqlClient;

// Install-Package MySql.Data -Version 8.0.23
class Program
{
    static void Main()
    {
        string connStr = "server=localhost;user=root;database=mydatabase;port=3306;password=mypassword";
        MySqlConnection conn = new MySqlConnection(connStr);
        try
        {
            Console.WriteLine("Connecting to MySQL...");
            conn.Open();

            string sql = "SELECT VERSION()";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            string version = cmd.ExecuteScalar().ToString();
            Console.WriteLine("MySQL version : {0}", version);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        conn.Close();
        Console.WriteLine("Done.");
    }
}
