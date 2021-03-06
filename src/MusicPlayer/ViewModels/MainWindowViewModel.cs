﻿using MusicPlayer.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Timers;
using MusicPlayer.Infrastructure;
using System.Dynamic;
using MusicPlayer.Services;

namespace MusicPlayer.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private Timer _incrementPlayingProgress;
        private Timer _findSongEnd;
        private int _playingIndex;
        private readonly IMusicPlayer _player;
        private SongCollection _currentQueue;
        private double _seconds;

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public ICommand AddToQueueCommand { get; private set; }
        public ICommand ClearQueueCommand { get; private set; }
        public ICommand PlaySong { get; private set; }
        public ICommand PauseSong { get; private set; }
        public ICommand StopSong { get; private set; }
        public ICommand FastForwardCommand { get; private set; }
        public ICommand RewindCommand { get; private set; }

        public MainWindowViewModel(IMusicPlayer m, SongCollection collection)
        {
            if (DesignerProperties.GetIsInDesignMode(
                new System.Windows.DependencyObject())) return;

            _seconds = 0;

            _playingSong = new Song();
            QueueInfo = "";
            _player = m;
            ElapsedTime = "00:00 / 00:00";
            _incrementPlayingProgress = new Timer();
            _incrementPlayingProgress.Interval = 1000;
            _currentQueue = collection;
            _incrementPlayingProgress.Elapsed += (sender, e) =>
            {
                _seconds += 1000;
                PlayingProgress += 1000;

                string displayProgress = TimeSpan.FromMilliseconds(_seconds).ToString("mm\\:ss");
                ElapsedTime = displayProgress + " / " + PlayingSong.DisplayDuration;
            };
            _findSongEnd = new Timer();
            _findSongEnd.Interval = 1;
            _findSongEnd.Elapsed += (sender, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_player.IsDone())
                    {
                        Debug.WriteLine("Done");
                        if (++_playingIndex < _currentQueue.SongList.Count)
                        {

                            SelectedSong = _currentQueue.SongList[_playingIndex];
                            PlaySongAction();
                        }
                        else
                        {
                            StopSongAction();
                        }
                    }
                });
            };

            AddToQueueCommand = new CommandHandler(() => AddToQueueAction(), () => true);
            ClearQueueCommand = new CommandHandler(() => ClearQueueAction(), () => true);
            PlaySong = new CommandHandler(() => PlaySongAction(), () => true);
            PauseSong = new CommandHandler(() => PauseSongAction(), () => true);
            StopSong = new CommandHandler(() => StopSongAction(), () => true);
            FastForwardCommand = new CommandHandler(() => FastForwardAction(), () => true);
            RewindCommand = new CommandHandler(() => RewindAction(), () => true);
        }

        #region ViewBindedProperties
        public string QueueFilePath { get; set; }
        public ObservableCollection<Song> SongList { get { return _currentQueue.SongList; } }

        private string _queueInfo;
        public string QueueInfo
        {
            get { return _queueInfo; }
            set
            {
                _queueInfo = value;
                PropertyChanged(this, new PropertyChangedEventArgs("QueueInfo"));
            }
        }

        private string _artistAlbumInfo;
        public string ArtistAlbumInfo {
            get { return _artistAlbumInfo; }
            set
            {
                _artistAlbumInfo = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ArtistAlbumInfo"));
            }
        }

        private string _trackTitleInfo;
        public string TrackTitleInfo
        {
            get { return _trackTitleInfo; }
            set
            {
                _trackTitleInfo = value;
                PropertyChanged(this, new PropertyChangedEventArgs("TrackTitleInfo"));
            }
        }

        private Song _selectedSong;
        public Song SelectedSong
        {
            get { return _selectedSong; }
            set
            {
                _selectedSong = value;
                PropertyChanged(this, new PropertyChangedEventArgs("SelectedSong"));
            }
        }

        private Song _playingSong;
        public Song PlayingSong
        {
            get { return _playingSong; }
            set
            {
                _playingSong = value;
                PropertyChanged(this, new PropertyChangedEventArgs("PlayingSong"));
            }
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                PropertyChanged(this, new PropertyChangedEventArgs("SelectedIndex"));
            }
        }

        private double _playingProgress;
        public double PlayingProgress
        {
            get { return _playingProgress; }
            set
            {
                _playingProgress = value;
                PropertyChanged(this, new PropertyChangedEventArgs("PlayingProgress"));
            }
        }

        private string _elapsedTime;
        public string ElapsedTime
        {
            get { return _elapsedTime; }
            set
            {
                _elapsedTime = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ElapsedTime"));
            }
        }

        #endregion

        #region CommandActions
        private void AddToQueueAction()
        {
            _currentQueue.Load(QueueFilePath);

            if (_currentQueue.SongList.Count > 0)
            {
                double queueDuration = _currentQueue.TotalSeconds();
                string format = "hh\\:mm\\:ss";
                if (queueDuration < 3600)
                {
                    format = "mm\\:ss";
                }
                TimeSpan totalDuration = TimeSpan.FromSeconds(queueDuration);
                QueueInfo = _currentQueue.SongList.Count + " songs - " + totalDuration.ToString(format);
            }
        }

        private void ClearQueueAction()
        {
            _currentQueue.SongList.Clear();
            QueueInfo = "";
        }

        private void PlaySongAction()
        {
            if (PlayingSong.FilePath != SelectedSong.FilePath)
            {
                StopSongAction();
                PlayingSong = SelectedSong;
                _playingIndex = SelectedIndex;
                ArtistAlbumInfo = PlayingSong.Artist + " - " + PlayingSong.Album + " [" + PlayingSong.Year + "]";
                TrackTitleInfo = PlayingSong.TrackNumber + ". " + PlayingSong.Title;
                _player.Play(new Uri(PlayingSong.FilePath));
            }
            else
            {
                _player.Play();
            }
            StartTimers();
        }

        private void PauseSongAction()
        {
            StopTimers();
            _player.Pause();
        }

        private void StopSongAction()
        {
            _player.Stop();
            StopTimers();
            PlayingProgress = 0;
            _seconds = 0;
            ElapsedTime = "00:00 / " + PlayingSong.DisplayDuration;
        }

        private void FastForwardAction()
        {
            _player.FastForward(10000);

            _seconds += 10000;
            PlayingProgress += 10000;

            string displayProgress = TimeSpan.FromMilliseconds(_seconds).ToString("mm\\:ss");
            ElapsedTime = displayProgress + " / " + PlayingSong.DisplayDuration;
        }

        private void RewindAction()
        {
            _player.Rewind(10000);
            _seconds -= 10000;
            PlayingProgress -= 10000;
            if (_seconds < 0)
            {
                _seconds = 0;
            }

            if (PlayingProgress < 0)
            {
                PlayingProgress = 0;
            }

            string displayProgress = TimeSpan.FromMilliseconds(_seconds).ToString("mm\\:ss");
            ElapsedTime = displayProgress + " / " + PlayingSong.DisplayDuration;
        }

        #endregion

        #region TimerControls
        private void StopTimers()
        {
            _incrementPlayingProgress.Stop();
            _findSongEnd.Stop();
        }

        private void StartTimers()
        {
            _incrementPlayingProgress.Start();
            _findSongEnd.Start();
        }
        #endregion
    }
}
