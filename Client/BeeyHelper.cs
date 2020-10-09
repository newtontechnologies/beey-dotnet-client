using Beey.Client.Logging.LogProviders;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Messaging.Subsystems;
using Beey.DataExchangeModel.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client
{
    public class BeeyHelper
    {
        private static readonly Logging.ILog log = Logging.LogProvider.For<BeeyHelper>();

        /// <summary>
        /// Uploads stream and starts transcribing when ready.
        /// </summary>
        /// <param name="beey">Client with logged user.</param>
        /// <param name="data"></param>
        /// <param name="length">Length of data in bytes. Null for stream.</param>
        /// <param name="saveMedia"></param>
        /// <param name="projectName"></param>
        /// <param name="language"></param>
        /// <param name="withPpc"></param>
        /// <param name="withVad"></param>
        /// <param name="saveTrsx"></param>
        /// <param name="maxWaitingTimeMinutes">Maximum waiting time to be able to start transcribing.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>project id</returns>
        public static async Task<int> UploadAndTranscribe(BeeyClient beey,
            Stream data, long? length, bool saveMedia, string projectName,
            string language = "cs-CZ", bool withPpc = true, bool withVad = true, bool withPunctuation = true, bool saveTrsx = true,
            int maxWaitingTimeMinutes = 60, CancellationToken cancellationToken = default)
        {
            try
            {
                log.Log(Logging.LogLevel.Info, () => "Creating project.");
                var project = await beey.CreateProjectAsync(projectName, "");
                log.Log(Logging.LogLevel.Info, () => "Project {id} created.", null, project.Id);

                var cts = new CancellationTokenSource();
                cancellationToken.Register(() => cts.Cancel());
                log.Log(Logging.LogLevel.Info, () => "Uploading stream.");
                var uploading = beey.UploadStreamAsync(project.Id, projectName, data, length, saveMedia, cts.Token);

                // Wait for an hour at max.
                int maxWaitingTimeMs = maxWaitingTimeMinutes * 60 * 1000;
                int delayMs = 10000;
                int retryCount = maxWaitingTimeMs / delayMs;
                TryValueResult<ProjectProgress> result;
                // Apparently, progress in backend can be created a bit late, so wait for a bit.
                await Task.Delay(2000);

                log.Log(Logging.LogLevel.Info, () => "Waiting to be able to transcribe.");
                while ((result = await beey.GetProjectProgressStateAsync(project.Id).TryAsync())
                    && !ProcessState.Finished.HasFlag(result.Value.FileIndexingState)
                    && result.Value.MediaIdentificationState != ProcessState.Completed
                    && retryCount > 0)
                {
                    await Task.Delay(delayMs);
                    retryCount--;
                }

                if (result.Value.FileIndexingState == ProcessState.Failed)
                {
                    cts.Cancel();
                    throw new Exception("File indexing failed.");
                }
                else if (result.Value.FileIndexingState != ProcessState.Completed && result.Value.MediaIdentificationState != ProcessState.Completed)
                {
                    cts.Cancel();
                    throw new Exception($"File indexing did not finish in {(maxWaitingTimeMs / 1000) / 60} minutes.");
                }

                // Wait a bit to let the server be ready to transcribe.
                await Task.Delay(2000);
                try
                {
                    log.Log(Logging.LogLevel.Info, () => "Starting transcription.");
                    await beey.TranscribeProjectAsync(project.Id, language, withPpc, withVad, withPunctuation, saveTrsx, cts.Token);
                }
                catch (Exception)
                {
                    cts.Cancel();
                    throw;
                }

                await uploading;
                log.Log(Logging.LogLevel.Info, () => "Upload finished.");
                return project.Id;
            }
            finally
            {
                data.Dispose();
            }
        }

        public delegate void Progress(int percentage);
        public delegate void MediaIdentified(TimeSpan duration);

        /// <summary>
        /// Waits until project can be transcribed and calls transcription. Calls callback methods when events happen.
        /// </summary>
        /// <param name="beey">Client with logged user.</param>
        /// <param name="projectId"></param>
        /// <param name="language"></param>
        /// <param name="withPpc"></param>
        /// <param name="withVad"></param>
        /// <param name="withPunctuation"></param>
        /// <param name="saveTrsx"></param>
        /// <param name="onMediaIdentified">With duration. Stream has zero duration.</param>
        /// <param name="onTranscriptionStarted"></param>
        /// <param name="onUploadProgress">With percentage of upload.</param>
        /// <param name="onTranscriptionProgress">With percentage of transcription. When percentage is -1, progress is invalid probably because of discrepance between duration in media file header and real dureation.</param>
        /// <param name="onConversionFinished"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task TranscribeAsync(BeeyClient beey,
            int projectId, string language = "cs-CZ",
            bool withPpc = true, bool withVad = true, bool withPunctuation = true, bool saveTrsx = true,
            MediaIdentified? onMediaIdentified = null, Action? onTranscriptionStarted = null,
            Progress? onUploadProgress = null, Progress? onTranscriptionProgress = null,
            Action? onConversionFinished = null,
            CancellationToken cancellationToken = default)
        {
            var cts = new CancellationTokenSource();
            cancellationToken.Register(() => cts.Cancel());

            var messages = (await beey.ListenToMessages(projectId, cts.Token))
                .Select(s => JsonSerializer.Deserialize<Message>(s, Message.CreateDefaultOptions()));

            bool isTranscribing = false;

            try
            {
                // Just try to transcribe straight away, nevermind if it fails.
                await beey.TranscribeProjectAsync(projectId, language, withPpc, withVad, withPunctuation, saveTrsx, cts.Token);
                onTranscriptionStarted?.Invoke();
                isTranscribing = true;
            }
            catch (Exception)
            {
            }

            TimeSpan duration = TimeSpan.Zero;
            try
            {
                await foreach (var message in messages.WithCancellation(cts.Token))
                {
                    if (message.Type == MessageType.Failed
                        && message.Subsystem != "MediaIdentification") // TODO: Remove when backend doesn't send fail when media not in faststart.
                    {
                        throw new Exception($"{message.Subsystem} failed with reason '{((FailedMessage)message).Reason}'.");
                    }
                    if (message.Subsystem == "MediaIdentification" && message.Type == MessageType.Progress)
                    {
                        var data = MediaIdentificationData.From(message);
                        if (data.Kind == MediaIdentificationData.DurationKind.Duration
                            || data.Kind == MediaIdentificationData.DurationKind.ApproximateDuration
                            || data.Kind == MediaIdentificationData.DurationKind.DurationlessStream)
                        {
                            duration = data.Duration ?? TimeSpan.Zero;
                            onMediaIdentified?.Invoke(duration);
                            if (!isTranscribing)
                            {
                                await beey.TranscribeProjectAsync(projectId, language, withPpc, withVad, withPunctuation, saveTrsx, cts.Token);
                                isTranscribing = true;
                                onTranscriptionStarted?.Invoke();
                            }
                        }
                    }
                    if (message.Subsystem == "MediaFilePackaging" && message.Type == MessageType.Completed)
                    {
                        onConversionFinished?.Invoke();
                        if (!isTranscribing)
                        {
                            await beey.TranscribeProjectAsync(projectId, language, withPpc, withVad, withPunctuation, saveTrsx, cts.Token);
                            isTranscribing = true;
                            onTranscriptionStarted?.Invoke();
                        }
                    }
                    if (message.Subsystem == "TranscriptionTracking" && message.Type == MessageType.Completed)
                    {
                        onTranscriptionProgress?.Invoke(100);
                        break;
                    }
                    if (message.Subsystem == "Upload" && message.Type == MessageType.Progress)
                    {
                        var data = UploadSubsystemData.From(message);
                        if (data.Kind == UploadSubsystemData.DataKind.UploadedBytes)
                        {
                            onUploadProgress?.Invoke(data.UploadPercentage!.Value);
                        }
                    }
                    if (message.Subsystem == "Upload" && message.Type == MessageType.Completed)
                    {
                        onUploadProgress?.Invoke(100);
                    }
                    if (duration > TimeSpan.Zero // not stream
                        && message.Subsystem == "Recognition" && message.Type == MessageType.Progress)
                    {
                        var data = RecognitionData.From(message);
                        if (data.Transcribed.HasValue)
                        {
                            int percentage = (int)((data.Transcribed.Value.TotalSeconds * 100) / duration.TotalSeconds);
                            if (percentage > 100)
                                percentage = -1;
                            onTranscriptionProgress?.Invoke(percentage);
                        }
                    }
                }
            }
            finally
            {
                // End listening to messages.
                cts.Cancel();
            }
        }
    }
}
