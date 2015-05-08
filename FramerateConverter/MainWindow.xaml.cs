// ***********************************************************************
// Assembly         : FramerateConverter
// Author           : freevideofpsconverter@gmail.com
// Created          : 07-17-2014
//
// Last Modified By : freevideofpsconverter@gmail.com
// Last Modified On : 07-23-2014
// ***********************************************************************

#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using CommandLine;
using Microsoft.Win32;

#endregion

namespace FreeVideoFPSConverter
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private struct Command2Execute
        {
            public Command2Execute(string command, string parameter)
            {
                Command = command;
                Parameters = parameter;
            }

            public readonly string Command;
            public readonly string Parameters;
        };

        /// <summary>
        ///     The executable FFprobe.exe
        /// </summary>
        private const string ExecutableFFprobe = @"tools\FFprobe.exe";

        /// <summary>
        ///     The executable FFmpeg.exe
        /// </summary>
        private const string ExecutableFFmpeg = @"tools\FFmpeg.exe";

        /// <summary>
        ///     The executable FFprobe stream info parameters
        /// </summary>
        private const string ExecutableFFprobeStreamInfoParameters = @" ""{0}"" -v 0 -select_streams v -print_format flat -show_entries stream=r_frame_rate,width,height,duration";

        /// <summary>
        ///     The executable FFprobe frames info parameters
        /// </summary>
        private const string ExecutableFFprobeFramesInfoParameters = @" -show_frames -pretty ""{0}"" -v 0 -select_streams v -print_format flat -show_entries frame=pkt_size";

        /// <summary>
        ///     The FFprobe output width
        /// </summary>
        private const string FFprobeOutputWidth = @"streams.stream.0.width=";

        /// <summary>
        ///     The FFprobe output height
        /// </summary>
        private const string FFprobeOutputHeight = @"streams.stream.0.height=";

        /// <summary>
        ///     The FFprobe output duration
        /// </summary>
        private const string FFprobeOutputDuration = @"streams.stream.0.duration=""";

        /// <summary>
        ///     The FFprobe output framerate
        /// </summary>
        private const string FFprobeOutputFramerate = @"streams.stream.0.r_frame_rate=""";

        /// <summary>
        ///     The FFmpeg converter parameters for IVF (only Key Frames)
        /// </summary>
        private const string ExecutableFFmpegParametersOnlyKeyFramesIvf = @" -y -i ""{0}"" -c:v libvpx -b:v {2} -g 1 -keyint_min 1 -sc_threshold 1 ""{1}""";
        //private const string ExecutableFFmpegParametersOnlyKeyFramesIvf = @" -y -i ""{0}"" -c:v libvpx -g 1 -keyint_min 1 -sc_threshold 1 ""{1}""";

        /// <summary>
        ///     The FFmpeg converter parameters for IVF
        /// </summary>
        private const string ExecutableFFmpegParametersStandardIvf = @" -y -i ""{0}"" -c:v libvpx -b:v {2} ""{1}""";
        //private const string ExecutableFFmpegParametersStandardIvf = @" -y -i ""{0}"" -c:v libvpx ""{1}""";

        /// <summary>
        ///     The FFmpeg converter parameters for H.264 (only Key Frames)
        /// </summary>
        private const string ExecutableFFmpegParametersOnlyKeyFramesH264 = @" -y -i ""{0}"" -c:v libx264 -b:v {2} -g 1 -keyint_min 1 -sc_threshold 1 ""{1}""";
        //private const string ExecutableFFmpegParametersOnlyKeyFramesH264 = @" -y -i ""{0}"" -c:v libx264 -g 1 -keyint_min 1 -sc_threshold 1 ""{1}""";

        /// <summary>
        ///     The FFmpeg converter parameters for H.264
        /// </summary>
        private const string ExecutableFFmpegParametersStandardH264 = @" -y -i ""{0}"" -c:v libx264 -b:v {2} ""{1}""";
        //private const string ExecutableFFmpegParametersStandardH264 = @" -y -i ""{0}"" -c:v libx264 ""{1}""";

        /// <summary>
        ///     The FFmpeg converter parameters to extract an audio stream
        /// </summary>
        private const string ExecutableFFmpegParametersExtractAudio = @" -y -i ""{0}"" -vn -c:a libvorbis ""{1}""";

        /// <summary>
        ///     The FFmpeg converter parameters to mux video+audio to output file
        /// </summary>
        private const string ExecutableFFmpegParametersMuxVideoAndAudio = @" -y -i ""{0}"" -i ""{1}"" -c copy ""{2}""";

        /// <summary>
        ///     The media filter
        /// </summary>
        private const string MediaFilter = @"Video Files|*.mpg;*.avi;*.wmv;*.mov;*.mp4;*.h264;*.mkv;*.ivf;*.webm|All Files|*.*";

        // Using a DependencyProperty as the backing store for KeyFramesOnly.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KeyFramesOnlyProperty =
            DependencyProperty.Register("KeyFramesOnly", typeof (bool), typeof (MainWindow), new PropertyMetadata(false));

        // Using a DependencyProperty as the backing store for NoFlickerMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NoFlickerModeProperty =
            DependencyProperty.Register("NoFlickerMode", typeof (bool), typeof (MainWindow), new PropertyMetadata(false));

        // Using a DependencyProperty as the backing store for NoFpsReduce.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NoFpsReduceProperty =
            DependencyProperty.Register("NoFpsReduce", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        // Using a DependencyProperty as the backing store for KeepAudio.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty KeepAudioProperty =
            DependencyProperty.Register("KeepAudio", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        // Using a DependencyProperty as the backing store for TargetFramerate.  This enables animation, styling, binding, etc...
        /// <summary>
        ///     The target framerate property
        /// </summary>
        public static readonly DependencyProperty TargetFramerateProperty =
            DependencyProperty.Register("TargetFramerate", typeof (double), typeof (MainWindow), new PropertyMetadata(60.0));

        // Using a DependencyProperty as the backing store for SourceFilename.  This enables animation, styling, binding, etc...
        /// <summary>
        ///     The source filename property
        /// </summary>
        public static readonly DependencyProperty SourceFilenameProperty =
            DependencyProperty.Register("SourceFilename", typeof (string), typeof (MainWindow), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for TargetFilename.  This enables animation, styling, binding, etc...
        /// <summary>
        ///     The target filename property
        /// </summary>
        public static readonly DependencyProperty TargetFilenameProperty =
            DependencyProperty.Register("TargetFilename", typeof (string), typeof (MainWindow), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for OriginalFramerateText.  This enables animation, styling, binding, etc...
        /// <summary>
        ///     The original framerate property
        /// </summary>
        public static readonly DependencyProperty OriginalFramerateTextProperty =
            DependencyProperty.Register("OriginalFramerateText", typeof (string), typeof (MainWindow), new PropertyMetadata("From \"Unknown\" to"));

        /// <summary>
        ///     The command line opts
        /// </summary>
        private readonly Options _cmdLineOpts = new Options();

        /// <summary>
        ///     The working directory
        /// </summary>
        private readonly string _workingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools");

        /// <summary>
        ///     The _conversion canceled
        /// </summary>
        private bool _conversionCanceled;

        /// <summary>
        /// The _output frame count
        /// </summary>
        private long _outputFrameCount;

        /// <summary>
        ///     The progress bar initialized flag
        /// </summary>
        private bool _progressbarInitialized;

        /// <summary>
        ///     The running process
        /// </summary>
        private Process _runningProcess;

        /// <summary>
        /// The temp audio file
        /// </summary>
        private string _tempAudioFile = string.Empty;

        /// <summary>
        ///     The temp AVS file
        /// </summary>
        private string _tempAvsFile = string.Empty;

        /// <summary>
        /// The temp video file
        /// </summary>
        private string _tempVideoFile = string.Empty;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            TargetFramerate = 60.0;
            _outputFrameCount = 0;
            OriginalFramerateText = "From \"Unknown\" to ";

#if DEBUG
            SourceFilename = @"C:\tmp\input.mp4";
            TargetFilename = @"C:\tmp\output.ivf";
#else
            SourceFilename = string.Empty;
            TargetFilename = string.Empty;
#endif

            InitializeComponent();

            if (!Parser.Default.ParseArguments(Environment.GetCommandLineArgs(), _cmdLineOpts))
            {
                Application.Current.Shutdown((int) ErrorCodes.IllegalCommand);
            }

            if (!string.IsNullOrEmpty(_cmdLineOpts.SourceFile))
            {
                SourceFilename = _cmdLineOpts.SourceFile;
            }

            if (!string.IsNullOrEmpty(_cmdLineOpts.TargetFile))
            {
                TargetFilename = _cmdLineOpts.TargetFile;
            }

            KeyFramesOnly = _cmdLineOpts.KeyFramesOnly;
            NoFlickerMode = _cmdLineOpts.NoFlicker;
            NoFpsReduce = _cmdLineOpts.MinFrameRate;
            KeepAudio = _cmdLineOpts.KeepAudio;

            if (_cmdLineOpts.FramesPerSecond != null)
            {
                if (_cmdLineOpts.FramesPerSecond < SpinnerFrameRate.Minimum)
                {
                    if (_cmdLineOpts.BatchMode)
                    {
                        Application.Current.Shutdown((int) ErrorCodes.IllegalFps);
                    }

                    _cmdLineOpts.FramesPerSecond = SpinnerFrameRate.Minimum;
                }

                if (_cmdLineOpts.FramesPerSecond > SpinnerFrameRate.Maximum)
                {
                    if (_cmdLineOpts.BatchMode)
                    {
                        Application.Current.Shutdown((int) ErrorCodes.IllegalFps);
                    }

                    _cmdLineOpts.FramesPerSecond = SpinnerFrameRate.Maximum;
                }

                if (_cmdLineOpts.FramesPerSecond != null)
                {
                    TargetFramerate = (int) _cmdLineOpts.FramesPerSecond;
                }
            }


            // check acceptance of agreement
            RegistryKey keySoftware = Registry.CurrentUser.OpenSubKey("Software", true);

            if (keySoftware != null)
            {
                RegistryKey keyApplication = keySoftware.CreateSubKey("FreeVideoFPSConverter");

                if (keyApplication != null)
                {
                    if (keyApplication.GetValue("LicenseAccepted") == null)
                    {
                        if (ShowLicense())
                        {
                            keyApplication.SetValue("LicenseAccepted", "1");
                        }
                        else
                        {
                            Application.Current.Shutdown((int) ErrorCodes.LicenseNotAccepted);
                            return;
                        }
                    }
                }
            }

            if (_cmdLineOpts.BatchMode)
            {
                TextBoxSourceFilename.IsEnabled = false;
                ButtonBrowseSource.IsEnabled = false;
                TextBoxTargetFilename.IsEnabled = false;
                ButtonBrowseTarget.IsEnabled = false;
                CheckBoxKeyFrames.IsEnabled = false;
                SpinnerFrameRate.IsEnabled = false;
                ButtonConvert.IsEnabled = false;
                ButtonAbout.IsEnabled = false;

                Application.Current.Dispatcher.BeginInvoke(
                    (Action) (() => ButtonConvert_Click(ButtonConvert, null)));
            }

            if (!string.IsNullOrEmpty(SourceFilename))
            {
                GetVideoFileInformationAndUpdateFramerate();
            }
        }

        /// <summary>
        ///     Gets the version information.
        /// </summary>
        /// <value>The version information.</value>
        public string VersionInfo
        {
            get { return "Version " + Assembly.GetExecutingAssembly().GetName().Version; }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether debug mode is active.
        /// </summary>
        /// <value><c>true</c> if [debug mode]; otherwise, <c>false</c>.</value>
        public bool DebugMode { get; set; }

        public bool KeyFramesOnly
        {
            get { return (bool) GetValue(KeyFramesOnlyProperty); }
            set { SetValue(KeyFramesOnlyProperty, value); }
        }

        public bool NoFlickerMode
        {
            get { return (bool) GetValue(NoFlickerModeProperty); }
            set { SetValue(NoFlickerModeProperty, value); }
        }

        public bool NoFpsReduce
        {
            get { return (bool) GetValue(NoFpsReduceProperty); }
            set { SetValue(NoFpsReduceProperty, value); }
        }

        public bool KeepAudio
        {
            get { return (bool)GetValue(KeepAudioProperty); }
            set { SetValue(KeepAudioProperty, value); }
        }

        /// <summary>
        ///     Gets or sets the target framerate.
        /// </summary>
        /// <value>The target framerate.</value>
        public double TargetFramerate
        {
            get { return (double) GetValue(TargetFramerateProperty); }
            set { SetValue(TargetFramerateProperty, value); }
        }

        /// <summary>
        ///     Gets or sets the source filename.
        /// </summary>
        /// <value>The source filename.</value>
        public string SourceFilename
        {
            get { return (string) GetValue(SourceFilenameProperty); }
            set
            {
                SetValue(SourceFilenameProperty, value);

                if (!string.IsNullOrEmpty(value))
                {
                    LastSourceDirectory = Path.GetDirectoryName(value);
                }

                UpdateHeader();
            }
        }

        /// <summary>
        ///     Gets or sets the target filename.
        /// </summary>
        /// <value>The target filename.</value>
        public string TargetFilename
        {
            get { return (string) GetValue(TargetFilenameProperty); }
            set
            {
                SetValue(TargetFilenameProperty, value);
                if (!string.IsNullOrEmpty(value))
                {
                    LastTargetDirectory = Path.GetDirectoryName(value);
                }
            }
        }

        /// <summary>
        ///     Gets or sets the original framerate.
        /// </summary>
        /// <value>The original framerate.</value>
        public string OriginalFramerateText
        {
            get { return (string) GetValue(OriginalFramerateTextProperty); }
            set { SetValue(OriginalFramerateTextProperty, value); }
        }

        /// <summary>
        ///     Gets or sets the original framerate.
        /// </summary>
        /// <value>The original framerate.</value>
        public double OriginalFramerate { get; set; }

        /// <summary>
        ///     Gets or sets the duration of the original.
        /// </summary>
        /// <value>The duration of the original.</value>
        public double OriginalDuration { get; set; }

        /// <summary>
        ///     Gets or sets the width of the original.
        /// </summary>
        /// <value>The width of the original.</value>
        public int OriginalWidth { get; set; }

        /// <summary>
        ///     Gets or sets the height of the original.
        /// </summary>
        /// <value>The height of the original.</value>
        public int OriginalHeight { get; set; }

        /// <summary>
        ///     Gets or sets the original numerator.
        /// </summary>
        /// <value>The original numerator.</value>
        public int OriginalNumerator { get; set; }

        /// <summary>
        ///     Gets or sets the original denominator.
        /// </summary>
        /// <value>The original denonimator.</value>
        public int OriginalDenominator { get; set; }

        private static string LastSourceDirectory
        {
            get { return (string) Registry.GetValue(@"HKEY_CURRENT_USER\Software\FreeVideoFPSConverter", "LastSourceDir", string.Empty); }
            set { Registry.SetValue(@"HKEY_CURRENT_USER\Software\FreeVideoFPSConverter", "LastSourceDir", value); }
        }

        private static string LastTargetDirectory
        {
            get { return (string) Registry.GetValue(@"HKEY_CURRENT_USER\Software\FreeVideoFPSConverter", "LastTargetDir", string.Empty); }
            set { Registry.SetValue(@"HKEY_CURRENT_USER\Software\FreeVideoFPSConverter", "LastTargetDir", value); }
        }

        /// <summary>
        ///     Handles the Click event of the ButtonBrowseSource control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void ButtonBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select video file..",
                InitialDirectory = !string.IsNullOrEmpty(LastSourceDirectory) ? LastSourceDirectory : AppDomain.CurrentDomain.BaseDirectory,
                DefaultExt = ".avi",
                Filter = MediaFilter
            };

            if (true != openFileDialog.ShowDialog())
            {
                return;
            }

            SourceFilename = openFileDialog.FileName;

            GetVideoFileInformationAndUpdateFramerate();
        }

        /// <summary>
        ///     Handles the Click event of the ButtonBrowseTarget control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void ButtonBrowseTarget_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog {Title = "Select video file.."};

            if (!string.IsNullOrEmpty(LastTargetDirectory))
            {
                saveFileDialog.InitialDirectory = LastTargetDirectory;
            }
            else if (!string.IsNullOrEmpty(LastSourceDirectory))
            {
                saveFileDialog.InitialDirectory = LastSourceDirectory;
            }
            else
            {
                saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            saveFileDialog.DefaultExt = ".avi";
            saveFileDialog.Filter = MediaFilter;
            if (true == saveFileDialog.ShowDialog())
            {
                TargetFilename = saveFileDialog.FileName;
            }
        }

        /// <summary>
        ///     Executes the and get output.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>System.String.</returns>
        private static string ExecuteAndGetOutput(string filename, string parameters)
        {
            string output = string.Empty;
            string error = string.Empty;

            ProcessStartInfo processStartInfo = new ProcessStartInfo(filename, parameters)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Normal,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process process = new Process {StartInfo = processStartInfo};
            process.OutputDataReceived += (s, e) => { output += e.Data; };
            process.ErrorDataReceived += (s, e) => { error += e.Data; };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return output + error;
        }

        /// <summary>
        ///     Gets information from the video and updates the framerate in the GUI.
        /// </summary>
        /// <returns>System.Double.</returns>
        private double GetVideoFileInformationAndUpdateFramerate()
        {
            // initialize defaults
            double result = 0.0;
            OriginalDuration = 0.0;
            OriginalWidth = 0;
            OriginalHeight = 0;
            OriginalFramerate = 0;
            OriginalFramerateText = "From \"Unknown\" to ";
            OriginalNumerator = 60;
            OriginalDenominator = 1;

            _progressbarInitialized = false;

            string command = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExecutableFFprobe);
            string output = ExecuteAndGetOutput(command, string.Format(ExecutableFFprobeStreamInfoParameters, SourceFilename));

            if (string.IsNullOrEmpty(output))
            {
                AddToLog("Error: source filename " + SourceFilename + " could not be analyzed!");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.SourceFileNoAcceptable);
                }
                return result;
            }

            // extract frame rate
            int fpsPositionInString = output.IndexOf(FFprobeOutputFramerate, StringComparison.Ordinal);

            if (fpsPositionInString < 0)
            {
                AddToLog("Error: source filename " + SourceFilename + " could not be analyzed!");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.SourceFileNoAcceptable);
                }
                return result;
            }

            int startFpsPositionInString = fpsPositionInString + FFprobeOutputFramerate.Length;
            int endOfFpsPositionInString = startFpsPositionInString + 1;
            while (endOfFpsPositionInString < output.Length && output[endOfFpsPositionInString] != '\"')
            {
                ++endOfFpsPositionInString;
            }

            string framerate = output.Substring(startFpsPositionInString, endOfFpsPositionInString - startFpsPositionInString);

            if (string.IsNullOrEmpty(framerate))
            {
                AddToLog("Error: source filename " + SourceFilename + " could not be analyzed!");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.SourceFileNoAcceptable);
                }
                return result;
            }

            // handle the case that there is a "/" in the string, so the real value has to be calculated
            int positionOfDivider = framerate.IndexOf("/", StringComparison.Ordinal);
            if (positionOfDivider > 0)
            {
                try
                {
                    double upper = OriginalNumerator = int.Parse(framerate.Substring(0, positionOfDivider));
                    double lower = OriginalDenominator = int.Parse(framerate.Substring(positionOfDivider + 1));

                    result = upper/lower;

                    framerate = string.Format("{0:F2}", result);
                }
                catch (Exception)
                {
                    result = 0.0;
                    AddToLog("Error: Problem calculating frame rate from string: " + framerate);

                    if (_cmdLineOpts.BatchMode)
                    {
                        Application.Current.Shutdown((int) ErrorCodes.SourceFileNoAcceptable);
                    }
                }
            }
            else
            {
                OriginalNumerator = (int) result;
                OriginalDenominator = 1;
            }

            OriginalFramerate = result;
            OriginalFramerateText = "From " + ((Math.Abs(result) < 1.0) ? @"""Unknown""" : string.Format("{0:F2}", result)) + " FPS to ";

            // extract width
            {
                int widthPositionInString = output.IndexOf(FFprobeOutputWidth, StringComparison.Ordinal);

                if (widthPositionInString < 0)
                {
                    AddToLog("Error: source filename " + SourceFilename + " could not be analyzed!");

                    if (_cmdLineOpts.BatchMode)
                    {
                        Application.Current.Shutdown((int) ErrorCodes.SourceFileNoAcceptable);
                    }
                    return result;
                }

                int startWidthPositionInString = widthPositionInString + FFprobeOutputWidth.Length;
                int endOfWidthPositionInString = startWidthPositionInString + 1;
                while (endOfWidthPositionInString < output.Length && char.IsDigit(output[endOfWidthPositionInString]))
                {
                    ++endOfWidthPositionInString;
                }

                OriginalWidth = int.Parse(output.Substring(startWidthPositionInString, endOfWidthPositionInString - startWidthPositionInString));
            }

            // extract height
            {
                int heightPositionInString = output.IndexOf(FFprobeOutputHeight, StringComparison.Ordinal);

                if (heightPositionInString < 0)
                {
                    AddToLog("Error: source filename " + SourceFilename + " could not be analyzed!");

                    if (_cmdLineOpts.BatchMode)
                    {
                        Application.Current.Shutdown((int) ErrorCodes.SourceFileNoAcceptable);
                    }
                    return result;
                }

                int startHeightPositionInString = heightPositionInString + FFprobeOutputHeight.Length;
                int endOfHeightPositionInString = startHeightPositionInString + 1;
                while (endOfHeightPositionInString < output.Length && char.IsDigit(output[endOfHeightPositionInString]))
                {
                    ++endOfHeightPositionInString;
                }

                OriginalHeight = int.Parse(output.Substring(startHeightPositionInString, endOfHeightPositionInString - startHeightPositionInString));
            }

            // extract duration
            {
                int durationPositionInString = output.IndexOf(FFprobeOutputDuration, StringComparison.Ordinal);

                if (durationPositionInString < 0)
                {
                    AddToLog("Error: source filename " + SourceFilename + " could not be analyzed!");

                    if (_cmdLineOpts.BatchMode)
                    {
                        Application.Current.Shutdown((int) ErrorCodes.SourceFileNoAcceptable);
                    }
                    return result;
                }

                int startDurationPositionInString = durationPositionInString + FFprobeOutputDuration.Length;
                int endOfDurationPositionInString = startDurationPositionInString + 1;
                while (endOfDurationPositionInString < output.Length && output[endOfDurationPositionInString] != '\"')
                {
                    ++endOfDurationPositionInString;
                }

                OriginalDuration = double.Parse(output.Substring(startDurationPositionInString, endOfDurationPositionInString - startDurationPositionInString), CultureInfo.InvariantCulture);

                ProgressBarConversion.Minimum = 0.0;
                ProgressBarConversion.Maximum = 100.0;
                ProgressBarConversion.Value = 0.0f;

                _progressbarInitialized = true;
            }
            return result;
        }

        /// <summary>
        ///     Handles the Click event of the Button Convert control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void ButtonConvert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SourceFilename))
            {
                AddToLog("Error: source filename is empty");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.IllegalCommand);
                }
                return;
            }

            string sourceExt = Path.GetExtension(SourceFilename).ToLower();

            if (sourceExt != ".mpg" && sourceExt != ".avi" && sourceExt != ".wmv" && sourceExt != ".mov" && sourceExt != ".mp4" && sourceExt != ".h264" && sourceExt != ".mkv")
            {
                AddToLog("Error: source filename has illegal extension");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.IllegalCommand);
                }
                return;
            }

            if (string.IsNullOrEmpty(TargetFilename))
            {
                AddToLog("Error: target filename is empty");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.IllegalCommand);
                }
                return;
            }

            string targetDir = Path.GetDirectoryName(TargetFilename);

            if (targetDir == null || !Directory.Exists(targetDir))
            {
                AddToLog("Error: target filename contains illegal directory");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.IllegalCommand);
                }
                return;
            }

            string targetExt = Path.GetExtension(TargetFilename).ToLower();

            if (targetExt != ".mpg" && targetExt != ".avi" && targetExt != ".wmv" && targetExt != ".mov" && targetExt != ".mp4" && targetExt != ".h264" && targetExt != ".mkv" && targetExt != ".ivf" && targetExt != ".webm")
            {
                AddToLog("Error: target filename has illegal extension");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.IllegalCommand);
                }
                return;
            }


            if (!File.Exists(SourceFilename))
            {
                AddToLog("Error: source filename " + SourceFilename + " does not exist!");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.IllegalCommand);
                }
                return;
            }

            if (File.Exists(TargetFilename))
            {
                MessageBoxResult result;

                if (_cmdLineOpts.BatchMode && _cmdLineOpts.Overwrite)
                {
                    result = MessageBoxResult.Yes;
                }
                else
                {
                    result = MessageBox.Show(string.Format("Do you want to overwrite {0}?", TargetFilename), "Target File already exists", MessageBoxButton.YesNo);
                }

                if (result != MessageBoxResult.Yes)
                {
                    AddToLog("Error: User chose not to overwrite existing file: " + TargetFilename);

                    if (_cmdLineOpts.BatchMode)
                    {
                        Application.Current.Shutdown((int) ErrorCodes.TargetExists);
                    }
                    return;
                }
            }

            double sourceFramerate = GetVideoFileInformationAndUpdateFramerate();

            if (TargetFramerate < 10.0 || TargetFramerate > 200.0)
            {
                AddToLog("Error: Frame rate exceeds range 10-200!");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.IllegalFps);
                }
                return;
            }

            if (NoFpsReduce && TargetFramerate < OriginalFramerate)
            {
                TargetFramerate = OriginalFramerate;
            }

            ConvertVideo(sourceFramerate, TargetFramerate);
        }

        /// <summary>
        ///     Returns GGTs of a and b
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns>System.Int32.</returns>
        private static int Ggt(int a, int b)
        {
            int rest;
            do
            {
                rest = b%a;
                b = a;
                a = rest;
            } while (rest != 0);
            return b;
        }

        /// <summary>
        ///     Converts the double to fraction.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="num">The numerator.</param>
        /// <param name="den">The denominator.</param>
        private void ConvertDoubleToFraction(double value, out int num, out int den)
        {
            if (NoFlickerMode)
            {
                // get rid of the decimal places
                value = (int) (value + 0.5f);
                TargetFramerate = value*OriginalFramerate;
            }

            num = (int) (1000000*value);
            den = 1000000;

            int tempGgt = Ggt(num, den);

            num /= tempGgt;
            den /= tempGgt;
        }

        /// <summary>
        /// Gets the temporary filename.
        /// </summary>
        /// <param name="extension">The extension.</param>
        /// <returns>System.String.</returns>
        private string GetTempFilename(string extension)
        {
            string tempDir = Path.GetTempPath();
            string tempPath;
            Random rnd = new Random();

            for (; ; )
            {
                int index = rnd.Next(0, 100000);

                tempPath = Path.Combine(tempDir, string.Format("conv{0:D6}.{1}", index, extension));

                if (!File.Exists(tempPath))
                {
                    break;
                }
            }

            return tempPath;
        }

        /// <summary>
        /// Converts the video.
        /// </summary>
        /// <param name="sourceFramerate">The source framerate.</param>
        /// <param name="targetFramerate">The target framerate.</param>
        private void ConvertVideo(double sourceFramerate, double targetFramerate)
        {
            StopAnyRunningProcess();
            CleanUp();

            _outputFrameCount = 0;

            _tempAvsFile = GetTempFilename("avs");

            try
            {
                double speedChangeFactor = targetFramerate/sourceFramerate;
                int num, den;

                ConvertDoubleToFraction(speedChangeFactor, out num, out den);

                AddToLog(string.Format("Starting conversion process for {0} -> {1}, original fps {2:F2} to {3:F2} fps",
                    Path.GetFileName(SourceFilename),
                    Path.GetFileName(TargetFilename),
                    sourceFramerate,
                    TargetFramerate));

                using (StreamWriter outfile = new StreamWriter(_tempAvsFile))
                {
                    outfile.WriteLine("global threads=" + (1 + Environment.ProcessorCount));
                    outfile.WriteLine("global fps_num=\"" + num + "\"");
                    outfile.WriteLine("global fps_den=\"" + den + "\"");
                    outfile.WriteLine("global source_file=\"" + SourceFilename + "\"");

                    const string resourceName = "FreeVideoFPSConverter.Templates.FpsConversionInput.avs.template";

                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

                    if (stream != null)
                    {
                        StreamReader reader = new StreamReader(stream);
                        outfile.WriteLine(reader.ReadToEnd());
                    }
                }

                string ffmpegCommand = "\"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExecutableFFmpeg) + "\"";
                string extension = Path.GetExtension(TargetFilename) ?? ".h264";
                string targetExtension = extension.ToLower();
                string bitrateInKBits = CalculateBitRate(OriginalWidth, OriginalHeight, TargetFramerate) + "K";
                string ffmpegParametersString;
                bool patchRequired = false;

                switch (targetExtension)
                {
                    case ".webm":
                        ffmpegParametersString = KeyFramesOnly ? ExecutableFFmpegParametersOnlyKeyFramesIvf : ExecutableFFmpegParametersStandardIvf;
                        break;
                    case ".ivf":
                        ffmpegParametersString = KeyFramesOnly ? ExecutableFFmpegParametersOnlyKeyFramesIvf : ExecutableFFmpegParametersStandardIvf;
                        patchRequired = true;
                        break;
                    default:
                        ffmpegParametersString = KeyFramesOnly ? ExecutableFFmpegParametersOnlyKeyFramesH264 : ExecutableFFmpegParametersStandardH264;
                        break;
                }

                List<Command2Execute> commands2Execute = new List<Command2Execute>();

                string ffmpegParameters;

                // extract the audio part (if required)
                if (KeepAudio)
                {
                    _tempAudioFile = GetTempFilename("ogg");
                    _tempVideoFile = GetTempFilename(targetExtension.Substring(1));

                    ffmpegParameters = string.Format(ExecutableFFmpegParametersExtractAudio, SourceFilename, _tempAudioFile);
                    AddToLog("--- DEMUXING AUDIO ----------------------------------------------------");
                    AddToLog("Command:   " + ffmpegCommand);
                    AddToLog("Parameters:" + ffmpegParameters);

                    commands2Execute.Add(new Command2Execute(ffmpegCommand, ffmpegParameters));
                }
                else
                {
                    _tempAudioFile = string.Empty;
                    _tempVideoFile = string.Empty;
                }

                // convert the video
                ffmpegParameters = string.Format(ffmpegParametersString, _tempAvsFile, string.IsNullOrEmpty(_tempVideoFile) ? TargetFilename : _tempVideoFile, bitrateInKBits);
                AddToLog("--- CONVERTING VIDEO --------------------------------------------------");
                AddToLog("Command:   " + ffmpegCommand);
                AddToLog("Parameters:" + ffmpegParameters);
                commands2Execute.Add(new Command2Execute(ffmpegCommand, ffmpegParameters));

                // mux video and audio (if required)
                if (KeepAudio)
                {
                    ffmpegParameters = string.Format(ExecutableFFmpegParametersMuxVideoAndAudio, _tempVideoFile, _tempAudioFile, TargetFilename);
                    AddToLog("--- MUXING VIDEO AND AUDIO --------------------------------------------");
                    AddToLog("Command:   " + ffmpegCommand);
                    AddToLog("Parameters:" + ffmpegParameters);
                    commands2Execute.Add(new Command2Execute(ffmpegCommand, ffmpegParameters));
                }

                ExecuteCommandsAndReportProgress(commands2Execute);
                
                if (patchRequired)
                {
                    PatchFrameCountInIvfFile();
                }
            }
            catch (Exception e)
            {
                StopAnyRunningProcess();
                CleanUp();

                AddToLog("Error, Exception: " + e.Message);

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.Unknown);
                }
            }
        }

        /// <summary>
        ///     Processes the UI tasks.
        /// </summary>
        public static void ProcessUiTasks()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(delegate
            {
                frame.Continue = false;
                return null;
            }), null);
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// Executes the command and report progress.
        /// </summary>
        /// <param name="commands2Execute">The list of commands to execute.</param>
        private void ExecuteCommandsAndReportProgress(IEnumerable<Command2Execute> commands2Execute)
        {
            _conversionCanceled = false;

            // disable all buttons except Debug-Mode, About, Usage, and Cancel
            TextBoxSourceFilename.IsEnabled = false;
            ButtonBrowseSource.IsEnabled = false;
            TextBoxTargetFilename.IsEnabled = false;
            ButtonBrowseTarget.IsEnabled = false;
            CheckBoxKeyFrames.IsEnabled = false;
            SpinnerFrameRate.IsEnabled = false;
            ButtonConvert.IsEnabled = false;
            ButtonAbout.IsEnabled = false;

            foreach (Command2Execute command2Execute in commands2Execute)
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(command2Execute.Command, command2Execute.Parameters)
                {
                    WorkingDirectory = _workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                _runningProcess = new Process {StartInfo = processStartInfo};
                _runningProcess.OutputDataReceived += process_OutputDataReceived;
                _runningProcess.ErrorDataReceived += process_ErrorDataReceived;
                _runningProcess.Start();
                _runningProcess.PriorityClass = ProcessPriorityClass.BelowNormal;

                _runningProcess.BeginOutputReadLine();
                _runningProcess.BeginErrorReadLine();

                try
                {
                    while (!_runningProcess.WaitForExit(100))
                    {
                        ProcessUiTasks();
                        Thread.Sleep(100);
                    }
                }
                catch
                {
                    _conversionCanceled = true;
                    break;
                }
            }

            if (!_conversionCanceled)
            {
                if (CreateXmlDescriptionFile())
                {
                    if (CreateXmlFrameInfoFile())
                    {
                        AddToLog("Conversion done!");
                    }
                }
                CleanUp();

                if (_cmdLineOpts.BatchMode && Application.Current != null)
                {
                    Application.Current.Shutdown((int)ErrorCodes.Ok);
                }
            }

            // re-enable all UI elements that are disabled during conversion
            TextBoxSourceFilename.IsEnabled = true;
            ButtonBrowseSource.IsEnabled = true;
            TextBoxTargetFilename.IsEnabled = true;
            ButtonBrowseTarget.IsEnabled = true;
            CheckBoxKeyFrames.IsEnabled = true;
            SpinnerFrameRate.IsEnabled = true;
            ButtonConvert.IsEnabled = true;
            ButtonAbout.IsEnabled = true;
        }

        /// <summary>
        ///     Creates the XML description file.
        /// </summary>
        /// <returns><c>true</c> if no exception when creating XML file, <c>false</c> otherwise.</returns>
        private bool CreateXmlDescriptionFile()
        {
            try
            {
                string xmlTargetFilename = TargetFilename + ".desc.xml";

                XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("VideoInfo", new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"), new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
                        new XElement("InputFilename", SourceFilename),
                        new XElement("OutputFilename", TargetFilename),
                        new XElement("Width", OriginalWidth),
                        new XElement("Height", OriginalHeight),
                        new XElement("OriginalFramerateText", OriginalFramerate.ToString("N8    ", CultureInfo.InvariantCulture)),
                        new XElement("Framerate", TargetFramerate.ToString("N8", CultureInfo.InvariantCulture)),
                        new XElement("Duration", OriginalDuration.ToString("N8", CultureInfo.InvariantCulture))));

                doc.Save(xmlTargetFilename);

                AddToLog("XML description file written!");

                return true;
            }
            catch (Exception)
            {
                AddToLog("Error: XML description file cannot be written!");
            }
            return false;
        }

        /// <summary>
        ///     Handles the ErrorDataReceived event of the process control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataReceivedEventArgs" /> instance containing the event data.</param>
        private void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (null == e || null == e.Data)
            {
                return;
            }

            if (DebugMode)
            {
                AddToLog("Encoder:  " + e.Data);
            }

            HandleFFmpegOutput(e.Data);
        }

        /// <summary>
        ///     Handles the OutputDataReceived event of the process control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DataReceivedEventArgs" /> instance containing the event data.</param>
        private void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (null == e || null == e.Data)
            {
                return;
            }

            if (DebugMode)
            {
                AddToLog("Encoder:  " + e.Data);
            }

            HandleFFmpegOutput(e.Data);
        }

        /// <summary>
        ///     Handles the FFmpeg output.
        /// </summary>
        /// <param name="message">The message.</param>
        private void HandleFFmpegOutput(string message)
        {
            if (_progressbarInitialized && OriginalDuration > 0)
            {
                int posFrame = message.IndexOf("frame=", StringComparison.Ordinal);

                if (posFrame >= 0)
                {
                    posFrame += "frame=".Length;

                    int posEnd = posFrame + 1;
                    while (char.IsDigit(message[posEnd]) || char.IsWhiteSpace(message[posEnd]))
                    {
                        posEnd++;
                    }

                    string frameCountAsString = message.Substring(posFrame, posEnd - posFrame).Trim();
                    long.TryParse(frameCountAsString, out _outputFrameCount);
                }
                
                int posTime = message.IndexOf("time=", StringComparison.Ordinal);

                if (posTime >= 0)
                {
                    posTime += "time=".Length;

                    double seconds = 0.0;

                    try
                    {
                        seconds += 60.0*60.0*long.Parse(message.Substring(posTime, 2));
                        seconds += 60.0*long.Parse(message.Substring(posTime + 3, 2));
                        seconds += long.Parse(message.Substring(posTime + 6, 2));
                        seconds += 0.01*long.Parse(message.Substring(posTime + 9, 2));

                        double percentage = seconds*100.0/OriginalDuration;

                        if (percentage > 99.5)
                        {
                            percentage = 100.0;
                        }

                        Application.Current.Dispatcher.BeginInvoke(
                            (Action) (() =>
                            {
                                ProgressBarConversion.Value = percentage;
                                UpdateHeader(percentage);
                            }));
                    }
                    catch (Exception)
                    {
                        _progressbarInitialized = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Updates the header.
        /// </summary>
        /// <param name="percentage">The percentage.</param>
        private void UpdateHeader(double? percentage = null)
        {
            string newHeader = "Video Frame Rate Converter";

            if (!string.IsNullOrEmpty(SourceFilename))
            {
                newHeader += " - " + Path.GetFileName(SourceFilename);
            }

            if (percentage.HasValue)
            {
                newHeader += " - " + percentage.Value.ToString("0.00", CultureInfo.InvariantCulture) + @"%";
            }

            Title = newHeader;
        }

        /// <summary>
        ///     Handles the Click event of the Button Cancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            StopAnyRunningProcess();
            CleanUp();

            _conversionCanceled = true;

            if (_cmdLineOpts.BatchMode)
            {
                Application.Current.Shutdown((int) ErrorCodes.Canceled);
            }
        }

        /// <summary>
        ///     Stops any running process.
        /// </summary>
        private void StopAnyRunningProcess()
        {
            if (null != _runningProcess)
            {
                if (!_runningProcess.HasExited)
                {
                    try
                    {
                        _runningProcess.Kill();
                        AddToLog("Conversion process was stopped!");
                    }
                    catch (Exception e)
                    {
                        AddToLog("Error: Exception when trying to stop conversion process: " + e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="filenName">Name of the filen.</param>
        private void DeleteFile(string filenName)
        {
            for (int i = 0; i < 10; i++)
            {
                if (!string.IsNullOrEmpty(filenName) && File.Exists(filenName))
                {
                    try
                    {
                        AddToLog("Trying to delete: " + filenName);
                        File.Delete(filenName);
                        AddToLog("Deleted: " + filenName);
                        break;
                    }
                    catch (Exception e)
                    {
                        if (i > 0)
                        {
                            AddToLog("Error: Exception when trying to clean up: " + e.Message);
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        /// <summary>
        ///     Cleans up.
        /// </summary>
        private void CleanUp()
        {
            if(!string.IsNullOrEmpty(_tempVideoFile))
            {
                DeleteFile(_tempVideoFile);
                _tempVideoFile = string.Empty;
            }

            if(!string.IsNullOrEmpty(_tempAudioFile))
            {
                DeleteFile(_tempAudioFile);
                _tempAudioFile = string.Empty;
            }

            if(!string.IsNullOrEmpty(_tempAvsFile))
            {
                DeleteFile(_tempAvsFile);
                _tempAvsFile = string.Empty;
            }
        }

        /// <summary>
        ///     Adds message to ListBox asynchronous.
        /// </summary>
        /// <param name="message">The message.</param>
        private void AddToListBoxAsync(string message)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(
                    (Action) (() =>
                    {
                        ListBoxReport.Items.Add(message);
                        ListBoxReport.SelectedIndex = ListBoxReport.Items.Count - 1;
                        ListBoxReport.ScrollIntoView(ListBoxReport.SelectedItem);
                    }));
            }
        }

        /// <summary>
        ///     Adds message to log.
        /// </summary>
        /// <param name="message">The message.</param>
        private void AddToLog(string message)
        {
            AddToListBoxAsync(message);
            Trace.WriteLine(message);
        }

        /// <summary>
        ///     Handles the OnClosing event of the MainWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CancelEventArgs" /> instance containing the event data.</param>
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            StopAnyRunningProcess();
            CleanUp();

            _conversionCanceled = true;

            if (_cmdLineOpts.BatchMode)
            {
                Application.Current.Shutdown((int) ErrorCodes.Canceled);
            }
        }

        /// <summary>
        ///     Shows the license.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private static bool ShowLicense()
        {
            AboutWindow newWindow = new AboutWindow();
            newWindow.ShowDialog();

            return newWindow.DialogResult.HasValue && newWindow.DialogResult.Value;
        }

        /// <summary>
        ///     Handles the Click event of the ButtonAbout control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void ButtonAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow newWindow = new AboutWindow();
            newWindow.ShowDialog();

            if (!newWindow.DialogResult.HasValue || !newWindow.DialogResult.Value)
            {
                // remove acceptance of license
                RegistryKey keySoftware = Registry.CurrentUser.OpenSubKey("Software", true);

                if (keySoftware != null)
                {
                    RegistryKey keyApplication = keySoftware.CreateSubKey("FreeVideoFPSConverter");

                    if (keyApplication != null)
                    {
                        if (keyApplication.GetValue("LicenseAccepted") != null)
                        {
                            keyApplication.DeleteValue("LicenseAccepted");
                        }
                    }
                }

                Application.Current.Shutdown((int) ErrorCodes.Canceled);
            }
        }

        private void ButtonUsage_OnClick(object sender, RoutedEventArgs e)
        {
            UsageWindow newWindow = new UsageWindow();
            newWindow.ShowDialog();
        }

        /// <summary>
        ///     Creates the XML frame information file.
        /// </summary>
        /// <returns><c>true</c> if no exception when creating output <c>false</c> otherwise.</returns>
        private bool CreateXmlFrameInfoFile()
        {
            string command = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExecutableFFprobe);
            string output = ExecuteAndGetOutput(command, string.Format(ExecutableFFprobeFramesInfoParameters, TargetFilename));

            if (string.IsNullOrEmpty(output))
            {
                AddToLog("Error: Frame analysis failed for " + TargetFilename + "!");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.TargetFileCorrupt);
                }
                return true;
            }

            Regex rgx = new Regex("frames\\.frame\\.\\d+\\.pkt_size=\"(\\d+)\"");
            MatchCollection allMatches = rgx.Matches(output);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < allMatches.Count; ++i)
            {
                sb.Append("<Frame length=");
                sb.Append(allMatches[i].Groups[1].Value);
                sb.Append(" />");
                sb.Append(Environment.NewLine);
            }

            string xmlTargetFilename = TargetFilename + ".xml";

            try
            {
                File.WriteAllText(xmlTargetFilename, sb.ToString(), Encoding.UTF8);

                AddToLog("XML frame info file written!");

                return true;
            }
            catch (Exception)
            {
                AddToLog("Error: Frame analysis failed for " + TargetFilename + "!");

                if (_cmdLineOpts.BatchMode)
                {
                    Application.Current.Shutdown((int) ErrorCodes.TargetFileCorrupt);
                }
            }

            return false;
        }

        /// <summary>
        ///     Handles the PreviewDropSourceFilename event of the TextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DragEventArgs" /> instance containing the event data.</param>
        private void TextBox_PreviewDropSourceFilename(object sender, DragEventArgs e)
        {
            object text = e.Data.GetData(DataFormats.FileDrop);

            SourceFilename = string.Format("{0}", ((string[]) text)[0]);
        }

        /// <summary>
        ///     Handles the PreviewDropTargetFilename event of the TextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DragEventArgs" /> instance containing the event data.</param>
        private void TextBox_PreviewDropTargetFilename(object sender, DragEventArgs e)
        {
            object text = e.Data.GetData(DataFormats.FileDrop);

            TargetFilename = string.Format("{0}", ((string[]) text)[0]);
        }

        /// <summary>
        ///     Handles the PreviewDragEnter event of the TextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DragEventArgs" /> instance containing the event data.</param>
        private void TextBox_PreviewDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Calculates the bit rate:
        /// Kush Gauge bitrate calculation
        /// motion factor can be 1 (low), 2 (medium) or 4 (high motion)
        /// frame width * frame height * frame rate * motion factor * 0.07 /1000 = Kbps
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="framerate">The framerate.</param>
        /// <returns>Bitrate in KBits (int)</returns>
        private static int CalculateBitRate(int width, int height, double framerate)
        {

            int iMotionFactor = 2;
            if (width < 320 || height < 240)
            {
                iMotionFactor = 4;
            }

            double dbBitRate = width * height * framerate * 0.07 * iMotionFactor;
            dbBitRate /= 1000;  // convert the target bit rate to kilobits per second

            return 500 * (int)((dbBitRate + 499) / 500);
        }

        /// <summary>
        /// Patches the frame count in ivf file.
        /// </summary>
        private void PatchFrameCountInIvfFile()
        {
            try
            {
                using (FileStream fs = File.Open(TargetFilename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    //bytes 0-3    signature: 'DKIF'
                    //bytes 4-5    version (should be 0)
                    //bytes 6-7    length of header in bytes
                    //bytes 8-11   codec FourCC (e.g., 'VP80')
                    //bytes 12-13  width in pixels
                    //bytes 14-15  height in pixels
                    //bytes 16-19  frame rate
                    //bytes 20-23  time scale
                    //bytes 24-27  number of frames in file (*) 
                    //bytes 28-31  unused
                    byte[] numberOfFrames = new byte[4];

                    fs.Seek(24, SeekOrigin.Begin);
                    fs.Read(numberOfFrames, 0, 4);

                    if (numberOfFrames[0] == 0 && numberOfFrames[1] == 0 && numberOfFrames[2] == 0 && numberOfFrames[3] == 0)
                    {
                        // the frame count was not set, set it now
                        numberOfFrames[0] = (byte)(_outputFrameCount & 0xFF);
                        numberOfFrames[1] = (byte)((_outputFrameCount >> 8) & 0xFF);
                        numberOfFrames[2] = (byte)((_outputFrameCount >> 16) & 0xFF);
                        numberOfFrames[3] = (byte)((_outputFrameCount >> 24) & 0xFF);

                        fs.Seek(24, SeekOrigin.Begin);
                        fs.Write(numberOfFrames, 0, 4);
                    }
                }
            }
            catch (Exception)
            {
                AddToLog("Error: Cannot patch target file " + TargetFilename + "!");
            }
        }
    }
}