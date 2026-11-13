using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Storage.AccessCache;

namespace LumiaMusicPlayer
{
    public sealed partial class MainPage : Page
    {
        private static MediaElement staticPlayer;

        public MainPage()
        {
            this.InitializeComponent();
            staticPlayer = Player;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.ContainsKey("LastPosition") && settings.Values.ContainsKey("FileToken"))
            {
                double seconds = (double)settings.Values["LastPosition"];
                if (seconds > 2)
                {
                    ShowPersistentNotification();
                }
            }

            if (e.Parameter is string arg && !string.IsNullOrEmpty(arg))
            {
                HandleAction(arg);
            }
        }

        private void ShowPersistentNotification()
        {
            // نوتیفیکیشن تعاملی ماندگار (Reminder)
            string xml = @"
            <toast scenario='reminder'>
              <visual>
                <binding template='ToastGeneric'>
                  <text>🎵 بازگشت به موزیک قبلی</text>
                  <text>پخش موزیک قطع شده بود، مایلید از کجا پخش شود؟</text>
                </binding>
              </visual>
              <actions>
                <action content='ادامه از مسیر قبلی' arguments='action=resume' activationType='foreground'/>
                <action content='پخش از ابتدا' arguments='action=restart' activationType='foreground'/>
                <action content='✕ بستن' arguments='action=close' activationType='foreground'/>
              </actions>
            </toast>";

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            var toast = new ToastNotification(xmlDoc);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public async void HandleAction(string action)
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.ContainsKey("FileToken")) return;

            try
            {
                string token = (string)settings.Values["FileToken"];
                StorageFile file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
                
                if (file != null)
                {
                    var stream = await file.OpenAsync(FileAccessMode.Read);
                    Player.SetSource(stream, file.ContentType);
                    TxtStatus.Text = "در حال پخش: " + file.DisplayName;

                    if (action == "action=resume" && settings.Values.ContainsKey("LastPosition"))
                    {
                        double pos = (double)settings.Values["LastPosition"];
                        Player.Position = TimeSpan.FromSeconds(pos);
                        Player.Play();
                    }
                    else if (action == "action=restart")
                    {
                        Player.Position = TimeSpan.Zero;
                        Player.Play();
                    }
                }
            }
            catch {}
        }

        private async void BtnPick_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".m4a");
            picker.FileTypeFilter.Add(".wma");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                string token = StorageApplicationPermissions.FutureAccessList.Add(file);
                ApplicationData.Current.LocalSettings.Values["FileToken"] = token;

                var stream = await file.OpenAsync(FileAccessMode.Read);
                Player.SetSource(stream, file.ContentType);
                Player.Play();
                TxtStatus.Text = "در حال پخش: " + file.DisplayName;
            }
        }

        public static void SaveState()
        {
            if (staticPlayer != null && staticPlayer.NaturalDuration.TimeSpan.TotalSeconds > 0)
            {
                ApplicationData.Current.LocalSettings.Values["LastPosition"] = staticPlayer.Position.TotalSeconds;
            }
        }
    }
}