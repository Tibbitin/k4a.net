using K4AdotNet.BodyTracking;
using System;
using System.IO;

namespace K4AdotNet.Samples.Wpf.BodyTracker
{
    class DataFilter
    {
        public struct ROI
        {
            public int x;
            public int y;
            public int w;
            public int h;

            public ROI(int x, int y, int w, int h)
            {
                this.x = x;
                this.y = y;
                this.w = w;
                this.h = h;
            }
        }

        public DataFilter(Func<Joint, Float2?> jointToImageProjector)
        {
            this.jointToImageProjector = jointToImageProjector;
        }

        /// <summary>
        /// Image with visualized skeletons (<c>Astra.BodyFrame</c>). You can use this property in WPF controls/windows.
        /// </summary>


        public ROI roi(Skeleton skeleton, K4AdotNet.Sensor.Image image)
        {

            //var left = float.MaxValue;
            //var right = float.MinValue;
            //var top = float.MinValue;
            //var bottom = float.MaxValue;
            var topYPoint = new Float2(0, float.MaxValue);
            var botYPoint = new Float2(0, float.MinValue);
            var rightXPoint = new Float2(float.MinValue, 0);
            var leftXPoint = new Float2(float.MaxValue, 0);

            var leftEar = new Float2(0, 0);
            var rightEar = new Float2(0, 0);

            foreach (var jointType in JointTypes.All)
            {
                Float2 coord = new Float2();
                if (!jointType.IsRoot())
                {
                    var parentJoint = skeleton[jointType.GetParent()];
                    var endJoint = skeleton[jointType];
                    var parentPoint2D = ProjectJointToImage(parentJoint);
                    var endPoint2D = ProjectJointToImage(endJoint);

                    
                    coord.X = (float)endPoint2D.Value.X;
                    coord.Y = (float)endPoint2D.Value.Y;
                    
                    if (botYPoint.Y < coord.Y)
                        botYPoint = coord;
                    if (leftXPoint.X > coord.X)
                        leftXPoint = coord;
                    if (topYPoint.Y > coord.Y)
                        topYPoint = coord;
                    if (rightXPoint.X < coord.X)
                        rightXPoint = coord;
                }

                if (jointType == JointType.EarLeft)
                    leftEar = coord;
                if (jointType == JointType.EarRight)
                    rightEar = coord;
            }

            Console.WriteLine("leftEar: " + leftEar.X + ", " + leftEar.Y);
            Console.WriteLine("rightEar: " + rightEar.X + ", " + rightEar.Y);

            //var distance = Math.Abs(rightEar.X - leftEar.X) + Math.Abs(rightEar.Y - leftEar.Y);
            var distance = Math.Sqrt(Math.Pow(rightEar.X - leftEar.X, 2) + Math.Pow(rightEar.Y - leftEar.Y, 2));
            var midpointX = Math.Abs(rightEar.X + leftEar.X) / 2;
            var midpointY = Math.Abs(rightEar.Y + leftEar.Y) / 2;
            var center = new Float2((float)midpointX, midpointY);
            var pad = 0.5 * Math.Sqrt(3) * distance;

            Console.WriteLine("Distance: " + distance);
            Console.WriteLine("Center: " + center);
            Console.WriteLine("Pad: " + pad);

            /*
            var pot1 = new Float2(center.X, (float)(center.Y + pad));
            var pot2 = new Float2(center.X, (float)(center.Y - pad));
            var pot3 = new Float2((float)(center.X + pad), center.Y);
            var pot4 = new Float2((float)(center.X - pad), center.Y);
            */

            var pot1x = center.X + pad < 0 ? 0 : center.X + pad;
            var pot1y = center.Y + pad < 0 ? 0 : center.Y + pad;

            var pot2x = center.X - pad < 0 ? 0 : center.X - pad;
            var pot2y = center.Y - pad < 0 ? 0 : center.Y - pad;


            // Console.WriteLine("Height: " + height + ", Width: " + width);
            Console.WriteLine("Top Y Point: (" + topYPoint.X + ", " + topYPoint.Y + ")");
            Console.WriteLine("Bot Y Point: (" + botYPoint.X + ", " + botYPoint.Y + ")");
            Console.WriteLine("Left X Point: (" + leftXPoint.X + ", " + leftXPoint.Y + ")");
            Console.WriteLine("Right X Point: (" + rightXPoint.X + ", " + rightXPoint.Y + ")");

            leftXPoint.X = Math.Min(leftXPoint.X, (float)pot2x);
            rightXPoint.X = Math.Max(rightXPoint.X, (float)pot1x);
            topYPoint.Y = Math.Min(topYPoint.Y, (float)pot2y);
            botYPoint.Y = Math.Max(botYPoint.Y, (float)pot1y);

            var height = Math.Abs(topYPoint.Y - botYPoint.Y);
            var width = Math.Abs(leftXPoint.X - rightXPoint.X);
            Console.WriteLine("Height: " + height + ", Width: " + width);
            Console.WriteLine("Top Y Point: (" + topYPoint.X + ", " + topYPoint.Y + ")");
            Console.WriteLine("Bot Y Point: (" + botYPoint.X + ", " + botYPoint.Y + ")");
            Console.WriteLine("Left X Point: (" + leftXPoint.X + ", " + leftXPoint.Y + ")");
            Console.WriteLine("Right X Point: (" + rightXPoint.X + ", " + rightXPoint.Y + ")");
            Console.WriteLine();

            ROI bodyFrameROI = new ROI((int)leftXPoint.X, (int)topYPoint.Y, (int)width, (int)height);
            return bodyFrameROI;
        }

        int depthFileIndex = 0;
        public void WriteFilteredDataToDiskRaw(byte[] data)
        {
            string filePathNew = "C:\\Users\\moshi\\Downloads\\test_data\\test" + depthFileIndex + ".bytes";
            depthFileIndex++;
            File.WriteAllBytes(filePathNew, data);
        }

        public void WriteFilteredDataToDiskJPG(byte[] data)
        {
            string filePathNew = "C:\\Users\\moshi\\Downloads\\test_data\\test" + depthFileIndex + ".jpg";
            depthFileIndex++;
            MemoryStream stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            using (System.Drawing.Image image = System.Drawing.Image.FromStream(stream))
            {
                image.Save(filePathNew, System.Drawing.Imaging.ImageFormat.Jpeg);  // Or Png
            }
        }


        private System.Windows.Point? ProjectJointToImage(Joint joint)
        {
            var res = jointToImageProjector(joint);
            if (!res.HasValue)
                return null;
            return new System.Windows.Point(res.Value.X, res.Value.Y);
        }

        public ROI[] GetROI(K4AdotNet.Sensor.Image image, BodyFrame bodyFrame)
        {
            // Is compatible?
            if (bodyFrame == null || bodyFrame.IsDisposed)
                return null;
            ROI[] rois = new ROI[bodyFrame.BodyCount];
            // 1st step: get information about bodies
            lock (skeletonsSync)
            {
                var bodyCount = bodyFrame.BodyCount;
                if (skeletons.Length != bodyCount)
                    skeletons = new Skeleton[bodyCount];
                for (var i = 0; i < bodyCount; i++)
                {
                    bodyFrame.GetBodySkeleton(i, out skeletons[i]);
                   rois[i] = roi(skeletons[i], image);
                }
            }
            return rois;
        }

        private Skeleton[] skeletons = new Skeleton[0];
        private readonly object skeletonsSync = new object();
        private readonly Func<Joint, Float2?> jointToImageProjector;
    }
}
