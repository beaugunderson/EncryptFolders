using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using BeauGunderson.Extensions;

using EncryptFolders.Config;

namespace EncryptFolders.Windows
{
    public partial class MainWindow
    {
        private Task _encryptionThread;

        private Timer _updateTimer;
        private Timer _showWindowTimer;

        private CancellationTokenSource _encryptionThreadCancellation;

        private int _skipped;
        private int _errors;
        private int _ignored;
        private int _encrypted;

        private string _file;

        private DateTime _lastUpdate;

        private bool DRY_RUN = false;

        private static readonly Color[] LogColors = new[]
        {
            Colors.DimGray,
            Colors.Black,
            Colors.DarkGray
        };

        #region Dependency Properties
        public static readonly DependencyProperty CurrentFileProperty =
            DependencyProperty.Register("CurrentFile",
                typeof (string),
                typeof (MainWindow),
                new PropertyMetadata(default(string)));

        public string CurrentFile
        {
            get
            {
                return (string)GetValue(CurrentFileProperty);
            }

            set
            {
                SetValue(CurrentFileProperty, value);
            }
        }

        public static readonly DependencyProperty CurrentDirectoryProperty =
            DependencyProperty.Register("CurrentDirectory", 
                typeof(string), 
                typeof(MainWindow), 
                new PropertyMetadata(default(string)));

        public string CurrentDirectory
        {
            get
            {
                return (string)GetValue(CurrentDirectoryProperty);
            }
            
            set
            {
                SetValue(CurrentDirectoryProperty, value);
            }
        }

        public static readonly DependencyProperty IgnoredProperty =
            DependencyProperty.Register("Ignored", 
                typeof(int), 
                typeof(MainWindow), 
                new PropertyMetadata(default(int)));

        public int Ignored
        {
            get
            {
                return (int)GetValue(IgnoredProperty);
            }

            set
            {
                SetValue(IgnoredProperty, value);
            }
        }

        public static readonly DependencyProperty EncryptedProperty =
            DependencyProperty.Register("Encrypted",
                typeof(int), 
                typeof(MainWindow), 
                new PropertyMetadata(default(int)));

        public int Encrypted
        {
            get
            {
                return (int)GetValue(EncryptedProperty);
            }

            set
            {
                SetValue(EncryptedProperty, value);
            }
        }

        public static readonly DependencyProperty SkippedProperty =
            DependencyProperty.Register("Skipped",
                typeof(int), 
                typeof(MainWindow), 
                new PropertyMetadata(default(int)));

        public int Skipped
        {
            get
            {
                return (int)GetValue(SkippedProperty);
            }

            set
            {
                SetValue(SkippedProperty, value);
            }
        }

        public static readonly DependencyProperty ErrorsProperty =
            DependencyProperty.Register("Errors", 
                typeof(int), 
                typeof(MainWindow), 
                new PropertyMetadata(default(int)));

        public int Errors
        {
            get
            {
                return (int)GetValue(ErrorsProperty);
            }

            set
            {
                SetValue(ErrorsProperty, value);
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Hide the window
            Visibility = Visibility.Collapsed;

            // Load the folders from the config file
            var folders = ((FolderConfigurationSection)ConfigurationManager.GetSection("folderSection")).Folders.ToStringArray();

            _encryptionThreadCancellation = new CancellationTokenSource();

            var token = _encryptionThreadCancellation.Token;

            // Create a background thread to do the encryption
            _encryptionThread = Task.Factory.StartNew(() =>
                EncryptFolders(folders, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            // Log the completion status of the encryption thread
            _encryptionThread.ContinueWith(delegate
            {
                LogMessage(1, "Thread ended with status '{0}'", _encryptionThread.Status.ToString());

                ShowAutomaticCloseDialog();
            });

            // Update the statistics every 5 seconds
            _updateTimer = new Timer(delegate
                {
                    UpdateStatistics(force: true);
                }, 
                null, 
                TimeSpan.Zero, 
                TimeSpan.FromSeconds(5));

            // Display the window after 15 seconds
            _showWindowTimer = new Timer(delegate
                {
                    Dispatcher.BeginInvoke(new Action(() => {
                        Visibility = Visibility.Visible;
                        WindowState = WindowState.Normal;
                    }));
                },
                null,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// Get the last Win32 error message
        /// </summary>
        /// <returns>The last Win32 error message</returns>
        private static string LastErrorMessage()
        {
            return new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        /// <summary>
        /// Use PInvoke to encrypt the specified directory
        /// </summary>
        /// <param name="directory">The directory to encrypt</param>
        /// <returns>True if the encryption was successful</returns>
        private bool EncryptDirectory(string directory)
        {
            var result = NativeMethods.EncryptFile(directory);

            if (!result)
            {
                LogMessage(0, "Couldn't encrypt '{0}': {1}", directory, LastErrorMessage());

                _errors++;
            }
            else
            {
                _encrypted++;
            }

            return result;
        }

        private void LogMessage(int level, string format, params string[] args)
        {
            var text = string.Format(format, args);

            var formattedText = string.Format("[{0}] {1}", DateTime.Now.ToString("h:mm:ss.ff").ToLower(), text);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var paragraph = new Paragraph(new Run(formattedText))
                {
                    Foreground = new SolidColorBrush(LogColors[level])
                };

                OutputRichTextBox.Document.Blocks.Add(paragraph);

                OutputRichTextBox.ScrollToEnd();
            }));
        }

        private void LogEncryptionException(FileSystemInfo file, Exception e)
        {
            LogMessage(0, "Couldn't encrypt '{0}': {1}", file.FullName, e.Message);

            _errors++;
        }

        private void UpdateStatistics(bool force = false)
        {
            // Only update the statistics every 350 milliseconds
            if ((DateTime.Now - _lastUpdate).TotalMilliseconds <= 350)
            {
                return;
            }

            // Update the dependency properties
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Ignored = _ignored;
                Skipped = _skipped;
                Errors = _errors;
                Encrypted = _encrypted;
                CurrentFile = _file;
            }));

            _lastUpdate = DateTime.Now;
        }

        private bool HandleFile(FileInfo file)
        {
            UpdateStatistics();

            // Ignore EFS temporary files, etc.
            if (Regex.IsMatch(file.Name, @"efs\d{1,}.tmp", RegexOptions.IgnoreCase) ||
                file.Name.ToLower() == "thumbs.db" ||
                file.Name.ToLower() == "desktop.ini" ||
                file.Attributes.HasFlag(FileAttributes.System))
            {
                _ignored++;

                return false;
            }

            // Skip files that are already encrypted
            if (file.Attributes.HasFlag(FileAttributes.Encrypted))
            {
                _skipped++;

                return true;
            }

            _file = file.FullName;

            try
            {
                // Here we're seeing if the file is set ReadOnly and resetting that status
                // after we encrypt it (since ReadOnly files are not encryptable)
                var readOnly = file.Attributes.HasFlag(FileAttributes.ReadOnly);

                if (readOnly)
                {
                    file.Attributes = file.Attributes.Remove(FileAttributes.ReadOnly);
                }

                if (!DRY_RUN)
                {
                    file.Encrypt();
                }

                if (readOnly)
                {
                    file.Attributes = file.Attributes.Add(FileAttributes.ReadOnly);
                }
            }
            catch (IOException e)
            {
                LogEncryptionException(file, e);

                return false;
            }
            catch (UnauthorizedAccessException e)
            {
                LogEncryptionException(file, e);

                return false;
            }
            catch (ArgumentException e)
            {
                LogEncryptionException(file, e);

                return false;
            }

            _encrypted++;

            return true;
        }

        /// <summary>
        /// Update the CurrentDirectory dependency property
        /// </summary>
        /// <param name="directory"></param>
        private void SetCurrentDirectory(string directory)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CurrentDirectory = directory;
            }));
        }

        private bool HandleDirectory(DirectoryInfo directory)
        {
            UpdateStatistics();

            SetCurrentDirectory(directory.FullName);

            var success = false;

            if (!directory.Attributes.HasFlag(FileAttributes.Encrypted))
            {
                if (!DRY_RUN)
                {
                    success = EncryptDirectory(directory.FullName);
                }
            }
            else
            {
                _skipped++;
            }

            return success;
        }

        // The body of our background thread
        private void EncryptFolders(string[] folders, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LogMessage(1, "Encrypting these folders:\n\t{0}", string.Join("\n\t", folders));

            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var directory = new DirectoryInfo(Environment.ExpandEnvironmentVariables(folder));

                LogMessage(1, "Beginning encryption of '{0}'", directory.FullName);

                if (directory.Exists)
                {
                    directory.TraverseTree(HandleFile, HandleDirectory, cancellationToken);
                }
                else
                {
                    LogMessage(0, "Directory '{0}' did not exist", directory.FullName);
                }

                LogMessage(1, "Finished encryption of '{0}'", directory.FullName);
            }

            LogMessage(1, "Encryption complete");

            // Force a statistics update
            UpdateStatistics(force: true);
        }

        private void ShowAutomaticCloseDialog()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (WindowState == WindowState.Minimized ||
                    Visibility == Visibility.Collapsed)
                {
                    Close();
                }
                else
                {
                    var automaticCloseDialog = new AutomaticCloseDialog
                    {
                        Owner = this
                    };

                    var result = automaticCloseDialog.ShowDialog();

                    if (result.HasValue &&
                        result == true)
                    {
                        Close();
                    }
                }
            }));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _updateTimer.Dispose();
            _showWindowTimer.Dispose();

            // Just quit if the thread already completed
            if (_encryptionThread == null ||
                _encryptionThread.IsCompleted)
            {
                return;
            }

            // If the thread is still running ask for confirmation
            var result = MessageBox.Show("Are you sure you want to stop the encryption process?",
                "Question",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            // If the user doesn't want to cancel then cancel the closing of the window
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;

                return;
            }

            // Cancel the thread
            _encryptionThreadCancellation.Cancel();

            // Wait for the thread to complete
            try
            {
                _encryptionThread.Wait();
            }
            catch (AggregateException)
            {
            }
        }
    }
}