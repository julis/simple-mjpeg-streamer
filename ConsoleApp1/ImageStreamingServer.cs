using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

// -------------------------------------------------
// Developed By : Ragheed Al-Tayeb
// e-Mail       : ragheedemail@gmail.com
// Date         : April 2012
// -------------------------------------------------

namespace rtaNetworking.Streaming
{

    /// <summary>
    /// Provides a streaming server that can be used to stream any images source
    /// to any client.
    /// </summary>
    public class ImageStreamingServer:IDisposable
    {

        private List<Socket> _Clients;
    private Socket _socketServer;
        private Thread _Thread;
    private CancellationTokenSource _stopListeningTokenSource;

        public ImageStreamingServer(IEnumerable<String> imagesSource)
        {
      

      _Clients = new List<Socket>();
            _Thread = null;

            this.ImagesSource = imagesSource;
            this.Interval = 2000;

        }


        /// <summary>
        /// Gets or sets the source of images that will be streamed to the 
        /// any connected client.
        /// </summary>
        public IEnumerable<String> ImagesSource { get; set; }

        /// <summary>
        /// Gets or sets the interval in milliseconds (or the delay time) between 
        /// the each image and the other of the stream (the default is . 
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// Gets a collection of client sockets.
        /// </summary>
        public IEnumerable<Socket> Clients { get { return _Clients; } }

        /// <summary>
        /// Returns the status of the server. True means the server is currently 
        /// running and ready to serve any client requests.
        /// </summary>
        public bool IsRunning { get { return (_Thread != null && _Thread.IsAlive); } }

        /// <summary>
        /// Starts the server to accepts any new connections on the specified port.
        /// </summary>
        /// <param name="port"></param>
        public void Start(int port)
        {

            lock (this)
            {
                _Thread = new Thread(new ParameterizedThreadStart(ServerThread));
                _Thread.IsBackground = true;
                _Thread.Start(port);
            }

        }

        /// <summary>
        /// Starts the server to accepts any new connections on the default port (8080).
        /// </summary>
        public void Start()
        {
            this.Start(8080);
        }


        public void Stop()
        {
      if (this.IsRunning)
            {
        _stopListeningTokenSource.Cancel();

                try
                {
          lock (_Clients)
          {

            foreach (var s in _Clients)
            {
              try
              {
                s.Close();
              }
              catch { }
            }
            _Clients.Clear();

          }

          _socketServer.Close();

          _Thread.Join();
                    _Thread.Abort();
                }
        catch(Exception e)
        {
          Console.WriteLine(e.StackTrace);
        }
                finally
                {
          


          _Thread = null;
                }
            }
        }

        /// <summary>
        /// This the main thread of the server that serves all the new 
        /// connections from clients.
        /// </summary>
        /// <param name="state"></param>
        private void ServerThread(object state)
        {
            try
            {
        _socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socketServer.Bind(new IPEndPoint(IPAddress.Any,(int)state));
                _socketServer.Listen(10);
        _stopListeningTokenSource = new CancellationTokenSource();

                System.Diagnostics.Debug.WriteLine(string.Format("Server started on port {0}.", state));
                
                foreach (Socket client in _socketServer.IncommingConnectoins())
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), client);

      }
      catch(Exception e)
      {
        if (!_stopListeningTokenSource.IsCancellationRequested)
          Console.WriteLine("{0} : {1}", e.Message, e.StackTrace);
      }

    }

        /// <summary>
        /// Each client connection will be served by this thread.
        /// </summary>
        /// <param name="client"></param>
        private void ClientThread(object client)
        {
      if (_stopListeningTokenSource.IsCancellationRequested)
        return;
            Socket socket = (Socket)client;

            System.Diagnostics.Debug.WriteLine(string.Format("New client from {0}",socket.RemoteEndPoint.ToString()));

            lock (_Clients)
                _Clients.Add(socket);

            try
            {
                using (MjpegWriter wr = new MjpegWriter(new NetworkStream(socket, true)))
                {

                    // Writes the response header to the client.
                    wr.WriteHeader();

                    // Streams the images from the source to the client.
                    foreach (var imgStream in Screen.Streams(this.ImagesSource))
                    {
                        if (this.Interval > 0)
                            Thread.Sleep(this.Interval);

                        wr.Write(imgStream);
                    }

                }
            }
            catch { }
            finally
            {
                lock (_Clients)
                    _Clients.Remove(socket);
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            this.Stop();
        }

        #endregion
    }

    static class SocketExtensions
    {

        public static IEnumerable<Socket> IncommingConnectoins(this Socket server)
        {
      while(true)
          yield return server.Accept();    
        }

    }


    static class Screen
    {
    
    internal static IEnumerable<MemoryStream> Streams(this IEnumerable<String> imagesFilePath)
    {
      MemoryStream ms = new MemoryStream();

      foreach (var imagePath in imagesFilePath)
      {
        ms.SetLength(0);
        byte[] file = File.ReadAllBytes(imagePath);
        ms.Write(file, 0, file.Length);
        
        yield return ms;
      }

      ms.Close();
      ms = null;

      yield break;
    }
      
    }
}
