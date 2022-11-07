using System;
using System.IO;
using System.Runtime.InteropServices;
using K4AdotNet.BodyTracking;
using K4AdotNet.Sensor;
using static K4AdotNet.Samples.Wpf.BodyTracker.DataFilter;

namespace K4AdotNet.Samples.Wpf.BodyTracker
{
    class depth
    {
        
        public depth()
        {

        }
        public byte[] Blackout(Image img, ROI[] roi)
        {

            byte[] byteArray = new byte[(img.WidthPixels * img.HeightPixels) * sizeof(short)];
            Marshal.Copy(img.Buffer, byteArray, 0, byteArray.Length);

            int lineStride = 640 * 2;
            int pixelStride = 2;
            for (int j = 0; j < 288 * 2; j++)
            {
                for (int i = 0; i < 320 * 2; i++)
                {
                    if (checkInRoi(i, j, roi) == 0)
                    {
                        byteArray[j * lineStride + i * pixelStride] = 0;
                        byteArray[j * lineStride + i * pixelStride + 1] = 0;
                    }
                }
            }

            //640 x 576
            Console.WriteLine($"ROI elements:\nx: {roi[0].x}\ny: {roi[0].y}");
            return byteArray;
        }
        public int checkInRoi(int p_x, int p_y, ROI[] roi)
        {
            int x = roi[0].x;
            int y = roi[0].y;
            int w = roi[0].w;
            int h = roi[0].h;

            if (p_x >= x && p_x <= (x + w))
            {
                if (p_y >= y && p_y <= (y + h))
                {
                    return 1;
                }
            }
            return 0;

        }
    }
}
