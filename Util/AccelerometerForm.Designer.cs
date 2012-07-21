namespace NeroOS.Util
{
    partial class AccelerometerForm
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
            this.aXBar = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.aYBar = new System.Windows.Forms.ProgressBar();
            this.label3 = new System.Windows.Forms.Label();
            this.aZBar = new System.Windows.Forms.ProgressBar();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.gXBar = new System.Windows.Forms.ProgressBar();
            this.gYBar = new System.Windows.Forms.ProgressBar();
            this.gZBar = new System.Windows.Forms.ProgressBar();
            this.button1 = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // aXBar
            // 
            this.aXBar.Location = new System.Drawing.Point(145, 12);
            this.aXBar.Maximum = 1024;
            this.aXBar.Name = "aXBar";
            this.aXBar.Size = new System.Drawing.Size(594, 69);
            this.aXBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.aXBar.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(31, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 31);
            this.label1.TabIndex = 1;
            this.label1.Text = "AX:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(31, 87);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 31);
            this.label2.TabIndex = 2;
            this.label2.Text = "AY:";
            // 
            // aYBar
            // 
            this.aYBar.Location = new System.Drawing.Point(145, 87);
            this.aYBar.Maximum = 1024;
            this.aYBar.Name = "aYBar";
            this.aYBar.Size = new System.Drawing.Size(594, 69);
            this.aYBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.aYBar.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(32, 162);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 31);
            this.label3.TabIndex = 4;
            this.label3.Text = "AZ:";
            // 
            // aZBar
            // 
            this.aZBar.Location = new System.Drawing.Point(145, 162);
            this.aZBar.Maximum = 1024;
            this.aZBar.Name = "aZBar";
            this.aZBar.Size = new System.Drawing.Size(594, 69);
            this.aZBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.aZBar.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(31, 237);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 31);
            this.label4.TabIndex = 6;
            this.label4.Text = "GX:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(31, 312);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 31);
            this.label5.TabIndex = 7;
            this.label5.Text = "GY:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(31, 391);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(63, 31);
            this.label6.TabIndex = 8;
            this.label6.Text = "GZ:";
            // 
            // gXBar
            // 
            this.gXBar.Location = new System.Drawing.Point(145, 237);
            this.gXBar.Maximum = 1024;
            this.gXBar.Name = "gXBar";
            this.gXBar.Size = new System.Drawing.Size(594, 69);
            this.gXBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.gXBar.TabIndex = 9;
            // 
            // gYBar
            // 
            this.gYBar.Location = new System.Drawing.Point(145, 312);
            this.gYBar.Maximum = 1024;
            this.gYBar.Name = "gYBar";
            this.gYBar.Size = new System.Drawing.Size(594, 69);
            this.gYBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.gYBar.TabIndex = 10;
            // 
            // gZBar
            // 
            this.gZBar.Location = new System.Drawing.Point(145, 391);
            this.gZBar.Maximum = 1024;
            this.gZBar.Name = "gZBar";
            this.gZBar.Size = new System.Drawing.Size(594, 69);
            this.gZBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.gZBar.TabIndex = 11;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(37, 466);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(190, 55);
            this.button1.TabIndex = 12;
            this.button1.Text = "Calibrate";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(306, 485);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(90, 17);
            this.statusLabel.TabIndex = 13;
            this.statusLabel.Text = "Status: None";
            // 
            // AccelerometerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(751, 528);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.gZBar);
            this.Controls.Add(this.gYBar);
            this.Controls.Add(this.gXBar);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.aZBar);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.aYBar);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.aXBar);
            this.Name = "AccelerometerForm";
            this.Text = "AccelerometerForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar aXBar;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar aYBar;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ProgressBar aZBar;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ProgressBar gXBar;
        private System.Windows.Forms.ProgressBar gYBar;
        private System.Windows.Forms.ProgressBar gZBar;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label statusLabel;
    }
}