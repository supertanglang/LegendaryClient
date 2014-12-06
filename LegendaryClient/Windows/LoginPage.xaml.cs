﻿using jabber.client;
using jabber.connection;
using LegendaryClient.Controls;
using LegendaryClient.Logic;
using LegendaryClient.Logic.JSON;
using LegendaryClient.Logic.Region;
using LegendaryClient.Logic.SQLite;
using LegendaryClient.Logic.SWF;
using LegendaryClient.Logic.SWF.SWFTypes;
using LegendaryClient.Windows;
using PVPNetConnect.RiotObjects.Platform.Clientfacade.Domain;
using PVPNetConnect.RiotObjects.Platform.Game;
using PVPNetConnect.RiotObjects.Platform.Login;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Champions = LegendaryClient.Logic.SQLite.Champions;
using Items = LegendaryClient.Logic.JSON.Items;
using Masteries = LegendaryClient.Logic.JSON.Masteries;
using Runes = LegendaryClient.Logic.JSON.Runes;

namespace LegendaryClient.Windows
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
            Version.TextChanged += WaterTextbox_TextChanged;
            if (Client.Version == "4.18.1" || Client.Version == "0.0.0")	
                Client.Version = "4.20.1";
            bool x = Properties.Settings.Default.DarkTheme;
            if (!x)
            {
                var bc = new BrushConverter();
                HideGrid.Background = (Brush)bc.ConvertFrom("#B24F4F4F");
                LoggingInProgressRing.Foreground = (Brush)bc.ConvertFrom("#FFFFFFFF");
            }
            //#B2C8C8C8
            Version.Text = Client.Version;

            if (!Properties.Settings.Default.DisableLoginMusic)
            {
                SoundPlayer.Source = new Uri(Path.Combine(Client.ExecutingDirectory, "Client", "Login.mp3"));
                SoundPlayer.Play();
                Sound.IsChecked = false;
            }
            else Sound.IsChecked = true;

            if (Properties.Settings.Default.LoginPageImage == "")
            {
                LoginPic.Source = new Uri(Path.Combine(Client.ExecutingDirectory, "Client", "Login.mp4"));
                LoginPic.LoadedBehavior = MediaState.Manual;
                LoginPic.MediaEnded += LoginPic_MediaEnded;
                SoundPlayer.MediaEnded += SoundPlayer_MediaEnded;
                LoginPic.Play();
            }
            else
            {
                if (File.Exists(Path.Combine(Client.ExecutingDirectory, "Assets", "champions", Properties.Settings.Default.LoginPageImage.Replace("\r\n", ""))))
                    LoginImage.Source = new BitmapImage(new Uri(Path.Combine(Client.ExecutingDirectory, "Assets", "champions", Properties.Settings.Default.LoginPageImage), UriKind.Absolute));
            }

            Video.IsChecked = false;

            //Get client data after patcher completed

            Client.SqliteDatabase = new SQLite.SQLiteConnection(Path.Combine(Client.ExecutingDirectory, Client.Sqlite));
            Client.Champions = (from s in Client.SqliteDatabase.Table<Champions>()
                                orderby s.Name
                                select s).ToList();
            
            foreach (Champions c in Client.Champions)
            {
                var Source = new Uri(Path.Combine(Client.ExecutingDirectory, "Assets", "champions", c.IconPath), UriKind.Absolute);
                c.Icon = new BitmapImage(Source);
                Debugger.Log(0, "Log", "Requesting :" + c.Name + " champ");
                
                Logic.JSON.Champions.InsertExtraChampData(c);
            }
            Client.ChampionSkins = (from s in Client.SqliteDatabase.Table<ChampionSkins>()
                                    orderby s.Name
                                    select s).ToList();
            Client.ChampionAbilities = (from s in Client.SqliteDatabase.Table<ChampionAbilities>() //Needs Fixed
                                        orderby s.Name
                                        select s).ToList();
            Client.SearchTags = (from s in Client.SqliteDatabase.Table<ChampionSearchTags>()
                                 orderby s.Id
                                 select s).ToList();
            Client.Keybinds = (from s in Client.SqliteDatabase.Table<KeybindingEvents>()
                               orderby s.Id
                               select s).ToList();
            Client.Items = Items.PopulateItems();
            Client.Masteries = Masteries.PopulateMasteries();
            Client.Runes = Runes.PopulateRunes();

            //Retrieve latest client version
            /*
            SWFReader reader = new SWFReader("ClientLibCommon.dat");
            foreach (Tag tag in reader.Tags)
            {
                if (tag is DoABC)
                {
                    DoABC abcTag = (DoABC)tag;
                    if (abcTag.Name.Contains("riotgames/platform/gameclient/application/Version"))
                    {
                        var str = System.Text.Encoding.Default.GetString(abcTag.ABCData);
                        //Ugly hack ahead - turn back now! (http://pastebin.com/yz1X4HBg)
                        string[] firstSplit = str.Split((char)6);
                        string[] secondSplit = firstSplit[0].Split((char)18);
                        //Client.Version = secondSplit[1];
                    }
                }
            }*/

            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.SavedUsername))
            {
                RememberUsernameCheckbox.IsChecked = true;
                LoginUsernameBox.Text = Properties.Settings.Default.SavedUsername;
            }
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.SavedPassword))
            {
                SHA1 sha = new SHA1CryptoServiceProvider();
                RememberPasswordCheckbox.IsChecked = true;
                LoginPasswordBox.Password = Properties.Settings.Default.SavedPassword.DecryptStringAES(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Properties.Settings.Default.Guid)).ToString());
            }
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.Region))
            {
                RegionComboBox.SelectedValue = Properties.Settings.Default.Region;
            }

            var uriSource = new Uri(Path.Combine(Client.ExecutingDirectory, "Assets", "champions", Champions.GetChampion(Client.LatestChamp).SplashPath), UriKind.Absolute);
            //LoginImage.Source = new BitmapImage(uriSource);//*/
            
            if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.SavedPassword) &&
                !String.IsNullOrWhiteSpace(Properties.Settings.Default.Region) &&
                Properties.Settings.Default.AutoLogin)
            {
                AutoLoginCheckBox.IsChecked = true;
                LoginButton_Click(null, null);
            }
            
        }

        void LoginPic_MediaEnded(object sender, RoutedEventArgs e)
        {
            LoginPic.Position = TimeSpan.FromSeconds(0);
            LoginPic.Play();
        }

        private void WaterTextbox_TextChanged(object sender, RoutedEventArgs e)
        {
            //Version.Text = Client.Version;]
            Client.Version = Version.Text;
        }
        bool PlayingSound = true;
        private void DisableSound_Click(object sender, RoutedEventArgs e)
        {
            
            if(PlayingSound)
            {
                SoundPlayer.Pause();
                Sound.IsChecked = true;
                PlayingSound = false;
            }
            else
            {
                SoundPlayer.Source = new Uri(Path.Combine(Client.ExecutingDirectory, "Client", "Login.mp3"));
                SoundPlayer.Play();
                Sound.IsChecked = false;
                PlayingSound = true;
            }

            if (Sound.IsChecked.Value) Properties.Settings.Default.DisableLoginMusic = true;
            else Properties.Settings.Default.DisableLoginMusic = false;
        }

        void SoundPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            SoundPlayer.Position = TimeSpan.FromSeconds(0);
            SoundPlayer.Play();
        }
        bool PlayingVideo = true;
        private void DisableVideo_Click(object sender, RoutedEventArgs e)
        {
            if(PlayingVideo == true)
            {
                Video.IsChecked = true;
                PlayingVideo = false;
                LoginPic.Source = new Uri("http://eddy5641.github.io/LegendaryClient/Login/Login.png");
            }
            else
            {
                LoginPic.Source = new Uri(Path.Combine(Client.ExecutingDirectory, "Login.mp4"));
                LoginPic.Play();
                Video.IsChecked = false;
                PlayingVideo = true;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Client.PvpNet = null;
            Client.PvpNet = new PVPNetConnect.PVPNetConnection();
            
            if (string.IsNullOrEmpty(Properties.Settings.Default.Guid))
                Properties.Settings.Default.Guid = Guid.NewGuid().ToString();
            Properties.Settings.Default.Save();
            SHA1 sha =new SHA1CryptoServiceProvider();
            if (RememberPasswordCheckbox.IsChecked == true)
                Properties.Settings.Default.SavedPassword = LoginPasswordBox.Password.EncryptStringAES(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Properties.Settings.Default.Guid)).ToString());
            else
                Properties.Settings.Default.SavedPassword = "";

            if (RememberUsernameCheckbox.IsChecked == true)
                Properties.Settings.Default.SavedUsername = LoginUsernameBox.Text;
            else
                Properties.Settings.Default.SavedUsername = "";

            Properties.Settings.Default.AutoLogin = (bool)AutoLoginCheckBox.IsChecked;
            Properties.Settings.Default.Region = (string)RegionComboBox.SelectedValue;
            Properties.Settings.Default.Save();

            HideGrid.Visibility = Visibility.Hidden;
            ErrorTextBox.Visibility = Visibility.Hidden;
            LoggingInLabel.Visibility = Visibility.Visible;
            LoggingInProgressRing.Visibility = Visibility.Visible;
            Client.PvpNet.OnError += PVPNet_OnError;
            Client.PvpNet.OnLogin += PVPNet_OnLogin;
            Client.PvpNet.OnMessageReceived += Client.OnMessageReceived;
            BaseRegion SelectedRegion = BaseRegion.GetRegion((string)RegionComboBox.SelectedValue);

            Client.Region = SelectedRegion;
            //Client.Version = "4.18.14";
            Client.PvpNet.Connect(LoginUsernameBox.Text, LoginPasswordBox.Password, SelectedRegion.PVPRegion, Client.Version);
        }

        private void PVPNet_OnLogin(object sender, string username, string ipAddress)
        {
            Client.PvpNet.GetLoginDataPacketForUser(new LoginDataPacket.Callback(GotLoginPacket));
        }

        private void PVPNet_OnError(object sender, PVPNetConnect.Error error)
        {
            //Display error message
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
            {
                HideGrid.Visibility = Visibility.Visible;
                ErrorTextBox.Visibility = Visibility.Visible;
                LoggingInProgressRing.Visibility = Visibility.Hidden;
                LoggingInLabel.Visibility = Visibility.Hidden;
                ErrorTextBox.Text = error.Message;
            }));
            Client.PvpNet.OnMessageReceived -= Client.OnMessageReceived;	
            Client.PvpNet.OnError -= PVPNet_OnError;			
            Client.PvpNet.OnLogin -= PVPNet_OnLogin;
        }

#pragma warning disable 4014 //Code does not need to be awaited


        private async void GotLoginPacket(LoginDataPacket packet)
        {
            if (packet.AllSummonerData == null)
            {
                //Just Created Account, need to put logic here.
                Client.Done = false;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    CreateSummonerNameOverlay createSummoner = new CreateSummonerNameOverlay();
                    Client.OverlayContainer.Content = createSummoner.Content;
                    Client.OverlayContainer.Visibility = Visibility.Visible;
                }));
                while (!Client.Done) ;
                Client.PvpNet.Connect(LoginUsernameBox.Text, LoginPasswordBox.Password, Client.Region.PVPRegion, Client.Version);
                return;
            }
            Client.LoginPacket = packet;
            if (packet.AllSummonerData.Summoner.ProfileIconId == -1)
            {
                Client.Done = false;
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() =>
                {
                    Client.OverlayContainer.Content = new ChooseProfilePicturePage().Content;
                    Client.OverlayContainer.Visibility = Visibility.Visible;
                }));
                while (!Client.Done) ;
                Client.PvpNet.Connect(LoginUsernameBox.Text, LoginPasswordBox.Password, Client.Region.PVPRegion, Client.Version);
                return;
            }
            Client.PlayerChampions = await Client.PvpNet.GetAvailableChampions();
            Client.PvpNet.OnError -= PVPNet_OnError;
            Client.GameConfigs = packet.GameTypeConfigs;
            Client.PvpNet.Subscribe("bc", packet.AllSummonerData.Summoner.AcctId);
            Client.PvpNet.Subscribe("cn", packet.AllSummonerData.Summoner.AcctId);
            Client.PvpNet.Subscribe("gn", packet.AllSummonerData.Summoner.AcctId);
            Client.IsLoggedIn = true;



            Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(async () =>
            {
                Client.StatusContainer.Visibility = System.Windows.Visibility.Visible;
                Client.Container.Margin = new Thickness(0, 0, 0, 40);

                AuthenticationCredentials newCredentials = new AuthenticationCredentials();
                newCredentials.Username = LoginUsernameBox.Text;
                newCredentials.Password = LoginPasswordBox.Password;
                newCredentials.ClientVersion = Client.Version;
                newCredentials.IpAddress = GetNewIpAddress();
                newCredentials.Locale = Client.Region.Locale;
                newCredentials.Domain = "lolclient.lol.riotgames.com";

                Session login = await Client.PvpNet.Login(newCredentials);
                Client.PlayerSession = login;

                //Setup chat
                Client.ChatClient.AutoReconnect = 30;
                Client.ChatClient.KeepAlive = 10;
                Client.ChatClient.NetworkHost = "chat." + Client.Region.ChatName + ".lol.riotgames.com";
                Client.ChatClient.Port = 5223;
                Client.ChatClient.Server = "pvp.net";
                Client.ChatClient.SSL = true;
                Client.ChatClient.User = LoginUsernameBox.Text;
                Client.ChatClient.Password = "AIR_" + LoginPasswordBox.Password;
                Client.ChatClient.OnInvalidCertificate += Client.ChatClient_OnInvalidCertificate;
                Client.ChatClient.OnMessage += Client.ChatClient_OnMessage;
                Client.ChatClient.OnPresence += Client.ChatClient_OnPresence;
                Client.ChatClient.Connect();

                Client.RostManager = new RosterManager();
                Client.RostManager.Stream = Client.ChatClient;
                Client.RostManager.AutoSubscribe = true;
                Client.RostManager.AutoAllow = jabber.client.AutoSubscriptionHanding.AllowAll;
                Client.RostManager.OnRosterItem += Client.RostManager_OnRosterItem;
                Client.RostManager.OnRosterEnd += new bedrock.ObjectHandler(Client.ChatClientConnect);

                Client.PresManager = new PresenceManager();
                Client.PresManager.Stream = Client.ChatClient;
                Client.PresManager.OnPrimarySessionChange += Client.PresManager_OnPrimarySessionChange;

                Client.ConfManager = new ConferenceManager();
                Client.ConfManager.Stream = Client.ChatClient;
                //switch
                Client.Log("Connected to " + Client.Region.RegionName + " and logged in as " + Client.LoginPacket.AllSummonerData.Summoner.Name);

                //Gather data and convert it that way that it does not cause errors
                PlatformGameLifecycleDTO data = Client.LoginPacket.ReconnectInfo;

                if (data != null && data.Game != null)
                {
                    Client.Log(data.PlayerCredentials.ChampionId.ToString());
                    Client.CurrentGame = data.PlayerCredentials;
                    Client.SwitchPage(new InGame());
                }
                else
                    Client.SwitchPage(new MainPage());
                Client.ClearPage(typeof(LoginPage));
            }));
        }

        public static string GetNewIpAddress()
        {
            StringBuilder sb = new StringBuilder();

            WebRequest con = WebRequest.Create("http://ll.leagueoflegends.com/services/connection_info");
            WebResponse response = con.GetResponse();

            int c;
            while ((c = response.GetResponseStream().ReadByte()) != -1)
                sb.Append((char)c);

            con.Abort();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, string> deserializedJSON = serializer.Deserialize<Dictionary<string, string>>(sb.ToString());

            return deserializedJSON["ip_address"];
        }

        private Vector MoveOffset;
        private Point CurrentLocation;
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                CurrentLocation = Mouse.GetPosition(MouseGrid);
                MoveOffset = new Vector(tt.X, tt.Y);
                HideGrid.CaptureMouse();
            }
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (HideGrid.IsMouseCaptured)
            {
                Vector offset = Point.Subtract(e.GetPosition(MouseGrid), CurrentLocation);

                tt.X = MoveOffset.X + offset.X;
                tt.Y = MoveOffset.Y + offset.Y;
            }
        }
        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            HideGrid.ReleaseMouseCapture();
        }
    }
}