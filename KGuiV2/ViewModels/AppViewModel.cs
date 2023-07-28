using KGuiV2.Interop;
using KGuiV2.Helpers;

using System.Collections.Generic;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Input;
using System.Diagnostics;
using System.Linq;
using System;

namespace KGuiV2.ViewModels
{
    internal class AppViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The background update timer.
        /// </summary>
        readonly DispatcherTimer _updateTimer;

        /// <summary>
        /// The tick at which the ramtest started.
        /// </summary>
        long _ramtestStartTick;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The ramtest cpu cache modes.
        /// </summary>
        public IEnumerable<Ramtest.CpuCacheMode> RamtestCpuCacheModes { get; } = Enum.GetValues(typeof(Ramtest.CpuCacheMode)).Cast<Ramtest.CpuCacheMode>();

        /// <summary>
        /// The ramtest rng modes.
        /// </summary>
        public IEnumerable<Ramtest.RngMode> RamtestRngModes { get; } = Enum.GetValues(typeof(Ramtest.RngMode)).Cast<Ramtest.RngMode>();

        /// <summary>
        /// Indicates if the ramtest is currently running.
        /// </summary>
        public bool RamtestIsRunning { get; set; }

        /// <summary>
        /// The amount of system memory in megabytes to test.
        /// Nullable for text field check.
        /// </summary>
        public uint? RamtestMegabytes { get; set; } = 1000;

        /// <summary>
        /// The amount of threads to use for the test.
        /// Nullable for text field check.
        /// </summary>
        public uint? RamtestThreads { get; set; } = (uint)(Environment.ProcessorCount / 2);

        /// <summary>
        /// The task scope at which to stop testing.
        /// Nullable for text field check.
        /// </summary>
        public uint? RamtestTaskScope { get; set; } = 5000;

        /// <summary>
        /// The cpu cache mode to use for the ramtest.
        /// </summary>
        public Ramtest.CpuCacheMode RamtestCpuCacheMode { get; set; } = Ramtest.CpuCacheMode.Enabled;

        /// <summary>
        /// The rng mode to use for the ramtest.
        /// </summary>
        public Ramtest.RngMode RamtestRngMode { get; set; } = Ramtest.RngMode.Default;

        /// <summary>
        /// If the ramtest should also stress the cpu fpu.
        /// </summary>
        public bool RamtestStressFpu { get; set; }

        /// <summary>
        /// The speed at which the ramtest is running.
        /// </summary>
        public double RamtestSpeed { get; set; } = 0;

        /// <summary>
        /// The max recorded speed while ramtest is running.
        /// </summary>
        public double RamtestMaxSpeed { get; set; } = 0;

        /// <summary>
        /// The current ramtest test duration.
        /// </summary>
        public TimeSpan RamtestDuration { get; set; }

        /// <summary>
        /// The current ramtest coverage.
        /// </summary>
        public double RamtestCoverage { get; set; }

        /// <summary>
        /// The current ramtest coverage in percent.
        /// </summary>
        public double RamtestCoveragePercent => 100 * RamtestCoverage;

        /// <summary>
        /// The next full ramtest coverage in percent.
        /// </summary>
        public double RamtestNextFullCoveragePercent => 100 * Math.Floor(RamtestCoverage + 1.0);

        /// <summary>
        /// The approximate time it will take to reach the next full coverage percentage.
        /// </summary>
        public TimeSpan RamtestNextFullCoverageIn => TimeSpan.FromSeconds((int)(0.01 * (RamtestNextFullCoveragePercent - RamtestCoveragePercent) * RamtestMegabytes / RamtestSpeed));

        /// <summary>
        /// The approximate time it will take for the test to finish with the given <see cref="RamtestTaskScope"/>.
        /// </summary>
        public TimeSpan RamtestFinishedIn => TimeSpan.FromSeconds((int)(0.01 * (RamtestTaskScope - RamtestCoveragePercent) * RamtestMegabytes / RamtestSpeed));

        /// <summary>
        /// Indicates if the test should be stopped if <see cref="RamtestTaskScope"/> is reached.
        /// </summary>
        public bool RamtestStopOnTaskScope { get; set; } = true;

        /// <summary>
        /// Indicates if the test should be stopped if an error is detected.
        /// </summary>
        public bool RamtestStopOnError { get; set; } = true;

        /// <summary>
        /// Indicates if <see cref="Console.Beep"/> should be used if the test encounters an error and <see cref="RamtestStopOnError"/> is enabled.
        /// </summary>
        public bool RamtestBeepOnError { get; set; } = true;

        /// <summary>
        /// The amount of ramtest errors.
        /// </summary>
        public uint RamtestErrorCount { get; set; }

        /// <summary>
        /// Enable task scope box if enabled and test is not running.
        /// </summary>
        public bool RamtestAllowTaskScope { get; set; }

        /// <summary>
        /// The total amount of system memory.
        /// </summary>
        public double SystemMemoryTotal { get; set; }

        /// <summary>
        /// The amount of currently unused system memory.
        /// </summary>
        public double SystemMemoryFree { get; set; }

        /// <summary>
        /// The percentage of currently unused system memory.
        /// </summary>
        public double SystemMemoryFreePercentage => 100 * (SystemMemoryFree / SystemMemoryTotal);

        /// <summary>
        /// The total number of cpu threads.
        /// </summary>
        public uint SystemCpuThreads = (uint)Environment.ProcessorCount;

        /// <summary>
        /// Calculated test size from available memory.
        /// </summary>
        public uint AutoRamtestMegabytes { get; set; }

        /// <summary>
        /// Command for starting the ramtest.
        /// </summary>
        public ICommand RamtestStartCommand { get; }

        /// <summary>
        /// Command for stopping the ramtest.
        /// </summary>
        public ICommand RamtestStopCommand { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AppViewModel"/>.
        /// </summary>
        public AppViewModel()
        {
            LoadUserSettings();

            _updateTimer = new DispatcherTimer(DispatcherPriority.Background);
            _updateTimer.Interval = TimeSpan.FromMilliseconds(150);
            _updateTimer.Tick += UpdaterOnTick;
            _updateTimer.Start();

            RamtestStartCommand = new RelayCommand(CanStartRamtest, StartRamtest);
            RamtestStopCommand = new RelayCommand(CanStopRamtest, StopRamtest);
        }

        /// <summary>
        /// Gets called when the application window is closing.
        /// </summary>
        public void OnWindowClosing()
        {
            if (RamtestStopCommand.CanExecute(null))
                RamtestStopCommand.Execute(null);

            SaveUserSettings();
        }

        /// <summary>
        /// Saves all user settings.
        /// </summary>
        void SaveUserSettings()
        {
            Properties.Settings.Default.RamtestRngMode = (int)RamtestRngMode;
            Properties.Settings.Default.RamtestCpuCacheMode = (int)RamtestCpuCacheMode;
            Properties.Settings.Default.RamtestMegabytes = RamtestMegabytes;
            Properties.Settings.Default.RamtestThreads = RamtestThreads;
            Properties.Settings.Default.RamtestStopOnError = RamtestStopOnError;
            Properties.Settings.Default.RamtestStopOnTaskScope = RamtestStopOnTaskScope;
            Properties.Settings.Default.RamtestTaskScope = RamtestTaskScope;
            Properties.Settings.Default.RamtestBeepOnError = RamtestBeepOnError;
            Properties.Settings.Default.RamtestStressFpu = RamtestStressFpu;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Loads all user settings.
        /// </summary>
        void LoadUserSettings()
        {
            RamtestRngMode = (Ramtest.RngMode)Properties.Settings.Default.RamtestRngMode;
            RamtestCpuCacheMode = (Ramtest.CpuCacheMode)Properties.Settings.Default.RamtestCpuCacheMode;
            RamtestMegabytes = Properties.Settings.Default.RamtestMegabytes;
            RamtestThreads = Properties.Settings.Default.RamtestThreads;
            RamtestStopOnError = Properties.Settings.Default.RamtestStopOnError;
            RamtestStopOnTaskScope = Properties.Settings.Default.RamtestStopOnTaskScope;
            RamtestTaskScope = Properties.Settings.Default.RamtestTaskScope;
            RamtestBeepOnError = Properties.Settings.Default.RamtestBeepOnError;
            RamtestStressFpu = Properties.Settings.Default.RamtestStressFpu;
        }

        /// <summary>
        /// Checks if the ramtest can be stopped.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        bool CanStopRamtest(object? param)
            => RamtestIsRunning;

        /// <summary>
        /// Checks if the ramtest can be started.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        bool CanStartRamtest(object? param)
            => !RamtestIsRunning
            && RamtestMegabytes >= 1
            && RamtestMegabytes <= SystemMemoryFree
            && RamtestThreads > 0
            && RamtestThreads <= SystemCpuThreads
            && !(RamtestStopOnTaskScope && RamtestTaskScope.GetValueOrDefault() <= 0);

        /// <summary>
        /// Stops the ramtest.
        /// </summary>
        /// <param name="param"></param>
        void StopRamtest(object? param)
            => RamtestIsRunning = !Ramtest.StopTest();

        /// <summary>
        /// Configures and starts the ramtest.
        /// </summary>
        /// <param name="param"></param>
        void StartRamtest(object? param)
        {
            Ramtest.SetStressFpu(RamtestStressFpu);
            Ramtest.SetCpuCache(RamtestCpuCacheMode);
            Ramtest.SetRng(RamtestRngMode);

            if (RamtestStopOnTaskScope && (RamtestTaskScope <= 0 || RamtestTaskScope.ToString() == ""))
                RamtestStopOnTaskScope = false;

            RamtestMaxSpeed = 0;

            var started = RamtestIsRunning = Ramtest.StartTest(RamtestMegabytes.GetValueOrDefault(), RamtestThreads.GetValueOrDefault());
            if (started)
                _ramtestStartTick = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// The background updater method used for updating variables.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UpdaterOnTick(object? sender, EventArgs e)
        {
            var memoryStatusEx = new Kernel32.MemoryStatusEx();

            if (Kernel32.GlobalMemoryStatusEx(ref memoryStatusEx))
            {
                SystemMemoryTotal = memoryStatusEx.TotalPhys / 1024 / 1024;
                SystemMemoryFree = memoryStatusEx.AvailPhys / 1024 / 1024;
            }

            if (RamtestMegabytes == null || RamtestMegabytes.ToString() == "")
                RamtestMegabytes = 0;

            if (RamtestThreads == null || RamtestThreads.ToString() == "")
                RamtestThreads = 0;

            if (RamtestTaskScope == null || RamtestTaskScope.ToString() == "")
                RamtestTaskScope = 0;

            RamtestAllowTaskScope = (RamtestStopOnTaskScope) ? !RamtestIsRunning : false;

            if (RamtestIsRunning)
            {
                RamtestErrorCount = Ramtest.GetErrorCount();
                RamtestCoverage = Ramtest.GetCoverage();
                RamtestDuration = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - _ramtestStartTick);
                RamtestSpeed = RamtestCoverage * (RamtestMegabytes.GetValueOrDefault() / RamtestDuration.TotalSeconds);

                if (RamtestCoveragePercent > 10)
                    RamtestMaxSpeed = Math.Max(RamtestSpeed,RamtestMaxSpeed);

                if ((RamtestStopOnError && RamtestErrorCount > 0) || (RamtestStopOnTaskScope && RamtestTaskScope <= RamtestCoveragePercent))
                {
                    if (RamtestStopCommand.CanExecute(null))
                        RamtestStopCommand.Execute(null);

                    if (RamtestBeepOnError && RamtestErrorCount > 0)
                        Console.Beep(1000, 300);
                }
            }
        }
    }
}
