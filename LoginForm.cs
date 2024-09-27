using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace DocumentManager
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();

            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath));

            // 创建连接
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    Role TEXT NOT NULL
                );
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
                {
                    cmd.ExecuteNonQuery();

                    // 插入默认 admin 用户
                    InsertDefaultAdminUser(conn);
                }

                conn.Close();
            }
        }

        private void InsertDefaultAdminUser(SQLiteConnection connection)
        {
            // 检查 admin 用户是否已经存在
            string checkAdminQuery = @"
                SELECT COUNT(*)
                FROM Users
                WHERE Username = 'admin';
            ";

            using (var checkCommand = new SQLiteCommand(checkAdminQuery, connection))
            {
                long count = (long)checkCommand.ExecuteScalar();
                if (count == 0)
                {
                    // 插入默认 admin 用户
                    string insertAdminQuery = @"
                        INSERT INTO Users (Username, Password, Role)
                        VALUES ('admin', @Password, 'Admin');
                    ";

                    using (var insertCommand = new SQLiteCommand(insertAdminQuery, connection))
                    {
                        // 默认 admin 密码（建议实际使用中更改）
                        insertCommand.Parameters.AddWithValue("@Password", "123");
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // login btn
            string username = usernameTextBox.Text;
            string password = passwordTextBox.Text;

            if (ValidateCredentials(username, password))
            {
                // 登录成功，打开主窗体
                this.Hide();
                Form1 mainForm = new Form1(username);
                mainForm.ShowDialog();
                this.Close();
            }
            else
            {
                // 登录失败，显示错误信息
                MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateCredentials(string username, string password)
        {
            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT COUNT(*)
                    FROM Users
                    WHERE Username = @Username AND Password = @Password;
                ";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    long count = (long)command.ExecuteScalar();
                    return count > 0; // 如果返回值大于 0，表示凭据有效
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //一般使用者
            // 登录成功，打开主窗体
            this.Hide();
            Form1 mainForm = new Form1("user");
            mainForm.ShowDialog();
            this.Close();
        }
    }
}
