namespace Jamaker.addon
{
    partial class PosPicker
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
            border = new Label();
            labelPos = new Label();
            inputValue = new TextBox();
            btnOk = new Button();
            SuspendLayout();
            // 
            // border
            // 
            border.BackColor = Color.Black;
            border.Location = new Point(16, 21);
            border.Margin = new Padding(6, 0, 6, 0);
            border.Name = "border";
            border.Size = new Size(204, 47);
            border.TabIndex = 0;
            // 
            // labelPos
            // 
            labelPos.BackColor = Color.White;
            labelPos.Location = new Point(18, 23);
            labelPos.Margin = new Padding(6, 0, 6, 0);
            labelPos.Name = "labelPos";
            labelPos.Size = new Size(200, 43);
            labelPos.TabIndex = 2;
            labelPos.Text = "1920.00,1080.00";
            labelPos.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // inputValue
            // 
            inputValue.Location = new Point(432, 587);
            inputValue.Name = "inputValue";
            inputValue.Size = new Size(200, 39);
            inputValue.TabIndex = 3;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(645, 589);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(150, 46);
            btnOk.TabIndex = 4;
            btnOk.Text = "입력";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += ClickBtnOk;
            // 
            // PosPicker
            // 
            AutoScaleDimensions = new SizeF(14F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Turquoise;
            ClientSize = new Size(3870, 2190);
            Controls.Add(btnOk);
            Controls.Add(inputValue);
            Controls.Add(labelPos);
            Controls.Add(border);
            Cursor = Cursors.Cross;
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(6, 9, 6, 9);
            Name = "PosPicker";
            StartPosition = FormStartPosition.Manual;
            Text = "PosPicker";
            TransparencyKey = Color.Turquoise;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label border;
        private System.Windows.Forms.Label labelPos;
        private TextBox inputValue;
        private Button btnOk;
    }
}