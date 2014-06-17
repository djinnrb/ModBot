﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModBotUpdater
{
    public partial class Updater : CustomForm
    {
        Changelog changelog;
        public Updater()
        {
            InitializeComponent();
            Text = "ModBot - Updater (v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ")";
            changelog = new Changelog(this);
            CheckUpdates();
        }

        private bool IsFileLocked(String FileLocation)
        {
            FileInfo file = new FileInfo(FileLocation);
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private void startDownload()
        {
            CurrentVersionLabel.Text = "Not Found";
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe"))
            {
                CurrentVersionLabel.Text = FileVersionInfo.GetVersionInfo(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe").FileVersion.ToString();
            }

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Updater"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Updater");
            }
            else
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe"))
                {
                    while (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe") && IsFileLocked(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe"))
                    {
                        MessageBox.Show("Please close ModBot that is inside the \"Updater\" inorder to continue with the update.", "ModBot Updater", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe");
                }
            } 

            StateLabel.Text = "Initializing download...";
            Thread thread = new Thread(() =>
            {
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        client.Proxy = null;
                        client.DownloadProgressChanged += new DownloadProgressChangedEventHandler((object sender, DownloadProgressChangedEventArgs e) =>
                        {
                            //label2.Text = "Downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive;
                            BeginInvoke((MethodInvoker)delegate
                            {
                                DownloadProgressBar.Value = int.Parse(Math.Truncate(double.Parse(e.BytesReceived.ToString()) / double.Parse(e.TotalBytesToReceive.ToString()) * 100).ToString());
                                //DownloadProgressBar.Value = e.ProgressPercentage;
                                StateLabel.Text = "Downloading...";
                            });
                        });
                        client.DownloadFileCompleted += new AsyncCompletedEventHandler((object sender, AsyncCompletedEventArgs e) =>
                        {
                            if (e.Error == null && !e.Cancelled)
                            {
                                BeginInvoke((MethodInvoker)delegate
                                {
                                    DoneDownloading();
                                });
                            }
                            else
                            {
                                BeginInvoke((MethodInvoker)delegate
                                {
                                    StateLabel.Text = "Error while attempting to update!";
                                });
                            }
                        });
                        client.DownloadFileAsync(new Uri("https://dl.dropboxusercontent.com/u/60356733/ModBot.exe"), AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe");
                    }
                    catch (SocketException)
                    {
                        StateLabel.Text = "Error while attempting to update!";
                    }
                    catch (Exception)
                    {
                        StateLabel.Text = "Error while attempting to update!";
                    }
                }
            });
            thread.Start();
            thread.Join();
        }

        private void DoneDownloading()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe"))
            {
                while (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe") && IsFileLocked(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe"))
                {
                    MessageBox.Show("Please close ModBot that is inside the \"Updater\" inorder to continue with the update.", "ModBot Updater", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                FileStream fiLockFile = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe").Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                StateLabel.Text = "Checking for older version...";
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe"))
                {
                    StateLabel.Text = "Deleting older version...";
                    while (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe") && IsFileLocked(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe"))
                    {
                        MessageBox.Show("Please close ModBot inorder to continue with the update.", "ModBot Updater", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe");
                    while (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe")) { }
                }

                if (fiLockFile != null)
                {
                    fiLockFile.Close();
                }

                while (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe") && IsFileLocked(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe"))
                {
                    MessageBox.Show("Please close ModBot that is inside the \"Updater\" inorder to continue with the update.", "ModBot Updater", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                StateLabel.Text = "Moving updated version...";
                using (Stream inStream = File.Open(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe", FileMode.Open))
                {
                    using (Stream outStream = File.Create(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe"))
                    {
                        while (inStream.Position < inStream.Length)
                        {
                            DownloadProgressBar.Value = int.Parse(Math.Truncate(double.Parse(inStream.Position.ToString()) / double.Parse(inStream.Length.ToString()) * 100).ToString()); ;
                            outStream.WriteByte((byte)inStream.ReadByte());
                        }
                    }
                }
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe");
                //File.Move(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe", AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe");
                while (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Updater\ModBot.exe")) { }

                if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Updater"))
                {
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "Updater", true);
                }

                DownloadProgressBar.Value = 100;
                CurrentVersionLabel.Text = "Not Found";
                StateLabel.Text = "Done updating!";
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe"))
                {
                    CurrentVersionLabel.Text = FileVersionInfo.GetVersionInfo(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe").FileVersion.ToString();
                    if (CurrentVersionLabel.Text == LatestVersionLabel.Text)
                    {
                        StateLabel.Text = "Done updating and up-to-date!";
                    }
                }
            }
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            startDownload();
        }

        private void CheckUpdatesButton_Click(object sender, EventArgs e)
        {
            CheckUpdates();
        }

        private void CheckUpdates()
        {
            CurrentVersionLabel.Text = "Not Found";
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe"))
            {
                CurrentVersionLabel.Text = FileVersionInfo.GetVersionInfo(AppDomain.CurrentDomain.BaseDirectory + "ModBot.exe").FileVersion.ToString();
            }

            string sLatestVersion = "Error!", sFileSizeSuffix = "Bytes";
            double iFileSize = 0;
            LatestVersionLabel.Text = "Checking...";
            StateLabel.Text = "Checking for updates...";

            Thread thread = new Thread(
                () =>
                {
                    using (WebClient w = new WebClient())
                    {
                        try
                        {
                            w.Proxy = null;
                            sLatestVersion = w.DownloadString("https://dl.dropboxusercontent.com/u/60356733/ModBot.txt");
                            w.OpenRead("https://dl.dropboxusercontent.com/u/60356733/ModBot.exe");
                            iFileSize = Convert.ToDouble(w.ResponseHeaders["Content-Length"]);
                        }
                        catch (SocketException)
                        {
                        }
                        catch (Exception)
                        {
                        }
                    }
                });
            thread.Start();
            thread.Join();

            while(iFileSize >= 1024)
            {
                iFileSize /= 1024;
                if(sFileSizeSuffix == "Bytes")
                {
                    sFileSizeSuffix = "KBs";
                }
                else if (sFileSizeSuffix == "KBs")
                {
                    sFileSizeSuffix = "MBs";
                }
                else if (sFileSizeSuffix == "MBs")
                {
                    sFileSizeSuffix = "GBs";
                }
            }

            LatestVersionLabel.Text = sLatestVersion;
            if (sLatestVersion != "Error!")
            {
                if (CurrentVersionLabel.Text != "Not Found")
                {
                    string[] sCurrent = CurrentVersionLabel.Text.Split('.');
                    string[] sLatest = sLatestVersion.Split('.');
                    int iCurrentMajor = Convert.ToInt32(sCurrent[0]), iCurrentMinor = Convert.ToInt32(sCurrent[1]), iCurrentBuild = Convert.ToInt32(sCurrent[2]), iCurrentRev = Convert.ToInt32(sCurrent[3]);
                    int iLatestMajor = Convert.ToInt32(sLatest[0]), iLatestMinor = Convert.ToInt32(sLatest[1]), iLatestBuild = Convert.ToInt32(sLatest[2]), iLatestRev = Convert.ToInt32(sLatest[3]);
                    if (iLatestMajor > iCurrentMajor || iLatestMajor == iCurrentMajor && iLatestMinor > iCurrentMinor || iLatestMajor == iCurrentMajor && iLatestMinor == iCurrentMinor && iLatestBuild > iCurrentBuild || iLatestMajor == iCurrentMajor && iLatestMinor == iCurrentMinor && iLatestBuild == iCurrentBuild && iLatestRev > iCurrentRev)
                    {
                        DownloadProgressBar.Value = 0;
                        if (iFileSize > 0)
                        {
                            StateLabel.Text = "Updates available... (" + iFileSize + " " + sFileSizeSuffix + ")";
                        }
                        else
                        {
                            StateLabel.Text = "Updates available...";
                        }
                    }
                    else
                    {
                        StateLabel.Text = "Current version is up-to-date!";
                    }
                }
                else
                {
                    DownloadProgressBar.Value = 0;
                    if (iFileSize > 0)
                    {
                        StateLabel.Text = "Updates available... (" + iFileSize + " " + sFileSizeSuffix + ")";
                    }
                    else
                    {
                        StateLabel.Text = "Updates available...";
                    }
                }
            }
            else
            {
                LatestVersionLabel.Text = "Error!";
                StateLabel.Text = "Error while checking for updates!";
            }

            UpdateChangelog();
        }

        private void StateLabel_TextChanged(object sender, EventArgs e)
        {
            StateLabel.ForeColor = Color.Black;
            if (StateLabel.Text == "Error while checking for updates!" || StateLabel.Text == "Error while attempting to update!" || StateLabel.Text.Contains("Updates available...") || StateLabel.Text == "Unknown")
            {
                StateLabel.ForeColor = Color.Red;
            }
            else if (StateLabel.Text == "Current version is up-to-date!" || StateLabel.Text == "Downloading..." || StateLabel.Text.Contains("Done updating") || StateLabel.Text == "Moving updated version...")
            {
                StateLabel.ForeColor = Color.Green;
            }
            else if (StateLabel.Text == "Initializing download..." || StateLabel.Text == "Checking for updates..." || StateLabel.Text == "Checking for older version..." || StateLabel.Text == "Deleting older version...")
            {
                StateLabel.ForeColor = Color.Orange;
            }

            DownloadProgressBar.Text = "";
            DownloadProgressBar.TextColor = Brushes.Black;
            if (StateLabel.Text == "Error while checking for updates!" || StateLabel.Text == "Error while attempting to update!")
            {
                DownloadProgressBar.Text = "Error!";
                DownloadProgressBar.TextColor = Brushes.Red;
            }

            if (StateLabel.Text.Contains("Updates available...") || StateLabel.Text == "Error while attempting to update!")
            {
                UpdateButton.Enabled = true;
                CheckUpdatesButton.Enabled = false;
            }
            else if (StateLabel.Text == "Initializing download..." || StateLabel.Text == "Downloading..." || StateLabel.Text == "Checking for updates...")
            {
                UpdateButton.Enabled = false;
                CheckUpdatesButton.Enabled = false;
            }
            else
            {
                UpdateButton.Enabled = false;
                CheckUpdatesButton.Enabled = true;
            }
        }

        private void LatestVersionLabel_TextChanged(object sender, EventArgs e)
        {
            LatestVersionLabel.ForeColor = Color.Black;
            if(LatestVersionLabel.Text == "Error!" || LatestVersionLabel.Text == "Not Found")
            {
                LatestVersionLabel.ForeColor = Color.Red;
            }
            else if(LatestVersionLabel.Text == "Checking...")
            {
                LatestVersionLabel.ForeColor = Color.Orange;
            }
        }

        private void CurrentVersionLabel_TextChanged(object sender, EventArgs e)
        {
            CurrentVersionLabel.ForeColor = Color.Black;
            if (CurrentVersionLabel.Text == "Error!" || CurrentVersionLabel.Text == "Not Found")
            {
                CurrentVersionLabel.ForeColor = Color.Red;
            }
        }

        public void UpdateChangelog()
        {
            string sData = "";
            changelog.ChangelogNotes.Text = "";
            Thread thread = new Thread(
                () =>
                {
                    using (WebClient w = new WebClient())
                    {
                        try
                        {
                            w.Proxy = null;
                            sData = w.DownloadString("https://dl.dropboxusercontent.com/u/60356733/ModBot-Changelog.txt");
                        }
                        catch (SocketException)
                        {
                        }
                        catch (Exception)
                        {
                        }
                    }
                });
            thread.Start();
            thread.Join();

            sData = sData.Replace("* ", "*  ");
            while (sData != "")
            {
                string sVersion = sData.Substring(sData.IndexOf("[\"") + 2, sData.IndexOf("\"]\r\n{\"") - sData.IndexOf("[\"") - 2), sChanges = sData.Substring(sData.IndexOf("\"]\r\n{\"") + 6, sData.IndexOf("\"}") - sData.IndexOf("\"]\r\n{\"") - 6);
                changelog.ChangelogNotes.SelectionColor = Color.Blue;
                if (CurrentVersionLabel.Text != "Not Found" && LatestVersionLabel.Text != "Error!")
                {
                    string[] sCurrent = CurrentVersionLabel.Text.Split('.');
                    string[] sLatest = LatestVersionLabel.Text.Split('.');
                    string[] sLogVersion = sVersion.Split('.');
                    if (sCurrent.Length == 4 && sLatest.Length == 4)
                    {
                        int iCurrentMajor = Convert.ToInt32(sCurrent[0]), iCurrentMinor = Convert.ToInt32(sCurrent[1]), iCurrentBuild = Convert.ToInt32(sCurrent[2]), iCurrentRev = Convert.ToInt32(sCurrent[3]);
                        int iLatestMajor = Convert.ToInt32(sLatest[0]), iLatestMinor = Convert.ToInt32(sLatest[1]), iLatestBuild = Convert.ToInt32(sLatest[2]), iLatestRev = Convert.ToInt32(sLatest[3]);
                        int iLogMajor = 0, iLogMinor = 0, iLogBuild = 0, iLogRev = 0;
                        iLogMajor = Convert.ToInt32(sLogVersion[0]);
                        if (sLogVersion[1] != "*")
                        {
                            iLogMinor = Convert.ToInt32(sLogVersion[1]);
                            if (sLogVersion[2] != "*")
                            {
                                iLogBuild = Convert.ToInt32(sLogVersion[2]);
                                if (sLogVersion[3] != "*")
                                {
                                    iLogRev = Convert.ToInt32(sLogVersion[3]);
                                }
                            }
                        }

                        if (LatestVersionLabel.Text == sVersion || iCurrentMajor < iLogMajor || iCurrentMajor == iLogMajor && iCurrentMinor < iLogMinor || iCurrentMajor == iLogMajor && iCurrentMinor == iLogMinor && iCurrentBuild < iLogBuild || iCurrentMajor == iLogMajor && iCurrentMinor == iLogMinor && iCurrentBuild == iLogBuild && iCurrentRev < iLogRev)
                        {
                            changelog.ChangelogNotes.SelectionColor = Color.Red;
                        }
                        if (iCurrentMajor > iLatestMajor || iLatestMajor == iCurrentMajor && iCurrentMinor > iLatestMinor || iLatestMajor == iCurrentMajor && iLatestMinor == iCurrentMinor && iCurrentBuild > iLatestBuild || iLatestMajor == iCurrentMajor && iLatestMinor == iCurrentMinor && iLatestBuild == iCurrentBuild && iCurrentRev > iLatestRev)
                        {
                            changelog.ChangelogNotes.SelectionColor = Color.Blue;
                        }
                        if (CurrentVersionLabel.Text == sVersion || iLogMajor == iCurrentMajor && sLogVersion[1] == "*" || iLogMajor == iCurrentMajor && iLogMinor == iCurrentMinor && sLogVersion[2] == "*" || iLogMajor == iCurrentMajor && iLogMinor == iCurrentMinor && iLogBuild == iCurrentBuild && sLogVersion[3] == "*")
                        {
                            changelog.ChangelogNotes.SelectionColor = Color.Green;
                        }
                    }
                }
                changelog.ChangelogNotes.SelectionFont = new Font("Segoe Print", 8, FontStyle.Bold);
                changelog.ChangelogNotes.SelectedText = sVersion +" :\r\n";
                //changelog.ChangelogNotes.SelectionColor = Color.Red;
                //changelog.ChangelogNotes.SelectionFont = new Font("Segoe Print", 7, FontStyle.Regular);
                changelog.ChangelogNotes.SelectionColor = Color.Black;
                changelog.ChangelogNotes.SelectionFont = new Font("Microsoft Sans Serif", 8);
                changelog.ChangelogNotes.SelectedText = sChanges;
                sData = sData.Substring(sData.IndexOf("\"}") + 2);
                if(sData != "")
                {
                    changelog.ChangelogNotes.SelectedText = "\r\n\r\n";
                }
            }
            changelog.ChangelogNotes.Select(0, 0);
            changelog.ChangelogNotes.ScrollToCaret();
        }

        private void ChangelogButton_Click(object sender, EventArgs e)
        {
            changelog.ShowDialog();
        }
    }
}