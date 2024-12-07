using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Jamaker
{
    public class StreamAttr
    {
        public string map;
        public string type;
        public string language;
        public Dictionary<string, string> metadata = new Dictionary<string, string>();
    }
	
    public class VideoInfo
    {
    	public static string exePath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg");

        // 원래 키프레임도 보려고 했는데...
        // 생각보다 키프레임이 화면전환에 안 맞는 경우가 많음
    	public static bool withKeyframe = false;

        public string path;
        public int duration;
        public List<StreamAttr> streams;
        public WebProgress progress;
        public bool isSkf = false;

        public delegate void AfterRefreshInfo(VideoInfo video);

        public VideoInfo(string path)
        {
            this.path = path;
            isSkf = true;
        }
        public VideoInfo(string path, WebProgress progress)
        {
            this.path = path;
            this.progress = progress;
        }
        public void RefreshInfo(AfterRefreshInfo afterRefreshInfo)
        {
            RefreshVideoInfo();
            afterRefreshInfo(this);
        }
        
        private static Process GetProcess(string exe)
        {
            return GetProcess(exe, false);
        }
        private static Process GetProcess(string exe, bool withStandardError)
        {
        	Process proc = new Process();
        	proc.StartInfo.UseShellExecute = false;
        	proc.StartInfo.CreateNoWindow = true;
        	proc.StartInfo.RedirectStandardOutput = true;
        	proc.StartInfo.RedirectStandardError = withStandardError;
        	proc.StartInfo.FileName = Path.Combine(exePath, exe);
        	return proc;
        }

        private void RefreshVideoInfo()
        {
            Process proc = GetProcess("ffprobe.exe", true);
            proc.StartInfo.Arguments = "\"" + path + "\"";
            proc.Start();

            streams = new List<StreamAttr>();
            duration = 10000000;
            
            StreamAttr lastStream = null;
            bool isMetadata = false;
            string[] lines = proc.StandardError.ReadToEnd().Split(new char[] { '\n', '\r' });
            foreach (string line in lines)
            {
                //Console.WriteLine(line);
                if (line.Trim().Length == 0) continue;
                if (lastStream != null && line.Equals("    Metadata:"))
                {
                    isMetadata = true;
                    continue;
                }
                if (lastStream != null && isMetadata)
                {
                    string pattern = "^      (.*): (.*)$";
                    System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(line, pattern);
                    if (m.Success)
                    {
                        System.Text.RegularExpressions.GroupCollection groups = m.Groups;
                        lastStream.metadata[groups[1].Value.Trim()] = groups[2].Value.Trim();
                        continue;
                    }
                    else
                    {
                        isMetadata = false;
                    }
                }
                if (line.StartsWith("  Duration: "))
                {
                    string dur = line.Substring(12, line.IndexOf(',', 12) - 12);
                    string[] vs = dur.Split(':');

                    double v = 0;
                    for (int i = 0; i < vs.Length; i++)
                    {
                        v = v * 60 + Convert.ToDouble(vs[i]);
                    }
                    duration = (int)(v * 1000);
                    //Console.WriteLine("duration: {0}", duration);
                }
                else if (line.StartsWith("    Stream #"))
                {
                    string pattern = "Stream #([0-9]+:[0-9])+\\((.*)\\): (.*): ";
                    System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(line, pattern);
                    System.Text.RegularExpressions.GroupCollection groups = m.Groups;
                    streams.Add(lastStream = new StreamAttr()
                    {   map = groups[1].Value
                      , type = groups[3].Value.ToLower()
                      , language = groups[2].Value
                    });
                }
                else if (lastStream != null && line.StartsWith("      title           : "))
                {
                    //lastStream.metadata["title"] = line.Substring(24);
                }
            }
            proc.Close();

            Console.WriteLine("RefreshVideoLength");
            proc = GetProcess("ffprobe.exe");
            proc.StartInfo.Arguments = "-hide_banner -show_format -show_streams -pretty \"" + path + "\"";
            proc.Start();
            lines = proc.StandardOutput.ReadToEnd().Split(new char[] { '\n', '\r' });
            foreach (string line in lines)
            {
                Console.WriteLine(line);
            }
            proc.Close();
        }

        public List<int> audioTrackIndexes = new List<int>();
        public string audioMap = null;
        private List<double> sfs = null;
        private List<double> kfs = null;

        public void RefreshSkf()
        {
            RefreshSkf(false);
        }
        public void RefreshSkf(bool withSaveSkf)
        {
            Console.WriteLine("RefreshSkf");
            if (isSkf)
            {
                LoadSkf();
            }
            else
            {
                GetSfs();
                GetKfs();
                if (withSaveSkf) SaveSkf();
            }
        }

        public List<double> GetSfs()
        {
            if (sfs != null)
            {
                return sfs;
            }
            sfs = new List<double>();

            Process proc = GetProcess("ffmpeg.exe", true);
            proc.StartInfo.Arguments = string.Format("-i \"{0}\" -vn -map {1} -ar 44100 -ac 1 -f f32le -", path, audioMap);
            proc.ErrorDataReceived += new DataReceivedEventHandler(Proc_ErrorDataReceived);
            proc.Start();
            proc.BeginErrorReadLine();

            Stream stream = proc.StandardOutput.BaseStream;

            int didread;
            int offset = 0;
            byte[] buffer = new byte[sizeof(float) * (1024 + 1)];

            int length, residual_length;

            int count = 0;
            double sum = 0;

            progress.Set(0);

            double ratio = withKeyframe ? 0.3 : 1;

            while ((didread = stream.Read(buffer, offset, sizeof(float) * 1024)) != 0)
            {
                length = offset + didread;
                residual_length = length % sizeof(float);

                length -= residual_length;

                for (int index = 0; index < length; index += sizeof(float))
                {
                    float value = BitConverter.ToSingle(buffer, index);
                    sum += Math.Abs(value);

                    if (++count % 441 == 0)
                    {
                        sfs.Add(sum / count);
                        sum = 0;
                        if (count % 441000 == 0)
                        {
                            progress.Set(ratio * (count / (duration * 44.1)));
                        }
                    }
                }
            }
            proc.Close();

            progress.Set(ratio);

            return sfs;
        }
        public List<double> GetKfs()
        {
            if (kfs != null)
            {
                return kfs;
            }

            kfs = new List<double>();

            if (withKeyframe)
            {
            	Process proc = GetProcess("ffmpeg.exe", true);
                proc.StartInfo.Arguments = "-loglevel error -skip_frame nokey -select_streams v:0 -show_entries frame=pkt_pts_time -of csv=print_section=0 \"" + path + "\"";
                proc.ErrorDataReceived += new DataReceivedEventHandler(Proc_ErrorDataReceived);
                proc.Start();
                proc.BeginErrorReadLine();

                progress.Set(0.3);

                StreamReader sr = new StreamReader(proc.StandardOutput.BaseStream);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    try
                    {
                        double time = double.Parse(line);
                        Console.WriteLine(time);
                        kfs.Add(time);
                        if (true)
                            progress.Set(0.3 + (0.7 * (time * 1000 / duration)));
                    }
                    catch (Exception) { }
                }
                proc.Close();

                progress.Set(1);
            }

            return kfs;
        }

        public int SaveSkf()
        {
            int length = 0;

            string skfPath = path.Substring(0, path.Length - new FileInfo(path).Extension.Length) + ".skf";

            byte[] buffer;
            FileStream fs = new FileStream(skfPath, FileMode.Create);

            buffer = BitConverter.GetBytes(sfs.Count);
            fs.Write(buffer, 0, buffer.Length);
            length += buffer.Length;

            buffer = BitConverter.GetBytes(kfs.Count);
            fs.Write(buffer, 0, buffer.Length);
            length += buffer.Length;

            foreach (double sf in sfs)
            {
                buffer = BitConverter.GetBytes(sf);
                fs.Write(buffer, 0, buffer.Length);
                length += buffer.Length;
            }

            foreach (double kf in kfs)
            {
                buffer = BitConverter.GetBytes(kf);
                fs.Write(buffer, 0, buffer.Length);
                length += buffer.Length;
            }

            fs.Close();

            //Console.Write("save length: " + length);

            return length;
        }
        public void LoadSkf()
        {
            sfs = new List<double>();
            kfs = new List<double>();

            FileStream fs = new FileStream(path, FileMode.Open);

            int didread;
            byte[] buffer = new byte[sizeof(double) * (1024 + 1)];

            int length, residual_length;

            fs.Read(buffer, 0, sizeof(int) * 2);
            int sfsLength = BitConverter.ToInt32(buffer, 0);
            int kfsLength = BitConverter.ToInt32(buffer, sizeof(int));

            int count = 0;

            while ((didread = fs.Read(buffer, 0, sizeof(double) * 1024)) != 0)
            {
                length = didread;
                residual_length = length % sizeof(double);

                length -= residual_length;

                for (int index = 0; index < length; index += sizeof(double))
                {
                    double value = BitConverter.ToDouble(buffer, index);
                    if (count < sfsLength)
                        sfs.Add(value);
                    else if (count < sfsLength + kfsLength)
                        kfs.Add(value);
                    else
                    {

                    }
                    count++;
                }
            }

            fs.Close();

            //Console.WriteLine(sfs.Count + "/" + sfsLength + ", " + kfs.Count + "/" + kfsLength);
        }

        public static void Proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Console.WriteLine(e.Data);
                // do nothing
            }
        }
    }
}
