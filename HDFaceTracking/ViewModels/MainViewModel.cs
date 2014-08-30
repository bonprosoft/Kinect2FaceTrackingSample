using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect.Face;

using HDFace3dTracking.Models;
using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace HDFace3dTracking.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {
        private HdFaceTrackingModel model = new HdFaceTrackingModel();

        public MainViewModel()
        {
            this.model.PropertyChanged += OnModelPropertyChanged;
        }

        #region "プロパティ"

        public MeshGeometry3D Geometry3d
        {
            get { return this.model.Geometry3d; }
        }

        public string FaceModelBuilderStatus
        {
            get { return this.model.FaceModelBuilderStatus; }
        }

        public string FaceModelCaptureStatus
        {
            get { return this.model.FaceModelCaptureStatus; }
        }

        public IReadOnlyDictionary<FaceShapeAnimations, float> AnimationUnits
        {
            get { return this.model.AnimationUnits; }
        }

        public Brush SkinColor
        {
            get { return new SolidColorBrush(this.model.SkinColor); }
        }

        public Brush HairColor
        {
            get { return new SolidColorBrush(this.model.HairColor); }
        }

        #endregion

        public void StartCommand()
        {
            this.model.Start();
        }

        public void StopCommand()
        {
            this.model.Stop();
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
