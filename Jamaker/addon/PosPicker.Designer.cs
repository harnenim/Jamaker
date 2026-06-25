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
            border.Location = new Point(8, 10);
            border.Name = "border";
            border.Size = new Size(102, 22);
            border.TabIndex = 0;
            // 
            // labelPos
            // 
            labelPos.BackColor = Color.White;
            labelPos.Location = new Point(9, 11);
            labelPos.Name = "labelPos";
            labelPos.Size = new Size(100, 20);
            labelPos.TabIndex = 2;
            labelPos.Text = "1920.00,1080.00";
            labelPos.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // inputValue
            // 
            inputValue.Location = new Point(84, 87);
            inputValue.Multiline = true;
            inputValue.Name = "inputValue";
            inputValue.Size = new Size(200, 39);
            inputValue.TabIndex = 3;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(305, 107);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(60, 22);
            btnOk.TabIndex = 4;
            btnOk.Text = "입력(&A)";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += ClickBtnOk;
            // 
            // PosPicker
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Turquoise;
            ClientSize = new Size(3860, 2180);
            Controls.Add(btnOk);
            Controls.Add(inputValue);
            Controls.Add(labelPos);
            Controls.Add(border);
            Cursor = Cursors.Cross;
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "PosPicker";
            StartPosition = FormStartPosition.Manual;
            Text = "PosPicker";
            TransparencyKey = Color.Turquoise;

            Shown += AfterShown;
            MouseDown += OnMouseDownForPosPicker;
            MouseMove += OnMouseMoveForPosPicker;
            MouseUp += OnMouseUpForPosPicker;
            MouseClick += OnClickForPosPicker;
            KeyDown += OnKeyDownForPosPicker;
            KeyUp += OnKeyUpForPosPicker;
            inputValue.KeyDown += OnKeyDownForPosPicker;
            inputValue.KeyUp += OnKeyUpForPosPicker;
            inputValue.TextChanged += OnTextChanged;

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