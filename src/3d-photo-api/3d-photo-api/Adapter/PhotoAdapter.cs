using photo_api;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace _3d_photo_api.Adapter
{
    public class PhotoAdapter
    {
        private static string Anaconda_Activate_Script = Startup.Configuration["AppSettings:AnacondaScript"];
        private static string Inpainting_AppDir = Startup.Configuration["AppSettings:InpaintingAppDir"];
        

        private static void ProcessOutputLine(string type, string line, PhotoProcessResult status)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }
            Startup.EphemeralLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {type}: {line}", false);
            if (type == "stderr")
            {
                if ((line.Contains("it/s") || line.Contains("s/it")) && line.Trim().EndsWith("]"))
                {
                    // not an error, process wrongly logging success status to stderr
                }
                else
                {
                    status.ErrorCount++;
                    status.Errors.Add(line);
                }
            }
        }

        public PhotoProcessResult Execute(string inputFolder, string configFilename = null)
        {
            var output = new StringBuilder();
            var status = new PhotoProcessResult();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"cmd.exe",
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            process.ErrorDataReceived += new DataReceivedEventHandler(delegate (object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ProcessOutputLine("stderr", e.Data, status);
                }
            });
            process.OutputDataReceived += new DataReceivedEventHandler(delegate (object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ProcessOutputLine("stdout", e.Data, status);
                }
            });
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var command = @$"python -m main";
            if (!string.IsNullOrEmpty(configFilename))
            {
                command += " --config " + configFilename;
            }

            using (var sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(Anaconda_Activate_Script);

                    // TODO: Remove this
                    /* DEBUG */
                    //sw.WriteLine(@"python -c ""import vispy; print(vispy.sys_info()); """);
                    //sw.WriteLine(@"python -c ""import time; print('waiting 10 secs'); time.sleep(10); print('done');""");
                    /* /DEBUG */

                    sw.WriteLine(@$"CD ""{Inpainting_AppDir}""");
                    sw.WriteLine(command);
                    sw.WriteLine("conda deactivate");
                }
            }
            WaitOrKill(process, 30, command);
            status.OutputFolder = inputFolder;
            status.ExitCode = process.ExitCode;
            return status;
        }


        private void WaitOrKill(Process process, int minutes, string command)
        {
            if (!process.WaitForExit(milliseconds: minutes * 60 * 1000))
            {
                Startup.EphemeralLog($"---------------> PROCESS EXITED AFTER TIMEOUT. Killing process. Command: {command}", true);
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    try { process.Kill(); } finally { }
                }
            }
            Startup.EphemeralLog($"--- Process Exited ---", true);
            process.CancelOutputRead();
            process.CancelErrorRead();
        }

    }

}
