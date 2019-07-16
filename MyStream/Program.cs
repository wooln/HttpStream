using System;
using System.Net.Http;
using R = RestSharp;
using IO = System.IO;
using RestSharp.Extensions;
using System.Net;

namespace MyStream
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var stream = GetStreamV2();

            Console.WriteLine(stream.Length);

            var bytes = stream.ReadAsBytes();
            Console.WriteLine(bytes.Length);
            IO.File.WriteAllBytes("_pi_1.jpg", bytes);

            stream.Seek(0, IO.SeekOrigin.Begin);
            bytes = stream.ReadAsBytes();
            Console.WriteLine(bytes.Length);
            IO.File.WriteAllBytes("_pi_2.jpg", bytes);

            stream.Seek(0, IO.SeekOrigin.Begin);
            bytes = stream.ReadAsBytes();
            Console.WriteLine(bytes.Length);
            IO.File.WriteAllBytes("_pi_3.jpg", bytes);

            Console.WriteLine("over");

        }
        
        static string baseUrl = "https://www.sinooceangroup.com/Content/Images/zh-cn/";
        static string resourcePath = "homeBanner3.jpg";

        static IO.Stream GetStreamV2()
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(baseUrl + resourcePath);

            MyHttpStream result = new MyHttpStream(webRequest, true);

            return result;
        }
    }


}



namespace MyStream
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;

    public class MyHttpStream : Stream
    {
        private HttpWebRequest _request;
        Stream _responseStream;
        private long _length;
        private long _position;
        private long _totalBytesRead;
        private int _totalReads;
        private bool _canSeek = false;
        private MemoryStream _memoryStream = null;

        public MyHttpStream(HttpWebRequest request, bool canSeek = false)
        {
            this._canSeek = canSeek;
            this._request = request;
            var response = (HttpWebResponse)request.GetResponse();
            this._length = response.ContentLength;
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"http状态码为{response.StatusCode}");
            }
            this._responseStream = response.GetResponseStream();
        }

        public long TotalBytesRead { get { return _totalBytesRead; } }
        public long TotalReads { get { return _totalReads; } }
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return this._canSeek; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return _length; } }

        public override bool CanTimeout
        {
            get
            {
                return base.CanTimeout;
            }
        }


        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (value < 0) throw new ArgumentException();
                _position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!this._canSeek)
            {
                throw new NotImplementedException(nameof(Seek));
            }
            else
            {

                if (this._memoryStream == null)
                {
                    this._responseStream.Close();

                    //重新下载一遍全部到内存
                    
                    var response = this.CloneRequest(this._request).GetResponse();
                    var allContent = response.GetResponseStream().ReadAsBytes();
                    this._memoryStream = new MemoryStream(allContent);
                }

                this._memoryStream.Position = this._position;
                long newPosition = this._memoryStream.Seek(offset, origin);
                this._position = newPosition;
                return newPosition;
            }

        }

        private HttpWebRequest CloneRequest(HttpWebRequest request)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(request.RequestUri);
            return webRequest;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Stream sourceSteam = this._memoryStream ?? this._responseStream;


            var readCount = sourceSteam.Read(buffer, offset, count);

            _totalBytesRead += readCount;
            _totalReads++;
            Position += readCount;

            //不可Seek的非内存流，读到最后就自动关闭吧。
            if (readCount == 0 && this._memoryStream == null)
            {
                _responseStream.Close();
            }

            return readCount;
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            base.Close();
            _responseStream.Close();
        }

    }
}



