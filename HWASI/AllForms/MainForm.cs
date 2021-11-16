using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;
using static HWASI.oksmart;

namespace HWASI.AllForms
{
    public partial class MainForm : Form
    {
        private ListViewColumnSorter lvwColumnSorter;
        protected PerformanceCounter cpuCounter;
        protected PerformanceCounter ramCounter;
        SmartProgram smart = new SmartProgram();
        public MainForm()
        {
            InitializeComponent();

            Timer timerlocal = new Timer();
            timerlocal.Interval = 3000;//5 minutes
            timerlocal.Tick += new System.EventHandler(timerlocal_Tick);
            timerlocal.Start();

            timer1.Interval = 500;//5 minutes
            timer1.Tick += new System.EventHandler(timer1_Tick);
            timer1.Start();

            lvwColumnSorter = new ListViewColumnSorter();
            this.listView1.ListViewItemSorter = lvwColumnSorter;

            var os = Environment.OSVersion;

            label2.Text = Convert.ToString("Название: ");
            label3.Text = Convert.ToString("Версия: ");

            var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().OfType<ManagementObject>()
                        select x.GetPropertyValue("Caption")).FirstOrDefault();
            var ver = (from x in new ManagementObjectSearcher("SELECT Version FROM Win32_OperatingSystem").Get().OfType<ManagementObject>()
                       select x.GetPropertyValue("Version")).FirstOrDefault();
            label2.Text = label2.Text + " " + Convert.ToString(name);
            label3.Text = label3.Text + " " + Convert.ToString(ver);



            ManagementObjectSearcher myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController");
            foreach (ManagementObject obj in myVideoObject.Get())
            {
                label12.Text = string.Format("Название: " + obj["Name"]);
                label11.Text = string.Format("VRAM:  " + FormatBytes((long)Convert.ToDouble(obj["AdapterRAM"])));
            }

            ManagementObjectSearcher myProcessorObject = new ManagementObjectSearcher("select * from Win32_Processor");
            foreach (ManagementObject obj in myProcessorObject.Get())
                label9.Text = string.Format("Название:  " + obj["Name"]);

            DriveInfo[] di = DriveInfo.GetDrives();

            foreach (DriveInfo d in di)
                listBox1.Items.Add(d.Name);


            ManagementObjectSearcher ramMonitor = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");

            foreach (ManagementObject obj in ramMonitor.Get())
                label17.Text = string.Format("Частота:  " + obj["Speed"] + " MHz");
          
            double TotalRam = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            label18.Text = string.Format("Объем:  " + FormatBytes((long)Convert.ToDouble(TotalRam)));
            if (tabControl1.SelectedTab.Name.Equals("SMART"))
            {
                outSmart();
            }
        }

        private void outSmart()
        {
            var smartList = smart.SmartFunction();

            if (smartList != null)
            {
                if (toolStripComboBox1.Text == "")
                {
                    int count = 0;
                    foreach (var driveN in smartList)
                    {
                        count += 1;
                        toolStripComboBox1.Items.Add(count.ToString() + ". " +
                            string.Format("DRIVE ({0}): " +
                            driveN.Model + " (S/N: " +
                            driveN.Serial + ") - " +
                            driveN.Type,
                            ((driveN.IsOK) ? "OK" : "BAD")));

                    }

                    toolStripComboBox1.SelectedIndex = 0;
                }

                listView2.Invoke(new Action(() => listView2.BeginUpdate()));
                listView2.Invoke(new Action(() => listView2.Items.Clear()));

                var drive = smartList[toolStripComboBox1.SelectedIndex];

                foreach (var attr in drive.Attributes)
                {
                    if (attr.Value.HasData)
                    {
                        string[] row =
                        {
                            (attr.Value.Attribute).ToString(),
                            (attr.Value.Current).ToString(),
                            (attr.Value.Worst).ToString(),
                            (attr.Value.Threshold).ToString(),
                            (attr.Value.Data).ToString(),
                            ((attr.Value.IsOK) ? "OK" : ""),
                        };

                        ListViewItem item = new ListViewItem(row);
                        listView2.Invoke(new Action(() => listView2.Items.Add(item)));
                    }
                }

                listView2.Invoke(new Action(() => listView2.EndUpdate()));
                listView2.Invoke(new Action(() => listView2.Refresh()));
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        static float GetTotalMemoryInBytes()
        {
            return new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
        }
        private void timerlocal_Tick(object sender, EventArgs e)
        {
            //do whatever you want 
            RefreshMyForm();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            //do whatever you want 
            kabom();
        }
        private void kabom()
        {
            progressBar1.Value = (int)(pcProcessor.NextValue());
            lblProcessor.Text = "Загрузка процессора: " + progressBar1.Value.ToString() + "%";
            double xxx = Math.Round(((double)pcMemoryAvailable.NextValue() / 1024), 2);
            lblMemoryAvailable.Text = (xxx.ToString() + "GB");
            double TotalRam = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            label4.Text = Convert.ToString(Math.Round((((Convert.ToDouble(TotalRam) / 1024) / 1024 / 1024)) - xxx, 2)) + "GB";
        }
        private void RefreshMyForm()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate { RefreshMyForm(); }));
            }
            else
            {
                // remove all current records
                listView1.BeginUpdate();
                listView1.Items.Clear();


                Process[] processes = Process.GetProcesses();
                for (int i = 0; i < processes.Length; i++)
                {
                    ListViewItem lvi = new ListViewItem(processes[i].ProcessName);
                    lvi.SubItems.Add(((processes[i].WorkingSet64 / 1024) / 1024).ToString() + " МБ");

                    listView1.Items.Add(lvi);
                }


                listView1.EndUpdate();
            }
        }
        private float[] GetMemory()
        {
            ObjectQuery sql = new ObjectQuery("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(sql);
            ManagementObjectCollection res = searcher.Get();

            float[] ans = new float[3];

            foreach (ManagementObject item in res)
            {
                ans[0] = (float)(Convert.ToDouble(item["TotalVisibleMemorySize"]) / (1024 * 1024));
            }

            sql = new ObjectQuery("Select FreePhysicalMemory FROM Win32_OperatingSystem");
            searcher = new ManagementObjectSearcher(sql);
            res = searcher.Get();

            foreach (ManagementObject item in res)
            {
                ans[1] = (float)(Convert.ToDouble(item["FreePhysicalMemory"]) / (1024 * 1024));
            }

            ans[2] = ans[0] - ans[1];

            return ans;

        }

        private void listView1_SelectedIndexChanged(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.listView1.Sort();
        }
        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.listView1.Sort();
        }

        private void listBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = "";
                DriveInfo di = new DriveInfo(listBox1.SelectedItem.ToString());
                textBox1.Text = "Свободное пространство: " + FormatBytes((long)Convert.ToDouble(di.AvailableFreeSpace)) + "\r\n"
                    + "Общий размер: " + FormatBytes((long)Convert.ToDouble(di.TotalSize)) + "\r\n"
                    + "Формат устройства: " + di.DriveFormat + "\r\n"
                    + "Тип устройства: " + di.DriveType + "\r\n"
                    + "Готовность: " + di.IsReady + "\r\n"
                    + "Имя " + di.Name
                    + "\r\nКорневой каталог: " + di.RootDirectory +
                    "\r\nМетка тома: " + di.VolumeLabel;
            }
            catch
            {
                textBox1.Text = "Было выбрано некорректное устройство";
            }
        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {
            outSmart();
        }
    }
}
