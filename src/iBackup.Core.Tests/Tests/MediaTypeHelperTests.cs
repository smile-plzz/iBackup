namespace iBackup.Core.Tests.Tests;

using FluentAssertions;
using iBackup.Core.Models;
using iBackup.Core.Services;
using Xunit;

public class MediaTypeHelperTests
{
    [Theory]
    [InlineData("IMG_0001.JPG", MediaType.Photo)]
    [InlineData("IMG_0002.jpeg", MediaType.Photo)]
    [InlineData("IMG_0003.HEIC", MediaType.Photo)]
    [InlineData("IMG_0004.heif", MediaType.Photo)]
    [InlineData("IMG_0005.PNG", MediaType.Photo)]
    [InlineData("photo.bmp", MediaType.Photo)]
    [InlineData("image.tiff", MediaType.Photo)]
    [InlineData("animation.gif", MediaType.Photo)]
    [InlineData("graphic.webp", MediaType.Photo)]
    public void DetermineMediaType_ShouldReturnPhoto_ForStandardPhotoExtensions(string fileName, MediaType expected)
    {
        // Act
        var result = MediaTypeHelper.DetermineMediaType(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("IMG_0100.MOV", MediaType.Video)]
    [InlineData("video.mp4", MediaType.Video)]
    [InlineData("clip.m4v", MediaType.Video)]
    public void DetermineMediaType_ShouldReturnVideo_ForVideoExtensions(string fileName, MediaType expected)
    {
        // Act
        var result = MediaTypeHelper.DetermineMediaType(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void DetermineMediaType_ShouldReturnRaw_ForDngFiles()
    {
        // Act
        var result = MediaTypeHelper.DetermineMediaType("IMG_9999.DNG");

        // Assert
        result.Should().Be(MediaType.Raw);
    }

    [Theory]
    [InlineData("Screenshot_20261013.png", MediaType.Screenshot)]
    [InlineData("screenshot-app.jpg", MediaType.Screenshot)]
    public void DetermineMediaType_ShouldReturnScreenshot_WhenIdentifiedAsScreenshot(string fileName, MediaType expected)
    {
        // Act
        var result = MediaTypeHelper.DetermineMediaType(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("IMG_0001.JPG", true)]
    [InlineData("IMG_0002.heic", true)]
    [InlineData("movie.mov", true)]
    [InlineData("video.mp4", true)]
    [InlineData("raw.dng", true)]
    [InlineData("notes.txt", false)]
    [InlineData("document.pdf", false)]
    [InlineData("presentation.pptx", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsMediaFile_ShouldValidateCorrectly(string? fileName, bool expected)
    {
        // Act
        var result = MediaTypeHelper.IsMediaFile(fileName!);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1073741824, "1.0 GB")]
    public void FormatFileSize_ShouldFormatBytesAccurately(long bytes, string expected)
    {
        // Act
        var result = MediaTypeHelper.FormatFileSize(bytes);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatDuration_ShouldFormatHumanReadableString()
    {
        // Arrange & Act
        var subSecond = MediaTypeHelper.FormatDuration(TimeSpan.FromMilliseconds(500));
        var seconds = MediaTypeHelper.FormatDuration(TimeSpan.FromSeconds(45));
        var minutes = MediaTypeHelper.FormatDuration(TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(30)));
        var hours = MediaTypeHelper.FormatDuration(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(15)));

        // Assert
        subSecond.Should().Be("< 1s");
        seconds.Should().Be("45s");
        minutes.Should().Be("2m 30s");
        hours.Should().Be("1h 15m");
    }
}
