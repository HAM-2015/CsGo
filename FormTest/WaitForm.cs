using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Go;

namespace FormTest
{
    public partial class WaitForm : Form
    {
        generator _action;
        public bool isCancel = false;

        public WaitForm()
        {
            InitializeComponent();
        }

        private void WaitForm_Load(object sender, EventArgs e)
        {
            _action = generator.tgo(FormTest._mainStrand, async delegate ()
            {
                while (true)
                {
                    await generator.sleep(30);
                    progressBar1.Value = (int)(system_tick.get_tick_ms() % 1000 / 10);
                }
            });
        }

        private void WaitForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _action.stop();
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            isCancel = true;
            Close();
        }
    }
}
