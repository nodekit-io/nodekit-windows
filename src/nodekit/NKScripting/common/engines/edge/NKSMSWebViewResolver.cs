#if WINDOWS_UWP
/*
* nodekit.io
*
* Copyright (c) 2016 OffGrid Networks. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web;

namespace io.nodekit.NKScripting.Engines.MSWebView
{
    public sealed class NKSMSWebViewResolver : IUriToStreamResolver
    {
        public IAsyncOperation<IInputStream> UriToStreamAsync(System.Uri uri)
        {
            string path = uri.AbsolutePath;

            if (path.StartsWith("/ajax", StringComparison.OrdinalIgnoreCase))
            {
                return GetObject(new
                {
                    a = 1,
                    b = "b"
                }).AsAsyncOperation();
            }

            string host = uri.Host;
            int delimiter = host.LastIndexOf('_');
            string encodedContentId = host.Substring(delimiter + 1);
            IBuffer buffer = CryptographicBuffer.DecodeFromHexString(encodedContentId);

            string contentIdentifier = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, buffer);
            string relativePath = uri.PathAndQuery;

            // For this sample, we will return a stream for a file under the local app data
            // folder, under the subfolder named after the contentIdentifier and having the
            // given relativePath.  Real apps can have more complex behavior, such as handling
            // contentIdentifiers in a custom manner (not necessarily as paths), and generating
            // arbitrary streams that are not read directly from a file.
            System.Uri appDataUri = new Uri("ms-appx:///app" + relativePath);

            return GetFileStreamFromApplicationUriAsync(appDataUri).AsAsyncOperation();
        }

        private async Task<IInputStream> GetFileStreamFromApplicationUriAsync(System.Uri uri)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
            return stream;
        }

        private async Task<IInputStream> GetObject<T>(T item)
        {
            IRandomAccessStream inMemoryStream = new InMemoryRandomAccessStream();
            var stream = inMemoryStream.AsStream();
            inMemoryStream.Seek(0);
            var writer = new StreamWriter(stream);
            _jsonSerializer.Serialize(writer, item);
            writer.Flush();
            stream.Seek(0L, SeekOrigin.Begin);
            return await Task.FromResult<IInputStream>(new InputStreamWithContentType(inMemoryStream, "application/javascript"));
        }

    }
}
#endif