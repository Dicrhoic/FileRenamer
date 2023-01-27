using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;



namespace FileRenamer
{
    public partial class Form1 : Form
    {
        private string folderName;
        private bool fileOpened = false;
        private List<string> exts = new List<string>();
        private List<string> filters = new List<string>();
        private List<string> fileList = new List<string>();
        private List<string> editList = new List<string>();
        private ToolStripProgressBar toolStripProgressBar;
        private BackgroundWorker backgroundWorker;
        private ToolStripStatusLabel toolStripStatusLabel;
        private Replacer? replacer;
        private string? replacement;
        private string? currentText;
        private int? count;
        private int? itemIndex;
        private ItemComparer comparer;

        public Form1()
        {
            InitializeComponent();
            InitialiseStatusStrip();
            comparer = new ItemComparer();
        }

        private void InitialiseStatusStrip()
        {
            toolStripProgressBar = new ToolStripProgressBar();
            toolStripProgressBar.Enabled = false;
            toolStripStatusLabel = new ToolStripStatusLabel();
            toolStripProgressBar.Dock= DockStyle.Fill;  
            statusStrip1.Items.Add(toolStripProgressBar);
            statusStrip1.Items.Add(toolStripStatusLabel);
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);

        }

        private void SetDirectory(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                folderName = folderBrowserDialog1.SelectedPath;
                if (!fileOpened)
                {
                    // No file is opened, bring up openFileDialog in selected path.
                    openFileDialog1.InitialDirectory = folderName;
                    openFileDialog1.FileName = null;
                    Debug.WriteLine(folderName);
                    CreateFileTypes();  
                    //openMenuItem.PerformClick();
                }
            }
        }

        private List<string> files(string path)
        {
            List<string> files = new List<string>();
            if(path != null)
            {
                var txtFiles = Directory.EnumerateFiles(path);
                foreach (var txtFile in txtFiles)
                {
                    string file = (string)txtFile.Replace(@$"{path}\", "");
                    files.Add(file);
                    
                }
            }
            return files;
        }

        private void CreateFileList()
        {
            tabPage1.Controls.Clear();
            if (folderName != null)
            {   
                int count = fileList.Count;
                var dirs = Directory.GetDirectories(folderName);
                count += dirs.Length;
                ListView list = new ListView();
                list.View = View.Details;
                list.GridLines = true;
                string[] dateCreated = new string[count];
                string[] dateModified = new string[count];
                double[] fileSize = new double[count];
                string[] fileSizeConv = new string[count];
                var txtFiles = Directory.EnumerateFiles(folderName);
                List<string> allFiles = txtFiles.ToList();
                allFiles.AddRange(dirs);
                double totalSize = 0;
                int index = 0;
                foreach (var fileName in allFiles)
                {
                   var fi = new FileInfo(fileName);
                    if(fi.Exists)
                    {
                        dateCreated[index] = fi.CreationTime.ToString();
                        dateModified[index] = fi.LastWriteTime.ToString();
                        fileSize[index] = fi.Length;
                        totalSize += fi.Length;
                    }
                   var di = new DirectoryInfo(fileName);
                    if(di.Exists)
                    {
                        dateCreated[index] = di.CreationTime.ToString();
                        dateModified[index] = di.LastWriteTime.ToString();
                        fileSize[index] = GetDirectorySize(di);
                        totalSize += GetDirectorySize(di);
                    }
                   index++;

                }
                for (int c = 0; c < fileSize.Length; c++) 
                {   
                    double byt = fileSize[c];   
                    long bytSize = (long)byt;
                    double mbCon = Math.Round(ConvertBytesToMegabytes(bytSize));
                    fileSize[c] = mbCon;
                    fileSizeConv[c] = $"{mbCon:n0} KB";
                }
                string[] files = allFiles.ToArray();
                
              
                List<ListViewItem> name = new List<ListViewItem>();
                for (int i = 0; i < index; i++)
                {
                    ListViewItem item = new ListViewItem(files[i], i);
                    item.SubItems.Add(fileSize[i].ToString());
                    item.SubItems.Add(dateCreated[i]);
                    item.SubItems.Add(dateModified[i]);
                    name.Add(item);
                }
            
                
                list.Columns.Add("File Name ", -2, HorizontalAlignment.Left);
                list.Columns.Add("Size (KB)", -2, HorizontalAlignment.Left);
                list.Columns.Add("Date Created", -2, HorizontalAlignment.Left);
                list.Columns.Add("Last Time Modified", -2, HorizontalAlignment.Left);
                list.Items.AddRange(name.ToArray());
                list.Dock= DockStyle.Fill;
                list.ColumnClick += new ColumnClickEventHandler(ColumnClick);
                list.ListViewItemSorter = comparer;
                tabPage1.Controls.Add(list);
                LoadSideInformation();
                double byt1 = totalSize;
                long bytSize1 = (long)byt1;
                double mbCo1 = Math.Round(ConvertBytesToMegabytes(bytSize1)/1024f);
                mbCo1 = Math.Round(mbCo1 / 1024f);
                var directory = Directory.GetDirectoryRoot(folderName);
                var driveInfo = DriveInfo.GetDrives().Where(d=> d.Name.Contains(directory.Replace("\\","").ToString()));
                double byt2 = driveInfo.First().TotalSize;
                long bytSize2 = (long)byt2;

                double rootSize = Math.Round(ConvertBytesToMegabytes(bytSize2) / 1024f);
                string rootName = directory.Replace("\\", "");
                toolStripStatusLabel.Text = $"{rootName} {mbCo1}/{rootSize} GB ";
           
            }
        }


        private static long GetDirectorySize(DirectoryInfo folderPath)
        {
           
            return folderPath.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        private void GetParentDrive()
        {
            if(folderName == null)
            {
                return;
            }
            var directory = Directory.GetDirectoryRoot(folderName);
            Debug.WriteLine(directory);
        }

        private void CreateFileTypes()
        {
            List<string> items;
            List<string> types = new List<string>();
            if (folderName != null)
            {
                items = files(folderName);
                var txtFiles = Directory.EnumerateFiles(folderName);  
                count = txtFiles.Count();
                fileList = txtFiles.ToList();   
                foreach ( var txtFile in txtFiles)
                {
                    string ext;
                    ext = Path.GetExtension(txtFile);   
                    Debug.WriteLine($"{txtFile} Ext: {ext}"); 
                    if(ext != null)
                    {
                        types.Add(ext);
                    }
                }
                exts = types.Distinct().ToList();
                CreateFilterSettings(exts);
                //CreateListPanel(items);
                CreateFileList();
            }
         
           
        }

        static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f);
        }

        private void ColumnClick(object o, ColumnClickEventArgs e)
        {
            ListView list = (ListView)tabPage1.Controls[0];
            Debug.WriteLine($"{e.Column} clicked");
            int columnIndex = e.Column;
            if (e.Column == comparer.SortColumn)
            {
                Debug.WriteLine("New column");
                if (comparer.SortColumn == columnIndex)
                {

                }
                // Reverse the current sort direction for this column.
                if (comparer.Order == SortOrder.Ascending)
                {
                    comparer.Order = SortOrder.Descending;
                }
                else
                {
                    comparer.Order = SortOrder.Ascending;
                }
            }
            else
            {
                Debug.WriteLine("New column");
                comparer.SortColumn = e.Column;
                comparer.Order = SortOrder.Ascending;
            }
            list.Sort();
        }


        private void CreateListPanel(List<string> files)
        {
            Panel panel = new Panel();  
            panel.Dock = DockStyle.Fill;
            RichTextBox richTextBox = new RichTextBox();
            richTextBox.Dock = DockStyle.Fill;
            string layout = $"{files.Count} Files in {folderName}: ";
            int i = 0;
            foreach (string file in files) 
            {
                layout += $"\n{i}. {file}";
                i++;
            }
            richTextBox.Text = layout;
            panel.Controls.Add(richTextBox);
            tabPage1.Controls.Add(panel);   
        }

        private void CreateFilterSettings(List<string> exts)
        {
            FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
            flowLayoutPanel.Name = "FlowPanel";
            flowLayoutPanel.Dock = DockStyle.Top;
            flowLayoutPanel.FlowDirection = FlowDirection.LeftToRight;
            foreach (string type in exts)
            {
                CheckBox cb = new CheckBox();
                string name = type.Replace(".", "");
                cb.Name = $"{name}_cb";
                cb.Checked = false;
                cb.Text = type;
                flowLayoutPanel.Controls.Add(cb);
                cb.Enabled= false;
                cb.CheckedChanged += (s, e) => UpdateFilterSelection(cb, e);
            }
            groupBox2.Controls.Add(flowLayoutPanel);
            foreach (Control control in groupBox2.Controls)
            {
                Debug.WriteLine(control.Name);
            }
        }

        private void AttemptUnlock(object sender, EventArgs e)
        {
            Debug.WriteLine("unlocking");
        }

        private void SetState(object sender, EventArgs e)
        {   
            panel2.Controls.Clear();    
            if(radioButton1.Checked)
            {

            }

            if(radioButton2.Checked)
            {
                Debug.WriteLine("Loading Replacer");
                replacer= new Replacer();
                replacer.Dock= DockStyle.Fill;  
                panel2.Controls.Add(replacer);   
            }
        }

        private void UpdateFilterSelection(object sender, EventArgs e)
        {   
           List<string> selection = new List<string>();    
           FlowLayoutPanel fp = (FlowLayoutPanel)groupBox2.Controls[0];
           foreach (CheckBox checkBox in fp.Controls)
            {
                if (checkBox.Checked)
                {   
                    string name = checkBox.Text.Replace("_cb","");
                    selection.Add($"{name}");
                }
            } 
           foreach (string name in selection) 
            {
                Debug.WriteLine(name);  
            }

        }

        private void GetDataFromForm(object sender, EventArgs e)
        {   
            button1.Enabled = false;    
            
            string str;
            string str1;
            if (radioButton1.Checked)
            {

            }
            if (radioButton2.Checked)
            {   
                if(replacer is null)
                {
                    Debug.WriteLine("Repalcer null");
                }
                if(replacer != null)
                {
                   
                    var target = replacer.Controls[0].Controls.Find("editStr", true);
                    //Debug.WriteLine(target.Length);
                    var goal = replacer.Controls[0].Controls.Find("replaceStr", true);
                    if(target.Length > 0 && goal.Length > 0)
                    {
                        TextBox tb = (TextBox)target[0];
                        str = tb.Text;
                        Debug.Write(str);
                        TextBox tb1 = (TextBox)goal[0];  
                        str1 = tb1.Text; 
                        Debug.Write(str1);
                        if (folderName != null)
                        {
                            RenameFiles(str, str1);
                        }
                    }
                }
               
            }
        }

        private void RenameFiles(string current, string edit)
        {   
            currentText = string.Empty;
            replacement = string.Empty;
            foreach(string it in fileList)
            {
                if(it.Contains(current))
                {
                    editList.Add(it);    
                }
            }
            count = editList.Count; 
            Debug.WriteLine(editList.Count);
            replacement = edit;
            currentText= current;
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
            }
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
        
            backgroundWorker1.ReportProgress(0, "Working...");
            if(count > 50)
            {
                count = 40;
            }
            for (int i = 0; i < count; ++i)
            {
                System.Threading.Thread.Sleep(100);
                backgroundWorker1.ReportProgress((1 * i), $"Renaming:{fileList[i]}...{i}/{count}");
                string curr = editList[i].ToString();
                string edit = curr.Replace(currentText, replacement);
                try
                {
                    File.Move(curr, edit);
                }
                catch (Exception ex)
                { 
                    Debug.WriteLine(ex.Message); 
                }
        
            }

            backgroundWorker1.ReportProgress(100, "Complete!");
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar.Value = e.ProgressPercentage;
            toolStripStatusLabel.Text = e.UserState as String;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            button1.Enabled = true;
            if (e.Error is OverflowException)
            { 
            }
            toolStripProgressBar.Enabled = false;
            CreateFileTypes();
        }

        private void LoadSideInformation()
        {
            var control = tabPage1.Controls[0];
            ListView list = (ListView)control;
            list.ItemMouseHover += DetailsEnhance;
            list.MouseClick += LoadDetailsSide;
           
        }

        private void DetailsEnhance(object? sender, ListViewItemMouseHoverEventArgs e)
        {
            var control = tabPage1.Controls[0];
            ListView list = (ListView)control;
            int index = e.Item.Index;
            itemIndex = index;
            Debug.WriteLine($"{e.Item.Text}");
            string size = e.Item.SubItems[1].Text;
            Debug.WriteLine($"{size}");
          
        }

        private void LoadDetailsSide(object? sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right) 
            {
                var control = tabPage1.Controls[0];
                ListView list = (ListView)control;
                ContextMenuStrip menuStrip = new();
                list.ContextMenuStrip = menuStrip;
                list.ContextMenuStrip.Items.Add("Open");
                list.ContextMenuStrip.Items[0].Click += LoadItem;
                list.ContextMenuStrip.Items.Add("Open Folder");
                list.ContextMenuStrip.Items[1].Click += LoadRoot;
                Debug.Write(e.Button);
            }
        }

        private void LoadRoot(object? sender, EventArgs e)
        {
            string root = folderName;
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(root)
            { UseShellExecute = true };
            p.Start();

        }

        private void LoadItem(object? sender, EventArgs e)
        {
            var control = tabPage1.Controls[0];
            ListView list = (ListView)control;
            string proc = list.Items[0].Text;
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(proc)
            { UseShellExecute = true };
            p.Start(); 
        }

        
    }
}
public class ItemComparer : IComparer
{
    public int Column { get; set; }

    public SortOrder Order { get; set; }
    private CaseInsensitiveComparer ObjectCompare;
    public ItemComparer()
    {
        Order = SortOrder.None;
        ObjectCompare = new CaseInsensitiveComparer();
    }

    public int Compare(object x, object y)
    {
        int result;
        ListViewItem listviewX, listviewY;

        listviewX = (ListViewItem)x;
        listviewY = (ListViewItem)y;
        result = ObjectCompare.Compare(listviewX.SubItems[Column].Text, listviewY.SubItems[Column].Text);
        var xStr = listviewX.SubItems[Column].Text;
        var yStr = listviewY.SubItems[Column].Text;
        double xValue = 0;
        double yValue = 0;
        double.TryParse(xStr,out xValue);
        double.TryParse(yStr,out yValue);
        if (yValue != 0 && xValue != 0)
        {
            Debug.WriteLine($"Number, comparing: {xValue} to {yValue}");
            double answer = xValue - yValue;
            result = (int)Math.Round(answer);
        }

        if (Order == SortOrder.Descending)
        {
            return (-result);
        }
        else if (Order == SortOrder.Ascending)
        {
            return result;
        }
        return 0;
    }
    public int SortColumn
    {
        set
        {
            Column = value;
        }
        get
        {
            return Column;
        }
    }

    public SortOrder OrderCol
    {
        set
        {
            Order = value;
        }
        get
        {
            return Order;
        }
    }
}