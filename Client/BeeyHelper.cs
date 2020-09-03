using Beey.Client.Logging.LogProviders;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        /// <returns></returns>
        public static async Task UploadAndTranscribe(BeeyClient beey,
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
            }
            finally
            {
                data.Dispose();
            }
        }
    }
}
