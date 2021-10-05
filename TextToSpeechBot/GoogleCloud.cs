using Google.Cloud.Storage.V1;
using Google.Cloud.TextToSpeech.V1;

namespace TextToSpeechBot;

internal class GoogleCloud
{
    private readonly string GOOGLE_APPLICATION_CREDENTIALS; //automatic used by client
    private readonly string GOOGLE_APPLICATION_PROJECT_ID; //automatic used by client
    private readonly TextToSpeechClient client;
    private readonly StorageClient storageClient;
    private readonly AudioConfig audioConfig;
    private readonly VoiceSelectionParams voiceSelection;
    private const string bucketSpeechName = "telegram_storage_speech";

    public GoogleCloud(string? GOOGLE_APPLICATION_CREDENTIALS, string? GOOGLE_APPLICATION_PROJECT_ID)
    {
        this.GOOGLE_APPLICATION_CREDENTIALS = GOOGLE_APPLICATION_CREDENTIALS ?? throw new ArgumentNullException();
        this.GOOGLE_APPLICATION_PROJECT_ID = GOOGLE_APPLICATION_PROJECT_ID ?? throw new ArgumentNullException();

        client = TextToSpeechClient.Create();
        storageClient = StorageClient.Create();
        try
        {
            storageClient.CreateBucket(GOOGLE_APPLICATION_PROJECT_ID, bucketSpeechName);
        }
        catch
        {
            storageClient.GetBucket(bucketSpeechName);
        }
        #region Default properties

        audioConfig = new AudioConfig
        {
            AudioEncoding = AudioEncoding.Mp3
        };

        //All uk-UA voices : (uk-UA-Wavenet-A (Female) || uk-UA-Standart-A (Female))
        voiceSelection = new VoiceSelectionParams
        {
            LanguageCode = "uk-UA",
            SsmlGender = SsmlVoiceGender.Female,
            Name = "Wavenet"
        };

        #endregion
    }

    public Stream GetSpeech(string text)
    {
        SynthesisInput input = new()
        {
            Text = text
        };

        SynthesizeSpeechResponse? response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);

        return new MemoryStream(response.AudioContent.ToByteArray());
    }

    public async Task<string> UploadFileAsync(Stream stream, string fileNameForStorage)
    {
        fileNameForStorage = fileNameForStorage.Trim().Replace(" ", "_");
        var uploadOptions = new UploadObjectOptions { PredefinedAcl = PredefinedObjectAcl.PublicRead };
        var dataObject = await storageClient.UploadObjectAsync(bucketSpeechName, fileNameForStorage, null, stream, uploadOptions);

        //auto delete
        new Thread(async () =>
        {
            Thread.Sleep(60 * 1000); //1 minute
            try
            {
                await DeleteFileAsync(fileNameForStorage);
            }
            catch { }
        }).Start();

        return dataObject.MediaLink;
    }

    private async Task DeleteFileAsync(string fileNameForStorage)
    {
        try
        {
            await storageClient.DeleteObjectAsync(bucketSpeechName, fileNameForStorage);
        }
        catch { throw; }
    }


}
