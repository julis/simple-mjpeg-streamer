﻿using Nancy;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;

namespace ConsoleApp1
{
  public class HelloModule : NancyModule
  {
    private StreamServer _streamServer;
    public HelloModule(StreamServer streamServer)
    {
      _streamServer = streamServer;

      Get["/"] = parameters => "Hello World";
      Get["/live-image/start"] = StartLiveImage;
      Get["/live-image/stop"] = StopLiveImage;
      Get["/live-simulation"] = SimulateLiveImage;
    }

    private dynamic SimulateLiveImage(dynamic arg)
    {
      LiveImageSource.SimulateCameraImaging();


      return HttpStatusCode.OK;
    }

    

    private dynamic StopLiveImage(dynamic arg)
    {
      _streamServer.Stop();
      return HttpStatusCode.OK;
    }

    private dynamic StartLiveImage(dynamic arg)
    {
      _streamServer.ImagesSource = LiveImageSource.SingleJpegStreams();
      _streamServer.Start(8081);
      return HttpStatusCode.OK;
    }
  }

  static class LiveImageSource
  {
    private static ReaderWriterLock _readWriteLock = new ReaderWriterLock();

    public static void SimulateCameraImaging()
    {
      Thread t = new Thread(SimulateCameraResult);
      t.Start();
    }
    private static void SimulateCameraResult()
    {
      List<String> images = new List<String>()
        {
          "D:\\pictures\\1.png",
          "D:\\pictures\\2.png",
          "D:\\pictures\\3.png",
          "D:\\pictures\\4.png",
          "D:\\pictures\\5.png",
          "D:\\pictures\\6.png",

        };
      while (true)
      {
        foreach (var image in images)
        {
          Thread.Sleep(100);
          _readWriteLock.AcquireWriterLock(Timeout.Infinite);
          try
          {
            File.Copy(image, "D:\\pictures\\result.png", true);
          }
          finally
          {
            _readWriteLock.ReleaseWriterLock();
          }
          
        }
      }
    }

    public static IEnumerable<Image> SingleJpegStreams()
    {
      string cameraResult = "D:\\pictures\\result.png";
      while (true)
      {
        Thread.Sleep(100);
        _readWriteLock.AcquireReaderLock(Timeout.Infinite);
        try
        {
          byte[] data = File.ReadAllBytes(cameraResult);
          MemoryStream ms = new MemoryStream(data);
          Image img = new Bitmap(ms);
          yield return img;
        }
        finally
        {
          _readWriteLock.ReleaseReaderLock();
        }
      }
    }
  }
}
