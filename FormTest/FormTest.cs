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
    public partial class FormTest : Form
    {
        control_strand _strand1;
        shared_strand _strand2;
        generator _action1;
        generator _action2;

        public FormTest()
        {
            InitializeComponent();
        }

        private void FormTest_Load(object sender, EventArgs e)
        {
            _strand1 = new control_strand(this);
            _strand2 = new shared_strand();
            _action1 = generator.tgo(_strand1, Action1);
            _action2 = generator.tgo(_strand2, Action2);
        }

        private async Task Action1()
        {
            while (true)
            {
                await generator.sleep((int)numericUpDown_SleepMs1.Value);
                textBox_Action1.Text = string.Format("{0}.{1}", DateTime.Now.ToLocalTime(), DateTime.Now.Millisecond);
            }
        }

        private async Task Action2()
        {
            while (true)
            {
                await generator.sleep(await generator.send_control(this, () => (int)numericUpDown_SleepMs2.Value));
                await generator.send_control(this, () => textBox_Action2.Text = string.Format("{0}.{1}", DateTime.Now.ToLocalTime(), DateTime.Now.Millisecond));
            }
        }

        private void btn_Pause1_Click(object sender, EventArgs e)
        {
            if ("pause" == btn_Pause1.Text)
            {
                btn_Pause1.Text = "resume";
                _action1.suspend();
            }
            else
            {
                btn_Pause1.Text = "pause";
                _action1.resume();
            }
        }

        private void btn_Pause2_Click(object sender, EventArgs e)
        {
            if ("pause" == btn_Pause2.Text)
            {
                btn_Pause2.Text = "resume";
                _action2.suspend();
            }
            else
            {
                btn_Pause2.Text = "pause";
                _action2.resume();
            }
        }
    }
}
