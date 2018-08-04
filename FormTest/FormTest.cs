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
        static public control_strand _mainStrand;
        generator _timeAction;

        public FormTest()
        {
            InitializeComponent();
        }

        private void FormTest_Load(object sender, EventArgs e)
        {
            _mainStrand = new control_strand(this);
            _timeAction = generator.tgo(_mainStrand, TimeAction);
        }

        private async Task TimeAction()
        {
            while (true)
            {
                await generator.sleep(1);
                textBox_Action.Text = DateTime.Now.ToString("yy-MM-dd HH:mm:ss.fff");
            }
        }

        private async Task Task1Action(int time)
        {
            WaitForm waitAction = new WaitForm();
            generator.children children = new generator.children();
            generator.child taskChild = children.tgo(async delegate ()
            {
                bool cancelTask = false;
                Task task = Task.Run(delegate ()
                {
                    int ct = time / 100;
                    for (int i = 0; i < ct && !cancelTask; i++)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                });
                try
                {
                    await generator.wait_task(task);
                }
                catch (generator.stop_exception)
                {
                    cancelTask = true;
                    await generator.wait_task(task);
                }
            });
            children.go(async delegate ()
            {
                await generator.sleep(500);
                await generator.send_control(this, delegate ()
                {
                    waitAction.StartPosition = FormStartPosition.CenterParent;
                    waitAction.ShowDialog();
                });
                if (waitAction.isCancel)
                {
                    taskChild.stop();
                }
            });
            await children.wait(taskChild);
            waitAction.Close();
            await children.stop();
        }

        private void btn_Pause_Click(object sender, EventArgs e)
        {
            if ("暂停" == btn_Pause.Text)
            {
                btn_Pause.Text = "恢复";
                _timeAction.suspend();
            }
            else
            {
                btn_Pause.Text = "暂停";
                _timeAction.resume();
            }
        }

        private void btn_Task1_Click(object sender, EventArgs e)
        {
            btn_Task1.Enabled = false;
            generator.go(_mainStrand, functional.bind(Task1Action, (int)numericUpDown_Time.Value), () => btn_Task1.Enabled = true);
        }
    }
}
