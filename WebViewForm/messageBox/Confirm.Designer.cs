namespace WebViewForm
{
    partial class Confirm
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
            btnNo = new Button();
            panelBtn = new Panel();
            btnYes = new Button();
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
            // btnNo
            // 
            btnNo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            btnNo.Font = new Font("맑은 고딕", 8F);
            btnNo.Location = new Point(294, 9);
            btnNo.Margin = new Padding(0);
            btnNo.Name = "btnNo";
            btnNo.Padding = new Padding(2, 0, 0, 0);
            btnNo.Size = new Size(75, 23);
            btnNo.TabIndex = 2;
            btnNo.Text = "아니오(&N)";
            btnNo.UseVisualStyleBackColor = true;
            btnNo.Click += OnClick;
            btnNo.KeyDown += OnKeyDown;
            // 
            // panelBtn
            // 
            panelBtn.BackColor = SystemColors.Menu;
            panelBtn.Controls.Add(btnYes);
            panelBtn.Controls.Add(btnNo);
            panelBtn.Dock = DockStyle.Bottom;
            panelBtn.Location = new Point(0, 65);
            panelBtn.Name = "panelBtn";
            panelBtn.Padding = new Padding(0, 9, 16, 10);
            panelBtn.Size = new Size(384, 42);
            panelBtn.TabIndex = 3;
            // 
            // btnYes
            // 
            btnYes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            btnYes.Font = new Font("맑은 고딕", 8F);
            btnYes.Location = new Point(210, 9);
            btnYes.Margin = new Padding(0);
            btnYes.Name = "btnYes";
            btnYes.Padding = new Padding(2, 0, 0, 0);
            btnYes.Size = new Size(75, 23);
            btnYes.TabIndex = 3;
            btnYes.Text = "예(&Y)";
            btnYes.UseVisualStyleBackColor = true;
            btnYes.Click += OnClick;
            btnYes.KeyDown += OnKeyDown;
            // 
            // Confirm
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
            Name = "Confirm";
            SizeGripStyle = SizeGripStyle.Hide;
            Text = "Confirm";
            TopMost = true;
            panelBtn.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private Label labelMsg;
        private Panel panelBtn;
        private Button btnYes;
        private Button btnNo;
    }
}