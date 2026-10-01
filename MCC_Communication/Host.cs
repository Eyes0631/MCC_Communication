using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MCC_Communication
{
    public partial class Host : Form
    {
        private MCC_BMP_Host M_Host = new MCC_BMP_Host();
        public Host()
        {
            InitializeComponent();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //M_Host.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //M_Host.Stop();
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }
    }
}
