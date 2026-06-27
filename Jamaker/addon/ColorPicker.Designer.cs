namespace Jamaker.addon
{
    partial class ColorPicker
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
            labelColor = new Label();
            SuspendLayout();
            // 
            // border
            // 
            border.BackColor = Color.Black;
            border.Location = new Point(8, 10);
            border.Name = "border";
            border.Size = new Size(62, 22);
            border.TabIndex = 0;
            // 
            // labelColor
            // 
            labelColor.BackColor = Color.White;
            labelColor.Location = new Point(9, 11);
            labelColor.Name = "labelColor";
            labelColor.Size = new Size(60, 20);
            labelColor.TabIndex = 2;
            labelColor.Text = "#FFFFFF";
            labelColor.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // ColorPicker
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Turquoise;
            ClientSize = new Size(3860, 2180);
            Controls.Add(labelColor);
            Controls.Add(border);
            Cursor = Cursors.Cross;
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "ColorPicker";
            StartPosition = FormStartPosition.Manual;
            Text = "ColorPicker";
            TransparencyKey = Color.Turquoise;
            pixel = Graphics.FromImage(buffer);
            MouseMove += OnMouseMoveForColorPicker;
            MouseClick += OnMouseClickForColorPicker;
            KeyDown += OnKeyDownForColorPicker;
            labelColor.MouseMove += OnMouseMoveForColorPicker;
            border.MouseMove += OnMouseMoveForColorPicker;
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label border;
        private System.Windows.Forms.Label labelColor;
    }
}