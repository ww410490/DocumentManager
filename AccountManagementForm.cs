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

namespace DocumentManager
{
    public partial class AccountManagementForm : Form
    {
        public AccountManagementForm()
        {
            InitializeComponent();
            SetupComboBox();
            LoadUserData();

            SetTextBoxesEnabled(false, false, false);
        }

        private void SetupComboBox()
        {
            cmbRole.Items.Add("Admin");
            cmbRole.Items.Add("Archivist");
        }

        private void SetTextBoxesEnabled(bool usernameEnabled, bool passwordEnabled, bool permissionEnabled)
        {
            txtUsername.Enabled = usernameEnabled;
            txtPassword.Enabled = passwordEnabled;
            cmbRole.Enabled = permissionEnabled;
        }

        private void LoadUserData()
        {
            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Username, Password, Role FROM Users";

                using (var adapter = new SQLiteDataAdapter(query, connection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    dataGridViewUsers.DataSource = dataTable;
                }
            }
        }

        private void UpdateUserData(int userId, string password, string role)
        {
            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Users SET Password = @Password, Role = @Role WHERE Id = @UserId";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Role", role);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        

        private void DeleteUserData(int userId)
        {
            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM Users WHERE Id = @userId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.ExecuteNonQuery();
                }
            }
        }

        

        private void InsertUserData(string username, string password, string role)
        {
            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                INSERT INTO Users (Username, Password, Role)
                VALUES (@Username, @Password, @Role);
            ";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password); // 默认密码，建议提示用户更改
                        command.Parameters.AddWithValue("@Role", role);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SQLiteException ex)
            {
                // 处理 SQLite 异常，例如违反唯一约束
                MessageBox.Show($"数据库错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // 处理其他类型的异常
                MessageBox.Show($"未知错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private int GetLastInsertedId()
        {
            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT last_insert_rowid()";

                using (var command = new SQLiteCommand(query, connection))
                {
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        private void dataGridViewUsers_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //新增
            // 使用函数启用 TextBox 控件
            SetTextBoxesEnabled(true, true, true);

            // 清空 TextBox 的内容
            txtUsername.Clear();
            txtPassword.Clear();

            // 将焦点设置到第一个输入框
            txtUsername.Focus();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //儲存
            // 从 TextBox 获取用户输入的数据
            string username = txtUsername.Text;
            string password = txtPassword.Text;
            string role = cmbRole.Text;

            // 数据验证（可以根据需要添加更多验证规则）
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                MessageBox.Show("所有字段都是必填的。", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 判断是新增还是修改
            if (txtUsername.Enabled)
            {
                // 保存到 SQLite 数据库
                InsertUserData(username, password, role);
            }
            else
            {
                // 修改操作
                int userId = Convert.ToInt32(dataGridViewUsers.SelectedRows[0].Cells["Id"].Value);
                UpdateUserData(userId, password, role);
            }

            // 刷新 DataGridView
            LoadUserData();

            // 使用函数禁用 TextBox 控件
            SetTextBoxesEnabled(false, false, false);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //修改
            // 从选定的 DataGridView 行获取用户信息
            if (dataGridViewUsers.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridViewUsers.SelectedRows[0];
                txtUsername.Text = selectedRow.Cells["Username"].Value.ToString();
                txtPassword.Text = selectedRow.Cells["Password"].Value.ToString();
                cmbRole.Text = selectedRow.Cells["Role"].Value.ToString();

                // 只启用密码和权限的 TextBox 控件
                SetTextBoxesEnabled(false, true, true);
            }
            else
            {
                MessageBox.Show("请先选择要修改的用户。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //刪除
            if (dataGridViewUsers.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridViewUsers.SelectedRows[0];
                int userId = Convert.ToInt32(selectedRow.Cells["Id"].Value);

                // 确认删除
                DialogResult dialogResult = MessageBox.Show("您确定要删除该用户吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogResult == DialogResult.Yes)
                {
                    DeleteUserData(userId);
                    LoadUserData();  // 刷新 DataGridView
                }
            }
            else
            {
                MessageBox.Show("请先选择要删除的用户。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
