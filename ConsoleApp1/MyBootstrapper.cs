using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.TinyIoc;
using rtaNetworking.Streaming;

namespace ConsoleApp1
{
  public class MyBootstrapper : DefaultNancyBootstrapper
  {
    ImageStreamingServer _liveImageServer;
    StreamServer _streamServer;
    protected override void ConfigureApplicationContainer(TinyIoCContainer container)
    {
      base.ConfigureApplicationContainer(container);
      List <String> images = new List<String>()
      {
        "D:\\pictures\\1.png",
        "D:\\pictures\\2.png",
        "D:\\pictures\\3.png",
        "D:\\pictures\\4.png",
        "D:\\pictures\\5.png",
        "D:\\pictures\\6.png",

      };
      _liveImageServer = new ImageStreamingServer(images);
      _streamServer = new JpegStreamServer();

      container.Register<ImageStreamingServer>(_liveImageServer);
      container.Register<StreamServer>(_streamServer);

    }
  }
}
