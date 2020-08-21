// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using SkiaSharp;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace Microsoft.Toolkit.Uwp.UI.Controls
{
    /// <summary>
    /// The <see cref="ImageCropper"/> control allows user to crop image freely.
    /// </summary>
    public partial class ImageCropper
    {
        /// <summary>
        /// Load an image from a file.
        /// </summary>
        /// <param name="imageFileName">The image file name.</param>
        public void LoadImageFromFile(string imageFileName)
        {
            using (var fileStream = File.OpenRead(imageFileName))
            {
                System.Diagnostics.Debug.WriteLine($"fileStream {fileStream.Length}");
                using (var skiaBitmap = SKBitmap.Decode(fileStream))
                {
                    System.Diagnostics.Debug.WriteLine($"skiaBitmap {skiaBitmap != null}");
                    using (var skiaImage = SKImage.FromPixels(skiaBitmap.PeekPixels()))
                    {
                        System.Diagnostics.Debug.WriteLine($"skiaImage {skiaImage != null}");
                        var info = new SKImageInfo(skiaImage.Width, skiaImage.Height);
                        using (var bitmap = new WriteableBitmap(info.Width, info.Height))
                        using (var tempImage = SKImage.Create(info))
                        {
                            System.Diagnostics.Debug.WriteLine($"tempImage {tempImage != null}");
                            using (var pixmap = tempImage.PeekPixels())
                            {
                                System.Diagnostics.Debug.WriteLine($"pixmap {pixmap != null}");
                                using (var data = SKData.Create(pixmap.GetPixels(), info.BytesSize))
                                {
                                    System.Diagnostics.Debug.WriteLine($"data {data != null}");
                                    if (skiaImage.ReadPixels(pixmap, 0, 0))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"ReadPixels true");
                                        using (var stream = bitmap.PixelBuffer.AsStream())
                                        {
                                            data.SaveTo(stream);
                                        }

                                        System.Diagnostics.Debug.WriteLine($"Saved data to stream");
                                        Source = bitmap;
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"ReadPixels false");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static async Task CropImageWithShapeAsync(WriteableBitmap writeableBitmap, IRandomAccessStream stream, Rect croppedRect, BitmapFileFormat bitmapFileFormat, CropShape cropShape)
        {
            var clipPath = CreateClipGeometry(cropShape, new Size(croppedRect.Width, croppedRect.Height));
            if (clipPath == null)
            {
                return;
            }

            SKBitmap sourceBitmap;
            using (var randomAccessStream = new InMemoryRandomAccessStream())
            {
                await CropImageAsync(writeableBitmap, randomAccessStream, croppedRect, bitmapFileFormat);
                sourceBitmap = SKBitmap.Decode(randomAccessStream.AsStream());
            }

            var info = new SKImageInfo(Convert.ToInt32(croppedRect.Width), Convert.ToInt32(croppedRect.Height));
            using (var surface = SKSurface.Create(info))
            {
                SKCanvas canvas = surface.Canvas;
                canvas.Clear();

                // Set transform to center and enlarge clip path to window height
                SKRect bounds;
                clipPath.GetTightBounds(out bounds);

                canvas.Translate(info.Width / 2, info.Height / 2);
                canvas.Scale(0.98f * info.Height / bounds.Height);
                canvas.Translate(-bounds.MidX, -bounds.MidY);

                // Set the clip path
                canvas.ClipPath(clipPath);

                // Reset transforms
                canvas.ResetMatrix();

                // Display image to fill height of window but maintain aspect ratio
                canvas.DrawBitmap(
                    sourceBitmap,
                    new SKRect((info.Width - info.Height) / 2, 0, (info.Width + info.Height) / 2, info.Height));

                clipPath.Dispose();
                sourceBitmap.Dispose();
                surface.Snapshot().EncodedData.SaveTo(stream.AsStream());
            }
        }

        private static SKPath CreateClipGeometry(CropShape cropShape, Size croppedSize)
        {
            switch (cropShape)
            {
                case CropShape.Rectangular:
                    break;
                case CropShape.Circular:
                    var radiusX = Convert.ToInt32(croppedSize.Width / 2);
                    var radiusY = Convert.ToInt32(croppedSize.Height / 2);
                    return SKPath.ParseSvgPathData($"M0,{radiusY}a{radiusX},{radiusY} 0 1,0 {radiusX * 2},0a{radiusX},{radiusY} 0 1,0 -{radiusX * 2},0");
            }

            return null;
        }
    }
}
