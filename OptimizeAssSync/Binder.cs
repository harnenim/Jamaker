﻿namespace Jamaker
{
    class BaseBinder
    {
        private readonly MainForm _;

        public BaseBinder(MainForm webForm)
        {
            _ = webForm;
        }

        public void Focus(string target)
        {
            _.FocusWindow(target);
        }

        public void InitAfterLoad(string title)
        {
            _.InitAfterLoad(title);
        }
        public void SetVideoFile(string path)
        {
            _.CheckAndMakeFkf(path);
        }

        public void ShowDragging()
        {
            _.ShowDragging();
        }
        public void HideDragging()
        {
            _.HideDragging();
        }

        public void Alert(string target, string msg) { _.Alert(target, msg); }
        public void Confirm(string target, string msg) { _.Confirm(target, msg); }
    }

    class Binder : BaseBinder
    {
        private readonly MainForm _;

        public Binder(MainForm mainForm) : base(mainForm)
        {
            _ = mainForm;
        }

        public void DropFiles()
        {
            _.DropFiles();
        }

        public void CheckAndMakeFkf(string path)
        {
            _.CheckAndMakeFkf(path);
        }

        public void Save(string path, string text)
        {
            _.Save(path, text);
        }
    }
}
