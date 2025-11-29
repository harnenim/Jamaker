namespace WebViewForm
{
    partial class Alert
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Alert));
            labelMsg = new Label();
            btnOk = new Button();
            panelBtn = new Panel();
            panelBtn.SuspendLayout();
            SuspendLayout();
            // 
            // labelMsg
            // 
            labelMsg.Dock = DockStyle.Top;
            labelMsg.Location = new Point(0, 0);
            labelMsg.Name = "labelMsg";
            labelMsg.Padding = new Padding(10, 0, 10, 0);
            labelMsg.Size = new Size(224, 48);
            labelMsg.TabIndex = 0;
            labelMsg.Text = "메시지";
            labelMsg.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelBtn
            // 
            panelBtn.BackColor = SystemColors.Menu;
            panelBtn.Controls.Add(btnOk);
            panelBtn.Dock = DockStyle.Bottom;
            panelBtn.Location = new Point(0, 51);
            panelBtn.Name = "panel1";
            panelBtn.Padding = new Padding(0, 10, 16, 11);
            panelBtn.Size = new Size(224, 42);
            panelBtn.TabIndex = 3;
            // 
            // btnOk
            // 
            btnOk.Dock = DockStyle.Right;
            btnOk.Location = new Point(135, 10);
            btnOk.Margin = new Padding(3, 4, 3, 10);
            btnOk.Name = "buttonSubmit";
            btnOk.Size = new Size(73, 21);
            btnOk.TabIndex = 2;
            btnOk.Text = "확인";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += OnClick;
            btnOk.KeyDown += OnKeyDown;
            // 
            // Alert
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(224, 93);
            Controls.Add(panelBtn);
            Controls.Add(labelMsg);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Alert";
            SizeGripStyle = SizeGripStyle.Hide;
            Text = "Alert";
            TopMost = true;
            panelBtn.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private Label labelMsg;
        private Panel panelBtn;
        private Button btnOk;
    }
}