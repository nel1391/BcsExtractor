using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BcsExtractor
{
    // This is basically ProxyStream taken from GARBro, all credit to its creator morkt
    public class ProxyStream : Stream
    {
        Stream m_stream;
        bool m_should_dispose;

        public ProxyStream(Stream input, bool leave_open = false)
        {
            m_stream = input;
            m_should_dispose = !leave_open;
        }

        public Stream BaseStream { get { return m_stream; } }

        public override bool CanRead { get { return m_stream.CanRead; } }
        public override bool CanSeek { get { return m_stream.CanSeek; } }
        public override bool CanWrite { get { return m_stream.CanWrite; } }
        public override long Length { get { return m_stream.Length; } }
        public override long Position
        {
            get { return m_stream.Position; }
            set { m_stream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_stream.Read(buffer, offset, count);
        }

        public override void Flush()
        {
            m_stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_stream.Seek(offset, origin);
        }

        public override void SetLength(long length)
        {
            m_stream.SetLength(length);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_stream.Write(buffer, offset, count);
        }

        bool _proxy_disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (!_proxy_disposed)
            {
                if (m_should_dispose && disposing)
                    m_stream.Dispose();
                _proxy_disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
