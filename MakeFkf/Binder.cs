namespace Jamaker
{
#pragma warning disable IDE1006 // 명명 스타일
    public class Binder(MainForm mainForm) : WebViewForm.BaseBinder(mainForm)
    {
        private readonly MainForm _ = mainForm;

        public void addFilesByDrag()
        {
            _.AddFilesByDrag();
        }

        public void makeFkfs(string files)
        {
            _.MakeFkfs(ParseJson<string[]>(files));
        }
    }
}