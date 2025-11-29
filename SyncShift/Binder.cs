using Newtonsoft.Json;
using System;

namespace Jamaker
{
#pragma warning disable IDE1006 // 명명 스타일
    public class Binder(MainForm mainForm) : WebViewForm.BaseBinder(mainForm)
    {
        private readonly MainForm _ = mainForm;

        public void exitAfterSaveSetting(string setting)
        {
            _.ExitAfterSaveSetting(setting);
        }
        public void openFileDialog(int type, bool withSaveSkf, bool withKf)
        {
            _.OpenFileDialog(type, withSaveSkf, withKf);
        }
        public void initOriginFiles(bool withSaveSkf)
        {
            _.InitOriginFiles(withSaveSkf);
        }
        public void dropOriginFile(bool withSaveSkf)
        {
            _.DropOriginFile(withSaveSkf);
        }
        public void dropTargetFile(bool withSaveSkf, bool withKf)
        {
            _.DropTargetFile(withSaveSkf, withKf);
        }
        public void selectAudio(string map, bool isOrigin, bool withSaveSkf, bool withKf)
        {
            _.SelectAudio(map, isOrigin, withSaveSkf, withKf);
        }
        public void calcShift(string ranges, string shifts)
        {
            _.CalcShift(ranges, shifts);
        }
        public void save(string result, string operation)
        {
            _.Save(result, operation);
        }
    }
}