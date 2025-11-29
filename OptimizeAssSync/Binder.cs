using Newtonsoft.Json;
using System;

namespace Jamaker
{
#pragma warning disable IDE1006 // 명명 스타일
    public class Binder(MainForm mainForm) : WebViewForm.BaseBinder(mainForm)
    {
        private readonly MainForm _ = mainForm;

        public void dropFiles()
        {
            _.DropFiles();
        }
        public void setVideoFile(string path)
        {
            _.CheckAndMakeFkf(path);
        }

        public void save(string path, string text)
        {
            _.Save(path, text);
        }
    }
}