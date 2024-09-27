using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Data.SQLite;
using System.IO;
using System.Configuration;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DocumentManager
{
    public partial class Form1 : Form
    {
        private string _username;
        public Form1(string username)
        {
            InitializeComponent();
            // 设置为全屏模式
            this.WindowState = FormWindowState.Maximized;

            _username = username;
            userLabel.Text = $@"Hi! {_username}";

            if (_username.ToLower() == "user") {
                SetReadOnlyMode();
            }

            if (_username.ToLower() != "admin") { 
                accountManagementButton.Visible = false;

                //刪除&資料匯入僅限admin可做, archivist則移除這兩個功能
                //資料匯入的按鈕確定要下放給Archivist
                button3.Enabled = false;
                //button7.Enabled = false;

            }

            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath));

            // 创建连接
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS DocumentArchive (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ArchiveDate TEXT,
                    Archivist TEXT,
                    ShelfNumber TEXT,
                    Department TEXT,
                    Owner TEXT,
                    YearOfDoc INTEGER,
                    ContentsOfDoc TEXT,
                    ArchivePeriod TEXT,
                    TransferDate TEXT,
                    RetrievalDate TEXT,
                    ReturnDate TEXT,
                    DisposalDate TEXT,
                    Status TEXT,
                    Note TEXT,
                    ModifyDate TEXT
                );
                ";

                using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }


            LoadArchivistOptions();
            LoadDepartmentOptions();
            LoadStatusOptions();
            InitializeDataGridView();
            LoadData();
            SetTextBoxesReadOnly(true);
            LoadContentsOfDocOptions();
            LoadYearOfDocOptions();

            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;

            transferDateTextBox.Click += transferDateTextBox_Click;
            transferDateTextBox.KeyDown += transferDateTextBox_KeyDown;

            retrievalDateTextBox.Click += retrievalDateTextBox_Click;
            retrievalDateTextBox.KeyDown += retrievalDateTextBox_KeyDown;

            returnDateTextBox.Click += returnDateTextBox_Click;
            returnDateTextBox.KeyDown += returnDateTextBox_KeyDown;

            disposalDateTextBox.Click += disposalDateTextBox_Click;
            disposalDateTextBox.KeyDown += disposalDateTextBox_KeyDown;

            yearOfDocComboBox.KeyPress += yearOfDocComboBox_KeyPress;
            yearOfDocComboBox.TextChanged += yearOfDocComboBox_TextChanged;

            archivePeriodTextBox.KeyPress += archivePeriodTextBox_KeyPress;

            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        }


        private void LoadArchivistOptions()
        {
            string archivists = ConfigurationManager.AppSettings["Archivists"];
            if (!string.IsNullOrEmpty(archivists))
            {
                string[] archivistArray = archivists.Split(',');
                archivistComboBox.Items.AddRange(archivistArray);
            }
        }

        private void LoadDepartmentOptions()
        {
            string departments = ConfigurationManager.AppSettings["Departments"];
            if (!string.IsNullOrEmpty(departments))
            {
                string[] departmentArray = departments.Split(',');
                //departmentComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                departmentComboBox.Items.AddRange(departmentArray);

                // 设置 DropDownHeight，显示的项数为10行（根据需要调整）
                departmentComboBox.DropDownHeight = departmentComboBox.ItemHeight * 10;
            }
        }

        private void LoadStatusOptions()
        {
            string statusOptions = ConfigurationManager.AppSettings["StatusOptions"];
            if (!string.IsNullOrEmpty(statusOptions))
            {
                string[] statusArray = statusOptions.Split(',');
                //statusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                statusComboBox.Items.AddRange(statusArray);
            }
        }

        private void LoadContentsOfDocOptions()
        {
            string contentsOfDocOptions = ConfigurationManager.AppSettings["ContentsOfDocOptions"];
            if (!string.IsNullOrEmpty(contentsOfDocOptions))
            {
                string[] contentsOfDocArray = contentsOfDocOptions.Split(',');
                //statusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                contentOfDoccomboBox.Items.AddRange(contentsOfDocArray);
            }
        }

        private void LoadYearOfDocOptions()
        {
            // 获取当前年份
            int currentYear = DateTime.Now.Year;

            // 清空 ComboBox 中的现有项目
            yearOfDocComboBox.Items.Clear();

            // 添加过去20年的年份
            for (int year = currentYear; year >= currentYear - 20; year--)
            {
                yearOfDocComboBox.Items.Add(year.ToString());
            }

            // 设置默认选项为当前年份
            yearOfDocComboBox.SelectedIndex = 0;

            // 设置 DropDownHeight，显示的项数为10行（根据需要调整）
            yearOfDocComboBox.DropDownHeight = yearOfDocComboBox.ItemHeight * 10;
        }


        private void button8_Click(object sender, EventArgs e)
        {
            //儲存
            try
            {
                // 从表单中获取数据
                string archiveDate = archiveDateTextBox.Text;
                string archivist = archivistComboBox.Text;
                string shelfNumber = shelfNumberTextBox.Text;
                string department = departmentComboBox.Text;
                string owner = ownerTextBox.Text;
                string yearOfDoc = yearOfDocComboBox.Text;
                string contentsOfDoc = contentOfDoccomboBox.Text;
                string archivePeriod = archivePeriodTextBox.Text;
                string transferDate = transferDateTextBox.Text;
                string retrievalDate = retrievalDateTextBox.Text;
                string returnDate = returnDateTextBox.Text;
                string disposalDate = disposalDateTextBox.Text;
                string status = statusComboBox.Text;
                string note = noteTextBox.Text;

                //Archivist, Shelf#,Dept., Owner, Year of Doc, Content of Doc, Archive Period必須有值
                if (string.IsNullOrEmpty(archivist)) { 
                    MessageBox.Show($@"Archivist 必須有值");
                    return;
                }
                if (string.IsNullOrEmpty(shelfNumber))
                {
                    MessageBox.Show($@"Shelf# 必須有值");
                    return;
                }
                if (string.IsNullOrEmpty(department))
                {
                    MessageBox.Show($@"Department 必須有值");
                    return;
                }
                if (string.IsNullOrEmpty(owner))
                {
                    MessageBox.Show($@"Owner 必須有值");
                    return;
                }
                if (string.IsNullOrEmpty(yearOfDoc))
                {
                    MessageBox.Show($@"Year of Doc 必須有值");
                    return;
                }
                if (string.IsNullOrEmpty(contentsOfDoc))
                {
                    MessageBox.Show($@"Content of Doc 必須有值");
                    return;
                }
                if (string.IsNullOrEmpty(archivePeriod))
                {
                    MessageBox.Show($@"Archive Period 必須有值");
                    return;
                }

                if (string.IsNullOrEmpty(note) && !string.IsNullOrEmpty(transferDate))
                {
                    MessageBox.Show($@"請輸入Transfer 原因(Note)");
                    return;
                }


                // 数据库连接字符串
                string databasePath = System.IO.Path.Combine(Application.StartupPath, "archive.db");
                string connectionString = $"Data Source={databasePath};Version=3;";

                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();

                    // 获取选中的行的ID（如果有选中行）
                    int selectedId = -1;
                    if (dataGridView1.SelectedRows.Count > 0)
                    {
                        selectedId = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["Id"].Value);
                    }

                    if (selectedId != -1) //準備更新 update
                    {
                        // 记录已存在，执行更新操作
                        string updateQuery = @"
                    UPDATE DocumentArchive
                    SET ArchiveDate = @ArchiveDate,
                        Archivist = @Archivist,
                        ShelfNumber = @ShelfNumber,
                        Department = @Department,
                        Owner = @Owner,
                        YearOfDoc = @YearOfDoc,
                        ContentsOfDoc = @ContentsOfDoc,
                        ArchivePeriod = @ArchivePeriod,
                        TransferDate = @TransferDate,
                        RetrievalDate = @RetrievalDate,
                        ReturnDate = @ReturnDate,
                        DisposalDate = @DisposalDate,
                        Status = @Status,
                        Note = @Note,
                        ModifyDate = @ModifyDate
                    WHERE Id = @Id";

                        using (SQLiteCommand updateCmd = new SQLiteCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@ArchiveDate", archiveDate);
                            updateCmd.Parameters.AddWithValue("@Archivist", archivist);
                            if (status == "Transfer" || status == "Disposal") { shelfNumber = ""; }
                            updateCmd.Parameters.AddWithValue("@ShelfNumber", shelfNumber);
                            updateCmd.Parameters.AddWithValue("@Department", department);
                            updateCmd.Parameters.AddWithValue("@Owner", owner);
                            updateCmd.Parameters.AddWithValue("@YearOfDoc", yearOfDoc);
                            updateCmd.Parameters.AddWithValue("@ContentsOfDoc", contentsOfDoc);
                            updateCmd.Parameters.AddWithValue("@ArchivePeriod", archivePeriod);
                            updateCmd.Parameters.AddWithValue("@TransferDate", transferDate);
                            updateCmd.Parameters.AddWithValue("@RetrievalDate", retrievalDate);
                            updateCmd.Parameters.AddWithValue("@ReturnDate", returnDate);
                            updateCmd.Parameters.AddWithValue("@DisposalDate", disposalDate);
                            updateCmd.Parameters.AddWithValue("@Status", status);
                            updateCmd.Parameters.AddWithValue("@Note", note);
                            updateCmd.Parameters.AddWithValue("@Id", selectedId);
                            updateCmd.Parameters.AddWithValue("@ModifyDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));

                            updateCmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("修改成功!");
                        LogAction("Modify", this._username, $@"{selectedId},{archiveDate},{archivist},{shelfNumber},{department},{owner},{yearOfDoc},{contentsOfDoc},{archivePeriod},{transferDate},{retrievalDate},{returnDate},{disposalDate},{status},{note}");

                    }
                    else // 準備新增 insert
                    {
                        // 检查是否存在相同的 ShelfNumber 但不同的 ArchiveDate
                        string checkShelfNumberQuery = @"
                            SELECT COUNT(*) FROM DocumentArchive
                            WHERE ShelfNumber = @ShelfNumber
                            AND SUBSTR(@ArchiveDate, 1, 10) not in 
                            (SELECT SUBSTR(ArchiveDate, 1, 10) from DocumentArchive where Archivist = @Archivist)
                            ";

                        int shelfCount;
                        using (SQLiteCommand checkShelfCmd = new SQLiteCommand(checkShelfNumberQuery, conn))
                        {
                            checkShelfCmd.Parameters.AddWithValue("@ShelfNumber", shelfNumber);
                            checkShelfCmd.Parameters.AddWithValue("@ArchiveDate", archiveDate);
                            checkShelfCmd.Parameters.AddWithValue("@Archivist", archivist);

                            shelfCount = Convert.ToInt32(checkShelfCmd.ExecuteScalar());
                        }

                        //同天+同個User使用同個shelf#不出現warning window

                        if (shelfCount > 0)
                        {
                            // 存在相同的 ShelfNumber 但不同的 ArchiveDate，显示警告消息并请求确认
                            DialogResult result = MessageBox.Show("此Shelf Number已被使用,請確認是否新增?",
                                                                  "Shelf Number Conflict", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                            if (result == DialogResult.No)
                            {
                                // 用户选择 "否"，不插入记录
                                return;
                            }
                        }

                        // 如果 ShelfNumber 和 ArchiveDate 的检查通过或用户选择 "是"，继续插入记录
                        // 检查是否存在完全相同的记录
                        // 此組合改成 Shelf# + Department + Owner + Year of Doc + Contents of Doc + Note
                        string checkQuery = @"
                            SELECT COUNT(*) FROM DocumentArchive
                            WHERE ShelfNumber = @ShelfNumber
                            AND Department = @Department
                            AND Owner = @Owner
                            AND YearOfDoc = @YearOfDoc
                            AND Note = @Note
                            AND ContentsOfDoc = @ContentsOfDoc";

                        int count;
                        using (SQLiteCommand checkCmd = new SQLiteCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@ShelfNumber", shelfNumber);
                            checkCmd.Parameters.AddWithValue("@Department", department);
                            checkCmd.Parameters.AddWithValue("@Owner", owner);
                            checkCmd.Parameters.AddWithValue("@YearOfDoc", yearOfDoc);
                            checkCmd.Parameters.AddWithValue("@Note", note);
                            checkCmd.Parameters.AddWithValue("@ContentsOfDoc", contentsOfDoc);

                            count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        }

                        if (count > 0)
                        {
                            // 记录完全相同，显示警告消息
                            MessageBox.Show("已經存在重複的文件資料", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        else
                        {
                            // 记录不存在，执行插入操作
                            string insertQuery = @"
                                INSERT INTO DocumentArchive (
                                    ArchiveDate, Archivist, ShelfNumber, Department, Owner, YearOfDoc, ContentsOfDoc, 
                                    ArchivePeriod, TransferDate, RetrievalDate, ReturnDate, DisposalDate, Status, Note
                                ) VALUES (
                                    @ArchiveDate, @Archivist, @ShelfNumber, @Department, @Owner, @YearOfDoc, @ContentsOfDoc,
                                    @ArchivePeriod, @TransferDate, @RetrievalDate, @ReturnDate, @DisposalDate, @Status, @Note
                                )";

                            using (SQLiteCommand insertCmd = new SQLiteCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@ArchiveDate", archiveDate);
                                insertCmd.Parameters.AddWithValue("@Archivist", archivist);
                                if (status == "Transfer" || status == "Disposal") { shelfNumber = ""; }
                                insertCmd.Parameters.AddWithValue("@ShelfNumber", shelfNumber);
                                insertCmd.Parameters.AddWithValue("@Department", department);
                                insertCmd.Parameters.AddWithValue("@Owner", owner);
                                insertCmd.Parameters.AddWithValue("@YearOfDoc", yearOfDoc);
                                insertCmd.Parameters.AddWithValue("@ContentsOfDoc", contentsOfDoc);
                                insertCmd.Parameters.AddWithValue("@ArchivePeriod", archivePeriod);
                                insertCmd.Parameters.AddWithValue("@TransferDate", transferDate);
                                insertCmd.Parameters.AddWithValue("@RetrievalDate", retrievalDate);
                                insertCmd.Parameters.AddWithValue("@ReturnDate", returnDate);
                                insertCmd.Parameters.AddWithValue("@DisposalDate", disposalDate);
                                insertCmd.Parameters.AddWithValue("@Status", status);
                                insertCmd.Parameters.AddWithValue("@Note", note);

                                insertCmd.ExecuteNonQuery();
                            }

                            MessageBox.Show("新增成功!");
                        }
                    }

                    conn.Close();
                }

                LoadData(); // 更新 DataGridView 显示最新数据
                SetTextBoxesReadOnly(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"錯誤訊息: {ex.Message}");
            }
        }



        private void LoadData()
        {
            string connectionString = $"Data Source={Path.Combine(Application.StartupPath, "archive.db")};Version=3;";
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                // 1. 更新符合条件的记录的 Status
                //UpdateStatus(conn);

                // 2. 获取所有数据
                string selectQuery = @"
                    SELECT * FROM DocumentArchive
                    ORDER BY Id DESC
                ";

                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(selectQuery, conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataGridView1.DataSource = dt; // 假设你在表单上有一个名为 dataGridView1 的 DataGridView 控件
                }
            }

            SetupDataGridView();
        }

        private void SetupDataGridView()
        {
            // Disable automatic column resizing
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Set width and minimum width for each column
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.Width = 100;        // Set the width you want
                col.MinimumWidth = 50; // Set the minimum width for each column
            }

            // Enable horizontal scrolling
            dataGridView1.ScrollBars = ScrollBars.Both;
        }

        private void UpdateStatus(SQLiteConnection conn)
        {
            string updateQuery = @"
                UPDATE DocumentArchive
                SET Status = 'Stored but Destructible'
                WHERE julianday('now') > julianday(ArchiveDate, '+' || ArchivePeriod || ' years')
            ";

            using (SQLiteCommand cmd = new SQLiteCommand(updateQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }


        private void InitializeDataGridView()
        {
            // 假设 dataGridView1 是你的 DataGridView 控件
            dataGridView1.Columns.Clear(); // 清除现有列（如果有的话）

            // 添加列
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                DataPropertyName = "Id",
                Width = 50
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ArchiveDate",
                HeaderText = "Archive Date",
                DataPropertyName = "ArchiveDate",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Archivist",
                HeaderText = "Archivist",
                DataPropertyName = "Archivist",
                Width = 100
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ShelfNumber",
                HeaderText = "Shelf Number",
                DataPropertyName = "ShelfNumber",
                Width = 100
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Department",
                HeaderText = "Department",
                DataPropertyName = "Department",
                Width = 100
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Owner",
                HeaderText = "Owner",
                DataPropertyName = "Owner",
                Width = 100
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "YearOfDoc",
                HeaderText = "Year Of Document",
                DataPropertyName = "YearOfDoc",
                Width = 80
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ContentsOfDoc",
                HeaderText = "Contents Of Document",
                DataPropertyName = "ContentsOfDoc",
                Width = 150
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ArchivePeriod",
                HeaderText = "Archive Period",
                DataPropertyName = "ArchivePeriod",
                Width = 100
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TransferDate",
                HeaderText = "Transfer Date",
                DataPropertyName = "TransferDate",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RetrievalDate",
                HeaderText = "Retrieval Date",
                DataPropertyName = "RetrievalDate",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ReturnDate",
                HeaderText = "Return Date",
                DataPropertyName = "ReturnDate",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DisposalDate",
                HeaderText = "Disposal Date",
                DataPropertyName = "DisposalDate",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" }
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                DataPropertyName = "Status",
                Width = 100
            });

            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Note",
                HeaderText = "Note",
                DataPropertyName = "Note",
                Width = 150
            });

            // 可选：设置自动列宽
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void SetTextBoxesReadOnly(bool readOnly)
        {
            // 根据参数设置 TextBox 的 ReadOnly 属性
            archiveDateTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            archiveDateTextBox.ReadOnly = readOnly;

            archivistComboBox.BackColor = readOnly ? Color.LightGray : Color.White;
            archivistComboBox.Enabled = !readOnly;

            //shelfNumberTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            //shelfNumberTextBox.ReadOnly = readOnly;

            //departmentComboBox.BackColor = readOnly ? Color.LightGray : Color.White;
            //departmentComboBox.Enabled = !readOnly;

            ownerTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            ownerTextBox.ReadOnly = readOnly;

            //yearOfDocTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            //yearOfDocTextBox.ReadOnly = readOnly;

            //contentsOfDocTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            //contentsOfDocTextBox.ReadOnly = readOnly;

            archivePeriodTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            archivePeriodTextBox.ReadOnly = readOnly;

            transferDateTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            transferDateTextBox.ReadOnly = readOnly;

            retrievalDateTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            retrievalDateTextBox.ReadOnly = readOnly;

            returnDateTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            returnDateTextBox.ReadOnly = readOnly;

            disposalDateTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            disposalDateTextBox.ReadOnly = readOnly;

            //statusComboBox.BackColor = readOnly ? Color.LightGray : Color.White;
            //statusComboBox.Enabled = !readOnly;

            //noteTextBox.BackColor = readOnly ? Color.LightGray : Color.White;
            //noteTextBox.ReadOnly = readOnly;

            if (readOnly) 
            { 
                button8.Visible = false;
                //yearOfDocTextBox.Leave -= yearOfDocTextBox_Leave;
            }
            else 
            { 
                button8.Visible = true;
                //yearOfDocTextBox.Leave += yearOfDocTextBox_Leave;
            }
        }

        // 定义一个布尔变量来跟踪 TextBox 的只读状态
        private bool isTextBoxesReadOnly = true;
        private void button1_Click(object sender, EventArgs e)
        {
            // 新增
            // 切换 TextBox 的只读状态
            SetTextBoxesReadOnly(!isTextBoxesReadOnly);

            // 更新 isTextBoxesReadOnly 标志
            isTextBoxesReadOnly = !isTextBoxesReadOnly;

            // 清空现有选择
            dataGridView1.ClearSelection();

            archiveDateTextBox.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            statusComboBox.Text = "Store";
        }


        private void button3_Click(object sender, EventArgs e)
        {
            //刪除
            // 确保至少选中了一行
            if (dataGridView1.SelectedRows.Count > 0)
            {
                // 确认删除操作
                DialogResult result = MessageBox.Show($@"確定要刪除已選取的文件嗎?", "Confirm Deletion", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    // 从数据库中删除选中的记录
                    string connectionString = $"Data Source={Path.Combine(Application.StartupPath, "archive.db")};Version=3;";
                    using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();

                        // 开始事务
                        using (SQLiteTransaction transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                                {
                                    // 跳过新行
                                    if (row.IsNewRow) continue;

                                    // 获取选中行的 Id（假设 Id 列为第一列）
                                    int id = Convert.ToInt32(row.Cells["Id"].Value);

                                    // 删除记录
                                    string deleteQuery = "DELETE FROM DocumentArchive WHERE Id = @Id";
                                    using (SQLiteCommand cmd = new SQLiteCommand(deleteQuery, conn))
                                    {
                                        cmd.Parameters.AddWithValue("@Id", id);
                                        cmd.ExecuteNonQuery();
                                    }

                                    LogAction("Delete", this._username, 
                                        $"{id}," +
                                        $"{row.Cells["ArchiveDate"].Value}," +
                                        $"{row.Cells["Archivist"].Value}," +
                                        $"{row.Cells["ShelfNumber"].Value}," +
                                        $"{row.Cells["Department"].Value}," +
                                        $"{row.Cells["Owner"].Value}," +
                                        $"{row.Cells["YearOfDoc"].Value}," +
                                        $"{row.Cells["ContentsOfDoc"].Value}," +
                                        $"{row.Cells["ArchivePeriod"].Value}," +
                                        $"{row.Cells["TransferDate"].Value}," +
                                        $"{row.Cells["RetrievalDate"].Value}," +
                                        $"{row.Cells["ReturnDate"].Value}," +
                                        $"{row.Cells["DisposalDate"].Value}," +
                                        $"{row.Cells["Status"].Value}," +
                                        $"{row.Cells["Note"].Value}");

                                }

                                // 提交事务
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                // 回滚事务并显示错误消息
                                transaction.Rollback();
                                MessageBox.Show("An error occurred while deleting records: " + ex.Message);
                            }
                        }

                        conn.Close();
                    }

                    // 重新加载 DataGridView 的数据
                    LoadData();

                    MessageBox.Show("刪除成功");
                    
                }
            }
            else
            {
                MessageBox.Show("Please select records to delete.");
            }
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            // 确保选中了一行
            if (dataGridView1.SelectedRows.Count > 0)
            {
                // 获取选中的行
                DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

                // 根据列名或索引从选中行中获取数据，并填充到 TextBox 控件中
                archiveDateTextBox.Text = selectedRow.Cells["ArchiveDate"].Value?.ToString();
                archivistComboBox.Text = selectedRow.Cells["Archivist"].Value?.ToString();
                shelfNumberTextBox.Text = selectedRow.Cells["ShelfNumber"].Value?.ToString();
                departmentComboBox.Text = selectedRow.Cells["Department"].Value?.ToString();
                ownerTextBox.Text = selectedRow.Cells["Owner"].Value?.ToString();
                yearOfDocComboBox.Text = selectedRow.Cells["YearOfDoc"].Value?.ToString();
                contentOfDoccomboBox.Text = selectedRow.Cells["ContentsOfDoc"].Value?.ToString();
                archivePeriodTextBox.Text = selectedRow.Cells["ArchivePeriod"].Value?.ToString();
                transferDateTextBox.Text = selectedRow.Cells["TransferDate"].Value?.ToString();
                retrievalDateTextBox.Text = selectedRow.Cells["RetrievalDate"].Value?.ToString();
                returnDateTextBox.Text = selectedRow.Cells["ReturnDate"].Value?.ToString();
                disposalDateTextBox.Text = selectedRow.Cells["DisposalDate"].Value?.ToString();
                statusComboBox.Text = selectedRow.Cells["Status"].Value?.ToString();
                noteTextBox.Text = selectedRow.Cells["Note"].Value?.ToString();
            }
            else
            {
                // 如果没有选中任何行，清空所有 TextBox 控件
                //ClearTextBoxes();
            }
        }

        private void ClearTextBoxes()
        {
            archiveDateTextBox.Text = string.Empty;
            //archivistComboBox.Text = string.Empty;
            //shelfNumberTextBox.Text = string.Empty;
            //departmentComboBox.Text = string.Empty;
            //ownerTextBox.Text = string.Empty;
            yearOfDocComboBox.Text = string.Empty;
            contentOfDoccomboBox.Text = string.Empty;
            archivePeriodTextBox.Text = string.Empty;
            transferDateTextBox.Text = string.Empty;
            retrievalDateTextBox.Text = string.Empty;
            returnDateTextBox.Text = string.Empty;
            disposalDateTextBox.Text = string.Empty;
            //statusComboBox.Text = string.Empty;
            noteTextBox.Text = string.Empty;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //修改

            if (dataGridView1.SelectedRows.Count <= 0)
            {
                MessageBox.Show("請先選擇要修改的資料");
                return;
            }

            // 更新 isTextBoxesReadOnly 标志
            isTextBoxesReadOnly = !isTextBoxesReadOnly;
            //修改
            SetTextBoxesReadOnly(isTextBoxesReadOnly);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // 查詢
            // 获取用户输入的字段值
            //string archiveDate = archiveDateTextBox.Text;
            //string archivist = archivistComboBox.Text;
            string shelfNumber = shelfNumberTextBox.Text;
            string department = departmentComboBox.Text;
            //string owner = ownerTextBox.Text;
            string yearOfDoc = yearOfDocComboBox.Text;
            string contentsOfDoc = contentOfDoccomboBox.Text;
            //string archivePeriod = archivePeriodTextBox.Text;
            //string transferDate = transferDateTextBox.Text;
            //string retrievalDate = retrievalDateTextBox.Text;
            //string returnDate = returnDateTextBox.Text;
            //string disposalDate = disposalDateTextBox.Text;
            string status = statusComboBox.Text;
            string note = noteTextBox.Text;

            // 构建查询条件
            StringBuilder queryBuilder = new StringBuilder("SELECT * FROM DocumentArchive WHERE 1=1");

            //if (!string.IsNullOrEmpty(archiveDate)) queryBuilder.Append(" AND ArchiveDate = @ArchiveDate");
            //if (!string.IsNullOrEmpty(archivist)) queryBuilder.Append(" AND Archivist = @Archivist");
            if (!string.IsNullOrEmpty(shelfNumber)) queryBuilder.Append(" AND ShelfNumber LIKE  @ShelfNumber");
            if (!string.IsNullOrEmpty(department)) queryBuilder.Append(" AND Department LIKE  @Department");
            //if (!string.IsNullOrEmpty(owner)) queryBuilder.Append(" AND Owner = @Owner");
            if (!string.IsNullOrEmpty(yearOfDoc)) queryBuilder.Append(" AND YearOfDoc LIKE  @YearOfDoc");
            if (!string.IsNullOrEmpty(contentsOfDoc)) queryBuilder.Append(" AND ContentsOfDoc LIKE  @ContentsOfDoc");
            //if (!string.IsNullOrEmpty(archivePeriod)) queryBuilder.Append(" AND ArchivePeriod = @ArchivePeriod");
            //if (!string.IsNullOrEmpty(transferDate)) queryBuilder.Append(" AND TransferDate = @TransferDate");
            //if (!string.IsNullOrEmpty(retrievalDate)) queryBuilder.Append(" AND RetrievalDate = @RetrievalDate");
            //if (!string.IsNullOrEmpty(returnDate)) queryBuilder.Append(" AND ReturnDate = @ReturnDate");
            //if (!string.IsNullOrEmpty(disposalDate)) queryBuilder.Append(" AND DisposalDate = @DisposalDate");
            if (!string.IsNullOrEmpty(status)) queryBuilder.Append(" AND Status LIKE  @Status");
            if (!string.IsNullOrEmpty(note)) queryBuilder.Append(" AND Note LIKE  @Note");

            // 执行查询
            string query = queryBuilder.ToString();
            string connectionString = $"Data Source={Path.Combine(Application.StartupPath, "archive.db")};Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    // 添加参数
                    //if (!string.IsNullOrEmpty(archiveDate)) command.Parameters.AddWithValue("@ArchiveDate", archiveDate);
                    //if (!string.IsNullOrEmpty(archivist)) command.Parameters.AddWithValue("@Archivist", archivist);
                    if (!string.IsNullOrEmpty(shelfNumber)) command.Parameters.AddWithValue("@ShelfNumber", "%" + shelfNumber + "%");
                    if (!string.IsNullOrEmpty(department)) command.Parameters.AddWithValue("@Department", "%" + department + "%");
                    //if (!string.IsNullOrEmpty(owner)) command.Parameters.AddWithValue("@Owner", owner);
                    if (!string.IsNullOrEmpty(yearOfDoc)) command.Parameters.AddWithValue("@YearOfDoc", "%" + yearOfDoc + "%");
                    if (!string.IsNullOrEmpty(contentsOfDoc)) command.Parameters.AddWithValue("@ContentsOfDoc", "%" + contentsOfDoc + "%");
                    //if (!string.IsNullOrEmpty(archivePeriod)) command.Parameters.AddWithValue("@ArchivePeriod", archivePeriod);
                    //if (!string.IsNullOrEmpty(transferDate)) command.Parameters.AddWithValue("@TransferDate", transferDate);
                    //if (!string.IsNullOrEmpty(retrievalDate)) command.Parameters.AddWithValue("@RetrievalDate", retrievalDate);
                    //if (!string.IsNullOrEmpty(returnDate)) command.Parameters.AddWithValue("@ReturnDate", returnDate);
                    //if (!string.IsNullOrEmpty(disposalDate)) command.Parameters.AddWithValue("@DisposalDate", disposalDate);
                    if (!string.IsNullOrEmpty(status)) command.Parameters.AddWithValue("@Status", "%" + status + "%");
                    if (!string.IsNullOrEmpty(note)) command.Parameters.AddWithValue("@Note", "%" + note + "%");

                    connection.Open();

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        // 处理查询结果
                        // 例如，将结果绑定到 DataGridView
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        dataGridView1.DataSource = dt; // 假设你的 DataGridView 名为 resultsDataGridView
                    }
                }
            }
        }


        // 维护一个布尔变量来跟踪当前的选择状态
        private bool isAllSelected = false;
        private void button5_Click(object sender, EventArgs e)
        {
            //全選
            // 根据当前选择状态切换全选/取消全选
            if (isAllSelected)
            {
                // 取消全选
                dataGridView1.ClearSelection();
            }
            else
            {
                // 全选
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    // 确保只选择可见的行
                    if (row.Visible)
                    {
                        row.Selected = true;
                    }
                }
            }

            // 更新选择状态标志
            isAllSelected = !isAllSelected;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //資料匯出
            if (dataGridView1.SelectedRows.Count <= 0) {
                MessageBox.Show("請選取文件資料");
                return;
            }
            // 创建一个 SaveFileDialog 让用户选择文件保存位置
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                saveFileDialog.Title = "Save CSV File";
                saveFileDialog.FileName = "export.csv"; // 默认文件名

                // 显示保存文件对话框
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 获取选定的文件路径
                    string filePath = saveFileDialog.FileName;

                    // 导出选中的数据到 CSV 文件
                    ExportToCsv(filePath);
                    // 显示导出完成通知
                    MessageBox.Show("資料匯出成功!", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExportToCsv(string filePath)
        {
            // 使用 StringBuilder 来构建 CSV 内容
            StringBuilder csvContent = new StringBuilder();

            // 写入列头
            string[] columnHeaders = dataGridView1.Columns.Cast<DataGridViewColumn>()
                                          .Select(column => WrapWithQuotes(column.HeaderText))
                                          .ToArray();
            csvContent.AppendLine(string.Join(",", columnHeaders));

            // 写入选中的行数据
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                string[] cellValues = row.Cells.Cast<DataGridViewCell>()
                                        .Select(cell => WrapWithQuotes(cell.Value?.ToString() ?? string.Empty))
                                        .ToArray();
                csvContent.AppendLine(string.Join(",", cellValues));
            }

            // 将内容写入 CSV 文件
            File.WriteAllText(filePath, csvContent.ToString(), Encoding.Default);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //資料匯入
            // 创建一个 OpenFileDialog 让用户选择 CSV 文件
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                openFileDialog.Title = "Open CSV File";

                // 显示打开文件对话框
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 获取选定的文件路径
                    string filePath = openFileDialog.FileName;

                    // 从 CSV 文件中导入数据
                    ImportFromCsv(filePath);

                    // 显示导入完成通知
                    MessageBox.Show("資料匯入成功!", "Import Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
            }
        }

        public string ConvertToUtf8(string input)
        {
            // Convert string to UTF-8 byte array
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(input);

            // Convert UTF-8 byte array back to string
            string utf8String = Encoding.UTF8.GetString(utf8Bytes);

            return utf8String;
        }

        private void ImportFromCsv(string filePath)
        {
            int totalRecords = 0;
            int newRecords = 0;
            int updatedRecords = 0;
            int skippedRecords = 0;

            try
            {
                Encoding encoding = DetectFileEncoding(filePath);
                // 读取 CSV 文件的内容
                using (var reader = new StreamReader(filePath, Encoding.Default))
                {
                    // 读取列头
                    var columnHeaders = reader.ReadLine()?.Split(',');

                    // 检查列头
                    if (columnHeaders == null || columnHeaders.Length == 0)
                    {
                        MessageBox.Show("CSV file is empty or not in the expected format.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 数据库连接字符串
                    string databasePath = Path.Combine(Application.StartupPath, "archive.db");
                    string connectionString = $"Data Source={databasePath};Version=3;";

                    using (var conn = new SQLiteConnection(connectionString))
                    {
                        conn.Open();

                        // 处理 CSV 中的每一行
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            totalRecords++;

                            // Skip empty lines or lines that contain only commas
                            if (string.IsNullOrWhiteSpace(line) || IsLineOnlyCommas(line))
                            {
                                continue;
                            }

                            var values = ParseCsvLine(line);

                            // 确保列数匹配
                            if (values.Length != columnHeaders.Length)
                            {
                                MessageBox.Show($@"{totalRecords},
{line} ,
Data row does not match the expected column count.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                skippedRecords++;
                                continue;
                            }

                            // 获取关键字段的值
                            string archiveDate = values[Array.IndexOf(columnHeaders, "Archive Date")];
                            // 尝试解析日期字符串为 DateTime 对象
                            if (DateTime.TryParseExact(archiveDate, new[] {
                        "yyyy/M/d", "yyyy/MM/dd",
                        "yyyy/M/d HH", "yyyy/MM/dd HH",
                        "yyyy/M/d HH:mm", "yyyy/MM/dd HH:mm",
                        "yyyy/M/d HH:mm:ss", "yyyy/MM/dd HH:mm:ss"
                    }, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                            {
                                // 重新格式化 統一存yyyy/MM/dd HH:mm:ss
                                archiveDate = parsedDate.ToString("yyyy/MM/dd HH:mm:ss");
                            }
                            string shelfNumber = values[Array.IndexOf(columnHeaders, "Shelf Number")];
                            string department = values[Array.IndexOf(columnHeaders, "Department")];
                            string owner = values[Array.IndexOf(columnHeaders, "Owner")];
                            string yearOfDoc = values[Array.IndexOf(columnHeaders, "Year Of Document")];
                            string contentsOfDoc = values[Array.IndexOf(columnHeaders, "Contents Of Document")];

                            // 检查是否存在重复记录
                            string checkQuery = @"
                        SELECT COUNT(*) FROM DocumentArchive
                        WHERE ShelfNumber = @ShelfNumber
                        AND Department = @Department
                        AND Owner = @Owner
                        AND YearOfDoc = @YearOfDoc
                        AND ContentsOfDoc = @ContentsOfDoc";

                            using (var checkCmd = new SQLiteCommand(checkQuery, conn))
                            {
                                checkCmd.Parameters.AddWithValue("@ShelfNumber", shelfNumber);
                                //checkCmd.Parameters.AddWithValue("@ArchiveDate", archiveDate);
                                checkCmd.Parameters.AddWithValue("@Department", department);
                                checkCmd.Parameters.AddWithValue("@Owner", owner);
                                checkCmd.Parameters.AddWithValue("@YearOfDoc", yearOfDoc);
                                checkCmd.Parameters.AddWithValue("@ContentsOfDoc", contentsOfDoc);

                                long count = (long)checkCmd.ExecuteScalar();
                                if (count > 0)
                                {
                                    // 记录已存在，显示通知并询问是否覆盖
                                    DialogResult result = MessageBox.Show(
                                        $@" 文件 ArchiveDate: {archiveDate}, 
ShelfNumber: {shelfNumber}, 
Department: {department}, 
Owner: {owner}, 
YearOfDoc: {yearOfDoc}, 
and ContentsOfDoc: {contentsOfDoc} 
已經存在. 是否覆蓋? (是:覆蓋; 否:略過)",
                                        "Duplicate Record",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Warning
                                    );

                                    if (result == DialogResult.No)
                                    {
                                        skippedRecords++;
                                        continue; // 用户选择不覆盖，略过该记录
                                    }

                                    // 如果用户选择覆盖，执行更新操作
                                    // 如果用戶選擇覆蓋，不需要更新archive date
                                    string updateQuery = @"
                                        UPDATE DocumentArchive SET
                                            Archivist = @Archivist,
                                            Department = @Department,
                                            Owner = @Owner,
                                            YearOfDoc = @YearOfDoc,
                                            ContentsOfDoc = @ContentsOfDoc,
                                            ArchivePeriod = @ArchivePeriod,
                                            TransferDate = @TransferDate,
                                            RetrievalDate = @RetrievalDate,
                                            ReturnDate = @ReturnDate,
                                            DisposalDate = @DisposalDate,
                                            Status = @Status,
                                            Note = @Note,
                                            ModifyDate = @ModifyDate
                                        WHERE ShelfNumber = @ShelfNumber
                                        AND Department = @Department
                                        AND Owner = @Owner
                                        AND YearOfDoc = @YearOfDoc
                                        AND ContentsOfDoc = @ContentsOfDoc";

                                    using (var updateCmd = new SQLiteCommand(updateQuery, conn))
                                    {
                                        //updateCmd.Parameters.AddWithValue("@ArchiveDate", archiveDate);
                                        updateCmd.Parameters.AddWithValue("@Archivist", values[Array.IndexOf(columnHeaders, "Archivist")]);
                                        updateCmd.Parameters.AddWithValue("@ShelfNumber", shelfNumber);
                                        updateCmd.Parameters.AddWithValue("@Department", department);
                                        updateCmd.Parameters.AddWithValue("@Owner", owner);
                                        updateCmd.Parameters.AddWithValue("@YearOfDoc", yearOfDoc);
                                        updateCmd.Parameters.AddWithValue("@ContentsOfDoc", contentsOfDoc);
                                        updateCmd.Parameters.AddWithValue("@ArchivePeriod", values[Array.IndexOf(columnHeaders, "Archive Period")]);
                                        updateCmd.Parameters.AddWithValue("@TransferDate", values[Array.IndexOf(columnHeaders, "Transfer Date")]);
                                        updateCmd.Parameters.AddWithValue("@RetrievalDate", values[Array.IndexOf(columnHeaders, "Retrieval Date")]);
                                        updateCmd.Parameters.AddWithValue("@ReturnDate", values[Array.IndexOf(columnHeaders, "Return Date")]);
                                        updateCmd.Parameters.AddWithValue("@DisposalDate", values[Array.IndexOf(columnHeaders, "Disposal Date")]);
                                        updateCmd.Parameters.AddWithValue("@Status", values[Array.IndexOf(columnHeaders, "Status")]);
                                        updateCmd.Parameters.AddWithValue("@Note", values[Array.IndexOf(columnHeaders, "Note")]);
                                        updateCmd.Parameters.AddWithValue("@ModifyDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));

                                        updateCmd.ExecuteNonQuery();
                                        updatedRecords++;
                                    }
                                }
                                else // 新增
                                {
                                    // 插入数据到数据库
                                    string insertQuery = @"
                                INSERT INTO DocumentArchive (
                                    ArchiveDate, Archivist, ShelfNumber, Department, Owner, YearOfDoc, ContentsOfDoc, 
                                    ArchivePeriod, TransferDate, RetrievalDate, ReturnDate, DisposalDate, Status, Note
                                ) VALUES (
                                    @ArchiveDate, @Archivist, @ShelfNumber, @Department, @Owner, @YearOfDoc, @ContentsOfDoc,
                                    @ArchivePeriod, @TransferDate, @RetrievalDate, @ReturnDate, @DisposalDate, @Status, @Note
                                )";

                                    using (var cmd = new SQLiteCommand(insertQuery, conn))
                                    {
                                        // 全新的一筆 data 匯入時, archive date 自動帶入匯入時間
                                        cmd.Parameters.AddWithValue("@ArchiveDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                                        cmd.Parameters.AddWithValue("@Archivist", values[Array.IndexOf(columnHeaders, "Archivist")]);
                                        cmd.Parameters.AddWithValue("@ShelfNumber", shelfNumber);
                                        cmd.Parameters.AddWithValue("@Department", department);
                                        cmd.Parameters.AddWithValue("@Owner", owner);
                                        cmd.Parameters.AddWithValue("@YearOfDoc", yearOfDoc);
                                        cmd.Parameters.AddWithValue("@ContentsOfDoc", contentsOfDoc);
                                        cmd.Parameters.AddWithValue("@ArchivePeriod", values[Array.IndexOf(columnHeaders, "Archive Period")]);
                                        cmd.Parameters.AddWithValue("@TransferDate", values[Array.IndexOf(columnHeaders, "Transfer Date")]);
                                        cmd.Parameters.AddWithValue("@RetrievalDate", values[Array.IndexOf(columnHeaders, "Retrieval Date")]);
                                        cmd.Parameters.AddWithValue("@ReturnDate", values[Array.IndexOf(columnHeaders, "Return Date")]);
                                        cmd.Parameters.AddWithValue("@DisposalDate", values[Array.IndexOf(columnHeaders, "Disposal Date")]);
                                        cmd.Parameters.AddWithValue("@Status", values[Array.IndexOf(columnHeaders, "Status")]);
                                        cmd.Parameters.AddWithValue("@Note", values[Array.IndexOf(columnHeaders, "Note")]);

                                        cmd.ExecuteNonQuery();
                                        newRecords++;
                                    }
                                }
                            }
                        }

                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 显示统计信息
            MessageBox.Show(
                $@"總共: {totalRecords}
新增: {newRecords}
覆蓋: {updatedRecords}
略過: {skippedRecords}",
                "Import Summary",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private string[] ParseCsvLine(string line)
        {
            // Custom logic to handle commas within quotes
            var values = new List<string>();
            bool insideQuotes = false;
            var currentField = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '\"')
                {
                    insideQuotes = !insideQuotes; // Toggle inside/outside of quotes
                }
                else if (c == ',' && !insideQuotes)
                {
                    values.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            values.Add(currentField.ToString()); // Add last field
            return values.ToArray();
        }

        private bool IsLineOnlyCommas(string line)
        {
            // This function checks if the line contains only commas or is empty
            return string.IsNullOrWhiteSpace(line.Replace(",", string.Empty));
        }

        private Encoding DetectFileEncoding(string filePath)
        {
            using (var reader = new StreamReader(filePath, true))
            {
                reader.Peek(); // Force reading the BOM if present
                return reader.CurrentEncoding;
            }
        }

        private void transferDateTextBox_TextChanged(object sender, EventArgs e)
        {
            if (transferDateTextBox.Text.Length > 0) { statusComboBox.SelectedIndex = 1; }
        }
        private void retrievalDateTextBox_TextChanged(object sender, EventArgs e)
        {
            if (retrievalDateTextBox.Text.Length > 0) { statusComboBox.SelectedIndex = 2; }
        }

        private void disposalDateTextBox_TextChanged(object sender, EventArgs e)
        {
            if (disposalDateTextBox.Text.Length > 0) { statusComboBox.SelectedIndex = 3; }
        }

        private void returnDateTextBox_TextChanged(object sender, EventArgs e)
        {
            if (returnDateTextBox.Text.Length > 0 && statusComboBox.Text == "Retrieval")
            {
                statusComboBox.SelectedIndex = 0;
            }
        }

        private void transferDateTextBox_Click(object sender, EventArgs e)
        {
            ShowCalendarForTextBox(transferDateTextBox);
        }

        private void transferDateTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 按下Delete或Backspace键时清空TextBox
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                transferDateTextBox.Clear();
            }
        }

        private void retrievalDateTextBox_Click(object sender, EventArgs e)
        {
            ShowCalendarForTextBox(retrievalDateTextBox);
        }

        private void retrievalDateTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 按下Delete或Backspace键时清空TextBox
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                retrievalDateTextBox.Clear();
            }
        }

        private void returnDateTextBox_Click(object sender, EventArgs e)
        {
            ShowCalendarForTextBox(returnDateTextBox);
        }

        private void returnDateTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 按下Delete或Backspace键时清空TextBox
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                returnDateTextBox.Clear();
            }
        }

        private void disposalDateTextBox_Click(object sender, EventArgs e)
        {
            ShowCalendarForTextBox(disposalDateTextBox);
        }

        private void disposalDateTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 按下Delete或Backspace键时清空TextBox
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                disposalDateTextBox.Clear();
            }
        }

        private void ShowCalendarForTextBox(System.Windows.Forms.TextBox targetTextBox)
        {
            // 显示 MonthCalendar 控件
            MonthCalendar calendar = new MonthCalendar();
            calendar.MaxSelectionCount = 1;

            Form calendarForm = new Form();
            calendarForm.StartPosition = FormStartPosition.Manual;
            calendarForm.Location = new Point(targetTextBox.PointToScreen(Point.Empty).X, targetTextBox.PointToScreen(Point.Empty).Y + targetTextBox.Height);
            calendarForm.FormBorderStyle = FormBorderStyle.None;
            calendarForm.Width = targetTextBox.Width + 10;
            calendarForm.Height = targetTextBox.Width - 45;
            calendarForm.Controls.Add(calendar);
            calendarForm.Show();

            // 处理日期选择事件
            calendar.DateSelected += (s, ev) =>
            {
                targetTextBox.Text = ev.Start.ToString("yyyy/MM/dd");
                calendarForm.Close();
            };

            // 点击窗体其他地方关闭日历
            calendar.LostFocus += (s, ev) => calendarForm.Close();
        }


        

        private void button9_Click(object sender, EventArgs e)
        {
            //帳號管理
            // 打开用户管理窗口
            AccountManagementForm accountManagementForm = new AccountManagementForm();
            accountManagementForm.ShowDialog();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckFileLock();
            //ShowEditorInfo();

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            string editorInfo = GetEditorInfo();
            if (editorInfo != null && editorInfo.Equals(_username)) {
                ReleaseFileLock();
            }
            
        }

        private void CheckFileLock()
        {
            string lockFilePath = Path.Combine(Application.StartupPath, "app.lock");

            if (this._username.ToLower() != "user")
            {
                if (IsFileLocked(lockFilePath))
                {
                    // 设置为只读模式
                    SetReadOnlyMode();
                    string editorInfo = GetEditorInfo();
                    MessageBox.Show($@"The application is currently being edited by another user {editorInfo}. You are in read-only mode.", "Read-Only Mode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // 创建锁定文件并记录用户名
                    using (var stream = new FileStream(lockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            writer.WriteLine(this._username);
                        }
                    }
                }
            }
        }


        private bool IsFileLocked(string lockFilePath)
        {
            try
            {
                using (var stream = new FileStream(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true; // 發現app.lock 代表被別人編輯中
                }
            }
            catch (IOException)
            {
                return false; // 沒有發現app.lock
            }
        }

        private void SetReadOnlyMode()
        {
            //禁止新增 修改 
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button10.Enabled = false;
        }

        private void ReleaseFileLock()
        {
            string lockFilePath = Path.Combine(Application.StartupPath, "app.lock");
            if (File.Exists(lockFilePath))
            {
                File.Delete(lockFilePath);
            }
        }

        private string GetEditorInfo()
        {
            string lockFilePath = Path.Combine(Application.StartupPath, "app.lock");
            if (File.Exists(lockFilePath))
            {
                using (var stream = new StreamReader(lockFilePath))
                {
                    return stream.ReadLine(); // 读取编辑者信息
                }
            }
            return null;
        }

        private void ShowEditorInfo()
        {
            string editorInfo = GetEditorInfo();
            if (!string.IsNullOrEmpty(editorInfo))
            {
                MessageBox.Show($"The application is currently being edited by {editorInfo}.", "Currently Editing", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private void button9_Click_1(object sender, EventArgs e)
        {
            //登出
            // 重新启动应用程序
            Application.Restart();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            //全部匯出
            // 选择文件保存位置
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                saveFileDialog.Title = "Save CSV File";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    try
                    {
                        ExportAllToCsv(filePath);
                        MessageBox.Show("全部匯出成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"全部匯出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

        }

        private void ExportAllToCsv(string filePath)
        {
            // 数据库连接字符串
            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            // 查询所有数据
            string selectQuery = "SELECT * FROM DocumentArchive;";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.Default))
                    {
                        // 写入 CSV 文件头
                        sw.WriteLine("ID,Archive Date,Archivist,Shelf Number,Department,Owner,Year Of Document,Contents Of Document,Archive Period,Transfer Date,Retrieval Date,Return Date,Disposal Date,Status,Note");

                        // 写入数据
                        while (reader.Read())
                        {
                            string line = $"{WrapWithQuotes(reader["Id"].ToString())}," +
                                          $"{WrapWithQuotes(reader["ArchiveDate"].ToString())}," +
                                          $"{WrapWithQuotes(reader["Archivist"].ToString())}," +
                                          $"{WrapWithQuotes(reader["ShelfNumber"].ToString())}," +
                                          $"{WrapWithQuotes(reader["Department"].ToString())}," +
                                          $"{WrapWithQuotes(reader["Owner"].ToString())}," +
                                          $"{WrapWithQuotes(reader["YearOfDoc"].ToString())}," +
                                          $"{WrapWithQuotes(reader["ContentsOfDoc"].ToString())}," +
                                          $"{WrapWithQuotes(reader["ArchivePeriod"].ToString())}," +
                                          $"{WrapWithQuotes(reader["TransferDate"].ToString())}," +
                                          $"{WrapWithQuotes(reader["RetrievalDate"].ToString())}," +
                                          $"{WrapWithQuotes(reader["ReturnDate"].ToString())}," +
                                          $"{WrapWithQuotes(reader["DisposalDate"].ToString())}," +
                                          $"{WrapWithQuotes(reader["Status"].ToString())}," +
                                          $"{WrapWithQuotes(reader["Note"].ToString())}";

                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }

        // 将字段用双引号包裹，防止逗号影响 CSV 结构
        // 处理逗号和双引号的字段包裹
        private string WrapWithQuotes(string field)
        {
            if (field.Contains("\""))
            {
                // 如果字段中有双引号，将其替换为两个双引号
                field = field.Replace("\"", "\"\"");
            }

            // 如果字段中有逗号或者双引号，将其用双引号括起来
            if (field.Contains(",") || field.Contains("\""))
            {
                return $"\"{field}\"";
            }

            return field;
        }


        private void yearOfDocComboBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 只允许控制键（如退格键）
            if (!char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void yearOfDocComboBox_TextChanged(object sender, EventArgs e)
        {
            UpdateStatusBasedOnYearAndPeriod();
        }

        private void archivePeriodTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateStatusBasedOnYearAndPeriod();
        }

        private void archivePeriodTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 只允许输入数字和控制键（如退格键）
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void UpdateStatusBasedOnYearAndPeriod()
        {
            if (int.TryParse(yearOfDocComboBox.Text, out int yearOfDoc) &&
                int.TryParse(archivePeriodTextBox.Text, out int archivePeriod))
            {
                int currentYear = DateTime.Now.Year;
                int calculatedYear = yearOfDoc + archivePeriod;

                if (calculatedYear < currentYear)
                {
                    statusComboBox.SelectedIndex = 4;
                }
                else {
                    statusComboBox.SelectedIndex = 0;
                }
            }
        }

        private void contentOfDoccomboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 自動帶入 Archive period
            string archivePeriodOptions = ConfigurationManager.AppSettings["ArchivePeriodOptions"];
            string[] archivePeriodArray = archivePeriodOptions.Split(',');

            // 获取当前选择的索引
            int selectedIndex = contentOfDoccomboBox.SelectedIndex;

            // 检查索引是否有效并在有效时更新 archivePeriodTextBox
            if (selectedIndex >= 0 && selectedIndex < archivePeriodArray.Length)
            {
                archivePeriodTextBox.Text = archivePeriodArray[selectedIndex];
            }
            else
            {
                archivePeriodTextBox.Clear(); // 清空文本框，防止出现无效输入
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
            //清空所有欄位
            archiveDateTextBox.Text = string.Empty;
            archivistComboBox.Text = string.Empty;
            shelfNumberTextBox.Text = string.Empty;
            departmentComboBox.Text = string.Empty;
            ownerTextBox.Text = string.Empty;
            yearOfDocComboBox.Text = string.Empty;
            contentOfDoccomboBox.Text = string.Empty;
            archivePeriodTextBox.Text = string.Empty;
            transferDateTextBox.Text = string.Empty;
            retrievalDateTextBox.Text = string.Empty;
            returnDateTextBox.Text = string.Empty;
            disposalDateTextBox.Text = string.Empty;
            statusComboBox.Text = string.Empty;
            noteTextBox.Text = string.Empty;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            //文件銷毀清單
            // 数据库连接字符串
            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            // SQL 查询语句
            string queryDisposalList = @"
                SELECT * 
                FROM DocumentArchive 
                WHERE ShelfNumber IN (
                    SELECT ShelfNumber
                    FROM DocumentArchive
                    GROUP BY ShelfNumber
                    HAVING COUNT(*) = SUM(CASE WHEN Status = 'Stored but Destructible' THEN 1 ELSE 0 END)
                )
                ORDER BY ShelfNumber, ArchiveDate";

            // 使用数据库连接
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(queryDisposalList, conn))
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable disposalListTable = new DataTable();
                    adapter.Fill(disposalListTable);

                    // 假设你有一个 DataGridView 用于显示结果
                    dataGridView1.DataSource = disposalListTable;

                    //if (disposalListTable.Rows.Count == 0)
                    //{
                    //    MessageBox.Show("No records found for disposal.", "Query Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //}
                }
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            //全部查詢
            // 数据库连接字符串
            string databasePath = Path.Combine(Application.StartupPath, "archive.db");
            string connectionString = $"Data Source={databasePath};Version=3;";

            // SQL 查询语句
            string queryDisposalList = @"
                SELECT * 
                FROM DocumentArchive                 
                ORDER BY Id DESC";

            // 使用数据库连接
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(queryDisposalList, conn))
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable disposalListTable = new DataTable();
                    adapter.Fill(disposalListTable);

                    // 假设你有一个 DataGridView 用于显示结果
                    dataGridView1.DataSource = disposalListTable;
                }
            }

        }

        private void LogAction(string actionType, string username, string details)
        {
            string logFolderPath = Path.Combine(Application.StartupPath, "log");

            // 确保log文件夹存在
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            // 生成日志文件路径，以当天日期命名
            string logFilePath = Path.Combine(logFolderPath, $"{DateTime.Now:yyyyMMdd}.txt");

            // 构建日志内容
            string logContent = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss} [{actionType}] {username}: {details}\n";

            // 追加写入日志文件
            File.AppendAllText(logFilePath, logContent);
        }

        private void statusComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
