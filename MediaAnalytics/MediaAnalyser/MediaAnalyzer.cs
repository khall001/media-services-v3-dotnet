using System.Text;
using Analyser_Context;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;


namespace MediaAnalyzer
{
    public class MediaAnalyzer
    {
        private MediaAnalyzerInput AnalyzerInput;
        public const string OutputFolder = @"MediaAnalyzerOutput";
        private Preset MediaAnalyzerPerset; 
        private MediaAnalyzerConfig Config;
        private IAzureMediaServicesClient AzureMediaServicesClient;
        private Job AnlyzerJob;
        private MediaAnalyzerAsset AnalyzerAsset;

        public MediaAnalyzer(MediaAnalyzerConfig config)
        {
            Config = config;
            
        }

        public async Task VideoAnalyzer(MediaAnalyzerInput mediaAnalyzerInput, VideoAnalyzerPreset? preset = null)
        {
            if (preset != null)
            {
                MediaAnalyzerPerset = preset;
            }
            else
            {
                // Create an VideoAnalyzer preset with audio insights and Basic audio mode.
                MediaAnalyzerPerset = new VideoAnalyzerPreset(
                    audioLanguage: AnalyzerInput.LanguageCode,
                    //
                    // There are two modes available, Basic and Standard
                    // Basic : This mode performs speech-to-text transcription and generation of a VTT subtitle/caption file. 
                    //         The output of this mode includes an Insights JSON file including only the keywords, transcription,and timing information. 
                    //         Automatic language detection and speaker diarization are not included in this mode.
                    // Standard : Performs all operations included in the Basic mode, additionally performing language detection and speaker diarization.
                    //
                    mode: AudioAnalysisMode.Standard,
                    insightsToExtract: InsightsType.AllInsights);
            }
            await StartAnalyszer(mediaAnalyzerInput);
        }
        public async Task AudioAnalyzer(MediaAnalyzerInput mediaAnalyserInput, AudioAnalyzerPreset? preset = null)
        {
            if (preset != null)
            {
                MediaAnalyzerPerset = preset;
            }
            else
            {
                // Create an AudioAnalyzer preset with audio insights and Basic audio mode.
                MediaAnalyzerPerset = new AudioAnalyzerPreset(
                    audioLanguage: AnalyzerInput.LanguageCode,
                    //
                    // There are two modes available, Basic and Standard
                    // Basic : This mode performs speech-to-text transcription and generation of a VTT subtitle/caption file. 
                    //         The output of this mode includes an Insights JSON file including only the keywords, transcription,and timing information. 
                    //         Automatic language detection and speaker diarization are not included in this mode.
                    // Standard : Performs all operations included in the Basic mode, additionally performing language detection and speaker diarization.
                    //
                    mode: AudioAnalysisMode.Standard
                );
            }
            await StartAnalyszer(mediaAnalyserInput);
        }
        public async Task FaceRedactor(MediaAnalyzerInput mediaAnalyzerInput, FaceDetectorPreset? preset = null)
        {
            if (preset != null)
            {
                MediaAnalyzerPerset = preset;
            }
            else
            {
                // Create a Face Detector preset and enable redaction/blurring of the faces.
                MediaAnalyzerPerset = new FaceDetectorPreset(
                    resolution: AnalysisResolution.StandardDefinition,
                    mode: FaceRedactorMode.Analyze, // Use the Combined mode here. This is the single pass mode where detection and blurring happens as one pass - if you want to analyze and get JSON results first before blur, use Analyze mode, followed by Redact mode. 
                    blurType: BlurType.Med // Sets the amount of blur. For debugging purposes you can set this to Box to just see the outlines of the faces.
                );
            }
            await StartAnalyszer(mediaAnalyzerInput);
        }
        private async Task StartAnalyszer (MediaAnalyzerInput mediaAnalyzerInput)
        {
            AnalyzerInput = mediaAnalyzerInput;

            AzureMediaServicesClient = Config.StartConfig();
            try
            {
                await RunAsync(Config, AzureMediaServicesClient, mediaAnalyzerInput);
            }
            catch (Exception exception)
            {
                Debug.Assert(true, $"{exception.Message}");

                if (exception.GetBaseException() is ErrorResponseException apiException)
                {
                    Debug.Assert(true,
                        $"ERROR: API call failed with error code '{apiException.Body.Error.Code}' and message '{apiException.Body.Error.Message}'.");
                }
            }
        }

        /// <summary>
        /// Run the sample async.
        /// </summary>
        /// <param name="config">The param is of type ConfigWrapper. This class reads values from local configuration file.</param>
        /// <returns></returns>
        private async Task RunAsync(MediaAnalyzerConfig config, IAzureMediaServicesClient client, MediaAnalyzerInput input)
        {
            // Set the polling interval for long running operations to 2 seconds.
            // The default value is 30 seconds for the .NET client SDK
            client.LongRunningOperationRetryTimeout = 2;

            // Creating a unique suffix so that we don't have name collisions if you run the sample
            // multiple times without cleaning up.
            string uniqueness = Guid.NewGuid().ToString("N");
            string jobName = $"job-{AnalyzerAsset.Uniqueness}";
            string outputAssetName = $"output-{AnalyzerAsset.Uniqueness}";
            string inputAssetName = $"input-{AnalyzerAsset.Uniqueness}";
            string transformName = "My_MediaAnalyzer";
            AnalyzerAsset = new MediaAnalyzerAsset()
            {
                Uniqueness = uniqueness,
                JobName = jobName,
                OutputAssetName = outputAssetName,
                InputAssetName = inputAssetName,
                TransformName = transformName
            };
            

            // Create a new input Asset and upload the specified local media file into it.
            if (!AnalyzerInput.IsHttpJob)
            {
                await CreateInputAssetAsync(client, 
                    config.ResourceGroup,
                    config.AccountName,
                    AnalyzerAsset.InputAssetName, 
                    !input.IsByteArray? input.InputFileUrl : input.ByteArrayName,
                    input.IsByteArray,
                    input.ByteArrayData);
            }

            // Output from the encoding Job must be written to an Asset, so let's create one
            Asset outputAsset = await CreateOutputAssetAsync(client, config.ResourceGroup, config.AccountName, outputAssetName);

            Preset preset = MediaAnalyzerPerset;
            

            // Ensure that you have the desired encoding Transform. This is really a one time setup operation.
            // Once it is created, we won't delete it.
            Transform mediaAnalyzerTransform = await GetOrCreateTransformAsync(client, config.ResourceGroup, config.AccountName, transformName, preset);

            // Use a preset override to change the language or mode on the Job level.
            // Above we created a Transform with a preset that was set to a specific audio language and mode. 
            // If we want to change that language or mode before submitting the job, we can modify it using the PresetOverride property 
            // on the JobOutput.

            #region PresetOverride

            // Then we use the PresetOverride property of the JobOutput to pass in the override values to use on this single Job 
            // without the need to create a completely separate and new Transform with another langauge code or Mode setting. 
            // This can save a lot of complexity in your AMS account and reduce the number of Transforms used.
            JobOutput jobOutput = new JobOutputAsset()
            {
                AssetName = outputAsset.Name,
                PresetOverride = preset
            };

            AnlyzerJob = await SubmitJobAsync(client, config.ResourceGroup, config.AccountName, transformName, jobName, inputAssetName, jobOutput);

            #endregion PresetOverride

            try
            {
                var storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                   config.StorageAccountName, config.StorageAccountKey);
                var blobContainerName = config.StorageContainerName;

                BlobContainerClient storageClient = new BlobContainerClient(
                    storageConnectionString,
                    blobContainerName);
                // Get the latest status of the job.
                AnlyzerJob = await client.Jobs.GetAsync(config.ResourceGroup, config.AccountName, transformName, jobName);
                Console.WriteLine("Polling job status...");
                AnlyzerJob = await WaitForJobToFinishAsync(client, config.ResourceGroup, config.AccountName, transformName, jobName);
            }
            catch (Exception e){
                Console.WriteLine(e.Message);
            }
      
        }


        /// <summary>
        /// If the specified transform exists, get that transform.
        /// If the it does not exist, creates a new transform with the specified output. 
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The name of the transform.</param>
        /// <returns></returns>
        private async Task<Transform> GetOrCreateTransformAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            Preset preset)
        {

            // Start by defining the desired outputs.
            TransformOutput[] outputs = new TransformOutput[]
            {
                new TransformOutput(preset),
            };

            // Does a Transform already exist with the desired name? This method will just overwrite (Update) the Transform if it exists already. 
            // In production code, you may want to be cautious about that. It really depends on your scenario.
            Transform transform = await client.Transforms.CreateOrUpdateAsync(resourceGroupName, accountName, transformName, outputs);

            return transform;
        }

        /// <summary>
        /// Creates a new input Asset and uploads the specified local media file into it.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="assetName">The asset name.</param>
        /// <param name="fileToUpload">The file you want to upload into the asset.</param>
        /// <returns></returns>
        private async Task<Asset> CreateInputAssetAsync(
            IAzureMediaServicesClient client,
            string  resourceGroupName,
            string  accountName,
            string  assetName,
            string  fileToUpload,
            bool    isByteArray,
            byte[]? byteArrayData = null)

        {
            // In this example, we are assuming that the asset name is unique.
            //
            // If you already have an asset with the desired name, use the Assets.Get method
            // to get the existing asset. In Media Services v3, the Get method on entities will return an ErrorResponseException if the resource is not found. 
            Asset asset;

            try
            {
                asset = await client.Assets.GetAsync(resourceGroupName, accountName, assetName);

                // The asset already exists and we are going to overwrite it. In your application, if you don't want to overwrite
                // an existing asset, use an unique name.
                Console.WriteLine($"Warning: The asset named {assetName} already exists. It will be overwritten.");

            }
            catch (ErrorResponseException)
            {
                // Call Media Services API to create an Asset.
                // This method creates a container in storage for the Asset.
                // The files (blobs) associated with the asset will be stored in this container.
                Console.WriteLine("Creating an input asset...");
                asset = await client.Assets.CreateOrUpdateAsync(resourceGroupName, accountName, assetName, new Asset());
            }

            // Use Media Services API to get back a response that contains
            // SAS URL for the Asset container into which to upload blobs.
            // That is where you would specify read-write permissions 
            // and the expiration time for the SAS URL.
            var response = await client.Assets.ListContainerSasAsync(
                resourceGroupName,
                accountName,
                assetName,
                permissions: AssetContainerPermission.ReadWrite,
                expiryTime: DateTime.UtcNow.AddHours(4).ToUniversalTime());

            var sasUri = new Uri(response.AssetContainerSasUrls.First());

            // Use Storage API to get a reference to the Asset container
            // that was created by calling Asset's CreateOrUpdate method.  
            BlobContainerClient container = new(sasUri);
            

            // Use Storage API to upload the file into the container in storage.
            Console.WriteLine("Uploading a media file to the asset...");
            if (!isByteArray)
            {
                BlobClient blob = container.GetBlobClient(Path.GetFileName(fileToUpload));
                await blob.UploadAsync(fileToUpload);
            }
            else
            {
                BlobClient blob = container.GetBlobClient(fileToUpload);
                await blob.UploadAsync(new BinaryData(byteArrayData));
            }
            return asset;
        }

        /// <summary>
        /// Creates an output asset. The output from the encoding Job must be written to an Asset.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="assetName">The output asset name.</param>
        /// <returns></returns>
        private async Task<Asset> CreateOutputAssetAsync(IAzureMediaServicesClient client, string resourceGroupName, string accountName, string assetName)
        {
            Asset outputAsset = new Asset();
            Console.WriteLine("Creating an output asset...");
            return await client.Assets.CreateOrUpdateAsync(resourceGroupName, accountName, assetName, outputAsset);
        }

        /// <summary>
        /// Submits a request to Media Services to apply the specified Transform to a given input video.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The name of the transform.</param>
        /// <param name="jobName">The (unique) name of the job.</param>
        /// <param name="inputAssetName"></param>
        /// <param name="outputAssetName">The (unique) name of the  output asset that will store the result of the encoding job. </param>
        // <SubmitJob>

        private async Task<Job> SubmitJobAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            string jobName,
            string inputAssetName,
            JobOutput jobOutput)
        {
            ClipTime end = null;
            JobInput jobInput;
            if (AnalyzerInput.IsHttpJob)
            {
                jobInput = new JobInputHttp(files: new[] { AnalyzerInput.InputFileUrl});
            }
            else
            {
                jobInput = new JobInputAsset(assetName: inputAssetName);
            }


            JobOutput[] jobOutputs =
            {
                jobOutput
            };

            // In this example, we are assuming that the job name is unique.
            //
            // If you already have a job with the desired name, use the Jobs.Get method
            // to get the existing job. In Media Services v3, Get methods on entities returns ErrorResponseException 
            // if the entity doesn't exist (a case-insensitive check on the name).
            Job job;

            try
            {
                job = await client.Jobs.CreateAsync(
                         resourceGroupName,
                         accountName,
                         transformName,
                         jobName,
                         new Job
                         {
                             Input = jobInput,
                             Outputs = jobOutputs,
                         });
            }
            catch (Exception exception)
            {
                if (exception.GetBaseException() is ErrorResponseException apiException)
                {
                    Console.WriteLine(
                        $"ERROR: API call failed with error code '{apiException.Body.Error.Code}' and message '{apiException.Body.Error.Message}'.");
                }
                throw;
            }

            return job;
        }

        /// <summary>
        /// Polls Media Services for the status of the Job.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The name of the transform.</param>
        /// <param name="jobName">The name of the job you submitted.</param>
        /// <returns></returns>
        private async Task<Job> WaitForJobToFinishAsync(IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string transformName,
            string jobName)
        {
            const int SleepIntervalMs = 30 * 1000;

            Job job;

            do
            {
                job = await client.Jobs.GetAsync(resourceGroupName, accountName, transformName, jobName);

                Debug.WriteLine($"Job is '{job.State}'.");
                for (int i = 0; i < job.Outputs.Count; i++)
                {
                    JobOutput output = job.Outputs[i];
                    Console.Write($"\tJobOutput[{i}] is '{output.State}'.");
                    if (output.State == JobState.Processing)
                    {
                        Console.Write($"  Progress: '{output.Progress}'.");
                    }

                    Console.WriteLine();
                }

                if (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled)
                {
                    await Task.Delay(SleepIntervalMs);
                }
            }
            while (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled);

            return job;
        }

        public async Task GenerateDataBaseEntryAsync()
        {
            if (AnlyzerJob.State == JobState.Finished)
            {
                await DownloadOutputAssetAsync(
                    AzureMediaServicesClient,
                    Config.ResourceGroup,
                    Config.AccountName,
                    AnalyzerAsset.OutputAssetName,
                    StorageType.DataBase);
            }
        }
        public async Task GreateLocalCopyAsync(string outputFolder = OutputFolder)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
           
            if (AnlyzerJob.State == JobState.Finished)
            {
                await DownloadOutputAssetAsync(
                    AzureMediaServicesClient,
                    Config.ResourceGroup,
                    Config.AccountName,
                    AnalyzerAsset.OutputAssetName,
                    StorageType.Local,
                    outputFolder);
            }
        }
        public async Task<string> GetJson()
        {
            if (AnlyzerJob.State == JobState.Finished)
            {
               return await DownloadOutputAssetAsync(
                    AzureMediaServicesClient,
                    Config.ResourceGroup,
                    Config.AccountName,
                    AnalyzerAsset.OutputAssetName,
                    StorageType.String
                    );
            }
            return String.Empty;    
        }

        /// <summary>
        ///  Downloads the results from the specified output asset, so you can see what you got.
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="assetName">The output asset.</param>
        /// <param name="outputFolderName">The name of the folder into which to download the results.</param>

        private async Task<string> DownloadOutputAssetAsync(
            IAzureMediaServicesClient client,
            string resourceGroup,
            string accountName,
            string assetName,
            StorageType storageType,
            string? outputFolderName = null
            )
        {
            AssetContainerSas assetContainerSas = await client.Assets.ListContainerSasAsync(
                resourceGroup,
                accountName,
                assetName,
                permissions: AssetContainerPermission.Read,
                expiryTime: DateTime.UtcNow.AddHours(1).ToUniversalTime());

            Uri containerSasUrl = new(assetContainerSas.AssetContainerSasUrls.FirstOrDefault());
            BlobContainerClient container = new(containerSasUrl);
            string ret = string.Empty;
          
           
            if (storageType == StorageType.Local)
            {
                string directory = Path.Combine(outputFolderName, assetName);
                Directory.CreateDirectory(directory);
                Debug.WriteLine($"Downloading output results to '{directory}'...");
                await SaveOutputAssetLocalAsync(container, directory);
            }
            else if (storageType == StorageType.DataBase)
            {
                await SaveOutputAssetAsStringAsync(container, true);
            }
            else
            {
                ret = SaveOutputAssetAsStringAsync(container, false).Result;
            }

            Debug.WriteLine("Download complete.");
            return ret; 
           
        }
        private async Task<string> SaveOutputAssetAsStringAsync(BlobContainerClient container, bool saveInDataBase)
        {
            string continuationToken = null;
            IList<Task> downloadTasks = new List<Task>();
            byte[] responseContent = null;
            string ret = string.Empty;
            do
            {
                var resultSegment = container.GetBlobs().AsPages(continuationToken);
                string tarnscriptVTT = string.Empty;
                string transcriptText = string.Empty;
                List<FacesNames> facesNames = new List<FacesNames>();

                foreach (Azure.Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        var blobClient = container.GetBlobClient(blobItem.Name);
                        try
                        {
                            Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync();
                            responseContent = response.Value.Content.ToArray();
                        }
                        catch
                        {

                        }

                        if (blobItem.Name.Equals("transcript.vtt"))
                        {
                            Console.WriteLine("save vtt in Data Base");

                            tarnscriptVTT = Encoding.UTF8.GetString(responseContent, 0, responseContent.Length);
                        }
                        if (blobItem.Name.Equals("insights.json"))
                        {
                            Console.WriteLine("save vtt Text in Data Base");
                            var insights = Encoding.UTF8.GetString(responseContent, 0, responseContent.Length);
                            JObject json = JsonConvert.DeserializeObject<JObject>(insights);
                            if (json.ContainsKey("transcript"))
                            {
                                foreach (var item in json["transcript"])
                                {
                                    transcriptText += " " + item["text"];
                                }
                                Console.WriteLine(transcriptText);
                            }
                        }
                        if (blobItem.Name.Contains(".jpg"))
                        {

                            FacesNames face = new()
                            {
                                ImageName = blobItem.Name,
                            };
                            facesNames.Add(face);
                            Console.WriteLine("Save Faces in Data Base");
                        }

                    }
                    var faces = JsonConvert.SerializeObject(facesNames.ToArray());
                    if (saveInDataBase)
                    {
                        Anlyser_Context anlyser_context = new Anlyser_Context();
                        anlyser_context.AddVideoEntries(AnalyzerInput.InputGuid, tarnscriptVTT, transcriptText, faces);
                    }
                    
                    else
                    {
                        AnalyzerOutput analyzerOutput = new()
                        {
                            TranscriptVtt = tarnscriptVTT,
                            TranscriptText = transcriptText,
                            Faces_Names = faces
                        };
                        ret = JsonConvert.SerializeObject(analyzerOutput);
                    }
                    // Get the continuation token and loop until it is empty.
                    continuationToken = blobPage.ContinuationToken;
                }

            } while (continuationToken != "");
            return ret;
        }
        ///<summary>
        /// Save the return data to the local storage.
        /// </summary>
        
        private async Task SaveOutputAssetLocalAsync(BlobContainerClient container, string directory)
        {
            string continuationToken = null;
            IList<Task> downloadTasks = new List<Task>();
            do
            {
                var resultSegment = container.GetBlobs().AsPages(continuationToken);

                foreach (Azure.Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        var blobClient = container.GetBlobClient(blobItem.Name);
                        string filename = Path.Combine(directory, blobItem.Name);

                        downloadTasks.Add(blobClient.DownloadToAsync(filename));
                    }
                    // Get the continuation token and loop until it is empty.
                    continuationToken = blobPage.ContinuationToken;
                }


            } while (continuationToken != "");

            await Task.WhenAll(downloadTasks);
        }

        /// Deletes the jobs and assets that were created.
        /// Generally, you should clean up everything except objects 
        /// that you are planning to reuse (typically, you will reuse Transforms, and you will persist StreamingLocators).
        /// </summary>
        public async Task CleanUpAsync()
        {
            if (AnlyzerJob.State == JobState.Finished)
            {
                await CleanUpAsync(
                        AzureMediaServicesClient,
                        Config.ResourceGroup,
                        Config.AccountName,
                        AnalyzerAsset.TransformName,
                        AnalyzerAsset.InputAssetName,
                        AnalyzerAsset.OutputAssetName,
                        AnalyzerAsset.JobName);
            }
        }
        ///<summary>
        /// Deletes the jobs that were created.
        /// </summary>
        public async Task DeleteJobAsync() 
        {
            if (AnlyzerJob.State == JobState.Finished)
            {
                await CleanUpAsync(
                        AzureMediaServicesClient,
                        Config.ResourceGroup,
                        Config.AccountName, 
                        transformName: AnalyzerAsset.TransformName,
                        jobName: AnalyzerAsset.JobName);
            }
        }
        ///<summary>
        /// Deletes the input_asset that were created.
        /// </summary>
        public async Task DeleteInputAssetAsync()
        {
            if (AnlyzerJob.State == JobState.Finished)
            {
                await CleanUpAsync(
                        AzureMediaServicesClient,
                        Config.ResourceGroup,
                        Config.AccountName,
                        inputAssetName:AnalyzerAsset.InputAssetName);
            }
        }
        ///<summary>
        /// Deletes the output_asset that were created.
        /// </summary>
        public async Task DeleteOutputAssetAsync()
        {
            if (AnlyzerJob.State == JobState.Finished)
            {
                await CleanUpAsync(AzureMediaServicesClient,
                        Config.ResourceGroup,
                        Config.AccountName,
                        outputAssetName: AnalyzerAsset.OutputAssetName);
            }
        }
        ///<summary>
        /// Deletes the transformation that were created.
        /// </summary>
        public async Task DeleteTransformAsync()
        {
            if (AnlyzerJob.State == JobState.Finished)
            {
                await CleanUpAsync(AzureMediaServicesClient,
                        Config.ResourceGroup,
                        Config.AccountName,
                        transformName: AnalyzerAsset.TransformName);
            }
        }

        /// Deletes the jobs and assets that were created.
        /// Generally, you should clean up everything except objects 
        /// that you are planning to reuse (typically, you will reuse Transforms, and you will persist StreamingLocators).
        /// </summary>
        /// <param name="client">The Media Services client.</param>
        /// <param name="resourceGroupName">The name of the resource group within the Azure subscription.</param>
        /// <param name="accountName"> The Media Services account name.</param>
        /// <param name="transformName">The transform name.</param>
        /// <param name="inputAssetName">The input asset name.</param>
        /// <param name="outputAssetName">The output asset name.</param>
        /// <param name="jobName">The job name.</param>
        private async Task CleanUpAsync(
            IAzureMediaServicesClient client,
            string resourceGroupName,
            string accountName,
            string? transformName = null,
            string? inputAssetName = null,
            string? outputAssetName = null,
            string? jobName = null)
        {
            Console.WriteLine("Cleaning up...");
            Console.WriteLine();
            if (!string.IsNullOrEmpty(jobName))
            {
                Console.WriteLine($"Deleting Job: {jobName}");
                await client.Jobs.DeleteAsync(resourceGroupName, accountName, transformName, jobName);
            }
            if (!string.IsNullOrEmpty(inputAssetName))
            {
                Console.WriteLine($"Deleting input asset: {inputAssetName}");
                await client.Assets.DeleteAsync(resourceGroupName, accountName, inputAssetName);
            }
            if (!string.IsNullOrEmpty(outputAssetName))
            {
                Console.WriteLine($"Deleting output asset: {outputAssetName}");
                await client.Assets.DeleteAsync(resourceGroupName, accountName, outputAssetName);
            }
            if (!string.IsNullOrEmpty(transformName))
            {
                Console.WriteLine($"Deleting Transform: {transformName}.");
                await client.Transforms.DeleteAsync(resourceGroupName, accountName, transformName);
            }
        }
    }
}
