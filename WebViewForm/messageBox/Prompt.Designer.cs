namespace WebViewForm
{
    partial class Prompt
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
            textBoxValue = new TextBox();
            buttonSubmit = new Button();
            SuspendLayout();
            // 
            // labelMsg
            // 
            labelMsg.AutoSize = true;
            labelMsg.Location = new Point(12, 9);
            labelMsg.Name = "labelMsg";
            labelMsg.Size = new Size(51, 19);
            labelMsg.TabIndex = 0;
            labelMsg.Text = "메시지";
            // 
            // textBoxValue
            // 
            textBoxValue.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxValue.Location = new Point(12, 35);
            textBoxValue.Margin = new Padding(0);
            textBoxValue.Multiline = true;
            textBoxValue.Name = "textBoxValue";
            textBoxValue.Size = new Size(199, 25);
            textBoxValue.TabIndex = 1;
            textBoxValue.KeyDown += OnKeyDown;
            // 
            // buttonSubmit
            // 
            buttonSubmit.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonSubmit.Location = new Point(217, 34);
            buttonSubmit.Margin = new Padding(0);
            buttonSubmit.Name = "buttonSubmit";
            buttonSubmit.Size = new Size(75, 27);
            buttonSubmit.TabIndex = 2;
            buttonSubmit.Text = "입력";
            buttonSubmit.UseVisualStyleBackColor = true;
            buttonSubmit.Click += OnClick;
            // 
            // Prompt
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(304, 71);
            Controls.Add(buttonSubmit);
            Controls.Add(textBoxValue);
            Controls.Add(labelMsg);
            Font = new Font("맑은 고딕", 10.5F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 5, 3, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Prompt";
            SizeGripStyle = SizeGripStyle.Hide;
            Text = "Prompt";
            TopMost = true;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelMsg;
        private System.Windows.Forms.TextBox textBoxValue;
        private System.Windows.Forms.Button buttonSubmit;
    }
}