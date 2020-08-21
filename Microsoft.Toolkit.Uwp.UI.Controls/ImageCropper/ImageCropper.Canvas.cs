// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
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
        private static async Task CropImageWithShapeAsync(WriteableBitmap writeableBitmap, IRandomAccessStream stream, Rect croppedRect, BitmapFileFormat bitmapFileFormat, CropShape cropShape)
        {
            var device = CanvasDevice.GetSharedDevice();
            var clipGeometry = CreateClipGeometry(device, cropShape, new Size(croppedRect.Width, croppedRect.Height));
            if (clipGeometry == null)
            {
                return;
            }

            CanvasBitmap sourceBitmap = null;
            using (var randomAccessStream = new InMemoryRandomAccessStream())
            {
                await CropImageAsync(writeableBitmap, randomAccessStream, croppedRect, bitmapFileFormat);
                sourceBitmap = await CanvasBitmap.LoadAsync(device, randomAccessStream);
            }

            using (var offScreen = new CanvasRenderTarget(device, (float)croppedRect.Width, (float)croppedRect.Height, 96f))
            {
                using (var drawingSession = offScreen.CreateDrawingSession())
                using (var markCommandList = new CanvasCommandList(device))
                {
                    using (var markDrawingSession = markCommandList.CreateDrawingSession())
                    {
                        markDrawingSession.FillGeometry(clipGeometry, Colors.Black);
                    }

                    var alphaMaskEffect = new AlphaMaskEffect
                    {
                        Source = sourceBitmap,
                        AlphaMask = markCommandList
                    };
                    drawingSession.DrawImage(alphaMaskEffect);
                    alphaMaskEffect.Dispose();
                }

                clipGeometry.Dispose();
                sourceBitmap.Dispose();
                var pixelBytes = offScreen.GetPixelBytes();
                var bitmapEncoder = await BitmapEncoder.CreateAsync(GetEncoderId(bitmapFileFormat), stream);
                bitmapEncoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, offScreen.SizeInPixels.Width, offScreen.SizeInPixels.Height, 96.0, 96.0, pixelBytes);
                await bitmapEncoder.FlushAsync();
            }
        }

        private static CanvasGeometry CreateClipGeometry(ICanvasResourceCreator resourceCreator, CropShape cropShape, Size croppedSize)
        {
            switch (cropShape)
            {
                case CropShape.Rectangular:
                    break;
                case CropShape.Circular:
                    var radiusX = croppedSize.Width / 2;
                    var radiusY = croppedSize.Height / 2;
                    var center = new Point(radiusX, radiusY);
                    return CanvasGeometry.CreateEllipse(resourceCreator, center.ToVector2(), (float)radiusX, (float)radiusY);
            }

            return null;
        }
    }
}
