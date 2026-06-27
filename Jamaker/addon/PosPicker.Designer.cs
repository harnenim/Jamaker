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
            labelPolygon = new Label();
            SuspendLayout();
            // 
            // border
            // 
            border.BackColor = Color.Black;
            border.Location = new Point(8, 10);
            border.Name = "border";
            border.Size = new Size(102, 22);
            border.TabIndex = 0;
            border.MouseMove += OnMouseMoveForPosPicker;
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
            labelPos.MouseMove += OnMouseMoveForPosPicker;
            // 
            // inputValue
            // 
            inputValue.Location = new Point(84, 87);
            inputValue.Multiline = true;
            inputValue.Name = "inputValue";
            inputValue.Size = new Size(200, 39);
            inputValue.TabIndex = 3;
            inputValue.TextChanged += OnTextChanged;
            inputValue.KeyDown += OnKeyDownForPosPicker;
            inputValue.KeyUp += OnKeyUpForPosPicker;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(290, 86);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(60, 22);
            btnOk.TabIndex = 4;
            btnOk.Text = "입력(&A)";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += ClickBtnOk;
            // 
            // labelPolygon
            // 
            labelPolygon.AutoSize = true;
            labelPolygon.BackColor = Color.WhiteSmoke;
            labelPolygon.Location = new Point(294, 111);
            labelPolygon.Name = "labelPolygon";
            labelPolygon.Size = new Size(274, 60);
            labelPolygon.TabIndex = 5;
            labelPolygon.Text = "첫 점을 찍을 때 드래그하면 사각형이 생성됩니다.\nShift 키를 누르면 중간 점을 추가할 수 있습니다.\nCtrl 키를 누르면 곡선을 만들 수 있습니다.\n점을 우클릭하면 삭제됩니다.";
            // 
            // PosPicker
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Turquoise;
            ClientSize = new Size(3860, 2180);
            Controls.Add(labelPolygon);
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
            KeyDown += OnKeyDownForPosPicker;
            KeyUp += OnKeyUpForPosPicker;
            MouseClick += OnClickForPosPicker;
            MouseDown += OnMouseDownForPosPicker;
            MouseMove += OnMouseMoveForPosPicker;
            MouseUp += OnMouseUpForPosPicker;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label border;
        private System.Windows.Forms.Label labelPos;
        private TextBox inputValue;
        private Button btnOk;
        private Label labelPolygon;
    }
}