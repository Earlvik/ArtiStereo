using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Gat.Controls;
using Microsoft.Win32;

namespace Earlvik.ArtiStereo

{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private const double closeDistance = 0.25;
        //Number of toolbar buttons
        const int buttonNumber=5;
// ReSharper disable once InconsistentNaming
        int pixelPerMeter = 40;
        private double mReflectedVolume = 1;
        private const int minPixelPerMeter = 5;
        private const int maxPixelPerMeter = 320;
        private const int rightPanelWidth = 250;
        private String mFilename;
        private SoundPlayer mPlayer;
        //List of handlers assigned to canvas mouseUp event
        readonly List<MouseButtonEventHandler> mCanvasMousehandlers = new List<MouseButtonEventHandler>();
        
        readonly List<UIElement> mMarkers = new List<UIElement>();
        readonly List<IRoomObject> mRedoElements = new List<IRoomObject>();
        readonly ToggleButton[] mToolButtons = new ToggleButton[buttonNumber];
        readonly List<IRoomObject> mUndoElements = new List<IRoomObject>();
        private Sound mBaseSound;
        private int mChosenButton = -1;
        private Sound mResultSound;
        private Sound mConvolveBaseSound;
        private Sound mKernelSound;
        private Sound mConvolveResultSound;
        private double mXDrawOffset = 0;
        private double mYDrawOffset = 0;
        //Room object
        Room mRoom;
        private IRoomObject mSelectedRoomObject;
        private BackgroundWorker mReflectionWorker;
        private const double numFieldWidth = 40;

        public MainWindow()
        {
            
            InitializeComponent();
            mToolButtons[0] = RectButton;
            mToolButtons[1] = LineButton;
            mToolButtons[2] = SourceButton;
            mToolButtons[3] = ListenerButton;
            mToolButtons[4] = MoveButton;
            foreach (ToggleButton button in mToolButtons)
            {
                button.Checked += ToolButtonToggled;
                button.Unchecked += (sender, args) => { 
                    mChosenButton = -1;
                    mMarkers.Clear();
                    DrawRoom();
                    foreach (MouseButtonEventHandler handler in mCanvasMousehandlers)
                    {
                        RoomCanvas.MouseUp -= handler;
                    }
                    RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
                    RoomCanvas.MouseUp += RoomCanvas_MouseUp;
                };
            }
            mRoom = new Room();
            mRoom.CeilingHeight = 2;
            mRoom.CeilingMaterial = Wall.Material.Brick;
            mRoom.FloorMaterial =Wall.Material.Brick;
            
            Loaded += delegate
            {
                DrawRoom();
                RoomPresetBox.ItemsSource = Enum.GetNames(typeof (Room.RoomPreset));
                CeilingHeightBox.Text = mRoom.CeilingHeight + "";
                CeilingMaterialBox.ItemsSource = Enum.GetValues(typeof(Wall.MaterialPreset));
                CeilingMaterialBox.SelectedItem = Wall.GetPreset(mRoom.CeilingMaterial);
                FloorMaterialBox.ItemsSource = Enum.GetValues(typeof (Wall.MaterialPreset));
                FloorMaterialBox.SelectedItem = Wall.GetPreset(mRoom.FloorMaterial);
            };
            

        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void AddMarker(System.Windows.Point position)
        {
            var first = new Ellipse { Fill = Brushes.Black, Stroke = Brushes.Black };
            first.Width = first.Height = 5;
            Canvas.SetLeft(first, position.X);
            Canvas.SetTop(first, position.Y);
            first.Visibility = Visibility.Visible;
            mMarkers.Add(first);
        }

        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            mRoom = new Room();
            mRoom.CeilingHeight = 2;
            mRoom.CeilingMaterial = Wall.Material.Brick;
            mRoom.FloorMaterial = Wall.Material.Brick;
            mSelectedRoomObject = null;
            
            mMarkers.Clear();
            foreach (MouseButtonEventHandler handler in mCanvasMousehandlers)
            {
                RoomCanvas.MouseUp -= handler;
            }
            RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
            RoomCanvas.MouseUp += RoomCanvas_MouseUp;
            DrawRoom();
        }

        private void CheckRoom()
        {
            String message;
            if (!mRoom.IsValid(out message))
            {
                StatusBlock.Foreground = Brushes.Red;
                StatusBlock.Text = "Room is invalid. "+message;
            }
            else
            {
                StatusBlock.Foreground = Brushes.DarkGreen;
                StatusBlock.Text = "Room is valid";
            }
        }

        /// <summary>
        /// Method of repainting room objects on canvas based on mRoom objects
        /// </summary>
        private void DrawRoom()
        {
            CheckRoom();
            RoomCanvas.Children.Clear();
            //==================Grid=============================
            const double dx = 20;
            const double dy = 20;
            int i = 0;
            while(i*dy<RoomCanvas.ActualHeight-20){
                i++;
                var lineH = new System.Windows.Shapes.Line
                {
                    StrokeThickness = 0.1,
                    Stroke = Brushes.Black,
                    X1 = 0,
                    Y1 = i*dy,
                    X2 = RoomCanvas.ActualWidth,
                    Y2 = i*dy,
                    Visibility = Visibility.Visible
                };
                RoomCanvas.Children.Add(lineH);
            }
            i = 0;
            while(i*dx<RoomCanvas.ActualWidth-20){
                i++;
                var lineV = new System.Windows.Shapes.Line
                {
                    StrokeThickness = 0.1,
                    Stroke = Brushes.Black,
                    X1 = i*dx,
                    Y1 = 0,
                    X2 = i*dx,
                    Y2 = RoomCanvas.ActualHeight,
                    Visibility = Visibility.Visible
                };

                RoomCanvas.Children.Add(lineV);
                
            }

            //=========================WALLS========================

            foreach (Wall wall in mRoom.Walls)
            {
                var line = new System.Windows.Shapes.Line
                {
                    X1 = pixelPerMeter*wall.Start.X+mXDrawOffset,
                    X2 = pixelPerMeter*wall.End.X+mXDrawOffset,
                    Y1 = pixelPerMeter*wall.Start.Y+mYDrawOffset,
                    Y2 = pixelPerMeter*wall.End.Y+mYDrawOffset,
                    StrokeThickness = 3
                };
                if (ReferenceEquals(mSelectedRoomObject, wall))
                {
                    line.Stroke = Brushes.Chartreuse;
                }
                else
                {
                    switch (wall.MatPreset)
                    {
                        case Wall.MaterialPreset.Brick:
                        {
                            line.Stroke = Brushes.Orange;
                            break;
                        }
                        case Wall.MaterialPreset.Glass:
                        {
                            line.Stroke = Brushes.CornflowerBlue;
                            break;
                        }
                        case Wall.MaterialPreset.Granite:
                        {
                            line.Stroke = Brushes.LightSlateGray;
                            break;
                        }
                        case Wall.MaterialPreset.OakWood:
                        {
                            line.Stroke = Brushes.Chocolate;
                            break;
                        }
                        case Wall.MaterialPreset.Rubber:
                        {
                            line.Stroke = Brushes.DarkSalmon;
                            break;
                        }
                        default:
                        {
                            line.Stroke = Brushes.SteelBlue;
                            break;
                        }
                    }
                }
                line.Visibility = Visibility.Visible;
                RoomCanvas.Children.Add(line);
            }

            //===============================SOUND POINTS=======================
            const double picSize = 20;
            foreach (ListenerPoint listener in mRoom.Listeners)
            {
                
                var image = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/micPict.png")),
                    Width = picSize,
                    Height = picSize
                };
                double x = listener.X*pixelPerMeter-(picSize/2) + mXDrawOffset;
                double y = listener.Y*pixelPerMeter-(picSize/2) + mYDrawOffset;
                Canvas.SetTop(image,y);
                Canvas.SetLeft(image,x);
                if (ReferenceEquals(listener, mSelectedRoomObject))
                {
                    var ellipse = new Ellipse
                    {
                        Width = picSize,
                        Height = picSize,
                        Stroke = Brushes.Chartreuse,
                        Fill = Brushes.Chartreuse
                    };
                    Canvas.SetTop(ellipse,y);
                    Canvas.SetLeft(ellipse,x);
                    ellipse.Visibility = Visibility.Visible;
                    RoomCanvas.Children.Add(ellipse);
                }
                RoomCanvas.Children.Add(image);

                if (listener.Directional)
                {
                    double angle = listener.DirectionAngle;
                    var line = new System.Windows.Shapes.Line
                    {
                        X1 = x + (picSize/2),
                        Y1 = y + (picSize/2),
                        X2 = x + (picSize/2) + Math.Cos(angle)*(picSize/2),
                        Y2 = y + (picSize/2) + Math.Sin(angle)*(picSize/2),
                        Stroke = Brushes.Red,
                        StrokeThickness = 1
                    };
                    RoomCanvas.Children.Add(line);
                }
                
            }

            foreach (SoundPoint source in mRoom.Sources)
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Images/notePict.png")),
                    Width = picSize,
                    Height = picSize
                };
                double x = source.X * pixelPerMeter - (picSize / 2) +mXDrawOffset;
                double y = source.Y * pixelPerMeter - (picSize / 2) +mYDrawOffset;
                Canvas.SetTop(image, y);
                Canvas.SetLeft(image, x);
                if (ReferenceEquals(source, mSelectedRoomObject))
                {
                    Ellipse ellipse = new Ellipse
                    {
                        Width = picSize,
                        Height = picSize,
                        Stroke = Brushes.Chartreuse,
                        Fill = Brushes.Chartreuse
                    };
                    Canvas.SetTop(ellipse, y);
                    Canvas.SetLeft(ellipse, x);
                    ellipse.Visibility = Visibility.Visible;
                    RoomCanvas.Children.Add(ellipse);
                }
                RoomCanvas.Children.Add(image);
            }
            //===================MARKERS==============
            foreach (UIElement marker in mMarkers)
            {
                RoomCanvas.Children.Add(marker);
            }
            //===================ZOOM=BUTTONS=========
            Button zoomInButton = new Button{Content = "Zoom In"};
            Button zoomOutButton = new Button {Content = "Zoom Out"};
            Canvas.SetRight(zoomOutButton,10);
            Canvas.SetTop(zoomOutButton,5);
            Canvas.SetRight(zoomInButton,70);
            Canvas.SetTop(zoomInButton,5);
            RoomCanvas.Children.Add(zoomOutButton);
            RoomCanvas.Children.Add(zoomInButton);

            zoomInButton.Click += delegate
            {
                if (pixelPerMeter < maxPixelPerMeter)
                {
                    pixelPerMeter *= 2;
                    DrawRoom();
                    zoomOutButton.IsEnabled = true;
                    if (pixelPerMeter >= maxPixelPerMeter)
                    {
                        zoomInButton.IsEnabled = false;
                    }
                }
            };

            zoomOutButton.Click+=delegate
            {
                if (pixelPerMeter > minPixelPerMeter)
                {
                    pixelPerMeter /= 2;
                    DrawRoom();
                    zoomInButton.IsEnabled = true;
                    if (pixelPerMeter <= minPixelPerMeter)
                    {
                        zoomOutButton.IsEnabled = false;
                    }
                }
            };
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!mRoom.IsValid())
            {
                MessageBox.Show(this, "Room is invalid, check status bar for further instructions");
                return;
            }
            try
            {
                mReflectionWorker = new BackgroundWorker();
                mReflectionWorker.WorkerSupportsCancellation = true;
                RecordButton.IsEnabled = false;
                for (int i = 0; i < buttonNumber - 1; i++)
                {
                    mToolButtons[i].IsChecked = false;
                    mToolButtons[i].IsEnabled = false;
                }
                RoomOpenMenuItem.IsEnabled = false;
                SoundOpenMenuItem.IsEnabled = false;
                UndoMenuItem.IsEnabled = false;
                RedoMenuItem.IsEnabled = false;
                ClearMenuItem.IsEnabled = false;
                PropsAccItem.IsEnabled = false;
                PresetsAccItem.IsEnabled = false;
                CeilingHeightBox.IsEnabled = false;
                FloorMaterialBox.IsEnabled = false;
                RefVolumeSlider.IsEnabled = false;
                CancelButton.IsEnabled = true;

                RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
                ((TextBlock)PropsPanel.Children[0]).Text = "Properties";
                PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
                mSelectedRoomObject = null;
                DrawRoom();

                SoundProgressBar.Visibility = Visibility.Visible;
                SoundProgressBar.Value = 0;
                SoundProgressBar.Maximum = mRoom.Sources.Count*mRoom.Listeners.Count*(mRoom.Walls.Count*2+1)+2;
                mReflectionWorker.WorkerReportsProgress = true;
                int num = 0;
                EventHandler reporter= delegate
                {
                    mReflectionWorker.ReportProgress(num++);
                };
                mRoom.CalculationProgress += reporter;
                
                mReflectionWorker.DoWork += delegate
                {
                  //  try
                  //  {
                        mRoom.CalculateSound(mReflectedVolume);
                 //   }
                 //   catch (Exception ex)
                //    {
                 //       Dispatcher.Invoke((Action) delegate
                 //       {
                //            if (CancelButton.IsEnabled)
                 //           {
                //               MessageBox.Show(this, "Error occurred during recording process: " + ex.Message);
                //                throw ex;
                //            }

                 //       });
                //    }
                    
                };
                mReflectionWorker.RunWorkerCompleted += delegate
                {
                    if (!CancelButton.IsEnabled) return;
                    CancelButton.IsEnabled = false;
                    mResultSound = mRoom.GetSoundFromListeners();
                    mResultSound.AdjustVolume(0.75);
                    if (!mReflectionWorker.CancellationPending)
                    {
                        SoundSaveMenuItem.IsEnabled = true;
                        PlayButton.IsEnabled = true;
                    }
                    RecordButton.IsEnabled = true;
                    mRoom.CalculationProgress -= reporter;
                    SoundProgressBar.Visibility = Visibility.Hidden;
                    for (int i = 0; i<buttonNumber-1;i++)
                    {

                        mToolButtons[i].IsEnabled = true;

                    }
                    RoomOpenMenuItem.IsEnabled = true;
                    SoundOpenMenuItem.IsEnabled = true;
                    UndoMenuItem.IsEnabled = true;
                    RedoMenuItem.IsEnabled = true;
                    ClearMenuItem.IsEnabled = true;
                    PropsAccItem.IsEnabled = true;
                    PresetsAccItem.IsEnabled = true;
                    CeilingHeightBox.IsEnabled = true;
                    FloorMaterialBox.IsEnabled = true;
                    RefVolumeSlider.IsEnabled = true;
                    RoomCanvas.MouseUp+=RoomCanvas_MouseUp;
                    StatusBlock.Foreground=Brushes.DarkGreen;
                    
                    StatusBlock.Text = mReflectionWorker.CancellationPending?"Recording cancelled":"Recording successfully finished";
                };
                mReflectionWorker.ProgressChanged += (obj,args)=>
                {
                    SoundProgressBar.Value++;
                };
                mReflectionWorker.RunWorkerAsync();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, "Error occurred during recording process: " + exception.Message);
            }
        }

        private void RedoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            IRoomObject lastObject = mRedoElements.Last();
            mUndoElements.Add(lastObject);
            mRedoElements.Remove(lastObject);
            if (lastObject is Wall)
            {
                mRoom.AddWall(lastObject as Wall);
            }
            else if (lastObject is ListenerPoint)
            {
                mRoom.AddListener(lastObject as ListenerPoint);
            }
            else
            {
                mRoom.AddSource(lastObject as SoundPoint);
            }
            if (mRedoElements.Count == 0) RedoMenuItem.IsEnabled = false;
            UndoMenuItem.IsEnabled = true;
            DrawRoom();
        }

        private void RoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mChosenButton == 4)
            {
                double xPrev = e.GetPosition(this).X;
                double yPrev = e.GetPosition(this).Y;
                MouseEventHandler moveHandler = null;
                
                moveHandler = (moveSender, args) =>
                {

                    if (args.LeftButton == MouseButtonState.Released)
                    {
                        RoomCanvas.Cursor = Cursors.Arrow;
                        RoomCanvas.MouseMove -= moveHandler;
                        moveHandler = null;
                        DrawRoom();
                        return;
                    }
                    RoomCanvas.Cursor = Cursors.ScrollAll;
                    double dx = (args.GetPosition(this).X - xPrev);
                    double dy = (args.GetPosition(this).Y - yPrev);
                    mXDrawOffset += dx;
                    mYDrawOffset += dy;
                    xPrev = args.GetPosition(this).X;
                    yPrev = args.GetPosition(this).Y;
                    DrawRoom();
                    
                };
                RoomCanvas.MouseMove += moveHandler;
                return;
            }
            if (mSelectedRoomObject != null)
            {
                double xPrev = e.GetPosition(RoomCanvas).X;
                double yPrev = e.GetPosition(RoomCanvas).Y;
                if (mSelectedRoomObject is SoundPoint && Geometry.Distance(new Point((xPrev-mXDrawOffset)/pixelPerMeter,(yPrev-mYDrawOffset)/pixelPerMeter), mSelectedRoomObject as SoundPoint )<closeDistance)
                {
                    MouseEventHandler moveHandler = null;
                    moveHandler = (moveSender, args) =>
                    {
                       
                        if (args.LeftButton == MouseButtonState.Released)
                        {
                          
                            RoomCanvas.MouseMove -= moveHandler;
                            moveHandler = null;
                            DrawRoom();
                            UpdateProps(mSelectedRoomObject);
                            return;
                        }
                        double dx = (args.GetPosition(RoomCanvas).X - xPrev);
                        double dy = (args.GetPosition(RoomCanvas).Y - yPrev);
                        ((SoundPoint)mSelectedRoomObject).X += dx / pixelPerMeter;
                        ((SoundPoint)mSelectedRoomObject).Y += dy / pixelPerMeter;
                        xPrev = args.GetPosition(RoomCanvas).X;
                        yPrev = args.GetPosition(RoomCanvas).Y;
                        UpdateProps(mSelectedRoomObject);
                        DrawRoom();
                    };


                    RoomCanvas.MouseMove += moveHandler;

                }
                else if (mSelectedRoomObject is Wall)
                {
                    Point clickPoint = new Point((xPrev-mXDrawOffset)/pixelPerMeter, (yPrev-mYDrawOffset)/pixelPerMeter);
                    Point point =
                        (Geometry.Distance(clickPoint,
                            (mSelectedRoomObject as Wall).Start) <
                         Geometry.Distance(clickPoint,
                             (mSelectedRoomObject as Wall).End))
                            ? (mSelectedRoomObject as Wall).Start
                            : (mSelectedRoomObject as Wall).End;
                    if (Geometry.Distance(clickPoint, point) > closeDistance) return;
                    MouseEventHandler moveHandler = null;
                    moveHandler = (moveSender, moveE) =>
                    {
                        RoomCanvas.Cursor = Cursors.None;
                        if (moveE.LeftButton == MouseButtonState.Released)
                        {
                            //Magneting
                            foreach (Wall wall in mRoom.Walls)
                            {
                                if (Geometry.Distance(wall.Start, point) < closeDistance)
                                {
                                    point.X = wall.Start.X;
                                    point.Y = wall.Start.Y;
                                    wall.Start = point;
                                    break;
                                }
                                if (Geometry.Distance(wall.End, point) < closeDistance)
                                {
                                    point.X = wall.End.X;
                                    point.Y = wall.End.Y;
                                    wall.End = point;
                                    break;
                                }
                            }
                            RoomCanvas.Cursor = Cursors.Arrow;
                            RoomCanvas.MouseMove -= moveHandler;
                            moveHandler = null;
                            DrawRoom();
                            UpdateProps(mSelectedRoomObject);
                            return;
                        }
                        double dx = (moveE.GetPosition(RoomCanvas).X - xPrev);
                        double dy = (moveE.GetPosition(RoomCanvas).Y - yPrev);
                        point.X += dx/pixelPerMeter;
                        point.Y += dy/pixelPerMeter;
                        
                        xPrev = moveE.GetPosition(RoomCanvas).X;
                        yPrev = moveE.GetPosition(RoomCanvas).Y;
                        UpdateProps(mSelectedRoomObject);
                        DrawRoom();
                    };
                    RoomCanvas.MouseMove += moveHandler;
                }
            }
        }

        private void RoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (mSelectedRoomObject is Wall)
            {
                double xPrev = e.GetPosition(RoomCanvas).X;
                double yPrev = e.GetPosition(RoomCanvas).Y;
                Point clickPoint = new Point((xPrev-mXDrawOffset)/pixelPerMeter, (yPrev-mYDrawOffset)/pixelPerMeter);
                Point point =
                    (Geometry.Distance(clickPoint,
                        (mSelectedRoomObject as Wall).Start) <
                     Geometry.Distance(clickPoint,
                         (mSelectedRoomObject as Wall).End))
                        ? (mSelectedRoomObject as Wall).Start
                        : (mSelectedRoomObject as Wall).End;
                if (Geometry.Distance(clickPoint, point) >closeDistance)
                {
                    RoomCanvas.Cursor = Cursors.Arrow;
                    return;
                }
                RoomCanvas.Cursor = Cursors.Cross;
            }
            else
            {
                RoomCanvas.Cursor = Cursors.Arrow;
            }
        }

        private void RoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (mChosenButton)
            {
                case 0:
                {
                    //Add wall rectangle
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    AddMarker(position);
                    Point leftTop = new Point((position.X-mXDrawOffset)/pixelPerMeter, (position.Y-mYDrawOffset)/pixelPerMeter);
                    RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
                    MouseButtonEventHandler roomCanvasRectDraw = null;
                    roomCanvasRectDraw=(sndr, earg) =>
                    {
                        mMarkers.Remove(mMarkers.Last());
                        RoomCanvas.MouseUp-=roomCanvasRectDraw;
                        RoomCanvas.MouseUp += RoomCanvas_MouseUp;
                        position = earg.GetPosition(RoomCanvas);
                        Point bottomRight = new Point((position.X - mXDrawOffset) / pixelPerMeter, (position.Y - mYDrawOffset) / pixelPerMeter);
                        if (Geometry.Distance(leftTop, bottomRight) < 0.25)
                        {
                            mMarkers.Clear();
                            DrawRoom();
                            return;
                        }
                        Point topRight = new Point(bottomRight.X,leftTop.Y);
                        Point bottomLeft = new Point(leftTop.X,bottomRight.Y);
                        mUndoElements.Add(mRoom.AddWall(new Wall(leftTop,topRight,Wall.MaterialPreset.OakWood)));
                        mUndoElements.Add(mRoom.AddWall(new Wall(topRight, bottomRight, Wall.MaterialPreset.OakWood)));
                        mUndoElements.Add(mRoom.AddWall(new Wall(bottomRight, bottomLeft, Wall.MaterialPreset.OakWood)));
                        mUndoElements.Add(mRoom.AddWall(new Wall(leftTop, bottomLeft, Wall.MaterialPreset.OakWood)));
                        UndoMenuItem.IsEnabled = true;
                        DrawRoom();
                        
                    };
                    mCanvasMousehandlers.Add(roomCanvasRectDraw);
                    RoomCanvas.MouseUp += roomCanvasRectDraw;
                    break;
                }
                case 1:
                {
                    //Add wall
                   
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    AddMarker(position);
                    Point first = new Point((position.X-mXDrawOffset) / pixelPerMeter, (position.Y - mYDrawOffset) / pixelPerMeter);
                    //Magneting
                    foreach (Wall wall in mRoom.Walls)
                    {
                        if (Geometry.Distance(wall.Start, first) < closeDistance)
                        {
                            first = wall.Start;
                            break;
                        }
                        if (Geometry.Distance(wall.End, first) < closeDistance)
                        {
                            first = wall.End;
                            break;
                        }
                    }
                    RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
                    MouseButtonEventHandler roomCanvasLineDraw = null;
                    roomCanvasLineDraw = (sndr, earg) =>
                    {
                        mMarkers.Remove(mMarkers.Last());
                        RoomCanvas.MouseUp -= roomCanvasLineDraw;
                        RoomCanvas.MouseUp+=RoomCanvas_MouseUp;
                        position = earg.GetPosition(RoomCanvas);
                        Point second = new Point((position.X-mXDrawOffset) / pixelPerMeter, (position.Y - mYDrawOffset) / pixelPerMeter);
                        if (Geometry.Distance(first, second) < closeDistance)
                        {
                            mMarkers.Clear();
                            DrawRoom();
                            return;
                        }
                        //Magneting
                        foreach (Wall wall in mRoom.Walls)
                        {
                            if (Geometry.Distance(wall.Start, second) < closeDistance)
                            {
                                second = wall.Start;
                                break;
                            }
                            if (Geometry.Distance(wall.End, second) < closeDistance)
                            {
                                second = wall.End;
                                break;
                            }
                        }
                        mUndoElements.Add(mRoom.AddWall(new Wall(first,second,Wall.MaterialPreset.OakWood)));
                        UndoMenuItem.IsEnabled = true;
                        DrawRoom();
                    };
                    mCanvasMousehandlers.Add(roomCanvasLineDraw);
                    RoomCanvas.MouseUp += roomCanvasLineDraw;
                    break;
                }
                case 2:
                {
                    //Add source
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    SoundPoint point = new SoundPoint((position.X-mXDrawOffset)/pixelPerMeter, (position.Y-mYDrawOffset)/pixelPerMeter);
                    if (mBaseSound != null)
                    {
                        point.Sound = mBaseSound;
                    }
                    mUndoElements.Add(point);
                    mRoom.AddSource(point);
                    break;
                }
                case 3:
                {
                    //Add listener
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    ListenerPoint point = new ListenerPoint((position.X-mXDrawOffset) / pixelPerMeter, (position.Y-mYDrawOffset) / pixelPerMeter);
                    mUndoElements.Add(point);
                    mRoom.AddListener(point);
                    break;
                }
                case 4:
                {
                    break;
                }
                default:
                {
                    //Select element
                    System.Windows.Point position = e.GetPosition(RoomCanvas);
                    Point point = new Point((position.X-mXDrawOffset) / pixelPerMeter, (position.Y-mYDrawOffset) / pixelPerMeter);
                    mSelectedRoomObject = SelectObject(point);
                    if (mSelectedRoomObject == null)
                    {
                        ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
                        PropsPanel.Children.RemoveRange(1,PropsPanel.Children.Count-1);
                        mSelectedRoomObject = null;
                        DrawRoom();
                        return;
                    }
                    if (mSelectedRoomObject is Wall)
                    {
                        Wall wall = mSelectedRoomObject as Wall;
                        UpdateWallProps(wall);

                    }
                    else if (mSelectedRoomObject is ListenerPoint)
                    {
                        ListenerPoint listener = mSelectedRoomObject as ListenerPoint;
                        UpdateListenerProps(listener);
                    }
                    else if (mSelectedRoomObject is SoundPoint)
                    {
                        SoundPoint source = mSelectedRoomObject as SoundPoint;
                        UpdateSourceProps(source);
                    }
                    break;
                }
            }
            
            DrawRoom();
        }

        private void RoomCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawRoom();
        }

        private void RoomOpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { CheckFileExists = true, Filter = "Serialized room files (*.asr)|*.asr" };
            if (dialog.ShowDialog(this) == true)
            {
                if (!dialog.FileName.EndsWith(".asr"))
                {
                    MessageBox.Show(this, "Error occured during opening process: Wrong file format");
                    return;
                }
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(dialog.FileName, FileMode.Open))
                {
                    try
                    {
                        mRoom = (Room) formatter.Deserialize(stream);
                        mXDrawOffset = 0;
                        mYDrawOffset = 0;
                        DrawRoom();
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(this, "Error occured during opening process: " + exception.Message);
                    }
                }
            }
        }

        private void RoomSaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog { AddExtension = true, CheckPathExists = true, Filter = "Serialized room files (*.asr)|*.asr" };
            bool? result = dialog.ShowDialog(this);
            if (result == true)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(dialog.FileName, FileMode.OpenOrCreate))
                {
                    try
                    {
                        formatter.Serialize(stream, mRoom);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(this, "Error occured during saving process: " + exception.Message);
                    }
                }
            }
        }

        private IRoomObject SelectObject(Point point)
        {
            IRoomObject result = null;
            double distance = 0.3;
            foreach (Wall wall in mRoom.Walls)
            {
                if (Geometry.Distance(point, wall) < distance && Geometry.ParallelProjection(wall,point,false)!=null)
                {
                    distance = Geometry.Distance(point, wall);
                    result = wall;
                }
            }
            foreach (SoundPoint source in mRoom.Sources)
            {
                if (Geometry.Distance(point, source) < distance)
                {
                    distance = Geometry.Distance(point, source);
                    result = source;
                }
            }
            foreach (ListenerPoint listener in mRoom.Listeners)
            {
                if (Geometry.Distance(point, listener) < distance)
                {
                    distance = Geometry.Distance(point, listener);
                    result = listener;
                }
            }
            return result;
        }

        private void SoundOpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog {CheckPathExists = true, CheckFileExists = true, Filter = "Wave Sound Files (*.wav)|*.wav"};
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    mBaseSound = Sound.GetSoundFromWav(dialog.FileName);
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Error occured during opening: " + exception.Message);
                    return;
                }
                RecordButton.IsEnabled = true;
                MessageBox.Show("Successfully opened " + dialog.FileName,"Sound Open",MessageBoxButton.OK);
                foreach (SoundPoint source in mRoom.Sources)
                {
                    source.Sound = mBaseSound;
                }
                FileNameBlock.Text = "Source: "+dialog.SafeFileName;
                SoundSeries.DataContext = mBaseSound.ToKeyValuePairs(0);
               // Timeline.Visibility = Visibility.Visible;
                DrawRoom();
            }
        }

        private void SoundSaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (mResultSound != null)
            {
                SaveFileDialog dialog = new SaveFileDialog { AddExtension = true, CheckPathExists = true, Filter = "Wave Sound Files (*.wav)|*.wav" };
                if (dialog.ShowDialog(this) == true)
                {
                    try
                    {
                        mResultSound.CreateWav(dialog.FileName);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show("Error occured during saving: " + exception.Message);
                    }
                }
            }
        }

        private void ToolButtonToggled(object sender, RoutedEventArgs e)
        {
            ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            mSelectedRoomObject = null;
            int chosen = -1;
            mMarkers.Clear();
            DrawRoom();
            foreach (MouseButtonEventHandler handler in mCanvasMousehandlers)
            {
                RoomCanvas.MouseUp -= handler;
            }
            RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
            RoomCanvas.MouseUp+=RoomCanvas_MouseUp;
            for (int i = 0; i < buttonNumber; i++)
            {
                
                if (mToolButtons[i] == sender)
                {
                    chosen = i;
                }
                else
                {
                    mToolButtons[i].IsChecked = false;
                }
                mChosenButton = chosen;
            }
        }

        private void UndoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            IRoomObject lastObject = mUndoElements.Last();
            mRedoElements.Add(lastObject);
            mUndoElements.Remove(lastObject);
            if (lastObject is Wall)
            {
                mRoom.RemoveWall(lastObject as Wall);
            }
            else if (lastObject is ListenerPoint)
            {
                mRoom.RemoveListener(lastObject as ListenerPoint);
            }
            else
            {
                mRoom.RemoveSource(lastObject as SoundPoint);
            }
            if (mUndoElements.Count == 0) UndoMenuItem.IsEnabled = false;
            RedoMenuItem.IsEnabled = true;
            DrawRoom();
        }

        private void UpdateListenerProps(ListenerPoint listener)
        {
            PropsPanel.Children.Clear();
            TextBlock name = new TextBlock
            {
                FontSize = 20,
                Text = "LISTENER",
                TextAlignment = TextAlignment.Center,
                Width = rightPanelWidth
            };
            PropsPanel.Children.Add(name);
            TextBlock location = new TextBlock { FontSize = 14, Text = "Location", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
            PropsPanel.Children.Add(location);
            StackPanel locationPanel = new StackPanel { Orientation = Orientation.Horizontal, Width = rightPanelWidth };
            TextBlock xblock = new TextBlock { FontSize = 10, Text = "X: ", Margin = new Thickness(5, 5, 5, 0) };
            locationPanel.Children.Add(xblock);
            TextBox xbox = new TextBox{ FontSize = 10, Text = listener.X + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            locationPanel.Children.Add(xbox);
            TextBlock yblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
            locationPanel.Children.Add(yblock);
            TextBox ybox = new TextBox{ FontSize = 10, Text = listener.Y + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            locationPanel.Children.Add(ybox);
            PropsPanel.Children.Add(locationPanel);
            StackPanel altitudePanel = new StackPanel {Orientation = Orientation.Horizontal, Width = rightPanelWidth};
            TextBlock altitudeBlock = new TextBlock{FontSize = 10, Text = "Altitude: ",Margin = new Thickness(5,5,5,0)};
            TextBox altitudeBox = new TextBox { FontSize = 10, Text = listener.Altitude + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left, Margin = new Thickness(5, 5, 5, 0) };
            altitudePanel.Children.Add(altitudeBlock);
            altitudePanel.Children.Add(altitudeBox);
            PropsPanel.Children.Add(altitudePanel);
            StackPanel channelPanel = new StackPanel {Orientation = Orientation.Horizontal, Width = rightPanelWidth};
            TextBlock channelBlock = new TextBlock{FontSize = 14, Text = "Channel",Margin = new Thickness(5,5,5,0)};
            ComboBox channelBox = new ComboBox{Margin = new Thickness(5,5,5,5)};
            channelBox.ItemsSource = Enum.GetValues(typeof(Sound.Channel));
            channelBox.SelectedItem = listener.Channel;
            channelPanel.Children.Add(channelBlock);
            channelPanel.Children.Add(channelBox);
            PropsPanel.Children.Add(channelPanel);

            CheckBox directional = new CheckBox{Width = rightPanelWidth, Content = "Directional", IsChecked = listener.Directional};
            PropsPanel.Children.Add(directional);
            TextBox xdirbox = null;
            TextBox ydirbox = null;
            if (listener.Directional)
            {
                TextBlock direction = new TextBlock { FontSize = 14, Text = "Direction vector", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
                PropsPanel.Children.Add(direction);
                StackPanel directionPanel = new StackPanel { Orientation = Orientation.Horizontal, Width = rightPanelWidth };
                TextBlock xdirblock = new TextBlock { FontSize = 10, Text = "X: ", Margin = new Thickness(5, 5, 5, 0) };
                directionPanel.Children.Add(xdirblock);
                xdirbox = new TextBox { FontSize = 10, Text = listener.DirectionX + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
                directionPanel.Children.Add(xdirbox);
                TextBlock ydirblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
                directionPanel.Children.Add(ydirblock);
                ydirbox = new TextBox { FontSize = 10, Text = listener.DirectionY + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
                directionPanel.Children.Add(ydirbox);
                PropsPanel.Children.Add(directionPanel);
            }

            channelBox.SelectionChanged += (sender, args) =>
            {
                if (listener.Channel != (Sound.Channel)channelBox.SelectedItem && mRoom.Listeners.Any(x => x.Channel == (Sound.Channel)channelBox.SelectedItem))
                {
                    channelBox.SelectedItem = listener.Channel;
                }
                listener.Channel = (Sound.Channel) channelBox.SelectedItem;

            };

            directional.Checked += delegate
            {
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (mRoom.Listeners[i] == listener)
                    {
                        mRoom.Listeners[i]=new ListenerPoint(listener,new Line(0,0,1,1),ListenerPoint.Cardioid );
                        mSelectedRoomObject = mRoom.Listeners[i];
                        DrawRoom();
                        UpdateListenerProps(mRoom.Listeners[i]);
                        return;
                    }
                }
            };

            directional.Unchecked += delegate
            {
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (mRoom.Listeners[i] == listener)
                    {
                        mRoom.Listeners[i] = new ListenerPoint(listener);
                        mSelectedRoomObject = mRoom.Listeners[i];
                        DrawRoom();
                        UpdateListenerProps(mRoom.Listeners[i]);
                    }
                }
            };

            xbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(xbox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (mRoom.Listeners[i] == (ListenerPoint)mSelectedRoomObject)
                    {
                        mRoom.Listeners[i].X = newValue;
                        DrawRoom();
                        return;
                    }
                }
                
            };
            xbox.LostFocus += delegate
            {
                var listenerPoint = mSelectedRoomObject as ListenerPoint;
                if (listenerPoint != null)
                    xbox.Text = listenerPoint.X+"";
            };

            ybox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(ybox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (mRoom.Listeners[i] == (ListenerPoint)mSelectedRoomObject)
                    {
                        mRoom.Listeners[i].Y = newValue;
                        DrawRoom();
                        return;
                    }
                }

            };
            ybox.LostFocus += delegate
            {
                var listenerPoint = mSelectedRoomObject as ListenerPoint;
                if (listenerPoint != null)
                    ybox.Text = listenerPoint.Y + "";
            };

            altitudeBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(altitudeBox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Listeners.Count; i++)
                {
                    if (mRoom.Listeners[i] == (ListenerPoint)mSelectedRoomObject)
                    {
                        mRoom.Listeners[i].Altitude = newValue;
                        DrawRoom();
                        return;
                    }
                }

            };
            altitudeBox.LostFocus += delegate
            {
                var listenerPoint = mSelectedRoomObject as ListenerPoint;
                if (listenerPoint != null)
                    altitudeBox.Text = listenerPoint.Altitude + "";
            };

            if (xdirbox != null)
            {
                xdirbox.TextChanged += delegate
                {
                    double newValue;
                    if (!double.TryParse(xdirbox.Text, out newValue)) return;
                    for (int i = 0; i < mRoom.Listeners.Count; i++)
                    {
                        if (mRoom.Listeners[i] == (ListenerPoint) mSelectedRoomObject)
                        {
                            mRoom.Listeners[i].DirectionX = newValue;
                            DrawRoom();
                            return;
                        }
                    }

                };
                xdirbox.LostFocus += delegate
                {
                    var listenerPoint = mSelectedRoomObject as ListenerPoint;
                    if (listenerPoint != null)
                        xdirbox.Text = listenerPoint.DirectionX + "";
                };
            }

            if (ydirbox != null)
            {
                ydirbox.TextChanged += delegate
                {
                    double newValue;
                    if (!double.TryParse(ydirbox.Text, out newValue)) return;
                    for (int i = 0; i < mRoom.Listeners.Count; i++)
                    {
                        if (mRoom.Listeners[i] == (ListenerPoint)mSelectedRoomObject)
                        {
                            mRoom.Listeners[i].DirectionY = newValue;
                            DrawRoom();
                            return;
                        }
                    }

                };
                ydirbox.LostFocus += delegate
                {
                    var listenerPoint = mSelectedRoomObject as ListenerPoint;
                    if (listenerPoint != null)
                        ydirbox.Text = listenerPoint.DirectionY + "";
                };

                
            }
            Button deleteButton = new Button { Content = "Delete listener", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(10, 10, 10, 10) };
            PropsPanel.Children.Add(deleteButton);
            deleteButton.Click += delegate
            {
                ((TextBlock)PropsPanel.Children[0]).Text = "Properties";
                PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
                mRoom.RemoveListener(mSelectedRoomObject as ListenerPoint);
                mSelectedRoomObject = null;
                DrawRoom();
            };
        }

        private void UpdateProps(IRoomObject obj)
        {
            if (obj is Wall)
            {
                UpdateWallProps((Wall)obj);
            }
            else if (obj is ListenerPoint)
            {
                UpdateListenerProps((ListenerPoint) obj);
            }
            else if(obj is SoundPoint)
            {
                UpdateSourceProps((SoundPoint)obj);
            }
        }

        private void UpdateSourceProps(SoundPoint source)
        {
            PropsPanel.Children.Clear();
            TextBlock name = new TextBlock
            {
                FontSize = 20,
                Text = "SOURCE",
                TextAlignment = TextAlignment.Center,
                Width = rightPanelWidth
            };
            PropsPanel.Children.Add(name);
            TextBlock location = new TextBlock { FontSize = 14, Text = "Location", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
            PropsPanel.Children.Add(location);
            StackPanel locationPanel = new StackPanel { Orientation = Orientation.Horizontal, Width = rightPanelWidth };
            TextBlock xblock = new TextBlock { FontSize = 10, Text = "X: ", Margin = new Thickness(5, 5, 5, 0) };
            locationPanel.Children.Add(xblock);
            TextBox xbox = new TextBox { FontSize = 10, Text = source.X + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            locationPanel.Children.Add(xbox);
            TextBlock yblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
            locationPanel.Children.Add(yblock);
            TextBox ybox = new TextBox { FontSize = 10, Text = source.Y + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            locationPanel.Children.Add(ybox);
            PropsPanel.Children.Add(locationPanel);
            StackPanel altitudePanel = new StackPanel { Orientation = Orientation.Horizontal, Width = rightPanelWidth };
            TextBlock altitudeBlock = new TextBlock { FontSize = 10, Text = "Altitude: ", Margin = new Thickness(5, 5, 5, 0) };
            TextBox altitudeBox = new TextBox { FontSize = 10, Text = source.Altitude + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left, Margin = new Thickness(5, 5, 5, 0) };
            altitudePanel.Children.Add(altitudeBlock);
            altitudePanel.Children.Add(altitudeBox);
            PropsPanel.Children.Add(altitudePanel);
            Button deleteButton = new Button { Content = "Delete source", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(10, 10, 10, 10) };
            PropsPanel.Children.Add(deleteButton);

            xbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(xbox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Sources.Count; i++)
                {
                    if (mRoom.Sources[i] == (SoundPoint)mSelectedRoomObject)
                    {
                        mRoom.Sources[i].X = newValue;
                        DrawRoom();
                        return;
                    }
                }

            };
            xbox.LostFocus += delegate
            {
                var sourcePoint = mSelectedRoomObject as SoundPoint;
                if (sourcePoint != null)
                    xbox.Text = sourcePoint.X + "";
            };

            ybox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(ybox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Sources.Count; i++)
                {
                    if (mRoom.Sources[i] == (SoundPoint)mSelectedRoomObject)
                    {
                        mRoom.Sources[i].Y = newValue;
                        DrawRoom();
                        return;
                    }
                }

            };
            ybox.LostFocus += delegate
            {
                var sourcePoint = mSelectedRoomObject as SoundPoint;
                if (sourcePoint != null)
                    ybox.Text = sourcePoint.Y + "";
            };

            altitudeBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(altitudeBox.Text, out newValue)) return;
                for (int i = 0; i < mRoom.Sources.Count; i++)
                {
                    if (mRoom.Sources[i] == (SoundPoint)mSelectedRoomObject)
                    {
                        mRoom.Sources[i].Altitude = newValue;
                        DrawRoom();
                        return;
                    }
                }

            };
            altitudeBox.LostFocus += delegate
            {
                var sourcePoint = mSelectedRoomObject as SoundPoint;
                if (sourcePoint != null)
                    altitudeBox.Text = sourcePoint.Altitude + "";
            };
            deleteButton.Click += delegate
            {
                ((TextBlock)PropsPanel.Children[0]).Text = "Properties";
                PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
                mRoom.RemoveSource(mSelectedRoomObject as SoundPoint);
                mSelectedRoomObject = null;
                DrawRoom();
            };
        }

        private void UpdateWallProps(Wall wall)
        {
            PropsPanel.Children.Clear();
            TextBlock name = new TextBlock
            {
                FontSize = 20,
                Text = "WALL",
                TextAlignment = TextAlignment.Center,
                Width = rightPanelWidth
            };
            PropsPanel.Children.Add(name);
            TextBlock material = new TextBlock
            {
                FontSize = 14,
                Text = "Material",
                Width = rightPanelWidth,
                TextAlignment = TextAlignment.Center
            };
            PropsPanel.Children.Add(material);
            ComboBox materialBox = new ComboBox {Width = 180};
            //materialBox.Items.Add(Wall.MaterialPreset.Brick);
            //materialBox.Items.Add(Wall.MaterialPreset.Glass);
            //materialBox.Items.Add(Wall.MaterialPreset.Granite);
            //materialBox.Items.Add(Wall.MaterialPreset.OakWood);
            //materialBox.Items.Add(Wall.MaterialPreset.Rubber);
            //materialBox.Items.Add(Wall.MaterialPreset.None);
            materialBox.ItemsSource = Enum.GetValues(typeof (Wall.MaterialPreset));
            materialBox.SelectedItem = wall.MatPreset;
            PropsPanel.Children.Add(materialBox);
            TextBlock matParams = new TextBlock { FontSize = 14, Text = "Material paramaters", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
            PropsPanel.Children.Add(matParams);
            TextBlock density = new TextBlock { FontSize = 10, Text = "Density", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(density);
            TextBox densityBox = new TextBox { FontSize = 10, Text = wall.WallMaterial.Density + "", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(densityBox);
            TextBlock soundspeed = new TextBlock { FontSize = 10, Text = "Speed of sound", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(soundspeed);

            TextBox soundspeedBox = new TextBox
            {
                FontSize = 10,
                Text = wall.WallMaterial.SoundSpeed + "",
                Width = rightPanelWidth,
                TextAlignment = TextAlignment.Left
            };
            PropsPanel.Children.Add(soundspeedBox);

            TextBlock location = new TextBlock { FontSize = 14, Text = "Location", Width = rightPanelWidth, TextAlignment = TextAlignment.Center };
            PropsPanel.Children.Add(location);
            TextBlock beginning = new TextBlock { FontSize = 10, Text = "First point", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(beginning);
            StackPanel firstPanel = new StackPanel{Orientation = Orientation.Horizontal, Width = rightPanelWidth};
            TextBlock xbegblock = new TextBlock{FontSize = 10, Text = "X: ",Margin = new Thickness(5,5,5,0)};
            firstPanel.Children.Add(xbegblock);
            TextBox xbegbox = new TextBox{ FontSize = 10, Text = wall.Start.X+"",Width=numFieldWidth,TextAlignment = TextAlignment.Left};
            firstPanel.Children.Add(xbegbox);
            TextBlock ybegblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
            firstPanel.Children.Add(ybegblock);
            TextBox ybegbox = new TextBox { FontSize = 10, Text = wall.Start.Y + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            firstPanel.Children.Add(ybegbox);
            PropsPanel.Children.Add(firstPanel);

            TextBlock ending = new TextBlock { FontSize = 10, Text = "Second point", Width = rightPanelWidth, TextAlignment = TextAlignment.Left };
            PropsPanel.Children.Add(ending);
            StackPanel secondPanel = new StackPanel { Orientation = Orientation.Horizontal, Width = rightPanelWidth };
            TextBlock xendblock = new TextBlock { FontSize = 10, Text = "X: ", Margin = new Thickness(5, 5, 5, 0) };
            secondPanel.Children.Add(xendblock);
            TextBox xendbox = new TextBox { FontSize = 10, Text = wall.End.X + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            secondPanel.Children.Add(xendbox);
            TextBlock yendblock = new TextBlock { FontSize = 10, Text = "Y: ", Margin = new Thickness(5, 5, 5, 0) };
            secondPanel.Children.Add(yendblock);
            TextBox yendbox = new TextBox{ FontSize = 10, Text = wall.End.Y + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            secondPanel.Children.Add(yendbox);
            PropsPanel.Children.Add(secondPanel);

            TextBlock filtering = new TextBlock
            {
                FontSize = 14,
                Text = "Level of filtering by frequency",
                Width = rightPanelWidth,
                TextAlignment = TextAlignment.Center
            };
            PropsPanel.Children.Add(filtering);
            StackPanel freqPanel = new StackPanel{Orientation = Orientation.Horizontal,Width = rightPanelWidth};
            TextBlock lowBlock = new TextBlock {FontSize = 10, Text = "LOW: ", Margin = new Thickness(5, 5, 5, 0)};
            TextBox lowBox = new TextBox{FontSize = 10,Text = wall.WallMaterial.Low+"",Width = numFieldWidth,TextAlignment = TextAlignment.Left};
            freqPanel.Children.Add(lowBlock);
            freqPanel.Children.Add(lowBox);

            TextBlock medBlock = new TextBlock { FontSize = 10, Text = "MED: ", Margin = new Thickness(5, 5, 5, 0) };
            TextBox medBox = new TextBox { FontSize = 10, Text = wall.WallMaterial.Medium + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            freqPanel.Children.Add(medBlock);
            freqPanel.Children.Add(medBox);

            TextBlock highBlock = new TextBlock { FontSize = 10, Text = "HIGH: ", Margin = new Thickness(5, 5, 5, 0) };
            TextBox highBox = new TextBox { FontSize = 10, Text = wall.WallMaterial.High + "", Width = numFieldWidth, TextAlignment = TextAlignment.Left };
            freqPanel.Children.Add(highBlock);
            freqPanel.Children.Add(highBox);

            PropsPanel.Children.Add(freqPanel);
            Button deleteButton = new Button{Content = "Delete wall", HorizontalAlignment = HorizontalAlignment.Center,Margin = new Thickness(10,10,10,10)};
            PropsPanel.Children.Add(deleteButton);
            deleteButton.Click += delegate
            {
                ((TextBlock) PropsPanel.Children[0]).Text = "Properties";
                PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
                mRoom.RemoveWall(mSelectedRoomObject as Wall);
                mSelectedRoomObject = null;
                DrawRoom();
            };
            materialBox.SelectionChanged += delegate
            {
                for (int i = 0; i < mRoom.Walls.Count; i++)
                {

                    if (mRoom.Walls[i] == wall)
                    {
                        if ((Wall.MaterialPreset) materialBox.SelectedItem == Wall.MaterialPreset.None)
                        {
                            mRoom.Walls[i].MatPreset = Wall.MaterialPreset.None;
                            return;
                        }
                        mRoom.Walls[i] = new Wall(wall.Start, wall.End,
                            (Wall.MaterialPreset) materialBox.Items[materialBox.SelectedIndex]);
                        mSelectedRoomObject = mRoom.Walls[i];
                        UpdateWallProps(mSelectedRoomObject as Wall);
                    }
                }
            };

            densityBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(densityBox.Text, out newValue) || newValue<0) return;
                wall.WallMaterial.Density = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;
            };
            densityBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    densityBox.Text = wall1.WallMaterial.Density+"";
            };
            soundspeedBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(soundspeedBox.Text, out newValue) || newValue<0) return;
                wall.WallMaterial.SoundSpeed = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;
            };
            soundspeedBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    soundspeedBox.Text = wall1.WallMaterial.SoundSpeed + "";
            };

            lowBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(lowBox.Text, out newValue) || newValue > 1 || newValue < 0) return;
                wall.WallMaterial.Low = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;
            };
            lowBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    lowBox.Text = wall1.WallMaterial.Low + "";
            };


            medBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(medBox.Text, out newValue) || newValue > 1 || newValue < 0) return;
                wall.WallMaterial.Medium = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;

            };
            medBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    medBox.Text = wall1.WallMaterial.Medium + "";
            };


            highBox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(highBox.Text, out newValue) || newValue > 1 || newValue < 0) return;
                wall.WallMaterial.High = newValue;
                wall.MatPreset = Wall.MaterialPreset.None;
                materialBox.SelectedItem = Wall.MaterialPreset.None;

            };
            highBox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null)
                    highBox.Text = wall1.WallMaterial.High + "";
            };
            xbegbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(xbegbox.Text, out newValue)) return;
                ((Wall) mSelectedRoomObject).Start.X = newValue;
                DrawRoom();

            };
            xbegbox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null) xbegbox.Text = wall1.Start.X+"";
            };
            ybegbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(ybegbox.Text, out newValue)) return;
                ((Wall) mSelectedRoomObject).Start.Y = newValue;
                DrawRoom();

            };
            ybegbox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null) ybegbox.Text = wall1.Start.Y + "";
            };
            xendbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(xendbox.Text, out newValue)) return;
                ((Wall) mSelectedRoomObject).End.X = newValue;
                DrawRoom();

            };
            xendbox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null) xendbox.Text = wall1.End.X + "";
            };
            yendbox.TextChanged += delegate
            {
                double newValue;
                if (!double.TryParse(yendbox.Text, out newValue)) return;
                ((Wall) mSelectedRoomObject).End.Y = newValue;
                DrawRoom();

            };
            yendbox.LostFocus += delegate
            {
                var wall1 = mSelectedRoomObject as Wall;
                if (wall1 != null) yendbox.Text = wall1.End.Y + "";
            };
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SoundProgressBar.Width = Width / 2-20;
        }

        private void ApplyRoomPresetButton_Click(object sender, RoutedEventArgs e)
        {
            Room.RoomPreset preset;
            if (!Enum.TryParse(RoomPresetBox.SelectedItem.ToString(), out preset)) return;
            mRoom = Room.CreatePresetRoom(preset);
            mXDrawOffset = 0;
            mYDrawOffset = 0;
            
            ((TextBlock)PropsPanel.Children[0]).Text = "Properties";
            PropsPanel.Children.RemoveRange(1, PropsPanel.Children.Count - 1);
            mSelectedRoomObject = null;
            mMarkers.Clear();
            foreach (MouseButtonEventHandler handler in mCanvasMousehandlers)
            {
                RoomCanvas.MouseUp -= handler;
            }
            RoomCanvas.MouseUp -= RoomCanvas_MouseUp;
            RoomCanvas.MouseUp += RoomCanvas_MouseUp;
            DrawRoom();
        }

        private void CeilingHeightBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            double newValue;
            if (!double.TryParse(CeilingHeightBox.Text, out newValue)) return;
            mRoom.CeilingHeight = newValue;
        }

        private void CeilingHeightBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            CeilingHeightBox.Text = mRoom.CeilingHeight+"";
        }

        private void CeilingMaterialBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mRoom.CeilingMaterial = Wall.GetMaterial((Wall.MaterialPreset) CeilingMaterialBox.SelectedItem);
        }

        private void FloorMaterialBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mRoom.FloorMaterial = Wall.GetMaterial((Wall.MaterialPreset)FloorMaterialBox.SelectedItem);
        }

        private void RefVolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mReflectedVolume = e.NewValue;
        }

        private void OpenBaseSoundButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { CheckPathExists = true, CheckFileExists = true, Filter = "Wave Sound Files (*.wav)|*.wav" };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    mConvolveBaseSound = Sound.GetSoundFromWav(dialog.FileName);
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Error occured during opening: " + exception.Message);
                    ConvolveButton.IsEnabled = false;
                    return;
                }

                BaseSeries.DataContext = mConvolveBaseSound.ToKeyValuePairs(0);
                MessageBox.Show("Successfully opened " + dialog.FileName, "Sound Open", MessageBoxButton.OK);
                if (mKernelSound != null)
                {
                    if (CheckSound(mKernelSound, mConvolveBaseSound))
                    {
                        ConvolveButton.IsEnabled = true;
                        ConvolutionStatusBlock.Foreground = Brushes.Green;
                        ConvolutionStatusBlock.Text = "Everything is OK. You can start convolution.";
                    }
                    else
                    {
                        ConvolveButton.IsEnabled = false;
                        ConvolutionStatusBlock.Foreground = Brushes.Red;
                        ConvolutionStatusBlock.Text =
                            "Chosen sound and impulse response should have same sound parameters";
                    }
                }
                BaseSoundBlock.Text = dialog.SafeFileName;
            }
        }

        private bool CheckSound(Sound first, Sound second)
        {
            return (first.BitsPerSample == second.BitsPerSample);
        }

        private void OpenKernelSoundButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { CheckPathExists = true, CheckFileExists = true, Filter = "Wave Sound Files (*.wav)|*.wav" };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    mKernelSound = Sound.GetSoundFromWav(dialog.FileName);
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Error occured during opening: " + exception.Message);
                    ConvolveButton.IsEnabled = false;
                    return;
                }
                KernelSeries.DataContext = mKernelSound.ToKeyValuePairs(0);
                MessageBox.Show("Successfully opened " + dialog.FileName, "Sound Open", MessageBoxButton.OK);
                if (mConvolveBaseSound != null)
                {
                    if (CheckSound(mKernelSound, mConvolveBaseSound))
                    {
                        ConvolveButton.IsEnabled = true;
                        ConvolutionStatusBlock.Foreground=Brushes.Green;
                        ConvolutionStatusBlock.Text = "Everything is OK. You can start convolution.";
                    }
                    else
                    {
                        ConvolveButton.IsEnabled = false;
                        ConvolutionStatusBlock.Foreground = Brushes.Red;
                        ConvolutionStatusBlock.Text =
                            "Chosen sound and impulse response should have same sound parameters";
                    }
                }
               KernelSoundBlock.Text = dialog.SafeFileName;
            }
        }

        private void ConvolveButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker =new BackgroundWorker();
            ConvolutionStatusBlock.Text = "In progress...";
            ConvolveButton.IsEnabled = false;
            OpenBaseSoundButton.IsEnabled = false;
            OpenKernelSoundButton.IsEnabled = false;
            SaveConvolvedButton.IsEnabled = false;
            worker.DoWork += delegate
            {
                if (mKernelSound.Channels < mConvolveBaseSound.Channels)
                {
                    for (int i = 0; i < mConvolveBaseSound.Channels; i++)
                    {
                        mConvolveBaseSound.Convolve(mKernelSound, i, 0);
                    }
                }
                else
                {
                    if (mKernelSound.Channels > mConvolveBaseSound.Channels)
                    {
                        Sound TempSound = new Sound(mKernelSound.Channels,mConvolveBaseSound.DiscretionRate,mConvolveBaseSound.BitsPerSample);
                        for (int i = 0; i < mKernelSound.Channels; i++)
                        {
                           TempSound.Add(mConvolveBaseSound,0,i,0);
                        }
                        mConvolveBaseSound = TempSound;
                    }
                    for (int i = 0; i < mConvolveBaseSound.Channels; i++)
                    {
                        mConvolveBaseSound.Convolve(mKernelSound, i, i);
                    }
                }
                mConvolveResultSound = mConvolveBaseSound;
                mConvolveResultSound.AdjustVolume(0.8);
                mConvolveBaseSound = null;
                mKernelSound = null;

            };
            worker.RunWorkerCompleted += delegate
            {
                BaseSoundBlock.Text = KernelSoundBlock.Text = "No file loaded";
                ConvolutionStatusBlock.Text = "Convolution finished. Feel free to save result";
                mConvolveBaseSound = null;
                mKernelSound = null;
                if (mConvolveResultSound == null)
                {
                    ConvolutionStatusBlock.Foreground=Brushes.Red;
                    ConvolutionStatusBlock.Text = "NMath library needs to be installed for convolution";
                }
                OpenBaseSoundButton.IsEnabled = true;
                OpenKernelSoundButton.IsEnabled = true;
                SaveConvolvedButton.IsEnabled = true;
            };
            worker.RunWorkerAsync();
        }

        private void SaveConvolvedButton_Click(object sender, RoutedEventArgs e)
        {
            if (mConvolveResultSound != null)
            {
                SaveFileDialog dialog = new SaveFileDialog { AddExtension = true, CheckPathExists = true, Filter = "Wave Sound Files (*.wav)|*.wav" };
                if (dialog.ShowDialog(this) == true)
                {
                    try
                    {
                        if (!dialog.FileName.EndsWith(".wav"))
                        {
                            dialog.FileName += ".wav";
                        }
                        mConvolveResultSound.CreateWav(dialog.FileName);
                        mFilename = dialog.FileName;
                        PlayPauseButton.IsEnabled = true;
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show("Error occurred during saving: " + exception.Message);
                    }
                    finally
                    {
                        SaveConvolvedButton.IsEnabled = false;
                        ConvolutionStatusBlock.Foreground = Brushes.Red;
                        ConvolutionStatusBlock.Text = "Both files should be chosen";
                    }
                }
            }
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (mReflectionWorker != null)
            {
                CancelButton.IsEnabled = false;
                mReflectionWorker.CancelAsync();
                mReflectionWorker.RunWorkerCompleted += delegate
                {
                    foreach (ListenerPoint listener in mRoom.Listeners)
                    {
                        listener.Sound = null;
                    }
                };
                    RecordButton.IsEnabled = true;
                    SoundProgressBar.Visibility = Visibility.Hidden;
                    for (int i = 0; i<buttonNumber-1;i++)
                    {

                        mToolButtons[i].IsEnabled = true;

                    }
                    RoomOpenMenuItem.IsEnabled = true;
                    SoundOpenMenuItem.IsEnabled = true;
                    UndoMenuItem.IsEnabled = true;
                    RedoMenuItem.IsEnabled = true;
                    ClearMenuItem.IsEnabled = true;
                    PropsAccItem.IsEnabled = true;
                    PresetsAccItem.IsEnabled = true;
                    CeilingHeightBox.IsEnabled = true;
                    FloorMaterialBox.IsEnabled = true;
                    RefVolumeSlider.IsEnabled = true;
                    RoomCanvas.MouseUp+=RoomCanvas_MouseUp;
                    StatusBlock.Foreground=Brushes.DarkOrange;

                StatusBlock.Text = "Recording cancelled";
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.ApplicationLogo = Icon;
            about.Description =
                "Coursework done by Viktor Lopatin in NRU HSE 2014.\n" +
                " Program for Decomposition of Sound from Monophonic Source to Several Channels Considering " +
                "Acoustic Parameters of the Environment, Position of Instruments and the Listener";
            about.Copyright = "";
            about.AdditionalNotes = "";
            about.Version = "1.0";
            about.Show();
        }

        private void RefDepthSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mRoom != null) mRoom.ImageMaxDepth = (int)e.NewValue;
        }

        private void PlayPauseButton_Checked(object sender, RoutedEventArgs e)
        {
            mPlayer = new SoundPlayer(mFilename);
            mPlayer.Load();
            mPlayer.PlayLooping();
           
        }

        private void PlayPauseButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (mPlayer != null)
            {
                mPlayer.Stop();
            }
        }


        private void PlayButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (mResultSound != null)
            {
                const string filename = "~temp_sound.wav";
                mResultSound.CreateWav(filename);
                File.SetAttributes(filename,File.GetAttributes(filename)|FileAttributes.Hidden);
                mPlayer = new SoundPlayer(filename);
                mPlayer.Load();
                mPlayer.PlayLooping();

            }
        }

        private void PlayButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (mPlayer != null)
            {
                const string filename = "~temp_sound.wav";
                mPlayer.Stop();
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
            }
        }
    }
}
