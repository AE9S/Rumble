using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using DtmfDetection.NAudio;
using System.Threading;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using System.IO;
using NAudio.CoreAudioApi;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Rumble
{

    public struct RumbleConfigLine
    {
        public string ServerNumber;
        public string ChannelNumber;
        public string ServerURL;
        public string Port;
        public string UserName;
        public string Password;
        public string ChannelPath;
    } // RumbleConfig

    public partial class Form1 : Form
    {
        WaveInEvent micIn = new WaveInEvent { WaveFormat = new WaveFormat(8000, 32, 1) };
        LiveAudioDtmfAnalyzer analyzer;
        string CurrentDTMFCommand = string.Empty;
        string FinalDTMFCommand = string.Empty;
        System.Diagnostics.Process currentMumbleProcess;
        System.Diagnostics.ProcessStartInfo currentMumbleProcessStartInfo;
        string ResetURI = @"mumble://noUser@0.0.0.0:0/";
        int DeviceNo;
        List<RumbleConfigLine> MyConfigs = new List<RumbleConfigLine>();        
        enum DTMFCommandStates {
            ignore,
            isCommand,
            isDisconnect,
            isNotDisconnect,
            isLoadConfig,
            isAdminSettingORChannelChange,
            isAdminSetting,
            isChangeChannel,
            isAdminSettingNotFinal,
            isChannelChangeNoChannelNumber,
            isChannelChangeNotFinal,
            isAdminSettingFinal,
            isChannelChangeFinal
        } // DTMFCommandStates
        DTMFCommandStates MyState;
        string ConfigFilePath = @"C:\Users\kb\Desktop\";

        public delegate void MumbleMuteDelegate();
        MumbleMuteDelegate MyMuteDelegate = new MumbleMuteDelegate(MumbleMuteToggle);

        public Form1()
        {
            InitializeComponent();
            SetText("starting...");
            DeviceNo = 4;
            //DeviceNo = 6;
            micIn.DeviceNumber = DeviceNo;
            MyState = DTMFCommandStates.ignore;
            SpeakIt("Welcome to Rumble!");
            LoadConfig("0");            
            analyzer = new LiveAudioDtmfAnalyzer(micIn, forceMono: false);
            analyzer.DtmfToneStarted += Analyzer_DtmfToneStarted;
            analyzer.DtmfToneStopped += Analyzer_DtmfToneStopped;
            cmdStop.Enabled = false;
        } // Form1

        private void cmdListen_Click(object sender, EventArgs e)
        {
            cmdListen.Enabled = false;
            cmdStop.Enabled = true;
            analyzer.StartCapturing();
        } // cmdListen_Click
        
        private void Analyzer_DtmfToneStarted(DtmfToneStart obj)
        {
            SetText(string.Empty);

            // TODO: Block sound going to Mumble client
            MumbleMute();
            //Console.WriteLine(GetDTMFShortHand(obj.DtmfTone.Key.ToString()) + " key start");
        } // Analyzer_DtmfToneStarted

        private void Analyzer_DtmfToneStopped(DtmfToneEnd obj)
        {
            string currentDTMFChar = GetDTMFShortHand(obj.DtmfTone.Key.ToString());
            bool fallThrough = false;

            // GET FIRST CHARACTER
            // is this the beginning of a new command?
            if (string.IsNullOrEmpty(CurrentDTMFCommand))
            {
                // is this character initiating a new command? (#)
                if (currentDTMFChar == @"#")
                {
                    MyState = DTMFCommandStates.isCommand;
                    CurrentDTMFCommand = currentDTMFChar;
                    fallThrough = true;
                } // if
            } // if


            // GET SECOND CHARACTER
            // command has started, get 2nd char
            if (MyState == DTMFCommandStates.isCommand && fallThrough == false)
            {
                if (currentDTMFChar == @"*")
                {
                    // 2nd position is *, command is #*
                    MyState = DTMFCommandStates.isDisconnect;
                    CurrentDTMFCommand += currentDTMFChar;
                    FinalDTMFCommand = CurrentDTMFCommand;
                } // if
                // 2nd char is NOT *, so it MUST be 0-9
                else if (IsNumeric(currentDTMFChar))
                {
                    MyState = DTMFCommandStates.isNotDisconnect;
                    CurrentDTMFCommand += currentDTMFChar;
                    fallThrough = true;
                } // else if
                // illegal char passed, reset
                else
                {
                    ResetDTMFCommandState();
                } // else
            } // if


            // GET THIRD CHARACTER
            // command has started and is not a disconnect, get 3rd char
            if (MyState == DTMFCommandStates.isNotDisconnect && fallThrough==false)
            {
                // if 3rd char is * this is a config change
                if (currentDTMFChar == @"*")
                {
                    MyState = DTMFCommandStates.isLoadConfig;
                    CurrentDTMFCommand += currentDTMFChar;
                    FinalDTMFCommand = CurrentDTMFCommand;
                } // if
                // if 3rd char is 0-9, this is isAdminSettingORChannelChange
                else if (IsNumeric(currentDTMFChar))
                {
                    MyState = DTMFCommandStates.isAdminSettingORChannelChange;
                    CurrentDTMFCommand += currentDTMFChar;
                    fallThrough = true;
                } // else if
                  // illegal char passed, reset
                else
                {
                    ResetDTMFCommandState();
                } // else
            } // if


            // GET FOURTH CHARACTER
            //command has started, and is either Admin Setting or Channel Change, get 4th char
            if (MyState==DTMFCommandStates.isAdminSettingORChannelChange && fallThrough==false)
            {
                // if 4th char is # this is an Admin Setting change
                if (currentDTMFChar == @"#")
                {
                    MyState = DTMFCommandStates.isAdminSetting;
                    CurrentDTMFCommand += currentDTMFChar;
                    fallThrough = true;
                } // if
                // if 4th char is 0-9, this is a channel change
                else if (IsNumeric(currentDTMFChar))
                {
                    MyState = DTMFCommandStates.isChangeChannel;
                    CurrentDTMFCommand += currentDTMFChar;
                    fallThrough = true;
                } // else if
                // illegal char passed, reset
                else
                {
                    ResetDTMFCommandState();
                } // else
            } // if


            // GET FIFTH CHARACTER
            //command has started, and is Admin Setting, get 5th char
            if (MyState == DTMFCommandStates.isAdminSetting && fallThrough==false)
            {
                // Admin Setting change, 5th char is 0-9
                if (IsNumeric(currentDTMFChar))
                {
                    MyState = DTMFCommandStates.isAdminSettingNotFinal;
                    CurrentDTMFCommand += currentDTMFChar;
                    fallThrough = true;
                } // if
                // illegal char passed, reset
                else
                {
                    ResetDTMFCommandState();
                } // else
            } // if
            
            //command has started, and is Channel Change, get 5th char
            if (MyState == DTMFCommandStates.isChangeChannel && fallThrough == false)
            {
                // Admin Setting change, 5th char is #
                if (currentDTMFChar == @"#")
                {
                    MyState = DTMFCommandStates.isChannelChangeNoChannelNumber;
                    CurrentDTMFCommand += currentDTMFChar;
                    fallThrough = true;
                } // if
                // illegal char passed, reset
                else
                {
                    ResetDTMFCommandState();
                } // else
            } // if


            //GET SIXTH CHARACTER
            // command has started, is Admin Setting, get 6th char
            if (MyState == DTMFCommandStates.isAdminSettingNotFinal && fallThrough == false)
            {
                // Admin Setting change, 6th char is *
                if (currentDTMFChar == @"*")
                {
                    MyState = DTMFCommandStates.isAdminSettingFinal;
                    CurrentDTMFCommand += currentDTMFChar;
                    FinalDTMFCommand = CurrentDTMFCommand;
                    fallThrough = true;
                } // if
                // illegal char passed, reset
                else
                {
                    ResetDTMFCommandState();
                } // else
            } // if

            // command has started, is Channel Change, get 6th char
            if (MyState == DTMFCommandStates.isChannelChangeNoChannelNumber && fallThrough==false)
            {
                // Channel Change, 6th char is 0-9
                if (IsNumeric(currentDTMFChar))
                {
                    MyState = DTMFCommandStates.isChannelChangeNotFinal;
                    CurrentDTMFCommand += currentDTMFChar;
                    fallThrough = true;
                } // if
                // illegal char passed, reset
                else
                {
                    ResetDTMFCommandState();
                } // else
            } // if


            //GET SEVENTH CHARACTER
            if (MyState == DTMFCommandStates.isChannelChangeNotFinal && fallThrough==false)
            {
                // Channel Change, 7th char is *
                if (currentDTMFChar == @"*")
                {
                    MyState = DTMFCommandStates.isChannelChangeFinal;
                    CurrentDTMFCommand += currentDTMFChar;
                    FinalDTMFCommand = CurrentDTMFCommand;
                    fallThrough = true;
                } // if
                // illegal char passed, reset
                else
                {
                    ResetDTMFCommandState();
                } // else
            } // if

            Thread.Sleep(250);
            ProcessDTMFCommand(FinalDTMFCommand, MyState);
            SetText(currentDTMFChar + "-" + CurrentDTMFCommand);
            MumbleUnmute();
        } // Analyzer_DtmfToneStopped

        private void ProcessDTMFCommand(string DTMFCommand, DTMFCommandStates CommandState)
        {
            SetText("Processing Command -- " + DTMFCommand);

            // only proceed is there's a complete command to process
            if (!string.IsNullOrEmpty(DTMFCommand))
            {
                switch (CommandState)
                {
                    case DTMFCommandStates.ignore:
                    case DTMFCommandStates.isCommand:
                    case DTMFCommandStates.isNotDisconnect:
                    case DTMFCommandStates.isAdminSettingORChannelChange:
                    case DTMFCommandStates.isAdminSetting:
                    case DTMFCommandStates.isChangeChannel:
                    case DTMFCommandStates.isAdminSettingNotFinal:
                    case DTMFCommandStates.isChannelChangeNoChannelNumber:
                    case DTMFCommandStates.isChannelChangeNotFinal:
                        break;
                    case DTMFCommandStates.isDisconnect:
                        // disconnect
                        Disconnect();
                        ResetDTMFCommandState();
                        break;
                    case DTMFCommandStates.isLoadConfig:
                        // load config
                        // get config #
                        string configNumber = DTMFCommand.Substring(1, 1);
                        LoadConfig(configNumber);
                        ResetDTMFCommandState();
                        break;
                    case DTMFCommandStates.isAdminSettingFinal:
                        // change admin setting
                        // get admin setting number and setting value number
                        string adminSetting = DTMFCommand.Substring(1, 2);
                        string adminSettingValue = DTMFCommand.Substring(4, 1);
                        ChangeAdminSetting(adminSetting, adminSettingValue);
                        ResetDTMFCommandState();
                        break;
                    case DTMFCommandStates.isChannelChangeFinal:
                        // change channel
                        // get server number and channel number
                        string serverNumber = DTMFCommand.Substring(1, 3);
                        string channelNumber = DTMFCommand.Substring(5, 1);
                        ChangeChannel(serverNumber, channelNumber);
                        ResetDTMFCommandState();
                        break;
                    default:
                        break;
                } // switch

            } // if
        } // ProcessDTMFCommand

        private void ChangeChannel(string ServerNumber, string ChannelNumber)
        {
            SetText(string.Format("changing channel to server {0}, channel {1}", ServerNumber, ChannelNumber));

            if (ChannelNumber == "0")
            {
                LaunchMumble(ResetURI);
            } // if

            RumbleConfigLine matchingConfig = new RumbleConfigLine();

            // find matching config line
            foreach (RumbleConfigLine myLine in MyConfigs)
            {
                if (myLine.ServerNumber == ServerNumber)
                {
                    if (myLine.ChannelNumber == ChannelNumber)
                    {
                        matchingConfig = myLine;
                        break;
                    } // if
                } // if
            } // foreach

            // get URI from config based on server and channel number
            if (!string.IsNullOrEmpty(matchingConfig.ServerURL))
            {
                string mumbleURI = BuildMumbleURI(matchingConfig);
                LaunchMumble(mumbleURI);
                SpeakIt(string.Format("channel changed to server {0}, channel {1}.", ServerNumber, ChannelNumber));
            } // if
            else
            {
                SpeakIt("requested server and channel pair could not be found in the current config.");
            } // else
            
        } // Change channel

        private void ChangeAdminSetting(string AdminSetting, string AdminSettingValue)
        {
            SetText(string.Format("changing admin setting {0} to value {1}", AdminSetting, AdminSettingValue));

            // TODO: change setting

            SpeakIt(string.Format("changed admin setting {0} to value {1}", AdminSetting, AdminSettingValue));

        } // ChangeAdminSetting

        private void LaunchMumble(string CommandText)
        {
            // start new process
            currentMumbleProcess = new System.Diagnostics.Process();

            currentMumbleProcessStartInfo = new System.Diagnostics.ProcessStartInfo();
            currentMumbleProcessStartInfo.FileName = CommandText;

            currentMumbleProcess.StartInfo = currentMumbleProcessStartInfo;
            currentMumbleProcess.Start();
            Thread.Sleep(1000);
        } // LaunchMumble

        private string GetDTMFShortHand(string DTMFKey)
        {
            string retVal = string.Empty;
            switch (DTMFKey)
            {
                case "None":
                    retVal = "?";
                    break;
                case "Zero":
                    retVal = "0";
                    break;
                case "One":
                    retVal = "1";
                    break;
                case "Two":
                    retVal = "2";
                    break;
                case "Three":
                    retVal = "3";
                    break;
                case "Four":
                    retVal = "4";
                    break;
                case "Five":
                    retVal = "5";
                    break;
                case "Six":
                    retVal = "6";
                    break;
                case "Seven":
                    retVal = "7";
                    break;
                case "Eight":
                    retVal = "8";
                    break;
                case "Nine":
                    retVal = "9";
                    break;
                case "Star":
                    retVal = "*";
                    break;
                case "Hash":
                    retVal = "#";
                    break;
                case "A":
                    retVal = "A";
                    break;
                case "B":
                    retVal = "B";
                    break;
                case "C":
                    retVal = "C";
                    break;
                case "D":
                    retVal = "D";
                    break;
                default:
                    break;
            } // switch
            return retVal;
        } // GetDTMFShorthand

        delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.textBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.textBox1.Text = text;
            }
        } // SetText

        private void cmdStop_Click(object sender, EventArgs e)
        {
            cmdStop.Enabled = false;
            cmdListen.Enabled = true;
            analyzer.StopCapturing();
        } // cmdStop_Click

        private bool IsNumeric(string EvaluateString)
        {
            bool retVal = false;

            switch (EvaluateString)
            {
                case @"0":
                case @"1":
                case @"2":
                case @"3":
                case @"4":
                case @"5":
                case @"6":
                case @"7":
                case @"8":
                case @"9":
                    retVal = true;
                    break;
                default:
                    break;
            } // switch

            return retVal;
        } // IsNumeric

        private void Disconnect()
        {
            LaunchMumble(ResetURI);
            //SpeakIt("client disconnected");
            PlaySound(@"C:\Users\kb\Desktop\wavs\clientDisconnected.wav");
            
        } // Disconnect

        private void ResetDTMFCommandState()
        {
            CurrentDTMFCommand = string.Empty;
            FinalDTMFCommand = string.Empty;
            MyState = DTMFCommandStates.ignore;
        } // ResetDTMFCommandState

        private void LoadConfig(string ConfigNumber)
        {
            SetText(string.Format("Loading config {0}", ConfigNumber));

            // clear out old config
            MyConfigs = new List<RumbleConfigLine>();

            string configFileName = string.Format(@"rumbleConfig_{0}.csv", ConfigNumber);
            string filePath = string.Format("{0}{1}", ConfigFilePath, configFileName);

            StreamReader sr = new StreamReader(filePath);
            string line;
            string[] dataRow = new string[6];
            RumbleConfigLine thisRumbleConfigLine;

            while ((line = sr.ReadLine()) != null)
            {
                dataRow = line.Split(',');
                thisRumbleConfigLine = new RumbleConfigLine();
                thisRumbleConfigLine.ServerNumber = dataRow[0];
                thisRumbleConfigLine.ChannelNumber = dataRow[1];
                thisRumbleConfigLine.ServerURL = dataRow[2];
                thisRumbleConfigLine.Port = dataRow[3];
                thisRumbleConfigLine.UserName = dataRow[4];
                thisRumbleConfigLine.Password = dataRow[5];
                thisRumbleConfigLine.ChannelPath = dataRow[6];
                MyConfigs.Add(thisRumbleConfigLine);
            } // while

            SpeakIt(string.Format("Configuration file number {0} has been loaded.", ConfigNumber));

        } // LoadConfig
        
        private void PlaySound(string FileToPlay)
        {
            var waveReader = new WaveFileReader(FileToPlay);
            var waveOut = new WaveOut();
            waveOut.DeviceNumber = DeviceNo;

            // doesn't work for volume... :(
            //float myFloat = 0.1F;
            //waveOut.Volume = myFloat;

            waveOut.Init(waveReader);
            waveOut.Play();
            Thread.Sleep(2500);
        } // PlaySound

        private void SpeakIt(string TextToSpeak)
        {
            //SetText(string.Format("speaking text {0}", TextToSpeak));
            IWaveProvider provider = null;
            var stream = new MemoryStream();
            using (var synth = new SpeechSynthesizer())
            {
                //synth.SetOutputToAudioStream(stream,
                    //new SpeechAudioFormatInfo(44100, AudioBitsPerSample.Eight, AudioChannel.Mono));
                
                synth.SetOutputToWaveStream(stream);
                synth.Rate = -1;
                synth.Speak(TextToSpeak);
                stream.Seek(0, SeekOrigin.Begin);
                provider = new RawSourceWaveStream(stream, new WaveFormat(22000, 16, 1));
            }
            var waveOut = new WaveOut();
            waveOut.DeviceNumber = DeviceNo;
            waveOut.Init(provider);
            waveOut.Play();
        } // SpeakIt
        
        private string BuildMumbleURI(RumbleConfigLine ConfigLine)
        {
            string MumbleURI = string.Empty;

            string portToUse = ConfigLine.Port;
            // no port specified, use default
            if (string.IsNullOrEmpty(portToUse))
            {
                portToUse = "64738";
            } // if

            string channelPath;
            if (ConfigLine.ChannelPath.Substring(0,1) == @"/")
            {
                channelPath = ConfigLine.ChannelPath;
            } // if
            else
            {
                channelPath = @"/" + ConfigLine.ChannelPath;
            } // else

            if (string.IsNullOrEmpty(ConfigLine.Password))
            {
                // mumble://TestUser@server.com:23840/Open%20Talk/Subchannel%20A/?version=1.2.0
                MumbleURI = string.Format("mumble://{0}@{1}:{2}{3}", ConfigLine.UserName, ConfigLine.ServerURL, portToUse, channelPath);
            } // if
            else
            {
                MumbleURI = string.Format("mumble://{0}:{1}@{2}:{3}{4}", ConfigLine.UserName, ConfigLine.Password, ConfigLine.ServerURL, portToUse, channelPath);
                
            } // else

            return MumbleURI;

        } // BuildMumbleURI

        private static void MumbleMuteToggle()
        {
        } // MumbleMuteToggle

        private void cmdMute_Click(object sender, EventArgs e)
        {
            //MumbleMuteToggle();
            MyMuteDelegate();
        }

    } // public partial class Form1 : Form
} // namespace Rumble
