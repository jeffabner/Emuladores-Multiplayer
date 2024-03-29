﻿using EmuladoresMultiplayer;
using Microsoft.VisualBasic;
using SimplePackets;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Firebase.Database;
using Firebase.Database.Query;
using System.Timers;

namespace ChatterClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public bool IsBeta = false;

        // Update Stuff
        private WebClient UpdateCheckClient = new WebClient();

        public string Ver = "0.0.0.0";
        private bool needsUpdate = false;
        public string NullDCPath = Directory.GetCurrentDirectory();
        public Configs ConfigFile;
        System.Timers.Timer timer = new System.Timers.Timer();
        public Process MednafenInstance = null;
        public MainWindow()
        {
            if (!File.Exists(NullDCPath + @"\Atualizador.exe"))
            {


                UnzipResToDir(EmuladoresMultiplayer.Properties.Resources.Atualizador, "tmp_Atualizador.zip", NullDCPath + @"\", true);
                needsUpdate = true;
            }
            else
            {
                File.SetAttributes(NullDCPath + @"\Atualizador.exe", FileAttributes.Normal);
                File.Delete(NullDCPath + @"\Atualizador.exe");
                UnzipResToDir(EmuladoresMultiplayer.Properties.Resources.Atualizador, "tmp_Atualizador.zip", NullDCPath + @"\", true);
                needsUpdate = true;
            }
            atualizar();
            InitializeComponent();
             needsUpdate = IsBeta;
            versao.Content = Assembly.GetEntryAssembly().GetName().Version.ToString();
            ((INotifyCollectionChanged)chatList2.Items).CollectionChanged
                += Messages_CollectionChanged;
        }
        private void CheckifUpdateRequired()
        {
            // Check if Update or unpack is Required
            if (File.Exists(NullDCPath + @"\NullDC_BEAR.cfg"))
            {
                var thefile = NullDCPath + @"\NullDC_BEAR.cfg";
                string[] lines = File.ReadAllLines(thefile);
                var tmpVersion = "";

                foreach (var line in lines)
                {
                    if (line.StartsWith("Version="))
                        tmpVersion = line.Split('=')[0].Trim();
                }

                if (tmpVersion != Ver)
                    needsUpdate = true;
            }
            else
                needsUpdate = true;
        }
        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            if (e.NewItems.Count > 0)
            {
                chatList2.ScrollIntoView(chatList2.Items[chatList2.Items.Count - 1]);
            }
        }

        private void MessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                var context = (MainWindowViewModel)DataContext;
                context.SendCommand.Execute(null);
            }
        }

        private async void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            var context = (MainWindowViewModel)DataContext;
            if (context.Partida_Key!=null) {
           await FBClient
                           .Child("Partidas")
                           .Child(context.Partida_Key)
                           .DeleteAsync();


               
                context.ApagarPartida.Execute(null);
                context.DisconnectCommand.Execute(null);
            }
            Application.Current.Shutdown();

        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var context = (MainWindowViewModel)DataContext;
            context.ChatRoom.Logar = true;
            CriarPasta(NullDCPath + @"\jogos\snes\");
            if (NullDCPath.ToLower().Contains("system32"))
            {

                MessageBox.Show("Não me inicie a partir da barra de pesquisa do Windows 10/11, kthx." + " Ou qualquer programa externo, basta criar um atalho para Mim em algum lugar");
                System.Environment.Exit(0);
            }

            if (NullDCPath.ToLower().Contains("download"))
            {
                MessageBox.Show("Por favor, tire-me da pasta de download, crie uma nova pasta para mim em algum lugar.");
                System.Environment.Exit(0);
            }

            //CheckifUpdateRequired();
            
            ConfigFile = new Configs(ref NullDCPath);
            var UpdateTries = 0;

        UpdateTry:
            ;
            try
            {
                UpdateTries += 1;
                UnpackUpdate();
                //CheckSDLVersion();
            }
            catch (Exception ex)
            {
                if (UpdateTries > 5)
                {
                    MessageBox.Show("Incapaz de descompactar o emulador / atualização, erro: " + ex.Message);
                    ConfigFile.Version = "0.0";
                    ConfigFile.SaveFile(false);
                    System.Environment.Exit(0);
                }
                else
                {
                    Thread.Sleep(100);
                    Console.WriteLine("Failed to update: " + ex.Message);
                    goto UpdateTry;
                }
            }

            // Once we're all done then add the version name so if it fails we try to update again next run
            ConfigFile.Version = Ver;
            ConfigFile.SaveFile(false);


            if (!IsBeta)
            {
                string[] files = Directory.GetFiles(NullDCPath + @"\", "*.exe");
                foreach (var file in files)
                {
                    var tempFileName = file.Split('\\')[file.Split('\\').Length - 1];
                    if (tempFileName.ToLower().StartsWith("Emuladores Multiplayer"))
                    {
                        if (tempFileName.ToLower() != "Emuladores Multiplayer.exe")
                        {

                            MessageBox.Show("Encontrou um exe estranho, remova / renomeie   " + tempFileName + "    para    Emuladores Multiplayer.exe   " + "ou você pode perder as configurações, ou continuar atualizando toda vez que você inicia e outras merdas estranhas.");
                            Process.Start(file.Replace(tempFileName, ""));
                            System.Environment.Exit(0);
                        }
                    }
                }
            }

            

        }

        private void atualizar()
        {
            WebClient webClient = new WebClient();

            try
            {
                if (!webClient.DownloadString("https://pastebin.com/raw/mG8L436B").Contains(Assembly.GetEntryAssembly().GetName().Version.ToString()))
                {



                    if (MessageBox.Show("Parece que há uma atualização! Você quer fazer o download?", "Atualização", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                      
                            Process.Start("Atualizador.exe");
                        System.Environment.Exit(0);

                    }
                
                        else
                    {
                        System.Environment.Exit(0);
                    }
                }
            }
            catch
            {

            }

        }

        private void UnpackUpdate()
        {
            // Unpack The Basic Shit

            if (!File.Exists(NullDCPath + @"\nullDC_Win32_Release-NoTrace.exe"))
            {
                MessageBoxResult result = MessageBox.Show("Os emuladores não foram encontrados nesta pasta, INSTALAR OS EMULADORES MULTIPLAYER NESTA PASTA? ", " Instalação dos Emuladores", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    MessageBoxResult result2 = MessageBox.Show("Isso criará vários arquivos para os emuladores funcionarem, OK? ", " Extração dos Emuladores ", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result2 == MessageBoxResult.Yes)
                    {
                  
                        try
                        {
                            //UnzipResToDir(My.Resources.NullNaomiClean, "bear_tmp_nulldc.zip", NullDCPath, true);
                            needsUpdate = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.StackTrace);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Oh!!! ok! até mais.");
                        System.Environment.Exit(0);
                    }
                }
                else
                {
                    MessageBox.Show("Preciso estar na pasta Raiz do emulador!!!");
                    System.Environment.Exit(0);
                }
            }

            // unpack Dreamcast
            //if (!File.Exists(NullDCPath + @"\dc\nullDC_Win32_Release-NoTrace.exe"))
            //{
             //   Directory.CreateDirectory(NullDCPath + @"\dc");
              //  UnzipResToDir(My.Resources.DcClean, "bear_tmp_dreamcast_clean.zip", NullDCPath + @"\dc", true);
             //   needsUpdate = true;
            //}

            // Mednafen unpack
            if (!File.Exists(NullDCPath + @"\mednafen\mednafen.exe"))
            {
                
                   Directory.CreateDirectory(NullDCPath + @"\mednafen");
                UnzipResToDir(EmuladoresMultiplayer.Properties.Resources.mednafen, "bear_tmp_mednafen.zip", NullDCPath + @"\mednafen", true);
                needsUpdate = true;
            }
         


            // Flycast unpack
            // if (!File.Exists(NullDCPath + @"\flycast\flycast.exe"))
            // {
            //    Directory.CreateDirectory(NullDCPath + @"\flycast");
            //   UnzipResToDir(My.Resources.flycast, "bear_tmp_mednafen.zip", NullDCPath + @"\flycast", true);
            //   needsUpdate = true;
            //}

            // Mednafen Server
            if (!File.Exists(NullDCPath + @"\mednafen\server\mednafen-server.exe"))
            {
                Directory.CreateDirectory(NullDCPath + @"\mednafen\server");
                UnzipResToDir(EmuladoresMultiplayer.Properties.Resources.mednafen_server, "bear_tmp_mednafen-server.zip", NullDCPath + @"\mednafen\server", true);
                needsUpdate = true;
            }

            // Mednafen unpack
           // if (!File.Exists(NullDCPath + @"\Mupen64Plus\mupen64plus-ui-console.exe"))
           // {
            //    Directory.CreateDirectory(NullDCPath + @"\Mupen64Plus");
              //  UnzipResToDir(My.Resources.Mupen64Plus, "bear_tmp_Mupen64Plus.zip", NullDCPath + @"\Mupen64Plus", true);
             //   needsUpdate = true;
           // }

            if (needsUpdate | IsBeta)
            {
                //RemoveGGPOStates(); // Remova os estados no caso de alguns estarem bagunçados para que possamos extrair os que usamos
                UnzipResToDir(EmuladoresMultiplayer.Properties.Resources.Updates, "bear_tmp_updates.zip", NullDCPath);
               // UnzipResToDir(My.Resources.Deps, "bear_tmp_deps.zip", NullDCPath, true);
               // UnzipResToDir(My.Resources.Deps, "bear_tmp_deps.zip", NullDCPath + @"\dc", true);
            }
        }
        public void CopyResourceToDirectoryThread( byte[] e)
        {
            File.WriteAllBytes(System.IO.Path.GetTempPath() + @"\" + 1, e);
        }
        public void UnzipResToDir(byte[] _res, string _name, string _dir, bool _override = false)
        {
            File.WriteAllBytes(System.IO.Path.GetTempPath() + @"\"+ _name, _res);
         
            using (ZipArchive archive = ZipFile.OpenRead(System.IO.Path.GetTempPath() + @"\" + _name))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.Name.EndsWith(".pdb") & Debugger.IsAttached)
                    {
                        continue;
                        Console.WriteLine("Skipped: " + entry.FullName + "Because we're debugging");
                    }

                    Console.Write(">:" + entry.FullName + "|");
                    // Console.WriteLine("Extracting: " & entry.FullName)
                    bool FolderOnly = false;
                    if (entry.FullName.EndsWith(@"\") | entry.FullName.EndsWith("/"))
                        FolderOnly = true;

                    var _fdir = entry.FullName.Replace("/", @"\");
                    var _dirbuilt = "";

                    foreach (var _subdir in _fdir.Split('\\'))
                    {
                        if (!Directory.Exists(_dir + @"\" + _dirbuilt + _subdir))
                        {
                            if (!FolderOnly & _subdir == _fdir.Split('\\')[_fdir.Split('\\').Length - 1])
                                break;
                            Directory.CreateDirectory(_dir + @"\" + _dirbuilt + @"\" + _subdir);
                        }

                        _dirbuilt += _subdir + @"\";
                    }

                    if (!FolderOnly)
                    {
                        var WaitTime = 0;
                        if (File.Exists(_dir + @"\" + entry.FullName.Replace("/", @"\")))
                        {
                            while (IsFileInUse(_dir + @"\" + entry.FullName.Replace("/", @"\")))
                            {
                                if (WaitTime > 50)
                                {
                                    MessageBox.Show("Error Extracting Update." + Constants.vbNewLine + "File is in use: " + _dir + @"\" + entry.FullName.Replace("/", @"\"));
                                    ConfigFile.Version = "0.0";
                                    ConfigFile.SaveFile(false);
                                    System.Environment.Exit(0);
                                }
                                Thread.Sleep(100);
                                WaitTime += 1;
                            }
                        }

                        if (File.Exists(_dir + @"\" + entry.FullName.Replace("/", @"\")))
                        {
                            if (!_override)
                            {
                                // If the file exists and it's not older than the file in the update, then don't update it.
                                var ExistingFileLastWriteTime = File.GetLastWriteTime(_dir + @"\" + entry.FullName.Replace("/", @"\"));
                                var Compare = ExistingFileLastWriteTime.CompareTo(entry.LastWriteTime.DateTime);
                                if (Compare >= 0)
                                {

                                    // DLL and EXE always get overwritten to make sure they are up to date.
                                    if (!entry.Name.EndsWith(".dll") & !entry.Name.EndsWith(".exe") & !entry.Name.EndsWith(".state-ggpo") & !entry.Name.EndsWith(".freedlc") & !entry.Name.EndsWith(".pdb") & entry.Name != "gamecontrollerdb.txt")
                                    {
                                        Console.WriteLine("Skipped: " + entry.FullName);
                                        continue;
                                    }
                                }
                            }

                            // Delete Older File
                            File.SetAttributes(_dir + @"\" + entry.FullName.Replace("/", @"\"), (FileAttributes)FileAttribute.Normal);
                            File.Delete(_dir + @"\" + entry.FullName.Replace("/", @"\"));
                        }

                        var WaitingToDelete = 0;
                        if (File.Exists(_dir + @"\" + entry.FullName.Replace("/", @"\")))
                        {
                            while (IsFileInUse(_dir + @"\" + entry.FullName.Replace("/", @"\")))
                            {
                                if (WaitingToDelete > 20)
                                {
                                    MessageBox.Show("Error Unpacking Update, could not remove old file: " + _dir + @"\" + entry.FullName.Replace("/", @"\"));
                                    System.Environment.Exit(0);
                                }

                                Thread.Sleep(50);
                                WaitingToDelete += 1;
                            }
                        }

                        Console.WriteLine("Extracting: " + entry.FullName);
                        entry.ExtractToFile(_dir + @"\" + entry.FullName.Replace("/", @"\"), true);
                    }
                }
            }
        }
        public bool IsFileInUse(string sFile)
        {
            try
            {
                using (FileStream f = new FileStream(sFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    f.Close();
                }
            }
            catch (Exception Ex)
            {
                return true;
            }

            return false;
        }

        

        void CriarPasta(string diretorio)
        {
            try
            {
                
                // Determine whether the directory exists.
                if (Directory.Exists(diretorio))
                {
                    Console.WriteLine("Esse caminho já existe.");
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(diretorio);
              Console.WriteLine("O diretório foi criado com sucesso em {0}.", Directory.GetCreationTime(diretorio));

                // Delete the directory.
                //di.Delete();
                //Console.WriteLine("O diretório foi excluído com sucesso.");
            }
            catch (Exception e)
            {
                Console.WriteLine("O processo falhou: {0}", e.ToString());
            }
        }
        public class Configs
        {
            private string _name = "Player";
            private string _network = "Radmin VPN";
            private string _port = "27886";
            private bool _useremapper = true;
            private bool _firstrun = true;
            private string _host = "127.0.0.1";
            private string _status = "Idle";
            private Int16 _delay = 1;
            private string _game = "None";
            private string _ver = "0.0";
            private string _keyprofile = "Default";
            private Int16 _recordreplay = 0;
            private string _ReplayFile = "";
            private Int16 _allowSpectators = 1;
            private string _awaystatus = "Idle";
            private Int16 _volume = 100;
            private Int16 _showconsole = 0;
            private Int16 _evolume = 100;
            private Int16 _peripheral = 0;
            private string _windowsettings = "0|200|200|656|538";
            private Int16 _vsnames = 3;
            private Int16 _ShowGameNameInTitle = 1;
            private string _sdlversopm = "Dev";
            private Int16 _vsync = 0;
            private string _theme = "Dark";
            private string _p2Name = "Local P2";
            private Int16 _nullDCPriorty = 0;
            private Int16 _DebugControls = 0;
            private Int16 _simDelay = 0;
            private Int16 _region = 0;
            private Int16 _nokey = 0;
            private Int16 _mintotray = 1;
            private Int16 _forcemono = 0;


            public Int16 SimulatedDelay
            {
                get
                {
                    return _simDelay;
                }
                set
                {
                    _simDelay = value;
                }
            }

            public Int16 Region
            {
                get
                {
                    return _region;
                }
                set
                {
                    _region = value;
                }
            }

            public Int16 DebugControls
            {
                get
                {
                    return _DebugControls;
                }
                set
                {
                    _DebugControls = value;
                }
            }

            public Int16 NullDCPriority
            {
                get
                {
                    return _nullDCPriorty;
                }
                set
                {
                    _nullDCPriorty = value;
                }
            }

            public string Host
            {
                get
                {
                    return _host;
                }
                set
                {
                    _host = value;
                }
            }

            public string Name
            {
                get
                {
                    return _name;
                }
                set
                {
                    _name = value;
                }
            }

            public string P2Name
            {
                get
                {
                    return _p2Name;
                }
                set
                {
                    _p2Name = value;
                }
            }

            public string Network
            {
                get
                {
                    return _network;
                }
                set
                {
                    _network = value;
                }
            }

            public string Port
            {
                get
                {
                    return _port;
                }
                set
                {
                    _port = value;
                }
            }

            public string Status
            {
                get
                {
                    return _status;
                }
                set
                {
                    _status = value;
                }
            }

            public Int16 Delay
            {
                get
                {
                    return _delay;
                }
                set
                {
                    _delay = value;
                }
            }

            public bool UseRemap
            {
                get
                {
                    return _useremapper;
                }
                set
                {
                    _useremapper = value;
                }
            }

            public bool FirstRun
            {
                get
                {
                    return _firstrun;
                }
                set
                {
                    _firstrun = value;
                }
            }

            public string Game
            {
                get
                {
                    return _game;
                }
                set
                {
                    _game = value;
                }
            }

            public string Version
            {
                get
                {
                    return _ver;
                }
                set
                {
                    _ver = value;
                }
            }

            public string KeyMapProfile
            {
                get
                {
                    return _keyprofile;
                }
                set
                {
                    _keyprofile = value;
                }
            }

            public Int16 RecordReplay
            {
                get
                {
                    return _recordreplay;
                }
                set
                {
                    _recordreplay = value;
                }
            }

            public string ReplayFile
            {
                get
                {
                    return _ReplayFile;
                }
                set
                {
                    _ReplayFile = value;
                }
            }

            public Int16 AllowSpectators
            {
                get
                {
                    return _allowSpectators;
                }
                set
                {
                    _allowSpectators = value;
                }
            }

            public string AwayStatus
            {
                get
                {
                    return _awaystatus;
                }
                set
                {
                    _awaystatus = value;
                }
            }

            public Int16 Volume
            {
                get
                {
                    return _volume;
                }
                set
                {
                    _volume = value;
                }
            }

            public Int16 ShowConsole
            {
                get
                {
                    return _showconsole;
                }
                set
                {
                    _showconsole = value;
                }
            }

            public Int16 EmulatorVolume
            {
                get
                {
                    return _evolume;
                }
                set
                {
                    _evolume = value;
                }
            }

            public Int16 Peripheral
            {
                get
                {
                    return _peripheral;
                }
                set
                {
                    _peripheral = value;
                }
            }

            public string WindowSettings
            {
                get
                {
                    return _windowsettings;
                }
                set
                {
                    _windowsettings = value;
                }
            }

            public Int16 VsNames
            {
                get
                {
                    return _vsnames;
                }
                set
                {
                    _vsnames = value;
                }
            }

            public Int16 ShowGameNameInTitle
            {
                get
                {
                    return _ShowGameNameInTitle;
                }
                set
                {
                    _ShowGameNameInTitle = value;
                }
            }

            public string SDLVersion
            {
                get
                {
                    return _sdlversopm;
                }

                set
                {
                    _sdlversopm = value;
                }
            }

            public Int16 Vsync
            {
                get
                {
                    return _vsync;
                }
                set
                {
                    _vsync = value;
                }
            }

            public string Theme
            {
                get
                {
                    return _theme;
                }
                set
                {
                    _theme = value;
                }
            }

            public Int16 NoKey
            {
                get
                {
                    return _nokey;
                }
                set
                {
                    _nokey = value;
                }
            }

            public Int16 MinimizeToTray
            {
                get
                {
                    return _mintotray;
                }
                set
                {
                    _mintotray = value;
                }
            }

            public Int16 ForceMono
            {
                get
                {
                    return _forcemono;
                }
                set
                {
                    _forcemono = value;
                }
            }


            public void SaveFile(bool SendIam = true)
            {
                string NullDCPath = Directory.GetCurrentDirectory();

                string[] lines = new[] {
            "[BEAR]",
            "Version=" + Version,
            "Name=" + Name,
            "Network=" + Network,
            "Port=" + Port,
            "Reclaw=" + UseRemap,
            "Host=" + Host,
            "Status=" + Status,
            "Delay=" + Delay,
            "Game=" + Game,
            "KeyProfile=" + KeyMapProfile,
            "RecordReplay=" + RecordReplay,
            "ReplayFile=" + ReplayFile,
            "AllowSpectators=" + AllowSpectators,
            "AwayStatus=" + AwayStatus,
            "Volume=" + Volume,
            "eVolume=" + EmulatorVolume,
            "ShowConsole=" + ShowConsole,
            "Peripheral=" + Peripheral,
            "WindowSettings=" + WindowSettings,
            "VsNames=" + VsNames,
            "ShowGameNameInTitle=" + ShowGameNameInTitle,
            "SDLVersion=" + SDLVersion,
            "Vsync=" + Vsync,
            "Theme=" + Theme,
            "P2Name=" + P2Name,
            "NullDCPriority=" + NullDCPriority,
            "DebugControls=" + DebugControls,
            "SimulatedDelay=" + SimulatedDelay,
            "Region=" + Region,
            "NoKey=" + NoKey,
            "MinimizeToTray=" + MinimizeToTray,
            "ForceMono=" + ForceMono
        };
                File.WriteAllLines(NullDCPath + @"\NullDC_BEAR.cfg", lines);


            }

            public Configs(ref string NullDCPath)
            {
                if (!File.Exists(NullDCPath + @"\NullDC_BEAR.cfg"))
                {
                    FirstRun = true;
                    SaveFile();
                }
                else
                {
                    FirstRun = false;
                    var thefile = NullDCPath + @"\NullDC_BEAR.cfg";
                    string[] lines = File.ReadAllLines(thefile);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Version="))
                            Version = line.Split('=')[1].Trim();
                        if (line.StartsWith("Name="))
                            Name = line.Split('=')[1].Trim();
                        if (line.StartsWith("Network="))
                            Network = line.Split('=')[1].Trim();
                        if (line.StartsWith("Port="))
                            Port = line.Split('=')[1].Trim();
                        //if (line.StartsWith("Reclaw="))
                        //  UseRemap = line.Split('=')[1].Trim();
                        if (line.StartsWith("Host="))
                            Host = line.Split('=')[1].Trim();
                        // if (line.StartsWith("Delay="))
                        //Delay = line.Split('=')[1].Trim();
                        if (line.StartsWith("KeyProfile="))
                            KeyMapProfile = line.Split('=')[1].Trim();
                        //if (line.StartsWith("RecordReplay="))
                        // RecordReplay = line.Split('=')[1].Trim();
                        if (line.StartsWith("ReplayFile="))
                            ReplayFile = line.Split('=')[1].Trim();
                        // if (line.StartsWith("AllowSpectators="))
                        // AllowSpectators = line.Split('=')[1].Trim();
                        if (line.StartsWith("AwayStatus="))
                        {
                            AwayStatus = line.Split('=')[1].Trim();
                            Status = line.Split('=')[1].Trim();
                        }

                        if (line.StartsWith("Volume="))
                        {
                            // Volume = line.Split('=')[1].Trim();
                            //EmulatorVolume = line.Split('=')[1].Trim();
                        }

                        // if (line.StartsWith("eVolume="))
                        //EmulatorVolume = line.Split('=')[1].Trim();
                        // if (line.StartsWith("ShowConsole="))
                        // ShowConsole = line.Split('=')[1].Trim();
                        //  if (line.StartsWith("Peripheral="))
                        //  Peripheral = line.Split('=')[1].Trim();
                        if (line.StartsWith("WindowSettings="))
                            WindowSettings = line.Split('=')[1].Trim();
                        // if (line.StartsWith("VsNames="))
                        //   VsNames = line.Split('=')[1].Trim();
                        // if (line.StartsWith("ShowGameNameInTitle="))
                        //    ShowGameNameInTitle = line.Split('=')[1].Trim();
                        if (line.StartsWith("SDLVersion="))
                            SDLVersion = line.Split('=')[1].Trim();
                        // if (line.StartsWith("Vsync="))
                        //   Vsync = line.Split('=')[1].Trim();
                        if (line.StartsWith("Theme="))
                            Theme = line.Split('=')[1].Trim();
                        if (line.StartsWith("P2Name="))
                            P2Name = line.Split('=')[1].Trim();
                        //if (line.StartsWith("NullDCPriority="))
                        //   NullDCPriority = line.Split('=')[1].Trim();
                        //  if (line.StartsWith("DebugControls="))
                        //  DebugControls = line.Split('=')[1].Trim();
                        // if (line.StartsWith("SimulatedDelay="))
                        //   SimulatedDelay = line.Split('=')[1].Trim();
                        // if (line.StartsWith("Region="))
                        //    Region = line.Split('=')[1].Trim();
                        //  if (line.StartsWith("NoKey="))
                        //   NoKey = line.Split('=')[1].Trim();
                        //if (line.StartsWith("MinimizeToTray="))
                        //  MinimizeToTray = line.Split('=')[1].Trim();
                        // if (line.StartsWith("ForceMono="))
                        // ForceMono = line.Split('=')[1].Trim();
                    }

                    Game = "None";
                    SaveFile();
                    return;
                }
            }
        }


        public async Task GetJogadores()
        {
            var context = (MainWindowViewModel)DataContext;
            var temp = await FBClient
                .Child("Jogadores")
                .OrderBy("Nome")
                .EqualTo(login_nome.Text)
                .LimitToFirst(1)
                .OnceAsync<Usuario>();
            

            foreach (var e in temp)
            {
                string nome = e.Object.Nome;
                string senha = e.Object.Senha;
                string key = e.Key;
                if (login_nome.Text == nome && login_senha.Password == senha)
                {

                    //MessageBox.Show("Logado! "+nome + " " + senha);
                  
                    context.Key = key;
                    context.ConnectCommand.Execute(null);
                    //MessageBox.Show(key);
                    //DisconnectCommand.Execute(null);
                    return;
                }
                else
                {
                   
                }
            }
            MessageBox.Show("Jogador não encontrado!!!");
            context.ChatRoom.Logar = true;
            status.Content = "Desconectado";
        }
        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var context = (MainWindowViewModel)DataContext;

            if (string.IsNullOrEmpty(login_nome.Text) || string.IsNullOrEmpty(login_senha.Password))
            { MessageBox.Show("Coloque o nome e a senha!"); return; }
            
            else
            {
                status.Content = "Conectando";
                context.ChatRoom.Logar = false;
                //conectar.IsEnabled = false;
            }
               


           
            
            await GetJogadores();
            
            
           
            //MessageBox.Show("conectar");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

            criarPartida.Visibility = Visibility.Visible;
           
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            var context = (MainWindowViewModel)DataContext;
            //context.ApagarPartida.Execute(null);
            context.DisconnectCommand.Execute(null);
           
            KillProcess("Mednafen");
            KillProcess("Controle");



        }
        public void KillProcess(string Name)
        {
            Process[] process = Process.GetProcessesByName(Name);
            foreach (var proc in process)
            {
                if (!proc.HasExited)
                    proc.Kill();
            }
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (lista_partidas.HasItems && lista_partidas.SelectedIndex>=0) {
           
                
                
                Partida partida = (Partida)lista_partidas.SelectedItem;



                // PartidasPacket drv = (PartidasPacket)lista_partidas.SelectedItem;
                //MessageBox.Show(partida.Engine);
                if (partida.Engine== "Mednafem".Trim())
                {
                    
                    if (partida.TipoServidor == "Público".Trim())
                  {
                        //MessageBox.Show("Engine:"+drv.Engine+ "Servidor:" + drv.TipoServidor + " IP:" + drv.Ip);
                        await EntrarPartida(partida.Nome, partida.Ip);
                    }
                }

            }
            
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
             CriarPartida();
          
        }
        public async Task EntrarPartida(string nome, string host)
        {
            Process myProcess;


           using (myProcess = new Process())
            {
                try
                {
                    // Start a process to print a file and raise an event when done.
                    myProcess.StartInfo.FileName = NullDCPath + @"\mednafen\mednafen.exe";
                    //myProcess.StartInfo.Verb = "Print";
                    string GameKey = " -netplay.gamekey \"\"";
                 


                    myProcess.StartInfo.Arguments = " -connect -netplay.host " + host + GameKey + " -netplay.nick "+nome+" ";
                    myProcess.StartInfo.Arguments += "\"" + NullDCPath + @"\jogos\snes\Super Bomberman 4 (Japan).sfc" + "\"";




                    Process correctionProcess = Process.Start(myProcess.StartInfo);
                    correctionProcess.EnableRaisingEvents = true;
                    correctionProcess.Exited += new EventHandler(MednafenInstance_Exited);
                    
                    
                   
                    // var context2 = (MainWindowViewModel)DataContext;
                    //context2.ChatRoom.Partidas[0].Jogadores.Append("abner2");

                    // var context = (MainWindowViewModel)DataContext;
                    // context.ApagarPartida.Execute(null);


                }
                catch (Exception ex)
                {
                    MessageBox.Show("Emulador não encontrado! Erro:" + ex.Message);
                }

                // Wait for Exited event, but not more than 30 seconds.

            }

        }
        private async void Time_Elapsed(object sender, ElapsedEventArgs e)
        {
           await App.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {  
            var context = (MainWindowViewModel)DataContext;
            //MessageBox.Show(context.Processo.Id.ToString()) ;
                 if (context.Processo.HasExited && timer.Enabled)
                {
                    timer.Enabled = false;
                    await App.Current.Dispatcher.BeginInvoke((Action)async delegate ()
                    {

                     
                        var temp = await FBClient
                       .Child("Partidas")
                       .OrderBy("Nome")
                       .EqualTo(context.Username)
                       .OnceAsync<Partida>();

                        //MessageBox.Show(temp.Count.ToString()) ; 
                        foreach (var j in temp)
                        {
                            
                                //string nome = j.Object.Nome;
                                //string senha = j.Object.Senha;
                                // string key = j.Key;
                                //if (login_nome.Text == nome && login_senha.Password == senha)
                                // {
                                //MessageBox.Show(j.Key);
                                //MessageBox.Show("Logado! "+nome + " " + senha);
                                await FBClient
                                 .Child("Partidas")
                                 .Child(j.Key)
                                 .DeleteAsync();
                                context.ApagarPartida.Execute(null);
                            

                        }



                    });

                }

            });
        }
        public async void CriarPartida()
        {
            Process myProcess;
            

            using (myProcess = new Process())
            {
                try
                {
                    // Start a process to print a file and raise an event when done.
                    myProcess.StartInfo.FileName = NullDCPath + @"\mednafen\mednafen.exe";
                    //myProcess.StartInfo.Verb = "Print";
                    string GameKey = " -netplay.gamekey \"\"";
           
                    myProcess.StartInfo.Arguments = " -connect -netplay.host " + "\"emuladores-br.ddns.net\"" + GameKey + " -netplay.nick \"Abner" + "\" ";
                    myProcess.StartInfo.Arguments += "\"" + NullDCPath + @"\jogos\snes\Super Bomberman 4 (Japan).sfc" + "\"";



                     
                 
                    Process correctionProcess = Process.Start(myProcess.StartInfo);
                    correctionProcess.EnableRaisingEvents = true;
                    correctionProcess.Exited += new EventHandler(MednafenInstance_Exited_Host);
                    var context = (MainWindowViewModel)DataContext;
                    context.SendPartida.Execute(null);
                    context.Processo = correctionProcess;
                   // MessageBox.Show(correctionProcess.Id.ToString()) ;
                    
                    timer.Interval = 1000; //1000 milésimos = 1 segundo
                    timer.Enabled = true;
                    timer.Elapsed += new ElapsedEventHandler(Time_Elapsed);


                    //var context2 = (PartidasPacket)context.ChatRoom.Partidas.GetEnumerator().Current;


                    //ChatroomViewModel c = new ChatroomViewModel();

                    //MessageBox.Show("Usuário:"+ drv.Username+ " Jogo:" + drv.Jogo + " Emulador:" + drv.Emulador + " Servidor:" + drv.TipoServidor + " Ip:" + drv.Ip + " Engine:" + drv.Engine);
                    //MessageBox.Show("Usuário:" + drv.Username + " Jogo:" + drv.Jogo + " Emulador:" + drv.Emulador.Trim() + " IP:" + drv.Ip.Trim() + " Jogadores:" + drv.Jogadores[0].Trim());

                    criarPartida.Visibility = Visibility.Hidden;


                  

                    // var temp = await FBClient
                    //  .Child("Partidas")
                    //  .OrderByKey()
                    // .OnceAsync<Partida>();
                    string[][] Jogadores = new string[][] {
                    new string[] {"Host", context.Username }
            };
                    Partida partida = new Partida();


                    partida.Nome = context.Username;
                    partida.Jogo = "Super Bomberman 4";
                    partida.Emulador = "Super Nintendo";
                    partida.Engine = "Mednafem";
                    partida.TipoServidor = "Público";
                    partida.Jogadores = Jogadores;
                    partida.Ip = "emuladores-br.ddns.net";
                    partida.Titulo = "Host:" + partida.Nome + " Jogo:" + partida.Emulador + " Jogadores:" + 1 + "/5  Servidor:" + partida.TipoServidor + "  Engine:" + partida.Engine;

                   await  FBClient
                                    .Child("Partidas")
                                    .PostAsync(partida, false);

                    var temp = await FBClient
               .Child("Partidas")
               .OrderBy("Nome")
               .EqualTo(context.Username)
               .OnceAsync<Partida>();
                    foreach (var j in temp)
                    {
                        
                            context.Partida_Key = j.Key;
                        
                    }

                   // MessageBox.Show(context.Partida_Key);


                        }
                catch (Exception ex)
                {
                    MessageBox.Show("Emulador não encontrado! Erro:" + ex.Message);
                }

                // Wait for Exited event, but not more than 30 seconds.
              
            }
          
        }

        private void jogadores()
        {
            var context2 = (MainWindowViewModel)DataContext;
            PartidasPacket drv = context2.ChatRoom.Partidas[0];

            tayres.ItemsSource = drv.Jogadores;
        }
        private async void MednafenInstance_Exited_Host(object sender, EventArgs e)
        {
            

            await App.Current.Dispatcher.BeginInvoke((Action)async delegate ()
            {

                var context = (MainWindowViewModel)DataContext;
                var temp = await FBClient
               .Child("Partidas")
               .OrderBy("Nome")
                .EqualTo(context.Username)
               .OnceAsync<Partida>();

                //MessageBox.Show(temp.Count.ToString()) ; 
                foreach (var j in temp)
                {
                    
                        //string nome = j.Object.Nome;
                        //string senha = j.Object.Senha;
                        // string key = j.Key;
                        //if (login_nome.Text == nome && login_senha.Password == senha)
                        // {
                        //MessageBox.Show(j.Key);
                        //MessageBox.Show("Logado! "+nome + " " + senha);
                        await FBClient
                         .Child("Partidas")
                         .Child(j.Key)
                         .DeleteAsync();
                       context.ApagarPartida.Execute(null);
                    

                }



            });
        }

        private void MednafenInstance_Exited(object sender, System.EventArgs e)
        {

            MessageBox.Show("Fechou");
        }
        private async void DeleteUser()
        {
            
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            criarPartida.Visibility = Visibility.Hidden;
        }

        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {
            cadastrar.IsEnabled = false;
          await  AdicionarJogador();
           // MessageBox.Show("Cadastrar");
        }
        private readonly FirebaseClient _fbClient = new FirebaseClient("https://emuladoresbr-94d9d-default-rtdb.firebaseio.com/");
        public FirebaseClient FBClient
        {
            get { return _fbClient; }
        }

        private async Task AdicionarJogador()
        {
            if (string.IsNullOrEmpty(cadastrar_nome.Text) || string.IsNullOrEmpty(cadastrar_senha.Password))
            { MessageBox.Show("Coloque o nome e a senha!"); cadastrar.IsEnabled = true;return; }
            else
            {

                var temp = await FBClient
                    .Child("Jogadores")
                    .OrderBy("Nome")
                       .EqualTo(cadastrar_nome.Text)
                    .OnceAsync<Usuario>();


                if (temp.Count > 0)
                {

                    MessageBox.Show("Jogador já cadastrado!!!");
                    cadastrar.IsEnabled = true;
                }
                else
                {
                    Usuario usuario = new Usuario();


                    usuario.Nome = cadastrar_nome.Text;
                    usuario.Senha = cadastrar_senha.Password;

                    await FBClient
                        .Child("Jogadores")
                        .PostAsync(usuario, false);
                    MessageBox.Show("Jogador Cadastrado com Sucesso!");
                    cadastrar.IsEnabled = true;
                }





            }
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var context = (MainWindowViewModel)DataContext;
            if (context.Partida_Key != null)
            {
                await FBClient
                                .Child("Partidas")
                                .Child(context.Partida_Key)
                                .DeleteAsync();



                context.ApagarPartida.Execute(null);
                context.DisconnectCommand.Execute(null);
            }
            Application.Current.Shutdown();

        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
           KillProcess("Mednafen");
            var context = (MainWindowViewModel)DataContext;
            //context.ApagarPartida.Execute(null);
            context.DisconnectCommand.Execute(null);

           

        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            Process myProcess;


            using (myProcess = new Process())
            {
                try
                {
                    // Start a process to print a file and raise an event when done.
                    myProcess.StartInfo.FileName = NullDCPath + @"\Controle.exe";
                    //myProcess.StartInfo.Verb = "Print";
                   



                   


                    Process correctionProcess = Process.Start(myProcess.StartInfo);
                    correctionProcess.EnableRaisingEvents = true;
                    //correctionProcess.Exited += new EventHandler(MednafenInstance_Exited);



                    // var context2 = (MainWindowViewModel)DataContext;
                    //context2.ChatRoom.Partidas[0].Jogadores.Append("abner2");

                    // var context = (MainWindowViewModel)DataContext;
                    // context.ApagarPartida.Execute(null);


                }
                catch (Exception ex)
                {
                    MessageBox.Show("Emulador não encontrado! Erro:" + ex.Message);
                }

                // Wait for Exited event, but not more than 30 seconds.

            }
        }
    }
}
