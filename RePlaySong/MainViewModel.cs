using System.Collections.ObjectModel;
using System.ComponentModel;
using Prism.Commands;
using System.Threading;
using System;
using System.Windows;
using VideoLibrary;
using NAudio.Wave;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using RePlaySong.Properties;
using Newtonsoft.Json;
using Microsoft.VisualBasic.Logging;
using System.Windows.Controls;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Markup;
using System.Collections.Concurrent;
using System.Windows.Threading;
using Application = System.Windows.Application;
using System.Xml.Linq;

namespace RePlaySong
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public class ListAndLoopsNumber
        {
            public int Input;
            public List<string> Songs { get; }
            public ListAndLoopsNumber(int input, List<string> songs)
            {
                Input=input; 
                Songs=songs;
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DelegateCommand PlaySong { get; private set; }
        public DelegateCommand DownloadSongs { get; private set; }
        public DelegateCommand DownloadSong { get; private set; }
        public DelegateCommand PauseSong { get; private set; }
        public DelegateCommand ResumeSong { get; private set; }
        public DelegateCommand StopSong { get; private set; }
        public DelegateCommand DeleteSongCommand { get; private set; }
        public DelegateCommand DeleteItemCommand { get; private set; }
        public DelegateCommand<string> DeleteSongFromTarget { get; private set; }
        public DelegateCommand SaveListCommand { get; private set; }
        public DelegateCommand LoadListCommand { get; private set; }
        public DelegateCommand MoveForwardCommand { get; private set; }
        public DelegateCommand MoveBackwardCommand { get; private set; }

        public MainModel Model { get; set; }

        private ObservableCollection<string> targetSongs = new ObservableCollection<string>();
        private ObservableCollection<string> sourceSongs = new ObservableCollection<string>();
        private readonly object collectionLock = new object();
        private readonly Dispatcher dispatcher;

        private WaveOutEvent outputDevice = new WaveOutEvent();

        private bool songStopped = false;

        private string selectedSongs;

        private string selectedTargetSongs;

        private ConcurrentQueue<(string, string)> downloadSongs; 

        private bool isSongNotPlayedOrPaused = true;

        private bool moveForward = false;

        private bool moveBackwoard = false;

        private string playedSong;
        public ObservableCollection<string> TargetSongs
        {
            get { return targetSongs; }
            set
            {
                targetSongs = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetSongs)));
            }
        }
        public ObservableCollection<string> SourceSongs
        {
            get { return sourceSongs; }
            set
            {
                sourceSongs = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SourceSongs)));            }
        }

        public string SelectedSongs
        {
            get { return selectedSongs; }
            set
            {
                selectedSongs = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSongs)));
            }
        }

        public string SelectedTargetSongs
        {
            get { return selectedTargetSongs; }
            set
            {
                selectedTargetSongs = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTargetSongs)));
            }
        }

        private int numberInput = 1;
        public int NumberInput
        {
            get { return numberInput; }
            set
            {
                numberInput = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NumberInput)));
            }
        }

        private string url;

        public string Url
        {
            get { return url; }
            set
            {
                url = value;
                if(url =="")
                {
                    url = "הכנס כתובת שיר להורדה";
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Url)));
            }
        }

        private string songName;

        public string SongName
        {
            get { return songName; }
            set
            {
                songName = value;
                if (songName == "")
                {
                    songName = "כתוב שם שיר לשמירה";
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SongName)));
            }
        }

        public string PlayedSong
        {
            get { return playedSong; }
            set
            {
                playedSong = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayedSong)));
            }
        }

        public bool IsSongNotPlayedOrPaused
        {
            get { return isSongNotPlayedOrPaused; }
            set
            {
                if (isSongNotPlayedOrPaused != value)
                {
                    isSongNotPlayedOrPaused = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSongNotPlayedOrPaused)));
                }
            }
        }

        public MainViewModel()
        {
            Model = new MainModel();
            if (Model.Songs!=null)
            {
                SourceSongs = new ObservableCollection<string>(Model.Songs.Keys);
            }
            else
            {
                SourceSongs = new ObservableCollection<string>();
            }
            PlaySong = new DelegateCommand(PlaySongExecuteAsync, CanExecuteYourCommand);
            DownloadSong = new DelegateCommand(InsertSongToQueue, CanExecuteYourCommand);
            PauseSong = new DelegateCommand(Pause, CanExecuteYourCommand);
            ResumeSong = new DelegateCommand(Resume, CanExecuteYourCommand);
            DeleteSongCommand = new DelegateCommand(DeleteSong, CanExecuteYourCommand);
            StopSong = new DelegateCommand(Stop, CanExecuteYourCommand);
            DeleteItemCommand = new DelegateCommand(DeleteItem, CanExecuteYourCommand);
            SaveListCommand = new DelegateCommand(SaveList, CanExecuteYourCommand);
            LoadListCommand = new DelegateCommand(LoadList, CanExecuteYourCommand);
            MoveBackwardCommand = new DelegateCommand(MoveBackward, CanExecuteYourCommand);
            MoveForwardCommand = new DelegateCommand(MoveForward, CanExecuteYourCommand);
            downloadSongs = new ConcurrentQueue<(string, string)>();
            Url = "";
            SongName = "";
            Task.Run(CheckForQueueElement);

        }

        private async void PlaySongExecuteAsync()
        {
            if(TargetSongs.Count ==0)
            {
                MessageBox.Show("אנא בחר שירים לניגון");
                return;
            }
            if(NumberInput<=0)
            {
                MessageBox.Show("אנא בחר מספר חיובי של לופים לשירים");
                return;
            }
            if (outputDevice.PlaybackState ==PlaybackState.Paused)
            {
                outputDevice.Play();
                return;
            }
            if (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                return;
            }
            
            int numberOfRepeats = numberInput;
            songStopped = false;
            for (int i = 0; i < numberOfRepeats; i++)
            {

               for (int index = 0;index< TargetSongs.Count || (moveBackwoard&& moveBackwoard); index++)
               {
                  if(songStopped)
                  {
                      return;
                  }
                  if(outputDevice.PlaybackState == PlaybackState.Paused && moveForward)
                    {
                        if (index >= TargetSongs.Count) return;
                        moveForward = false;
                        outputDevice.Stop();
                    }
                    if (outputDevice.PlaybackState == PlaybackState.Paused && moveBackwoard)
                    {
                        moveBackwoard = false;
                        if(index>1)
                        {
                            index -= 2;
                        }
                        else if (index==1)
                        {
                            index--;
                        }
                        outputDevice.Stop();
                    }
                    try
                  {
                      string videoId = Model.Songs[TargetSongs[index]];
                      string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{TargetSongs[index]}.mp4");
                      if (!File.Exists(outputPath))
                      {
                            Task.Run(() =>
                            {
                                MessageBox.Show($"מוריד את השיר - \n {songName}");
                            });
                          var youTube = YouTube.Default; // starting point for YouTube actions
                          var video = youTube.GetVideo(videoId); // gets a Video object with info about the video
                          var bytes = await video.GetBytesAsync();
                          File.WriteAllBytes(outputPath, bytes);
                      }
                        PlayedSong = TargetSongs[index];
                        await PlayVideo(outputPath);
                      Thread.Sleep(800);

                  }
                  catch (Exception ex)
                  {
                      MessageBox.Show(ex.Message);
                  }
                }
            }
            IsSongNotPlayedOrPaused = true;
            PlayedSong = "";


        }

        private bool CanExecuteYourCommand()
        {
            return true;
        }

        private async Task PlayVideo(string videoPath)
        {
            if (outputDevice.PlaybackState == PlaybackState.Paused)
            {
                await Task.Run(PlayAndWait);
            }
            else
            {
                using (var audioFile = new AudioFileReader(videoPath))
                {
                    outputDevice.Init(audioFile);
                    await Task.Run(PlayAndWait);
                }
            }

        }

        private void PlayAndWait()
        {
            outputDevice.Play();
            IsSongNotPlayedOrPaused = false;
            while(outputDevice.PlaybackState==PlaybackState.Playing || (outputDevice.PlaybackState == PlaybackState.Paused && (!moveBackwoard && !moveForward)))
            {
                Thread.Sleep(500);
            }
        }

        private void Pause()
        {
            try
            {
                outputDevice.Pause();

            }
            catch(Exception)
            {

            }
        }

        private void MoveForward()
        {
            try
            {
                outputDevice.Pause();
                moveForward = true;

            }
            catch (Exception)
            {

            }
        }

        private void MoveBackward() 
        {
            try
            {
                outputDevice.Pause();
                moveBackwoard = true;

            }
            catch (Exception)
            {

            }
        } 

        private void Resume()
        {
            try
            {
                outputDevice.Play();

            }
            catch (Exception)
            {

            }
        }

        private void Stop()
        {
            try
            {
                outputDevice.Stop();
                songStopped = true;
                IsSongNotPlayedOrPaused = true;
                songName = "";
            }
            catch (Exception)
            {

            }

        }

        private  void DownloadSongUrl(string name,string url)
        {
            try
            {
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), $"{name}.mp4")))
                {
                    var youTube = YouTube.Default; // starting point for YouTube actions 
                    var video = youTube.GetVideo(url); // gets a Video object with info about the video
                    string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{name}.mp4");
                    Task.Run(() =>
                    {
                        MessageBox.Show($" - {name} השיר החל  לרדת");
                    });
                    var bytes = video.GetBytes();
                    File.WriteAllBytes(outputPath, bytes);
                    Model.Songs.Add(songName, url);
                    Settings.Default.SongsDictionaryJson = JsonConvert.SerializeObject(Model.Songs);
                    Settings.Default.Save();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SourceSongs.Add(name);
                        SourceSongs = new ObservableCollection<string>(SourceSongs.OrderBy(i => i));
                    });
                    Task.Run(() =>
                    {
                        MessageBox.Show($" סיים לרדת {name} השיר ");
                    });
                }
                else
                {
                    if (!Model.Songs.ContainsKey(name)) 
                    {
                        Model.Songs.Add(name, url);
                        Settings.Default.SongsDictionaryJson = JsonConvert.SerializeObject(Model.Songs);
                        Settings.Default.Save();
                        Task.Run(() =>
                        {
                            MessageBox.Show($" סיים לרדת {name} השיר ");
                        });
                    }

                    else
                    {
                        Task.Run(() =>
                        {
                            MessageBox.Show($" כבר קיים ברשימה {name} השיר ");
                        });
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"השיר לא ירד! \n {ex.Message}");
            }                  
           
        }

        private void DeleteSong()
        {
            if (!string.IsNullOrEmpty(selectedSongs)) 
            {
                MessageBoxResult response = MessageBox.Show($"אתה מעוניין למחוק את השיר?", $"{selectedSongs}", MessageBoxButton.YesNo);
                if (response == MessageBoxResult.Yes)
                {
                    string[] songs = selectedSongs.Split(",");
                    foreach (var song in songs)
                    {
                        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), $"{song}.mp4")))
                        {
                            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), $"{song}.mp4"));
                        }
                        Model.Songs.Remove(song);
                        SourceSongs.Remove(song);
                        if (TargetSongs.Contains(song)) TargetSongs.Remove(song);
                        Settings.Default.SongsDictionaryJson = JsonConvert.SerializeObject(Model.Songs);
                        Settings.Default.Save();
                        MessageBox.Show($"השיר {song} נמחק!");
                    }
                }

            
            }
        }

        public void AddToTarget(string name)
        {
            if(!TargetSongs.Contains(name))
            {
                TargetSongs.Add(name);
            }
        }

        public void MoveItem(int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= TargetSongs.Count ||
                targetIndex < 0 || targetIndex >= TargetSongs.Count)
            {
                return;
            }

            string itemToMove = TargetSongs[sourceIndex];
            TargetSongs.RemoveAt(sourceIndex);
            TargetSongs.Insert(targetIndex, itemToMove);

            // Notify property change for TargetItems if needed
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TargetSongs)));
        }

        private void DeleteItem()
        {

            if (TargetSongs.Contains(selectedTargetSongs))
            {
                TargetSongs.Remove(selectedTargetSongs);
            }
        }

        private void SaveList()
        {
            if(TargetSongs.Count < 0) {
                MessageBox.Show("אנא בחר שירים לשמור");
                return;
            }
            try
            {
                ListAndLoopsNumber listAndLoopsNumber = new ListAndLoopsNumber(NumberInput, TargetSongs.ToList());
                var fbd = new SaveFileDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    var localPath = fbd.FileName;
                    if (!fbd.FileName.EndsWith(".txt"))
                    {
                        localPath = $"{fbd.FileName}.txt";
                    }
                    string text = JsonConvert.SerializeObject(listAndLoopsNumber);
                    using (StreamWriter writer = new StreamWriter(localPath))
                    {
                        // Write the data to the file
                        writer.Write(text);
                    }
                    MessageBox.Show("הרשימה נשמרה בהצלחה");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"הרשימה לא נשמרה בהצלחה \n {ex.Message}");

            }

        }

        private void LoadList()
        {
            try
            {
                var fbd = new OpenFileDialog();
                fbd.Filter = "Text Files (*.txt)|*.txt";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    var chosenFile = File.ReadAllText(fbd.FileName);
                    var info = JsonConvert.DeserializeObject<ListAndLoopsNumber>(chosenFile);
                    NumberInput = info.Input;
                    TargetSongs = new ObservableCollection<string>(info.Songs);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"הייתה בעיה בטעינה \n {ex.Message}");
            }

        }

        private void InsertSongToQueue()
        {
            (string, string) item = new(SongName, Url) { };
            if(downloadSongs.Contains<(string, string)>(item))
            {
                Task.Run(() =>
                {
                    MessageBox.Show($" כבר קיים ברשימה להורדה {SongName} השיר ");
                });
            }
            else
            {
                downloadSongs.Enqueue(item);
                Task.Run(() =>
                {
                    MessageBox.Show($" הוכנס לרשימה להורדה {SongName} השיר ");
                });

            }
        }
        
        private void CheckForQueueElement()
        {
            while (true) {
                if(!downloadSongs.IsEmpty)
                {
                    var item = ("", "");
                    if(downloadSongs.TryDequeue(out item))
                    {
                        DownloadSongUrl(item.Item1, item.Item2);
                    }
                }
                Thread.Sleep(500);
            }
        }


    }
    
}

