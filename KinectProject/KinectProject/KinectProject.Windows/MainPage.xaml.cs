using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Kinect;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace KinectProject
{
    /// <summary>
    /// Final Year Project by Aditya Gandhi
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Size of RGB Pixel in Bitmap
        private const int BytesPerPixel = 4;

        private KinectSensor kinectSensor = null;
        private WriteableBitmap bitmap = null;

        // Initialize Infrared Frame
        private InfraredFrameReader infraredFrameReader = null;
        private ushort[] infraredFrameData = null;
        private byte[] infraredPixels = null;

        // START : Infrared - Initialize the variables needed to convert Sensor Data to Image
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;
        private const float InfraredOutputValueMinimum = 0.01f;
        private const float InfraredOutputValueMaximum = 1.0f;
        private const float InfraredSceneValueAverage = 0.08f;
        private const float InfraredSceneStandardDeviations = 3.0f;
        // END : Infrared - Initialize the variables needed to convert Sensor Data to Image

        public MainPage()
        {
            // Get the default Kinect Sensor
            this.kinectSensor = KinectSensor.GetDefault();

            // START : Setup Infrared before activating Kinect Sensor
            
            // Get the infraredFrameDescription from the InfraredFrameSource
            FrameDescription infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;

            // Open the reader for the infrared frames
            this.infraredFrameReader = this.kinectSensor.InfraredFrameSource.OpenReader();

            // Handler for frame arrival
            this.infraredFrameReader.FrameArrived += this.Reader_InfraredFrameArrived;

            // Allocate space for pixels being recieved and converted
            this.infraredFrameData = new ushort[infraredFrameDescription.Width * infraredFrameDescription.Height];
            this.infraredPixels = new byte[infraredFrameDescription.Width * infraredFrameDescription.Height * BytesPerPixel];

            // Create the Bitmap to display
            this.bitmap = new WriteableBitmap(infraredFrameDescription.Width, infraredFrameDescription.Height);

            // END : Setup Infrared before activating Kinect Sensor

            // Start the Kinect Sensor
            this.kinectSensor.Open();
            this.InitializeComponent();
        }

        private void Reader_InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            // FUNCTION : Event handle for recieving frames
            bool infraredFrameProcessed = false;
            
            using (InfraredFrame infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame != null)
                {
                    FrameDescription infraredFrameDescription = infraredFrame.FrameDescription;

                    // Write the new infrared frame data to the display bitmap
                    if (((infraredFrameDescription.Width * infraredFrameDescription.Height) == this.infraredFrameData.Length) && (infraredFrameDescription.Width == this.bitmap.PixelWidth) && (infraredFrameDescription.Height == this.bitmap.PixelHeight))
                    {
                        // Add pixel data to temporary array
                        infraredFrame.CopyFrameDataToArray(this.infraredFrameData);

                        infraredFrameProcessed = true;
                    }
                }
            }  

            // Render Frame
            if (infraredFrameProcessed)
            {
                ConvertInfraredDataToPixels();
                RenderPixelArray(this.infraredPixels);
            }
        }

        private void ConvertInfraredDataToPixels()
        {
            // Convert infrared to RGB
            int colorPixelIndex = 0;
            for (int i = 0; i < this.infraredFrameData.Length; ++i)
            {
                // Divide incoming value by source maximum value
                float intensityRatio = (float)this.infraredFrameData[i] / InfraredSourceValueMaximum;

                // Divide by Average Scene Value multiplied by the Standard Deviations
                intensityRatio /= InfraredSceneValueAverage * InfraredSceneStandardDeviations;

                // Limit values to InfraredOutputValueMaximum
                intensityRatio = Math.Min(InfraredOutputValueMaximum, intensityRatio);

                // Limit values to InfraredOutputValueMinimum
                intensityRatio = Math.Max(InfraredOutputValueMinimum, intensityRatio);

                // Convert normalized value to byte and use result as RGB component
                byte intensity = (byte)(intensityRatio * 255.0f);
                this.infraredPixels[colorPixelIndex++] = intensity; // Blue
                this.infraredPixels[colorPixelIndex++] = intensity; // Green
                this.infraredPixels[colorPixelIndex++] = intensity; // Red
                this.infraredPixels[colorPixelIndex++] = 255; // Alpha
            }
        }

        private void RenderPixelArray (byte[] pixels)
        {
            pixels.CopyTo(this.bitmap.PixelBuffer);
            this.bitmap.Invalidate();
            FrameDisplayImage.Source = this.bitmap;
        }
    }
}
