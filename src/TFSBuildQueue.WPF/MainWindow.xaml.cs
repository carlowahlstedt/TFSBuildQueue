using MahApps.Metro.Controls;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TFSBuildQueue.Domain;
using ToastinetWPF;
using Humanizer;
using System.Windows.Media;
using System.Media;
using System.Collections.Generic;

namespace TFSBuildQueue.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        QueueBuilds _QueueBuilds;
        IOrderedEnumerable<IQueuedBuild> _Builds;
        Timer _RefreshTimer;
        TimerCallback _RefreshTimerCallback;
        public IEnumerable<TfsBuild> BuildList;
        readonly string _WindowTitle = "TFS Build Queue";
        int _PreviousBuildCount;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            progressRing.Visibility = Visibility.Hidden;
            Title = _WindowTitle;
            txtTfsUrl.Text = Properties.Settings.Default.TfsUrl;
            chkBuildCompleteNotifications.IsChecked = Properties.Settings.Default.BuildCompleteNotifications;
            chkNewBuildNotifications.IsChecked = Properties.Settings.Default.NewBuildNotifications;
            txtName.Content = "Current User: " + System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            await SetupQueueBuilds();
            SetupRefreshTimer();

            //TODO:
            //Columns on the grid need spaces (fixing the all caps thing would be nice too)
            //fix my name to have a link to the github account/website
            //create popup notification that acts like a toast outside of the app
        }

        private void mainWindow_Closed(object sender, EventArgs e)
        {
            if (_QueueBuilds != null)
            {
                _QueueBuilds.Dispose();
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await CheckTfs();
        }

        async Task CheckTfs()
        {
            await Task.Factory.StartNew(() =>
            {
                DoWork(null);
            });
        }

        private void DoWork(object state)
        {
            if (_QueueBuilds == null)
            {
                Dispatcher.Invoke(() =>
                {
                    ToastTfs.Message = "Please Setup the TFS URL in the settings";
                    flyoutSettings.IsOpen = true;
                    ToastTfs.Visibility = Visibility.Visible;
                });
                return;
            }
            ToastTfs.Dispatcher.Invoke(() =>
            {
                ToastTfs.Visibility = Visibility.Hidden;
            });

            progressRing.Dispatcher.Invoke(() =>
            {
                progressRing.Visibility = Visibility.Visible;
            });

            try
            {
                _Builds = _QueueBuilds.GetBuilds();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
            if (_Builds != null)
            {
                try
                {
                    var list = from build in _Builds
                               select new TfsBuild
                               {
                                   BuildControllerName = build.BuildController != null ? build.BuildController.Name : string.Empty,
                                   Agent = QueueBuilds.GetBuildAgent(build.Build),
                                   TeamProject = build.TeamProject,
                                   BuildDefinitionName = build.BuildDefinition.Name,
                                   Status = build.Status.Humanize(LetterCasing.Title),
                                   Priority = build.Priority,
                                   QueueTime = build.QueueTime,
                                   ElapsedTime = (DateTime.Now - build.QueueTime),
                                   Reason = build.Reason.Humanize(LetterCasing.Title),
                                   Requestor = build.RequestedForDisplayName
                               };

                    BuildList = list.ToList();

                    UpdateUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error");
                }
            }
        }

        private void UpdateUI()
        {
            Dispatcher.Invoke(() =>
            {
                //Notification.IsOpen = true;
                if (Properties.Settings.Default.NewBuildNotifications || Properties.Settings.Default.BuildCompleteNotifications)
                {
                    if (BuildList.Count() > 0 && _PreviousBuildCount != BuildList.Count())
                    {
                        SetupNotificationLocation();
                        NotificationTitleText.Content = string.Format("{0} New Build(s)", BuildList.Count());
                        NotificationText.Content = BuildList.GetBuildsList();
                        Notification.IsOpen = true;
                    }
                }
                _PreviousBuildCount = BuildList.Count();
                Title = string.Format("{0} ({1})", _WindowTitle, BuildList.Count());
                dataGrid.ItemsSource = BuildList;
                mainWindow.SizeToContent = BuildList.Count() != 0 ? SizeToContent.Width : SizeToContent.Manual;
                btnExport.IsEnabled = (BuildList.Count() != 0);
                imgDownload.Source = BuildList.Count() != 0 ? new BitmapImage(new Uri("Resources/download.png", UriKind.Relative)) : new BitmapImage(new Uri("Resources/download_disabled.png", UriKind.Relative));
                progressRing.Visibility = Visibility.Hidden;
            });
        }

        #region Export

        /// <summary>
        /// Exports the current grid to CSV.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new SaveFileDialog()
            {
                FileName = QueueBuilds.FileName,
                Filter = "Comma Separated Files (*.csv,*.txt)|*.csv;*.txt"
            };

            var result = fileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                try
                {
                    _QueueBuilds.Export(fileDialog.FileName, _Builds);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error");
                }
            }
        }

        #endregion

        #region Setup

        void SetupRefreshTimer()
        {
            _RefreshTimerCallback = new TimerCallback(DoWork);
            _RefreshTimer = new Timer(_RefreshTimerCallback, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(long.Parse(txtRefreshInterval.Text)));
        }

        async Task SetupQueueBuilds()
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.TfsUrl))
            {
                try
                {
                    _QueueBuilds = new QueueBuilds(Properties.Settings.Default.TfsUrl);
                    await _QueueBuilds.Setup();
                }
                catch (Exception ex)
                {
                    _QueueBuilds = null;
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        #endregion

        #region Notification

        private void SetupNotificationLocation()
        {
            var x = SystemParameters.PrimaryScreenWidth - Notification.Width - 10;
            var y = SystemParameters.PrimaryScreenHeight - Notification.Height - 40;
            Notification.HorizontalOffset = x;
            Notification.VerticalOffset = y;
        }

        async void Notification_Opened(object sender, EventArgs e)
        {
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    var uri = new Uri(@"/Resources/ding.wav", UriKind.Relative);
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var player = new SoundPlayer(Properties.Resources.ding);
                            //player.Stream = ;
                            player.Play();
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    });
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                Thread.Sleep(new TimeSpan(0, 0, Properties.Settings.Default.NotificationLength));
                Notification.Dispatcher.Invoke(() =>
                {
                    Notification.IsOpen = false;
                });
            });
        }

        private void Notification_MouseUp(object sender, MouseButtonEventArgs e)
        {
            mainWindow.Activate();
            Notification.IsOpen = false;
        }

        private void btnCloseNotification_Click(object sender, RoutedEventArgs e)
        {
            Notification.IsOpen = false;
        }

        #endregion

        #region Settings

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            flyoutSettings.IsOpen = !flyoutSettings.IsOpen;
        }

        private async void btnTfsUrl_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.TfsUrl != txtTfsUrl.Text)
            {
                Properties.Settings.Default.TfsUrl = txtTfsUrl.Text;
                Properties.Settings.Default.Save();
                await SetupQueueBuilds();
                await CheckTfs();
            }
        }

        #region txtRefreshInterval

        private void txtRefreshInterval_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var c = Convert.ToChar(e.Text);
            if (!char.IsNumber(c))
            {
                e.Handled = true;
            }
        }

        private void txtRefreshInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_RefreshTimer != null)
            {
                //if there is no refresh interval then never refresh
                if (string.IsNullOrWhiteSpace(txtRefreshInterval.Text))
                {
                    _RefreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                else
                {
                    var interval = short.Parse(txtRefreshInterval.Text);
                    _RefreshTimer.Change(TimeSpan.FromSeconds(interval), TimeSpan.FromSeconds(interval));
                    Properties.Settings.Default.RefreshInterval = interval;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void txtRefreshInterval_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) { e.Handled = true; }
        }

        #endregion

        #region txtNotificationLength

        private void txtNotificationLength_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var c = Convert.ToChar(e.Text);
            if (!char.IsNumber(c))
            {
                e.Handled = true;
            }
        }

        private void txtNotificationLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtNotificationLength.Text))
            {
                Properties.Settings.Default.NotificationLength = int.Parse(txtNotificationLength.Text);
                Properties.Settings.Default.Save();
            }
        }

        private void txtNotificationLength_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) { e.Handled = true; }
        }

        #endregion

        private void chkNewBuildNotifications_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.NewBuildNotifications = chkNewBuildNotifications.IsChecked.Value;
            Properties.Settings.Default.Save();
        }

        private void chkBuildCompleteNotifications_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.BuildCompleteNotifications = chkBuildCompleteNotifications.IsChecked.Value;
            Properties.Settings.Default.Save();
        }

        #endregion
    }
}
