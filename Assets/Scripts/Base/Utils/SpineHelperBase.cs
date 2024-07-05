using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NikkeViewerEX.Utils
{
    /// <summary>
    /// Spine related helper methods.
    /// </summary>
    public class SpineHelperBase
    {
        /// <summary>
        /// Get the Skeleton asset version.
        /// </summary>
        /// <param name="skelPath"></param>
        /// <returns>Skeleton asset version.</returns>
        public async UniTask<string> GetSkelVersion(string skelPath)
        {
            byte[] skelData = new byte[3];
            await using FileStream fs = new(skelPath, FileMode.Open, FileAccess.Read);
            fs.Seek(9, SeekOrigin.Begin);
            fs.Read(skelData, 0, 3);
            return Encoding.UTF8.GetString(skelData);
        }

        /// <summary>
        /// Get distance between 2 vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">Teh second vector.</param>
        /// <returns>The distance.</returns>
        public static float GetDistance(Vector3 a, Vector3 b)
        {
            return (a - b).magnitude;
        }

        /// <summary>
        /// Get midpoint between 2 vectors.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>Midpoint.</returns>
        public static Vector3 GetMidpoint(Vector3 a, Vector3 b)
        {
            // float x2 = a.x + b.x;
            // float y2 = a.y + b.y;
            // return new Vector3(x2, y2, 0) / 2;
            return (a + b) / 2f;
        }

        /// <summary>
        /// Calculates the angle in degrees between two vectors.
        /// </summary>
        /// <param name="l">The first vector.</param>
        /// <param name="r">The second vector.</param>
        /// <returns>The angle in degrees between the two vectors.</returns>
        public static float GetAngle(Vector3 a, Vector3 b)
        {
            // Vector3 dir = l - r;
            // dir = dir.normalized;
            // float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // angle = angle < 0 ? angle + 360 : angle;
            // return angle;

            Vector3 direction = (a - b).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return (angle + 360) % 360;
        }
    }
}
