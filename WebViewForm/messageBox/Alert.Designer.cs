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
            labelMsg = new Label();
            btnOk = new Button();
            panelBtn = new Panel();
            panelBtn.SuspendLayout();
            SuspendLayout();
            // 
            // labelMsg
            // 
            labelMsg.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelMsg.Font = new Font("맑은 고딕", 8F);
            labelMsg.Location = new Point(0, 0);
            labelMsg.Name = "labelMsg";
            labelMsg.Padding = new Padding(8, 0, 8, 0);
            labelMsg.Size = new Size(384, 64);
            labelMsg.TabIndex = 0;
            labelMsg.Text = "메시지";
            labelMsg.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnOk
            // 
            btnOk.Dock = DockStyle.Right;
            btnOk.Font = new Font("맑은 고딕", 8F);
            btnOk.Location = new Point(294, 9);
            btnOk.Margin = new Padding(3, 4, 3, 10);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 2;
            btnOk.Text = "확인";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += OnClick;
            btnOk.KeyDown += OnKeyDown;
            // 
            // panelBtn
            // 
            panelBtn.BackColor = SystemColors.Menu;
            panelBtn.Controls.Add(btnOk);
            panelBtn.Dock = DockStyle.Bottom;
            panelBtn.Location = new Point(0, 65);
            panelBtn.Name = "panelBtn";
            panelBtn.Padding = new Padding(0, 9, 15, 10);
            panelBtn.Size = new Size(384, 42);
            panelBtn.TabIndex = 3;
            // 
            // Alert
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(384, 107);
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