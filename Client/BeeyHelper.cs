﻿using Beey.Client.Logging.LogProviders;
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
        /// <param name="onMediaIdentified">With duration. Stream has zero duration. Might occure second time if value in media header was incorrect.</param>
        /// <param name="onTranscriptionStarted"></param>
        /// <param name="onUploadProgress">With uploaded bytes and percentage of upload. For stream, percentage is -1.</param>
        /// <param name="onTranscriptionProgress">With percentage of transcription. Percentage is -1 for streams or if progress is invalid probably because of discrepance between duration in media file header and real dureation.</param>
        /// <param name="onConversionCompleted"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task TranscribeAsync(BeeyClient beey,
            int projectId, string language = "cs-CZ",
            bool withPpc = true, bool withVad = true, bool withPunctuation = true, bool saveTrsx = true,
            Action<TimeSpan>? onMediaIdentified = null, Action? onTranscriptionStarted = null,
            Action<long, int?>? onUploadProgress = null, Action<int>? onTranscriptionProgress = null,
            Action? onUploadCompleted = null, Action? onConversionCompleted = null,
            Action? onTranscriptionCompleted = null, TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var cts = new CancellationTokenSource();
            cancellationToken.Register(() => cts.Cancel());

            var messages = (await beey.ListenToMessages(projectId, cts.Token))
                .Select(s => JsonSerializer.Deserialize<Message>(s, Message.CreateDefaultOptions()));

            bool isTranscribing = false;
            TimeSpan? duration = await TryGetDurationFromProjectAsync(beey, projectId, cancellationToken);
            if (duration != null)
            {
                onMediaIdentified?.Invoke(duration.Value);
            }
            try
            {
                // Try to transcribe straight away.
                await beey.TranscribeProjectAsync(projectId, language, withPpc, withVad, withPunctuation, saveTrsx, cts.Token);
                onTranscriptionStarted?.Invoke();
                isTranscribing = true;
            }
            catch (Exception)
            {
                // Nevermind if it fails.
            }

            try
            {
                if (timeout.HasValue)
                    cts.CancelAfter(timeout.Value);
                await foreach (var message in messages)
                {
                    // Disable cancelling while processing message.
                    if (timeout.HasValue)
                        cts.CancelAfter(TimeSpan.FromMilliseconds(-1));

                    if (message.Type == MessageType.Failed
                        && message.Subsystem != "MediaIdentification") // TODO: Remove when backend doesn't send fail when media not in faststart.
                    {
                        throw new Exception($"{message.Subsystem} failed with reason '{((FailedMessage)message).Reason}'.");
                    }
                    else if (message.Subsystem == "MediaIdentification" && message.Type == MessageType.Progress)
                    {
                        if (TryGetDuration(message, out var d))
                        {
                            if (duration == null)
                            {
                                duration = d;
                                onMediaIdentified?.Invoke(duration.Value);
                            }
                            if (!isTranscribing)
                            {
                                await beey.TranscribeProjectAsync(projectId, language, withPpc, withVad, withPunctuation, saveTrsx, cts.Token);
                                isTranscribing = true;
                                onTranscriptionStarted?.Invoke();
                            }
                        }
                    }
                    else if (message.Subsystem == "MediaFileIndexing" && message.Type == MessageType.Completed)
                    {
                        var proj = await beey.GetProjectAsync(projectId, cancellationToken);
                        if (duration == null
                            || Math.Abs((duration.Value - proj.Length).TotalSeconds) > 1)
                        {
                            // Duration after MediaFileIndexing is correct, the one in media file header might be incorrect,
                            // so update if needed (i.e. the saved duration differs from the one after indexing).
                            duration = proj.Length;
                            onMediaIdentified?.Invoke(duration.Value);
                        }
                    }
                    else if (message.Subsystem == "MediaFilePackaging" && message.Type == MessageType.Completed)
                    {
                        onConversionCompleted?.Invoke();
                        if (!isTranscribing)
                        {
                            await beey.TranscribeProjectAsync(projectId, language, withPpc, withVad, withPunctuation, saveTrsx, cts.Token);
                            isTranscribing = true;
                            onTranscriptionStarted?.Invoke();
                        }
                    }
                    else if (message.Subsystem == "TranscriptionTracking" && message.Type == MessageType.Completed)
                    {
                        onTranscriptionCompleted?.Invoke();
                        break;
                    }
                    else if (message.Subsystem == "Upload" && message.Type == MessageType.Progress)
                    {
                        var data = UploadSubsystemData.From(message);
                        if (data.Kind == UploadSubsystemData.DataKind.UploadedBytes)
                        {
                            onUploadProgress?.Invoke(data.FileOffset!.Value, data.UploadPercentage ?? -1);
                        }
                    }
                    else if (message.Subsystem == "Upload" && message.Type == MessageType.Completed)
                    {
                        onUploadCompleted?.Invoke();
                    }
                    else if (message.Subsystem == "Recognition" && message.Type == MessageType.Progress)
                    {
                        var data = RecognitionData.From(message);
                        if (data.Transcribed.HasValue)
                        {
                            int percentage = -1;
                            if (duration.HasValue)
                            {
                                percentage = (int)((data.Transcribed.Value.TotalSeconds * 100) / duration.Value.TotalSeconds);
                                if (percentage > 100)
                                    percentage = -1;
                            }
                            onTranscriptionProgress?.Invoke(percentage);
                        }
                    }

                    if (timeout.HasValue)
                        cts.CancelAfter(timeout.Value);
                }
            }
            catch (OperationCanceledException)
            {
                if (cts.IsCancellationRequested)
                    throw new TimeoutException($"No messages in {timeout!.Value.TotalSeconds}s.");
                else throw;
            }
        }

        private static bool TryGetDuration(Message mediaIdentificationMsg, out TimeSpan duration)
        {
            var data = MediaIdentificationData.From(mediaIdentificationMsg);
            if (data.Kind == MediaIdentificationData.DurationKind.Duration
                || data.Kind == MediaIdentificationData.DurationKind.ApproximateDuration
                || data.Kind == MediaIdentificationData.DurationKind.DurationlessStream)
            {
                duration = data.Duration ?? TimeSpan.Zero;
                return true;
            }

            duration = TimeSpan.Zero;
            return false;
        }

        private static async Task<TimeSpan?> TryGetDurationFromProjectAsync(BeeyClient beey, int projectId, CancellationToken cancellationToken)
        {
            TimeSpan? result = null;
            var proj = await beey.GetProjectAsync(projectId, cancellationToken);
            if (proj.IndexFileId != null)
            {
                result = proj.Length;
            }

            if (result == null)
            {
                var oldMessages = await beey.GetProjectProgressMessagesAsync(projectId, cancellationToken: cancellationToken);
                result = oldMessages.Where(message => message.Subsystem == "MediaIdentification" && message.Type == MessageType.Progress)
                    .Select(m => new { IsDurationIdentified = TryGetDuration(m, out var d), Duration = d })
                    .FirstOrDefault(m => m.IsDurationIdentified)?.Duration;
            }

            return result;
        }
    }
}
