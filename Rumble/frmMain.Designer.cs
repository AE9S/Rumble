namespace Rumble
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.cmdListen = new System.Windows.Forms.Button();
            this.cmdStop = new System.Windows.Forms.Button();
            this.cmdMute = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.comboWaveIn = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboWaveOut = new System.Windows.Forms.ComboBox();
            this.cmdUseDevices = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.cmdSelectIDFile = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtTimerInterval = new System.Windows.Forms.TextBox();
            this.lblWavIDFile = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(13, 143);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(729, 112);
            this.textBox1.TabIndex = 0;
            // 
            // cmdListen
            // 
            this.cmdListen.Enabled = false;
            this.cmdListen.Location = new System.Drawing.Point(12, 261);
            this.cmdListen.Name = "cmdListen";
            this.cmdListen.Size = new System.Drawing.Size(75, 23);
            this.cmdListen.TabIndex = 1;
            this.cmdListen.Text = "Listen";
            this.cmdListen.UseVisualStyleBackColor = true;
            this.cmdListen.Click += new System.EventHandler(this.cmdListen_Click);
            // 
            // cmdStop
            // 
            this.cmdStop.Enabled = false;
            this.cmdStop.Location = new System.Drawing.Point(667, 261);
            this.cmdStop.Name = "cmdStop";
            this.cmdStop.Size = new System.Drawing.Size(75, 23);
            this.cmdStop.TabIndex = 2;
            this.cmdStop.Text = "Stop";
            this.cmdStop.UseVisualStyleBackColor = true;
            this.cmdStop.Click += new System.EventHandler(this.cmdStop_Click);
            // 
            // cmdMute
            // 
            this.cmdMute.Location = new System.Drawing.Point(94, 261);
            this.cmdMute.Name = "cmdMute";
            this.cmdMute.Size = new System.Drawing.Size(75, 23);
            this.cmdMute.TabIndex = 3;
            this.cmdMute.Text = "Mute";
            this.cmdMute.UseVisualStyleBackColor = true;
            this.cmdMute.Click += new System.EventHandler(this.cmdMute_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Wave In Device";
            // 
            // comboWaveIn
            // 
            this.comboWaveIn.FormattingEnabled = true;
            this.comboWaveIn.Location = new System.Drawing.Point(16, 30);
            this.comboWaveIn.Name = "comboWaveIn";
            this.comboWaveIn.Size = new System.Drawing.Size(233, 21);
            this.comboWaveIn.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(93, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Wave Out Device";
            // 
            // comboWaveOut
            // 
            this.comboWaveOut.FormattingEnabled = true;
            this.comboWaveOut.Location = new System.Drawing.Point(16, 75);
            this.comboWaveOut.Name = "comboWaveOut";
            this.comboWaveOut.Size = new System.Drawing.Size(233, 21);
            this.comboWaveOut.TabIndex = 7;
            // 
            // cmdUseDevices
            // 
            this.cmdUseDevices.Location = new System.Drawing.Point(19, 103);
            this.cmdUseDevices.Name = "cmdUseDevices";
            this.cmdUseDevices.Size = new System.Drawing.Size(75, 23);
            this.cmdUseDevices.TabIndex = 8;
            this.cmdUseDevices.Text = "Use These";
            this.cmdUseDevices.UseVisualStyleBackColor = true;
            this.cmdUseDevices.Click += new System.EventHandler(this.cmdUseDevices_Click);
            // 
            // cmdSelectIDFile
            // 
            this.cmdSelectIDFile.Location = new System.Drawing.Point(303, 58);
            this.cmdSelectIDFile.Name = "cmdSelectIDFile";
            this.cmdSelectIDFile.Size = new System.Drawing.Size(75, 23);
            this.cmdSelectIDFile.TabIndex = 9;
            this.cmdSelectIDFile.Text = "Select ID File";
            this.cmdSelectIDFile.UseVisualStyleBackColor = true;
            this.cmdSelectIDFile.Click += new System.EventHandler(this.cmdSelectIDFile_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(300, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Timer Interval (sec)";
            // 
            // txtTimerInterval
            // 
            this.txtTimerInterval.Location = new System.Drawing.Point(303, 30);
            this.txtTimerInterval.Name = "txtTimerInterval";
            this.txtTimerInterval.Size = new System.Drawing.Size(100, 20);
            this.txtTimerInterval.TabIndex = 11;
            this.txtTimerInterval.Text = "600";
            // 
            // lblWavIDFile
            // 
            this.lblWavIDFile.AutoSize = true;
            this.lblWavIDFile.Location = new System.Drawing.Point(384, 63);
            this.lblWavIDFile.Name = "lblWavIDFile";
            this.lblWavIDFile.Size = new System.Drawing.Size(0, 13);
            this.lblWavIDFile.TabIndex = 12;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(754, 292);
            this.Controls.Add(this.lblWavIDFile);
            this.Controls.Add(this.txtTimerInterval);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmdSelectIDFile);
            this.Controls.Add(this.cmdUseDevices);
            this.Controls.Add(this.comboWaveOut);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboWaveIn);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdMute);
            this.Controls.Add(this.cmdStop);
            this.Controls.Add(this.cmdListen);
            this.Controls.Add(this.textBox1);
            this.Name = "frmMain";
            this.Text = "Rumble";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button cmdListen;
        private System.Windows.Forms.Button cmdStop;
        private System.Windows.Forms.Button cmdMute;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboWaveIn;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboWaveOut;
        private System.Windows.Forms.Button cmdUseDevices;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button cmdSelectIDFile;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtTimerInterval;
        private System.Windows.Forms.Label lblWavIDFile;
    }
}

