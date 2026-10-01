using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using PaeLibGeneral;

namespace MCC_Communication
{
    public partial class ClientForm : Form
    {
        //private Host Host_Program = new Host();
        private MCC_BMP BMP = new MCC_BMP();
        //private MCC_BMP_Host Listen = new MCC_BMP_Host();
        public ClientForm()
        {
            InitializeComponent();
            InitialSetting();
        }

        private void InitialSetting()
        {
            tb_IP.Text = "127.0.0.1";
            tb_Port.Text = "5555";
            tb_RunMode.Text = "Load";
            tb_SlotNo.Text = "1";
            tb_ChamberNo.Text = "1";
            tb_LotNo.Text = "Lot123";
            tb_BoardType.Text = "TYPE_A";
            tb_Program.Text = "PROGRAM_X";
            string _fullPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "System", "Setup.ini");
            if (File.Exists(_fullPath))
            {
                using (TINIFile ini = new TINIFile(_fullPath))
                {
                    ini.ReadString("MCCLink", "MainAddress", tb_IP.Text.Trim());
                    ini.ReadInteger("MCCLink", "ServerPort", Convert.ToInt32(tb_Port.Text.Trim()));
                    ini.ReadString("MCCLink", "RunMode", tb_RunMode.Text.Trim());
                    ini.ReadInteger("MCCLink", "SlotNo", Convert.ToInt32(tb_SlotNo.Text.Trim()));
                    ini.ReadInteger("MCCLink", "ChamberNo", Convert.ToInt32(tb_ChamberNo.Text.Trim()));
                    ini.ReadString("MCCLink", "LotNo", tb_LotNo.Text.Trim());
                    ini.ReadString("MCCLink", "BoardType", tb_BoardType.Text.Trim());
                    ini.ReadString("MCCLink", "Program", tb_Program.Text.Trim());
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string _msg = string.Empty;
            BMP.Action_ISB(tb_RunMode.Text.Trim(), Convert.ToInt32(tb_SlotNo.Text.Trim()), Convert.ToInt32(tb_ChamberNo.Text.Trim()), ref _msg);
            if (string.IsNullOrEmpty(_msg) == false)
            {
                richTextBox1.AppendText("Error Message: " + _msg);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (BMP.bRefresh)
            {
                richTextBox1.AppendText(BMP.UpdateText());
                richTextBox1.ScrollToCaret(); 
                //richTextBox1.Text += BMP.UpdateText();
            }
            //BMP.UpdateListen();
            //if (Listen.bRefresh)
            //{
            //    richTextBox1.AppendText(Listen.UpdateText());
            //    richTextBox1.ScrollToCaret();
            //}
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string _msg = string.Empty;
            BMP.Action_EJB(tb_RunMode.Text.Trim(), Convert.ToInt32(tb_SlotNo.Text.Trim()), Convert.ToInt32(tb_ChamberNo.Text.Trim()), ref _msg);
            if (string.IsNullOrEmpty(_msg) == false)
            {
                richTextBox1.AppendText("Error Message: " + _msg);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string _msg = string.Empty;
            //BMP.Action_ISB("Insert", 1, 1, ref _msg);
            BMP.Action_STG(tb_RunMode.Text.Trim(), Convert.ToInt32(tb_SlotNo.Text.Trim()), Convert.ToInt32(tb_ChamberNo.Text.Trim()), 
                           tb_LotNo.Text.Trim(), tb_BoardType.Text.Trim(), tb_Program.Text.Trim(), ref _msg);
            
            if (string.IsNullOrEmpty(_msg) == false)
            {
                richTextBox1.AppendText("Error Message: " + _msg);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //檢查是否可以轉數字
            int value = 0;
            if (!int.TryParse(tb_Port.Text, out value))
            {
                MessageBox.Show("Port setting error!!, save faile");
                return;
            }
            if (!int.TryParse(tb_SlotNo.Text, out value))
            {
                MessageBox.Show("SlotNo setting error!!, save faile");
                return;
            }
            if (!int.TryParse(tb_ChamberNo.Text, out value))
            {
                MessageBox.Show("ChamberNo setting error!!, save faile");
                return;
            }
            string _fullPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "System", "Setup.ini");
            if (File.Exists(_fullPath))
            {
                using (TINIFile ini = new TINIFile(_fullPath))
                {
                    ini.WriteString("MCCLink", "MainAddress", tb_IP.Text.Trim());
                    ini.WriteInteger("MCCLink", "ServerPort", Convert.ToInt32(tb_Port.Text.Trim()));
                    ini.WriteString("MCCLink", "RunMode", tb_RunMode.Text.Trim());
                    ini.WriteInteger("MCCLink", "SlotNo", Convert.ToInt32(tb_SlotNo.Text.Trim()));
                    ini.WriteInteger("MCCLink", "ChamberNo", Convert.ToInt32(tb_ChamberNo.Text.Trim()));
                    ini.WriteString("MCCLink", "LotNo", tb_LotNo.Text.Trim());
                    ini.WriteString("MCCLink", "BoardType", tb_BoardType.Text.Trim());
                    ini.WriteString("MCCLink", "Program", tb_Program.Text.Trim());
                }
            }
        }

        private  void ClientForm_Load(object sender, EventArgs e)
        {
            //await Listen.Start();
        }

        private void ClientForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Listen.Stop();
        }
    }
}
