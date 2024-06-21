using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Logging;

namespace BA2LW.Utils
{
    /// <summary>
    /// Some handy utility to perform Unity Web Request.
    /// </summary>
    public static class WebRequestHelper
    {
        /// <summary>
        /// Request a file and retrieve the data as text.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Text data.</returns>
        public static async UniTask<string> GetTextData(string url)
        {
            try
            {
                using UnityWebRequest uwr = UnityWebRequest.Get(url);
                UniTaskCompletionSource<string> tcs = new UniTaskCompletionSource<string>();

                uwr.SendWebRequest().completed += _ =>
                {
                    if (uwr.result == UnityWebRequest.Result.Success)
                        tcs.TrySetResult(uwr.downloadHandler.text);
                    else
                        tcs.TrySetException(new Exception(uwr.error));
                };

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Request a file and retrieve the data as bytes.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Array of byte.</returns>
        public static async UniTask<byte[]> GetBinaryData(string url)
        {
            try
            {
                using UnityWebRequest uwr = UnityWebRequest.Get(url);
                UniTaskCompletionSource<byte[]> tcs = new UniTaskCompletionSource<byte[]>();

                uwr.SendWebRequest().completed += _ =>
                {
                    if (uwr.result == UnityWebRequest.Result.Success)
                        tcs.TrySetResult(uwr.downloadHandler.data);
                    else
                        tcs.TrySetException(new Exception(uwr.error));
                };

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Request an audio clip.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async UniTask<AudioClip> GetAudioClip(string url)
        {
            try
            {
                using UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(
                    url,
                    GetAudioType(url)
                );
                UniTaskCompletionSource<AudioClip> tcs = new UniTaskCompletionSource<AudioClip>();

                uwr.SendWebRequest().completed += _ =>
                {
                    if (uwr.result == UnityWebRequest.Result.Success)
                        tcs.TrySetResult(DownloadHandlerAudioClip.GetContent(uwr));
                    else
                        tcs.TrySetException(new Exception(uwr.error));
                };

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Automatically assign AudioType for .mp3, .ogg, and .wav files.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>AudioType</returns>
        static AudioType GetAudioType(string url)
        {
            return Path.GetExtension(url) switch
            {
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                ".wav" => AudioType.WAV,
                _ => AudioType.UNKNOWN,
            };
        }
    }
}
