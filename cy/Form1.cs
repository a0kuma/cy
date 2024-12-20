﻿using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace cy
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void loadCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 打開文件對話框以選擇 CSV 文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                DataTable dataTable = new DataTable();

                // 讀取 CSV 文件
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string[] headers = sr.ReadLine().Split(',');
                    foreach (string header in headers)
                    {
                        dataTable.Columns.Add(header);
                    }
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');
                        DataRow dr = dataTable.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dr[i] = rows[i];
                        }
                        dataTable.Rows.Add(dr);
                        mainTableHeight.Text = (Int64.Parse(mainTableHeight.Text) + 1).ToString();
                    }
                }

                // 將 DataTable 綁定到 DataGridView
                dataGridView1.DataSource = dataTable;
                ce();
            }
        }

        private void colNameExToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ce();
        }
        private double qq(DataGridViewRow cc, int ci)
        {
            double tmp = -1.0;
            try
            {
                string cellValue = cc.Cells[ci].Value.ToString();
                if (cellValue == "TRUE" || cellValue == "True" || cellValue == "true")
                {
                    tmp = toolStripMenuItem3.Checked ? 1.0 : 1.0;
                }
                else if (cellValue == "FALSE" || cellValue == "False" || cellValue == "false")
                {
                    tmp = toolStripMenuItem3.Checked ? -1.0 : 0.0;
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(cellValue, @"^\d{4}-\d{2}-\d{2}$"))
                {
                    DateTime dateTime = DateTime.ParseExact(cellValue, "yyyy-MM-dd", null);
                    tmp = (dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
                }
                else
                {
                    tmp = Convert.ToDouble(cellValue);
                }
            }
            catch (Exception ex)
            {
                //LogError(ex, cc.Cells[ci].Value.ToString());
                throw;
            }
            return tmp;
        }

        private void ce()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Name");
            dataTable.Columns.Add("DataType");
            dataTable.Columns.Add("Min");
            dataTable.Columns.Add("Max");
            dataTable.Columns.Add("Sample Mean");
            dataTable.Columns.Add("Median");
            dataTable.Columns.Add("Mode");
            dataTable.Columns.Add("SD");
            dataTable.Columns.Add("empty count");
            dataTable.Columns.Add("empty %");

            treeView1.Nodes.Clear(); // Clear existing nodes

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                DataRow row = dataTable.NewRow();
                row["Name"] = column.Name;
                row["DataType"] = "String";

                row["empty count"] = dataGridView1.Rows
                        .Cast<DataGridViewRow>()
                        .Where(r => r.Cells[column.Index].Value != null && String.IsNullOrEmpty(r.Cells[column.Index].Value.ToString()) == true).Count();
                row["empty %"] = 100.0 * double.Parse(row["empty count"].ToString()) / double.Parse(mainTableHeight.Text);

                bool okdo = true;
                List<double> columnData = new List<double>();
                try
                {
                    columnData = dataGridView1.Rows
                        .Cast<DataGridViewRow>()
                        .Where(r => r.Cells[column.Index].Value != null && String.IsNullOrEmpty(r.Cells[column.Index].Value.ToString()) == false)
                        .Select(r => qq(r, column.Index))
                        .ToList();
                    row["DataType"] = "Number";
                }
                catch (Exception ex)
                {
                    okdo = false;
                }

                if (okdo && columnData.Any())
                {
                    row["Min"] = columnData.Min();
                    row["Max"] = columnData.Max();
                    row["Sample Mean"] = columnData.Average();
                    row["Median"] = GetMedian(columnData);
                    row["Mode"] = GetMode(columnData);
                    row["SD"] = GetStandardDeviation(columnData);
                }
                else
                {
                    row["Min"] = -1.0;
                    row["Max"] = -1.0;
                    row["Sample Mean"] = -1.0;
                    row["Median"] = -1.0;
                    row["Mode"] = -1.0;
                    row["SD"] = -1.0;

                    // Add node to treeView1
                    TreeNode columnNode = new TreeNode(column.Name);
                    HashSet<string> uniqueStrings = new HashSet<string>();
                    foreach (DataGridViewRow dataRow in dataGridView1.Rows)
                    {
                        if (dataRow.Cells[column.Index].Value != null && String.IsNullOrEmpty(dataRow.Cells[column.Index].Value.ToString()) == false)
                        {
                            /*if (columnNode.Nodes.Find(dataRow.Cells[column.Index].Value.ToString(), false).Length > 0)
                            {
                                //do nothing
                            }
                            else
                            {
                                columnNode.Nodes.Add(dataRow.Cells[column.Index].Value.ToString());
                            }*/
                            uniqueStrings.Add(dataRow.Cells[column.Index].Value.ToString());
                        }
                    }
                    foreach (string uniqueString in uniqueStrings)
                    {
                        columnNode.Nodes.Add(uniqueString);
                    }
                    treeView1.Nodes.Add(columnNode);
                }

                dataTable.Rows.Add(row);


            }

            dataGridView2.DataSource = dataTable;
        }

        private void LogError(Exception ex, string val = "none")
        {
            string logFilePath = "error_log.txt";
            using (StreamWriter sw = new StreamWriter(logFilePath, true))
            {
                sw.WriteLine($"{DateTime.Now}: {ex.Message}");
                sw.WriteLine(ex.StackTrace);
                sw.WriteLine(val);
            }
        }

        private double GetMedian(List<double> values)
        {
            values.Sort();
            int count = values.Count;
            if (count % 2 == 0)
            {
                return (values[count / 2 - 1] + values[count / 2]) / 2.0;
            }
            else
            {
                return values[count / 2];
            }
        }

        private double GetMode(List<double> values)
        {
            return values.GroupBy(v => v)
                         .OrderByDescending(g => g.Count())
                         .First()
                         .Key;
        }

        private double GetStandardDeviation(List<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }

        private void rowTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = dataGridView1.Rows[0].Cells[3].Value.ToString();
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }

        private void toDouTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = Convert.ToDouble("-0.1545485148514851485148514").ToString();
        }

        private void ConvertDatesToUnixEpoch()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null && !string.IsNullOrEmpty(cell.Value.ToString()))
                    {
                        double dumb;
                        string cellValue = cell.Value.ToString();
                        if (System.Text.RegularExpressions.Regex.IsMatch(cellValue, @"^\d{4}-\d{2}-\d{2}$"))
                        {
                            DateTime dateTime = DateTime.ParseExact(cellValue, "yyyy-MM-dd", null);
                            double unixEpoch = (dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
                            cell.Value = unixEpoch.ToString();
                        }
                        else
                        if (cellValue == "TRUE" || cellValue == "True" || cellValue == "true")
                        {
                            cell.Value = (toolStripMenuItem3.Checked ? 1.0 : 1.0).ToString();
                        }
                        else if (cellValue == "FALSE" || cellValue == "False" || cellValue == "false")
                        {
                            cell.Value = (toolStripMenuItem3.Checked ? -1.0 : 0.0).ToString();
                        }
                        else if (IsValueInTreeView(cell.OwningColumn.Name) && (!double.TryParse(cellValue, out dumb)))
                        {//cellValue)) {
                            cell.Value = t2v(cellValue, cell.OwningColumn.Name).ToString();
                        }
                    }
                }
            }
        }
        private bool IsValueInTreeView(string value)
        {
            //foreach (TreeNode node in treeView1.Nodes)
            //{
            if (treeView1.Nodes.Cast<TreeNode>().Any(n => n.Text == value))
            {
                return true;
            }
            //}
            return false;
        }
        private int t2v(string value, string coln)
        {
            foreach (TreeNode node in treeView1.Nodes)
            {
                if (node.Text == coln)
                {
                    int index = 0;
                    foreach (TreeNode nn in node.Nodes)
                    {
                        if (nn.Text == value)
                        {
                            return index;
                        }
                        index++;
                    }
                }
            }
            return -1; // 如果未找到，返回 -1
        }
        private void doall2numToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConvertDatesToUnixEpoch();
        }

        private void huuugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = IsValueInTreeView("PDF").ToString();
        }

        private void stringToNumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConvertDatesToUnixEpoch();
        }

        private void cutColumnToDataGridView3(string columnName)
        {
            if (dataGridView1.Columns.Contains(columnName))
            {
                // Create a new DataTable for dataGridView3 if it doesn't exist
                if (dataGridView3.DataSource == null)
                {
                    dataGridView3.DataSource = new DataTable();
                }

                DataTable dataTable3 = (DataTable)dataGridView3.DataSource;

                // Add the column to dataGridView3
                if (!dataTable3.Columns.Contains(columnName))
                {
                    dataTable3.Columns.Add(columnName);
                }

                // Copy data from dataGridView2 to dataGridView3
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        DataRow newRow;
                        if (dataTable3.Rows.Count <= row.Index)
                        {
                            newRow = dataTable3.NewRow();
                            dataTable3.Rows.Add(newRow);
                        }
                        else
                        {
                            newRow = dataTable3.Rows[row.Index];
                        }
                        newRow[columnName] = row.Cells[columnName].Value;
                    }
                }

                // Remove the column from dataGridView2
                dataGridView1.Columns.Remove(columnName);
            }
            else
            {
                toolStripStatusLabel1.Text = "Column not found";
            }
        }

        private void selYToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count > 0)
            {
                string current = dataGridView2.SelectedRows[0].Cells["Name"].Value.ToString();
                toolStripStatusLabel1.Text = current;
                cutColumnToDataGridView3(current);
            }
        }

        private void appendValCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 打開文件對話框以選擇 CSV 文件
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                DataTable dataTable = (DataTable)dataGridView1.DataSource;

                // 讀取 CSV 文件
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string[] csvHeaders = sr.ReadLine().Split(',');
                    List<string> commonHeaders = dataTable.Columns.Cast<DataColumn>()
                        .Select(c => c.ColumnName)
                        .Intersect(csvHeaders)
                        .ToList();

                    // Remove columns from dataGridView1 that are not in the CSV headers
                    foreach (DataGridViewColumn column in dataGridView1.Columns.Cast<DataGridViewColumn>().ToList())
                    {
                        if (!commonHeaders.Contains(column.Name))
                        {
                            dataGridView1.Columns.Remove(column.Name);
                        }
                    }

                    // Append data from CSV to dataGridView1
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');
                        DataRow dr = dataTable.NewRow();
                        foreach (string header in commonHeaders)
                        {
                            int csvIndex = Array.IndexOf(csvHeaders, header);
                            dr[header] = rows[csvIndex];
                        }
                        dataTable.Rows.Add(dr);
                    }
                }

                // 更新 DataGridView
                dataGridView1.DataSource = dataTable;
            }
        }

        private void note1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Only headers that exist in both the CSV and the table will be retained.", "note1", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private String FindRowByColumnNameType(string columnName, string value)
        {
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.Cells[columnName].Value != null && row.Cells[columnName].Value.ToString() == value)
                {
                    return row.Cells["DataType"].Value.ToString();
                }
            }
            throw new Exception("Row not found");
        }
        private void fillEmptyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value != null && String.IsNullOrEmpty(cell.Value.ToString()))
                    {
                        /*string currentName = cell.OwningColumn.Name;
                        if (FindRowByColumnNameType("Name", currentName) == "Number")
                        {

                        }
                        else { 
                        
                        }*/

                        foreach (DataGridViewRow row2 in dataGridView2.Rows)
                        {
                            if (row2.Cells["Name"].Value != null && row2.Cells["Name"].Value.ToString() == cell.OwningColumn.Name)
                            {
                                cell.Value = row2.Cells["Sample Mean"].Value;
                            }
                        }
                    }
                }
            }
        }

        private void statForStringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.Cells["DataType"].Value != null && row.Cells["DataType"].Value.ToString() == "String")
                {
                    string columnName = row.Cells["Name"].Value.ToString();
                    List<double> columnData = new List<double>();

                    foreach (DataGridViewRow dataRow in dataGridView1.Rows)
                    {
                        if (dataRow.Cells[columnName].Value != null && !String.IsNullOrEmpty(dataRow.Cells[columnName].Value.ToString()))
                        {
                            try
                            {
                                double value = qq(dataRow, dataRow.Cells[columnName].ColumnIndex);
                                columnData.Add(value);
                            }
                            catch (Exception ex)
                            {
                                // 忽略無法轉換的值
                                throw;
                            }
                        }
                    }

                    if (columnData.Any())
                    {
                        row.Cells["Min"].Value = columnData.Min();
                        row.Cells["Max"].Value = columnData.Max();
                        row.Cells["Sample Mean"].Value = columnData.Average();
                        row.Cells["Median"].Value = GetMedian(columnData);
                        row.Cells["Mode"].Value = GetMode(columnData);
                        row.Cells["SD"].Value = GetStandardDeviation(columnData);
                    }
                    else
                    {
                        row.Cells["Min"].Value = -1.0;
                        row.Cells["Max"].Value = -1.0;
                        row.Cells["Sample Mean"].Value = -1.0;
                        row.Cells["Median"].Value = -1.0;
                        row.Cells["Mode"].Value = -1.0;
                        row.Cells["SD"].Value = -1.0;
                    }
                }
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            toolStripMenuItem2.Checked = !toolStripMenuItem2.Checked;
            toolStripMenuItem3.Checked = !toolStripMenuItem3.Checked;
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            toolStripMenuItem2.Checked = !toolStripMenuItem2.Checked;
            toolStripMenuItem3.Checked = !toolStripMenuItem3.Checked;
        }

        private void zScoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            zScoreToolStripMenuItem.Checked = !zScoreToolStripMenuItem.Checked;
            minMaxMapToolStripMenuItem.Checked = !minMaxMapToolStripMenuItem.Checked;
        }

        private void minMaxMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            zScoreToolStripMenuItem.Checked = !zScoreToolStripMenuItem.Checked;
            minMaxMapToolStripMenuItem.Checked = !minMaxMapToolStripMenuItem.Checked;
        }

        private void doARowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count > 0)
            {
                string current = dataGridView2.SelectedRows[0].Cells["Name"].Value.ToString();
                bool columnExists = dataGridView1.Columns.Cast<DataGridViewColumn>().Any(col => col.Name == current);
                if (!columnExists)
                {
                    MessageBox.Show($"Column '{current}' does not exist in dataGridView1.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (!row.IsNewRow && row.Cells[current] != null)
                    {
                        if (row.Cells[current].Value != null && !String.IsNullOrEmpty(row.Cells[current].Value.ToString()))
                        {
                            double value = double.Parse(row.Cells[current].Value.ToString());
                            double min = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Min"].Value.ToString());
                            double max = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Max"].Value.ToString());
                            double SampleMean = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Sample Mean"].Value.ToString());
                            double Median = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Median"].Value.ToString());
                            double Mode = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Mode"].Value.ToString());
                            double SD = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["SD"].Value.ToString());

                            double newValue = min + (max - min) * Math.Abs(value - min) / (max - min);
                            row.Cells[current].Value = zScoreToolStripMenuItem.Checked ?
                                zScore(value, min, max, SampleMean, Median, Mode, SD)
                                : newValue;
                        }
                    }
                }
            }
        }

        private double zScore(double value,
         double Min,
       double Max,
               double SampleMean,
               double Median,
                double Mode,
                     double SD)
        {
            double z = (value - SampleMean) / SD;
            return ZScoreCDF(z);
        }

        private double ZScoreCDF(double z)
        {
            // 使用標準正態分佈計算 CDF
            MathNet.Numerics.Distributions.Normal normalDist = new Normal(0, 1);
            return normalDist.CumulativeDistribution(z);
        }

        private void ExportDataGridView3ToTxt(string filePath, bool valY = false,int disp=-1,bool s=false)//s stand for show debug
        {
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                //MessageBox.Show($"debug,{valY},{Int32.Parse(mainTableHeight.Text)},{dataGridView1.Rows.Count}");
                for (int i = (valY ? Int32.Parse(mainTableHeight.Text) : 0); i < (valY ? dataGridView1.Rows.Count : Int32.Parse(mainTableHeight.Text)); i++)
                {
                    if (!dataGridView1.Rows[i].IsNewRow) // Check if not empty row
                    {
                        int a = valY ? -1 : Convert.ToInt16(dataGridView3.Rows[i].Cells[0].Value);
                        sw.Write(a.ToString());
                        sw.Write(" ");
                        int v = dataGridView1.Rows[i].Cells.Count;
                        for (int j = 0; j < v; j++)
                        {
                            if (disp == -1)
                            {
                                sw.Write(j.ToString());
                                sw.Write(":");
                                sw.Write(dataGridView1.Rows[i].Cells[j].Value.ToString());
                                if (j < v - 1)
                                {
                                    sw.Write(" ");
                                }
                            }
                            else {
                                if (j == disp)
                                {
                                    //do nothing
                                }
                                else if (j > disp)
                                {
                                    sw.Write((j-1).ToString());
                                    sw.Write(":");
                                    sw.Write(dataGridView1.Rows[i].Cells[j].Value.ToString());
                                    if (j < v - 1)
                                    {
                                        sw.Write(" ");
                                    }
                                }
                                else {
                                    sw.Write(j.ToString());
                                    sw.Write(":");
                                    sw.Write(dataGridView1.Rows[i].Cells[j].Value.ToString());
                                    if (j < v - 1)
                                    {
                                        sw.Write(" ");
                                    }
                                }
                            }
                        }
                        sw.WriteLine();
                    }
                }
            }
            if(s==false)MessageBox.Show("Data exported to output.txt", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (outputLocSetToTmpToolStripMenuItem.Checked)
            {
                string filePath = Path.GetTempPath() + "libsvmHKDKtrain.txt";
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                ExportDataGridView3ToTxt(filePath);
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    ExportDataGridView3ToTxt(filePath);
                }
            }
        }

        private void exportValToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (outputLocSetToTmpToolStripMenuItem.Checked)
            {
                string filePath = Path.GetTempPath() + "libsvmHKDKval.txt";
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                ExportDataGridView3ToTxt(filePath, true);
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    ExportDataGridView3ToTxt(filePath, true);
                }
            }
        }

        private void removeColumnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count > 0)
            {
                string current = dataGridView2.SelectedRows[0].Cells["Name"].Value.ToString();
                bool columnExists = dataGridView1.Columns.Cast<DataGridViewColumn>().Any(col => col.Name == current);
                if (!columnExists)
                {
                    MessageBox.Show($"Column '{current}' does not exist in dataGridView1.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                dataGridView1.Columns.Remove(current);

            }

        }

        private void toolStripStatusLabel3_Click(object sender, EventArgs e)
        {

        }

        private void setMeanRangeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input = Form2.Show("Please enter a number:", "Enter number", "1");
            if (double.TryParse(input, out double result))
            {
                toolStripStatusLabel4.Text = result.ToString();
            }
            else
            {
                MessageBox.Show("Please enter a valid number.", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void doAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row0 in dataGridView2.Rows)
            {
                if ((!row0.IsNewRow) && (Math.Abs(Convert.ToDouble(row0.Cells["Sample Mean"].Value)) > Convert.ToDouble(toolStripStatusLabel4.Text)))
                {
                    string current = row0.Cells["Name"].Value.ToString();
                    bool columnExists = dataGridView1.Columns.Cast<DataGridViewColumn>().Any(col => col.Name == current);
                    if (!columnExists)
                    {
                        toolStripStatusLabel1.Text = $"Column '{current}' does not exist in dataGridView1.";
                        continue;
                    }
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (!row.IsNewRow && row.Cells[current] != null)
                        {
                            if (row.Cells[current].Value != null && !String.IsNullOrEmpty(row.Cells[current].Value.ToString()))
                            {
                                double value = double.Parse(row.Cells[current].Value.ToString());
                                double min = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Min"].Value.ToString());
                                double max = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Max"].Value.ToString());
                                double SampleMean = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Sample Mean"].Value.ToString());
                                double Median = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Median"].Value.ToString());
                                double Mode = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["Mode"].Value.ToString());
                                double SD = double.Parse(dataGridView2.Rows.Cast<DataGridViewRow>().Where(r => r.Cells["Name"].Value.ToString() == current).First().Cells["SD"].Value.ToString());

                                double newValue = min + (max - min) * Math.Abs(value - min) / (max - min);
                                row.Cells[current].Value = zScoreToolStripMenuItem.Checked ?
                                    zScore(value, min, max, SampleMean, Median, Mode, SD)
                                    : newValue;
                            }
                        }
                    }
                }
            }
        }

        private void installToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WriteEmbeddedZipToTemp();
        }

        private void WriteEmbeddedZipToTemp()
        {
            string resourceName = "cy.svm3windows35.zip";
            string tempFilePath = Path.Combine(Path.GetTempPath(), "svm3windows35.zip");

            //if file exists, delete it
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    MessageBox.Show("Embedded ZIP file not found.");
                    return;
                }

                using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
            }

            MessageBox.Show($"The ZIP file has been written to the temporary directory:{tempFilePath}");

            /*remove if htese name exist
            ------       2024/12/8  下午 05:08         291840 libsvm.dll
------       2024/12/8  下午 05:08          14848 libsvmread.mexw64
------       2024/12/8  下午 05:08          13824 libsvmwrite.mexw64
------       2024/12/8  下午 05:08         243712 svm-predict.exe
------       2024/12/8  下午 05:08         191488 svm-scale.exe
------       2024/12/8  下午 05:08         255488 svm-toy.exe
------       2024/12/8  下午 05:08         282624 svm-train.exe
------       2024/12/8  下午 05:08          29184 svmpredict.mexw64
------       2024/12/8  下午 05:08          72704 svmtrain.mexw64*/
            string tempPath = Path.GetTempPath();
            string[] files = { "libsvm.dll", "libsvmread.mexw64", "libsvmwrite.mexw64", "svm-predict.exe", "svm-scale.exe", "svm-toy.exe", "svm-train.exe", "svmpredict.mexw64", "svmtrain.mexw64" };
            foreach (string file in files)
            {
                string filePath = Path.Combine(tempPath, file);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            //unzip the file
            System.IO.Compression.ZipFile.ExtractToDirectory(tempFilePath, Path.GetTempPath());
        }
        private string RunProcess(string exePath, string arguments,bool s=false)
        {
            try
            {
                // 初始化進程
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = false;

                // 啟動進程
                process.Start();

                // 讀取輸出
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (error != null&&s==false) {
                    MessageBox.Show(error, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                process.WaitForExit();

                return output;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error running process");
                return string.Empty;
            }
        }
        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //set path to tmp, run "svm-train.exe libsvmHKDKtrain.txt", stdout save to string, show in messagebox
            string tempPath = Path.GetTempPath();
            string trainFilePath = Path.Combine(tempPath, "libsvmHKDKtrain.txt");
            string valFilePath = Path.Combine(tempPath, "libsvmHKDKval.txt");
            if (!File.Exists(trainFilePath))
            {
                MessageBox.Show("Please load a training file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!File.Exists(valFilePath))
            {
                MessageBox.Show("Please load a val file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string exePath = Path.Combine(tempPath, "svm-train.exe");
            string exePathVal = Path.Combine(tempPath, "svm-predict.exe");
            string modelPath = Path.Combine(tempPath, "libsvmHKDKtrain.model");
            string valFilePathSol = Path.Combine(tempPath, "libsvmHKDKval.txt.predict");
            string ansCsv = Path.Combine(tempPath, "ansCsv.csv");
            if (File.Exists(ansCsv))
            {
                File.Delete(ansCsv);
            }

            if (File.Exists(modelPath))
            {
                File.Delete(modelPath);
            }
            if (File.Exists(valFilePathSol))
            {
                File.Delete(valFilePathSol);
            }

            string output = RunProcess(exePath, $"-g 0.01 {trainFilePath} {modelPath}");//-c -k
            MessageBox.Show($"{output}", "Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
            string outputVal = RunProcess(exePathVal, $"{valFilePath} {modelPath} {valFilePathSol}");
            MessageBox.Show($"{outputVal}", "Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (!File.Exists(modelPath))
            {
                MessageBox.Show("internal error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            FileProcessor.ProcessFile(valFilePathSol, ansCsv);
            //remove trainFilePath valFilePath exePath exePathVal modelPath valFilePathSol

        }

        private void runWithOutOneLoopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows[0].Cells.Count; i++) {
                exportAndRun(i);
            }
        }

        private void exportAndRun(int disp = -1)
        {
            string filePath = Path.GetTempPath() + $"libsvmHKDKtrain{disp}.txt";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            ExportDataGridView3ToTxt(filePath, false, disp,true);

            string filePath2 = Path.GetTempPath() + $"libsvmHKDKval{disp}.txt";
            if (File.Exists(filePath2))
            {
                File.Delete(filePath2);
            }
            ExportDataGridView3ToTxt(filePath2, true, disp, true);

            string tempPath = Path.GetTempPath();
            string trainFilePath = Path.Combine(tempPath, $"libsvmHKDKtrain{disp}.txt");
            string valFilePath = Path.Combine(tempPath, $"libsvmHKDKval{disp}.txt");
            if (!File.Exists(trainFilePath))
            {
                //*MessageBox.Show("Please load a training file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!File.Exists(valFilePath))
            {
                //*MessageBox.Show("Please load a val file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string exePath = Path.Combine(tempPath, "svm-train.exe");
            string exePathVal = Path.Combine(tempPath, "svm-predict.exe");
            string modelPath = Path.Combine(tempPath, $"libsvmHKDKtrain{disp}.model");
            string valFilePathSol = Path.Combine(tempPath, $"libsvmHKDKval{disp}.txt.predict");
            string ansCsv = Path.Combine(tempPath, $"ansCsv{disp}.csv");
            if (File.Exists(ansCsv))
            {
                File.Delete(ansCsv);
            }

            if (File.Exists(modelPath))
            {
                File.Delete(modelPath);
            }
            if (File.Exists(valFilePathSol))
            {
                File.Delete(valFilePathSol);
            }

            string output = RunProcess(exePath, $"-g 0.01 -c 1.022 {trainFilePath} {modelPath}", true);//-c -k
            //*MessageBox.Show($"{output}", "Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
            string outputVal = RunProcess(exePathVal, $"{valFilePath} {modelPath} {valFilePathSol}", true);
            //*MessageBox.Show($"{outputVal}", "Output", MessageBoxButtons.OK, MessageBoxIcon.Information);

            string logFilePath = Path.Combine(tempPath, $"libsvmDisp{disp}.log");
            File.WriteAllText(logFilePath, $"{output}\n{outputVal}");

            if (!File.Exists(modelPath))
            {
                //*MessageBox.Show("internal error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            FileProcessor.ProcessFile(valFilePathSol, ansCsv);
        }

        private void exportAndRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exportAndRun(-1);
        }

        private void runWithRandomDispToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Random random = new Random();
            int r = random.Next(0, dataGridView1.Rows[0].Cells.Count);
            exportAndRun(r);
        }
    }
}
