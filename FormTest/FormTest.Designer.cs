namespace FormTest
{
    partial class FormTest
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_Action = new System.Windows.Forms.TextBox();
            this.btn_Pause = new System.Windows.Forms.Button();
            this.btn_Task1 = new System.Windows.Forms.Button();
            this.numericUpDown_Time = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Time)).BeginInit();
            this.SuspendLayout();
            // 
            // textBox_Action
            // 
            this.textBox_Action.Location = new System.Drawing.Point(12, 12);
            this.textBox_Action.Name = "textBox_Action";
            this.textBox_Action.Size = new System.Drawing.Size(139, 21);
            this.textBox_Action.TabIndex = 0;
            // 
            // btn_Pause
            // 
            this.btn_Pause.Location = new System.Drawing.Point(175, 12);
            this.btn_Pause.Name = "btn_Pause";
            this.btn_Pause.Size = new System.Drawing.Size(75, 23);
            this.btn_Pause.TabIndex = 2;
            this.btn_Pause.Text = "暂停";
            this.btn_Pause.UseVisualStyleBackColor = true;
            this.btn_Pause.Click += new System.EventHandler(this.btn_Pause_Click);
            // 
            // btn_Task1
            // 
            this.btn_Task1.Location = new System.Drawing.Point(175, 43);
            this.btn_Task1.Name = "btn_Task1";
            this.btn_Task1.Size = new System.Drawing.Size(75, 23);
            this.btn_Task1.TabIndex = 3;
            this.btn_Task1.Text = "耗时任务";
            this.btn_Task1.UseVisualStyleBackColor = true;
            this.btn_Task1.Click += new System.EventHandler(this.btn_Task1_Click);
            // 
            // numericUpDown_Time
            // 
            this.numericUpDown_Time.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown_Time.Location = new System.Drawing.Point(12, 44);
            this.numericUpDown_Time.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.numericUpDown_Time.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDown_Time.Name = "numericUpDown_Time";
            this.numericUpDown_Time.Size = new System.Drawing.Size(139, 21);
            this.numericUpDown_Time.TabIndex = 4;
            this.numericUpDown_Time.Value = new decimal(new int[] {
            3000,
            0,
            0,
            0});
            // 
            // FormTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(264, 78);
            this.Controls.Add(this.numericUpDown_Time);
            this.Controls.Add(this.btn_Task1);
            this.Controls.Add(this.btn_Pause);
            this.Controls.Add(this.textBox_Action);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FormTest";
            this.Text = "FormTest";
            this.Load += new System.EventHandler(this.FormTest_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_Time)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_Action;
        private System.Windows.Forms.Button btn_Pause;
        private System.Windows.Forms.Button btn_Task1;
        private System.Windows.Forms.NumericUpDown numericUpDown_Time;
    }
}

