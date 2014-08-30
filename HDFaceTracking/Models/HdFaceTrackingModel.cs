using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using System.Collections.ObjectModel;

namespace HDFace3dTracking.Models
{
    class HdFaceTrackingModel : INotifyPropertyChanged
    {

        #region "変数"

        /// <summary>
        /// Kinectセンサーとの接続を示します
        /// </summary>
        private KinectSensor kinect;

        /// <summary>
        /// Kinectセンサーから複数のデータを受け取るためのFrameReaderを示します
        /// </summary>
        private MultiSourceFrameReader reader;

        /// <summary>
        /// Kinectセンサーから取得した骨格情報を示します
        /// </summary>
        private Body[] bodies;

        private FaceAlignment faceAlignment = null;
        private FaceModel faceModel = null;

        private FaceModelBuilderAttributes DefaultAttributes = FaceModelBuilderAttributes.SkinColor | FaceModelBuilderAttributes.HairColor;

        /// <summary>
        /// 顔情報データの取得元を示します
        /// </summary>
        private HighDefinitionFaceFrameSource hdFaceFrameSource = null;

        /// <summary>
        /// 顔情報データを受け取るためのFrameReaderを示します
        /// </summary>
        private HighDefinitionFaceFrameReader hdFaceFrameReader = null;

        /// <summary>
        /// 顔情報を使用して3Dモデルを作成するModelBuilderを示します
        /// </summary>
        private FaceModelBuilder faceModelBuilder = null;

        #endregion

        #region "プロパティ"
        private MeshGeometry3D _Geometry3d;
        /// <summary>
        /// Kinectセンサーから取得した顔情報のMeshを示します
        /// </summary>
        public MeshGeometry3D Geometry3d
        {
            get { return this._Geometry3d; }
            set
            {
                this._Geometry3d = value;
                OnPropertyChanged();
            }
        }

        private string _FaceModelBuilderStatus;
        /// <summary>
        /// FaceModelBuilderのモデル状態を示します
        /// </summary>
        public string FaceModelBuilderStatus
        {
            get { return this._FaceModelBuilderStatus; }
            set
            {
                this._FaceModelBuilderStatus = value;
                OnPropertyChanged();
            }
        }

        private string _FaceModelCaptureStatus;
        /// <summary>
        /// FaceModelBuilderの取得状況を示します
        /// </summary>
        public string FaceModelCaptureStatus
        {
            get { return this._FaceModelCaptureStatus; }
            set
            {
                this._FaceModelCaptureStatus = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Kinectセンサーから取得した顔の状態を示します
        /// </summary>
        public IReadOnlyDictionary<FaceShapeAnimations, float> AnimationUnits
        {
            get
            {
                if (this.faceAlignment == null) return null;
                return this.faceAlignment.AnimationUnits; 
            }
        }

        private Color _SkinColor;
        /// <summary>
        /// 現在のFaceModelのSkinColorを示します
        /// </summary>
        public Color SkinColor
        {
            get { return this._SkinColor; }
            set
            {
                this._SkinColor = value;
                OnPropertyChanged();
            }
        }


        private Color _HairColor;
        /// <summary>
        /// 現在のFaceModelのHairColorを示します
        /// </summary>
        public Color HairColor
        {
            get { return this._HairColor; }
            set
            {
                this._HairColor = value;
                OnPropertyChanged();
            }
        }

        #endregion

        /// <summary>
        /// センサーからの情報の取得を開始します
        /// </summary>
        public void Start()
        {
            Initialize();
        }

        /// <summary>
        /// センサーからの情報の取得を終了します
        /// </summary>
        public void Stop()
        {
            if (this.reader != null)
                this.reader.Dispose();

            this.hdFaceFrameSource = null;

            if (this.hdFaceFrameReader != null)
                this.hdFaceFrameReader.Dispose();

            if (this.faceModelBuilder != null)
                this.faceModelBuilder.Dispose();

            this.kinect.Close();
            this.kinect = null;
        }

        /// <summary>
        /// Kinectセンサーを初期化し、データの取得用に各種変数を初期化します
        /// </summary>
        private void Initialize()
        {
            // Kinectセンサーを取得
            this.kinect = KinectSensor.GetDefault();

            if (kinect == null) return;

            // KinectセンサーからBody(骨格情報)とColor(色情報)を取得するFrameReaderを作成
            reader = kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Body);
            reader.MultiSourceFrameArrived += OnMultiSourceFrameArrived;

            // Kinectセンサーから詳細なFaceTrackingを行う、ソースとFrameReaderを宣言
            this.hdFaceFrameSource = new HighDefinitionFaceFrameSource(this.kinect);
            this.hdFaceFrameSource.TrackingIdLost += this.OnTrackingIdLost;
            
            this.hdFaceFrameReader = this.hdFaceFrameSource.OpenReader();
            this.hdFaceFrameReader.FrameArrived += this.OnFaceFrameArrived;

            this.faceModel = new FaceModel();
            this.faceAlignment = new FaceAlignment();

            // 各種Viewのアップデート
            InitializeMesh();
            UpdateMesh();

            // センサーの開始
            kinect.Open();
        }

        /// <summary>
        /// 表示用のMeshを初期化します
        /// </summary>
        private void InitializeMesh()
        {
            // MeshGeometry3Dのインスタンスを作成
            this._Geometry3d = new MeshGeometry3D();

            // Vertexを計算する
            var vertices = this.faceModel.CalculateVerticesForAlignment(this.faceAlignment);

            var triangleIndices = this.faceModel.TriangleIndices;

            var indices = new Int32Collection(triangleIndices.Count);

            // 3つの点で1セット
            for (int i = 0; i < triangleIndices.Count; i += 3)
            {
                uint index1 = triangleIndices[i];
                uint index2 = triangleIndices[i + 1];
                uint index3 = triangleIndices[i + 2];

                indices.Add((int)index3);
                indices.Add((int)index2);
                indices.Add((int)index1);
            }

            this._Geometry3d.TriangleIndices = indices;
            this._Geometry3d.Normals = null;
            this._Geometry3d.Positions = new Point3DCollection();
            this._Geometry3d.TextureCoordinates = new PointCollection();

            foreach (var vert in vertices)
            {
                this._Geometry3d.Positions.Add(new Point3D(vert.X, vert.Y, -vert.Z));
                this._Geometry3d.TextureCoordinates.Add(new Point());
            }
            // 表示の更新
            OnPropertyChanged("Geometry3d");
        }

        /// <summary>
        /// Kinectセンサーから取得した情報を用いて表示用のMeshを更新します
        /// </summary>
        private void UpdateMesh()
        {
            var vertices = this.faceModel.CalculateVerticesForAlignment(this.faceAlignment);

            for (int i = 0; i < vertices.Count; i++)
            {
                var vert = vertices[i];
                this._Geometry3d.Positions[i] = new Point3D(vert.X, vert.Y, -vert.Z);
            }
            OnPropertyChanged("Geometry3d");
        }


        /// <summary>
        /// センサーから骨格データを受け取り処理します
        /// </summary>
        private void OnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var frame = e.FrameReference.AcquireFrame();
            if (frame == null) return;

            // BodyFrameに関してフレームを取得する
            using (var bodyFrame = frame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (bodies == null)
                        bodies = new Body[bodyFrame.BodyCount];

                    // 骨格データを格納
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    // FaceTrackingが開始されていないか確認
                    if (!this.hdFaceFrameSource.IsTrackingIdValid)
                    {
                        // トラッキング先の骨格を選択
                        var target = (from body in this.bodies where body.IsTracked select body).FirstOrDefault();
                        if (target != null)
                        {
                            // 検出されたBodyに対してFaceTrackingを行うよう、FaceFrameSourceを設定
                            hdFaceFrameSource.TrackingId = target.TrackingId;
                            // FaceModelBuilderを初期化
                            if (this.faceModelBuilder != null)
                            {
                                this.faceModelBuilder.Dispose();
                                this.faceModelBuilder = null;
                            }
                            this.faceModelBuilder = this.hdFaceFrameSource.OpenModelBuilder(DefaultAttributes);
                            // FaceModelBuilderがモデルの構築を完了した時に発生するイベント
                            this.faceModelBuilder.CollectionCompleted += this.OnModelBuilderCollectionCompleted;
                            // FaceModelBuilderの状態を報告するイベント
                            this.faceModelBuilder.CaptureStatusChanged += faceModelBuilder_CaptureStatusChanged;
                            this.faceModelBuilder.CollectionStatusChanged += faceModelBuilder_CollectionStatusChanged;

                            // キャプチャの開始
                            this.faceModelBuilder.BeginFaceDataCollection();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// FaceModelBuilderの収集状況が変更されたときのイベントを処理します
        /// </summary>
        private void faceModelBuilder_CollectionStatusChanged(object sender, FaceModelBuilderCollectionStatusChangedEventArgs e)
        {
            if (this.faceModelBuilder != null)
                this.FaceModelBuilderStatus = GetCollectionStatus(((FaceModelBuilder)sender).CollectionStatus);
        }

        /// <summary>
        /// FaceModelBuilderの取得状況が変更されたときのイベントを処理します
        /// </summary>
        private void faceModelBuilder_CaptureStatusChanged(object sender, FaceModelBuilderCaptureStatusChangedEventArgs e)
        {
            if (this.faceModelBuilder != null)
                this.FaceModelCaptureStatus = ((FaceModelBuilder)sender).CaptureStatus.ToString();
        }

        /// <summary>
        /// FaceTrackingの対象をロストしたときのイベントを処理します
        /// </summary>
        private void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            // faceReaderとfaceSourceを初期化して次のトラッキングに備える
            //this.isCaptured = false;
            this.hdFaceFrameSource.TrackingId = 0;
        }


        /// <summary>
        /// FaceModelBuilderがモデルの構築を完了したときのイベントを処理します
        /// </summary>
        private void OnModelBuilderCollectionCompleted(object sender, FaceModelBuilderCollectionCompletedEventArgs e)
        {
            var modelData = e.ModelData;
            this.faceModel = modelData.ProduceFaceModel();
            
            // MeshをUpdate
            UpdateMesh();

            // SkinColorとHairColorの値も更新
            this.SkinColor = UIntToColor(this.faceModel.SkinColor);
            this.HairColor = UIntToColor(this.faceModel.HairColor);
            
            // 更新を行う
            this.FaceModelBuilderStatus = GetCollectionStatus(((FaceModelBuilder)sender).CollectionStatus);
            this.FaceModelCaptureStatus = ((FaceModelBuilder)sender).CaptureStatus.ToString();

            this.faceModelBuilder.Dispose();
            this.faceModelBuilder = null;
        }

        /// <summary>
        /// FaceFrameが利用できるようになった時のイベントを処理します
        /// </summary>
        private void OnFaceFrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            using (var faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame == null || !faceFrame.IsFaceTracked) return;

                // FaceAlignmentを更新
                faceFrame.GetAndRefreshFaceAlignmentResult(this.faceAlignment);
                UpdateMesh();

                // Animation Unitを更新
                OnPropertyChanged("AnimationUnits");

            }
        }

        /// <summary>
        /// FaceModelBuilderのCollectionStatusを取得します
        /// </summary>
        private string GetCollectionStatus(FaceModelBuilderCollectionStatus status)
        {
            var msgs = new List<string>();

            if ((status & FaceModelBuilderCollectionStatus.FrontViewFramesNeeded) != 0)
                msgs.Add("FrontViewFramesNeeded");

            if ((status & FaceModelBuilderCollectionStatus.LeftViewsNeeded)!= 0)
                msgs.Add("LeftViewsNeeded");

            if ((status & FaceModelBuilderCollectionStatus.MoreFramesNeeded) != 0)
                msgs.Add("MoreFramesNeeded");

            if ((status & FaceModelBuilderCollectionStatus.RightViewsNeeded) != 0)
                msgs.Add("RightViewsNeeded");

            if ((status & FaceModelBuilderCollectionStatus.TiltedUpViewsNeeded) != 0)
                msgs.Add("TiltedUpViewsNeeded");

            if ((status & FaceModelBuilderCollectionStatus.Complete) != 0)
                msgs.Add("Complete!");

            return string.Join(" / ", msgs);
        }

        /// <summary>
        /// UIntの値をColorに変換します
        /// </summary>
        private Color UIntToColor(uint color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);
            return Color.FromArgb(a, r, g, b);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
