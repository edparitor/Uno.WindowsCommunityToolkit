﻿// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using Microsoft.Toolkit.Win32.UI.Controls.Interop.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Toolkit.Win32.UI.Controls.Test.WebView.Shared
{
    public static class OperatingSystemAssertions
    {
        public static void OSBuildShouldBeAtLeast(this Assert _, int buildNumber) => AssertOSSince(buildNumber);

        public static void OSBuildShouldBeAtLeast(this Assert _, TestConstants.Windows10Builds build) => AssertOSSince((int)build);

        private static void AssertOSSince(int buildNumber)
        {
            var os = NativeMethods.RtlGetVersion();
            if (os.BuildNumber < buildNumber)
            {
                Assert.Inconclusive($"Required build number {buildNumber}. OS is {os.BuildNumber}");
            }
        }
    }
}