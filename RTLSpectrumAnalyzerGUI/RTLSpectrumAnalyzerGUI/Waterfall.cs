﻿
/*
* Author: Clint Mclean
*
* RTL SDR SpectrumAnalyzer turns a RTL2832 based DVB dongle into a spectrum analyzer
* 
* This spectrum analyzer, though, is specifically designed for detecting reradiated
* signals from humans. These are frequencies that are transmitted and could have intentional
* and unintentional biological effects.
* 
* The signals generate electrical currents in humans, and could have biological effects
* because of our electrochemical systems that use biologically generated electrical currents.
* 
* These radio/microwaves are reradiated, the electrical currents that are produced generate
* another reradiated electromagnetic wave. So they are detectable.
* 
* This rtl sdr spectrum analyzer then is designed for automatically detecting these reradiated signals.
* 
* Uses RTLSDRDevice.DLL for doing the frequency scans
* which makes use of the librtlsdr library: https://github.com/steve-m/librtlsdr
* and based on that library's included rtl_power.c code to get frequency strength readings
* 
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using System.Windows.Forms;


namespace RTLSpectrumAnalyzerGUI
{
    public enum WaterFallMode { Off, Strength, Difference }
    public enum WaterFallRangeMode { Fixed, Auto }

    public class Waterfall
    {
        PictureBox source = null;

        IntPtr Iptr = IntPtr.Zero;
        BitmapData bitmapData = null;
        static public List<Color> colors;

        WaterFallMode mode = WaterFallMode.Strength;

        WaterFallRangeMode rangeMode = WaterFallRangeMode.Auto;

        double minWaterFall = -50, maxWaterFall = -10;

        double nearStrengthDeltaRange = 1;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private bool bitsLocked = false;


        public Waterfall(PictureBox source)
        {
            this.source = source;

            int r = 255, g = 0, b = 0;

            int stage = 0;

            colors = new List<Color>();

            while (stage != 4)
            {
                colors.Add(Color.FromArgb(r, g, b));

                switch (stage)
                {
                    case (0):
                        g++;

                        if (g == 255)
                            stage++;
                        break;


                    case (1):
                        r--;

                        if (r == 0)
                            stage++;
                        break;


                    case (2):
                        b++;

                        if (b == 255)
                            stage++;
                        break;

                    case (3):
                        g--;

                        if (g == 0)
                            stage++;
                        break;
                }

            }
        }

        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                if (source.Width > 0 && source.Height > 0)
                {
                    bitsLocked = true;
                    // Get width and height of bitmap
                    Width = source.Width;
                    Height = source.Height;

                    // get total locked pixels count
                    int PixelCount = Width * Height;

                    // Create rectangle to lock
                    Rectangle rect = new Rectangle(0, 0, Width, Height);

                    Bitmap bitmap = (Bitmap)(source.Image);

                    // get source bitmap pixel format size
                    Depth = System.Drawing.Bitmap.GetPixelFormatSize(bitmap.PixelFormat);

                    // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                    if (Depth != 8 && Depth != 24 && Depth != 32)
                    {
                        throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                    }

                    // Lock bitmap and return bitmap data
                    bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

                    // create byte array to copy pixel values
                    int step = Depth / 8;
                    Pixels = new byte[PixelCount * step];
                    Iptr = bitmapData.Scan0;

                    // Copy data from pointer to array
                    Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
                }
            }
            catch (Exception ex)
            {                
                throw ex;
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            if (bitsLocked)
            {
                try
                {
                    // Copy data from byte array to pointer
                    Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);

                    Bitmap bitmap = (Bitmap)(source.Image);
                    // Unlock bitmap data
                    bitmap.UnlockBits(bitmapData);

                    bitsLocked = false;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            Color clr = Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) // For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = Color.FromArgb(r, g, b);
            }
            if (Depth == 8)
            // For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = Color.FromArgb(c, c, c);
            }
            return clr;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, Color color)
        {
            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (Depth == 32) // For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) // For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            if (Depth == 8)
            // For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color.B;
            }
        }

        public void DrawLine(float[] array, long lowerIndex, long upperIndex)
        {
            double waterFallRange = maxWaterFall - minWaterFall;

            if (array.Length > 0 && waterFallRange > 0)
            {
                long graphBinCount = upperIndex - lowerIndex;

                long lowerResGraphBinCount;

                if (graphBinCount > Form1.MAXIMUM_GRAPH_BIN_COUNT)
                    lowerResGraphBinCount = Form1.MAXIMUM_GRAPH_BIN_COUNT;
                else
                    lowerResGraphBinCount = graphBinCount;

                double index = lowerIndex;

                LockBits();

                double inc = (double)graphBinCount / Width;

                double minStrength, maxStrength;
                double normalizedColorRange;                

                int count;
                int y = 0;
                {
                    for (int x = 0; x < Width; x++)
                    {
                        minStrength = 99999999;
                        count = 0;
                        for (int j = (int)index; j < (int)(index + inc); j++)
                        {
                            if (array[j] < minStrength)
                                minStrength = array[j];

                            count++;
                        }

                        if (count == 0)
                            minStrength = array[(long)index];

                        maxStrength = -99999999;
                        count = 0;
                        for (int j = (int)index; j < (int)(index + inc); j++)
                        {
                            if (array[j] > maxStrength)
                                maxStrength = array[j];

                            count++;
                        }

                        if (count == 0)
                            maxStrength = array[(long)index];
                        
                        try
                        {
                            normalizedColorRange = (maxStrength - minWaterFall) / waterFallRange;

                            if (normalizedColorRange > 1)
                                normalizedColorRange = 1;

                            if (normalizedColorRange < 0)
                                normalizedColorRange = 0;

                            SetPixel(x, y, colors[(int)((1 - normalizedColorRange) * (colors.Count - 1))]);

                        }
                        catch (Exception ex)
                        {

                        }


                        index += inc;
                    }
                }

                UnlockBits();
            }
        }

        public void DrawDeltaLine(float[] array1, float[] array2, long lowerIndex, long upperIndex)
        {
            if (nearStrengthDeltaRange > 0)
            {
                long graphBinCount = upperIndex - lowerIndex;

                long lowerResGraphBinCount;

                if (graphBinCount > Form1.MAXIMUM_GRAPH_BIN_COUNT)
                    lowerResGraphBinCount = Form1.MAXIMUM_GRAPH_BIN_COUNT;
                else
                    lowerResGraphBinCount = graphBinCount;

                double index = lowerIndex;

                LockBits();

                double inc = (double)graphBinCount / Width;

                double maxDelta, delta;
                double normalizedColorRange;                

                int count;
                int y = 0;
                {
                    for (int x = 0; x < Width; x++)
                    {
                        maxDelta = -99999999;
                        count = 0;
                        for (int j = (int)index; j < (int)(index + inc); j++)
                        {
                            delta = CalculateStrengthDifference(array1, array2, j);

                            if (delta > maxDelta)
                                maxDelta = delta;

                            count++;
                        }

                        if (count == 0)
                            maxDelta = CalculateStrengthDifference(array1, array2, (long)index);

                        normalizedColorRange = maxDelta / nearStrengthDeltaRange;

                        if (normalizedColorRange > 1)
                            normalizedColorRange = 1;

                        if (normalizedColorRange < 0)
                            normalizedColorRange = 0;

                        SetPixel(x, y, colors[(int)((1 - normalizedColorRange) * (colors.Count - 1))]);

                        index += inc;
                    }
                }
                
                UnlockBits();
            }
        }

        private void ScrollPictureBox()
        {
            // Create the new bitmap and associated graphics object
            Graphics g = Graphics.FromImage(source.Image);

            Rectangle srcRect = new Rectangle(0, 0, source.Image.Width, source.Image.Height);
            Rectangle dstRect = new Rectangle(0, 1, source.Image.Width, source.Image.Height);

            // Draw the specified section of the source bitmap to the new one
            g.DrawImage(source.Image, 0, 1, source.Image.Width, source.Image.Height);

            // Clean up-
            g.Dispose();
        }      

        static public double GetSurroundNoiseFloorStrength(float[] array, long frequencyIndex, long width)
        {
            if (frequencyIndex < width / 2)
            {
                width = width / 2 + (width / 2 - frequencyIndex);

                frequencyIndex = width / 2;
            }

            if (frequencyIndex + width / 2 >= array.Length)
            {
                width = ((array.Length - frequencyIndex) * 2);

                width = width / 2 + (width / 2 - (array.Length - frequencyIndex));

                frequencyIndex = array.Length - width / 2;
            }

            double totalStrength = 0;

            for (long i = frequencyIndex - width / 2; i < frequencyIndex + width / 2; i++)
            {
                totalStrength += array[i];
            }

            return totalStrength /= width;
        }


        static public double CalculateRatio(float[] array1, float[] array2, long frequencyIndex)
        {
            if (array1[frequencyIndex] == 0)
                return 0;

            double ratio = (array2[frequencyIndex] / array1[frequencyIndex]);

            return ratio;
        }

        static public double CalculateStrengthDifference(float[] array1, float[] array2, long frequencyIndex)
        {
            /*////if (array1[frequencyIndex] == 0)
                return 0;

            double dif = (array2[frequencyIndex] - array1[frequencyIndex]);
            */

            /*////if (dif < 0)
                return dif;
                */


            /*////if (frequencyIndex == 0 || frequencyIndex == array1.Length - 1)
                return 0;
                */

            /*////if (array1[frequencyIndex - 1] >= array1[frequencyIndex] || array1[frequencyIndex + 1] >= array1[frequencyIndex])
                return 0;*/

            /*////double ratio1 = array1[frequencyIndex] / array1[frequencyIndex - 1];

            double ratio2 = array1[frequencyIndex] / array1[frequencyIndex + 1];
            */

            /*if (ratio1 > 0.9 && ratio1 < 1.1 && ratio2 > 0.9 && ratio2 < 1.1)
                return 0;*/


            /*double array1NoiseFloor = GetSurroundNoiseFloorStrength(array1, frequencyIndex, 20);

            double array2NoiseFloor = GetSurroundNoiseFloorStrength(array2, frequencyIndex, 20);

            double strength1 = array1[frequencyIndex] - array1NoiseFloor;

            double strength2 = array2[frequencyIndex] - array2NoiseFloor;

            if (strength2 < 0)
                return strength2;

            dif = strength2 - strength1;
            */

            /*double noiseFloorDif = array2NoiseFloor - array1NoiseFloor;

            dif -= noiseFloorDif;
            */

            ////double ratio = dif / strength2;

            ////double ratio = dif / array1[frequencyIndex];


            double array2NearestPeakStrength = SignalDataUtilities.GetNearestPeakStrength(array2, frequencyIndex);
            double array1NearestPeakStrength = SignalDataUtilities.GetNearestPeakStrength(array1, frequencyIndex);

            ////double ratio = array2NearestPeakStrength / array1NearestPeakStrength * 100;

            if (array1NearestPeakStrength > 0)
            {
                double ratio = array2[frequencyIndex] / array1NearestPeakStrength * 100;

                ////double ratio = array2[frequencyIndex] / array1[frequencyIndex] * 100;

                ////return ratio * Math.Abs(strength2) + ratio * (Math.Abs(strength2) /100);

                ////////return Math.Abs(dif) + ratio * Form1.series2Max * 0.01;
                return ratio;
            }
            else
                return 0;

        }

        static public double CalculateStrengthDifference2(float[] array1, float[] array2, long frequencyIndex)
        {
            if (array1[frequencyIndex] == 0)
                return 0;

            double dif = (array2[frequencyIndex] - array1[frequencyIndex]);

            return dif;
        }


        public void CalculateRanges(float[] array1, float[] array2)
        {
            if (rangeMode == WaterFallRangeMode.Auto)
            {
                if (mode == WaterFallMode.Strength)
                {
                    minWaterFall = 9999999999;
                    maxWaterFall = -9999999999;

                    for (int i = 0; i < array1.Length; i++)
                    {
                        if (!Double.IsNaN(array1[i]))
                        {
                            if (array1[i] < minWaterFall)
                                minWaterFall = array1[i];

                            if (array1[i] > maxWaterFall)
                                maxWaterFall = array1[i];
                        }
                    }
                }
                else
                    if (mode == WaterFallMode.Difference)
                {
                    if (array1.Length == array2.Length && array1.Length > 0)
                    {
                        nearStrengthDeltaRange = -99999999;
                        double delta;

                        for (int i = 0; i < array1.Length; i++)
                        {
                            if (!Double.IsNaN(array1[i]) && !Double.IsNaN(array2[i]))
                            {
                                delta = CalculateStrengthDifference(array1, array2, i);

                                if (delta > nearStrengthDeltaRange)
                                    nearStrengthDeltaRange = delta;
                            }
                        }
                    }
                }
            }
        }

        public void RefreshWaterfall(float[] array1, float[] array2, long lowerIndex, long upperIndex)
        {
            if (mode == WaterFallMode.Strength)
            {
                ScrollPictureBox();
                DrawLine(array1, lowerIndex, upperIndex);

                source.Refresh();
            }
            else if (mode == WaterFallMode.Difference)
            {
                if (array1.Length == array2.Length && array1.Length > 0)
                {
                    ScrollPictureBox();
                    DrawDeltaLine(array1, array2, lowerIndex, upperIndex);

                    source.Refresh();
                }
            }
        }

        public void SetMode(WaterFallMode mode)
        {
            this.mode = mode;
        }

        public WaterFallMode GetMode()
        {
            return mode;
        }

        public void SetStrengthRange(double min, double max)
        {
            minWaterFall = min;
            maxWaterFall = max;
        }

        public void SetStrengthMinimum(double min)
        {
            minWaterFall = min;
        }

        public void SetStrengthMaximum(double max)
        {
            maxWaterFall = max;
        }

        public double GetStrengthMinimum()
        {
            return minWaterFall;
        }

        public double GetStrengthMaximum()
        {
            return maxWaterFall;
        }

        public void SetRangeMode(WaterFallRangeMode mode)
        {
            rangeMode = mode;
        }

        public WaterFallRangeMode GetRangeMode()
        {
            return rangeMode;
        }

        public double GetNearStrengthDeltaRange()
        {
            return nearStrengthDeltaRange;
        }

        public void SetNearStrengthDeltaRange(double deltaRange)
        {
            nearStrengthDeltaRange = deltaRange;
        }
    }
}
