[![Build status](https://ci.appveyor.com/api/projects/status/7wm2f2d6913wwspo/branch/master?svg=true)](https://ci.appveyor.com/project/olegtarasov/yandexcloudapi/branch/master)

# Yandex Cloud .NET API

This is a .NET Standard 2.0 Yandex Cloud API wrapper. There is not much stuff right now, because I only need SpeechKit. Nevertheless, wrapper architecture is versatile and can accomodate all cloud APIs.

## Async

All methods are asynchronous.

## Authentication

Only full account (someone@yandex.ru) authentication is currently supported. Service account token retrieval is more complicated and I don't have time to implement JWT token management for service accounts at this moment. Will appreciate the help with this.

Authorization works through `IAuthorizer` interface. Right now there is only one implementation: `FullAccountAuthorizer`. You use it with OAuth token as per docs: https://cloud.yandex.ru/docs/iam/operations/iam-token/create. 

```csharp
var fullAuthorizer = new FullAccountAuthorizer(_configuration["OAuthToken"]);
var speechKit = new SpeechKitApi(_fullAuthorizer, _configuration["FolderId"]);
```

Of course, you don't store your tokens in plain config. You should use .NET Core secrets. For example, read: https://cmatskas.com/configure-and-use-user-secrets-in-net-core-2-0-console-apps-in-development. `ApiTest` project implements this approach.

## Error handling

Expect all meaningful API errors to be wrapped in `ApiException`. Audio converter errors are wrapped in `ConverterException`. Treat all other exceptions as unexpected.

## SpeechKit

The primary purpose of this wrapper is an ability to recognize and synthesize speech through SpeechKit API. Use `SpeechKitApi` as your entry point.

For example, to recognize text from speech:

```csharp
var speechKit = new SpeechKitApi(_fullAuthorizer, _configuration["FolderId"]);
string text = await speechKit.RecognizeTextAsync(data, AudioFormat.PCM);
```

To synthesize speech from text:

```csharp
var speechKit = new SpeechKitApi(_fullAuthorizer, _configuration["FolderId"]);
var data = await speechKit.SynthesizeSpeechAsync(
                text, 
                AudioFormat.PCM,
                speed: 0.8f,
                emotion: Emotion.Good);

// In this example *data* will contain raw PCM stream.
```

### Using OPUS format

If you want to recognize a phrase with more than 7-10 words in it, you'll have to use OPUS encoding, as PCM data stream quickly grows over allowed 1 MB file size. OPUS turned out to be a real pain in the butt in .NET, so I ended up using `opusec.exe`. Saving a temporary `wav` file to disk and converting it to `ogg` was the only reliable way to get the stream that SpeechKit doesn't nag at. Using stdin and stdout streams doesn't work reliably. Using https://github.com/lostromb/concentus.oggfile doesn't work. If you find a way to **reliably** encode OPUS on .NET without `opusenc.exe`, I welcome your pull request.

**OPUS encoding works only on Windows at this moment**.

To encode raw PCM stream to OPUS, use `AudioConverter`. It expects raw PCM stream without WAV header.

```csharp
var oggData = await new AudioConverter().ConvertPcmToOpusAsync(data, waveIn.WaveFormat);
``` 

## Testing stuff

You can run `ApiTest` with different commands to test different things. But first of all, you need to store your OAuth token and folder id to app secret storage.

Fire up the terminal and navigate to `ApiTest` project folder. Then do:

```
# dotnet user-secrets set OAuthToken "<your token>"
# dotnet user-secrets set FolderId "<your folder id>"
```

Now you can build and run the app. To test speech recognition, do:

```
# ApiTest SpeechRecognition
```

To test speech synthesis, do:

```
# ApiTest TextToSpeech
```

## Unit tests

There aren't any at the moment. The code is pretty simple and testing API methods would require a working Cloud account with active payment method. And someone would have to, well, pay for those tests.