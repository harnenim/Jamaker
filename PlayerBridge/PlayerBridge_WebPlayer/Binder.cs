namespace Jamaker
{
#pragma warning disable IDE1006 // 명명 스타일
    public class Binder(MainForm mainForm) : WebViewForm.BaseBinder(mainForm)
    {
        private readonly MainForm _ = mainForm;

        public void openFileByDrag()
        {
            this._.openFileByDrag();
        }
        public void openFileDialog()
        {
            this._.openFileDialog();
        }
        public void close()
        {
            this._.close();
        }
    }
}