﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Threading;
using System.Security.AccessControl;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


namespace AAEmu.Launcher
{

    public partial class LauncherForm : Form
    {
        // I'm keeping the CreateProcess structs and imports here in case I'll need'm again later
        //------------------------------------------------------------------------------------------------------
        /*
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        public struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        public struct SECURITY_ATTRIBUTES
        {
            public int length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [DllImport("Kernel32.dll")]
        private static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("Kernel32.dll")]
        private static extern int ResumeThread(IntPtr hThread);

        [DllImport("Kernel32.dll")]
        private static extern uint GetLastError();
        //------------------------------------------------------------------------------------------------------
        */

        //------------------------------------------------------------------------------------------------------
        // Imports for using ToolsA.dll, currently required to be able to use Trion-style login authentication
        //------------------------------------------------------------------------------------------------------
        // [DllImport("ToolsA.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?_g3@@YA_NPAEH0HPAPAX1@Z")] // named entrypoint is ugly
        [DllImport("ToolsA.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "#15")]
        public static extern bool generateInitStr(byte[] byte_0, int int_28, byte[] byte_1, int int_29, ref uint uint_0, ref uint uint_1);
        //------------------------------------------------------------------------------------------------------


        public partial class Settings
        {
            [JsonProperty("configName")]
            public string configName { get; set; }

            [JsonProperty("lang")]
            public string Lang { get; set; }

            [JsonProperty("pathToGame")]
            public string PathToGame { get; set; }

            [JsonProperty("serverIPAddress")]
            public string ServerIpAddress { get; set; }

            [JsonProperty("saveLoginAndPassword")]
            public string SaveLoginAndPassword { get; set; }

            [JsonProperty("skipIntro")]
            public string SkipIntro { get; set; }

            [JsonProperty("hideSplashLogo")]
            public string HideSplashLogo { get; set; }

            [JsonProperty("lastLoginUser")]
            public string LastLoginUser { get; set; }

            [JsonProperty("lastLoginPass")]
            public string LastLoginPass { get; set; }

            [JsonProperty("loginType")]
            public string ClientLoginType { get; set; }

            [JsonProperty("updateLocale")]
            public string UpdateLocale { get; set; }

            [JsonProperty("allowGameUpdates")]
            public string AllowGameUpdates { get; set; }

            [JsonProperty("serverGameUpdateURL")]
            public string ServerGameUpdateURL { get; set; }

            [JsonProperty("serverWebsiteURL")]
            public string ServerWebSiteURL { get; set; }

            [JsonProperty("serverNewsFeedURL")]
            public string ServerNewsFeedURL { get; set; }

            [JsonProperty("userHistory")]
            public List<string> UserHistory { get; set; }
        }

        public partial class ClientLookupHelper
        {
            [JsonProperty("serverName")]
            public List<string> serverNames { get; set; }

            [JsonProperty("clientLocation")]
            public List<string> clientLocations { get; set; }
        }


        public Settings Setting = new Settings();
        public ClientLookupHelper ClientLookup = new ClientLookupHelper();

        const string archeAgeEXE = "archeage.exe";
        const string launcherDefaultConfigFile = "settings.aelcf"; // .aelcf = ArcheAge Emu Launcher Configuration File
        const string clientLookupDefaultFile = "clientslist.json";
        string launcherOpenedConfigFile = "";
        const string urlAAEmuGitHub = "https://github.com/atel0/AAEmu";
        const string urlLauncherGitHub = "https://github.com/ZeromusXYZ/AAEmu-Launcher";
        const string urlDiscordInvite = "https://discord.gg/vn8E8E6";
        const string urlNews = "https://aaemu.pw/updater/";
        const string urlWebsite = "https://aaemu.info/";
        // const string urlNews = "https://cl2.widgetbot.io/channels/479677351618281472/481782245087248400";
        const string dx9downloadURL = "https://www.microsoft.com/en-us/download/confirmation.aspx?id=35";

        // Some strings for our language settings
        private const string settingsLangRU = "ru";
        private const string settingsLangEN = "en_us";
        private const string settingsLangDE = "de";
        private const string settingsLangFR = "fr";
        // unused for now
        private const string settingsLangKR = "ko";
        private const string settingsLangJP = "ja";
        private const string settingsLangCH = "zh_cn";
        private const string settingsLangTW = "zh_tw";

        // launcher protocol indentifiers
        private const string stringMailRu_1_0 = "mailru_1_0";
        private const string stringTrino_1_2 = "trino_1_2";

        // Stuff for dragable form
        private bool formMouseDown;
        private Point lastLocation;

        public LauncherForm()
        {
            InitializeComponent();
        }

        private void SetCustomCheckBox(Label targetLabel, string checkState)
        {
            if (checkState == "True")
            {
                targetLabel.Text = "✓";
            }
            else
            {
                targetLabel.Text = " ";
            }
        }

        private string ToggleSettingCheckBox(Label targetLabel, string settingString)
        {
            string s = "x";
            if (settingString == "True")
            {
                s = "False";
            }
            else
            {
                s = "True";
            }

            SetCustomCheckBox(targetLabel, s);
            return s;
        }

        private void UpdateControlsForPanel(byte panelID)
        {
            // The reason for doing things this way is because drawing reliable transparent stuff
            // onto a panel is hard as hell. It's easier to just put EVERYTHING on the form, and
            // Just show/hide what you need (swapping out background where needed)

            // 0: Main login "panel"
            lLogin.Visible = (panelID == 0);
            eLogin.Visible = (panelID == 0); // "Transparent" TextBox always hidden
            cbLoginList.Visible = ((panelID == 0) && (cbLoginList.Items.Count > 0));
            lPassword.Visible = (panelID == 0);
            ePassword.Visible = (panelID == 0); // "Transparent" TextBox always hidden
            btnPlay.Visible = (panelID == 0);
            btnSettings.Visible = (panelID == 0);
            btnWebsite.Visible = (panelID == 0);
            lNewsFeed.Visible = (panelID == 0);
            imgBigNews.Visible = (panelID == 0);

            // 1: Settings "panel"
            lSettingsBack.Visible = (panelID == 1);
            lIPAddress.Visible = (panelID == 1);
            eServerIP.Visible = (panelID == 1);
            lGamePath.Visible = (panelID == 1);
            lPathToGameLabel.Visible = (panelID == 1);
            lHideSplash.Visible = (panelID == 1);
            cbHideSplash.Visible = (panelID == 1);
            lSaveUser.Visible = (panelID == 1);
            cbSaveUser.Visible = (panelID == 1);
            lSkipIntro.Visible = (panelID == 1); // Currently disabled/unused
            cbSkipIntro.Visible = (panelID == 1);
            lGameClientType.Visible = (panelID == 1);
            lUpdateLocale.Visible = (panelID == 1);
            cbUpdateLocale.Visible = (panelID == 1);

            lAllowUpdates.Visible = (panelID == 1);
            cbAllowUpdates.Visible = (panelID == 1);
            // Gray out this setting if no update url is set
            if ((Setting.ServerGameUpdateURL != null) && (Setting.ServerGameUpdateURL != ""))
            {
                lAllowUpdates.ForeColor = Color.White;
                cbAllowUpdates.ForeColor = Color.White;
                cbAllowUpdates.Cursor = Cursors.Hand;
            }
            else
            {
                lAllowUpdates.ForeColor = Color.Gray;
                cbAllowUpdates.ForeColor = Color.Gray;
                cbAllowUpdates.Cursor = Cursors.No;
            }


            switch (panelID)
            {
                case 0:
                    BackgroundImage = Properties.Resources.bg_login;
                    break;
                case 1:
                    BackgroundImage = Properties.Resources.bg_setup;
                    break;
                default:
                    BackgroundImage = Properties.Resources.bg;
                    break;
            }
            
        }

        private void UpdateFormLanguageElements(string localeOverride)
        {

            switch (Setting.Lang)
            {
                case settingsLangRU:
                    btnLangChange.Image = Properties.Resources.But_Lang_Ru;
                    lLogin.Text = "Логин";
                    lPassword.Text = "Пароль";
                    lIPAddress.Text = "адрес сервера";
                    lPathToGameLabel.Text = "Путь к игровому";
                    lSaveUser.Text = "Сохранять учетные данные";
                    lSkipIntro.Text = "Пропустить заставку";
                    lHideSplash.Text = "Показывать логотип загрузки";
                    lSettingsBack.Text = "Сохранить";
                    //btnSettingsCancel.Text = "Отмена";
                    btnSettings.Text = "Настройки";
                    btnWebsite.Text = "сайт";
                    btnPlay.Text = "играть";
                    lUpdateLocale.Text = "обновить locale";
                    break;
                case settingsLangDE:
                    btnLangChange.Image = Properties.Resources.But_Lang_De;
                    lLogin.Text = "Benutzername";
                    lPassword.Text = "Passwort";
                    lIPAddress.Text = "Server Adresse";
                    lPathToGameLabel.Text = "Pfad zum Game";
                    lSaveUser.Text = "Anmeldeinformationen speichern";
                    lSkipIntro.Text = "Intro überspringen";
                    lHideSplash.Text = "Begrüßungsbildschirm ausblenden";
                    lSettingsBack.Text = "Speichern";
                    //btnSettingsCancel.Text = "Abbrechen";
                    btnSettings.Text = "Einstellungen";
                    btnWebsite.Text = "Website";
                    btnPlay.Text = "Spielen" ;
                    lUpdateLocale.Text = "locale aktualisieren";
                    break;
                case settingsLangEN:
                default:
                    Setting.Lang = settingsLangEN;
                    btnLangChange.Image = Properties.Resources.But_Lang_En;
                    lLogin.Text = "Username";
                    lPassword.Text = "Password";
                    lIPAddress.Text = "Server Address";
                    lPathToGameLabel.Text = "Path to Game";
                    lSaveUser.Text = "Save Credentials";
                    lSkipIntro.Text = "Skip Intro";
                    lHideSplash.Text = "Hide Splash Screen";
                    lSettingsBack.Text = "Save";
                    //btnSettingsCancel.Text = "Cancel";
                    btnSettings.Text = "Settings";
                    btnWebsite.Text = "Website";
                    btnPlay.Text = "Play";
                    lUpdateLocale.Text = "Update locale";
                    break;
            }

            btnLangChange.Refresh();

            if (localeOverride != "")
            {
                Setting.Lang = localeOverride;
            }

        }

        private void PicButLangChange_Click(object sender, EventArgs e)
        {
            switch(Setting.Lang)
            {
                case settingsLangRU:
                    Setting.Lang = settingsLangEN;
                    break;
                case settingsLangEN:
                    Setting.Lang = settingsLangDE;
                    break;
                case settingsLangDE:
                    Setting.Lang = settingsLangRU;
                    break;
            }
            Console.WriteLine("Updating Language: {0}",Setting.Lang);
            UpdateFormLanguageElements("");
            btnLangChange.Refresh();
        }

        private void PicButGithub_MouseEnter(object sender, EventArgs e)
        {
            btnGithub.Image = Properties.Resources.GitHub_Logo_Only_Active;
        }

        private void PicButGithub_MouseLeave(object sender, EventArgs e)
        {
            btnGithub.Image = Properties.Resources.GitHub_Logo_Only;
        }

        private void PicButDiscord_MouseEnter(object sender, EventArgs e)
        {
            btnDiscord.Image = Properties.Resources.Discord_Logo_Only_Active;
        }

        private void PicButDiscord_MouseLeave(object sender, EventArgs e)
        {
            btnDiscord.Image = Properties.Resources.Discord_Logo_Only;
        }

        private void PicButGithub_Click(object sender, EventArgs e)
        {
            cmsGitHub.Show(btnGithub, new Point(0, btnGithub.Height));
            // Process.Start(urlAAEmuGitHub);
        }

        private void PicButDiscord_Click(object sender, EventArgs e)
        {
            Process.Start(urlDiscordInvite);
        }

        private void LauncherForm_Load(object sender, EventArgs e)
        {

            // Helps to keep the editing window cleaner
            imgBigNews.SizeMode = PictureBoxSizeMode.Normal;
            imgBigNews.Size = imgBigNews.Image.Size ;
            imgBigNews.Invalidate();

            string openCommandLineSettingsFile = "";
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                // No additional possible settings yet, only check if a argument is a valid file
                if (File.Exists(arg))
                {
                    openCommandLineSettingsFile = arg;
                }
            }

            LoadClientLookup();

            if (openCommandLineSettingsFile != "")
            {
                if (!LoadSettings(openCommandLineSettingsFile)) openCommandLineSettingsFile = "";
            }
            // Load local settings file if no external config specified, or if it failed to load
            if (openCommandLineSettingsFile == "")
            {
                LoadSettings(Application.StartupPath + "\\" + launcherDefaultConfigFile);
            }

            if ((Setting.PathToGame == null) || (Setting.PathToGame == "") || (File.Exists(Setting.PathToGame) == false))
            {
                Setting.PathToGame = "";
                TryAutoFindGameExe();
            }

            if ((Setting.PathToGame == "") || (File.Exists(Setting.PathToGame) == false))
            {
                // open settings if no valid game file
                UpdateControlsForPanel(1);
            }
            else 
            {
                UpdateControlsForPanel(0);
                if (eLogin.Text != "")
                {
                    ePassword.Focus();
                    ePassword.SelectionStart = 0;
                    ePassword.SelectionLength = 0;
                }
                else
                {
                    eLogin.Focus();
                    eLogin.SelectionStart = 0;
                    eLogin.SelectionLength = 0;
                }

            }
        }

        private bool LoadClientLookup()
        {
            bool res = false;
            string configFileName = Application.StartupPath + "\\" + clientLookupDefaultFile;

            StreamReader reader = null;
            Console.WriteLine(configFileName);
            try
            {
                reader = new StreamReader(configFileName);
                var ConfigFile = reader.ReadToEnd();
                Console.Write(ConfigFile.ToString());

                ClientLookup = JsonConvert.DeserializeObject<ClientLookupHelper>(ConfigFile);
                res = true;
            }
            catch
            {
                ClientLookup.serverNames = new List<string>();
                ClientLookup.clientLocations = new List<string>();
            }
            finally
            {
                // Make sure we close our stream so the file won't be in use when we need to save it
                if (reader != null)
                {
                    reader.Close();
                }
            }

            return res;
        }

        private bool LoadSettings(string configFileName)
        {
            bool res = false;

            StreamReader reader = null ;
            Console.WriteLine(configFileName);
            try
            {
                reader = new StreamReader(configFileName);
                var ConfigFile = reader.ReadToEnd();
                Console.Write(ConfigFile.ToString());

                Setting = JsonConvert.DeserializeObject<Settings>(ConfigFile);
                res = true;
                launcherOpenedConfigFile = configFileName;

                if (launcherOpenedConfigFile == Application.StartupPath + "\\" + launcherDefaultConfigFile)
                {
                    lLoadedConfig.Text = "";
                } else if ((Setting.configName == null) || (Setting.configName == ""))
                {
                    lLoadedConfig.Text = Path.GetFileNameWithoutExtension(launcherOpenedConfigFile);
                }
                else
                {
                    lLoadedConfig.Text = Setting.configName;
                }
            }
            catch
            {
                lLoadedConfig.Text = "";
                // If loading fails, just put in some defaults instead
                Setting.configName = ""; // Local setting doesn't display a name
                Setting.PathToGame = "";
                Setting.ServerIpAddress = "127.0.0.1";
                Setting.Lang = settingsLangEN;
                Setting.SaveLoginAndPassword = "True";
                Setting.SkipIntro = "False";
                Setting.HideSplashLogo = "False";
                Setting.LastLoginUser = "test";
                Setting.LastLoginPass = "test";
                Setting.UserHistory = new List<string>();
                Setting.UserHistory.Clear();
                Setting.ClientLoginType = stringMailRu_1_0;
                Setting.UpdateLocale = "False";
                Setting.AllowGameUpdates = "False";
                Setting.ServerGameUpdateURL = ""; // Not allowed without a server settings file by default
                Setting.ServerWebSiteURL = urlWebsite; // default to aaemu.info
                Setting.ServerNewsFeedURL = ""; // Not implemented yet

            }
            finally
            {
                // Make sure we close our stream so the file won't be in use when we need to save it
                if (reader != null)
                {
                    reader.Close();
                }
            }

            lGamePath.Text = Setting.PathToGame;
            eServerIP.Text = Setting.ServerIpAddress;

            cbLoginList.Items.Clear();
            if (Setting.UserHistory != null)
            foreach (string s in Setting.UserHistory)
                if (s != "")
                    cbLoginList.Items.Add(s);

            eLogin.Text = Setting.LastLoginUser;
            ePassword.Text = Setting.LastLoginPass;

            if (((Setting.ServerGameUpdateURL == null)) || (Setting.ServerGameUpdateURL == ""))
            {
                Setting.AllowGameUpdates = "False";
            }

            updateGameClientTypeLabel();

            UpdateFormLanguageElements("");

            SetCustomCheckBox(cbSaveUser, Setting.SaveLoginAndPassword);
            SetCustomCheckBox(cbSkipIntro, Setting.SkipIntro);
            SetCustomCheckBox(cbHideSplash, Setting.HideSplashLogo);
            SetCustomCheckBox(cbUpdateLocale, Setting.UpdateLocale);
            SetCustomCheckBox(cbAllowUpdates, Setting.AllowGameUpdates);

            return res;
        }

        private static string CreateTrinoHandleIDs(string user, string pass)
        {
            byte[] data = Encoding.Default.GetBytes(pass);
            var passHash = new SHA256Managed().ComputeHash(data);

            uint handleID1 = 0;
            uint handleID2 = 0;

            string stringForSignature = "dGVzdA==";
            //string stringForSignature = "Signature 1:";

            string stringForTicket = "<?xml version=\"1.0\" encoding=\"UTF - 8\" standalone=\"yes\"?>";
            stringForTicket += "<authTicket version = \"1.2\">";
            stringForTicket += "<storeToken>1</storeToken>";
            stringForTicket += "<username>" + user + "</username>";
            stringForTicket += "<password>" + BitConverter.ToString(passHash).Replace("-", "").ToLower() + "</password>";
            stringForTicket += "</authTicket>";

            // Basically A complex way of doing stringForSignature + LineFeed + stringForTicket in a byte array
            byte[] bufferIntPtrID1 = Encoding.UTF8.GetBytes(stringForSignature);
            byte[] bufferIntPtrID2 = Encoding.UTF8.GetBytes(stringForTicket);
            byte[] bufferTotal = new byte[(bufferIntPtrID2.Length + 1) + bufferIntPtrID1.Length];
            Array.Copy(bufferIntPtrID1, 0, bufferTotal, 0, bufferIntPtrID1.Length);
            bufferTotal[bufferIntPtrID1.Length] = 10;
            Array.Copy(bufferIntPtrID2, 0, bufferTotal, bufferIntPtrID1.Length + 1, bufferIntPtrID2.Length);

            bool genRes = false ;
            try
            {
                genRes = generateInitStr(bufferTotal, bufferTotal.Length, bufferIntPtrID1, bufferIntPtrID1.Length, ref handleID1, ref handleID2);
            }
            catch
            {
                MessageBox.Show("Failed to load ToolsA.DLL, you might have a debugger open, if so, please close it and restart the launcher");
            }
            if (genRes == false)
            {
                //MessageBox.Show("Error generating login handle");
                return "00000000:00000000";
            }
            else
            {
                return handleID1.ToString("X8") + ":" + handleID2.ToString("X8");
            }

        }

        private string CreateArgs_1_0(string user, string pass)
        {
            byte[] data = Encoding.Default.GetBytes(pass);
            var passHash = new SHA256Managed().ComputeHash(data);

            string gameProviderArg, languageArg;
            switch(Setting.Lang)
            {
                case settingsLangRU:
                    gameProviderArg = "-r ";
                    languageArg = "";
                    break;
                case settingsLangFR:
                    gameProviderArg = "-r ";
                    languageArg = "";
                    break;
                case settingsLangDE:
                    gameProviderArg = "-r ";
                    languageArg = "";
                    break;
                case settingsLangEN:
                default:
                    gameProviderArg = "-r ";
                    languageArg = "";
                    break;
            }
            return gameProviderArg + "+auth_ip " + eServerIP.Text + " -uid " + eLogin.Text + " -token " + BitConverter.ToString(passHash).Replace("-", "").ToLower()+languageArg;
        }

        private string CreateArgsTrino_1_2(string user, string pass)
        {

            string gameProviderArg, languageArg;
            switch (Setting.Lang)
            {
                // This will likely need some tweaking in the future
                case settingsLangRU:
                case settingsLangFR:
                case settingsLangDE:
                case settingsLangEN:
                    gameProviderArg = "-t ";
                    languageArg = " -lang "+Setting.Lang ;
                    break;
                default:
                    gameProviderArg = "-t ";
                    languageArg = "";
                    break;
            }

            // string handleArgs = "-handle " + GetHandleIDs(user, pass);
            string handleArgs = "-handle " + CreateTrinoHandleIDs(user, pass);

            // archeage.exe -t -auth_ip 127.0.0.1 -auth_port 1237 -handle 00000000:00000000 -lang en_us
            return gameProviderArg + "+auth_ip " + eServerIP.Text + " -auth_port 1237 " + handleArgs + languageArg;
        }

        private void StartGame()
        { 
            if (Setting.PathToGame != "")
            {
                if (eLogin.Text != "" && ePassword.Text != "")
                {

                    // Mutex mutUser = new Mutex(false, "archeage_auth_ticket_event");
                    // mutex name might be: archeage_auth_ticket_event

                    if (Setting.SaveLoginAndPassword == "true")
                        SaveSettings();

                    if (Setting.UpdateLocale == "True")
                    {
                        UpdateGameConfigLocale(Setting.Lang);
                    }

                    string LoginArg = "";

                    switch(Setting.ClientLoginType)
                    {
                        case stringTrino_1_2:
                            // Trion style auth ticket with handles, generated by ToolsA.dll
                            LoginArg = CreateArgsTrino_1_2(eLogin.Text, ePassword.Text);
                            break;
                        case stringMailRu_1_0:
                            // Original style using uid and hashed password as token
                            LoginArg = CreateArgs_1_0(eLogin.Text, ePassword.Text);
                            break;
                        default:
                            MessageBox.Show("Unknown launcher protocol: "+Setting.ClientLoginType);
                            return;
                    }

                    if (Setting.HideSplashLogo == "True")
                    {
                        LoginArg += " -nosplash";
                    }

                    string HShield = " +acpxmk";

                    if (cbUseDebugMode.Checked)
                    {
                        DebugHelperForm dlg = new DebugHelperForm();
                        dlg.eArgs.Text = LoginArg;
                        dlg.eHackShieldArg.Text = HShield;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            LoginArg = dlg.eArgs.Text;
                            HShield = dlg.eHackShieldArg.Text;
                        }
                        dlg.Dispose();
                    }


                    /*
                    // required for 1.2+
                    PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
                    STARTUPINFO si = new STARTUPINFO();
                    uint lerr = 0;

                    try
                    {

                        bool isCreated = CreateProcess(
                            Setting.PathToGame, // app
                            "\"" + Setting.PathToGame + "\" " + LoginArg + HShield, //cmdline
                            IntPtr.Zero, // ProcAttrib
                            IntPtr.Zero, // ThreadAttrib
                            true, // inherit handles
                            0x00000004, // create flags: 0x00000004 = CREATE_SUSPENDED ;
                            IntPtr.Zero, // envi (null = inherit)
                            null, // current dir (null = app's dir)
                            ref si, // start info
                            out pi // proc info
                            );

                        lerr = GetLastError();

                        if (isCreated == false)
                        {
                            if (lerr == 740) // 0x2E4
                            {
                                MessageBox.Show("Elevation required ! Please run the launcher with admin rights.");
                            }
                            else if (lerr != 0)
                            {
                                MessageBox.Show("Failed to create process, error: " + lerr.ToString());
                            }
                            return;
                        }

                        if (ResumeThread(pi.hThread) == -1)
                        {
                            lerr = GetLastError();
                            if (lerr != 0)
                            {
                                MessageBox.Show("Failed to resume thread, error: " + lerr.ToString());
                                return;
                            }
                        }


                        if (Setting.SaveLoginAndPassword == "true")
                            SaveSettings();

                        // Minimize after launching AA
                        WindowState = FormWindowState.Minimized;
                    }
                    catch {
                        MessageBox.Show("Error: Failed to start the game");
                        // MessageBox.Show("Ошибка: Проверьте указанный путь до клиента игры!");
                    }
                    */

                    // Loading using Process.Start();
                    ProcessStartInfo GameClientProcessInfo;
                    GameClientProcessInfo = new ProcessStartInfo(Setting.PathToGame, LoginArg + HShield);
                    GameClientProcessInfo.UseShellExecute = true;
                    GameClientProcessInfo.Verb = "runas";
                    bool startOK = false;
                    try
                    {
                        Process.Start(GameClientProcessInfo);
                        startOK = true;
                    } catch
                    {
                        startOK = false;
                    }
                    // Minimize after launching AA
                    if (startOK)
                    {
                        WindowState = FormWindowState.Minimized;
                    }


                }
                else
                {
                    MessageBox.Show("Please enter a username and password");
                    // MessageBox.Show("Логин и пароль должны быть заполнены!");
                }
            } else
            {
                MessageBox.Show("Error: No game path set");
                // MessageBox.Show("Не указан путь размещения клиента игры!");
            }

        }

        private void ButSettingSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            UpdateControlsForPanel(0); // Show Login
        }

        private void ButSettingCancel_Click(object sender, EventArgs e)
        {
            UpdateControlsForPanel(0); // Show Login
        }

        private void LauncherForm_MouseDown(object sender, MouseEventArgs e)
        {
            // Code for form drag
            formMouseDown = true;
            lastLocation = e.Location;
        }

        private void LauncherForm_MouseMove(object sender, MouseEventArgs e)
        {
            // Code for form drag
            if (formMouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }

        private void LauncherForm_MouseUp(object sender, MouseEventArgs e)
        {
            // Code for form drag
            formMouseDown = false;
        }

        private void txtLogin_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //lFakePassword.Hide();
                ePassword.Show();
                ePassword.Focus();
                ePassword.SelectAll();
                eLogin.Hide();
                cbLoginList.Hide();
            }
        }

        private void txtPassword_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnPlay_Click(null,null);
            }

        }

        private void LauncherForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettings();
        }

        private void SaveClientLookups()
        {
            string configFileName = Application.StartupPath + "\\" + clientLookupDefaultFile;

            if ((Setting.configName != null) && (Setting.configName != ""))
            {
                string findExe = TryAutoFindFromLookup();
                if ((findExe == "") && File.Exists(Setting.PathToGame))
                {
                    ClientLookup.serverNames.Add(Setting.configName);
                    ClientLookup.clientLocations.Add(Setting.PathToGame);
                }
                else
                {
                    return;
                }
                var ClientLookupJson = JsonConvert.SerializeObject(ClientLookup);
                try
                {
                    File.WriteAllText(configFileName, ClientLookupJson);
                }
                catch
                {
                    Console.Write("Unable to save client lookup data to :\n" + launcherOpenedConfigFile);
                }
            }
        }

        private void SaveSettings()
        {
            Setting.PathToGame = lGamePath.Text;
            // Try saving lookups after we set the path
            SaveClientLookups();

            Setting.ServerIpAddress = eServerIP.Text;
            // Setting.SaveLoginAndPassword = cbSaveLogin.Checked.ToString();
            // Setting.SkipIntro = cbSkipIntro.Checked.ToString();
            // Setting.HideSplashLogo = cbHideSplashLogo.Checked.ToString();

            if (Setting.SaveLoginAndPassword == "True")
            {
                Setting.LastLoginUser = eLogin.Text;
                // TODO: Save the password in a somewhat more safe way
                Setting.LastLoginPass = ePassword.Text;
                // Setting.LastLoginPass = ""; // don't save password, unsafe
                if ((eLogin.Text != "") && (!cbLoginList.Items.Contains(eLogin.Text)))
                {
                    cbLoginList.Items.Add(eLogin.Text);
                }
            }
            else
            {
                Setting.LastLoginUser = "";
                Setting.LastLoginPass = "";
                cbLoginList.Items.Clear(); // Also delete history if we put off the save option
            }
            Setting.UserHistory = new List<string>();
            Setting.UserHistory.Clear();
            foreach(Object o in cbLoginList.Items)
                Setting.UserHistory.Add(cbLoginList.GetItemText(o));

            var SettingJson = JsonConvert.SerializeObject(Setting);

            if ((launcherOpenedConfigFile == null) || (launcherOpenedConfigFile == "") || (File.Exists(launcherOpenedConfigFile) == false))
            {
                launcherOpenedConfigFile = Application.StartupPath + "\\" + launcherDefaultConfigFile;
            }
            Console.Write("Saving Settings to "+ launcherOpenedConfigFile +" :\n" + SettingJson);
            try
            {
                File.WriteAllText(launcherOpenedConfigFile, SettingJson);
            }
            catch 
            {
                Console.Write("Unable to save settings to :\n" + launcherOpenedConfigFile);
            }
        }

        private void txtLoginList_SelectedValueChanged(object sender, EventArgs e)
        {
            eLogin.Text = cbLoginList.Text;
            //lFakeUser.Text = cbLoginList.Text;
            cbLoginList.Hide();
        }

        private void txtLogin_Leave(object sender, EventArgs e)
        {
            eLogin.Hide();
            //lFakeUser.Text = eLogin.Text;
            //lFakeUser.Show();
        }

        private void txtPassword_Leave(object sender, EventArgs e)
        {
            ePassword.Hide();
            //lFakePassword.Text = new String('●', ePassword.TextLength);
            //lFakePassword.Show();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            Application.UseWaitCursor = true;
            StartGame();
            Application.UseWaitCursor = false;
        }

        private void btnPlay_MouseEnter(object sender, EventArgs e)
        {
            btnPlay.Image = Properties.Resources.btn_green_a;
        }

        private void btnPlay_MouseLeave(object sender, EventArgs e)
        {
            btnPlay.Image = Properties.Resources.btn_green;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            UpdateControlsForPanel(1); // Show settings
        }

        private void btnWebsite_Click(object sender, EventArgs e)
        {
            if ((Setting.ServerWebSiteURL != null) && (Setting.ServerWebSiteURL != ""))
            {
                Process.Start(Setting.ServerWebSiteURL);
            }
            else
            {
                Process.Start(urlWebsite);
            }
        }

        private void LauncherForm_BackgroundImageChanged(object sender, EventArgs e)
        {
            Invalidate(true);
        }

        private void minimizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnSystem_Click(object sender, EventArgs e)
        {
            cmsAAEmuButton.Show(btnSystem,new Point(0, btnSystem.Height));
        }

        private void lSettingsBack_Click(object sender, EventArgs e)
        {
            SaveSettings();
            UpdateControlsForPanel(0);
        }

        private void lGamePath_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (lGamePath.Text != "")
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(lGamePath.Text);
            }
            if (openFileDialog.InitialDirectory == "")
            {
                openFileDialog.InitialDirectory = "C:\\ArcheAge\\Working\\Bin32";
            }
            openFileDialog.Filter = "ArcheAge Game|"+ archeAgeEXE +"|Executeable|*.exe|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                Setting.PathToGame = openFileDialog.FileName;
                lGamePath.Text = Setting.PathToGame;
                Console.WriteLine(openFileDialog.OpenFile());
            }
        }

        private void cbHideSplash_Click(object sender, EventArgs e)
        {
            Setting.HideSplashLogo = ToggleSettingCheckBox(cbHideSplash, Setting.HideSplashLogo);
        }

        private void cbSkipIntro_Click(object sender, EventArgs e)
        {
            Setting.SkipIntro = ToggleSettingCheckBox(cbSkipIntro, Setting.SkipIntro);
        }

        private void cbSaveUser_Click(object sender, EventArgs e)
        {
            Setting.SaveLoginAndPassword = ToggleSettingCheckBox(cbSaveUser, Setting.SaveLoginAndPassword);
        }

        private void cbLoginList_SelectedIndexChanged(object sender, EventArgs e)
        {
            eLogin.Text = cbLoginList.Text;
            ePassword.Text = "";
        }

        private void cbUpdateLocale_Click(object sender, EventArgs e)
        {
            Setting.UpdateLocale = ToggleSettingCheckBox(cbUpdateLocale, Setting.UpdateLocale);
        }

        private void updateGameClientTypeLabel()
        {
            switch (Setting.ClientLoginType)
            {
                case stringMailRu_1_0:
                    lGameClientType.Text = "Mail.ru 1.0 Auth (-r)";
                    break;
                case stringTrino_1_2:
                    lGameClientType.Text = "Trion 1.2 Auth (-t)";
                    break;
                default:
                    lGameClientType.Text = "???: " + Setting.ClientLoginType ;
                    break;
            }
        }

        private void lGameClientType_Click(object sender, EventArgs e)
        {
            // toggle client types
            switch (Setting.ClientLoginType)
            {
                case stringMailRu_1_0:
                    Setting.ClientLoginType = stringTrino_1_2;
                    break;
                case stringTrino_1_2:
                default:
                    Setting.ClientLoginType = stringMailRu_1_0;
                    break;
            }

            updateGameClientTypeLabel();
        }

        private void UpdateGameConfigLocale(string locale)
        {
            // C:\ArcheAge\Documents => UserHomeFolder\ArcheAge
            string configFileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\ArcheAge\\system.cfg";
            const string localeField = "locale = ";

            List<string> lines = new List<string>();
            List<string> newLines = new List<string>();

            bool updatedLocale = false;

            if (File.Exists(configFileName) == true)
            {
                lines = File.ReadAllLines(configFileName).ToList();

                foreach (string line in lines)
                {
                    if (line.IndexOf(localeField) >= 0)
                    {
                        // replace here
                        if (updatedLocale == false)
                        {
                            newLines.Add(localeField + locale);
                        }
                        updatedLocale = true;
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }
            }
            if (updatedLocale == false)
            {
                newLines.Add(localeField + locale);
            }

            try
            {
                File.WriteAllLines(configFileName, newLines);
            } catch
            {
                MessageBox.Show("ERROR updating " + configFileName);
            }

        }

        private void cbLocaleSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Setting.Lang = cbLocaleSelect.Text;
            //UpdateFormLanguageElements(cbLocaleSelect.Text);
        }

        private void aAEmuServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(urlAAEmuGitHub);
        }

        private void aAEmuLauncherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(urlLauncherGitHub);
        }

        private void cbAllowUpdates_Click(object sender, EventArgs e)
        {
            if ((Setting.ServerGameUpdateURL != null) && (Setting.ServerGameUpdateURL != ""))
            {
                Setting.AllowGameUpdates = ToggleSettingCheckBox(cbAllowUpdates, Setting.AllowGameUpdates);
            }
            else
            {
                Setting.AllowGameUpdates = "False";
                SetCustomCheckBox(cbAllowUpdates, Setting.AllowGameUpdates);
            }
        }

        private string TryAutoFindInDir(string dirName, string fileName,int depth)
        {
            pb1.PerformStep();
            pb1.Refresh();

            // Don't search too deep, 5 deep should be enough to be in range of what we want
            if (depth >= 5)
            {
                return "";
            }

            DirectoryInfo di = new DirectoryInfo(dirName);
            FileInfo[] files = di.GetFiles(fileName);
            foreach(FileInfo fi in files)
            {
                if (fi.Name.ToLower() == fileName.ToLower() && (File.Exists(dirName+fi.Name)))
                {
                    return dirName + fi.Name;
                }
            }

            DirectoryInfo[] dirs = di.GetDirectories();
            pb1.Maximum = pb1.Maximum + dirs.Length ;
            foreach (DirectoryInfo downDir in dirs)
            {
                string dirRes = TryAutoFindInDir(dirName + downDir.Name + "\\", fileName,depth+1);
                if (dirRes != "")
                {
                    return dirRes;
                }
            }

            return "";
        }

        private string TryAutoFindFromLookup()
        {
            if (ClientLookup.serverNames.Count != ClientLookup.clientLocations.Count)
            {
                Console.WriteLine("Error in client lookup file, array size mismatch ! Clearing settings");
                ClientLookup.serverNames.Clear();
                ClientLookup.clientLocations.Clear();
            }
            for (int i = 0;i < ClientLookup.serverNames.Count();i++)
            {
                if (ClientLookup.serverNames[i] == Setting.configName)
                {
                    return ClientLookup.clientLocations[i];
                }
            }
            return "";
        }

        private void TryAutoFindGameExe()
        {
            Application.UseWaitCursor = true;

            string exeFile = "";

            if ((Setting.configName != null) && (Setting.configName != ""))
            {
                // If we loaded a named config, try to pick from a saved list first
                exeFile = TryAutoFindFromLookup();
                if (exeFile != "")
                {
                    Setting.PathToGame = exeFile;
                    lGamePath.Text = Setting.PathToGame;
                    Application.UseWaitCursor = false;
                    return;
                }
            }

            string configPath = Path.GetDirectoryName(launcherOpenedConfigFile);
            // Yes I know this trim looks silly, but it's to prevent stuff like "C:\\\\directory\\pathtogame.exe"
            configPath = configPath.TrimEnd('\\') + "\\" ;

            pb1.Visible = true;
            pb1.Minimum = 0;
            pb1.Maximum = 2;
            pb1.Value = 0;
            pb1.Step = 1;

            // Let's put a big old try/catch around this to prevent any file system shananigans
            try
            {
                exeFile = TryAutoFindInDir(configPath, archeAgeEXE, 0);
                if ((exeFile != "") && (File.Exists(exeFile)))
                {
                    Setting.PathToGame = exeFile;
                    lGamePath.Text = Setting.PathToGame;
                }
            }
            catch
            {
                // actually do nothing with this, user doesn't need to know
            }
            pb1.Visible = false;
            Application.UseWaitCursor = false;
        }


    }
}
