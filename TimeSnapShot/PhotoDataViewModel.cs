using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.Graphics.Imaging;

namespace TimeSnapShot
{
    
        public class PhotoDataViewModel : INotifyPropertyChanged
        {
            public delegate void PhotoDataNotedHandler(object sender);
                    
            private int _clickCount;
            private bool _playing;
            private Visibility _startControlsVisibility = Visibility.Visible;
            private Visibility _stopControlsVisibility = Visibility.Collapsed;

            public int ClickCount
            {
                get => _clickCount;
                set
                {
                    _clickCount = value;
                    OnPropertyChanged("ClickCount");
                }
            }

            public Visibility StartControlsVisibility
            {
                get => _startControlsVisibility;
                set
                {
                    _startControlsVisibility = value;
                    OnPropertyChanged("StartControlsVisibility");
                }
            }

            public Visibility StopControlsVisibility
            {
                get { return _stopControlsVisibility; }
                set
                {
                    _stopControlsVisibility = value;
                    OnPropertyChanged("StopControlsVisibility");
                }
            }

            public bool PhotoDataOping
            {
                get { return _playing; }
                set
                {
                    _playing = value;
                    OnPropertyChanged("PhotoDataOping");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public event PhotoDataNotedHandler PhotoDataCompleted;

            protected void OnPropertyChanged(string property)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            }

            protected void OnPhotoDataCompleted()
            {
                PhotoDataCompleted?.Invoke(this);
            }

            internal PhotoData StorePhotoData(string metadata, byte[] bmp)
            {
                var data = PhotoDataService.RecordPhotoData(metadata, bmp);
                StopPhotoData();
                OnPhotoDataCompleted();
                return data;
            }

            public void StartNewPhotoData()
            {
                ClickCount = 0;
                PhotoDataOping = true;
                StartControlsVisibility = Visibility.Collapsed;
                StopControlsVisibility = Visibility.Visible;
            }

        public void StopPhotoData()
        {
           
            StartControlsVisibility = Visibility.Visible;
            StopControlsVisibility = Visibility.Collapsed;
            PhotoDataOping = false;
        }

        public void HandleClick()
            {
                if (PhotoDataOping) ClickCount++;
            }
        }
    
}
