using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpEXR.Viewer.HTML {
    public static class StringShExtensions {

        public static string Sh(this string cmd, string workingDirectory = ".") {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var fileName = "/bin/bash";
            var arguments = $"-c \"{escapedArgs}\"";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                fileName = "cmd.exe";
                arguments = $"/C \"{escapedArgs}\"";
            }

            var process = new Process() {
                StartInfo = new ProcessStartInfo {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            };
            process.Start();
            var stdOut = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return stdOut;
        }

        public static void Sh2(this string cmd, out Process process, string workingDirectory = ".") {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var fileName = "/bin/bash";
            var arguments = $"-c \"{escapedArgs}\"";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                fileName = "cmd.exe";
                arguments = $"/C \"{escapedArgs}\"";
            }

            process = new Process() {
                StartInfo = new ProcessStartInfo {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            };
            process.Start();
        }



        public static int Sh2(this string cmd, out string stdOut, string workingDirectory = ".") {
            Process process;
            cmd.Sh2(out process, workingDirectory);
            stdOut = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode;
        }

        public static int Sh2(this string cmd, Action<string> lineCallback = null, string workingDirectory = ".") {
            Process process;
            cmd.Sh2(out process, workingDirectory);
            string line;
            if (lineCallback != null)
                while ((line = process.StandardOutput.ReadLine()) != null)
                    lineCallback(line);
            process.WaitForExit();
            return process.ExitCode;
        }

        public static int Sh2(this string cmd, string workingDirectory = ".") {
            Process process;
            cmd.Sh2(out process, workingDirectory);
            string line;
            while ((line = process.StandardOutput.ReadLine()) != null)
                Console.WriteLine($"Process {process.Id}: {line}");
            process.WaitForExit();
            return process.ExitCode;
        }


        public static Version ExtractVersion(this string self) {
            var match = Regex.Match(self, @"^.*(\d+)\.(\d+)\.(\d+).*$");
            if (match != null)
                return new Version(
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value),
                    int.Parse(match.Groups[3].Value));
            throw new Exception($"String '{self}' does not contain a version number");
        }
    }
}
