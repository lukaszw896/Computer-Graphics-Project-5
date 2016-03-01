using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;

namespace CGProject5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WriteableBitmap writeableBitmap;
        public int lineThickness = 1;
        myPoint[] pointsTable = new myPoint[24];
        float[][] projectionMatrix = new float[4][];
        float[][] YRotationMatrix = new float[4][];
        float[][] XRotationMatrix = new float[4][];
        float[][] matrixT = new float[4][];
        int Cx = 500;
        int Cy = 350;
        int d = (int)((float)350 * 0.48);
        double YDegrees = 0;
        double XDegrees = 0;
        double CylinderDegreeY = 0;
        double degreeStep = 2;

        object objectLock = new object();
        object putPixelLock = new object();

        public Thread movementThread;

        public MainWindow()
        {
            writeableBitmap = new WriteableBitmap(1000,700, 300, 300, System.Windows.Media.PixelFormats.Bgra32, null);
            InitializeComponent();
            myImage.Source = writeableBitmap;
            InitializePointTable();
            InitializeProjectionMatrix();
            InitializeYRotationMatrix();
            InitializeXRotationMatrix();
            ChangeXRotationMatrix();
            ChangeYRotationMatrix(0);
            InitializeTranslationMatrix();
            Draw();
            movementThread = new Thread(new ThreadStart(MoveCylinderRight));
            movementThread.Start();

        }

        void MidpointLine(int x1, int y1, int x2, int y2)
        {
            bool thickenX;
            int longerAxis;
            int shorterAxis;
            int dx = x2 - x1;
            int dy = y2 - y1;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (dx < 0) dx1 = -1; else if (dx > 0) dx1 = 1;
            if (dy < 0) dy1 = -1; else if (dy > 0) dy1 = 1;
            if (Math.Abs(dx) <= Math.Abs(dy))
            {
                thickenX = false;
                longerAxis = Math.Abs(dy);
                shorterAxis = Math.Abs(dx);
                if (dy < 0) dy2 = -1; else if (dy > 0) dy2 = 1;
            }
            else
            {
                thickenX = true;
                longerAxis = Math.Abs(dx);
                shorterAxis = Math.Abs(dy);
                if (dx < 0) dx2 = -1; else if (dx > 0) dx2 = 1;
            }
            int numerator = longerAxis;
            #region line thickness
            int loopStartValue = lineThickness / 2 * (-1);
            int loopEndValue;
            if (lineThickness == 1)
            {
                loopEndValue = 1;
            }
            else
                loopEndValue = lineThickness / 2;
            #endregion
            for (int i = 0; i <= longerAxis; i++)
            {
                if (thickenX)
                {
                    for (int j = loopStartValue; j < loopEndValue; j++)
                    {
                        putPixel(x1, y1 + j);
                    }
                }
                else
                {
                    for (int j = loopStartValue; j < loopEndValue; j++)
                    {
                        putPixel(x1 + j, y1);
                    }
                }
                numerator += shorterAxis;
                if (numerator >= longerAxis)
                {
                    numerator -= longerAxis;
                    x1 += dx1;
                    y1 += dy1;
                }
                else
                {
                    x1 += dx2;
                    y1 += dy2;
                }
            }
        }
        private void putPixel(int x, int y)
        {
            int column = x;
            int row = y;

            lock (putPixelLock)
            {
                writeableBitmap.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Reserve the back buffer for updates.
                    writeableBitmap.Lock();

                    unsafe
                    {
                        // Get a pointer to the back buffer.
                        int pBackBuffer = (int)writeableBitmap.BackBuffer;

                        // Find the address of the pixel to draw.
                        pBackBuffer += row * writeableBitmap.BackBufferStride;
                        pBackBuffer += column * 4;

                        // Compute the pixel's color.
                        int color_data = 255 << 24; // R
                        color_data |= 0 << 16;   // G
                        color_data |= 0 << 8;   // B
                        color_data |= 0 << 0;
                        // Assign the color data to the pixel.
                        *((int*)pBackBuffer) = color_data;
                    }

                    // Specify the area of the bitmap that changed.
                    writeableBitmap.AddDirtyRect(new Int32Rect(column, row, 1, 1));

                    // Release the back buffer and make it available for display.
                    writeableBitmap.Unlock();
                }));
            }
            
        }
        private void InitializePointTable()
        {
            //Central cube
            pointsTable[0] = new myPoint(-1.0f, -1.0f, -1.0f, 1.0f);
            pointsTable[1] = new myPoint(1.0f, -1.0f, -1.0f, 1.0f);
            pointsTable[2] = new myPoint(1.0f, 1.0f, -1.0f, 1.0f);
            pointsTable[3] = new myPoint(-1.0f, 1.0f, -1.0f, 1.0f);
            pointsTable[4] = new myPoint(-1.0f, 1.0f, 1.0f, 1.0f);
            pointsTable[5] = new myPoint(1.0f, 1.0f, 1.0f, 1.0f);
            pointsTable[6] = new myPoint(1.0f, -1.0f, 1.0f, 1.0f);
            pointsTable[7] = new myPoint(-1.0f, -1.0f, 1.0f, 1.0f);

            //second cube

            pointsTable[8] = new myPoint(4.5f, 0f, -1.0f, 1.0f);
            pointsTable[9] = new myPoint(4.5f, 0f, 1.0f, 1.0f);
            pointsTable[10] = new myPoint(4.145f, -0.145f, -1.0f, 1.0f);
            pointsTable[11] = new myPoint(4.145f, -0.145f, 1.0f, 1.0f);
            pointsTable[12] = new myPoint(4.0f, -0.5f, -1.0f, 1.0f);
            pointsTable[13] = new myPoint(4.0f, -0.5f, 1.0f, 1.0f);
            pointsTable[14] = new myPoint(4.145f, -0.855f, -1.0f, 1.0f);
            pointsTable[15] = new myPoint(4.145f, -0.855f, 1.0f, 1.0f);
            pointsTable[16] = new myPoint(4.5f, -1.0f, -1.0f, 1.0f);
            pointsTable[17] = new myPoint(4.5f, -1.0f, 1.0f, 1.0f);
            pointsTable[18] = new myPoint(4.855f, -0.855f, -1.0f, 1.0f);
            pointsTable[19] = new myPoint(4.855f, -0.855f, 1.0f, 1.0f);
            pointsTable[20] = new myPoint(5.0f, -0.5f, -1.0f, 1.0f);
            pointsTable[21] = new myPoint(5.0f, -0.5f, 1.0f, 1.0f);
            pointsTable[22] = new myPoint(4.855f, -0.145f, -1.0f, 1.0f);
            pointsTable[23] = new myPoint(4.855f, -0.145f, 1.0f, 1.0f);
            //pointsTable[24] = new myPoint(4.0f, -1.0f, -1.0f, 1.0f);

            
            
        }
        private void InitializeProjectionMatrix()
        {
            for (int i = 0; i < 4; i++)
            {
                projectionMatrix[i] = new float[4];
            }
            for(int i=0;i<4;i++){
                for(int j=0;j<4;j++){
                    projectionMatrix[i][j]=0;
                }
            }
            projectionMatrix[0][0] = d;
            projectionMatrix[1][1] = -d;
            projectionMatrix[2][0] = Cx;
            projectionMatrix[2][1] = Cy;
            projectionMatrix[3][2] = 1;
            projectionMatrix[2][3] = 1;
        }
        private void InitializeYRotationMatrix()
        {
            for (int i = 0; i < 4; i++)
            {
                YRotationMatrix[i] = new float[4];
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    YRotationMatrix[i][j] = 0;
                }
            }
        }
        private void InitializeXRotationMatrix()
        {
            for (int i = 0; i < 4; i++)
            {
                XRotationMatrix[i] = new float[4];
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    XRotationMatrix[i][j] = 0;
                }
            }
        }
        private void ChangeYRotationMatrix(int pointNum)
        {
            double angle;
            if (pointNum < 4)
            {
                angle = Math.PI * YDegrees / 180.0;
            }
            else
            {
                angle = Math.PI * (YDegrees + CylinderDegreeY)/ 180.0;
            }
            YRotationMatrix[0][0] = (float) Math.Cos(angle);
            YRotationMatrix[2][0] = -(float)Math.Sin(angle);
            YRotationMatrix[1][1] = 1;
            YRotationMatrix[0][2] = (float) Math.Sin(angle);
            YRotationMatrix[2][2] = (float)Math.Cos(angle);
            YRotationMatrix[3][3] = 1;
        }
        private void ChangeXRotationMatrix()
        {
            double angle = Math.PI * XDegrees / 180.0;
            XRotationMatrix[0][0] = 1;
            XRotationMatrix[1][1] = (float)Math.Cos(angle);
            XRotationMatrix[2][1] = -(float)Math.Sin(angle);
            XRotationMatrix[1][2] = (float)Math.Sin(angle);
            XRotationMatrix[2][2] = (float)Math.Cos(angle);
            XRotationMatrix[3][3] = 1;
        }
        private void Draw()
        {

                for (int i = 0; i < 24; i++)
                {
                    PointPosition(pointsTable[i], i);
                }

                for (int i = 0; i < 1; i++)
                {

                    //front
                    MidpointLine((int)pointsTable[0 + (i * 8)].coordinates[0], (int)pointsTable[0 + (i * 8)].coordinates[1], (int)pointsTable[1 + (i * 8)].coordinates[0], (int)pointsTable[1 + (i * 8)].coordinates[1]);
                    MidpointLine((int)pointsTable[1 + (i * 8)].coordinates[0], (int)pointsTable[1 + (i * 8)].coordinates[1], (int)pointsTable[2 + (i * 8)].coordinates[0], (int)pointsTable[2 + (i * 8)].coordinates[1]);
                    MidpointLine((int)pointsTable[2 + (i * 8)].coordinates[0], (int)pointsTable[2 + (i * 8)].coordinates[1], (int)pointsTable[3 + (i * 8)].coordinates[0], (int)pointsTable[3 + (i * 8)].coordinates[1]);
                    MidpointLine((int)pointsTable[3 + (i * 8)].coordinates[0], (int)pointsTable[3 + (i * 8)].coordinates[1], (int)pointsTable[0 + (i * 8)].coordinates[0], (int)pointsTable[0 + (i * 8)].coordinates[1]);

                    //back
                    MidpointLine((int)pointsTable[4 + (i * 8)].coordinates[0], (int)pointsTable[4 + (i * 8)].coordinates[1], (int)pointsTable[5 + (i * 8)].coordinates[0], (int)pointsTable[5 + (i * 8)].coordinates[1]);
                    MidpointLine((int)pointsTable[5 + (i * 8)].coordinates[0], (int)pointsTable[5 + (i * 8)].coordinates[1], (int)pointsTable[6 + (i * 8)].coordinates[0], (int)pointsTable[6 + (i * 8)].coordinates[1]);
                    MidpointLine((int)pointsTable[6 + (i * 8)].coordinates[0], (int)pointsTable[6 + (i * 8)].coordinates[1], (int)pointsTable[7 + (i * 8)].coordinates[0], (int)pointsTable[7 + (i * 8)].coordinates[1]);
                    MidpointLine((int)pointsTable[7 + (i * 8)].coordinates[0], (int)pointsTable[7 + (i * 8)].coordinates[1], (int)pointsTable[4 + (i * 8)].coordinates[0], (int)pointsTable[4 + (i * 8)].coordinates[1]);


                    //checked
                    MidpointLine((int)pointsTable[0 + (i * 8)].coordinates[0], (int)pointsTable[0 + (i * 8)].coordinates[1], (int)pointsTable[7 + (i * 8)].coordinates[0], (int)pointsTable[7 + (i * 8)].coordinates[1]);
                    MidpointLine((int)pointsTable[1 + (i * 8)].coordinates[0], (int)pointsTable[1 + (i * 8)].coordinates[1], (int)pointsTable[6 + (i * 8)].coordinates[0], (int)pointsTable[6 + (i * 8)].coordinates[1]);
                    //checked
                    MidpointLine((int)pointsTable[2 + (i * 8)].coordinates[0], (int)pointsTable[2 + (i * 8)].coordinates[1], (int)pointsTable[5 + (i * 8)].coordinates[0], (int)pointsTable[5 + (i * 8)].coordinates[1]);
                    MidpointLine((int)pointsTable[3 + (i * 8)].coordinates[0], (int)pointsTable[3 + (i * 8)].coordinates[1], (int)pointsTable[4 + (i * 8)].coordinates[0], (int)pointsTable[4 + (i * 8)].coordinates[1]);

                }
                MidpointLine((int)pointsTable[8].coordinates[0], (int)pointsTable[8].coordinates[1], (int)pointsTable[9].coordinates[0], (int)pointsTable[9].coordinates[1]);
                MidpointLine((int)pointsTable[10].coordinates[0], (int)pointsTable[10].coordinates[1], (int)pointsTable[11].coordinates[0], (int)pointsTable[11].coordinates[1]);
                MidpointLine((int)pointsTable[12].coordinates[0], (int)pointsTable[12].coordinates[1], (int)pointsTable[13].coordinates[0], (int)pointsTable[13].coordinates[1]);
                MidpointLine((int)pointsTable[14].coordinates[0], (int)pointsTable[14].coordinates[1], (int)pointsTable[15].coordinates[0], (int)pointsTable[15].coordinates[1]);
                MidpointLine((int)pointsTable[16].coordinates[0], (int)pointsTable[16].coordinates[1], (int)pointsTable[17].coordinates[0], (int)pointsTable[17].coordinates[1]);
                MidpointLine((int)pointsTable[18].coordinates[0], (int)pointsTable[18].coordinates[1], (int)pointsTable[19].coordinates[0], (int)pointsTable[19].coordinates[1]);
                MidpointLine((int)pointsTable[20].coordinates[0], (int)pointsTable[20].coordinates[1], (int)pointsTable[21].coordinates[0], (int)pointsTable[21].coordinates[1]);
                MidpointLine((int)pointsTable[22].coordinates[0], (int)pointsTable[22].coordinates[1], (int)pointsTable[23].coordinates[0], (int)pointsTable[23].coordinates[1]);

                int[] points = new int[16];
                int[] points2 = new int[16]; 
                for(int i=8;i<23;i+=2){
                    points[i-8]=(int)pointsTable[i].coordinates[0];
                    points[i-7]=(int)pointsTable[i].coordinates[1];
                    points2[i - 8] = (int)pointsTable[i+1].coordinates[0];
                    points2[i - 7] = (int)pointsTable[i+1].coordinates[1];
                }

                lock (putPixelLock)
                {
                    writeableBitmap.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        writeableBitmap.DrawCurveClosed(points, 0.5f, Colors.Black);
                        writeableBitmap.DrawCurveClosed(points2, 0.5f, Colors.Black);
                    }));
                    
                }

            
                



        }
        private void InitializeTranslationMatrix()
        {
            for (int i = 0; i < 4; i++)
            {
                matrixT[i] = new float[4];
                for (int j = 0; j < 4; j++)
                {
                    if (i == j)
                    {
                        matrixT[i][j] = 1;
                    }
                    else
                    {
                        matrixT[i][j] = 0;
                    }
                }
            }
            matrixT[3][2] = 7;
        }

        private void PointPosition(myPoint mP,int pointNum)
        {
            if (pointNum == 8)
            {
                ChangeYRotationMatrix(9);
            }
            float[][] pointMatrix = new float[4][];
            float[][] postTranslationMatrix = new float[4][];
            for (int i = 0; i < 4; i++)
            {
                pointMatrix[i] = new float[4];
                postTranslationMatrix[i] = new float[4];
                for (int j = 0; j < 4; j++)
                {
                    pointMatrix[i][j] = 0;
                    postTranslationMatrix[i][j] = 0;
                }
            }

            /******************ROTATION * POINTS *******/
            float[] currentVector = new float[4];
            for (int i = 0; i < 4; i++) currentVector[i] = 0;

            for (int i = 0; i < 4; i++)
            {
                float sum = 0;
                for (int j = 0; j < 4; j++)
                {
                    sum += mP.coordinates[j] * YRotationMatrix[j][i];
                }
                currentVector[i] = sum;
            }

            float[] tmpVector = new float[4];
            for (int j = 0; j < 4; j++)
            {
                tmpVector[j] = currentVector[j];
            }
            for (int i = 0; i < 4; i++)
            {
                float sum = 0;
                for (int j = 0; j < 4; j++)
                {

                    sum += tmpVector[j] * XRotationMatrix[j][i];

                }
                currentVector[i] = sum;
            }

            /***** PROJECTION * TRANSLATION **********/
            //float[] tmpVector = new float[4];
            for (int j = 0; j < 4; j++)
            {
                tmpVector[j] = currentVector[j];
            }
            for (int i = 0; i < 4; i++)
            {
                float sum = 0;
                for (int j = 0; j < 4; j++)
                {

                    sum += tmpVector[j] * matrixT[j][i];

                }
                currentVector[i] = sum;
            }
            /********* TRANSLATION * ROTATION *********/
            /*for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    pointMatrix[i][j] = 0;
                }
            }*/
            //float[] tmpVector = new float[4];
            for (int j = 0; j < 4; j++)
            {
                tmpVector[j] = currentVector[j];
            }
            for (int i = 0; i < 4; i++)
            {

                float sum = 0;
                for (int j = 0; j < 4; j++)
                {

                    sum += tmpVector[j] * projectionMatrix[j][i];

                }
                currentVector[i] = sum;
            }

            for (int i = 0; i < 4; i++)
            {

                mP.coordinates[i] = currentVector[i];

            }
            for (int i = 0; i < 4; i++)
            {
                mP.coordinates[i] /= mP.coordinates[3];
            }
            //  mP.coordinates[0] += 500;
            //  mP.coordinates[1] += 350;
        }

        private void MoveCylinderRight(){
            for (; ; )
            {
                lock (objectLock)
                {
                    InitializePointTable();
                    InitializeYRotationMatrix();
                    InitializeProjectionMatrix();
                    writeableBitmap.Dispatcher.BeginInvoke((Action)(() => { writeableBitmap.Clear(); myImage.Source = writeableBitmap; }));

                    //writeableBitmap = new WriteableBitmap(1000, 700, 300, 300, System.Windows.Media.PixelFormats.Bgra32, null);

                    CylinderDegreeY += degreeStep;
                    ChangeYRotationMatrix(0);
                    Draw();
                }
                Thread.Sleep(30);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    lock (objectLock)
                    {
                        InitializePointTable();
                        InitializeYRotationMatrix();
                        InitializeProjectionMatrix();
                        writeableBitmap.Clear();
                        //writeableBitmap = new WriteableBitmap(1000, 700, 300, 300, System.Windows.Media.PixelFormats.Bgra32, null);
                        myImage.Source = writeableBitmap;
                        YDegrees -= degreeStep;
                        ChangeYRotationMatrix(0);
                        Draw();
                    }
                    break;
                case Key.Right:
                    lock (objectLock)
                    {
                        InitializePointTable();
                        InitializeYRotationMatrix();
                        InitializeProjectionMatrix();
                        writeableBitmap.Clear();
                        // writeableBitmap = new WriteableBitmap(1000, 700, 300, 300, System.Windows.Media.PixelFormats.Bgra32, null);
                        myImage.Source = writeableBitmap;
                        YDegrees += degreeStep;
                        ChangeYRotationMatrix(0);
                        Draw();
                    }
                    break;
                case Key.Up:
                    InitializePointTable();
                    lock (objectLock)
                    {
                        InitializePointTable();
                        // InitializeYRotationMatrix();
                        InitializeXRotationMatrix();
                        InitializeProjectionMatrix();
                        writeableBitmap.Clear();
                        // writeableBitmap = new WriteableBitmap(1000, 700, 300, 300, System.Windows.Media.PixelFormats.Bgra32, null);
                        myImage.Source = writeableBitmap;
                        XDegrees += degreeStep;
                        ChangeYRotationMatrix(0);
                        ChangeXRotationMatrix();
                        Draw();
                    }
                    break;

                case Key.Down:
                    lock (objectLock)
                    {
                        InitializePointTable();
                        //InitializeYRotationMatrix();
                        InitializeXRotationMatrix();
                        InitializeProjectionMatrix();
                        writeableBitmap.Clear();
                        // writeableBitmap = new WriteableBitmap(1000, 700, 300, 300, System.Windows.Media.PixelFormats.Bgra32, null);
                        myImage.Source = writeableBitmap;
                        XDegrees -= degreeStep;
                        ChangeYRotationMatrix(0);
                        ChangeXRotationMatrix();
                        Draw();
                    }
                    break;
                case Key.Add:
                    lock (objectLock)
                    {
                        InitializePointTable();
                        InitializeYRotationMatrix();
                        InitializeProjectionMatrix();
                        writeableBitmap.Clear();
                        //writeableBitmap = new WriteableBitmap(1000, 700, 300, 300, System.Windows.Media.PixelFormats.Bgra32, null);
                        myImage.Source = writeableBitmap;
                        CylinderDegreeY += degreeStep;
                        ChangeYRotationMatrix(0);
                        Draw();
                    }
                    break;    
            }

        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            movementThread.Abort();
            base.OnClosing(e);
        }
        
    }
    public class myPoint
    {
        public float[] coordinates = new float[4];

        public myPoint(float Px, float Py, float Pz, float Pw)
        {
            this.coordinates[0] = Px;
            this.coordinates[1] = Py;
            this.coordinates[2] = Pz;
            this.coordinates[3] = Pw;
        }
    }
}
