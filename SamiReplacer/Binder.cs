using Newtonsoft.Json;
using System;

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

        public void startReplace(string files)
        {
            _.StartReplace(ParseJson<string[]>(files));
        }
        public void saveAndReplaceNext(int index, string text)
        {
            _.SaveAndReplaceNext(index, text);
        }

        public void exitAfterSaveSetting(string setting)
        {
            _.ExitAfterSaveSetting(setting);
        }
    }
}