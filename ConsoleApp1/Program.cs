using Nancy.Hosting.Self;
using System;

namespace ConsoleApp1
{
  class Program
  {
    static void Main(string[] args)
    {
      Uri endpoint = new Uri("http://localhost:1234");
      using (var host = new NancyHost(endpoint, new MyBootstrapper()))
      {
        host.Start();
        Console.WriteLine("Running on http://localhost:1234");
        Console.ReadLine();
        host.Stop();
      }
    }
  }
}
