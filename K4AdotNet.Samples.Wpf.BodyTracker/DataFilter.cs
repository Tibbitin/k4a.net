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

            var left = float.MaxValue;
            var right = float.MinValue;
            var top = float.MinValue;
            var bottom = float.MaxValue;
            var topYPoint = new Float2(0, top);
            var botYPoint = new Float2(0, bottom);
            var rightXPoint = new Float2(right, 0);
            var leftXPoint = new Float2(left, 0);

            foreach (var jointType in JointTypes.All)
            {
                if (!jointType.IsRoot())
                {
                    var parentJoint = skeleton[jointType.GetParent()];
                    var endJoint = skeleton[jointType];
                    var parentPoint2D = ProjectJointToImage(parentJoint);
                    var endPoint2D = ProjectJointToImage(endJoint);

                    Float2 coord = new Float2();
                    coord.X = (float)endPoint2D.Value.X;
                    coord.Y = (float)endPoint2D.Value.Y;

                    if (botYPoint.Y > coord.Y)
                        botYPoint = coord;
                    if (leftXPoint.X > coord.X)
                        leftXPoint = coord;
                    if (topYPoint.Y < coord.Y)
                        topYPoint = coord;
                    if (rightXPoint.X < coord.X)
                        rightXPoint = coord;
                }
            }

            var height = Math.Abs(topYPoint.Y - botYPoint.Y);
            var width = Math.Abs(leftXPoint.X - rightXPoint.X);
            Console.WriteLine("Height: " + height + ", Width: " + width);
            Console.WriteLine("Top Y Point: (" + topYPoint.X + ", " + topYPoint.Y + ")");
            Console.WriteLine("Bot Y Point: (" + botYPoint.X + ", " + botYPoint.Y + ")");
            Console.WriteLine("Left X Point: (" + leftXPoint.X + ", " + leftXPoint.Y + ")");
            Console.WriteLine("Right X Point: (" + rightXPoint.X + ", " + rightXPoint.Y + ")");
            Console.WriteLine();

            ROI bodyFrameROI = new ROI((int)leftXPoint.X, (int)botYPoint.Y, (int)width, (int)height);
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
            //if (bodyFrame == null || bodyFrame.IsDisposed)
            //    return null;
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
