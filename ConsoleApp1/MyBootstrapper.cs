using Nancy;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;

namespace ConsoleApp1
{
  public class MyBootstrapper : DefaultNancyBootstrapper
  {
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
      _streamServer = new JpegStreamServer();

      container.Register<StreamServer>(_streamServer);

    }
  }
}
