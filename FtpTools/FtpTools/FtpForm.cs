using FtpTools.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FtpTools
{
    public partial class FtpForm : Form
    {
        #region Declarations
        private List<String> strlistfile = null;
        private static List<Fileinfo> listfile = null;
        private static string strHostName = string.Empty;
        private static string strUserName = string.Empty;
        private static string strPassword = string.Empty;
        private static string strPort = string.Empty;
        private static string strHostdest = string.Empty;
        private static FtpWebResponse ftpResponse = null;
        private static string SelectedFile = string.Empty;
        private int currentPosition = 0;
        private long transferRate = 0;
        private long totalTransfered = 0;
        private long size = 0;
        private delegate void updatebar();
        #endregion

        #region Contructor
        public FtpForm()
        {
            InitializeComponent();
            getConnDetails();
        }
        #endregion

        #region Event
        private void FtpForm_Load(object sender, EventArgs e)
        {
            //fillGrid();
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                fillGrid();
                saveConnDetails();
                SelectedFile = string.Empty;
                btnCancel.Text = "Cancel";
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                strlistfile = new List<String>();
                listfile = new List<Fileinfo>();
                OpenFileDialog ofdFile = new OpenFileDialog();
                ofdFile.Multiselect = true;
                ofdFile.Filter = "All files (*.*)|*.*";
                ofdFile.Title = "Select File.";
                if (ofdFile.ShowDialog() == DialogResult.OK)
                {
                    var files = ofdFile.FileNames;
                    int totalfile = files.Length; int i = 0;
                    btnUpload.Text = "Processing: (" + totalfile + ") " + ((totalfile <= 1) ? "File" : "Files").ToString();
                    if (totalfile > 0)
                    {
                        foreach (var file in files)
                        {
                            i++;
                            txtFile.Text = file;
                            Fileinfo objfile = new Fileinfo()
                            {
                                Id = i,
                                Filename = Path.GetFileName(file),
                                Filesource = file,
                                Filedestination = strHostdest + Path.GetFileName(file),
                            };

                            listfile.Add(objfile);
                            GenerateLine(file, i);
                        }
                    }

                    richTextBox1.Lines = strlistfile.ToArray();
                    richTextBox1.SelectAll();
                    richTextBox1.SelectionColor = Color.Black;
                }

                btnUpload.Text = "Upload";
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                strHostName = txtHost.Text.Trim();
                strUserName = txtusername.Text.Trim();
                strPassword = txtPassword.Text.Trim();
                strPort = txtPort.Text.Trim();
                SelectedFile = string.Empty;
                btnCancel.Text = "Cancel";

                if (listfile != null)
                {
                    int i = 0;
                    var lines = richTextBox1.Lines;

                    //Create Directory
                    if (!isDirectoryExist())
                        createDirectory();

                    foreach (var file in listfile)
                    {
                        if (file.Filedestination != string.Empty.Trim())
                        {
                            ColorText(i);
                            this.Cursor = Cursors.WaitCursor;
                            this.Text = "Uploading...";
                            btnUpload.Text = "Uploading..";
                            btnUpload.Enabled = false;
                            txtFile.Text = file.Filesource;

                            //Upload Start
                            FtpUploadFile(file);
                        }

                        i++;
                    }

                    MessageBox.Show("File Uploaded!!", "Message");

                    //Finally Clear
                    listfile.Clear();

                    fillGrid();
                    progbarFtp.Value = 0;
                    btnUpload.Text = "File Uploaded";
                    this.Text = "Uploading Progress..Done";
                    btnUpload.Enabled = true;
                    txtFile.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
                this.Text = "Error";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                if (SelectedFile == string.Empty)
                {
                    Application.Exit();
                }
                else
                {
                    //Delete File
                    deleteFile();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
        #endregion

        #region FTP
        List<Fileinfo> getDirectoryFiles()
        {
            FtpWebRequest ftpRequest = null;
            List<Fileinfo> resultlistFile = null;
            try
            {
                strHostName = txtHost.Text.Trim();
                strUserName = txtusername.Text.Trim();
                strPassword = txtPassword.Text.Trim();
                strPort = txtPort.Text.Trim();

                if ((strHostName != string.Empty) && (strUserName != string.Empty) && (strPassword != string.Empty))
                {
                    //FTP Request
                    string _strHostName = strHostName + "/" + strHostdest;
                    ftpRequest = (FtpWebRequest)FtpWebRequest.Create(_strHostName);
                    ftpRequest.Credentials = new NetworkCredential(strUserName, strPassword);
                    ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                    ftpRequest.UseBinary = true;
                    ftpRequest.KeepAlive = false;
                    ftpRequest.UsePassive = true;
                    ftpRequest.Timeout = Int32.MaxValue;
                    ftpRequest.ReadWriteTimeout = Int32.MaxValue;
                    ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                    //Read Directory
                    string[] listFile = null;
                    using (StreamReader reader = new StreamReader(ftpResponse.GetResponseStream()))
                    {
                        string strFiles = reader.ReadToEnd();
                        listFile = strFiles.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (listFile.Length > 0)
                    {
                        resultlistFile = new List<Fileinfo>();
                        int i = 0;
                        foreach (string file in listFile)
                        {
                            i++;

                            //GET FileInformation
                            string filename = file;
                            filename = filename.Remove(0, 24); filename = filename.Trim();
                            string[] fileinfo = filename.Split(' ');
                            if (fileinfo.Length > 2)
                            {
                                string _filename = string.Empty;
                                for (int j = 1; j < fileinfo.Length; j++) { _filename += fileinfo[j] + ' '; }
                                filename = _filename;
                            }
                            else
                            {
                                filename = fileinfo[1];
                            }

                            string filesize = ToFileSize(Convert.ToDouble(fileinfo[0]));
                            string date = file.Substring(0, 17);

                            //SET FileInformation
                            Fileinfo item = new Fileinfo()
                            {
                                Id = i,
                                DateCreated = DateTime.Parse(date),
                                Filename = filename,
                                Filesize = filesize
                            };

                            resultlistFile.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Error!!", "Message");
                ex.ToString();
            }
            finally
            {
                if (ftpRequest != null)
                {
                    ftpRequest = null;
                }

                if (ftpResponse != null)
                {
                    ftpResponse.Close();
                }
            }
            return resultlistFile;
        }

        void FtpUploadFile(Fileinfo file)
        {
            FtpWebRequest ftpRequest = null;
            FileStream filestream = null;
            Stream stream = null;
            try
            {
                currentPosition = 0; totalTransfered = 0; size = 0; transferRate = 0;
                FileInfo fileInfo = new FileInfo(file.Filesource);
                size = fileInfo.Length;
                progbarFtp.Minimum = 0;
                progbarFtp.Maximum = 100;
                long currentSize = 0;
                long incrementSize = (size / 100);

                string _strHostName = strHostName + "/" + file.Filedestination;
                filestream = new FileStream(file.Filesource, FileMode.Open, FileAccess.Read);
                ftpRequest = FtpWebRequest.Create(_strHostName) as FtpWebRequest;
                ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
                ftpRequest.Credentials = new NetworkCredential(strUserName, strPassword);
                ftpRequest.UseBinary = true;
                ftpRequest.KeepAlive = false;
                ftpRequest.UsePassive = true;
                ftpRequest.Timeout = Int32.MaxValue;
                ftpRequest.ReadWriteTimeout = Int32.MaxValue;
                stream = ftpRequest.GetRequestStream();

                const int bufferLength = (1 * 1024);
                byte[] buffer = new byte[bufferLength];
                int read = 0;
                while ((read = filestream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    stream.Write(buffer, 0, read);
                    currentSize += read;
                    totalTransfered += read;
                    transferRate = read;
                    if (currentSize >= incrementSize)
                    {
                        currentSize -= incrementSize;
                        progbarFtp.Invoke(new updatebar(this.UpdateProgress));
                    }
                }
                stream.Flush();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Error!!", "Message");
                ex.ToString();
            }
            finally
            {
                if (filestream != null)
                {
                    filestream.Close();
                    filestream.Dispose();
                }

                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }

                if (ftpRequest != null)
                {
                    ftpRequest = null;
                }

                if (ftpResponse != null)
                {
                    ftpResponse.Close();
                }
            }
        }

        void createDirectory()
        {
            FtpWebRequest ftpRequest = null;
            try
            {
                string _strHostName = strHostName + "/" + strHostdest;
                ftpRequest = (FtpWebRequest)WebRequest.Create(_strHostName);
                ftpRequest.Credentials = new NetworkCredential(strUserName, strPassword);
                ftpRequest.UseBinary = true;
                ftpRequest.KeepAlive = false;
                ftpRequest.UsePassive = true;
                ftpRequest.Timeout = Int32.MaxValue;
                ftpRequest.ReadWriteTimeout = Int32.MaxValue;
                ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Error!!", "Message");
                ex.ToString();
            }
            finally
            {
                if (ftpRequest != null)
                {
                    ftpRequest = null;
                }

                if (ftpResponse != null)
                {
                    ftpResponse.Close();
                }
            }
        }

        public bool isDirectoryExist()
        {
            FtpWebRequest ftpRequest = null;
            try
            {
                string _strHostName = strHostName + "/" + strHostdest;
                ftpRequest = (FtpWebRequest)WebRequest.Create(_strHostName);
                ftpRequest.Credentials = new NetworkCredential(strUserName, strPassword);
                ftpRequest.UseBinary = true;
                ftpRequest.KeepAlive = false;
                ftpRequest.UsePassive = true;
                ftpRequest.Timeout = Int32.MaxValue;
                ftpRequest.ReadWriteTimeout = Int32.MaxValue;
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                return true;
            }
            catch (WebException ex)
            {
                ex.ToString();
                return false;
            }
            finally
            {
                if (ftpRequest != null)
                {
                    ftpRequest = null;
                }

                if (ftpResponse != null)
                {
                    ftpResponse.Close();
                }
            }
        }

        void deleteFile()
        {
            FtpWebRequest ftpRequest = null;
            try
            {
                string _strHostName = strHostName + "/" + strHostdest + "/" + SelectedFile;
                ftpRequest = (FtpWebRequest)FtpWebRequest.Create(_strHostName);
                ftpRequest.Credentials = new NetworkCredential(strUserName, strPassword);
                ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                ftpRequest.UsePassive = true;
                ftpRequest.UseBinary = true;
                ftpRequest.KeepAlive = false;
                ftpRequest.Timeout = Int32.MaxValue;
                ftpRequest.ReadWriteTimeout = Int32.MaxValue;
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

                fillGrid();
                SelectedFile = string.Empty;
                btnCancel.Text = "Cancel";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection Error!!", "Message");
                ex.ToString();
            }
            finally
            {
                if (ftpRequest != null)
                {
                    ftpRequest = null;
                }

                if (ftpResponse != null)
                {
                    ftpResponse.Close();
                }
            }
        }

        void UpdateProgress()
        {
            currentPosition++;
            progbarFtp.Value = currentPosition;
            this.Text = "Transferring : " + totalTransfered.ToString() + " bytes (" + ToFileSize(transferRate).ToString() + "/s) " + ToFileSize(totalTransfered).ToString() + " / " + ToFileSize(size).ToString();
            this.btnUpload.Text = string.Format("Uploading Progress..{0}", currentPosition.ToString() + "%");
            Application.DoEvents();
        }
        #endregion

        #region GridView
        void fillGrid()
        {
            try
            {
                List<Fileinfo> listing = getDirectoryFiles();
                if (listing != null)
                {
                    if (listing.Count > 0)
                    {
                        this.dataGridView1.DataSource = null;
                        this.dataGridView1.DataSource = listing;
                        this.dataGridView1.Columns[0].Visible = false;
                        this.dataGridView1.Columns[2].Width = 30;
                        this.dataGridView1.Columns[2].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        this.dataGridView1.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        this.dataGridView1.Columns[3].Width = 50;
                        this.dataGridView1.Columns[3].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        this.dataGridView1.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                        this.dataGridView1.Columns[4].Visible = false;
                        this.dataGridView1.Columns[5].Visible = false;
                    }
                }
                else
                {
                    this.dataGridView1.DataSource = null;
                    MessageBox.Show("Empty Directory!!", "Message");
                }
            }
            catch (WebException ex)
            {
                ex.ToString();
            }
        }

        void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            if (dgv.SelectedCells.Count > 0)
            {
                SelectedFile = Convert.ToString(dgv.Rows[dgv.SelectedCells[1].RowIndex].Cells[1].Value.ToString());
                if (SelectedFile != string.Empty)
                    btnCancel.Text = "Delete File";
            }
        }
        #endregion

        #region Utilities
        void GenerateLine(string file, int i)
        {
            try
            {
                int limit = 50;
                string filedispname = Path.GetFileNameWithoutExtension(file).ToString();
                if (filedispname.Length > limit)
                    strlistfile.Add("File-" + i.ToString() + " : " + filedispname.Substring(0, limit) + "..");
                else
                    strlistfile.Add("File-" + i.ToString() + " : " + filedispname);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        void ColorText(int i)
        {
            try
            {
                int firstcharindex = richTextBox1.GetFirstCharIndexFromLine(i);
                int currentline = richTextBox1.GetLineFromCharIndex(firstcharindex);
                string currentlinetext = richTextBox1.Lines[currentline];
                richTextBox1.Select(firstcharindex, currentlinetext.Length);
                richTextBox1.SelectionColor = Color.Green;
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        static string ToFileSize(double value)
        {
            string result = string.Empty;
            try
            {
                string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
                for (int i = 0; i < suffixes.Length; i++)
                {
                    if (value <= (Math.Pow(1024, i + 1)))
                        return ThreeNonZeroDigits(value / Math.Pow(1024, i)) + " " + suffixes[i];
                }
                result = ThreeNonZeroDigits(value / Math.Pow(1024, suffixes.Length - 1)) + " " + suffixes[suffixes.Length - 1];
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            return result;
        }

        static string ThreeNonZeroDigits(double value)
        {
            if (value >= 100)
                return value.ToString("0,0");
            else if (value >= 10)
                return value.ToString("0.0");
            else
                return value.ToString("0.00");
        }

        string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", " ", RegexOptions.Compiled);
        }
        #endregion

        #region Setting
        void saveConnDetails()
        {
            Settings.Default.HostName = txtHost.Text.Trim();
            Settings.Default.UserName = txtusername.Text.Trim();
            Settings.Default.Password = txtPassword.Text;
            Settings.Default.Port = txtPort.Text.Trim();
            Settings.Default.Save();
        }

        void getConnDetails()
        {
            txtHost.Text = Settings.Default.HostName;
            txtusername.Text = Settings.Default.UserName;
            txtPassword.Text = Settings.Default.Password;
            txtPort.Text = Settings.Default.Port;
            strHostdest = ConfigurationManager.AppSettings["basefolder"];
        }
        #endregion
    }
}
