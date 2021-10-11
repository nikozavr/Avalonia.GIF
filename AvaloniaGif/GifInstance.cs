using AvaloniaGif.Decoding;
using System;
using System.IO;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace AvaloniaGif
{
    public class GifInstance : IDisposable
    { 
        private readonly Image _targerImage;
        private readonly Stream _stream;
        private readonly IterationCount _iterationCount;
        private readonly bool _autoStart;

        private readonly object _bitmapSync = new object();

        private GifDecoder _gifDecoder;
        private GifBackgroundWorker _bgWorker;
        private WriteableBitmap _targetBitmap;
        private bool _hasNewFrame;
        private bool _isDisposed;

        public GifInstance(Image target, Stream stream, IterationCount iterationCount, bool autoStart = true)
        {
            _isDisposed = false;
            _targerImage = target;
            _stream = stream;
            _iterationCount = iterationCount;
            _autoStart = autoStart;
        }

        public void Process()
        {
            GifRepeatBehavior gifRepeatBehavior = new GifRepeatBehavior();
            if (_iterationCount.IsInfinite)
            {
                gifRepeatBehavior.LoopForever = true;
            }
            else
            {
                gifRepeatBehavior.LoopForever = false;
                gifRepeatBehavior.Count = (int)_iterationCount.Value;
            }

            _gifDecoder = new GifDecoder(_stream);
            var pixSize = new PixelSize(_gifDecoder.Header.Dimensions.Width, _gifDecoder.Header.Dimensions.Height);
            _targetBitmap = new WriteableBitmap(pixSize, new Vector(96, 96), PixelFormat.Bgra8888);
            _targerImage.Source = _targetBitmap;
            _bgWorker = new GifBackgroundWorker(_gifDecoder, gifRepeatBehavior);
            _bgWorker.CurrentFrameChanged += FrameChanged;

            Run();
        }

        public PixelSize GifPixelSize { get; private set; }
 
        public WriteableBitmap GetBitmap()
        {
            WriteableBitmap ret = null;
            
            lock (_bitmapSync)
            {
                if (_hasNewFrame)
                {
                    _hasNewFrame = false;
                    ret = _targetBitmap;
                }
            }

            return ret;
        }
        
        private void FrameChanged()
        {
            lock (_bitmapSync)
            {
                if (_isDisposed) return;
                _hasNewFrame = true;
                using (var lockedBitmap = _targetBitmap?.Lock())
                    _gifDecoder?.WriteBackBufToFb(lockedBitmap.Address);
            }
        }

        private void Run()
        {
            if (!_stream.CanSeek)
                throw new ArgumentException("The stream is not seekable");

            AvaloniaLocator.Current.GetService<IRenderTimer>().Tick += RenderTick;
            _bgWorker?.SendCommand(BgWorkerCommand.Play);
        }

        private void RenderTick(TimeSpan time)
        {
            if (_isDisposed | !_hasNewFrame) return;
            lock (_bitmapSync)
            {
                _targerImage?.InvalidateVisual();
                _hasNewFrame = false;
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            AvaloniaLocator.Current.GetService<IRenderTimer>().Tick += RenderTick;
            _bgWorker?.SendCommand(BgWorkerCommand.Dispose);
            _targetBitmap?.Dispose();
        }
    }
}