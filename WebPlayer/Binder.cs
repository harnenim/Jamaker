namespace Jamaker
{
#pragma warning disable IDE1006 // 명명 스타일
    public class Binder(WebPlayer mainForm) : WebViewForm.BaseBinder(mainForm)
    {
        private readonly WebPlayer _ = mainForm;

        public void startResizeWindow(int type)
        {
            _.StartResizeWindow(type);
        }

        public void setTime(int time)
        {
            _.SetTime(time);
        }

        public void openFileDialog()
        {
            _.OpenFileDialog();
        }

        public void close()
        {
            _.Close();
        }
    }
}