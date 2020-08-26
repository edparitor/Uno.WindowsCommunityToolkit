// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.Toolkit.Uwp.SampleApp.SamplePages
{
    /// <summary>
    /// A page that shows how to use the ImageCropper control.
    /// </summary>
    public sealed partial class ImageCropperPage : Page, IXamlRenderListener
    {
        private ImageCropper _imageCropper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCropperPage"/> class.
        /// </summary>
        public ImageCropperPage()
        {
            this.InitializeComponent();
            Load();
        }

        public async void OnXamlRendered(FrameworkElement control)
        {
            _imageCropper = control.FindChildByName("ImageCropper") as ImageCropper;
            if (_imageCropper != null)
            {
#if HAS_UNO
#if __ANDROID__
                using (var drawable = Uno.Helpers.DrawableHelper.FromUri(new Uri("ms-appx:///Assets/Photos/Owl.jpg")))
                {
                    using (var bitmap = ((Android.Graphics.Drawables.BitmapDrawable)drawable).Bitmap)
                    {
                        var memoryStream = new MemoryStream();
                        await bitmap.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Jpeg, 100, memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        _imageCropper.LoadImageFromStream(memoryStream);
                    }
                }
#else
                var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync("Assets/Photos/Owl.jpg");
                _imageCropper.LoadImageFromStream(File.OpenRead(file.Path));
#endif
#else
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Photos/Owl.jpg"));
                await _imageCropper.LoadImageFromFile(file);
#endif
            }
        }

        private void Load()
        {
            SampleController.Current.RegisterNewCommand("Pick Image", async (sender, args) =>
            {
                await PickImage();
            });
            SampleController.Current.RegisterNewCommand("Save", async (sender, args) =>
            {
                await SaveCroppedImage();
            });
            SampleController.Current.RegisterNewCommand("Reset", (sender, args) =>
            {
                _imageCropper?.Reset();
            });
            var itemsSource = new List<AspectRatioConfig>
            {
                new AspectRatioConfig
                {
                    Name = "Custom",
                    AspectRatio = null
                },
                new AspectRatioConfig
                {
                    Name = "Square",
                    AspectRatio = 1
                },
                new AspectRatioConfig
                {
                    Name = "Landscape(16:9)",
                    AspectRatio = 16d / 9d
                },
                new AspectRatioConfig
                {
                    Name = "Portrait(9:16)",
                    AspectRatio = 9d / 16d
                },
                new AspectRatioConfig
                {
                    Name = "4:3",
                    AspectRatio = 4d / 3d
                },
                new AspectRatioConfig
                {
                    Name = "3:2",
                    AspectRatio = 3d / 2d
                }
            };
            AspectRatioComboBox.ItemsSource = itemsSource;
            AspectRatioComboBox.DisplayMemberPath = "Name";
            AspectRatioComboBox.SelectedValuePath = "AspectRatio";
            AspectRatioComboBox.SelectedIndex = 0;
            AspectRatioComboBox.SelectionChanged += this.AspectRatioComboBox_SelectionChanged;
        }

        private void AspectRatioComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var aspectRatio = AspectRatioComboBox.SelectedValue as double?;
            if (_imageCropper != null)
            {
                _imageCropper.AspectRatio = aspectRatio;
            }
        }

        private async Task PickImage()
        {
            var filePicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                FileTypeFilter =
                {
                    ".png", ".jpg", ".jpeg"
                }
            };
            var file = await filePicker.PickSingleFileAsync();
            if (file != null && _imageCropper != null)
            {
                await _imageCropper.LoadImageFromFile(file);
            }
        }

        private async Task SaveCroppedImage()
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                SuggestedFileName = "Cropped_Image",
                FileTypeChoices =
                {
                    { "PNG Picture", new List<string> { ".png" } },
                    { "JPEG Picture", new List<string> { ".jpg" } }
                }
            };
            var imageFile = await savePicker.PickSaveFileAsync();
            if (imageFile != null)
            {
                BitmapFileFormat bitmapFileFormat;
                switch (imageFile.FileType.ToLower())
                {
                    case ".png":
                        bitmapFileFormat = BitmapFileFormat.Png;
                        break;
                    case ".jpg":
                        bitmapFileFormat = BitmapFileFormat.Jpeg;
                        break;
                    default:
                        bitmapFileFormat = BitmapFileFormat.Png;
                        break;
                }

                using (var fileStream = await imageFile.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.None))
                {
                    await _imageCropper.SaveAsync(fileStream, bitmapFileFormat);
                }
            }
        }
    }
}
