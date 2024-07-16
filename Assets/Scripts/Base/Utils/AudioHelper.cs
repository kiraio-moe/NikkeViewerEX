using UnityEngine;

namespace NikkeViewerEX.Utils
{
    public static class AudioHelper
    {
        static bool IsReversePitch(AudioSource source)
        {
            return source.pitch < 0f;
        }

        /// <summary>
        /// Get audio clip playing remaining time.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static float GetClipRemainingTime(AudioSource source)
        {
            // Calculate the remainingTime of the given AudioSource,
            // if we keep playing with the same pitch.
            float remainingTime = (source.clip.length - source.time) / source.pitch;
            Debug.Log(
                IsReversePitch(source) ? (source.clip.length + remainingTime) : remainingTime
            );
            return IsReversePitch(source) ? (source.clip.length + remainingTime) : remainingTime;
        }
    }
}
