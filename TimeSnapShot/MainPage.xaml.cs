using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using SDKTemplate;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.WindowsAzure.Storage;
using QueueStorage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TimeSnapShot
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly PhotoDataViewModel _photoData;

        private MediaCapture _mediaCapture;

        private List<MediaFrameReader> _sourceReaders = new List<MediaFrameReader>();
        private IReadOnlyDictionary<MediaFrameSourceKind, FrameRenderer> _frameRenderers;

        private int _groupSelectionIndex;
        private readonly SimpleLogger _logger;

        public MainPage()
        {
            this.InitializeComponent();
            _photoData = new PhotoDataViewModel();

            _photoData.PhotoDataCompleted += s => { ReloadHistory(); };
            PhotoDataPane.DataContext = _photoData;

            _logger = new SimpleLogger(PhotoMeta);

            // This sample reads three kinds of frames: Color, Depth, and Infrared.
            _frameRenderers = new Dictionary<MediaFrameSourceKind, FrameRenderer>()
            {
                { MediaFrameSourceKind.Color, new FrameRenderer(colorPreviewImage) },
                
            };
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadHistory();
        }

        private async void Play_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _photoData.StartNewPhotoData();
            await PickNextMediaSourceAsync();
        }

        private async void Analyze_Tapped(object sender, TappedRoutedEventArgs e)
        {
            byte[] img = _frameRenderers[MediaFrameSourceKind.Color].CurrentBitmap;

            var metadata = await CpuVision.MakeAnalysisRequest(img);
            var base64Array = System.Text.UTF8Encoding.UTF8.GetBytes(metadata);
            var base64Metadata = System.Convert.ToBase64String(base64Array);

           // var smetadata = string.Format("\"{0}\"", metadata);
            var data = _photoData.StorePhotoData(base64Metadata, img);
          
            var jsonData = JsonConvert.SerializeObject(data);

           var result = await PostToAzureFunction(jsonData);

            PhotoMeta.Text = "";
            _logger.Log(string.Format("{0}-{1}", result, metadata));

        }

        private async void Stop_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _photoData.StopPhotoData();
            PhotoMeta.Text = "";
            await CleanupMediaCaptureAsync();
        }

        private void ReloadHistory()
        {
            PhotoDataList.ItemsSource = PhotoDataService.GetRecentPhotoDatas(5);
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            PhotoDataService.ClearHistory();
            ReloadHistory();
        }



        #region "Camera Rendering"


          protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await PickNextMediaSourceAsync();
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            await CleanupMediaCaptureAsync();
        }

        private async void NextButton_Click(object eventSender, RoutedEventArgs eventArgs)
        {
            await PickNextMediaSourceAsync();
        }

        /// <summary>
        /// Disables the "Next Group" button while we switch to the next camera
        /// source and start reading frames.
        /// </summary>
        private async Task PickNextMediaSourceAsync()
        {
            try
            {
                //this.NextButton.IsEnabled = false;
                await PickNextMediaSourceWorkerAsync();
            }
            finally
            {
               // this.NextButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Switches to the next camera source and starts reading frames.
        /// </summary>
        private async Task PickNextMediaSourceWorkerAsync()
        {
            await CleanupMediaCaptureAsync();

            var allGroups = await MediaFrameSourceGroup.FindAllAsync();

            if (allGroups.Count == 0)
            {
                _logger.Log("No source groups found.");
                return;
            }

            // Pick next group in the array after each time the Next button is clicked.
            _groupSelectionIndex = (_groupSelectionIndex + 1) % allGroups.Count;
            var selectedGroup = allGroups[1];

           // _logger.Log($"Found {allGroups.Count} groups and selecting index [{_groupSelectionIndex}]: {selectedGroup.DisplayName}");

            try
            {
                // Initialize MediaCapture with selected group.
                // This can raise an exception if the source no longer exists,
                // or if the source could not be initialized.
                await InitializeMediaCaptureAsync(selectedGroup);
            }
            catch (Exception exception)
            {
                _logger.Log($"MediaCapture initialization error: {exception.Message}");
                await CleanupMediaCaptureAsync();
                return;
            }

            // Set up frame readers, register event handlers and start streaming.
            var startedKinds = new HashSet<MediaFrameSourceKind>();
            foreach (MediaFrameSource source in _mediaCapture.FrameSources.Values)
            {
                MediaFrameSourceKind kind = source.Info.SourceKind;

                // Ignore this source if we already have a source of this kind.
                if (startedKinds.Contains(kind))
                {
                    continue;
                }

                // Look for a format which the FrameRenderer can render.
                string requestedSubtype = null;
                foreach (MediaFrameFormat format in source.SupportedFormats)
                {
                    requestedSubtype = FrameRenderer.GetSubtypeForFrameReader(kind, format);
                    if (requestedSubtype != null)
                    {
                        // Tell the source to use the format we can render.
                        await source.SetFormatAsync(format);
                        break;
                    }
                }
                if (requestedSubtype == null)
                {
                    // No acceptable format was found. Ignore this source.
                    continue;
                }

                MediaFrameReader frameReader = await _mediaCapture.CreateFrameReaderAsync(source, requestedSubtype);

                frameReader.FrameArrived += FrameReader_FrameArrived;
                _sourceReaders.Add(frameReader);

                MediaFrameReaderStartStatus status = await frameReader.StartAsync();
                if (status == MediaFrameReaderStartStatus.Success)
                {
                  //  _logger.Log($"Started {kind} reader.");
                    startedKinds.Add(kind);
                }
                else
                {
                    _logger.Log($"Unable to start {kind} reader. Error: {status}");
                }
            }

            if (startedKinds.Count == 0)
            {
                _logger.Log($"No eligible sources in {selectedGroup.DisplayName}.");
            }
        }

        /// <summary>
        /// Initializes the MediaCapture object with the given source group.
        /// </summary>
        /// <param name="sourceGroup">SourceGroup with which to initialize.</param>
        private async Task InitializeMediaCaptureAsync(MediaFrameSourceGroup sourceGroup)
        {
            if (_mediaCapture != null)
            {
                return;
            }

            // Initialize mediacapture with the source group.
            _mediaCapture = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings
            {
                SourceGroup = sourceGroup,

                // This media capture can share streaming with other apps.
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,

                // Only stream video and don't initialize audio capture devices.
                StreamingCaptureMode = StreamingCaptureMode.Video,

                // Set to CPU to ensure frames always contain CPU SoftwareBitmap images
                // instead of preferring GPU D3DSurface images.
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            await _mediaCapture.InitializeAsync(settings);
            //_logger.Log("MediaCapture is successfully initialized in shared mode.");
        }

        /// <summary>
        /// Unregisters FrameArrived event handlers, stops and disposes frame readers
        /// and disposes the MediaCapture object.
        /// </summary>
        private async Task CleanupMediaCaptureAsync()
        {
            if (_mediaCapture != null)
            {
                using (var mediaCapture = _mediaCapture)
                {
                    _mediaCapture = null;

                    foreach (var reader in _sourceReaders)
                    {
                        if (reader != null)
                        {
                            reader.FrameArrived -= FrameReader_FrameArrived;
                            await reader.StopAsync();
                            reader.Dispose();
                        }
                    }
                    _sourceReaders.Clear();
                }
            }
        }

        /// <summary>
        /// Handles a frame arrived event and renders the frame to the screen.
        /// </summary>
        private void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            // TryAcquireLatestFrame will return the latest frame that has not yet been acquired.
            // This can return null if there is no such frame, or if the reader is not in the
            // "Started" state. The latter can occur if a FrameArrived event was in flight
            // when the reader was stopped.
            using (var frame = sender.TryAcquireLatestFrame())
            {
                if (frame != null)
                {
                    var renderer = _frameRenderers[frame.SourceKind];
                    renderer.ProcessFrame(frame);
                }
            }
        }
        #endregion

        #region "StorageQueue"

        private async void UpdateQueue(string data)
        {
            var q = await CreateQueueAsync("abccortanademo");

            await BasicSendMessageToQueueAsync(q, data);
        }

        private async Task<CloudQueue> CreateQueueAsync(string queueName)
        {
            // Retrieve storage account information from connection string.
            CloudStorageAccount storageAccount = Common.CreateStorageAccountFromConnectionString(string.Empty);

            // Create a queue client for interacting with the queue service
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            System.Diagnostics.Trace.WriteLine("Getting the queue for the demo");

            CloudQueue queue = queueClient.GetQueueReference(queueName); // "abccortanademo"); // queueName);
            try
            {
                await queue.CreateIfNotExistsAsync();
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine("If you are running with the default configuration please make sure you have started the storage emulator.  ess the Windows key and type Azure Storage to select and run it from the list of applications - then restart the sample.");
                throw;
            }

            return queue;
        }

        /// <summary>
        /// Demonstrate basic queue operations such as adding a message to a queue, peeking at the front of the queue and dequeing a message.
        /// </summary>
        /// <param name="queue">The sample queue</param>
        private async Task BasicSendMessageToQueueAsync(CloudQueue queue, string data)
        {
            // Insert a message into the queue using the AddMessage method. 
            System.Diagnostics.Trace.WriteLine("Insert a single message into a queue");
            await queue.AddMessageAsync(new CloudQueueMessage(data));
                        
        }

        #endregion

        #region "Going through Azure Function"

        private async Task<string> PostToAzureFunction(string data)
        {
            var url = "https://abccortanademo.azurewebsites.net/api/StorePhotoData?code=Zen4nR1G53wsAmBDni0LAc6qqLHwtmIxjojATMJUs7JD8irOZbKmsA==";
            
            var http = new System.Net.Http.HttpClient();

            var stringContent = new StringContent(data);
            var result = await http.PostAsync(url, stringContent);

            return await result.Content.ReadAsStringAsync();
        }


        #endregion


    }
}
