using System;
using System.IO;
using UnityEngine;

namespace LudumDare.VoiceFog
{
    /// <summary>
    /// Continuous offline recognition of Russian «сигнал» via Vosk — <see cref="Microphone.Start"/> in Awake, PCM processed in <see cref="Update"/>.
    /// Model under <see cref="Application.streamingAssetsPath"/>; disabled if missing or native init fails.
    /// </summary>
    public sealed class VoskSignalKeywordSource : KeywordSourceBehaviour
    {
        const string KeywordNorm = "сигнал";

        [Tooltip("Relative to StreamingAssets, e.g. vosk-model-small-ru")]
        [SerializeField] string modelRelativePath = "vosk-model-small-ru";

        [SerializeField] string microphoneDevice = null;
        [SerializeField] int sampleRate = 16000;
        [SerializeField] [Range(128, 8192)] int chunkMaxSamples = 4096;

        [Tooltip("Cap samples fed to Vosk per frame — large chunks + AcceptWaveform on the main thread freeze the editor.")]
        [SerializeField] [Range(256, 4096)] int maxMicSamplesPerFrame = 1024;

        [Header("Logging")]
        [Tooltip("Log when Vosk output contains «сигнал» (final utterance).")]
        [SerializeField] bool logKeywordFinal = true;

        [Tooltip("Log partial hypotheses that already contain «сигнал» (can be chatty).")]
        [SerializeField] bool logKeywordPartial = false;

        Vosk.Model _model;
        Vosk.VoskRecognizer _recognizer;
        AudioClip _clip;
        string _deviceName;
        int _lastMicSample;
        bool _ready;
        float _nextPartialRaiseTime;

        void Awake()
        {
            var root = Application.streamingAssetsPath;
            var modelDir = Path.Combine(root, modelRelativePath);
            if (!Directory.Exists(modelDir))
            {
                Debug.LogWarning(
                    $"[VoskSignalKeywordSource] Model folder not found at \"{modelDir}\". " +
                    "Unpack https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip here. Keyboard fallback still works.");
                enabled = false;
                return;
            }

            if (!LooksLikeVoskModelDirectory(modelDir))
            {
                Debug.LogWarning(
                    $"[VoskSignalKeywordSource] Folder \"{modelDir}\" has no Vosk model files (expected am/final.mdl, conf/model.conf, …). " +
                    "Extract vosk-model-small-ru-0.22.zip **into** this folder so those paths exist. Keyboard fallback still works.");
                enabled = false;
                return;
            }

            try
            {
                _model = new Vosk.Model(modelDir);
                _recognizer = new Vosk.VoskRecognizer(_model, sampleRate);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VoskSignalKeywordSource] Init failed: {e.Message}");
                enabled = false;
                return;
            }

            _deviceName = string.IsNullOrEmpty(microphoneDevice) ? null : microphoneDevice;
            if (_deviceName == null && Microphone.devices.Length > 0)
                _deviceName = Microphone.devices[0];

            if (string.IsNullOrEmpty(_deviceName))
            {
                Debug.LogWarning("[VoskSignalKeywordSource] No microphone device.");
                enabled = false;
                return;
            }

            _clip = Microphone.Start(_deviceName, true, 2, sampleRate);
            if (_clip == null)
            {
                Debug.LogWarning("[VoskSignalKeywordSource] Microphone.Start failed.");
                enabled = false;
                return;
            }

            _lastMicSample = 0;
            _nextPartialRaiseTime = 0f;
            _ready = true;
            Debug.Log(
                $"[VoskSignalKeywordSource] Listening — mic \"{_deviceName}\" @ {sampleRate} Hz. Say «{KeywordNorm}».",
                this);
        }

        void OnDestroy()
        {
            if (!string.IsNullOrEmpty(_deviceName) && Microphone.IsRecording(_deviceName))
                Microphone.End(_deviceName);

            if (_recognizer != null)
            {
                try
                {
                    var tail = _recognizer.FinalResult();
                    TryRaiseFromJson(tail, true);
                }
                catch
                {
                    // ignore shutdown races
                }

                _recognizer.Dispose();
                _recognizer = null;
            }

            _model?.Dispose();
            _model = null;
        }

        void OnValidate()
        {
            maxMicSamplesPerFrame = Mathf.Clamp(maxMicSamplesPerFrame, 256, 4096);
            if (maxMicSamplesPerFrame > chunkMaxSamples)
                maxMicSamplesPerFrame = chunkMaxSamples;
        }

        void Update()
        {
            if (!_ready || _recognizer == null)
                return;

            int micPos = Microphone.GetPosition(_deviceName);
            int available = micPos - _lastMicSample;
            if (available < 0)
                available += _clip.samples;

            if (available > _clip.samples / 2)
            {
                _lastMicSample = micPos;
                return;
            }

            if (available < sampleRate / 50)
                return;

            var perFrameCap = Mathf.Min(chunkMaxSamples, maxMicSamplesPerFrame);
            int read = Mathf.Min(available, perFrameCap);
            float[] data = new float[read];
            CopyClipSamplesWrapped(_clip, _lastMicSample, data);
            _lastMicSample = (_lastMicSample + read) % _clip.samples;

            var shorts = new short[read];
            for (int i = 0; i < read; i++)
                shorts[i] = (short)(Mathf.Clamp(data[i], -1f, 1f) * short.MaxValue);

            if (_recognizer.AcceptWaveform(shorts, shorts.Length))
                TryRaiseFromJson(_recognizer.Result(), true);

            TryRaiseFromJson(_recognizer.PartialResult(), false);
        }

        static void CopyClipSamplesWrapped(AudioClip clip, int startIndex, float[] destination)
        {
            int length = destination.Length;
            int total = clip.samples;
            int firstLen = Mathf.Min(length, total - startIndex);
            if (firstLen > 0)
            {
                var first = new float[firstLen];
                clip.GetData(first, startIndex);
                Array.Copy(first, destination, firstLen);
            }

            int remaining = length - firstLen;
            if (remaining > 0)
            {
                var second = new float[remaining];
                clip.GetData(second, 0);
                Array.Copy(second, 0, destination, firstLen, remaining);
            }
        }

        void TryRaiseFromJson(string json, bool isFinalUtterance)
        {
            if (string.IsNullOrEmpty(json))
                return;

            var text = ExtractText(json);
            if (!ContainsNormalizedKeyword(text))
                return;

            if (!isFinalUtterance && Time.unscaledTime < _nextPartialRaiseTime)
                return;

            if (!isFinalUtterance)
                _nextPartialRaiseTime = Time.unscaledTime + 0.45f;

            if (isFinalUtterance ? logKeywordFinal : logKeywordPartial)
                Debug.Log(
                    $"[VoskSignalKeywordSource] Understood «{KeywordNorm}» ({(isFinalUtterance ? "final" : "partial")}). " +
                    $"Phrase: \"{text}\"",
                    this);

            RaiseKeywordSignal();
        }

        static string ExtractText(string json)
        {
            foreach (var key in new[] { "\"text\"", "\"partial\"" })
            {
                int i = json.IndexOf(key, StringComparison.Ordinal);
                if (i < 0)
                    continue;

                int colon = json.IndexOf(':', i);
                if (colon < 0)
                    continue;

                int start = json.IndexOf('"', colon + 1);
                if (start < 0)
                    continue;

                int end = json.IndexOf('"', start + 1);
                if (end <= start)
                    continue;

                return json.Substring(start + 1, end - start - 1);
            }

            return json;
        }

        static bool ContainsNormalizedKeyword(string phrase)
        {
            if (string.IsNullOrEmpty(phrase))
                return false;

            var lower = phrase.Trim().ToLowerInvariant();
            return lower.Contains(KeywordNorm);
        }

        /// <summary>
        /// Vosk logs an error and may SIGSEGV in <see cref="Vosk.VoskRecognizer"/> if the path is a placeholder folder.
        /// Only touch native APIs after these files exist.
        /// </summary>
        static bool LooksLikeVoskModelDirectory(string modelDir)
        {
            var am = Path.Combine(modelDir, "am", "final.mdl");
            var conf = Path.Combine(modelDir, "conf", "model.conf");
            return File.Exists(am) && File.Exists(conf);
        }
    }
}
