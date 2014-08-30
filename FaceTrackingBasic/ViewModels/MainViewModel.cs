using FaceTrackingBasic.Models;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FaceTrackingBasic.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        private FaceTrackingModel model = new FaceTrackingModel();

        public MainViewModel()
        {
            this.model.PropertyChanged += OnModelPropertyChanged;

        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        public WriteableBitmap ColorBitmap
        {
            get { return this.model.ColorBitmap; }
        }

        public RenderTargetBitmap FacePointBitmap
        {
            get { return this.model.FacePointBitmap; }
        }

        public Vector4 FaceRotation
        {
            get { return this.model.FaceRotation; }
        }

        public string Happy
        {
            get { return this.model.Happy; }
        }

        public string MouthMoved
        {
            get { return this.model.MouthMoved; }
        }

        public string MouthOpen
        {
            get { return this.model.MouthOpen; }
        }

        public string LeftEyeClosed
        {
            get { return this.model.LeftEyeClosed; }
        }

        public string RightEyeClosed
        {
            get { return this.model.RightEyeClosed; }
        }

        public string LookingAway
        {
            get { return this.model.LookingAway; }
        }

        public string FaceEngagement
        {
            get { return this.model.FaceEngagement; }
        }

        public string Glasses
        {
            get { return this.model.Glasses; }
        }


        public void StartCommand()
        {
            this.model.Start();
        }

        public void StopCommand()
        {
            this.model.Stop();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
