﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SortThing.Services;
using System;
using System.IO;

namespace SortThing.Tests;

[TestClass]
public class PathTransformerTests
{
    private readonly DateTime _dateTaken = new(2021, 9, 27, 7, 22, 00);
    private readonly string _exampleDestination = @"D:\Photos\Sorted\{year}\{month}\{day}\{camera}\{hour}_{minute} - {filename}.{extension}";
    private readonly string _exampleSource = @"D:\Sync\Camera\WP_20151116_08_38_40_Pro.jpg";
    private readonly string _expectedTransform = @"D:\Photos\Sorted\2021\09\27\Nikon\07_22 - WP_20151116_08_38_40_Pro.jpg";
    private PathTransformer _pathTransformer;

    [TestInitialize]
    public void Init()
    {
        _pathTransformer = new PathTransformer();
    }

    [TestMethod]
    public void TransformPath_GivenEmptyPath_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() => _pathTransformer.TransformPath(null, _exampleDestination, _dateTaken, "Nikon"));
        Assert.ThrowsException<ArgumentNullException>(() => _pathTransformer.TransformPath(_exampleSource, null, _dateTaken, "Nikon"));
        Assert.ThrowsException<ArgumentNullException>(() => _pathTransformer.TransformPath(" ", _exampleDestination, _dateTaken, "Nikon"));
        Assert.ThrowsException<ArgumentNullException>(() => _pathTransformer.TransformPath(_exampleSource, " ", _dateTaken, "Nikon"));
    }

    [TestMethod]
    public void TransformPath_GivenValidPath_Succeeds()
    {
        var result = _pathTransformer.TransformPath(_exampleSource, _exampleDestination, _dateTaken, "Nikon");
        Assert.AreEqual(_expectedTransform, result);

        result = _pathTransformer.TransformPath(_exampleSource, _exampleDestination);
        Assert.AreEqual(@"D:\Photos\Sorted\{year}\{month}\{day}\{camera}\{hour}_{minute} - WP_20151116_08_38_40_Pro.jpg", result);
    }

    [TestMethod]
    public void GetUniqueFilePath_ReturnsUnique()
    {
        var randomString = Guid.NewGuid().ToString();
        var filePath = Path.Combine(Path.GetTempPath(), randomString + ".txt");

        try
        {

            Assert.AreEqual(filePath, _pathTransformer.GetUniqueFilePath(filePath));

            File.Create(filePath).Close();

            var uniquePath = Path.Combine(
                Path.GetTempPath(),
                randomString + "_0.txt");
            Assert.AreEqual(uniquePath, _pathTransformer.GetUniqueFilePath(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}