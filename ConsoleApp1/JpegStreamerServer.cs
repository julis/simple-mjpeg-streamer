using rtaNetworking.Streaming;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
  public abstract class StreamServer : IDisposable
  {
    public abstract void Start(int port);
    public abstract void Stop();
    public abstract bool IsRunning { get; }

    public IEnumerable<Image> ImagesSource { get; set; }

    #region IDisposable Members

    public void Dispose()
    {
      this.Stop();
    }

    #endregion
  }

  public class JpegStreamServer : StreamServer
  {
    private Socket _socketServer;
    private List<Socket> _clients;
    private Thread _thread;

    private CancellationTokenSource _stopListeningTokenSource;

    private int _interval = 50;
    public JpegStreamServer()
    {
      _clients = new List<Socket>();
      _thread = null;
    }
    public override bool IsRunning { get { return (_thread != null && _thread.IsAlive); } }

    public override void Start(int port)
    {
      lock(this)
      {
        _thread = new Thread(new ParameterizedThreadStart(ServerThread));
        _thread.IsBackground = true;
        _thread.Start(port);
      }
    }

    private void ServerThread(object port)
    {
      try
      {
        _socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socketServer.Bind(new IPEndPoint(IPAddress.Any, (int)port));
        _socketServer.Listen(10);
        _stopListeningTokenSource = new CancellationTokenSource();

        System.Diagnostics.Debug.WriteLine(string.Format("Jpeg Stream Server started on port {0}.", port));

        foreach (Socket client in _socketServer.IncommingConnectoins())
          ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), client);
      }
      catch (Exception e)
      {
        if (!_stopListeningTokenSource.IsCancellationRequested)
          Console.WriteLine("{0} : {1}", e.Message, e.StackTrace);
      }
    }

    private void ClientThread(object client)
    {
      if (_stopListeningTokenSource.IsCancellationRequested)
        return;

      Socket socket = (Socket)client;
      lock (_clients)
        _clients.Add(socket);

      try
      {
        using (MjpegWriter writer = new MjpegWriter(new NetworkStream(socket, true)))
        {
          writer.WriteHeader();

          while (!_stopListeningTokenSource.IsCancellationRequested)
          {
            MemoryStream ms = new MemoryStream();

            foreach (var imgStream in ImagesSource.Streams())
            {
              Thread.Sleep(_interval);
              writer.Write(imgStream);
            }
          }
        }
      }
      catch
      {
        // Shit happens...
        // Client probably disconnected.
      }
      finally
      {
        lock (_clients)
          _clients.Remove(socket);
      }

    }

    public override void Stop()
    {
      if (this.IsRunning)
      {
        _stopListeningTokenSource.Cancel();

        try
        {
          _socketServer.Close();
          _thread.Join();
          _thread.Abort();
        }
        catch { }
        finally
        {
          lock (_clients)
          {
            lock (_clients)
            {
              foreach (var s in _clients)
              {
                try
                {
                  s.Close();
                }
                catch { }
              }
              _clients.Clear();
              _thread = null;
            }
          }
        }
      }
    }

   
  }

  static class SocketExtensions
  {
    public static IEnumerable<Socket> IncommingConnectoins(this Socket server)
    {
      while (true)
        yield return server.Accept();
    }
  }

  static class ImageSourceExtensions
  {
    public static IEnumerable<MemoryStream> Streams( this IEnumerable<Image> source)
    {
      MemoryStream ms = new MemoryStream();

      foreach (var img in source)
      {
        ms.SetLength(0);
        img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        yield return ms;
      }

      ms.Close();
      ms = null;

      yield break;
    }
   
  }
}
