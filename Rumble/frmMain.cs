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
using System.Reflection;
using Utility;
using System.Timers;

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
        public string ServerNickname;
        public string ChannelNickname;
    } // RumbleConfigLine

      public partial class frmMain : Form
    {

        #region Variables and Constants

        string TraceString = string.Empty;
        WaveInEvent micIn = new WaveInEvent { WaveFormat = new WaveFormat(8000, 32, 1) };
        LiveAudioDtmfAnalyzer analyzer;
        string CurrentDTMFCommand = string.Empty;
        string FinalDTMFCommand = string.Empty;
        Process currentMumbleProcess;
        ProcessStartInfo currentMumbleProcessStartInfo;
        string ResetURI = @"mumble://noUser@0.0.0.0:0/";
        int DeviceInNo = 0;
        int DeviceOutNo = 0;
        List<RumbleConfigLine> MyConfigs = new List<RumbleConfigLine>();
        enum DTMFCommandStates
        {
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
        // TODO: put this on UI
        string ConfigFilePath = string.Empty;
        string MumbleExePath = Environment.ExpandEnvironmentVariables(@"%PROGRAMFILES%\mumble\mumble.exe");
        bool IsMuted = false;
        bool IsDeaf = false;
        bool StayMuted = false;
        string IDWaveFile = string.Empty;
        int IDTimerInterval = 6000;
        System.Timers.Timer MyTimer;
        delegate void SetTextCallback(string text);

        #endregion // Variables and Constants


        #region Event Handlers

        public frmMain()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                InitializeComponent();
                SetText("starting...");
                lblWavIDFile.Text = IDWaveFile;
                PopulateWaveInDevices();
                PopulateWaveOutDevices();

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch           
        } // frmMain

        private void cmdListen_Click(object sender, EventArgs e)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                cmdListen.Enabled = false;
                cmdStop.Enabled = true;
                StartIDTimerJob();
                analyzer.StartCapturing();
                SpeakIt("Now listening for commands.");

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch           
        } // cmdListen_Click

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                PlaySound(IDWaveFile);

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch            
        } // OnTimedEvent

        private void Analyzer_DtmfToneStarted(DtmfToneStart obj)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                SetText(string.Empty);
                MumbleMute();

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // Analyzer_DtmfToneStarted

        private void Analyzer_DtmfToneStopped(DtmfToneEnd obj)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

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
                    else
                    {
                        ResetDTMFCommandState();
                    } // else
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
                if (MyState == DTMFCommandStates.isNotDisconnect && fallThrough == false)
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
                if (MyState == DTMFCommandStates.isAdminSettingORChannelChange && fallThrough == false)
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
                if (MyState == DTMFCommandStates.isAdminSetting && fallThrough == false)
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
                if (MyState == DTMFCommandStates.isChannelChangeNoChannelNumber && fallThrough == false)
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
                if (MyState == DTMFCommandStates.isChannelChangeNotFinal && fallThrough == false)
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

                ProcessDTMFCommand(FinalDTMFCommand, MyState);
                SetText(currentDTMFChar + "-" + CurrentDTMFCommand);
                
                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // Analyzer_DtmfToneStopped

        private void cmdStop_Click(object sender, EventArgs e)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                cmdStop.Enabled = false;
                cmdListen.Enabled = true;
                analyzer.StopCapturing();
                StopIDTimerJob();
                KillMumble();

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch           
        } // cmdStop_Click

        private void cmdUseDevices_Click(object sender, EventArgs e)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                DeviceInNo = (int)comboWaveIn.SelectedValue;
                DeviceOutNo = (int)comboWaveOut.SelectedValue;

                SetText(string.Format("DeviceIn set to {0} -- DeviceOut set to {1}", DeviceInNo.ToString(), DeviceOutNo.ToString()));

                micIn.BufferMilliseconds = 100;
                micIn.NumberOfBuffers = 3;
                micIn.DeviceNumber = DeviceInNo;

                MyState = DTMFCommandStates.ignore;
                SpeakIt("Welcome to Rumble!");
                LoadConfig("0");
                
                analyzer = new LiveAudioDtmfAnalyzer(micIn, forceMono: false);
                analyzer.DtmfToneStarted += Analyzer_DtmfToneStarted;
                analyzer.DtmfToneStopped += Analyzer_DtmfToneStopped;
                cmdListen.Enabled = true;

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // cmdUseDevices_Click

        private void cmdSelectIDFile_Click(object sender, EventArgs e)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                openFileDialog1.Title = "Select the ID .wav file";
                openFileDialog1.Filter = @"Wav files(*.wav)|*.wav";
                openFileDialog1.ShowDialog();
                IDWaveFile = openFileDialog1.FileName;
                lblWavIDFile.Text = IDWaveFile;

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // cmdSelectIDFile_Click

        private void cmdMute_Click(object sender, EventArgs e)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                StringBuilder mySB = new StringBuilder();
                mySB.AppendLine(string.Format("Selected Input Device is {0} using Device Number {1}", 
                    comboWaveIn.SelectedItem, 
                    comboWaveIn.SelectedValue.ToString()));
                mySB.AppendLine(string.Format("Selected Output Device is {0} using Device Number {1}", 
                    comboWaveOut.SelectedItem, 
                    comboWaveOut.SelectedValue.ToString()));

                MessageBox.Show(mySB.ToString());

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch            
        } // cmdMute_Click

        private void cmdSelectConfigLocation_Click(object sender, EventArgs e)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);
                folderBrowserDialog1.Description = "Select the Config File location";
                folderBrowserDialog1.ShowDialog();
                ConfigFilePath = folderBrowserDialog1.SelectedPath;
                lblConfigFilePath.Text = ConfigFilePath;

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // cmdSelectConfigLocation_Click
        
        #endregion // Event Handlers


        #region Supporting Methods

        private void StartIDTimerJob()
        {

            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                int timerInterval;
                int.TryParse(txtTimerInterval.Text, out timerInterval);
                IDTimerInterval = timerInterval * 1000;

                MyTimer = new System.Timers.Timer();
                MyTimer.Interval = IDTimerInterval;
                MyTimer.AutoReset = true;
                MyTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
                MyTimer.Start();

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // StartIDTimerJob

        private void StopIDTimerJob()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                MyTimer.Stop();

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch            
        } // StopIDTimerJob

        private void ProcessDTMFCommand(string DTMFCommand, DTMFCommandStates CommandState)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

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

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // ProcessDTMFCommand

        private void ChangeChannel(string ServerNumber, string ChannelNumber)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                SetText(string.Format("changing channel to server {0}, channel {1}", ServerNumber, ChannelNumber));

                if (ChannelNumber == "0")
                {
                    LaunchMumble(ResetURI);
                    Thread.Sleep(500);
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

                    string serverName = string.Empty;
                    string channelName = string.Empty;

                    if (!string.IsNullOrEmpty(matchingConfig.ServerNickname))
                    {
                        serverName = matchingConfig.ServerNickname;
                    } // if
                    else
                    {
                        serverName = string.Format("server {0}", ServerNumber);
                    } // else

                    if (!string.IsNullOrEmpty(matchingConfig.ChannelNickname))
                    {
                        channelName = matchingConfig.ChannelNickname;
                    } // if
                    else
                    {
                        channelName = string.Format("channel {0}", ChannelNumber);
                    } // else

                    SpeakIt(string.Format("Channel changed to {0}, {1}.", serverName, channelName));
                } // if
                else
                {
                    SpeakIt("requested server and channel pair could not be found in the current config.");
                } // else

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // Change channel

        private void ChangeAdminSetting(string AdminSetting, string AdminSettingValue)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                SetText(string.Format("changing admin setting {0} to value {1}", AdminSetting, AdminSettingValue));

                switch (AdminSetting)
                {
                    case "00": // Mute / Unmute
                        switch (AdminSettingValue)
                        {
                            case "0":
                                StayMuted = true;
                                MumbleMute();
                                SpeakIt("muted");
                                break;
                            case "1":
                                StayMuted = false;
                                MumbleUnmute();
                                SpeakIt("un-muted");
                                break;
                            default:
                                break;
                        } // switch
                        break;
                    case "01": // Deaf / Undeaf
                        switch (AdminSettingValue)
                        {
                            case "0":
                                StayMuted = true;
                                MumbleDeaf();
                                SpeakIt("deaf");
                                break;
                            case "1":
                                StayMuted = false;
                                MumbleUndeaf();
                                SpeakIt("un deaf");
                                break;
                            default:
                                break;
                        } // switch
                        break;
                    // TODO: add settings
                    default:
                        break;
                } // switch

                //SpeakIt(string.Format("changed admin setting {0} to value {1}", AdminSetting, AdminSettingValue));

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // ChangeAdminSetting

        private void LaunchMumble(string CommandText)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                // start new process
                currentMumbleProcess = new System.Diagnostics.Process();

                currentMumbleProcessStartInfo = new System.Diagnostics.ProcessStartInfo();
                currentMumbleProcessStartInfo.FileName = CommandText;

                currentMumbleProcess.StartInfo = currentMumbleProcessStartInfo;
                currentMumbleProcess.Start();
                Thread.Sleep(500);

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch                   
        } // LaunchMumbleCommand

        private void IssueCommand(string CommandText, string Arguments)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = CommandText;
                startInfo.Arguments = Arguments;
                process.StartInfo = startInfo;
                process.Start();

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // IssueCommand

        private string GetDTMFShortHand(string DTMFKey)
        {
            string retVal = string.Empty;
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

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

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch

            return retVal;
        } // GetDTMFShorthand

        private void SetText(string text)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

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

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch      
        } // SetText

        private bool IsNumeric(string EvaluateString)
        {
            bool retVal = false;

            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

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

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch

            return retVal;
        } // IsNumeric

        private void Disconnect()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                LaunchMumble(ResetURI);
                SpeakIt("client disconnected");

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // Disconnect

        private void ResetDTMFCommandState()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                CurrentDTMFCommand = string.Empty;
                FinalDTMFCommand = string.Empty;
                MyState = DTMFCommandStates.ignore;
                MumbleUnmute();

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // ResetDTMFCommandState

        private void LoadConfig(string ConfigNumber)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

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
                    thisRumbleConfigLine.ServerNickname = dataRow[7];
                    thisRumbleConfigLine.ChannelNickname = dataRow[8];
                    MyConfigs.Add(thisRumbleConfigLine);
                } // while

                SpeakIt(string.Format("Configuration file number {0} has been loaded.", ConfigNumber));

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // LoadConfig

        private void PlaySound(string FileToPlay)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                var waveReader = new WaveFileReader(FileToPlay);
                var waveOut = new WaveOut();
                waveOut.DeviceNumber = DeviceOutNo;

                // doesn't work for volume... :(
                //float myFloat = 0.1F;
                //waveOut.Volume = myFloat;

                waveOut.Init(waveReader);
                waveOut.Play();
                Thread.Sleep(2500);

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // PlaySound

        private void SpeakIt(string TextToSpeak)
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                IWaveProvider provider = null;
                var stream = new MemoryStream();
                using (var synth = new SpeechSynthesizer())
                {
                    synth.SetOutputToAudioStream(stream,
                    new SpeechAudioFormatInfo(28000, AudioBitsPerSample.Eight, AudioChannel.Mono));

                    //synth.SetOutputToWaveStream(stream);
                    synth.Rate = -1;
                    
                    synth.Speak(TextToSpeak);
                    stream.Seek(0, SeekOrigin.Begin);
                    provider = new RawSourceWaveStream(stream, new WaveFormat(28000, 8, 1));
                }
                var waveOut = new WaveOut();
                waveOut.DeviceNumber = DeviceOutNo;
                waveOut.NumberOfBuffers = 250000;
                waveOut.Init(provider);
                waveOut.Play();
                waveOut.Dispose();
                
                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // SpeakIt

        private string BuildMumbleURI(RumbleConfigLine ConfigLine)
        {
            string MumbleURI = string.Empty;

            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                string portToUse = ConfigLine.Port;
                // no port specified, use default
                if (string.IsNullOrEmpty(portToUse))
                {
                    portToUse = "64738";
                } // if

                string channelPath;
                if (ConfigLine.ChannelPath.Substring(0, 1) == @"/")
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

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch

            return MumbleURI;
        } // BuildMumbleURI

        private void MumbleMute()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                IssueCommand(@MumbleExePath, @"rpc mute");
                IsMuted = true;

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // MumbleMute

        private void MumbleUnmute()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                if (!StayMuted)
                {
                    IssueCommand(@MumbleExePath, @"rpc unmute");
                    IsMuted = false;
                } // if

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch           
        } // MumbleUnmute

        private void MumbleDeaf()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                IssueCommand(@MumbleExePath, @"rpc deaf");
                IsDeaf = true;

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch            
        } // MumbleDeaf

        private void MumbleUndeaf()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                IssueCommand(@MumbleExePath, @"rpc undeaf");
                IsDeaf = false;

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch
        } // MumbleUndeaf

        private void PopulateWaveInDevices()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                // Bind combobox to dictionary
                Dictionary<string, int> myDevices = new Dictionary<string, int>();

                for (int deviceId = 0; deviceId < WaveIn.DeviceCount; deviceId++)
                {
                    var capabilitiesIn = WaveIn.GetCapabilities(deviceId);
                    myDevices.Add(capabilitiesIn.ProductName, deviceId);
                } // for

                comboWaveIn.DataSource = new BindingSource(myDevices, null);
                comboWaveIn.DisplayMember = "Key";
                comboWaveIn.ValueMember = "Value";

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch            

        } // PopulateWaveInDevices

        private void PopulateWaveOutDevices()
        {
            try
            {
                // logging
                MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
                MethodBeginLogging(myMethod);

                // Bind combobox to dictionary
                Dictionary<string, int> myDevices = new Dictionary<string, int>();

                for (int deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
                {
                    var capabilitiesOut = WaveOut.GetCapabilities(deviceId);
                    myDevices.Add(capabilitiesOut.ProductName, deviceId);
                } // for

                comboWaveOut.DataSource = new BindingSource(myDevices, null);
                comboWaveOut.DisplayMember = "Key";
                comboWaveOut.ValueMember = "Value";

                // logging
                MethodEndLogging(myMethod);
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch            

        } // PopulateWaveInDevices

        private void KillMumble()
        {
            MethodBase myMethod = new StackTrace().GetFrame(0).GetMethod();
            Process[] procs = null;

            try
            {
                // logging
                MethodBeginLogging(myMethod);

                procs = Process.GetProcessesByName("mumble");
                if (procs.Count<object>() > 0)
                {
                    Process mumbleProc = procs[0];
                    if (!mumbleProc.HasExited)
                    {
                        mumbleProc.Kill();
                    } // if
                } // if
            } // try
            catch (Exception ex)
            {
                UtilityMethods.ExceptionHandler(ex, TraceString);
            } // catch            
            finally
            {
                if (procs != null)
                {
                    foreach (Process p in procs)
                    {
                        p.Dispose();
                    } // foreach
                } // if

                // logging
                MethodEndLogging(myMethod);
            } // finally
        } // KillMumble

        #endregion // Supporting Methods


        #region Logging

        /// <summary>
        /// Generates trace strings and writes them to the debugger.
        /// </summary>
        /// <param name="CurrentMethod">A MethodBase object representing the calling method.</param>
        private void MethodBeginLogging(MethodBase CurrentMethod)
        {
            TraceString += @"|" + CurrentMethod.Name + "("; // Append method name to trace string
            IEnumerable<ParameterInfo> myParams = CurrentMethod.GetParameters(); // Get method parameter info
            foreach (ParameterInfo myParam in myParams) { TraceString += myParam.Name + ", "; } // Add parameter names to trace string
            if (TraceString.EndsWith(", ")) { TraceString = TraceString.Substring(0, (TraceString.Length - 2)); } // clean up trace string
            TraceString += ")"; // clean up trace string
            Debug.WriteLine(TraceString); // show trace string
        } // MethodBeginLogging

        /// <summary>
        /// Cleans up trace string.
        /// </summary>
        /// <param name="CurrentMethod">A MethodBase object representing the calling method.</param>
        private void MethodEndLogging(MethodBase CurrentMethod)
        {
            // Remove method name from end of trace string
            TraceString = TraceString.Substring(0, TraceString.LastIndexOf(@"|" + CurrentMethod.Name));
        } // MethodEndLogging

        #endregion // Logging
        
    } // //frmMain

} // namespace Rumble
