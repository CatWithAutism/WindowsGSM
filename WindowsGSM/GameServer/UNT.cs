﻿using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    class UNT
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Unturned Dedicated Server";
        public string StartPath = "Unturned.exe";
        public bool ToggleConsole = true;
        public int PortIncrements = 2;
        public dynamic QueryMethod = new Query.A2S();
        public bool requireSteamAccount = true;

        public string Port = "27015";
        public string QueryPort = "27016";
        public string Defaultmap = "pei";
        public string Maxplayers = "20";
        public string Additional = "+secureserver/Default";

        public string AppId = "1110390";

        public UNT(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            
        }

        public async Task<Process> Start()
        {
            string exePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(exePath))
            {
                Error = $"{Path.GetFileName(exePath)} not found ({exePath})";
                return null;
            }

            string param = "-batchmode --nographics";
            param += string.IsNullOrWhiteSpace(_serverData.ServerPort) ? string.Empty : $" -port:{_serverData.ServerPort}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer) ? string.Empty : $" -players:{_serverData.ServerMaxPlayer}";
            param += string.IsNullOrWhiteSpace(_serverData.ServerMap) ? string.Empty : $" -{_serverData.ServerMap}";
            param += $" {_serverData.ServerParam}";

            Process p;
            if (ToggleConsole)
            {
                p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID),
                        FileName = exePath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Minimized,
                    },
                    EnableRaisingEvents = true
                };
                p.Start();
            }
            else
            {
                p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID),
                        FileName = exePath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Minimized,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true
                };
                var serverConsole = new Functions.ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p.StartInfo.CreateNoWindow)
                {
                    p.Kill();
                }
                else
                {
                    Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                    Functions.ServerConsole.SendWaitToMainWindow("shutdown");
                    Functions.ServerConsole.SendWaitToMainWindow("{ENTER}");
                }
            });
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(_serverData.ServerID, string.Empty, AppId, true, loginAnonymous: false);
            Error = steamCMD.Error;

            return p;
        }

        public async Task<bool> Update(bool validate = false)
        {
            var steamCMD = new Installer.SteamCMD();
            bool updateSuccess = await steamCMD.Update(_serverData.ServerID, string.Empty, AppId, validate, loginAnonymous: false);
            Error = steamCMD.Error;

            return updateSuccess;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            string importPath = Path.Combine(path, StartPath);
            Error = $"Invalid Path! Fail to find {Path.GetFileName(StartPath)}";
            return File.Exists(importPath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}
