// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FileBufferingWriteStreamTests : IDisposable
    {
        private readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "FileBufferingWriteTests", Path.GetRandomFileName());

        public FileBufferingWriteStreamTests()
        {
            Directory.CreateDirectory(TempDirectory);
        }

        [Fact]
        public void Write_BuffersContentToMemory()
        {
            // Arrange
            using var bufferingStream = new FileBufferingWriteStream(tempFileDirectoryAccessor: () => TempDirectory);
            var input = Encoding.UTF8.GetBytes("Hello world");

            // Act
            bufferingStream.Write(input, 0, input.Length);

            // Assert
            // We should have written content to memory
            var pagedByteBuffer = bufferingStream.PagedByteBuffer;
            Assert.Equal(input, ReadBufferedContent(pagedByteBuffer));

            // No files should not have been created.
            Assert.Null(bufferingStream.FileStream);
        }

        [Fact]
        public void Write_BeforeMemoryThresholdIsReached_WritesToMemory()
        {
            // Arrange
            var input = new byte[] { 1, 2, };
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            bufferingStream.Write(input, 0, 2);

            // Assert
            var pageBuffer = bufferingStream.PagedByteBuffer;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.Null(fileStream);

            // No content should be in the memory stream
            Assert.Equal(2, pageBuffer.Length);
            Assert.Equal(input, ReadBufferedContent(pageBuffer));
        }

        [Fact]
        public void Write_BuffersContentToDisk_WhenMemoryThresholdIsReached()
        {
            // Arrange
            var input = new byte[] { 1, 2, 3, };
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);
            bufferingStream.Write(input, 0, 2);

            // Act
            bufferingStream.Write(input, 2, 1);

            // Assert
            var pageBuffer = bufferingStream.PagedByteBuffer;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.NotNull(fileStream);
            var fileBytes = ReadFileContent(fileStream);
            Assert.Equal(input, fileBytes);

            // No content should be in the memory stream
            Assert.Equal(0, pageBuffer.Length);
        }

        [Fact]
        public void Write_BuffersContentToDisk_WhenWriteWillOverflowMemoryThreshold()
        {
            // Arrange
            var input = new byte[] { 1, 2, 3, };
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            bufferingStream.Write(input, 0, input.Length);

            // Assert
            var pageBuffer = bufferingStream.PagedByteBuffer;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.NotNull(fileStream);
            var fileBytes = ReadFileContent(fileStream);
            Assert.Equal(input, fileBytes);

            // No content should be in the memory stream
            Assert.Equal(0, pageBuffer.Length);
        }

        [Fact]
        public void Write_AfterMemoryThresholdIsReached_BuffersToMemory()
        {
            // Arrange
            var input = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 4, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            bufferingStream.Write(input, 0, 5);
            bufferingStream.Write(input, 5, 2);

            // Assert
            var pageBuffer = bufferingStream.PagedByteBuffer;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.NotNull(fileStream);
            var fileBytes = ReadFileContent(fileStream);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, }, fileBytes);

            Assert.Equal(new byte[] { 6, 7 }, ReadBufferedContent(pageBuffer));
        }

        [Fact]
        public async Task WriteAsync_BuffersContentToMemory()
        {
            // Arrange
            using var bufferingStream = new FileBufferingWriteStream(tempFileDirectoryAccessor: () => TempDirectory);
            var input = Encoding.UTF8.GetBytes("Hello world");

            // Act
            await bufferingStream.WriteAsync(input, 0, input.Length);

            // Assert
            // We should have written content to memory
            var pagedByteBuffer = bufferingStream.PagedByteBuffer;
            Assert.Equal(input, ReadBufferedContent(pagedByteBuffer));

            // No files should not have been created.
            Assert.Null(bufferingStream.FileStream);
        }

        [Fact]
        public async Task WriteAsync_BeforeMemoryThresholdIsReached_WritesToMemory()
        {
            // Arrange
            var input = new byte[] { 1, 2, };
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            await bufferingStream.WriteAsync(input, 0, 2);

            // Assert
            var pageBuffer = bufferingStream.PagedByteBuffer;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.Null(fileStream);

            // No content should be in the memory stream
            Assert.Equal(2, pageBuffer.Length);
            Assert.Equal(input, ReadBufferedContent(pageBuffer));
        }

        [Fact]
        public async Task WriteAsync_BuffersContentToDisk_WhenMemoryThresholdIsReached()
        {
            // Arrange
            var input = new byte[] { 1, 2, 3, };
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);
            bufferingStream.Write(input, 0, 2);

            // Act
            await bufferingStream.WriteAsync(input, 2, 1);

            // Assert
            var pageBuffer = bufferingStream.PagedByteBuffer;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.NotNull(fileStream);
            var fileBytes = ReadFileContent(fileStream);
            Assert.Equal(input, fileBytes);

            // No content should be in the memory stream
            Assert.Equal(0, pageBuffer.Length);
        }

        [Fact]
        public async Task WriteAsync_BuffersContentToDisk_WhenWriteWillOverflowMemoryThreshold()
        {
            // Arrange
            var input = new byte[] { 1, 2, 3, };
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            await bufferingStream.WriteAsync(input, 0, input.Length);

            // Assert
            var pageBuffer = bufferingStream.PagedByteBuffer;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.NotNull(fileStream);
            var fileBytes = ReadFileContent(fileStream);
            Assert.Equal(input, fileBytes);

            // No content should be in the memory stream
            Assert.Equal(0, pageBuffer.Length);
        }

        [Fact]
        public async Task WriteAsync_AfterMemoryThresholdIsReached_BuffersToMemory()
        {
            // Arrange
            var input = new byte[] { 1, 2, 3, 4, 5, 6, 7 };
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 4, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            await bufferingStream.WriteAsync(input, 0, 5);
            await bufferingStream.WriteAsync(input, 5, 2);

            // Assert
            var pageBuffer = bufferingStream.PagedByteBuffer;
            var fileStream = bufferingStream.FileStream;

            // File should have been created.
            Assert.NotNull(fileStream);
            var fileBytes = ReadFileContent(fileStream);

            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, }, fileBytes);
            Assert.Equal(new byte[] { 6, 7 }, ReadBufferedContent(pageBuffer));
        }

        [Fact]
        public void Write_Throws_IfSingleWriteExceedsBufferLimit()
        {
            // Arrange
            var input = new byte[20];
            var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            var exception = Assert.Throws<IOException>(() => bufferingStream.Write(input, 0, input.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);

            Assert.True(bufferingStream.Disposed);
        }

        [Fact]
        public void Write_Throws_IfWriteCumulativeWritesExceedsBuffersLimit()
        {
            // Arrange
            var input = new byte[6];
            var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            bufferingStream.Write(input, 0, input.Length);
            var exception = Assert.Throws<IOException>(() => bufferingStream.Write(input, 0, input.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);

            // Verify we return the buffer.
            Assert.True(bufferingStream.Disposed);
        }

        [Fact]
        public void Write_DoesNotThrow_IfBufferLimitIsReached()
        {
            // Arrange
            var input = new byte[5];
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            bufferingStream.Write(input, 0, input.Length);
            bufferingStream.Write(input, 0, input.Length); // Should get to exactly the buffer limit, which is fine

            // If we got here, the test succeeded.
        }

        [Fact]
        public async Task WriteAsync_Throws_IfSingleWriteExceedsBufferLimit()
        {
            // Arrange
            var input = new byte[20];
            var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            var exception = await Assert.ThrowsAsync<IOException>(() => bufferingStream.WriteAsync(input, 0, input.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);

            Assert.True(bufferingStream.Disposed);
        }

        [Fact]
        public async Task WriteAsync_Throws_IfWriteCumulativeWritesExceedsBuffersLimit()
        {
            // Arrange
            var input = new byte[6];
            var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            await bufferingStream.WriteAsync(input, 0, input.Length);
            var exception = await Assert.ThrowsAsync<IOException>(() => bufferingStream.WriteAsync(input, 0, input.Length));
            Assert.Equal("Buffer limit exceeded.", exception.Message);

            // Verify we return the buffer.
            Assert.True(bufferingStream.Disposed);
        }

        [Fact]
        public async Task WriteAsync_DoesNotThrow_IfBufferLimitIsReached()
        {
            // Arrange
            var input = new byte[5];
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 2, bufferLimit: 10, tempFileDirectoryAccessor: () => TempDirectory);

            // Act
            await bufferingStream.WriteAsync(input, 0, input.Length);
            await bufferingStream.WriteAsync(input, 0, input.Length); // Should get to exactly the buffer limit, which is fine

            // If we got here, the test succeeded.
        }

        [Fact]
        public void CopyTo_CopiesContentFromMemoryStream()
        {
            // Arrange
            var input = new byte[] { 1, 2, 3, 4, 5 };
            using var bufferingStream = new FileBufferingWriteStream(tempFileDirectoryAccessor: () => TempDirectory);
            bufferingStream.Write(input, 0, input.Length);
            var memoryStream = new MemoryStream();

            // Act
            bufferingStream.CopyTo(memoryStream);

            // Assert
            Assert.Equal(input, memoryStream.ToArray());
        }

        [Fact]
        public void CopyTo_WithContentInDisk_CopiesContentFromMemoryStream()
        {
            // Arrange
            var input = Enumerable.Repeat((byte)0xca, 30).ToArray();
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 21, tempFileDirectoryAccessor: () => TempDirectory);
            bufferingStream.Write(input, 0, input.Length);
            var memoryStream = new MemoryStream();

            // Act
            bufferingStream.CopyTo(memoryStream);

            // Assert
            Assert.Equal(input, memoryStream.ToArray());
        }

        [Fact]
        public void CopyTo_InvokedMultipleTimes_Works()
        {
            // Arrange
            var input = Enumerable.Repeat((byte)0xca, 30).ToArray();
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 21, tempFileDirectoryAccessor: () => TempDirectory);
            bufferingStream.Write(input, 0, input.Length);
            var memoryStream1 = new MemoryStream();
            var memoryStream2 = new MemoryStream();

            // Act
            bufferingStream.CopyTo(memoryStream1);
            bufferingStream.CopyTo(memoryStream2);

            // Assert
            Assert.Equal(input, memoryStream1.ToArray());
            Assert.Equal(input, memoryStream2.ToArray());
        }

        [Fact]
        public async Task CopyToAsync_CopiesContentFromMemoryStream()
        {
            // Arrange
            var input = new byte[] { 1, 2, 3, 4, 5 };
            using var bufferingStream = new FileBufferingWriteStream(tempFileDirectoryAccessor: () => TempDirectory);
            bufferingStream.Write(input, 0, input.Length);
            var memoryStream = new MemoryStream();

            // Act
            await bufferingStream.CopyToAsync(memoryStream);

            // Assert
            Assert.Equal(input, memoryStream.ToArray());
        }

        [Fact]
        public async Task CopyToAsync_WithContentInDisk_CopiesContentFromMemoryStream()
        {
            // Arrange
            var input = Enumerable.Repeat((byte)0xca, 30).ToArray();
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 21, tempFileDirectoryAccessor: () => TempDirectory);
            bufferingStream.Write(input, 0, input.Length);
            var memoryStream = new MemoryStream();

            // Act
            await bufferingStream.CopyToAsync(memoryStream);

            // Assert
            Assert.Equal(input, memoryStream.ToArray());
        }

        [Fact]
        public async Task CopyToAsync_InvokedMultipleTimes_Works()
        {
            // Arrange
            var input = Enumerable.Repeat((byte)0xca, 30).ToArray();
            using var bufferingStream = new FileBufferingWriteStream(memoryThreshold: 21, tempFileDirectoryAccessor: () => TempDirectory);
            bufferingStream.Write(input, 0, input.Length);
            var memoryStream1 = new MemoryStream();
            var memoryStream2 = new MemoryStream();

            // Act
            await bufferingStream.CopyToAsync(memoryStream1);
            await bufferingStream.CopyToAsync(memoryStream2);

            // Assert
            Assert.Equal(input, memoryStream1.ToArray());
            Assert.Equal(input, memoryStream2.ToArray());
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(TempDirectory, recursive: true);
            }
            catch
            {
            }
        }

        private static byte[] ReadFileContent(FileStream fileStream)
        {
            fileStream.Position = 0;
            using var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }

        private static byte[] ReadBufferedContent(PagedByteBuffer buffer)
        {
            using var memoryStream = new MemoryStream();
            buffer.CopyTo(memoryStream, clearBuffers: false);

            return memoryStream.ToArray();
        }
    }
}
