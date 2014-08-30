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
using System.Windows.Media.Imaging;

namespace FaceTrackingBasic.Models
{
    class FaceTrackingModel : INotifyPropertyChanged
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
        /// 顔情報データの取得元を示します
        /// </summary>
        private FaceFrameSource faceSource;

        /// <summary>
        /// 顔情報データを受け取るためのFrameReaderを示します
        /// </summary>
        private FaceFrameReader faceReader;

        /// <summary>
        /// FaceFrameに渡す、解析用の項目を示します
        /// </summary>
        private const FaceFrameFeatures DefaultFaceFrameFeatures = FaceFrameFeatures.PointsInColorSpace
                                        | FaceFrameFeatures.Happy
                                        | FaceFrameFeatures.FaceEngagement
                                        | FaceFrameFeatures.Glasses
                                        | FaceFrameFeatures.LeftEyeClosed
                                        | FaceFrameFeatures.RightEyeClosed
                                        | FaceFrameFeatures.MouthOpen
                                        | FaceFrameFeatures.MouthMoved
                                        | FaceFrameFeatures.LookingAway
                                        | FaceFrameFeatures.RotationOrientation;

        /// <summary>
        /// Kinectセンサーから取得した骨格情報を示します
        /// </summary>
        private Body[] bodies;

        /// <summary>
        /// Kinectセンサーから取得した色情報用の一時的なバッファーを示します
        /// </summary>
        private byte[] colorPixels = null;

        /// <summary>
        /// FacePointBitmap描画用の再利用可能なレンダーを示します
        /// </summary>
        private DrawingVisual drawVisual = new DrawingVisual();

        /// <summary>
        /// Kinectセンサーから受信するピクセル単位のバイト数を示します
        /// </summary>
        private readonly int bytePerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        /// <summary>
        /// 顔のパーツを描画する際の色セットを示します
        /// </summary>
        private Brush[] facePointColor = new Brush[] {
            Brushes.Red,
            Brushes.Blue,
            Brushes.Green,
            Brushes.Yellow,
            Brushes.Purple
        };

        #endregion

        #region "プロパティ"

        private WriteableBitmap _ColorBitmap = null;
        /// <summary>
        /// Kinectセンサーから取得した表示用の色情報を示します
        /// </summary>
        public WriteableBitmap ColorBitmap
        {
            get { return _ColorBitmap; }
            set
            {
                _ColorBitmap = value;
                OnPropertyChanged();
            }
        }


        private RenderTargetBitmap _FacePointBitmap;
        /// <summary>
        /// Kinectセンサーから取得した顔のパーツの座標のマッピング情報を示します
        /// </summary>
        public RenderTargetBitmap FacePointBitmap
        {
            get { return _FacePointBitmap; }
            set
            {
                this._FacePointBitmap = value;
                OnPropertyChanged();
            }
        }

        private Vector4 _FaceRotation;
        /// <summary>
        /// 顔の回転情報を示します
        /// </summary>
        public Vector4 FaceRotation
        {
            get { return this._FaceRotation; }
            set
            {
                this._FaceRotation = value;
                OnPropertyChanged();
            }
        }

        /*
         * 
         * 　以下、表情分析の結果を示すプロパティ
         * 
         */

        private string _MouthMoved;
        public string MouthMoved
        {
            get { return this._MouthMoved; }
            set
            {
                this._MouthMoved = value;
                OnPropertyChanged();
            }
        }

        private string _MouthOpen;
        public string MouthOpen
        {
            get { return this._MouthOpen; }
            set
            {
                this._MouthOpen = value;
                OnPropertyChanged();
            }
        }

        private string _LeftEyeClosed;
        public string LeftEyeClosed
        {
            get { return this._LeftEyeClosed; }
            set
            {
                this._LeftEyeClosed = value;
                OnPropertyChanged();
            }
        }

        private string _RightEyeClosed;
        public string RightEyeClosed
        {
            get { return this._RightEyeClosed; }
            set
            {
                this._RightEyeClosed = value;
                OnPropertyChanged();
            }
        }

        private string _LookingAway;
        public string LookingAway
        {
            get { return this._LookingAway; }
            set
            {
                this._LookingAway = value;
                OnPropertyChanged();
            }
        }

        private string _Happy;
        public string Happy
        {
            get { return this._Happy; }
            set
            {
                this._Happy = value;
                OnPropertyChanged();
            }
        }

        private string _FaceEngagement;
        public string FaceEngagement
        {
            get { return this._FaceEngagement; }
            set
            {
                this._FaceEngagement = value;
                OnPropertyChanged();
            }
        }

        private string _Glasses;
        public string Glasses
        {
            get { return this._Glasses; }
            set
            {
                this._Glasses = value;
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

            if (this.faceSource != null)
                this.faceSource.Dispose();

            if (this.faceReader != null)
                this.faceReader.Dispose();

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

            if (this.kinect == null) return;

            // Kinectセンサーの情報を取得
            var desc = kinect.ColorFrameSource.FrameDescription;
            // 各種描画用変数をセンサー情報をもとに初期化
            this.colorPixels = new byte[desc.Width * desc.Height * bytePerPixel];
            this._ColorBitmap = new WriteableBitmap(desc.Width, desc.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this._FacePointBitmap = new RenderTargetBitmap(desc.Width, desc.Height, 96.0, 96.0, PixelFormats.Default);

            // KinectセンサーからBody(骨格情報)とColor(色情報)を取得するFrameReaderを作成
            this.reader = kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);
            this.reader.MultiSourceFrameArrived += OnMultiSourceFrameArrived;

            // FaceFrameSourceを作成
            faceSource = new FaceFrameSource(kinect,0, DefaultFaceFrameFeatures);

            // Readerを作成する
            faceReader = faceSource.OpenReader();

            // FaceReaderからフレームを受け取ることができるようになった際に発生するイベント
            faceReader.FrameArrived += OnFaceFrameArrived;
            // FaceFrameSourceが指定されたTrackingIdのトラッキングに失敗した際に発生するイベント
            faceSource.TrackingIdLost += OnTrackingIdLost;

            // センサーの開始
            kinect.Open();
        }

        /// <summary>
        /// センサーから骨格データ・色データを受け取り処理します
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
                    if (!this.faceSource.IsTrackingIdValid)
                    {
                        // トラッキング先の骨格を選択
                        var target = (from body in this.bodies where body.IsTracked select body).FirstOrDefault();
                        if (target != null)
                        {
                             // 検出されたBodyに対してFaceTrackingを行うよう、FaceFrameSourceを設定
                            this.faceSource.TrackingId = target.TrackingId;
                        }
                    }
                }
            }

            // ColorFrameに関してフレームを取得する
            using (var colorFrame = frame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    var desc = colorFrame.FrameDescription;

                    // FrameDescriptionが確保分のサイズと一致しているか確認する
                    if (desc.Width == _ColorBitmap.PixelWidth && desc.Height == _ColorBitmap.PixelHeight)
                    {
                        // データをColorPixelにコピー(ImageFormatが異なる場合は変換を行う)
                        if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                            colorFrame.CopyRawFrameDataToArray(colorPixels);
                        else
                            colorFrame.CopyConvertedFrameDataToArray(colorPixels, ColorImageFormat.Bgra);

                        // 描画
                        _ColorBitmap.WritePixels(
                            new Int32Rect(0, 0, _ColorBitmap.PixelWidth, _ColorBitmap.PixelHeight),
                            colorPixels,
                            _ColorBitmap.PixelWidth * bytePerPixel,
                        0);
                        OnPropertyChanged("ColorBitmap");
                    }
                }
            }

        }

        /// <summary>
        /// FaceTrackingの対象をロストしたときのイベントを処理します
        /// </summary>
        private void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            // UIの更新
            this.Happy = "NONE";
            this.FaceEngagement = "NONE";
            this.Glasses = "NONE";
            this.LeftEyeClosed = "NONE";
            this.RightEyeClosed = "NONE";
            this.MouthOpen = "NONE";
            this.MouthMoved = "NONE";
            this.LookingAway = "NONE";

            //// faceReaderとfaceSourceを初期化して次のトラッキングに備える(初期化)
            //this.faceSource.TrackingId = 0;
            //this.isCaptured = false;
        }

        /// <summary>
        /// FaceFrameが利用できるようになった時のイベントを処理します
        /// </summary>
        private void OnFaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (var faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame == null) return;

                // 顔情報に関するフレームを取得
                if (!faceFrame.IsTrackingIdValid)
                    return;
                
                var result = faceFrame.FaceFrameResult;
                if (result == null) return;

                // 表情等に関する結果を取得し、プロパティを更新
                this.Happy = result.FaceProperties[FaceProperty.Happy].ToString();
                this.FaceEngagement = result.FaceProperties[FaceProperty.Engaged].ToString();
                this.Glasses = result.FaceProperties[FaceProperty.WearingGlasses].ToString();
                this.LeftEyeClosed = result.FaceProperties[FaceProperty.LeftEyeClosed].ToString();
                this.RightEyeClosed = result.FaceProperties[FaceProperty.RightEyeClosed].ToString();
                this.MouthOpen = result.FaceProperties[FaceProperty.MouthOpen].ToString();
                this.MouthMoved = result.FaceProperties[FaceProperty.MouthMoved].ToString();
                this.LookingAway = result.FaceProperties[FaceProperty.LookingAway].ToString();

                // 顔の回転に関する結果を取得する
                this.FaceRotation = result.FaceRotationQuaternion;

                // カラーデータを描画する
                var drawContext = drawVisual.RenderOpen();

                // 顔の特徴点を取得し描画する
                foreach (var point in result.FacePointsInColorSpace)
                {
                    if (point.Key == FacePointType.None) continue;
                  
                    drawContext.DrawEllipse(facePointColor[(int)point.Key], null, new Point(point.Value.X, point.Value.Y), 5, 5);
                }
                drawContext.Close();

                // ビットマップの描画
                _FacePointBitmap.Clear();
                _FacePointBitmap.Render(drawVisual);

                OnPropertyChanged("FacePointBitmap");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
