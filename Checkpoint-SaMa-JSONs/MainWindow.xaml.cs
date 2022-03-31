using Checkpoint_SaMa_JSONs.JSONBuilders;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace Checkpoint_SaMa_JSONs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string folder = @"..\..\..\..\src\";
        string filePath(string file) => folder + file;
        bool[] finished = { false, false, false };
        public MainWindow()
        {
            InitializeComponent();
            ToastNotificationManagerCompat.History.Clear();
            utils.CreateShortcut();
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            _ = N_WiiU.GenerateCheckpointJSON(filePath("wiiu-csm.json"), WiiU_Notify).ConfigureAwait(false);
            _ = N_3DS.GenerateCheckpointJSON(filePath("3ds-csm.json"), _3DS_Notify).ConfigureAwait(false);


            /*Visibility = Visibility.Hidden;
            //switch-csm.json build
            if (DateTime.Now > (await utils.DateJSON("switch-csm")).AddDays(1))
            {
                _ = N_Switch.GenerateCheckpointJSON(filePath("switch-csm.json"), Switch_Notify).ConfigureAwait(false);
                await utils.NowDateJSON("switch-csm");
            }
            else
                finished[0] = true;
            //wiiu-csm.json build
            if (DateTime.Now > (await utils.DateJSON("wiiu-csm")).AddDays(7))
            {
                _ = N_WiiU.GenerateCheckpointJSON(filePath("wiiu-csm.json"), WiiU_Notify).ConfigureAwait(false);
                await utils.NowDateJSON("wiiu-csm");
            }
            else
                finished[1] = true;
            //3ds-csm.json build
            if (DateTime.Now > (await utils.DateJSON("3ds-csm")).AddDays(7))
            {
                _ = N_3DS.GenerateCheckpointJSON(filePath("3ds-csm.json"), _3DS_Notify).ConfigureAwait(false);
                await utils.NowDateJSON("3ds-csm");
            }
            else
                finished[2] = true;*/
            CloseProgramCheck();
        }

        private void Switch_Notify(object? sender, GeneratedJSONEventArgs e)
        {
            if (utils.DownloadMD5("switch-csm.md5") != e.MD5)
                utils.SendToast("switch-csm.json has been updated", "Please open GitHub to make a commit", "switch.png", 01);
            Trace.WriteLine($"Switch: {e.MD5}");
            finished[0] = true;
            CloseProgramCheck();
        }

        private void WiiU_Notify(object? sender, GeneratedJSONEventArgs e)
        {
            if (utils.DownloadMD5("wiiu-csm.md5") != e.MD5)
                utils.SendToast("wiiu-csm.json has been updated", "Please open GitHub to make a commit", "wiiu.png", 02);
            Trace.WriteLine($"WiiU: {e.MD5}");
            finished[1] = true;
            CloseProgramCheck();
        }

        private void _3DS_Notify(object? sender, GeneratedJSONEventArgs e)
        {
            if (utils.DownloadMD5("3ds-csm.md5") != e.MD5)
                utils.SendToast("3ds-csm.json has been updated", "Please open GitHub to make a commit", "3ds.png", 03);
            Trace.WriteLine($"3DS: {e.MD5}");
            finished[2] = true;
            CloseProgramCheck();
        }

        private void CloseProgramCheck()
        {
            bool AllareTrue = finished.All(process => process == true);

            if (AllareTrue)
                Close();
        }
    }
}
