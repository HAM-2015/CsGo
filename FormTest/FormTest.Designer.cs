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
            this.textBox_Action1 = new System.Windows.Forms.TextBox();
            this.numericUpDown_SleepMs1 = new System.Windows.Forms.NumericUpDown();
            this.btn_Pause1 = new System.Windows.Forms.Button();
            this.textBox_Action2 = new System.Windows.Forms.TextBox();
            this.numericUpDown_SleepMs2 = new System.Windows.Forms.NumericUpDown();
            this.btn_Pause2 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_SleepMs1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_SleepMs2)).BeginInit();
            this.SuspendLayout();
            // 
            // textBox_Action1
            // 
            this.textBox_Action1.Location = new System.Drawing.Point(12, 12);
            this.textBox_Action1.Name = "textBox_Action1";
            this.textBox_Action1.Size = new System.Drawing.Size(139, 21);
            this.textBox_Action1.TabIndex = 0;
            // 
            // numericUpDown_SleepMs1
            // 
            this.numericUpDown_SleepMs1.Location = new System.Drawing.Point(13, 40);
            this.numericUpDown_SleepMs1.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDown_SleepMs1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_SleepMs1.Name = "numericUpDown_SleepMs1";
            this.numericUpDown_SleepMs1.Size = new System.Drawing.Size(138, 21);
            this.numericUpDown_SleepMs1.TabIndex = 1;
            this.numericUpDown_SleepMs1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // btn_Pause1
            // 
            this.btn_Pause1.Location = new System.Drawing.Point(175, 12);
            this.btn_Pause1.Name = "btn_Pause1";
            this.btn_Pause1.Size = new System.Drawing.Size(75, 23);
            this.btn_Pause1.TabIndex = 2;
            this.btn_Pause1.Text = "pause";
            this.btn_Pause1.UseVisualStyleBackColor = true;
            this.btn_Pause1.Click += new System.EventHandler(this.btn_Pause1_Click);
            // 
            // textBox_Action2
            // 
            this.textBox_Action2.Location = new System.Drawing.Point(13, 92);
            this.textBox_Action2.Name = "textBox_Action2";
            this.textBox_Action2.Size = new System.Drawing.Size(139, 21);
            this.textBox_Action2.TabIndex = 0;
            // 
            // numericUpDown_SleepMs2
            // 
            this.numericUpDown_SleepMs2.Location = new System.Drawing.Point(14, 120);
            this.numericUpDown_SleepMs2.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDown_SleepMs2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown_SleepMs2.Name = "numericUpDown_SleepMs2";
            this.numericUpDown_SleepMs2.Size = new System.Drawing.Size(138, 21);
            this.numericUpDown_SleepMs2.TabIndex = 1;
            this.numericUpDown_SleepMs2.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // btn_Pause2
            // 
            this.btn_Pause2.Location = new System.Drawing.Point(176, 92);
            this.btn_Pause2.Name = "btn_Pause2";
            this.btn_Pause2.Size = new System.Drawing.Size(75, 23);
            this.btn_Pause2.TabIndex = 2;
            this.btn_Pause2.Text = "pause";
            this.btn_Pause2.UseVisualStyleBackColor = true;
            this.btn_Pause2.Click += new System.EventHandler(this.btn_Pause2_Click);
            // 
            // FormTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 157);
            this.Controls.Add(this.btn_Pause2);
            this.Controls.Add(this.btn_Pause1);
            this.Controls.Add(this.numericUpDown_SleepMs2);
            this.Controls.Add(this.numericUpDown_SleepMs1);
            this.Controls.Add(this.textBox_Action2);
            this.Controls.Add(this.textBox_Action1);
            this.Name = "FormTest";
            this.Text = "FormTest";
            this.Load += new System.EventHandler(this.FormTest_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_SleepMs1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_SleepMs2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_Action1;
        private System.Windows.Forms.NumericUpDown numericUpDown_SleepMs1;
        private System.Windows.Forms.Button btn_Pause1;
        private System.Windows.Forms.TextBox textBox_Action2;
        private System.Windows.Forms.NumericUpDown numericUpDown_SleepMs2;
        private System.Windows.Forms.Button btn_Pause2;
    }
}

