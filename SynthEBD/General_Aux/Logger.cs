﻿using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace SynthEBD
{
    public sealed class Logger : INotifyPropertyChanged
    {
        private static Logger instance;
        private static object lockObj = new Object();

        public event PropertyChangedEventHandler PropertyChanged;

        public VM_RunButton RunButton { get; set; }
        public string StatusString { get; set; }
        public string LogString { get; set; }

        public SolidColorBrush StatusColor { get; set; }

        public SolidColorBrush ReadyColor = new SolidColorBrush(Colors.Green);
        public SolidColorBrush WarningColor = new SolidColorBrush(Colors.Yellow);
        public SolidColorBrush ErrorColor = new SolidColorBrush(Colors.Red);
        public string ReadyString = "Ready To Patch";

        public DateTime PatcherExecutionStart { get; set; }

        System.Windows.Threading.DispatcherTimer UpdateTimer { get; set; }
        System.Diagnostics.Stopwatch EllapsedTimer { get; set; }

        public class NPCReport
        {
            public NPCReport(NPCInfo npcInfo)
            {
                NameString = GetNPCLogReportingString(npcInfo.NPC);
                LogCurrentNPC = false;
                SaveCurrentNPCLog = false;
                RootElement = null;
                CurrentElement = null;
                ReportElementHierarchy = new Dictionary<XElement, XElement>();
            }
            public string NameString { get; set; }
            public bool LogCurrentNPC { get; set; }
            public bool SaveCurrentNPCLog { get; set; }
            public System.Xml.Linq.XElement RootElement { get; set; }
            public System.Xml.Linq.XElement CurrentElement { get; set; }
            public Dictionary<System.Xml.Linq.XElement, System.Xml.Linq.XElement> ReportElementHierarchy { get; set; }
            public int CurrentLayer;
        }

        private Logger()
        {
            this.StatusColor = this.ReadyColor;
            this.StatusString = this.ReadyString;
            this.UpdateTimer = new System.Windows.Threading.DispatcherTimer();
            this.EllapsedTimer = new System.Diagnostics.Stopwatch();
        }

        public static Logger Instance
        {
            get
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = new Logger();
                    }
                }
                return instance;
            }
        }

        public static void LogMessage(string message)
        {
            Instance.LogString += message + Environment.NewLine;
        }

        public static void TriggerNPCReporting(NPCInfo npcInfo)
        {
            npcInfo.Report.LogCurrentNPC = true;
        }

        public static void TriggerNPCReportingSave(NPCInfo npcInfo)
        {
            if (npcInfo.Report.LogCurrentNPC)
            {
                npcInfo.Report.SaveCurrentNPCLog = true;
            }
        }

        public static void InitializeNewReport(NPCInfo npcInfo)
        {
            if (npcInfo.Report.LogCurrentNPC)
            {
                npcInfo.Report.RootElement = new XElement("Report");
                npcInfo.Report.CurrentElement = npcInfo.Report.RootElement;
                npcInfo.Report.ReportElementHierarchy = new Dictionary<XElement, XElement>();

                LogReport("Patching NPC " + npcInfo.Report.NameString, false, npcInfo);
            }
        }

        public static void OpenReportSubsection(string header, NPCInfo npcInfo)
        {
            if (npcInfo.Report.LogCurrentNPC)
            {
                var newElement = new XElement(header);
                npcInfo.Report.ReportElementHierarchy.Add(newElement, npcInfo.Report.CurrentElement);
                npcInfo.Report.CurrentElement.Add(newElement);
                npcInfo.Report.CurrentElement = newElement;
            }
        }

        public static void LogReport(string message, bool triggerSave, NPCInfo npcInfo) // detailed operation log; not reflected on screen
        {
            if (npcInfo.Report.LogCurrentNPC)
            {
                AddStringToReport(npcInfo.Report.CurrentElement, message);

                if (triggerSave)
                {
                    npcInfo.Report.SaveCurrentNPCLog = true;
                }
            }
        }

        private static void AddStringToReport(XElement element, string value)
        {
            var split = value.Trim().Split(Environment.NewLine);

            if (element.Value.Any())
            {
                element.Add(Environment.NewLine);
            }

            foreach (var item in split)
            {
                element.Add(item);
                element.Add(Environment.NewLine);
            }
        }

        public static void CloseReportSubsection(NPCInfo npcInfo)
        {
            if (npcInfo.Report.LogCurrentNPC)
            {
                npcInfo.Report.CurrentElement = npcInfo.Report.ReportElementHierarchy[npcInfo.Report.CurrentElement];
            }
        }

        public static void CloseReportSubsectionsTo(string label, NPCInfo npcInfo)
        {
            if (npcInfo.Report.LogCurrentNPC)
            {
                while (npcInfo.Report.CurrentElement.Name != label)
                {
                    npcInfo.Report.CurrentElement = npcInfo.Report.ReportElementHierarchy[npcInfo.Report.CurrentElement];
                }
            }
        }

        public static void SaveReport(NPCInfo npcInfo)
        {
            if (npcInfo.Report.LogCurrentNPC)
            {
                if (npcInfo.Report.SaveCurrentNPCLog)
                {
                    string outputFile = System.IO.Path.Combine(PatcherSettings.Paths.LogFolderPath, Logger.Instance.PatcherExecutionStart.ToString("yyyy-MM-dd-HH-mm", System.Globalization.CultureInfo.InvariantCulture), npcInfo.Report.NameString + ".xml");

                    XDocument output = new XDocument();
                    output.Add(npcInfo.Report.RootElement);

                    Task.Run(() => PatcherIO.WriteTextFile(outputFile, FormatLogStringIndents(output.ToString())));
                }
            }
        }

        private static string FormatLogStringIndents(string s)
        {
            int indent = 0;

            s = s.Replace("><", ">" + Environment.NewLine + "<");

            string[] split = s.Split(Environment.NewLine);
            for (int i = 0; i < split.Length; i++)
            {
                if (split[i].Trim().StartsWith("</"))
                {
                    indent--;
                    split[i] = Indent(split[i], indent);
                }
                else if (split[i].Trim().StartsWith('<'))
                {
                    split[i] = Indent(split[i], indent);
                    indent++;
                }
                else
                {
                    split[i] = Indent(split[i], indent);
                }
            }

            return string.Join(Environment.NewLine, split);
        }

        private static string Indent(string s, int count)
        {
            for (int i = 0; i < count; i++)
            {
                s = "\t" + s;
            }
            return s;
        }

        private class Utf8StringWriter : System.IO.StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }

        public static void LogError(string error)
        {
            Instance.LogString += error + Environment.NewLine;
        }
        public static string SpreadFlattenedAssetPack(FlattenedAssetPack ap, int index, bool indentAtIndex)
        {
            string spread = Environment.NewLine;
            for (int i = 0; i < ap.Subgroups.Count; i++)
            {
                if (indentAtIndex && i == index) { spread += "\t"; }
                spread += i + ": [" + String.Join(',', ap.Subgroups[i].Select(x => x.Id)) + "]" + Environment.NewLine;
            }
            return spread;
        }

        public static void LogErrorWithStatusUpdate(string error, ErrorType type)
        {
            Instance.LogString += error + Environment.NewLine;
            Instance.StatusString = error;
            switch (type)
            {
                case ErrorType.Warning: Instance.StatusColor = Instance.WarningColor; break;
                case ErrorType.Error: Instance.StatusColor = Instance.ErrorColor; break;
            }
        }

        public static void UpdateStatus(string message, bool triggerWarning)
        {
            Instance.StatusString = message;
            if (triggerWarning)
            {
                Instance.StatusColor = Instance.WarningColor;
            }
        }

        public static void TimedNotifyStatusUpdate(string error, ErrorType type, int durationSec)
        {
            LogErrorWithStatusUpdate(error, type);

            var t = Task.Factory.StartNew(() =>
            {
                Task.Delay(durationSec * 1000).Wait();
            });
            t.Wait();
            ClearStatusError();
        }

        public static void CallTimedNotifyStatusUpdateAsync(string error, ErrorType type, int durationSec)
        {
            Task.Run(() => TimedNotifyStatusUpdateAsync(error, type, durationSec));
        }
        /*
        public async Task CallTimedNotifyStatusUpdateAsync(string error, ErrorType type, int durationSec)
  => await TimedNotifyStatusUpdateAsync(error, type, durationSec);*/

        private static async Task TimedNotifyStatusUpdateAsync(string error, ErrorType type, int durationSec)
        {
            LogErrorWithStatusUpdate(error, type);

            // Await the Task to allow the UI thread to render the view
            // in order to show the changes     
            await Task.Delay(durationSec * 1000);

            ClearStatusError();
        }

        public static void ClearStatusError()
        {
            Instance.StatusString = Instance.ReadyString;
            Instance.StatusColor = Instance.ReadyColor;
        }
        public static void StartTimer()
        {
            Instance.UpdateTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background, System.Windows.Application.Current.Dispatcher); // arguments here are forcing the dispatcher to run on the UI thread (otherwise UpdateTimer.Tick fires on a different thread and gets missed by the UI, so the event handler is never called).
            Instance.EllapsedTimer = new System.Diagnostics.Stopwatch();
            Instance.UpdateTimer.Interval = TimeSpan.FromSeconds(1);
            Instance.UpdateTimer.Tick += timer_Tick;
            Instance.UpdateTimer.Start();
            Instance.EllapsedTimer.Start();
        }

        public static void StopTimer()
        {
            Instance.EllapsedTimer.Stop();
            Instance.UpdateTimer.Stop();
        }

        private static void timer_Tick(object sender, EventArgs e)
        {
            UpdateStatus("Patching: " + GetEllapsedTime(), false);
        }

        public static string GetEllapsedTime()
        {
            TimeSpan ts = Instance.EllapsedTimer.Elapsed;
            return string.Format("{0:D2}:{1:D2}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds);
        }

        public static string GetNPCLogNameString(INpcGetter npc)
        {
            return npc.Name?.String + " | " + npc.EditorID + " | " + npc.FormKey.ToString();
        }

        public static string GetNPCLogReportingString(INpcGetter npc)
        {
            return npc.Name?.String + " (" + npc.EditorID + ") " + npc.FormKey.ToString().Replace(':', '-');
        }
    }

    public enum ErrorType
    {
        Warning,
        Error
    }
}
